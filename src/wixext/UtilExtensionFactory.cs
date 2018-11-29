// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility;

    public class UtilExtensionFactory : BaseExtensionFactory
    {
        protected override IEnumerable<Type> ExtensionTypes => new[]
        {
            typeof(UtilCompiler),
            typeof(UtilExtensionData),
            typeof(UtilWindowsInstallerBackendBinderExtension),
        };
    }
}
