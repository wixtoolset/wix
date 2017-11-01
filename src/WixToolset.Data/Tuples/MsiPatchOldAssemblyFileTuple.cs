// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiPatchOldAssemblyFile = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiPatchOldAssemblyFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyFileTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyFileTupleFields.Assembly_), IntermediateFieldType.String),
            },
            typeof(MsiPatchOldAssemblyFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiPatchOldAssemblyFileTupleFields
    {
        File_,
        Assembly_,
    }

    public class MsiPatchOldAssemblyFileTuple : IntermediateTuple
    {
        public MsiPatchOldAssemblyFileTuple() : base(TupleDefinitions.MsiPatchOldAssemblyFile, null, null)
        {
        }

        public MsiPatchOldAssemblyFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiPatchOldAssemblyFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchOldAssemblyFileTupleFields index] => this.Fields[(int)index];

        public string File_
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyFileTupleFields.File_]?.Value;
            set => this.Set((int)MsiPatchOldAssemblyFileTupleFields.File_, value);
        }

        public string Assembly_
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyFileTupleFields.Assembly_]?.Value;
            set => this.Set((int)MsiPatchOldAssemblyFileTupleFields.Assembly_, value);
        }
    }
}