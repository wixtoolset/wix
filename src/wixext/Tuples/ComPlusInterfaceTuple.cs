// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusInterface = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusInterface.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusInterfaceSymbolFields.ComPlusComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusInterfaceSymbolFields.IID), IntermediateFieldType.String),
            },
            typeof(ComPlusInterfaceSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusInterfaceSymbolFields
    {
        ComPlusComponentRef,
        IID,
    }

    public class ComPlusInterfaceSymbol : IntermediateSymbol
    {
        public ComPlusInterfaceSymbol() : base(ComPlusSymbolDefinitions.ComPlusInterface, null, null)
        {
        }

        public ComPlusInterfaceSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusInterface, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusInterfaceSymbolFields index] => this.Fields[(int)index];

        public string ComPlusComponentRef
        {
            get => this.Fields[(int)ComPlusInterfaceSymbolFields.ComPlusComponentRef].AsString();
            set => this.Set((int)ComPlusInterfaceSymbolFields.ComPlusComponentRef, value);
        }

        public string IID
        {
            get => this.Fields[(int)ComPlusInterfaceSymbolFields.IID].AsString();
            set => this.Set((int)ComPlusInterfaceSymbolFields.IID, value);
        }
    }
}