// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


#ifdef DEBUG
    #define IgnoreRollbackError(x, f, ...) if (FAILED(x)) { TraceError(x, f, __VA_ARGS__); }
#else
    #define IgnoreRollbackError(x, f, ...)
#endif

const DWORD BURN_CACHE_MAX_RECOMMENDED_VERIFY_TRYAGAIN_ATTEMPTS = 2;

enum BURN_CACHE_PROGRESS_TYPE
{
    BURN_CACHE_PROGRESS_TYPE_ACQUIRE,
    BURN_CACHE_PROGRESS_TYPE_CONTAINER_OR_PAYLOAD_VERIFY,
    BURN_CACHE_PROGRESS_TYPE_EXTRACT,
    BURN_CACHE_PROGRESS_TYPE_FINALIZE,
    BURN_CACHE_PROGRESS_TYPE_HASH,
    BURN_CACHE_PROGRESS_TYPE_PAYLOAD_VERIFY,
    BURN_CACHE_PROGRESS_TYPE_STAGE,
};

// structs

typedef struct _BURN_CACHE_CONTEXT
{
    BURN_USER_EXPERIENCE* pUX;
    BURN_VARIABLES* pVariables;
    BURN_PAYLOADS* pPayloads;
    HANDLE hPipe;
    HANDLE hSourceEngineFile;
    DWORD64 qwTotalCacheSize;
    DWORD64 qwSuccessfulCacheProgress;
    LPCWSTR wzLayoutDirectory;
    LPWSTR* rgSearchPaths;
    DWORD cSearchPaths;
    DWORD cSearchPathsMax;
    LPWSTR sczLastUsedFolderCandidate;
} BURN_CACHE_CONTEXT;

typedef struct _BURN_CACHE_PROGRESS_CONTEXT
{
    BURN_CACHE_CONTEXT* pCacheContext;
    BURN_CACHE_PROGRESS_TYPE type;
    BURN_CONTAINER* pContainer;
    BURN_PACKAGE* pPackage;
    BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem;
    BURN_PAYLOAD* pPayload;

    BOOL fCancel;
    HRESULT hrError;
} BURN_CACHE_PROGRESS_CONTEXT;

typedef struct _BURN_EXECUTE_CONTEXT
{
    BURN_USER_EXPERIENCE* pUX;
    BOOL fRollback;
    BURN_PACKAGE* pExecutingPackage;
    DWORD cExecutedPackages;
    DWORD cExecutePackagesTotal;
    DWORD* pcOverallProgressTicks;
} BURN_EXECUTE_CONTEXT;


// internal function declarations
static HRESULT WINAPI AuthenticationRequired(
    __in LPVOID pData,
    __in HINTERNET hUrl,
    __in long lHttpCode,
    __out BOOL* pfRetrySend,
    __out BOOL* pfRetry
    );

static void CalculateKeepRegistration(
    __in BURN_ENGINE_STATE* pEngineState,
    __inout BOOL* pfKeepRegistration
    );
static HRESULT ExecuteDependentRegistrationActions(
    __in HANDLE hPipe,
    __in const BURN_REGISTRATION* pRegistration,
    __in_ecount(cActions) const BURN_DEPENDENT_REGISTRATION_ACTION* rgActions,
    __in DWORD cActions
    );
static HRESULT ApplyCachePackage(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_PACKAGE* pPackage
    );
static HRESULT ApplyExtractContainer(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_CONTAINER* pContainer
    );
static HRESULT ApplyLayoutBundle(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_PAYLOAD_GROUP* pPayloads,
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzUnverifiedPath,
    __in DWORD64 qwBundleSize
    );
static HRESULT ApplyLayoutContainer(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_CONTAINER* pContainer
    );
static HRESULT ApplyProcessPayload(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_opt BURN_PACKAGE* pPackage,
    __in BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem
    );
static HRESULT ApplyCacheVerifyContainerOrPayload(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_opt BURN_CONTAINER* pContainer,
    __in_opt BURN_PACKAGE* pPackage,
    __in_opt BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem
    );
static HRESULT ExtractContainer(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_CONTAINER* pContainer
    );
static HRESULT LayoutBundle(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzUnverifiedPath,
    __in DWORD64 qwBundleSize
    );
static HRESULT ApplyAcquireContainerOrPayload(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_opt BURN_CONTAINER* pContainer,
    __in_opt BURN_PACKAGE* pPackage,
    __in_opt BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem
    );
static HRESULT AcquireContainerOrPayload(
    __in BURN_CACHE_PROGRESS_CONTEXT* pProgress,
    __out BOOL* pfRetry
    );
static HRESULT LayoutOrCacheContainerOrPayload(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_opt BURN_CONTAINER* pContainer,
    __in_opt BURN_PACKAGE* pPackage,
    __in_opt BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem,
    __in DWORD cTryAgainAttempts,
    __out BOOL* pfRetry
    );
static HRESULT PreparePayloadDestinationPath(
    __in_z LPCWSTR wzDestinationPath
    );
static HRESULT CopyPayload(
    __in BURN_CACHE_PROGRESS_CONTEXT* pProgress,
    __in HANDLE hSourceFile,
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzDestinationPath
    );
static HRESULT DownloadPayload(
    __in BURN_CACHE_PROGRESS_CONTEXT* pProgress,
    __in_z LPCWSTR wzDestinationPath
    );
static HRESULT CALLBACK CacheMessageHandler(
    __in BURN_CACHE_MESSAGE* pMessage,
    __in LPVOID pvContext
    );
static HRESULT CompleteCacheProgress(
    __in BURN_CACHE_PROGRESS_CONTEXT* pContext,
    __in DWORD64 qwFileSize
    );
static DWORD CALLBACK CacheProgressRoutine(
    __in LARGE_INTEGER TotalFileSize,
    __in LARGE_INTEGER TotalBytesTransferred,
    __in LARGE_INTEGER StreamSize,
    __in LARGE_INTEGER StreamBytesTransferred,
    __in DWORD dwStreamNumber,
    __in DWORD dwCallbackReason,
    __in HANDLE hSourceFile,
    __in HANDLE hDestinationFile,
    __in_opt LPVOID lpData
    );
static void DoRollbackCache(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PLAN* pPlan,
    __in HANDLE hPipe,
    __in DWORD dwCheckpoint
    );
static HRESULT DoExecuteAction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in_opt HANDLE hCacheThread,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary,
    __inout BURN_EXECUTE_ACTION_CHECKPOINT** ppCheckpoint,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT DoRollbackActions(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in DWORD dwCheckpoint,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT ExecuteExePackage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in BOOL fRollback,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT ExecuteMsiPackage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in BOOL fInsideMsiTransaction,
    __in BOOL fRollback,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT ExecuteMspPackage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in BOOL fInsideMsiTransaction,
    __in BOOL fRollback,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT ExecuteMsuPackage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in BOOL fRollback,
    __in BOOL fStopWusaService,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static HRESULT ExecutePackageProviderAction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pAction,
    __in BURN_EXECUTE_CONTEXT* pContext
    );
static HRESULT ExecuteDependencyAction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pAction,
    __in BURN_EXECUTE_CONTEXT* pContext
    );
static HRESULT ExecuteMsiBeginTransaction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in BURN_EXECUTE_CONTEXT* pContext
    );
static HRESULT ExecuteMsiCommitTransaction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in BURN_EXECUTE_CONTEXT* pContext
    );
static HRESULT ExecuteMsiRollbackTransaction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in BURN_EXECUTE_CONTEXT* pContext
    );
static void ResetTransactionRegistrationState(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BOOL fCommit
    );
static HRESULT CleanPackage(
    __in HANDLE hElevatedPipe,
    __in BURN_PACKAGE* pPackage
    );
static int GenericExecuteMessageHandler(
    __in GENERIC_EXECUTE_MESSAGE* pMessage,
    __in LPVOID pvContext
    );
static int MsiExecuteMessageHandler(
    __in WIU_MSI_EXECUTE_MESSAGE* pMessage,
    __in_opt LPVOID pvContext
    );
static HRESULT ReportOverallProgressTicks(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BOOL fRollback,
    __in DWORD cOverallProgressTicksTotal,
    __in DWORD cOverallProgressTicks
    );
static HRESULT ExecutePackageComplete(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PACKAGE* pPackage,
    __in HRESULT hrOverall,
    __in HRESULT hrExecute,
    __in BOOL fRollback,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend
    );


// function definitions

extern "C" void ApplyInitialize()
{
    // Prevent the system from sleeping.
    ::SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED);
}

extern "C" void ApplyUninitialize()
{
    ::SetThreadExecutionState(ES_CONTINUOUS);
}

extern "C" HRESULT ApplySetVariables(
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;

    hr = VariableSetString(pVariables, BURN_BUNDLE_FORCED_RESTART_PACKAGE, NULL, TRUE, FALSE);
    ExitOnFailure(hr, "Failed to set the bundle forced restart package built-in variable.");

LExit:
    return hr;
}

extern "C" void ApplyReset(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGES* pPackages
    )
{
    UserExperienceExecuteReset(pUX);

    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        BURN_PACKAGE* pPackage = pPackages->rgPackages + i;
        pPackage->hrCacheResult = S_OK;
        pPackage->transactionRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN;
    }
}

extern "C" HRESULT ApplyLock(
    __in BOOL /*fPerMachine*/,
    __out HANDLE* phLock
    )
{
    HRESULT hr = S_OK;
    *phLock = NULL;

#if 0 // eventually figure out the correct way to support this. In its current form, embedded bundles (including related bundles) are hosed.
    DWORD er = ERROR_SUCCESS;
    HANDLE hLock = NULL;

    hLock = ::CreateMutexW(NULL, TRUE, fPerMachine ? L"Global\\WixBurnExecutionLock" : L"Local\\WixBurnExecutionLock");
    ExitOnNullWithLastError(hLock, hr, "Failed to create lock.");

    er = ::GetLastError();
    if (ERROR_ALREADY_EXISTS == er)
    {
        ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSTALL_ALREADY_RUNNING));
    }

    *phLock = hLock;
    hLock = NULL;

LExit:
    ReleaseHandle(hLock);
#endif
    return hr;
}

extern "C" HRESULT ApplyRegister(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczEngineWorkingPath = NULL;

    hr = UserExperienceOnRegisterBegin(&pEngineState->userExperience);
    ExitOnRootFailure(hr, "BA aborted register begin.");

    // If we have a resume mode that suggests the bundle is on the machine.
    if (BOOTSTRAPPER_RESUME_TYPE_REBOOT_PENDING < pEngineState->command.resumeType)
    {
        // resume previous session
        if (pEngineState->registration.fPerMachine)
        {
            hr = ElevationSessionResume(pEngineState->companionConnection.hPipe, pEngineState->registration.sczResumeCommandLine, pEngineState->registration.fDisableResume, &pEngineState->variables);
            ExitOnFailure(hr, "Failed to resume registration session in per-machine process.");
        }
        else
        {
            hr = RegistrationSessionResume(&pEngineState->registration, &pEngineState->variables);
            ExitOnFailure(hr, "Failed to resume registration session.");
        }
    }
    else // need to complete registration on the machine.
    {
        hr = CacheCalculateBundleWorkingPath(pEngineState->registration.sczId, pEngineState->registration.sczExecutableName, &sczEngineWorkingPath);
        ExitOnFailure(hr, "Failed to calculate working path for engine.");

        // begin new session
        if (pEngineState->registration.fPerMachine)
        {
            hr = ElevationSessionBegin(pEngineState->companionConnection.hPipe, sczEngineWorkingPath, pEngineState->registration.sczResumeCommandLine, pEngineState->registration.fDisableResume, &pEngineState->variables, pEngineState->plan.dwRegistrationOperations, pEngineState->plan.dependencyRegistrationAction, pEngineState->plan.qwEstimatedSize);
            ExitOnFailure(hr, "Failed to begin registration session in per-machine process.");
        }
        else
        {
            hr = RegistrationSessionBegin(sczEngineWorkingPath, &pEngineState->registration, &pEngineState->variables, pEngineState->plan.dwRegistrationOperations, pEngineState->plan.dependencyRegistrationAction, pEngineState->plan.qwEstimatedSize);
            ExitOnFailure(hr, "Failed to begin registration session.");
        }
    }

    // Apply any registration actions.
    HRESULT hrExecuteRegistration = ExecuteDependentRegistrationActions(pEngineState->companionConnection.hPipe, &pEngineState->registration, pEngineState->plan.rgRegistrationActions, pEngineState->plan.cRegistrationActions);
    UNREFERENCED_PARAMETER(hrExecuteRegistration);

    // Try to save engine state.
    hr = CoreSaveEngineState(pEngineState);
    if (FAILED(hr))
    {
        LogErrorId(hr, MSG_STATE_NOT_SAVED);
        hr = S_OK;
    }

LExit:
    UserExperienceOnRegisterComplete(&pEngineState->userExperience, hr);
    ReleaseStr(sczEngineWorkingPath);

    return hr;
}

extern "C" HRESULT ApplyUnregister(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BOOL fFailedOrRollback,
    __in BOOL fSuspend,
    __in BOOTSTRAPPER_APPLY_RESTART restart
    )
{
    HRESULT hr = S_OK;
    BURN_RESUME_MODE resumeMode = BURN_RESUME_MODE_NONE;
    BOOL fKeepRegistration = pEngineState->plan.fDisallowRemoval;

    CalculateKeepRegistration(pEngineState, &fKeepRegistration);

    hr = UserExperienceOnUnregisterBegin(&pEngineState->userExperience, &fKeepRegistration);
    ExitOnRootFailure(hr, "BA aborted unregister begin.");

    // Calculate the correct resume mode. If a restart has been initiated, that trumps all other
    // modes. If the user chose to suspend the install then we'll use that as the resume mode.
    // Barring those special cases, if it was determined that we should keep the registration then
    // do that, otherwise the resume mode was initialized to none and registration will be removed.
    if (BOOTSTRAPPER_APPLY_RESTART_INITIATED == restart)
    {
        resumeMode = BURN_RESUME_MODE_REBOOT_PENDING;
    }
    else if (fSuspend)
    {
        resumeMode = BURN_RESUME_MODE_SUSPEND;
    }
    else if (fKeepRegistration)
    {
        resumeMode = BURN_RESUME_MODE_ARP;
    }

    // If apply failed in any way and we're going to be keeping the bundle registered then
    // execute any rollback dependency registration actions.
    if (fFailedOrRollback && fKeepRegistration)
    {
        // Execute any rollback registration actions.
        HRESULT hrRegistrationRollback = ExecuteDependentRegistrationActions(pEngineState->companionConnection.hPipe, &pEngineState->registration, pEngineState->plan.rgRollbackRegistrationActions, pEngineState->plan.cRollbackRegistrationActions);
        UNREFERENCED_PARAMETER(hrRegistrationRollback);
    }

    if (pEngineState->registration.fPerMachine)
    {
        hr = ElevationSessionEnd(pEngineState->companionConnection.hPipe, resumeMode, restart, pEngineState->plan.dependencyRegistrationAction);
        ExitOnFailure(hr, "Failed to end session in per-machine process.");
    }
    else
    {
        hr = RegistrationSessionEnd(&pEngineState->registration, &pEngineState->variables, &pEngineState->packages, resumeMode, restart, pEngineState->plan.dependencyRegistrationAction);
        ExitOnFailure(hr, "Failed to end session in per-user process.");
    }

    pEngineState->resumeMode = resumeMode;

LExit:
    UserExperienceOnUnregisterComplete(&pEngineState->userExperience, hr);

    return hr;
}

