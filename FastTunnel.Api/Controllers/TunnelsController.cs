using FastTunnel.Api.Data;
using FastTunnel.Api.Models.Entities;
using FastTunnel.Api.Services;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace FastTunnel.Api.Controllers;

public class TunnelsController(FastTunnelDbContext db) : BaseController
{
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
            x.ClientId,
            clientName = $"Client#{x.ClientId}",
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
            ClientId = request.ClientId ?? 0,
            WwwsJson = request.Wwws != null ? JsonSerializer.Serialize(request.Wwws) : null,
        };
        db.WebTunnels.Add(entity);
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "create", "web_tunnel", $"创建 Web 隧道: {entity.SubDomain} → {entity.LocalIp}:{entity.LocalPort}", GetUserName());

        ApiResponse.Data = new
        {
            entity.Id, entity.SubDomain, entity.LocalIp, entity.LocalPort,
            entity.ClientId, entity.IsEnabled,
            createdAt = entity.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };
        ApiResponse.Success = true;
        ApiResponse.Message = "隧道创建成功";
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
        if (request.ClientId.HasValue) entity.ClientId = request.ClientId.Value;
        if (request.Wwws != null) entity.WwwsJson = JsonSerializer.Serialize(request.Wwws);

        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "update", "web_tunnel", $"修改 Web 隧道: id={id}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = "隧道更新成功";
        return ApiResponse;
    }

    [HttpDelete("webs/{id}")]
    public async Task<ApiResponse> DeleteWeb(int id)
    {
        var entity = await db.WebTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();
        entity.IsEnabled = false;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "delete", "web_tunnel", $"删除 Web 隧道: id={id}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = "隧道已删除";
        return ApiResponse;
    }

    [HttpPatch("webs/{id}/toggle")]
    public async Task<ApiResponse> ToggleWeb(int id, [FromBody] ToggleRequest request)
    {
        var entity = await db.WebTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();
        entity.IsEnabled = request.IsEnabled;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "toggle", "web_tunnel", $"{(entity.IsEnabled ? "启用" : "停用")} Web 隧道: id={id}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Data = new { entity.Id, entity.IsEnabled };
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
            x.ClientId,
            clientName = $"Client#{x.ClientId}",
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
            ClientId = request.ClientId ?? 0,
        };
        db.ForwardTunnels.Add(entity);
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "create", "forward_tunnel", $"创建 Forward 隧道: {entity.RemotePort} → {entity.LocalIp}:{entity.LocalPort}", GetUserName());

        ApiResponse.Data = new
        {
            entity.Id, entity.RemotePort, entity.LocalIp, entity.LocalPort,
            entity.Protocol, entity.ClientId, entity.IsEnabled,
            createdAt = entity.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        };
        ApiResponse.Success = true;
        ApiResponse.Message = "隧道创建成功";
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
        if (request.ClientId.HasValue) entity.ClientId = request.ClientId.Value;

        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "update", "forward_tunnel", $"修改 Forward 隧道: id={id}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = "隧道更新成功";
        return ApiResponse;
    }

    [HttpDelete("forwards/{id}")]
    public async Task<ApiResponse> DeleteForward(int id)
    {
        var entity = await db.ForwardTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();
        entity.IsEnabled = false;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "delete", "forward_tunnel", $"删除 Forward 隧道: id={id}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = "隧道已删除";
        return ApiResponse;
    }

    [HttpPatch("forwards/{id}/toggle")]
    public async Task<ApiResponse> ToggleForward(int id, [FromBody] ToggleRequest request)
    {
        var entity = await db.ForwardTunnels.FindAsync(id);
        if (entity == null) return TunnelNotFound();
        entity.IsEnabled = request.IsEnabled;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "toggle", "forward_tunnel", $"{(entity.IsEnabled ? "启用" : "停用")} Forward 隧道: id={id}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Data = new { entity.Id, entity.IsEnabled };
        return ApiResponse;
    }

    private ApiResponse TunnelNotFound()
    {
        ApiResponse.Success = false;
        ApiResponse.Message = "隧道不存在";
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
    public int? ClientId { get; set; }
}

public class ForwardTunnelRequest
{
    public int? RemotePort { get; set; }
    public string? LocalIp { get; set; }
    public int? LocalPort { get; set; }
    public string? Protocol { get; set; }
    public int? ClientId { get; set; }
}

public class ToggleRequest
{
    public bool IsEnabled { get; set; }
}
