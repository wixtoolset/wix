// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition XmlFile = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.XmlFile.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(XmlFileSymbolFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileSymbolFields.ElementPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileSymbolFields.Flags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(XmlFileSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlFileSymbolFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(XmlFileSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum XmlFileSymbolFields
    {
        File,
        ElementPath,
        Name,
        Value,
        Flags,
        ComponentRef,
        Sequence,
    }

    public class XmlFileSymbol : IntermediateSymbol
    {
        public XmlFileSymbol() : base(UtilSymbolDefinitions.XmlFile, null, null)
        {
        }

        public XmlFileSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.XmlFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[XmlFileSymbolFields index] => this.Fields[(int)index];

        public string File
        {
            get => this.Fields[(int)XmlFileSymbolFields.File].AsString();
            set => this.Set((int)XmlFileSymbolFields.File, value);
        }

        public string ElementPath
        {
            get => this.Fields[(int)XmlFileSymbolFields.ElementPath].AsString();
            set => this.Set((int)XmlFileSymbolFields.ElementPath, value);
        }

        public string Name
        {
            get => this.Fields[(int)XmlFileSymbolFields.Name].AsString();
            set => this.Set((int)XmlFileSymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)XmlFileSymbolFields.Value].AsString();
            set => this.Set((int)XmlFileSymbolFields.Value, value);
        }

        public int Flags
        {
            get => this.Fields[(int)XmlFileSymbolFields.Flags].AsNumber();
            set => this.Set((int)XmlFileSymbolFields.Flags, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)XmlFileSymbolFields.ComponentRef].AsString();
            set => this.Set((int)XmlFileSymbolFields.ComponentRef, value);
        }

        public int? Sequence
        {
            get => this.Fields[(int)XmlFileSymbolFields.Sequence].AsNullableNumber();
            set => this.Set((int)XmlFileSymbolFields.Sequence, value);
        }
    }
}