// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusUserInApplicationRole = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusUserInApplicationRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleSymbolFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusUserInApplicationRoleSymbolFields.UserRef), IntermediateFieldType.String),
            },
            typeof(ComPlusUserInApplicationRoleSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusUserInApplicationRoleSymbolFields
    {
        ApplicationRoleRef,
        ComponentRef,
        UserRef,
    }

    public class ComPlusUserInApplicationRoleSymbol : IntermediateSymbol
    {
        public ComPlusUserInApplicationRoleSymbol() : base(ComPlusSymbolDefinitions.ComPlusUserInApplicationRole, null, null)
        {
        }

        public ComPlusUserInApplicationRoleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusUserInApplicationRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusUserInApplicationRoleSymbolFields index] => this.Fields[(int)index];

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleSymbolFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleSymbolFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleSymbolFields.ComponentRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)ComPlusUserInApplicationRoleSymbolFields.UserRef].AsString();
            set => this.Set((int)ComPlusUserInApplicationRoleSymbolFields.UserRef, value);
        }
    }
}