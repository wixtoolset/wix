// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace ForTestingUseOnly
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Burn;

    public enum ForTestingUseOnlySymbolDefinitionType
    {
        ForTestingUseOnlyBundle,
    }

    public static partial class ForTestingUseOnlySymbolDefinitions
    {
        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out ForTestingUseOnlySymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(ForTestingUseOnlySymbolDefinitionType type)
        {
            switch (type)
            {
                case ForTestingUseOnlySymbolDefinitionType.ForTestingUseOnlyBundle:
                    return ForTestingUseOnlySymbolDefinitions.ForTestingUseOnlyBundle;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        static ForTestingUseOnlySymbolDefinitions()
        {
            ForTestingUseOnlyBundle.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
        }
    }
}
