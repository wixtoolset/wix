// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace ForTestingUseOnly
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    public sealed class ForTestingUseOnlyExtensionData : BaseExtensionData
    {
        public override bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition)
        {
            symbolDefinition = ForTestingUseOnlySymbolDefinitions.ByName(name);
            return symbolDefinition != null;
        }
    }
}
