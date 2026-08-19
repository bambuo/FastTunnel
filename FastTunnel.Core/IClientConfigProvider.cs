// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Collections.Generic;
using System.Threading.Tasks;
using FastTunnel.Core.Models;

namespace FastTunnel.Core;

/// <summary>
///     按客户端 Token 读取隧道配置（数据库实现位于 FastTunnel.Api）。
///     返回 null 表示该 Token 无任何记录。
/// </summary>
public interface IClientConfigProvider
{
    Task<List<ForwardConfig>?> GetForwardsAsync(string token);

    Task<List<WebConfig>?> GetWebsAsync(string token);
}
