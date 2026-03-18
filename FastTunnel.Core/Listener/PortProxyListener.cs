// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Threading;
using FastTunnel.Core.Handlers;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Listener;

public class PortProxyListener
{
    private readonly ILogger _logerr;
    private readonly WebSocket client;
    private readonly Socket listenSocket;
    private ForwardDispatcher _requestDispatcher;

    private int m_numConnectedSockets;

    private bool shutdown;

    public PortProxyListener(string ip, int port, ILogger logerr, WebSocket client)
    {
        this.client = client;
        _logerr = logerr;
        ListenIp = ip;
        ListenPort = port;

        var ipa = IPAddress.Parse(ListenIp);
        var localEndPoint = new IPEndPoint(ipa, ListenPort);

        listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        listenSocket.Bind(localEndPoint);
    }

    public string ListenIp { get; set; }

    public int ListenPort { get; set; }

    public void Start(ForwardDispatcher requestDispatcher)
    {
        shutdown = false;
        _requestDispatcher = requestDispatcher;

        listenSocket.Listen();

        StartAccept(null);
    }

    private void StartAccept(SocketAsyncEventArgs acceptEventArg)
    {
        try
        {
            _logerr.LogDebug($"【{ListenIp}:{ListenPort}】: StartAccept");
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += AcceptEventArg_Completed;
            }
            else
            {
                // socket must be cleared since the context object is being reused
                acceptEventArg.AcceptSocket = null;
            }

            var willRaiseEvent = listenSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAcceptAsync(acceptEventArg);
            }
        }
        catch (Exception ex)
        {
            _logerr.LogError(ex, "待处理异常");
        }
    }

    private async void ProcessAcceptAsync(SocketAsyncEventArgs e)
    {
        if (e.SocketError == SocketError.Success)
        {
            var accept = e.AcceptSocket;

            IncrementClients();

            // 将此客户端交由Dispatcher进行管理
            _requestDispatcher.DispatchAsync(accept, client, this);

            // Accept the next connection request
            StartAccept(e);
        }
        else
        {
            Stop();
        }
    }

    private void AcceptEventArg_Completed(object sender, SocketAsyncEventArgs e)
    {
        ProcessAcceptAsync(e);
    }

    public void Stop()
    {
        if (shutdown)
        {
            return;
        }

        try
        {
            if (listenSocket.Connected)
            {
                listenSocket.Shutdown(SocketShutdown.Both);
            }
        }
        catch (Exception)
        {
            // ignored
        }
        finally
        {
            shutdown = true;
            listenSocket.Close();
        }
    }

    private void IncrementClients()
    {
        Interlocked.Increment(ref m_numConnectedSockets);
        _logerr.LogInformation($"[Listener:{ListenPort}] Accepted. There are {{0}} clients connected", m_numConnectedSockets);
    }

    internal void DecrementClients()
    {
        Interlocked.Decrement(ref m_numConnectedSockets);
        _logerr.LogInformation($"[Listener:{ListenPort}] DisConnet. There are {{0}} clients connecting", m_numConnectedSockets);
    }
}