extern "C" HRESULT ApplyCache(
    __in HANDLE hSourceEngineFile,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PLAN* pPlan,
    __in HANDLE hPipe,
    __inout DWORD* pcOverallProgressTicks,
    __inout BOOL* pfRollback
    )
{
    HRESULT hr = S_OK;
    DWORD dwCheckpoint = 0;
    BURN_CACHE_CONTEXT cacheContext = { };
    BURN_PACKAGE* pPackage = NULL;

    *pfRollback = FALSE;

    hr = UserExperienceOnCacheBegin(pUX);
    ExitOnRootFailure(hr, "BA aborted cache.");

    cacheContext.hSourceEngineFile = hSourceEngineFile;
    cacheContext.pPayloads = pPlan->pPayloads;
    cacheContext.pUX = pUX;
    cacheContext.pVariables = pVariables;
    cacheContext.qwTotalCacheSize = pPlan->qwCacheSizeTotal;
    cacheContext.wzLayoutDirectory = pPlan->sczLayoutDirectory;

    hr = MemAllocArray(reinterpret_cast<LPVOID*>(&cacheContext.rgSearchPaths), sizeof(LPWSTR), BURN_CACHE_MAX_SEARCH_PATHS);
    ExitOnNull(cacheContext.rgSearchPaths, hr, E_OUTOFMEMORY, "Failed to allocate cache search paths array.");

    for (DWORD i = 0; i < pPlan->cCacheActions; ++i)
    {
        BURN_CACHE_ACTION* pCacheAction = pPlan->rgCacheActions + i;
        cacheContext.hPipe = hPipe;
        pPackage = NULL;

        switch (pCacheAction->type)
        {
        case BURN_CACHE_ACTION_TYPE_CHECKPOINT:
            dwCheckpoint = pCacheAction->checkpoint.dwId;
            break;

        case BURN_CACHE_ACTION_TYPE_LAYOUT_BUNDLE:
            hr = ApplyLayoutBundle(&cacheContext, pCacheAction->bundleLayout.pPayloadGroup, pCacheAction->bundleLayout.sczExecutableName, pCacheAction->bundleLayout.sczUnverifiedPath, pCacheAction->bundleLayout.qwBundleSize);
            ExitOnFailure(hr, "Failed cache action: %ls", L"layout bundle");

            ++(*pcOverallProgressTicks);

            hr = ReportOverallProgressTicks(pUX, FALSE, pPlan->cOverallProgressTicksTotal, *pcOverallProgressTicks);
            LogExitOnFailure(hr, MSG_USER_CANCELED, "Cancel during cache: %ls", L"layout bundle");

            break;

        case BURN_CACHE_ACTION_TYPE_PACKAGE:
            pPackage = pCacheAction->package.pPackage;

            if (!pPackage->fPerMachine && !cacheContext.wzLayoutDirectory)
            {
                hr = CacheGetCompletedPath(FALSE, pPackage->sczCacheId, &pPackage->sczCacheFolder);
                ExitOnFailure(hr, "Failed to get cached path for package with cache id: %ls", pPackage->sczCacheId);

                cacheContext.hPipe = INVALID_HANDLE_VALUE;
            }

            hr = ApplyCachePackage(&cacheContext, pPackage);
            ExitOnFailure(hr, "Failed cache action: %ls", L"cache package");

            ++(*pcOverallProgressTicks);

            hr = ReportOverallProgressTicks(pUX, FALSE, pPlan->cOverallProgressTicksTotal, *pcOverallProgressTicks);
            LogExitOnFailure(hr, MSG_USER_CANCELED, "Cancel during cache: %ls", L"cache package");

            break;

        case BURN_CACHE_ACTION_TYPE_CONTAINER:
            Assert(pPlan->sczLayoutDirectory);
            hr = ApplyLayoutContainer(&cacheContext, pCacheAction->container.pContainer);
            ExitOnFailure(hr, "Failed cache action: %ls", L"layout container");
            
            break;

        case BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT:
            if (!::SetEvent(pCacheAction->syncpoint.hEvent))
            {
                ExitWithLastError(hr, "Failed to set syncpoint event.");
            }
            break;

        default:
            AssertSz(FALSE, "Unknown cache action.");
            break;
        }
    }

LExit:
    if (FAILED(hr))
    {
        DoRollbackCache(pUX, pPlan, hPipe, dwCheckpoint);
        *pfRollback = TRUE;
    }

    // Clean up any remanents in the cache.
    if (INVALID_HANDLE_VALUE != hPipe)
    {
        ElevationCacheCleanup(hPipe);
    }

    CacheCleanup(FALSE, pPlan->wzBundleId);

    for (DWORD i = 0; i < cacheContext.cSearchPathsMax; ++i)
    {
        ReleaseNullStr(cacheContext.rgSearchPaths[i]);
    }
    ReleaseMem(cacheContext.rgSearchPaths);
    ReleaseStr(cacheContext.sczLastUsedFolderCandidate);

    UserExperienceOnCacheComplete(pUX, hr);
    return hr;
}

extern "C" HRESULT ApplyExecute(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HANDLE hCacheThread,
    __inout DWORD* pcOverallProgressTicks,
    __out BOOL* pfRollback,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    HRESULT hrRollback = S_OK;
    BURN_EXECUTE_ACTION_CHECKPOINT* pCheckpoint = NULL;
    BURN_EXECUTE_CONTEXT context = { };
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = NULL;
    BOOL fSeekNextRollbackBoundary = FALSE;

    context.pUX = &pEngineState->userExperience;
    context.cExecutePackagesTotal = pEngineState->plan.cExecutePackagesTotal;
    context.pcOverallProgressTicks = pcOverallProgressTicks;

    *pfRollback = FALSE;
    *pfSuspend = FALSE;

    // Send execute begin to BA.
    hr = UserExperienceOnExecuteBegin(&pEngineState->userExperience, pEngineState->plan.cExecutePackagesTotal);
    ExitOnRootFailure(hr, "BA aborted execute begin.");

    // Do execute actions.
    for (DWORD i = 0; i < pEngineState->plan.cExecuteActions; ++i)
    {
        BURN_EXECUTE_ACTION* pExecuteAction = &pEngineState->plan.rgExecuteActions[i];
        if (pExecuteAction->fDeleted)
        {
            continue;
        }

        // If we are seeking the next rollback boundary, skip if this action wasn't it.
        if (fSeekNextRollbackBoundary)
        {
            if (BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY == pExecuteAction->type)
            {
                continue;
            }
            else
            {
                fSeekNextRollbackBoundary = FALSE;
            }
        }

        // Execute the action.
        hr = DoExecuteAction(pEngineState, pExecuteAction, hCacheThread, &context, &pRollbackBoundary, &pCheckpoint, pfSuspend, pRestart);

        if (*pfSuspend || BOOTSTRAPPER_APPLY_RESTART_INITIATED == *pRestart)
        {
            if (pCheckpoint && pCheckpoint->pActiveRollbackBoundary && pCheckpoint->pActiveRollbackBoundary->fActiveTransaction)
            {
                hr = E_INVALIDSTATE;
                LogId(REPORT_ERROR, MSG_RESTART_REQUEST_DURING_MSI_TRANSACTION, pCheckpoint->pActiveRollbackBoundary->sczId);
            }
            else
            {
                ExitFunction();
            }
        }

        if (FAILED(hr))
        {
            // If rollback is disabled, keep what we have and always end execution here.
            if (pEngineState->plan.fDisableRollback)
            {
                LogId(REPORT_WARNING, MSG_PLAN_ROLLBACK_DISABLED);

                if (pCheckpoint && pCheckpoint->pActiveRollbackBoundary && pCheckpoint->pActiveRollbackBoundary->fActiveTransaction)
                {
                    hrRollback = ExecuteMsiCommitTransaction(pEngineState, pCheckpoint->pActiveRollbackBoundary, &context);
                    IgnoreRollbackError(hrRollback, "Failed commit transaction from disable rollback");
                }

                *pfRollback = TRUE;
                break;
            }

            if (pCheckpoint)
            {
                // If inside a MSI transaction, roll it back.
                if (pCheckpoint->pActiveRollbackBoundary && pCheckpoint->pActiveRollbackBoundary->fActiveTransaction)
                {
                    hrRollback = ExecuteMsiRollbackTransaction(pEngineState, pCheckpoint->pActiveRollbackBoundary, &context);
                    IgnoreRollbackError(hrRollback, "Failed rolling back transaction");
                }

                // The action failed, roll back to previous rollback boundary.
                hrRollback = DoRollbackActions(pEngineState, &context, pCheckpoint->dwId, pRestart);
                IgnoreRollbackError(hrRollback, "Failed rollback actions");
            }

            // If the rollback boundary is vital, end execution here.
            if (pRollbackBoundary && pRollbackBoundary->fVital)
            {
                *pfRollback = TRUE;
                break;
            }

            // Move forward to next rollback boundary.
            fSeekNextRollbackBoundary = TRUE;
        }
    }

LExit:
    // Send execute complete to BA.
    UserExperienceOnExecuteComplete(&pEngineState->userExperience, hr);

    return hr;
}

extern "C" void ApplyClean(
    __in BURN_USER_EXPERIENCE* /*pUX*/,
    __in BURN_PLAN* pPlan,
    __in HANDLE hPipe
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pPlan->cCleanActions; ++i)
    {
        BURN_CLEAN_ACTION* pCleanAction = pPlan->rgCleanActions + i;
        BURN_PACKAGE* pPackage = pCleanAction->pPackage;

        hr = CleanPackage(hPipe, pPackage);
    }
}


// internal helper functions

static void CalculateKeepRegistration(
    __in BURN_ENGINE_STATE* pEngineState,
    __inout BOOL* pfKeepRegistration
    )
{
    LogId(REPORT_STANDARD, MSG_POST_APPLY_CALCULATE_REGISTRATION);

    for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
    {
        BURN_PACKAGE* pPackage = pEngineState->packages.rgPackages + i;

        if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
        {
            MspEngineFinalizeInstallRegistrationState(pPackage);
        }

        LogId(REPORT_STANDARD, MSG_POST_APPLY_PACKAGE, pPackage->sczId, LoggingPackageRegistrationStateToString(pPackage->fCanAffectRegistration, pPackage->installRegistrationState), LoggingPackageRegistrationStateToString(pPackage->fCanAffectRegistration, pPackage->cacheRegistrationState));

        if (!pPackage->fCanAffectRegistration)
        {
            continue;
        }

        if (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pPackage->installRegistrationState ||
            BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pPackage->cacheRegistrationState)
        {
            *pfKeepRegistration = TRUE;
        }
    }
}

static HRESULT ExecuteDependentRegistrationActions(
    __in HANDLE hPipe,
    __in const BURN_REGISTRATION* pRegistration,
    __in_ecount(cActions) const BURN_DEPENDENT_REGISTRATION_ACTION* rgActions,
    __in DWORD cActions
    )
{
    HRESULT hr = S_OK;

    for (DWORD iAction = 0; iAction < cActions; ++iAction)
    {
        const BURN_DEPENDENT_REGISTRATION_ACTION* pAction = rgActions + iAction;

        if (pRegistration->fPerMachine)
        {
            hr = ElevationProcessDependentRegistration(hPipe, pAction);
            ExitOnFailure(hr, "Failed to execute dependent registration action.");
        }
        else
        {
            hr = DependencyProcessDependentRegistration(pRegistration, pAction);
            ExitOnFailure(hr, "Failed to process dependency registration action.");
        }
    }

LExit:
    return hr;
}

static HRESULT ApplyCachePackage(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BOOL fCanceledBegin = FALSE;
    BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION cachePackageCompleteAction = BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION_NONE;

    for (;;)
    {
        fCanceledBegin = FALSE;

        hr = UserExperienceOnCachePackageBegin(pContext->pUX, pPackage->sczId, pPackage->payloads.cItems, pPackage->payloads.qwTotalSize);
        if (FAILED(hr))
        {
            fCanceledBegin = TRUE;
        }
        else
        {
            for (DWORD i = 0; i < pPackage->payloads.cItems; ++i)
            {
                BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem = pPackage->payloads.rgItems + i;

                hr = ApplyProcessPayload(pContext, pPackage, pPayloadGroupItem);
                if (FAILED(hr))
                {
                    break;
                }
            }
        }

        pPackage->hrCacheResult = hr;
        cachePackageCompleteAction = SUCCEEDED(hr) || pPackage->fVital || fCanceledBegin ? BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION_NONE : BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION_IGNORE;
        UserExperienceOnCachePackageComplete(pContext->pUX, pPackage->sczId, hr, &cachePackageCompleteAction);

        if (SUCCEEDED(hr))
        {
            break;
        }

        if (BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION_RETRY == cachePackageCompleteAction)
        {
            for (DWORD i = 0; i < pPackage->payloads.cItems; ++i)
            {
                BURN_PAYLOAD_GROUP_ITEM* pItem = pPackage->payloads.rgItems + i;
                if (pItem->fCached)
                {
                    pItem->pPayload->cRemainingInstances += 1;
                    pItem->fCached = FALSE;
                }

                if (pItem->qwCommittedCacheProgress)
                {
                    pContext->qwSuccessfulCacheProgress -= pItem->qwCommittedCacheProgress;
                    pItem->qwCommittedCacheProgress = 0;
                }
            }

            LogErrorId(hr, MSG_CACHE_RETRYING_PACKAGE, pPackage->sczId, NULL, NULL);

            continue;
        }
        else if (BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION_IGNORE == cachePackageCompleteAction && !pPackage->fVital) // ignore non-vital download failures.
        {
            LogId(REPORT_STANDARD, MSG_CACHE_CONTINUING_NONVITAL_PACKAGE, pPackage->sczId, hr);
            hr = S_OK;
        }
        else if (fCanceledBegin)
        {
            LogExitOnFailure(hr, MSG_USER_CANCELED, "Cancel during cache: %ls: %ls", L"begin cache package", pPackage->sczId);
        }

        break;
    }

LExit:
    return hr;
}

