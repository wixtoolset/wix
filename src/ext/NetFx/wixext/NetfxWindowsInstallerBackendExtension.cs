// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Netfx
{
    using System.Collections.Generic;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    public class NetfxWindowsInstallerBackendBinderExtension : BaseWindowsInstallerBackendBinderExtension
    {
        public override IReadOnlyCollection<TableDefinition> TableDefinitions => NetfxTableDefinitions.All;
    }
}
