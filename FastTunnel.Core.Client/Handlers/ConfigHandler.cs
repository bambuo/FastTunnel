// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using FastTunnel.Core.Models.Massage;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Handlers.Client;

/// <summary>
///     处理服务端下发的隧道配置清单（ConfigUpdate）。
///     客户端仅持有清单（内存），转发时按服务端指令中的内网地址连接本地服务。
/// </summary>
public class ConfigHandler : IClientHandler
{
    private readonly ILogger<ConfigHandler> _logger;

    public ConfigHandler(ILogger<ConfigHandler> logger)
    {
        _logger = logger;
    }

    public async Task HandlerMsgAsync(FastTunnelClient cleint, string msg, CancellationToken cancellationToken)
    {
        var config = JsonSerializer.Deserialize(msg, SourceGenerationContext.Default.TunnelConfigMessage);
        if (config == null)
        {
            _logger.LogWarning("收到空的隧道配置清单");
            return;
        }

        cleint.UpdateTunnelConfig(config.Webs ?? [], config.Forwards ?? []);
        _logger.LogInformation($"已更新隧道配置清单：Web隧道 {config.Webs?.Count() ?? 0} 个，端口转发 {config.Forwards?.Count() ?? 0} 个");
    }
}
