// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixStdbaOptions = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixStdbaOptions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsSymbolFields.SuppressOptionsUI), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsSymbolFields.SuppressDowngradeFailure), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsSymbolFields.SuppressRepair), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsSymbolFields.ShowVersion), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixStdbaOptionsSymbolFields.SupportCacheOnly), IntermediateFieldType.Number),
            },
            typeof(WixStdbaOptionsSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using WixToolset.Data;

    public enum WixStdbaOptionsSymbolFields
    {
        SuppressOptionsUI,
        SuppressDowngradeFailure,
        SuppressRepair,
        ShowVersion,
        SupportCacheOnly,
    }

    public class WixStdbaOptionsSymbol : IntermediateSymbol
    {
        public WixStdbaOptionsSymbol() : base(BalSymbolDefinitions.WixStdbaOptions, null, null)
        {
        }

        public WixStdbaOptionsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixStdbaOptions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixStdbaOptionsSymbolFields index] => this.Fields[(int)index];

        public int SuppressOptionsUI
        {
            get => this.Fields[(int)WixStdbaOptionsSymbolFields.SuppressOptionsUI].AsNumber();
            set => this.Set((int)WixStdbaOptionsSymbolFields.SuppressOptionsUI, value);
        }

        public int SuppressDowngradeFailure
        {
            get => this.Fields[(int)WixStdbaOptionsSymbolFields.SuppressDowngradeFailure].AsNumber();
            set => this.Set((int)WixStdbaOptionsSymbolFields.SuppressDowngradeFailure, value);
        }

        public int SuppressRepair
        {
            get => this.Fields[(int)WixStdbaOptionsSymbolFields.SuppressRepair].AsNumber();
            set => this.Set((int)WixStdbaOptionsSymbolFields.SuppressRepair, value);
        }

        public int ShowVersion
        {
            get => this.Fields[(int)WixStdbaOptionsSymbolFields.ShowVersion].AsNumber();
            set => this.Set((int)WixStdbaOptionsSymbolFields.ShowVersion, value);
        }

        public int SupportCacheOnly
        {
            get => this.Fields[(int)WixStdbaOptionsSymbolFields.SupportCacheOnly].AsNumber();
            set => this.Set((int)WixStdbaOptionsSymbolFields.SupportCacheOnly, value);
        }
    }
}