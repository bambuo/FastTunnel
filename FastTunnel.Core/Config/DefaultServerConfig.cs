// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Collections.Generic;

namespace FastTunnel.Core.Config;

public class DefaultServerConfig : IServerConfig
{
    [Obsolete("由Tokens替换")]
    public string Token { get; set; } = string.Empty;

    public List<string> Tokens { get; set; } = [];

    public ApiOptions? Api { get; set; }
    public string WebDomain { get; set; } = string.Empty;

    public string[] WebAllowAccessIps { get; set; } = [];

    public bool EnableForward { get; set; }

    public class ApiOptions
    {
        public JWTOptions JWT { get; set; } = null!;

        public Account[] Accounts { get; set; } = [];
    }

    public class JWTOptions
    {
        public int ClockSkew { get; set; }

        public string ValidAudience { get; set; } = string.Empty;

        public string ValidIssuer { get; set; } = string.Empty;

        public string IssuerSigningKey { get; set; } = string.Empty;

        public int Expires { get; set; }
    }

    public class Account
    {
        public string Name { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;
    }
}
