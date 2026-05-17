using FastTunnel.Api.Data;
using FastTunnel.Api.Models.Entities;
using FastTunnel.Api.Resources;
using FastTunnel.Api.Services;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FastTunnel.Api.Controllers;

public class SetupController(FastTunnelDbContext db, TotpService totp, IStringLocalizer<ApiMessages> localizer) : BaseController
{
    private readonly IStringLocalizer<ApiMessages> _localizer = localizer;
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
            ApiResponse.Message = _localizer["Setup.AlreadyInitialized"];
            return ApiResponse;
        }

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length < 3)
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Setup.NameTooShort"];
            return ApiResponse;
        }

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8 ||
            !request.Password.Any(char.IsLetter) || !request.Password.Any(char.IsDigit))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Setup.WeakPassword"];
            return ApiResponse;
        }

        db.Accounts.Add(new AccountEntity
        {
            Name = request.Name,
            PasswordHash = PasswordService.Hash(request.Password),
            CreatedAt = DateTime.UtcNow,
        });

        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "create", "account", string.Format(_localizer["Audit.CreateAccount"], request.Name), request.Name);

        ApiResponse.Success = true;
        ApiResponse.Message = _localizer["Setup.Completed"];
        return ApiResponse;
    }
}

public class SetupInitRequest
{
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}
