// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition UpgradedFilesOptionalData = new IntermediateSymbolDefinition(
            SymbolDefinitionType.UpgradedFilesOptionalData,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataSymbolFields.Upgraded), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataSymbolFields.FTK), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataSymbolFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataSymbolFields.AllowIgnoreOnPatchError), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(UpgradedFilesOptionalDataSymbolFields.IncludeWholeFile), IntermediateFieldType.Bool),
            },
            typeof(UpgradedFilesOptionalDataSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum UpgradedFilesOptionalDataSymbolFields
    {
        Upgraded,
        FTK,
        SymbolPaths,
        AllowIgnoreOnPatchError,
        IncludeWholeFile,
    }

    public class UpgradedFilesOptionalDataSymbol : IntermediateSymbol
    {
        public UpgradedFilesOptionalDataSymbol() : base(SymbolDefinitions.UpgradedFilesOptionalData, null, null)
        {
        }

        public UpgradedFilesOptionalDataSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.UpgradedFilesOptionalData, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UpgradedFilesOptionalDataSymbolFields index] => this.Fields[(int)index];

        public string Upgraded
        {
            get => (string)this.Fields[(int)UpgradedFilesOptionalDataSymbolFields.Upgraded];
            set => this.Set((int)UpgradedFilesOptionalDataSymbolFields.Upgraded, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)UpgradedFilesOptionalDataSymbolFields.FTK];
            set => this.Set((int)UpgradedFilesOptionalDataSymbolFields.FTK, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)UpgradedFilesOptionalDataSymbolFields.SymbolPaths];
            set => this.Set((int)UpgradedFilesOptionalDataSymbolFields.SymbolPaths, value);
        }

        public bool? AllowIgnoreOnPatchError
        {
            get => (bool?)this.Fields[(int)UpgradedFilesOptionalDataSymbolFields.AllowIgnoreOnPatchError];
            set => this.Set((int)UpgradedFilesOptionalDataSymbolFields.AllowIgnoreOnPatchError, value);
        }

        public bool? IncludeWholeFile
        {
            get => (bool?)this.Fields[(int)UpgradedFilesOptionalDataSymbolFields.IncludeWholeFile];
            set => this.Set((int)UpgradedFilesOptionalDataSymbolFields.IncludeWholeFile, value);
        }
    }
}
