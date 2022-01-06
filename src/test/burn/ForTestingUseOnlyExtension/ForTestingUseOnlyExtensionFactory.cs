// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace ForTestingUseOnly
{
    using System;
    using System.Collections.Generic;
    using WixToolset.Extensibility;

    public class ForTestingUseOnlyExtensionFactory : BaseExtensionFactory
    {
        protected override IReadOnlyCollection<Type> ExtensionTypes => new[]
        {
            typeof(ForTestingUseOnlyCompiler),
            typeof(ForTestingUseOnlyExtensionData),
            typeof(ForTestingUseOnlyBurnBackendExtension),
        };
    }
}
