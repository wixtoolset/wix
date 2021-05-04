// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Symbols;

    public static partial class ComPlusSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition ComPlusSubscription = new IntermediateSymbolDefinition(
            ComPlusSymbolDefinitionType.ComPlusSubscription.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionSymbolFields.Subscription), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionSymbolFields.ComPlusComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionSymbolFields.SubscriptionId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionSymbolFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionSymbolFields.EventCLSID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionSymbolFields.PublisherID), IntermediateFieldType.String),
            },
            typeof(ComPlusSubscriptionSymbol));
    }
}

namespace WixToolset.ComPlus.Symbols
{
    using WixToolset.Data;

    public enum ComPlusSubscriptionSymbolFields
    {
        Subscription,
        ComPlusComponentRef,
        ComponentRef,
        SubscriptionId,
        Name,
        EventCLSID,
        PublisherID,
    }

    public class ComPlusSubscriptionSymbol : IntermediateSymbol
    {
        public ComPlusSubscriptionSymbol() : base(ComPlusSymbolDefinitions.ComPlusSubscription, null, null)
        {
        }

        public ComPlusSubscriptionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusSymbolDefinitions.ComPlusSubscription, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusSubscriptionSymbolFields index] => this.Fields[(int)index];

        public string Subscription
        {
            get => this.Fields[(int)ComPlusSubscriptionSymbolFields.Subscription].AsString();
            set => this.Set((int)ComPlusSubscriptionSymbolFields.Subscription, value);
        }

        public string ComPlusComponentRef
        {
            get => this.Fields[(int)ComPlusSubscriptionSymbolFields.ComPlusComponentRef].AsString();
            set => this.Set((int)ComPlusSubscriptionSymbolFields.ComPlusComponentRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusSubscriptionSymbolFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusSubscriptionSymbolFields.ComponentRef, value);
        }

        public string SubscriptionId
        {
            get => this.Fields[(int)ComPlusSubscriptionSymbolFields.SubscriptionId].AsString();
            set => this.Set((int)ComPlusSubscriptionSymbolFields.SubscriptionId, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusSubscriptionSymbolFields.Name].AsString();
            set => this.Set((int)ComPlusSubscriptionSymbolFields.Name, value);
        }

        public string EventCLSID
        {
            get => this.Fields[(int)ComPlusSubscriptionSymbolFields.EventCLSID].AsString();
            set => this.Set((int)ComPlusSubscriptionSymbolFields.EventCLSID, value);
        }

        public string PublisherID
        {
            get => this.Fields[(int)ComPlusSubscriptionSymbolFields.PublisherID].AsString();
            set => this.Set((int)ComPlusSubscriptionSymbolFields.PublisherID, value);
        }
    }
}