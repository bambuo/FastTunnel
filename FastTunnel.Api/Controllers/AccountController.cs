// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using FastTunnel.Api.Models;
using FastTunnel.Core.Config;
using FastTunnel.Server.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace FastTunnel.Api.Controllers;

public class AccountController : BaseController
{
    readonly IOptionsMonitor<DefaultServerConfig> _serverOptionsMonitor;

    public AccountController(IOptionsMonitor<DefaultServerConfig> optionsMonitor)
    {
        _serverOptionsMonitor = optionsMonitor;
    }

    [AllowAnonymous]
    [HttpPost]
    public ApiResponse GetToken(GetTokenRequest request)
    {
        var currentValue = _serverOptionsMonitor.CurrentValue;
        if (currentValue?.Api?.Accounts == null)
        {
            ApiResponse.Success = false;
            ApiResponse.Message = "认证失败";
            return ApiResponse;
        }

        var account = currentValue.Api.Accounts.FirstOrDefault(x =>
            x.Name.Equals(request.name) && x.Password.Equals(request.password));

        if (account == null)
        {
            ApiResponse.Success = false;
            ApiResponse.Message = "认证失败";
            return ApiResponse;
        }

        var claims = new[] { new Claim("Name", account.Name) };

        ApiResponse.Data = GenerateToken(
            claims,
            currentValue.Api.JWT.IssuerSigningKey,
            currentValue.Api.JWT.Expires,
            currentValue.Api.JWT.ValidIssuer,
            currentValue.Api.JWT.ValidAudience);

        return ApiResponse;
    }

    public static string GenerateToken(
        IEnumerable<Claim> claims, string secret, int expiresMinutes = 60, string? issuer = null, string? audience = null)
    {
        var key = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var securityToken = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.Now.AddMinutes(expiresMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(securityToken);
    }
}
