// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition TargetImages = new IntermediateTupleDefinition(
            TupleDefinitionType.TargetImages,
            new[]
            {
                new IntermediateFieldDefinition(nameof(TargetImagesTupleFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesTupleFields.MsiPath), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesTupleFields.SymbolPaths), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesTupleFields.Upgraded), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesTupleFields.Order), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(TargetImagesTupleFields.ProductValidateFlags), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(TargetImagesTupleFields.IgnoreMissingSrcFiles), IntermediateFieldType.Number),
            },
            typeof(TargetImagesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum TargetImagesTupleFields
    {
        Target,
        MsiPath,
        SymbolPaths,
        Upgraded,
        Order,
        ProductValidateFlags,
        IgnoreMissingSrcFiles,
    }

    public class TargetImagesTuple : IntermediateTuple
    {
        public TargetImagesTuple() : base(TupleDefinitions.TargetImages, null, null)
        {
        }

        public TargetImagesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.TargetImages, sourceLineNumber, id)
        {
        }

        public IntermediateField this[TargetImagesTupleFields index] => this.Fields[(int)index];

        public string Target
        {
            get => (string)this.Fields[(int)TargetImagesTupleFields.Target]?.Value;
            set => this.Set((int)TargetImagesTupleFields.Target, value);
        }

        public string MsiPath
        {
            get => (string)this.Fields[(int)TargetImagesTupleFields.MsiPath]?.Value;
            set => this.Set((int)TargetImagesTupleFields.MsiPath, value);
        }

        public string SymbolPaths
        {
            get => (string)this.Fields[(int)TargetImagesTupleFields.SymbolPaths]?.Value;
            set => this.Set((int)TargetImagesTupleFields.SymbolPaths, value);
        }

        public string Upgraded
        {
            get => (string)this.Fields[(int)TargetImagesTupleFields.Upgraded]?.Value;
            set => this.Set((int)TargetImagesTupleFields.Upgraded, value);
        }

        public int Order
        {
            get => (int)this.Fields[(int)TargetImagesTupleFields.Order]?.Value;
            set => this.Set((int)TargetImagesTupleFields.Order, value);
        }

        public string ProductValidateFlags
        {
            get => (string)this.Fields[(int)TargetImagesTupleFields.ProductValidateFlags]?.Value;
            set => this.Set((int)TargetImagesTupleFields.ProductValidateFlags, value);
        }

        public int IgnoreMissingSrcFiles
        {
            get => (int)this.Fields[(int)TargetImagesTupleFields.IgnoreMissingSrcFiles]?.Value;
            set => this.Set((int)TargetImagesTupleFields.IgnoreMissingSrcFiles, value);
        }
    }
}