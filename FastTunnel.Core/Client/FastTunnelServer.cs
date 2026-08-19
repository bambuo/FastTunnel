// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Config;
using FastTunnel.Core.Forwarder;
using FastTunnel.Core.Forwarder.MiddleWare;
using FastTunnel.Core.Handlers;
using FastTunnel.Core.Listener;
using FastTunnel.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Yarp.ReverseProxy.Configuration;

namespace FastTunnel.Core.Client;

public class FastTunnelServer(ILogger<FastTunnelServer> logger, IProxyConfigProvider proxyConfig, IOptionsMonitor<DefaultServerConfig> serverSettings)
{
    public readonly IOptionsMonitor<DefaultServerConfig> ServerOption = serverSettings;

    public IList<TunnelClient> Clients = new List<TunnelClient>();

    public int ConnectedClientCount;

    public ConcurrentDictionary<string, (TaskCompletionSource<Stream>, CancellationToken)> ResponseTasks { get; } = new();

    public ConcurrentDictionary<string, WebInfo> WebList { get; private set; } = new();

    public ConcurrentDictionary<int, ForwardInfo<ForwardHandlerArg>> ForwardList { get; private set; }
        = new();

    /// <summary>
    ///     客户端登录
    /// </summary>
    /// <param name="client"></param>
    internal void ClientLogin(TunnelClient client)
    {
        Interlocked.Increment(ref ConnectedClientCount);
        logger.LogInformation($"客户端连接 {client.RemoteIpAddress} 当前在线数：{ConnectedClientCount}，统计CLIENT连接数：{FastTunnelClientHandler.ConnectionCount}");
        Clients.Add(client);
    }

    /// <summary>
    ///     客户端退出
    /// </summary>
    /// <param name="client"></param>
    /// <exception cref="NotImplementedException"></exception>
    internal void ClientLogout(TunnelClient client)
    {
        Interlocked.Decrement(ref ConnectedClientCount);
        logger.LogInformation($"客户端关闭  {client.RemoteIpAddress} 当前在线数：{ConnectedClientCount}，统计CLIENT连接数：{FastTunnelClientHandler.ConnectionCount - 1}");
        Clients.Remove(client);
        client.Logout();
    }

    /// <summary>
    ///     移除客户端的 Web 路由（WebList 与 YARP 配置同步）
    /// </summary>
    internal void RemoveWebRoute(string hostName)
    {
        WebList.TryRemove(hostName, out _);
        (proxyConfig as FastTunnelInMemoryConfigProvider)?.RemoveWeb(hostName);
    }

    /// <summary>
    ///     按配置重建客户端的端口转发监听（先清旧监听，再按新配置创建）
    /// </summary>
    public void ApplyForwardConfig(TunnelClient client, IEnumerable<ForwardConfig> forwards)
    {
        foreach (var item in client.ForwardInfos.ToList())
        {
            try
            {
                ForwardList.TryRemove(item.SSHConfig.RemotePort, out _);
                item.Listener.Stop();
            }
            catch
            {
            }
        }
        client.ForwardInfos.Clear();

        foreach (var item in forwards)
        {
            try
            {
                if (ForwardList.TryGetValue(item.RemotePort, out var old))
                {
                    logger.LogDebug($"Remove Listener {old.Listener.ListenIp}:{old.Listener.ListenPort}");
                    old.Listener.Stop();
                    ForwardList.TryRemove(item.RemotePort, out _);
                }

                IPortListener ls = item.Protocol == ProtocolEnum.UDP
                    ? new UdpProxyListener("0.0.0.0", item.RemotePort, logger, client.webSocket)
                    : new PortProxyListener("0.0.0.0", item.RemotePort, logger, client.webSocket);
                ls.Start(new ForwardDispatcher(logger, this, item));

                var forwardInfo = new ForwardInfo<ForwardHandlerArg> { Listener = ls, Socket = client.webSocket, SSHConfig = item };

                ForwardList.TryAdd(item.RemotePort, forwardInfo);
                logger.LogDebug($"SSH proxy success: {item.RemotePort} => {item.LocalIp}:{item.LocalPort}");

                client.AddForward(forwardInfo);
            }
            catch (Exception ex)
            {
                logger.LogError($"SSH proxy error: {item.RemotePort} => {item.LocalIp}:{item.LocalPort}");
                logger.LogError(ex.Message);
            }
        }
    }

    /// <summary>
    ///     按配置重建客户端的 Web 路由（先清旧路由，再按新配置注册，含 WWW 附加域名）
    /// </summary>
    public void ApplyWebConfig(TunnelClient client, IEnumerable<WebConfig> webs)
    {
        foreach (var info in client.WebInfos.ToList())
        {
            try
            {
                RemoveWebRoute($"{info.WebConfig.SubDomain}.{ServerOption.CurrentValue.WebDomain}".Trim().ToLower());

                if (info.WebConfig.WWW != null)
                {
                    foreach (var www in info.WebConfig.WWW)
                    {
                        RemoveWebRoute(www.Trim().ToLower());
                    }
                }
            }
            catch
            {
            }
        }
        client.WebInfos.Clear();

        foreach (var item in webs)
        {
            var info = new WebInfo { Socket = client.webSocket, WebConfig = item };
            var hostName = $"{item.SubDomain}.{ServerOption.CurrentValue.WebDomain}".Trim().ToLower();

            logger.LogDebug($"new domain '{hostName}'");
            WebList.AddOrUpdate(hostName, info, (key, oldInfo) => { return info; });
            (proxyConfig as FastTunnelInMemoryConfigProvider)?.AddWeb(hostName);
            client.AddWeb(info);

            if (item.WWW != null)
            {
                foreach (var www in item.WWW)
                {
                    // TODO:validateDomain
                    var wwwHost = www.Trim().ToLower();
                    WebList.AddOrUpdate(wwwHost, info, (key, oldInfo) => { return info; });
                    (proxyConfig as FastTunnelInMemoryConfigProvider)?.AddWeb(wwwHost);
                    client.AddWeb(info);
                }
            }
        }
    }
}
