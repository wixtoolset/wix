#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

// structs


// function declarations

HRESULT BACallbackOnApplyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwPhaseCount
    );
HRESULT BACallbackOnApplyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* pAction
    );
HRESULT BACallbackOnApplyDowngrade(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout HRESULT* phrStatus
    );
HRESULT BACallbackOnBeginMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    );
HRESULT BACallbackOnBeginMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnCacheAcquireBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z LPWSTR* pwzSource,
    __in_z LPWSTR* pwzDownloadUrl,
    __in_z_opt LPCWSTR wzPayloadContainerId,
    __out BOOTSTRAPPER_CACHE_OPERATION* pCacheOperation
    );
HRESULT BACallbackOnCacheAcquireComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus,
    __inout BOOL* pfRetry
    );
HRESULT BACallbackOnCacheAcquireProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    );
HRESULT BACallbackOnCacheAcquireResolving(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z LPWSTR* rgSearchPaths,
    __in DWORD cSearchPaths,
    __in BOOL fFoundLocal,
    __in DWORD* pdwChosenSearchPath,
    __in_z_opt LPWSTR* pwzDownloadUrl,
    __in_z_opt LPCWSTR wzPayloadContainerId,
    __inout BOOTSTRAPPER_CACHE_RESOLVE_OPERATION* pCacheOperation
    );
HRESULT BACallbackOnCacheBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT BACallbackOnCacheComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnCacheContainerOrPayloadVerifyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    );
HRESULT BACallbackOnCacheContainerOrPayloadVerifyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnCacheContainerOrPayloadVerifyProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    );
HRESULT BACallbackOnCachePackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cCachePayloads,
    __in DWORD64 dw64PackageCacheSize,
    __in BOOL fVital
    );
HRESULT BACallbackOnCachePackageNonVitalValidationFailure(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION* pAction
    );
HRESULT BACallbackOnCachePackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION* pAction
    );
HRESULT BACallbackOnCachePayloadExtractBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    );
HRESULT BACallbackOnCachePayloadExtractComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnCachePayloadExtractProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    );
HRESULT BACallbackOnCacheVerifyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    );
HRESULT BACallbackOnCacheVerifyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION* pAction
    );
HRESULT BACallbackOnCacheVerifyProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage,
    __in BOOTSTRAPPER_CACHE_VERIFY_STEP verifyStep
    );
HRESULT BACallbackOnCommitMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    );
HRESULT BACallbackOnCommitMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION* pAction
);
HRESULT BACallbackOnCreate(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_COMMAND* pCommand
);
HRESULT BACallbackOnDestroy(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fReload
);
HRESULT BACallbackOnDetectBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fCached,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType,
    __in DWORD cPackages
    );
HRESULT BACallbackOnDetectCompatibleMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in VERUTIL_VERSION* pCompatiblePackageVersion
    );
HRESULT BACallbackOnDetectComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOL fEligibleForCleanup
    );
HRESULT BACallbackOnDetectForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOL fMissingFromCache
    );
HRESULT BACallbackOnDetectMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __in BOOTSTRAPPER_FEATURE_STATE state
    );
HRESULT BACallbackOnDetectPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId
    );
HRESULT BACallbackOnDetectPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOL fCached
    );
HRESULT BACallbackOnDetectRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOL fMissingFromCache
    );
HRESULT BACallbackOnDetectRelatedBundlePackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion
    );
HRESULT BACallbackOnDetectRelatedMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzUpgradeCode,
    __in_z LPCWSTR wzProductCode,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    );
HRESULT BACallbackOnDetectPatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __in BOOTSTRAPPER_PACKAGE_STATE patchState
    );
HRESULT BACallbackOnDetectUpdate(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzUpdateLocation,
    __in DWORD64 dw64Size,
    __in_z_opt LPCWSTR wzHash,
    __in BOOTSTRAPPER_UPDATE_HASH_TYPE hashAlgorithm,
    __in VERUTIL_VERSION* pVersion,
    __in_z_opt LPCWSTR wzTitle,
    __in_z_opt LPCWSTR wzSummary,
    __in_z_opt LPCWSTR wzContentType,
    __in_z_opt LPCWSTR wzContent,
    __inout BOOL* pfStopProcessingUpdates
    );
HRESULT BACallbackOnDetectUpdateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzUpdateLocation,
    __inout BOOL* pfSkip
    );
HRESULT BACallbackOnDetectUpdateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __inout BOOL* pfIgnoreError
    );
