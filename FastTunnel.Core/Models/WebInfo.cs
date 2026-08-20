// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Net.WebSockets;

namespace FastTunnel.Core.Models;

public class WebInfo
{
    public WebSocket Socket { get; set; } = null!;

    public WebConfig WebConfig { get; set; } = null!;

    /// <summary>所属客户端的 Token（流量统计用）</summary>
    public string Token { get; set; } = string.Empty;
}
