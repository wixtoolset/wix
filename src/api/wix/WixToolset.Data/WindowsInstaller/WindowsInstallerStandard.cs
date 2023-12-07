// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixToolset.Data.Symbols;

    public static class WindowsInstallerStandard
    {
        private static readonly Dictionary<string, WixActionSymbol> standardActionsById;
        private static readonly HashSet<string> standardActionNames;

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

        private static readonly Dictionary<string, string> standardDirectoryNamesById = new Dictionary<string, string>
        {
            ["TARGETDIR"] = "SourceDir",
            ["AdminToolsFolder"] = "Admin",
            ["AppDataFolder"] = "AppData",
            ["CommonAppDataFolder"] = "CommApp",
            ["CommonFilesFolder"] = "CFiles",
            ["CommonFiles64Folder"] = "CFiles64",
            ["CommonFiles6432Folder"] = ".",
            ["DesktopFolder"] = "Desktop",
            ["FavoritesFolder"] = "Favs",
            ["FontsFolder"] = "Fonts",
            ["LocalAppDataFolder"] = "LocalApp",
            ["MyPicturesFolder"] = "Pictures",
            ["NetHoodFolder"] = "NetHood",
            ["PersonalFolder"] = "Personal",
            ["PrintHoodFolder"] = "Printers",
            ["ProgramFilesFolder"] = "PFiles",
            ["ProgramFiles64Folder"] = "PFiles64",
            ["ProgramFiles6432Folder"] = ".",
            ["ProgramMenuFolder"] = "PMenu",
            ["RecentFolder"] = "Recent",
            ["SendToFolder"] = "SendTo",
            ["StartMenuFolder"] = "StrtMenu",
            ["StartupFolder"] = "StartUp",
            ["SystemFolder"] = "System",
            ["System16Folder"] = "System16",
            ["System64Folder"] = "System64",
            ["System6432Folder"] = ".",
            ["TempFolder"] = "Temp",
            ["TemplateFolder"] = "Template",
            ["WindowsFolder"] = "Windows",
        };


        static WindowsInstallerStandard()
        {
            var standardActions = new[]
            {
                // AdminExecuteSequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/LaunchConditions"))    { Action="LaunchConditions",   Sequence=100, SequenceTable=SequenceTable.AdminExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/CostInitialize"))      { Action="CostInitialize",     Sequence=800, SequenceTable=SequenceTable.AdminExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/FileCost"))            { Action="FileCost",           Sequence=900, SequenceTable=SequenceTable.AdminExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/CostFinalize"))        { Action="CostFinalize",       Sequence=1000, SequenceTable=SequenceTable.AdminExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/InstallValidate"))     { Action="InstallValidate",    Sequence=1400, SequenceTable=SequenceTable.AdminExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/InstallInitialize"))   { Action="InstallInitialize",  Sequence=1500, SequenceTable=SequenceTable.AdminExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/InstallAdminPackage")) { Action="InstallAdminPackage",Sequence=3900, SequenceTable=SequenceTable.AdminExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/InstallFiles"))        { Action="InstallFiles",       Sequence=4000, SequenceTable=SequenceTable.AdminExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/PatchFiles"))          { Action="PatchFiles",         Sequence=4090, SequenceTable=SequenceTable.AdminExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminExecuteSequence/InstallFinalize"))     { Action="InstallFinalize",    Sequence=6600, SequenceTable=SequenceTable.AdminExecuteSequence },

                // AdminUISequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminUISequence/LaunchConditions")) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.AdminUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminUISequence/CostInitialize"))   { Action="CostInitialize",   Sequence=800, SequenceTable=SequenceTable.AdminUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminUISequence/FileCost"))         { Action="FileCost",         Sequence=900, SequenceTable=SequenceTable.AdminUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminUISequence/CostFinalize"))     { Action="CostFinalize",     Sequence=1000, SequenceTable=SequenceTable.AdminUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdminUISequence/ExecuteAction"))    { Action="ExecuteAction",    Sequence=1300, SequenceTable=SequenceTable.AdminUISequence },
                
                // AdvertiseExecuteSequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/CostInitialize"))        { Action="CostInitialize",        Sequence=800, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/CostFinalize"))          { Action="CostFinalize",          Sequence=1000, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/InstallValidate"))       { Action="InstallValidate",       Sequence=1400, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/InstallInitialize"))     { Action="InstallInitialize",     Sequence=1500, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/CreateShortcuts"))       { Action="CreateShortcuts",       Sequence=4500, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/RegisterClassInfo"))     { Action="RegisterClassInfo",     Sequence=4600, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/RegisterExtensionInfo")) { Action="RegisterExtensionInfo", Sequence=4700, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/RegisterProgIdInfo"))    { Action="RegisterProgIdInfo",    Sequence=4800, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/RegisterMIMEInfo"))      { Action="RegisterMIMEInfo",      Sequence=4900, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/PublishComponents"))     { Action="PublishComponents",     Sequence=6200, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/MsiPublishAssemblies"))  { Action="MsiPublishAssemblies",  Sequence=6250, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/PublishFeatures"))       { Action="PublishFeatures",       Sequence=6300, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/PublishProduct"))        { Action="PublishProduct",        Sequence=6400, SequenceTable=SequenceTable.AdvertiseExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "AdvertiseExecuteSequence/InstallFinalize"))       { Action="InstallFinalize",       Sequence=6600, SequenceTable=SequenceTable.AdvertiseExecuteSequence },

                // InstallUISequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/FindRelatedProducts"))  { Action="FindRelatedProducts",  Sequence=25, SequenceTable=SequenceTable.InstallUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/AppSearch"))            { Action="AppSearch",            Sequence=50, SequenceTable=SequenceTable.InstallUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/LaunchConditions"))     { Action="LaunchConditions",     Sequence=100, SequenceTable=SequenceTable.InstallUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/CCPSearch"))            { Action="CCPSearch",            Sequence=500, SequenceTable=SequenceTable.InstallUISequence, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/RMCCPSearch"))          { Action="RMCCPSearch",          Sequence=600, SequenceTable=SequenceTable.InstallUISequence, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/ValidateProductID"))    { Action="ValidateProductID",    Sequence=700, SequenceTable=SequenceTable.InstallUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/CostInitialize"))       { Action="CostInitialize",       Sequence=800, SequenceTable=SequenceTable.InstallUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/FileCost"))             { Action="FileCost",             Sequence=900, SequenceTable=SequenceTable.InstallUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/IsolateComponents"))    { Action="IsolateComponents",    Sequence=950, SequenceTable=SequenceTable.InstallUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/CostFinalize"))         { Action="CostFinalize",         Sequence=1000, SequenceTable=SequenceTable.InstallUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/MigrateFeatureStates")) { Action="MigrateFeatureStates", Sequence=1200, SequenceTable=SequenceTable.InstallUISequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallUISequence/ExecuteAction"))        { Action="ExecuteAction",        Sequence=1300, SequenceTable=SequenceTable.InstallUISequence },

                // InstallExecuteSequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/FindRelatedProducts"))      { Action="FindRelatedProducts",      Sequence=25, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/AppSearch"))                { Action="AppSearch",                Sequence=50, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/LaunchConditions"))         { Action="LaunchConditions",         Sequence=100, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/CCPSearch"))                { Action="CCPSearch",                Sequence=500, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RMCCPSearch"))              { Action="RMCCPSearch",              Sequence=600, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/ValidateProductID"))        { Action="ValidateProductID",        Sequence=700, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/CostInitialize"))           { Action="CostInitialize",           Sequence=800, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/FileCost"))                 { Action="FileCost",                 Sequence=900, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/IsolateComponents"))        { Action="IsolateComponents",        Sequence=950, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/CostFinalize"))             { Action="CostFinalize",             Sequence=1000, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/SetODBCFolders"))           { Action="SetODBCFolders",           Sequence=1100, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/MigrateFeatureStates"))     { Action="MigrateFeatureStates",     Sequence=1200, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/InstallValidate"))          { Action="InstallValidate",          Sequence=1400, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/InstallInitialize"))        { Action="InstallInitialize",        Sequence=1500, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/AllocateRegistrySpace"))    { Action="AllocateRegistrySpace",    Sequence=1550, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/ProcessComponents"))        { Action="ProcessComponents",        Sequence=1600, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/UnpublishComponents"))      { Action="UnpublishComponents",      Sequence=1700, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/MsiUnpublishAssemblies"))   { Action="MsiUnpublishAssemblies",   Sequence=1750, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/UnpublishFeatures"))        { Action="UnpublishFeatures",        Sequence=1800, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/StopServices"))             { Action="StopServices",             Sequence=1900, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/DeleteServices"))           { Action="DeleteServices",           Sequence=2000, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/UnregisterComPlus"))        { Action="UnregisterComPlus",        Sequence=2100, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/SelfUnregModules"))         { Action="SelfUnregModules",         Sequence=2200, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/UnregisterTypeLibraries"))  { Action="UnregisterTypeLibraries",  Sequence=2300, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RemoveODBC"))               { Action="RemoveODBC",               Sequence=2400, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/UnregisterFonts"))          { Action="UnregisterFonts",          Sequence=2500, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RemoveRegistryValues"))     { Action="RemoveRegistryValues",     Sequence=2600, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/UnregisterClassInfo"))      { Action="UnregisterClassInfo",      Sequence=2700, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/UnregisterExtensionInfo"))  { Action="UnregisterExtensionInfo",  Sequence=2800, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/UnregisterProgIdInfo"))     { Action="UnregisterProgIdInfo",     Sequence=2900, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/UnregisterMIMEInfo"))       { Action="UnregisterMIMEInfo",       Sequence=3000, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RemoveIniValues"))          { Action="RemoveIniValues",          Sequence=3100, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RemoveShortcuts"))          { Action="RemoveShortcuts",          Sequence=3200, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RemoveEnvironmentStrings")) { Action="RemoveEnvironmentStrings", Sequence=3300, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RemoveDuplicateFiles"))     { Action="RemoveDuplicateFiles",     Sequence=3400, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RemoveFiles"))              { Action="RemoveFiles",              Sequence=3500, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RemoveFolders"))            { Action="RemoveFolders",            Sequence=3600, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/CreateFolders"))            { Action="CreateFolders",            Sequence=3700, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/MoveFiles"))                { Action="MoveFiles",                Sequence=3800, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/InstallFiles"))             { Action="InstallFiles",             Sequence=4000, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/PatchFiles"))               { Action="PatchFiles",               Sequence=4090, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/DuplicateFiles"))           { Action="DuplicateFiles",           Sequence=4210, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/BindImage"))                { Action="BindImage",                Sequence=4300, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/CreateShortcuts"))          { Action="CreateShortcuts",          Sequence=4500, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RegisterClassInfo"))        { Action="RegisterClassInfo",        Sequence=4600, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RegisterExtensionInfo"))    { Action="RegisterExtensionInfo",    Sequence=4700, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RegisterProgIdInfo"))       { Action="RegisterProgIdInfo",       Sequence=4800, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RegisterMIMEInfo"))         { Action="RegisterMIMEInfo",         Sequence=4900, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/WriteRegistryValues"))      { Action="WriteRegistryValues",      Sequence=5000, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/WriteIniValues"))           { Action="WriteIniValues",           Sequence=5100, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/WriteEnvironmentStrings"))  { Action="WriteEnvironmentStrings",  Sequence=5200, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RegisterFonts"))            { Action="RegisterFonts",            Sequence=5300, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/InstallODBC"))              { Action="InstallODBC",              Sequence=5400, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RegisterTypeLibraries"))    { Action="RegisterTypeLibraries",    Sequence=5500, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/SelfRegModules"))           { Action="SelfRegModules",           Sequence=5600, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RegisterComPlus"))          { Action="RegisterComPlus",          Sequence=5700, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/InstallServices"))          { Action="InstallServices",          Sequence=5800, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/MsiConfigureServices"))     { Action="MsiConfigureServices",     Sequence=5850, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT>=600" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/StartServices"))            { Action="StartServices",            Sequence=5900, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RegisterUser"))             { Action="RegisterUser",             Sequence=6000, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/RegisterProduct"))          { Action="RegisterProduct",          Sequence=6100, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/PublishComponents"))        { Action="PublishComponents",        Sequence=6200, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/MsiPublishAssemblies"))     { Action="MsiPublishAssemblies",     Sequence=6250, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/PublishFeatures"))          { Action="PublishFeatures",          Sequence=6300, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/PublishProduct"))           { Action="PublishProduct",           Sequence=6400, SequenceTable=SequenceTable.InstallExecuteSequence },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/InstallExecute"))           { Action="InstallExecute",           Sequence=6500, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/InstallExecuteAgain"))      { Action="InstallExecuteAgain",      Sequence=6550, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Virtual, "InstallExecuteSequence/InstallFinalize"))          { Action="InstallFinalize",          Sequence=6600, SequenceTable=SequenceTable.InstallExecuteSequence },
            };

            standardActionNames = new HashSet<string>(standardActions.Select(a => a.Action));
            standardActionsById = standardActions.ToDictionary(a => a.Id.Id);
        }

        /// <summary>
        /// Find out if an action is a standard action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <returns>true if the action is standard, false otherwise.</returns>
        public static bool IsStandardAction(string actionName)
        {
            return standardActionNames.Contains(actionName);
        }

        /// <summary>
        /// Standard actions.
        /// </summary>
        public static IReadOnlyCollection<WixActionSymbol> StandardActions()
        {
            return standardActionsById.Values;
        }

        /// <summary>
        /// Standard directory identifiers.
        /// </summary>
        public static IReadOnlyCollection<string> StandardDirectoryIds()
        {
            return standardDirectoryNamesById.Keys;
        }

        /// <summary>
        /// Gets the platform specific directory id for a directory. Most directories are not platform
        /// specific and return themselves.
        /// </summary>
        /// <param name="directoryId">Directory id to get platform specific.</param>
        /// <param name="platform">Platform to use.</param>
        /// <returns>Platform specific directory id.</returns>
        public static string GetPlatformSpecificDirectoryId(string directoryId, Platform platform)
        {
            switch (directoryId)
            {
                case "CommonFiles6432Folder":
                    return platform == Platform.X86 ? "CommonFilesFolder" : "CommonFiles64Folder";

                case "ProgramFiles6432Folder":
                    return platform == Platform.X86 ? "ProgramFilesFolder" : "ProgramFiles64Folder";

                case "System6432Folder":
                    return platform == Platform.X86 ? "SystemFolder" : "System64Folder";

                default:
                    return directoryId;
            }
        }

        /// <summary>
        /// Find out if a directory is a standard directory.
        /// </summary>
        /// <param name="directoryId">Name of the directory.</param>
        /// <returns>true if the directory is standard, false otherwise.</returns>
        public static bool IsStandardDirectory(string directoryId)
        {
            return standardDirectoryNamesById.ContainsKey(directoryId);
        }

        /// <summary>
        /// Find out if a property is a standard property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>true if a property is standard, false otherwise.</returns>
        public static bool IsStandardProperty(string propertyName)
        {
            return standardProperties.Contains(propertyName);
        }

        /// <summary>
        /// Try to get standard action by id.
        /// </summary>
        public static bool TryGetStandardAction(string id, out WixActionSymbol standardAction)
        {
            return standardActionsById.TryGetValue(id, out standardAction);
        }

        /// <summary>
        /// Try to get standard action by sequence and action name.
        /// </summary>
        public static bool TryGetStandardAction(string sequenceName, string actioname, out WixActionSymbol standardAction)
        {
            return standardActionsById.TryGetValue(String.Concat(sequenceName, "/", actioname), out standardAction);
        }

        /// <summary>
        /// Try to get standard directory name by id.
        /// </summary>
        public static bool TryGetStandardDirectoryName(string directoryId, out string name)
        {
           return standardDirectoryNamesById.TryGetValue(directoryId, out name);
        }
    }
}
