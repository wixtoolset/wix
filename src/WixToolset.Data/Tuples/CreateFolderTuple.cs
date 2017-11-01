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
                new IntermediateFieldDefinition(nameof(CreateFolderTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CreateFolderTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(CreateFolderTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum CreateFolderTupleFields
    {
        Directory_,
        Component_,
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

        public string Directory_
        {
            get => (string)this.Fields[(int)CreateFolderTupleFields.Directory_]?.Value;
            set => this.Set((int)CreateFolderTupleFields.Directory_, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)CreateFolderTupleFields.Component_]?.Value;
            set => this.Set((int)CreateFolderTupleFields.Component_, value);
        }
    }
}