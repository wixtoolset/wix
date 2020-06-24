// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Symbols;

    public static partial class SymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition PublishComponent = new IntermediateSymbolDefinition(
            SymbolDefinitionType.PublishComponent,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PublishComponentSymbolFields.ComponentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PublishComponentSymbolFields.Qualifier), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PublishComponentSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PublishComponentSymbolFields.AppData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PublishComponentSymbolFields.FeatureRef), IntermediateFieldType.String),
            },
            typeof(PublishComponentSymbol));
    }
}

namespace WixToolset.Data.Symbols
{
    public enum PublishComponentSymbolFields
    {
        ComponentId,
        Qualifier,
        ComponentRef,
        AppData,
        FeatureRef,
    }

    public class PublishComponentSymbol : IntermediateSymbol
    {
        public PublishComponentSymbol() : base(SymbolDefinitions.PublishComponent, null, null)
        {
        }

        public PublishComponentSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SymbolDefinitions.PublishComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PublishComponentSymbolFields index] => this.Fields[(int)index];

        public string ComponentId
        {
            get => (string)this.Fields[(int)PublishComponentSymbolFields.ComponentId];
            set => this.Set((int)PublishComponentSymbolFields.ComponentId, value);
        }

        public string Qualifier
        {
            get => (string)this.Fields[(int)PublishComponentSymbolFields.Qualifier];
            set => this.Set((int)PublishComponentSymbolFields.Qualifier, value);
        }

        public string ComponentRef
        {
            get => (string)this.Fields[(int)PublishComponentSymbolFields.ComponentRef];
            set => this.Set((int)PublishComponentSymbolFields.ComponentRef, value);
        }

        public string AppData
        {
            get => (string)this.Fields[(int)PublishComponentSymbolFields.AppData];
            set => this.Set((int)PublishComponentSymbolFields.AppData, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)PublishComponentSymbolFields.FeatureRef];
            set => this.Set((int)PublishComponentSymbolFields.FeatureRef, value);
        }
    }
}