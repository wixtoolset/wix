// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixMbaPrereqInformation = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixMbaPrereqInformation.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixMbaPrereqInformationSymbolFields.PackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMbaPrereqInformationSymbolFields.LicenseFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixMbaPrereqInformationSymbolFields.LicenseUrl), IntermediateFieldType.String),
            },
            typeof(WixMbaPrereqInformationSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using WixToolset.Data;

    public enum WixMbaPrereqInformationSymbolFields
    {
        PackageId,
        LicenseFile,
        LicenseUrl,
    }

    public class WixMbaPrereqInformationSymbol : IntermediateSymbol
    {
        public WixMbaPrereqInformationSymbol() : base(BalSymbolDefinitions.WixMbaPrereqInformation, null, null)
        {
        }

        public WixMbaPrereqInformationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixMbaPrereqInformation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixMbaPrereqInformationSymbolFields index] => this.Fields[(int)index];

        public string PackageId
        {
            get => this.Fields[(int)WixMbaPrereqInformationSymbolFields.PackageId].AsString();
            set => this.Set((int)WixMbaPrereqInformationSymbolFields.PackageId, value);
        }

        public string LicenseFile
        {
            get => this.Fields[(int)WixMbaPrereqInformationSymbolFields.LicenseFile].AsString();
            set => this.Set((int)WixMbaPrereqInformationSymbolFields.LicenseFile, value);
        }

        public string LicenseUrl
        {
            get => this.Fields[(int)WixMbaPrereqInformationSymbolFields.LicenseUrl].AsString();
            set => this.Set((int)WixMbaPrereqInformationSymbolFields.LicenseUrl, value);
        }
    }
}