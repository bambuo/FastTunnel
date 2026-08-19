// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Linq;
using FastTunnel.Api.Models;
using FastTunnel.Core.Client;
using Microsoft.AspNetCore.Mvc;

namespace FastTunnel.Api.Controllers;

public class SystemController : BaseController
{
    private readonly FastTunnelServer _fastTunnelServer;

    public SystemController(FastTunnelServer fastTunnelServer)
    {
        _fastTunnelServer = fastTunnelServer;
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
}
