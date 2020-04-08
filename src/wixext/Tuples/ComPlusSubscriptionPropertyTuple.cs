// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using WixToolset.Data;
    using WixToolset.ComPlus.Tuples;

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition ComPlusSubscriptionProperty = new IntermediateTupleDefinition(
            ComPlusTupleDefinitionType.ComPlusSubscriptionProperty.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionPropertyTupleFields.SubscriptionRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionPropertyTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(ComPlusSubscriptionPropertyTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(ComPlusSubscriptionPropertyTuple));
    }
}

namespace WixToolset.ComPlus.Tuples
{
    using WixToolset.Data;

    public enum ComPlusSubscriptionPropertyTupleFields
    {
        SubscriptionRef,
        Name,
        Value,
    }

    public class ComPlusSubscriptionPropertyTuple : IntermediateTuple
    {
        public ComPlusSubscriptionPropertyTuple() : base(ComPlusTupleDefinitions.ComPlusSubscriptionProperty, null, null)
        {
        }

        public ComPlusSubscriptionPropertyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(ComPlusTupleDefinitions.ComPlusSubscriptionProperty, sourceLineNumber, id)
        {
        }

        public IntermediateField this[ComPlusSubscriptionPropertyTupleFields index] => this.Fields[(int)index];

        public string SubscriptionRef
        {
            get => this.Fields[(int)ComPlusSubscriptionPropertyTupleFields.SubscriptionRef].AsString();
            set => this.Set((int)ComPlusSubscriptionPropertyTupleFields.SubscriptionRef, value);
        }

        public string Name
        {
            get => this.Fields[(int)ComPlusSubscriptionPropertyTupleFields.Name].AsString();
            set => this.Set((int)ComPlusSubscriptionPropertyTupleFields.Name, value);
        }

        public string Value
        {
            get => this.Fields[(int)ComPlusSubscriptionPropertyTupleFields.Value].AsString();
            set => this.Set((int)ComPlusSubscriptionPropertyTupleFields.Value, value);
        }
    }
}