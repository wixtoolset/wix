// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixStdbaOverridableVariable = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixStdbaOverridableVariable.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixStdbaOverridableVariableSymbolFields.Name), IntermediateFieldType.String),
            },
            typeof(WixStdbaOverridableVariableSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using WixToolset.Data;

    public enum WixStdbaOverridableVariableSymbolFields
    {
        Name,
    }

    public class WixStdbaOverridableVariableSymbol : IntermediateSymbol
    {
        public WixStdbaOverridableVariableSymbol() : base(BalSymbolDefinitions.WixStdbaOverridableVariable, null, null)
        {
        }

        public WixStdbaOverridableVariableSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixStdbaOverridableVariable, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixStdbaOverridableVariableSymbolFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)WixStdbaOverridableVariableSymbolFields.Name].AsString();
            set => this.Set((int)WixStdbaOverridableVariableSymbolFields.Name, value);
        }
    }
}