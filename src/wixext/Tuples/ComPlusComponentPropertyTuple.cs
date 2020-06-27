// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusComponentProperty = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusComponentProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusComponentPropertySymbolFields.ComPlusComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusComponentPropertySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusComponentPropertySymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusComponentPropertySymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusComponentPropertySymbolFields
    {
        ComPlusComponentRef,
        Name,
        Value,
    }

    public class ComPlusComponentPropertySymbol : IntermediateSymbol
    {
        public ComPlusComponentPropertySymbol() : base(ComPlusSymbolDefinitions.ComPlusComponentProperty, null, null)
        {
        }

        public ComPlusComponentPropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusComponentProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusComponentPropertySymbolFields index] => this.Fields[(int)index];

        public string ComPlusComponentRef
        {
            get => this.Fields[(int)ComPlusComponentPropertySymbolFields.ComPlusComponentRef].AsString();
            set => this.Set((int)ComPlusComponentPropertySymbolFields.ComPlusComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusComponentPropertySymbolFields.Name].AsString();
            set => this.Set((int)ComPlusComponentPropertySymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusComponentPropertySymbolFields.Value].AsString();
            set => this.Set((int)ComPlusComponentPropertySymbolFields.Value, value);
        }
    }
}