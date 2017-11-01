// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixDirectory = new IntermediateTupleDefinition(
            TupleDefinitionType.WixDirectory,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDirectoryTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixDirectoryTupleFields.ComponentGuidGenerationSeed), IntermediateFieldType.String),
            },
            typeof(WixDirectoryTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixDirectoryTupleFields
    {
        Directory_,
        ComponentGuidGenerationSeed,
    }

    public class WixDirectoryTuple : IntermediateTuple
    {
        public WixDirectoryTuple() : base(TupleDefinitions.WixDirectory, null, null)
        {
        }

        public WixDirectoryTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixDirectory, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDirectoryTupleFields index] => this.Fields[(int)index];

        public string Directory_
        {
            get => (string)this.Fields[(int)WixDirectoryTupleFields.Directory_]?.Value;
            set => this.Set((int)WixDirectoryTupleFields.Directory_, value);
        }

        public string ComponentGuidGenerationSeed
        {
            get => (string)this.Fields[(int)WixDirectoryTupleFields.ComponentGuidGenerationSeed]?.Value;
            set => this.Set((int)WixDirectoryTupleFields.ComponentGuidGenerationSeed, value);
        }
    }
}