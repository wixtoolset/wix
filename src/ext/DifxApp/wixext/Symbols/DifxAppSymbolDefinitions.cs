// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.DifxApp
{
    using System;
    using WixToolset.Data;

    public enum DifxAppSymbolDefinitionType
    {
        MsiDriverPackages,
    }

    public static partial class DifxAppSymbolDefinitions
    {
        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out DifxAppSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(DifxAppSymbolDefinitionType type)
        {
            switch (type)
            {
                case DifxAppSymbolDefinitionType.MsiDriverPackages:
                    return DifxAppSymbolDefinitions.MsiDriverPackages;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }
    }
}
