// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition CreateFolder = new IntermediateTupleDefinition(
            TupleDefinitionType.CreateFolder,
            new[]
            {
                new IntermediateFieldDefinition(nameof(CreateFolderTupleFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CreateFolderTupleFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(CreateFolderTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum CreateFolderTupleFields
    {
        DirectoryRef,
        ComponentRef,
    }

    public class CreateFolderTuple : IntermediateTuple
    {
        public CreateFolderTuple() : base(TupleDefinitions.CreateFolder, null, null)
        {
        }

        public CreateFolderTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.CreateFolder, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CreateFolderTupleFields index] => this.Fields[(int)index];

        public string DirectoryRef
        {
            get => (string)this.Fields[(int)CreateFolderTupleFields.DirectoryRef];
            set => this.Set((int)CreateFolderTupleFields.DirectoryRef, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)CreateFolderTupleFields.ComponentRef];
            set => this.Set((int)CreateFolderTupleFields.ComponentRef, value);
        }
    }
}