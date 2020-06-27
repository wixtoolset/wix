// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusMethodProperty = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusMethodProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusMethodPropertySymbolFields.MethodRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusMethodPropertySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusMethodPropertySymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusMethodPropertySymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusMethodPropertySymbolFields
    {
        MethodRef,
        Name,
        Value,
    }

    public class ComPlusMethodPropertySymbol : IntermediateSymbol
    {
        public ComPlusMethodPropertySymbol() : base(ComPlusSymbolDefinitions.ComPlusMethodProperty, null, null)
        {
        }

        public ComPlusMethodPropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusMethodProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusMethodPropertySymbolFields index] => this.Fields[(int)index];

        public string MethodRef
        {
            get => this.Fields[(int)ComPlusMethodPropertySymbolFields.MethodRef].AsString();
            set => this.Set((int)ComPlusMethodPropertySymbolFields.MethodRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusMethodPropertySymbolFields.Name].AsString();
            set => this.Set((int)ComPlusMethodPropertySymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusMethodPropertySymbolFields.Value].AsString();
            set => this.Set((int)ComPlusMethodPropertySymbolFields.Value, value);
        }
    }
}