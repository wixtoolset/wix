// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition RemoveFile = new IntermediateTupleDefinition(
            TupleDefinitionType.RemoveFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.FileKey), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.DirProperty), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.InstallMode), IntermediateFieldType.Number),
            },
            typeof(RemoveFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RemoveFileTupleFields
    {
        FileKey,
        Component_,
        FileName,
        DirProperty,
        InstallMode,
    }

    public class RemoveFileTuple : IntermediateTuple
    {
        public RemoveFileTuple() : base(TupleDefinitions.RemoveFile, null, null)
        {
        }

        public RemoveFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.RemoveFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RemoveFileTupleFields index] => this.Fields[(int)index];

        public string FileKey
        {
            get => (string)this.Fields[(int)RemoveFileTupleFields.FileKey];
            set => this.Set((int)RemoveFileTupleFields.FileKey, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)RemoveFileTupleFields.Component_];
            set => this.Set((int)RemoveFileTupleFields.Component_, value);
        }

        public string FileName
        {
            get => (string)this.Fields[(int)RemoveFileTupleFields.FileName];
            set => this.Set((int)RemoveFileTupleFields.FileName, value);
        }

        public string DirProperty
        {
            get => (string)this.Fields[(int)RemoveFileTupleFields.DirProperty];
            set => this.Set((int)RemoveFileTupleFields.DirProperty, value);
        }

        public int InstallMode
        {
            get => (int)this.Fields[(int)RemoveFileTupleFields.InstallMode];
            set => this.Set((int)RemoveFileTupleFields.InstallMode, value);
        }
    }
}