static HRESULT ApplyExtractContainer(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_CONTAINER* pContainer
    )
{
    HRESULT hr = S_OK;

    if (pContainer->qwCommittedCacheProgress)
    {
        pContext->qwSuccessfulCacheProgress -= pContainer->qwCommittedCacheProgress;
        pContainer->qwCommittedCacheProgress = 0;
    }

    if (pContainer->qwCommittedExtractProgress)
    {
        pContext->qwSuccessfulCacheProgress -= pContainer->qwCommittedExtractProgress;
        pContainer->qwCommittedExtractProgress = 0;
    }

    if (!pContainer->fActuallyAttached)
    {
        hr = ApplyAcquireContainerOrPayload(pContext, pContainer, NULL, NULL);
        LogExitOnFailure(hr, MSG_FAILED_ACQUIRE_CONTAINER, "Failed to acquire container: %ls to working path: %ls", pContainer->sczId, pContainer->sczUnverifiedPath);
    }

    hr = ExtractContainer(pContext, pContainer);
    LogExitOnFailure(hr, MSG_FAILED_EXTRACT_CONTAINER, "Failed to extract payloads from container: %ls to working path: %ls", pContainer->sczId, pContainer->sczUnverifiedPath);

    if (pContext->sczLastUsedFolderCandidate)
    {
        // We successfully copied from a source location, set that as the last used source.
        CacheSetLastUsedSource(pContext->pVariables, pContext->sczLastUsedFolderCandidate, pContainer->sczFilePath);
    }

    if (pContainer->qwExtractSizeTotal < pContainer->qwCommittedExtractProgress)
    {
        AssertSz(FALSE, "Container extracted more than planned.");
        pContext->qwSuccessfulCacheProgress -= pContainer->qwCommittedExtractProgress;
        pContext->qwSuccessfulCacheProgress += pContainer->qwExtractSizeTotal;
    }
    else
    {
        pContext->qwSuccessfulCacheProgress += pContainer->qwExtractSizeTotal - pContainer->qwCommittedExtractProgress;
    }

    pContainer->qwCommittedExtractProgress = pContainer->qwExtractSizeTotal;

LExit:
    ReleaseNullStr(pContext->sczLastUsedFolderCandidate);

    return hr;
}

static HRESULT ApplyLayoutBundle(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_PAYLOAD_GROUP* pPayloads,
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzUnverifiedPath,
    __in DWORD64 qwBundleSize
    )
{
    HRESULT hr = S_OK;

    hr = LayoutBundle(pContext, wzExecutableName, wzUnverifiedPath, qwBundleSize);
    ExitOnFailure(hr, "Failed to layout bundle.");

    for (DWORD i = 0; i < pPayloads->cItems; ++i)
    {
        BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem = pPayloads->rgItems + i;

        hr = ApplyProcessPayload(pContext, NULL, pPayloadGroupItem);
        ExitOnFailure(hr, "Failed to layout bundle payload: %ls", pPayloadGroupItem->pPayload->sczKey);
    }

LExit:
    return hr;
}

static HRESULT ApplyLayoutContainer(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_CONTAINER* pContainer
    )
{
    HRESULT hr = S_OK;
    DWORD cTryAgainAttempts = 0;
    BOOL fRetry = FALSE;

    Assert(!pContainer->fAttached);

    hr = ApplyCacheVerifyContainerOrPayload(pContext, pContainer, NULL, NULL);
    if (SUCCEEDED(hr))
    {
        ExitFunction();
    }

    for (;;)
    {
        fRetry = FALSE;

        hr = ApplyAcquireContainerOrPayload(pContext, pContainer, NULL, NULL);
        LogExitOnFailure(hr, MSG_FAILED_ACQUIRE_CONTAINER, "Failed to acquire container: %ls to working path: %ls", pContainer->sczId, pContainer->sczUnverifiedPath);

        hr = LayoutOrCacheContainerOrPayload(pContext, pContainer, NULL, NULL, cTryAgainAttempts, &fRetry);
        if (SUCCEEDED(hr))
        {
            break;
        }
        else
        {
            LogErrorId(hr, MSG_FAILED_LAYOUT_CONTAINER, pContainer->sczId, pContext->wzLayoutDirectory, pContainer->sczUnverifiedPath);

            if (!fRetry)
            {
                ExitFunction();
            }

            ++cTryAgainAttempts;
            pContext->qwSuccessfulCacheProgress -= pContainer->qwCommittedCacheProgress;
            pContainer->qwCommittedCacheProgress = 0;
            ReleaseNullStr(pContext->sczLastUsedFolderCandidate);
            LogErrorId(hr, MSG_CACHE_RETRYING_CONTAINER, pContainer->sczId, NULL, NULL);
        }
    }

LExit:
    ReleaseNullStr(pContext->sczLastUsedFolderCandidate);

    return hr;
}

static HRESULT ApplyProcessPayload(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_opt BURN_PACKAGE* pPackage,
    __in BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem
    )
{
    HRESULT hr = S_OK;
    DWORD cTryAgainAttempts = 0;
    BOOL fRetry = FALSE;
    BURN_PAYLOAD* pPayload = pPayloadGroupItem->pPayload;

    Assert(pContext->pPayloads && pPackage || pContext->wzLayoutDirectory);

    if (pPayload->pContainer && pContext->wzLayoutDirectory)
    {
        ExitFunction();
    }

    hr = ApplyCacheVerifyContainerOrPayload(pContext, NULL, pPackage, pPayloadGroupItem);
    if (SUCCEEDED(hr))
    {
        ExitFunction();
    }

    for (;;)
    {
        fRetry = FALSE;

        hr = ApplyAcquireContainerOrPayload(pContext, NULL, pPackage, pPayloadGroupItem);
        LogExitOnFailure(hr, MSG_FAILED_ACQUIRE_PAYLOAD, "Failed to acquire payload: %ls to working path: %ls", pPayload->sczKey, pPayload->sczUnverifiedPath);

        hr = LayoutOrCacheContainerOrPayload(pContext, NULL, pPackage, pPayloadGroupItem, cTryAgainAttempts, &fRetry);
        if (SUCCEEDED(hr))
        {
            break;
        }
        else
        {
            LogErrorId(hr, pContext->wzLayoutDirectory ? MSG_FAILED_LAYOUT_PAYLOAD : MSG_FAILED_CACHE_PAYLOAD, pPayload->sczKey, pContext->wzLayoutDirectory, pPayload->sczUnverifiedPath);

            if (!fRetry)
            {
                ExitFunction();
            }

            ++cTryAgainAttempts;
            pContext->qwSuccessfulCacheProgress -= pPayloadGroupItem->qwCommittedCacheProgress;
            pPayloadGroupItem->qwCommittedCacheProgress = 0;
            ReleaseNullStr(pContext->sczLastUsedFolderCandidate);
            LogErrorId(hr, MSG_CACHE_RETRYING_PAYLOAD, pPayload->sczKey, NULL, NULL);
        }
    }

LExit:
    ReleaseNullStr(pContext->sczLastUsedFolderCandidate);

    return hr;
}

static HRESULT ApplyCacheVerifyContainerOrPayload(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_opt BURN_CONTAINER* pContainer,
    __in_opt BURN_PACKAGE* pPackage,
    __in_opt BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem
    )
{
    AssertSz(pContainer || pPayloadGroupItem, "Must provide a container or a payload.");

    HRESULT hr = S_OK;
    BURN_CACHE_PROGRESS_CONTEXT progress = { };

    progress.pCacheContext = pContext;
    progress.pContainer = pContainer;
    progress.pPackage = pPackage;
    progress.pPayloadGroupItem = pPayloadGroupItem;

    if (pContainer)
    {
        hr = CacheVerifyContainer(pContainer, pContext->wzLayoutDirectory, CacheMessageHandler, CacheProgressRoutine, &progress);
    }
    else if (!pContext->wzLayoutDirectory && INVALID_HANDLE_VALUE != pContext->hPipe)
    {
        hr = ElevationCacheVerifyPayload(pContext->hPipe, pPackage, pPayloadGroupItem->pPayload, CacheMessageHandler, CacheProgressRoutine, &progress);
    }
    else
    {
        hr = CacheVerifyPayload(pPayloadGroupItem->pPayload, pContext->wzLayoutDirectory ? pContext->wzLayoutDirectory : pPackage->sczCacheFolder, CacheMessageHandler, CacheProgressRoutine, &progress);
    }

    return hr;
}

static HRESULT ExtractContainer(
    __in BURN_CACHE_CONTEXT* pContext,
    __in BURN_CONTAINER* pContainer
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER_CONTEXT context = { };
    HANDLE hContainerHandle = INVALID_HANDLE_VALUE;
    LPWSTR sczStreamName = NULL;
    BURN_PAYLOAD* pExtract = NULL;
    BURN_CACHE_PROGRESS_CONTEXT progress = { };

    progress.pCacheContext = pContext;
    progress.pContainer = pContainer;
    progress.type = BURN_CACHE_PROGRESS_TYPE_EXTRACT;

    // If the container is actually attached, then it was planned to be acquired through hSourceEngineFile.
    if (pContainer->fActuallyAttached)
    {
        hContainerHandle = pContext->hSourceEngineFile;
    }

    hr = ContainerOpen(&context, pContainer, hContainerHandle, pContainer->sczUnverifiedPath);
    ExitOnFailure(hr, "Failed to open container: %ls.", pContainer->sczId);

    while (S_OK == (hr = ContainerNextStream(&context, &sczStreamName)))
    {
        BOOL fExtracted = FALSE;

        hr = PayloadFindEmbeddedBySourcePath(pContext->pPayloads, sczStreamName, &pExtract);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to find embedded payload by source path: %ls container: %ls", sczStreamName, pContainer->sczId);

            // Skip payloads that weren't planned or have already been cached.
            if (pExtract->sczUnverifiedPath && pExtract->cRemainingInstances)
            {
                progress.pPayload = pExtract;

                hr = PreparePayloadDestinationPath(pExtract->sczUnverifiedPath);
                ExitOnFailure(hr, "Failed to prepare payload destination path: %ls", pExtract->sczUnverifiedPath);

                hr = UserExperienceOnCachePayloadExtractBegin(pContext->pUX, pContainer->sczId, pExtract->sczKey);
                if (FAILED(hr))
                {
                    UserExperienceOnCachePayloadExtractComplete(pContext->pUX, pContainer->sczId, pExtract->sczKey, hr);
                    ExitOnRootFailure(hr, "BA aborted cache payload extract begin.");
                }

                // TODO: Send progress when extracting stream to file.
                hr = ContainerStreamToFile(&context, pExtract->sczUnverifiedPath);
                // Error handling happens after sending complete message to BA.

                // If succeeded, send 100% complete here to make sure progress was sent to the BA.
                if (SUCCEEDED(hr))
                {
                    hr = CompleteCacheProgress(&progress, pExtract->qwFileSize);
                }

                UserExperienceOnCachePayloadExtractComplete(pContext->pUX, pContainer->sczId, pExtract->sczKey, hr);
                ExitOnFailure(hr, "Failed to extract payload: %ls from container: %ls", sczStreamName, pContainer->sczId);

                fExtracted = TRUE;
            }
        }

        if (!fExtracted)
        {
            hr = ContainerSkipStream(&context);
            ExitOnFailure(hr, "Failed to skip the extraction of payload: %ls from container: %ls", sczStreamName, pContainer->sczId);
        }
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to extract all payloads from container: %ls", pContainer->sczId);

LExit:
    ReleaseStr(sczStreamName);
    ContainerClose(&context);

    return hr;
}

static HRESULT LayoutBundle(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzUnverifiedPath,
    __in DWORD64 qwBundleSize
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczBundlePath = NULL;
    LPWSTR sczBundleDownloadUrl = NULL;
    LPWSTR sczDestinationPath = NULL;
    int nEquivalentPaths = 0;
    BOOTSTRAPPER_CACHE_OPERATION cacheOperation = BOOTSTRAPPER_CACHE_OPERATION_NONE;
    BURN_CACHE_PROGRESS_CONTEXT progress = { };
    BOOL fRetry = FALSE;
    BOOL fRetryAcquire = FALSE;
    BOOL fCanceledBegin = FALSE;

    progress.pCacheContext = pContext;

    hr = VariableGetString(pContext->pVariables, BURN_BUNDLE_SOURCE_PROCESS_PATH, &sczBundlePath);
    if (FAILED(hr))
    {
        if  (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get path to bundle source process path to layout.");
        }

        hr = PathForCurrentProcess(&sczBundlePath, NULL);
        ExitOnFailure(hr, "Failed to get path to bundle to layout.");
    }

    hr = PathConcat(pContext->wzLayoutDirectory, wzExecutableName, &sczDestinationPath);
    ExitOnFailure(hr, "Failed to concat layout path for bundle.");

    // If the destination path is the currently running bundle, bail.
    hr = PathCompare(sczBundlePath, sczDestinationPath, &nEquivalentPaths);
    ExitOnFailure(hr, "Failed to determine if layout bundle path was equivalent with current process path.");

    if (CSTR_EQUAL == nEquivalentPaths && FileExistsEx(sczDestinationPath, NULL))
    {
        hr = UserExperienceOnCacheContainerOrPayloadVerifyBegin(pContext->pUX, NULL, NULL);
        if (FAILED(hr))
        {
            UserExperienceOnCacheContainerOrPayloadVerifyComplete(pContext->pUX, NULL, NULL, hr);
            ExitOnRootFailure(hr, "BA aborted cache payload verify begin.");
        }

        progress.type = BURN_CACHE_PROGRESS_TYPE_CONTAINER_OR_PAYLOAD_VERIFY;
        hr = CompleteCacheProgress(&progress, qwBundleSize);

        UserExperienceOnCacheContainerOrPayloadVerifyComplete(pContext->pUX, NULL, NULL, hr);

        ExitFunction();
    }

    do
    {
        hr = S_OK;
        fRetry = FALSE;
        progress.type = BURN_CACHE_PROGRESS_TYPE_ACQUIRE;

        for (;;)
        {
            fRetryAcquire = FALSE;
            progress.fCancel = FALSE;
            fCanceledBegin = FALSE;

            hr = UserExperienceOnCacheAcquireBegin(pContext->pUX, NULL, NULL, &sczBundlePath, &sczBundleDownloadUrl, NULL, &cacheOperation);

            if (FAILED(hr))
            {
                fCanceledBegin = TRUE;
            }
            else
            {
                hr = CopyPayload(&progress, pContext->hSourceEngineFile, sczBundlePath, wzUnverifiedPath);
                // Error handling happens after sending complete message to BA.

                // If succeeded, send 100% complete here to make sure progress was sent to the BA.
                if (SUCCEEDED(hr))
                {
                    hr = CompleteCacheProgress(&progress, qwBundleSize);
                }
            }

            UserExperienceOnCacheAcquireComplete(pContext->pUX, NULL, NULL, hr, &fRetryAcquire);
            if (fRetryAcquire)
            {
                continue;
            }
            else if (fCanceledBegin)
            {
                ExitOnRootFailure(hr, "BA aborted cache acquire begin.");
            }

            ExitOnFailure(hr, "Failed to copy bundle from: '%ls' to: '%ls'", sczBundlePath, wzUnverifiedPath);
            break;
        }

        do
        {
            fCanceledBegin = FALSE;

            hr = UserExperienceOnCacheVerifyBegin(pContext->pUX, NULL, NULL);

            if (FAILED(hr))
            {
                fCanceledBegin = TRUE;
            }
            else
            {
                hr = CacheLayoutBundle(wzExecutableName, pContext->wzLayoutDirectory, wzUnverifiedPath, qwBundleSize, CacheMessageHandler, CacheProgressRoutine, &progress);
            }

            BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION action = BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION_NONE;
            UserExperienceOnCacheVerifyComplete(pContext->pUX, NULL, NULL, hr, &action);
            if (BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION_RETRYVERIFICATION == action)
            {
                hr = S_FALSE; // retry verify.
            }
            else if (BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION_RETRYACQUISITION == action)
            {
                fRetry = TRUE; // go back and retry acquire.
            }
            else if (fCanceledBegin)
            {
                ExitOnRootFailure(hr, "BA aborted cache verify begin.");
            }
        } while (S_FALSE == hr);

        if (fRetry)
        {
            pContext->qwSuccessfulCacheProgress -= qwBundleSize; // Acquire
        }
    } while (fRetry);
    LogExitOnFailure(hr, MSG_FAILED_LAYOUT_BUNDLE, "Failed to layout bundle: %ls to layout directory: %ls", sczBundlePath, pContext->wzLayoutDirectory);

LExit:
    ReleaseStr(sczDestinationPath);
    ReleaseStr(sczBundleDownloadUrl);
    ReleaseStr(sczBundlePath);

    return hr;
}

