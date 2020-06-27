// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusRoleForInterface = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusRoleForInterface.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceSymbolFields.InterfaceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceSymbolFields.ApplicationRoleRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusRoleForInterfaceSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(ComPlusRoleForInterfaceSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusRoleForInterfaceSymbolFields
    {
        InterfaceRef,
        ApplicationRoleRef,
        ComponentRef,
    }

    public class ComPlusRoleForInterfaceSymbol : IntermediateSymbol
    {
        public ComPlusRoleForInterfaceSymbol() : base(ComPlusSymbolDefinitions.ComPlusRoleForInterface, null, null)
        {
        }

        public ComPlusRoleForInterfaceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusRoleForInterface, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusRoleForInterfaceSymbolFields index] => this.Fields[(int)index];

        public string InterfaceRef
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceSymbolFields.InterfaceRef].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceSymbolFields.InterfaceRef, value);
        }

        public string ApplicationRoleRef
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceSymbolFields.ApplicationRoleRef].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceSymbolFields.ApplicationRoleRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusRoleForInterfaceSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusRoleForInterfaceSymbolFields.ComponentRef, value);
        }
    }
}