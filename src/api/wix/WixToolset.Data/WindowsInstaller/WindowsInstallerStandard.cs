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
        private static readonly Dictionary<string, DirectorySymbol> standardDirectoriesById;

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
                // AdminExecuteSequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/LaunchConditions"))    { Action="LaunchConditions",   Sequence=100, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/CostInitialize"))      { Action="CostInitialize",     Sequence=800, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/FileCost"))            { Action="FileCost",           Sequence=900, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/CostFinalize"))        { Action="CostFinalize",       Sequence=1000, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/InstallValidate"))     { Action="InstallValidate",    Sequence=1400, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/InstallInitialize"))   { Action="InstallInitialize",  Sequence=1500, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/InstallAdminPackage")) { Action="InstallAdminPackage",Sequence=3900, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/InstallFiles"))        { Action="InstallFiles",       Sequence=4000, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/PatchFiles"))          { Action="PatchFiles",         Sequence=4090, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminExecuteSequence/InstallFinalize"))     { Action="InstallFinalize",    Sequence=6600, SequenceTable=SequenceTable.AdminExecuteSequence, Overridable = true },

                // AdminUISequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminUISequence/LaunchConditions")) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.AdminUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminUISequence/CostInitialize"))   { Action="CostInitialize",   Sequence=800, SequenceTable=SequenceTable.AdminUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminUISequence/FileCost"))         { Action="FileCost",         Sequence=900, SequenceTable=SequenceTable.AdminUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminUISequence/CostFinalize"))     { Action="CostFinalize",     Sequence=1000, SequenceTable=SequenceTable.AdminUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdminUISequence/ExecuteAction"))    { Action="ExecuteAction",    Sequence=1300, SequenceTable=SequenceTable.AdminUISequence, Overridable = true },
                
                // AdvertiseExecuteSequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/CostInitialize"))        { Action="CostInitialize",        Sequence=800, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/CostFinalize"))          { Action="CostFinalize",          Sequence=1000, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/InstallValidate"))       { Action="InstallValidate",       Sequence=1400, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/InstallInitialize"))     { Action="InstallInitialize",     Sequence=1500, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/CreateShortcuts"))       { Action="CreateShortcuts",       Sequence=4500, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/RegisterClassInfo"))     { Action="RegisterClassInfo",     Sequence=4600, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/RegisterExtensionInfo")) { Action="RegisterExtensionInfo", Sequence=4700, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/RegisterProgIdInfo"))    { Action="RegisterProgIdInfo",    Sequence=4800, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/RegisterMIMEInfo"))      { Action="RegisterMIMEInfo",      Sequence=4900, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/PublishComponents"))     { Action="PublishComponents",     Sequence=6200, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/MsiPublishAssemblies"))  { Action="MsiPublishAssemblies",  Sequence=6250, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/PublishFeatures"))       { Action="PublishFeatures",       Sequence=6300, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/PublishProduct"))        { Action="PublishProduct",        Sequence=6400, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "AdvertiseExecuteSequence/InstallFinalize"))       { Action="InstallFinalize",       Sequence=6600, SequenceTable=SequenceTable.AdvertiseExecuteSequence, Overridable = true },

                // InstallUISequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/FindRelatedProducts"))  { Action="FindRelatedProducts",  Sequence=25, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/AppSearch"))            { Action="AppSearch",            Sequence=50, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/LaunchConditions"))     { Action="LaunchConditions",     Sequence=100, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/CCPSearch"))            { Action="CCPSearch",            Sequence=500, SequenceTable=SequenceTable.InstallUISequence, Overridable = true, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/RMCCPSearch"))          { Action="RMCCPSearch",          Sequence=600, SequenceTable=SequenceTable.InstallUISequence, Overridable = true, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/ValidateProductID"))    { Action="ValidateProductID",    Sequence=700, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/CostInitialize"))       { Action="CostInitialize",       Sequence=800, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/FileCost"))             { Action="FileCost",             Sequence=900, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/IsolateComponents"))    { Action="IsolateComponents",    Sequence=950, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/CostFinalize"))         { Action="CostFinalize",         Sequence=1000, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/MigrateFeatureStates")) { Action="MigrateFeatureStates", Sequence=1200, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallUISequence/ExecuteAction"))        { Action="ExecuteAction",        Sequence=1300, SequenceTable=SequenceTable.InstallUISequence, Overridable = true },

                // InstallExecuteSequence
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/FindRelatedProducts"))      { Action="FindRelatedProducts",      Sequence=25, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/AppSearch"))                { Action="AppSearch",                Sequence=50, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/LaunchConditions"))         { Action="LaunchConditions",         Sequence=100, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/CCPSearch"))                { Action="CCPSearch",                Sequence=500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RMCCPSearch"))              { Action="RMCCPSearch",              Sequence=600, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/ValidateProductID"))        { Action="ValidateProductID",        Sequence=700, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/CostInitialize"))           { Action="CostInitialize",           Sequence=800, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/FileCost"))                 { Action="FileCost",                 Sequence=900, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/IsolateComponents"))        { Action="IsolateComponents",        Sequence=950, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/CostFinalize"))             { Action="CostFinalize",             Sequence=1000, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/SetODBCFolders"))           { Action="SetODBCFolders",           Sequence=1100, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/MigrateFeatureStates"))     { Action="MigrateFeatureStates",     Sequence=1200, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/InstallValidate"))          { Action="InstallValidate",          Sequence=1400, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/InstallInitialize"))        { Action="InstallInitialize",        Sequence=1500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/AllocateRegistrySpace"))    { Action="AllocateRegistrySpace",    Sequence=1550, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/ProcessComponents"))        { Action="ProcessComponents",        Sequence=1600, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/UnpublishComponents"))      { Action="UnpublishComponents",      Sequence=1700, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/MsiUnpublishAssemblies"))   { Action="MsiUnpublishAssemblies",   Sequence=1750, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/UnpublishFeatures"))        { Action="UnpublishFeatures",        Sequence=1800, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/StopServices"))             { Action="StopServices",             Sequence=1900, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/DeleteServices"))           { Action="DeleteServices",           Sequence=2000, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/UnregisterComPlus"))        { Action="UnregisterComPlus",        Sequence=2100, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/SelfUnregModules"))         { Action="SelfUnregModules",         Sequence=2200, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/UnregisterTypeLibraries"))  { Action="UnregisterTypeLibraries",  Sequence=2300, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RemoveODBC"))               { Action="RemoveODBC",               Sequence=2400, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/UnregisterFonts"))          { Action="UnregisterFonts",          Sequence=2500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RemoveRegistryValues"))     { Action="RemoveRegistryValues",     Sequence=2600, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/UnregisterClassInfo"))      { Action="UnregisterClassInfo",      Sequence=2700, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/UnregisterExtensionInfo"))  { Action="UnregisterExtensionInfo",  Sequence=2800, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/UnregisterProgIdInfo"))     { Action="UnregisterProgIdInfo",     Sequence=2900, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/UnregisterMIMEInfo"))       { Action="UnregisterMIMEInfo",       Sequence=3000, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RemoveIniValues"))          { Action="RemoveIniValues",          Sequence=3100, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RemoveShortcuts"))          { Action="RemoveShortcuts",          Sequence=3200, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RemoveEnvironmentStrings")) { Action="RemoveEnvironmentStrings", Sequence=3300, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RemoveDuplicateFiles"))     { Action="RemoveDuplicateFiles",     Sequence=3400, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RemoveFiles"))              { Action="RemoveFiles",              Sequence=3500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RemoveFolders"))            { Action="RemoveFolders",            Sequence=3600, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/CreateFolders"))            { Action="CreateFolders",            Sequence=3700, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/MoveFiles"))                { Action="MoveFiles",                Sequence=3800, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/InstallFiles"))             { Action="InstallFiles",             Sequence=4000, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/PatchFiles"))               { Action="PatchFiles",               Sequence=4090, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/DuplicateFiles"))           { Action="DuplicateFiles",           Sequence=4210, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/BindImage"))                { Action="BindImage",                Sequence=4300, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/CreateShortcuts"))          { Action="CreateShortcuts",          Sequence=4500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RegisterClassInfo"))        { Action="RegisterClassInfo",        Sequence=4600, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RegisterExtensionInfo"))    { Action="RegisterExtensionInfo",    Sequence=4700, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RegisterProgIdInfo"))       { Action="RegisterProgIdInfo",       Sequence=4800, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RegisterMIMEInfo"))         { Action="RegisterMIMEInfo",         Sequence=4900, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/WriteRegistryValues"))      { Action="WriteRegistryValues",      Sequence=5000, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/WriteIniValues"))           { Action="WriteIniValues",           Sequence=5100, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/WriteEnvironmentStrings"))  { Action="WriteEnvironmentStrings",  Sequence=5200, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RegisterFonts"))            { Action="RegisterFonts",            Sequence=5300, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/InstallODBC"))              { Action="InstallODBC",              Sequence=5400, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RegisterTypeLibraries"))    { Action="RegisterTypeLibraries",    Sequence=5500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/SelfRegModules"))           { Action="SelfRegModules",           Sequence=5600, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RegisterComPlus"))          { Action="RegisterComPlus",          Sequence=5700, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/InstallServices"))          { Action="InstallServices",          Sequence=5800, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/MsiConfigureServices"))     { Action="MsiConfigureServices",     Sequence=5850, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT>=600" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/StartServices"))            { Action="StartServices",            Sequence=5900, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="VersionNT" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RegisterUser"))             { Action="RegisterUser",             Sequence=6000, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/RegisterProduct"))          { Action="RegisterProduct",          Sequence=6100, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/PublishComponents"))        { Action="PublishComponents",        Sequence=6200, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/MsiPublishAssemblies"))     { Action="MsiPublishAssemblies",     Sequence=6250, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/PublishFeatures"))          { Action="PublishFeatures",          Sequence=6300, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/PublishProduct"))           { Action="PublishProduct",           Sequence=6400, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/InstallExecute"))           { Action="InstallExecute",           Sequence=6500, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/InstallExecuteAgain"))      { Action="InstallExecuteAgain",      Sequence=6550, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true, Condition="NOT Installed" },
                new WixActionSymbol(null, new Identifier(AccessModifier.Global, "InstallExecuteSequence/InstallFinalize"))          { Action="InstallFinalize",          Sequence=6600, SequenceTable=SequenceTable.InstallExecuteSequence, Overridable = true },
            };

            var standardDirectories = new[]
            {
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "TARGETDIR")) { Name = "SourceDir" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "AdminToolsFolder")) { Name = "Admin" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "AppDataFolder")) { Name = "AppData" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "CommonAppDataFolder")) { Name = "CommApp" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "CommonFilesFolder")) { Name = "CFiles" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "CommonFiles64Folder")) { Name = "CFiles64" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "CommonFiles6432Folder")) { Name = "." },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "DesktopFolder")) { Name = "Desktop" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "FavoritesFolder")) { Name = "Favs" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "FontsFolder")) { Name = "Fonts" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "LocalAppDataFolder")) { Name = "LocalApp" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "MyPicturesFolder")) { Name = "Pictures" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "NetHoodFolder")) { Name = "NetHood" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "PersonalFolder")) { Name = "Personal" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "PrintHoodFolder")) { Name = "Printers" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "ProgramFilesFolder")) { Name = "PFiles" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "ProgramFiles64Folder")) { Name = "PFiles64" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "ProgramFiles6432Folder")) { Name = "." },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "ProgramMenuFolder")) { Name = "PMenu" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "RecentFolder")) { Name = "Recent" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "SendToFolder")) { Name = "SendTo" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "StartMenuFolder")) { Name = "StrtMenu" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "StartupFolder")) { Name = "StartUp" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "SystemFolder")) { Name = "System" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "System16Folder")) { Name = "System16" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "System64Folder")) { Name = "System64" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "System6432Folder")) { Name = "." },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "TempFolder")) { Name = "Temp" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "TemplateFolder")) { Name = "Template" },
                new DirectorySymbol(null, new Identifier(AccessModifier.Global, "WindowsFolder")) { Name = "Windows" },
            };

            standardActionNames = new HashSet<string>(standardActions.Select(a => a.Action));
            standardActionsById = standardActions.ToDictionary(a => a.Id.Id);
            standardDirectoriesById = standardDirectories.ToDictionary(d => d.Id.Id);
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
        public static IReadOnlyCollection<WixActionSymbol> StandardActions() => standardActionsById.Values;

        /// <summary>
        /// Standard directories.
        /// </summary>
        public static IReadOnlyCollection<DirectorySymbol> StandardDirectories() => standardDirectoriesById.Values;

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
        public static bool IsStandardDirectory(string directoryId) => standardDirectoriesById.ContainsKey(directoryId);

        /// <summary>
        /// Find out if a property is a standard property.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>true if a property is standard, false otherwise.</returns>
        public static bool IsStandardProperty(string propertyName) => standardProperties.Contains(propertyName);

        /// <summary>
        /// Try to get standard action by id.
        /// </summary>
        public static bool TryGetStandardAction(string id, out WixActionSymbol standardAction) => standardActionsById.TryGetValue(id, out standardAction);

        /// <summary>
        /// Try to get standard action by sequence and action name.
        /// </summary>
        public static bool TryGetStandardAction(string sequenceName, string actioname, out WixActionSymbol standardAction) => standardActionsById.TryGetValue(String.Concat(sequenceName, "/", actioname), out standardAction);

        /// <summary>
        /// Try to get standard directory symbol by id.
        /// </summary>
        public static bool TryGetStandardDirectory(string directoryId, out DirectorySymbol symbol) => standardDirectoriesById.TryGetValue(directoryId, out symbol);
    }
}
