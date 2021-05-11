// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsMimeMap = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsMimeMap.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsMimeMapSymbolFields.ParentType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsMimeMapSymbolFields.ParentValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsMimeMapSymbolFields.MimeType), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsMimeMapSymbolFields.Extension), IntermediateFieldType.String),
            },
            typeof(IIsMimeMapSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsMimeMapSymbolFields
    {
        ParentType,
        ParentValue,
        MimeType,
        Extension,
    }

    public class IIsMimeMapSymbol : IntermediateSymbol
    {
        public IIsMimeMapSymbol() : base(IisSymbolDefinitions.IIsMimeMap, null, null)
        {
        }

        public IIsMimeMapSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsMimeMap, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsMimeMapSymbolFields index] => this.Fields[(int)index];

        public int ParentType
        {
            get => this.Fields[(int)IIsMimeMapSymbolFields.ParentType].AsNumber();
            set => this.Set((int)IIsMimeMapSymbolFields.ParentType, value);
        }

        public string ParentValue
        {
            get => this.Fields[(int)IIsMimeMapSymbolFields.ParentValue].AsString();
            set => this.Set((int)IIsMimeMapSymbolFields.ParentValue, value);
        }

        public string MimeType
        {
            get => this.Fields[(int)IIsMimeMapSymbolFields.MimeType].AsString();
            set => this.Set((int)IIsMimeMapSymbolFields.MimeType, value);
        }

        public string Extension
        {
            get => this.Fields[(int)IIsMimeMapSymbolFields.Extension].AsString();
            set => this.Set((int)IIsMimeMapSymbolFields.Extension, value);
        }
    }
}