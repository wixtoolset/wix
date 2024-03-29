// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System;
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        [Obsolete]
        public static readonly IntermediateSymbolDefinition WixBootstrapperApplicationDll = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBootstrapperApplicationDll,
            new IntermediateFieldDefinition[]
            {
                new IntermediateFieldDefinition(nameof(WixBootstrapperApplicationDllSymbolFields.DpiAwareness), IntermediateFieldType.Number),
            },
            typeof(WixBootstrapperApplicationDllSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixBootstrapperApplicationDllSymbolFields
    {
        DpiAwareness,
    }

    [Obsolete]
    public enum WixBootstrapperApplicationDpiAwarenessType
    {
        Unaware,
        System,
        PerMonitor,
        PerMonitorV2,
        GdiScaled,
    }

    [Obsolete]
    public class WixBootstrapperApplicationDllSymbol : IntermediateSymbol
    {
        public WixBootstrapperApplicationDllSymbol() : base(SymbolDefinitions.WixBootstrapperApplicationDll, null, null)
        {
        }

        public WixBootstrapperApplicationDllSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBootstrapperApplicationDll, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBootstrapperApplicationDllSymbolFields index] => this.Fields[(int)index];

        public WixBootstrapperApplicationDpiAwarenessType DpiAwareness
        {
            get => (WixBootstrapperApplicationDpiAwarenessType)this.Fields[(int)WixBootstrapperApplicationDllSymbolFields.DpiAwareness].AsNumber();
            set => this.Set((int)WixBootstrapperApplicationDllSymbolFields.DpiAwareness, (int)value);
        }
    }
}
