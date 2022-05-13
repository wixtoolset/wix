// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixMbaPrereqOptions = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixMbaPrereqOptions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixMbaPrereqOptionsSymbolFields.AlwaysInstallPrereqs), IntermediateFieldType.Number),
            },
            typeof(WixMbaPrereqOptionsSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using WixToolset.Data;

    public enum WixMbaPrereqOptionsSymbolFields
    {
        AlwaysInstallPrereqs,
    }

    public class WixMbaPrereqOptionsSymbol : IntermediateSymbol
    {
        public WixMbaPrereqOptionsSymbol() : base(BalSymbolDefinitions.WixMbaPrereqOptions, null, null)
        {
        }

        public WixMbaPrereqOptionsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixMbaPrereqOptions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixMbaPrereqOptionsSymbolFields index] => this.Fields[(int)index];

        public int AlwaysInstallPrereqs
        {
            get => this.Fields[(int)WixMbaPrereqOptionsSymbolFields.AlwaysInstallPrereqs].AsNumber();
            set => this.Set((int)WixMbaPrereqOptionsSymbolFields.AlwaysInstallPrereqs, value);
        }
    }
}