static HRESULT ApplyAcquireContainerOrPayload(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_opt BURN_CONTAINER* pContainer,
    __in_opt BURN_PACKAGE* pPackage,
    __in_opt BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem
    )
{
    AssertSz(pContainer || pPayloadGroupItem, "Must provide a container or a payload.");

    HRESULT hr = S_OK;
    BURN_CACHE_PROGRESS_CONTEXT progress = { };
    BOOL fRetry = FALSE;

    progress.pCacheContext = pContext;
    progress.type = BURN_CACHE_PROGRESS_TYPE_ACQUIRE;
    progress.pContainer = pContainer;
    progress.pPackage = pPackage;
    progress.pPayloadGroupItem = pPayloadGroupItem;

    do
    {
        hr = AcquireContainerOrPayload(&progress, &fRetry);

        if (fRetry)
        {
            LogErrorId(hr, pContainer ? MSG_APPLY_RETRYING_ACQUIRE_CONTAINER : MSG_APPLY_RETRYING_ACQUIRE_PAYLOAD, pContainer ? pContainer->sczId : pPayloadGroupItem->pPayload->sczKey, NULL, NULL);
            hr = S_OK;
        }

        ExitOnFailure(hr, "Failed to acquire %hs: %ls", pContainer ? "container" : "payload", pContainer ? pContainer->sczId : pPayloadGroupItem->pPayload->sczKey);
    } while (fRetry);

LExit:
    return hr;
}

static HRESULT AcquireContainerOrPayload(
    __in BURN_CACHE_PROGRESS_CONTEXT* pProgress,
    __out BOOL* pfRetry
    )
{
    BURN_CACHE_CONTEXT* pContext = pProgress->pCacheContext;
    BURN_CONTAINER* pContainer = pProgress->pContainer;
    BURN_PACKAGE* pPackage = pProgress->pPackage;
    BURN_PAYLOAD* pPayload = pProgress->pPayloadGroupItem ? pProgress->pPayloadGroupItem->pPayload : NULL;
    AssertSz(pContainer || pPayload, "Must provide a container or a payload.");

    HRESULT hr = S_OK;
    int nEquivalentPaths = 0;
    LPCWSTR wzPackageOrContainerId = pContainer ? pContainer->sczId : pPackage ? pPackage->sczId : NULL;
    LPCWSTR wzPayloadId = pPayload ? pPayload->sczKey : NULL;
    LPCWSTR wzPayloadContainerId = pPayload && pPayload->pContainer ? pPayload->pContainer->sczId : NULL;
    LPCWSTR wzDestinationPath = pContainer ? pContainer->sczUnverifiedPath: pPayload->sczUnverifiedPath;
    LPCWSTR wzRelativePath = pContainer ? pContainer->sczFilePath : pPayload->sczFilePath;
    DWORD dwChosenSearchPath = 0;
    DWORD dwDestinationSearchPath = 0;
    BOOTSTRAPPER_CACHE_OPERATION cacheOperation = BOOTSTRAPPER_CACHE_OPERATION_NONE;
    BOOTSTRAPPER_CACHE_RESOLVE_OPERATION resolveOperation = BOOTSTRAPPER_CACHE_RESOLVE_NONE;
    LPWSTR* pwzDownloadUrl = pContainer ? &pContainer->downloadSource.sczUrl : &pPayload->downloadSource.sczUrl;
    LPWSTR* pwzSourcePath = pContainer ? &pContainer->sczSourcePath : &pPayload->sczSourcePath;
    BOOL fFoundLocal = FALSE;
    BOOL fPreferExtract = FALSE;

    pContext->cSearchPaths = 0;
    *pfRetry = FALSE;
    pProgress->fCancel = FALSE;

    hr = UserExperienceOnCacheAcquireBegin(pContext->pUX, wzPackageOrContainerId, wzPayloadId, pwzSourcePath, pwzDownloadUrl, wzPayloadContainerId, &cacheOperation);
    ExitOnRootFailure(hr, "BA aborted cache acquire begin.");

    // Skip the Resolving event and probing local paths if the BA already knew it wanted to download or extract.
    if (BOOTSTRAPPER_CACHE_OPERATION_DOWNLOAD != cacheOperation &&
        BOOTSTRAPPER_CACHE_OPERATION_EXTRACT != cacheOperation)
    {
        do
        {
            fFoundLocal = FALSE;
            fPreferExtract = FALSE;
            resolveOperation = BOOTSTRAPPER_CACHE_RESOLVE_NONE;
            dwChosenSearchPath = 0;
            dwDestinationSearchPath = 0;

            hr = CacheGetLocalSourcePaths(wzRelativePath, *pwzSourcePath, wzDestinationPath, pContext->wzLayoutDirectory, pContext->pVariables, &pContext->rgSearchPaths, &pContext->cSearchPaths, &dwChosenSearchPath, &dwDestinationSearchPath);
            ExitOnFailure(hr, "Failed to search local source.");

            // When a payload comes from a container, the container has the highest chance of being correct.
            // But we want to avoid extracting the container multiple times.
            // So only consider the destination path, which means the container was already extracted.
            if (wzPayloadContainerId)
            {
                if (FileExistsEx(pContext->rgSearchPaths[dwDestinationSearchPath], NULL))
                {
                    fFoundLocal = TRUE;
                    dwChosenSearchPath = dwDestinationSearchPath;
                }
                else
                {
                    fPreferExtract = TRUE;
                }
            }

            if (!fFoundLocal)
            {
                for (DWORD i = 0; i < pContext->cSearchPaths; ++i)
                {
                    // If the file exists locally, choose it.
                    if (FileExistsEx(pContext->rgSearchPaths[i], NULL))
                    {
                        dwChosenSearchPath = i;

                        fFoundLocal = TRUE;
                        break;
                    }
                }
            }

            if (BOOTSTRAPPER_CACHE_OPERATION_COPY == cacheOperation)
            {
                if (fFoundLocal)
                {
                    resolveOperation = BOOTSTRAPPER_CACHE_RESOLVE_LOCAL;
                }
            }
            else
            {
                if (fPreferExtract) // the file comes from a container which hasn't been extracted yet, so extract it.
                {
                    resolveOperation = BOOTSTRAPPER_CACHE_RESOLVE_CONTAINER;
                }
                else if (fFoundLocal) // the file exists locally, so copy it.
                {
                    resolveOperation = BOOTSTRAPPER_CACHE_RESOLVE_LOCAL;
                }
                else if (wzPayloadContainerId)
                {
                    resolveOperation = BOOTSTRAPPER_CACHE_RESOLVE_CONTAINER;
                }
                else if (*pwzDownloadUrl && **pwzDownloadUrl)
                {
                    resolveOperation = BOOTSTRAPPER_CACHE_RESOLVE_DOWNLOAD;
                }
            }

            // Let the BA have a chance to override the source.
            hr = UserExperienceOnCacheAcquireResolving(pContext->pUX, wzPackageOrContainerId, wzPayloadId, pContext->rgSearchPaths, pContext->cSearchPaths, fFoundLocal, &dwChosenSearchPath, pwzDownloadUrl, wzPayloadContainerId, &resolveOperation);
            ExitOnRootFailure(hr, "BA aborted cache acquire resolving.");

            switch (resolveOperation)
            {
            case BOOTSTRAPPER_CACHE_RESOLVE_LOCAL:
                cacheOperation = BOOTSTRAPPER_CACHE_OPERATION_COPY;
                break;
            case BOOTSTRAPPER_CACHE_RESOLVE_DOWNLOAD:
                cacheOperation = BOOTSTRAPPER_CACHE_OPERATION_DOWNLOAD;
                break;
            case BOOTSTRAPPER_CACHE_RESOLVE_CONTAINER:
                cacheOperation = BOOTSTRAPPER_CACHE_OPERATION_EXTRACT;
                break;
            case BOOTSTRAPPER_CACHE_RESOLVE_RETRY:
                pContext->cSearchPathsMax = max(pContext->cSearchPaths, pContext->cSearchPathsMax);
                break;
            }
        } while (BOOTSTRAPPER_CACHE_RESOLVE_RETRY == resolveOperation);
    }

    switch (cacheOperation)
    {
    case BOOTSTRAPPER_CACHE_OPERATION_COPY:
        // If the source path and destination path are different, do the copy (otherwise there's no point).
        hr = PathCompare(pContext->rgSearchPaths[dwChosenSearchPath], wzDestinationPath, &nEquivalentPaths);
        ExitOnFailure(hr, "Failed to determine if payload paths were equivalent, source: %ls, destination: %ls.", pContext->rgSearchPaths[dwChosenSearchPath], wzDestinationPath);

        if (CSTR_EQUAL != nEquivalentPaths)
        {
            hr = CopyPayload(pProgress, INVALID_HANDLE_VALUE, pContext->rgSearchPaths[dwChosenSearchPath], wzDestinationPath);
            ExitOnFailure(hr, "Failed to copy payload: %ls", wzPayloadId);

            // Store the source path so it can be used as the LastUsedFolder if it passes verification.
            pContext->sczLastUsedFolderCandidate = pContext->rgSearchPaths[dwChosenSearchPath];
            pContext->rgSearchPaths[dwChosenSearchPath] = NULL;
        }

        break;
    case BOOTSTRAPPER_CACHE_OPERATION_DOWNLOAD:
        hr = DownloadPayload(pProgress, wzDestinationPath);
        ExitOnFailure(hr, "Failed to download payload: %ls", wzPayloadId);

        break;
    case BOOTSTRAPPER_CACHE_OPERATION_EXTRACT:
        Assert(pPayload->pContainer);

        hr = ApplyExtractContainer(pContext, pPayload->pContainer);
        ExitOnFailure(hr, "Failed to extract container for payload: %ls", wzPayloadId);

        break;
    default:
        hr = E_FILENOTFOUND;
        LogExitOnFailure(hr, MSG_RESOLVE_SOURCE_FAILED, "Failed to resolve source, payload: %ls, package: %ls, container: %ls", wzPayloadId, pPackage ? pPackage->sczId : NULL, pContainer ? pContainer->sczId : NULL);
    }

    // Send 100% complete here. This is sometimes the only progress sent to the BA.
    hr = CompleteCacheProgress(pProgress, pContainer ? pContainer->qwFileSize : pPayload->qwFileSize);

LExit:
    UserExperienceOnCacheAcquireComplete(pContext->pUX, wzPackageOrContainerId, wzPayloadId, hr, pfRetry);

    pContext->cSearchPathsMax = max(pContext->cSearchPaths, pContext->cSearchPathsMax);

    return hr;
}

static HRESULT LayoutOrCacheContainerOrPayload(
    __in BURN_CACHE_CONTEXT* pContext,
    __in_opt BURN_CONTAINER* pContainer,
    __in_opt BURN_PACKAGE* pPackage,
    __in_opt BURN_PAYLOAD_GROUP_ITEM* pPayloadGroupItem,
    __in DWORD cTryAgainAttempts,
    __out BOOL* pfRetry
    )
{
    HRESULT hr = S_OK;
    BURN_PAYLOAD* pPayload = pPayloadGroupItem ? pPayloadGroupItem->pPayload : NULL;
    LPCWSTR wzPackageOrContainerId = pContainer ? pContainer->sczId : pPackage ? pPackage->sczId : L"";
    LPCWSTR wzUnverifiedPath = pContainer ? pContainer->sczUnverifiedPath : pPayload->sczUnverifiedPath;
    LPCWSTR wzPayloadId = pPayload ? pPayload->sczKey : L"";
    BOOL fCanAffectRegistration = FALSE;
    BURN_CACHE_PROGRESS_CONTEXT progress = { };
    BOOL fMove = !pPayload || 1 == pPayload->cRemainingInstances;
    BOOL fCanceledBegin = FALSE;

    if (pContainer)
    {
        Assert(!pPayloadGroupItem);
    }
    else
    {
        Assert(pPayload);
        AssertSz(0 < pPayload->cRemainingInstances, "Laying out payload more times than planned.");
        AssertSz(!pPayloadGroupItem->fCached, "Laying out payload group item that was already cached.");
    }

    if (!pContext->wzLayoutDirectory)
    {
        Assert(!pContainer);
        Assert(pPackage);

        fCanAffectRegistration = pPackage->fCanAffectRegistration;
    }

    *pfRetry = FALSE;
    progress.pCacheContext = pContext;
    progress.pContainer = pContainer;
    progress.pPackage = pPackage;
    progress.pPayloadGroupItem = pPayloadGroupItem;

    do
    {
        fCanceledBegin = FALSE;

        hr = UserExperienceOnCacheVerifyBegin(pContext->pUX, wzPackageOrContainerId, wzPayloadId);

        if (FAILED(hr))
        {
            fCanceledBegin = TRUE;
        }
        else
        {
            if (pContext->wzLayoutDirectory) // layout the container or payload.
            {
                if (pContainer)
                {
                    hr = CacheLayoutContainer(pContainer, pContext->wzLayoutDirectory, wzUnverifiedPath, fMove, CacheMessageHandler, CacheProgressRoutine, &progress);
                }
                else
                {
                    hr = CacheLayoutPayload(pPayload, pContext->wzLayoutDirectory, wzUnverifiedPath, fMove, CacheMessageHandler, CacheProgressRoutine, &progress);
                }
            }
            else if (INVALID_HANDLE_VALUE != pContext->hPipe) // pass the decision off to the elevated process.
            {
                hr = ElevationCacheCompletePayload(pContext->hPipe, pPackage, pPayload, wzUnverifiedPath, fMove, CacheMessageHandler, CacheProgressRoutine, &progress);
            }
            else // complete the payload.
            {
                hr = CacheCompletePayload(pPackage->fPerMachine, pPayload, pPackage->sczCacheId, wzUnverifiedPath, fMove, CacheMessageHandler, CacheProgressRoutine, &progress);
            }
        }

        if (SUCCEEDED(hr) && fCanAffectRegistration)
        {
            pPackage->cacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
        }

        BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION action = FAILED(hr) && !fCanceledBegin && cTryAgainAttempts < BURN_CACHE_MAX_RECOMMENDED_VERIFY_TRYAGAIN_ATTEMPTS ? BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION_RETRYACQUISITION : BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION_NONE;
        UserExperienceOnCacheVerifyComplete(pContext->pUX, wzPackageOrContainerId, wzPayloadId, hr, &action);
        if (BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION_RETRYVERIFICATION == action)
        {
            hr = S_FALSE; // retry verify.
        }
        else if (BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION_RETRYACQUISITION == action)
        {
            *pfRetry = TRUE; // go back and retry acquire.
        }
        else if (fCanceledBegin)
        {
            ExitOnRootFailure(hr, "BA aborted cache verify begin.");
        }
    } while (S_FALSE == hr);

    if (SUCCEEDED(hr) && pPayloadGroupItem)
    {
        pPayload->cRemainingInstances -= 1;
        pPayloadGroupItem->fCached = TRUE;
    }

LExit:
    return hr;
}

