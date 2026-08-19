// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

namespace FastTunnel.Core;

/// <summary>
///
/// </summary>
public static class FastTunnelConst
{
    /// <summary>
    ///
    /// </summary>
    public const string FasttunnelVersion = "FT_VERSION";
    /// <summary>
    ///
    /// </summary>
    public const string FasttunnelMsgId = "FT_MSGID";
    /// <summary>
    ///
    /// </summary>
    public const string FasttunnelToken = "FT_TOKEN";
    /// <summary>
    ///     服务端致命错误消息前缀（如 Token 验证失败、版本不兼容），
    ///     客户端收到后应退出进程而非重试。
    /// </summary>
    public const string FatalErrorPrefix = "ERR:";
}
