using System.Text.Json;
using FastTunnel.Api.Data;
using FastTunnel.Api.Models.Entities;
using FastTunnel.Core;
using FastTunnel.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace FastTunnel.Api.Services;

/// <summary>
///     按客户端 Token 从数据库读取隧道配置。
///     使用 IServiceScopeFactory 自建作用域（LoginHandler 从根容器解析，不能直接注入 Scoped DbContext）。
/// </summary>
public class ClientConfigProvider(IServiceScopeFactory scopeFactory) : IClientConfigProvider
{
    public async Task<List<ForwardConfig>?> GetForwardsAsync(string token)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FastTunnelDbContext>();
        var list = await db.ForwardTunnels
            .Where(x => x.ClientToken == token && x.IsEnabled)
            .ToListAsync();

        return list.Select(x => new ForwardConfig
        {
            LocalIp = x.LocalIp,
            LocalPort = x.LocalPort,
            RemotePort = x.RemotePort,
            Protocol = Enum.TryParse<ProtocolEnum>(x.Protocol, true, out var p) ? p : ProtocolEnum.TCP,
        }).ToList();
    }

    public async Task<List<WebConfig>?> GetWebsAsync(string token)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FastTunnelDbContext>();
        var list = await db.WebTunnels
            .Where(x => x.ClientToken == token && x.IsEnabled)
            .ToListAsync();

        return list.Select(x => new WebConfig
        {
            SubDomain = x.SubDomain,
            LocalIp = x.LocalIp,
            LocalPort = x.LocalPort,
            WWW = DeserializeWwws(x.WwwsJson),
        }).ToList();
    }

    private static string[] DeserializeWwws(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch { return []; }
    }
}
