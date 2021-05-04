// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusRoleForMethod = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusRoleForMethod.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodSymbolFields.MethodRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodSymbolFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForMethodSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(ComPlusRoleForMethodSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusRoleForMethodSymbolFields
    {
        MethodRef,
        ApplicationRoleRef,
        ComponentRef,
    }

    public class ComPlusRoleForMethodSymbol : IntermediateSymbol
    {
        public ComPlusRoleForMethodSymbol() : base(ComPlusSymbolDefinitions.ComPlusRoleForMethod, null, null)
        {
        }

        public ComPlusRoleForMethodSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusRoleForMethod, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusRoleForMethodSymbolFields index] => this.Fields[(int)index];

        public string MethodRef
        {
            get => this.Fields[(int)ComPlusRoleForMethodSymbolFields.MethodRef].AsString();
            set => this.Set((int)ComPlusRoleForMethodSymbolFields.MethodRef, value);
        }

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusRoleForMethodSymbolFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusRoleForMethodSymbolFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusRoleForMethodSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusRoleForMethodSymbolFields.ComponentRef, value);
        }
    }
}