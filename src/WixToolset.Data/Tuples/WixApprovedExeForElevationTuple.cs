// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixApprovedExeForElevation = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixApprovedExeForElevation,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationSymbolFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationSymbolFields.ValueName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixApprovedExeForElevationSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixApprovedExeForElevationSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    using System;

    public enum WixApprovedExeForElevationSymbolFields
    {
        Key,
        ValueName,
        Attributes,
    }

    [Flags]
    public enum WixApprovedExeForElevationAttributes
    {
        None = 0x0,
        Win64 = 0x1,
    }

    public class WixApprovedExeForElevationSymbol : IntermediateSymbol
    {
        public WixApprovedExeForElevationSymbol() : base(SymbolDefinitions.WixApprovedExeForElevation, null, null)
        {
        }

        public WixApprovedExeForElevationSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixApprovedExeForElevation, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixApprovedExeForElevationSymbolFields index] => this.Fields[(int)index];

        public string Key
        {
            get => (string)this.Fields[(int)WixApprovedExeForElevationSymbolFields.Key];
            set => this.Set((int)WixApprovedExeForElevationSymbolFields.Key, value);
        }

        public string ValueName
        {
            get => (string)this.Fields[(int)WixApprovedExeForElevationSymbolFields.ValueName];
            set => this.Set((int)WixApprovedExeForElevationSymbolFields.ValueName, value);
        }

        public WixApprovedExeForElevationAttributes Attributes
        {
            get => (WixApprovedExeForElevationAttributes)this.Fields[(int)WixApprovedExeForElevationSymbolFields.Attributes].AsNumber();
            set => this.Set((int)WixApprovedExeForElevationSymbolFields.Attributes, (int)value);
        }

        public bool Win64 => (this.Attributes & WixApprovedExeForElevationAttributes.Win64) == WixApprovedExeForElevationAttributes.Win64;
    }
}
