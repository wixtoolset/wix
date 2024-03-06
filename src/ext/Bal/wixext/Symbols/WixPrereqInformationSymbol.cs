// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplications
{
    using WixToolset.Data;
    using WixToolset.BootstrapperApplications.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPrereqInformation = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixPrereqInformation.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPrereqInformationSymbolFields.PackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPrereqInformationSymbolFields.LicenseFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPrereqInformationSymbolFields.LicenseUrl), IntermediateFieldType.String),
            },
            typeof(WixPrereqInformationSymbol));
    }
}

namespace WixToolset.BootstrapperApplications.Symbols
{
    using WixToolset.Data;

    public enum WixPrereqInformationSymbolFields
    {
        PackageId,
        LicenseFile,
        LicenseUrl,
    }

    public class WixPrereqInformationSymbol : IntermediateSymbol
    {
        public WixPrereqInformationSymbol() : base(BalSymbolDefinitions.WixPrereqInformation, null, null)
        {
        }

        public WixPrereqInformationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixPrereqInformation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPrereqInformationSymbolFields index] => this.Fields[(int)index];

        public string PackageId
        {
            get => this.Fields[(int)WixPrereqInformationSymbolFields.PackageId].AsString();
            set => this.Set((int)WixPrereqInformationSymbolFields.PackageId, value);
        }

        public string LicenseFile
        {
            get => this.Fields[(int)WixPrereqInformationSymbolFields.LicenseFile].AsString();
            set => this.Set((int)WixPrereqInformationSymbolFields.LicenseFile, value);
        }

        public string LicenseUrl
        {
            get => this.Fields[(int)WixPrereqInformationSymbolFields.LicenseUrl].AsString();
            set => this.Set((int)WixPrereqInformationSymbolFields.LicenseUrl, value);
        }
    }
}
