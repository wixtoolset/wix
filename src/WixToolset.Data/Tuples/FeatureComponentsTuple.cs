// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition FeatureComponents = new IntermediateSymbolDefinition(
            SymbolDefinitionType.FeatureComponents,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FeatureComponentsSymbolFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FeatureComponentsSymbolFields.ComponentRef), IntermediateFieldType.String),
            },
            typeof(FeatureComponentsSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum FeatureComponentsSymbolFields
    {
        FeatureRef,
        ComponentRef,
    }

    public class FeatureComponentsSymbol : IntermediateSymbol
    {
        public FeatureComponentsSymbol() : base(SymbolDefinitions.FeatureComponents, null, null)
        {
        }

        public FeatureComponentsSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.FeatureComponents, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FeatureComponentsSymbolFields index] => this.Fields[(int)index];

        public string FeatureRef
        {
            get => (string)this.Fields[(int)FeatureComponentsSymbolFields.FeatureRef];
            set => this.Set((int)FeatureComponentsSymbolFields.FeatureRef, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)FeatureComponentsSymbolFields.ComponentRef];
            set => this.Set((int)FeatureComponentsSymbolFields.ComponentRef, value);
        }
    }
}