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
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.ExecutionType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Source), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.SourceType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.TargetType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Async), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Hidden), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.IgnoreResult), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Impersonate), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.PatchUninstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.TSAware), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionTupleFields.Win64), IntermediateFieldType.Bool),
            },
            typeof(CustomActionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum CustomActionTupleFields
    {
        ExecutionType,
        Source,
        SourceType,
        Target,
        TargetType,
        Async,
        Hidden,
        IgnoreResult,
        Impersonate,
        PatchUninstall,
        TSAware,
        Win64,
    }

    public enum CustomActionExecutionType
    {
        Immediate,
        FirstSequence = 256,
        OncePerProcess = 512,
        ClientRepeat = 768,
        Deferred = 1024,
        Rollback = 1280,
        Commit = 1536,
    }

    public enum CustomActionSourceType
    {
        Binary,
        File = 0x10,
        Directory = 0x20,
        Property = 0x30,
    }

    public enum CustomActionTargetType
    {
        Dll = 1,
        Exe = 2,
        TextData = 3,
        JScript = 5,
        VBScript = 6,
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

        public CustomActionExecutionType ExecutionType
        {
            get => (CustomActionExecutionType)this.Fields[(int)CustomActionTupleFields.ExecutionType].AsNumber();
            set => this.Set((int)CustomActionTupleFields.ExecutionType, (int)value);
        }

        public string Source
        {
            get => (string)this.Fields[(int)CustomActionTupleFields.Source]?.Value;
            set => this.Set((int)CustomActionTupleFields.Source, value);
        }

        public CustomActionSourceType SourceType
        {
            get => (CustomActionSourceType)this.Fields[(int)CustomActionTupleFields.SourceType].AsNumber();
            set => this.Set((int)CustomActionTupleFields.SourceType, (int)value);
        }

        public string Target
        {
            get => (string)this.Fields[(int)CustomActionTupleFields.Target]?.Value;
            set => this.Set((int)CustomActionTupleFields.Target, value);
        }

        public CustomActionTargetType TargetType
        {
            get => (CustomActionTargetType)this.Fields[(int)CustomActionTupleFields.TargetType].AsNumber();
            set => this.Set((int)CustomActionTupleFields.TargetType, (int)value);
        }

        public bool Async
        {
            get => this.Fields[(int)CustomActionTupleFields.Async].AsBool();
            set => this.Set((int)CustomActionTupleFields.Async, value);
        }

        public bool Hidden
        {
            get => this.Fields[(int)CustomActionTupleFields.Hidden].AsBool();
            set => this.Set((int)CustomActionTupleFields.Hidden, value);
        }

        public bool IgnoreResult
        {
            get => this.Fields[(int)CustomActionTupleFields.IgnoreResult].AsBool();
            set => this.Set((int)CustomActionTupleFields.IgnoreResult, value);
        }

        public bool Impersonate
        {
            get => this.Fields[(int)CustomActionTupleFields.Impersonate].AsBool();
            set => this.Set((int)CustomActionTupleFields.Impersonate, value);
        }

        public bool PatchUninstall
        {
            get => this.Fields[(int)CustomActionTupleFields.PatchUninstall].AsBool();
            set => this.Set((int)CustomActionTupleFields.PatchUninstall, value);
        }

        public bool TSAware
        {
            get => this.Fields[(int)CustomActionTupleFields.TSAware].AsBool();
            set => this.Set((int)CustomActionTupleFields.TSAware, value);
        }

        public bool Win64
        {
            get => this.Fields[(int)CustomActionTupleFields.Win64].AsBool();
            set => this.Set((int)CustomActionTupleFields.Win64, value);
        }
    }
}