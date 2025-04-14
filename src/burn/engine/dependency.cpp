// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// constants

#define INITIAL_STRINGDICT_SIZE 48
const LPCWSTR vcszIgnoreDependenciesDelim = L";";


// internal function declarations

static HRESULT DetectPackageDependents(
    __in BURN_PACKAGE* pPackage,
    __in const BURN_REGISTRATION* pRegistration
    );

static HRESULT SplitIgnoreDependencies(
    __in_z LPCWSTR wzIgnoreDependencies,
    __deref_inout_ecount_opt(*pcDependencies) DEPENDENCY** prgDependencies,
    __inout LPUINT pcDependencies,
    __out BOOL* pfIgnoreAll
    );

static HRESULT JoinIgnoreDependencies(
    __out_z LPWSTR* psczIgnoreDependencies,
    __in_ecount(cDependencies) const DEPENDENCY* rgDependencies,
    __in UINT cDependencies
    );

static HRESULT GetIgnoredDependents(
    __in const BURN_PACKAGE* pPackage,
    __in const BURN_PLAN* pPlan,
    __deref_inout STRINGDICT_HANDLE* psdIgnoredDependents
    );

static BOOL GetProviderExists(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzProviderKey
    );

static void CalculateDependencyActionStates(
    __in const BURN_PACKAGE* pPackage,
    __out BURN_DEPENDENCY_ACTION* pDependencyExecuteAction,
    __out BURN_DEPENDENCY_ACTION* pDependencyRollbackAction
    );

static HRESULT AddPackageDependencyActions(
    __in_opt DWORD *pdwInsertSequence,
    __in const BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in const BURN_DEPENDENCY_ACTION dependencyExecuteAction,
    __in const BURN_DEPENDENCY_ACTION dependencyRollbackAction
    );

static LPCWSTR GetPackageProviderId(
    __in const BURN_PACKAGE* pPackage
    );

static HRESULT RegisterPackageProvider(
    __in const BURN_DEPENDENCY_PROVIDER* pProvider,
    __in LPCWSTR wzPackageId,
    __in LPCWSTR wzPackageProviderId,
    __in HKEY hkRoot,
    __in BOOL fVital
    );

static void UnregisterPackageProvider(
    __in const BURN_DEPENDENCY_PROVIDER* pProvider,
    __in LPCWSTR wzPackageId,
    __in HKEY hkRoot
    );

static HRESULT RegisterPackageProviderDependent(
    __in const BURN_DEPENDENCY_PROVIDER* pProvider,
    __in BOOL fVital,
    __in HKEY hkRoot,
    __in LPCWSTR wzPackageId,
    __in_z LPCWSTR wzDependentProviderKey
    );

static void UnregisterPackageDependency(
    __in BOOL fPerMachine,
    __in const BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzDependentProviderKey
    );

static void UnregisterPackageProviderDependent(
    __in const BURN_DEPENDENCY_PROVIDER* pProvider,
    __in HKEY hkRoot,
    __in LPCWSTR wzPackageId,
    __in_z LPCWSTR wzDependentProviderKey
    );
static void UnregisterOrphanPackageProviders(
    __in const BURN_PACKAGE* pPackage
    );


// functions

extern "C" void DependencyUninitializeProvider(
    __in BURN_DEPENDENCY_PROVIDER* pProvider
    )
{
    ReleaseStr(pProvider->sczKey);
    ReleaseStr(pProvider->sczVersion);
    ReleaseStr(pProvider->sczDisplayName);
    ReleaseDependencyArray(pProvider->rgDependents, pProvider->cDependents);

    memset(pProvider, 0, sizeof(BURN_DEPENDENCY_PROVIDER));
}

extern "C" HRESULT DependencyParseProvidersFromXml(
    __in BURN_PACKAGE* pPackage,
    __in IXMLDOMNode* pixnPackage
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    DWORD cNodes = 0;
    IXMLDOMNode* pixnNode = NULL;

    // Select dependency provider nodes.
    hr = XmlSelectNodes(pixnPackage, L"Provides", &pixnNodes);
    ExitOnFailure(hr, "Failed to select dependency provider nodes.");

    // Get dependency provider node count.
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get the dependency provider node count.");

    if (!cNodes)
    {
        ExitFunction1(hr = S_OK);
    }

    // Allocate memory for dependency provider pointers.
    pPackage->rgDependencyProviders = (BURN_DEPENDENCY_PROVIDER*)MemAlloc(sizeof(BURN_DEPENDENCY_PROVIDER) * cNodes, TRUE);
    ExitOnNull(pPackage->rgDependencyProviders, hr, E_OUTOFMEMORY, "Failed to allocate memory for dependency providers.");

    pPackage->cDependencyProviders = cNodes;

    // Parse dependency provider elements.
    for (DWORD i = 0; i < cNodes; i++)
    {
        BURN_DEPENDENCY_PROVIDER* pDependencyProvider = &pPackage->rgDependencyProviders[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get the next dependency provider node.");

        // @Key
        hr = XmlGetAttributeEx(pixnNode, L"Key", &pDependencyProvider->sczKey);
        ExitOnFailure(hr, "Failed to get the Key attribute.");

        // @Version
        hr = XmlGetAttributeEx(pixnNode, L"Version", &pDependencyProvider->sczVersion);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get the Version attribute.");
        }

        // @DisplayName
        hr = XmlGetAttributeEx(pixnNode, L"DisplayName", &pDependencyProvider->sczDisplayName);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get the DisplayName attribute.");
        }

        // @Imported
        hr = XmlGetYesNoAttribute(pixnNode, L"Imported", &pDependencyProvider->fImported);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get the Imported attribute.");
        }
        else
        {
            pDependencyProvider->fImported = FALSE;
            hr = S_OK;
        }

        // Prepare next iteration.
        ReleaseNullObject(pixnNode);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNode);
    ReleaseObject(pixnNodes);

    return hr;
}

