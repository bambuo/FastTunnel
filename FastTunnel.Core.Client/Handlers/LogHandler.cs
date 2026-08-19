// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Threading;
using System.Threading.Tasks;
using FastTunnel.Core.Client;
using Microsoft.Extensions.Logging;

namespace FastTunnel.Core.Handlers.Client;

public class LogHandler : IClientHandler
{
    private readonly ILogger<LogHandler> _logger;

    public LogHandler(ILogger<LogHandler> logger)
    {
        _logger = logger;
    }

    public Task HandlerMsgAsync(FastTunnelClient cleint, string msg, CancellationToken cancellationToken)
    {
        // 服务端致命错误（Token 验证失败、版本不兼容等）：退出进程而非重试
        if (msg.StartsWith(FastTunnelConst.FatalErrorPrefix))
        {
            cleint.HandleFatalError(msg);
            return Task.CompletedTask;
        }

        _logger.LogInformation(msg);
        return Task.CompletedTask;
    }
}
