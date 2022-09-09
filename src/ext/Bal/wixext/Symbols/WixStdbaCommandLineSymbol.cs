// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixStdbaCommandLine = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixStdbaCommandLine.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixStdbaCommandLineSymbolFields.VariableType), IntermediateFieldType.Number),
            },
            typeof(WixStdbaCommandLineSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using System;
    using WixToolset.Data;

    public enum WixStdbaCommandLineSymbolFields
    {
        VariableType,
    }

    public enum WixStdbaCommandLineVariableType
    {
        CaseSensitive,
        CaseInsensitive,
    }

    public class WixStdbaCommandLineSymbol : IntermediateSymbol
    {
        public WixStdbaCommandLineSymbol() : base(BalSymbolDefinitions.WixStdbaCommandLine, null, null)
        {
        }

        public WixStdbaCommandLineSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixStdbaCommandLine, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixStdbaCommandLineSymbolFields index] => this.Fields[(int)index];

        public WixStdbaCommandLineVariableType VariableType
        {
            get => (WixStdbaCommandLineVariableType)this.Fields[(int)WixStdbaCommandLineSymbolFields.VariableType].AsNumber();
            set => this.Set((int)WixStdbaCommandLineSymbolFields.VariableType, (int)value);
        }
    }
}