extern "C" HRESULT DependencyInitialize(
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in BURN_DEPENDENCIES* pDependencies,
    __in BURN_REGISTRATION* pRegistration
    )
{
    AssertSz(!pDependencies->cIgnoredDependencies, "Dependencies already initalized.");

    HRESULT hr = S_OK;

    // If no parent was specified at all, use the bundle code as the self dependent.
    if (!pInternalCommand->sczActiveParent)
    {
        pDependencies->wzSelfDependent = pRegistration->sczCode;
    }
    else if (*pInternalCommand->sczActiveParent) // if parent was specified use that as the self dependent.
    {
        pDependencies->wzSelfDependent = pInternalCommand->sczActiveParent;
    }
    // else parent:none was used which means we should not register a dependency on ourself.

    pDependencies->wzActiveParent = pInternalCommand->sczActiveParent;

    // The current bundle provider key should always be ignored for dependency checks.
    hr = DepDependencyArrayAlloc(&pDependencies->rgIgnoredDependencies, &pDependencies->cIgnoredDependencies, pRegistration->sczProviderKey, NULL);
    ExitOnFailure(hr, "Failed to add the bundle provider key to the list of dependencies to ignore.");

    // Add the list of dependencies to ignore.
    if (pInternalCommand->sczIgnoreDependencies)
    {
        hr = SplitIgnoreDependencies(pInternalCommand->sczIgnoreDependencies, &pDependencies->rgIgnoredDependencies, &pDependencies->cIgnoredDependencies, &pDependencies->fIgnoreAllDependents);
        ExitOnFailure(hr, "Failed to split the list of dependencies to ignore.");
    }

    pDependencies->fSelfDependent = NULL != pDependencies->wzSelfDependent;
    pDependencies->fActiveParent = NULL != pInternalCommand->sczActiveParent && NULL != *pInternalCommand->sczActiveParent;

LExit:
    return hr;
}

extern "C" void DependencyUninitialize(
    __in BURN_DEPENDENCIES* pDependencies
    )
{
    if (pDependencies->rgIgnoredDependencies)
    {
        ReleaseDependencyArray(pDependencies->rgIgnoredDependencies, pDependencies->cIgnoredDependencies);
    }

    memset(pDependencies, 0, sizeof(BURN_DEPENDENCIES));
}

extern "C" HRESULT DependencyDetectProviderKeyBundleCode(
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;

    hr = DepGetProviderInformation(pRegistration->hkRoot, pRegistration->sczProviderKey, &pRegistration->sczDetectedProviderKeyBundleCode, NULL, NULL);
    if (E_NOTFOUND == hr)
    {
        ReleaseNullStr(pRegistration->sczDetectedProviderKeyBundleCode);
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to get provider key bundle code.");

    // If a bundle code was not explicitly set, default the provider key bundle code to this bundle's provider key.
    if (!pRegistration->sczDetectedProviderKeyBundleCode || !*pRegistration->sczDetectedProviderKeyBundleCode)
    {
        hr = StrAllocString(&pRegistration->sczDetectedProviderKeyBundleCode, pRegistration->sczProviderKey, 0);
        ExitOnFailure(hr, "Failed to initialize provider key bundle code.");
    }
    else if (CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pRegistration->sczCode, -1, pRegistration->sczDetectedProviderKeyBundleCode, -1))
    {
        pRegistration->fDetectedForeignProviderKeyBundleCode = TRUE;
        LogId(REPORT_STANDARD, MSG_DETECTED_FOREIGN_BUNDLE_PROVIDER_REGISTRATION, pRegistration->sczProviderKey, pRegistration->sczDetectedProviderKeyBundleCode);
    }

LExit:
    return hr;
}

extern "C" HRESULT DependencyDetectBundle(
    __in BURN_DEPENDENCIES* pDependencies,
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;
    BOOL fExists = FALSE;

    hr = DependencyDetectProviderKeyBundleCode(pRegistration);
    ExitOnFailure(hr, "Failed to detect provider key bundle code.");

    hr = DepCheckDependents(pRegistration->hkRoot, pRegistration->sczProviderKey, 0, NULL, &pRegistration->rgDependents, &pRegistration->cDependents);
    ExitOnPathFailure(hr, fExists, "Failed dependents check on bundle.");

    if (pDependencies->fSelfDependent || pDependencies->fActiveParent)
    {
        for (DWORD i = 0; i < pRegistration->cDependents; ++i)
        {
            DEPENDENCY* pDependent = pRegistration->rgDependents + i;

            if (pDependencies->fActiveParent && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pDependencies->wzActiveParent, -1, pDependent->sczKey, -1))
            {
                pRegistration->fParentRegisteredAsDependent = TRUE;
            }

            if (pDependencies->fSelfDependent && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pDependencies->wzSelfDependent, -1, pDependent->sczKey, -1))
            {
                pRegistration->fSelfRegisteredAsDependent = TRUE;
            }
        }
    }

LExit:
    return hr;
}

extern "C" HRESULT DependencyDetectChainPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;

    hr = DetectPackageDependents(pPackage, pRegistration);
    ExitOnFailure(hr, "Failed to detect dependents for package '%ls'", pPackage->sczId);

    hr = DependencyDetectCompatibleEntry(pPackage, pRegistration);
    ExitOnFailure(hr, "Failed to detect compatible package for package '%ls'", pPackage->sczId);

LExit:
    return hr;
}

extern "C" HRESULT DependencyDetectRelatedBundle(
    __in BURN_RELATED_BUNDLE* pRelatedBundle,
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;
    BURN_PACKAGE* pPackage = &pRelatedBundle->package;

    if (pRelatedBundle->fPlannable)
    {
        hr = DetectPackageDependents(pPackage, pRegistration);
        ExitOnFailure(hr, "Failed to detect dependents for related bundle '%ls'", pPackage->sczId);
    }

LExit:
    return hr;
}

extern "C" HRESULT DependencyPlanInitialize(
    __in BURN_DEPENDENCIES* pDependencies,
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;

    // TODO: After adding enumeration to STRINGDICT, a single STRINGDICT_HANDLE can be used everywhere.
    for (DWORD i = 0; i < pDependencies->cIgnoredDependencies; ++i)
    {
        DEPENDENCY* pDependency = pDependencies->rgIgnoredDependencies + i;

        hr = DepDependencyArrayAlloc(&pPlan->rgPlannedProviders, &pPlan->cPlannedProviders, pDependency->sczKey, pDependency->sczName);
        ExitOnFailure(hr, "Failed to add the detected provider to the list of dependencies to ignore.");
    }

LExit:
    return hr;
}

extern "C" HRESULT DependencyAllocIgnoreDependencies(
    __in const BURN_PLAN *pPlan,
    __out_z LPWSTR* psczIgnoreDependencies
    )
{
    HRESULT hr = S_OK;

    // Join the list of dependencies to ignore for each related bundle.
    if (0 < pPlan->cPlannedProviders)
    {
        hr = JoinIgnoreDependencies(psczIgnoreDependencies, pPlan->rgPlannedProviders, pPlan->cPlannedProviders);
        ExitOnFailure(hr, "Failed to join the list of dependencies to ignore.");
    }

LExit:
    return hr;
}

