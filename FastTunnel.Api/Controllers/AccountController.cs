using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FastTunnel.Api.Data;
using FastTunnel.Api.Services;
using FastTunnel.Core.Config;
using FastTunnel.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FastTunnel.Api.Controllers;

public class AccountController(
    IOptionsMonitor<DefaultServerConfig> serverOptions,
    FastTunnelDbContext db,
    TotpService totp) : BaseController
{
    private DefaultServerConfig ServerConfig => serverOptions.CurrentValue;

    [AllowAnonymous]
    [HttpPost("token")]
    public async Task<ApiResponse> GetToken([FromBody] LoginRequest request)
    {
        var account = await db.Accounts.FirstOrDefaultAsync(x => x.Name == request.Name);
        if (account == null || !PasswordService.Verify(request.Password, account.PasswordHash))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = "用户名或密码错误";
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
            ApiResponse.Message = "MFA 已绑定";
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
            ApiResponse.Message = "请先调用 mfa/setup";
            return ApiResponse;
        }

        if (!totp.ValidateCode(account.MfaSecret, request.Code))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = "验证码错误";
            return ApiResponse;
        }

        account.MfaEnabled = true;
        await db.SaveChangesAsync();
        await AuditService.LogAsync(db, "bind", "mfa", $"绑定 MFA: {account.Name}", account.Name);

        ApiResponse.Success = true;
        ApiResponse.Data = new
        {
            token = GenerateFullToken(account.Name),
            username = account.Name,
        };
        ApiResponse.Message = "MFA 绑定成功";
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
            ApiResponse.Message = "请先绑定 MFA";
            return ApiResponse;
        }

        if (!totp.ValidateCode(account.MfaSecret, request.Code))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = "验证码错误";
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
        ApiResponse.Message = "账号不存在";
        return ApiResponse;
    }

    private ApiResponse Unauthorized()
    {
        ApiResponse.Success = false;
        ApiResponse.Message = "预认证令牌无效";
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
