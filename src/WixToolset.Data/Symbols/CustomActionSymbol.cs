// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition CustomAction = new IntermediateSymbolDefinition(
            SymbolDefinitionType.CustomAction,
            new[]
            {
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.ExecutionType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.Source), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.SourceType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.TargetType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.Async), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.Hidden), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.IgnoreResult), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.Impersonate), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.PatchUninstall), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.TSAware), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.Win64), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(CustomActionSymbolFields.ScriptFile), IntermediateFieldType.Path),
            },
            typeof(CustomActionSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum CustomActionSymbolFields
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
        ScriptFile
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

    public class CustomActionSymbol : IntermediateSymbol
    {
        public CustomActionSymbol() : base(SymbolDefinitions.CustomAction, null, null)
        {
        }

        public CustomActionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.CustomAction, sourceLineNumber, id)
        {
        }

        public IntermediateField this[CustomActionSymbolFields index] => this.Fields[(int)index];

        public CustomActionExecutionType ExecutionType
        {
            get => (CustomActionExecutionType)this.Fields[(int)CustomActionSymbolFields.ExecutionType].AsNumber();
            set => this.Set((int)CustomActionSymbolFields.ExecutionType, (int)value);
        }

        public string Source
        {
            get => (string)this.Fields[(int)CustomActionSymbolFields.Source];
            set => this.Set((int)CustomActionSymbolFields.Source, value);
        }

        public CustomActionSourceType SourceType
        {
            get => (CustomActionSourceType)this.Fields[(int)CustomActionSymbolFields.SourceType].AsNumber();
            set => this.Set((int)CustomActionSymbolFields.SourceType, (int)value);
        }

        public string Target
        {
            get => (string)this.Fields[(int)CustomActionSymbolFields.Target];
            set => this.Set((int)CustomActionSymbolFields.Target, value);
        }

        public CustomActionTargetType TargetType
        {
            get => (CustomActionTargetType)this.Fields[(int)CustomActionSymbolFields.TargetType].AsNumber();
            set => this.Set((int)CustomActionSymbolFields.TargetType, (int)value);
        }

        public bool Async
        {
            get => this.Fields[(int)CustomActionSymbolFields.Async].AsBool();
            set => this.Set((int)CustomActionSymbolFields.Async, value);
        }

        public bool Hidden
        {
            get => this.Fields[(int)CustomActionSymbolFields.Hidden].AsBool();
            set => this.Set((int)CustomActionSymbolFields.Hidden, value);
        }

        public bool IgnoreResult
        {
            get => this.Fields[(int)CustomActionSymbolFields.IgnoreResult].AsBool();
            set => this.Set((int)CustomActionSymbolFields.IgnoreResult, value);
        }

        public bool Impersonate
        {
            get => this.Fields[(int)CustomActionSymbolFields.Impersonate].AsBool();
            set => this.Set((int)CustomActionSymbolFields.Impersonate, value);
        }

        public bool PatchUninstall
        {
            get => this.Fields[(int)CustomActionSymbolFields.PatchUninstall].AsBool();
            set => this.Set((int)CustomActionSymbolFields.PatchUninstall, value);
        }

        public bool TSAware
        {
            get => this.Fields[(int)CustomActionSymbolFields.TSAware].AsBool();
            set => this.Set((int)CustomActionSymbolFields.TSAware, value);
        }

        public bool Win64
        {
            get => this.Fields[(int)CustomActionSymbolFields.Win64].AsBool();
            set => this.Set((int)CustomActionSymbolFields.Win64, value);
        }

        public IntermediateFieldPathValue ScriptFile
        {
            get => this.Fields[(int)CustomActionSymbolFields.ScriptFile].AsPath();
            set => this.Set((int)CustomActionSymbolFields.ScriptFile, value);
        }
    }
}
