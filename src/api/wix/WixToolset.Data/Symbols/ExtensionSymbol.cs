// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition Extension = new IntermediateSymbolDefinition(
            SymbolDefinitionType.Extension,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ExtensionSymbolFields.Extension), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionSymbolFields.ProgIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionSymbolFields.MimeRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ExtensionSymbolFields.FeatureRef), IntermediateFieldType.String),
            },
            typeof(ExtensionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ExtensionSymbolFields
    {
        Extension,
        ComponentRef,
        ProgIdRef,
        MimeRef,
        FeatureRef,
    }

    public class ExtensionSymbol : IntermediateSymbol
    {
        public ExtensionSymbol() : base(SymbolDefinitions.Extension, null, null)
        {
        }

        public ExtensionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.Extension, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ExtensionSymbolFields index] => this.Fields[(int)index];

        public string Extension
        {
            get => (string)this.Fields[(int)ExtensionSymbolFields.Extension];
            set => this.Set((int)ExtensionSymbolFields.Extension, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ExtensionSymbolFields.ComponentRef];
            set => this.Set((int)ExtensionSymbolFields.ComponentRef, value);
        }

        public string ProgIdRef
        {
            get => (string)this.Fields[(int)ExtensionSymbolFields.ProgIdRef];
            set => this.Set((int)ExtensionSymbolFields.ProgIdRef, value);
        }

        public string MimeRef
        {
            get => (string)this.Fields[(int)ExtensionSymbolFields.MimeRef];
            set => this.Set((int)ExtensionSymbolFields.MimeRef, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)ExtensionSymbolFields.FeatureRef];
            set => this.Set((int)ExtensionSymbolFields.FeatureRef, value);
        }
    }
}