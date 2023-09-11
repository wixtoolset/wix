// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using System;
    using WixToolset.Data;

    public enum ComPlusSymbolDefinitionType
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

    public static partial class ComPlusSymbolDefinitions
    {
        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out ComPlusSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(ComPlusSymbolDefinitionType type)
        {
            switch (type)
            {
                case ComPlusSymbolDefinitionType.ComPlusApplication:
                    return ComPlusSymbolDefinitions.ComPlusApplication;

                case ComPlusSymbolDefinitionType.ComPlusApplicationProperty:
                    return ComPlusSymbolDefinitions.ComPlusApplicationProperty;

                case ComPlusSymbolDefinitionType.ComPlusApplicationRole:
                    return ComPlusSymbolDefinitions.ComPlusApplicationRole;

                case ComPlusSymbolDefinitionType.ComPlusApplicationRoleProperty:
                    return ComPlusSymbolDefinitions.ComPlusApplicationRoleProperty;

                case ComPlusSymbolDefinitionType.ComPlusAssembly:
                    return ComPlusSymbolDefinitions.ComPlusAssembly;

                case ComPlusSymbolDefinitionType.ComPlusAssemblyDependency:
                    return ComPlusSymbolDefinitions.ComPlusAssemblyDependency;

                case ComPlusSymbolDefinitionType.ComPlusComponent:
                    return ComPlusSymbolDefinitions.ComPlusComponent;

                case ComPlusSymbolDefinitionType.ComPlusComponentProperty:
                    return ComPlusSymbolDefinitions.ComPlusComponentProperty;

                case ComPlusSymbolDefinitionType.ComPlusGroupInApplicationRole:
                    return ComPlusSymbolDefinitions.ComPlusGroupInApplicationRole;

                case ComPlusSymbolDefinitionType.ComPlusGroupInPartitionRole:
                    return ComPlusSymbolDefinitions.ComPlusGroupInPartitionRole;

                case ComPlusSymbolDefinitionType.ComPlusInterface:
                    return ComPlusSymbolDefinitions.ComPlusInterface;

                case ComPlusSymbolDefinitionType.ComPlusInterfaceProperty:
                    return ComPlusSymbolDefinitions.ComPlusInterfaceProperty;

                case ComPlusSymbolDefinitionType.ComPlusMethod:
                    return ComPlusSymbolDefinitions.ComPlusMethod;

                case ComPlusSymbolDefinitionType.ComPlusMethodProperty:
                    return ComPlusSymbolDefinitions.ComPlusMethodProperty;

                case ComPlusSymbolDefinitionType.ComPlusPartition:
                    return ComPlusSymbolDefinitions.ComPlusPartition;

                case ComPlusSymbolDefinitionType.ComPlusPartitionProperty:
                    return ComPlusSymbolDefinitions.ComPlusPartitionProperty;

                case ComPlusSymbolDefinitionType.ComPlusPartitionRole:
                    return ComPlusSymbolDefinitions.ComPlusPartitionRole;

                case ComPlusSymbolDefinitionType.ComPlusPartitionUser:
                    return ComPlusSymbolDefinitions.ComPlusPartitionUser;

                case ComPlusSymbolDefinitionType.ComPlusRoleForComponent:
                    return ComPlusSymbolDefinitions.ComPlusRoleForComponent;

                case ComPlusSymbolDefinitionType.ComPlusRoleForInterface:
                    return ComPlusSymbolDefinitions.ComPlusRoleForInterface;

                case ComPlusSymbolDefinitionType.ComPlusRoleForMethod:
                    return ComPlusSymbolDefinitions.ComPlusRoleForMethod;

                case ComPlusSymbolDefinitionType.ComPlusSubscription:
                    return ComPlusSymbolDefinitions.ComPlusSubscription;

                case ComPlusSymbolDefinitionType.ComPlusSubscriptionProperty:
                    return ComPlusSymbolDefinitions.ComPlusSubscriptionProperty;

                case ComPlusSymbolDefinitionType.ComPlusUserInApplicationRole:
                    return ComPlusSymbolDefinitions.ComPlusUserInApplicationRole;

                case ComPlusSymbolDefinitionType.ComPlusUserInPartitionRole:
                    return ComPlusSymbolDefinitions.ComPlusUserInPartitionRole;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
