// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Collections.Generic;

namespace FastTunnel.Core.Models.Massage;

/// <summary>
///     服务端下发给客户端的隧道配置清单（ConfigUpdate 消息内容）。
/// </summary>
public class TunnelConfigMessage
{
    public IEnumerable<WebConfig> Webs { get; set; } = [];

    public IEnumerable<ForwardConfig> Forwards { get; set; } = [];
}
