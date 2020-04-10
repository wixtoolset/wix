// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using WixToolset.Data;
    using WixToolset.Msmq.Tuples;

    public static partial class MsmqTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MessageQueueGroupPermission = new IntermediateTupleDefinition(
            MsmqTupleDefinitionType.MessageQueueGroupPermission.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionTupleFields.MessageQueueRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionTupleFields.GroupRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionTupleFields.Permissions), IntermediateFieldType.Number),
            },
            typeof(MessageQueueGroupPermissionTuple));
    }
}

namespace WixToolset.Msmq.Tuples
{
    using WixToolset.Data;

    public enum MessageQueueGroupPermissionTupleFields
    {
        ComponentRef,
        MessageQueueRef,
        GroupRef,
        Permissions,
    }

    public class MessageQueueGroupPermissionTuple : IntermediateTuple
    {
        public MessageQueueGroupPermissionTuple() : base(MsmqTupleDefinitions.MessageQueueGroupPermission, null, null)
        {
        }

        public MessageQueueGroupPermissionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(MsmqTupleDefinitions.MessageQueueGroupPermission, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MessageQueueGroupPermissionTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)MessageQueueGroupPermissionTupleFields.ComponentRef].AsString();
            set => this.Set((int)MessageQueueGroupPermissionTupleFields.ComponentRef, value);
        }

        public string MessageQueueRef
        {
            get => this.Fields[(int)MessageQueueGroupPermissionTupleFields.MessageQueueRef].AsString();
            set => this.Set((int)MessageQueueGroupPermissionTupleFields.MessageQueueRef, value);
        }

        public string GroupRef
        {
            get => this.Fields[(int)MessageQueueGroupPermissionTupleFields.GroupRef].AsString();
            set => this.Set((int)MessageQueueGroupPermissionTupleFields.GroupRef, value);
        }

        public int Permissions
        {
            get => this.Fields[(int)MessageQueueGroupPermissionTupleFields.Permissions].AsNumber();
            set => this.Set((int)MessageQueueGroupPermissionTupleFields.Permissions, value);
        }
    }
}