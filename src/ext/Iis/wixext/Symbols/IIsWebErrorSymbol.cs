// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebError = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebError.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebErrorSymbolFields.ErrorCode), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebErrorSymbolFields.SubCode), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebErrorSymbolFields.ParentType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsWebErrorSymbolFields.ParentValue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebErrorSymbolFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebErrorSymbolFields.URL), IntermediateFieldType.String),
            },
            typeof(IIsWebErrorSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebErrorSymbolFields
    {
        ErrorCode,
        SubCode,
        ParentType,
        ParentValue,
        File,
        URL,
    }

    public class IIsWebErrorSymbol : IntermediateSymbol
    {
        public IIsWebErrorSymbol() : base(IisSymbolDefinitions.IIsWebError, null, null)
        {
        }

        public IIsWebErrorSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebError, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebErrorSymbolFields index] => this.Fields[(int)index];

        public int ErrorCode
        {
            get => this.Fields[(int)IIsWebErrorSymbolFields.ErrorCode].AsNumber();
            set => this.Set((int)IIsWebErrorSymbolFields.ErrorCode, value);
        }

        public int SubCode
        {
            get => this.Fields[(int)IIsWebErrorSymbolFields.SubCode].AsNumber();
            set => this.Set((int)IIsWebErrorSymbolFields.SubCode, value);
        }

        public int ParentType
        {
            get => this.Fields[(int)IIsWebErrorSymbolFields.ParentType].AsNumber();
            set => this.Set((int)IIsWebErrorSymbolFields.ParentType, value);
        }

        public string ParentValue
        {
            get => this.Fields[(int)IIsWebErrorSymbolFields.ParentValue].AsString();
            set => this.Set((int)IIsWebErrorSymbolFields.ParentValue, value);
        }

        public string File
        {
            get => this.Fields[(int)IIsWebErrorSymbolFields.File].AsString();
            set => this.Set((int)IIsWebErrorSymbolFields.File, value);
        }

        public string URL
        {
            get => this.Fields[(int)IIsWebErrorSymbolFields.URL].AsString();
            set => this.Set((int)IIsWebErrorSymbolFields.URL, value);
        }
    }
}