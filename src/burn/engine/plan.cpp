// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define PlanDumpLevel REPORT_DEBUG

// internal struct definitions


// internal function definitions

static void PlannedExecutePackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );
static void UninitializeRegistrationAction(
    __in BURN_DEPENDENT_REGISTRATION_ACTION* pAction
    );
static void UninitializeCacheAction(
    __in BURN_CACHE_ACTION* pCacheAction
    );
static void ResetPlannedContainerState(
    __in BURN_CONTAINER* pContainer
    );
static void ResetPlannedPayloadsState(
    __in BURN_PAYLOADS* pPayloads
    );
static void ResetPlannedPayloadGroupState(
    __in BURN_PAYLOAD_GROUP* pPayloadGroup
    );
static void ResetPlannedPackageState(
    __in BURN_PACKAGE* pPackage
    );
static void ResetPlannedRollbackBoundaryState(
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary
    );
static HRESULT PlanPackagesHelper(
    __in BURN_PACKAGE* rgPackages,
    __in DWORD cPackages,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT InitializePackage(
    __in BURN_PLAN* pPlan,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PACKAGE* pPackage
    );
static HRESULT ProcessPackage(
    __in BOOL fBundlePerMachine,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary
    );
static HRESULT ProcessPackageRollbackBoundary(
    __in BURN_PLAN* pPlan,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in_opt BURN_ROLLBACK_BOUNDARY* pEffectiveRollbackBoundary,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary
    );
static HRESULT GetActionDefaultRequestState(
    __in BOOTSTRAPPER_ACTION action,
    __in BOOTSTRAPPER_PACKAGE_STATE currentState,
    __out BOOTSTRAPPER_REQUEST_STATE* pRequestState
    );
static HRESULT AddRegistrationAction(
    __in BURN_PLAN* pPlan,
    __in BURN_DEPENDENT_REGISTRATION_ACTION_TYPE type,
    __in_z LPCWSTR wzDependentProviderKey,
    __in_z LPCWSTR wzOwnerBundleCode
    );
static HRESULT AddCachePackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BOOL fVital
    );
static HRESULT AddCachePackageHelper(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BOOL fVital
    );
static HRESULT AddCacheSlipstreamMsps(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );
static DWORD GetNextCheckpointId(
    __in BURN_PLAN* pPlan
    );
static HRESULT AppendCacheAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CACHE_ACTION** ppCacheAction
    );
static HRESULT AppendRollbackCacheAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CACHE_ACTION** ppCacheAction
    );
static HRESULT AppendCleanAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CLEAN_ACTION** ppCleanAction
    );
static HRESULT AppendRestoreRelatedBundleAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    );
static HRESULT ProcessPayloadGroup(
    __in BURN_PLAN* pPlan,
    __in BURN_PAYLOAD_GROUP* pPayloadGroup
    );
static void RemoveUnnecessaryActions(
    __in BOOL fExecute,
    __in BURN_EXECUTE_ACTION* rgActions,
    __in DWORD cActions
    );
static void FinalizePatchActions(
    __in BOOL fExecute,
    __in BURN_EXECUTE_ACTION* rgActions,
    __in DWORD cActions
    );
static void CalculateExpectedRegistrationStates(
    __in BURN_PACKAGE* rgPackages,
    __in DWORD cPackages
    );
static HRESULT PlanDependencyActions(
    __in BOOL fBundlePerMachine,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );
static HRESULT CalculateExecuteActions(
    __in BURN_PACKAGE* pPackage,
    __in_opt BURN_ROLLBACK_BOUNDARY* pActiveRollbackBoundary
    );
static BURN_CACHE_PACKAGE_TYPE GetCachePackageType(
    __in BURN_PACKAGE* pPackage,
    __in BOOL fExecute
    );
static BOOL ForceCache(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );

// function definitions

extern "C" void PlanReset(
    __in BURN_PLAN* pPlan,
    __in BURN_VARIABLES* pVariables,
    __in BURN_CONTAINERS* pContainers,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOAD_GROUP* pLayoutPayloads
    )
{
    ReleaseNullStr(pPlan->sczLayoutDirectory);
    PackageUninitialize(&pPlan->forwardCompatibleBundle);

    if (pPlan->rgRegistrationActions)
    {
        for (DWORD i = 0; i < pPlan->cRegistrationActions; ++i)
        {
            UninitializeRegistrationAction(&pPlan->rgRegistrationActions[i]);
        }
        MemFree(pPlan->rgRegistrationActions);
    }

    if (pPlan->rgRollbackRegistrationActions)
    {
        for (DWORD i = 0; i < pPlan->cRollbackRegistrationActions; ++i)
        {
            UninitializeRegistrationAction(&pPlan->rgRollbackRegistrationActions[i]);
        }
        MemFree(pPlan->rgRollbackRegistrationActions);
    }

    if (pPlan->rgCacheActions)
    {
        for (DWORD i = 0; i < pPlan->cCacheActions; ++i)
        {
            UninitializeCacheAction(&pPlan->rgCacheActions[i]);
        }
        MemFree(pPlan->rgCacheActions);
    }

    if (pPlan->rgExecuteActions)
    {
        for (DWORD i = 0; i < pPlan->cExecuteActions; ++i)
        {
            PlanUninitializeExecuteAction(&pPlan->rgExecuteActions[i]);
        }
        MemFree(pPlan->rgExecuteActions);
    }

    if (pPlan->rgRollbackActions)
    {
        for (DWORD i = 0; i < pPlan->cRollbackActions; ++i)
        {
            PlanUninitializeExecuteAction(&pPlan->rgRollbackActions[i]);
        }
        MemFree(pPlan->rgRollbackActions);
    }

    if (pPlan->rgRestoreRelatedBundleActions)
    {
        for (DWORD i = 0; i < pPlan->cRestoreRelatedBundleActions; ++i)
        {
            PlanUninitializeExecuteAction(&pPlan->rgRestoreRelatedBundleActions[i]);
        }
        MemFree(pPlan->rgRestoreRelatedBundleActions);
    }

    if (pPlan->rgCleanActions)
    {
        // Nothing needs to be freed inside clean actions today.
        MemFree(pPlan->rgCleanActions);
    }

    if (pPlan->rgPlannedProviders)
    {
        ReleaseDependencyArray(pPlan->rgPlannedProviders, pPlan->cPlannedProviders);
    }

    if (pPlan->rgContainerProgress)
    {
        MemFree(pPlan->rgContainerProgress);
    }

    if (pPlan->shContainerProgress)
    {
        ReleaseDict(pPlan->shContainerProgress);
    }

    if (pPlan->rgPayloadProgress)
    {
        MemFree(pPlan->rgPayloadProgress);
    }

    if (pPlan->shPayloadProgress)
    {
        ReleaseDict(pPlan->shPayloadProgress);
    }

    if (pPlan->pPayloads)
    {
        ResetPlannedPayloadsState(pPlan->pPayloads);
    }

    memset(pPlan, 0, sizeof(BURN_PLAN));

    if (pContainers->rgContainers)
    {
        for (DWORD i = 0; i < pContainers->cContainers; ++i)
        {
            ResetPlannedContainerState(&pContainers->rgContainers[i]);
        }
    }

    // Reset the planned actions for each package.
    if (pPackages->rgPackages)
    {
        for (DWORD i = 0; i < pPackages->cPackages; ++i)
        {
            ResetPlannedPackageState(&pPackages->rgPackages[i]);
        }
    }

    ResetPlannedPayloadGroupState(pLayoutPayloads);

    // Reset the planned state for each rollback boundary.
    if (pPackages->rgRollbackBoundaries)
    {
        for (DWORD i = 0; i < pPackages->cRollbackBoundaries; ++i)
        {
            ResetPlannedRollbackBoundaryState(&pPackages->rgRollbackBoundaries[i]);
        }
    }

    PlanSetVariables(BOOTSTRAPPER_ACTION_UNKNOWN, BOOTSTRAPPER_PACKAGE_SCOPE_INVALID, BOOTSTRAPPER_SCOPE_DEFAULT, pVariables);
}

extern "C" void PlanUninitializeExecuteAction(
    __in BURN_EXECUTE_ACTION* pExecuteAction
    )
{
    switch (pExecuteAction->type)
    {
    case BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE:
        ReleaseStr(pExecuteAction->relatedBundle.sczIgnoreDependencies);
        ReleaseStr(pExecuteAction->relatedBundle.sczAncestors);
        ReleaseStr(pExecuteAction->relatedBundle.sczEngineWorkingDirectory);
        break;

    case BURN_EXECUTE_ACTION_TYPE_BUNDLE_PACKAGE:
        ReleaseStr(pExecuteAction->bundlePackage.sczParent);
        ReleaseStr(pExecuteAction->bundlePackage.sczIgnoreDependencies);
        ReleaseStr(pExecuteAction->bundlePackage.sczAncestors);
        ReleaseStr(pExecuteAction->bundlePackage.sczEngineWorkingDirectory);
        break;

    case BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE:
        ReleaseStr(pExecuteAction->exePackage.sczAncestors);
        ReleaseStr(pExecuteAction->exePackage.sczEngineWorkingDirectory);
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE:
        ReleaseStr(pExecuteAction->msiPackage.sczLogPath);
        ReleaseMem(pExecuteAction->msiPackage.rgFeatures);
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSP_TARGET:
        ReleaseStr(pExecuteAction->mspTarget.sczTargetProductCode);
        ReleaseStr(pExecuteAction->mspTarget.sczLogPath);
        ReleaseMem(pExecuteAction->mspTarget.rgOrderedPatches);
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE:
        ReleaseStr(pExecuteAction->msuPackage.sczLogPath);
        break;

    case BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY:
        ReleaseStr(pExecuteAction->packageDependency.sczBundleProviderKey);
        break;

    case BURN_EXECUTE_ACTION_TYPE_UNINSTALL_MSI_COMPATIBLE_PACKAGE:
        ReleaseStr(pExecuteAction->uninstallMsiCompatiblePackage.sczLogPath);
        break;
    }
}

extern "C" HRESULT PlanSetVariables(
    __in BOOTSTRAPPER_ACTION action,
    __in BOOTSTRAPPER_PACKAGE_SCOPE authoredScope,
    __in BOOTSTRAPPER_SCOPE plannedScope,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;

    hr = VariableSetNumeric(pVariables, BURN_BUNDLE_ACTION, action, TRUE);
    ExitOnFailure(hr, "Failed to set the bundle action built-in variable.");

    hr = VariableSetNumeric(pVariables, BURN_BUNDLE_AUTHORED_SCOPE, authoredScope, TRUE);
    ExitOnFailure(hr, "Failed to set the bundle authored scope built-in variable.");

    hr = VariableSetNumeric(pVariables, BURN_BUNDLE_PLANNED_SCOPE, plannedScope, TRUE);
    ExitOnFailure(hr, "Failed to set the bundle planned scope built-in variable.");

LExit:
    return hr;
}

