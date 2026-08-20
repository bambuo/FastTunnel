// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using FastTunnel.Core.Extensions;
using FastTunnel.Core.Models;
using FastTunnel.Core.Models.Massage;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Handlers.Server;

[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(LogInMassage))]
[JsonSerializable(typeof(TunnelConfigMessage))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}

public class LoginHandler(ILogger<LoginHandler> logger, IClientConfigProvider configProvider) : ILoginHandler
{
    public const bool NeedRecive = true;
    private readonly ILogger _logger = logger;


    public virtual async Task<bool> HandlerMsg(FastTunnelServer fastTunnelServer, TunnelClient tunnelClient, string lineCmd, CancellationToken cancellationToken)
    {
        var msg = JsonSerializer.Deserialize(lineCmd, SourceGenerationContext.Default.LogInMassage);
        if (msg?.ClientInfo != null)
        {
            tunnelClient.ClientInfo = msg.ClientInfo;
        }

        await HandleLoginAsync(fastTunnelServer, tunnelClient, msg, cancellationToken);
        return NeedRecive;
    }

    protected async Task HandleLoginAsync(FastTunnelServer server, TunnelClient client, LogInMassage requet, CancellationToken cancellationToken)
    {
        var tips = new List<string>();

        await client.webSocket.SendCmdAsync(MessageType.Log, "穿透协议 | 映射关系（公网=>内网）", cancellationToken);
        Thread.Sleep(300);

        // 客户端零上报，隧道配置一律以服务端数据库（按 Token）为准
        var webs = await configProvider.GetWebsAsync(client.Token) ?? [];
        var forwards = await configProvider.GetForwardsAsync(client.Token) ?? [];

        server.ApplyWebConfig(client, webs);

        foreach (var item in webs)
        {
            var hostName = $"{item.SubDomain}.{server.ServerOption.CurrentValue.WebDomain}".Trim().ToLower();
            await client.webSocket.SendCmdAsync(MessageType.Log, $"  HTTP   | http://{hostName}:{client.ConnectionPort} => {item.LocalIp}:{item.LocalPort}", CancellationToken.None);

            if (item.WWW != null)
            {
                foreach (var www in item.WWW)
                {
                    await client.webSocket.SendCmdAsync(MessageType.Log, $"  HTTP   | http://{www.Trim().ToLower()}:{client.ConnectionPort} => {item.LocalIp}:{item.LocalPort}", CancellationToken.None);
                }
            }
        }

        if (server.ServerOption.CurrentValue.EnableForward)
        {
            foreach (var item in forwards)
            {
                if (item.LocalPort == 3389)
                {
                    tips.Add("您已将3389端口暴露，请确保您的PC密码足够安全。");
                }

                if (item.LocalPort == 22)
                {
                    tips.Add("您已将22端口暴露，请确保您的PC密码足够安全。");
                }
            }

            server.ApplyForwardConfig(client, forwards);

            foreach (var item in forwards)
            {
                await client.webSocket.SendCmdAsync(MessageType.Log, $"  {item.Protocol}    | {server.ServerOption.CurrentValue.WebDomain}:{item.RemotePort} => {item.LocalIp}:{item.LocalPort}", CancellationToken.None);
            }
        }
        else
        {
            await client.webSocket.SendCmdAsync(MessageType.Log, TunnelResource.ForwardDisabled, CancellationToken.None);
        }

        foreach (var item in tips)
        {
            await client.webSocket.SendCmdAsync(MessageType.Log, item, CancellationToken.None);
        }

        // 下发隧道配置清单
        var configMsg = new TunnelConfigMessage { Webs = webs, Forwards = forwards };
        await client.webSocket.SendCmdAsync(MessageType.ConfigUpdate, JsonSerializer.Serialize(configMsg, SourceGenerationContext.Default.TunnelConfigMessage), cancellationToken);

        if (!webs.Any() && !forwards.Any())
        {
            await client.webSocket.SendCmdAsync(MessageType.Log, TunnelResource.NoTunnel, CancellationToken.None);
        }
    }
}
