// Licensed under the Apache License, Version 2.0 (the "License").
// You may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//     https://github.com/FastTunnel/FastTunnel/edit/v2/LICENSE
// Copyright (c) 2019 Gui.H

using System;
using System.Reflection;

namespace FastTunnel.Core.Utilitys;

public static class AssemblyUtility
{
    public static Version GetVersion()
    {
        return Assembly.GetExecutingAssembly().GetName().Version;
    }
}
