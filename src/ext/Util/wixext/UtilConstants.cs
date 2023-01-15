// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using System.Xml.Linq;

    /// <summary>
    /// Constants used by Utility Extension.
    /// </summary>
    internal static class UtilConstants
    {
        internal static readonly XNamespace Namespace = "http://wixtoolset.org/schemas/v4/wxs/util";

        internal static readonly XName BroadcastEnvironmentChange = Namespace + "BroadcastEnvironmentChange";
        internal static readonly XName BroadcastSettingChange = Namespace + "BroadcastSettingChange";
        internal static readonly XName CheckRebootRequired = Namespace + "CheckRebootRequired";
        internal static readonly XName CloseApplicationName = Namespace + "CloseApplication";
        internal static readonly XName EventManifestName = Namespace + "EventManifest";
        internal static readonly XName FileShareName = Namespace + "FileShare";
        internal static readonly XName FileSharePermissionName = Namespace + "FileSharePermission";
        internal static readonly XName GroupName = Namespace + "Group";
        internal static readonly XName GroupRefName = Namespace + "GroupRef";
        internal static readonly XName InternetShortcutName = Namespace + "InternetShortcut";
        internal static readonly XName PerfCounterName = Namespace + "PerfCounter";
        internal static readonly XName PerfCounterManifestName = Namespace + "PerfCounterManifest";
        internal static readonly XName PermissionExName = Namespace + "PermissionEx";
        internal static readonly XName QueryNativeMachine = Namespace + "QueryNativeMachine";
        internal static readonly XName QueryWindowsDriverInfo = Namespace + "QueryWindowsDriverInfo";
        internal static readonly XName QueryWindowsSuiteInfo = Namespace + "QueryWindowsSuiteInfo";
        internal static readonly XName RemoveFolderExName = Namespace + "RemoveFolderEx";
        internal static readonly XName RestartResourceName = Namespace + "RestartResource";
        internal static readonly XName ServiceConfigName = Namespace + "ServiceConfig";
        internal static readonly XName UserName = Namespace + "User";
        internal static readonly XName XmlConfigName = Namespace + "XmlConfig";
        internal static readonly XName XmlFileName = Namespace + "XmlFile";

        internal static readonly string[] FilePermissions = { "Read", "Write", "Append", "ReadExtendedAttributes", "WriteExtendedAttributes", "Execute", null, "ReadAttributes", "WriteAttributes" };
        internal static readonly string[] FolderPermissions = { "Read", "CreateFile", "CreateChild", "ReadExtendedAttributes", "WriteExtendedAttributes", "Traverse", "DeleteChild", "ReadAttributes", "WriteAttributes" };
        internal static readonly string[] GenericPermissions = { "GenericAll", "GenericExecute", "GenericWrite", "GenericRead" };
        internal static readonly string[] RegistryPermissions = { "Read", "Write", "CreateSubkeys", "EnumerateSubkeys", "Notify", "CreateLink" };
        internal static readonly string[] ServicePermissions = { "ServiceQueryConfig", "ServiceChangeConfig", "ServiceQueryStatus", "ServiceEnumerateDependents", "ServiceStart", "ServiceStop", "ServicePauseContinue", "ServiceInterrogate", "ServiceUserDefinedControl" };
        internal static readonly string[] StandardPermissions = { "Delete", "ReadPermission", "ChangePermission", "TakeOwnership", "Synchronize" };
    }
}