HRESULT BACallbackOnElevateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT BACallbackOnElevateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnError(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in DWORD dwCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD dwUIHint,
    __in DWORD cData,
    __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
    __inout int* pnResult
    );
HRESULT BACallbackOnExecuteBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cExecutingPackages
    );
HRESULT BACallbackOnExecuteComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnExecuteFilesInUse(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cFiles,
    __in_ecount_z_opt(cFiles) LPCWSTR* rgwzFiles,
    __in BOOTSTRAPPER_FILES_IN_USE_TYPE source,
    __inout int* pnResult
    );
HRESULT BACallbackOnExecuteMsiMessage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in INSTALLMESSAGE messageType,
    __in DWORD dwUIHint,
    __in_z LPCWSTR wzMessage,
    __in DWORD cData,
    __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
    __inout int* pnResult
    );
HRESULT BACallbackOnExecutePackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOL fExecute,
    __in BOOTSTRAPPER_ACTION_STATE action,
    __in INSTALLUILEVEL uiLevel,
    __in BOOL fDisableExternalUiHandler
    );
HRESULT BACallbackOnExecutePackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION* pAction
    );
HRESULT BACallbackOnExecutePatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzTargetProductCode
    );
HRESULT BACallbackOnExecuteProcessCancel(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD dwProcessId,
    __inout BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION* pAction
    );
HRESULT BACallbackOnExecuteProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD dwProgressPercentage,
    __in DWORD dwOverallPercentage,
    __out int* pnResult
    );
HRESULT BACallbackOnLaunchApprovedExeBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT BACallbackOnLaunchApprovedExeComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in DWORD dwProcessId
    );
HRESULT BACallbackOnPauseAUBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT BACallbackOnPauseAUComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnPlanBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cPackages
    );
HRESULT BACallbackOnPlanCompatibleMsiPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in VERUTIL_VERSION* pCompatiblePackageVersion,
    __inout BOOL* pfRequested
    );
HRESULT BACallbackOnPlanCompatibleMsiPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in HRESULT hrStatus,
    __in BOOL fRequested
    );
HRESULT BACallbackOnPlanComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnPlanForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __inout BOOL* pfIgnoreBundle
    );
HRESULT BACallbackOnPlanMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __inout BOOTSTRAPPER_FEATURE_STATE* pRequestedState
    );
HRESULT BACallbackOnPlanMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOL fExecute,
    __in BOOTSTRAPPER_ACTION_STATE action,
    __inout BURN_MSI_PROPERTY* pActionMsiProperty,
    __inout INSTALLUILEVEL* pUiLevel,
    __inout BOOL* pfDisableExternalUiHandler,
    __inout BOOTSTRAPPER_MSI_FILE_VERSIONING* pFileVersioning
    );
HRESULT BACallbackOnPlannedCompatiblePackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCompatiblePackageId,
    __in BOOL fRemove
    );
HRESULT BACallbackOnPlannedPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOTSTRAPPER_ACTION_STATE execute,
    __in BOOTSTRAPPER_ACTION_STATE rollback,
    __in BOOL fPlannedCache,
    __in BOOL fPlannedUncache
    );
HRESULT BACallbackOnPlanPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOL fCached,
    __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition,
    __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT repairCondition,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState,
    __inout BOOTSTRAPPER_CACHE_TYPE* pRequestedCacheType
    );
HRESULT BACallbackOnPlanPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_REQUEST_STATE requested
    );
HRESULT BACallbackOnPlanRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
HRESULT BACallbackOnPlanRelatedBundleType(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __inout BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE* pRequestedType
    );
HRESULT BACallbackOnPlanRestoreRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
HRESULT BACallbackOnPlanRollbackBoundary(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzRollbackBoundaryId,
    __inout BOOL *pfTransaction
    );
HRESULT BACallbackOnPlanPatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
HRESULT BACallbackOnProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwProgressPercentage,
    __in DWORD dwOverallPercentage
    );
HRESULT BACallbackOnRegisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_REGISTRATION_TYPE* pRegistrationType
    );
HRESULT BACallbackOnRegisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnRollbackMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    );
HRESULT BACallbackOnRollbackMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION* pAction
);
HRESULT BACallbackOnShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_SHUTDOWN_ACTION* pAction
    );
HRESULT BACallbackOnStartup(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT BACallbackOnSystemRestorePointBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT BACallbackOnSystemRestorePointComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
HRESULT BACallbackOnUnregisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_REGISTRATION_TYPE* pRegistrationType
    );
HRESULT BACallbackOnUnregisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );

#if defined(__cplusplus)
}
#endif
