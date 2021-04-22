// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    public static class WindowsInstallerConstants
    {
        // Component.Attributes
        public const int MsidbComponentAttributesLocalOnly = 0;
        public const int MsidbComponentAttributesSourceOnly = 1;
        public const int MsidbComponentAttributesOptional = 2;
        public const int MsidbComponentAttributesRegistryKeyPath = 4;
        public const int MsidbComponentAttributesSharedDllRefCount = 8;
        public const int MsidbComponentAttributesPermanent = 16;
        public const int MsidbComponentAttributesODBCDataSource = 32;
        public const int MsidbComponentAttributesTransitive = 64;
        public const int MsidbComponentAttributesNeverOverwrite = 128;
        public const int MsidbComponentAttributes64bit = 256;
        public const int MsidbComponentAttributesDisableRegistryReflection = 512;
        public const int MsidbComponentAttributesUninstallOnSupersedence = 1024;
        public const int MsidbComponentAttributesShared = 2048;

        // BBControl.Attributes & Control.Attributes
        public const int MsidbControlAttributesVisible = 0x00000001;
        public const int MsidbControlAttributesEnabled = 0x00000002;
        public const int MsidbControlAttributesSunken = 0x00000004;
        public const int MsidbControlAttributesIndirect = 0x00000008;
        public const int MsidbControlAttributesInteger = 0x00000010;
        public const int MsidbControlAttributesRTLRO = 0x00000020;
        public const int MsidbControlAttributesRightAligned = 0x00000040;
        public const int MsidbControlAttributesLeftScroll = 0x00000080;
        public const int MsidbControlAttributesBiDi = MsidbControlAttributesRTLRO | MsidbControlAttributesRightAligned | MsidbControlAttributesLeftScroll;

        // Text controls
        public const int MsidbControlAttributesTransparent = 0x00010000;
        public const int MsidbControlAttributesNoPrefix = 0x00020000;
        public const int MsidbControlAttributesNoWrap = 0x00040000;
        public const int MsidbControlAttributesFormatSize = 0x00080000;
        public const int MsidbControlAttributesUsersLanguage = 0x00100000;

        // Edit controls
        public const int MsidbControlAttributesMultiline = 0x00010000;
        public const int MsidbControlAttributesPasswordInput = 0x00200000;

        // ProgressBar controls
        public const int MsidbControlAttributesProgress95 = 0x00010000;

        // VolumeSelectCombo and DirectoryCombo controls
        public const int MsidbControlAttributesRemovableVolume = 0x00010000;
        public const int MsidbControlAttributesFixedVolume = 0x00020000;
        public const int MsidbControlAttributesRemoteVolume = 0x00040000;
        public const int MsidbControlAttributesCDROMVolume = 0x00080000;
        public const int MsidbControlAttributesRAMDiskVolume = 0x00100000;
        public const int MsidbControlAttributesFloppyVolume = 0x00200000;

        // VolumeCostList controls
        public const int MsidbControlShowRollbackCost = 0x00400000;

        // ListBox and ComboBox controls
        public const int MsidbControlAttributesSorted = 0x00010000;
        public const int MsidbControlAttributesComboList = 0x00020000;

        // picture button controls
        public const int MsidbControlAttributesImageHandle = 0x00010000;
        public const int MsidbControlAttributesPushLike = 0x00020000;
        public const int MsidbControlAttributesBitmap = 0x00040000;
        public const int MsidbControlAttributesIcon = 0x00080000;
        public const int MsidbControlAttributesFixedSize = 0x00100000;
        public const int MsidbControlAttributesIconSize16 = 0x00200000;
        public const int MsidbControlAttributesIconSize32 = 0x00400000;
        public const int MsidbControlAttributesIconSize48 = 0x00600000;
        public const int MsidbControlAttributesElevationShield = 0x00800000;

        // RadioButton controls
        public const int MsidbControlAttributesHasBorder = 0x01000000;

        // CustomAction.Type
        // executable types
        public const int MsidbCustomActionTypeDll = 0x00000001;  // Target = entry point name
        public const int MsidbCustomActionTypeExe = 0x00000002;  // Target = command line args
        public const int MsidbCustomActionTypeTextData = 0x00000003;  // Target = text string to be formatted and set into property
        public const int MsidbCustomActionTypeJScript = 0x00000005;  // Target = entry point name; null if none to call
        public const int MsidbCustomActionTypeVBScript = 0x00000006;  // Target = entry point name; null if none to call
        public const int MsidbCustomActionTypeInstall = 0x00000007;  // Target = property list for nested engine initialization
        public const int MsidbCustomActionTypeSourceBits = 0x00000030;
        public const int MsidbCustomActionTypeTargetBits = 0x00000007;
        public const int MsidbCustomActionTypeReturnBits = 0x000000C0;
        public const int MsidbCustomActionTypeExecuteBits = 0x00000700;

        // source of code
        public const int MsidbCustomActionTypeBinaryData = 0x00000000;  // Source = Binary.Name; data stored in stream
        public const int MsidbCustomActionTypeSourceFile = 0x00000010;  // Source = File.File; file part of installation
        public const int MsidbCustomActionTypeDirectory = 0x00000020;  // Source = Directory.Directory; folder containing existing file
        public const int MsidbCustomActionTypeProperty = 0x00000030;  // Source = Property.Property; full path to executable

        // return processing; default is syncronous execution; process return code
        public const int MsidbCustomActionTypeContinue = 0x00000040;  // ignore action return status; continue running
        public const int MsidbCustomActionTypeAsync = 0x00000080;  // run asynchronously

        // execution scheduling flags; default is execute whenever sequenced
        public const int MsidbCustomActionTypeFirstSequence = 0x00000100;  // skip if UI sequence already run
        public const int MsidbCustomActionTypeOncePerProcess = 0x00000200;  // skip if UI sequence already run in same process
        public const int MsidbCustomActionTypeClientRepeat = 0x00000300;  // run on client only if UI already run on client
        public const int MsidbCustomActionTypeInScript = 0x00000400;  // queue for execution within script
        public const int MsidbCustomActionTypeRollback = 0x00000100;  // in conjunction with InScript: queue in Rollback script
        public const int MsidbCustomActionTypeCommit = 0x00000200;  // in conjunction with InScript: run Commit ops from script on success

        // security context flag; default to impersonate as user; valid only if InScript
        public const int MsidbCustomActionTypeNoImpersonate = 0x00000800;  // no impersonation; run in system context
        public const int MsidbCustomActionTypeTSAware = 0x00004000;  // impersonate for per-machine installs on TS machines
        public const int MsidbCustomActionType64BitScript = 0x00001000;  // script should run in 64bit process
        public const int MsidbCustomActionTypeHideTarget = 0x00002000;  // don't record the contents of the Target field in the log file.

        public const int MsidbCustomActionTypePatchUninstall = 0x00008000;  // run on patch uninstall

        // Dialog.Attributes
        public const int MsidbDialogAttributesVisible = 0x00000001;
        public const int MsidbDialogAttributesModal = 0x00000002;
        public const int MsidbDialogAttributesMinimize = 0x00000004;
        public const int MsidbDialogAttributesSysModal = 0x00000008;
        public const int MsidbDialogAttributesKeepModeless = 0x00000010;
        public const int MsidbDialogAttributesTrackDiskSpace = 0x00000020;
        public const int MsidbDialogAttributesUseCustomPalette = 0x00000040;
        public const int MsidbDialogAttributesRTLRO = 0x00000080;
        public const int MsidbDialogAttributesRightAligned = 0x00000100;
        public const int MsidbDialogAttributesLeftScroll = 0x00000200;
        public const int MsidbDialogAttributesBiDi = MsidbDialogAttributesRTLRO | MsidbDialogAttributesRightAligned | MsidbDialogAttributesLeftScroll;
        public const int MsidbDialogAttributesError = 0x00010000;
        public const int CommonControlAttributesInvert = MsidbControlAttributesVisible + MsidbControlAttributesEnabled;
        public const int DialogAttributesInvert = MsidbDialogAttributesVisible + MsidbDialogAttributesModal + MsidbDialogAttributesMinimize;

        // Feature.Attributes
        public const int MsidbFeatureAttributesFavorLocal = 0;
        public const int MsidbFeatureAttributesFavorSource = 1;
        public const int MsidbFeatureAttributesFollowParent = 2;
        public const int MsidbFeatureAttributesFavorAdvertise = 4;
        public const int MsidbFeatureAttributesDisallowAdvertise = 8;
        public const int MsidbFeatureAttributesUIDisallowAbsent = 16;
        public const int MsidbFeatureAttributesNoUnsupportedAdvertise = 32;

        // File.Attributes
        public const int MsidbFileAttributesReadOnly = 1;
        public const int MsidbFileAttributesHidden = 2;
        public const int MsidbFileAttributesSystem = 4;
        public const int MsidbFileAttributesVital = 512;
        public const int MsidbFileAttributesChecksum = 1024;
        public const int MsidbFileAttributesPatchAdded = 4096;
        public const int MsidbFileAttributesNoncompressed = 8192;
        public const int MsidbFileAttributesCompressed = 16384;

        // IniFile.Action & RemoveIniFile.Action
        public const int MsidbIniFileActionAddLine = 0;
        public const int MsidbIniFileActionCreateLine = 1;
        public const int MsidbIniFileActionRemoveLine = 2;
        public const int MsidbIniFileActionAddTag = 3;
        public const int MsidbIniFileActionRemoveTag = 4;

        // MoveFile.Options
        public const int MsidbMoveFileOptionsMove = 1;

        // ServiceInstall.Attributes
        public const int MsidbServiceInstallOwnProcess = 0x00000010;
        public const int MsidbServiceInstallShareProcess = 0x00000020;
        public const int MsidbServiceInstallInteractive = 0x00000100;
        public const int MsidbServiceInstallAutoStart = 0x00000002;
        public const int MsidbServiceInstallDemandStart = 0x00000003;
        public const int MsidbServiceInstallDisabled = 0x00000004;
        public const int MsidbServiceInstallErrorIgnore = 0x00000000;
        public const int MsidbServiceInstallErrorNormal = 0x00000001;
        public const int MsidbServiceInstallErrorCritical = 0x00000003;
        public const int MsidbServiceInstallErrorControlVital = 0x00008000;

        // ServiceConfig.Event
        public const int MsidbServiceConfigEventInstall = 0x00000001;
        public const int MsidbServiceConfigEventUninstall = 0x00000002;
        public const int MsidbServiceConfigEventReinstall = 0x00000004;

        // ServiceControl.Attributes
        public const int MsidbServiceControlEventStart = 0x00000001;
        public const int MsidbServiceControlEventStop = 0x00000002;
        public const int MsidbServiceControlEventDelete = 0x00000008;
        public const int MsidbServiceControlEventUninstallStart = 0x00000010;
        public const int MsidbServiceControlEventUninstallStop = 0x00000020;
        public const int MsidbServiceControlEventUninstallDelete = 0x00000080;

        // TextStyle.StyleBits
        public const int MsidbTextStyleStyleBitsBold = 1;
        public const int MsidbTextStyleStyleBitsItalic = 2;
        public const int MsidbTextStyleStyleBitsUnderline = 4;
        public const int MsidbTextStyleStyleBitsStrike = 8;

        // Upgrade.Attributes
        public const int MsidbUpgradeAttributesMigrateFeatures = 0x00000001;
        public const int MsidbUpgradeAttributesOnlyDetect = 0x00000002;
        public const int MsidbUpgradeAttributesIgnoreRemoveFailure = 0x00000004;
        public const int MsidbUpgradeAttributesVersionMinInclusive = 0x00000100;
        public const int MsidbUpgradeAttributesVersionMaxInclusive = 0x00000200;
        public const int MsidbUpgradeAttributesLanguagesExclusive = 0x00000400;

        // Registry Hive Roots
        public const int MsidbRegistryRootClassesRoot = 0;
        public const int MsidbRegistryRootCurrentUser = 1;
        public const int MsidbRegistryRootLocalMachine = 2;
        public const int MsidbRegistryRootUsers = 3;

        // Locator Types
        public const int MsidbLocatorTypeDirectory = 0;
        public const int MsidbLocatorTypeFileName = 1;
        public const int MsidbLocatorTypeRawValue = 2;
        public const int MsidbLocatorType64bit = 16;

        public const int MsidbClassAttributesRelativePath = 1;

        // RemoveFile.InstallMode
        public const int MsidbRemoveFileInstallModeOnInstall = 0x00000001;
        public const int MsidbRemoveFileInstallModeOnRemove = 0x00000002;
        public const int MsidbRemoveFileInstallModeOnBoth = 0x00000003;

        // ODBCDataSource.Registration
        public const int MsidbODBCDataSourceRegistrationPerMachine = 0;
        public const int MsidbODBCDataSourceRegistrationPerUser = 1;

        // ModuleConfiguration.Format
        public const int MsidbModuleConfigurationFormatText = 0;
        public const int MsidbModuleConfigurationFormatKey = 1;
        public const int MsidbModuleConfigurationFormatInteger = 2;
        public const int MsidbModuleConfigurationFormatBitfield = 3;

        // ModuleConfiguration.Attributes
        public const int MsidbMsmConfigurableOptionKeyNoOrphan = 1;
        public const int MsidbMsmConfigurableOptionNonNullable = 2;

        // ' Windows API function ShowWindow constants - used in Shortcut table
        public const int SWSHOWNORMAL = 0x00000001;
        public const int SWSHOWMAXIMIZED = 0x00000003;
        public const int SWSHOWMINNOACTIVE = 0x00000007;

        public const int MsidbEmbeddedUI = 0x01;
        public const int MsidbEmbeddedHandlesBasic = 0x02;

        public const int INSTALLLOGMODE_FATALEXIT = 0x00001;
        public const int INSTALLLOGMODE_ERROR = 0x00002;
        public const int INSTALLLOGMODE_WARNING = 0x00004;
        public const int INSTALLLOGMODE_USER = 0x00008;
        public const int INSTALLLOGMODE_INFO = 0x00010;
        public const int INSTALLLOGMODE_FILESINUSE = 0x00020;
        public const int INSTALLLOGMODE_RESOLVESOURCE = 0x00040;
        public const int INSTALLLOGMODE_OUTOFDISKSPACE = 0x00080;
        public const int INSTALLLOGMODE_ACTIONSTART = 0x00100;
        public const int INSTALLLOGMODE_ACTIONDATA = 0x00200;
        public const int INSTALLLOGMODE_PROGRESS = 0x00400;
        public const int INSTALLLOGMODE_COMMONDATA = 0x00800;
        public const int INSTALLLOGMODE_INITIALIZE = 0x01000;
        public const int INSTALLLOGMODE_TERMINATE = 0x02000;
        public const int INSTALLLOGMODE_SHOWDIALOG = 0x04000;
        public const int INSTALLLOGMODE_RMFILESINUSE = 0x02000000;
        public const int INSTALLLOGMODE_INSTALLSTART = 0x04000000;
        public const int INSTALLLOGMODE_INSTALLEND = 0x08000000;
    }
}