extern "C" HRESULT DependencyAddIgnoreDependencies(
    __in STRINGDICT_HANDLE sdIgnoreDependencies,
    __in_z LPCWSTR wzAddIgnoreDependencies
    )
{
    HRESULT hr = S_OK;
    LPWSTR wzContext = NULL;

    // Parse through the semicolon-delimited tokens and add to the array.
    for (LPCWSTR wzToken = ::wcstok_s(const_cast<LPWSTR>(wzAddIgnoreDependencies), vcszIgnoreDependenciesDelim, &wzContext); wzToken; wzToken = ::wcstok_s(NULL, vcszIgnoreDependenciesDelim, &wzContext))
    {
        hr = DictKeyExists(sdIgnoreDependencies, wzToken);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to check the dictionary of unique dependencies.");
        }
        else
        {
            hr = DictAddKey(sdIgnoreDependencies, wzToken);
            ExitOnFailure(hr, "Failed to add \"%ls\" to the string dictionary.", wzToken);
        }
    }

LExit:
    return hr;
}

extern "C" HRESULT DependencyPlanPackageBegin(
    __in BOOL fPerMachine,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    STRINGDICT_HANDLE sdIgnoredDependents = NULL;
    BURN_DEPENDENCY_ACTION dependencyExecuteAction = BURN_DEPENDENCY_ACTION_NONE;
    BURN_DEPENDENCY_ACTION dependencyRollbackAction = BURN_DEPENDENCY_ACTION_NONE;
    BOOL fDependentBlocksUninstall = FALSE;
    BOOL fAttemptingUninstall = BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pPackage->execute || pPackage->compatiblePackage.fRemove;

    pPackage->dependencyExecute = BURN_DEPENDENCY_ACTION_NONE;
    pPackage->dependencyRollback = BURN_DEPENDENCY_ACTION_NONE;

    // Make sure the package defines at least one provider.
    if (0 == pPackage->cDependencyProviders)
    {
        LogId(REPORT_VERBOSE, MSG_DEPENDENCY_PACKAGE_SKIP_NOPROVIDERS, pPackage->sczId);
        ExitFunction1(hr = S_OK);
    }

    // Make sure the package is in the same scope as the bundle.
    if (fPerMachine != pPackage->fPerMachine)
    {
        LogId(REPORT_STANDARD, MSG_DEPENDENCY_PACKAGE_SKIP_WRONGSCOPE, pPackage->sczId, LoggingPerMachineToString(fPerMachine), LoggingPerMachineToString(pPackage->fPerMachine));
        ExitFunction1(hr = S_OK);
    }

    // Check if any dependents are registered which would prevent the package from being uninstalled.
    // Build up a list of dependents to ignore, including the current bundle.
    hr = GetIgnoredDependents(pPackage, pPlan, &sdIgnoredDependents);
    ExitOnFailure(hr, "Failed to build the list of ignored dependents.");

    // Skip the dependency check if "ALL" was authored for IGNOREDEPENDENCIES.
    hr = DictKeyExists(sdIgnoredDependents, L"ALL");
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to check if \"ALL\" was set in IGNOREDEPENDENCIES.");
    }
    else
    {
        hr = S_OK;
        BOOL fDependenciesWarned = FALSE;

        for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
        {
            const BURN_DEPENDENCY_PROVIDER* pProvider = pPackage->rgDependencyProviders + i;

            for (DWORD j = 0; j < pProvider->cDependents; ++j)
            {
                const DEPENDENCY* pDependency = pProvider->rgDependents + j;

                hr = DictKeyExists(sdIgnoredDependents, pDependency->sczKey);
                if (E_NOTFOUND == hr)
                {
                    hr = S_OK;

                    if (pPackage->requested == BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT)
                    {
                        if (!fDependenciesWarned)
                        {
                            fDependenciesWarned = TRUE;
                            LogId(REPORT_STANDARD, MSG_DEPENDENCY_PACKAGE_DEPENDENTS_OVERRIDDEN, pPackage->sczId);
                        }
                    }
                    else if (!fDependentBlocksUninstall)
                    {
                        fDependentBlocksUninstall = TRUE;

                        LogId(REPORT_STANDARD, MSG_DEPENDENCY_PACKAGE_HASDEPENDENTS, pPackage->sczId);
                    }

                    LogId(REPORT_VERBOSE, MSG_DEPENDENCY_PACKAGE_DEPENDENT, pDependency->sczKey, LoggingStringOrUnknownIfNull(pDependency->sczName));
                }
                ExitOnFailure(hr, "Failed to check the dictionary of ignored dependents.");
            }
        }
    }

    if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pPackage->execute)
    {
        pPackage->fDependencyManagerWasHere = fDependentBlocksUninstall;
    }

    // Calculate the dependency actions before the package itself is planned.
    CalculateDependencyActionStates(pPackage, &dependencyExecuteAction, &dependencyRollbackAction);

    // If dependents were found, change the action to not uninstall the package.
    if (fAttemptingUninstall && fDependentBlocksUninstall)
    {
        pPackage->execute = BOOTSTRAPPER_ACTION_STATE_NONE;
        pPackage->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;

        // Assume the compatible package has the same exact providers.
        pPackage->compatiblePackage.fRemove = FALSE;
    }
    else
    {
        // Trust the forward compatible nature of providers - don't uninstall the package during rollback if there were dependents.
        if (fDependentBlocksUninstall && BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pPackage->rollback)
        {
            pPackage->rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
        }

        // Only plan providers when the package is current (not obsolete).
        if (BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE != pPackage->currentState)
        {
            for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
            {
                BURN_DEPENDENCY_PROVIDER* pProvider = &pPackage->rgDependencyProviders[i];

                // Only need to handle providers that were authored directly in the bundle.
                if (pProvider->fImported)
                {
                    continue;
                }

                pProvider->providerExecute = dependencyExecuteAction;
                pProvider->providerRollback = dependencyRollbackAction;

                // Don't overwrite providers that we don't own.
                if (pPackage->compatiblePackage.fDetected)
                {
                    if (BURN_DEPENDENCY_ACTION_REGISTER == pProvider->providerExecute)
                    {
                        pProvider->providerExecute = BURN_DEPENDENCY_ACTION_NONE;
                        pProvider->providerRollback = BURN_DEPENDENCY_ACTION_NONE;
                    }

                    if (BURN_DEPENDENCY_ACTION_REGISTER == pProvider->providerRollback)
                    {
                        pProvider->providerRollback = BURN_DEPENDENCY_ACTION_NONE;
                    }
                }

                if (BURN_DEPENDENCY_ACTION_UNREGISTER == pProvider->providerExecute && !pProvider->fExists)
                {
                    pProvider->providerExecute = BURN_DEPENDENCY_ACTION_NONE;
                }

                if (BURN_DEPENDENCY_ACTION_UNREGISTER == pProvider->providerRollback && pProvider->fExists ||
                    BURN_DEPENDENCY_ACTION_REGISTER == pProvider->providerRollback && !pProvider->fExists)
                {
                    pProvider->providerRollback = BURN_DEPENDENCY_ACTION_NONE;
                }

                if (BURN_DEPENDENCY_ACTION_NONE != pProvider->providerExecute)
                {
                    pPackage->fProviderExecute = TRUE;
                }

                if (BURN_DEPENDENCY_ACTION_NONE != pProvider->providerRollback)
                {
                    pPackage->fProviderRollback = TRUE;
                }
            }
        }

        // If the package will be removed, add its providers to the growing list in the plan.
        if (fAttemptingUninstall)
        {
            for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
            {
                const BURN_DEPENDENCY_PROVIDER* pProvider = &pPackage->rgDependencyProviders[i];

                hr = DepDependencyArrayAlloc(&pPlan->rgPlannedProviders, &pPlan->cPlannedProviders, pProvider->sczKey, NULL);
                ExitOnFailure(hr, "Failed to add the package provider key \"%ls\" to the planned list.", pProvider->sczKey);
            }
        }
    }

    for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
    {
        BURN_DEPENDENCY_PROVIDER* pProvider = &pPackage->rgDependencyProviders[i];

        pProvider->dependentExecute = dependencyExecuteAction;
        pProvider->dependentRollback = dependencyRollbackAction;

        if (BURN_DEPENDENCY_ACTION_REGISTER == pProvider->dependentRollback &&
            BURN_DEPENDENCY_ACTION_UNREGISTER  == pProvider->providerExecute && BURN_DEPENDENCY_ACTION_REGISTER != pProvider->providerRollback)
        {
            pProvider->dependentRollback = BURN_DEPENDENCY_ACTION_NONE;
        }

        if (BURN_DEPENDENCY_ACTION_UNREGISTER == pProvider->dependentExecute && !pProvider->fBundleRegisteredAsDependent)
        {
            pProvider->dependentExecute = BURN_DEPENDENCY_ACTION_NONE;
        }

        if (BURN_DEPENDENCY_ACTION_UNREGISTER == pProvider->dependentRollback && pProvider->fBundleRegisteredAsDependent ||
            BURN_DEPENDENCY_ACTION_REGISTER == pProvider->dependentRollback && !pProvider->fBundleRegisteredAsDependent)
        {
            pProvider->dependentRollback = BURN_DEPENDENCY_ACTION_NONE;
        }

        // The highest aggregate action state found will be returned.
        if (pPackage->dependencyExecute < pProvider->dependentExecute)
        {
            pPackage->dependencyExecute = pProvider->dependentExecute;
        }

        if (pPackage->dependencyRollback < pProvider->dependentRollback)
        {
            pPackage->dependencyRollback = pProvider->dependentRollback;
        }
    }

