using FastTunnel.Api.Data;
using FastTunnel.Api.Models.Entities;
using FastTunnel.Api.Resources;
using FastTunnel.Api.Services;
using FastTunnel.Api.Models;
using FastTunnel.Api.Utils;
using FastTunnel.Core;
using FastTunnel.Core.Client;
using FastTunnel.Core.Extensions;
using FastTunnel.Core.Models;
using FastTunnel.Core.Models.Massage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace FastTunnel.Api.Controllers;

public class TunnelsController(FastTunnelDbContext db, IStringLocalizer<ApiMessages> localizer, FastTunnelServer ftServer, IClientConfigProvider configProvider) : BaseController
{
    private readonly IStringLocalizer<ApiMessages> _localizer = localizer;

    [HttpGet("webs")]
    public async Task<ApiResponse> GetWebs([FromQuery] string? keyword)
    {
        var query = db.WebTunnels.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(x => x.SubDomain.Contains(keyword) || x.LocalIp.Contains(keyword));

        var list = await query.OrderByDescending(x => x.Id).ToListAsync();
        ApiResponse.Data = list.Select(x => new
        {
            x.Id, x.SubDomain, x.LocalIp, x.LocalPort,
            wwws = DeserializeWwws(x.WwwsJson),
            x.ClientToken,
            clientName = TokenMasker.Mask(x.ClientToken),
            x.IsEnabled,
            createdAt = x.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        });
        ApiResponse.Success = true;
        return ApiResponse;
    }

    [HttpPost("webs")]
    public async Task<ApiResponse> PostWeb([FromBody] WebTunnelRequest request)
    {
        var entity = new WebTunnelEntity
        {
            SubDomain = request.SubDomain ?? string.Empty,
            LocalIp = request.LocalIp ?? string.Empty,
            LocalPort = request.LocalPort ?? 80,
            ClientToken = request.ClientToken ?? string.Empty,
            WwwsJson = request.Wwws != null ? JsonSerializer.Serialize(request.Wwws) : null,
        };
        db.WebTunnels.Add(entity);
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "create", "web_tunnel", string.Format(_localizer["Audit.CreateTunnel"], "Web", entity.SubDomain, entity.LocalIp, entity.LocalPort), GetUserName());

