// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using System.Collections.Generic;
    using System.Reflection;
    using System.Xml;
    using WixToolset.Data.Rows;

    /// <summary>
    /// Represents the Windows Installer standard objects.
    /// </summary>
    public static class WindowsInstallerStandard
    {
        private static readonly object lockObject = new object();

        private static TableDefinitionCollection tableDefinitions;
        private static WixActionRowCollection standardActions;

        private static HashSet<string> standardActionNames;
        private static HashSet<string> standardDirectories;
        private static HashSet<string> standardProperties;


        /// <summary>
        /// Gets the table definitions stored in this assembly.
        /// </summary>
        /// <returns>Table definition collection for tables stored in this assembly.</returns>
        public static TableDefinitionCollection GetTableDefinitions()
        {
            lock (lockObject)
            {
                if (null == WindowsInstallerStandard.tableDefinitions)
                {
                    using (XmlReader reader = XmlReader.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("WixToolset.Data.Data.tables.xml")))
                    {
                        tableDefinitions = TableDefinitionCollection.Load(reader);
                    }
                }
            }

            return WindowsInstallerStandard.tableDefinitions;
        }

        /// <summary>
        /// Gets the standard actions stored in this assembly.
        /// </summary>
        /// <returns>Collection of standard actions in this assembly.</returns>
        public static WixActionRowCollection GetStandardActions()
        {
            lock (lockObject)
            {
                if (null == standardActions)
                {
                    using (XmlReader reader = XmlReader.Create(Assembly.GetExecutingAssembly().GetManifestResourceStream("WixToolset.Data.Data.actions.xml")))
                    {
                        standardActions = WixActionRowCollection.Load(reader);
                    }
                }
            }

            return standardActions;
        }


        /// <summary>
        /// Gets (and loads if not yet loaded) the list of standard MSI directories.
        /// </summary>
        /// <value>The list of standard MSI directories.</value>
        public static HashSet<string> GetStandardDirectories()
        {
            lock (lockObject)
            {
                if (null == standardDirectories)
                {
                    LoadStandardDirectories();
                }
            }

            return standardDirectories;
        }

        /// <summary>
        /// Find out if an action is a standard action.
        /// </summary>
        /// <param name="actionName">Name of the action.</param>
        /// <returns>true if the action is standard, false otherwise.</returns>
        public static bool IsStandardAction(string actionName)
        {
            lock (lockObject)
            {
                if (null == standardActionNames)
                {
                    standardActionNames = new HashSet<string>();
                    standardActionNames.Add("AllocateRegistrySpace");
                    standardActionNames.Add("AppSearch");
                    standardActionNames.Add("BindImage");
                    standardActionNames.Add("CCPSearch");
                    standardActionNames.Add("CostFinalize");
                    standardActionNames.Add("CostInitialize");
                    standardActionNames.Add("CreateFolders");
                    standardActionNames.Add("CreateShortcuts");
                    standardActionNames.Add("DeleteServices");
                    standardActionNames.Add("DisableRollback");
                    standardActionNames.Add("DuplicateFiles");
                    standardActionNames.Add("ExecuteAction");
                    standardActionNames.Add("FileCost");
                    standardActionNames.Add("FindRelatedProducts");
                    standardActionNames.Add("ForceReboot");
                    standardActionNames.Add("InstallAdminPackage");
                    standardActionNames.Add("InstallExecute");
                    standardActionNames.Add("InstallExecuteAgain");
                    standardActionNames.Add("InstallFiles");
                    standardActionNames.Add("InstallFinalize");
                    standardActionNames.Add("InstallInitialize");
                    standardActionNames.Add("InstallODBC");
                    standardActionNames.Add("InstallServices");
                    standardActionNames.Add("InstallSFPCatalogFile");
                    standardActionNames.Add("InstallValidate");
                    standardActionNames.Add("IsolateComponents");
                    standardActionNames.Add("LaunchConditions");
                    standardActionNames.Add("MigrateFeatureStates");
                    standardActionNames.Add("MoveFiles");
                    standardActionNames.Add("MsiConfigureServices");
                    standardActionNames.Add("MsiPublishAssemblies");
                    standardActionNames.Add("MsiUnpublishAssemblies");
                    standardActionNames.Add("PatchFiles");
                    standardActionNames.Add("ProcessComponents");
                    standardActionNames.Add("PublishComponents");
                    standardActionNames.Add("PublishFeatures");
                    standardActionNames.Add("PublishProduct");
                    standardActionNames.Add("RegisterClassInfo");
                    standardActionNames.Add("RegisterComPlus");
                    standardActionNames.Add("RegisterExtensionInfo");
                    standardActionNames.Add("RegisterFonts");
                    standardActionNames.Add("RegisterMIMEInfo");
                    standardActionNames.Add("RegisterProduct");
                    standardActionNames.Add("RegisterProgIdInfo");
                    standardActionNames.Add("RegisterTypeLibraries");
                    standardActionNames.Add("RegisterUser");
                    standardActionNames.Add("RemoveDuplicateFiles");
                    standardActionNames.Add("RemoveEnvironmentStrings");
                    standardActionNames.Add("RemoveExistingProducts");
                    standardActionNames.Add("RemoveFiles");
                    standardActionNames.Add("RemoveFolders");
                    standardActionNames.Add("RemoveIniValues");
                    standardActionNames.Add("RemoveODBC");
                    standardActionNames.Add("RemoveRegistryValues");
                    standardActionNames.Add("RemoveShortcuts");
                    standardActionNames.Add("ResolveSource");
                    standardActionNames.Add("RMCCPSearch");
                    standardActionNames.Add("ScheduleReboot");
                    standardActionNames.Add("SelfRegModules");
                    standardActionNames.Add("SelfUnregModules");
                    standardActionNames.Add("SetODBCFolders");
                    standardActionNames.Add("StartServices");
                    standardActionNames.Add("StopServices");
                    standardActionNames.Add("UnpublishComponents");
                    standardActionNames.Add("UnpublishFeatures");
                    standardActionNames.Add("UnregisterClassInfo");
                    standardActionNames.Add("UnregisterComPlus");
                    standardActionNames.Add("UnregisterExtensionInfo");
                    standardActionNames.Add("UnregisterFonts");
                    standardActionNames.Add("UnregisterMIMEInfo");
                    standardActionNames.Add("UnregisterProgIdInfo");
                    standardActionNames.Add("UnregisterTypeLibraries");
                    standardActionNames.Add("ValidateProductID");
                    standardActionNames.Add("WriteEnvironmentStrings");
                    standardActionNames.Add("WriteIniValues");
                    standardActionNames.Add("WriteRegistryValues");
                }
            }

            return standardActionNames.Contains(actionName);
        }

        /// <summary>
        /// Find out if a directory is a standard directory.
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns>true if the directory is standard, false otherwise.</returns>
        public static bool IsStandardDirectory(string directoryName)
        {
            lock (lockObject)
            {
                if (null == standardDirectories)
                {
                    LoadStandardDirectories();
                }
            }

            return standardDirectories.Contains(directoryName);
        }

        /// <summary>
        /// Find out if a property is a standard property.
        /// References: 
        /// Title:   Property Reference [Windows Installer]: 
        /// URL:     http://msdn.microsoft.com/library/en-us/msi/setup/property_reference.asp
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <returns>true if a property is standard, false otherwise.</returns>
        public static bool IsStandardProperty(string propertyName)
        {
            lock (lockObject)
            {
                if (null == standardProperties)
                {
                    standardProperties = new HashSet<string>();
                    standardProperties.Add("~"); // REG_MULTI_SZ/NULL marker
                    standardProperties.Add("ACTION");
                    standardProperties.Add("ADDDEFAULT");
                    standardProperties.Add("ADDLOCAL");
                    standardProperties.Add("ADDDSOURCE");
                    standardProperties.Add("AdminProperties");
                    standardProperties.Add("AdminUser");
                    standardProperties.Add("ADVERTISE");
                    standardProperties.Add("AFTERREBOOT");
                    standardProperties.Add("AllowProductCodeMismatches");
                    standardProperties.Add("AllowProductVersionMajorMismatches");
                    standardProperties.Add("ALLUSERS");
                    standardProperties.Add("Alpha");
                    standardProperties.Add("ApiPatchingSymbolFlags");
                    standardProperties.Add("ARPAUTHORIZEDCDFPREFIX");
                    standardProperties.Add("ARPCOMMENTS");
                    standardProperties.Add("ARPCONTACT");
                    standardProperties.Add("ARPHELPLINK");
                    standardProperties.Add("ARPHELPTELEPHONE");
                    standardProperties.Add("ARPINSTALLLOCATION");
                    standardProperties.Add("ARPNOMODIFY");
                    standardProperties.Add("ARPNOREMOVE");
                    standardProperties.Add("ARPNOREPAIR");
                    standardProperties.Add("ARPPRODUCTIONICON");
                    standardProperties.Add("ARPREADME");
                    standardProperties.Add("ARPSIZE");
                    standardProperties.Add("ARPSYSTEMCOMPONENT");
                    standardProperties.Add("ARPULRINFOABOUT");
                    standardProperties.Add("ARPURLUPDATEINFO");
                    standardProperties.Add("AVAILABLEFREEREG");
                    standardProperties.Add("BorderSize");
                    standardProperties.Add("BorderTop");
                    standardProperties.Add("CaptionHeight");
                    standardProperties.Add("CCP_DRIVE");
                    standardProperties.Add("ColorBits");
                    standardProperties.Add("COMPADDLOCAL");
                    standardProperties.Add("COMPADDSOURCE");
                    standardProperties.Add("COMPANYNAME");
                    standardProperties.Add("ComputerName");
                    standardProperties.Add("CostingComplete");
                    standardProperties.Add("Date");
                    standardProperties.Add("DefaultUIFont");
                    standardProperties.Add("DISABLEADVTSHORTCUTS");
                    standardProperties.Add("DISABLEMEDIA");
                    standardProperties.Add("DISABLEROLLBACK");
                    standardProperties.Add("DiskPrompt");
                    standardProperties.Add("DontRemoveTempFolderWhenFinished");
                    standardProperties.Add("EnableUserControl");
                    standardProperties.Add("EXECUTEACTION");
                    standardProperties.Add("EXECUTEMODE");
                    standardProperties.Add("FASTOEM");
                    standardProperties.Add("FILEADDDEFAULT");
                    standardProperties.Add("FILEADDLOCAL");
                    standardProperties.Add("FILEADDSOURCE");
                    standardProperties.Add("IncludeWholeFilesOnly");
                    standardProperties.Add("Installed");
                    standardProperties.Add("INSTALLLEVEL");
                    standardProperties.Add("Intel");
                    standardProperties.Add("Intel64");
                    standardProperties.Add("IsAdminPackage");
                    standardProperties.Add("LeftUnit");
                    standardProperties.Add("LIMITUI");
                    standardProperties.Add("ListOfPatchGUIDsToReplace");
                    standardProperties.Add("ListOfTargetProductCode");
                    standardProperties.Add("LOGACTION");
                    standardProperties.Add("LogonUser");
                    standardProperties.Add("Manufacturer");
                    standardProperties.Add("MEDIAPACKAGEPATH");
                    standardProperties.Add("MediaSourceDir");
                    standardProperties.Add("MinimumRequiredMsiVersion");
                    standardProperties.Add("MsiAMD64");
                    standardProperties.Add("MSIAPRSETTINGSIDENTIFIER");
                    standardProperties.Add("MSICHECKCRCS");
                    standardProperties.Add("MSIDISABLERMRESTART");
                    standardProperties.Add("MSIENFORCEUPGRADECOMPONENTRULES");
                    standardProperties.Add("MSIFASTINSTALL");
                    standardProperties.Add("MsiFileToUseToCreatePatchTables");
                    standardProperties.Add("MsiHiddenProperties");
                    standardProperties.Add("MSIINSTALLPERUSER");
                    standardProperties.Add("MSIINSTANCEGUID");
                    standardProperties.Add("MsiLogFileLocation");
                    standardProperties.Add("MsiLogging");
                    standardProperties.Add("MsiNetAssemblySupport");
                    standardProperties.Add("MSINEWINSTANCE");
                    standardProperties.Add("MSINODISABLEMEDIA");
                    standardProperties.Add("MsiNTProductType");
                    standardProperties.Add("MsiNTSuiteBackOffice");
                    standardProperties.Add("MsiNTSuiteDataCenter");
                    standardProperties.Add("MsiNTSuiteEnterprise");
                    standardProperties.Add("MsiNTSuiteSmallBusiness");
                    standardProperties.Add("MsiNTSuiteSmallBusinessRestricted");
                    standardProperties.Add("MsiNTSuiteWebServer");
                    standardProperties.Add("MsiNTSuitePersonal");
                    standardProperties.Add("MsiPatchRemovalList");
                    standardProperties.Add("MSIPATCHREMOVE");
                    standardProperties.Add("MSIRESTARTMANAGERCONTROL");
                    standardProperties.Add("MsiRestartManagerSessionKey");
                    standardProperties.Add("MSIRMSHUTDOWN");
                    standardProperties.Add("MsiRunningElevated");
                    standardProperties.Add("MsiUIHideCancel");
                    standardProperties.Add("MsiUIProgressOnly");
                    standardProperties.Add("MsiUISourceResOnly");
                    standardProperties.Add("MsiSystemRebootPending");
                    standardProperties.Add("MsiWin32AssemblySupport");
                    standardProperties.Add("NOCOMPANYNAME");
                    standardProperties.Add("NOUSERNAME");
                    standardProperties.Add("OLEAdvtSupport");
                    standardProperties.Add("OptimizePatchSizeForLargeFiles");
                    standardProperties.Add("OriginalDatabase");
                    standardProperties.Add("OutOfDiskSpace");
                    standardProperties.Add("OutOfNoRbDiskSpace");
                    standardProperties.Add("ParentOriginalDatabase");
                    standardProperties.Add("ParentProductCode");
                    standardProperties.Add("PATCH");
                    standardProperties.Add("PATCH_CACHE_DIR");
                    standardProperties.Add("PATCH_CACHE_ENABLED");
                    standardProperties.Add("PatchGUID");
                    standardProperties.Add("PATCHNEWPACKAGECODE");
                    standardProperties.Add("PATCHNEWSUMMARYCOMMENTS");
                    standardProperties.Add("PATCHNEWSUMMARYSUBJECT");
                    standardProperties.Add("PatchOutputPath");
                    standardProperties.Add("PatchSourceList");
                    standardProperties.Add("PhysicalMemory");
                    standardProperties.Add("PIDKEY");
                    standardProperties.Add("PIDTemplate");
                    standardProperties.Add("Preselected");
                    standardProperties.Add("PRIMARYFOLDER");
                    standardProperties.Add("PrimaryVolumePath");
                    standardProperties.Add("PrimaryVolumeSpaceAvailable");
                    standardProperties.Add("PrimaryVolumeSpaceRemaining");
                    standardProperties.Add("PrimaryVolumeSpaceRequired");
                    standardProperties.Add("Privileged");
                    standardProperties.Add("ProductCode");
                    standardProperties.Add("ProductID");
                    standardProperties.Add("ProductLanguage");
                    standardProperties.Add("ProductName");
                    standardProperties.Add("ProductState");
                    standardProperties.Add("ProductVersion");
                    standardProperties.Add("PROMPTROLLBACKCOST");
                    standardProperties.Add("REBOOT");
                    standardProperties.Add("REBOOTPROMPT");
                    standardProperties.Add("RedirectedDllSupport");
                    standardProperties.Add("REINSTALL");
                    standardProperties.Add("REINSTALLMODE");
                    standardProperties.Add("RemoveAdminTS");
                    standardProperties.Add("REMOVE");
                    standardProperties.Add("ReplacedInUseFiles");
                    standardProperties.Add("RestrictedUserControl");
                    standardProperties.Add("RESUME");
                    standardProperties.Add("RollbackDisabled");
                    standardProperties.Add("ROOTDRIVE");
                    standardProperties.Add("ScreenX");
                    standardProperties.Add("ScreenY");
                    standardProperties.Add("SecureCustomProperties");
                    standardProperties.Add("ServicePackLevel");
                    standardProperties.Add("ServicePackLevelMinor");
                    standardProperties.Add("SEQUENCE");
                    standardProperties.Add("SharedWindows");
                    standardProperties.Add("ShellAdvtSupport");
                    standardProperties.Add("SHORTFILENAMES");
                    standardProperties.Add("SourceDir");
                    standardProperties.Add("SOURCELIST");
                    standardProperties.Add("SystemLanguageID");
                    standardProperties.Add("TARGETDIR");
                    standardProperties.Add("TerminalServer");
                    standardProperties.Add("TextHeight");
                    standardProperties.Add("Time");
                    standardProperties.Add("TRANSFORMS");
                    standardProperties.Add("TRANSFORMSATSOURCE");
                    standardProperties.Add("TRANSFORMSSECURE");
                    standardProperties.Add("TTCSupport");
                    standardProperties.Add("UILevel");
                    standardProperties.Add("UpdateStarted");
                    standardProperties.Add("UpgradeCode");
                    standardProperties.Add("UPGRADINGPRODUCTCODE");
                    standardProperties.Add("UserLanguageID");
                    standardProperties.Add("USERNAME");
                    standardProperties.Add("UserSID");
                    standardProperties.Add("Version9X");
                    standardProperties.Add("VersionDatabase");
                    standardProperties.Add("VersionMsi");
                    standardProperties.Add("VersionNT");
                    standardProperties.Add("VersionNT64");
                    standardProperties.Add("VirtualMemory");
                    standardProperties.Add("WindowsBuild");
                    standardProperties.Add("WindowsVolume");
                }
            }

            return standardProperties.Contains(propertyName);
        }

        /// <summary>
        /// Sets up a hashtable with the set of standard MSI directories
        /// </summary>
        private static void LoadStandardDirectories()
        {
            lock (lockObject)
            {
                if (null == standardDirectories)
                {
                    standardDirectories = new HashSet<string>();
                    standardDirectories.Add("TARGETDIR");
                    standardDirectories.Add("AdminToolsFolder");
                    standardDirectories.Add("AppDataFolder");
                    standardDirectories.Add("CommonAppDataFolder");
                    standardDirectories.Add("CommonFilesFolder");
                    standardDirectories.Add("DesktopFolder");
                    standardDirectories.Add("FavoritesFolder");
                    standardDirectories.Add("FontsFolder");
                    standardDirectories.Add("LocalAppDataFolder");
                    standardDirectories.Add("MyPicturesFolder");
                    standardDirectories.Add("PersonalFolder");
                    standardDirectories.Add("ProgramFilesFolder");
                    standardDirectories.Add("ProgramMenuFolder");
                    standardDirectories.Add("SendToFolder");
                    standardDirectories.Add("StartMenuFolder");
                    standardDirectories.Add("StartupFolder");
                    standardDirectories.Add("System16Folder");
                    standardDirectories.Add("SystemFolder");
                    standardDirectories.Add("TempFolder");
                    standardDirectories.Add("TemplateFolder");
                    standardDirectories.Add("WindowsFolder");
                    standardDirectories.Add("CommonFiles64Folder");
                    standardDirectories.Add("ProgramFiles64Folder");
                    standardDirectories.Add("System64Folder");
                    standardDirectories.Add("NetHoodFolder");
                    standardDirectories.Add("PrintHoodFolder");
                    standardDirectories.Add("RecentFolder");
                    standardDirectories.Add("WindowsVolume");
                }
            }
        }
    }
}
