// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition WixBundleMsiProperty = new IntermediateSymbolDefinition(
            SymbolDefinitionType.WixBundleMsiProperty,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleMsiPropertySymbolFields.PackageRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPropertySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPropertySymbolFields.Value), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleMsiPropertySymbolFields.Condition), IntermediateFieldType.String),
            },
            typeof(WixBundleMsiPropertySymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum WixBundleMsiPropertySymbolFields
    {
        PackageRef,
        Name,
        Value,
        Condition,
    }

    public class WixBundleMsiPropertySymbol : IntermediateSymbol
    {
        public WixBundleMsiPropertySymbol() : base(SymbolDefinitions.WixBundleMsiProperty, null, null)
        {
        }

        public WixBundleMsiPropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.WixBundleMsiProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleMsiPropertySymbolFields index] => this.Fields[(int)index];

        public string PackageRef
        {
            get => (string)this.Fields[(int)WixBundleMsiPropertySymbolFields.PackageRef];
            set => this.Set((int)WixBundleMsiPropertySymbolFields.PackageRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleMsiPropertySymbolFields.Name];
            set => this.Set((int)WixBundleMsiPropertySymbolFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)WixBundleMsiPropertySymbolFields.Value];
            set => this.Set((int)WixBundleMsiPropertySymbolFields.Value, value);
        }

        public string Condition
        {
            get => (string)this.Fields[(int)WixBundleMsiPropertySymbolFields.Condition];
            set => this.Set((int)WixBundleMsiPropertySymbolFields.Condition, value);
        }
    }
}