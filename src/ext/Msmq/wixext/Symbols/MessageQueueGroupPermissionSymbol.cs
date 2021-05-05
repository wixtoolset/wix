// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using WixToolset.Data;
    using WixToolset.Msmq.Symbols;

    public static partial class MsmqSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MessageQueueGroupPermission = new IntermediateSymbolDefinition(
            MsmqSymbolDefinitionType.MessageQueueGroupPermission.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionSymbolFields.MessageQueueRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionSymbolFields.GroupRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueGroupPermissionSymbolFields.Permissions), IntermediateFieldType.Number),
            },
            typeof(MessageQueueGroupPermissionSymbol));
    }
}

namespace WixToolset.Msmq.Symbols
{
    using WixToolset.Data;

    public enum MessageQueueGroupPermissionSymbolFields
    {
        ComponentRef,
        MessageQueueRef,
        GroupRef,
        Permissions,
    }

    public class MessageQueueGroupPermissionSymbol : IntermediateSymbol
    {
        public MessageQueueGroupPermissionSymbol() : base(MsmqSymbolDefinitions.MessageQueueGroupPermission, null, null)
        {
        }

        public MessageQueueGroupPermissionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(MsmqSymbolDefinitions.MessageQueueGroupPermission, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MessageQueueGroupPermissionSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)MessageQueueGroupPermissionSymbolFields.ComponentRef].AsString();
            set => this.Set((int)MessageQueueGroupPermissionSymbolFields.ComponentRef, value);
        }

        public string MessageQueueRef
        {
            get => this.Fields[(int)MessageQueueGroupPermissionSymbolFields.MessageQueueRef].AsString();
            set => this.Set((int)MessageQueueGroupPermissionSymbolFields.MessageQueueRef, value);
        }

        public string GroupRef
        {
            get => this.Fields[(int)MessageQueueGroupPermissionSymbolFields.GroupRef].AsString();
            set => this.Set((int)MessageQueueGroupPermissionSymbolFields.GroupRef, value);
        }

        public int Permissions
        {
            get => this.Fields[(int)MessageQueueGroupPermissionSymbolFields.Permissions].AsNumber();
            set => this.Set((int)MessageQueueGroupPermissionSymbolFields.Permissions, value);
        }
    }
}