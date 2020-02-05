// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data.Tuples;

    public static class WindowsInstallerStandard
    {
        private static readonly Dictionary<string, WixActionTuple> standardActionsById;
        private static readonly HashSet<string> standardActionNames;

        private static readonly HashSet<string> standardDirectories = new HashSet<string>
        {
            "TARGETDIR",
            "AdminToolsFolder",
            "AppDataFolder",
            "CommonAppDataFolder",
            "CommonFilesFolder",
            "DesktopFolder",
            "FavoritesFolder",
            "FontsFolder",
            "LocalAppDataFolder",
            "MyPicturesFolder",
            "PersonalFolder",
            "ProgramFilesFolder",
            "ProgramMenuFolder",
            "SendToFolder",
            "StartMenuFolder",
            "StartupFolder",
            "System16Folder",
            "SystemFolder",
            "TempFolder",
            "TemplateFolder",
            "WindowsFolder",
            "CommonFiles64Folder",
            "ProgramFiles64Folder",
            "System64Folder",
            "NetHoodFolder",
            "PrintHoodFolder",
            "RecentFolder",
            "WindowsVolume",
        };

        /// <summary>
        /// References: 
        /// Title:   Property Reference [Windows Installer]: 
        /// URL:     http://msdn.microsoft.com/library/en-us/msi/setup/property_reference.asp
        /// </summary>
        private static readonly HashSet<string> standardProperties = new HashSet<string>
        {
            "~", // REG_MULTI_SZ/NULL marker
            "ACTION",
            "ADDDEFAULT",
            "ADDLOCAL",
            "ADDDSOURCE",
            "AdminProperties",
            "AdminUser",
            "ADVERTISE",
            "AFTERREBOOT",
            "AllowProductCodeMismatches",
            "AllowProductVersionMajorMismatches",
            "ALLUSERS",
            "Alpha",
            "ApiPatchingSymbolFlags",
            "ARPAUTHORIZEDCDFPREFIX",
            "ARPCOMMENTS",
            "ARPCONTACT",
            "ARPHELPLINK",
            "ARPHELPTELEPHONE",
            "ARPINSTALLLOCATION",
            "ARPNOMODIFY",
            "ARPNOREMOVE",
            "ARPNOREPAIR",
            "ARPPRODUCTIONICON",
            "ARPREADME",
            "ARPSIZE",
            "ARPSYSTEMCOMPONENT",
            "ARPULRINFOABOUT",
            "ARPURLUPDATEINFO",
            "AVAILABLEFREEREG",
            "BorderSize",
            "BorderTop",
            "CaptionHeight",
            "CCP_DRIVE",
            "ColorBits",
            "COMPADDLOCAL",
            "COMPADDSOURCE",
            "COMPANYNAME",
            "ComputerName",
            "CostingComplete",
            "Date",
            "DefaultUIFont",
            "DISABLEADVTSHORTCUTS",
            "DISABLEMEDIA",
            "DISABLEROLLBACK",
            "DiskPrompt",
            "DontRemoveTempFolderWhenFinished",
            "EnableUserControl",
            "EXECUTEACTION",
            "EXECUTEMODE",
            "FASTOEM",
            "FILEADDDEFAULT",
            "FILEADDLOCAL",
            "FILEADDSOURCE",
            "IncludeWholeFilesOnly",
            "Installed",
            "INSTALLLEVEL",
            "Intel",
            "Intel64",
            "IsAdminPackage",
            "LeftUnit",
            "LIMITUI",
            "ListOfPatchGUIDsToReplace",
            "ListOfTargetProductCode",
            "LOGACTION",
            "LogonUser",
            "Manufacturer",
            "MEDIAPACKAGEPATH",
            "MediaSourceDir",
            "MinimumRequiredMsiVersion",
            "MsiAMD64",
            "MSIAPRSETTINGSIDENTIFIER",
            "MSICHECKCRCS",
            "MSIDISABLERMRESTART",
            "MSIENFORCEUPGRADECOMPONENTRULES",
            "MSIFASTINSTALL",
            "MsiFileToUseToCreatePatchTables",
            "MsiHiddenProperties",
            "MSIINSTALLPERUSER",
            "MSIINSTANCEGUID",
            "MsiLogFileLocation",
            "MsiLogging",
            "MsiNetAssemblySupport",
            "MSINEWINSTANCE",
            "MSINODISABLEMEDIA",
            "MsiNTProductType",
            "MsiNTSuiteBackOffice",
            "MsiNTSuiteDataCenter",
            "MsiNTSuiteEnterprise",
            "MsiNTSuiteSmallBusiness",
            "MsiNTSuiteSmallBusinessRestricted",
            "MsiNTSuiteWebServer",
            "MsiNTSuitePersonal",
            "MsiPatchRemovalList",
            "MSIPATCHREMOVE",
            "MSIRESTARTMANAGERCONTROL",
            "MsiRestartManagerSessionKey",
            "MSIRMSHUTDOWN",
            "MsiRunningElevated",
            "MsiUIHideCancel",
            "MsiUIProgressOnly",
            "MsiUISourceResOnly",
            "MsiSystemRebootPending",
            "MsiWin32AssemblySupport",
            "NOCOMPANYNAME",
            "NOUSERNAME",
            "OLEAdvtSupport",
            "OptimizePatchSizeForLargeFiles",
            "OriginalDatabase",
            "OutOfDiskSpace",
            "OutOfNoRbDiskSpace",
            "ParentOriginalDatabase",
            "ParentProductCode",
            "PATCH",
            "PATCH_CACHE_DIR",
            "PATCH_CACHE_ENABLED",
            "PatchGUID",
            "PATCHNEWPACKAGECODE",
            "PATCHNEWSUMMARYCOMMENTS",
            "PATCHNEWSUMMARYSUBJECT",
            "PatchOutputPath",
            "PatchSourceList",
            "PhysicalMemory",
            "PIDKEY",
            "PIDTemplate",
            "Preselected",
            "PRIMARYFOLDER",
            "PrimaryVolumePath",
            "PrimaryVolumeSpaceAvailable",
            "PrimaryVolumeSpaceRemaining",
            "PrimaryVolumeSpaceRequired",
            "Privileged",
            "ProductCode",
            "ProductID",
            "ProductLanguage",
            "ProductName",
            "ProductState",
            "ProductVersion",
            "PROMPTROLLBACKCOST",
            "REBOOT",
            "REBOOTPROMPT",
            "RedirectedDllSupport",
            "REINSTALL",
            "REINSTALLMODE",
            "RemoveAdminTS",
            "REMOVE",
            "ReplacedInUseFiles",
            "RestrictedUserControl",
            "RESUME",
            "RollbackDisabled",
            "ROOTDRIVE",
            "ScreenX",
            "ScreenY",
            "SecureCustomProperties",
            "ServicePackLevel",
            "ServicePackLevelMinor",
            "SEQUENCE",
            "SharedWindows",
            "ShellAdvtSupport",
            "SHORTFILENAMES",
            "SourceDir",
            "SOURCELIST",
            "SystemLanguageID",
            "TARGETDIR",
            "TerminalServer",
            "TextHeight",
            "Time",
            "TRANSFORMS",
            "TRANSFORMSATSOURCE",
            "TRANSFORMSSECURE",
            "TTCSupport",
            "UILevel",
            "UpdateStarted",
            "UpgradeCode",
            "UPGRADINGPRODUCTCODE",
            "UserLanguageID",
            "USERNAME",
            "UserSID",
            "Version9X",
            "VersionDatabase",
            "VersionMsi",
            "VersionNT",
            "VersionNT64",
            "VirtualMemory",
            "WindowsBuild",
            "WindowsVolume",
        };

        static WindowsInstallerStandard()
        {
            var standardActions = new[]
            {
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/InstallInitialize")) { Action="InstallInitialize", Sequence=1500, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/InstallInitialize")) { Action="InstallInitialize", Sequence=1500, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/InstallInitialize")) { Action="InstallInitialize", Sequence=1500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/InstallExecute")) { Action="InstallExecute", Sequence=6500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="NOT Installed" },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/InstallExecuteAgain")) { Action="InstallExecuteAgain", Sequence=6550, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="NOT Installed" },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/InstallFinalize")) { Action="InstallFinalize", Sequence=6600, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/InstallFinalize")) { Action="InstallFinalize", Sequence=6600, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/InstallFinalize")) { Action="InstallFinalize", Sequence=6600, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/InstallFiles")) { Action="InstallFiles", Sequence=4000, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/InstallFiles")) { Action="InstallFiles", Sequence=4000, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/InstallAdminPackage")) { Action="InstallAdminPackage", Sequence=3900, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/FileCost")) { Action="FileCost", Sequence=900, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminUISequence/FileCost")) { Action="FileCost", Sequence=900, SequenceTable=SequenceTable.AdminUISequence, Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/FileCost")) { Action="FileCost", Sequence=900, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/FileCost")) { Action="FileCost", Sequence=900, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/CostInitialize")) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.AdminExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminUISequence/CostInitialize")) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.AdminUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/CostInitialize")) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/CostInitialize")) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/CostInitialize")) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/CostFinalize")) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.AdminExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminUISequence/CostFinalize")) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.AdminUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/CostFinalize")) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/CostFinalize")) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/CostFinalize")) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/InstallValidate")) { Action="InstallValidate", Sequence=1400, SequenceTable=SequenceTable.AdminExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/InstallValidate")) { Action="InstallValidate", Sequence=1400, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/InstallValidate")) { Action="InstallValidate", Sequence=1400, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminUISequence/ExecuteAction")) { Action="ExecuteAction", Sequence=1300, SequenceTable=SequenceTable.AdminUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/ExecuteAction")) { Action="ExecuteAction", Sequence=1300, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/CreateShortcuts")) { Action="CreateShortcuts", Sequence=4500, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/CreateShortcuts")) { Action="CreateShortcuts", Sequence=4500, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/MsiPublishAssemblies")) { Action="MsiPublishAssemblies", Sequence=6250, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/MsiPublishAssemblies")) { Action="MsiPublishAssemblies", Sequence=6250, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/PublishComponents")) { Action="PublishComponents", Sequence=6200, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/PublishComponents")) { Action="PublishComponents", Sequence=6200, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/PublishFeatures")) { Action="PublishFeatures", Sequence=6300, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/PublishFeatures")) { Action="PublishFeatures", Sequence=6300, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/PublishProduct")) { Action="PublishProduct", Sequence=6400, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/PublishProduct")) { Action="PublishProduct", Sequence=6400, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/RegisterClassInfo")) { Action="RegisterClassInfo", Sequence=4600, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RegisterClassInfo")) { Action="RegisterClassInfo", Sequence=4600, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/RegisterExtensionInfo")) { Action="RegisterExtensionInfo", Sequence=4700, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RegisterExtensionInfo")) { Action="RegisterExtensionInfo", Sequence=4700, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/RegisterMIMEInfo")) { Action="RegisterMIMEInfo", Sequence=4900, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RegisterMIMEInfo")) { Action="RegisterMIMEInfo", Sequence=4900, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdvertiseExecuteSequence/RegisterProgIdInfo")) { Action="RegisterProgIdInfo", Sequence=4800, SequenceTable=SequenceTable.AdvertiseExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RegisterProgIdInfo")) { Action="RegisterProgIdInfo", Sequence=4800, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/AllocateRegistrySpace")) { Action="AllocateRegistrySpace", Sequence=1550, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/AppSearch")) { Action="AppSearch", Sequence=50, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/AppSearch")) { Action="AppSearch", Sequence=50, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/BindImage")) { Action="BindImage", Sequence=4300, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/CreateFolders")) { Action="CreateFolders", Sequence=3700, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/DuplicateFiles")) { Action="DuplicateFiles", Sequence=4210, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/FindRelatedProducts")) { Action="FindRelatedProducts", Sequence=25, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/FindRelatedProducts")) { Action="FindRelatedProducts", Sequence=25, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/InstallODBC")) { Action="InstallODBC", Sequence=5400, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/InstallServices")) { Action="InstallServices", Sequence=5800, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT" },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/MsiConfigureServices")) { Action="MsiConfigureServices", Sequence=5850, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT>=600" },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/IsolateComponents")) { Action="IsolateComponents", Sequence=950, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/IsolateComponents")) { Action="IsolateComponents", Sequence=950, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminUISequence/LaunchConditions")) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.AdminUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/LaunchConditions")) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.AdminExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/LaunchConditions")) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/LaunchConditions")) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/MigrateFeatureStates")) { Action="MigrateFeatureStates", Sequence=1200, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/MigrateFeatureStates")) { Action="MigrateFeatureStates", Sequence=1200, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/MoveFiles")) { Action="MoveFiles", Sequence=3800, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "AdminExecuteSequence/PatchFiles")) { Action="PatchFiles", Sequence=4090, SequenceTable=SequenceTable.AdminExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/PatchFiles")) { Action="PatchFiles", Sequence=4090, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/ProcessComponents")) { Action="ProcessComponents", Sequence=1600, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RegisterComPlus")) { Action="RegisterComPlus", Sequence=5700, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RegisterFonts")) { Action="RegisterFonts", Sequence=5300, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RegisterProduct")) { Action="RegisterProduct", Sequence=6100, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RegisterTypeLibraries")) { Action="RegisterTypeLibraries", Sequence=5500, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RegisterUser")) { Action="RegisterUser", Sequence=6000, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RemoveDuplicateFiles")) { Action="RemoveDuplicateFiles", Sequence=3400, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RemoveEnvironmentStrings")) { Action="RemoveEnvironmentStrings", Sequence=3300, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RemoveFiles")) { Action="RemoveFiles", Sequence=3500, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RemoveFolders")) { Action="RemoveFolders", Sequence=3600, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RemoveIniValues")) { Action="RemoveIniValues", Sequence=3100, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RemoveODBC")) { Action="RemoveODBC", Sequence=2400, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RemoveRegistryValues")) { Action="RemoveRegistryValues", Sequence=2600, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RemoveShortcuts")) { Action="RemoveShortcuts", Sequence=3200, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/SelfRegModules")) { Action="SelfRegModules", Sequence=5600, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/SelfUnregModules")) { Action="SelfUnregModules", Sequence=2200, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/SetODBCFolders")) { Action="SetODBCFolders", Sequence=1100, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/CCPSearch")) { Action="CCPSearch", Sequence=500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="NOT Installed" },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/CCPSearch")) { Action="CCPSearch", Sequence=500, SequenceTable=SequenceTable.InstallUISequence, Overridable = true, Condition="NOT Installed" },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/DeleteServices")) { Action="DeleteServices", Sequence=2000, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT" },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/RMCCPSearch")) { Action="RMCCPSearch", Sequence=600, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="NOT Installed" },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/RMCCPSearch")) { Action="RMCCPSearch", Sequence=600, SequenceTable=SequenceTable.InstallUISequence, Overridable = true, Condition="NOT Installed" },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/StartServices")) { Action="StartServices", Sequence=5900, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT" },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/StopServices")) { Action="StopServices", Sequence=1900, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT" },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/MsiUnpublishAssemblies")) { Action="MsiUnpublishAssemblies", Sequence=1750, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/UnpublishComponents")) { Action="UnpublishComponents", Sequence=1700, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/UnpublishFeatures")) { Action="UnpublishFeatures", Sequence=1800, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/UnregisterClassInfo")) { Action="UnregisterClassInfo", Sequence=2700, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/UnregisterComPlus")) { Action="UnregisterComPlus", Sequence=2100, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/UnregisterExtensionInfo")) { Action="UnregisterExtensionInfo", Sequence=2800, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/UnregisterFonts")) { Action="UnregisterFonts", Sequence=2500, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/UnregisterMIMEInfo")) { Action="UnregisterMIMEInfo", Sequence=3000, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/UnregisterProgIdInfo")) { Action="UnregisterProgIdInfo", Sequence=2900, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/UnregisterTypeLibraries")) { Action="UnregisterTypeLibraries", Sequence=2300, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallUISequence/ValidateProductID")) { Action="ValidateProductID", Sequence=700, SequenceTable=SequenceTable.InstallUISequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/ValidateProductID")) { Action="ValidateProductID", Sequence=700, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },

                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/WriteEnvironmentStrings")) { Action="WriteEnvironmentStrings", Sequence=5200, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/WriteIniValues")) { Action="WriteIniValues", Sequence=5100, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
                new WixActionTuple(null, new Identifier(AccessModifier.Public, "InstallExecuteSequence/WriteRegistryValues")) { Action="WriteRegistryValues", Sequence=5000, SequenceTable=SequenceTable.InstallExecuteSequence , Overridable = true },
            };

            standardActionNames = new HashSet<string>(standardActions.Select(a => a.Action));
            standardActionsById = standardActions.ToDictionary(a => a.Id.Id);
        }

        /// <summary>
        /// Find out if an action is a standard action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <returns>true if the action is standard, false otherwise.</returns>
        public static bool IsStandardAction(string actionName) => standardActionNames.Contains(actionName);

        /// <summary>
        /// Standard actions.
        /// </summary>
        public static IEnumerable<WixActionTuple> StandardActions() => standardActionsById.Values;

        /// <summary>
        /// Find out if a directory is a standard directory.
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns>true if the directory is standard, false otherwise.</returns>
        public static bool IsStandardDirectory(string directoryName) => standardDirectories.Contains(directoryName);

        /// <summary>
        /// Find out if a property is a standard property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>true if a property is standard, false otherwise.</returns>
        public static bool IsStandardProperty(string propertyName) => standardProperties.Contains(propertyName);

        /// <summary>
        /// Try to get standard action by id.
        /// </summary>
        public static bool TryGetStandardAction(string id, out WixActionTuple standardAction) => standardActionsById.TryGetValue(id, out standardAction);

        /// <summary>
        /// Try to get standard action by sequence and action name.
        /// </summary>
        public static bool TryGetStandardAction(string sequenceName, string actioname, out WixActionTuple standardAction) => standardActionsById.TryGetValue(String.Concat(sequenceName, "/", actioname), out standardAction);
    }
}
