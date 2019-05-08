// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IniFile = new IntermediateTupleDefinition(
            TupleDefinitionType.IniFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(IniFileTupleFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileTupleFields.DirProperty), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileTupleFields.Section), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileTupleFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniFileTupleFields.Action), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IniFileTupleFields.Component_), IntermediateFieldType.String),
            },
            typeof(IniFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum IniFileTupleFields
    {
        FileName,
        DirProperty,
        Section,
        Key,
        Value,
        Action,
        Component_,
    }

    public class IniFileTuple : IntermediateTuple
    {
        public IniFileTuple() : base(TupleDefinitions.IniFile, null, null)
        {
        }

        public IniFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.IniFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IniFileTupleFields index] => this.Fields[(int)index];

        public string FileName
        {
            get => (string)this.Fields[(int)IniFileTupleFields.FileName]?.Value;
            set => this.Set((int)IniFileTupleFields.FileName, value);
        }

        public string DirProperty
        {
            get => (string)this.Fields[(int)IniFileTupleFields.DirProperty]?.Value;
            set => this.Set((int)IniFileTupleFields.DirProperty, value);
        }

        public string Section
        {
            get => (string)this.Fields[(int)IniFileTupleFields.Section]?.Value;
            set => this.Set((int)IniFileTupleFields.Section, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)IniFileTupleFields.Key]?.Value;
            set => this.Set((int)IniFileTupleFields.Key, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)IniFileTupleFields.Value]?.Value;
            set => this.Set((int)IniFileTupleFields.Value, value);
        }

        public InifFileActionType Action
        {
            get => (InifFileActionType)this.Fields[(int)IniFileTupleFields.Action]?.AsNumber();
            set => this.Set((int)IniFileTupleFields.Action, (int)value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)IniFileTupleFields.Component_]?.Value;
            set => this.Set((int)IniFileTupleFields.Component_, value);
        }
    }
}