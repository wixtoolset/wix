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
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionTupleFields.MessageQueueGroupPermission), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionTupleFields.MessageQueue_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionTupleFields.Group_), IntermediateFieldType.String),
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
        MessageQueueGroupPermission,
        Component_,
        MessageQueue_,
        Group_,
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

        public string MessageQueueGroupPermission
        {
            get => this.Fields[(int)MessageQueueGroupPermissionTupleFields.MessageQueueGroupPermission].AsString();
            set => this.Set((int)MessageQueueGroupPermissionTupleFields.MessageQueueGroupPermission, value);
        }

        public string Component_
        {
            get => this.Fields[(int)MessageQueueGroupPermissionTupleFields.Component_].AsString();
            set => this.Set((int)MessageQueueGroupPermissionTupleFields.Component_, value);
        }

        public string MessageQueue_
        {
            get => this.Fields[(int)MessageQueueGroupPermissionTupleFields.MessageQueue_].AsString();
            set => this.Set((int)MessageQueueGroupPermissionTupleFields.MessageQueue_, value);
        }

        public string Group_
        {
            get => this.Fields[(int)MessageQueueGroupPermissionTupleFields.Group_].AsString();
            set => this.Set((int)MessageQueueGroupPermissionTupleFields.Group_, value);
        }

        public int Permissions
        {
            get => this.Fields[(int)MessageQueueGroupPermissionTupleFields.Permissions].AsNumber();
            set => this.Set((int)MessageQueueGroupPermissionTupleFields.Permissions, value);
        }
    }
}