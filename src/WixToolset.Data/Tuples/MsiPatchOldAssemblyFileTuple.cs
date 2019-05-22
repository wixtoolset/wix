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
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyFileTupleFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyFileTupleFields.AssemblyRef), IntermediateFieldType.String),
            },
            typeof(MsiPatchOldAssemblyFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiPatchOldAssemblyFileTupleFields
    {
        FileRef,
        AssemblyRef,
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

        public string FileRef
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyFileTupleFields.FileRef];
            set => this.Set((int)MsiPatchOldAssemblyFileTupleFields.FileRef, value);
        }

        public string AssemblyRef
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyFileTupleFields.AssemblyRef];
            set => this.Set((int)MsiPatchOldAssemblyFileTupleFields.AssemblyRef, value);
        }
    }
}