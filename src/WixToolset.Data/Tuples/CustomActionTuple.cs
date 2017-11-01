// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition CustomAction = new IntermediateTupleDefinition(
            TupleDefinitionType.CustomAction,
            new[]
            {
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Type), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Source), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.ExtendedType), IntermediateFieldType.Number),
            },
            typeof(CustomActionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum CustomActionTupleFields
    {
        Action,
        Type,
        Source,
        Target,
        ExtendedType,
    }

    public class CustomActionTuple : IntermediateTuple
    {
        public CustomActionTuple() : base(TupleDefinitions.CustomAction, null, null)
        {
        }

        public CustomActionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.CustomAction, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CustomActionTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)CustomActionTupleFields.Action]?.Value;
            set => this.Set((int)CustomActionTupleFields.Action, value);
        }

        public int Type
        {
            get => (int)this.Fields[(int)CustomActionTupleFields.Type]?.Value;
            set => this.Set((int)CustomActionTupleFields.Type, value);
        }

        public string Source
        {
            get => (string)this.Fields[(int)CustomActionTupleFields.Source]?.Value;
            set => this.Set((int)CustomActionTupleFields.Source, value);
        }

        public string Target
        {
            get => (string)this.Fields[(int)CustomActionTupleFields.Target]?.Value;
            set => this.Set((int)CustomActionTupleFields.Target, value);
        }

        public int ExtendedType
        {
            get => (int)this.Fields[(int)CustomActionTupleFields.ExtendedType]?.Value;
            set => this.Set((int)CustomActionTupleFields.ExtendedType, value);
        }
    }
}