// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ODBCSourceAttribute = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ODBCSourceAttribute,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCSourceAttributeSymbolFields.DataSourceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCSourceAttributeSymbolFields.Attribute), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCSourceAttributeSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ODBCSourceAttributeSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ODBCSourceAttributeSymbolFields
    {
        DataSourceRef,
        Attribute,
        Value,
    }

    public class ODBCSourceAttributeSymbol : IntermediateSymbol
    {
        public ODBCSourceAttributeSymbol() : base(SymbolDefinitions.ODBCSourceAttribute, null, null)
        {
        }

        public ODBCSourceAttributeSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ODBCSourceAttribute, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCSourceAttributeSymbolFields index] => this.Fields[(int)index];

        public string DataSourceRef
        {
            get => (string)this.Fields[(int)ODBCSourceAttributeSymbolFields.DataSourceRef];
            set => this.Set((int)ODBCSourceAttributeSymbolFields.DataSourceRef, value);
        }

        public string Attribute
        {
            get => (string)this.Fields[(int)ODBCSourceAttributeSymbolFields.Attribute];
            set => this.Set((int)ODBCSourceAttributeSymbolFields.Attribute, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ODBCSourceAttributeSymbolFields.Value];
            set => this.Set((int)ODBCSourceAttributeSymbolFields.Value, value);
        }
    }
}