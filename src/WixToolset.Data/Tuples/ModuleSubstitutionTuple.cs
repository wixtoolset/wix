// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ModuleSubstitution = new IntermediateTupleDefinition(
            TupleDefinitionType.ModuleSubstitution,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ModuleSubstitutionTupleFields.Table), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleSubstitutionTupleFields.Row), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleSubstitutionTupleFields.Column), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ModuleSubstitutionTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ModuleSubstitutionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ModuleSubstitutionTupleFields
    {
        Table,
        Row,
        Column,
        Value,
    }

    public class ModuleSubstitutionTuple : IntermediateTuple
    {
        public ModuleSubstitutionTuple() : base(TupleDefinitions.ModuleSubstitution, null, null)
        {
        }

        public ModuleSubstitutionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ModuleSubstitution, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ModuleSubstitutionTupleFields index] => this.Fields[(int)index];

        public string Table
        {
            get => (string)this.Fields[(int)ModuleSubstitutionTupleFields.Table];
            set => this.Set((int)ModuleSubstitutionTupleFields.Table, value);
        }

        public string Row
        {
            get => (string)this.Fields[(int)ModuleSubstitutionTupleFields.Row];
            set => this.Set((int)ModuleSubstitutionTupleFields.Row, value);
        }

        public string Column
        {
            get => (string)this.Fields[(int)ModuleSubstitutionTupleFields.Column];
            set => this.Set((int)ModuleSubstitutionTupleFields.Column, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)ModuleSubstitutionTupleFields.Value];
            set => this.Set((int)ModuleSubstitutionTupleFields.Value, value);
        }
    }
}