// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusPartitionProperty = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusPartitionProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusPartitionPropertySymbolFields.PartitionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionPropertySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionPropertySymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionPropertySymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusPartitionPropertySymbolFields
    {
        PartitionRef,
        Name,
        Value,
    }

    public class ComPlusPartitionPropertySymbol : IntermediateSymbol
    {
        public ComPlusPartitionPropertySymbol() : base(ComPlusSymbolDefinitions.ComPlusPartitionProperty, null, null)
        {
        }

        public ComPlusPartitionPropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusPartitionProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusPartitionPropertySymbolFields index] => this.Fields[(int)index];

        public string PartitionRef
        {
            get => this.Fields[(int)ComPlusPartitionPropertySymbolFields.PartitionRef].AsString();
            set => this.Set((int)ComPlusPartitionPropertySymbolFields.PartitionRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusPartitionPropertySymbolFields.Name].AsString();
            set => this.Set((int)ComPlusPartitionPropertySymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusPartitionPropertySymbolFields.Value].AsString();
            set => this.Set((int)ComPlusPartitionPropertySymbolFields.Value, value);
        }
    }
}