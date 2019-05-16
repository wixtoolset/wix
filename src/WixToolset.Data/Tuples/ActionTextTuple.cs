// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ActionText = new IntermediateTupleDefinition(
            TupleDefinitionType.ActionText,
            new[]
            {
                new IntermediateFieldDefinition(nameof(ActionTextTupleFields.Action), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ActionTextTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ActionTextTupleFields.Template), IntermediateFieldType.String),
            },
            typeof(ActionTextTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum ActionTextTupleFields
    {
        Action,
        Description,
        Template,
    }

    public class ActionTextTuple : IntermediateTuple
    {
        public ActionTextTuple() : base(TupleDefinitions.ActionText, null, null)
        {
        }

        public ActionTextTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.ActionText, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ActionTextTupleFields index] => this.Fields[(int)index];

        public string Action
        {
            get => (string)this.Fields[(int)ActionTextTupleFields.Action];
            set => this.Set((int)ActionTextTupleFields.Action, value);
        }

        public string Description
        {
            get => (string)this.Fields[(int)ActionTextTupleFields.Description];
            set => this.Set((int)ActionTextTupleFields.Description, value);
        }

        public string Template
        {
            get => (string)this.Fields[(int)ActionTextTupleFields.Template];
            set => this.Set((int)ActionTextTupleFields.Template, value);
        }
    }
}