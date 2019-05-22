// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBindUpdatedFiles = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBindUpdatedFiles,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBindUpdatedFilesTupleFields.FileRef), IntermediateFieldType.String),
            },
            typeof(WixBindUpdatedFilesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBindUpdatedFilesTupleFields
    {
        FileRef,
    }

    public class WixBindUpdatedFilesTuple : IntermediateTuple
    {
        public WixBindUpdatedFilesTuple() : base(TupleDefinitions.WixBindUpdatedFiles, null, null)
        {
        }

        public WixBindUpdatedFilesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBindUpdatedFiles, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBindUpdatedFilesTupleFields index] => this.Fields[(int)index];

        public string FileRef
        {
            get => (string)this.Fields[(int)WixBindUpdatedFilesTupleFields.FileRef];
            set => this.Set((int)WixBindUpdatedFilesTupleFields.FileRef, value);
        }
    }
}