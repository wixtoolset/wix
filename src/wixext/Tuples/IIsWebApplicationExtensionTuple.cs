// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebApplicationExtension = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebApplicationExtension.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionSymbolFields.ApplicationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionSymbolFields.Extension), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionSymbolFields.Verbs), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionSymbolFields.Executable), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebApplicationExtensionSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(IIsWebApplicationExtensionSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebApplicationExtensionSymbolFields
    {
        ApplicationRef,
        Extension,
        Verbs,
        Executable,
        Attributes,
    }

    public class IIsWebApplicationExtensionSymbol : IntermediateSymbol
    {
        public IIsWebApplicationExtensionSymbol() : base(IisSymbolDefinitions.IIsWebApplicationExtension, null, null)
        {
        }

        public IIsWebApplicationExtensionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebApplicationExtension, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebApplicationExtensionSymbolFields index] => this.Fields[(int)index];

        public string ApplicationRef
        {
            get => this.Fields[(int)IIsWebApplicationExtensionSymbolFields.ApplicationRef].AsString();
            set => this.Set((int)IIsWebApplicationExtensionSymbolFields.ApplicationRef, value);
        }

        public string Extension
        {
            get => this.Fields[(int)IIsWebApplicationExtensionSymbolFields.Extension].AsString();
            set => this.Set((int)IIsWebApplicationExtensionSymbolFields.Extension, value);
        }

        public string Verbs
        {
            get => this.Fields[(int)IIsWebApplicationExtensionSymbolFields.Verbs].AsString();
            set => this.Set((int)IIsWebApplicationExtensionSymbolFields.Verbs, value);
        }

        public string Executable
        {
            get => this.Fields[(int)IIsWebApplicationExtensionSymbolFields.Executable].AsString();
            set => this.Set((int)IIsWebApplicationExtensionSymbolFields.Executable, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsWebApplicationExtensionSymbolFields.Attributes].AsNumber();
            set => this.Set((int)IIsWebApplicationExtensionSymbolFields.Attributes, value);
        }
    }
}