static HRESULT PreparePayloadDestinationPath(
    __in_z LPCWSTR wzDestinationPath
    )
{
    HRESULT hr = S_OK;
    DWORD dwFileAttributes = 0;

    // If the destination file already exists, clear the readonly bit to avoid E_ACCESSDENIED.
    if (FileExistsEx(wzDestinationPath, &dwFileAttributes))
    {
        if (FILE_ATTRIBUTE_READONLY & dwFileAttributes)
        {
            dwFileAttributes &= ~FILE_ATTRIBUTE_READONLY;
            if (!::SetFileAttributes(wzDestinationPath, dwFileAttributes))
            {
                ExitWithLastError(hr, "Failed to clear readonly bit on payload destination path: %ls", wzDestinationPath);
            }
        }
    }

LExit:
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        hr = S_OK;
    }

    return hr;
}

static HRESULT CopyPayload(
    __in BURN_CACHE_PROGRESS_CONTEXT* pProgress,
    __in HANDLE hSourceFile,
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzDestinationPath
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzPackageOrContainerId = pProgress->pContainer ? pProgress->pContainer->sczId : pProgress->pPackage ? pProgress->pPackage->sczId : L"";
    LPCWSTR wzPayloadId = pProgress->pPayloadGroupItem ? pProgress->pPayloadGroupItem->pPayload->sczKey : L"";
    HANDLE hDestinationFile = INVALID_HANDLE_VALUE;
    HANDLE hSourceOpenedFile = INVALID_HANDLE_VALUE;

    DWORD dwLogId = pProgress->pContainer ? MSG_ACQUIRE_CONTAINER : pProgress->pPackage ? MSG_ACQUIRE_PACKAGE_PAYLOAD : MSG_ACQUIRE_BUNDLE_PAYLOAD;
    LogId(REPORT_STANDARD, dwLogId, wzPackageOrContainerId, wzPayloadId, "copy", wzSourcePath);

    hr = PreparePayloadDestinationPath(wzDestinationPath);
    ExitOnFailure(hr, "Failed to prepare payload destination path: %ls", wzDestinationPath);

    if (INVALID_HANDLE_VALUE == hSourceFile)
    {
        hSourceOpenedFile = ::CreateFileW(wzSourcePath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
        if (INVALID_HANDLE_VALUE == hSourceOpenedFile)
        {
            ExitWithLastError(hr, "Failed to open source file to copy payload from: '%ls' to: %ls.", wzSourcePath, wzDestinationPath);
        }

        hSourceFile = hSourceOpenedFile;
    }
    else
    {
        hr = FileSetPointer(hSourceFile, 0, NULL, FILE_BEGIN);
        ExitOnRootFailure(hr, "Failed to read from start of source file to copy payload from: '%ls' to: %ls.", wzSourcePath, wzDestinationPath);
    }

    hDestinationFile = ::CreateFileW(wzDestinationPath, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hDestinationFile)
    {
        ExitWithLastError(hr, "Failed to open destination file to copy payload from: '%ls' to: %ls.", wzSourcePath, wzDestinationPath);
    }

    hr = FileCopyUsingHandlesWithProgress(hSourceFile, hDestinationFile, 0, CacheProgressRoutine, pProgress);
    if (FAILED(hr))
    {
        if (pProgress->fCancel)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
            ExitOnRootFailure(hr, "BA aborted copy of payload from: '%ls' to: %ls.", wzSourcePath, wzDestinationPath);
        }
        else
        {
            ExitOnRootFailure(hr, "Failed attempt to copy payload from: '%ls' to: %ls.", wzSourcePath, wzDestinationPath);
        }
    }

LExit:
    ReleaseFileHandle(hDestinationFile);
    ReleaseFileHandle(hSourceOpenedFile);

    return hr;
}

static HRESULT DownloadPayload(
    __in BURN_CACHE_PROGRESS_CONTEXT* pProgress,
    __in_z LPCWSTR wzDestinationPath
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzPackageOrContainerId = pProgress->pContainer ? pProgress->pContainer->sczId : pProgress->pPackage ? pProgress->pPackage->sczId : L"";
    LPCWSTR wzPayloadId = pProgress->pPayloadGroupItem ? pProgress->pPayloadGroupItem->pPayload->sczKey : L"";
    DOWNLOAD_SOURCE* pDownloadSource = pProgress->pContainer ? &pProgress->pContainer->downloadSource : &pProgress->pPayloadGroupItem->pPayload->downloadSource;
    DWORD64 qwDownloadSize = pProgress->pContainer ? pProgress->pContainer->qwFileSize : pProgress->pPayloadGroupItem->pPayload->qwFileSize;
    DOWNLOAD_CACHE_CALLBACK cacheCallback = { };
    DOWNLOAD_AUTHENTICATION_CALLBACK authenticationCallback = { };
    APPLY_AUTHENTICATION_REQUIRED_DATA authenticationData = { };

    DWORD dwLogId = pProgress->pContainer ? MSG_ACQUIRE_CONTAINER : pProgress->pPackage ? MSG_ACQUIRE_PACKAGE_PAYLOAD : MSG_ACQUIRE_BUNDLE_PAYLOAD;
    LogId(REPORT_STANDARD, dwLogId, wzPackageOrContainerId, wzPayloadId, "download", pDownloadSource->sczUrl);

    hr = PreparePayloadDestinationPath(wzDestinationPath);
    ExitOnFailure(hr, "Failed to prepare payload destination path: %ls", wzDestinationPath);

    cacheCallback.pfnProgress = CacheProgressRoutine;
    cacheCallback.pfnCancel = NULL; // TODO: set this
    cacheCallback.pv = pProgress;
   
    authenticationData.pUX = pProgress->pCacheContext->pUX;
    authenticationData.wzPackageOrContainerId = wzPackageOrContainerId;
    authenticationData.wzPayloadId = wzPayloadId;
    authenticationCallback.pv =  static_cast<LPVOID>(&authenticationData);
    authenticationCallback.pfnAuthenticate = &AuthenticationRequired;
        
    hr = DownloadUrl(pDownloadSource, qwDownloadSize, wzDestinationPath, &cacheCallback, &authenticationCallback);
    ExitOnFailure(hr, "Failed attempt to download URL: '%ls' to: '%ls'", pDownloadSource->sczUrl, wzDestinationPath);

LExit:
    return hr;
}

static HRESULT WINAPI AuthenticationRequired(
    __in LPVOID pData,
    __in HINTERNET hUrl,
    __in long lHttpCode,
    __out BOOL* pfRetrySend,
    __out BOOL* pfRetry
    )
{
    Assert(401 == lHttpCode || 407 == lHttpCode);

    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    BOOTSTRAPPER_ERROR_TYPE errorType = (401 == lHttpCode) ? BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_SERVER : BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_PROXY;
    LPWSTR sczError = NULL;
    int nResult = IDNOACTION;

    *pfRetrySend = FALSE;
    *pfRetry = FALSE;

    hr = StrAllocFromError(&sczError, HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED), NULL);
    ExitOnFailure(hr, "Failed to allocation error string.");

    APPLY_AUTHENTICATION_REQUIRED_DATA* authenticationData = reinterpret_cast<APPLY_AUTHENTICATION_REQUIRED_DATA*>(pData);

    UserExperienceOnError(authenticationData->pUX, errorType, authenticationData->wzPackageOrContainerId, ERROR_ACCESS_DENIED, sczError, MB_RETRYTRYAGAIN, 0, NULL, &nResult); // ignore return value;
    nResult = UserExperienceCheckExecuteResult(authenticationData->pUX, FALSE, MB_RETRYTRYAGAIN, nResult);
    if (IDTRYAGAIN == nResult && authenticationData->pUX->hwndApply)
    {
        er = ::InternetErrorDlg(authenticationData->pUX->hwndApply, hUrl, ERROR_INTERNET_INCORRECT_PASSWORD, FLAGS_ERROR_UI_FILTER_FOR_ERRORS | FLAGS_ERROR_UI_FLAGS_CHANGE_OPTIONS | FLAGS_ERROR_UI_FLAGS_GENERATE_DATA, NULL);
        if (ERROR_SUCCESS == er || ERROR_CANCELLED == er)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
        }
        else if (ERROR_INTERNET_FORCE_RETRY == er)
        {
            *pfRetrySend = TRUE;
            hr = S_OK;
        }
        else
        {
            hr = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
        }
    }
    else if (IDRETRY == nResult)
    {
        *pfRetry = TRUE;
        hr = S_OK;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED);
    }

LExit:
    ReleaseStr(sczError);

    return hr;
}

static HRESULT CALLBACK CacheMessageHandler(
    __in BURN_CACHE_MESSAGE* pMessage,
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_PROGRESS_CONTEXT* pProgress = static_cast<BURN_CACHE_PROGRESS_CONTEXT*>(pvContext);
    LPCWSTR wzPackageOrContainerId = pProgress->pContainer ? pProgress->pContainer->sczId : pProgress->pPackage ? pProgress->pPackage->sczId : NULL;
    LPCWSTR wzPayloadId = pProgress->pPayloadGroupItem ? pProgress->pPayloadGroupItem->pPayload->sczKey : pProgress->pPayload ? pProgress->pPayload->sczKey : NULL;

    switch (pMessage->type)
    {
    case BURN_CACHE_MESSAGE_BEGIN:
        switch (pMessage->begin.cacheStep)
        {
        case BURN_CACHE_STEP_HASH_TO_SKIP_ACQUIRE:
            pProgress->type = BURN_CACHE_PROGRESS_TYPE_CONTAINER_OR_PAYLOAD_VERIFY;
            hr = UserExperienceOnCacheContainerOrPayloadVerifyBegin(pProgress->pCacheContext->pUX, wzPackageOrContainerId, wzPayloadId);
            break;
        case BURN_CACHE_STEP_HASH_TO_SKIP_VERIFY:
            pProgress->type = BURN_CACHE_PROGRESS_TYPE_PAYLOAD_VERIFY;
            break;
        case BURN_CACHE_STEP_STAGE:
            pProgress->type = BURN_CACHE_PROGRESS_TYPE_STAGE;
            break;
        case BURN_CACHE_STEP_HASH:
            pProgress->type = BURN_CACHE_PROGRESS_TYPE_HASH;
            break;
        case BURN_CACHE_STEP_FINALIZE:
            pProgress->type = BURN_CACHE_PROGRESS_TYPE_FINALIZE;
            break;
        }
        break;
    case BURN_CACHE_MESSAGE_SUCCESS:
        hr = CompleteCacheProgress(pProgress, pMessage->success.qwFileSize);
        break;
    case BURN_CACHE_MESSAGE_COMPLETE:
        switch (pProgress->type)
        {
        case BURN_CACHE_PROGRESS_TYPE_CONTAINER_OR_PAYLOAD_VERIFY:
            hr = UserExperienceOnCacheContainerOrPayloadVerifyComplete(pProgress->pCacheContext->pUX, wzPackageOrContainerId, wzPayloadId, hr);
            break;
        }
    }

    return hr;
}

static HRESULT CompleteCacheProgress(
    __in BURN_CACHE_PROGRESS_CONTEXT* pContext,
    __in DWORD64 qwFileSize
    )
{
    HRESULT hr = S_OK;
    LARGE_INTEGER liContainerOrPayloadSize = { };
    LARGE_INTEGER liZero = { };
    DWORD dwResult = 0;
    DWORD64 qwCommitSize = 0;

    liContainerOrPayloadSize.QuadPart = qwFileSize;

    // Need to commit the steps that were skipped.
    if (BURN_CACHE_PROGRESS_TYPE_CONTAINER_OR_PAYLOAD_VERIFY == pContext->type || BURN_CACHE_PROGRESS_TYPE_PAYLOAD_VERIFY == pContext->type)
    {
        Assert(!pContext->pPayload);

        qwCommitSize = qwFileSize * (pContext->pCacheContext->wzLayoutDirectory ? 2 : 3); // Acquire (+ Stage) + Hash + Finalize - 1 (that's added later)

        pContext->pCacheContext->qwSuccessfulCacheProgress += qwCommitSize;

        if (pContext->pContainer)
        {
            pContext->pContainer->qwCommittedCacheProgress += qwCommitSize;
        }
        else if (pContext->pPayloadGroupItem)
        {
            pContext->pPayloadGroupItem->qwCommittedCacheProgress += qwCommitSize;
        }
    }

    dwResult = CacheProgressRoutine(liContainerOrPayloadSize, liContainerOrPayloadSize, liZero, liZero, 0, 0, INVALID_HANDLE_VALUE, INVALID_HANDLE_VALUE, pContext);

    if (PROGRESS_CONTINUE == dwResult)
    {
        pContext->pCacheContext->qwSuccessfulCacheProgress += qwFileSize;

        if (pContext->pPayload)
        {
            pContext->pContainer->qwCommittedExtractProgress += qwFileSize;
        }
        else if (pContext->pContainer)
        {
            pContext->pContainer->qwCommittedCacheProgress += qwFileSize;
        }
        else if (pContext->pPayloadGroupItem)
        {
            pContext->pPayloadGroupItem->qwCommittedCacheProgress += qwFileSize;
        }

        if (BURN_CACHE_PROGRESS_TYPE_FINALIZE == pContext->type && pContext->pCacheContext->sczLastUsedFolderCandidate)
        {
            // We successfully copied from a source location, set that as the last used source.
            CacheSetLastUsedSource(pContext->pCacheContext->pVariables, pContext->pCacheContext->sczLastUsedFolderCandidate, pContext->pContainer ? pContext->pContainer->sczFilePath : pContext->pPayloadGroupItem->pPayload->sczFilePath);
        }
    }
    else if (PROGRESS_CANCEL == dwResult)
    {
        if (pContext->fCancel)
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
        }
        else
        {
            hr = pContext->hrError;
        }

        if (qwCommitSize)
        {
            pContext->pCacheContext->qwSuccessfulCacheProgress -= qwCommitSize;

            if (pContext->pContainer)
            {
                pContext->pContainer->qwCommittedCacheProgress -= qwCommitSize;
            }
            else if (pContext->pPayloadGroupItem)
            {
                pContext->pPayloadGroupItem->qwCommittedCacheProgress -= qwCommitSize;
            }
        }
    }

    return hr;
}

