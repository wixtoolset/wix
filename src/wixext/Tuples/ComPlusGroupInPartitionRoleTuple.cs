// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusGroupInPartitionRole = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusGroupInPartitionRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleSymbolFields.PartitionRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInPartitionRoleSymbolFields.GroupRef), IntermediateFieldType.String),
            },
            typeof(ComPlusGroupInPartitionRoleSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusGroupInPartitionRoleSymbolFields
    {
        PartitionRoleRef,
        ComponentRef,
        GroupRef,
    }

    public class ComPlusGroupInPartitionRoleSymbol : IntermediateSymbol
    {
        public ComPlusGroupInPartitionRoleSymbol() : base(ComPlusSymbolDefinitions.ComPlusGroupInPartitionRole, null, null)
        {
        }

        public ComPlusGroupInPartitionRoleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusGroupInPartitionRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusGroupInPartitionRoleSymbolFields index] => this.Fields[(int)index];

        public string PartitionRoleRef
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleSymbolFields.PartitionRoleRef].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleSymbolFields.PartitionRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleSymbolFields.ComponentRef, value);
        }

        public string GroupRef
        {
            get => this.Fields[(int)ComPlusGroupInPartitionRoleSymbolFields.GroupRef].AsString();
            set => this.Set((int)ComPlusGroupInPartitionRoleSymbolFields.GroupRef, value);
        }
    }
}