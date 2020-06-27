// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.UI
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    public sealed class UIExtensionData : BaseExtensionData
    {
        public override string DefaultCulture => "en-US";

        public override bool TryGetSymbolDefinitionByName(string name, out IntermediateSymbolDefinition symbolDefinition)
        {
            symbolDefinition = null;
            return symbolDefinition != null;
        }

        public override Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions)
        {
            return Intermediate.Load(typeof(UIExtensionData).Assembly, "WixToolset.UI.ui.wixlib", symbolDefinitions);
        }
    }
}
