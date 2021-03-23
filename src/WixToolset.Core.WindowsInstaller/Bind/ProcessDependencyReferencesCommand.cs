// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility.Services;

    internal class ProcessDependencyReferencesCommand
    {
        // The root registry key for the dependency extension. We write to Software\Classes explicitly
        // based on the current security context instead of HKCR. See
        // http://msdn.microsoft.com/en-us/library/ms724475(VS.85).aspx for more information.
        private const string DependencyRegistryRoot = @"Software\Classes\Installer\Dependencies\";
        private const string RegistryDependents = "Dependents";

        public ProcessDependencyReferencesCommand(IBackendHelper backendHelper, IntermediateSection section, IEnumerable<WixDependencyRefSymbol> dependencyRefSymbols)
        {
            this.BackendHelper = backendHelper;
            this.Section = section;
            this.DependencyRefSymbols = dependencyRefSymbols;
        }

        private IBackendHelper BackendHelper { get; }

        private IntermediateSection Section { get; }

        private IEnumerable<WixDependencyRefSymbol> DependencyRefSymbols { get; }

        public void Execute()
        {
            var wixDependencyRows = this.Section.Symbols.OfType<WixDependencySymbol>().ToDictionary(d => d.Id.Id);
            var wixDependencyProviderRows = this.Section.Symbols.OfType<WixDependencyProviderSymbol>().ToDictionary(d => d.Id.Id);

            // For each relationship, get the provides and requires rows to generate registry values.
            foreach (var wixDependencyRefRow in this.DependencyRefSymbols)
            {
                var providesId = wixDependencyRefRow.WixDependencyProviderRef;
                var requiresId = wixDependencyRefRow.WixDependencyRef;

                // If we do not find both symbols, skip the registry key generation.
                if (!wixDependencyRows.TryGetValue(requiresId, out var wixDependencyRow))
                {
                    continue;
                }

                if (!wixDependencyProviderRows.TryGetValue(providesId, out var wixDependencyProviderRow))
                {
                    continue;
                }

                // Format the root registry key using the required provider key and the current provider key.
                var requiresKey = wixDependencyRow.Id.Id;
                var providesKey = wixDependencyRow.ProviderKey;
                var keyRequires = String.Format(@"{0}{1}\{2}\{3}", DependencyRegistryRoot, requiresKey, RegistryDependents, providesKey);

                // Get the component ID from the provider.
                var componentId = wixDependencyProviderRow.ParentRef;

                var id = this.BackendHelper.GenerateIdentifier("reg", providesId, requiresId, "(Default)");
                this.Section.AddSymbol(new RegistrySymbol(wixDependencyRefRow.SourceLineNumbers, new Identifier(AccessModifier.Section, id))
                {
                    ComponentRef = componentId,
                    Root = RegistryRootType.MachineUser,
                    Key = keyRequires,
                    Name = "*",
                });

                if (!String.IsNullOrEmpty(wixDependencyRow.MinVersion))
                {
                    id = this.BackendHelper.GenerateIdentifier("reg", providesId, requiresId, "MinVersion");
                    this.Section.AddSymbol(new RegistrySymbol(wixDependencyRefRow.SourceLineNumbers, new Identifier(AccessModifier.Section, id))
                    {
                        ComponentRef = componentId,
                        Root = RegistryRootType.MachineUser,
                        Key = keyRequires,
                        Name = "MinVersion",
                        Value = wixDependencyRow.MinVersion
                    });
                }

                var maxVersion = (string)wixDependencyRow[3];
                if (!String.IsNullOrEmpty(wixDependencyRow.MaxVersion))
                {
                    id = this.BackendHelper.GenerateIdentifier("reg", providesId, requiresId, "MaxVersion");
                    this.Section.AddSymbol(new RegistrySymbol(wixDependencyRefRow.SourceLineNumbers, new Identifier(AccessModifier.Section, id))
                    {
                        ComponentRef = componentId,
                        Root = RegistryRootType.MachineUser,
                        Key = keyRequires,
                        Name = "MaxVersion",
                        Value = wixDependencyRow.MaxVersion
                    });
                }

                if (wixDependencyRow.Attributes != WixDependencySymbolAttributes.None)
                {
                    id = this.BackendHelper.GenerateIdentifier("reg", providesId, requiresId, "Attributes");
                    this.Section.AddSymbol(new RegistrySymbol(wixDependencyRefRow.SourceLineNumbers, new Identifier(AccessModifier.Section, id))
                    {
                        ComponentRef = componentId,
                        Root = RegistryRootType.MachineUser,
                        Key = keyRequires,
                        Name = "Attributes",
                        Value = String.Concat("#", (int)wixDependencyRow.Attributes)
                    });
                }
            }
        }
    }
}
