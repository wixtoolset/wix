// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System.Collections.Generic;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;

    internal class WindowsInstallerStandard
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

            //<action name="PublishComponents" sequence="6200" AdvtExecuteSequence="yes" InstallExecuteSequence="yes" />
            //<action name="PublishFeatures" sequence="6300" AdvtExecuteSequence="yes" InstallExecuteSequence="yes" />
            //<action name="PublishProduct" sequence="6400" AdvtExecuteSequence="yes" InstallExecuteSequence="yes" />
            //<action name="RegisterClassInfo" sequence="4600" AdvtExecuteSequence="yes" InstallExecuteSequence="yes" />
            //<action name="RegisterExtensionInfo" sequence="4700" AdvtExecuteSequence="yes" InstallExecuteSequence="yes" />
            //<action name="RegisterMIMEInfo" sequence="4900" AdvtExecuteSequence="yes" InstallExecuteSequence="yes" />
            //<action name="RegisterProgIdInfo" sequence="4800" AdvtExecuteSequence="yes" InstallExecuteSequence="yes" />
            //<action name="AllocateRegistrySpace" condition="NOT Installed" sequence="1550" InstallExecuteSequence="yes" />
            //<action name="AppSearch" sequence="50" InstallExecuteSequence="yes" InstallUISequence="yes" />
            //<action name="BindImage" sequence="4300" InstallExecuteSequence="yes" />
            //<action name="CreateFolders" sequence="3700" InstallExecuteSequence="yes" />
            //<action name="DuplicateFiles" sequence="4210" InstallExecuteSequence="yes" />
            //<action name="FindRelatedProducts" sequence="25" InstallExecuteSequence="yes" InstallUISequence="yes" />
            //<action name="InstallODBC" sequence="5400" InstallExecuteSequence="yes" />
            //<action name="InstallServices" condition="VersionNT" sequence="5800" InstallExecuteSequence="yes" />
            //<action name="MsiConfigureServices" condition="VersionNT>=600" sequence="5850" InstallExecuteSequence="yes" />
            //<action name="IsolateComponents" sequence="950" InstallExecuteSequence="yes" InstallUISequence="yes" />
            //<action name="LaunchConditions" sequence="100" AdminExecuteSequence="yes" AdminUISequence="yes" InstallExecuteSequence="yes" InstallUISequence="yes" />
            //<action name="MigrateFeatureStates" sequence="1200" InstallExecuteSequence="yes" InstallUISequence="yes" />
            //<action name="MoveFiles" sequence="3800" InstallExecuteSequence="yes" />
            //<action name="PatchFiles" sequence="4090" AdminExecuteSequence="yes" InstallExecuteSequence="yes" />
            //<action name="ProcessComponents" sequence="1600" InstallExecuteSequence="yes" />
            //<action name="RegisterComPlus" sequence="5700" InstallExecuteSequence="yes" />
            //<action name="RegisterFonts" sequence="5300" InstallExecuteSequence="yes" />
            //<action name="RegisterProduct" sequence="6100" InstallExecuteSequence="yes" />
            //<action name="RegisterTypeLibraries" sequence="5500" InstallExecuteSequence="yes" />
            //<action name="RegisterUser" sequence="6000" InstallExecuteSequence="yes" />
            //<action name="RemoveDuplicateFiles" sequence="3400" InstallExecuteSequence="yes" />
            //<action name="RemoveEnvironmentStrings" sequence="3300" InstallExecuteSequence="yes" />
            //<action name="RemoveFiles" sequence="3500" InstallExecuteSequence="yes" />
            //<action name="RemoveFolders" sequence="3600" InstallExecuteSequence="yes" />
            //<action name="RemoveIniValues" sequence="3100" InstallExecuteSequence="yes" />
            //<action name="RemoveODBC" sequence="2400" InstallExecuteSequence="yes" />
            //<action name="RemoveRegistryValues" sequence="2600" InstallExecuteSequence="yes" />
            //<action name="RemoveShortcuts" sequence="3200" InstallExecuteSequence="yes" />
            //<action name="SelfRegModules" sequence="5600" InstallExecuteSequence="yes" />
            //<action name="SelfUnregModules" sequence="2200" InstallExecuteSequence="yes" />
            //<action name="SetODBCFolders" sequence="1100" InstallExecuteSequence="yes" />


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
        /// Find out if an action is a standard action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <returns>true if the action is standard, false otherwise.</returns>
        public static bool IsStandardAction(string actionName)
        {
            return standardActionNames.Contains(actionName);
        }

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
    }
}
