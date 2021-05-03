// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Burn;

    public enum UtilSymbolDefinitionType
    {
        EventManifest,
        FileShare,
        FileSharePermissions,
        Group,
        Perfmon,
        PerfmonManifest,
        PerformanceCategory,
        SecureObjects,
        ServiceConfig,
        User,
        UserGroup,
        WixCloseApplication,
        WixFormatFiles,
        WixInternetShortcut,
        WixRemoveFolderEx,
        WixRemoveRegistryKeyEx,
        WixRestartResource,
        WixTouchFile,
        WixWindowsFeatureSearch,
        XmlConfig,
        XmlFile,
    }

    public static partial class UtilSymbolDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateSymbolDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out UtilSymbolDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateSymbolDefinition ByType(UtilSymbolDefinitionType type)
        {
            switch (type)
            {
                case UtilSymbolDefinitionType.EventManifest:
                    return UtilSymbolDefinitions.EventManifest;

                case UtilSymbolDefinitionType.FileShare:
                    return UtilSymbolDefinitions.FileShare;

                case UtilSymbolDefinitionType.FileSharePermissions:
                    return UtilSymbolDefinitions.FileSharePermissions;

                case UtilSymbolDefinitionType.Group:
                    return UtilSymbolDefinitions.Group;

                case UtilSymbolDefinitionType.Perfmon:
                    return UtilSymbolDefinitions.Perfmon;

                case UtilSymbolDefinitionType.PerfmonManifest:
                    return UtilSymbolDefinitions.PerfmonManifest;

                case UtilSymbolDefinitionType.PerformanceCategory:
                    return UtilSymbolDefinitions.PerformanceCategory;

                case UtilSymbolDefinitionType.SecureObjects:
                    return UtilSymbolDefinitions.SecureObjects;

                case UtilSymbolDefinitionType.ServiceConfig:
                    return UtilSymbolDefinitions.ServiceConfig;

                case UtilSymbolDefinitionType.User:
                    return UtilSymbolDefinitions.User;

                case UtilSymbolDefinitionType.UserGroup:
                    return UtilSymbolDefinitions.UserGroup;

                case UtilSymbolDefinitionType.WixCloseApplication:
                    return UtilSymbolDefinitions.WixCloseApplication;

                case UtilSymbolDefinitionType.WixFormatFiles:
                    return UtilSymbolDefinitions.WixFormatFiles;

                case UtilSymbolDefinitionType.WixInternetShortcut:
                    return UtilSymbolDefinitions.WixInternetShortcut;

                case UtilSymbolDefinitionType.WixRemoveFolderEx:
                    return UtilSymbolDefinitions.WixRemoveFolderEx;

                case UtilSymbolDefinitionType.WixRemoveRegistryKeyEx:
                    return UtilSymbolDefinitions.WixRemoveRegistryKeyEx;
                    
                case UtilSymbolDefinitionType.WixRestartResource:
                    return UtilSymbolDefinitions.WixRestartResource;

                case UtilSymbolDefinitionType.WixTouchFile:
                    return UtilSymbolDefinitions.WixTouchFile;

                case UtilSymbolDefinitionType.WixWindowsFeatureSearch:
                    return UtilSymbolDefinitions.WixWindowsFeatureSearch;

                case UtilSymbolDefinitionType.XmlConfig:
                    return UtilSymbolDefinitions.XmlConfig;

                case UtilSymbolDefinitionType.XmlFile:
                    return UtilSymbolDefinitions.XmlFile;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        static UtilSymbolDefinitions()
        {
            WixWindowsFeatureSearch.AddTag(BurnConstants.BundleExtensionSearchSymbolDefinitionTag);
        }
    }
}
