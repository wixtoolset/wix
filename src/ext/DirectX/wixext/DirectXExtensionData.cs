// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.DirectX
{
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The WiX Toolset DirectX Extension.
    /// </summary>
    public sealed class DirectXExtensionData : BaseExtensionData
    {
        public override Intermediate GetLibrary(ISymbolDefinitionCreator symbolDefinitions)
        {
            return Intermediate.Load(typeof(DirectXExtensionData).Assembly, "WixToolset.DirectX.directx.wixlib", symbolDefinitions);
        }
    }
}
