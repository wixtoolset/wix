// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using System;
    using WixToolset.Data;

    public enum DependencyTupleDefinitionType
    {
        WixDependency,
        WixDependencyRef,
    }

    public static partial class DependencyTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out DependencyTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(DependencyTupleDefinitionType type)
        {
            switch (type)
            {
                case DependencyTupleDefinitionType.WixDependency:
                    return DependencyTupleDefinitions.WixDependency;

                case DependencyTupleDefinitionType.WixDependencyRef:
                    return DependencyTupleDefinitions.WixDependencyRef;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
