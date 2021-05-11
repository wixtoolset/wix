// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using WixToolset.Data;
    using WixToolset.Msmq.Symbols;

    public static partial class MsmqSymbolDefinitions
    {
        public static readonly IntermediateSymbolDefinition MessageQueueUserPermission = new IntermediateSymbolDefinition(
            MsmqSymbolDefinitionType.MessageQueueUserPermission.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionSymbolFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionSymbolFields.MessageQueueRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionSymbolFields.UserRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MessageQueueUserPermissionSymbolFields.Permissions), IntermediateFieldType.Number),
            },
            typeof(MessageQueueUserPermissionSymbol));
    }
}

namespace WixToolset.Msmq.Symbols
{
    using WixToolset.Data;

    public enum MessageQueueUserPermissionSymbolFields
    {
        ComponentRef,
        MessageQueueRef,
        UserRef,
        Permissions,
    }

    public class MessageQueueUserPermissionSymbol : IntermediateSymbol
    {
        public MessageQueueUserPermissionSymbol() : base(MsmqSymbolDefinitions.MessageQueueUserPermission, null, null)
        {
        }

        public MessageQueueUserPermissionSymbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base(MsmqSymbolDefinitions.MessageQueueUserPermission, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MessageQueueUserPermissionSymbolFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)MessageQueueUserPermissionSymbolFields.ComponentRef].AsString();
            set => this.Set((int)MessageQueueUserPermissionSymbolFields.ComponentRef, value);
        }

        public string MessageQueueRef
        {
            get => this.Fields[(int)MessageQueueUserPermissionSymbolFields.MessageQueueRef].AsString();
            set => this.Set((int)MessageQueueUserPermissionSymbolFields.MessageQueueRef, value);
        }

        public string UserRef
        {
            get => this.Fields[(int)MessageQueueUserPermissionSymbolFields.UserRef].AsString();
            set => this.Set((int)MessageQueueUserPermissionSymbolFields.UserRef, value);
        }

        public int Permissions
        {
            get => this.Fields[(int)MessageQueueUserPermissionSymbolFields.Permissions].AsNumber();
            set => this.Set((int)MessageQueueUserPermissionSymbolFields.Permissions, value);
        }
    }
}