LExit:
    ReleaseDict(sdIgnoredDependents);

    return hr;
}

extern "C" HRESULT DependencyPlanPackage(
    __in_opt DWORD *pdwInsertSequence,
    __in const BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;

    // If the dependency execution action is to unregister, add the dependency actions to the plan
    // *before* the provider key is potentially removed.
    if (BURN_DEPENDENCY_ACTION_UNREGISTER == pPackage->dependencyExecute)
    {
        hr = AddPackageDependencyActions(pdwInsertSequence, pPackage, pPlan, pPackage->dependencyExecute, pPackage->dependencyRollback);
        ExitOnFailure(hr, "Failed to plan the dependency actions for package: %ls", pPackage->sczId);
    }

    // Add the provider rollback plan.
    if (pPackage->fProviderRollback)
    {
        hr = PlanAppendRollbackAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append provider rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER;
        pAction->packageProvider.pPackage = const_cast<BURN_PACKAGE*>(pPackage);

        // Put a checkpoint before the execute action so that rollback happens
        // if execute fails.
        hr = PlanExecuteCheckpoint(pPlan);
        ExitOnFailure(hr, "Failed to plan provider checkpoint action.");
    }

    // Add the provider execute plan. This comes after rollback so if something goes wrong
    // rollback will try to clean up after us.
    if (pPackage->fProviderExecute)
    {
        if (NULL != pdwInsertSequence)
        {
            hr = PlanInsertExecuteAction(*pdwInsertSequence, pPlan, &pAction);
            ExitOnFailure(hr, "Failed to insert provider execute action.");

            // Always move the sequence after this dependency action so the provider registration
            // stays in front of the inserted actions.
            ++(*pdwInsertSequence);
        }
        else
        {
            hr = PlanAppendExecuteAction(pPlan, &pAction);
            ExitOnFailure(hr, "Failed to append provider execute action.");
        }

        pAction->type = BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER;
        pAction->packageProvider.pPackage = const_cast<BURN_PACKAGE*>(pPackage);
    }

LExit:
    return hr;
}

extern "C" HRESULT DependencyPlanPackageComplete(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan
    )
{
    HRESULT hr = S_OK;

    // Registration of dependencies happens here, after the package is planned to be
    // installed and all that good stuff.
    if (BURN_DEPENDENCY_ACTION_REGISTER == pPackage->dependencyExecute)
    {
        hr = AddPackageDependencyActions(NULL, pPackage, pPlan, pPackage->dependencyExecute, pPackage->dependencyRollback);
        ExitOnFailure(hr, "Failed to plan the dependency actions for package: %ls", pPackage->sczId);
    }

LExit:
    return hr;
}

extern "C" HRESULT DependencyExecutePackageProviderAction(
    __in const BURN_EXECUTE_ACTION* pAction,
    __in BOOL fRollback
    )
{
    AssertSz(BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER == pAction->type, "Execute action type not supported by this function.");

    HRESULT hr = S_OK;
    const BURN_PACKAGE* pPackage = pAction->packageProvider.pPackage;
    HKEY hkRoot = pPackage->fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;
    LPCWSTR wzId = GetPackageProviderId(pPackage);

    for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
    {
        const BURN_DEPENDENCY_PROVIDER* pProvider = pPackage->rgDependencyProviders + i;
        BURN_DEPENDENCY_ACTION action = fRollback ? pProvider->providerRollback : pProvider->providerExecute;
        HRESULT hrProvider = S_OK;

        // Register or unregister the package provider.
        switch (action)
        {
        case BURN_DEPENDENCY_ACTION_REGISTER:
            hrProvider = RegisterPackageProvider(pProvider, pPackage->sczId, wzId, hkRoot, pPackage->fVital);
            if (SUCCEEDED(hr) && FAILED(hrProvider))
            {
                hr = hrProvider;
            }
            break;
        case BURN_DEPENDENCY_ACTION_UNREGISTER:
            UnregisterPackageProvider(pProvider, pPackage->sczId, hkRoot);
            break;
        }
    }

    if (!pPackage->fVital)
    {
        hr = S_OK;
    }

    return hr;
}

