// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Symbols;

    public static partial class BalSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixDncOptions = new IntermediateSymbolDefinition(
            BalSymbolDefinitionType.WixDncOptions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDncOptionsSymbolFields.SelfContainedDeployment), IntermediateFieldType.Number),
            },
            typeof(WixDncOptionsSymbol));
    }
}

namespace WixToolset.Bal.Symbols
{
    using WixToolset.Data;

    public enum WixDncOptionsSymbolFields
    {
        SelfContainedDeployment,
    }

    public class WixDncOptionsSymbol : IntermediateSymbol
    {
        public WixDncOptionsSymbol() : base(BalSymbolDefinitions.WixDncOptions, null, null)
        {
        }

        public WixDncOptionsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalSymbolDefinitions.WixDncOptions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDncOptionsSymbolFields index] => this.Fields[(int)index];

        public int SelfContainedDeployment
        {
            get => this.Fields[(int)WixDncOptionsSymbolFields.SelfContainedDeployment].AsNumber();
            set => this.Set((int)WixDncOptionsSymbolFields.SelfContainedDeployment, value);
        }
    }
}