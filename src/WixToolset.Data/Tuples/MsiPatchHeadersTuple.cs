// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiPatchHeaders = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiPatchHeaders,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchHeadersSymbolFields.StreamRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchHeadersSymbolFields.Header), IntermediateFieldType.Path),
            },
            typeof(MsiPatchHeadersSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiPatchHeadersSymbolFields
    {
        StreamRef,
        Header,
    }

    public class MsiPatchHeadersSymbol : IntermediateSymbol
    {
        public MsiPatchHeadersSymbol() : base(SymbolDefinitions.MsiPatchHeaders, null, null)
        {
        }

        public MsiPatchHeadersSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiPatchHeaders, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchHeadersSymbolFields index] => this.Fields[(int)index];

        public string StreamRef
        {
            get => (string)this.Fields[(int)MsiPatchHeadersSymbolFields.StreamRef];
            set => this.Set((int)MsiPatchHeadersSymbolFields.StreamRef, value);
        }

        public string Header
        {
            get => (string)this.Fields[(int)MsiPatchHeadersSymbolFields.Header];
            set => this.Set((int)MsiPatchHeadersSymbolFields.Header, value);
        }
    }
}