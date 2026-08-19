using System.Collections.Generic;
using System.Linq;
using FastTunnel.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace FastTunnel.Api.Data;

/// <summary>
///     把数据库 SystemConfigs 表挂载为 FastTunnel 配置段的配置源。
///     配置阶段无 DI 可用，直接持有 DbContextOptions 手动建上下文。
/// </summary>
public class SystemConfigSource(DbContextOptions<FastTunnelDbContext> dbOptions) : IConfigurationSource
{
    /// <summary>
    ///     供保存接口触发重载的单例 Provider
    /// </summary>
    public SystemConfigProvider Provider { get; } = new(dbOptions);

    public IConfigurationProvider Build(IConfigurationBuilder builder) => Provider;
}

public class SystemConfigProvider : ConfigurationProvider
{
    private readonly DbContextOptions<FastTunnelDbContext> _dbOptions;

    public SystemConfigProvider(DbContextOptions<FastTunnelDbContext> dbOptions)
    {
        _dbOptions = dbOptions;
    }

    /// <summary>
    ///     内置默认值（键相对 FastTunnel 段，与旧 appsettings 一致）。
    ///     首次部署表为空时兜底使用；Program.cs 启动时播种入库。
    /// </summary>
    public static IReadOnlyDictionary<string, string> DefaultValues { get; } = new Dictionary<string, string>
    {
        ["EnableForward"] = "true",
        ["WebDomain"] = "",
        ["WebAllowAccessIps"] = "[]",
        ["Api:JWT:ClockSkew"] = "10",
        ["Api:JWT:ValidAudience"] = "https://suidao.io",
        ["Api:JWT:ValidIssuer"] = "FastTunnel",
        ["Api:JWT:IssuerSigningKey"] = "FastTunnel Admin Panel JWT Signing Key 2024!",
        ["Api:JWT:Expires"] = "120",
    };

    public override void Load()
    {
        var values = new Dictionary<string, string>();
        try
        {
            using var db = new FastTunnelDbContext(_dbOptions);
            foreach (var row in db.SystemConfigs.ToList())
            {
                values[$"FastTunnel:{row.Key}"] = row.Value;
            }
        }
        catch
        {
            // 数据库尚未就绪（首次启动配置阶段），使用默认值兜底
        }

        foreach (var kv in DefaultValues)
        {
            values.TryAdd($"FastTunnel:{kv.Key}", kv.Value);
        }

        Data = values;
    }

    /// <summary>
    ///     保存配置后调用：重新读取数据库并触发配置重载（OptionsMonitor 热更新）
    /// </summary>
    public void ReloadConfig()
    {
        Load();
        OnReload();
    }
}
