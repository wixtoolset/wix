// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixApprovedExeForElevation = new IntermediateTupleDefinition(
            TupleDefinitionType.WixApprovedExeForElevation,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationTupleFields.ValueName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixApprovedExeForElevationTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixApprovedExeForElevationTupleFields
    {
        Key,
        ValueName,
        Attributes,
    }

    [Flags]
    public enum WixApprovedExeForElevationAttributes
    {
        None = 0x0,
        Win64 = 0x1,
    }

    public class WixApprovedExeForElevationTuple : IntermediateTuple
    {
        public WixApprovedExeForElevationTuple() : base(TupleDefinitions.WixApprovedExeForElevation, null, null)
        {
        }

        public WixApprovedExeForElevationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixApprovedExeForElevation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixApprovedExeForElevationTupleFields index] => this.Fields[(int)index];

        public string Key
        {
            get => (string)this.Fields[(int)WixApprovedExeForElevationTupleFields.Key];
            set => this.Set((int)WixApprovedExeForElevationTupleFields.Key, value);
        }

        public string ValueName
        {
            get => (string)this.Fields[(int)WixApprovedExeForElevationTupleFields.ValueName];
            set => this.Set((int)WixApprovedExeForElevationTupleFields.ValueName, value);
        }

        public WixApprovedExeForElevationAttributes Attributes
        {
            get => (WixApprovedExeForElevationAttributes)this.Fields[(int)WixApprovedExeForElevationTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixApprovedExeForElevationTupleFields.Attributes, (int)value);
        }

        public bool Win64 => (this.Attributes & WixApprovedExeForElevationAttributes.Win64) == WixApprovedExeForElevationAttributes.Win64;
    }
}
