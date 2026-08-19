using System.Diagnostics.CodeAnalysis;
using System.Text;
using FastTunnel.Api.Data;
using FastTunnel.Core.Config;
using FastTunnel.Core.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;

namespace FastTunnel.Server;

public class Program
{
    [RequiresUnreferencedCode("")]
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.Console().WriteTo.File("Logs/log-.log", rollingInterval: RollingInterval.Day)
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions { Args = args });

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("corsPolicy", policy =>
                {
                    policy.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin()
                        .WithExposedHeaders("Set-Token");
                });
            });

            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console());

            builder.Configuration.AddJsonFile("appsettings.json", false, true);
            builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true, true);

            builder.Services.AddLocalization();

            builder.Services.AddFastTunnelServer(builder.Configuration.GetSection("FastTunnel"));
            builder.Services.AddOpenApi();

            var dbPath = System.IO.Path.Combine(AppContext.BaseDirectory, "data", "fasttunnel.db");
            var dbDir = System.IO.Path.GetDirectoryName(dbPath);
            if (!System.IO.Directory.Exists(dbDir)) System.IO.Directory.CreateDirectory(dbDir!);
            builder.Services.AddDbContext<FastTunnelDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            builder.Services.AddSingleton<FastTunnel.Api.Services.TotpService>();
            builder.Services.AddSingleton<FastTunnel.Api.Filters.CustomExceptionFilterAttribute>();
            builder.Services.AddSingleton<FastTunnel.Core.IClientConfigProvider, FastTunnel.Api.Services.ClientConfigProvider>();
            builder.Services.AddSingleton<FastTunnel.Core.ITokenValidator, FastTunnel.Api.Services.TokenValidator>();

            var jwtConfig = builder.Configuration.GetSection("FastTunnel:Api:JWT").Get<FastTunnel.Core.Config.DefaultServerConfig.JWTOptions>();
            if (jwtConfig != null)
            {
                builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromSeconds(jwtConfig.ClockSkew),
                            ValidateIssuerSigningKey = true,
                            ValidAudience = jwtConfig.ValidAudience,
                            ValidIssuer = jwtConfig.ValidIssuer,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                Encoding.UTF8.GetBytes(jwtConfig.IssuerSigningKey)),
                        };
                    });

                builder.Services.AddAuthorizationBuilder()
                    .AddPolicy("MfaOnly", policy =>
                        policy.RequireClaim("scope", "mfa"));
            }

            builder.Host.UseWindowsService();

            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<FastTunnelDbContext>();

                // 兼容已有数据库：如果数据库已存在但没有迁移历史表，
                // 说明是之前通过 EnsureCreated 创建的，需要标记初始迁移为已应用
                if (db.Database.CanConnect())
                {
                    var pendingMigrations = db.Database.GetPendingMigrations().ToList();
                    var appliedMigrations = db.Database.GetAppliedMigrations().ToList();

                    if (appliedMigrations.Count == 0 && pendingMigrations.Count > 0)
                    {
                        // 数据库存在但没有迁移记录 → 旧数据库
                        // 检查 Tokens 表是否已存在（作为判断标志）
                        try
                        {
                            var conn = db.Database.GetDbConnection();
                            await conn.OpenAsync();
                            using var cmd = conn.CreateCommand();
                            cmd.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table' AND name='Tokens'";
                            var result = await cmd.ExecuteScalarAsync();
                            if (Convert.ToInt64(result) > 0)
                            {
                                // 表已存在，说明是旧数据库，需要手动标记初始迁移为已应用
                                var initialMigration = pendingMigrations.First();
                                cmd.CommandText = @"
                                    CREATE TABLE IF NOT EXISTS ""__EFMigrationsHistory"" (
                                        ""MigrationId"" TEXT NOT NULL CONSTRAINT ""PK___EFMigrationsHistory"" PRIMARY KEY,
                                        ""ProductVersion"" TEXT NOT NULL
                                    );
                                    INSERT OR IGNORE INTO ""__EFMigrationsHistory"" (""MigrationId"", ""ProductVersion"")
                                    VALUES (@migrationId, @productVersion);";

                                cmd.Parameters.Clear();
                                var migrationIdParam = cmd.CreateParameter();
                                migrationIdParam.ParameterName = "@migrationId";
                                migrationIdParam.Value = initialMigration;
                                cmd.Parameters.Add(migrationIdParam);

                                var productVersionParam = cmd.CreateParameter();
                                productVersionParam.ParameterName = "@productVersion";
                                productVersionParam.Value = "10.0.0-preview.3.25171.1";
                                cmd.Parameters.Add(productVersionParam);

                                await cmd.ExecuteNonQueryAsync();
                            }
                            await conn.CloseAsync();
                        }
                        catch
                        {
                            // 如果检查失败，忽略，让 Migrate 自己处理
                        }
                    }
                }

                // 执行迁移（全新数据库会创建所有表，已有数据库只执行新迁移）
                db.Database.Migrate();

                if (!db.Accounts.Any())
    {
        var config = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<FastTunnel.Core.Config.DefaultServerConfig>>().CurrentValue;
        // 旧版 appsettings.json 中的明文密码不再自动导入，首次部署请通过 /setup 初始化页创建管理员
        // 如果需要从旧配置迁移，请取消下方注释：
        // if (config.Api?.Accounts != null)
        // {
        //     foreach (var acct in config.Api.Accounts)
        //     {
        //         db.Accounts.Add(new Api.Models.Entities.AccountEntity
        //         {
        //             Name = acct.Name,
        //             PasswordHash = Api.Services.PasswordService.Hash(acct.Password),
        //             CreatedAt = DateTime.UtcNow,
        //         });
        //     }
        //     db.SaveChanges();
        // }
    }

                // Token 由管理台创建（数据库 Tokens 表），不再从配置文件导入
            }

            var supportedCultures = new[] { "zh-CN", "en-US" };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture("zh-CN")
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);
            app.UseRequestLocalization(localizationOptions);

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseCors("corsPolicy");

            app.UseStaticFiles();
            app.MapFallbackToFile("index.html");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.UseFastTunnelServer();
            app.MapFastTunnelServer();

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "致命异常");
            throw;
        }
    }
}
