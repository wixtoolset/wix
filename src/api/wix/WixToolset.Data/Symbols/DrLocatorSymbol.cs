// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition DrLocator = new IntermediateSymbolDefinition(
            SymbolDefinitionType.DrLocator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(DrLocatorSymbolFields.SignatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DrLocatorSymbolFields.Parent), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DrLocatorSymbolFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DrLocatorSymbolFields.Depth), IntermediateFieldType.Number),
            },
            typeof(DrLocatorSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum DrLocatorSymbolFields
    {
        SignatureRef,
        Parent,
        Path,
        Depth,
    }

    public class DrLocatorSymbol : IntermediateSymbol
    {
        public DrLocatorSymbol() : base(SymbolDefinitions.DrLocator, null, null)
        {
        }

        public DrLocatorSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.DrLocator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[DrLocatorSymbolFields index] => this.Fields[(int)index];

        public string SignatureRef
        {
            get => (string)this.Fields[(int)DrLocatorSymbolFields.SignatureRef];
            set => this.Set((int)DrLocatorSymbolFields.SignatureRef, value);
        }

        public string Parent
        {
            get => (string)this.Fields[(int)DrLocatorSymbolFields.Parent];
            set => this.Set((int)DrLocatorSymbolFields.Parent, value);
        }

        public string Path
        {
            get => (string)this.Fields[(int)DrLocatorSymbolFields.Path];
            set => this.Set((int)DrLocatorSymbolFields.Path, value);
        }

        public int? Depth
        {
            get => (int?)this.Fields[(int)DrLocatorSymbolFields.Depth];
            set => this.Set((int)DrLocatorSymbolFields.Depth, value);
        }
    }
}