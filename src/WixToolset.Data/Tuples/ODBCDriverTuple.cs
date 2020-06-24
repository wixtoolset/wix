// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ODBCDriver = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ODBCDriver,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCDriverSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCDriverSymbolFields.SetupFileRef), IntermediateFieldType.String),
            },
            typeof(ODBCDriverSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ODBCDriverSymbolFields
    {
        ComponentRef,
        Description,
        FileRef,
        SetupFileRef,
    }

    public class ODBCDriverSymbol : IntermediateSymbol
    {
        public ODBCDriverSymbol() : base(SymbolDefinitions.ODBCDriver, null, null)
        {
        }

        public ODBCDriverSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ODBCDriver, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCDriverSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ODBCDriverSymbolFields.ComponentRef];
            set => this.Set((int)ODBCDriverSymbolFields.ComponentRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ODBCDriverSymbolFields.Description];
            set => this.Set((int)ODBCDriverSymbolFields.Description, value);
        }

        public string FileRef
        {
            get => (string)this.Fields[(int)ODBCDriverSymbolFields.FileRef];
            set => this.Set((int)ODBCDriverSymbolFields.FileRef, value);
        }

        public string SetupFileRef
        {
            get => (string)this.Fields[(int)ODBCDriverSymbolFields.SetupFileRef];
            set => this.Set((int)ODBCDriverSymbolFields.SetupFileRef, value);
        }
    }
}
