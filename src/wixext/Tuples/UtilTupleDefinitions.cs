// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System;
    using WixToolset.Data;
    using WixToolset.Data.Burn;

    public enum UtilTupleDefinitionType
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
        WixDetectSHA2Support,
        WixFormatFiles,
        WixInternetShortcut,
        WixRemoveFolderEx,
        WixRestartResource,
        WixTouchFile,
        XmlConfig,
        XmlFile,
    }

    public static partial class UtilTupleDefinitions
    {
        public static readonly Version Version = new Version("4.0.0");

        public static IntermediateTupleDefinition ByName(string name)
        {
            if (!Enum.TryParse(name, out UtilTupleDefinitionType type))
            {
                return null;
            }

            return ByType(type);
        }

        public static IntermediateTupleDefinition ByType(UtilTupleDefinitionType type)
        {
            switch (type)
            {
                case UtilTupleDefinitionType.EventManifest:
                    return UtilTupleDefinitions.EventManifest;

                case UtilTupleDefinitionType.FileShare:
                    return UtilTupleDefinitions.FileShare;

                case UtilTupleDefinitionType.FileSharePermissions:
                    return UtilTupleDefinitions.FileSharePermissions;

                case UtilTupleDefinitionType.Group:
                    return UtilTupleDefinitions.Group;

                case UtilTupleDefinitionType.Perfmon:
                    return UtilTupleDefinitions.Perfmon;

                case UtilTupleDefinitionType.PerfmonManifest:
                    return UtilTupleDefinitions.PerfmonManifest;

                case UtilTupleDefinitionType.PerformanceCategory:
                    return UtilTupleDefinitions.PerformanceCategory;

                case UtilTupleDefinitionType.SecureObjects:
                    return UtilTupleDefinitions.SecureObjects;

                case UtilTupleDefinitionType.ServiceConfig:
                    return UtilTupleDefinitions.ServiceConfig;

                case UtilTupleDefinitionType.User:
                    return UtilTupleDefinitions.User;

                case UtilTupleDefinitionType.UserGroup:
                    return UtilTupleDefinitions.UserGroup;

                case UtilTupleDefinitionType.WixCloseApplication:
                    return UtilTupleDefinitions.WixCloseApplication;

                case UtilTupleDefinitionType.WixDetectSHA2Support:
                    return UtilTupleDefinitions.WixDetectSHA2Support;

                case UtilTupleDefinitionType.WixFormatFiles:
                    return UtilTupleDefinitions.WixFormatFiles;

                case UtilTupleDefinitionType.WixInternetShortcut:
                    return UtilTupleDefinitions.WixInternetShortcut;

                case UtilTupleDefinitionType.WixRemoveFolderEx:
                    return UtilTupleDefinitions.WixRemoveFolderEx;

                case UtilTupleDefinitionType.WixRestartResource:
                    return UtilTupleDefinitions.WixRestartResource;

                case UtilTupleDefinitionType.WixTouchFile:
                    return UtilTupleDefinitions.WixTouchFile;

                case UtilTupleDefinitionType.XmlConfig:
                    return UtilTupleDefinitions.XmlConfig;

                case UtilTupleDefinitionType.XmlFile:
                    return UtilTupleDefinitions.XmlFile;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
        }

        static UtilTupleDefinitions()
        {
            WixDetectSHA2Support.AddTag(BurnConstants.BundleExtensionSearchTupleDefinitionTag);
        }
    }
}
