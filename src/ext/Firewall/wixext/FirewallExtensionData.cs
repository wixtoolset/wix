// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    public sealed class FirewallExtensionData : BaseExtensionData
    {
        public override bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition)
        {
            symbolDefinition = FirewallSymbolDefinitions.ByName(name);
            return symbolDefinition != null;
        }

        public override Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions)
        {
            return Intermediate.Load(typeof(FirewallExtensionData).Assembly, "WixToolset.Firewall.firewall.wixlib", symbolDefinitions);
        }
    }
}
