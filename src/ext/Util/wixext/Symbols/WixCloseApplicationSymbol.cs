// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Symbols;

    public static partial class UtilSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixCloseApplication = new IntermediateSymbolDefinition(
            UtilSymbolDefinitionType.WixCloseApplication.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixCloseApplicationSymbolFields.Target), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationSymbolFields.Description), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationSymbolFields.Condition), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationSymbolFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationSymbolFields.Sequence), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationSymbolFields.Property), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationSymbolFields.TerminateExitCode), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixCloseApplicationSymbolFields.Timeout), IntermediateFieldType.Number),
            },
            typeof(WixCloseApplicationSymbol));
    }
}

namespace WixToolset.Util.Symbols
{
    using WixToolset.Data;

    public enum WixCloseApplicationSymbolFields
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

    public class WixCloseApplicationSymbol : IntermediateSymbol
    {
        public WixCloseApplicationSymbol() : base(UtilSymbolDefinitions.WixCloseApplication, null, null)
        {
        }

        public WixCloseApplicationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilSymbolDefinitions.WixCloseApplication, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixCloseApplicationSymbolFields index] => this.Fields[(int)index];

        public string Target
        {
            get => this.Fields[(int)WixCloseApplicationSymbolFields.Target].AsString();
            set => this.Set((int)WixCloseApplicationSymbolFields.Target, value);
        }

        public string Description
        {
            get => this.Fields[(int)WixCloseApplicationSymbolFields.Description].AsString();
            set => this.Set((int)WixCloseApplicationSymbolFields.Description, value);
        }

        public string Condition
        {
            get => this.Fields[(int)WixCloseApplicationSymbolFields.Condition].AsString();
            set => this.Set((int)WixCloseApplicationSymbolFields.Condition, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixCloseApplicationSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixCloseApplicationSymbolFields.Attributes, value);
        }

        public int? Sequence
        {
            get => this.Fields[(int)WixCloseApplicationSymbolFields.Sequence].AsNullableNumber();
            set => this.Set((int)WixCloseApplicationSymbolFields.Sequence, value);
        }

        public string Property
        {
            get => this.Fields[(int)WixCloseApplicationSymbolFields.Property].AsString();
            set => this.Set((int)WixCloseApplicationSymbolFields.Property, value);
        }

        public int? TerminateExitCode
        {
            get => this.Fields[(int)WixCloseApplicationSymbolFields.TerminateExitCode].AsNullableNumber();
            set => this.Set((int)WixCloseApplicationSymbolFields.TerminateExitCode, value);
        }

        public int? Timeout
        {
            get => this.Fields[(int)WixCloseApplicationSymbolFields.Timeout].AsNullableNumber();
            set => this.Set((int)WixCloseApplicationSymbolFields.Timeout, value);
        }
    }
}