extern "C" HRESULT DependencyExecutePackageDependencyAction(
    __in BOOL fPerMachine,
    __in const BURN_EXECUTE_ACTION* pAction,
    __in BOOL fRollback
    )
{
    AssertSz(BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY == pAction->type, "Execute action type not supported by this function.");

    HRESULT hr = S_OK;
    const BURN_PACKAGE* pPackage = pAction->packageDependency.pPackage;
    HKEY hkRoot = pPackage->fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;

    // Do not register a dependency on a package in a different install context.
    if (fPerMachine != pPackage->fPerMachine)
    {
        LogId(REPORT_STANDARD, MSG_DEPENDENCY_PACKAGE_SKIP_WRONGSCOPE, pPackage->sczId, LoggingPerMachineToString(fPerMachine), LoggingPerMachineToString(pPackage->fPerMachine));
        ExitFunction1(hr = S_OK);
    }

    for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
    {
        const BURN_DEPENDENCY_PROVIDER* pProvider = pPackage->rgDependencyProviders + i;
        BURN_DEPENDENCY_ACTION action = fRollback ? pProvider->dependentRollback : pProvider->dependentExecute;
        HRESULT hrProvider = S_OK;

        // Register or unregister the bundle as a dependent of the package dependency provider.
        switch (action)
        {
        case BURN_DEPENDENCY_ACTION_REGISTER:
            hrProvider = RegisterPackageProviderDependent(pProvider, pPackage->fVital, hkRoot, pPackage->sczId, pAction->packageDependency.sczBundleProviderKey);
            if (SUCCEEDED(hr) && FAILED(hrProvider))
            {
                hr = hrProvider;
            }
            break;
        case BURN_DEPENDENCY_ACTION_UNREGISTER:
            UnregisterPackageProviderDependent(pProvider, hkRoot, pPackage->sczId, pAction->packageDependency.sczBundleProviderKey);
            break;
        }
    }

LExit:
    if (!pPackage->fVital)
    {
        hr = S_OK;
    }

    return hr;
}

extern "C" HRESULT DependencyRegisterBundle(
    __in const BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;

    LogId(REPORT_VERBOSE, MSG_DEPENDENCY_BUNDLE_REGISTER, pRegistration->sczProviderKey, pRegistration->pVersion->sczVersion);

    // Register the bundle provider key.
    hr = DepRegisterDependency(pRegistration->hkRoot, pRegistration->sczProviderKey, pRegistration->pVersion->sczVersion, pRegistration->sczDisplayName, pRegistration->sczCode, 0);
    ExitOnFailure(hr, "Failed to register the bundle dependency provider.");

LExit:
    return hr;
}

extern "C" HRESULT DependencyProcessDependentRegistration(
    __in const BURN_REGISTRATION* pRegistration,
    __in const BURN_DEPENDENT_REGISTRATION_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BOOL fDeleted = FALSE;

    switch (pAction->type)
    {
    case BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER:
        hr = DepRegisterDependent(pRegistration->hkRoot, pRegistration->sczProviderKey, pAction->sczDependentProviderKey, NULL, NULL, 0);
        ExitOnFailure(hr, "Failed to register dependent: %ls", pAction->sczDependentProviderKey);
        break;

    case BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_UNREGISTER:
        hr = DepUnregisterDependent(pRegistration->hkRoot, pRegistration->sczProviderKey, pAction->sczDependentProviderKey);
        ExitOnPathFailure(hr, fDeleted, "Failed to unregister dependent: %ls", pAction->sczDependentProviderKey);
        break;

    default:
        ExitWithRootFailure(hr, E_INVALIDARG, "Unrecognized registration action type: %d", pAction->type);
    }

LExit:
    return hr;
}

extern "C" void DependencyUnregisterBundle(
    __in const BURN_REGISTRATION* pRegistration,
    __in const BURN_PACKAGES* pPackages
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzDependentProviderKey = pRegistration->sczCode;

    // If we own the bundle dependency then remove it.
    if (!pRegistration->fDetectedForeignProviderKeyBundleCode)
    {
        // Remove the bundle provider key.
        hr = DepUnregisterDependency(pRegistration->hkRoot, pRegistration->sczProviderKey);
        if (SUCCEEDED(hr) || E_FILENOTFOUND == hr)
        {
            LogId(REPORT_VERBOSE, MSG_DEPENDENCY_BUNDLE_UNREGISTERED, pRegistration->sczProviderKey);
        }
        else
        {
            LogId(REPORT_VERBOSE, MSG_DEPENDENCY_BUNDLE_UNREGISTERED_FAILED, pRegistration->sczProviderKey, hr);
        }
    }

    // Best effort to make sure this bundle is not registered as a dependent for anything.
    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        const BURN_PACKAGE* pPackage = pPackages->rgPackages + i;
        UnregisterPackageDependency(pPackage->fPerMachine, pPackage, wzDependentProviderKey);
    }

    for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
    {
        const BURN_PACKAGE* pPackage = &pRegistration->relatedBundles.rgRelatedBundles[i].package;
        UnregisterPackageDependency(pPackage->fPerMachine, pPackage, wzDependentProviderKey);
    }

    // Best effort to make sure package providers are removed if unused.
    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        const BURN_PACKAGE* pPackage = pPackages->rgPackages + i;
        UnregisterOrphanPackageProviders(pPackage);
    }
}

extern "C" HRESULT DependencyDetectCompatibleEntry(
    __in BURN_PACKAGE* pPackage,
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczId = NULL;
    LPWSTR sczName = NULL;
    LPWSTR sczVersion = NULL;
    LPCWSTR wzPackageProviderId = GetPackageProviderId(pPackage);
    HKEY hkHive = pRegistration->fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;

    for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
    {
        BURN_DEPENDENCY_PROVIDER* pProvider = &pPackage->rgDependencyProviders[i];

        hr = DepGetProviderInformation(hkHive, pProvider->sczKey, &sczId, &sczName, &sczVersion);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
            continue;
        }
        ExitOnFailure(hr, "Failed to get provider information for compatible package: %ls", pProvider->sczKey);

        // Make sure the compatible package is not the package itself.
        if (!wzPackageProviderId)
        {
            if (!sczId)
            {
                continue;
            }
        }
        else if (sczId && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, wzPackageProviderId, -1, sczId, -1))
        {
            continue;
        }

        pPackage->compatiblePackage.fDetected = TRUE;

        hr = StrAllocString(&pPackage->compatiblePackage.compatibleEntry.sczProviderKey, pProvider->sczKey, 0);
        ExitOnFailure(hr, "Failed to copy provider key for compatible entry.");

        pPackage->compatiblePackage.compatibleEntry.sczId = sczId;
        sczId = NULL;

        pPackage->compatiblePackage.compatibleEntry.sczName = sczName;
        sczName = NULL;

        pPackage->compatiblePackage.compatibleEntry.sczVersion = sczVersion;
        sczVersion = NULL;

        break;
    }