static DWORD CALLBACK CacheProgressRoutine(
    __in LARGE_INTEGER TotalFileSize,
    __in LARGE_INTEGER TotalBytesTransferred,
    __in LARGE_INTEGER /*StreamSize*/,
    __in LARGE_INTEGER /*StreamBytesTransferred*/,
    __in DWORD /*dwStreamNumber*/,
    __in DWORD /*dwCallbackReason*/,
    __in HANDLE /*hSourceFile*/,
    __in HANDLE /*hDestinationFile*/,
    __in_opt LPVOID lpData
    )
{
    HRESULT hr = S_OK;
    DWORD dwResult = PROGRESS_CONTINUE;
    BURN_CACHE_PROGRESS_CONTEXT* pProgress = static_cast<BURN_CACHE_PROGRESS_CONTEXT*>(lpData);
    LPCWSTR wzPackageOrContainerId = pProgress->pContainer ? pProgress->pContainer->sczId : pProgress->pPackage ? pProgress->pPackage->sczId : NULL;
    LPCWSTR wzPayloadId = pProgress->pPayloadGroupItem ? pProgress->pPayloadGroupItem->pPayload->sczKey : pProgress->pPayload ? pProgress->pPayload->sczKey : NULL;
    DWORD64 qwCacheProgress = pProgress->pCacheContext->qwSuccessfulCacheProgress + TotalBytesTransferred.QuadPart;
    if (qwCacheProgress > pProgress->pCacheContext->qwTotalCacheSize)
    {
        //AssertSz(FALSE, "Apply has cached more than Plan envisioned.");
        qwCacheProgress = pProgress->pCacheContext->qwTotalCacheSize;
    }
    DWORD dwOverallPercentage = pProgress->pCacheContext->qwTotalCacheSize ? static_cast<DWORD>(qwCacheProgress * 100 / pProgress->pCacheContext->qwTotalCacheSize) : 0;

    switch (pProgress->type)
    {
    case BURN_CACHE_PROGRESS_TYPE_ACQUIRE:
        hr = UserExperienceOnCacheAcquireProgress(pProgress->pCacheContext->pUX, wzPackageOrContainerId, wzPayloadId, TotalBytesTransferred.QuadPart, TotalFileSize.QuadPart, dwOverallPercentage);
        ExitOnRootFailure(hr, "BA aborted acquire of %hs: %ls", pProgress->pContainer ? "container" : "payload", pProgress->pContainer ? wzPackageOrContainerId : wzPayloadId);
        break;
    case BURN_CACHE_PROGRESS_TYPE_PAYLOAD_VERIFY:
        hr = UserExperienceOnCacheVerifyProgress(pProgress->pCacheContext->pUX, wzPackageOrContainerId, wzPayloadId, TotalBytesTransferred.QuadPart, TotalFileSize.QuadPart, dwOverallPercentage, BOOTSTRAPPER_CACHE_VERIFY_STEP_HASH);
        ExitOnRootFailure(hr, "BA aborted payload verify step during verify of %hs: %ls", pProgress->pContainer ? "container" : "payload", pProgress->pContainer ? wzPackageOrContainerId : wzPayloadId);
        break;
    case BURN_CACHE_PROGRESS_TYPE_STAGE:
        hr = UserExperienceOnCacheVerifyProgress(pProgress->pCacheContext->pUX, wzPackageOrContainerId, wzPayloadId, TotalBytesTransferred.QuadPart, TotalFileSize.QuadPart, dwOverallPercentage, BOOTSTRAPPER_CACHE_VERIFY_STEP_STAGE);
        ExitOnRootFailure(hr, "BA aborted stage step during verify of %hs: %ls", pProgress->pContainer ? "container" : "payload", pProgress->pContainer ? wzPackageOrContainerId : wzPayloadId);
        break;
    case BURN_CACHE_PROGRESS_TYPE_HASH:
        hr = UserExperienceOnCacheVerifyProgress(pProgress->pCacheContext->pUX, wzPackageOrContainerId, wzPayloadId, TotalBytesTransferred.QuadPart, TotalFileSize.QuadPart, dwOverallPercentage, BOOTSTRAPPER_CACHE_VERIFY_STEP_HASH);
        ExitOnRootFailure(hr, "BA aborted hash step during verify of %hs: %ls", pProgress->pContainer ? "container" : "payload", pProgress->pContainer ? wzPackageOrContainerId : wzPayloadId);
        break;
    case BURN_CACHE_PROGRESS_TYPE_FINALIZE:
        hr = UserExperienceOnCacheVerifyProgress(pProgress->pCacheContext->pUX, wzPackageOrContainerId, wzPayloadId, TotalBytesTransferred.QuadPart, TotalFileSize.QuadPart, dwOverallPercentage, BOOTSTRAPPER_CACHE_VERIFY_STEP_FINALIZE);
        ExitOnRootFailure(hr, "BA aborted finalize step during verify of %hs: %ls", pProgress->pContainer ? "container" : "payload", pProgress->pContainer ? wzPackageOrContainerId : wzPayloadId);
        break;
    case BURN_CACHE_PROGRESS_TYPE_CONTAINER_OR_PAYLOAD_VERIFY:
        hr = UserExperienceOnCacheContainerOrPayloadVerifyProgress(pProgress->pCacheContext->pUX, wzPackageOrContainerId, wzPayloadId, TotalBytesTransferred.QuadPart, TotalFileSize.QuadPart, dwOverallPercentage);
        ExitOnRootFailure(hr, "BA aborted container or payload verify: %ls", wzPayloadId);
        break;
    case BURN_CACHE_PROGRESS_TYPE_EXTRACT:
        hr = UserExperienceOnCachePayloadExtractProgress(pProgress->pCacheContext->pUX, wzPackageOrContainerId, wzPayloadId, TotalBytesTransferred.QuadPart, TotalFileSize.QuadPart, dwOverallPercentage);
        ExitOnRootFailure(hr, "BA aborted extract container: %ls, payload: %ls", wzPackageOrContainerId, wzPayloadId);
        break;
    }

LExit:
    if (HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) == hr)
    {
        dwResult = PROGRESS_CANCEL;
        pProgress->fCancel = TRUE;
    }
    else if (FAILED(hr))
    {
        dwResult = PROGRESS_CANCEL;
        pProgress->hrError = hr;
    }
    else
    {
        dwResult = PROGRESS_CONTINUE;
    }

    return dwResult;
}

static void DoRollbackCache(
    __in BURN_USER_EXPERIENCE* /*pUX*/,
    __in BURN_PLAN* pPlan,
    __in HANDLE hPipe,
    __in DWORD dwCheckpoint
    )
{
    HRESULT hr = S_OK;
    DWORD iCheckpoint = 0;

    // Scan to last checkpoint.
    for (DWORD i = 0; i < pPlan->cRollbackCacheActions; ++i)
    {
        BURN_CACHE_ACTION* pRollbackCacheAction = &pPlan->rgRollbackCacheActions[i];

        if (BURN_CACHE_ACTION_TYPE_CHECKPOINT == pRollbackCacheAction->type && pRollbackCacheAction->checkpoint.dwId == dwCheckpoint)
        {
            iCheckpoint = i;
            break;
        }
    }

    // Rollback cache actions.
    if (iCheckpoint)
    {
        // i has to be a signed integer so it doesn't get decremented to 0xFFFFFFFF.
        for (int i = iCheckpoint - 1; i >= 0; --i)
        {
            BURN_CACHE_ACTION* pRollbackCacheAction = &pPlan->rgRollbackCacheActions[i];

            switch (pRollbackCacheAction->type)
            {
            case BURN_CACHE_ACTION_TYPE_CHECKPOINT:
                break;

            case BURN_CACHE_ACTION_TYPE_ROLLBACK_PACKAGE:
                hr = CleanPackage(hPipe, pRollbackCacheAction->rollbackPackage.pPackage);
                break;

            default:
                AssertSz(FALSE, "Invalid rollback cache action.");
                break;
            }
        }
    }
}

static HRESULT DoExecuteAction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in_opt HANDLE hCacheThread,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary,
    __inout BURN_EXECUTE_ACTION_CHECKPOINT** ppCheckpoint,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    Assert(!pExecuteAction->fDeleted);

    HRESULT hr = S_OK;
    HANDLE rghWait[2] = { };
    BOOTSTRAPPER_APPLY_RESTART restart = BOOTSTRAPPER_APPLY_RESTART_NONE;
    BOOL fRetry = FALSE;
    BOOL fStopWusaService = FALSE;
    BOOL fInsideMsiTransaction = FALSE;

    pContext->fRollback = FALSE;

    do
    {
        fInsideMsiTransaction = *ppRollbackBoundary && (*ppRollbackBoundary)->fActiveTransaction;

        switch (pExecuteAction->type)
        {
        case BURN_EXECUTE_ACTION_TYPE_CHECKPOINT:
            *ppCheckpoint = &pExecuteAction->checkpoint;
            break;

        case BURN_EXECUTE_ACTION_TYPE_WAIT_SYNCPOINT:
            // wait for cache sync-point
            rghWait[0] = pExecuteAction->syncpoint.hEvent;
            rghWait[1] = hCacheThread;
            switch (::WaitForMultipleObjects(rghWait[1] ? 2 : 1, rghWait, FALSE, INFINITE))
            {
            case WAIT_OBJECT_0:
                break;

            case WAIT_OBJECT_0 + 1:
                if (!::GetExitCodeThread(hCacheThread, (DWORD*)&hr))
                {
                    ExitWithLastError(hr, "Failed to get cache thread exit code.");
                }

                if (SUCCEEDED(hr))
                {
                    hr = E_UNEXPECTED;
                }
                ExitOnFailure(hr, "Cache thread exited unexpectedly.");

            case WAIT_FAILED: __fallthrough;
            default:
                ExitWithLastError(hr, "Failed to wait for cache check-point.");
            }
            break;

        case BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE:
            hr = ExecuteExePackage(pEngineState, pExecuteAction, pContext, FALSE, &fRetry, pfSuspend, &restart);
            ExitOnFailure(hr, "Failed to execute EXE package.");
            break;

        case BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE:
            hr = ExecuteMsiPackage(pEngineState, pExecuteAction, pContext, fInsideMsiTransaction, FALSE, &fRetry, pfSuspend, &restart);
            ExitOnFailure(hr, "Failed to execute MSI package.");
            break;

        case BURN_EXECUTE_ACTION_TYPE_MSP_TARGET:
            hr = ExecuteMspPackage(pEngineState, pExecuteAction, pContext, fInsideMsiTransaction, FALSE, &fRetry, pfSuspend, &restart);
            ExitOnFailure(hr, "Failed to execute MSP package.");
            break;

        case BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE:
            hr = ExecuteMsuPackage(pEngineState, pExecuteAction, pContext, FALSE, fStopWusaService, &fRetry, pfSuspend, &restart);
            fStopWusaService = fRetry;
            ExitOnFailure(hr, "Failed to execute MSU package.");
            break;

        case BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER:
            hr = ExecutePackageProviderAction(pEngineState, pExecuteAction, pContext);
            ExitOnFailure(hr, "Failed to execute package provider registration action.");
            break;

        case BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY:
            hr = ExecuteDependencyAction(pEngineState, pExecuteAction, pContext);
            ExitOnFailure(hr, "Failed to execute dependency action.");
            break;

            break;

        case BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY:
            *ppRollbackBoundary = pExecuteAction->rollbackBoundary.pRollbackBoundary;
            break;

        case BURN_EXECUTE_ACTION_TYPE_BEGIN_MSI_TRANSACTION:
            hr = ExecuteMsiBeginTransaction(pEngineState, pExecuteAction->msiTransaction.pRollbackBoundary, pContext);
            ExitOnFailure(hr, "Failed to execute begin MSI transaction action.");
            break;

        case BURN_EXECUTE_ACTION_TYPE_COMMIT_MSI_TRANSACTION:
            hr = ExecuteMsiCommitTransaction(pEngineState, pExecuteAction->msiTransaction.pRollbackBoundary, pContext);
            ExitOnFailure(hr, "Failed to execute commit MSI transaction action.");
            break;

        default:
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Invalid execute action.");
        }

        if (*pRestart < restart)
        {
            *pRestart = restart;
        }
    } while (fRetry && *pRestart < BOOTSTRAPPER_APPLY_RESTART_INITIATED);

LExit:
    return hr;
}

static HRESULT DoRollbackActions(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in DWORD dwCheckpoint,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    DWORD iCheckpoint = 0;
    BOOL fRetryIgnored = FALSE;
    BOOL fSuspendIgnored = FALSE;

    pContext->fRollback = TRUE;

    // scan to last checkpoint
    for (DWORD i = 0; i < pEngineState->plan.cRollbackActions; ++i)
    {
        BURN_EXECUTE_ACTION* pRollbackAction = &pEngineState->plan.rgRollbackActions[i];
        if (pRollbackAction->fDeleted)
        {
            continue;
        }

        if (BURN_EXECUTE_ACTION_TYPE_CHECKPOINT == pRollbackAction->type)
        {
            if (pRollbackAction->checkpoint.dwId == dwCheckpoint)
            {
                iCheckpoint = i;
                break;
            }
        }
    }

    // execute rollback actions
    if (iCheckpoint)
    {
        // i has to be a signed integer so it doesn't get decremented to 0xFFFFFFFF.
        for (int i = iCheckpoint - 1; i >= 0; --i)
        {
            BURN_EXECUTE_ACTION* pRollbackAction = &pEngineState->plan.rgRollbackActions[i];
            if (pRollbackAction->fDeleted)
            {
                continue;
            }

            BOOTSTRAPPER_APPLY_RESTART restart = BOOTSTRAPPER_APPLY_RESTART_NONE;
            switch (pRollbackAction->type)
            {
            case BURN_EXECUTE_ACTION_TYPE_CHECKPOINT:
                break;

            case BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE:
                hr = ExecuteExePackage(pEngineState, pRollbackAction, pContext, TRUE, &fRetryIgnored, &fSuspendIgnored, &restart);
                IgnoreRollbackError(hr, "Failed to rollback EXE package.");
                break;

            case BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE:
                hr = ExecuteMsiPackage(pEngineState, pRollbackAction, pContext, FALSE, TRUE, &fRetryIgnored, &fSuspendIgnored, &restart);
                IgnoreRollbackError(hr, "Failed to rollback MSI package.");
                break;

            case BURN_EXECUTE_ACTION_TYPE_MSP_TARGET:
                hr = ExecuteMspPackage(pEngineState, pRollbackAction, pContext, FALSE, TRUE, &fRetryIgnored, &fSuspendIgnored, &restart);
                IgnoreRollbackError(hr, "Failed to rollback MSP package.");
                break;

            case BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE:
                hr = ExecuteMsuPackage(pEngineState, pRollbackAction, pContext, TRUE, FALSE, &fRetryIgnored, &fSuspendIgnored, &restart);
                IgnoreRollbackError(hr, "Failed to rollback MSU package.");
                break;

            case BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER:
                hr = ExecutePackageProviderAction(pEngineState, pRollbackAction, pContext);
                IgnoreRollbackError(hr, "Failed to rollback package provider action.");
                break;

            case BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY:
                hr = ExecuteDependencyAction(pEngineState, pRollbackAction, pContext);
                IgnoreRollbackError(hr, "Failed to rollback dependency action.");
                break;

            case BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY:
                ExitFunction1(hr = S_OK);

            case BURN_EXECUTE_ACTION_TYPE_UNCACHE_PACKAGE:
                // TODO: This used to be skipped if the package was already cached.
                //       Need to figure out new logic for when (if?) to skip it.
                hr = CleanPackage(pEngineState->companionConnection.hPipe, pRollbackAction->uncachePackage.pPackage);
                IgnoreRollbackError(hr, "Failed to uncache package for rollback.");
                break;

            default:
                hr = E_UNEXPECTED;
                ExitOnFailure(hr, "Invalid rollback action: %d.", pRollbackAction->type);
            }

            if (*pRestart < restart)
            {
                *pRestart = restart;
            }
        }
    }

LExit:
    return hr;
}

