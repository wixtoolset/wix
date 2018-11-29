// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility;

    public class NetfxExtensionFactory : BaseExtensionFactory
    {
        protected override IEnumerable<Type> ExtensionTypes => new[]
        {
            typeof(NetfxCompiler),
            typeof(NetfxExtensionData),
            typeof(NetfxWindowsInstallerBackendBinderExtension),
        };
    }
}
