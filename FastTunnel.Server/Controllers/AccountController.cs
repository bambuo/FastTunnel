// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FastTunnel.Api.Models;
using FastTunnel.Core.Config;
using FastTunnel.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FastTunnel.Api.Controllers;

public class AccountController : BaseController
{
    private readonly IOptionsMonitor<DefaultServerConfig> _serverOptionsMonitor;

    public AccountController(IOptionsMonitor<DefaultServerConfig> optionsMonitor)
    {
        _serverOptionsMonitor = optionsMonitor;
    }

    [AllowAnonymous]
    [HttpPost]
    public ApiResponse GetToken(GetTokenRequest request)
    {
        if ((_serverOptionsMonitor.CurrentValue?.Api?.Accounts?.Length ?? 0) == 0)
        {
            ApiResponse.code = ErrorCodeEnum.NoAccount;
            ApiResponse.message = "账号或密码错误";
            return ApiResponse;
        }

        var account = _serverOptionsMonitor.CurrentValue!.Api!.Accounts.FirstOrDefault(x =>
            x.Name.Equals(request.account) && x.Password.Equals(request.password));

        if (account == null)
        {
            ApiResponse.code = ErrorCodeEnum.NoAccount;
            ApiResponse.message = "账号或密码错误";
            return ApiResponse;
        }

        var claims = new[] { new Claim("Name", account.Name) };

        ApiResponse.data = "Bearer " + GenerateToken(
            claims,
            _serverOptionsMonitor.CurrentValue.Api!.JWT.IssuerSigningKey,
            _serverOptionsMonitor.CurrentValue.Api!.JWT.Expires,
            _serverOptionsMonitor.CurrentValue.Api!.JWT.ValidIssuer,
            _serverOptionsMonitor.CurrentValue.Api!.JWT.ValidAudience);

        return ApiResponse;
    }

    public static string GenerateToken(
        IEnumerable<Claim> claims, string secret, int expiresMinutes = 60, string? issuer = null, string? audience = null)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var securityToken = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: DateTime.Now.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }
}
