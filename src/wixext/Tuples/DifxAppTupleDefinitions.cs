// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.DifxApp
{
    using System;
    using WixToolset.Data;

    public enum DifxAppTupleDefinitionType
    {
        MsiDriverPackages,
    }

    public static partial class DifxAppTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out DifxAppTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(DifxAppTupleDefinitionType type)
        {
            switch (type)
            {
                case DifxAppTupleDefinitionType.MsiDriverPackages:
                    return DifxAppTupleDefinitions.MsiDriverPackages;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
