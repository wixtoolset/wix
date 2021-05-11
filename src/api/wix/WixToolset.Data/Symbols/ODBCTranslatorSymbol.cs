// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ODBCTranslator = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ODBCTranslator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ODBCTranslatorSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorSymbolFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ODBCTranslatorSymbolFields.SetupFileRef), IntermediateFieldType.String),
            },
            typeof(ODBCTranslatorSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ODBCTranslatorSymbolFields
    {
        ComponentRef,
        Description,
        FileRef,
        SetupFileRef,
    }

    public class ODBCTranslatorSymbol : IntermediateSymbol
    {
        public ODBCTranslatorSymbol() : base(SymbolDefinitions.ODBCTranslator, null, null)
        {
        }

        public ODBCTranslatorSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ODBCTranslator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ODBCTranslatorSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)ODBCTranslatorSymbolFields.ComponentRef];
            set => this.Set((int)ODBCTranslatorSymbolFields.ComponentRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ODBCTranslatorSymbolFields.Description];
            set => this.Set((int)ODBCTranslatorSymbolFields.Description, value);
        }

        public string FileRef
        {
            get => (string)this.Fields[(int)ODBCTranslatorSymbolFields.FileRef];
            set => this.Set((int)ODBCTranslatorSymbolFields.FileRef, value);
        }

        public string SetupFileRef
        {
            get => (string)this.Fields[(int)ODBCTranslatorSymbolFields.SetupFileRef];
            set => this.Set((int)ODBCTranslatorSymbolFields.SetupFileRef, value);
        }
    }
}
