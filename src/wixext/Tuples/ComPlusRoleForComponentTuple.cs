// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusRoleForComponent = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusRoleForComponent.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentSymbolFields.ComPlusComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentSymbolFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForComponentSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(ComPlusRoleForComponentSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusRoleForComponentSymbolFields
    {
        ComPlusComponentRef,
        ApplicationRoleRef,
        ComponentRef,
    }

    public class ComPlusRoleForComponentSymbol : IntermediateSymbol
    {
        public ComPlusRoleForComponentSymbol() : base(ComPlusSymbolDefinitions.ComPlusRoleForComponent, null, null)
        {
        }

        public ComPlusRoleForComponentSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusRoleForComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusRoleForComponentSymbolFields index] => this.Fields[(int)index];

        public string ComPlusComponentRef
        {
            get => this.Fields[(int)ComPlusRoleForComponentSymbolFields.ComPlusComponentRef].AsString();
            set => this.Set((int)ComPlusRoleForComponentSymbolFields.ComPlusComponentRef, value);
        }

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusRoleForComponentSymbolFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusRoleForComponentSymbolFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusRoleForComponentSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusRoleForComponentSymbolFields.ComponentRef, value);
        }
    }
}