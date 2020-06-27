// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusPartition = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusPartition.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusPartitionSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionSymbolFields.PartitionId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionSymbolFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusPartitionSymbolFields
    {
        ComponentRef,
        PartitionId,
        Name,
    }

    public class ComPlusPartitionSymbol : IntermediateSymbol
    {
        public ComPlusPartitionSymbol() : base(ComPlusSymbolDefinitions.ComPlusPartition, null, null)
        {
        }

        public ComPlusPartitionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusPartition, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusPartitionSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusPartitionSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusPartitionSymbolFields.ComponentRef, value);
        }

        public string PartitionId
        {
            get => this.Fields[(int)ComPlusPartitionSymbolFields.PartitionId].AsString();
            set => this.Set((int)ComPlusPartitionSymbolFields.PartitionId, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusPartitionSymbolFields.Name].AsString();
            set => this.Set((int)ComPlusPartitionSymbolFields.Name, value);
        }
    }
}