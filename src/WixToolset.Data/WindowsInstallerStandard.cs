// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System.Collections.Generic;
    using WixToolset.Data.Tuples;

    public class WindowsInstallerStandard
    {
        private static readonly HashSet<string> standardActionNames = new HashSet<string>
        {
            "AllocateRegistrySpace",
            "AppSearch",
            "BindImage",
            "CCPSearch",
            "CostFinalize",
            "CostInitialize",
            "CreateFolders",
            "CreateShortcuts",
            "DeleteServices",
            "DisableRollback",
            "DuplicateFiles",
            "ExecuteAction",
            "FileCost",
            "FindRelatedProducts",
            "ForceReboot",
            "InstallAdminPackage",
            "InstallExecute",
            "InstallExecuteAgain",
            "InstallFiles",
            "InstallFinalize",
            "InstallInitialize",
            "InstallODBC",
            "InstallServices",
            "InstallSFPCatalogFile",
            "InstallValidate",
            "IsolateComponents",
            "LaunchConditions",
            "MigrateFeatureStates",
            "MoveFiles",
            "MsiConfigureServices",
            "MsiPublishAssemblies",
            "MsiUnpublishAssemblies",
            "PatchFiles",
            "ProcessComponents",
            "PublishComponents",
            "PublishFeatures",
            "PublishProduct",
            "RegisterClassInfo",
            "RegisterComPlus",
            "RegisterExtensionInfo",
            "RegisterFonts",
            "RegisterMIMEInfo",
            "RegisterProduct",
            "RegisterProgIdInfo",
            "RegisterTypeLibraries",
            "RegisterUser",
            "RemoveDuplicateFiles",
            "RemoveEnvironmentStrings",
            "RemoveExistingProducts",
            "RemoveFiles",
            "RemoveFolders",
            "RemoveIniValues",
            "RemoveODBC",
            "RemoveRegistryValues",
            "RemoveShortcuts",
            "ResolveSource",
            "RMCCPSearch",
            "ScheduleReboot",
            "SelfRegModules",
            "SelfUnregModules",
            "SetODBCFolders",
            "StartServices",
            "StopServices",
            "UnpublishComponents",
            "UnpublishFeatures",
            "UnregisterClassInfo",
            "UnregisterComPlus",
            "UnregisterExtensionInfo",
            "UnregisterFonts",
            "UnregisterMIMEInfo",
            "UnregisterProgIdInfo",
            "UnregisterTypeLibraries",
            "ValidateProductID",
            "WriteEnvironmentStrings",
            "WriteIniValues",
            "WriteRegistryValues",
        };

        private static readonly WixActionTuple[] standardActions = new[]
        {
            new WixActionTuple(null, new Identifier("AdminExecuteSequence/InstallInitialize", AccessModifier.Public)) { Action="InstallInitialize", Sequence=1500, SequenceTable=SequenceTable.AdminExecuteSequence },
            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/InstallInitialize", AccessModifier.Public)) { Action="InstallInitialize", Sequence=1500, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/InstallInitialize", AccessModifier.Public)) { Action="InstallInitialize", Sequence=1500, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/InstallExecute", AccessModifier.Public)) { Action="InstallExecute", Sequence=6500, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="NOT Installed" },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/InstallExecuteAgain", AccessModifier.Public)) { Action="InstallExecuteAgain", Sequence=6550, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="NOT Installed" },

            new WixActionTuple(null, new Identifier("AdminExecuteSequence/InstallFinalize", AccessModifier.Public)) { Action="InstallFinalize", Sequence=6600, SequenceTable=SequenceTable.AdminExecuteSequence },
            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/InstallFinalize", AccessModifier.Public)) { Action="InstallFinalize", Sequence=6600, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/InstallFinalize", AccessModifier.Public)) { Action="InstallFinalize", Sequence=6600, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdminExecuteSequence/InstallFiles", AccessModifier.Public)) { Action="InstallFiles", Sequence=4000, SequenceTable=SequenceTable.AdminExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/InstallFiles", AccessModifier.Public)) { Action="InstallFiles", Sequence=4000, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdminExecuteSequence/InstallAdminPackage", AccessModifier.Public)) { Action="InstallAdminPackage", Sequence=3900, SequenceTable=SequenceTable.AdminExecuteSequence },

            new WixActionTuple(null, new Identifier("AdminExecuteSequence/FileCost", AccessModifier.Public)) { Action="FileCost", Sequence=900, SequenceTable=SequenceTable.AdminExecuteSequence },
            new WixActionTuple(null, new Identifier("AdminUISequence/FileCost", AccessModifier.Public)) { Action="FileCost", Sequence=900, SequenceTable=SequenceTable.AdminUISequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/FileCost", AccessModifier.Public)) { Action="FileCost", Sequence=900, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallUISequence/FileCost", AccessModifier.Public)) { Action="FileCost", Sequence=900, SequenceTable=SequenceTable.InstallUISequence },

            new WixActionTuple(null, new Identifier("AdminExecuteSequence/CostInitialize", AccessModifier.Public)) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.AdminExecuteSequence },
            new WixActionTuple(null, new Identifier("AdminUISequence/CostInitialize", AccessModifier.Public)) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.AdminUISequence },
            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/CostInitialize", AccessModifier.Public)) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/CostInitialize", AccessModifier.Public)) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallUISequence/CostInitialize", AccessModifier.Public)) { Action="CostInitialize", Sequence=800, SequenceTable=SequenceTable.InstallUISequence },

            new WixActionTuple(null, new Identifier("AdminExecuteSequence/CostFinalize", AccessModifier.Public)) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.AdminExecuteSequence },
            new WixActionTuple(null, new Identifier("AdminUISequence/CostFinalize", AccessModifier.Public)) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.AdminUISequence },
            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/CostFinalize", AccessModifier.Public)) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/CostFinalize", AccessModifier.Public)) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallUISequence/CostFinalize", AccessModifier.Public)) { Action="CostFinalize", Sequence=1000, SequenceTable=SequenceTable.InstallUISequence },

            new WixActionTuple(null, new Identifier("AdminExecuteSequence/InstallValidate", AccessModifier.Public)) { Action="InstallValidate", Sequence=1400, SequenceTable=SequenceTable.AdminExecuteSequence },
            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/InstallValidate", AccessModifier.Public)) { Action="InstallValidate", Sequence=1400, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/InstallValidate", AccessModifier.Public)) { Action="InstallValidate", Sequence=1400, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdminUISequence/ExecuteAction", AccessModifier.Public)) { Action="ExecuteAction", Sequence=1300, SequenceTable=SequenceTable.AdminUISequence },
            new WixActionTuple(null, new Identifier("InstallUISequence/ExecuteAction", AccessModifier.Public)) { Action="ExecuteAction", Sequence=1300, SequenceTable=SequenceTable.InstallUISequence },

            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/CreateShortcuts", AccessModifier.Public)) { Action="CreateShortcuts", Sequence=4500, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/CreateShortcuts", AccessModifier.Public)) { Action="CreateShortcuts", Sequence=4500, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/MsiPublishAssemblies", AccessModifier.Public)) { Action="MsiPublishAssemblies", Sequence=6250, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/MsiPublishAssemblies", AccessModifier.Public)) { Action="MsiPublishAssemblies", Sequence=6250, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/PublishComponents", AccessModifier.Public)) { Action="PublishComponents", Sequence=6200, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/PublishComponents", AccessModifier.Public)) { Action="PublishComponents", Sequence=6200, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/PublishFeatures", AccessModifier.Public)) { Action="PublishFeatures", Sequence=6300, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/PublishFeatures", AccessModifier.Public)) { Action="PublishFeatures", Sequence=6300, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/PublishProduct", AccessModifier.Public)) { Action="PublishProduct", Sequence=6400, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/PublishProduct", AccessModifier.Public)) { Action="PublishProduct", Sequence=6400, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/RegisterClassInfo", AccessModifier.Public)) { Action="RegisterClassInfo", Sequence=4600, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RegisterClassInfo", AccessModifier.Public)) { Action="RegisterClassInfo", Sequence=4600, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/RegisterExtensionInfo", AccessModifier.Public)) { Action="RegisterExtensionInfo", Sequence=4700, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RegisterExtensionInfo", AccessModifier.Public)) { Action="RegisterExtensionInfo", Sequence=4700, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/RegisterMIMEInfo", AccessModifier.Public)) { Action="RegisterMIMEInfo", Sequence=4900, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RegisterMIMEInfo", AccessModifier.Public)) { Action="RegisterMIMEInfo", Sequence=4900, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdvtExecuteSequence/RegisterProgIdInfo", AccessModifier.Public)) { Action="RegisterProgIdInfo", Sequence=4800, SequenceTable=SequenceTable.AdvtExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RegisterProgIdInfo", AccessModifier.Public)) { Action="RegisterProgIdInfo", Sequence=4800, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/AllocateRegistrySpace", AccessModifier.Public)) { Action="AllocateRegistrySpace", Sequence=1550, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallUISequence/AppSearch", AccessModifier.Public)) { Action="AppSearch", Sequence=50, SequenceTable=SequenceTable.InstallUISequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/AppSearch", AccessModifier.Public)) { Action="AppSearch", Sequence=50, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/BindImage", AccessModifier.Public)) { Action="BindImage", Sequence=4300, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/CreateFolders", AccessModifier.Public)) { Action="CreateFolders", Sequence=3700, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/DuplicateFiles", AccessModifier.Public)) { Action="DuplicateFiles", Sequence=4210, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallUISequence/FindRelatedProducts", AccessModifier.Public)) { Action="FindRelatedProducts", Sequence=25, SequenceTable=SequenceTable.InstallUISequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/FindRelatedProducts", AccessModifier.Public)) { Action="FindRelatedProducts", Sequence=25, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/InstallODBC", AccessModifier.Public)) { Action="InstallODBC", Sequence=5400, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/InstallServices", AccessModifier.Public)) { Action="InstallServices", Sequence=5800, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT" },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/MsiConfigureServices", AccessModifier.Public)) { Action="MsiConfigureServices", Sequence=5850, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT>=600" },

            new WixActionTuple(null, new Identifier("InstallUISequence/IsolateComponents", AccessModifier.Public)) { Action="IsolateComponents", Sequence=950, SequenceTable=SequenceTable.InstallUISequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/IsolateComponents", AccessModifier.Public)) { Action="IsolateComponents", Sequence=950, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdminUISequence/LaunchConditions", AccessModifier.Public)) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.AdminUISequence },
            new WixActionTuple(null, new Identifier("AdminExecuteSequence/LaunchConditions", AccessModifier.Public)) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.AdminExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallUISequence/LaunchConditions", AccessModifier.Public)) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.InstallUISequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/LaunchConditions", AccessModifier.Public)) { Action="LaunchConditions", Sequence=100, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallUISequence/MigrateFeatureStates", AccessModifier.Public)) { Action="MigrateFeatureStates", Sequence=1200, SequenceTable=SequenceTable.InstallUISequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/MigrateFeatureStates", AccessModifier.Public)) { Action="MigrateFeatureStates", Sequence=1200, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/MoveFiles", AccessModifier.Public)) { Action="MoveFiles", Sequence=3800, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("AdminExecuteSequence/PatchFiles", AccessModifier.Public)) { Action="PatchFiles", Sequence=4090, SequenceTable=SequenceTable.AdminExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/PatchFiles", AccessModifier.Public)) { Action="PatchFiles", Sequence=4090, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/ProcessComponents", AccessModifier.Public)) { Action="ProcessComponents", Sequence=1600, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RegisterComPlus", AccessModifier.Public)) { Action="RegisterComPlus", Sequence=5700, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RegisterFonts", AccessModifier.Public)) { Action="RegisterFonts", Sequence=5300, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RegisterProduct", AccessModifier.Public)) { Action="RegisterProduct", Sequence=6100, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RegisterTypeLibraries", AccessModifier.Public)) { Action="RegisterTypeLibraries", Sequence=5500, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RegisterUser", AccessModifier.Public)) { Action="RegisterUser", Sequence=6000, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RemoveDuplicateFiles", AccessModifier.Public)) { Action="RemoveDuplicateFiles", Sequence=3400, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RemoveEnvironmentStrings", AccessModifier.Public)) { Action="RemoveEnvironmentStrings", Sequence=3300, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RemoveFiles", AccessModifier.Public)) { Action="RemoveFiles", Sequence=3500, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RemoveFolders", AccessModifier.Public)) { Action="RemoveFolders", Sequence=3600, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RemoveIniValues", AccessModifier.Public)) { Action="RemoveIniValues", Sequence=3100, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RemoveODBC", AccessModifier.Public)) { Action="RemoveODBC", Sequence=2400, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RemoveRegistryValues", AccessModifier.Public)) { Action="RemoveRegistryValues", Sequence=2600, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RemoveShortcuts", AccessModifier.Public)) { Action="RemoveShortcuts", Sequence=3200, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/SelfRegModules", AccessModifier.Public)) { Action="SelfRegModules", Sequence=5600, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/SelfUnregModules", AccessModifier.Public)) { Action="SelfUnregModules", Sequence=2200, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/SetODBCFolders", AccessModifier.Public)) { Action="SetODBCFolders", Sequence=1100, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/CCPSearch", AccessModifier.Public)) { Action="CCPSearch", Sequence=500, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="NOT Installed" },
            new WixActionTuple(null, new Identifier("InstallUISequence/CCPSearch", AccessModifier.Public)) { Action="CCPSearch", Sequence=500, SequenceTable=SequenceTable.InstallUISequence, Condition="NOT Installed" },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/DeleteServices", AccessModifier.Public)) { Action="DeleteServices", Sequence=2000, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT" },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/RMCCPSearch", AccessModifier.Public)) { Action="RMCCPSearch", Sequence=600, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="NOT Installed" },
            new WixActionTuple(null, new Identifier("InstallUISequence/RMCCPSearch", AccessModifier.Public)) { Action="RMCCPSearch", Sequence=600, SequenceTable=SequenceTable.InstallUISequence, Condition="NOT Installed" },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/StartServices", AccessModifier.Public)) { Action="StartServices", Sequence=5900, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT" },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/StopServices", AccessModifier.Public)) { Action="StopServices", Sequence=1900, SequenceTable=SequenceTable.InstallExecuteSequence, Condition="VersionNT" },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/MsiUnpublishAssemblies", AccessModifier.Public)) { Action="MsiUnpublishAssemblies", Sequence=1750, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/UnpublishComponents", AccessModifier.Public)) { Action="UnpublishComponents", Sequence=1700, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/UnpublishFeatures", AccessModifier.Public)) { Action="UnpublishFeatures", Sequence=1800, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/UnregisterClassInfo", AccessModifier.Public)) { Action="UnregisterClassInfo", Sequence=2700, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/UnregisterComPlus", AccessModifier.Public)) { Action="UnregisterComPlus", Sequence=2100, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/UnregisterExtensionInfo", AccessModifier.Public)) { Action="UnregisterExtensionInfo", Sequence=2800, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/UnregisterFonts", AccessModifier.Public)) { Action="UnregisterFonts", Sequence=2500, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/UnregisterMIMEInfo", AccessModifier.Public)) { Action="UnregisterMIMEInfo", Sequence=3000, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/UnregisterProgIdInfo", AccessModifier.Public)) { Action="UnregisterProgIdInfo", Sequence=2900, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/UnregisterTypeLibraries", AccessModifier.Public)) { Action="UnregisterTypeLibraries", Sequence=2300, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallUISequence/ValidateProductID", AccessModifier.Public)) { Action="ValidateProductID", Sequence=700, SequenceTable=SequenceTable.InstallUISequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/ValidateProductID", AccessModifier.Public)) { Action="ValidateProductID", Sequence=700, SequenceTable=SequenceTable.InstallExecuteSequence },

            new WixActionTuple(null, new Identifier("InstallExecuteSequence/WriteEnvironmentStrings", AccessModifier.Public)) { Action="WriteEnvironmentStrings", Sequence=5200, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/WriteIniValues", AccessModifier.Public)) { Action="WriteIniValues", Sequence=5100, SequenceTable=SequenceTable.InstallExecuteSequence },
            new WixActionTuple(null, new Identifier("InstallExecuteSequence/WriteRegistryValues", AccessModifier.Public)) { Action="WriteRegistryValues", Sequence=5000, SequenceTable=SequenceTable.InstallExecuteSequence },
        };

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
        /// Array of standard actions.
        /// </summary>
        public static WixActionTuple[] StandardActions() => standardActions;

        /// <summary>
        /// Find out if a directory is a standard directory.
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns>true if the directory is standard, false otherwise.</returns>
        public static bool IsStandardDirectory(string directoryName)
        {
            return standardDirectories.Contains(directoryName);
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
    }
}
