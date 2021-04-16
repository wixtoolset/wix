#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#define BAAPI HRESULT __stdcall

#if defined(__cplusplus)
extern "C" {
#endif


// constants

const DWORD MB_RETRYTRYAGAIN = 0xF;


// structs

typedef struct _BOOTSTRAPPER_ENGINE_CONTEXT BOOTSTRAPPER_ENGINE_CONTEXT;

typedef struct _BURN_USER_EXPERIENCE
{
    BOOL fSplashScreen;
    BURN_PAYLOADS payloads;

    HMODULE hUXModule;
    PFN_BOOTSTRAPPER_APPLICATION_PROC pfnBAProc;
    LPVOID pvBAProcContext;
    BOOL fDisableUnloading;
    LPWSTR sczTempDirectory;

    CRITICAL_SECTION csEngineActive;    // Changing the engine active state in the user experience must be
                                        // syncronized through this critical section.
                                        // Note: The engine must never do a UX callback while in this critical section.

    BOOL fEngineActive;                 // Indicates that the engine is currently active with one of the execution
                                        // steps (detect, plan, apply), and cannot accept requests from the UX.
                                        // This flag should be cleared by the engine prior to UX callbacks that
                                        // allows altering of the engine state.

    HRESULT hrApplyError;               // Tracks is an error occurs during apply that requires the cache or
                                        // execute threads to bail.

    HWND hwndApply;                     // The window handle provided at the beginning of Apply(). Only valid
                                        // during apply.

    HWND hwndDetect;                    // The window handle provided at the beginning of Detect(). Only valid
                                        // during Detect.

    DWORD dwExitCode;                   // Exit code returned by the user experience for the engine overall.
} BURN_USER_EXPERIENCE;

// functions

HRESULT UserExperienceParseFromXml(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IXMLDOMNode* pixnBundle
    );
void UserExperienceUninitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT UserExperienceLoad(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in BOOTSTRAPPER_COMMAND* pCommand
    );
HRESULT UserExperienceUnload(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT UserExperienceEnsureWorkingFolder(
    __in LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczUserExperienceWorkingFolder
    );
HRESULT UserExperienceRemove(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
int UserExperienceSendError(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in HRESULT hrCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD uiFlags,
    __in int nRecommendation
    );
void UserExperienceActivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
void UserExperienceDeactivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
/********************************************************************
 UserExperienceEnsureEngineInactive - Verifies the engine is inactive.
   The caller MUST enter the csActive critical section before calling.

*********************************************************************/
HRESULT UserExperienceEnsureEngineInactive(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
void UserExperienceExecuteReset(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
void UserExperienceExecutePhaseComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrResult
    );
BAAPI UserExperienceOnApplyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwPhaseCount
    );
BAAPI UserExperienceOnApplyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* pAction
    );
BAAPI UserExperienceOnBeginMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    );
BAAPI UserExperienceOnBeginMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnCacheAcquireBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z LPWSTR* pwzSource,
    __in_z LPWSTR* pwzDownloadUrl,
    __in_z_opt LPCWSTR wzPayloadContainerId,
    __out BOOTSTRAPPER_CACHE_OPERATION* pCacheOperation
    );
BAAPI UserExperienceOnCacheAcquireComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus,
    __inout BOOL* pfRetry
    );
BAAPI UserExperienceOnCacheAcquireProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    );
BAAPI UserExperienceOnCacheAcquireResolving(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z LPWSTR* rgSearchPaths,
    __in DWORD cSearchPaths,
    __in BOOL fFoundLocal,
    __in DWORD* pdwChosenSearchPath,
    __in_z_opt LPCWSTR wzDownloadUrl,
    __in_z_opt LPCWSTR wzPayloadContainerId,
    __inout BOOTSTRAPPER_CACHE_OPERATION* pCacheOperation
    );
BAAPI UserExperienceOnCacheBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnCacheComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnCachePackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cCachePayloads,
    __in DWORD64 dw64PackageCacheSize
    );
BAAPI UserExperienceOnCachePackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION* pAction
    );
BAAPI UserExperienceOnCacheVerifyBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId
    );
BAAPI UserExperienceOnCacheVerifyComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrStatus,
    __inout BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION* pAction
    );
BAAPI UserExperienceOnCacheVerifyProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in DWORD dwOverallPercentage
    );
BAAPI UserExperienceOnCommitMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    );
BAAPI UserExperienceOnCommitMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnDetectBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fCached,
    __in BOOL fInstalled,
    __in DWORD cPackages
    );
BAAPI UserExperienceOnDetectComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in BOOL fEligibleForCleanup
    );
BAAPI UserExperienceOnDetectForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOL fMissingFromCache
    );
BAAPI UserExperienceOnDetectMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __in BOOTSTRAPPER_FEATURE_STATE state
    );
BAAPI UserExperienceOnDetectPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId
    );
BAAPI UserExperienceOnDetectPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_PACKAGE_STATE state
    );
