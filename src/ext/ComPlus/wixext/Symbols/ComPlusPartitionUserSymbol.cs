// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusPartitionUser = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusPartitionUser.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserSymbolFields.PartitionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusPartitionUserSymbolFields.UserRef), IntermediateFieldType.String),
            },
            typeof(ComPlusPartitionUserSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusPartitionUserSymbolFields
    {
        PartitionRef,
        ComponentRef,
        UserRef,
    }

    public class ComPlusPartitionUserSymbol : IntermediateSymbol
    {
        public ComPlusPartitionUserSymbol() : base(ComPlusSymbolDefinitions.ComPlusPartitionUser, null, null)
        {
        }

        public ComPlusPartitionUserSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusPartitionUser, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusPartitionUserSymbolFields index] => this.Fields[(int)index];

        public string PartitionRef
        {
            get => this.Fields[(int)ComPlusPartitionUserSymbolFields.PartitionRef].AsString();
            set => this.Set((int)ComPlusPartitionUserSymbolFields.PartitionRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusPartitionUserSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusPartitionUserSymbolFields.ComponentRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)ComPlusPartitionUserSymbolFields.UserRef].AsString();
            set => this.Set((int)ComPlusPartitionUserSymbolFields.UserRef, value);
        }
    }
}