static HRESULT ExecuteExePackage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in BOOL fRollback,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    HRESULT hrExecute = S_OK;
    GENERIC_EXECUTE_MESSAGE message = { };
    int nResult = 0;
    BOOL fBeginCalled = FALSE;
    BOOL fExecuted = FALSE;
    BURN_PACKAGE* pPackage = pExecuteAction->exePackage.pPackage;

    if (FAILED(pPackage->hrCacheResult))
    {
        LogId(REPORT_STANDARD, MSG_APPLY_SKIPPED_FAILED_CACHED_PACKAGE, pPackage->sczId, pPackage->hrCacheResult);
        ExitFunction1(hr = S_OK);
    }

    Assert(pContext->fRollback == fRollback);
    pContext->pExecutingPackage = pPackage;
    fBeginCalled = TRUE;

    // Send package execute begin to BA.
    hr = UserExperienceOnExecutePackageBegin(&pEngineState->userExperience, pPackage->sczId, !fRollback, pExecuteAction->exePackage.action, INSTALLUILEVEL_NOCHANGE, FALSE);
    ExitOnRootFailure(hr, "BA aborted execute EXE package begin.");

    message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
    message.dwAllowedResults = MB_OKCANCEL;
    message.progress.dwPercentage = fRollback ? 100 : 0;
    nResult = GenericExecuteMessageHandler(&message, pContext);
    hr = UserExperienceInterpretExecuteResult(&pEngineState->userExperience, fRollback, message.dwAllowedResults, nResult);
    ExitOnRootFailure(hr, "BA aborted EXE progress.");

    fExecuted = TRUE;

    // Execute package.
    if (pPackage->fPerMachine)
    {
        hrExecute = ElevationExecuteExePackage(pEngineState->companionConnection.hPipe, pExecuteAction, &pEngineState->variables, fRollback, GenericExecuteMessageHandler, pContext, pRestart);
        ExitOnFailure(hrExecute, "Failed to configure per-machine EXE package.");
    }
    else
    {
        hrExecute = ExeEngineExecutePackage(pExecuteAction, &pEngineState->variables, fRollback, GenericExecuteMessageHandler, pContext, pRestart);
        ExitOnFailure(hrExecute, "Failed to configure per-user EXE package.");
    }

    message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
    message.dwAllowedResults = MB_OKCANCEL;
    message.progress.dwPercentage = fRollback ? 0 : 100;
    nResult = GenericExecuteMessageHandler(&message, pContext);
    hr = UserExperienceInterpretExecuteResult(&pEngineState->userExperience, fRollback, message.dwAllowedResults, nResult);
    ExitOnRootFailure(hr, "BA aborted EXE progress.");

    pContext->cExecutedPackages += fRollback ? -1 : 1;
    (*pContext->pcOverallProgressTicks) += fRollback ? -1 : 1;

    hr = ReportOverallProgressTicks(&pEngineState->userExperience, fRollback, pEngineState->plan.cOverallProgressTicksTotal, *pContext->pcOverallProgressTicks);
    ExitOnRootFailure(hr, "BA aborted EXE package execute progress.");

LExit:
    if (fExecuted)
    {
        ExeEngineUpdateInstallRegistrationState(pExecuteAction, hrExecute);
    }

    if (fBeginCalled)
    {
        hr = ExecutePackageComplete(&pEngineState->userExperience, &pEngineState->variables, pPackage, hr, hrExecute, fRollback, pRestart, pfRetry, pfSuspend);
    }

    return hr;
}

static HRESULT ExecuteMsiPackage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in BOOL fInsideMsiTransaction,
    __in BOOL fRollback,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    HRESULT hrExecute = S_OK;
    BOOL fBeginCalled = FALSE;
    BOOL fExecuted = FALSE;
    BURN_PACKAGE* pPackage = pExecuteAction->msiPackage.pPackage;

    if (FAILED(pPackage->hrCacheResult))
    {
        LogId(REPORT_STANDARD, MSG_APPLY_SKIPPED_FAILED_CACHED_PACKAGE, pPackage->sczId, pPackage->hrCacheResult);
        ExitFunction1(hr = S_OK);
    }

    Assert(pContext->fRollback == fRollback);
    pContext->pExecutingPackage = pPackage;
    fBeginCalled = TRUE;

    // Send package execute begin to BA.
    hr = UserExperienceOnExecutePackageBegin(&pEngineState->userExperience, pPackage->sczId, !fRollback, pExecuteAction->msiPackage.action, pExecuteAction->msiPackage.uiLevel, pExecuteAction->msiPackage.fDisableExternalUiHandler);
    ExitOnRootFailure(hr, "BA aborted execute MSI package begin.");

    fExecuted = TRUE;

    // execute package
    if (pPackage->fPerMachine)
    {
        hrExecute = ElevationExecuteMsiPackage(pEngineState->companionConnection.hPipe, pEngineState->userExperience.hwndApply, pExecuteAction, &pEngineState->variables, fRollback, MsiExecuteMessageHandler, pContext, pRestart);
        ExitOnFailure(hrExecute, "Failed to configure per-machine MSI package.");
    }
    else
    {
        hrExecute = MsiEngineExecutePackage(pEngineState->userExperience.hwndApply, pExecuteAction, &pEngineState->variables, fRollback, MsiExecuteMessageHandler, pContext, pRestart);
        ExitOnFailure(hrExecute, "Failed to configure per-user MSI package.");
    }

    pContext->cExecutedPackages += fRollback ? -1 : 1;
    (*pContext->pcOverallProgressTicks) += fRollback ? -1 : 1;

    hr = ReportOverallProgressTicks(&pEngineState->userExperience, fRollback, pEngineState->plan.cOverallProgressTicksTotal, *pContext->pcOverallProgressTicks);
    ExitOnRootFailure(hr, "BA aborted MSI package execute progress.");

LExit:
    if (fExecuted)
    {
        MsiEngineUpdateInstallRegistrationState(pExecuteAction, fRollback, hrExecute, fInsideMsiTransaction);
    }

    if (fBeginCalled)
    {
        hr = ExecutePackageComplete(&pEngineState->userExperience, &pEngineState->variables, pPackage, hr, hrExecute, fRollback, pRestart, pfRetry, pfSuspend);
    }

    return hr;
}

static HRESULT ExecuteMspPackage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in BOOL fInsideMsiTransaction,
    __in BOOL fRollback,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    HRESULT hrExecute = S_OK;
    BOOL fBeginCalled = FALSE;
    BOOL fExecuted = FALSE;
    BURN_PACKAGE* pPackage = pExecuteAction->mspTarget.pPackage;

    if (FAILED(pPackage->hrCacheResult))
    {
        LogId(REPORT_STANDARD, MSG_APPLY_SKIPPED_FAILED_CACHED_PACKAGE, pPackage->sczId, pPackage->hrCacheResult);
        ExitFunction1(hr = S_OK);
    }

    Assert(pContext->fRollback == fRollback);
    pContext->pExecutingPackage = pPackage;
    fBeginCalled = TRUE;

    // Send package execute begin to BA.
    hr = UserExperienceOnExecutePackageBegin(&pEngineState->userExperience, pPackage->sczId, !fRollback, pExecuteAction->mspTarget.action, pExecuteAction->mspTarget.uiLevel, pExecuteAction->mspTarget.fDisableExternalUiHandler);
    ExitOnRootFailure(hr, "BA aborted execute MSP package begin.");

    // Now send all the patches that target this product code.
    for (DWORD i = 0; i < pExecuteAction->mspTarget.cOrderedPatches; ++i)
    {
        BURN_PACKAGE* pMspPackage = pExecuteAction->mspTarget.rgOrderedPatches[i].pPackage;

        hr = UserExperienceOnExecutePatchTarget(&pEngineState->userExperience, pMspPackage->sczId, pExecuteAction->mspTarget.sczTargetProductCode);
        ExitOnRootFailure(hr, "BA aborted execute MSP target.");
    }

    fExecuted = TRUE;

    // execute package
    if (pExecuteAction->mspTarget.fPerMachineTarget)
    {
        hrExecute = ElevationExecuteMspPackage(pEngineState->companionConnection.hPipe, pEngineState->userExperience.hwndApply, pExecuteAction, &pEngineState->variables, fRollback, MsiExecuteMessageHandler, pContext, pRestart);
        ExitOnFailure(hrExecute, "Failed to configure per-machine MSP package.");
    }
    else
    {
        hrExecute = MspEngineExecutePackage(pEngineState->userExperience.hwndApply, pExecuteAction, &pEngineState->variables, fRollback, MsiExecuteMessageHandler, pContext, pRestart);
        ExitOnFailure(hrExecute, "Failed to configure per-user MSP package.");
    }

    pContext->cExecutedPackages += fRollback ? -1 : 1;
    (*pContext->pcOverallProgressTicks) += fRollback ? -1 : 1;

    hr = ReportOverallProgressTicks(&pEngineState->userExperience, fRollback, pEngineState->plan.cOverallProgressTicksTotal, *pContext->pcOverallProgressTicks);
    ExitOnRootFailure(hr, "BA aborted MSP package execute progress.");

LExit:
    if (fExecuted)
    {
        MspEngineUpdateInstallRegistrationState(pExecuteAction, hrExecute, fInsideMsiTransaction);
    }

    if (fBeginCalled)
    {
        hr = ExecutePackageComplete(&pEngineState->userExperience, &pEngineState->variables, pPackage, hr, hrExecute, fRollback, pRestart, pfRetry, pfSuspend);
    }

    return hr;
}

static HRESULT ExecuteMsuPackage(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_EXECUTE_CONTEXT* pContext,
    __in BOOL fRollback,
    __in BOOL fStopWusaService,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    HRESULT hrExecute = S_OK;
    GENERIC_EXECUTE_MESSAGE message = { };
    int nResult = 0;
    BOOL fBeginCalled = FALSE;
    BOOL fExecuted = FALSE;
    BURN_PACKAGE* pPackage = pExecuteAction->msuPackage.pPackage;

    if (FAILED(pPackage->hrCacheResult))
    {
        LogId(REPORT_STANDARD, MSG_APPLY_SKIPPED_FAILED_CACHED_PACKAGE, pPackage->sczId, pPackage->hrCacheResult);
        ExitFunction1(hr = S_OK);
    }

    Assert(pContext->fRollback == fRollback);
    pContext->pExecutingPackage = pPackage;
    fBeginCalled = TRUE;

    // Send package execute begin to BA.
    hr = UserExperienceOnExecutePackageBegin(&pEngineState->userExperience, pPackage->sczId, !fRollback, pExecuteAction->msuPackage.action, INSTALLUILEVEL_NOCHANGE, FALSE);
    ExitOnRootFailure(hr, "BA aborted execute MSU package begin.");

    message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
    message.dwAllowedResults = MB_OKCANCEL;
    message.progress.dwPercentage = fRollback ? 100 : 0;
    nResult = GenericExecuteMessageHandler(&message, pContext);
    hr = UserExperienceInterpretExecuteResult(&pEngineState->userExperience, fRollback, message.dwAllowedResults, nResult);
    ExitOnRootFailure(hr, "BA aborted MSU progress.");

    fExecuted = TRUE;

    // execute package
    if (pPackage->fPerMachine)
    {
        hrExecute = ElevationExecuteMsuPackage(pEngineState->companionConnection.hPipe, pExecuteAction, fRollback, fStopWusaService, GenericExecuteMessageHandler, pContext, pRestart);
        ExitOnFailure(hrExecute, "Failed to configure per-machine MSU package.");
    }
    else
    {
        hrExecute = E_UNEXPECTED;
        ExitOnFailure(hr, "MSU packages cannot be per-user.");
    }

    message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
    message.dwAllowedResults = MB_OKCANCEL;
    message.progress.dwPercentage = fRollback ? 0 : 100;
    nResult = GenericExecuteMessageHandler(&message, pContext);
    hr = UserExperienceInterpretExecuteResult(&pEngineState->userExperience, fRollback, message.dwAllowedResults, nResult);
    ExitOnRootFailure(hr, "BA aborted MSU progress.");

    pContext->cExecutedPackages += fRollback ? -1 : 1;
    (*pContext->pcOverallProgressTicks) += fRollback ? -1 : 1;

    hr = ReportOverallProgressTicks(&pEngineState->userExperience, fRollback, pEngineState->plan.cOverallProgressTicksTotal, *pContext->pcOverallProgressTicks);
    ExitOnRootFailure(hr, "BA aborted MSU package execute progress.");

LExit:
    if (fExecuted)
    {
        MsuEngineUpdateInstallRegistrationState(pExecuteAction, hrExecute);
    }

    if (fBeginCalled)
    {
        hr = ExecutePackageComplete(&pEngineState->userExperience, &pEngineState->variables, pPackage, hr, hrExecute, fRollback, pRestart, pfRetry, pfSuspend);
    }

    return hr;
}

static HRESULT ExecutePackageProviderAction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pAction,
    __in BURN_EXECUTE_CONTEXT* /*pContext*/
    )
{
    HRESULT hr = S_OK;

    if (pAction->packageProvider.pPackage->fPerMachine)
    {
        hr = ElevationExecutePackageProviderAction(pEngineState->companionConnection.hPipe, pAction);
        ExitOnFailure(hr, "Failed to register the package provider on per-machine package.");
    }
    else
    {
        hr = DependencyExecutePackageProviderAction(pAction);
        ExitOnFailure(hr, "Failed to register the package provider on per-user package.");
    }

LExit:
    return hr;
}

