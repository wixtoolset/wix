// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition PerfmonManifest = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.PerfmonManifest.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(PerfmonManifestTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerfmonManifestTupleFields.File), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PerfmonManifestTupleFields.ResourceFileDirectory), IntermediateFieldType.String),
            },
            typeof(PerfmonManifestTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum PerfmonManifestTupleFields
    {
        ComponentRef,
        File,
        ResourceFileDirectory,
    }

    public class PerfmonManifestTuple : IntermediateTuple
    {
        public PerfmonManifestTuple() : base(UtilTupleDefinitions.PerfmonManifest, null, null)
        {
        }

        public PerfmonManifestTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.PerfmonManifest, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PerfmonManifestTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)PerfmonManifestTupleFields.ComponentRef].AsString();
            set => this.Set((int)PerfmonManifestTupleFields.ComponentRef, value);
        }

        public string File
        {
            get => this.Fields[(int)PerfmonManifestTupleFields.File].AsString();
            set => this.Set((int)PerfmonManifestTupleFields.File, value);
        }

        public string ResourceFileDirectory
        {
            get => this.Fields[(int)PerfmonManifestTupleFields.ResourceFileDirectory].AsString();
            set => this.Set((int)PerfmonManifestTupleFields.ResourceFileDirectory, value);
        }
    }
}