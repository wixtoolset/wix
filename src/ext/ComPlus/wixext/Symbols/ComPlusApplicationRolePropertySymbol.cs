// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusApplicationRoleProperty = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusApplicationRoleProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRolePropertySymbolFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRolePropertySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusApplicationRolePropertySymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusApplicationRolePropertySymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusApplicationRolePropertySymbolFields
    {
        ApplicationRoleRef,
        Name,
        Value,
    }

    public class ComPlusApplicationRolePropertySymbol : IntermediateSymbol
    {
        public ComPlusApplicationRolePropertySymbol() : base(ComPlusSymbolDefinitions.ComPlusApplicationRoleProperty, null, null)
        {
        }

        public ComPlusApplicationRolePropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusApplicationRoleProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusApplicationRolePropertySymbolFields index] => this.Fields[(int)index];

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusApplicationRolePropertySymbolFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusApplicationRolePropertySymbolFields.ApplicationRoleRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusApplicationRolePropertySymbolFields.Name].AsString();
            set => this.Set((int)ComPlusApplicationRolePropertySymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusApplicationRolePropertySymbolFields.Value].AsString();
            set => this.Set((int)ComPlusApplicationRolePropertySymbolFields.Value, value);
        }
    }
}