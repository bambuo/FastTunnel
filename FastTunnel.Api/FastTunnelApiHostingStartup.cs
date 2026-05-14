using System.Diagnostics;
using System.Text.Json;
using FastTunnel.Api.Data;
using FastTunnel.Api.Filters;
using FastTunnel.Api.Services;
using FastTunnel.Core.Config;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

[assembly: HostingStartup(typeof(FastTunnel.Api.FastTunnelApiHostingStartup))]

namespace FastTunnel.Api;

public class FastTunnelApiHostingStartup : IHostingStartup
{
    public void Configure(IWebHostBuilder builder)
    {
        Debug.WriteLine("FastTunnelApiHostingStartup Configured");

        builder.ConfigureServices((ctx, services) =>
        {
            services.AddControllers();

            var dbPath = Path.Combine(AppContext.BaseDirectory, "data", "fasttunnel.db");
            var dir = Path.GetDirectoryName(dbPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir!);

            services.AddDbContext<FastTunnelDbContext>(options =>
                options.UseSqlite($"Data Source={dbPath}"));

            services.AddSingleton<TotpService>();
            services.AddSingleton<CustomExceptionFilterAttribute>();

            var serverOptions = ctx.Configuration.GetSection("FastTunnel").Get<DefaultServerConfig>();
            if (serverOptions?.Api?.JWT != null)
            {
                var jwt = serverOptions.Api.JWT;
                services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                    .AddJwtBearer(options =>
                    {
                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            ValidateLifetime = true,
                            ClockSkew = TimeSpan.FromSeconds(jwt.ClockSkew),
                            ValidateIssuerSigningKey = true,
                            ValidAudience = jwt.ValidAudience,
                            ValidIssuer = jwt.ValidIssuer,
                            IssuerSigningKey = new SymmetricSecurityKey(
                                System.Text.Encoding.UTF8.GetBytes(jwt.IssuerSigningKey)),
                        };

                        options.Events = new JwtBearerEvents
                        {
                            OnChallenge = async context =>
                            {
                                context.HandleResponse();
                                context.Response.ContentType = "application/json;charset=utf-8";
                                context.Response.StatusCode = StatusCodes.Status200OK;
                                var errJson = JsonSerializer.Serialize(
                                    new { errorCode = 1, errorMessage = context.Error ?? "Token is Required" });
                                await context.Response.WriteAsync(errJson);
                            },
                        };
                    });
            }

            services.AddAuthorizationBuilder()
                .AddPolicy("MfaOnly", policy =>
                    policy.RequireClaim("scope", "mfa"));
        });
    }
}
