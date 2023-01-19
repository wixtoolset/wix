// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Burn;

    public enum BalSymbolDefinitionType
    {
        WixBalBAFactoryAssembly,
        WixBalBAFunctions,
        WixBalCondition,
        WixBalPackageInfo,
        WixDncOptions,
        WixMbaPrereqInformation,
        WixStdbaCommandLine,
        WixStdbaOptions,
        WixStdbaOverridableVariable,
        WixMbaPrereqOptions,
        WixBalBootstrapperApplication,
    }

    public static partial class BalSymbolDefinitions
    {
        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out BalSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(BalSymbolDefinitionType type)
        {
            switch (type)
            {
                case BalSymbolDefinitionType.WixBalBAFactoryAssembly:
                    return BalSymbolDefinitions.WixBalBAFactoryAssembly;

                case BalSymbolDefinitionType.WixBalBAFunctions:
                    return BalSymbolDefinitions.WixBalBAFunctions;

                case BalSymbolDefinitionType.WixBalCondition:
                    return BalSymbolDefinitions.WixBalCondition;

                case BalSymbolDefinitionType.WixBalPackageInfo:
                    return BalSymbolDefinitions.WixBalPackageInfo;

                case BalSymbolDefinitionType.WixDncOptions:
                    return BalSymbolDefinitions.WixDncOptions;

                case BalSymbolDefinitionType.WixMbaPrereqInformation:
                    return BalSymbolDefinitions.WixMbaPrereqInformation;

                case BalSymbolDefinitionType.WixStdbaCommandLine:
                    return BalSymbolDefinitions.WixStdbaCommandLine;

                case BalSymbolDefinitionType.WixStdbaOptions:
                    return BalSymbolDefinitions.WixStdbaOptions;

                case BalSymbolDefinitionType.WixStdbaOverridableVariable:
                    return BalSymbolDefinitions.WixStdbaOverridableVariable;

                case BalSymbolDefinitionType.WixMbaPrereqOptions:
                    return BalSymbolDefinitions.WixMbaPrereqOptions;

                case BalSymbolDefinitionType.WixBalBootstrapperApplication:
                    return BalSymbolDefinitions.WixBalBootstrapperApplication;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        static BalSymbolDefinitions()
        {
            WixBalBAFactoryAssembly.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixBalBAFunctions.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixBalCondition.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixBalPackageInfo.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixDncOptions.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixMbaPrereqInformation.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixStdbaCommandLine.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixStdbaOptions.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixStdbaOverridableVariable.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixMbaPrereqOptions.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
        }
    }
}
