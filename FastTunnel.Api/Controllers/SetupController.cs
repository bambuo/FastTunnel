using FastTunnel.Api.Data;
using FastTunnel.Api.Models.Entities;
using FastTunnel.Api.Services;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FastTunnel.Api.Controllers;

public class SetupController(FastTunnelDbContext db, TotpService totp) : BaseController
{
    [AllowAnonymous]
    [HttpGet("status")]
    public async Task<ApiResponse> Status()
    {
        var count = await db.Accounts.CountAsync();
        ApiResponse.Data = new { initialized = count > 0 };
        ApiResponse.Success = true;
        return ApiResponse;
    }

    [AllowAnonymous]
    [HttpPost("init")]
    public async Task<ApiResponse> Init([FromBody] SetupInitRequest request)
    {
        if (await db.Accounts.AnyAsync())
        {
            ApiResponse.Success = false;
            ApiResponse.Message = "系统已完成初始化";
            return ApiResponse;
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3)
        {
            ApiResponse.Success = false;
            ApiResponse.Message = "用户名至少3位";
            return ApiResponse;
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8 ||
            !request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = "密码强度不足：至少 8 位，包含字母和数字";
            return ApiResponse;
        }

        db.Accounts.Add(new AccountEntity
        {
            Name = request.Name,
            PasswordHash = PasswordService.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "create", "account", $"创建管理员账号: {request.Name}", request.Name);

        ApiResponse.Success = true;
        ApiResponse.Message = "初始化完成";
        return ApiResponse;
    }
}

public class SetupInitRequest
{
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
