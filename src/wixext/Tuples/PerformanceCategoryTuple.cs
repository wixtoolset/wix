// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition PerformanceCategory = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.PerformanceCategory.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(PerformanceCategorySymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerformanceCategorySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerformanceCategorySymbolFields.IniData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerformanceCategorySymbolFields.ConstantData), IntermediateFieldType.String),
            },
            typeof(PerformanceCategorySymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum PerformanceCategorySymbolFields
    {
        ComponentRef,
        Name,
        IniData,
        ConstantData,
    }

    public class PerformanceCategorySymbol : IntermediateSymbol
    {
        public PerformanceCategorySymbol() : base(UtilSymbolDefinitions.PerformanceCategory, null, null)
        {
        }

        public PerformanceCategorySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.PerformanceCategory, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PerformanceCategorySymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)PerformanceCategorySymbolFields.ComponentRef].AsString();
            set => this.Set((int)PerformanceCategorySymbolFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)PerformanceCategorySymbolFields.Name].AsString();
            set => this.Set((int)PerformanceCategorySymbolFields.Name, value);
        }

        public string IniData
        {
            get => this.Fields[(int)PerformanceCategorySymbolFields.IniData].AsString();
            set => this.Set((int)PerformanceCategorySymbolFields.IniData, value);
        }

        public string ConstantData
        {
            get => this.Fields[(int)PerformanceCategorySymbolFields.ConstantData].AsString();
            set => this.Set((int)PerformanceCategorySymbolFields.ConstantData, value);
        }
    }
}