static HRESULT ExecuteDependencyAction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_EXECUTE_ACTION* pAction,
    __in BURN_EXECUTE_CONTEXT* /*pContext*/
    )
{
    HRESULT hr = S_OK;

    if (pAction->packageDependency.pPackage->fPerMachine)
    {
        hr = ElevationExecutePackageDependencyAction(pEngineState->companionConnection.hPipe, pAction);
        ExitOnFailure(hr, "Failed to register the dependency on per-machine package.");
    }
    else
    {
        hr = DependencyExecutePackageDependencyAction(FALSE, pAction);
        ExitOnFailure(hr, "Failed to register the dependency on per-user package.");
    }

    if (pAction->packageDependency.pPackage->fCanAffectRegistration)
    {
        if (BURN_DEPENDENCY_ACTION_REGISTER == pAction->packageDependency.action)
        {
            if (BURN_PACKAGE_REGISTRATION_STATE_IGNORED == pAction->packageDependency.pPackage->cacheRegistrationState)
            {
                pAction->packageDependency.pPackage->cacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
            }

            if (BURN_PACKAGE_TYPE_MSP == pAction->packageDependency.pPackage->type)
            {
                for (DWORD i = 0; i < pAction->packageDependency.pPackage->Msp.cTargetProductCodes; ++i)
                {
                    BURN_MSPTARGETPRODUCT* pTargetProduct = pAction->packageDependency.pPackage->Msp.rgTargetProducts + i;

                    if (BURN_PACKAGE_REGISTRATION_STATE_IGNORED == pTargetProduct->registrationState)
                    {
                        pTargetProduct->registrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
                    }
                }
            }
            else if (BURN_PACKAGE_REGISTRATION_STATE_IGNORED == pAction->packageDependency.pPackage->installRegistrationState)
            {
                pAction->packageDependency.pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
            }
        }
        else if (BURN_DEPENDENCY_ACTION_UNREGISTER == pAction->packageDependency.action)
        {
            if (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pAction->packageDependency.pPackage->cacheRegistrationState)
            {
                pAction->packageDependency.pPackage->cacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_IGNORED;
            }

            if (BURN_PACKAGE_TYPE_MSP == pAction->packageDependency.pPackage->type)
            {
                for (DWORD i = 0; i < pAction->packageDependency.pPackage->Msp.cTargetProductCodes; ++i)
                {
                    BURN_MSPTARGETPRODUCT* pTargetProduct = pAction->packageDependency.pPackage->Msp.rgTargetProducts + i;

                    if (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pTargetProduct->registrationState)
                    {
                        pTargetProduct->registrationState = BURN_PACKAGE_REGISTRATION_STATE_IGNORED;
                    }
                }
            }
            else if (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pAction->packageDependency.pPackage->installRegistrationState)
            {
                pAction->packageDependency.pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_IGNORED;
            }
        }
    }

LExit:
    return hr;
}

static HRESULT ExecuteMsiBeginTransaction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in BURN_EXECUTE_CONTEXT* /*pContext*/
    )
{
    HRESULT hr = S_OK;
    BOOL fBeginCalled = FALSE;

    if (pRollbackBoundary->fActiveTransaction)
    {
        ExitFunction1(hr = E_INVALIDSTATE);
    }

    fBeginCalled = TRUE;
    hr = UserExperienceOnBeginMsiTransactionBegin(&pEngineState->userExperience, pRollbackBoundary->sczId);
    ExitOnRootFailure(hr, "BA aborted execute begin MSI transaction.");

    if (pEngineState->plan.fPerMachine)
    {
        hr = ElevationMsiBeginTransaction(pEngineState->companionConnection.hPipe, pRollbackBoundary);
        ExitOnFailure(hr, "Failed to begin an elevated MSI transaction.");
    }
    else
    {
        hr = MsiEngineBeginTransaction(pRollbackBoundary);
    }

    if (SUCCEEDED(hr))
    {
        pRollbackBoundary->fActiveTransaction = TRUE;

        ResetTransactionRegistrationState(pEngineState, FALSE);
    }

LExit:
    if (fBeginCalled)
    {
        UserExperienceOnBeginMsiTransactionComplete(&pEngineState->userExperience, pRollbackBoundary->sczId, hr);
    }

    return hr;
}

static HRESULT ExecuteMsiCommitTransaction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in BURN_EXECUTE_CONTEXT* /*pContext*/
    )
{
    HRESULT hr = S_OK;
    BOOL fCommitBeginCalled = FALSE;

    if (!pRollbackBoundary->fActiveTransaction)
    {
        ExitFunction1(hr = E_INVALIDSTATE);
    }

    fCommitBeginCalled = TRUE;
    hr = UserExperienceOnCommitMsiTransactionBegin(&pEngineState->userExperience, pRollbackBoundary->sczId);
    ExitOnRootFailure(hr, "BA aborted execute commit MSI transaction.");

    if (pEngineState->plan.fPerMachine)
    {
        hr = ElevationMsiCommitTransaction(pEngineState->companionConnection.hPipe, pRollbackBoundary);
        ExitOnFailure(hr, "Failed to commit an elevated MSI transaction.");
    }
    else
    {
        hr = MsiEngineCommitTransaction(pRollbackBoundary);
    }

    if (SUCCEEDED(hr))
    {
        pRollbackBoundary->fActiveTransaction = FALSE;

        ResetTransactionRegistrationState(pEngineState, TRUE);
    }

LExit:
    if (fCommitBeginCalled)
    {
        UserExperienceOnCommitMsiTransactionComplete(&pEngineState->userExperience, pRollbackBoundary->sczId, hr);
    }

    return hr;
}

static HRESULT ExecuteMsiRollbackTransaction(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in BURN_EXECUTE_CONTEXT* /*pContext*/
    )
{
    HRESULT hr = S_OK;
    BOOL fRollbackBeginCalled = FALSE;

    if (!pRollbackBoundary->fActiveTransaction)
    {
        ExitFunction();
    }

    fRollbackBeginCalled = TRUE;
    UserExperienceOnRollbackMsiTransactionBegin(&pEngineState->userExperience, pRollbackBoundary->sczId);

    if (pEngineState->plan.fPerMachine)
    {
        hr = ElevationMsiRollbackTransaction(pEngineState->companionConnection.hPipe, pRollbackBoundary);
        ExitOnFailure(hr, "Failed to rollback an elevated MSI transaction.");
    }
    else
    {
        hr = MsiEngineRollbackTransaction(pRollbackBoundary);
    }

LExit:
    pRollbackBoundary->fActiveTransaction = FALSE;

    ResetTransactionRegistrationState(pEngineState, FALSE);

    if (fRollbackBeginCalled)
    {
        UserExperienceOnRollbackMsiTransactionComplete(&pEngineState->userExperience, pRollbackBoundary->sczId, hr);
    }

    return hr;
}

static void ResetTransactionRegistrationState(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BOOL fCommit
    )
{
    for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
    {
        BURN_PACKAGE* pPackage = pEngineState->packages.rgPackages + i;

        if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
        {
            for (DWORD j = 0; j < pPackage->Msp.cTargetProductCodes; ++j)
            {
                BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + j;

                if (fCommit && BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN != pTargetProduct->transactionRegistrationState)
                {
                    pTargetProduct->registrationState = pTargetProduct->transactionRegistrationState;
                }

                pTargetProduct->transactionRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN;
            }
        }
        else if (fCommit && BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN != pPackage->transactionRegistrationState)
        {
            pPackage->installRegistrationState = pPackage->transactionRegistrationState;
        }

        pPackage->transactionRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN;
    }
}

static HRESULT CleanPackage(
    __in HANDLE hElevatedPipe,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;

    if (pPackage->fPerMachine)
    {
        hr = ElevationCleanPackage(hElevatedPipe, pPackage);
    }
    else
    {
        hr = CacheRemovePackage(FALSE, pPackage->sczId, pPackage->sczCacheId);
    }

    if (pPackage->fCanAffectRegistration)
    {
        pPackage->cacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
    }

    return hr;
}

static int GenericExecuteMessageHandler(
    __in GENERIC_EXECUTE_MESSAGE* pMessage,
    __in LPVOID pvContext
    )
{
    BURN_EXECUTE_CONTEXT* pContext = (BURN_EXECUTE_CONTEXT*)pvContext;
    int nResult = IDNOACTION;

    switch (pMessage->type)
    {
    case GENERIC_EXECUTE_MESSAGE_PROGRESS:
        {
            DWORD dwOverallProgress = pContext->cExecutePackagesTotal ? (pContext->cExecutedPackages * 100 + pMessage->progress.dwPercentage) / (pContext->cExecutePackagesTotal) : 0;
            UserExperienceOnExecuteProgress(pContext->pUX, pContext->pExecutingPackage->sczId, pMessage->progress.dwPercentage, dwOverallProgress, &nResult); // ignore return value.
        }
        break;

    case GENERIC_EXECUTE_MESSAGE_ERROR:
        UserExperienceOnError(pContext->pUX, BOOTSTRAPPER_ERROR_TYPE_EXE_PACKAGE, pContext->pExecutingPackage->sczId, pMessage->error.dwErrorCode, pMessage->error.wzMessage, pMessage->dwAllowedResults, 0, NULL, &nResult); // ignore return value.
        break;

    case GENERIC_EXECUTE_MESSAGE_FILES_IN_USE:
        UserExperienceOnExecuteFilesInUse(pContext->pUX, pContext->pExecutingPackage->sczId, pMessage->filesInUse.cFiles, pMessage->filesInUse.rgwzFiles, &nResult); // ignore return value.
        break;
    }

    nResult = UserExperienceCheckExecuteResult(pContext->pUX, pContext->fRollback, pMessage->dwAllowedResults, nResult);
    return nResult;
}

static int MsiExecuteMessageHandler(
    __in WIU_MSI_EXECUTE_MESSAGE* pMessage,
    __in_opt LPVOID pvContext
    )
{
    BURN_EXECUTE_CONTEXT* pContext = (BURN_EXECUTE_CONTEXT*)pvContext;
    int nResult = IDNOACTION;

    switch (pMessage->type)
    {
    case WIU_MSI_EXECUTE_MESSAGE_PROGRESS:
        {
        DWORD dwOverallProgress = pContext->cExecutePackagesTotal ? (pContext->cExecutedPackages * 100 + pMessage->progress.dwPercentage) / (pContext->cExecutePackagesTotal) : 0;
        UserExperienceOnExecuteProgress(pContext->pUX, pContext->pExecutingPackage->sczId, pMessage->progress.dwPercentage, dwOverallProgress, &nResult); // ignore return value.
        }
        break;

    case WIU_MSI_EXECUTE_MESSAGE_ERROR:
        nResult = pMessage->nResultRecommendation;
        UserExperienceOnError(pContext->pUX, BOOTSTRAPPER_ERROR_TYPE_WINDOWS_INSTALLER, pContext->pExecutingPackage->sczId, pMessage->error.dwErrorCode, pMessage->error.wzMessage, pMessage->dwAllowedResults, pMessage->cData, pMessage->rgwzData, &nResult); // ignore return value.
        break;

    case WIU_MSI_EXECUTE_MESSAGE_MSI_MESSAGE:
        nResult = pMessage->nResultRecommendation;
        UserExperienceOnExecuteMsiMessage(pContext->pUX, pContext->pExecutingPackage->sczId, pMessage->msiMessage.mt, pMessage->dwAllowedResults, pMessage->msiMessage.wzMessage, pMessage->cData, pMessage->rgwzData, &nResult); // ignore return value.
        break;

    case WIU_MSI_EXECUTE_MESSAGE_MSI_FILES_IN_USE:
        UserExperienceOnExecuteFilesInUse(pContext->pUX, pContext->pExecutingPackage->sczId, pMessage->msiFilesInUse.cFiles, pMessage->msiFilesInUse.rgwzFiles, &nResult); // ignore return value.
        break;
    }

    nResult = UserExperienceCheckExecuteResult(pContext->pUX, pContext->fRollback, pMessage->dwAllowedResults, nResult);
    return nResult;
}

static HRESULT ReportOverallProgressTicks(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BOOL fRollback,
    __in DWORD cOverallProgressTicksTotal,
    __in DWORD cOverallProgressTicks
    )
{
    HRESULT hr = S_OK;
    DWORD dwProgress = cOverallProgressTicksTotal ? (cOverallProgressTicks * 100 / cOverallProgressTicksTotal) : 0;

    // TODO: consider sending different progress numbers in the future.
    hr = UserExperienceOnProgress(pUX, fRollback, dwProgress, dwProgress);

    return hr;
}

static HRESULT ExecutePackageComplete(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PACKAGE* pPackage,
    __in HRESULT hrOverall,
    __in HRESULT hrExecute,
    __in BOOL fRollback,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart,
    __out BOOL* pfRetry,
    __out BOOL* pfSuspend
    )
{
    HRESULT hr = FAILED(hrOverall) ? hrOverall : hrExecute; // if the overall function failed use that otherwise use the execution result.
    BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION executePackageCompleteAction = FAILED(hrOverall) || SUCCEEDED(hrExecute) || pPackage->fVital ? BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION_NONE : BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION_IGNORE;

    // Send package execute complete to BA.
    UserExperienceOnExecutePackageComplete(pUX, pPackage->sczId, hr, *pRestart, &executePackageCompleteAction);
    if (BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION_RESTART == executePackageCompleteAction)
    {
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_INITIATED;
    }
    *pfRetry = (FAILED(hrExecute) && BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION_RETRY == executePackageCompleteAction); // allow retry only on failures.
    *pfSuspend = (BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION_SUSPEND == executePackageCompleteAction);

    // Remember this package as the package that initiated the forced restart.
    if (BOOTSTRAPPER_APPLY_RESTART_INITIATED == *pRestart)
    {
        // Best effort to set the forced restart package variable.
        VariableSetString(pVariables, BURN_BUNDLE_FORCED_RESTART_PACKAGE, pPackage->sczId, TRUE, FALSE);
    }

    // If we're retrying, leave a message in the log file and say everything is okay.
    if (*pfRetry)
    {
        LogId(REPORT_STANDARD, MSG_APPLY_RETRYING_PACKAGE, pPackage->sczId, hrExecute);
        hr = S_OK;
    }
    else if (SUCCEEDED(hrOverall) && FAILED(hrExecute) && BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION_IGNORE == executePackageCompleteAction && !pPackage->fVital) // If we *only* failed to execute and the BA ignored this *not-vital* package, say everything is okay.
    {
        LogId(REPORT_STANDARD, MSG_APPLY_CONTINUING_NONVITAL_PACKAGE, pPackage->sczId, hrExecute);
        hr = S_OK;
    }
    else
    {
        LogId(REPORT_STANDARD, MSG_APPLY_COMPLETED_PACKAGE, LoggingRollbackOrExecute(fRollback), pPackage->sczId, hr, LoggingRestartToString(*pRestart));
    }

    return hr;
}
