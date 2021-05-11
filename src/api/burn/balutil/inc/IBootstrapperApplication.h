#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


DECLARE_INTERFACE_IID_(IBootstrapperApplication, IUnknown, "53C31D56-49C0-426B-AB06-099D717C67FE")
{
    // BAProc - The PFN_BOOTSTRAPPER_APPLICATION_PROC can call this method to give the BA raw access to the callback from the engine.
    //          This might be used to help the BA support more than one version of the engine.
    STDMETHOD(BAProc)(
        __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __in_opt LPVOID pvContext
        ) = 0;

    // BAProcFallback - The PFN_BOOTSTRAPPER_APPLICATION_PROC can call this method
    //                  to give the BA the ability to use default behavior
    //                  and then forward the message to extensions.
    STDMETHOD_(void, BAProcFallback)(
        __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __inout HRESULT* phr,
        __in_opt LPVOID pvContext
        ) = 0;

    // OnStartup - called when the engine is ready for the bootstrapper application to start.
    //
    STDMETHOD(OnStartup)() = 0;

    // OnShutdown - called after the bootstrapper application quits the engine.
    STDMETHOD(OnShutdown)(
        __inout BOOTSTRAPPER_SHUTDOWN_ACTION* pAction
        ) = 0;

    // OnSystemShutdown - called when the operating system is instructed to shutdown the machine.
    STDMETHOD(OnSystemShutdown)(
        __in DWORD dwEndSession,
        __inout BOOL* pfCancel
        ) = 0;

    // OnDetectBegin - called when the engine begins detection.
    STDMETHOD(OnDetectBegin)(
        __in BOOL fCached,
        __in BOOL fInstalled,
        __in DWORD cPackages,
        __inout BOOL* pfCancel
        ) = 0;

    // OnDetectForwardCompatibleBundle - called when the engine detects a forward compatible bundle.
    STDMETHOD(OnDetectForwardCompatibleBundle)(
        __in_z LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_RELATION_TYPE relationType,
        __in_z LPCWSTR wzBundleTag,
        __in BOOL fPerMachine,
        __in_z LPCWSTR wzVersion,
        __in BOOL fMissingFromCache,
        __inout BOOL* pfCancel
        ) = 0;

    // OnDetectUpdateBegin - called when the engine begins detection for bundle update.
    STDMETHOD(OnDetectUpdateBegin)(
        __in_z LPCWSTR wzUpdateLocation,
        __inout BOOL* pfCancel,
        __inout BOOL* pfSkip
        ) = 0;

    // OnDetectUpdate - called when the engine has an update candidate for bundle update.
    STDMETHOD(OnDetectUpdate)(
        __in_z_opt LPCWSTR wzUpdateLocation,
        __in DWORD64 dw64Size,
        __in_z LPCWSTR wzVersion,
        __in_z_opt LPCWSTR wzTitle,
        __in_z_opt LPCWSTR wzSummary,
        __in_z_opt LPCWSTR wzContentType,
        __in_z_opt LPCWSTR wzContent,
        __inout BOOL* pfCancel,
        __inout BOOL* pfStopProcessingUpdates
        ) = 0;

    // OnDetectUpdateComplete - called when the engine completes detection for bundle update.
    STDMETHOD(OnDetectUpdateComplete)(
        __in HRESULT hrStatus,
        __inout BOOL* pfIgnoreError
        ) = 0;

    // OnDetectRelatedBundle - called when the engine detects a related bundle.
    STDMETHOD(OnDetectRelatedBundle)(
        __in_z LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_RELATION_TYPE relationType,
        __in_z LPCWSTR wzBundleTag,
        __in BOOL fPerMachine,
        __in_z LPCWSTR wzVersion,
        __in BOOTSTRAPPER_RELATED_OPERATION operation,
        __in BOOL fMissingFromCache,
        __inout BOOL* pfCancel
        ) = 0;

    // OnDetectPackageBegin - called when the engine begins detecting a package.
    STDMETHOD(OnDetectPackageBegin)(
        __in_z LPCWSTR wzPackageId,
        __inout BOOL* pfCancel
        ) = 0;

    // OnDetectRelatedMsiPackage - called when the engine begins detects a related package.
    STDMETHOD(OnDetectRelatedMsiPackage)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzUpgradeCode,
        __in_z LPCWSTR wzProductCode,
        __in BOOL fPerMachine,
        __in_z LPCWSTR wzVersion,
        __in BOOTSTRAPPER_RELATED_OPERATION operation,
        __inout BOOL* pfCancel
        ) = 0;

    // OnDetectPatchTarget - called when the engine detects a target product
    //                       for an MSP package.
    STDMETHOD(OnDetectPatchTarget)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzProductCode,
        __in BOOTSTRAPPER_PACKAGE_STATE patchState,
        __inout BOOL* pfCancel
        ) = 0;

    // OnDetectMsiFeature - called when the engine detects a feature in an MSI package.
    STDMETHOD(OnDetectMsiFeature)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzFeatureId,
        __in BOOTSTRAPPER_FEATURE_STATE state,
        __inout BOOL* pfCancel
        ) = 0;

    // OnDetectPackageComplete - called after the engine detects a package.
    //
    STDMETHOD(OnDetectPackageComplete)(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_PACKAGE_STATE state,
        __in BOOL fCached
        ) = 0;

    // OnDetectPackageComplete - called after the engine completes detection.
    //
    STDMETHOD(OnDetectComplete)(
        __in HRESULT hrStatus,
        __in BOOL fEligibleForCleanup
        ) = 0;

    // OnPlanBegin - called when the engine begins planning.
    STDMETHOD(OnPlanBegin)(
        __in DWORD cPackages,
        __inout BOOL* pfCancel
        ) = 0;

    // OnPlanRelatedBundle - called when the engine begins planning a related bundle.
    STDMETHOD(OnPlanRelatedBundle)(
        __in_z LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_REQUEST_STATE recommendedState,
        __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState,
        __inout BOOL* pfCancel
        ) = 0;

    // OnPlanPackageBegin - called when the engine has begun getting the BA's input
    //                      for planning a package.
    STDMETHOD(OnPlanPackageBegin)(
        __in_z LPCWSTR wzPackageId,
        __in BOOTSTRAPPER_PACKAGE_STATE state,
        __in BOOL fCached,
        __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition,
        __in BOOTSTRAPPER_REQUEST_STATE recommendedState,
        __in BOOTSTRAPPER_CACHE_TYPE recommendedCacheType,
        __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState,
        __inout BOOTSTRAPPER_CACHE_TYPE* pRequestedCacheType,
        __inout BOOL* pfCancel
        ) = 0;

    // OnPlanPatchTarget - called when the engine is about to plan a target
    //                     of an MSP package.
    STDMETHOD(OnPlanPatchTarget)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzProductCode,
        __in BOOTSTRAPPER_REQUEST_STATE recommendedState,
        __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState,
        __inout BOOL* pfCancel
        ) = 0;

    // OnPlanMsiFeature - called when the engine plans a feature in an
    //                    MSI package.
    STDMETHOD(OnPlanMsiFeature)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzFeatureId,
        __in BOOTSTRAPPER_FEATURE_STATE recommendedState,
        __inout BOOTSTRAPPER_FEATURE_STATE* pRequestedState,
        __inout BOOL* pfCancel
        ) = 0;

    // OnPlanMsiPackage - called when the engine plans an MSI or MSP package.
    //
    STDMETHOD(OnPlanMsiPackage)(
        __in_z LPCWSTR wzPackageId,
        __in BOOL fExecute, // false means rollback.
        __in BOOTSTRAPPER_ACTION_STATE action,
        __inout BOOL* pfCancel,
        __inout BURN_MSI_PROPERTY* pActionMsiProperty,
        __inout INSTALLUILEVEL* pUiLevel,
        __inout BOOL* pfDisableExternalUiHandler
        ) = 0;

    // OnPlanPackageComplete - called after the engine has completed getting the BA's input
    //                         for planning a package.
    STDMETHOD(OnPlanPackageComplete)(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_REQUEST_STATE requested
        ) = 0;

    // OnPlannedPackage - called after the engine has completed planning a package.
    STDMETHOD(OnPlannedPackage)(
        __in_z LPCWSTR wzPackageId,
        __in BOOTSTRAPPER_ACTION_STATE execute,
        __in BOOTSTRAPPER_ACTION_STATE rollback,
        __in BOOL fPlannedCache,
        __in BOOL fPlannedUncache
        ) = 0;

    // OnPlanComplete - called when the engine completes planning.
    //
    STDMETHOD(OnPlanComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnApplyBegin - called when the engine begins applying the plan.
    //
    STDMETHOD(OnApplyBegin)(
        __in DWORD dwPhaseCount,
        __inout BOOL* pfCancel
        ) = 0;

    // OnElevateBegin - called before the engine displays an elevation prompt.
    //                  Will only happen once per execution of the engine,
    //                  assuming the elevation was successful.
    STDMETHOD(OnElevateBegin)(
        __inout BOOL* pfCancel
        ) = 0;

    // OnElevateComplete - called after the engine attempted to elevate.
    //
    STDMETHOD(OnElevateComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnProgress - called when the engine makes progress.
    //
    STDMETHOD(OnProgress)(
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallPercentage,
        __inout BOOL* pfCancel
        ) = 0;

    // OnError - called when the engine encounters an error.
    //
    // nResult:
    //  uiFlags is a combination of valid ID* return values appropriate for
    //          the error.
    //
    //  IDNOACTION instructs the engine to pass the error through to default
    //             handling which usually results in the apply failing.
    STDMETHOD(OnError)(
        __in BOOTSTRAPPER_ERROR_TYPE errorType,
        __in_z_opt LPCWSTR wzPackageId,
        __in DWORD dwCode,
        __in_z_opt LPCWSTR wzError,
        __in DWORD dwUIHint,
        __in DWORD cData,
        __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
        __in int nRecommendation,
        __inout int* pResult
        ) = 0;

    // OnRegisterBegin - called when the engine registers the bundle.
    //
    STDMETHOD(OnRegisterBegin)(
        __inout BOOL* pfCancel
        ) = 0;

    // OnRegisterComplete - called when the engine registration is
    //                      complete.
    //
    STDMETHOD(OnRegisterComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnCacheBegin - called when the engine begins caching.
    //
    STDMETHOD(OnCacheBegin)(
        __inout BOOL* pfCancel
        ) = 0;

    // OnCachePackageBegin - called when the engine begins caching
    //                       a package.
    //
    STDMETHOD(OnCachePackageBegin)(
        __in_z LPCWSTR wzPackageId,
        __in DWORD cCachePayloads,
        __in DWORD64 dw64PackageCacheSize,
        __inout BOOL* pfCancel
        )  = 0;

    // OnCacheAcquireBegin - called when the engine begins acquiring a payload or container.
    //
    // Notes:
    //  It is expected the BA may call IBootstrapperEngine::SetLocalSource() or IBootstrapperEngine::SetDownloadSource()
    //  to update the source location before returning.
    //
    STDMETHOD(OnCacheAcquireBegin)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzSource,
        __in_z_opt LPCWSTR wzDownloadUrl,
        __in_z_opt LPCWSTR wzPayloadContainerId,
        __in BOOTSTRAPPER_CACHE_OPERATION recommendation,
        __inout BOOTSTRAPPER_CACHE_OPERATION* pAction,
        __inout BOOL* pfCancel
        ) = 0;

    // OnCacheAcquireProgress - called when the engine makes progress acquiring the payload or container.
    //
    STDMETHOD(OnCacheAcquireProgress)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage,
        __inout BOOL* pfCancel
        ) = 0;

    // OnCacheAcquireResolving - called to allow the BA to override the acquisition action for the payload or container.
    //
    // Parameters:
    //  wzPackageOrContainerId will be NULL when resolving a layout-only payload.
    //  wzPayloadId will be NULL when resolving a container.
    //  wzDownloadUrl will be NULL if the container or payload does not provide a DownloadURL.
    //  wzPayloadContainerId will not be NULL if acquiring a payload that is in a container.
    //
    //  rgSearchPaths are the search paths used for source resolution.
    //  fFoundLocal is TRUE when dwRecommendedSearchPath indicates that the file was found.
    //  dwRecommendedSearchPath is the index into rgSearchPaths for the recommended local file.
    //
    STDMETHOD(OnCacheAcquireResolving)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR* rgSearchPaths,
        __in DWORD cSearchPaths,
        __in BOOL fFoundLocal,
        __in DWORD dwRecommendedSearchPath,
        __in_z_opt LPCWSTR wzDownloadUrl,
        __in_z_opt LPCWSTR wzPayloadContainerId,
        __in BOOTSTRAPPER_CACHE_RESOLVE_OPERATION recommendation,
        __inout DWORD* pdwChosenSearchPath,
        __inout BOOTSTRAPPER_CACHE_RESOLVE_OPERATION* pAction,
        __inout BOOL* pfCancel
        ) = 0;

    // OnCacheAcquireComplete - called after the engine acquired the payload or container.
    //
    // Notes:
    //  It is expected the BA may call IBootstrapperEngine::SetLocalSource() or IBootstrapperEngine::SetDownloadSource()
    //  to update the source location before returning BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION_RETRY.
    //
    STDMETHOD(OnCacheAcquireComplete)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION* pAction
        ) = 0;

    // OnCacheVerifyBegin - called when the engine begins to verify then copy
    //                      a payload or container to the package cache folder.
    //
    STDMETHOD(OnCacheVerifyBegin)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __inout BOOL* pfCancel
        ) = 0;

    STDMETHOD(OnCacheVerifyProgress)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage,
        __in BOOTSTRAPPER_CACHE_VERIFY_STEP verifyStep,
        __inout BOOL* pfCancel
        ) = 0;

    // OnCacheVerifyComplete - called after the engine verifies and copies
    //                         a payload or container to the package cache folder.
    //
    STDMETHOD(OnCacheVerifyComplete)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION* pAction
        ) = 0;

    // OnCachePackageComplete - called after the engine attempts to copy or download all
    //                          payloads of a package into the package cache folder.
    //
    STDMETHOD(OnCachePackageComplete)(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION* pAction
        )  = 0;

    // OnCacheComplete - called when the engine caching is complete.
    //
    STDMETHOD(OnCacheComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnExecuteBegin - called when the engine begins executing the plan.
    //
    STDMETHOD(OnExecuteBegin)(
        __in DWORD cExecutingPackages,
        __inout BOOL* pfCancel
        ) = 0;

    // OnExecutePackageBegin - called when the engine begins executing a package.
    //
    STDMETHOD(OnExecutePackageBegin)(
        __in_z LPCWSTR wzPackageId,
        __in BOOL fExecute, // false means rollback.
        __in BOOTSTRAPPER_ACTION_STATE action,
        __in INSTALLUILEVEL uiLevel,
        __in BOOL fDisableExternalUiHandler,
        __inout BOOL* pfCancel
        ) = 0;

    // OnExecutePatchTarget - called for each patch in an MspPackage targeting the product
    //                        when the engine begins executing the MspPackage.
    //
    STDMETHOD(OnExecutePatchTarget)(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzTargetProductCode,
        __inout BOOL* pfCancel
        ) = 0;

    // OnExecuteProgress - called when the engine makes progress executing a package.
    //
    STDMETHOD(OnExecuteProgress)(
        __in_z LPCWSTR wzPackageId,
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallPercentage,
        __inout BOOL* pfCancel
        ) = 0;

    // OnExecuteMsiMessage - called when the engine receives an MSI package message.
    //
    // Return:
    //  uiFlags is a combination of valid ID* return values appropriate for
    //          the message.
    //
    //  IDNOACTION instructs the engine to pass the message through to default
    //             handling which usually results in the execution continuing.
    STDMETHOD(OnExecuteMsiMessage)(
        __in_z LPCWSTR wzPackageId,
        __in INSTALLMESSAGE messageType,
        __in DWORD dwUIHint,
        __in_z LPCWSTR wzMessage,
        __in DWORD cData,
        __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
        __in int nRecommendation,
        __inout int* pResult
        ) = 0;

    // OnExecuteFilesInUse - called when the engine encounters files in use while
    //                       executing a package.
    //
    // Return:
    //  IDOK instructs the engine to let the Restart Manager attempt to close the
    //       applications to avoid a restart.
    //
    //  IDCANCEL instructs the engine to abort the execution and start rollback.
    //
    //  IDIGNORE instructs the engine to ignore the running applications. A restart will be
    //           required.
    //
    //  IDRETRY instructs the engine to check if the applications are still running again.
    //
    //  IDNOACTION is equivalent to ignoring the running applications. A restart will be
    //             required.
    STDMETHOD(OnExecuteFilesInUse)(
        __in_z LPCWSTR wzPackageId,
        __in DWORD cFiles,
        __in_ecount_z(cFiles) LPCWSTR* rgwzFiles,
        __in int nRecommendation,
        __inout int* pResult
        ) = 0;

    // OnExecutePackageComplete - called when a package execution is complete.
    //
    STDMETHOD(OnExecutePackageComplete)(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_APPLY_RESTART restart,
        __in BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION* pAction
        ) = 0;

    // OnExecuteComplete - called when the engine execution is complete.
    //
    STDMETHOD(OnExecuteComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnUnregisterBegin - called when the engine unregisters the bundle.
    //
    STDMETHOD(OnUnregisterBegin)(
        __in BOOL fKeepRegistration,
        __inout BOOL* pfForceKeepRegistration
        ) = 0;

    // OnUnregisterComplete - called when the engine unregistration is complete.
    //
    STDMETHOD(OnUnregisterComplete)(
        __in HRESULT hrStatus
        ) = 0;

    // OnApplyComplete - called after the plan has been applied.
    //
    STDMETHOD(OnApplyComplete)(
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_APPLY_RESTART restart,
        __in BOOTSTRAPPER_APPLYCOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* pAction
        ) = 0;

    // OnLaunchApprovedExeBegin - called before trying to launch the preapproved executable.
    // 
    STDMETHOD(OnLaunchApprovedExeBegin)(
        __inout BOOL* pfCancel
        ) = 0;

    // OnLaunchApprovedExeComplete - called after trying to launch the preapproved executable.
    //
    STDMETHOD(OnLaunchApprovedExeComplete)(
        __in HRESULT hrStatus,
        __in DWORD dwProcessId
        ) = 0;

    STDMETHOD(OnBeginMsiTransactionBegin)(
        __in_z LPCWSTR wzTransactionId,
        __inout BOOL* pfCancel
        ) = 0;

    STDMETHOD(OnBeginMsiTransactionComplete)(
        __in_z LPCWSTR wzTransactionId,
        __in HRESULT hrStatus
        ) = 0;

    STDMETHOD(OnCommitMsiTransactionBegin)(
        __in_z LPCWSTR wzTransactionId,
        __inout BOOL* pfCancel
        ) = 0;

    STDMETHOD(OnCommitMsiTransactionComplete)(
        __in_z LPCWSTR wzTransactionId,
        __in HRESULT hrStatus
        ) = 0;

    STDMETHOD(OnRollbackMsiTransactionBegin)(
        __in_z LPCWSTR wzTransactionId
        ) = 0;

    STDMETHOD(OnRollbackMsiTransactionComplete)(
        __in_z LPCWSTR wzTransactionId,
        __in HRESULT hrStatus
        ) = 0;

    STDMETHOD(OnPauseAutomaticUpdatesBegin)(
        ) = 0;

    STDMETHOD(OnPauseAutomaticUpdatesComplete)(
        __in HRESULT hrStatus
        ) = 0;

    STDMETHOD(OnSystemRestorePointBegin)(
        ) = 0;

    STDMETHOD(OnSystemRestorePointComplete)(
        __in HRESULT hrStatus
        ) = 0;

    STDMETHOD(OnPlanForwardCompatibleBundle)(
        __in_z LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_RELATION_TYPE relationType,
        __in_z LPCWSTR wzBundleTag,
        __in BOOL fPerMachine,
        __in_z LPCWSTR wzVersion,
        __in BOOL fRecommendedIgnoreBundle,
        __inout BOOL* pfCancel,
        __inout BOOL* pfIgnoreBundle
        ) = 0;

    STDMETHOD(OnCacheContainerOrPayloadVerifyBegin)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __inout BOOL* pfCancel
        ) = 0;

    STDMETHOD(OnCacheContainerOrPayloadVerifyProgress)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage,
        __inout BOOL* pfCancel
        ) = 0;

    STDMETHOD(OnCacheContainerOrPayloadVerifyComplete)(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus
        ) = 0;

    STDMETHOD(OnCachePayloadExtractBegin)(
        __in_z_opt LPCWSTR wzContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __inout BOOL* pfCancel
        ) = 0;

    STDMETHOD(OnCachePayloadExtractProgress)(
        __in_z_opt LPCWSTR wzContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage,
        __inout BOOL* pfCancel
        ) = 0;

    STDMETHOD(OnCachePayloadExtractComplete)(
        __in_z_opt LPCWSTR wzContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus
        ) = 0;
};
