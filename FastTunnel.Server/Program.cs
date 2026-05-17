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
    public static void Main(string[] args)
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

            var dbPath = System.IO.Path.Combine(AppContext.BaseDirectory, "data", "fasttunnel.db");
            var dbDir = System.IO.Path.GetDirectoryName(dbPath);
            if (!System.IO.Directory.Exists(dbDir)) System.IO.Directory.CreateDirectory(dbDir!);
            builder.Services.AddDbContext<FastTunnelDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            builder.Services.AddSingleton<FastTunnel.Api.Services.TotpService>();
            builder.Services.AddSingleton<FastTunnel.Api.Filters.CustomExceptionFilterAttribute>();

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
                db.Database.EnsureCreated();

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

                if (!db.Tokens.Any())
                {
                    var config = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptionsMonitor<FastTunnel.Core.Config.DefaultServerConfig>>().CurrentValue;
                    if (config.Tokens != null)
                    {
                        foreach (var t in config.Tokens)
                        {
                            db.Tokens.Add(new Api.Models.Entities.TokenEntity
                            {
                                Value = t,
                                Description = "配置文件导入",
                            });
                        }
                        db.SaveChanges();
                    }
                }
            }

            var supportedCultures = new[] { "zh-CN", "en-US" };
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture("zh-CN")
                .AddSupportedCultures(supportedCultures)
                .AddSupportedUICultures(supportedCultures);
            app.UseRequestLocalization(localizationOptions);

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
