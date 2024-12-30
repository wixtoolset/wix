// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.BootstrapperApplicationApi
{
    using System;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Allows customization of the bootstrapper application.
    /// </summary>
    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("53C31D56-49C0-426B-AB06-099D717C67FE")]
    public interface IBootstrapperApplication
    {
        /// <summary>
        /// Low level method that is called directly from the engine.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int BAProc(
            int message,
            IntPtr pvArgs,
            IntPtr pvResults
            );

        /// <summary>
        /// Low level method that is called directly from the engine.
        /// </summary>
        void BAProcFallback(
            int message,
            IntPtr pvArgs,
            IntPtr pvResults,
            ref int phr
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.Create"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCreate(IBootstrapperEngine engine, ref Command command);

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.Destroy"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDestroy(bool reload);

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.Startup"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnStartup();

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.Shutdown"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnShutdown(ref BOOTSTRAPPER_SHUTDOWN_ACTION action);

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectBegin(
            [MarshalAs(UnmanagedType.Bool)] bool fCached,
            [MarshalAs(UnmanagedType.U4)] RegistrationType registrationType,
            [MarshalAs(UnmanagedType.U4)] int cPackages,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectForwardCompatibleBundle"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectForwardCompatibleBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleCode,
            [MarshalAs(UnmanagedType.U4)] RelationType relationType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleTag,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion,
            [MarshalAs(UnmanagedType.Bool)] bool fMissingFromCache,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectUpdateBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectUpdateBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpdateLocation,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel,
            [MarshalAs(UnmanagedType.Bool)] ref bool fSkip
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectUpdate"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectUpdate(
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpdateLocation,
            [MarshalAs(UnmanagedType.U8)] long dw64Size,
            [MarshalAs(UnmanagedType.LPWStr)] string wzHash,
            [MarshalAs(UnmanagedType.U4)] UpdateHashType hashAlgorithm,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion,
            [MarshalAs(UnmanagedType.LPWStr)] string wzTitle,
            [MarshalAs(UnmanagedType.LPWStr)] string wzSummary,
            [MarshalAs(UnmanagedType.LPWStr)] string wzContentType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzContent,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel,
            [MarshalAs(UnmanagedType.Bool)] ref bool fStopProcessingUpdates
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectUpdateComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectUpdateComplete(
            int hrStatus,
            [MarshalAs(UnmanagedType.Bool)] ref bool fIgnoreError
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectRelatedBundle"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectRelatedBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleCode,
            [MarshalAs(UnmanagedType.U4)] RelationType relationType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleTag,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion,
            [MarshalAs(UnmanagedType.Bool)] bool fMissingFromCache,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectPackageBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectPackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectCompatibleMsiPackage"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectCompatibleMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageVersion,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectRelatedMsiPackage"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectRelatedMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpgradeCode,
            [MarshalAs(UnmanagedType.LPWStr)] string wzProductCode,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion,
            [MarshalAs(UnmanagedType.U4)] RelatedOperation operation,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectPatchTarget"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectPatchTarget(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzProductCode,
            [MarshalAs(UnmanagedType.U4)] PackageState patchState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectMsiFeature"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectMsiFeature(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzFeatureId,
            [MarshalAs(UnmanagedType.U4)] FeatureState state,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectPackageComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectPackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] PackageState state,
            [MarshalAs(UnmanagedType.Bool)] bool fCached
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectComplete(
            int hrStatus,
            [MarshalAs(UnmanagedType.Bool)] bool fEligibleForCleanup
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanBegin(
            [MarshalAs(UnmanagedType.U4)] int cPackages,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanRelatedBundle"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanRelatedBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleCode,
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanRollbackBoundary"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanRollbackBoundary(
            [MarshalAs(UnmanagedType.LPWStr)] string wzRollbackBoundaryId,
            [MarshalAs(UnmanagedType.Bool)] bool fRecommendedTransaction,
            [MarshalAs(UnmanagedType.Bool)] ref bool fTransaction,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanPackageBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanPackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] PackageState state,
            [MarshalAs(UnmanagedType.Bool)] bool fCached,
            [MarshalAs(UnmanagedType.U4)] BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition,
            [MarshalAs(UnmanagedType.U4)] BOOTSTRAPPER_PACKAGE_CONDITION_RESULT repairCondition,
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] BOOTSTRAPPER_CACHE_TYPE recommendedCacheType,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
            [MarshalAs(UnmanagedType.U4)] ref BOOTSTRAPPER_CACHE_TYPE pRequestedCacheType,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanCompatibleMsiPackageBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanCompatibleMsiPackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageVersion,
            [MarshalAs(UnmanagedType.Bool)] bool fRecommendedRemove,
            [MarshalAs(UnmanagedType.Bool)] ref bool fRequestRemove,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanCompatibleMsiPackageComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanCompatibleMsiPackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.Bool)] bool fRequestedRemove
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanPatchTarget"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanPatchTarget(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzProductCode,
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanMsiFeature"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanMsiFeature(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzFeatureId,
            [MarshalAs(UnmanagedType.U4)] FeatureState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref FeatureState pRequestedState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanMsiPackage"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.Bool)] bool fExecute,
            [MarshalAs(UnmanagedType.U4)] ActionState action,
            [MarshalAs(UnmanagedType.U4)] BOOTSTRAPPER_MSI_FILE_VERSIONING recommendedFileVersioning,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel,
            [MarshalAs(UnmanagedType.U4)] ref BURN_MSI_PROPERTY actionMsiProperty,
            [MarshalAs(UnmanagedType.U4)] ref INSTALLUILEVEL uiLevel,
            [MarshalAs(UnmanagedType.Bool)] ref bool fDisableExternalUiHandler,
            [MarshalAs(UnmanagedType.U4)] ref BOOTSTRAPPER_MSI_FILE_VERSIONING fileVersioning
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanPackageComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanPackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] RequestState requested
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlannedCompatiblePackage"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlannedCompatiblePackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzCompatiblePackageId,
            [MarshalAs(UnmanagedType.Bool)] bool fRemove
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlannedPackage"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlannedPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] ActionState execute,
            [MarshalAs(UnmanagedType.U4)] ActionState rollback,
            [MarshalAs(UnmanagedType.Bool)] bool fPlannedCache,
            [MarshalAs(UnmanagedType.Bool)] bool fPlannedUncache
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ApplyBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnApplyBegin(
            [MarshalAs(UnmanagedType.U4)] int dwPhaseCount,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ElevateBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnElevateBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ElevateComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnElevateComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.Progress"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnProgress(
            [MarshalAs(UnmanagedType.U4)] int dwProgressPercentage,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.Error"/>.
        /// </summary>
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

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.RegisterBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRegisterBegin(
            [MarshalAs(UnmanagedType.I4)] RegistrationType recommendedRegistrationType,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel,
            [MarshalAs(UnmanagedType.I4)] ref RegistrationType pRegistrationType
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.RegisterComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRegisterComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CachePackageBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCachePackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int cCachePayloads,
            [MarshalAs(UnmanagedType.U8)] long dw64PackageCacheSize,
            [MarshalAs(UnmanagedType.Bool)] bool fVital,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheAcquireBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheAcquireBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzSource,
            [MarshalAs(UnmanagedType.LPWStr)] string wzDownloadUrl,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadContainerId,
            [MarshalAs(UnmanagedType.U4)] CacheOperation recommendation,
            [MarshalAs(UnmanagedType.I4)] ref CacheOperation action,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheAcquireProgress"/>.
        /// </summary>
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

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheAcquireResolving"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheAcquireResolving(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3, ArraySubType = UnmanagedType.LPWStr), In] string[] searchPaths,
            [MarshalAs(UnmanagedType.U4)] int cSearchPaths,
            [MarshalAs(UnmanagedType.Bool)] bool fFoundLocal,
            [MarshalAs(UnmanagedType.U4)] int dwRecommendedSearchPath,
            [MarshalAs(UnmanagedType.LPWStr)] string wzDownloadUrl,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadContainerId,
            [MarshalAs(UnmanagedType.I4)] CacheResolveOperation recommendation,
            [MarshalAs(UnmanagedType.U4)] ref int dwChosenSearchPath,
            [MarshalAs(UnmanagedType.I4)] ref CacheResolveOperation action,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheAcquireComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheAcquireComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            int hrStatus,
            BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION recommendation,
            ref BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION pAction
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheVerifyBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheVerifyBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheVerifyProgress"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheVerifyProgress(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.U8)] long dw64Progress,
            [MarshalAs(UnmanagedType.U8)] long dw64Total,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage,
            [MarshalAs(UnmanagedType.I4)] CacheVerifyStep verifyStep,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheVerifyComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheVerifyComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            int hrStatus,
            BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION recommendation,
            ref BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION action
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CachePackageComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCachePackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION recommendation,
            ref BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION action
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheComplete"/>.
        /// </summary>
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecuteBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteBegin(
            [MarshalAs(UnmanagedType.U4)] int cExecutingPackages,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecutePackageBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecutePackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.Bool)] bool fExecute,
            [MarshalAs(UnmanagedType.U4)] ActionState action,
            [MarshalAs(UnmanagedType.U4)] INSTALLUILEVEL uiLevel,
            [MarshalAs(UnmanagedType.Bool)] bool fDisableExternalUiHandler,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecutePatchTarget"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecutePatchTarget(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzTargetProductCode,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecuteProgress"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteProgress(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int dwProgressPercentage,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecuteMsiMessage"/>.
        /// </summary>
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

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecuteFilesInUse"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteFilesInUse(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int cFiles,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.LPWStr), In] string[] rgwzFiles,
            [MarshalAs(UnmanagedType.I4)] Result nRecommendation,
            [MarshalAs(UnmanagedType.I4)] FilesInUseType source,
            [MarshalAs(UnmanagedType.I4)] ref Result pResult
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecutePackageComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecutePackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] ApplyRestart restart,
            [MarshalAs(UnmanagedType.I4)] BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION recommendation,
            [MarshalAs(UnmanagedType.I4)] ref BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION pAction
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecuteComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.UnregisterBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnUnregisterBegin(
            [MarshalAs(UnmanagedType.I4)] RegistrationType recommendedRegistrationType,
            [MarshalAs(UnmanagedType.I4)] ref RegistrationType pRegistrationType
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.UnregisterComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnUnregisterComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ApplyComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnApplyComplete(
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] ApplyRestart restart,
            [MarshalAs(UnmanagedType.I4)] BOOTSTRAPPER_APPLYCOMPLETE_ACTION recommendation,
            [MarshalAs(UnmanagedType.I4)] ref BOOTSTRAPPER_APPLYCOMPLETE_ACTION pAction
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.LaunchApprovedExeBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnLaunchApprovedExeBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.LaunchApprovedExeComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnLaunchApprovedExeComplete(
            int hrStatus,
            int processId
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.BeginMsiTransactionBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnBeginMsiTransactionBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.BeginMsiTransactionComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnBeginMsiTransactionComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CommitMsiTransactionBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCommitMsiTransactionBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CommitMsiTransactionComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCommitMsiTransactionComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] ApplyRestart restart,
            [MarshalAs(UnmanagedType.I4)] BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION recommendation,
            [MarshalAs(UnmanagedType.I4)] ref BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION pAction
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.RollbackMsiTransactionBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRollbackMsiTransactionBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.RollbackMsiTransactionComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRollbackMsiTransactionComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] ApplyRestart restart,
            [MarshalAs(UnmanagedType.I4)] BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION recommendation,
            [MarshalAs(UnmanagedType.I4)] ref BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION pAction
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PauseAutomaticUpdatesBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPauseAutomaticUpdatesBegin(
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PauseAutomaticUpdatesComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPauseAutomaticUpdatesComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.SystemRestorePointBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnSystemRestorePointBegin(
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.SystemRestorePointComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnSystemRestorePointComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanForwardCompatibleBundle"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanForwardCompatibleBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleCode,
            [MarshalAs(UnmanagedType.U4)] RelationType relationType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleTag,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion,
            [MarshalAs(UnmanagedType.Bool)] bool fRecommendedIgnoreBundle,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel,
            [MarshalAs(UnmanagedType.Bool)] ref bool fIgnoreBundle
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheContainerOrPayloadVerifyBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheContainerOrPayloadVerifyBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheContainerOrPayloadVerifyProgress"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheContainerOrPayloadVerifyProgress(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.U8)] long dw64Progress,
            [MarshalAs(UnmanagedType.U8)] long dw64Total,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheContainerOrPayloadVerifyComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheContainerOrPayloadVerifyComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CachePayloadExtractBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCachePayloadExtractBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CachePayloadExtractProgress"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCachePayloadExtractProgress(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageOrContainerId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            [MarshalAs(UnmanagedType.U8)] long dw64Progress,
            [MarshalAs(UnmanagedType.U8)] long dw64Total,
            [MarshalAs(UnmanagedType.U4)] int dwOverallPercentage,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CachePayloadExtractComplete"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCachePayloadExtractComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzPayloadId,
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanRestoreRelatedBundle"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanRestoreRelatedBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleCode,
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanRelatedBundleType"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanRelatedBundleType(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleCode,
            [MarshalAs(UnmanagedType.U4)] RelatedBundlePlanType recommendedType,
            [MarshalAs(UnmanagedType.U4)] ref RelatedBundlePlanType pRequestedType,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ApplyDowngrade"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnApplyDowngrade(
            [MarshalAs(UnmanagedType.I4)] int hrRecommended,
            [MarshalAs(UnmanagedType.I4)] ref int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecuteProcessCancel"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteProcessCancel(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int processId,
            [MarshalAs(UnmanagedType.I4)] BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION recommendation,
            [MarshalAs(UnmanagedType.I4)] ref BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION pAction
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectRelatedBundlePackage"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectRelatedBundlePackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleCode,
            [MarshalAs(UnmanagedType.U4)] RelationType relationType,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CachePackageNonVitalValidationFailure"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCachePackageNonVitalValidationFailure(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION recommendation,
            ref BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION action
            );
    }

    /// <summary>
    /// The display level for the BA.
    /// </summary>
    public enum Display
    {
        /// <summary>
        /// Invalid value.
        /// </summary>
        Unknown,

        /// <summary>
        /// The bundle is being run through the Burn protocol by an external application.
        /// </summary>
        Embedded,

        /// <summary>
        /// No UI should be shown.
        /// </summary>
        None,

        /// <summary>
        /// The UI should not require any interaction from the user.
        /// </summary>
        Passive,

        /// <summary>
        /// The UI should be fully interactive.
        /// </summary>
        Full,
    }

    /// <summary>
    /// Messages from Windows Installer (msi.h).
    /// </summary>
    public enum InstallMessage
    {
        /// <summary>
        /// premature termination, possibly fatal OOM
        /// </summary>
        FatalExit,

        /// <summary>
        /// formatted error message
        /// </summary>
        Error = 0x01000000,

        /// <summary>
        /// formatted warning message
        /// </summary>
        Warning = 0x02000000,

        /// <summary>
        /// user request message
        /// </summary>
        User = 0x03000000,

        /// <summary>
        /// informative message for log
        /// </summary>
        Info = 0x04000000,

        /// <summary>
        /// list of files in use that need to be replaced
        /// </summary>
        FilesInUse = 0x05000000,

        /// <summary>
        /// request to determine a valid source location
        /// </summary>
        ResolveSource = 0x06000000,

        /// <summary>
        /// insufficient disk space message
        /// </summary>
        OutOfDiskSpace = 0x07000000,

        /// <summary>
        /// start of action: action name &amp; description
        /// </summary>
        ActionStart = 0x08000000,

        /// <summary>
        /// formatted data associated with individual action item
        /// </summary>
        ActionData = 0x09000000,

        /// <summary>
        /// progress gauge info: units so far, total
        /// </summary>
        Progress = 0x0a000000,

        /// <summary>
        /// product info for dialog: language Id, dialog caption
        /// </summary>
        CommonData = 0x0b000000,

        /// <summary>
        /// sent prior to UI initialization, no string data
        /// </summary>
        Initialize = 0x0c000000,

        /// <summary>
        /// sent after UI termination, no string data
        /// </summary>
        Terminate = 0x0d000000,

        /// <summary>
        /// sent prior to display or authored dialog or wizard
        /// </summary>
        ShowDialog = 0x0e000000,

        /// <summary>
        /// log only, to log performance number like action time
        /// </summary>
        Performance = 0x0f000000,

        /// <summary>
        /// the list of apps that the user can request Restart Manager to shut down and restart
        /// </summary>
        RMFilesInUse = 0x19000000,

        /// <summary>
        /// sent prior to server-side install of a product
        /// </summary>
        InstallStart = 0x1a000000,

        /// <summary>
        /// sent after server-side install
        /// </summary>
        InstallEnd = 0x1B000000,
    }

    /// <summary>
    /// The action to perform when a reboot is necessary.
    /// </summary>
    public enum Restart
    {
        /// <summary>
        /// Invalid value.
        /// </summary>
        Unknown,

        /// <summary>
        /// The bundle should never initiate a restart.
        /// </summary>
        Never,

        /// <summary>
        /// The bundle should prompt the user whether to restart.
        /// </summary>
        Prompt,

        /// <summary>
        /// The bundle should restart if necessary.
        /// </summary>
        Automatic,

        /// <summary>
        /// The bundle should always restart when given the option.
        /// </summary>
        Always,
    }

    /// <summary>
    /// The display name to use when registering in Add/Remove Programs.
    /// </summary>
    public enum RegistrationType
    {
        /// <summary>
        /// No registration.
        /// The engine will ignore None if it recommended InProgress or Full.
        /// </summary>
        None,

        /// <summary>
        /// The in-progress display name.
        /// </summary>
        InProgress,

        /// <summary>
        /// The default display name.
        /// </summary>
        Full,
    }

    /// <summary>
    /// Result codes (based on Dialog Box Command IDs from WinUser.h).
    /// </summary>
    public enum Result
    {
        /// <summary>
        /// An error occurred.
        /// </summary>
        Error = -1,

        /// <summary>
        /// Invalid value.
        /// </summary>
        None,

        /// <summary>
        /// IDOK
        /// </summary>
        Ok,

        /// <summary>
        /// IDCANCEL
        /// </summary>
        Cancel,

        /// <summary>
        /// IDABORT
        /// </summary>
        Abort,

        /// <summary>
        /// IDRETRY
        /// </summary>
        Retry,

        /// <summary>
        /// IDIGNORE
        /// </summary>
        Ignore,

        /// <summary>
        /// IDYES
        /// </summary>
        Yes,

        /// <summary>
        /// IDNO
        /// </summary>
        No,

        /// <summary>
        /// IDCLOSE
        /// </summary>
        Close,

        /// <summary>
        /// IDHELP
        /// </summary>
        Help,

        /// <summary>
        /// IDTRYAGAIN
        /// </summary>
        TryAgain,

        /// <summary>
        /// IDCONTINUE
        /// </summary>
        Continue,
    }

    /// <summary>
    /// Describes why a bundle or packaged is being resumed.
    /// </summary>
    public enum ResumeType
    {
        /// <summary>
        /// No resume information.
        /// </summary>
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

    /// <summary>
    /// Indicates the source of the FilesInUse message.
    /// </summary>
    public enum FilesInUseType
    {
        /// <summary>
        /// Generated from INSTALLMESSAGE_FILESINUSE.
        /// </summary>
        Msi,
        /// <summary>
        /// Generated from INSTALLMESSAGE_RMFILESINUSE.
        /// </summary>
        MsiRm,
        /// <summary>
        /// Generated from MMIO_CLOSE_APPS.
        /// </summary>
        Netfx,
    }

    /// <summary>
    /// The calculated operation for the related MSI package.
    /// </summary>
    public enum RelatedOperation
    {
        /// <summary>
        /// No relation.
        /// </summary>
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
        /// There is no source available.
        /// </summary>
        None,

        /// <summary>
        /// Copy the payload or container from the chosen local source.
        /// </summary>
        Copy,

        /// <summary>
        /// Download the payload or container using the download URL.
        /// </summary>
        Download,

        /// <summary>
        /// Extract the payload from the container.
        /// </summary>
        Extract,
    }

    /// <summary>
    /// The source to be used to acquire a container or payload.
    /// </summary>
    public enum CacheResolveOperation
    {
        /// <summary>
        /// There is no source available.
        /// </summary>
        None,

        /// <summary>
        /// Copy the payload or container from the chosen local source.
        /// </summary>
        Local,

        /// <summary>
        /// Download the payload or container from the download URL.
        /// </summary>
        Download,

        /// <summary>
        /// Extract the payload from the container.
        /// </summary>
        Container,

        /// <summary>
        /// Look again for the payload or container locally.
        /// </summary>
        Retry,
    }

    /// <summary>
    /// The current step when verifying a container or payload.
    /// </summary>
    public enum CacheVerifyStep
    {
        /// <summary>
        /// Copying or moving the file from the working path to the unverified path.
        /// Not used during Layout.
        /// </summary>
        Stage,
        /// <summary>
        /// Hashing the file.
        /// </summary>
        Hash,
        /// <summary>
        /// Copying or moving the file to the final location.
        /// </summary>
        Finalize,
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
        /// <summary>
        /// No relation.
        /// </summary>
        None,

        /// <summary>
        /// The related bundle is detected by the running bundle.
        /// This relationship is reversed for <see cref="IBootstrapperCommand.Relation" />
        /// </summary>
        Detect,

        /// <summary>
        /// The related bundle shares an upgrade code with the running bundle.
        /// This relationship is reversed for <see cref="IBootstrapperCommand.Relation" />
        /// </summary>
        Upgrade,

        /// <summary>
        /// The related bundle is an add-on for the running bundle.
        /// This relationship is reversed for <see cref="IBootstrapperCommand.Relation" />
        /// </summary>
        Addon,

        /// <summary>
        /// The related bundle is a patch for the running bundle.
        /// This relationship is reversed for <see cref="IBootstrapperCommand.Relation" />
        /// </summary>
        Patch,

        /// <summary>
        /// The running bundle is an add-on for the related bundle.
        /// This relationship is reversed for <see cref="IBootstrapperCommand.Relation" />
        /// </summary>
        DependentAddon,

        /// <summary>
        /// The running bundle is a patch for the related bundle.
        /// This relationship is reversed for <see cref="IBootstrapperCommand.Relation" />
        /// </summary>
        DependentPatch,

        /// <summary>
        /// The related bundle is a newer version of the running bundle.
        /// This relationship is reversed for <see cref="IBootstrapperCommand.Relation" />
        /// </summary>
        Update,

        /// <summary>
        /// The related bundle is in the running bundle's chain.
        /// This relationship is reversed for <see cref="IBootstrapperCommand.Relation" />
        /// </summary>
        ChainPackage,
    }

    /// <summary>
    /// The planned relation type for related bundles.
    /// </summary>
    public enum RelatedBundlePlanType
    {
        /// <summary>
        /// Don't execute the related bundle.
        /// </summary>
        None,

        /// <summary>
        /// The running bundle is a downgrade for the related bundle.
        /// </summary>
        Downgrade,

        /// <summary>
        /// The running bundle is an upgrade for the related bundle.
        /// </summary>
        Upgrade,

        /// <summary>
        /// The related bundle is an add-on of the running bundle.
        /// </summary>
        Addon,

        /// <summary>
        /// The related bundle is a patch for the running bundle.
        /// </summary>
        Patch,

        /// <summary>
        /// The running bundle is an add-on for the related bundle.
        /// </summary>
        DependentAddon,

        /// <summary>
        /// The running bundle is a patch for the related bundle.
        /// </summary>
        DependentPatch,
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
    /// The available actions for <see cref="IDefaultBootstrapperApplication.ApplyComplete"/>.
    /// </summary>
    public enum BOOTSTRAPPER_APPLYCOMPLETE_ACTION
    {
        /// <summary>
        /// Instructs the engine to not take any special action.
        /// </summary>
        None,

        /// <summary>
        /// Instructs the engine to restart.
        /// The engine will not launch again after the machine is rebooted.
        /// Ignored if reboot was already initiated by <see cref="IDefaultBootstrapperApplication.ExecutePackageComplete"/>.
        /// </summary>
        Restart,
    }

    /// <summary>
    /// The cache strategy to be used for the package.
    /// </summary>
    public enum BOOTSTRAPPER_CACHE_TYPE
    {
        /// <summary>
        /// The package will be cached in order to securely run the package, but will always be cleaned from the cache at the end.
        /// </summary>
        Remove,

        /// <summary>
        /// The package will be cached in order to run the package, and then kept in the cache until the package is uninstalled.
        /// </summary>
        Keep,

        /// <summary>
        /// The package will always be cached and stay in the cache, unless the package and bundle are both being uninstalled.
        /// </summary>
        Force,
    }

    /// <summary>
    /// The available actions for <see cref="IDefaultBootstrapperApplication.CacheAcquireComplete"/>.
    /// </summary>
    public enum BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION
    {
        /// <summary>
        /// Instructs the engine to not take any special action.
        /// </summary>
        None,

        /// <summary>
        /// Instructs the engine to try the acquisition of the payload again.
        /// Ignored if hrStatus is a success.
        /// </summary>
        Retry,
    }

    /// <summary>
    /// The available actions for <see cref="IDefaultBootstrapperApplication.CachePackageComplete"/>.
    /// </summary>
    public enum BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION
    {
        /// <summary>
        /// Instructs the engine to not take any special action.
        /// </summary>
        None,

        /// <summary>
        /// Instructs the engine to ignore non-vital package failures and continue with the caching.
        /// Ignored if hrStatus is a success or the package is vital.
        /// </summary>
        Ignore,

        /// <summary>
        /// Instructs the engine to try the acquisition and verification of the package again.
        /// Ignored if hrStatus is a success.
        /// </summary>
        Retry,
    }

    /// <summary>
    /// The available actions for <see cref="IDefaultBootstrapperApplication.CachePackageNonVitalValidationFailure"/>
    /// </summary>
    public enum BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION
    {
        /// <summary>
        /// Instructs the engine to not take any special action.
        /// </summary>
        None,

        /// <summary>
        /// Instructs the engine to try to acquire the package so execution can use it.
        /// Most of the time this is used for installing the package during rollback.
        /// </summary>
        Acquire,
    }

    /// <summary>
    /// The available actions for <see cref="IDefaultBootstrapperApplication.CacheVerifyComplete"/>.
    /// </summary>
    public enum BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION
    {
        /// <summary>
        /// Instructs the engine to not take any special action.
        /// </summary>
        None,

        /// <summary>
        /// Ignored if hrStatus is a success.
        /// </summary>
        RetryVerification,

        /// <summary>
        /// Ignored if hrStatus is a success.
        /// </summary>
        RetryAcquisition,
    }

    /// <summary>
    /// The available actions for <see cref="IDefaultBootstrapperApplication.ExecutePackageComplete"/>.
    /// </summary>
    public enum BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION
    {
        /// <summary>
        /// Instructs the engine to not take any special action.
        /// </summary>
        None,

        /// <summary>
        /// Instructs the engine to ignore non-vital package failures and continue with the install.
        /// Ignored if hrStatus is a success or the package is vital.
        /// </summary>
        Ignore,

        /// <summary>
        /// Instructs the engine to try the execution of the package again.
        /// Ignored if hrStatus is a success.
        /// </summary>
        Retry,

        /// <summary>
        /// Instructs the engine to stop processing the chain and restart.
        /// The engine will launch again after the machine is restarted.
        /// </summary>
        Restart,

        /// <summary>
        /// Instructs the engine to stop processing the chain and suspend the current state.
        /// </summary>
        Suspend,
    }

    /// <summary>
    /// The available actions for <see cref="IDefaultBootstrapperApplication.CommitMsiTransactionComplete"/> and <see cref="IDefaultBootstrapperApplication.RollbackMsiTransactionComplete"/>.
    /// </summary>
    public enum BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION
    {
        /// <summary>
        /// Instructs the engine to not take any special action.
        /// </summary>
        None,

        /// <summary>
        /// Instructs the engine to stop processing the chain and restart.
        /// The engine will launch again after the machine is restarted.
        /// </summary>
        Restart,
    };

    /// <summary>
    /// The available actions for <see cref="IDefaultBootstrapperApplication.ExecuteProcessCancel"/>.
    /// </summary>
    public enum BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION
    {
        /// <summary>
        /// Instructs the engine to stop waiting for the process to exit.
        /// The package is immediately considered to have failed with ERROR_INSTALL_USEREXIT.
        /// The engine will never rollback the package.
        /// </summary>
        Abandon,

        /// <summary>
        /// Instructs the engine to wait for the process to exit.
        /// Once the process has exited, the package is considered to have failed with ERROR_INSTALL_USEREXIT.
        /// This allows the engine to rollback the package if necessary.
        /// </summary>
        Wait,
    }

    /// <summary>
    /// The result of evaluating a condition from a package.
    /// </summary>
    public enum BOOTSTRAPPER_PACKAGE_CONDITION_RESULT
    {
        /// <summary>
        /// No condition was authored.
        /// </summary>
        Default,

        /// <summary>
        /// Evaluated to false.
        /// </summary>
        False,

        /// <summary>
        /// Evaluated to true.
        /// </summary>
        True,
    }

    /// <summary>
    /// The available actions for <see cref="IDefaultBootstrapperApplication.CacheAcquireResolving"/>.
    /// </summary>
    public enum BOOTSTRAPPER_RESOLVESOURCE_ACTION
    {
        /// <summary>
        /// Instructs the engine that the source can't be found.
        /// </summary>
        None,

        /// <summary>
        /// Instructs the engine to try the local source again.
        /// </summary>
        Retry,

        /// <summary>
        /// Instructs the engine to try the download source.
        /// </summary>
        Download,
    }

    /// <summary>
    /// The available actions for <see cref="IDefaultBootstrapperApplication.Shutdown"/>.
    /// </summary>
    public enum BOOTSTRAPPER_SHUTDOWN_ACTION
    {
        /// <summary>
        /// Instructs the engine to not take any special action.
        /// </summary>
        None,

        /// <summary>
        /// Instructs the engine to restart.
        /// The engine will not launch again after the machine is rebooted.
        /// Ignored if reboot was already initiated by <see cref="IDefaultBootstrapperApplication.ExecutePackageComplete"/>.
        /// </summary>
        Restart,

        /// <summary>
        /// Instructs the engine to unload the bootstrapper application and
        /// restart the engine which will load the bootstrapper application again.
        /// Typically used to switch from a native bootstrapper application to a managed one.
        /// </summary>
        ReloadBootstrapper,

        /// <summary>
        /// Opts out of the engine behavior of trying to uninstall itself when no non-permanent packages are installed.
        /// </summary>
        SkipCleanup,
    }

    /// <summary>
    /// The file versioning options for REINSTALLMODE, see https://docs.microsoft.com/en-us/windows/win32/msi/reinstallmode.
    /// </summary>
    public enum BOOTSTRAPPER_MSI_FILE_VERSIONING
    {
        /// <summary>
        /// o
        /// </summary>
        Older,
        /// <summary>
        /// e
        /// </summary>
        Equal,
        /// <summary>
        /// a
        /// </summary>
        All,
    }

    /// <summary>
    /// The property Burn will add so the MSI can know the planned action for the package.
    /// </summary>
    public enum BURN_MSI_PROPERTY
    {
        /// <summary>
        /// No property will be added.
        /// </summary>
        None,

        /// <summary>
        /// Add BURNMSIINSTALL=1
        /// </summary>
        Install,

        /// <summary>
        /// Add BURNMSIMODFIY=1
        /// </summary>
        Modify,

        /// <summary>
        /// Add BURNMSIREPAIR=1
        /// </summary>
        Repair,

        /// <summary>
        /// Add BURNMSIUNINSTALL=1
        /// </summary>
        Uninstall,
    }

    /// <summary>
    /// From msi.h
    /// https://docs.microsoft.com/en-us/windows/win32/api/msi/nf-msi-msisetinternalui
    /// </summary>
    [Flags]
    public enum INSTALLUILEVEL
    {
        /// <summary>
        /// UI level is unchanged
        /// </summary>
        NoChange = 0,

        /// <summary>
        /// default UI is used
        /// </summary>
        Default = 1,

        /// <summary>
        /// completely silent installation
        /// </summary>
        None = 2,

        /// <summary>
        /// simple progress and error handling
        /// </summary>
        Basic = 3,

        /// <summary>
        /// authored UI, wizard dialogs suppressed
        /// </summary>
        Reduced = 4,

        /// <summary>
        /// authored UI with wizards, progress, errors
        /// </summary>
        Full = 5,

        /// <summary>
        /// display success/failure dialog at end of install
        /// </summary>
        EndDialog = 0x80,

        /// <summary>
        /// display only progress dialog
        /// </summary>
        ProgressOnly = 0x40,

        /// <summary>
        /// do not display the cancel button in basic UI
        /// </summary>
        HideCancel = 0x20,

        /// <summary>
        /// force display of source resolution even if quiet
        /// </summary>
        SourceResOnly = 0x100,

        /// <summary>
        /// show UAC prompt even if quiet
        /// Can only be used if on Windows Installer 5.0 or later
        /// </summary>
        UacOnly = 0x200,
    }
}
