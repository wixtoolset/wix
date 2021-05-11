// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Symbols;

    public static partial class IisSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition IIsFilter = new IntermediateSymbolDefinition(
            IisSymbolDefinitionType.IIsFilter.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsFilterSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterSymbolFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterSymbolFields.WebRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsFilterSymbolFields.Flags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IIsFilterSymbolFields.LoadOrder), IntermediateFieldType.Number),
            },
            typeof(IIsFilterSymbol));
    }
}

namespace WixToolset.Iis.Symbols
{
    using WixToolset.Data;

    public enum IIsFilterSymbolFields
    {
        Name,
        ComponentRef,
        Path,
        WebRef,
        Description,
        Flags,
        LoadOrder,
    }

    public class IIsFilterSymbol : IntermediateSymbol
    {
        public IIsFilterSymbol() : base(IisSymbolDefinitions.IIsFilter, null, null)
        {
        }

        public IIsFilterSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisSymbolDefinitions.IIsFilter, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsFilterSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)IIsFilterSymbolFields.Name].AsString();
            set => this.Set((int)IIsFilterSymbolFields.Name, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)IIsFilterSymbolFields.ComponentRef].AsString();
            set => this.Set((int)IIsFilterSymbolFields.ComponentRef, value);
        }

        public string Path
        {
            get => this.Fields[(int)IIsFilterSymbolFields.Path].AsString();
            set => this.Set((int)IIsFilterSymbolFields.Path, value);
        }

        public string WebRef
        {
            get => this.Fields[(int)IIsFilterSymbolFields.WebRef].AsString();
            set => this.Set((int)IIsFilterSymbolFields.WebRef, value);
        }

        public string Description
        {
            get => this.Fields[(int)IIsFilterSymbolFields.Description].AsString();
            set => this.Set((int)IIsFilterSymbolFields.Description, value);
        }

        public int Flags
        {
            get => this.Fields[(int)IIsFilterSymbolFields.Flags].AsNumber();
            set => this.Set((int)IIsFilterSymbolFields.Flags, value);
        }

        public int? LoadOrder
        {
            get => this.Fields[(int)IIsFilterSymbolFields.LoadOrder].AsNullableNumber();
            set => this.Set((int)IIsFilterSymbolFields.LoadOrder, value);
        }
    }
}