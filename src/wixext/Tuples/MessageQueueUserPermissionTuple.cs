// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using WixToolset.Data;
    using WixToolset.Msmq.Tuples;

    public static partial class MsmqTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MessageQueueUserPermission = new IntermediateTupleDefinition(
            MsmqTupleDefinitionType.MessageQueueUserPermission.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionTupleFields.MessageQueueUserPermission), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionTupleFields.MessageQueue_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionTupleFields.User_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionTupleFields.Permissions), IntermediateFieldType.Number),
            },
            typeof(MessageQueueUserPermissionTuple));
    }
}

namespace WixToolset.Msmq.Tuples
{
    using WixToolset.Data;

    public enum MessageQueueUserPermissionTupleFields
    {
        MessageQueueUserPermission,
        Component_,
        MessageQueue_,
        User_,
        Permissions,
    }

    public class MessageQueueUserPermissionTuple : IntermediateTuple
    {
        public MessageQueueUserPermissionTuple() : base(MsmqTupleDefinitions.MessageQueueUserPermission, null, null)
        {
        }

        public MessageQueueUserPermissionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(MsmqTupleDefinitions.MessageQueueUserPermission, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MessageQueueUserPermissionTupleFields index] => this.Fields[(int)index];

        public string MessageQueueUserPermission
        {
            get => this.Fields[(int)MessageQueueUserPermissionTupleFields.MessageQueueUserPermission].AsString();
            set => this.Set((int)MessageQueueUserPermissionTupleFields.MessageQueueUserPermission, value);
        }

        public string Component_
        {
            get => this.Fields[(int)MessageQueueUserPermissionTupleFields.Component_].AsString();
            set => this.Set((int)MessageQueueUserPermissionTupleFields.Component_, value);
        }

        public string MessageQueue_
        {
            get => this.Fields[(int)MessageQueueUserPermissionTupleFields.MessageQueue_].AsString();
            set => this.Set((int)MessageQueueUserPermissionTupleFields.MessageQueue_, value);
        }

        public string User_
        {
            get => this.Fields[(int)MessageQueueUserPermissionTupleFields.User_].AsString();
            set => this.Set((int)MessageQueueUserPermissionTupleFields.User_, value);
        }

        public int Permissions
        {
            get => this.Fields[(int)MessageQueueUserPermissionTupleFields.Permissions].AsNumber();
            set => this.Set((int)MessageQueueUserPermissionTupleFields.Permissions, value);
        }
    }
}