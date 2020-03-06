// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Perfmon = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.Perfmon.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(PerfmonTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerfmonTupleFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerfmonTupleFields.Name), IntermediateFieldType.String),
            },
            typeof(PerfmonTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum PerfmonTupleFields
    {
        ComponentRef,
        File,
        Name,
    }

    public class PerfmonTuple : IntermediateTuple
    {
        public PerfmonTuple() : base(UtilTupleDefinitions.Perfmon, null, null)
        {
        }

        public PerfmonTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.Perfmon, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PerfmonTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)PerfmonTupleFields.ComponentRef].AsString();
            set => this.Set((int)PerfmonTupleFields.ComponentRef, value);
        }

        public string File
        {
            get => this.Fields[(int)PerfmonTupleFields.File].AsString();
            set => this.Set((int)PerfmonTupleFields.File, value);
        }

        public string Name
        {
            get => this.Fields[(int)PerfmonTupleFields.Name].AsString();
            set => this.Set((int)PerfmonTupleFields.Name, value);
        }
    }
}