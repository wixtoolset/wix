// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBalPackageInfo = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixBalPackageInfo.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalPackageInfoSymbolFields.PackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBalPackageInfoSymbolFields.DisplayInternalUICondition), IntermediateFieldType.String),
            },
            typeof(WixBalPackageInfoSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using WixToolset.Data;

    public enum WixBalPackageInfoSymbolFields
    {
        PackageId,
        DisplayInternalUICondition,
    }

    public class WixBalPackageInfoSymbol : IntermediateSymbol
    {
        public WixBalPackageInfoSymbol() : base(BalSymbolDefinitions.WixBalPackageInfo, null, null)
        {
        }

        public WixBalPackageInfoSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixBalPackageInfo, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalPackageInfoSymbolFields index] => this.Fields[(int)index];

        public string PackageId
        {
            get => this.Fields[(int)WixBalPackageInfoSymbolFields.PackageId].AsString();
            set => this.Set((int)WixBalPackageInfoSymbolFields.PackageId, value);
        }

        public string DisplayInternalUICondition
        {
            get => this.Fields[(int)WixBalPackageInfoSymbolFields.DisplayInternalUICondition].AsString();
            set => this.Set((int)WixBalPackageInfoSymbolFields.DisplayInternalUICondition, value);
        }
    }
}
