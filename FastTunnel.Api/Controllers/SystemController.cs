// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Linq;
using System.Text.Json;
using FastTunnel.Api.Data;
using FastTunnel.Api.Models;
using FastTunnel.Api.Models.Entities;
using FastTunnel.Api.Resources;
using FastTunnel.Api.Services;
using FastTunnel.Core.Client;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;

namespace FastTunnel.Api.Controllers;

public class SystemController : BaseController
{
    private readonly FastTunnelServer _fastTunnelServer;
    private readonly FastTunnelDbContext _db;
    private readonly SystemConfigSource _configSource;
    private readonly IStringLocalizer<ApiMessages> _localizer;

    public SystemController(FastTunnelServer fastTunnelServer, FastTunnelDbContext db, SystemConfigSource configSource, IStringLocalizer<ApiMessages> localizer)
    {
        _fastTunnelServer = fastTunnelServer;
        _db = db;
        _configSource = configSource;
        _localizer = localizer;
    }

    [HttpGet("getresponsetemplist")]
    public ApiResponse GetResponseTempList()
    {
        ApiResponse.Data = new
        {
            Count = _fastTunnelServer.ResponseTasks.Count,
            Rows = _fastTunnelServer.ResponseTasks.Select(x => new { x.Key }),
        };

        ApiResponse.Success = true;
        return ApiResponse;
    }

    [HttpGet("getallweblist")]
    public ApiResponse GetAllWebList()
    {
        ApiResponse.Data = new
        {
            Count = _fastTunnelServer.WebList.Count,
            Rows = _fastTunnelServer.WebList.Select(x => new { x.Key, x.Value.WebConfig.LocalIp, x.Value.WebConfig.LocalPort }),
        };

        ApiResponse.Success = true;
        return ApiResponse;
    }

    [HttpGet("getserveroption")]
    public ApiResponse GetServerOption()
    {
        ApiResponse.Data = _fastTunnelServer.ServerOption;
        ApiResponse.Success = true;
        return ApiResponse;
    }

    [HttpGet("getallforwardlist")]
    public ApiResponse GetAllForwardList()
    {
        ApiResponse.Data = new
        {
            Count = _fastTunnelServer.ForwardList.Count,
            Rows = _fastTunnelServer.ForwardList.Select(x => new { x.Key, x.Value.SSHConfig.LocalIp, x.Value.SSHConfig.LocalPort, x.Value.SSHConfig.RemotePort }),
        };

        ApiResponse.Success = true;
        return ApiResponse;
    }

    [HttpGet("getonlineclientcount")]
    public ApiResponse GetOnlineClientCount()
    {
        ApiResponse.Data = _fastTunnelServer.ConnectedClientCount;
        ApiResponse.Success = true;
        return ApiResponse;
    }

    [HttpGet("clients")]
    public ApiResponse Clients()
    {
        ApiResponse.Data = _fastTunnelServer.Clients.Select(x => new
        {
            x.WebInfos,
            x.ForwardInfos,
            RemoteIpAddress = x.RemoteIpAddress.ToString(),
            StartTime = x.StartTime.ToString("yyyy-MM-dd HH:mm:ss"),
        });
        ApiResponse.Success = true;
        return ApiResponse;
    }

    /// <summary>
    ///     读取 FastTunnel 配置（数据库为准，缺失项返回默认值）
    /// </summary>
    [HttpGet("config")]
    public async Task<ApiResponse> GetConfig()
    {
        var rows = await _db.SystemConfigs.ToDictionaryAsync(x => x.Key, x => x.Value);

        string Get(string key, string def) => rows.TryGetValue(key, out var v) ? v : def;

        ApiResponse.Data = new
        {
            enableForward = Get("EnableForward", "true") == "true",
            webDomain = Get("WebDomain", string.Empty),
            webAllowAccessIps = DeserializeIps(Get("WebAllowAccessIps", "[]")),
            jwt = new
            {
                clockSkew = int.TryParse(Get("Api:JWT:ClockSkew", "10"), out var cs) ? cs : 10,
                validAudience = Get("Api:JWT:ValidAudience", string.Empty),
                validIssuer = Get("Api:JWT:ValidIssuer", string.Empty),
                issuerSigningKey = Get("Api:JWT:IssuerSigningKey", string.Empty),
                expires = int.TryParse(Get("Api:JWT:Expires", "120"), out var exp) ? exp : 120,
            },
        };
        ApiResponse.Success = true;
        return ApiResponse;
    }

    /// <summary>
    ///     保存 FastTunnel 配置：写库并触发配置热重载。
    ///     EnableForward/WebDomain 立即生效；JWT 验证参数为启动快照，需重启服务端。
    /// </summary>
    [HttpPut("config")]
    public async Task<ApiResponse> PutConfig([FromBody] SystemConfigRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.IssuerSigningKey))
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Config.SigningKeyRequired"];
            return ApiResponse;
        }

        if (request.Expires <= 0 || request.ClockSkew < 0)
        {
            ApiResponse.Success = false;
            ApiResponse.Message = _localizer["Config.JwtInvalid"];
            return ApiResponse;
        }

        var entries = new Dictionary<string, string>
        {
            ["EnableForward"] = request.EnableForward ? "true" : "false",
            ["WebDomain"] = request.WebDomain ?? string.Empty,
            ["WebAllowAccessIps"] = JsonSerializer.Serialize(request.WebAllowAccessIps ?? []),
            ["Api:JWT:ClockSkew"] = request.ClockSkew.ToString(),
            ["Api:JWT:ValidAudience"] = request.ValidAudience ?? string.Empty,
            ["Api:JWT:ValidIssuer"] = request.ValidIssuer ?? string.Empty,
            ["Api:JWT:IssuerSigningKey"] = request.IssuerSigningKey,
            ["Api:JWT:Expires"] = request.Expires.ToString(),
        };

        var now = DateTime.UtcNow;
        foreach (var (key, value) in entries)
        {
            var entity = await _db.SystemConfigs.FirstOrDefaultAsync(x => x.Key == key);
            if (entity == null)
            {
                _db.SystemConfigs.Add(new SystemConfigEntity { Key = key, Value = value, UpdatedAt = now });
            }
            else
            {
                entity.Value = value;
                entity.UpdatedAt = now;
            }
        }
        await _db.SaveChangesAsync();

        _configSource.Provider.ReloadConfig();

        await AuditService.LogAsync(_db, "update", "server_config", string.Format(_localizer["Audit.UpdateConfig"], string.Join(", ", entries.Keys)), GetUserName());

        ApiResponse.Success = true;
        ApiResponse.Message = _localizer["Config.Saved"];
        return ApiResponse;
    }

    private string GetUserName() => User.FindFirst("Name")?.Value ?? "unknown";

    private static string[] DeserializeIps(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try { return JsonSerializer.Deserialize<string[]>(json) ?? []; }
        catch { return []; }
    }
}

public class SystemConfigRequest
{
    public bool EnableForward { get; set; }
    public string? WebDomain { get; set; }
    public string[]? WebAllowAccessIps { get; set; }
    public int ClockSkew { get; set; }
    public string? ValidAudience { get; set; }
    public string? ValidIssuer { get; set; }
    public string? IssuerSigningKey { get; set; }
    public int Expires { get; set; }
}
