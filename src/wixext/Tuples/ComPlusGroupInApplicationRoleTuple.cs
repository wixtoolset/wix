// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusGroupInApplicationRole = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusGroupInApplicationRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleSymbolFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusGroupInApplicationRoleSymbolFields.GroupRef), IntermediateFieldType.String),
            },
            typeof(ComPlusGroupInApplicationRoleSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusGroupInApplicationRoleSymbolFields
    {
        ApplicationRoleRef,
        ComponentRef,
        GroupRef,
    }

    public class ComPlusGroupInApplicationRoleSymbol : IntermediateSymbol
    {
        public ComPlusGroupInApplicationRoleSymbol() : base(ComPlusSymbolDefinitions.ComPlusGroupInApplicationRole, null, null)
        {
        }

        public ComPlusGroupInApplicationRoleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusGroupInApplicationRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusGroupInApplicationRoleSymbolFields index] => this.Fields[(int)index];

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleSymbolFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleSymbolFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleSymbolFields.ComponentRef, value);
        }

        public string GroupRef
        {
            get => this.Fields[(int)ComPlusGroupInApplicationRoleSymbolFields.GroupRef].AsString();
            set => this.Set((int)ComPlusGroupInApplicationRoleSymbolFields.GroupRef, value);
        }
    }
}