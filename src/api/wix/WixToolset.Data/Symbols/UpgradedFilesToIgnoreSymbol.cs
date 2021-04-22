// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition UpgradedFilesToIgnore = new IntermediateSymbolDefinition(
            SymbolDefinitionType.UpgradedFilesToIgnore,
            new[]
            {
                new IntermediateFieldDefinition(nameof(UpgradedFilesToIgnoreSymbolFields.Upgraded), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(UpgradedFilesToIgnoreSymbolFields.FTK), IntermediateFieldType.String),
            },
            typeof(UpgradedFilesToIgnoreSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum UpgradedFilesToIgnoreSymbolFields
    {
        Upgraded,
        FTK,
    }

    public class UpgradedFilesToIgnoreSymbol : IntermediateSymbol
    {
        public UpgradedFilesToIgnoreSymbol() : base(SymbolDefinitions.UpgradedFilesToIgnore, null, null)
        {
        }

        public UpgradedFilesToIgnoreSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.UpgradedFilesToIgnore, sourceLineNumber, id)
        {
        }

        public IntermediateField this[UpgradedFilesToIgnoreSymbolFields index] => this.Fields[(int)index];

        public string Upgraded
        {
            get => (string)this.Fields[(int)UpgradedFilesToIgnoreSymbolFields.Upgraded];
            set => this.Set((int)UpgradedFilesToIgnoreSymbolFields.Upgraded, value);
        }

        public string FTK
        {
            get => (string)this.Fields[(int)UpgradedFilesToIgnoreSymbolFields.FTK];
            set => this.Set((int)UpgradedFilesToIgnoreSymbolFields.FTK, value);
        }
    }
}