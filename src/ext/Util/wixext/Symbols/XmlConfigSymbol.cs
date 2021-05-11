// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition XmlConfig = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.XmlConfig.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(XmlConfigSymbolFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigSymbolFields.ElementId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigSymbolFields.ElementPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigSymbolFields.VerifyPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigSymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigSymbolFields.Flags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(XmlConfigSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(XmlConfigSymbolFields.Sequence), IntermediateFieldType.Number),
            },
            typeof(XmlConfigSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum XmlConfigSymbolFields
    {
        File,
        ElementId,
        ElementPath,
        VerifyPath,
        Name,
        Value,
        Flags,
        ComponentRef,
        Sequence,
    }

    public class XmlConfigSymbol : IntermediateSymbol
    {
        public XmlConfigSymbol() : base(UtilSymbolDefinitions.XmlConfig, null, null)
        {
        }

        public XmlConfigSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.XmlConfig, sourceLineNumber, id)
        {
        }

        public IntermediateField this[XmlConfigSymbolFields index] => this.Fields[(int)index];

        public string File
        {
            get => this.Fields[(int)XmlConfigSymbolFields.File].AsString();
            set => this.Set((int)XmlConfigSymbolFields.File, value);
        }

        public string ElementId
        {
            get => this.Fields[(int)XmlConfigSymbolFields.ElementId].AsString();
            set => this.Set((int)XmlConfigSymbolFields.ElementId, value);
        }

        public string ElementPath
        {
            get => this.Fields[(int)XmlConfigSymbolFields.ElementPath].AsString();
            set => this.Set((int)XmlConfigSymbolFields.ElementPath, value);
        }

        public string VerifyPath
        {
            get => this.Fields[(int)XmlConfigSymbolFields.VerifyPath].AsString();
            set => this.Set((int)XmlConfigSymbolFields.VerifyPath, value);
        }

        public string Name
        {
            get => this.Fields[(int)XmlConfigSymbolFields.Name].AsString();
            set => this.Set((int)XmlConfigSymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)XmlConfigSymbolFields.Value].AsString();
            set => this.Set((int)XmlConfigSymbolFields.Value, value);
        }

        public int Flags
        {
            get => this.Fields[(int)XmlConfigSymbolFields.Flags].AsNumber();
            set => this.Set((int)XmlConfigSymbolFields.Flags, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)XmlConfigSymbolFields.ComponentRef].AsString();
            set => this.Set((int)XmlConfigSymbolFields.ComponentRef, value);
        }

        public int? Sequence
        {
            get => this.Fields[(int)XmlConfigSymbolFields.Sequence].AsNullableNumber();
            set => this.Set((int)XmlConfigSymbolFields.Sequence, value);
        }
    }
}