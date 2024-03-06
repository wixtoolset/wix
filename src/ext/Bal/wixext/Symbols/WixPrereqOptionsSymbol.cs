// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplications
{
    using WixToolset.Data;
    using WixToolset.BootstrapperApplications.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixPrereqOptions = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixPrereqOptions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPrereqOptionsSymbolFields.Primary), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPrereqOptionsSymbolFields.HandleHelp), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPrereqOptionsSymbolFields.HandleLayout), IntermediateFieldType.Number),
            },
            typeof(WixPrereqOptionsSymbol));
    }
}

namespace WixToolset.BootstrapperApplications.Symbols
{
    using WixToolset.Data;

    public enum WixPrereqOptionsSymbolFields
    {
        Primary,
        HandleHelp,
        HandleLayout,
    }

    public class WixPrereqOptionsSymbol : IntermediateSymbol
    {
        public WixPrereqOptionsSymbol() : base(BalSymbolDefinitions.WixPrereqOptions, null, null)
        {
        }

        public WixPrereqOptionsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixPrereqOptions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPrereqOptionsSymbolFields index] => this.Fields[(int)index];

        public int Primary
        {
            get => this.Fields[(int)WixPrereqOptionsSymbolFields.Primary].AsNumber();
            set => this.Set((int)WixPrereqOptionsSymbolFields.Primary, value);
        }

        public int? HandleHelp
        {
            get => (int?)this.Fields[(int)WixPrereqOptionsSymbolFields.HandleHelp];
            set => this.Set((int)WixPrereqOptionsSymbolFields.HandleHelp, value);
        }

        public int? HandleLayout
        {
            get => (int?)this.Fields[(int)WixPrereqOptionsSymbolFields.HandleLayout];
            set => this.Set((int)WixPrereqOptionsSymbolFields.HandleLayout, value);
        }
    }
}
