// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IniLocator = new IntermediateTupleDefinition(
            TupleDefinitionType.IniLocator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(IniLocatorTupleFields.Signature_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniLocatorTupleFields.FileName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniLocatorTupleFields.Section), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniLocatorTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IniLocatorTupleFields.Field), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(IniLocatorTupleFields.Type), IntermediateFieldType.Number),
            },
            typeof(IniLocatorTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum IniLocatorTupleFields
    {
        Signature_,
        FileName,
        Section,
        Key,
        Field,
        Type,
    }

    public class IniLocatorTuple : IntermediateTuple
    {
        public IniLocatorTuple() : base(TupleDefinitions.IniLocator, null, null)
        {
        }

        public IniLocatorTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.IniLocator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IniLocatorTupleFields index] => this.Fields[(int)index];

        public string Signature_
        {
            get => (string)this.Fields[(int)IniLocatorTupleFields.Signature_]?.Value;
            set => this.Set((int)IniLocatorTupleFields.Signature_, value);
        }

        public string FileName
        {
            get => (string)this.Fields[(int)IniLocatorTupleFields.FileName]?.Value;
            set => this.Set((int)IniLocatorTupleFields.FileName, value);
        }

        public string Section
        {
            get => (string)this.Fields[(int)IniLocatorTupleFields.Section]?.Value;
            set => this.Set((int)IniLocatorTupleFields.Section, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)IniLocatorTupleFields.Key]?.Value;
            set => this.Set((int)IniLocatorTupleFields.Key, value);
        }

        public int Field
        {
            get => (int)this.Fields[(int)IniLocatorTupleFields.Field]?.Value;
            set => this.Set((int)IniLocatorTupleFields.Field, value);
        }

        public int Type
        {
            get => (int)this.Fields[(int)IniLocatorTupleFields.Type]?.Value;
            set => this.Set((int)IniLocatorTupleFields.Type, value);
        }
    }
}