extern "C" HRESULT PlanDefaultPackageRequestState(
    __in BURN_PACKAGE_TYPE packageType,
    __in BOOTSTRAPPER_PACKAGE_STATE currentState,
    __in BOOTSTRAPPER_ACTION action,
    __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition,
    __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT repairCondition,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __out BOOTSTRAPPER_REQUEST_STATE* pRequestState
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_REQUEST_STATE defaultRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;

    // If doing layout, then always default to requesting the package be cached.
    if (BOOTSTRAPPER_ACTION_LAYOUT == action)
    {
        *pRequestState = BOOTSTRAPPER_REQUEST_STATE_CACHE;
    }
    else if (BOOTSTRAPPER_ACTION_CACHE == action)
    {
        switch (currentState)
        {
        case BOOTSTRAPPER_PACKAGE_STATE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
            break;

        default:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_CACHE;
            break;
        }
    }
    else // pick the best option for the action state and install condition.
    {
        hr = GetActionDefaultRequestState(action, currentState, &defaultRequestState);
        ExitOnFailure(hr, "Failed to get default request state for action.");

        if (BOOTSTRAPPER_ACTION_UNINSTALL != action && BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL != action)
        {
            // For patch related bundles, only install a patch if currently absent during install, modify, or repair.
            if (BOOTSTRAPPER_RELATION_PATCH == relationType && BURN_PACKAGE_TYPE_MSP == packageType)
            {
                if (BOOTSTRAPPER_PACKAGE_STATE_ABSENT != currentState)
                {
                    defaultRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
                }
                else if (BOOTSTRAPPER_ACTION_INSTALL == action ||
                    BOOTSTRAPPER_ACTION_MODIFY == action ||
                    BOOTSTRAPPER_ACTION_REPAIR == action)
                {
                    defaultRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
                }
            }

            // If we're not doing an uninstall, use the install condition
            // to determine whether to use the default request state or make the package absent.
            if (BOOTSTRAPPER_PACKAGE_CONDITION_FALSE == installCondition)
            {
                defaultRequestState = BOOTSTRAPPER_REQUEST_STATE_ABSENT;
            }

            // Obsolete means the package is not on the machine and should not be installed,
            // *except* patches can be obsolete and present.
            // Superseded means the package is on the machine but not active, so only uninstall operations are allowed.
            // All other operations do nothing.
            else if (BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE == currentState || BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED == currentState)
            {
                defaultRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT <= defaultRequestState ? BOOTSTRAPPER_REQUEST_STATE_NONE : defaultRequestState;
            }
            else if (BOOTSTRAPPER_ACTION_REPAIR == action && BOOTSTRAPPER_PACKAGE_CONDITION_FALSE == repairCondition)
            {
                defaultRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
            }
        }

        *pRequestState = defaultRequestState;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanLayoutBundle(
    __in BURN_PLAN* pPlan,
    __in_z LPCWSTR wzExecutableName,
    __in DWORD64 qwBundleSize,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PAYLOAD_GROUP* pLayoutPayloads
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_ACTION* pCacheAction = NULL;
    LPWSTR sczLayoutDirectory = NULL;
    LPWSTR sczExecutablePath = NULL;

    // Get the layout directory.
    hr = VariableGetString(pVariables, BURN_BUNDLE_LAYOUT_DIRECTORY, &sczLayoutDirectory);
    if (E_NOTFOUND == hr) // if not set, use the current directory as the layout directory.
    {
        hr = PathForCurrentProcess(&sczExecutablePath, NULL);
        ExitOnFailure(hr, "Failed to get path for current executing process as layout directory.");

        hr = PathGetDirectory(sczExecutablePath, &sczLayoutDirectory);
        ExitOnFailure(hr, "Failed to get executing process as layout directory.");
    }
    ExitOnFailure(hr, "Failed to get bundle layout directory property.");

    hr = PathGetFullPathName(sczLayoutDirectory, &pPlan->sczLayoutDirectory, NULL, NULL);
    ExitOnFailure(hr, "Failed to ensure layout directory is fully qualified.");

    hr = PathBackslashTerminate(&pPlan->sczLayoutDirectory);
    ExitOnFailure(hr, "Failed to ensure layout directory is backslash terminated.");

    hr = ProcessPayloadGroup(pPlan, pLayoutPayloads);
    ExitOnFailure(hr, "Failed to process payload group for bundle.");

    // Plan the layout of the bundle engine itself.
    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append bundle start action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_LAYOUT_BUNDLE;

    hr = StrAllocString(&pCacheAction->bundleLayout.sczExecutableName, wzExecutableName, 0);
    ExitOnFailure(hr, "Failed to to copy executable name for bundle.");

    hr = CacheCalculateBundleLayoutWorkingPath(pPlan->pCache, pPlan->wzBundleCode, &pCacheAction->bundleLayout.sczUnverifiedPath);
    ExitOnFailure(hr, "Failed to calculate bundle layout working path.");

    pCacheAction->bundleLayout.qwBundleSize = qwBundleSize;
    pCacheAction->bundleLayout.pPayloadGroup = pLayoutPayloads;

    // Acquire + Verify + Finalize
    pPlan->qwCacheSizeTotal += 3 * qwBundleSize;

    ++pPlan->cOverallProgressTicksTotal;

LExit:
    ReleaseStr(sczExecutablePath);
    ReleaseStr(sczLayoutDirectory);

    return hr;
}

extern "C" HRESULT PlanForwardCompatibleBundles(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PLAN* pPlan,
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;
    BOOL fRecommendIgnore = TRUE;
    BOOL fIgnoreBundle = FALSE;
    BOOTSTRAPPER_ACTION action = pPlan->action;

    if (!pRegistration->fForwardCompatibleBundleExists)
    {
        ExitFunction();
    }

    // Only change the recommendation if an active parent was provided.
    if (pPlan->pInternalCommand->sczActiveParent && *pPlan->pInternalCommand->sczActiveParent)
    {
        // On install, recommend running the forward compatible bundle because there is an active parent. This
        // will essentially register the parent with the forward compatible bundle.
        if (BOOTSTRAPPER_ACTION_INSTALL == action)
        {
            fRecommendIgnore = FALSE;
        }
        else if (BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == action ||
                 BOOTSTRAPPER_ACTION_UNINSTALL == action ||
                    BOOTSTRAPPER_ACTION_MODIFY == action ||
                    BOOTSTRAPPER_ACTION_REPAIR == action)
        {
            // When modifying the bundle, only recommend running the forward compatible bundle if the parent
            // is already registered as a dependent of the provider key.
            if (pRegistration->fParentRegisteredAsDependent)
            {
                fRecommendIgnore = FALSE;
            }
        }
    }

    for (DWORD iRelatedBundle = 0; iRelatedBundle < pRegistration->relatedBundles.cRelatedBundles; ++iRelatedBundle)
    {
        BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + iRelatedBundle;
        if (!pRelatedBundle->fForwardCompatible)
        {
            continue;
        }

        fIgnoreBundle = fRecommendIgnore;

        hr = BACallbackOnPlanForwardCompatibleBundle(pUX, pRelatedBundle->package.sczId, pRelatedBundle->detectRelationType, pRelatedBundle->sczTag, pRelatedBundle->package.fPerMachine, pRelatedBundle->pVersion, &fIgnoreBundle);
        ExitOnRootFailure(hr, "BA aborted plan forward compatible bundle.");

        if (!fIgnoreBundle)
        {
            hr = PseudoBundleInitializePassthrough(&pPlan->forwardCompatibleBundle, pPlan->pInternalCommand, pPlan->pCommand, &pRelatedBundle->package);
            ExitOnFailure(hr, "Failed to initialize pass through bundle.");

            pPlan->fEnabledForwardCompatibleBundle = TRUE;
            break;
        }
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanPackages(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;

    hr = PlanPackagesHelper(pPackages->rgPackages, pPackages->cPackages, pUX, pPlan, pLog, pVariables);

    return hr;
}

extern "C" HRESULT PlanRegistration(
    __in BURN_PLAN* pPlan,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_DEPENDENCIES* pDependencies,
    __in BOOTSTRAPPER_RESUME_TYPE /*resumeType*/,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __inout BOOL* pfContinuePlanning
    )
{
    HRESULT hr = S_OK;
    STRINGDICT_HANDLE sdBundleDependents = NULL;
    STRINGDICT_HANDLE sdIgnoreDependents = NULL;
    BOOL fDependentBlocksUninstall = FALSE;

    pPlan->fCanAffectMachineState = TRUE; // register the bundle since we're modifying machine state.
    pPlan->fDisallowRemoval = FALSE; // by default the bundle can be planned to be removed

    // Ensure the bundle is cached if not running from the cache.
    if (!CacheBundleRunningFromCache(pPlan->pCache))
    {
        pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE;
    }

    if (pPlan->pInternalCommand->fArpSystemComponent)
    {
        pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_ARP_SYSTEM_COMPONENT;
    }

    if (BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action || BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == pPlan->action)
    {
        // If our provider key was not owned by a different bundle,
        // then plan to write our provider key registration to "fix it" if broken
        // in case the bundle isn't successfully uninstalled.
        if (!pRegistration->fDetectedForeignProviderKeyBundleCode)
        {
            pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY;
        }

        // Create the dictionary of dependents that should be ignored.
        hr = DictCreateStringList(&sdIgnoreDependents, 5, DICT_FLAG_CASEINSENSITIVE);
        ExitOnFailure(hr, "Failed to create the string dictionary.");

        // If the self-dependent dependent exists, plan its removal. If we did not do this, we
        // would prevent self-removal.
        if (pRegistration->fSelfRegisteredAsDependent)
        {
            hr = AddRegistrationAction(pPlan, BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_UNREGISTER, pDependencies->wzSelfDependent, pRegistration->sczCode);
            ExitOnFailure(hr, "Failed to allocate registration action.");

            hr = DependencyAddIgnoreDependencies(sdIgnoreDependents, pDependencies->wzSelfDependent);
            ExitOnFailure(hr, "Failed to add self-dependent to ignore dependents.");
        }

        if (!pDependencies->fIgnoreAllDependents)
        {
            // If we are not doing an upgrade, we check to see if there are still dependents on us and if so we skip planning.
            // However, when being upgraded, we always execute our uninstall because a newer version of us is probably
            // already on the machine and we need to clean up the stuff specific to this bundle.
            if (BOOTSTRAPPER_RELATION_UPGRADE != relationType)
            {
                // If there were other dependencies to ignore, add them.
                for (DWORD iDependency = 0; iDependency < pDependencies->cIgnoredDependencies; ++iDependency)
                {
                    DEPENDENCY* pDependency = pDependencies->rgIgnoredDependencies + iDependency;

                    hr = DictKeyExists(sdIgnoreDependents, pDependency->sczKey);
                    if (E_NOTFOUND != hr)
                    {
                        ExitOnFailure(hr, "Failed to check the dictionary of ignored dependents.");
                    }
                    else
                    {
                        hr = DictAddKey(sdIgnoreDependents, pDependency->sczKey);
                        ExitOnFailure(hr, "Failed to add dependent key to ignored dependents.");
                    }
                }

                // For addon or patch bundles, dependent related bundles should be ignored. This allows
                // that addon or patch to be removed even though bundles it targets still are registered.
                for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
                {
                    const BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + i;

                    if (BOOTSTRAPPER_RELATION_DEPENDENT_ADDON == pRelatedBundle->planRelationType ||
                        BOOTSTRAPPER_RELATION_DEPENDENT_PATCH == pRelatedBundle->planRelationType)
                    {
                        for (DWORD j = 0; j < pRelatedBundle->package.cDependencyProviders; ++j)
                        {
                            const BURN_DEPENDENCY_PROVIDER* pProvider = pRelatedBundle->package.rgDependencyProviders + j;

                            hr = DependencyAddIgnoreDependencies(sdIgnoreDependents, pProvider->sczKey);
                            ExitOnFailure(hr, "Failed to add dependent bundle provider key to ignore dependents.");
                        }
                    }
                }

                // If there are any (non-ignored and not-planned-to-be-removed) dependents left, skip planning.
                for (DWORD iDependent = 0; iDependent < pRegistration->cDependents; ++iDependent)
                {
                    DEPENDENCY* pDependent = pRegistration->rgDependents + iDependent;

                    hr = DictKeyExists(sdIgnoreDependents, pDependent->sczKey);
                    if (E_NOTFOUND == hr)
                    {
                        hr = S_OK;

                        // TODO: callback to the BA and let it have the option to ignore this dependent?
                        if (!fDependentBlocksUninstall)
                        {
                            fDependentBlocksUninstall = TRUE;

                            LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_DUE_TO_DEPENDENTS);
                        }

                        LogId(REPORT_VERBOSE, MSG_DEPENDENCY_BUNDLE_DEPENDENT, pDependent->sczKey, LoggingStringOrUnknownIfNull(pDependent->sczName));
                    }
                    ExitOnFailure(hr, "Failed to check for remaining dependents during planning.");
                }

                if (fDependentBlocksUninstall)
                {
                    if (BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == pPlan->action)
                    {
                        fDependentBlocksUninstall = FALSE;
                        LogId(REPORT_STANDARD, MSG_PLAN_NOT_SKIPPED_DUE_TO_DEPENDENTS);
                    }
                    else
                    {
                        pPlan->fDisallowRemoval = TRUE; // ensure the registration stays
                        *pfContinuePlanning = FALSE; // skip the rest of planning.
                    }
                }
            }
        }
    }
    else
    {
        BOOL fAddonOrPatchBundle = (pRegistration->cAddonCodes || pRegistration->cPatchCodes);

        // Always plan to write our provider key registration when installing/modify/repair to "fix it"
        // if broken.
        pPlan->dwRegistrationOperations |= BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY;

        // Create the dictionary of bundle dependents.
        hr = DictCreateStringList(&sdBundleDependents, 5, DICT_FLAG_CASEINSENSITIVE);
        ExitOnFailure(hr, "Failed to create the string dictionary.");

        for (DWORD iDependent = 0; iDependent < pRegistration->cDependents; ++iDependent)
        {
            DEPENDENCY* pDependent = pRegistration->rgDependents + iDependent;

            hr = DictKeyExists(sdBundleDependents, pDependent->sczKey);
            if (E_NOTFOUND == hr)
            {
                hr = DictAddKey(sdBundleDependents, pDependent->sczKey);
                ExitOnFailure(hr, "Failed to add dependent key to bundle dependents.");
            }
            ExitOnFailure(hr, "Failed to check the dictionary of bundle dependents.");
        }

        // Register each dependent related bundle. The ensures that addons and patches are reference
        // counted and stick around until the last targeted bundle is removed.
        for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
        {
            const BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + i;

            if (BOOTSTRAPPER_RELATION_DEPENDENT_ADDON == pRelatedBundle->planRelationType ||
                BOOTSTRAPPER_RELATION_DEPENDENT_PATCH == pRelatedBundle->planRelationType)
            {
                for (DWORD j = 0; j < pRelatedBundle->package.cDependencyProviders; ++j)
                {
                    const BURN_DEPENDENCY_PROVIDER* pProvider = pRelatedBundle->package.rgDependencyProviders + j;

                    hr = DictKeyExists(sdBundleDependents, pProvider->sczKey);
                    if (E_NOTFOUND == hr)
                    {
                        hr = DictAddKey(sdBundleDependents, pProvider->sczKey);
                        ExitOnFailure(hr, "Failed to add new dependent key to bundle dependents.");

                        hr = AddRegistrationAction(pPlan, BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER, pProvider->sczKey, pRelatedBundle->package.sczId);
                        ExitOnFailure(hr, "Failed to add registration action for dependent related bundle.");
                    }
                    ExitOnFailure(hr, "Failed to check the dictionary of bundle dependents.");
                }
            }
        }

        // Only do the following if we decided there was a dependent self to register. If so and and an explicit parent was
        // provided, register dependent self. Otherwise, if this bundle is not an addon or patch bundle then self-regisiter
        // as our own dependent.
        if (pDependencies->wzSelfDependent && !pRegistration->fSelfRegisteredAsDependent && (pDependencies->wzActiveParent || !fAddonOrPatchBundle))
        {
            hr = AddRegistrationAction(pPlan, BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER, pDependencies->wzSelfDependent, pRegistration->sczCode);
            ExitOnFailure(hr, "Failed to add registration action for self dependent.");
        }
    }

LExit:
    ReleaseDict(sdBundleDependents);
    ReleaseDict(sdIgnoreDependents);

    return hr;
}

extern "C" HRESULT PlanPassThroughBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;

    // Plan passthrough package.
    hr = PlanPackagesHelper(pPackage, 1, pUX, pPlan, pLog, pVariables);
    ExitOnFailure(hr, "Failed to process passthrough package.");

LExit:
    return hr;
}

extern "C" HRESULT PlanUpdateBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;

    Assert(!pPackage->fPerMachine);
    Assert(BURN_PACKAGE_TYPE_EXE == pPackage->type);
    pPackage->Exe.fFireAndForget = BOOTSTRAPPER_ACTION_UPDATE_REPLACE == pPlan->action;

    // Plan update package.
    hr = PlanPackagesHelper(pPackage, 1, pUX, pPlan, pLog, pVariables);
    ExitOnFailure(hr, "Failed to process update package.");

LExit:
    return hr;
}

extern "C" HRESULT PlanPackagesAndBundleScope(
    __in BURN_PACKAGE* rgPackages,
    __in DWORD cPackages,
    __in BOOTSTRAPPER_SCOPE scope,
    __in BOOTSTRAPPER_PACKAGE_SCOPE authoredScope,
    __in BOOTSTRAPPER_SCOPE commandLineScope,
    __in BOOTSTRAPPER_SCOPE detectedScope,
    __out BOOTSTRAPPER_SCOPE* pResultingScope,
    __out BOOL* pfRegistrationPerMachine
)
{
    HRESULT hr = S_OK;
    BOOL fRegistrationPerMachine = TRUE;

    // If a scope was specified on the command line and the BA didn't set a scope,
    // let the command-line switch override.
    if (BOOTSTRAPPER_SCOPE_DEFAULT != commandLineScope)
    {
        if (BOOTSTRAPPER_PACKAGE_SCOPE_PER_MACHINE_OR_PER_USER == authoredScope || BOOTSTRAPPER_PACKAGE_SCOPE_PER_USER_OR_PER_MACHINE == authoredScope)
        {
            if (BOOTSTRAPPER_SCOPE_DEFAULT == scope)
            {
                scope = commandLineScope;
            }
            else
            {
                LogId(REPORT_STANDARD, MSG_SCOPE_IGNORED_BA_SCOPE);
            }
        }
        else
        {
            LogId(REPORT_STANDARD, MSG_SCOPE_IGNORED_UNCONFIGURABLE);
        }
    }

    if (BOOTSTRAPPER_SCOPE_DEFAULT != detectedScope)
    {
        scope = detectedScope;

        LogId(REPORT_WARNING, MSG_PLAN_INSTALLED_SCOPE, LoggingBundleScopeToString(detectedScope));
    }

    for (DWORD i = 0; i < cPackages; ++i)
    {
        BURN_PACKAGE* pPackage = rgPackages + i;

        pPackage->fPerMachine =
            (BOOTSTRAPPER_PACKAGE_SCOPE_PER_MACHINE == pPackage->scope)
            || (BOOTSTRAPPER_PACKAGE_SCOPE_PER_MACHINE_OR_PER_USER == pPackage->scope &&
                (BOOTSTRAPPER_SCOPE_DEFAULT == scope || BOOTSTRAPPER_SCOPE_PER_MACHINE == scope))
            || (BOOTSTRAPPER_PACKAGE_SCOPE_PER_USER_OR_PER_MACHINE == pPackage->scope &&
                BOOTSTRAPPER_SCOPE_PER_MACHINE == scope);

        // Any per-user package makes the registration per-user as well.
        if (!pPackage->fPerMachine)
        {
            fRegistrationPerMachine = FALSE;
        }
    }

    *pResultingScope = scope;
    *pfRegistrationPerMachine = fRegistrationPerMachine;

//LExit:
    return hr;
}


static HRESULT PlanPackagesHelper(
    __in BURN_PACKAGE* rgPackages,
    __in DWORD cPackages,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BOOL fBundlePerMachine = pPlan->fPerMachine; // bundle is per-machine if plan starts per-machine.
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = NULL;
    BOOL fReverseOrder = BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action || BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == pPlan->action;

    // Initialize the packages.
    for (DWORD i = 0; i < cPackages; ++i)
    {
        DWORD iPackage = fReverseOrder ? cPackages - 1 - i : i;
        BURN_PACKAGE* pPackage = rgPackages + iPackage;

        hr = InitializePackage(pPlan, pUX, pVariables, pPackage);
        ExitOnFailure(hr, "Failed to initialize package.");
    }

    // Initialize the patch targets after all packages, since they could rely on the requested state of packages that are after the patch's package in the chain.
    for (DWORD i = 0; i < cPackages; ++i)
    {
        DWORD iPackage = fReverseOrder ? cPackages - 1 - i : i;
        BURN_PACKAGE* pPackage = rgPackages + iPackage;

        if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
        {
            hr = MspEnginePlanInitializePackage(pPackage, pUX);
            ExitOnFailure(hr, "Failed to initialize plan package: %ls", pPackage->sczId);
        }
    }

    // Plan the packages.
    for (DWORD i = 0; i < cPackages; ++i)
    {
        DWORD iPackage = fReverseOrder ? cPackages - 1 - i : i;
        BURN_PACKAGE* pPackage = rgPackages + iPackage;

        hr = ProcessPackage(fBundlePerMachine, pUX, pPlan, pPackage, pLog, pVariables, &pRollbackBoundary);
        ExitOnFailure(hr, "Failed to process package.");
    }

    // If we still have an open rollback boundary, complete it.
    if (pRollbackBoundary)
    {
        hr = PlanRollbackBoundaryComplete(pPlan);
        ExitOnFailure(hr, "Failed to plan final rollback boundary complete.");

        pRollbackBoundary = NULL;
    }

    // Passthrough packages are never cleaned up by the calling bundle (they delete themselves when appropriate).
    if (!pPlan->fEnabledForwardCompatibleBundle && BOOTSTRAPPER_ACTION_LAYOUT != pPlan->action)
    {
        // Plan clean up of packages.
        for (DWORD i = 0; i < cPackages; ++i)
        {
            DWORD iPackage = fReverseOrder ? cPackages - 1 - i : i;
            BURN_PACKAGE* pPackage = rgPackages + iPackage;

            hr = PlanCleanPackage(pPlan, pPackage);
            ExitOnFailure(hr, "Failed to plan clean package.");
        }
    }

    // Remove unnecessary actions.
    hr = PlanFinalizeActions(pPlan);
    ExitOnFailure(hr, "Failed to remove unnecessary actions from plan.");

    CalculateExpectedRegistrationStates(rgPackages, cPackages);

    // Let the BA know the actions that were planned.
    for (DWORD i = 0; i < cPackages; ++i)
    {
        DWORD iPackage = fReverseOrder ? cPackages - 1 - i : i;
        BURN_PACKAGE* pPackage = rgPackages + iPackage;

        BACallbackOnPlannedPackage(pUX, pPackage->sczId, pPackage->execute, pPackage->rollback, NULL != pPackage->hCacheEvent, pPackage->fPlannedUncache);

        if (pPackage->compatiblePackage.fPlannable)
        {
            BACallbackOnPlannedCompatiblePackage(pUX, pPackage->sczId, pPackage->compatiblePackage.compatibleEntry.sczId, pPackage->compatiblePackage.fRemove);
        }
    }

LExit:
    return hr;
}

static HRESULT InitializePackage(
    __in BURN_PLAN* pPlan,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition = BOOTSTRAPPER_PACKAGE_CONDITION_DEFAULT;
    BOOTSTRAPPER_PACKAGE_CONDITION_RESULT repairCondition = BOOTSTRAPPER_PACKAGE_CONDITION_DEFAULT;
    BOOL fEvaluatedCondition = FALSE;
    BOOL fBeginCalled = FALSE;
    BOOTSTRAPPER_RELATION_TYPE relationType = pPlan->pCommand->relationType;

    if (BURN_PACKAGE_TYPE_EXE == pPackage->type && pPackage->Exe.fPseudoPackage)
    {
        // Exe pseudo packages are not configurable.
        // The BA already requested this package to be executed
        // * by the overall plan action for UpdateReplace
        // * by enabling the forward compatible bundle for Passthrough
        pPackage->defaultRequested = pPackage->requested = BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT;
        ExitFunction();
    }

    if (pPackage->fCanAffectRegistration)
    {
        pPackage->expectedCacheRegistrationState = pPackage->cacheRegistrationState;
        pPackage->expectedInstallRegistrationState = pPackage->installRegistrationState;
    }

    if (pPackage->sczInstallCondition && *pPackage->sczInstallCondition)
    {
        hr = ConditionEvaluate(pVariables, pPackage->sczInstallCondition, &fEvaluatedCondition);
        ExitOnFailure(hr, "Failed to evaluate install condition.");

        installCondition = fEvaluatedCondition ? BOOTSTRAPPER_PACKAGE_CONDITION_TRUE : BOOTSTRAPPER_PACKAGE_CONDITION_FALSE;
    }

    if (pPackage->sczRepairCondition && *pPackage->sczRepairCondition)
    {
        hr = ConditionEvaluate(pVariables, pPackage->sczRepairCondition, &fEvaluatedCondition);
        ExitOnFailure(hr, "Failed to evaluate repair condition.");

        repairCondition = fEvaluatedCondition ? BOOTSTRAPPER_PACKAGE_CONDITION_TRUE : BOOTSTRAPPER_PACKAGE_CONDITION_FALSE;
    }

    // Remember the default requested state so the engine doesn't get blamed for planning the wrong thing if the BA changes it.
    hr = PlanDefaultPackageRequestState(pPackage->type, pPackage->currentState, pPlan->action, installCondition, repairCondition, relationType, &pPackage->defaultRequested);
    ExitOnFailure(hr, "Failed to set default package state.");

    pPackage->requested = pPackage->defaultRequested;
    fBeginCalled = TRUE;

    hr = BACallbackOnPlanPackageBegin(pUX, pPackage->sczId, pPackage->currentState, pPackage->fCached, installCondition, repairCondition, &pPackage->requested, &pPackage->cacheType);
    ExitOnRootFailure(hr, "BA aborted plan package begin.");

    if (BURN_PACKAGE_TYPE_MSI == pPackage->type)
    {
        hr = MsiEnginePlanInitializePackage(pPackage, pPlan->action, pVariables, pUX);
        ExitOnFailure(hr, "Failed to initialize plan package: %ls", pPackage->sczId);
    }

LExit:
    if (fBeginCalled)
    {
        BACallbackOnPlanPackageComplete(pUX, pPackage->sczId, hr, pPackage->requested);
    }

    return hr;
}

static HRESULT ProcessPackage(
    __in BOOL fBundlePerMachine,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary
    )
{
    HRESULT hr = S_OK;
    BURN_ROLLBACK_BOUNDARY* pEffectiveRollbackBoundary = NULL;
    BOOL fBackward = BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action || BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == pPlan->action;

    pEffectiveRollbackBoundary = fBackward ? pPackage->pRollbackBoundaryBackward : pPackage->pRollbackBoundaryForward;
    hr = ProcessPackageRollbackBoundary(pPlan, pUX, pLog, pVariables, pEffectiveRollbackBoundary, ppRollbackBoundary);
    ExitOnFailure(hr, "Failed to process package rollback boundary.");

    if (BOOTSTRAPPER_ACTION_LAYOUT == pPlan->action)
    {
        if (BOOTSTRAPPER_REQUEST_STATE_NONE != pPackage->requested)
        {
            hr = PlanLayoutPackage(pPlan, pPackage, TRUE);
            ExitOnFailure(hr, "Failed to plan layout package.");
        }
    }
    else
    {
        if (BOOTSTRAPPER_REQUEST_STATE_NONE != pPackage->requested || pPackage->compatiblePackage.fRequested)
        {
            // If the package is in a requested state, plan it.
            hr = PlanExecutePackage(fBundlePerMachine, pUX, pPlan, pPackage, pLog, pVariables);
            ExitOnFailure(hr, "Failed to plan execute package.");
        }
        else
        {
            if (ForceCache(pPlan, pPackage))
            {
                hr = AddCachePackage(pPlan, pPackage, TRUE);
                ExitOnFailure(hr, "Failed to plan cache package.");

                if (pPackage->fPerMachine)
                {
                    pPlan->fPerMachine = TRUE;
                }
            }

            // Make sure the package is properly ref-counted even if no plan is requested.
            hr = PlanDependencyActions(fBundlePerMachine, pPlan, pPackage);
            ExitOnFailure(hr, "Failed to plan dependency actions for package: %ls", pPackage->sczId);
        }
    }

    // Add the checkpoint after each package and dependency registration action.
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute || BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback || BURN_DEPENDENCY_ACTION_NONE != pPackage->dependencyExecute)
    {
        hr = PlanExecuteCheckpoint(pPlan);
        ExitOnFailure(hr, "Failed to append execute checkpoint.");
    }

LExit:
    return hr;
}

