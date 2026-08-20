// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Collections.Generic;
using FastTunnel.Core.Models;

namespace FastTunnel.Core.Models.Massage;

public class LogInMassage : TunnelMassage
{
    public IEnumerable<WebConfig> Webs { get; set; } = [];

    public IEnumerable<ForwardConfig> Forwards { get; set; } = [];

    /// <summary>客户端运行环境信息（可选，旧客户端不上报）</summary>
    public ClientInfo? ClientInfo { get; set; }
}
