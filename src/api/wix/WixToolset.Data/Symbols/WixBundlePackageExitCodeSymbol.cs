// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundlePackageExitCode = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundlePackageExitCode,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundlePackageExitCodeSymbolFields.ChainPackageId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundlePackageExitCodeSymbolFields.Code), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundlePackageExitCodeSymbolFields.Behavior), IntermediateFieldType.String),
            },
            typeof(WixBundlePackageExitCodeSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBundlePackageExitCodeSymbolFields
    {
        ChainPackageId,
        Code,
        Behavior,
    }

    public enum ExitCodeBehaviorType
    {
        NotSet = -1,
        Success,
        Error,
        ScheduleReboot,
        ForceReboot,
    }

    public class WixBundlePackageExitCodeSymbol : IntermediateSymbol
    {
        public WixBundlePackageExitCodeSymbol() : base(SymbolDefinitions.WixBundlePackageExitCode, null, null)
        {
        }

        public WixBundlePackageExitCodeSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundlePackageExitCode, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundlePackageExitCodeSymbolFields index] => this.Fields[(int)index];

        public string ChainPackageId
        {
            get => (string)this.Fields[(int)WixBundlePackageExitCodeSymbolFields.ChainPackageId];
            set => this.Set((int)WixBundlePackageExitCodeSymbolFields.ChainPackageId, value);
        }

        public int? Code
        {
            get => (int?)this.Fields[(int)WixBundlePackageExitCodeSymbolFields.Code];
            set => this.Set((int)WixBundlePackageExitCodeSymbolFields.Code, value);
        }

        public ExitCodeBehaviorType Behavior
        {
            get => Enum.TryParse((string)this.Fields[(int)WixBundlePackageExitCodeSymbolFields.Behavior], true, out ExitCodeBehaviorType value) ? value : ExitCodeBehaviorType.NotSet;
            set => this.Set((int)WixBundlePackageExitCodeSymbolFields.Behavior, value.ToString());
        }
    }
}
