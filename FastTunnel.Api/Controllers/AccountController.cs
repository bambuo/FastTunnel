using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FastTunnel.Api.Data;
using FastTunnel.Api.Resources;
using FastTunnel.Api.Services;
using FastTunnel.Core.Config;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FastTunnel.Api.Controllers;

public class AccountController(
    IOptionsMonitor<DefaultServerConfig> serverOptions,
    FastTunnelDbContext db,
    TotpService totp,
    IStringLocalizer<ApiMessages> localizer) : BaseController
{
    private readonly IStringLocalizer<ApiMessages> _localizer = localizer;
    private DefaultServerConfig ServerConfig => serverOptions.CurrentValue;

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ApiResponse> GetToken([FromBody] LoginRequest request)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Name == request.Name);
        if (account == null || !PasswordService.Verify(request.Password, account.PasswordHash))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Account.LoginFailed"];
            return ApiResponse;
        }

        var preAuthToken = GeneratePreAuthToken(account.Name, account.MfaEnabled);
        ApiResponse.Success = true;
        ApiResponse.Data = new
        {
            requiresMfa = true,
            mfaBound = account.MfaEnabled,
            preAuthToken,
            username = account.Name,
        };
        return ApiResponse;
    }

    [HttpPost("mfa/setup")]
    public async Task<ApiResponse> MfaSetup()
    {
        var username = GetMfaUsername();
        if (username == null) return Unauthorized();

        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Name == username);
        if (account == null) return NotFound();

        if (account.MfaEnabled)
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Account.MfaAlreadyBound"];
            return ApiResponse;
        }

        var secret = totp.GenerateSecret();
        account.MfaSecret = secret;
        await db.SaveChangesAsync();

        ApiResponse.Success = true;
        ApiResponse.Data = new
        {
            secret,
            qrCodeUrl = totp.GenerateQrCodeUrl(secret, account.Name),
        };
        return ApiResponse;
    }

    [HttpPost("mfa/bind")]
    public async Task<ApiResponse> MfaBind([FromBody] MfaCodeRequest request)
    {
        var username = GetMfaUsername();
        if (username == null) return Unauthorized();

        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Name == username);
        if (account == null) return NotFound();

        if (account.MfaEnabled || string.IsNullOrEmpty(account.MfaSecret))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Account.MfaSetupRequired"];
            return ApiResponse;
        }

        if (!totp.ValidateCode(account.MfaSecret, request.Code))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Account.CodeInvalid"];
            return ApiResponse;
        }

        account.MfaEnabled = true;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "bind", "mfa", string.Format(_localizer["Audit.BindMfa"], account.Name), account.Name);

        ApiResponse.Success = true;
        ApiResponse.Data = new
        {
            token = GenerateFullToken(account.Name),
            username = account.Name,
        };
        ApiResponse.Message = _localizer["Account.MfaBindSuccess"];
        return ApiResponse;
    }

    [HttpPost("mfa/verify")]
    public async Task<ApiResponse> MfaVerify([FromBody] MfaCodeRequest request)
    {
        var username = GetMfaUsername();
        if (username == null) return Unauthorized();

        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Name == username);
        if (account == null) return NotFound();

        if (!account.MfaEnabled || string.IsNullOrEmpty(account.MfaSecret))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Account.MfaBindRequired"];
            return ApiResponse;
        }

        if (!totp.ValidateCode(account.MfaSecret, request.Code))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Account.CodeInvalid"];
            return ApiResponse;
        }

        ApiResponse.Success = true;
        ApiResponse.Data = new
        {
            token = GenerateFullToken(account.Name),
            username = account.Name,
        };
        return ApiResponse;
    }

    private string? GetMfaUsername()
    {
        var claim = User.FindFirst("mfa_username");
        return claim?.Value;
    }

    private string GeneratePreAuthToken(string username, bool mfaBound)
    {
        var jwtOpts = ServerConfig.Api?.JWT;
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtOpts!.IssuerSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("mfa_username", username),
            new Claim("mfa_bound", mfaBound.ToString().ToLower()),
            new Claim("scope", "mfa"),
        };

        var token = new JwtSecurityToken(
            issuer: jwtOpts.ValidIssuer,
            audience: jwtOpts.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateFullToken(string username)
    {
        var jwtOpts = ServerConfig.Api?.JWT;
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtOpts!.IssuerSigningKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[] { new Claim("Name", username) };

        var token = new JwtSecurityToken(
            issuer: jwtOpts.ValidIssuer,
            audience: jwtOpts.ValidAudience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(jwtOpts.Expires > 0 ? jwtOpts.Expires : 120),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ApiResponse NotFound()
    {
        ApiResponse.Success = false;
        ApiResponse.Message = _localizer["Account.AccountNotFound"];
        return ApiResponse;
    }

    private ApiResponse Unauthorized()
    {
        ApiResponse.Success = false;
        ApiResponse.Message = _localizer["Account.InvalidPreAuth"];
        HttpContext.Response.StatusCode = 401;
        return ApiResponse;
    }
}

public class LoginRequest
{
    public string Name { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class MfaCodeRequest
{
    public string Code { get; set; } = string.Empty;
}
