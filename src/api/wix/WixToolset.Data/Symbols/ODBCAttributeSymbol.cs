// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ODBCAttribute = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ODBCAttribute,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCAttributeSymbolFields.DriverRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCAttributeSymbolFields.Attribute), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCAttributeSymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ODBCAttributeSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ODBCAttributeSymbolFields
    {
        DriverRef,
        Attribute,
        Value,
    }

    public class ODBCAttributeSymbol : IntermediateSymbol
    {
        public ODBCAttributeSymbol() : base(SymbolDefinitions.ODBCAttribute, null, null)
        {
        }

        public ODBCAttributeSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ODBCAttribute, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCAttributeSymbolFields index] => this.Fields[(int)index];

        public string DriverRef
        {
            get => (string)this.Fields[(int)ODBCAttributeSymbolFields.DriverRef];
            set => this.Set((int)ODBCAttributeSymbolFields.DriverRef, value);
        }

        public string Attribute
        {
            get => (string)this.Fields[(int)ODBCAttributeSymbolFields.Attribute];
            set => this.Set((int)ODBCAttributeSymbolFields.Attribute, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ODBCAttributeSymbolFields.Value];
            set => this.Set((int)ODBCAttributeSymbolFields.Value, value);
        }
    }
}