LExit:
    return hr;
}

// internal functions


static HRESULT DetectPackageDependents(
    __in BURN_PACKAGE* pPackage,
    __in const BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;
    HKEY hkHive = pPackage->fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;
    BOOL fCanIgnorePresence = pPackage->fCanAffectRegistration && 0 < pPackage->cDependencyProviders &&
                              (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pPackage->cacheRegistrationState || BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pPackage->installRegistrationState);
    BOOL fBundleRegisteredAsDependent = FALSE;

    // There's currently no point in getting the dependents if the scope doesn't match,
    // because they will just get ignored.
    if (pRegistration->fPerMachine != pPackage->fPerMachine)
    {
        ExitFunction();
    }

    for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
    {
        BURN_DEPENDENCY_PROVIDER* pProvider = &pPackage->rgDependencyProviders[i];
        BOOL fExists = FALSE;

        hr = DepCheckDependents(hkHive, pProvider->sczKey, 0, NULL, &pProvider->rgDependents, &pProvider->cDependents);
        ExitOnPathFailure(hr, fExists, "Failed dependents check on package provider: %ls", pProvider->sczKey);

        if (0 < pProvider->cDependents || GetProviderExists(hkHive, pProvider->sczKey))
        {
            pProvider->fExists = TRUE;
        }

        for (DWORD iDependent = 0; iDependent < pProvider->cDependents; ++iDependent)
        {
            DEPENDENCY* pDependent = pProvider->rgDependents + iDependent;

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pRegistration->sczCode, -1, pDependent->sczKey, -1))
            {
                pProvider->fBundleRegisteredAsDependent = TRUE;
                fBundleRegisteredAsDependent = TRUE;
                break;
            }
        }
    }

    if (fCanIgnorePresence && !fBundleRegisteredAsDependent)
    {
        if (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pPackage->cacheRegistrationState)
        {
            pPackage->cacheRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_IGNORED;
        }
        if (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pPackage->installRegistrationState)
        {
            pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_IGNORED;
        }
        if (BURN_PACKAGE_TYPE_MSP == pPackage->type)
        {
            for (DWORD i = 0; i < pPackage->Msp.cTargetProductCodes; ++i)
            {
                BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + i;

                if (BURN_PACKAGE_REGISTRATION_STATE_PRESENT == pTargetProduct->registrationState)
                {
                    pTargetProduct->registrationState = BURN_PACKAGE_REGISTRATION_STATE_IGNORED;
                }
            }
        }
    }

LExit:
    return hr;
}

/********************************************************************
 SplitIgnoreDependencies - Splits a semicolon-delimited
  string into a list of unique dependencies to ignore.

*********************************************************************/
static HRESULT SplitIgnoreDependencies(
    __in_z LPCWSTR wzIgnoreDependencies,
    __deref_inout_ecount_opt(*pcDependencies) DEPENDENCY** prgDependencies,
    __inout LPUINT pcDependencies,
    __out BOOL* pfIgnoreAll
    )
{
    HRESULT hr = S_OK;
    LPWSTR wzContext = NULL;
    STRINGDICT_HANDLE sdIgnoreDependencies = NULL;
    *pfIgnoreAll = FALSE;

    // Create a dictionary to hold unique dependencies.
    hr = DictCreateStringList(&sdIgnoreDependencies, INITIAL_STRINGDICT_SIZE, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create the string dictionary.");

    // Parse through the semicolon-delimited tokens and add to the array.
    for (LPCWSTR wzToken = ::wcstok_s(const_cast<LPWSTR>(wzIgnoreDependencies), vcszIgnoreDependenciesDelim, &wzContext); wzToken; wzToken = ::wcstok_s(NULL, vcszIgnoreDependenciesDelim, &wzContext))
    {
        hr = DictKeyExists(sdIgnoreDependencies, wzToken);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to check the dictionary of unique dependencies.");
        }
        else
        {
            hr = DepDependencyArrayAlloc(prgDependencies, pcDependencies, wzToken, NULL);
            ExitOnFailure(hr, "Failed to add \"%ls\" to the list of dependencies to ignore.", wzToken);

            hr = DictAddKey(sdIgnoreDependencies, wzToken);
            ExitOnFailure(hr, "Failed to add \"%ls\" to the string dictionary.", wzToken);

            if (!*pfIgnoreAll && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, L"ALL", -1, wzToken, -1))
            {
                *pfIgnoreAll = TRUE;
            }
        }
    }

LExit:
    ReleaseDict(sdIgnoreDependencies);

    return hr;
}

/********************************************************************
 JoinIgnoreDependencies - Joins a list of dependencies
  to ignore into a semicolon-delimited string of unique values.

*********************************************************************/
static HRESULT JoinIgnoreDependencies(
    __out_z LPWSTR* psczIgnoreDependencies,
    __in_ecount(cDependencies) const DEPENDENCY* rgDependencies,
    __in UINT cDependencies
    )
{
    HRESULT hr = S_OK;
    STRINGDICT_HANDLE sdIgnoreDependencies = NULL;

    // Make sure we pass back an empty string if there are no dependencies.
    if (0 == cDependencies)
    {
        ExitFunction1(hr = S_OK);
    }

    // Create a dictionary to hold unique dependencies.
    hr = DictCreateStringList(&sdIgnoreDependencies, INITIAL_STRINGDICT_SIZE, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create the string dictionary.");

    for (UINT i = 0; i < cDependencies; ++i)
    {
        const DEPENDENCY* pDependency = &rgDependencies[i];

        hr = DictKeyExists(sdIgnoreDependencies, pDependency->sczKey);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to check the dictionary of unique dependencies.");
        }
        else
        {
            if (0 < i)
            {
                hr = StrAllocConcat(psczIgnoreDependencies, vcszIgnoreDependenciesDelim, 1);
                ExitOnFailure(hr, "Failed to append the string delimiter.");
            }

            hr = StrAllocConcat(psczIgnoreDependencies, pDependency->sczKey, 0);
            ExitOnFailure(hr, "Failed to append the key \"%ls\".", pDependency->sczKey);

            hr = DictAddKey(sdIgnoreDependencies, pDependency->sczKey);
            ExitOnFailure(hr, "Failed to add \"%ls\" to the string dictionary.", pDependency->sczKey);
        }
    }

LExit:
    ReleaseDict(sdIgnoreDependencies);

    return hr;
}

