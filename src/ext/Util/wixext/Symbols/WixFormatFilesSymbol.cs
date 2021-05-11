// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixFormatFiles = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.WixFormatFiles.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFormatFilesSymbolFields.BinaryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFormatFilesSymbolFields.FileRef), IntermediateFieldType.String),
            },
            typeof(WixFormatFilesSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum WixFormatFilesSymbolFields
    {
        BinaryRef,
        FileRef,
    }

    public class WixFormatFilesSymbol : IntermediateSymbol
    {
        public WixFormatFilesSymbol() : base(UtilSymbolDefinitions.WixFormatFiles, null, null)
        {
        }

        public WixFormatFilesSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.WixFormatFiles, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFormatFilesSymbolFields index] => this.Fields[(int)index];

        public string BinaryRef
        {
            get => this.Fields[(int)WixFormatFilesSymbolFields.BinaryRef].AsString();
            set => this.Set((int)WixFormatFilesSymbolFields.BinaryRef, value);
        }

        public string FileRef
        {
            get => this.Fields[(int)WixFormatFilesSymbolFields.FileRef].AsString();
            set => this.Set((int)WixFormatFilesSymbolFields.FileRef, value);
        }
    }
}