// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBuildInfo = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBuildInfo,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBuildInfoTupleFields.WixVersion), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBuildInfoTupleFields.WixOutputFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBuildInfoTupleFields.WixProjectFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBuildInfoTupleFields.WixPdbFile), IntermediateFieldType.String),
            },
            typeof(WixBuildInfoTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBuildInfoTupleFields
    {
        WixVersion,
        WixOutputFile,
        WixProjectFile,
        WixPdbFile,
    }

    public class WixBuildInfoTuple : IntermediateTuple
    {
        public WixBuildInfoTuple() : base(TupleDefinitions.WixBuildInfo, null, null)
        {
        }

        public WixBuildInfoTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBuildInfo, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBuildInfoTupleFields index] => this.Fields[(int)index];

        public string WixVersion
        {
            get => (string)this.Fields[(int)WixBuildInfoTupleFields.WixVersion];
            set => this.Set((int)WixBuildInfoTupleFields.WixVersion, value);
        }

        public string WixOutputFile
        {
            get => (string)this.Fields[(int)WixBuildInfoTupleFields.WixOutputFile];
            set => this.Set((int)WixBuildInfoTupleFields.WixOutputFile, value);
        }

        public string WixProjectFile
        {
            get => (string)this.Fields[(int)WixBuildInfoTupleFields.WixProjectFile];
            set => this.Set((int)WixBuildInfoTupleFields.WixProjectFile, value);
        }

        public string WixPdbFile
        {
            get => (string)this.Fields[(int)WixBuildInfoTupleFields.WixPdbFile];
            set => this.Set((int)WixBuildInfoTupleFields.WixPdbFile, value);
        }
    }
}