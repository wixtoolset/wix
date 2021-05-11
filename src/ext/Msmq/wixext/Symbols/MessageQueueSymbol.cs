// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using WixToolset.Data;
    using WixToolset.Msmq.Symbols;

    public static partial class MsmqSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MessageQueue = new IntermediateSymbolDefinition(
            MsmqSymbolDefinitionType.MessageQueue.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.BasePriority), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.JournalQuota), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.Label), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.MulticastAddress), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.PathName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.PrivLevel), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.Quota), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.ServiceTypeGuid), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueSymbolFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(MessageQueueSymbol));
    }
}

namespace WixToolset.Msmq.Symbols
{
    using WixToolset.Data;

    public enum MessageQueueSymbolFields
    {
        ComponentRef,
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

    public class MessageQueueSymbol : IntermediateSymbol
    {
        public MessageQueueSymbol() : base(MsmqSymbolDefinitions.MessageQueue, null, null)
        {
        }

        public MessageQueueSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(MsmqSymbolDefinitions.MessageQueue, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MessageQueueSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)MessageQueueSymbolFields.ComponentRef].AsString();
            set => this.Set((int)MessageQueueSymbolFields.ComponentRef, value);
        }

        public int? BasePriority
        {
            get => this.Fields[(int)MessageQueueSymbolFields.BasePriority].AsNullableNumber();
            set => this.Set((int)MessageQueueSymbolFields.BasePriority, value);
        }

        public int? JournalQuota
        {
            get => this.Fields[(int)MessageQueueSymbolFields.JournalQuota].AsNullableNumber();
            set => this.Set((int)MessageQueueSymbolFields.JournalQuota, value);
        }

        public string Label
        {
            get => this.Fields[(int)MessageQueueSymbolFields.Label].AsString();
            set => this.Set((int)MessageQueueSymbolFields.Label, value);
        }

        public string MulticastAddress
        {
            get => this.Fields[(int)MessageQueueSymbolFields.MulticastAddress].AsString();
            set => this.Set((int)MessageQueueSymbolFields.MulticastAddress, value);
        }

        public string PathName
        {
            get => this.Fields[(int)MessageQueueSymbolFields.PathName].AsString();
            set => this.Set((int)MessageQueueSymbolFields.PathName, value);
        }

        public int? PrivLevel
        {
            get => this.Fields[(int)MessageQueueSymbolFields.PrivLevel].AsNullableNumber();
            set => this.Set((int)MessageQueueSymbolFields.PrivLevel, value);
        }

        public int? Quota
        {
            get => this.Fields[(int)MessageQueueSymbolFields.Quota].AsNullableNumber();
            set => this.Set((int)MessageQueueSymbolFields.Quota, value);
        }

        public string ServiceTypeGuid
        {
            get => this.Fields[(int)MessageQueueSymbolFields.ServiceTypeGuid].AsString();
            set => this.Set((int)MessageQueueSymbolFields.ServiceTypeGuid, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)MessageQueueSymbolFields.Attributes].AsNumber();
            set => this.Set((int)MessageQueueSymbolFields.Attributes, value);
        }
    }
}