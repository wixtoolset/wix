// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusApplicationProperty = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusApplicationProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusApplicationPropertySymbolFields.ApplicationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationPropertySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationPropertySymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusApplicationPropertySymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusApplicationPropertySymbolFields
    {
        ApplicationRef,
        Name,
        Value,
    }

    public class ComPlusApplicationPropertySymbol : IntermediateSymbol
    {
        public ComPlusApplicationPropertySymbol() : base(ComPlusSymbolDefinitions.ComPlusApplicationProperty, null, null)
        {
        }

        public ComPlusApplicationPropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusApplicationProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusApplicationPropertySymbolFields index] => this.Fields[(int)index];

        public string ApplicationRef
        {
            get => this.Fields[(int)ComPlusApplicationPropertySymbolFields.ApplicationRef].AsString();
            set => this.Set((int)ComPlusApplicationPropertySymbolFields.ApplicationRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationPropertySymbolFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationPropertySymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusApplicationPropertySymbolFields.Value].AsString();
            set => this.Set((int)ComPlusApplicationPropertySymbolFields.Value, value);
        }
    }
}