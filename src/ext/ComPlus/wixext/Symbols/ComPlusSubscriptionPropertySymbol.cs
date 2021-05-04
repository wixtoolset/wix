// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusSubscriptionProperty = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusSubscriptionProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionPropertySymbolFields.SubscriptionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionPropertySymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionPropertySymbolFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusSubscriptionPropertySymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusSubscriptionPropertySymbolFields
    {
        SubscriptionRef,
        Name,
        Value,
    }

    public class ComPlusSubscriptionPropertySymbol : IntermediateSymbol
    {
        public ComPlusSubscriptionPropertySymbol() : base(ComPlusSymbolDefinitions.ComPlusSubscriptionProperty, null, null)
        {
        }

        public ComPlusSubscriptionPropertySymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusSubscriptionProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusSubscriptionPropertySymbolFields index] => this.Fields[(int)index];

        public string SubscriptionRef
        {
            get => this.Fields[(int)ComPlusSubscriptionPropertySymbolFields.SubscriptionRef].AsString();
            set => this.Set((int)ComPlusSubscriptionPropertySymbolFields.SubscriptionRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusSubscriptionPropertySymbolFields.Name].AsString();
            set => this.Set((int)ComPlusSubscriptionPropertySymbolFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusSubscriptionPropertySymbolFields.Value].AsString();
            set => this.Set((int)ComPlusSubscriptionPropertySymbolFields.Value, value);
        }
    }
}