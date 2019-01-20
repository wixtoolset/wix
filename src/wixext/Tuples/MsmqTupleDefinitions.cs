// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Msmq
{
    using System;
    using WixToolset.Data;

    public enum MsmqTupleDefinitionType
    {
        MessageQueue,
        MessageQueueGroupPermission,
        MessageQueueUserPermission,
    }

    public static partial class MsmqTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out MsmqTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(MsmqTupleDefinitionType type)
        {
            switch (type)
            {
                case MsmqTupleDefinitionType.MessageQueue:
                    return MsmqTupleDefinitions.MessageQueue;

                case MsmqTupleDefinitionType.MessageQueueGroupPermission:
                    return MsmqTupleDefinitions.MessageQueueGroupPermission;

                case MsmqTupleDefinitionType.MessageQueueUserPermission:
                    return MsmqTupleDefinitions.MessageQueueUserPermission;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
