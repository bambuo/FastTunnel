// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System.Threading.Tasks;

namespace FastTunnel.Core;

/// <summary>
///     客户端 Token 校验（数据库实现位于 FastTunnel.Api，Token 在管理台创建）。
/// </summary>
public interface ITokenValidator
{
    Task<bool> IsValidAsync(string token);
}
