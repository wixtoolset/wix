// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition RemoveIniFile = new IntermediateTupleDefinition(
            TupleDefinitionType.RemoveIniFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RemoveIniFileTupleFields.RemoveIniFile), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveIniFileTupleFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveIniFileTupleFields.DirProperty), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveIniFileTupleFields.Section), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveIniFileTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveIniFileTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RemoveIniFileTupleFields.Action), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RemoveIniFileTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(RemoveIniFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RemoveIniFileTupleFields
    {
        RemoveIniFile,
        FileName,
        DirProperty,
        Section,
        Key,
        Value,
        Action,
        Component_,
    }

    public class RemoveIniFileTuple : IntermediateTuple
    {
        public RemoveIniFileTuple() : base(TupleDefinitions.RemoveIniFile, null, null)
        {
        }

        public RemoveIniFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.RemoveIniFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RemoveIniFileTupleFields index] => this.Fields[(int)index];

        public string RemoveIniFile
        {
            get => (string)this.Fields[(int)RemoveIniFileTupleFields.RemoveIniFile];
            set => this.Set((int)RemoveIniFileTupleFields.RemoveIniFile, value);
        }

        public string FileName
        {
            get => (string)this.Fields[(int)RemoveIniFileTupleFields.FileName];
            set => this.Set((int)RemoveIniFileTupleFields.FileName, value);
        }

        public string DirProperty
        {
            get => (string)this.Fields[(int)RemoveIniFileTupleFields.DirProperty];
            set => this.Set((int)RemoveIniFileTupleFields.DirProperty, value);
        }

        public string Section
        {
            get => (string)this.Fields[(int)RemoveIniFileTupleFields.Section];
            set => this.Set((int)RemoveIniFileTupleFields.Section, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)RemoveIniFileTupleFields.Key];
            set => this.Set((int)RemoveIniFileTupleFields.Key, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)RemoveIniFileTupleFields.Value];
            set => this.Set((int)RemoveIniFileTupleFields.Value, value);
        }

        public int Action
        {
            get => (int)this.Fields[(int)RemoveIniFileTupleFields.Action];
            set => this.Set((int)RemoveIniFileTupleFields.Action, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)RemoveIniFileTupleFields.Component_];
            set => this.Set((int)RemoveIniFileTupleFields.Component_, value);
        }
    }
}