BAAPI UserExperienceOnDetectRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOTSTRAPPER_RELATED_OPERATION operation,
    __in BOOL fMissingFromCache
    );
BAAPI UserExperienceOnDetectRelatedMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzUpgradeCode,
    __in_z LPCWSTR wzProductCode,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    );
BAAPI UserExperienceOnDetectPatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __in BOOTSTRAPPER_PACKAGE_STATE patchState
    );
BAAPI UserExperienceOnDetectUpdate(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z_opt LPCWSTR wzUpdateLocation,
    __in DWORD64 dw64Size,
    __in VERUTIL_VERSION* pVersion,
    __in_z_opt LPCWSTR wzTitle,
    __in_z_opt LPCWSTR wzSummary,
    __in_z_opt LPCWSTR wzContentType,
    __in_z_opt LPCWSTR wzContent,
    __inout BOOL* pfStopProcessingUpdates
    );
BAAPI UserExperienceOnDetectUpdateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzUpdateLocation,
    __inout BOOL* pfSkip
    );
BAAPI UserExperienceOnDetectUpdateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __inout BOOL* pfIgnoreError
    );
BAAPI UserExperienceOnElevateBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnElevateComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnError(
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
BAAPI UserExperienceOnExecuteBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cExecutingPackages
    );
BAAPI UserExperienceOnExecuteComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnExecuteFilesInUse(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD cFiles,
    __in_ecount_z_opt(cFiles) LPCWSTR* rgwzFiles,
    __inout int* pnResult
    );
BAAPI UserExperienceOnExecuteMsiMessage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in INSTALLMESSAGE messageType,
    __in DWORD dwUIHint,
    __in_z LPCWSTR wzMessage,
    __in DWORD cData,
    __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
    __inout int* pnResult
    );
BAAPI UserExperienceOnExecutePackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOL fExecute,
    __in BOOTSTRAPPER_ACTION_STATE action,
    __in INSTALLUILEVEL uiLevel,
    __in BOOL fDisableExternalUiHandler
    );
BAAPI UserExperienceOnExecutePackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __inout BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION* pAction
    );
BAAPI UserExperienceOnExecutePatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzTargetProductCode
    );
BAAPI UserExperienceOnExecuteProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in DWORD dwProgressPercentage,
    __in DWORD dwOverallPercentage,
    __out int* pnResult
    );
BAAPI UserExperienceOnLaunchApprovedExeBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnLaunchApprovedExeComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus,
    __in DWORD dwProcessId
    );
BAAPI UserExperienceOnPauseAUBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnPauseAUComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnPlanBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD cPackages
    );
BAAPI UserExperienceOnPlanComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnPlanForwardCompatibleBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in_z LPCWSTR wzBundleTag,
    __in BOOL fPerMachine,
    __in VERUTIL_VERSION* pVersion,
    __inout BOOL* pfIgnoreBundle
    );
BAAPI UserExperienceOnPlanMsiFeature(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzFeatureId,
    __inout BOOTSTRAPPER_FEATURE_STATE* pRequestedState
    );
BAAPI UserExperienceOnPlanMsiPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOL fExecute,
    __in BOOTSTRAPPER_ACTION_STATE action,
    __inout BURN_MSI_PROPERTY* pActionMsiProperty,
    __inout INSTALLUILEVEL* pUiLevel,
    __inout BOOL* pfDisableExternalUiHandler
    );
BAAPI UserExperienceOnPlannedPackage(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOTSTRAPPER_ACTION_STATE execute,
    __in BOOTSTRAPPER_ACTION_STATE rollback
    );
BAAPI UserExperienceOnPlanPackageBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOL fInstallCondition,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
BAAPI UserExperienceOnPlanPackageComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrStatus,
    __in BOOTSTRAPPER_REQUEST_STATE requested
    );
BAAPI UserExperienceOnPlanRelatedBundle(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzBundleId,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
BAAPI UserExperienceOnPlanPatchTarget(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzProductCode,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestedState
    );
BAAPI UserExperienceOnProgress(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwProgressPercentage,
    __in DWORD dwOverallPercentage
    );
BAAPI UserExperienceOnRegisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnRegisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnRollbackMsiTransactionBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId
    );
BAAPI UserExperienceOnRollbackMsiTransactionComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzTransactionId,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOTSTRAPPER_SHUTDOWN_ACTION* pAction
    );
BAAPI UserExperienceOnStartup(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnSystemRestorePointBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
BAAPI UserExperienceOnSystemRestorePointComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
BAAPI UserExperienceOnSystemShutdown(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in DWORD dwEndSession,
    __inout BOOL* pfCancel
    );
BAAPI UserExperienceOnUnregisterBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOL* pfKeepRegistration
    );
BAAPI UserExperienceOnUnregisterComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrStatus
    );
int UserExperienceCheckExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    );
HRESULT UserExperienceInterpretExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    );
#if defined(__cplusplus)
}
#endif
