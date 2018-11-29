// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility;

    public class FirewallExtensionFactory : BaseExtensionFactory
    {
        protected override IEnumerable<Type> ExtensionTypes => new[]
        {
            typeof(FirewallCompiler),
            typeof(FirewallExtensionData),
            typeof(FirewallWindowsInstallerBackendBinderExtension),
        };
    }
}
