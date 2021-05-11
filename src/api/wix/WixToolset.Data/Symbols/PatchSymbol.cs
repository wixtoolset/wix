// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Patch = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Patch,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PatchSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchSymbolFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(PatchSymbolFields.PatchSize), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(PatchSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(PatchSymbolFields.Header), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(PatchSymbolFields.StreamRef), IntermediateFieldType.String),
            },
            typeof(PatchSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum PatchSymbolFields
    {
        FileRef,
        Sequence,
        PatchSize,
        Attributes,
        Header,
        StreamRef,
    }

    public class PatchSymbol : IntermediateSymbol
    {
        public PatchSymbol() : base(SymbolDefinitions.Patch, null, null)
        {
        }

        public PatchSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Patch, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PatchSymbolFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => (string)this.Fields[(int)PatchSymbolFields.FileRef];
            set => this.Set((int)PatchSymbolFields.FileRef, value);
        }

        public int Sequence
        {
            get => (int)this.Fields[(int)PatchSymbolFields.Sequence];
            set => this.Set((int)PatchSymbolFields.Sequence, value);
        }

        public int PatchSize
        {
            get => (int)this.Fields[(int)PatchSymbolFields.PatchSize];
            set => this.Set((int)PatchSymbolFields.PatchSize, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)PatchSymbolFields.Attributes];
            set => this.Set((int)PatchSymbolFields.Attributes, value);
        }

        public string Header
        {
            get => (string)this.Fields[(int)PatchSymbolFields.Header];
            set => this.Set((int)PatchSymbolFields.Header, value);
        }

        public string StreamRef
        {
            get => (string)this.Fields[(int)PatchSymbolFields.StreamRef];
            set => this.Set((int)PatchSymbolFields.StreamRef, value);
        }
    }
}