static HRESULT ProcessPackageRollbackBoundary(
    __in BURN_PLAN* pPlan,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in_opt BURN_ROLLBACK_BOUNDARY* pEffectiveRollbackBoundary,
    __inout BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary
    )
{
    HRESULT hr = S_OK;

    // If the package marks the start of a rollback boundary, start a new one.
    if (pEffectiveRollbackBoundary)
    {
        // Complete previous rollback boundary.
        if (*ppRollbackBoundary)
        {
            hr = PlanRollbackBoundaryComplete(pPlan);
            ExitOnFailure(hr, "Failed to plan rollback boundary complete.");
        }

        // Start new rollback boundary.
        hr = PlanRollbackBoundaryBegin(pPlan, pUX, pLog, pVariables, pEffectiveRollbackBoundary);
        ExitOnFailure(hr, "Failed to plan rollback boundary begin.");

        *ppRollbackBoundary = pEffectiveRollbackBoundary;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanLayoutContainer(
    __in BURN_PLAN* pPlan,
    __in BURN_CONTAINER* pContainer
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_ACTION* pCacheAction = NULL;

    Assert(!pContainer->fPlanned);
    pContainer->fPlanned = TRUE;

    if (pPlan->sczLayoutDirectory)
    {
        if (!pContainer->fAttached)
        {
            hr = AppendCacheAction(pPlan, &pCacheAction);
            ExitOnFailure(hr, "Failed to append package start action.");

            pCacheAction->type = BURN_CACHE_ACTION_TYPE_CONTAINER;
            pCacheAction->container.pContainer = pContainer;

            // Acquire + Verify + Finalize
            pPlan->qwCacheSizeTotal += 3 * pContainer->qwFileSize;
        }
    }
    else
    {
        if (!pContainer->fActuallyAttached)
        {
            // Acquire
            pPlan->qwCacheSizeTotal += pContainer->qwFileSize;
        }
    }

    if (!pContainer->sczUnverifiedPath)
    {
        if (pContainer->fActuallyAttached)
        {
            hr = PathForCurrentProcess(&pContainer->sczUnverifiedPath, NULL);
            ExitOnFailure(hr, "Failed to get path for executing module as attached container working path.");
        }
        else
        {
            hr = CacheCalculateContainerWorkingPath(pPlan->pCache, pContainer, &pContainer->sczUnverifiedPath);
            ExitOnFailure(hr, "Failed to calculate unverified path for container.");
        }
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanLayoutPackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BOOL fVital
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_ACTION* pCacheAction = NULL;

    AssertSz(!pPlan->fEnabledForwardCompatibleBundle, "Passthrough packages must already be cached");

    hr = ProcessPayloadGroup(pPlan, &pPackage->payloads);
    ExitOnFailure(hr, "Failed to process payload group for package: %ls.", pPackage->sczId);

    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append package start action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_PACKAGE;
    pCacheAction->package.pPackage = pPackage;
    pPackage->fCacheVital = fVital;

    ++pPlan->cOverallProgressTicksTotal;

LExit:
    return hr;
}

extern "C" HRESULT PlanExecutePackage(
    __in BOOL fPerMachine,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_DISPLAY display = pPlan->pCommand->display;
    BOOL fRequestedCache = BOOTSTRAPPER_CACHE_TYPE_REMOVE < pPackage->cacheType && (BOOTSTRAPPER_REQUEST_STATE_CACHE == pPackage->requested || ForceCache(pPlan, pPackage));

    hr = CalculateExecuteActions(pPackage, pPlan->pActiveRollbackBoundary);
    ExitOnFailure(hr, "Failed to calculate plan actions for package: %ls", pPackage->sczId);

    // Calculate package states based on reference count and plan certain dependency actions prior to planning the package execute action.
    hr = DependencyPlanPackageBegin(fPerMachine, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to begin plan dependency actions for package: %ls", pPackage->sczId);

    pPackage->executeCacheType = fRequestedCache ? BURN_CACHE_PACKAGE_TYPE_REQUIRED : GetCachePackageType(pPackage, TRUE);
    pPackage->rollbackCacheType = GetCachePackageType(pPackage, FALSE);

    if (BURN_CACHE_PACKAGE_TYPE_NONE != pPackage->executeCacheType || BURN_CACHE_PACKAGE_TYPE_NONE != pPackage->rollbackCacheType)
    {
        hr = AddCachePackage(pPlan, pPackage, BURN_CACHE_PACKAGE_TYPE_REQUIRED == pPackage->executeCacheType);
        ExitOnFailure(hr, "Failed to plan cache package.");
    }

    // Add execute actions.
    switch (pPackage->type)
    {
    case BURN_PACKAGE_TYPE_BUNDLE:
        hr = BundlePackageEnginePlanAddPackage(pPackage, pPlan, pLog, pVariables);
        break;

    case BURN_PACKAGE_TYPE_EXE:
        hr = ExeEnginePlanAddPackage(pPackage, pPlan, pLog, pVariables);
        break;

    case BURN_PACKAGE_TYPE_MSI:
        hr = MsiEnginePlanAddPackage(display, pUserExperience, pPackage, pPlan, pLog, pVariables);
        break;

    case BURN_PACKAGE_TYPE_MSP:
        hr = MspEnginePlanAddPackage(display, pUserExperience, pPackage, pPlan, pLog, pVariables);
        break;

    case BURN_PACKAGE_TYPE_MSU:
        hr = MsuEnginePlanAddPackage(pPackage, pPlan, pLog, pVariables);
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Invalid package type.");
    }
    ExitOnFailure(hr, "Failed to add plan actions for package: %ls", pPackage->sczId);

    // Plan certain dependency actions after planning the package execute action.
    hr = DependencyPlanPackageComplete(pPackage, pPlan);
    ExitOnFailure(hr, "Failed to complete plan dependency actions for package: %ls", pPackage->sczId);

    // If we are going to take any action on this package, add progress for it.
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute || BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback)
    {
        PlannedExecutePackage(pPlan, pPackage);
    }

    // If we are going to take any action on the compatible package, add progress for it.
    if (pPackage->compatiblePackage.fRemove)
    {
        PlannedExecutePackage(pPlan, pPackage);
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanDefaultRelatedBundlePlanType(
    __in BOOTSTRAPPER_RELATION_TYPE relatedBundleRelationType,
    __in VERUTIL_VERSION* pRegistrationVersion,
    __in VERUTIL_VERSION* pRelatedBundleVersion,
    __inout BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE* pPlanRelationType
    )
{
    HRESULT hr = S_OK;
    int nCompareResult = 0;

    switch (relatedBundleRelationType)
    {
    case BOOTSTRAPPER_RELATION_UPGRADE:
        hr = VerCompareParsedVersions(pRegistrationVersion, pRelatedBundleVersion, &nCompareResult);
        ExitOnFailure(hr, "Failed to compare bundle version '%ls' to related bundle version '%ls'", pRegistrationVersion->sczVersion, pRelatedBundleVersion->sczVersion);

        if (nCompareResult < 0)
        {
            *pPlanRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DOWNGRADE;
        }
        else
        {
            *pPlanRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_UPGRADE;
        }
        break;
    case BOOTSTRAPPER_RELATION_ADDON:
        *pPlanRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_ADDON;
        break;
    case BOOTSTRAPPER_RELATION_PATCH:
        *pPlanRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_PATCH;
        break;
    case BOOTSTRAPPER_RELATION_DEPENDENT_ADDON:
        *pPlanRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_ADDON;
        break;
    case BOOTSTRAPPER_RELATION_DEPENDENT_PATCH:
        *pPlanRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_PATCH;
        break;
    case BOOTSTRAPPER_RELATION_DETECT:
        break;
    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Unexpected relation type encountered during plan: %d", relatedBundleRelationType);
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanDefaultRelatedBundleRequestState(
    __in BOOTSTRAPPER_RELATION_TYPE commandRelationType,
    __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE relatedBundleRelationType,
    __in BOOTSTRAPPER_ACTION action,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestState
    )
{
    HRESULT hr = S_OK;
    BOOL fUninstalling = BOOTSTRAPPER_ACTION_UNINSTALL == action || BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == action;

    // Never touch related bundles during Cache.
    if (BOOTSTRAPPER_ACTION_CACHE == action)
    {
        ExitFunction1(*pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE);
    }

    switch (relatedBundleRelationType)
    {
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_UPGRADE:
        if (BOOTSTRAPPER_RELATION_UPGRADE != commandRelationType && !fUninstalling)
        {
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_ABSENT;
        }
        break;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_PATCH: __fallthrough;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_ADDON:
        if (fUninstalling)
        {
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_ABSENT;
        }
        else if (BOOTSTRAPPER_ACTION_INSTALL == action || BOOTSTRAPPER_ACTION_MODIFY == action)
        {
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT;
        }
        else if (BOOTSTRAPPER_ACTION_REPAIR == action)
        {
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_REPAIR;
        }
        break;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_ADDON: __fallthrough;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_PATCH:
        // Automatically repair dependent bundles to restore missing
        // packages after uninstall unless we're being upgraded with the
        // assumption that upgrades are cumulative (as intended).
        if (BOOTSTRAPPER_RELATION_UPGRADE != commandRelationType && fUninstalling)
        {
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_REPAIR;
        }
        break;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DOWNGRADE: __fallthrough;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_NONE:
        break;
    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Unexpected plan relation type encountered during plan: %d", relatedBundleRelationType);
        break;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanRelatedBundlesInitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    BOOL fUninstalling = BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action || BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == pPlan->action;

    for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
    {
        BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgRelatedBundles + i;

        pRelatedBundle->defaultRequestedRestore = BOOTSTRAPPER_REQUEST_STATE_NONE;
        pRelatedBundle->requestedRestore = BOOTSTRAPPER_REQUEST_STATE_NONE;
        pRelatedBundle->restore = BOOTSTRAPPER_ACTION_STATE_NONE;
        pRelatedBundle->package.defaultRequested = BOOTSTRAPPER_REQUEST_STATE_NONE;
        pRelatedBundle->package.requested = BOOTSTRAPPER_REQUEST_STATE_NONE;
        pRelatedBundle->defaultPlanRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_NONE;
        pRelatedBundle->planRelationType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_NONE;
        pRelatedBundle->package.executeCacheType = BURN_CACHE_PACKAGE_TYPE_NONE;
        pRelatedBundle->package.rollbackCacheType = BURN_CACHE_PACKAGE_TYPE_NONE;

        // Determine the plan relation type even if later it is ignored due to the planned action, the command relation type, or the related bundle not being plannable.
        // This gives more information to the BA in case it wants to override default behavior.
        // Doing it during plan instead of Detect allows the BA to change its mind without having to go all the way through Detect again.
        hr = PlanDefaultRelatedBundlePlanType(pRelatedBundle->detectRelationType, pRegistration->pVersion, pRelatedBundle->pVersion, &pRelatedBundle->defaultPlanRelationType);
        ExitOnFailure(hr, "Failed to get default plan type for related bundle.");

        pRelatedBundle->planRelationType = pRelatedBundle->defaultPlanRelationType;

        hr = BACallbackOnPlanRelatedBundleType(pUserExperience, pRelatedBundle->package.sczId, &pRelatedBundle->planRelationType);
        ExitOnRootFailure(hr, "BA aborted plan related bundle type.");

        if (BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DOWNGRADE == pRelatedBundle->planRelationType &&
            pRelatedBundle->fPlannable && !fUninstalling && BOOTSTRAPPER_RELATION_UPGRADE != relationType)
        {
            if (!pPlan->fDowngrade)
            {
                pPlan->fDowngrade = TRUE;

                LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_DUE_TO_DOWNGRADE);
            }

            LogId(REPORT_VERBOSE, MSG_UPGRADE_BUNDLE_DOWNGRADE, pRelatedBundle->package.sczId, pRelatedBundle->pVersion->sczVersion);
        }
    }

    RelatedBundlesSortPlan(&pRegistration->relatedBundles);

LExit:
    return hr;
}

extern "C" HRESULT PlanRelatedBundlesBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    LPWSTR* rgsczAncestors = NULL;
    UINT cAncestors = 0;
    STRINGDICT_HANDLE sdAncestors = NULL;
    BOOL fUninstalling = BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action || BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == pPlan->action;

    if (pPlan->pInternalCommand->sczAncestors)
    {
        hr = StrSplitAllocArray(&rgsczAncestors, &cAncestors, pPlan->pInternalCommand->sczAncestors, L";");
        ExitOnFailure(hr, "Failed to create string array from ancestors.");

        hr = DictCreateStringListFromArray(&sdAncestors, rgsczAncestors, cAncestors, DICT_FLAG_CASEINSENSITIVE);
        ExitOnFailure(hr, "Failed to create dictionary from ancestors array.");
    }

    for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
    {
        BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgpPlanSortedRelatedBundles[i];

        if (!pRelatedBundle->fPlannable)
        {
            continue;
        }

        BOOL fDependent = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_ADDON == pRelatedBundle->planRelationType ||
                          BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_PATCH == pRelatedBundle->planRelationType;

        // Do not execute the same bundle twice.
        if (sdAncestors)
        {
            hr = DictKeyExists(sdAncestors, pRelatedBundle->package.sczId);
            if (SUCCEEDED(hr))
            {
                LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_RELATED_BUNDLE_SCHEDULED, pRelatedBundle->package.sczId);
                continue;
            }
            else if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to lookup the bundle code in the ancestors dictionary.");
            }
        }
        else if (fDependent && BOOTSTRAPPER_RELATION_NONE != relationType)
        {
            // Avoid repair loops for older bundles that do not handle ancestors.
            LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_RELATED_BUNDLE_DEPENDENT, pRelatedBundle->package.sczId, LoggingRelationTypeToString(relationType));
            continue;
        }

        // Pass along any ancestors and ourself to prevent infinite loops.
        pRelatedBundle->package.Bundle.wzAncestors = pRegistration->sczBundlePackageAncestors;
        pRelatedBundle->package.Bundle.wzEngineWorkingDirectory = pPlan->pInternalCommand->sczEngineWorkingDirectory;

        hr = PlanDefaultRelatedBundleRequestState(relationType, pRelatedBundle->planRelationType, pPlan->action, &pRelatedBundle->package.requested);
        ExitOnFailure(hr, "Failed to get default request state for related bundle.");

        pRelatedBundle->package.defaultRequested = pRelatedBundle->package.requested;

        hr = BACallbackOnPlanRelatedBundle(pUserExperience, pRelatedBundle->package.sczId, &pRelatedBundle->package.requested);
        ExitOnRootFailure(hr, "BA aborted plan related bundle.");

        // If uninstalling and the dependent related bundle may be executed, ignore its provider key to allow for downgrades with ref-counting.
        if (fUninstalling && fDependent && BOOTSTRAPPER_REQUEST_STATE_NONE != pRelatedBundle->package.requested)
        {
            if (0 < pRelatedBundle->package.cDependencyProviders)
            {
                // Bundles only support a single provider key.
                const BURN_DEPENDENCY_PROVIDER* pProvider = pRelatedBundle->package.rgDependencyProviders;

                hr = DepDependencyArrayAlloc(&pPlan->rgPlannedProviders, &pPlan->cPlannedProviders, pProvider->sczKey, pProvider->sczDisplayName);
                ExitOnFailure(hr, "Failed to add the package provider key \"%ls\" to the planned list.", pProvider->sczKey);
            }
        }
    }

LExit:
    ReleaseDict(sdAncestors);
    ReleaseStrArray(rgsczAncestors, cAncestors);

    return hr;
}

extern "C" HRESULT PlanRelatedBundlesComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in DWORD dwExecuteActionEarlyIndex
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczIgnoreDependencies = NULL;
    STRINGDICT_HANDLE sdProviderKeys = NULL;
    BOOL fExecutingAnyPackage = FALSE;
    BOOL fInstallingAnyPackage = FALSE;
    BOOL fUninstalling = BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action || BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == pPlan->action;

    // Get the list of dependencies to ignore to pass to related bundles.
    hr = DependencyAllocIgnoreDependencies(pPlan, &sczIgnoreDependencies);
    ExitOnFailure(hr, "Failed to get the list of dependencies to ignore.");

    hr = DictCreateStringList(&sdProviderKeys, pPlan->cExecuteActions, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create dictionary for planned packages.");

    for (DWORD i = 0; i < pPlan->cExecuteActions; ++i)
    {
        BOOTSTRAPPER_ACTION_STATE packageAction = BOOTSTRAPPER_ACTION_STATE_NONE;
        BURN_PACKAGE* pPackage = &pPlan->rgExecuteActions[i].relatedBundle.pRelatedBundle->package;
        BOOL fBundle = FALSE;

        switch (pPlan->rgExecuteActions[i].type)
        {
        case BURN_EXECUTE_ACTION_TYPE_BUNDLE_PACKAGE:
            packageAction = pPlan->rgExecuteActions[i].bundlePackage.action;
            pPackage = pPlan->rgExecuteActions[i].bundlePackage.pPackage;
            fBundle = TRUE;
            break;

        case BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE:
            packageAction = pPlan->rgExecuteActions[i].exePackage.action;
            pPackage = pPlan->rgExecuteActions[i].exePackage.pPackage;
            fBundle = pPackage->Exe.fBundle;
            break;

        case BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE:
            packageAction = pPlan->rgExecuteActions[i].msiPackage.action;
            break;

        case BURN_EXECUTE_ACTION_TYPE_MSP_TARGET:
            packageAction = pPlan->rgExecuteActions[i].mspTarget.action;
            break;

        case BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE:
            packageAction = pPlan->rgExecuteActions[i].msuPackage.action;
            break;
        }

        if (fBundle && BOOTSTRAPPER_ACTION_STATE_NONE != packageAction)
        {
            if (pPackage && pPackage->cDependencyProviders)
            {
                // Bundles only support a single provider key.
                const BURN_DEPENDENCY_PROVIDER* pProvider = pPackage->rgDependencyProviders;
                DictAddKey(sdProviderKeys, pProvider->sczKey);
            }
        }

        fExecutingAnyPackage |= BOOTSTRAPPER_ACTION_STATE_NONE != packageAction;
        fInstallingAnyPackage |= BOOTSTRAPPER_ACTION_STATE_INSTALL == packageAction || BOOTSTRAPPER_ACTION_STATE_MINOR_UPGRADE == packageAction;
    }

    for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
    {
        DWORD* pdwInsertIndex = NULL;
        BURN_RELATED_BUNDLE* pRelatedBundle = pRegistration->relatedBundles.rgpPlanSortedRelatedBundles[i];
        BOOL fDependent = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_ADDON == pRelatedBundle->planRelationType ||
                          BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_PATCH == pRelatedBundle->planRelationType;
        BOOL fAddonOrPatch = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_ADDON == pRelatedBundle->planRelationType ||
                             BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_PATCH == pRelatedBundle->planRelationType;

        if (!pRelatedBundle->fPlannable)
        {
            continue;
        }

        // Do not execute if a major upgrade to the related bundle is an embedded bundle (Provider keys are the same)
        if (0 < pRelatedBundle->package.cDependencyProviders)
        {
            // Bundles only support a single provider key.
            const BURN_DEPENDENCY_PROVIDER* pProvider = pRelatedBundle->package.rgDependencyProviders;
            hr = DictKeyExists(sdProviderKeys, pProvider->sczKey);
            if (E_NOTFOUND != hr)
            {
                ExitOnFailure(hr, "Failed to check the dictionary for a related bundle provider key: \"%ls\".", pProvider->sczKey);
                // Key found, so there is an embedded bundle with the same provider key that will be executed.  So this related bundle should not be added to the plan
                LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_RELATED_BUNDLE_EMBEDDED_BUNDLE_NEWER, pRelatedBundle->package.sczId, pProvider->sczKey);
                continue;
            }
            else
            {
                hr = S_OK;
            }
        }

        // For an uninstall, there is no need to repair dependent bundles if no packages are executing.
        if (!fExecutingAnyPackage && fDependent && BOOTSTRAPPER_REQUEST_STATE_REPAIR == pRelatedBundle->package.requested && fUninstalling)
        {
            pRelatedBundle->package.requested = BOOTSTRAPPER_REQUEST_STATE_NONE;
            LogId(REPORT_STANDARD, MSG_PLAN_SKIPPED_DEPENDENT_BUNDLE_REPAIR, pRelatedBundle->package.sczId);
        }

        if (fAddonOrPatch)
        {
            // Addon and patch bundles will be passed a list of dependencies to ignore for planning.
            hr = StrAllocString(&pRelatedBundle->package.Bundle.sczIgnoreDependencies, sczIgnoreDependencies, 0);
            ExitOnFailure(hr, "Failed to copy the list of dependencies to ignore.");

            // Uninstall addons and patches early in the chain, before other packages are uninstalled.
            if (fUninstalling)
            {
                pdwInsertIndex = &dwExecuteActionEarlyIndex;
            }
        }

        if (BOOTSTRAPPER_REQUEST_STATE_NONE != pRelatedBundle->package.requested)
        {
            hr = BundlePackageEnginePlanCalculatePackage(&pRelatedBundle->package);
            ExitOnFailure(hr, "Failed to calculate plan for related bundle: %ls", pRelatedBundle->package.sczId);

            // Calculate package states based on reference count for addon and patch related bundles.
            if (fAddonOrPatch)
            {
                hr = DependencyPlanPackageBegin(pRegistration->fPerMachine, &pRelatedBundle->package, pPlan);
                ExitOnFailure(hr, "Failed to begin plan dependency actions to  package: %ls", pRelatedBundle->package.sczId);

                // If uninstalling a related bundle, make sure the bundle is uninstalled after removing registration.
                if (pdwInsertIndex && fUninstalling)
                {
                    ++(*pdwInsertIndex);
                }
            }

            hr = BundlePackageEnginePlanAddRelatedBundle(pdwInsertIndex, pRelatedBundle, pPlan, pLog, pVariables);
            ExitOnFailure(hr, "Failed to add to plan related bundle: %ls", pRelatedBundle->package.sczId);

            // Calculate package states based on reference count for addon and patch related bundles.
            if (fAddonOrPatch)
            {
                hr = DependencyPlanPackageComplete(&pRelatedBundle->package, pPlan);
                ExitOnFailure(hr, "Failed to complete plan dependency actions for related bundle package: %ls", pRelatedBundle->package.sczId);
            }

            // If we are going to take any action on this package, add progress for it.
            if (BOOTSTRAPPER_ACTION_STATE_NONE != pRelatedBundle->package.execute || BOOTSTRAPPER_ACTION_STATE_NONE != pRelatedBundle->package.rollback)
            {
                PlannedExecutePackage(pPlan, &pRelatedBundle->package);
            }
        }
        else if (fAddonOrPatch)
        {
            // Make sure the package is properly ref-counted even if no plan is requested.
            hr = DependencyPlanPackageBegin(pRegistration->fPerMachine, &pRelatedBundle->package, pPlan);
            ExitOnFailure(hr, "Failed to begin plan dependency actions for related bundle package: %ls", pRelatedBundle->package.sczId);

            hr = DependencyPlanPackage(pdwInsertIndex, &pRelatedBundle->package, pPlan);
            ExitOnFailure(hr, "Failed to plan related bundle package provider actions.");

            hr = DependencyPlanPackageComplete(&pRelatedBundle->package, pPlan);
            ExitOnFailure(hr, "Failed to complete plan dependency actions for related bundle package: %ls", pRelatedBundle->package.sczId);
        }

        if (fInstallingAnyPackage && BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_UPGRADE == pRelatedBundle->planRelationType)
        {
            BURN_EXECUTE_ACTION* pAction = NULL;

            pRelatedBundle->defaultRequestedRestore = pRelatedBundle->requestedRestore = BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT;

            hr = BACallbackOnPlanRestoreRelatedBundle(pUserExperience, pRelatedBundle->package.sczId, &pRelatedBundle->requestedRestore);
            ExitOnRootFailure(hr, "BA aborted plan restore related bundle.");

            switch (pRelatedBundle->requestedRestore)
            {
            case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
                pRelatedBundle->restore = BOOTSTRAPPER_ACTION_STATE_REPAIR;
                break;
            case BOOTSTRAPPER_REQUEST_STATE_ABSENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_CACHE: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
                pRelatedBundle->restore = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
                break;
            case BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT:
                pRelatedBundle->restore = BOOTSTRAPPER_ACTION_STATE_INSTALL;
                break;
            default:
                pRelatedBundle->restore = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            }

            if (BOOTSTRAPPER_ACTION_STATE_NONE != pRelatedBundle->restore)
            {
                hr = AppendRestoreRelatedBundleAction(pPlan, &pAction);
                ExitOnFailure(hr, "Failed to append restore related bundle action to plan.");

                pAction->type = BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE;
                pAction->relatedBundle.pRelatedBundle = pRelatedBundle;
                pAction->relatedBundle.action = pRelatedBundle->restore;

                if (pRelatedBundle->package.Bundle.sczIgnoreDependencies)
                {
                    hr = StrAllocString(&pAction->relatedBundle.sczIgnoreDependencies, pRelatedBundle->package.Bundle.sczIgnoreDependencies, 0);
                    ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
                }

                if (pRelatedBundle->package.Bundle.wzAncestors)
                {
                    hr = StrAllocString(&pAction->relatedBundle.sczAncestors, pRelatedBundle->package.Bundle.wzAncestors, 0);
                    ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
                }

                if (pRelatedBundle->package.Bundle.wzEngineWorkingDirectory)
                {
                    hr = StrAllocString(&pAction->relatedBundle.sczEngineWorkingDirectory, pRelatedBundle->package.Bundle.wzEngineWorkingDirectory, 0);
                    ExitOnFailure(hr, "Failed to allocate the custom working directory.");
                }
            }
        }
    }

LExit:
    ReleaseDict(sdProviderKeys);
    ReleaseStr(sczIgnoreDependencies);

    return hr;
}

extern "C" HRESULT PlanFinalizeActions(
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;

    FinalizePatchActions(TRUE, pPlan->rgExecuteActions, pPlan->cExecuteActions);

    FinalizePatchActions(FALSE, pPlan->rgRollbackActions, pPlan->cRollbackActions);

    RemoveUnnecessaryActions(TRUE, pPlan->rgExecuteActions, pPlan->cExecuteActions);

    RemoveUnnecessaryActions(FALSE, pPlan->rgRollbackActions, pPlan->cRollbackActions);

    return hr;
}

extern "C" HRESULT PlanCleanPackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BOOL fPlanCleanPackage = FALSE;
    BURN_CLEAN_ACTION* pCleanAction = NULL;
    BOOL fUninstalling = BOOTSTRAPPER_ACTION_UNINSTALL == pPlan->action || BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL == pPlan->action;

    // The following is a complex set of logic that determines when a package should be cleaned from the cache.
    if (BOOTSTRAPPER_CACHE_TYPE_FORCE > pPackage->cacheType || fUninstalling)
    {
        // The following are all different reasons why the package should be cleaned from the cache.
        // The else-ifs are used to make the conditions easier to see (rather than have them combined
        // in one huge condition).
        if (BOOTSTRAPPER_CACHE_TYPE_KEEP > pPackage->cacheType)  // easy, package is not supposed to stay cached.
        {
            fPlanCleanPackage = TRUE;
        }
        else if ((BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT == pPackage->requested ||
                  BOOTSTRAPPER_REQUEST_STATE_ABSENT == pPackage->requested) &&      // requested to be removed and
                 BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pPackage->execute)          // actually being removed.
        {
            fPlanCleanPackage = TRUE;
        }
        else if ((BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT == pPackage->requested ||
                  BOOTSTRAPPER_REQUEST_STATE_ABSENT == pPackage->requested) &&      // requested to be removed but
                 BOOTSTRAPPER_ACTION_STATE_NONE == pPackage->execute &&             // execute is do nothing and
                 !pPackage->fDependencyManagerWasHere &&                            // dependency manager didn't change execute and
                 BOOTSTRAPPER_PACKAGE_STATE_PRESENT > pPackage->currentState)       // currently not installed.
        {
            fPlanCleanPackage = TRUE;
        }
        else if (fUninstalling &&                                                   // uninstalling and
                 BOOTSTRAPPER_REQUEST_STATE_NONE == pPackage->requested &&          // requested do nothing (aka: default) and
                 BOOTSTRAPPER_ACTION_STATE_NONE == pPackage->execute &&             // execute is still do nothing and
                 !pPackage->fDependencyManagerWasHere &&                            // dependency manager didn't change execute and
                 BOOTSTRAPPER_PACKAGE_STATE_PRESENT > pPackage->currentState)       // currently not installed.
        {
            fPlanCleanPackage = TRUE;
        }
    }

    if (fPlanCleanPackage)
    {
        hr = AppendCleanAction(pPlan, &pCleanAction);
        ExitOnFailure(hr, "Failed to append clean action to plan.");

        pCleanAction->type = BURN_CLEAN_ACTION_TYPE_PACKAGE;
        pCleanAction->pPackage = pPackage;

        pPackage->fPlannedUncache = TRUE;

        if (pPackage->fCanAffectRegistration)
        {
            pPackage->expectedCacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
        }
    }

    if (pPackage->compatiblePackage.fRemove)
    {
        hr = AppendCleanAction(pPlan, &pCleanAction);
        ExitOnFailure(hr, "Failed to append clean action to plan.");

        pCleanAction->type = BURN_CLEAN_ACTION_TYPE_COMPATIBLE_PACKAGE;
        pCleanAction->pPackage = pPackage;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanExecuteCacheSyncAndRollback(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;

    if (pPlan->fPlanPackageCacheRollback)
    {
        hr = PlanAppendRollbackAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_UNCACHE_PACKAGE;
        pAction->uncachePackage.pPackage = pPackage;
    }

    hr = PlanExecuteCheckpoint(pPlan);
    ExitOnFailure(hr, "Failed to append execute checkpoint for cache rollback.");

    hr = PlanAppendExecuteAction(pPlan, &pAction);
    ExitOnFailure(hr, "Failed to append wait action for caching.");

    pAction->type = BURN_EXECUTE_ACTION_TYPE_WAIT_CACHE_PACKAGE;
    pAction->waitCachePackage.pPackage = pPackage;

LExit:
    return hr;
}

extern "C" HRESULT PlanExecuteCheckpoint(
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;
    DWORD dwCheckpointId = GetNextCheckpointId(pPlan);

    // execute checkpoint
    hr = PlanAppendExecuteAction(pPlan, &pAction);
    ExitOnFailure(hr, "Failed to append execute action.");

    pAction->type = BURN_EXECUTE_ACTION_TYPE_CHECKPOINT;
    pAction->checkpoint.dwId = dwCheckpointId;
    pAction->checkpoint.pActiveRollbackBoundary = pPlan->pActiveRollbackBoundary;

    // rollback checkpoint
    hr = PlanAppendRollbackAction(pPlan, &pAction);
    ExitOnFailure(hr, "Failed to append rollback action.");

    pAction->type = BURN_EXECUTE_ACTION_TYPE_CHECKPOINT;
    pAction->checkpoint.dwId = dwCheckpointId;
    pAction->checkpoint.pActiveRollbackBoundary = pPlan->pActiveRollbackBoundary;

LExit:
    return hr;
}

extern "C" HRESULT PlanInsertExecuteAction(
    __in DWORD dwIndex,
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    )
{
    HRESULT hr = S_OK;

    hr = MemInsertIntoArray((void**)&pPlan->rgExecuteActions, dwIndex, 1, pPlan->cExecuteActions + 1, sizeof(BURN_EXECUTE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of execute actions.");

    *ppExecuteAction = pPlan->rgExecuteActions + dwIndex;
    ++pPlan->cExecuteActions;

LExit:
    return hr;
}

extern "C" HRESULT PlanInsertRollbackAction(
    __in DWORD dwIndex,
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppRollbackAction
    )
{
    HRESULT hr = S_OK;

    hr = MemInsertIntoArray((void**)&pPlan->rgRollbackActions, dwIndex, 1, pPlan->cRollbackActions + 1, sizeof(BURN_EXECUTE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of rollback actions.");

    *ppRollbackAction = pPlan->rgRollbackActions + dwIndex;
    ++pPlan->cRollbackActions;

LExit:
    return hr;
}

extern "C" HRESULT PlanAppendExecuteAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize((void**)&pPlan->rgExecuteActions, pPlan->cExecuteActions + 1, sizeof(BURN_EXECUTE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of execute actions.");

    *ppExecuteAction = pPlan->rgExecuteActions + pPlan->cExecuteActions;
    ++pPlan->cExecuteActions;

LExit:
    return hr;
}

extern "C" HRESULT PlanAppendRollbackAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppRollbackAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize((void**)&pPlan->rgRollbackActions, pPlan->cRollbackActions + 1, sizeof(BURN_EXECUTE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of rollback actions.");

    *ppRollbackAction = pPlan->rgRollbackActions + pPlan->cRollbackActions;
    ++pPlan->cRollbackActions;

LExit:
    return hr;
}

extern "C" HRESULT PlanRollbackBoundaryBegin(
    __in BURN_PLAN* pPlan,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pExecuteAction = NULL;

    AssertSz(!pPlan->pActiveRollbackBoundary, "PlanRollbackBoundaryBegin called without completing previous RollbackBoundary");
    pPlan->pActiveRollbackBoundary = pRollbackBoundary;

    // Add begin rollback boundary to execute plan.
    hr = PlanAppendExecuteAction(pPlan, &pExecuteAction);
    ExitOnFailure(hr, "Failed to append rollback boundary begin action.");

    pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_START;
    pExecuteAction->rollbackBoundary.pRollbackBoundary = pRollbackBoundary;

    // Add begin rollback boundary to rollback plan.
    hr = PlanAppendRollbackAction(pPlan, &pExecuteAction);
    ExitOnFailure(hr, "Failed to append rollback boundary begin action.");

    pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_START;
    pExecuteAction->rollbackBoundary.pRollbackBoundary = pRollbackBoundary;

    hr = BACallbackOnPlanRollbackBoundary(pUX, pRollbackBoundary->sczId, &pRollbackBoundary->fTransaction);
    ExitOnRootFailure(hr, "BA aborted plan rollback boundary.");

    // Only use MSI transaction if authored and the BA requested it.
    if (!pRollbackBoundary->fTransactionAuthored || !pRollbackBoundary->fTransaction)
    {
        pRollbackBoundary->fTransaction = FALSE;
    }
    else
    {
        LoggingSetTransactionVariable(pRollbackBoundary, NULL, pLog, pVariables); // ignore errors.

        // Add begin MSI transaction to execute plan.
        hr = PlanExecuteCheckpoint(pPlan);
        ExitOnFailure(hr, "Failed to append checkpoint before MSI transaction begin action.");

        hr = PlanAppendExecuteAction(pPlan, &pExecuteAction);
        ExitOnFailure(hr, "Failed to append MSI transaction begin action.");

        pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_BEGIN_MSI_TRANSACTION;
        pExecuteAction->msiTransaction.pRollbackBoundary = pRollbackBoundary;
    }

LExit:
    return hr;
}

extern "C" HRESULT PlanRollbackBoundaryComplete(
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pExecuteAction = NULL;
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundary = pPlan->pActiveRollbackBoundary;

    AssertSz(pRollbackBoundary, "PlanRollbackBoundaryComplete called without an active RollbackBoundary");

    if (pRollbackBoundary && pRollbackBoundary->fTransaction)
    {
        // Add commit MSI transaction to execute plan.
        hr = PlanAppendExecuteAction(pPlan, &pExecuteAction);
        ExitOnFailure(hr, "Failed to append MSI transaction commit action.");

        pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_COMMIT_MSI_TRANSACTION;
        pExecuteAction->msiTransaction.pRollbackBoundary = pRollbackBoundary;
    }

    pPlan->pActiveRollbackBoundary = NULL;

    // Add checkpoints.
    hr = PlanExecuteCheckpoint(pPlan);
    ExitOnFailure(hr, "Failed to append execute checkpoint for rollback boundary complete.");

    // Add complete rollback boundary to execute plan.
    hr = PlanAppendExecuteAction(pPlan, &pExecuteAction);
    ExitOnFailure(hr, "Failed to append rollback boundary complete action.");

    pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_END;

    // Add begin rollback boundary to rollback plan.
    hr = PlanAppendRollbackAction(pPlan, &pExecuteAction);
    ExitOnFailure(hr, "Failed to append rollback boundary complete action.");

    pExecuteAction->type = BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_END;

LExit:
    return hr;
}

/*******************************************************************
 PlanSetResumeCommand - Initializes resume command string

*******************************************************************/
extern "C" HRESULT PlanSetResumeCommand(
    __in BURN_PLAN* pPlan,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_LOGGING* pLog
    )
{
    HRESULT hr = S_OK;

    // build the resume command-line.
    hr = CoreCreateResumeCommandLine(&pRegistration->sczResumeCommandLine, pPlan, pLog);
    ExitOnFailure(hr, "Failed to create resume command-line.");

LExit:
    return hr;
}


// internal function definitions


static void PlannedExecutePackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    LoggingIncrementPackageSequence();

    ++pPlan->cExecutePackagesTotal;
    ++pPlan->cOverallProgressTicksTotal;

    // If package is per-machine and is being executed, flag the plan to be per-machine as well.
    if (pPackage->fPerMachine)
    {
        pPlan->fPerMachine = TRUE;
    }
}

static void UninitializeRegistrationAction(
    __in BURN_DEPENDENT_REGISTRATION_ACTION* pAction
    )
{
    ReleaseStr(pAction->sczDependentProviderKey);
    ReleaseStr(pAction->sczBundleCode);
    memset(pAction, 0, sizeof(BURN_DEPENDENT_REGISTRATION_ACTION));
}

static void UninitializeCacheAction(
    __in BURN_CACHE_ACTION* pCacheAction
    )
{
    switch (pCacheAction->type)
    {
    case BURN_CACHE_ACTION_TYPE_LAYOUT_BUNDLE:
        ReleaseStr(pCacheAction->bundleLayout.sczExecutableName);
        ReleaseStr(pCacheAction->bundleLayout.sczUnverifiedPath);
        break;
    }
}

static void ResetPlannedContainerState(
    __in BURN_CONTAINER* pContainer
    )
{
    pContainer->fPlanned = FALSE;
    pContainer->qwExtractSizeTotal = 0;
    pContainer->qwCommittedCacheProgress = 0;
    pContainer->qwCommittedExtractProgress = 0;
    pContainer->fExtracted = FALSE;
    pContainer->fFailedVerificationFromAcquisition = FALSE;
    ReleaseNullStr(pContainer->sczFailedLocalAcquisitionPath);
}

static void ResetPlannedPayloadsState(
    __in BURN_PAYLOADS* pPayloads
    )
{
    for (DWORD i = 0; i < pPayloads->cPayloads; ++i)
    {
        BURN_PAYLOAD* pPayload = pPayloads->rgPayloads + i;

        pPayload->cRemainingInstances = 0;
        pPayload->state = BURN_PAYLOAD_STATE_NONE;
        pPayload->fFailedVerificationFromAcquisition = FALSE;
        ReleaseFileHandle(pPayload->hLocalFile);
        ReleaseNullStr(pPayload->sczLocalFilePath);
        ReleaseNullStr(pPayload->sczFailedLocalAcquisitionPath);
    }
}

static void ResetPlannedPayloadGroupState(
    __in BURN_PAYLOAD_GROUP* pPayloadGroup
    )
{
    for (DWORD i = 0; i < pPayloadGroup->cItems; ++i)
    {
        BURN_PAYLOAD_GROUP_ITEM* pItem = pPayloadGroup->rgItems + i;

        pItem->fCached = FALSE;
        pItem->qwCommittedCacheProgress = 0;
    }
}

static void ResetPlannedPackageState(
    __in BURN_PACKAGE* pPackage
    )
{
    // Reset package state that is a result of planning.
    pPackage->cacheType = pPackage->authoredCacheType;
    pPackage->defaultRequested = BOOTSTRAPPER_REQUEST_STATE_NONE;
    pPackage->requested = BOOTSTRAPPER_REQUEST_STATE_NONE;
    pPackage->fCacheVital = FALSE;
    pPackage->fPlannedUncache = FALSE;
    pPackage->execute = BOOTSTRAPPER_ACTION_STATE_NONE;
    pPackage->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
    pPackage->fProviderExecute = FALSE;
    pPackage->fProviderRollback = FALSE;
    pPackage->dependencyExecute = BURN_DEPENDENCY_ACTION_NONE;
    pPackage->dependencyRollback = BURN_DEPENDENCY_ACTION_NONE;
    pPackage->fDependencyManagerWasHere = FALSE;
    pPackage->expectedCacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN;
    pPackage->expectedInstallRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN;
    pPackage->executeCacheType = BURN_CACHE_PACKAGE_TYPE_NONE;
    pPackage->rollbackCacheType = BURN_CACHE_PACKAGE_TYPE_NONE;
    ReleaseHandle(pPackage->hCacheEvent);

    ReleaseNullStr(pPackage->sczCacheFolder);

    if (BURN_PACKAGE_TYPE_MSI == pPackage->type)
    {
        for (DWORD i = 0; i < pPackage->Msi.cFeatures; ++i)
        {
            BURN_MSIFEATURE* pFeature = &pPackage->Msi.rgFeatures[i];

            pFeature->expectedState = BOOTSTRAPPER_FEATURE_STATE_UNKNOWN;
            pFeature->defaultRequested = BOOTSTRAPPER_FEATURE_STATE_UNKNOWN;
            pFeature->requested = BOOTSTRAPPER_FEATURE_STATE_UNKNOWN;
            pFeature->execute = BOOTSTRAPPER_FEATURE_ACTION_NONE;
            pFeature->rollback = BOOTSTRAPPER_FEATURE_ACTION_NONE;
        }

        for (DWORD i = 0; i < pPackage->Msi.cSlipstreamMspPackages; ++i)
        {
            BURN_SLIPSTREAM_MSP* pSlipstreamMsp = &pPackage->Msi.rgSlipstreamMsps[i];

            pSlipstreamMsp->execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            pSlipstreamMsp->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
        }
    }
    else if (BURN_PACKAGE_TYPE_MSP == pPackage->type && pPackage->Msp.rgTargetProducts)
    {
        for (DWORD i = 0; i < pPackage->Msp.cTargetProductCodes; ++i)
        {
            BURN_MSPTARGETPRODUCT* pTargetProduct = &pPackage->Msp.rgTargetProducts[i];

            pTargetProduct->defaultRequested = BOOTSTRAPPER_REQUEST_STATE_NONE;
            pTargetProduct->requested = BOOTSTRAPPER_REQUEST_STATE_NONE;
            pTargetProduct->execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            pTargetProduct->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
            pTargetProduct->executeSkip = BURN_PATCH_SKIP_STATE_NONE;
            pTargetProduct->rollbackSkip = BURN_PATCH_SKIP_STATE_NONE;
        }
    }

    for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
    {
        BURN_DEPENDENCY_PROVIDER* pProvider = &pPackage->rgDependencyProviders[i];

        pProvider->dependentExecute = BURN_DEPENDENCY_ACTION_NONE;
        pProvider->dependentRollback = BURN_DEPENDENCY_ACTION_NONE;
        pProvider->providerExecute = BURN_DEPENDENCY_ACTION_NONE;
        pProvider->providerRollback = BURN_DEPENDENCY_ACTION_NONE;
    }

    ResetPlannedPayloadGroupState(&pPackage->payloads);
}

static void ResetPlannedRollbackBoundaryState(
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary
    )
{
    pRollbackBoundary->fActiveTransaction = FALSE;
    pRollbackBoundary->fTransaction = pRollbackBoundary->fTransactionAuthored;
    ReleaseNullStr(pRollbackBoundary->sczLogPath);
}

static HRESULT GetActionDefaultRequestState(
    __in BOOTSTRAPPER_ACTION action,
    __in BOOTSTRAPPER_PACKAGE_STATE currentState,
    __out BOOTSTRAPPER_REQUEST_STATE* pRequestState
    )
{
    HRESULT hr = S_OK;

    switch (action)
    {
    case BOOTSTRAPPER_ACTION_INSTALL:
        *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
        break;

    case BOOTSTRAPPER_ACTION_REPAIR:
        *pRequestState = BOOTSTRAPPER_REQUEST_STATE_REPAIR;
        break;

    case BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL: __fallthrough;
    case BOOTSTRAPPER_ACTION_UNINSTALL:
        *pRequestState = BOOTSTRAPPER_REQUEST_STATE_ABSENT;
        break;

    case BOOTSTRAPPER_ACTION_MODIFY:
        switch (currentState)
        {
        case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_ABSENT;
            break;

        case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
            break;

        default:
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
            break;
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid action state.");
    }

LExit:
    return hr;
}

static HRESULT AddRegistrationAction(
    __in BURN_PLAN* pPlan,
    __in BURN_DEPENDENT_REGISTRATION_ACTION_TYPE type,
    __in_z LPCWSTR wzDependentProviderKey,
    __in_z LPCWSTR wzOwnerBundleCode
    )
{
    HRESULT hr = S_OK;
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE rollbackType = (BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER == type) ? BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_UNREGISTER : BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER;
    BURN_DEPENDENT_REGISTRATION_ACTION* pAction = NULL;

    // Create forward registration action.
    hr = MemEnsureArraySize((void**)&pPlan->rgRegistrationActions, pPlan->cRegistrationActions + 1, sizeof(BURN_DEPENDENT_REGISTRATION_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of registration actions.");

    pAction = pPlan->rgRegistrationActions + pPlan->cRegistrationActions;
    ++pPlan->cRegistrationActions;

    pAction->type = type;

    hr = StrAllocString(&pAction->sczBundleCode, wzOwnerBundleCode, 0);
    ExitOnFailure(hr, "Failed to copy owner bundle to registration action.");

    hr = StrAllocString(&pAction->sczDependentProviderKey, wzDependentProviderKey, 0);
    ExitOnFailure(hr, "Failed to copy dependent provider key to registration action.");

    // Create rollback registration action.
    hr = MemEnsureArraySize((void**)&pPlan->rgRollbackRegistrationActions, pPlan->cRollbackRegistrationActions + 1, sizeof(BURN_DEPENDENT_REGISTRATION_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of rollback registration actions.");

    pAction = pPlan->rgRollbackRegistrationActions + pPlan->cRollbackRegistrationActions;
    ++pPlan->cRollbackRegistrationActions;

    pAction->type = rollbackType;

    hr = StrAllocString(&pAction->sczBundleCode, wzOwnerBundleCode, 0);
    ExitOnFailure(hr, "Failed to copy owner bundle to registration action.");

    hr = StrAllocString(&pAction->sczDependentProviderKey, wzDependentProviderKey, 0);
    ExitOnFailure(hr, "Failed to copy dependent provider key to rollback registration action.");

LExit:
    return hr;
}

static HRESULT AddCachePackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BOOL fVital
    )
{
    HRESULT hr = S_OK;

    // If this is an MSI package with slipstream MSPs, ensure the MSPs are cached first.
    // TODO: Slipstream packages are not accounted for when caching the MSI package is optional.
    if (BURN_PACKAGE_TYPE_MSI == pPackage->type && 0 < pPackage->Msi.cSlipstreamMspPackages && fVital)
    {
        hr = AddCacheSlipstreamMsps(pPlan, pPackage);
        ExitOnFailure(hr, "Failed to plan slipstream patches for package.");
    }

    hr = AddCachePackageHelper(pPlan, pPackage, fVital);
    ExitOnFailure(hr, "Failed to plan cache package.");

LExit:
    return hr;
}

static HRESULT AddCachePackageHelper(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BOOL fVital
    )
{
    AssertSz(pPackage->sczCacheId && *pPackage->sczCacheId, "AddCachePackageHelper() expects the package to have a cache id.");

    HRESULT hr = S_OK;
    BURN_CACHE_ACTION* pCacheAction = NULL;
    DWORD dwCheckpoint = 0;

    if (pPlan->fEnabledForwardCompatibleBundle) // Passthrough packages must already be cached.
    {
        ExitFunction();
    }

    if (pPackage->hCacheEvent) // Only cache the package once.
    {
        ExitFunction();
    }

    pPackage->hCacheEvent = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(pPackage->hCacheEvent, hr, "Failed to create syncpoint event.");

    // Cache checkpoints happen before the package is cached because downloading packages'
    // payloads will not roll themselves back the way installation packages rollback on
    // failure automatically.
    dwCheckpoint = GetNextCheckpointId(pPlan);

    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append checkpoint before package start action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_CHECKPOINT;
    pCacheAction->checkpoint.dwId = dwCheckpoint;

    if (pPlan->fPlanPackageCacheRollback)
    {
        // Create a package cache rollback action *before* the checkpoint.
        hr = AppendRollbackCacheAction(pPlan, &pCacheAction);
        ExitOnFailure(hr, "Failed to append rollback cache action.");

        pCacheAction->type = BURN_CACHE_ACTION_TYPE_ROLLBACK_PACKAGE;
        pCacheAction->rollbackPackage.pPackage = pPackage;

        hr = AppendRollbackCacheAction(pPlan, &pCacheAction);
        ExitOnFailure(hr, "Failed to append rollback cache action.");

        pCacheAction->type = BURN_CACHE_ACTION_TYPE_CHECKPOINT;
        pCacheAction->checkpoint.dwId = dwCheckpoint;
    }

    hr = PlanLayoutPackage(pPlan, pPackage, fVital);
    ExitOnFailure(hr, "Failed to plan cache for package.");

    // Create syncpoint action.
    hr = AppendCacheAction(pPlan, &pCacheAction);
    ExitOnFailure(hr, "Failed to append cache action.");

    pCacheAction->type = BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT;
    pCacheAction->syncpoint.pPackage = pPackage;

    hr = PlanExecuteCacheSyncAndRollback(pPlan, pPackage);
    ExitOnFailure(hr, "Failed to plan package cache syncpoint");

    if (pPackage->fCanAffectRegistration)
    {
        pPackage->expectedCacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
    }

LExit:
    return hr;
}

static HRESULT AddCacheSlipstreamMsps(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;

    AssertSz(BURN_PACKAGE_TYPE_MSI == pPackage->type, "Only MSI packages can have slipstream patches.");

    for (DWORD i = 0; i < pPackage->Msi.cSlipstreamMspPackages; ++i)
    {
        BURN_PACKAGE* pMspPackage = pPackage->Msi.rgSlipstreamMsps[i].pMspPackage;
        AssertSz(BURN_PACKAGE_TYPE_MSP == pMspPackage->type, "Only MSP packages can be slipstream patches.");

        hr = AddCachePackageHelper(pPlan, pMspPackage, TRUE);
        ExitOnFailure(hr, "Failed to plan slipstream MSP: %ls", pMspPackage->sczId);
    }

LExit:
    return hr;
}

static DWORD GetNextCheckpointId(
    __in BURN_PLAN* pPlan
    )
{
    return ++pPlan->dwNextCheckpointId;
}

static HRESULT AppendCacheAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CACHE_ACTION** ppCacheAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPlan->rgCacheActions), pPlan->cCacheActions + 1, sizeof(BURN_CACHE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of cache actions.");

    *ppCacheAction = pPlan->rgCacheActions + pPlan->cCacheActions;
    ++pPlan->cCacheActions;

LExit:
    return hr;
}

static HRESULT AppendRollbackCacheAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CACHE_ACTION** ppCacheAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPlan->rgRollbackCacheActions), pPlan->cRollbackCacheActions + 1, sizeof(BURN_CACHE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of rollback cache actions.");

    *ppCacheAction = pPlan->rgRollbackCacheActions + pPlan->cRollbackCacheActions;
    ++pPlan->cRollbackCacheActions;

LExit:
    return hr;
}

static HRESULT AppendCleanAction(
    __in BURN_PLAN* pPlan,
    __out BURN_CLEAN_ACTION** ppCleanAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(&pPlan->rgCleanActions), pPlan->cCleanActions, 1, sizeof(BURN_CLEAN_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of clean actions.");


    *ppCleanAction = pPlan->rgCleanActions + pPlan->cCleanActions;
    ++pPlan->cCleanActions;

LExit:
    return hr;
}

static HRESULT AppendRestoreRelatedBundleAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(&pPlan->rgRestoreRelatedBundleActions), pPlan->cRestoreRelatedBundleActions, 1, sizeof(BURN_EXECUTE_ACTION), 5);
    ExitOnFailure(hr, "Failed to grow plan's array of restore related bundle actions.");

    *ppExecuteAction = pPlan->rgRestoreRelatedBundleActions + pPlan->cRestoreRelatedBundleActions;
    ++pPlan->cRestoreRelatedBundleActions;

LExit:
    return hr;
}

static HRESULT ProcessPayloadGroup(
    __in BURN_PLAN* pPlan,
    __in BURN_PAYLOAD_GROUP* pPayloadGroup
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pPayloadGroup->cItems; ++i)
    {
        BURN_PAYLOAD_GROUP_ITEM* pItem = pPayloadGroup->rgItems + i;
        BURN_PAYLOAD* pPayload = pItem->pPayload;

        pPayload->cRemainingInstances += 1;

        if (pPayload->pContainer && !pPayload->pContainer->fPlanned)
        {
            hr = PlanLayoutContainer(pPlan, pPayload->pContainer);
            ExitOnFailure(hr, "Failed to plan container: %ls", pPayload->pContainer->sczId);
        }

        if (!pPlan->sczLayoutDirectory || !pPayload->pContainer)
        {
            // Acquire + Verify + Finalize
            pPlan->qwCacheSizeTotal += 3 * pPayload->qwFileSize;

            if (!pPlan->sczLayoutDirectory)
            {
                // Staging
                pPlan->qwCacheSizeTotal += pPayload->qwFileSize;
            }
        }

        if (!pPlan->sczLayoutDirectory && pPayload->pContainer && 1 == pPayload->cRemainingInstances)
        {
            // Extract
            pPlan->qwCacheSizeTotal += pPayload->qwFileSize;
            pPayload->pContainer->qwExtractSizeTotal += pPayload->qwFileSize;
        }

        if (!pPayload->sczUnverifiedPath)
        {
            hr = CacheCalculatePayloadWorkingPath(pPlan->pCache, pPayload, &pPayload->sczUnverifiedPath);
            ExitOnFailure(hr, "Failed to calculate unverified path for payload.");
        }
    }

LExit:
    return hr;
}

static void RemoveUnnecessaryActions(
    __in BOOL fExecute,
    __in BURN_EXECUTE_ACTION* rgActions,
    __in DWORD cActions
    )
{
    LPCSTR szExecuteOrRollback = fExecute ? "execute" : "rollback";

    for (DWORD i = 0; i < cActions; ++i)
    {
        BURN_EXECUTE_ACTION* pAction = rgActions + i;

        if (BURN_EXECUTE_ACTION_TYPE_MSP_TARGET == pAction->type && pAction->mspTarget.pChainedTargetPackage)
        {
            BURN_MSPTARGETPRODUCT* pFirstTargetProduct = pAction->mspTarget.rgOrderedPatches->pTargetProduct;
            BURN_PATCH_SKIP_STATE skipState = fExecute ? pFirstTargetProduct->executeSkip : pFirstTargetProduct->rollbackSkip;
            BOOTSTRAPPER_ACTION_STATE chainedTargetPackageAction = fExecute ? pAction->mspTarget.pChainedTargetPackage->execute : pAction->mspTarget.pChainedTargetPackage->rollback;

            switch (skipState)
            {
            case BURN_PATCH_SKIP_STATE_TARGET_UNINSTALL:
                pAction->fDeleted = TRUE;
                LogId(REPORT_STANDARD, MSG_PLAN_SKIP_PATCH_ACTION, pAction->mspTarget.pPackage->sczId, LoggingActionStateToString(pAction->mspTarget.action), pAction->mspTarget.pChainedTargetPackage->sczId, LoggingActionStateToString(chainedTargetPackageAction), szExecuteOrRollback);
                break;
            case BURN_PATCH_SKIP_STATE_SLIPSTREAM:
                pAction->fDeleted = TRUE;
                LogId(REPORT_STANDARD, MSG_PLAN_SKIP_SLIPSTREAM_ACTION, pAction->mspTarget.pPackage->sczId, LoggingActionStateToString(pAction->mspTarget.action), pAction->mspTarget.pChainedTargetPackage->sczId, LoggingActionStateToString(chainedTargetPackageAction), szExecuteOrRollback);
                break;
            }
        }
    }
}

static void FinalizePatchActions(
    __in BOOL fExecute,
    __in BURN_EXECUTE_ACTION* rgActions,
    __in DWORD cActions
    )
{
    for (DWORD i = 0; i < cActions; ++i)
    {
        BURN_EXECUTE_ACTION* pAction = rgActions + i;

        if (BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE == pAction->type)
        {
            BURN_PACKAGE* pPackage = pAction->msiPackage.pPackage;
            AssertSz(BOOTSTRAPPER_ACTION_STATE_NONE < pAction->msiPackage.action, "Planned execute MSI action to do nothing");

            if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pAction->msiPackage.action)
            {
                // If we are uninstalling the MSI, we must skip all the patches.
                for (DWORD j = 0; j < pPackage->Msi.cChainedPatches; ++j)
                {
                    BURN_CHAINED_PATCH* pChainedPatch = pPackage->Msi.rgChainedPatches + j;
                    BURN_MSPTARGETPRODUCT* pTargetProduct = pChainedPatch->pMspPackage->Msp.rgTargetProducts + pChainedPatch->dwMspTargetProductIndex;

                    if (fExecute)
                    {
                        pTargetProduct->execute = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
                        pTargetProduct->executeSkip = BURN_PATCH_SKIP_STATE_TARGET_UNINSTALL;
                    }
                    else
                    {
                        pTargetProduct->rollback = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
                        pTargetProduct->rollbackSkip = BURN_PATCH_SKIP_STATE_TARGET_UNINSTALL;
                    }
                }
            }
            else
            {
                // If the slipstream target is being installed or upgraded (not uninstalled or repaired) then we will slipstream so skip
                // the patch's standalone action. Also, if the slipstream target is being repaired and the patch is being
                // repaired, skip this operation since it will be redundant.
                //
                // The primary goal here is to ensure that a slipstream patch that is yet not installed is installed even if the MSI
                // is already on the machine. The slipstream must be installed standalone if the MSI is being repaired.
                for (DWORD j = 0; j < pPackage->Msi.cSlipstreamMspPackages; ++j)
                {
                    BURN_SLIPSTREAM_MSP* pSlipstreamMsp = pPackage->Msi.rgSlipstreamMsps + j;
                    BURN_CHAINED_PATCH* pChainedPatch = pPackage->Msi.rgChainedPatches + pSlipstreamMsp->dwMsiChainedPatchIndex;
                    BURN_MSPTARGETPRODUCT* pTargetProduct = pSlipstreamMsp->pMspPackage->Msp.rgTargetProducts + pChainedPatch->dwMspTargetProductIndex;
                    BOOTSTRAPPER_ACTION_STATE action = fExecute ? pTargetProduct->execute : pTargetProduct->rollback;
                    BOOL fSlipstream = BOOTSTRAPPER_ACTION_STATE_UNINSTALL < action &&
                                       (BOOTSTRAPPER_ACTION_STATE_REPAIR != pAction->msiPackage.action || BOOTSTRAPPER_ACTION_STATE_REPAIR == action);

                    if (fSlipstream)
                    {
                        if (fExecute)
                        {
                            pSlipstreamMsp->execute = action;
                            pTargetProduct->executeSkip = BURN_PATCH_SKIP_STATE_SLIPSTREAM;
                        }
                        else
                        {
                            pSlipstreamMsp->rollback = action;
                            pTargetProduct->rollbackSkip = BURN_PATCH_SKIP_STATE_SLIPSTREAM;
                        }
                    }
                }
            }
        }
    }
}

static void CalculateExpectedRegistrationStates(
    __in BURN_PACKAGE* rgPackages,
    __in DWORD cPackages
    )
{
    for (DWORD i = 0; i < cPackages; ++i)
    {
        BURN_PACKAGE* pPackage = rgPackages + i;

        // MspPackages can have actions throughout the plan, so the plan needed to be finalized before anything could be calculated.
        if (BURN_PACKAGE_TYPE_MSP == pPackage->type && !pPackage->fDependencyManagerWasHere)
        {
            pPackage->execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            pPackage->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;

            for (DWORD j = 0; j < pPackage->Msp.cTargetProductCodes; ++j)
            {
                BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + j;

                // The highest aggregate action state found will be used.
                if (pPackage->execute < pTargetProduct->execute)
                {
                    pPackage->execute = pTargetProduct->execute;
                }

                if (pPackage->rollback < pTargetProduct->rollback)
                {
                    pPackage->rollback = pTargetProduct->rollback;
                }
            }
        }

        if (pPackage->fCanAffectRegistration)
        {
            if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL < pPackage->execute)
            {
                pPackage->expectedInstallRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
            }
            else if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pPackage->execute)
            {
                pPackage->expectedInstallRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
            }

            if (BURN_DEPENDENCY_ACTION_REGISTER == pPackage->dependencyExecute)
            {
                if (BURN_PACKAGE_REGISTRATION_STATE_IGNORED == pPackage->expectedCacheRegistrationState)
                {
                    pPackage->expectedCacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
                }
                if (BURN_PACKAGE_REGISTRATION_STATE_IGNORED == pPackage->expectedInstallRegistrationState)
                {
                    pPackage->expectedInstallRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
                }
            }
            else if (BURN_DEPENDENCY_ACTION_UNREGISTER == pPackage->dependencyExecute)
            {
                if (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pPackage->expectedCacheRegistrationState)
                {
                    pPackage->expectedCacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_IGNORED;
                }
                if (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pPackage->expectedInstallRegistrationState)
                {
                    pPackage->expectedInstallRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_IGNORED;
                }
            }
        }
    }
}

static HRESULT PlanDependencyActions(
    __in BOOL fBundlePerMachine,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;

    hr = DependencyPlanPackageBegin(fBundlePerMachine, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to begin plan dependency actions for package: %ls", pPackage->sczId);

    hr = DependencyPlanPackage(NULL, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to plan package dependency actions.");

    hr = DependencyPlanPackageComplete(pPackage, pPlan);
    ExitOnFailure(hr, "Failed to complete plan dependency actions for package: %ls", pPackage->sczId);

LExit:
    return hr;
}

static HRESULT CalculateExecuteActions(
    __in BURN_PACKAGE* pPackage,
    __in_opt BURN_ROLLBACK_BOUNDARY* pActiveRollbackBoundary
    )
{
    HRESULT hr = S_OK;
    BOOL fInsideMsiTransaction = pActiveRollbackBoundary && pActiveRollbackBoundary->fTransaction;

    // Calculate execute actions.
    switch (pPackage->type)
    {
    case BURN_PACKAGE_TYPE_BUNDLE:
        hr = BundlePackageEnginePlanCalculatePackage(pPackage);
        break;

    case BURN_PACKAGE_TYPE_EXE:
        hr = ExeEnginePlanCalculatePackage(pPackage);
        break;

    case BURN_PACKAGE_TYPE_MSI:
        hr = MsiEnginePlanCalculatePackage(pPackage, fInsideMsiTransaction);
        break;

    case BURN_PACKAGE_TYPE_MSP:
        hr = MspEnginePlanCalculatePackage(pPackage, fInsideMsiTransaction);
        break;

    case BURN_PACKAGE_TYPE_MSU:
        hr = MsuEnginePlanCalculatePackage(pPackage);
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Invalid package type.");
    }

    pPackage->compatiblePackage.fRemove = pPackage->compatiblePackage.fPlannable && pPackage->compatiblePackage.fRequested;

LExit:
    return hr;
}

static BURN_CACHE_PACKAGE_TYPE GetCachePackageType(
    __in BURN_PACKAGE* pPackage,
    __in BOOL fExecute
    )
{
    BURN_CACHE_PACKAGE_TYPE cachePackageType = BURN_CACHE_PACKAGE_TYPE_NONE;

    switch (fExecute ? pPackage->execute : pPackage->rollback)
    {
    case BOOTSTRAPPER_ACTION_STATE_NONE:
        break;
    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        if (BURN_PACKAGE_TYPE_EXE == pPackage->type && BURN_EXE_DETECTION_TYPE_ARP != pPackage->Exe.detectionType)
        {
            // non-ArpEntry Exe packages require the package for all operations (even uninstall).
            cachePackageType = BURN_CACHE_PACKAGE_TYPE_REQUIRED;
        }
        else if (BURN_PACKAGE_TYPE_BUNDLE == pPackage->type)
        {
            // Bundle packages prefer the cache but can fallback to the ARP registration.
            cachePackageType = BURN_CACHE_PACKAGE_TYPE_OPTIONAL;
        }
        else
        {
            // The other package types can uninstall without the original package.
            cachePackageType = BURN_CACHE_PACKAGE_TYPE_NONE;
        }
        break;
    case BOOTSTRAPPER_ACTION_STATE_INSTALL: __fallthrough;
    case BOOTSTRAPPER_ACTION_STATE_MODIFY: __fallthrough;
    case BOOTSTRAPPER_ACTION_STATE_REPAIR: __fallthrough;
    case BOOTSTRAPPER_ACTION_STATE_MINOR_UPGRADE: __fallthrough;
    default:
        // TODO: bundles could theoretically use package cache.
        cachePackageType = BURN_CACHE_PACKAGE_TYPE_REQUIRED;
        break;
    }

    return cachePackageType;
}

static BOOL ForceCache(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    )
{
    switch (pPackage->cacheType)
    {
    case BOOTSTRAPPER_CACHE_TYPE_KEEP:
        // During actions that are expected to have source media available,
        // all packages that have cacheType set to keep should be cached if the package is going to be present.
        return (BOOTSTRAPPER_ACTION_CACHE == pPlan->action || BOOTSTRAPPER_ACTION_INSTALL == pPlan->action) &&
               BOOTSTRAPPER_REQUEST_STATE_CACHE < pPackage->requested;
    case BOOTSTRAPPER_CACHE_TYPE_FORCE:
        // All packages that have cacheType set to force should be cached if the bundle is going to be present.
        return BOOTSTRAPPER_ACTION_UNINSTALL != pPlan->action && BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL != pPlan->action;
    default:
        return FALSE;
    }
}

static void DependentRegistrationActionLog(
    __in DWORD iAction,
    __in BURN_DEPENDENT_REGISTRATION_ACTION* pAction,
    __in BOOL fRollback
    )
{
    LPCWSTR wzBase = fRollback ? L"   Rollback dependent registration" : L"   Dependent registration";
    LPCWSTR wzType = NULL;

    switch (pAction->type)
    {
    case BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER:
        wzType = L"REGISTER";
        break;

    case BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_UNREGISTER:
        wzType = L"UNREGISTER";
        break;

    default:
        AssertSz(FALSE, "Unknown cache action type.");
        break;
    }

    if (wzType)
    {
        LogStringLine(PlanDumpLevel, "%ls action[%u]: %ls bundle code: %ls, provider key: %ls", wzBase, iAction, wzType, pAction->sczBundleCode, pAction->sczDependentProviderKey);
    }
}

static void CacheActionLog(
    __in DWORD iAction,
    __in BURN_CACHE_ACTION* pAction,
    __in BOOL fRollback
    )
{
    LPCWSTR wzBase = fRollback ? L"   Rollback cache" : L"   Cache";
    switch (pAction->type)
    {
    case BURN_CACHE_ACTION_TYPE_CHECKPOINT:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: CHECKPOINT id: %u", wzBase, iAction, pAction->checkpoint.dwId);
        break;

    case BURN_CACHE_ACTION_TYPE_LAYOUT_BUNDLE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: LAYOUT_BUNDLE working path: %ls, exe name: %ls", wzBase, iAction, pAction->bundleLayout.sczUnverifiedPath, pAction->bundleLayout.sczExecutableName);
        break;

    case BURN_CACHE_ACTION_TYPE_CONTAINER:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: CONTAINER container id: %ls, working path: %ls", wzBase, iAction, pAction->container.pContainer->sczId, pAction->container.pContainer->sczUnverifiedPath);
        break;

    case BURN_CACHE_ACTION_TYPE_PACKAGE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: PACKAGE id: %ls, vital: %hs, execute cache type: %hs, rollback cache type: %hs", wzBase, iAction, pAction->package.pPackage->sczId, LoggingBoolToString(pAction->package.pPackage->fCacheVital), LoggingCachePackageTypeToString(pAction->package.pPackage->executeCacheType), LoggingCachePackageTypeToString(pAction->package.pPackage->rollbackCacheType));
        break;

    case BURN_CACHE_ACTION_TYPE_ROLLBACK_PACKAGE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: ROLLBACK_PACKAGE id: %ls", wzBase, iAction, pAction->rollbackPackage.pPackage->sczId);
        break;

    case BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: SIGNAL_SYNCPOINT package id: %ls, event handle: 0x%p", wzBase, iAction, pAction->syncpoint.pPackage->sczId, pAction->syncpoint.pPackage->hCacheEvent);
        break;

    default:
        AssertSz(FALSE, "Unknown cache action type.");
        break;
    }
}

static void ExecuteActionLog(
    __in DWORD iAction,
    __in BURN_EXECUTE_ACTION* pAction,
    __in BOOL fRollback
    )
{
    LPCWSTR wzBase = fRollback ? L"   Rollback" : L"   Execute";
    switch (pAction->type)
    {
    case BURN_EXECUTE_ACTION_TYPE_CHECKPOINT:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: CHECKPOINT id: %u, msi transaction id: %ls", wzBase, iAction, pAction->checkpoint.dwId, pAction->checkpoint.pActiveRollbackBoundary && pAction->checkpoint.pActiveRollbackBoundary->fTransaction ? pAction->checkpoint.pActiveRollbackBoundary->sczId : L"(none)");
        break;

    case BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: PACKAGE_PROVIDER package id: %ls", wzBase, iAction, pAction->packageProvider.pPackage->sczId);
        for (DWORD j = 0; j < pAction->packageProvider.pPackage->cDependencyProviders; ++j)
        {
            const BURN_DEPENDENCY_PROVIDER* pProvider = pAction->packageProvider.pPackage->rgDependencyProviders + j;
            LogStringLine(PlanDumpLevel, "      Provider[%u]: key: %ls, action: %hs", j, pProvider->sczKey, LoggingDependencyActionToString(fRollback ? pProvider->providerRollback : pProvider->providerExecute));
        }
        break;

    case BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: PACKAGE_DEPENDENCY package id: %ls, bundle provider key: %ls", wzBase, iAction, pAction->packageDependency.pPackage->sczId, pAction->packageDependency.sczBundleProviderKey);
        for (DWORD j = 0; j < pAction->packageDependency.pPackage->cDependencyProviders; ++j)
        {
            const BURN_DEPENDENCY_PROVIDER* pProvider = pAction->packageDependency.pPackage->rgDependencyProviders + j;
            LogStringLine(PlanDumpLevel, "      Provider[%u]: key: %ls, action: %hs", j, pProvider->sczKey, LoggingDependencyActionToString(fRollback ? pProvider->dependentRollback : pProvider->dependentExecute));
        }
        break;

    case BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: RELATED_BUNDLE package id: %ls, action: %hs, ignore dependencies: %ls", wzBase, iAction, pAction->relatedBundle.pRelatedBundle->package.sczId, LoggingActionStateToString(pAction->relatedBundle.action), pAction->relatedBundle.sczIgnoreDependencies);
        break;

    case BURN_EXECUTE_ACTION_TYPE_BUNDLE_PACKAGE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: BUNDLE_PACKAGE package id: %ls, action: %hs", wzBase, iAction, pAction->bundlePackage.pPackage->sczId, LoggingActionStateToString(pAction->bundlePackage.action));
        break;

    case BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: EXE_PACKAGE package id: %ls, action: %hs", wzBase, iAction, pAction->exePackage.pPackage->sczId, LoggingActionStateToString(pAction->exePackage.action));
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: MSI_PACKAGE package id: %ls, scope: %hs, action: %hs, action msi property: %ls, ui level: %u, disable externaluihandler: %hs, file versioning: %hs, log path: %ls, logging attrib: %u", wzBase, iAction, pAction->msiPackage.pPackage->sczId, LoggingPackageScopeToString(pAction->msiPackage.pPackage->scope), LoggingActionStateToString(pAction->msiPackage.action), LoggingBurnMsiPropertyToString(pAction->msiPackage.actionMsiProperty), pAction->msiPackage.uiLevel, LoggingBoolToString(pAction->msiPackage.fDisableExternalUiHandler), LoggingMsiFileVersioningToString(pAction->msiPackage.fileVersioning), pAction->msiPackage.sczLogPath, pAction->msiPackage.dwLoggingAttributes);
        for (DWORD j = 0; j < pAction->msiPackage.pPackage->Msi.cSlipstreamMspPackages; ++j)
        {
            const BURN_SLIPSTREAM_MSP* pSlipstreamMsp = pAction->msiPackage.pPackage->Msi.rgSlipstreamMsps + j;
            LogStringLine(PlanDumpLevel, "      Patch[%u]: msp package id: %ls, action: %hs", j, pSlipstreamMsp->pMspPackage->sczId, LoggingActionStateToString(fRollback ? pSlipstreamMsp->rollback : pSlipstreamMsp->execute));
        }
        break;

    case BURN_EXECUTE_ACTION_TYPE_UNINSTALL_MSI_COMPATIBLE_PACKAGE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: UNINSTALL_MSI_COMPATIBLE_PACKAGE package id: %ls, compatible package id: %ls, cache id: %ls, log path: %ls, logging attrib: %u", wzBase, iAction, pAction->uninstallMsiCompatiblePackage.pParentPackage->sczId, pAction->uninstallMsiCompatiblePackage.pParentPackage->compatiblePackage.compatibleEntry.sczId, pAction->uninstallMsiCompatiblePackage.pParentPackage->compatiblePackage.sczCacheId, pAction->uninstallMsiCompatiblePackage.sczLogPath, pAction->uninstallMsiCompatiblePackage.dwLoggingAttributes);
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSP_TARGET:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: MSP_TARGET package id: %ls, action: %hs, target product code: %ls, target per-machine: %hs, action msi property: %ls, ui level: %u, disable externaluihandler: %hs, file versioning: %hs, log path: %ls", wzBase, iAction, pAction->mspTarget.pPackage->sczId, LoggingActionStateToString(pAction->mspTarget.action), pAction->mspTarget.sczTargetProductCode, LoggingBoolToString(pAction->mspTarget.fPerMachineTarget), LoggingBurnMsiPropertyToString(pAction->mspTarget.actionMsiProperty), pAction->mspTarget.uiLevel, LoggingBoolToString(pAction->mspTarget.fDisableExternalUiHandler), LoggingMsiFileVersioningToString(pAction->mspTarget.fileVersioning), pAction->mspTarget.sczLogPath);
        for (DWORD j = 0; j < pAction->mspTarget.cOrderedPatches; ++j)
        {
            LogStringLine(PlanDumpLevel, "      Patch[%u]: order: %u, msp package id: %ls", j, pAction->mspTarget.rgOrderedPatches[j].pTargetProduct->dwOrder, pAction->mspTarget.rgOrderedPatches[j].pPackage->sczId);
        }
        break;

    case BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: MSU_PACKAGE package id: %ls, action: %hs, log path: %ls", wzBase, iAction, pAction->msuPackage.pPackage->sczId, LoggingActionStateToString(pAction->msuPackage.action), pAction->msuPackage.sczLogPath);
        break;

    case BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_START:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: ROLLBACK_BOUNDARY_START id: %ls, vital: %ls", wzBase, iAction, pAction->rollbackBoundary.pRollbackBoundary->sczId, pAction->rollbackBoundary.pRollbackBoundary->fVital ? L"yes" : L"no");
        break;

    case BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_END:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: ROLLBACK_BOUNDARY_END", wzBase, iAction);
        break;

    case BURN_EXECUTE_ACTION_TYPE_WAIT_CACHE_PACKAGE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: WAIT_CACHE_PACKAGE id: %ls, event handle: 0x%p", wzBase, iAction, pAction->waitCachePackage.pPackage->sczId, pAction->waitCachePackage.pPackage->hCacheEvent);
        break;

    case BURN_EXECUTE_ACTION_TYPE_UNCACHE_PACKAGE:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: UNCACHE_PACKAGE id: %ls", wzBase, iAction, pAction->uncachePackage.pPackage->sczId);
        break;

    case BURN_EXECUTE_ACTION_TYPE_BEGIN_MSI_TRANSACTION:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: BEGIN_MSI_TRANSACTION id: %ls", wzBase, iAction, pAction->msiTransaction.pRollbackBoundary->sczId);
        break;

    case BURN_EXECUTE_ACTION_TYPE_COMMIT_MSI_TRANSACTION:
        LogStringLine(PlanDumpLevel, "%ls action[%u]: COMMIT_MSI_TRANSACTION id: %ls", wzBase, iAction, pAction->msiTransaction.pRollbackBoundary->sczId);
        break;

    default:
        AssertSz(FALSE, "Unknown execute action type.");
        break;
    }

    if (pAction->fDeleted)
    {
        LogStringLine(PlanDumpLevel, "      (deleted action)");
    }
}

static void RestoreRelatedBundleActionLog(
    __in DWORD iAction,
    __in BURN_EXECUTE_ACTION* pAction
    )
{
    switch (pAction->type)
    {
    case BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE:
        LogStringLine(PlanDumpLevel, "Restore action[%u]: RELATED_BUNDLE package id: %ls, action: %hs, ignore dependencies: %ls", iAction, pAction->relatedBundle.pRelatedBundle->package.sczId, LoggingActionStateToString(pAction->relatedBundle.action), pAction->relatedBundle.sczIgnoreDependencies);
        break;

    default:
        AssertSz(FALSE, "Unknown execute action type.");
        break;
    }

    if (pAction->fDeleted)
    {
        LogStringLine(PlanDumpLevel, "      (deleted action)");
    }
}

static void CleanActionLog(
    __in DWORD iAction,
    __in BURN_CLEAN_ACTION* pAction
    )
{
    switch (pAction->type)
    {
    case BURN_CLEAN_ACTION_TYPE_COMPATIBLE_PACKAGE:
        LogStringLine(PlanDumpLevel, "   Clean action[%u]: CLEAN_COMPATIBLE_PACKAGE package id: %ls", iAction, pAction->pPackage->sczId);
        break;

    case BURN_CLEAN_ACTION_TYPE_PACKAGE:
        LogStringLine(PlanDumpLevel, "   Clean action[%u]: CLEAN_PACKAGE package id: %ls", iAction, pAction->pPackage->sczId);
        break;

    default:
        AssertSz(FALSE, "Unknown clean action type.");
        break;
    }
}

extern "C" void PlanDump(
    __in BURN_PLAN* pPlan
    )
{
    LogStringLine(PlanDumpLevel, "--- Begin plan dump ---");

    LogStringLine(PlanDumpLevel, "Plan action: %hs", LoggingBurnActionToString(pPlan->action));
    LogStringLine(PlanDumpLevel, "     bundle code: %ls", pPlan->wzBundleCode);
    LogStringLine(PlanDumpLevel, "     bundle provider key: %ls", pPlan->wzBundleProviderKey);
    LogStringLine(PlanDumpLevel, "     use-forward-compatible: %hs", LoggingTrueFalseToString(pPlan->fEnabledForwardCompatibleBundle));
    LogStringLine(PlanDumpLevel, "     planned scope: %hs", LoggingBundleScopeToString(pPlan->plannedScope));
    LogStringLine(PlanDumpLevel, "     per-machine: %hs", LoggingTrueFalseToString(pPlan->fPerMachine));
    LogStringLine(PlanDumpLevel, "     can affect machine state: %hs", LoggingTrueFalseToString(pPlan->fCanAffectMachineState));
    LogStringLine(PlanDumpLevel, "     disable-rollback: %hs", LoggingTrueFalseToString(pPlan->fDisableRollback));
    LogStringLine(PlanDumpLevel, "     disallow-removal: %hs", LoggingTrueFalseToString(pPlan->fDisallowRemoval));
    LogStringLine(PlanDumpLevel, "     downgrade: %hs", LoggingTrueFalseToString(pPlan->fDowngrade));
    LogStringLine(PlanDumpLevel, "     registration options: %hs", LoggingRegistrationOptionsToString(pPlan->dwRegistrationOperations));
    if (pPlan->sczLayoutDirectory)
    {
        LogStringLine(PlanDumpLevel, "     layout directory: %ls", pPlan->sczLayoutDirectory);
    }

    for (DWORD i = 0; i < pPlan->cRegistrationActions; ++i)
    {
        DependentRegistrationActionLog(i, pPlan->rgRegistrationActions + i, FALSE);
    }

    for (DWORD i = 0; i < pPlan->cRollbackRegistrationActions; ++i)
    {
        DependentRegistrationActionLog(i, pPlan->rgRollbackRegistrationActions + i, TRUE);
    }

    LogStringLine(PlanDumpLevel, "Plan cache size: %llu", pPlan->qwCacheSizeTotal);
    for (DWORD i = 0; i < pPlan->cCacheActions; ++i)
    {
        CacheActionLog(i, pPlan->rgCacheActions + i, FALSE);
    }

    for (DWORD i = 0; i < pPlan->cRollbackCacheActions; ++i)
    {
        CacheActionLog(i, pPlan->rgRollbackCacheActions + i, TRUE);
    }

    LogStringLine(PlanDumpLevel, "Plan execute package count: %u", pPlan->cExecutePackagesTotal);
    LogStringLine(PlanDumpLevel, "     overall progress ticks: %u", pPlan->cOverallProgressTicksTotal);
    for (DWORD i = 0; i < pPlan->cExecuteActions; ++i)
    {
        ExecuteActionLog(i, pPlan->rgExecuteActions + i, FALSE);
    }

    for (DWORD i = 0; i < pPlan->cRollbackActions; ++i)
    {
        ExecuteActionLog(i, pPlan->rgRollbackActions + i, TRUE);
    }

    for (DWORD i = 0; i < pPlan->cRestoreRelatedBundleActions; ++i)
    {
        RestoreRelatedBundleActionLog(i, pPlan->rgRestoreRelatedBundleActions + i);
    }

    for (DWORD i = 0; i < pPlan->cCleanActions; ++i)
    {
        CleanActionLog(i, pPlan->rgCleanActions + i);
    }

    for (DWORD i = 0; i < pPlan->cPlannedProviders; ++i)
    {
        LogStringLine(PlanDumpLevel, "   Dependency action[%u]: PLANNED_PROVIDER key: %ls, name: %ls", i, pPlan->rgPlannedProviders[i].sczKey, pPlan->rgPlannedProviders[i].sczName);
    }

    LogStringLine(PlanDumpLevel, "--- End plan dump ---");
}
