using FastTunnel.Api.Data;
using FastTunnel.Api.Models.Entities;
using FastTunnel.Api.Services;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastTunnel.Api.Controllers;

public class TokensController(FastTunnelDbContext db) : BaseController
{
    [HttpGet]
    public async Task<ApiResponse> Index()
    {
        var tokens = await db.Tokens.OrderByDescending(x => x.Id).ToListAsync();
        ApiResponse.Data = tokens.Select(x => new
        {
            x.Id,
            x.Value,
            x.Description,
            x.IsEnabled,
            clientCount = 0,
            createdAt = x.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        });
        ApiResponse.Success = true;
        return ApiResponse;
    }

    [HttpPost]
    public async Task<ApiResponse> Create([FromBody] TokenCreateRequest request)
    {
        var value = string.IsNullOrWhiteSpace(request.Value) ? Guid.NewGuid().ToString() : request.Value;
        var entity = new TokenEntity
        {
            Value = value,
            Description = request.Description ?? string.Empty,
        };
        db.Tokens.Add(entity);
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "create", "token", $"创建 Token: {entity.Description}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = "Token 创建成功";
        return ApiResponse;
    }

    [HttpPut("{id}")]
    public async Task<ApiResponse> Update(int id, [FromBody] TokenCreateRequest request)
    {
        var entity = await db.Tokens.FindAsync(id);
        if (entity == null) return NotFound();

        if (request.Description != null) entity.Description = request.Description;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "update", "token", $"修改 Token: id={id}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = "Token 更新成功";
        return ApiResponse;
    }

    [HttpDelete("{id}")]
    public async Task<ApiResponse> Delete(int id)
    {
        var entity = await db.Tokens.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsEnabled = false;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "delete", "token", $"删除 Token: id={id}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = "Token 已删除";
        return ApiResponse;
    }

    [HttpPatch("{id}/toggle")]
    public async Task<ApiResponse> Toggle(int id, [FromBody] ToggleRequest request)
    {
        var entity = await db.Tokens.FindAsync(id);
        if (entity == null) return NotFound();
        entity.IsEnabled = request.IsEnabled;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "toggle", "token", $"{(entity.IsEnabled ? "启用" : "停用")} Token: id={id}", GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Data = new { entity.Id, entity.IsEnabled };
        return ApiResponse;
    }

    private ApiResponse NotFound()
    {
        ApiResponse.Success = false;
        ApiResponse.Message = "Token 不存在";
        return ApiResponse;
    }

    private string GetUserName() => User.FindFirst("Name")?.Value ?? "unknown";
}

public class TokenCreateRequest
{
    public string? Value { get; set; }
    public string? Description { get; set; }
}
