// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusPartitionRole = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusPartitionRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleSymbolFields.PartitionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionRoleSymbolFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionRoleSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusPartitionRoleSymbolFields
    {
        PartitionRef,
        ComponentRef,
        Name,
    }

    public class ComPlusPartitionRoleSymbol : IntermediateSymbol
    {
        public ComPlusPartitionRoleSymbol() : base(ComPlusSymbolDefinitions.ComPlusPartitionRole, null, null)
        {
        }

        public ComPlusPartitionRoleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusPartitionRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusPartitionRoleSymbolFields index] => this.Fields[(int)index];

        public string PartitionRef
        {
            get => this.Fields[(int)ComPlusPartitionRoleSymbolFields.PartitionRef].AsString();
            set => this.Set((int)ComPlusPartitionRoleSymbolFields.PartitionRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusPartitionRoleSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusPartitionRoleSymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusPartitionRoleSymbolFields.Name].AsString();
            set => this.Set((int)ComPlusPartitionRoleSymbolFields.Name, value);
        }
    }
}