// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusSubscription = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusSubscription.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionTupleFields.Subscription), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionTupleFields.ComPlusComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionTupleFields.SubscriptionId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionTupleFields.EventCLSID), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionTupleFields.PublisherID), IntermediateFieldType.String),
            },
            typeof(ComPlusSubscriptionTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusSubscriptionTupleFields
    {
        Subscription,
        ComPlusComponentRef,
        ComponentRef,
        SubscriptionId,
        Name,
        EventCLSID,
        PublisherID,
    }

    public class ComPlusSubscriptionTuple : IntermediateTuple
    {
        public ComPlusSubscriptionTuple() : base(ComPlusTupleDefinitions.ComPlusSubscription, null, null)
        {
        }

        public ComPlusSubscriptionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusSubscription, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusSubscriptionTupleFields index] => this.Fields[(int)index];

        public string Subscription
        {
            get => this.Fields[(int)ComPlusSubscriptionTupleFields.Subscription].AsString();
            set => this.Set((int)ComPlusSubscriptionTupleFields.Subscription, value);
        }

        public string ComPlusComponentRef
        {
            get => this.Fields[(int)ComPlusSubscriptionTupleFields.ComPlusComponentRef].AsString();
            set => this.Set((int)ComPlusSubscriptionTupleFields.ComPlusComponentRef, value);
        }

        public string ComponentRef
        {
            get => this.Fields[(int)ComPlusSubscriptionTupleFields.ComponentRef].AsString();
            set => this.Set((int)ComPlusSubscriptionTupleFields.ComponentRef, value);
        }

        public string SubscriptionId
        {
            get => this.Fields[(int)ComPlusSubscriptionTupleFields.SubscriptionId].AsString();
            set => this.Set((int)ComPlusSubscriptionTupleFields.SubscriptionId, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusSubscriptionTupleFields.Name].AsString();
            set => this.Set((int)ComPlusSubscriptionTupleFields.Name, value);
        }

        public string EventCLSID
        {
            get => this.Fields[(int)ComPlusSubscriptionTupleFields.EventCLSID].AsString();
            set => this.Set((int)ComPlusSubscriptionTupleFields.EventCLSID, value);
        }

        public string PublisherID
        {
            get => this.Fields[(int)ComPlusSubscriptionTupleFields.PublisherID].AsString();
            set => this.Set((int)ComPlusSubscriptionTupleFields.PublisherID, value);
        }
    }
}