/********************************************************************
 GetIgnoredDependents - Combines the current bundle's
  provider key, packages' provider keys that are being uninstalled,
  and any ignored dependencies authored for packages into a string
  list to pass to deputil.

*********************************************************************/
static HRESULT GetIgnoredDependents(
    __in const BURN_PACKAGE* pPackage,
    __in const BURN_PLAN* pPlan,
    __deref_inout STRINGDICT_HANDLE* psdIgnoredDependents
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczIgnoreDependencies = NULL;

    // Create the dictionary and add the bundle provider key initially.
    hr = DictCreateStringList(psdIgnoredDependents, INITIAL_STRINGDICT_SIZE, DICT_FLAG_CASEINSENSITIVE);
    ExitOnFailure(hr, "Failed to create the string dictionary.");

    hr = DictAddKey(*psdIgnoredDependents, pPlan->wzBundleProviderKey);
    ExitOnFailure(hr, "Failed to add the bundle provider key \"%ls\" to the list of ignored dependencies.", pPlan->wzBundleProviderKey);

    // Add previously planned package providers to the dictionary.
    for (DWORD i = 0; i < pPlan->cPlannedProviders; ++i)
    {
        const DEPENDENCY* pDependency = &pPlan->rgPlannedProviders[i];

        hr = DictAddKey(*psdIgnoredDependents, pDependency->sczKey);
        ExitOnFailure(hr, "Failed to add the package provider key \"%ls\" to the list of ignored dependencies.", pDependency->sczKey);
    }

    // Get the IGNOREDEPENDENCIES property if defined.
    hr = PackageGetProperty(pPackage, DEPENDENCY_IGNOREDEPENDENCIES, &sczIgnoreDependencies);
    if (E_NOTFOUND != hr)
    {
        ExitOnFailure(hr, "Failed to get the package property: %ls", DEPENDENCY_IGNOREDEPENDENCIES);

        // TODO: this is the raw value of the property, all property values are currently formatted in a different part of planning.
        hr = DependencyAddIgnoreDependencies(*psdIgnoredDependents, sczIgnoreDependencies);
        ExitOnFailure(hr, "Failed to add the authored ignored dependencies to the cumulative list of ignored dependencies.");
    }
    else
    {
        hr = S_OK;
    }

LExit:
    ReleaseStr(sczIgnoreDependencies);

    return hr;
}

/********************************************************************
 GetProviderExists - Gets whether the provider key is registered.

*********************************************************************/
static BOOL GetProviderExists(
    __in HKEY hkRoot,
    __in_z LPCWSTR wzProviderKey
    )
{
    HRESULT hr = DepGetProviderInformation(hkRoot, wzProviderKey, NULL, NULL, NULL);
    return SUCCEEDED(hr);
}

/********************************************************************
 CalculateDependencyActionStates - Calculates the dependency execute and
  rollback actions for a package.

*********************************************************************/
static void CalculateDependencyActionStates(
    __in const BURN_PACKAGE* pPackage,
    __out BURN_DEPENDENCY_ACTION* pDependencyExecuteAction,
    __out BURN_DEPENDENCY_ACTION* pDependencyRollbackAction
    )
{
    switch (pPackage->execute)
    {
    case BOOTSTRAPPER_ACTION_STATE_NONE:
        switch (pPackage->requested)
        {
        case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
            // Unregister if the package is not requested but already not installed.
            *pDependencyExecuteAction = BURN_DEPENDENCY_ACTION_UNREGISTER;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_NONE:
            // Register if a newer, compatible package is already installed.
            switch (pPackage->currentState)
            {
            case BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE: __fallthrough;
            case BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED:
                *pDependencyExecuteAction = BURN_DEPENDENCY_ACTION_REGISTER;
                break;
            }
            break;
        case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            // Register if the package is requested but already installed.
            *pDependencyExecuteAction = BURN_DEPENDENCY_ACTION_REGISTER;
            break;
        }
        break;
    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        *pDependencyExecuteAction = BURN_DEPENDENCY_ACTION_UNREGISTER;
        break;
    case BOOTSTRAPPER_ACTION_STATE_INSTALL: __fallthrough;
    case BOOTSTRAPPER_ACTION_STATE_MODIFY: __fallthrough;
    case BOOTSTRAPPER_ACTION_STATE_REPAIR: __fallthrough;
    case BOOTSTRAPPER_ACTION_STATE_MINOR_UPGRADE:
        *pDependencyExecuteAction = BURN_DEPENDENCY_ACTION_REGISTER;
        break;
    }

    switch (*pDependencyExecuteAction)
    {
    case BURN_DEPENDENCY_ACTION_REGISTER:
        *pDependencyRollbackAction = BURN_DEPENDENCY_ACTION_UNREGISTER;
        break;
    case BURN_DEPENDENCY_ACTION_UNREGISTER:
        *pDependencyRollbackAction = BURN_DEPENDENCY_ACTION_REGISTER;
        break;
    }
}

/********************************************************************
 AddPackageDependencyActions - Adds the dependency execute and rollback
  actions to the plan.

*********************************************************************/
static HRESULT AddPackageDependencyActions(
    __in_opt DWORD *pdwInsertSequence,
    __in const BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in const BURN_DEPENDENCY_ACTION dependencyExecuteAction,
    __in const BURN_DEPENDENCY_ACTION dependencyRollbackAction
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;

    // Add the rollback plan.
    if (BURN_DEPENDENCY_ACTION_NONE != dependencyRollbackAction)
    {
        hr = PlanAppendRollbackAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY;
        pAction->packageDependency.pPackage = const_cast<BURN_PACKAGE*>(pPackage);

        hr = StrAllocString(&pAction->packageDependency.sczBundleProviderKey, pPlan->wzBundleProviderKey, 0);
        ExitOnFailure(hr, "Failed to copy the bundle dependency provider.");

        // Put a checkpoint before the execute action so that rollback happens
        // if execute fails.
        hr = PlanExecuteCheckpoint(pPlan);
        ExitOnFailure(hr, "Failed to plan dependency checkpoint action.");
    }

    // Add the execute plan. This comes after rollback so if something goes wrong
    // rollback will try to clean up after us correctly.
    if (BURN_DEPENDENCY_ACTION_NONE != dependencyExecuteAction)
    {
        if (NULL != pdwInsertSequence)
        {
            hr = PlanInsertExecuteAction(*pdwInsertSequence, pPlan, &pAction);
            ExitOnFailure(hr, "Failed to insert execute action.");

            // Always move the sequence after this dependency action so the dependency registration
            // stays in front of the inserted actions.
            ++(*pdwInsertSequence);
        }
        else
        {
            hr = PlanAppendExecuteAction(pPlan, &pAction);
            ExitOnFailure(hr, "Failed to append execute action.");
        }

        pAction->type = BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY;
        pAction->packageDependency.pPackage = const_cast<BURN_PACKAGE*>(pPackage);

        hr = StrAllocString(&pAction->packageDependency.sczBundleProviderKey, pPlan->wzBundleProviderKey, 0);
        ExitOnFailure(hr, "Failed to copy the bundle dependency provider.");
    }

LExit:
    return hr;
}

