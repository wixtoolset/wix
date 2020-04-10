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
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionTupleFields.MessageQueueRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionTupleFields.UserRef), IntermediateFieldType.String),
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
        ComponentRef,
        MessageQueueRef,
        UserRef,
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

        public string ComponentRef
        {
            get => this.Fields[(int)MessageQueueUserPermissionTupleFields.ComponentRef].AsString();
            set => this.Set((int)MessageQueueUserPermissionTupleFields.ComponentRef, value);
        }

        public string MessageQueueRef
        {
            get => this.Fields[(int)MessageQueueUserPermissionTupleFields.MessageQueueRef].AsString();
            set => this.Set((int)MessageQueueUserPermissionTupleFields.MessageQueueRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)MessageQueueUserPermissionTupleFields.UserRef].AsString();
            set => this.Set((int)MessageQueueUserPermissionTupleFields.UserRef, value);
        }

        public int Permissions
        {
            get => this.Fields[(int)MessageQueueUserPermissionTupleFields.Permissions].AsNumber();
            set => this.Set((int)MessageQueueUserPermissionTupleFields.Permissions, value);
        }
    }
}