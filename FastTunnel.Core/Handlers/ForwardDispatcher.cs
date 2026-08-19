// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.IO;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using FastTunnel.Core.Exceptions;
using FastTunnel.Core.Extensions;
using FastTunnel.Core.Listener;
using FastTunnel.Core.Models;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Handlers;

public class ForwardDispatcher(ILogger logger, FastTunnelServer server, ForwardConfig config)
{
    /// <summary>
    /// </summary>
    /// <param name="socket">用户请求</param>
    /// <param name="client">FastTunnel客户端</param>
    /// <returns></returns>
    public async Task DispatchAsync(Socket socket, WebSocket client, PortProxyListener listener)
    {
        var msgId = Guid.NewGuid().ToString().Replace("-", "");

        try
        {
            await Task.Yield();
            logger.LogDebug($"[Forward]Swap开始 {msgId}|{config.RemotePort}=>{config.LocalIp}:{config.LocalPort}");

            var tcs = new TaskCompletionSource<Stream>();

            server.ResponseTasks.TryAdd(msgId, (tcs, CancellationToken.None));

            try
            {
                await client.SendCmdAsync(MessageType.Forward, $"{msgId}|{config.LocalIp}:{config.LocalPort}", CancellationToken.None);
            }
            catch (SocketClosedException sex)
            {
                // TODO:客户端已掉线，但是没有移除对端口的监听
                logger.LogError($"[Forward]Swap 客户端已离线 {sex.Message}");
                tcs.TrySetCanceled();
                Close(socket);
                return;
            }
            catch (Exception ex)
            {
                // 网络不稳定
                logger.LogError(ex, "[Forward]Swap Exception");
                tcs.TrySetCanceled();
                Close(socket);
                return;
            }

            await using var stream1 = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            await using var stream2 = new NetworkStream(socket, true);
            stream2.ReadTimeout = 1000 * 60 * 10;
            await Task.WhenAny(stream1.CopyToAsync(stream2), stream2.CopyToAsync(stream1));
        }
        catch (Exception ex)
        {
            logger.LogDebug($"[Forward]Swap Error {msgId}：" + ex.Message);
        }
        finally
        {
            logger.LogDebug($"[Forward]Swap结束 {msgId}");
            server.ResponseTasks.TryRemove(msgId, out _);
            listener.DecrementClients();
        }
    }

    private void Close(Socket socket)
    {
        try
        {
            socket.Shutdown(SocketShutdown.Both);
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            socket.Close();
        }
    }

    /// <summary>
    /// Open a tunnel stream towards the FastTunnel client for UDP forwarding.
    /// The protocol marker "udp" is included so the client knows it must
    /// connect to the local target via UDP and frame datagrams.
    /// </summary>
    public async Task<Stream> RequestUdpTunnelAsync(WebSocket client, CancellationToken cancellationToken)
    {
        var msgId = Guid.NewGuid().ToString().Replace("-", "");
        var tcs = new TaskCompletionSource<Stream>();
        server.ResponseTasks.TryAdd(msgId, (tcs, cancellationToken));

        try
        {
            logger.LogDebug($"[Forward-UDP]Swap开始 {msgId}|{config.RemotePort}=>{config.LocalIp}:{config.LocalPort}");
            await client.SendCmdAsync(MessageType.Forward, $"{msgId}|udp|{config.LocalIp}:{config.LocalPort}", cancellationToken);
            return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"[Forward-UDP]Swap failed {msgId}");
            server.ResponseTasks.TryRemove(msgId, out _);
            tcs.TrySetCanceled();
            return null;
        }
    }
}
