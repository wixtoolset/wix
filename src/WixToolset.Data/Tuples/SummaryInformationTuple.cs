// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition SummaryInformation = new IntermediateSymbolDefinition(
            SymbolDefinitionType.SummaryInformation,
            new[]
            {
                new IntermediateFieldDefinition(nameof(SummaryInformationSymbolFields.PropertyId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(SummaryInformationSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(SummaryInformationSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum SummaryInformationSymbolFields
    {
        PropertyId,
        Value,
    }

    public enum SummaryInformationType
    {
        Codepage = 1,
        Title,
        Subject,
        PatchPackageName = 3, //used by patches
        Author,
        Keywords,
        Comments,
        PlatformAndLanguage,
        PatchProductCodes = 7, // used by patches
        TransformPlatformAndLanguageOrStorageNames,
        TransformNames = 8, // used by patches
        PackageCode,
        PatchCode = 9, // used by patches
        TransformProductCodes = 9, // used by transforms
        Reserved11 = 11, // reserved by patches
        Created,
        LastSaved,
        WindowsInstallerVersion,
        Reserved14 = 14, // reserved by patches
        WordCount,
        PatchInstallerRequirement = 15, // used by patches
        Reserved16, // reserved by patches
        TransformValidationFlags = 16, // used by transforms
        CreatingApplication = 18,
        Security
    }

    /// <summary>
    /// Summary information values for the PachInstallerRequirement property.
    /// </summary>
    public enum PatchInstallerRequirement
    {
        /// <summary>Any version of the installer will do</summary>
        Version10 = 1,

        /// <summary>At least 1.2</summary>
        Version12 = 2,

        /// <summary>At least 2.0</summary>
        Version20 = 3,

        /// <summary>At least 3.0</summary>
        Version30 = 4,

        /// <summary>At least 3.1</summary>
        Version31 = 5,
    }

    public class SummaryInformationSymbol : IntermediateSymbol
    {
        public SummaryInformationSymbol() : base(SymbolDefinitions.SummaryInformation, null, null)
        {
        }

        public SummaryInformationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.SummaryInformation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SummaryInformationSymbolFields index] => this.Fields[(int)index];

        public SummaryInformationType PropertyId
        {
            get => (SummaryInformationType)this.Fields[(int)SummaryInformationSymbolFields.PropertyId].AsNumber();
            set => this.Set((int)SummaryInformationSymbolFields.PropertyId, (int)value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)SummaryInformationSymbolFields.Value];
            set => this.Set((int)SummaryInformationSymbolFields.Value, value);
        }
    }
}
