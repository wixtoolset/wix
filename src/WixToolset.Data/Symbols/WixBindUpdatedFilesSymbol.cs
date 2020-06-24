// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBindUpdatedFiles = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBindUpdatedFiles,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBindUpdatedFilesSymbolFields.FileRef), IntermediateFieldType.String),
            },
            typeof(WixBindUpdatedFilesSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBindUpdatedFilesSymbolFields
    {
        FileRef,
    }

    public class WixBindUpdatedFilesSymbol : IntermediateSymbol
    {
        public WixBindUpdatedFilesSymbol() : base(SymbolDefinitions.WixBindUpdatedFiles, null, null)
        {
        }

        public WixBindUpdatedFilesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBindUpdatedFiles, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBindUpdatedFilesSymbolFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => (string)this.Fields[(int)WixBindUpdatedFilesSymbolFields.FileRef];
            set => this.Set((int)WixBindUpdatedFilesSymbolFields.FileRef, value);
        }
    }
}