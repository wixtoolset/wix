// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusInterfaceProperty = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusInterfaceProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusInterfacePropertySymbolFields.InterfaceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusInterfacePropertySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusInterfacePropertySymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusInterfacePropertySymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusInterfacePropertySymbolFields
    {
        InterfaceRef,
        Name,
        Value,
    }

    public class ComPlusInterfacePropertySymbol : IntermediateSymbol
    {
        public ComPlusInterfacePropertySymbol() : base(ComPlusSymbolDefinitions.ComPlusInterfaceProperty, null, null)
        {
        }

        public ComPlusInterfacePropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusInterfaceProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusInterfacePropertySymbolFields index] => this.Fields[(int)index];

        public string InterfaceRef
        {
            get => this.Fields[(int)ComPlusInterfacePropertySymbolFields.InterfaceRef].AsString();
            set => this.Set((int)ComPlusInterfacePropertySymbolFields.InterfaceRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusInterfacePropertySymbolFields.Name].AsString();
            set => this.Set((int)ComPlusInterfacePropertySymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusInterfacePropertySymbolFields.Value].AsString();
            set => this.Set((int)ComPlusInterfacePropertySymbolFields.Value, value);
        }
    }
}