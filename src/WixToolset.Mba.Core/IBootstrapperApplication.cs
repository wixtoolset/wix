// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
{
    using System;
    using System.CodeDom.Compiler;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Allows customization of the bootstrapper application.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("53C31D56-49C0-426B-AB06-099D717C67FE")]
    [GeneratedCodeAttribute("WixToolset.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public interface IBootstrapperApplication
    {
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnStartup();

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnShutdown(ref BOOTSTRAPPER_SHUTDOWN_ACTION action);

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnSystemShutdown(
            [MarshalAs(UnmanagedType.U4)] EndSessionReasons dwEndSession,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectBegin(
            [MarshalAs(UnmanagedType.Bool)] bool fInstalled,
            [MarshalAs(UnmanagedType.U4)] int cPackages,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectForwardCompatibleBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.U4)] RelationType relationType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleTag,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.U8)] long dw64Version,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel,
            [MarshalAs(UnmanagedType.Bool)] ref bool fIgnoreBundle
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectUpdateBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpdateLocation,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel,
            [MarshalAs(UnmanagedType.Bool)] ref bool fSkip
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectUpdate(
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpdateLocation,
            [MarshalAs(UnmanagedType.U8)] long dw64Size,
            [MarshalAs(UnmanagedType.U8)] long dw64Version,
            [MarshalAs(UnmanagedType.LPWStr)] string wzTitle,
            [MarshalAs(UnmanagedType.LPWStr)] string wzSummary,
            [MarshalAs(UnmanagedType.LPWStr)] string wzContentType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzContent,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel,
            [MarshalAs(UnmanagedType.Bool)] ref bool fStopProcessingUpdates
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectUpdateComplete(
            int hrStatus,
            [MarshalAs(UnmanagedType.Bool)] ref bool fIgnoreError
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectRelatedBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.U4)] RelationType relationType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleTag,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.U8)] long dw64Version,
            [MarshalAs(UnmanagedType.U4)] RelatedOperation operation,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectPackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectCompatibleMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageId,
            [MarshalAs(UnmanagedType.U8)] long dw64CompatiblePackageVersion,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectRelatedMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpgradeCode,
            [MarshalAs(UnmanagedType.LPWStr)] string wzProductCode,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.U8)] long dw64Version,
            [MarshalAs(UnmanagedType.U4)] RelatedOperation operation,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectTargetMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzProductCode,
            [MarshalAs(UnmanagedType.U4)] PackageState patchState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectMsiFeature(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzFeatureId,
            [MarshalAs(UnmanagedType.U4)] FeatureState state,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectPackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] PackageState state
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanBegin(
            [MarshalAs(UnmanagedType.U4)] int cPackages,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanRelatedBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanPackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanCompatibleMsiPackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageId,
            [MarshalAs(UnmanagedType.U8)] long dw64CompatiblePackageVersion,
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanCompatibleMsiPackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] PackageState state,
            [MarshalAs(UnmanagedType.U4)] RequestState requested,
            [MarshalAs(UnmanagedType.U4)] ActionState execute,
            [MarshalAs(UnmanagedType.U4)] ActionState rollback
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanTargetMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzProductCode,
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanMsiFeature(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzFeatureId,
            [MarshalAs(UnmanagedType.U4)] FeatureState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref FeatureState pRequestedState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanPackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] PackageState state,
            [MarshalAs(UnmanagedType.U4)] RequestState requested,
            [MarshalAs(UnmanagedType.U4)] ActionState execute,
            [MarshalAs(UnmanagedType.U4)] ActionState rollback
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnApplyBegin(
            [MarshalAs(UnmanagedType.U4)] int dwPhaseCount,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnElevateBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnElevateComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnProgress(
            [MarshalAs(UnmanagedType.U4)] int dwProgressPercentage,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnError(
            [MarshalAs(UnmanagedType.U4)] ErrorType errorType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int dwCode,
            [MarshalAs(UnmanagedType.LPWStr)] string wzError,
            [MarshalAs(UnmanagedType.I4)] int dwUIHint,
            [MarshalAs(UnmanagedType.U4)] int cData,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 5, ArraySubType = UnmanagedType.LPWStr), In] string[] rgwzData,
            [MarshalAs(UnmanagedType.I4)] Result nRecommendation,
            [MarshalAs(UnmanagedType.I4)] ref Result pResult
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRegisterBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRegisterComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCachePackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int cCachePayloads,
            [MarshalAs(UnmanagedType.U8)] long dw64PackageCacheSize,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheAcquireBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.U4)] CacheOperation operation,
            [MarshalAs(UnmanagedType.LPWStr)] string wzSource,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheAcquireProgress(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.U8)] long dw64Progress,
            [MarshalAs(UnmanagedType.U8)] long dw64Total,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnResolveSource(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzLocalSource,
            [MarshalAs(UnmanagedType.LPWStr)] string wzDownloadSource,
            BOOTSTRAPPER_RESOLVESOURCE_ACTION recommendation,
            ref BOOTSTRAPPER_RESOLVESOURCE_ACTION action,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheAcquireComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            int hrStatus,
            BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION recommendation,
            ref BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION pAction
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheVerifyBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheVerifyComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            int hrStatus,
            BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION recommendation,
            ref BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION action
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCachePackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION recommendation,
            ref BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION action
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteBegin(
            [MarshalAs(UnmanagedType.U4)] int cExecutingPackages,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecutePackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.Bool)] bool fExecute,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecutePatchTarget(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzTargetProductCode,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteProgress(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int dwProgressPercentage,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteMsiMessage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] InstallMessage messageType,
            [MarshalAs(UnmanagedType.I4)] int dwUIHint,
            [MarshalAs(UnmanagedType.LPWStr)] string wzMessage,
            [MarshalAs(UnmanagedType.U4)] int cData,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 4, ArraySubType = UnmanagedType.LPWStr), In] string[] rgwzData,
            [MarshalAs(UnmanagedType.I4)] Result nRecommendation,
            [MarshalAs(UnmanagedType.I4)] ref Result pResult
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteFilesInUse(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int cFiles,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.LPWStr), In] string[] rgwzFiles,
            [MarshalAs(UnmanagedType.I4)] Result nRecommendation,
            [MarshalAs(UnmanagedType.I4)] ref Result pResult
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecutePackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] ApplyRestart restart,
            [MarshalAs(UnmanagedType.I4)] BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION recommendation,
            [MarshalAs(UnmanagedType.I4)] ref BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION pAction
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnUnregisterBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnUnregisterComplete(
            int hrStatus
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnApplyComplete(
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] ApplyRestart restart,
            [MarshalAs(UnmanagedType.I4)] BOOTSTRAPPER_APPLYCOMPLETE_ACTION recommendation,
            [MarshalAs(UnmanagedType.I4)] ref BOOTSTRAPPER_APPLYCOMPLETE_ACTION pAction
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnLaunchApprovedExeBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnLaunchApprovedExeComplete(
            int hrStatus,
            int processId
            );

        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int BAProc(
            int message,
            IntPtr pvArgs,
            IntPtr pvResults,
            IntPtr pvContext
            );

        void BAProcFallback(
            int message,
            IntPtr pvArgs,
            IntPtr pvResults,
            ref int phr,
            IntPtr pvContext
            );
    }

    /// <summary>
    /// The display level for the BA.
    /// </summary>
    public enum Display
    {
        Unknown,
        Embedded,
        None,
        Passive,
        Full,
    }

    /// <summary>
    /// Messages from Windows Installer.
    /// </summary>
    public enum InstallMessage
    {
        FatalExit,
        Error = 0x01000000,
        Warning = 0x02000000,
        User = 0x03000000,
        Info = 0x04000000,
        FilesInUse = 0x05000000,
        ResolveSource = 0x06000000,
        OutOfDiskSpace = 0x07000000,
        ActionStart = 0x08000000,
        ActionData = 0x09000000,
        Progress = 0x0a000000,
        CommonData = 0x0b000000,
        Initialize = 0x0c000000,
        Terminate = 0x0d000000,
        ShowDialog = 0x0e000000,
        RMFilesInUse = 0x19000000,
    }

    /// <summary>
    /// The action to perform when a reboot is necessary.
    /// </summary>
    public enum Restart
    {
        Unknown,
        Never,
        Prompt,
        Automatic,
        Always,
    }

    /// <summary>
    /// Result codes.
    /// </summary>
    public enum Result
    {
        Error = -1,
        None,
        Ok,
        Cancel,
        Abort,
        Retry,
        Ignore,
        Yes,
        No,
        Close,
        Help,
        TryAgain,
        Continue,
    }

    /// <summary>
    /// Describes why a bundle or packaged is being resumed.
    /// </summary>
    public enum ResumeType
    {
        None,

        /// <summary>
        /// Resume information exists but is invalid.
        /// </summary>
        Invalid,

        /// <summary>
        /// The bundle was re-launched after an unexpected interruption.
        /// </summary>
        Interrupted,

        /// <summary>
        /// A reboot is pending.
        /// </summary>
        RebootPending,

        /// <summary>
        /// The bundle was re-launched after a reboot.
        /// </summary>
        Reboot,

        /// <summary>
        /// The bundle was re-launched after being suspended.
        /// </summary>
        Suspend,

        /// <summary>
        /// The bundle was launched from Add/Remove Programs.
        /// </summary>
        Arp,
    }

    /// <summary>
    /// Indicates what caused the error.
    /// </summary>
    public enum ErrorType
    {
        /// <summary>
        /// The error occurred trying to elevate.
        /// </summary>
        Elevate,

        /// <summary>
        /// The error came from the Windows Installer.
        /// </summary>
        WindowsInstaller,

        /// <summary>
        /// The error came from an EXE Package.
        /// </summary>
        ExePackage,

        /// <summary>
        /// The error came while trying to authenticate with an HTTP server.
        /// </summary>
        HttpServerAuthentication,

        /// <summary>
        /// The error came while trying to authenticate with an HTTP proxy.
        /// </summary>
        HttpProxyAuthentication,

        /// <summary>
        /// The error occurred during apply.
        /// </summary>
        Apply,
    };

    public enum RelatedOperation
    {
        None,

        /// <summary>
        /// The related bundle or package will be downgraded.
        /// </summary>
        Downgrade,

        /// <summary>
        /// The related package will be upgraded as a minor revision.
        /// </summary>
        MinorUpdate,

        /// <summary>
        /// The related bundle or package will be upgraded as a major revision.
        /// </summary>
        MajorUpgrade,

        /// <summary>
        /// The related bundle will be removed.
        /// </summary>
        Remove,

        /// <summary>
        /// The related bundle will be installed.
        /// </summary>
        Install,

        /// <summary>
        /// The related bundle will be repaired.
        /// </summary>
        Repair,
    };

    /// <summary>
    /// The cache operation used to acquire a container or payload.
    /// </summary>
    public enum CacheOperation
    {
        /// <summary>
        /// Container or payload is being copied.
        /// </summary>
        Copy,

        /// <summary>
        /// Container or payload is being downloaded.
        /// </summary>
        Download,

        /// <summary>
        /// Container or payload is being extracted.
        /// </summary>
        Extract
    }

    /// <summary>
    /// The restart state after a package or all packages were applied.
    /// </summary>
    public enum ApplyRestart
    {
        /// <summary>
        /// Package or chain does not require a restart.
        /// </summary>
        None,

        /// <summary>
        /// Package or chain requires a restart but it has not been initiated yet.
        /// </summary>
        RestartRequired,

        /// <summary>
        /// Package or chain has already initiated the restart.
        /// </summary>
        RestartInitiated
    }

    /// <summary>
    /// The relation type for related bundles.
    /// </summary>
    public enum RelationType
    {
        None,
        Detect,
        Upgrade,
        Addon,
        Patch,
        Dependent,
        Update,
    }

    /// <summary>
    /// One or more reasons why the application is requested to be closed or is being closed.
    /// </summary>
    [Flags]
    public enum EndSessionReasons
    {
        /// <summary>
        /// The system is shutting down or restarting (it is not possible to determine which event is occurring).
        /// </summary>
        Unknown,

        /// <summary>
        /// The application is using a file that must be replaced, the system is being serviced, or system resources are exhausted.
        /// </summary>
        CloseApplication,

        /// <summary>
        /// The application is forced to shut down.
        /// </summary>
        Critical = 0x40000000,

        /// <summary>
        /// The user is logging off.
        /// </summary>
        Logoff = unchecked((int)0x80000000)
    }

    /// <summary>
    /// The available actions for OnApplyComplete.
    /// </summary>
    public enum BOOTSTRAPPER_APPLYCOMPLETE_ACTION
    {
        None,
        Restart,
    }

    /// <summary>
    /// The available actions for OnCacheAcquireComplete.
    /// </summary>
    public enum BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION
    {
        None,
        Retry,
    }

    /// <summary>
    /// The available actions for OnCachePackageComplete.
    /// </summary>
    public enum BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION
    {
        None,
        Ignore,
        Retry,
    }

    /// <summary>
    /// The available actions for OnCacheVerifyComplete.
    /// </summary>
    public enum BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION
    {
        None,
        RetryVerification,
        RetryAcquisition,
    }

    /// <summary>
    /// The available actions for OnExecutePackageComplete.
    /// </summary>
    public enum BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION
    {
        None,
        Ignore,
        Retry,
        Restart,
        Suspend,
    }

    /// <summary>
    /// The available actions for OnResolveSource.
    /// </summary>
    public enum BOOTSTRAPPER_RESOLVESOURCE_ACTION
    {
        None,
        Retry,
        Download,
    }

    /// <summary>
    /// The available actions for OnShutdown.
    /// </summary>
    public enum BOOTSTRAPPER_SHUTDOWN_ACTION
    {
        None,
        Restart,
        ReloadBootstrapper,
    }
}
