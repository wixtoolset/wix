// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusUserInPartitionRole = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusUserInPartitionRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleSymbolFields.PartitionRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInPartitionRoleSymbolFields.UserRef), IntermediateFieldType.String),
            },
            typeof(ComPlusUserInPartitionRoleSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusUserInPartitionRoleSymbolFields
    {
        PartitionRoleRef,
        ComponentRef,
        UserRef,
    }

    public class ComPlusUserInPartitionRoleSymbol : IntermediateSymbol
    {
        public ComPlusUserInPartitionRoleSymbol() : base(ComPlusSymbolDefinitions.ComPlusUserInPartitionRole, null, null)
        {
        }

        public ComPlusUserInPartitionRoleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusUserInPartitionRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusUserInPartitionRoleSymbolFields index] => this.Fields[(int)index];

        public string PartitionRoleRef
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleSymbolFields.PartitionRoleRef].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleSymbolFields.PartitionRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleSymbolFields.ComponentRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)ComPlusUserInPartitionRoleSymbolFields.UserRef].AsString();
            set => this.Set((int)ComPlusUserInPartitionRoleSymbolFields.UserRef, value);
        }
    }
}