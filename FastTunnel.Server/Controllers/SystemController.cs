// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using FastTunnel.Core.Client;
using FastTunnel.Server.Models;
using Microsoft.AspNetCore.Mvc;

namespace FastTunnel.Api.Controllers;

public class SystemController : BaseController
{
    private readonly FastTunnelServer _fastTunnelServer;

    public SystemController(FastTunnelServer fastTunnelServer)
    {
        _fastTunnelServer = fastTunnelServer;
    }

    [HttpGet]
    public ApiResponse GetResponseTempList()
    {
        ApiResponse.data = new { _fastTunnelServer.ResponseTasks.Count, Rows = _fastTunnelServer.ResponseTasks.Select(x => new { x.Key }) };

        return ApiResponse;
    }

    [HttpGet]
    public ApiResponse GetAllWebList()
    {
        ApiResponse.data = new { _fastTunnelServer.WebList.Count, Rows = _fastTunnelServer.WebList.Select(x => new { x.Key, x.Value.WebConfig.LocalIp, x.Value.WebConfig.LocalPort }) };

        return ApiResponse;
    }

    [HttpGet]
    public ApiResponse GetServerOption()
    {
        ApiResponse.data = _fastTunnelServer.ServerOption;
        return ApiResponse;
    }

    [HttpGet]
    public ApiResponse GetAllForwardList()
    {
        ApiResponse.data = new { _fastTunnelServer.ForwardList.Count, Rows = _fastTunnelServer.ForwardList.Select(x => new { x.Key, x.Value.SSHConfig.LocalIp, x.Value.SSHConfig.LocalPort, x.Value.SSHConfig.RemotePort }) };

        return ApiResponse;
    }

    [HttpGet]
    public ApiResponse GetOnlineClientCount()
    {
        ApiResponse.data = _fastTunnelServer.ConnectedClientCount;
        return ApiResponse;
    }

    [HttpGet]
    public ApiResponse Clients()
    {
        ApiResponse.data = _fastTunnelServer.Clients.Select(x => new { x.WebInfos, x.ForwardInfos, RemoteIpAddress = x.RemoteIpAddress.ToString(), StartTime = x.StartTime.ToString("yyyy-MM-dd HH:mm:ss") });
        return ApiResponse;
    }
}
