// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using System;
    using WixToolset.Data;

    public enum DependencySymbolDefinitionType
    {
        WixDependency,
        WixDependencyRef,
    }

    public static partial class DependencySymbolDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out DependencySymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(DependencySymbolDefinitionType type)
        {
            switch (type)
            {
                case DependencySymbolDefinitionType.WixDependency:
                    return DependencySymbolDefinitions.WixDependency;

                case DependencySymbolDefinitionType.WixDependencyRef:
                    return DependencySymbolDefinitions.WixDependencyRef;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
