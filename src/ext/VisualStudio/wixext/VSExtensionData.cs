// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.VisualStudio
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    public sealed class VSExtensionData : BaseExtensionData
    {
        /// <summary>
        /// Gets the default culture.
        /// </summary>
        /// <value>The default culture.</value>
        public override string DefaultCulture => "en-US";

        public override Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions)
        {
            return Intermediate.Load(typeof(VSExtensionData).Assembly, "WixToolset.VisualStudio.vs.wixlib", symbolDefinitions);
        }
    }
}
