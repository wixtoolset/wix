// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixCloseApplication = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.WixCloseApplication.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixCloseApplicationTupleFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationTupleFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationTupleFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationTupleFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationTupleFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationTupleFields.TerminateExitCode), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationTupleFields.Timeout), IntermediateFieldType.Number),
            },
            typeof(WixCloseApplicationTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum WixCloseApplicationTupleFields
    {
        Target,
        Description,
        Condition,
        Attributes,
        Sequence,
        Property,
        TerminateExitCode,
        Timeout,
    }

    public class WixCloseApplicationTuple : IntermediateTuple
    {
        public WixCloseApplicationTuple() : base(UtilTupleDefinitions.WixCloseApplication, null, null)
        {
        }

        public WixCloseApplicationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.WixCloseApplication, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixCloseApplicationTupleFields index] => this.Fields[(int)index];

        public string Target
        {
            get => this.Fields[(int)WixCloseApplicationTupleFields.Target].AsString();
            set => this.Set((int)WixCloseApplicationTupleFields.Target, value);
        }

        public string Description
        {
            get => this.Fields[(int)WixCloseApplicationTupleFields.Description].AsString();
            set => this.Set((int)WixCloseApplicationTupleFields.Description, value);
        }

        public string Condition
        {
            get => this.Fields[(int)WixCloseApplicationTupleFields.Condition].AsString();
            set => this.Set((int)WixCloseApplicationTupleFields.Condition, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixCloseApplicationTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixCloseApplicationTupleFields.Attributes, value);
        }

        public int Sequence
        {
            get => this.Fields[(int)WixCloseApplicationTupleFields.Sequence].AsNumber();
            set => this.Set((int)WixCloseApplicationTupleFields.Sequence, value);
        }

        public string Property
        {
            get => this.Fields[(int)WixCloseApplicationTupleFields.Property].AsString();
            set => this.Set((int)WixCloseApplicationTupleFields.Property, value);
        }

        public int TerminateExitCode
        {
            get => this.Fields[(int)WixCloseApplicationTupleFields.TerminateExitCode].AsNumber();
            set => this.Set((int)WixCloseApplicationTupleFields.TerminateExitCode, value);
        }

        public int Timeout
        {
            get => this.Fields[(int)WixCloseApplicationTupleFields.Timeout].AsNumber();
            set => this.Set((int)WixCloseApplicationTupleFields.Timeout, value);
        }
    }
}