        ApiResponse.Data = new
        {
            entity.Id, entity.SubDomain, entity.LocalIp, entity.LocalPort,
            entity.ClientToken, entity.IsEnabled,
            createdAt = entity.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };
        ApiResponse.Success = true;
        ApiResponse.Message = $"{_localizer["Tunnel.Created"]}（{await PushConfigAsync(entity.ClientToken, CancellationToken.None)}）";
        return ApiResponse;
    }

    [HttpPut("webs/{id}")]
    public async Task<ApiResponse> PutWeb(int id, [FromBody] WebTunnelRequest request)
    {
        var entity = await db.WebTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();

        if (request.SubDomain != null) entity.SubDomain = request.SubDomain;
        if (request.LocalIp != null) entity.LocalIp = request.LocalIp;
        if (request.LocalPort.HasValue) entity.LocalPort = request.LocalPort.Value;
        if (request.ClientToken != null) entity.ClientToken = request.ClientToken;
        if (request.Wwws != null) entity.WwwsJson = JsonSerializer.Serialize(request.Wwws);

        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "update", "web_tunnel", string.Format(_localizer["Audit.UpdateTunnel"], "Web", id), GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = $"{_localizer["Tunnel.Updated"]}（{await PushConfigAsync(entity.ClientToken, CancellationToken.None)}）";
        return ApiResponse;
    }

    [HttpDelete("webs/{id}")]
    public async Task<ApiResponse> DeleteWeb(int id)
    {
        var entity = await db.WebTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();
        entity.IsEnabled = false;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "delete", "web_tunnel", string.Format(_localizer["Audit.DeleteTunnel"], "Web", id), GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = $"{_localizer["Tunnel.Deleted"]}（{await PushConfigAsync(entity.ClientToken, CancellationToken.None)}）";
        return ApiResponse;
    }

    [HttpPatch("webs/{id}/toggle")]
    public async Task<ApiResponse> ToggleWeb(int id, [FromBody] ToggleRequest request)
    {
        var entity = await db.WebTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();
        entity.IsEnabled = request.IsEnabled;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "toggle", "web_tunnel", string.Format(_localizer["Audit.ToggleTunnel"], entity.IsEnabled ? _localizer["Common.Enabled"] : _localizer["Common.Disabled"], "Web", id), GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Data = new { entity.Id, entity.IsEnabled };
        ApiResponse.Message = $"{_localizer["Tunnel.Updated"]}（{await PushConfigAsync(entity.ClientToken, CancellationToken.None)}）";
        return ApiResponse;
    }

    [HttpGet("forwards")]
    public async Task<ApiResponse> GetForwards([FromQuery] string? keyword)
    {
        var query = db.ForwardTunnels.AsQueryable();
        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(x => x.LocalIp.Contains(keyword) || x.RemotePort.ToString().Contains(keyword));

        var list = await query.OrderByDescending(x => x.Id).ToListAsync();
        ApiResponse.Data = list.Select(x => new
        {
            x.Id, x.RemotePort, x.LocalIp, x.LocalPort,
            x.Protocol,
            x.ClientToken,
            clientName = TokenMasker.Mask(x.ClientToken),
            x.IsEnabled,
            createdAt = x.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        });
        ApiResponse.Success = true;
        return ApiResponse;
    }

    [HttpPost("forwards")]
    public async Task<ApiResponse> PostForward([FromBody] ForwardTunnelRequest request)
    {
        var entity = new ForwardTunnelEntity
        {
            RemotePort = request.RemotePort ?? 0,
            LocalIp = request.LocalIp ?? string.Empty,
            LocalPort = request.LocalPort ?? 0,
            Protocol = request.Protocol ?? "TCP",
            ClientToken = request.ClientToken ?? string.Empty,
        };
        db.ForwardTunnels.Add(entity);
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "create", "forward_tunnel", string.Format(_localizer["Audit.CreateTunnel"], "Forward", entity.RemotePort, entity.LocalIp, entity.LocalPort), GetUserName());

        ApiResponse.Data = new
        {
            entity.Id, entity.RemotePort, entity.LocalIp, entity.LocalPort,
            entity.Protocol, entity.ClientToken, entity.IsEnabled,
            createdAt = entity.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };
        ApiResponse.Success = true;
        ApiResponse.Message = $"{_localizer["Tunnel.Created"]}（{await PushConfigAsync(entity.ClientToken, CancellationToken.None)}）";
        return ApiResponse;
    }

    [HttpPut("forwards/{id}")]
    public async Task<ApiResponse> PutForward(int id, [FromBody] ForwardTunnelRequest request)
    {
        var entity = await db.ForwardTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();

        if (request.RemotePort.HasValue) entity.RemotePort = request.RemotePort.Value;
        if (request.LocalIp != null) entity.LocalIp = request.LocalIp;
        if (request.LocalPort.HasValue) entity.LocalPort = request.LocalPort.Value;
        if (request.Protocol != null) entity.Protocol = request.Protocol;
        if (request.ClientToken != null) entity.ClientToken = request.ClientToken;

        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "update", "forward_tunnel", string.Format(_localizer["Audit.UpdateTunnel"], "Forward", id), GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = $"{_localizer["Tunnel.Updated"]}（{await PushConfigAsync(entity.ClientToken, CancellationToken.None)}）";
        return ApiResponse;
    }

    [HttpDelete("forwards/{id}")]
    public async Task<ApiResponse> DeleteForward(int id)
    {
        var entity = await db.ForwardTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();
        entity.IsEnabled = false;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "delete", "forward_tunnel", string.Format(_localizer["Audit.DeleteTunnel"], "Forward", id), GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = $"{_localizer["Tunnel.Deleted"]}（{await PushConfigAsync(entity.ClientToken, CancellationToken.None)}）";
        return ApiResponse;
    }

    [HttpPatch("forwards/{id}/toggle")]
    public async Task<ApiResponse> ToggleForward(int id, [FromBody] ToggleRequest request)
    {
        var entity = await db.ForwardTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();
        entity.IsEnabled = request.IsEnabled;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "toggle", "forward_tunnel", string.Format(_localizer["Audit.ToggleTunnel"], entity.IsEnabled ? _localizer["Common.Enabled"] : _localizer["Common.Disabled"], "Forward", id), GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Data = new { entity.Id, entity.IsEnabled };
        ApiResponse.Message = $"{_localizer["Tunnel.Updated"]}（{await PushConfigAsync(entity.ClientToken, CancellationToken.None)}）";
        return ApiResponse;
    }

    /// <summary>
    ///     按 Token 重建在线客户端的监听/路由并下发隧道配置清单。
    ///     返回下发结果描述（已下发并生效 / 客户端离线，登录时生效）。
    /// </summary>
    private async Task<string> PushConfigAsync(string token, CancellationToken cancellationToken)
    {
        var client = ftServer.Clients.FirstOrDefault(c => c.Token == token);
        if (client == null)
        {
            return "客户端离线，配置已保存，登录时生效";
        }

        var forwards = await configProvider.GetForwardsAsync(token) ?? [];
        var webs = await configProvider.GetWebsAsync(token) ?? [];

        ftServer.ApplyForwardConfig(client, forwards);
        ftServer.ApplyWebConfig(client, webs);

        var configMsg = new TunnelConfigMessage { Webs = webs, Forwards = forwards };
        await client.webSocket.SendCmdAsync(MessageType.ConfigUpdate, JsonSerializer.Serialize(configMsg), cancellationToken);
        return "已下发并生效";
    }

    private ApiResponse TunnelNotFound()
    {
        ApiResponse.Success = false;
        ApiResponse.Message = _localizer["Tunnel.NotFound"];
        return ApiResponse;
    }

    private string GetUserName() => User.FindFirst("Name")?.Value ?? "unknown";

    private static string[] DeserializeWwws(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch { return []; }
    }
}

public class WebTunnelRequest
{
    public string? SubDomain { get; set; }
    public string? LocalIp { get; set; }
    public int? LocalPort { get; set; }
    public string[]? Wwws { get; set; }
    public string? ClientToken { get; set; }
}

public class ForwardTunnelRequest
{
    public int? RemotePort { get; set; }
    public string? LocalIp { get; set; }
    public int? LocalPort { get; set; }
    public string? Protocol { get; set; }
    public string? ClientToken { get; set; }
}

public class ToggleRequest
{
    public bool IsEnabled { get; set; }
}
