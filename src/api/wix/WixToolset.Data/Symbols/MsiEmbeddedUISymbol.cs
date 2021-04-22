// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MsiEmbeddedUI = new IntermediateSymbolDefinition(
            SymbolDefinitionType.MsiEmbeddedUI,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUISymbolFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUISymbolFields.EntryPoint), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUISymbolFields.SupportsBasicUI), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUISymbolFields.MessageFilter), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MsiEmbeddedUISymbolFields.Source), IntermediateFieldType.Path),
            },
            typeof(MsiEmbeddedUISymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum MsiEmbeddedUISymbolFields
    {
        FileName,
        EntryPoint,
        SupportsBasicUI,
        MessageFilter,
        Source,
    }

    public class MsiEmbeddedUISymbol : IntermediateSymbol
    {
        public MsiEmbeddedUISymbol() : base(SymbolDefinitions.MsiEmbeddedUI, null, null)
        {
        }

        public MsiEmbeddedUISymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.MsiEmbeddedUI, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiEmbeddedUISymbolFields index] => this.Fields[(int)index];

        public string FileName
        {
            get => (string)this.Fields[(int)MsiEmbeddedUISymbolFields.FileName];
            set => this.Set((int)MsiEmbeddedUISymbolFields.FileName, value);
        }

        public bool EntryPoint
        {
            get => this.Fields[(int)MsiEmbeddedUISymbolFields.EntryPoint].AsBool();
            set => this.Set((int)MsiEmbeddedUISymbolFields.EntryPoint, value);
        }

        public bool SupportsBasicUI
        {
            get => this.Fields[(int)MsiEmbeddedUISymbolFields.SupportsBasicUI].AsBool();
            set => this.Set((int)MsiEmbeddedUISymbolFields.SupportsBasicUI, value);
        }

        public int? MessageFilter
        {
            get => (int?)this.Fields[(int)MsiEmbeddedUISymbolFields.MessageFilter];
            set => this.Set((int)MsiEmbeddedUISymbolFields.MessageFilter, value);
        }

        public string Source
        {
            get => (string)this.Fields[(int)MsiEmbeddedUISymbolFields.Source];
            set => this.Set((int)MsiEmbeddedUISymbolFields.Source, value);
        }
    }
}
