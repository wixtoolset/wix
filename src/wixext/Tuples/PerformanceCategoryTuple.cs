// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition PerformanceCategory = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.PerformanceCategory.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(PerformanceCategoryTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerformanceCategoryTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerformanceCategoryTupleFields.IniData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerformanceCategoryTupleFields.ConstantData), IntermediateFieldType.String),
            },
            typeof(PerformanceCategoryTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum PerformanceCategoryTupleFields
    {
        ComponentRef,
        Name,
        IniData,
        ConstantData,
    }

    public class PerformanceCategoryTuple : IntermediateTuple
    {
        public PerformanceCategoryTuple() : base(UtilTupleDefinitions.PerformanceCategory, null, null)
        {
        }

        public PerformanceCategoryTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.PerformanceCategory, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PerformanceCategoryTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)PerformanceCategoryTupleFields.ComponentRef].AsString();
            set => this.Set((int)PerformanceCategoryTupleFields.ComponentRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)PerformanceCategoryTupleFields.Name].AsString();
            set => this.Set((int)PerformanceCategoryTupleFields.Name, value);
        }

        public string IniData
        {
            get => this.Fields[(int)PerformanceCategoryTupleFields.IniData].AsString();
            set => this.Set((int)PerformanceCategoryTupleFields.IniData, value);
        }

        public string ConstantData
        {
            get => this.Fields[(int)PerformanceCategoryTupleFields.ConstantData].AsString();
            set => this.Set((int)PerformanceCategoryTupleFields.ConstantData, value);
        }
    }
}