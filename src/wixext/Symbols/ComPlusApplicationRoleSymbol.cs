// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusApplicationRole = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusApplicationRole.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRoleSymbolFields.ApplicationRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRoleSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRoleSymbolFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusApplicationRoleSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusApplicationRoleSymbolFields
    {
        ApplicationRef,
        ComponentRef,
        Name,
    }

    public class ComPlusApplicationRoleSymbol : IntermediateSymbol
    {
        public ComPlusApplicationRoleSymbol() : base(ComPlusSymbolDefinitions.ComPlusApplicationRole, null, null)
        {
        }

        public ComPlusApplicationRoleSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusApplicationRole, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusApplicationRoleSymbolFields index] => this.Fields[(int)index];

        public string ApplicationRef
        {
            get => this.Fields[(int)ComPlusApplicationRoleSymbolFields.ApplicationRef].AsString();
            set => this.Set((int)ComPlusApplicationRoleSymbolFields.ApplicationRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusApplicationRoleSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusApplicationRoleSymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationRoleSymbolFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationRoleSymbolFields.Name, value);
        }
    }
}