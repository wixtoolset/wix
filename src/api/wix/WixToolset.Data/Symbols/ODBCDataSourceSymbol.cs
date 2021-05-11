// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ODBCDataSource = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ODBCDataSource,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCDataSourceSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDataSourceSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDataSourceSymbolFields.DriverDescription), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDataSourceSymbolFields.Registration), IntermediateFieldType.Number),
            },
            typeof(ODBCDataSourceSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ODBCDataSourceSymbolFields
    {
        ComponentRef,
        Description,
        DriverDescription,
        Registration,
    }

    public class ODBCDataSourceSymbol : IntermediateSymbol
    {
        public ODBCDataSourceSymbol() : base(SymbolDefinitions.ODBCDataSource, null, null)
        {
        }

        public ODBCDataSourceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ODBCDataSource, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCDataSourceSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ODBCDataSourceSymbolFields.ComponentRef];
            set => this.Set((int)ODBCDataSourceSymbolFields.ComponentRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ODBCDataSourceSymbolFields.Description];
            set => this.Set((int)ODBCDataSourceSymbolFields.Description, value);
        }

        public string DriverDescription
        {
            get => (string)this.Fields[(int)ODBCDataSourceSymbolFields.DriverDescription];
            set => this.Set((int)ODBCDataSourceSymbolFields.DriverDescription, value);
        }

        public int Registration
        {
            get => (int)this.Fields[(int)ODBCDataSourceSymbolFields.Registration];
            set => this.Set((int)ODBCDataSourceSymbolFields.Registration, value);
        }
    }
}
