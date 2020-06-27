// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusMethod = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusMethod.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusMethodSymbolFields.InterfaceRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusMethodSymbolFields.Index), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(ComPlusMethodSymbolFields.Name), IntermediateFieldType.String),
            },
            typeof(ComPlusMethodSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusMethodSymbolFields
    {
        InterfaceRef,
        Index,
        Name,
    }

    public class ComPlusMethodSymbol : IntermediateSymbol
    {
        public ComPlusMethodSymbol() : base(ComPlusSymbolDefinitions.ComPlusMethod, null, null)
        {
        }

        public ComPlusMethodSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusMethod, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusMethodSymbolFields index] => this.Fields[(int)index];

        public string InterfaceRef
        {
            get => this.Fields[(int)ComPlusMethodSymbolFields.InterfaceRef].AsString();
            set => this.Set((int)ComPlusMethodSymbolFields.InterfaceRef, value);
        }

        public int? Index
        {
            get => this.Fields[(int)ComPlusMethodSymbolFields.Index].AsNullableNumber();
            set => this.Set((int)ComPlusMethodSymbolFields.Index, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusMethodSymbolFields.Name].AsString();
            set => this.Set((int)ComPlusMethodSymbolFields.Name, value);
        }
    }
}