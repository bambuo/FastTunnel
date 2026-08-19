// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

namespace FastTunnel.Core.Models;

public class ForwardConfig
{
    public string LocalIp { get; set; } = string.Empty;

    public int LocalPort { get; set; } = 22;

    public int RemotePort { get; set; }

    public ProtocolEnum Protocol { get; set; }
}

public enum ProtocolEnum
{
    TCP = 0,
    UDP = 1
}
