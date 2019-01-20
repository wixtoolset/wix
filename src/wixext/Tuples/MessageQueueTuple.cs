// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using WixToolset.Data;
    using WixToolset.Msmq.Tuples;

    public static partial class MsmqTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MessageQueue = new IntermediateTupleDefinition(
            MsmqTupleDefinitionType.MessageQueue.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.MessageQueue), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.BasePriority), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.JournalQuota), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.Label), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.MulticastAddress), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.PathName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.PrivLevel), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.Quota), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.ServiceTypeGuid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(MessageQueueTuple));
    }
}

namespace WixToolset.Msmq.Tuples
{
    using WixToolset.Data;

    public enum MessageQueueTupleFields
    {
        MessageQueue,
        Component_,
        BasePriority,
        JournalQuota,
        Label,
        MulticastAddress,
        PathName,
        PrivLevel,
        Quota,
        ServiceTypeGuid,
        Attributes,
    }

    public class MessageQueueTuple : IntermediateTuple
    {
        public MessageQueueTuple() : base(MsmqTupleDefinitions.MessageQueue, null, null)
        {
        }

        public MessageQueueTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(MsmqTupleDefinitions.MessageQueue, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MessageQueueTupleFields index] => this.Fields[(int)index];

        public string MessageQueue
        {
            get => this.Fields[(int)MessageQueueTupleFields.MessageQueue].AsString();
            set => this.Set((int)MessageQueueTupleFields.MessageQueue, value);
        }

        public string Component_
        {
            get => this.Fields[(int)MessageQueueTupleFields.Component_].AsString();
            set => this.Set((int)MessageQueueTupleFields.Component_, value);
        }

        public int BasePriority
        {
            get => this.Fields[(int)MessageQueueTupleFields.BasePriority].AsNumber();
            set => this.Set((int)MessageQueueTupleFields.BasePriority, value);
        }

        public int JournalQuota
        {
            get => this.Fields[(int)MessageQueueTupleFields.JournalQuota].AsNumber();
            set => this.Set((int)MessageQueueTupleFields.JournalQuota, value);
        }

        public string Label
        {
            get => this.Fields[(int)MessageQueueTupleFields.Label].AsString();
            set => this.Set((int)MessageQueueTupleFields.Label, value);
        }

        public string MulticastAddress
        {
            get => this.Fields[(int)MessageQueueTupleFields.MulticastAddress].AsString();
            set => this.Set((int)MessageQueueTupleFields.MulticastAddress, value);
        }

        public string PathName
        {
            get => this.Fields[(int)MessageQueueTupleFields.PathName].AsString();
            set => this.Set((int)MessageQueueTupleFields.PathName, value);
        }

        public int PrivLevel
        {
            get => this.Fields[(int)MessageQueueTupleFields.PrivLevel].AsNumber();
            set => this.Set((int)MessageQueueTupleFields.PrivLevel, value);
        }

        public int Quota
        {
            get => this.Fields[(int)MessageQueueTupleFields.Quota].AsNumber();
            set => this.Set((int)MessageQueueTupleFields.Quota, value);
        }

        public string ServiceTypeGuid
        {
            get => this.Fields[(int)MessageQueueTupleFields.ServiceTypeGuid].AsString();
            set => this.Set((int)MessageQueueTupleFields.ServiceTypeGuid, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)MessageQueueTupleFields.Attributes].AsNumber();
            set => this.Set((int)MessageQueueTupleFields.Attributes, value);
        }
    }
}