// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using System;
    using WixToolset.Data;

    public enum MsmqSymbolDefinitionType
    {
        MessageQueue,
        MessageQueueGroupPermission,
        MessageQueueUserPermission,
    }

    public static partial class MsmqSymbolDefinitions
    {
        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out MsmqSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(MsmqSymbolDefinitionType type)
        {
            switch (type)
            {
                case MsmqSymbolDefinitionType.MessageQueue:
                    return MsmqSymbolDefinitions.MessageQueue;

                case MsmqSymbolDefinitionType.MessageQueueGroupPermission:
                    return MsmqSymbolDefinitions.MessageQueueGroupPermission;

                case MsmqSymbolDefinitionType.MessageQueueUserPermission:
                    return MsmqSymbolDefinitions.MessageQueueUserPermission;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
