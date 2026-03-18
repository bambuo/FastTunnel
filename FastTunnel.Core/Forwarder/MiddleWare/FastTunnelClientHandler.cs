// Copyright (c) 2019-2022 Gui.H. https://github.com/FastTunnel/FastTunnel
// The FastTunnel licenses this file to you under the Apache License Version 2.0.
// For more details,You may obtain License file at: https://github.com/FastTunnel/FastTunnel/blob/v2/LICENSE

using System;
using System.Net.WebSockets;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using FastTunnel.Core.Extensions;
using FastTunnel.Core.Handlers.Server;
using FastTunnel.Core.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Forwarder.MiddleWare;

public class FastTunnelClientHandler(ILogger<FastTunnelClientHandler> logger, FastTunnelServer fastTunnelServer, ILoginHandler loginHandler)
{
    private static int _connectionCount;
    private readonly Version _serverVersion = Assembly.GetExecutingAssembly().GetName().Version;

    public static int ConnectionCount => _connectionCount;

    public async Task Handle(HttpContext context, Func<Task> next)
    {
        try
        {
            if (!context.WebSockets.IsWebSocketRequest || !context.Request.Headers.TryGetValue(FastTunnelConst.FasttunnelVersion, out var version))
            {
                await next();
                return;
            }

            Interlocked.Increment(ref _connectionCount);

            try
            {
                await HandleClient(context, version);
            }
            finally
            {
                Interlocked.Decrement(ref _connectionCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex);
        }
    }

    private async Task HandleClient(HttpContext context, string clientVersion)
    {
        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

        if (Version.Parse(clientVersion).Major != _serverVersion.Major)
        {
            await Close(webSocket, $"客户端版本{clientVersion}与服务端版本{_serverVersion}不兼容，请升级。");
            return;
        }

        if (!CheckToken(context))
        {
            await Close(webSocket, "Token验证失败");
            return;
        }

        var loggerFactory = context.RequestServices.GetRequiredService<ILoggerFactory>();
        var log = loggerFactory.CreateLogger<TunnelClient>();
        var client = new TunnelClient(webSocket, fastTunnelServer, loginHandler, context.Connection.RemoteIpAddress, log) { ConnectionPort = context.Connection.LocalPort };

        try
        {
            fastTunnelServer.ClientLogin(client);
            await client.ReviceAsync(context.RequestAborted);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "客户端异常");
        }
        finally
        {
            fastTunnelServer.ClientLogout(client);
        }
    }

    private static async Task Close(WebSocket webSocket, string reason)
    {
        await webSocket.SendCmdAsync(MessageType.Log, reason, CancellationToken.None);
        await webSocket.CloseAsync(WebSocketCloseStatus.Empty, string.Empty, CancellationToken.None);
    }

    private bool CheckToken(HttpContext context)
    {
        var checkToken = fastTunnelServer.ServerOption.CurrentValue.Tokens != null && fastTunnelServer.ServerOption.CurrentValue.Tokens.Count != 0;

        if (!checkToken)
        {
            return true;
        }

        // 客户端未携带token，登录失败
        if (!context.Request.Headers.TryGetValue(FastTunnelConst.FasttunnelToken, out var token))
        {
            return false;
        }

        return fastTunnelServer.ServerOption.CurrentValue.Tokens?.Contains(token) ?? false;
    }
}
