// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiFileHash = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiFileHash,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiFileHashSymbolFields.Options), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiFileHashSymbolFields.HashPart1), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiFileHashSymbolFields.HashPart2), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiFileHashSymbolFields.HashPart3), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiFileHashSymbolFields.HashPart4), IntermediateFieldType.Number),
            },
            typeof(MsiFileHashSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiFileHashSymbolFields
    {
        Options,
        HashPart1,
        HashPart2,
        HashPart3,
        HashPart4,
    }

    public class MsiFileHashSymbol : IntermediateSymbol
    {
        public MsiFileHashSymbol() : base(SymbolDefinitions.MsiFileHash, null, null)
        {
        }

        public MsiFileHashSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiFileHash, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiFileHashSymbolFields index] => this.Fields[(int)index];

        public int Options
        {
            get => (int)this.Fields[(int)MsiFileHashSymbolFields.Options];
            set => this.Set((int)MsiFileHashSymbolFields.Options, value);
        }

        public int HashPart1
        {
            get => (int)this.Fields[(int)MsiFileHashSymbolFields.HashPart1];
            set => this.Set((int)MsiFileHashSymbolFields.HashPart1, value);
        }

        public int HashPart2
        {
            get => (int)this.Fields[(int)MsiFileHashSymbolFields.HashPart2];
            set => this.Set((int)MsiFileHashSymbolFields.HashPart2, value);
        }

        public int HashPart3
        {
            get => (int)this.Fields[(int)MsiFileHashSymbolFields.HashPart3];
            set => this.Set((int)MsiFileHashSymbolFields.HashPart3, value);
        }

        public int HashPart4
        {
            get => (int)this.Fields[(int)MsiFileHashSymbolFields.HashPart4];
            set => this.Set((int)MsiFileHashSymbolFields.HashPart4, value);
        }
    }
}