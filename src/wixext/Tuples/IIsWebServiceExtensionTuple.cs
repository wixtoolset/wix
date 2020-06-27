// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsWebServiceExtension = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsWebServiceExtension.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionSymbolFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionSymbolFields.Group), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebServiceExtensionSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(IIsWebServiceExtensionSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsWebServiceExtensionSymbolFields
    {
        ComponentRef,
        File,
        Description,
        Group,
        Attributes,
    }

    public class IIsWebServiceExtensionSymbol : IntermediateSymbol
    {
        public IIsWebServiceExtensionSymbol() : base(IisSymbolDefinitions.IIsWebServiceExtension, null, null)
        {
        }

        public IIsWebServiceExtensionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsWebServiceExtension, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebServiceExtensionSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)IIsWebServiceExtensionSymbolFields.ComponentRef].AsString();
            set => this.Set((int)IIsWebServiceExtensionSymbolFields.ComponentRef, value);
        }

        public string File
        {
            get => this.Fields[(int)IIsWebServiceExtensionSymbolFields.File].AsString();
            set => this.Set((int)IIsWebServiceExtensionSymbolFields.File, value);
        }

        public string Description
        {
            get => this.Fields[(int)IIsWebServiceExtensionSymbolFields.Description].AsString();
            set => this.Set((int)IIsWebServiceExtensionSymbolFields.Description, value);
        }

        public string Group
        {
            get => this.Fields[(int)IIsWebServiceExtensionSymbolFields.Group].AsString();
            set => this.Set((int)IIsWebServiceExtensionSymbolFields.Group, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)IIsWebServiceExtensionSymbolFields.Attributes].AsNumber();
            set => this.Set((int)IIsWebServiceExtensionSymbolFields.Attributes, value);
        }
    }
}