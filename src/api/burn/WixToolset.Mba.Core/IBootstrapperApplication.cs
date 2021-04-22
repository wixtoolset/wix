// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Mba.Core
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
            IntPtr pvResults,
            IntPtr pvContext
            );

        /// <summary>
        /// Low level method that is called directly from the engine.
        /// </summary>
        void BAProcFallback(
            int message,
            IntPtr pvArgs,
            IntPtr pvResults,
            ref int phr,
            IntPtr pvContext
            );

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
        /// See <see cref="IDefaultBootstrapperApplication.SystemShutdown"/>.
        /// </summary>
        /// <param name="dwEndSession"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnSystemShutdown(
            [MarshalAs(UnmanagedType.U4)] EndSessionReasons dwEndSession,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectBegin"/>.
        /// </summary>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectBegin(
            [MarshalAs(UnmanagedType.Bool)] bool fCached,
            [MarshalAs(UnmanagedType.Bool)] bool fInstalled,
            [MarshalAs(UnmanagedType.U4)] int cPackages,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectForwardCompatibleBundle"/>.
        /// </summary>
        /// <param name="wzBundleId"></param>
        /// <param name="relationType"></param>
        /// <param name="wzBundleTag"></param>
        /// <param name="fPerMachine"></param>
        /// <param name="wzVersion"></param>
        /// <param name="fMissingFromCache"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectForwardCompatibleBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
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
        /// <param name="wzUpdateLocation"></param>
        /// <param name="fCancel"></param>
        /// <param name="fSkip"></param>
        /// <returns></returns>
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
        /// <param name="wzUpdateLocation"></param>
        /// <param name="dw64Size"></param>
        /// <param name="wzVersion"></param>
        /// <param name="wzTitle"></param>
        /// <param name="wzSummary"></param>
        /// <param name="wzContentType"></param>
        /// <param name="wzContent"></param>
        /// <param name="fCancel"></param>
        /// <param name="fStopProcessingUpdates"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectUpdate(
            [MarshalAs(UnmanagedType.LPWStr)] string wzUpdateLocation,
            [MarshalAs(UnmanagedType.U8)] long dw64Size,
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
        /// <param name="hrStatus"></param>
        /// <param name="fIgnoreError"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectUpdateComplete(
            int hrStatus,
            [MarshalAs(UnmanagedType.Bool)] ref bool fIgnoreError
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectRelatedBundle"/>.
        /// </summary>
        /// <param name="wzBundleId"></param>
        /// <param name="relationType"></param>
        /// <param name="wzBundleTag"></param>
        /// <param name="fPerMachine"></param>
        /// <param name="wzVersion"></param>
        /// <param name="operation"></param>
        /// <param name="fMissingFromCache"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectRelatedBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.U4)] RelationType relationType,
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleTag,
            [MarshalAs(UnmanagedType.Bool)] bool fPerMachine,
            [MarshalAs(UnmanagedType.LPWStr)] string wzVersion,
            [MarshalAs(UnmanagedType.U4)] RelatedOperation operation,
            [MarshalAs(UnmanagedType.Bool)] bool fMissingFromCache,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectPackageBegin"/>.
        /// </summary>
        /// <param name="wzPackageId"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectPackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.DetectRelatedMsiPackage"/>.
        /// </summary>
        /// <param name="wzPackageId"></param>
        /// <param name="wzUpgradeCode"></param>
        /// <param name="wzProductCode"></param>
        /// <param name="fPerMachine"></param>
        /// <param name="wzVersion"></param>
        /// <param name="operation"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
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
        /// <param name="wzPackageId"></param>
        /// <param name="wzProductCode"></param>
        /// <param name="patchState"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
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
        /// <param name="wzPackageId"></param>
        /// <param name="wzFeatureId"></param>
        /// <param name="state"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
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
        /// <param name="hrStatus"></param>
        /// <param name="fEligibleForCleanup"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnDetectComplete(
            int hrStatus,
            [MarshalAs(UnmanagedType.Bool)] bool fEligibleForCleanup
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanBegin"/>.
        /// </summary>
        /// <param name="cPackages"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanBegin(
            [MarshalAs(UnmanagedType.U4)] int cPackages,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanRelatedBundle"/>.
        /// </summary>
        /// <param name="wzBundleId"></param>
        /// <param name="recommendedState"></param>
        /// <param name="pRequestedState"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanRelatedBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
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
            [MarshalAs(UnmanagedType.U4)] RequestState recommendedState,
            [MarshalAs(UnmanagedType.U4)] BOOTSTRAPPER_CACHE_TYPE recommendedCacheType,
            [MarshalAs(UnmanagedType.U4)] ref RequestState pRequestedState,
            [MarshalAs(UnmanagedType.U4)] ref BOOTSTRAPPER_CACHE_TYPE pRequestedCacheType,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanPatchTarget"/>.
        /// </summary>
        /// <param name="wzPackageId"></param>
        /// <param name="wzProductCode"></param>
        /// <param name="recommendedState"></param>
        /// <param name="pRequestedState"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
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
        /// <param name="wzPackageId"></param>
        /// <param name="wzFeatureId"></param>
        /// <param name="recommendedState"></param>
        /// <param name="pRequestedState"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
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
        /// <param name="wzPackageId"></param>
        /// <param name="fExecute"></param>
        /// <param name="action"></param>
        /// <param name="fCancel"></param>
        /// <param name="actionMsiProperty"></param>
        /// <param name="uiLevel"></param>
        /// <param name="fDisableExternalUiHandler"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanMsiPackage(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.Bool)] bool fExecute,
            [MarshalAs(UnmanagedType.U4)] ActionState action,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel,
            [MarshalAs(UnmanagedType.U4)] ref BURN_MSI_PROPERTY actionMsiProperty,
            [MarshalAs(UnmanagedType.U4)] ref INSTALLUILEVEL uiLevel,
            [MarshalAs(UnmanagedType.Bool)] ref bool fDisableExternalUiHandler
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanPackageComplete"/>.
        /// </summary>
        /// <param name="wzPackageId"></param>
        /// <param name="hrStatus"></param>
        /// <param name="requested"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanPackageComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            int hrStatus,
            [MarshalAs(UnmanagedType.U4)] RequestState requested
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
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ApplyBegin"/>.
        /// </summary>
        /// <param name="dwPhaseCount"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnApplyBegin(
            [MarshalAs(UnmanagedType.U4)] int dwPhaseCount,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ElevateBegin"/>.
        /// </summary>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnElevateBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ElevateComplete"/>.
        /// </summary>
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnElevateComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.Progress"/>.
        /// </summary>
        /// <param name="dwProgressPercentage"></param>
        /// <param name="dwOverallPercentage"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
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
        /// <param name="errorType"></param>
        /// <param name="wzPackageId"></param>
        /// <param name="dwCode"></param>
        /// <param name="wzError"></param>
        /// <param name="dwUIHint"></param>
        /// <param name="cData"></param>
        /// <param name="rgwzData"></param>
        /// <param name="nRecommendation"></param>
        /// <param name="pResult"></param>
        /// <returns></returns>
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
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRegisterBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.RegisterComplete"/>.
        /// </summary>
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRegisterComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CacheBegin"/>.
        /// </summary>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCacheBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CachePackageBegin"/>.
        /// </summary>
        /// <param name="wzPackageId"></param>
        /// <param name="cCachePayloads"></param>
        /// <param name="dw64PackageCacheSize"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCachePackageBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int cCachePayloads,
            [MarshalAs(UnmanagedType.U8)] long dw64PackageCacheSize,
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
        /// <param name="wzPackageId"></param>
        /// <param name="hrStatus"></param>
        /// <param name="recommendation"></param>
        /// <param name="action"></param>
        /// <returns></returns>
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
        /// <param name="cExecutingPackages"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteBegin(
            [MarshalAs(UnmanagedType.U4)] int cExecutingPackages,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecutePackageBegin"/>.
        /// </summary>
        /// <param name="wzPackageId"></param>
        /// <param name="fExecute"></param>
        /// <param name="action"></param>
        /// <param name="uiLevel"></param>
        /// <param name="fDisableExternalUiHandler"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
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
        /// <param name="wzPackageId"></param>
        /// <param name="wzTargetProductCode"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
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
        /// <param name="wzPackageId"></param>
        /// <param name="dwProgressPercentage"></param>
        /// <param name="dwOverallPercentage"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
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
        /// <param name="wzPackageId"></param>
        /// <param name="messageType"></param>
        /// <param name="dwUIHint"></param>
        /// <param name="wzMessage"></param>
        /// <param name="cData"></param>
        /// <param name="rgwzData"></param>
        /// <param name="nRecommendation"></param>
        /// <param name="pResult"></param>
        /// <returns></returns>
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
        /// <param name="wzPackageId"></param>
        /// <param name="cFiles"></param>
        /// <param name="rgwzFiles"></param>
        /// <param name="nRecommendation"></param>
        /// <param name="pResult"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteFilesInUse(
            [MarshalAs(UnmanagedType.LPWStr)] string wzPackageId,
            [MarshalAs(UnmanagedType.U4)] int cFiles,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1, ArraySubType = UnmanagedType.LPWStr), In] string[] rgwzFiles,
            [MarshalAs(UnmanagedType.I4)] Result nRecommendation,
            [MarshalAs(UnmanagedType.I4)] ref Result pResult
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ExecutePackageComplete"/>.
        /// </summary>
        /// <param name="wzPackageId"></param>
        /// <param name="hrStatus"></param>
        /// <param name="restart"></param>
        /// <param name="recommendation"></param>
        /// <param name="pAction"></param>
        /// <returns></returns>
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
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnExecuteComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.UnregisterBegin"/>.
        /// </summary>
        /// <param name="fKeepRegistration"></param>
        /// <param name="fForceKeepRegistration"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnUnregisterBegin(
            [MarshalAs(UnmanagedType.Bool)] bool fKeepRegistration,
            [MarshalAs(UnmanagedType.Bool)] ref bool fForceKeepRegistration
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.UnregisterComplete"/>.
        /// </summary>
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnUnregisterComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.ApplyComplete"/>.
        /// </summary>
        /// <param name="hrStatus"></param>
        /// <param name="restart"></param>
        /// <param name="recommendation"></param>
        /// <param name="pAction"></param>
        /// <returns></returns>
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
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnLaunchApprovedExeBegin(
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.LaunchApprovedExeComplete"/>.
        /// </summary>
        /// <param name="hrStatus"></param>
        /// <param name="processId"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnLaunchApprovedExeComplete(
            int hrStatus,
            int processId
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.BeginMsiTransactionBegin"/>.
        /// </summary>
        /// <param name="wzTransactionId"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnBeginMsiTransactionBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.BeginMsiTransactionComplete"/>.
        /// </summary>
        /// <param name="wzTransactionId"></param>
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnBeginMsiTransactionComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CommitMsiTransactionBegin"/>.
        /// </summary>
        /// <param name="wzTransactionId"></param>
        /// <param name="fCancel"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCommitMsiTransactionBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            [MarshalAs(UnmanagedType.Bool)] ref bool fCancel
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.CommitMsiTransactionComplete"/>.
        /// </summary>
        /// <param name="wzTransactionId"></param>
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnCommitMsiTransactionComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.RollbackMsiTransactionBegin"/>.
        /// </summary>
        /// <param name="wzTransactionId"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRollbackMsiTransactionBegin(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.RollbackMsiTransactionComplete"/>.
        /// </summary>
        /// <param name="wzTransactionId"></param>
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnRollbackMsiTransactionComplete(
            [MarshalAs(UnmanagedType.LPWStr)] string wzTransactionId,
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PauseAutomaticUpdatesBegin"/>.
        /// </summary>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPauseAutomaticUpdatesBegin(
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PauseAutomaticUpdatesComplete"/>.
        /// </summary>
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPauseAutomaticUpdatesComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.SystemRestorePointBegin"/>.
        /// </summary>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnSystemRestorePointBegin(
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.SystemRestorePointComplete"/>.
        /// </summary>
        /// <param name="hrStatus"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnSystemRestorePointComplete(
            int hrStatus
            );

        /// <summary>
        /// See <see cref="IDefaultBootstrapperApplication.PlanForwardCompatibleBundle"/>.
        /// </summary>
        /// <param name="wzBundleId"></param>
        /// <param name="relationType"></param>
        /// <param name="wzBundleTag"></param>
        /// <param name="fPerMachine"></param>
        /// <param name="wzVersion"></param>
        /// <param name="fRecommendedIgnoreBundle"></param>
        /// <param name="fCancel"></param>
        /// <param name="fIgnoreBundle"></param>
        /// <returns></returns>
        [PreserveSig]
        [return: MarshalAs(UnmanagedType.I4)]
        int OnPlanForwardCompatibleBundle(
            [MarshalAs(UnmanagedType.LPWStr)] string wzBundleId,
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
    }

    /// <summary>
    /// The display level for the BA.
    /// </summary>
    public enum Display
    {
        /// <summary>
        /// 
        /// </summary>
        Unknown,

        /// <summary>
        /// 
        /// </summary>
        Embedded,

        /// <summary>
        /// 
        /// </summary>
        None,

        /// <summary>
        /// 
        /// </summary>
        Passive,

        /// <summary>
        /// 
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
        /// 
        /// </summary>
        Unknown,

        /// <summary>
        /// 
        /// </summary>
        Never,

        /// <summary>
        /// 
        /// </summary>
        Prompt,

        /// <summary>
        /// 
        /// </summary>
        Automatic,

        /// <summary>
        /// 
        /// </summary>
        Always,
    }

    /// <summary>
    /// Result codes (based on Dialog Box Command IDs from WinUser.h).
    /// </summary>
    public enum Result
    {
        /// <summary>
        /// 
        /// </summary>
        Error = -1,

        /// <summary>
        /// 
        /// </summary>
        None,

        /// <summary>
        /// 
        /// </summary>
        Ok,

        /// <summary>
        /// 
        /// </summary>
        Cancel,

        /// <summary>
        /// 
        /// </summary>
        Abort,

        /// <summary>
        /// 
        /// </summary>
        Retry,

        /// <summary>
        /// 
        /// </summary>
        Ignore,

        /// <summary>
        /// 
        /// </summary>
        Yes,

        /// <summary>
        /// 
        /// </summary>
        No,

        /// <summary>
        /// /
        /// </summary>
        Close,

        /// <summary>
        /// 
        /// </summary>
        Help,

        /// <summary>
        /// 
        /// </summary>
        TryAgain,

        /// <summary>
        /// 
        /// </summary>
        Continue,
    }

    /// <summary>
    /// Describes why a bundle or packaged is being resumed.
    /// </summary>
    public enum ResumeType
    {
        /// <summary>
        /// 
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

    /// <summary>
    /// The calculated operation for the related bundle.
    /// </summary>
    public enum RelatedOperation
    {
        /// <summary>
        /// 
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
        /// 
        /// </summary>
        None,

        /// <summary>
        /// 
        /// </summary>
        Detect,

        /// <summary>
        /// 
        /// </summary>
        Upgrade,

        /// <summary>
        /// 
        /// </summary>
        Addon,

        /// <summary>
        /// 
        /// </summary>
        Patch,

        /// <summary>
        /// 
        /// </summary>
        Dependent,

        /// <summary>
        /// 
        /// </summary>
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
    /// The available actions for <see cref="IDefaultBootstrapperApplication.ApplyComplete"/>.
    /// </summary>
    public enum BOOTSTRAPPER_APPLYCOMPLETE_ACTION
    {
        /// <summary>
        /// 
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
        /// 
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
        /// 
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
    /// The available actions for <see cref="IDefaultBootstrapperApplication.CacheVerifyComplete"/>.
    /// </summary>
    public enum BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION
    {
        /// <summary>
        /// 
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
        /// 
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
        /// 
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
