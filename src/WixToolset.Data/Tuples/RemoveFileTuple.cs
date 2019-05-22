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
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.DirProperty), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.OnInstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(RemoveFileTupleFields.OnUninstall), IntermediateFieldType.Bool),
            },
            typeof(RemoveFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RemoveFileTupleFields
    {
        Component_,
        FileName,
        DirProperty,
        OnInstall,
        OnUninstall,
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

        public bool? OnInstall
        {
            get => (bool?)this.Fields[(int)RemoveFileTupleFields.OnInstall];
            set => this.Set((int)RemoveFileTupleFields.OnInstall, value);
        }

        public bool? OnUninstall
        {
            get => (bool?)this.Fields[(int)RemoveFileTupleFields.OnUninstall];
            set => this.Set((int)RemoveFileTupleFields.OnUninstall, value);
        }
    }
}