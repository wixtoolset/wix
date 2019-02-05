// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using System;
    using WixToolset.Data;

    public enum ComPlusTupleDefinitionType
    {
        ComPlusApplication,
        ComPlusApplicationProperty,
        ComPlusApplicationRole,
        ComPlusApplicationRoleProperty,
        ComPlusAssembly,
        ComPlusAssemblyDependency,
        ComPlusComponent,
        ComPlusComponentProperty,
        ComPlusGroupInApplicationRole,
        ComPlusGroupInPartitionRole,
        ComPlusInterface,
        ComPlusInterfaceProperty,
        ComPlusMethod,
        ComPlusMethodProperty,
        ComPlusPartition,
        ComPlusPartitionProperty,
        ComPlusPartitionRole,
        ComPlusPartitionUser,
        ComPlusRoleForComponent,
        ComPlusRoleForInterface,
        ComPlusRoleForMethod,
        ComPlusSubscription,
        ComPlusSubscriptionProperty,
        ComPlusUserInApplicationRole,
        ComPlusUserInPartitionRole,
    }

    public static partial class ComPlusTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out ComPlusTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(ComPlusTupleDefinitionType type)
        {
            switch (type)
            {
                case ComPlusTupleDefinitionType.ComPlusApplication:
                    return ComPlusTupleDefinitions.ComPlusApplication;

                case ComPlusTupleDefinitionType.ComPlusApplicationProperty:
                    return ComPlusTupleDefinitions.ComPlusApplicationProperty;

                case ComPlusTupleDefinitionType.ComPlusApplicationRole:
                    return ComPlusTupleDefinitions.ComPlusApplicationRole;

                case ComPlusTupleDefinitionType.ComPlusApplicationRoleProperty:
                    return ComPlusTupleDefinitions.ComPlusApplicationRoleProperty;

                case ComPlusTupleDefinitionType.ComPlusAssembly:
                    return ComPlusTupleDefinitions.ComPlusAssembly;

                case ComPlusTupleDefinitionType.ComPlusAssemblyDependency:
                    return ComPlusTupleDefinitions.ComPlusAssemblyDependency;

                case ComPlusTupleDefinitionType.ComPlusComponent:
                    return ComPlusTupleDefinitions.ComPlusComponent;

                case ComPlusTupleDefinitionType.ComPlusComponentProperty:
                    return ComPlusTupleDefinitions.ComPlusComponentProperty;

                case ComPlusTupleDefinitionType.ComPlusGroupInApplicationRole:
                    return ComPlusTupleDefinitions.ComPlusGroupInApplicationRole;

                case ComPlusTupleDefinitionType.ComPlusGroupInPartitionRole:
                    return ComPlusTupleDefinitions.ComPlusGroupInPartitionRole;

                case ComPlusTupleDefinitionType.ComPlusInterface:
                    return ComPlusTupleDefinitions.ComPlusInterface;

                case ComPlusTupleDefinitionType.ComPlusInterfaceProperty:
                    return ComPlusTupleDefinitions.ComPlusInterfaceProperty;

                case ComPlusTupleDefinitionType.ComPlusMethod:
                    return ComPlusTupleDefinitions.ComPlusMethod;

                case ComPlusTupleDefinitionType.ComPlusMethodProperty:
                    return ComPlusTupleDefinitions.ComPlusMethodProperty;

                case ComPlusTupleDefinitionType.ComPlusPartition:
                    return ComPlusTupleDefinitions.ComPlusPartition;

                case ComPlusTupleDefinitionType.ComPlusPartitionProperty:
                    return ComPlusTupleDefinitions.ComPlusPartitionProperty;

                case ComPlusTupleDefinitionType.ComPlusPartitionRole:
                    return ComPlusTupleDefinitions.ComPlusPartitionRole;

                case ComPlusTupleDefinitionType.ComPlusPartitionUser:
                    return ComPlusTupleDefinitions.ComPlusPartitionUser;

                case ComPlusTupleDefinitionType.ComPlusRoleForComponent:
                    return ComPlusTupleDefinitions.ComPlusRoleForComponent;

                case ComPlusTupleDefinitionType.ComPlusRoleForInterface:
                    return ComPlusTupleDefinitions.ComPlusRoleForInterface;

                case ComPlusTupleDefinitionType.ComPlusRoleForMethod:
                    return ComPlusTupleDefinitions.ComPlusRoleForMethod;

                case ComPlusTupleDefinitionType.ComPlusSubscription:
                    return ComPlusTupleDefinitions.ComPlusSubscription;

                case ComPlusTupleDefinitionType.ComPlusSubscriptionProperty:
                    return ComPlusTupleDefinitions.ComPlusSubscriptionProperty;

                case ComPlusTupleDefinitionType.ComPlusUserInApplicationRole:
                    return ComPlusTupleDefinitions.ComPlusUserInApplicationRole;

                case ComPlusTupleDefinitionType.ComPlusUserInPartitionRole:
                    return ComPlusTupleDefinitions.ComPlusUserInPartitionRole;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
