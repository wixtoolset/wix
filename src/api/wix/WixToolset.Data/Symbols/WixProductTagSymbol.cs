// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixProductTag = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixProductTag,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixProductTagSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixProductTagSymbolFields.Regid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixProductTagSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixProductTagSymbolFields.Attributes), IntermediateFieldType.Number)
            },
            typeof(WixProductTagSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixProductTagSymbolFields
    {
        FileRef,
        Regid,
        Name,
        Attributes
    }

    public class WixProductTagSymbol : IntermediateSymbol
    {
        public WixProductTagSymbol() : base(SymbolDefinitions.WixProductTag, null, null)
        {
        }

        public WixProductTagSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixProductTag, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixProductTagSymbolFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => this.Fields[(int)WixProductTagSymbolFields.FileRef].AsString();
            set => this.Set((int)WixProductTagSymbolFields.FileRef, value);
        }

        public string Regid
        {
            get => this.Fields[(int)WixProductTagSymbolFields.Regid].AsString();
            set => this.Set((int)WixProductTagSymbolFields.Regid, value);
        }

        public string Name
        {
            get => this.Fields[(int)WixProductTagSymbolFields.Name].AsString();
            set => this.Set((int)WixProductTagSymbolFields.Name, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixProductTagSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixProductTagSymbolFields.Attributes, value);
        }
    }
}