static LPCWSTR GetPackageProviderId(
    __in const BURN_PACKAGE* pPackage
    )
{
    LPCWSTR wzId = NULL;

    switch (pPackage->type)
    {
    case BURN_PACKAGE_TYPE_MSI:
        wzId = pPackage->Msi.sczProductCode;
        break;
    case BURN_PACKAGE_TYPE_MSP:
        wzId = pPackage->Msp.sczPatchCode;
        break;
    }

    return wzId;
}

static HRESULT RegisterPackageProvider(
    __in const BURN_DEPENDENCY_PROVIDER* pProvider,
    __in LPCWSTR wzPackageId,
    __in LPCWSTR wzPackageProviderId,
    __in HKEY hkRoot,
    __in BOOL fVital
    )
{
    HRESULT hr = S_OK;

    LogId(REPORT_VERBOSE, MSG_DEPENDENCY_PACKAGE_REGISTER, pProvider->sczKey, pProvider->sczVersion, wzPackageId);

    hr = DepRegisterDependency(hkRoot, pProvider->sczKey, pProvider->sczVersion, pProvider->sczDisplayName, wzPackageProviderId, 0);
    ExitOnFailure(hr, "Failed to register the package dependency provider: %ls", pProvider->sczKey);

LExit:
    if (!fVital)
    {
        hr = S_OK;
    }

    return hr;
}

/********************************************************************
 UnregisterPackageProvider - Removes the dependency provider.

 Note: Does not check for existing dependents before removing the key.
*********************************************************************/
static void UnregisterPackageProvider(
    __in const BURN_DEPENDENCY_PROVIDER* pProvider,
    __in LPCWSTR wzPackageId,
    __in HKEY hkRoot
    )
{
    HRESULT hr = S_OK;

    hr = DepUnregisterDependency(hkRoot, pProvider->sczKey);
    if (SUCCEEDED(hr) || E_FILENOTFOUND == hr)
    {
        LogId(REPORT_VERBOSE, MSG_DEPENDENCY_PACKAGE_UNREGISTERED, pProvider->sczKey, wzPackageId);
    }
    else
    {
        LogId(REPORT_VERBOSE, MSG_DEPENDENCY_PACKAGE_UNREGISTERED_FAILED, pProvider->sczKey, wzPackageId, hr);
    }
}

/********************************************************************
 RegisterPackageProviderDependent - Registers the provider key
  as a dependent of a package provider.

*********************************************************************/
static HRESULT RegisterPackageProviderDependent(
    __in const BURN_DEPENDENCY_PROVIDER* pProvider,
    __in BOOL fVital,
    __in HKEY hkRoot,
    __in LPCWSTR wzPackageId,
    __in_z LPCWSTR wzDependentProviderKey
    )
{
    HRESULT hr = S_OK;
    BOOL fExists = FALSE;

    LogId(REPORT_VERBOSE, MSG_DEPENDENCY_PACKAGE_REGISTER_DEPENDENCY, wzDependentProviderKey, pProvider->sczKey, wzPackageId);

    hr = DepRegisterDependent(hkRoot, pProvider->sczKey, wzDependentProviderKey, NULL, NULL, 0);
    ExitOnPathFailure(hr, fExists, "Failed to register the dependency on package dependency provider: %ls", pProvider->sczKey);

    if (!fExists)
    {
        LogId(REPORT_VERBOSE, MSG_DEPENDENCY_PACKAGE_SKIP_MISSING, pProvider->sczKey, wzPackageId);
    }

LExit:
    if (!fVital)
    {
        hr = S_OK;
    }

    return hr;
}

/********************************************************************
 UnregisterPackageDependency - Unregisters the provider key
  as a dependent of a package.

*********************************************************************/
static void UnregisterPackageDependency(
    __in BOOL fPerMachine,
    __in const BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzDependentProviderKey
    )
{
    HKEY hkRoot = fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;

    // Should be no registration to remove since we don't write keys across contexts.
    if (fPerMachine != pPackage->fPerMachine)
    {
        LogId(REPORT_STANDARD, MSG_DEPENDENCY_PACKAGE_SKIP_WRONGSCOPE, pPackage->sczId, LoggingPerMachineToString(fPerMachine), LoggingPerMachineToString(pPackage->fPerMachine));
        return;
    }

    // Loop through each package provider and remove the bundle dependency key.
    if (pPackage->rgDependencyProviders)
    {
        for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
        {
            const BURN_DEPENDENCY_PROVIDER* pProvider = &pPackage->rgDependencyProviders[i];

            UnregisterPackageProviderDependent(pProvider, hkRoot, pPackage->sczId, wzDependentProviderKey);
        }
    }
}

static void UnregisterPackageProviderDependent(
    __in const BURN_DEPENDENCY_PROVIDER* pProvider,
    __in HKEY hkRoot,
    __in LPCWSTR wzPackageId,
    __in_z LPCWSTR wzDependentProviderKey
    )
{
    HRESULT hr = S_OK;

    hr = DepUnregisterDependent(hkRoot, pProvider->sczKey, wzDependentProviderKey);
    if (SUCCEEDED(hr) || E_FILENOTFOUND == hr)
    {
        LogId(REPORT_VERBOSE, MSG_DEPENDENCY_PACKAGE_UNREGISTERED_DEPENDENCY, wzDependentProviderKey, pProvider->sczKey, wzPackageId);
    }
    else
    {
        LogId(REPORT_VERBOSE, MSG_DEPENDENCY_PACKAGE_UNREGISTERED_DEPENDENCY_FAILED, wzDependentProviderKey, pProvider->sczKey, wzPackageId, hr);
    }
}

static void UnregisterOrphanPackageProviders(
    __in const BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    DEPENDENCY* rgDependents = NULL;
    UINT cDependents = 0;
    HKEY hkRoot = pPackage->fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;

    for (DWORD i = 0; i < pPackage->cDependencyProviders; ++i)
    {
        const BURN_DEPENDENCY_PROVIDER* pProvider = &pPackage->rgDependencyProviders[i];

        // Skip providers not owned by the bundle.
        if (pProvider->fImported)
        {
            continue;
        }

        hr = DepCheckDependents(hkRoot, pProvider->sczKey, 0, NULL, &rgDependents, &cDependents);
        if (SUCCEEDED(hr) && !cDependents)
        {
            UnregisterPackageProvider(pProvider, pPackage->sczId, hkRoot);
        }

        ReleaseDependencyArray(rgDependents, cDependents);
        rgDependents = NULL;
        cDependents = 0;
    }
}
