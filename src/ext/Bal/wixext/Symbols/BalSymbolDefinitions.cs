// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplications
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
        WixPrereqInformation,
        WixStdbaCommandLine,
        WixStdbaOptions,
        WixStdbaOverridableVariable,
        WixPrereqOptions,
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
#pragma warning disable 0612 // obsolete
                case BalSymbolDefinitionType.WixBalBAFactoryAssembly:
                    return BalSymbolDefinitions.WixBalBAFactoryAssembly;
#pragma warning restore 0612

                case BalSymbolDefinitionType.WixBalBAFunctions:
                    return BalSymbolDefinitions.WixBalBAFunctions;

                case BalSymbolDefinitionType.WixBalCondition:
                    return BalSymbolDefinitions.WixBalCondition;

                case BalSymbolDefinitionType.WixBalPackageInfo:
                    return BalSymbolDefinitions.WixBalPackageInfo;

                case BalSymbolDefinitionType.WixPrereqInformation:
                    return BalSymbolDefinitions.WixPrereqInformation;

                case BalSymbolDefinitionType.WixStdbaCommandLine:
                    return BalSymbolDefinitions.WixStdbaCommandLine;

                case BalSymbolDefinitionType.WixStdbaOptions:
                    return BalSymbolDefinitions.WixStdbaOptions;

                case BalSymbolDefinitionType.WixStdbaOverridableVariable:
                    return BalSymbolDefinitions.WixStdbaOverridableVariable;

                case BalSymbolDefinitionType.WixPrereqOptions:
                    return BalSymbolDefinitions.WixPrereqOptions;

                case BalSymbolDefinitionType.WixBalBootstrapperApplication:
                    return BalSymbolDefinitions.WixBalBootstrapperApplication;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        static BalSymbolDefinitions()
        {
#pragma warning disable 0612 // obsolete
            WixBalBAFactoryAssembly.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
#pragma warning restore 0612
            WixBalBAFunctions.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixBalCondition.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixBalPackageInfo.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixPrereqInformation.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixStdbaCommandLine.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixStdbaOptions.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixStdbaOverridableVariable.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
            WixPrereqOptions.AddTag(BurnConstants.BootstrapperApplicationDataSymbolDefinitionTag);
        }
    }
}
