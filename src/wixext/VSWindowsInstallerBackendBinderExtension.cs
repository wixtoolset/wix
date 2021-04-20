// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using System.Collections.Generic;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;

    public class VSWindowsInstallerBackendBinderExtension : BaseWindowsInstallerBackendBinderExtension
    {
        public override IReadOnlyCollection<TableDefinition> TableDefinitions => VSTableDefinitions.All;
    }
}
