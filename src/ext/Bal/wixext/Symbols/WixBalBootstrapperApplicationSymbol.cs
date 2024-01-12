// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBalBootstrapperApplication = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixBalBootstrapperApplication.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalBootstrapperApplicationSymbolFields.Type), IntermediateFieldType.Number),
            },
            typeof(WixBalBootstrapperApplicationSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using System;
    using WixToolset.Data;

    public enum WixBalBootstrapperApplicationType
    {
        Unknown,
        Standard,
        [Obsolete]
        ManagedHost,
        [Obsolete]
        DotNetCoreHost,
        InternalUi,
        Prerequisite,
    }

    public enum WixBalBootstrapperApplicationSymbolFields
    {
        Type,
    }

    public class WixBalBootstrapperApplicationSymbol : IntermediateSymbol
    {
        public WixBalBootstrapperApplicationSymbol() : base(BalSymbolDefinitions.WixBalBootstrapperApplication, null, null)
        {
        }

        public WixBalBootstrapperApplicationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixBalBootstrapperApplication, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalBootstrapperApplicationSymbolFields index] => this.Fields[(int)index];

        public WixBalBootstrapperApplicationType Type
        {
            get => (WixBalBootstrapperApplicationType)this.Fields[(int)WixBalBootstrapperApplicationSymbolFields.Type].AsNumber();
            set => this.Set((int)WixBalBootstrapperApplicationSymbolFields.Type, (int)value);
        }
    }
}
