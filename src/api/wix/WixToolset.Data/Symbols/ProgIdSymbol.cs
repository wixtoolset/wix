// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ProgId = new IntermediateSymbolDefinition(
            SymbolDefinitionType.ProgId,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ProgIdSymbolFields.ProgId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdSymbolFields.ParentProgIdRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdSymbolFields.ClassRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdSymbolFields.IconRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ProgIdSymbolFields.IconIndex), IntermediateFieldType.Number),
            },
            typeof(ProgIdSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum ProgIdSymbolFields
    {
        ProgId,
        ParentProgIdRef,
        ClassRef,
        Description,
        IconRef,
        IconIndex,
    }

    public class ProgIdSymbol : IntermediateSymbol
    {
        public ProgIdSymbol() : base(SymbolDefinitions.ProgId, null, null)
        {
        }

        public ProgIdSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.ProgId, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ProgIdSymbolFields index] => this.Fields[(int)index];

        public string ProgId
        {
            get => (string)this.Fields[(int)ProgIdSymbolFields.ProgId];
            set => this.Set((int)ProgIdSymbolFields.ProgId, value);
        }

        public string ParentProgIdRef
        {
            get => (string)this.Fields[(int)ProgIdSymbolFields.ParentProgIdRef];
            set => this.Set((int)ProgIdSymbolFields.ParentProgIdRef, value);
        }

        public string ClassRef
        {
            get => (string)this.Fields[(int)ProgIdSymbolFields.ClassRef];
            set => this.Set((int)ProgIdSymbolFields.ClassRef, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ProgIdSymbolFields.Description];
            set => this.Set((int)ProgIdSymbolFields.Description, value);
        }

        public string IconRef
        {
            get => (string)this.Fields[(int)ProgIdSymbolFields.IconRef];
            set => this.Set((int)ProgIdSymbolFields.IconRef, value);
        }

        public int? IconIndex
        {
            get => (int?)this.Fields[(int)ProgIdSymbolFields.IconIndex];
            set => this.Set((int)ProgIdSymbolFields.IconIndex, value);
        }
    }
}