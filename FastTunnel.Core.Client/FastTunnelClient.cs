// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Buffers;
using System.Net.NetworkInformation;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Config;
using FastTunnel.Core.Extensions;
using FastTunnel.Core.Handlers.Client;
using FastTunnel.Core.Models;
using FastTunnel.Core.Models.Massage;
using FastTunnel.Core.Utilitys;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FastTunnel.Core.Client;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(LogInMassage))]
[JsonSerializable(typeof(TunnelConfigMessage))]
public partial class SourceGenerationContext : JsonSerializerContext
{
}

public class FastTunnelClient : IFastTunnelClient
{
    protected readonly ILogger<FastTunnelClient> _logger;
    private readonly LogHandler logHandler;

    private readonly SwapHandler swapHandler;
    private readonly ConfigHandler configHandler;
    private readonly IHostApplicationLifetime _appLifetime;
    private ClientWebSocket socket;
    private volatile bool _fatalError;

    public FastTunnelClient(
        ILogger<FastTunnelClient> logger,
        SwapHandler newCustomerHandler,
        ConfigHandler configHandler,
        LogHandler logHandler,
        IHostApplicationLifetime appLifetime,
        IOptionsMonitor<DefaultClientConfig> configuration)
    {
        var span = new ReadOnlySpan<int>();
        _logger = logger;
        swapHandler = newCustomerHandler;
        this.configHandler = configHandler;
        this.logHandler = logHandler;
        _appLifetime = appLifetime;
        ClientConfig = configuration.CurrentValue;
        Server = ClientConfig.Server;
    }

    protected DefaultClientConfig ClientConfig { get; }

    private static ReadOnlySpan<byte> EndSpan => new(new[] { (byte)'\n' });

    public SuiDaoServer Server { get; protected set; }

    /// <summary>
    ///     启动客户端
    /// </summary>
    public virtual async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("===== FastTunnel Client Start =====");

        while (!cancellationToken.IsCancellationRequested && !_fatalError)
        {
            try
            {
                await loginAsync(cancellationToken);
                await ReceiveServerAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                if (!_fatalError) _logger.LogError(ex.Message);
            }

            if (_fatalError) break;
            await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
        }

        _logger.LogInformation("===== FastTunnel Client End =====");
    }

    /// <summary>
    ///     服务端致命错误（Token 验证失败、版本不兼容等）：记录错误、终止重连循环并停止进程
    /// </summary>
    public void HandleFatalError(string message)
    {
        _fatalError = true;
        _logger.LogError(message);
        try { socket?.Abort(); } catch { }
        _appLifetime.StopApplication();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("===== FastTunnel Client Stoping =====");
        if (socket != null)
        {
            socket.Abort();
        }
    }

    private async Task loginAsync(CancellationToken cancellationToken)
    {
        var logMsg = GetLoginMsg(cancellationToken);
        if (socket != null)
        {
            socket.Abort();
        }

        // 连接到的目标IP
        socket = new ClientWebSocket();
        socket.Options.RemoteCertificateValidationCallback = delegate { return true; };
        socket.Options.SetRequestHeader(FastTunnelConst.FasttunnelVersion, AssemblyUtility.GetVersion().ToString());
        socket.Options.SetRequestHeader(FastTunnelConst.FasttunnelToken, ClientConfig.Token);

        var address = Server.ServerAddr; // 你可以替换为任何你想 ping 的地址
        var ping = new Ping();
        var reply = await ping.SendPingAsync(address);

        if (reply.Status == IPStatus.Success)
        {
            _logger.LogInformation($"Ping to {address} successful.");
            _logger.LogInformation($"Address: {reply.Address}");
            _logger.LogInformation($"Status: {reply.Status}");
        }
        else
        {
            _logger.LogInformation($"Ping to {address} failed. Status: {reply.Status}");
        }

        var url = $"{Server.Protocol}://{Server.ServerAddr}:{Server.ServerPort}";
        _logger.LogInformation($"正在连接服务端 {url}");
        await socket.ConnectAsync(new Uri(url), cancellationToken);

        _logger.LogDebug("连接服务端成功");

        // 登录
        await socket.SendCmdAsync(MessageType.LogIn, logMsg, cancellationToken);
    }


    protected virtual string GetLoginMsg(CancellationToken cancellationToken)
    {
        Server = ClientConfig.Server;

        // 客户端不上报隧道配置，配置由服务端按 Token 下发；仅上报运行环境信息
# if NET8_0_OR_GREATER
        return new LogInMassage { ClientInfo = ClientInfoCollector.Collect() }.ToJson(jsonTypeInfo: SourceGenerationContext.Default.LogInMassage);

#else
        return new LogInMassage { ClientInfo = ClientInfoCollector.Collect() }.ToJson();
#endif
    }

    /// <summary>
    ///     更新服务端下发的隧道配置清单（仅内存持有，不参与上报）
    /// </summary>
    public void UpdateTunnelConfig(IEnumerable<WebConfig> webs, IEnumerable<ForwardConfig> forwards)
    {
        ClientConfig.Webs = webs;
        ClientConfig.Forwards = forwards;
    }


    protected async Task ReceiveServerAsync(CancellationToken cancellationToken)
    {
        var utility = new WebSocketUtility(socket, ProcessLine);
        await utility.ProcessLinesAsync(cancellationToken);
    }

    private void ProcessLine(ReadOnlySequence<byte> line, CancellationToken cancellationToken)
    {
        HandleServerRequestAsync(line, cancellationToken);
    }

    private void HandleServerRequestAsync(ReadOnlySequence<byte> line, CancellationToken cancellationToken)
    {
        try
        {
            var row = line.ToArray();
            var cmd = row[0];
            IClientHandler handler;
            switch ((MessageType)cmd)
            {
                case MessageType.SwapMsg:
                case MessageType.Forward:
                    handler = swapHandler;
                    break;
                case MessageType.Log:
                    handler = logHandler;
                    break;
                case MessageType.ConfigUpdate:
                    handler = configHandler;
                    break;
                default:
                    throw new Exception($"未处理的消息：cmd={cmd}");
            }

#if NETCOREAPP3_1
            var content = Encoding.UTF8.GetString(line.Slice(1).ToArray());
#endif

#if NET5_0_OR_GREATER
            var content = Encoding.UTF8.GetString(line.Slice(1));
#endif
            handler.HandlerMsgAsync(this, content, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex);
        }
    }
}
