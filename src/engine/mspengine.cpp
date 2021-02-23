// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// constants


// structs

struct POSSIBLE_TARGETPRODUCT
{
    WCHAR wzProductCode[39];
    LPWSTR pszLocalPackage;
    MSIINSTALLCONTEXT context;
};

// internal function declarations

static HRESULT GetPossibleTargetProductCodes(
    __in BURN_PACKAGES*     pPackages,
    __deref_inout_ecount_opt(*pcPossibleTargetProductCodes) POSSIBLE_TARGETPRODUCT** prgPossibleTargetProductCodes,
    __inout DWORD* pcPossibleTargetProductCodes
    );
static HRESULT AddPossibleTargetProduct(
    __in STRINGDICT_HANDLE sdUniquePossibleTargetProductCodes,
    __in_z LPCWSTR wzPossibleTargetProductCode,
    __in MSIINSTALLCONTEXT context,
    __deref_inout_ecount_opt(*pcPossibleTargetProducts) POSSIBLE_TARGETPRODUCT** prgPossibleTargetProducts,
    __inout DWORD* pcPossibleTargetProducts
    );
static HRESULT AddDetectedTargetProduct(
    __in BURN_PACKAGE* pPackage,
    __in DWORD dwOrder,
    __in_z LPCWSTR wzProductCode,
    __in MSIINSTALLCONTEXT context,
    __out DWORD* pdwTargetProductIndex
    );
static HRESULT AddMsiChainedPatch(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PACKAGE* pMspPackage,
    __in DWORD dwMspTargetProductIndex,
    __out DWORD* pdwChainedPatchIndex
    );
static HRESULT DeterminePatchChainedTarget(
    __in BURN_PACKAGES* pPackages,
    __in BURN_PACKAGE* pMspPackage,
    __in LPCWSTR wzTargetProductCode,
    __in DWORD dwMspTargetProductIndex
    );
static HRESULT PlanTargetProduct(
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_ACTION_STATE actionState,
    __in BURN_PACKAGE* pPackage,
    __in BURN_MSPTARGETPRODUCT* pTargetProduct,
    __in_opt HANDLE hCacheEvent
    );


// function definitions

extern "C" HRESULT MspEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnMspPackage,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;

    // @PatchCode
    hr = XmlGetAttributeEx(pixnMspPackage, L"PatchCode", &pPackage->Msp.sczPatchCode);
    ExitOnFailure(hr, "Failed to get @PatchCode.");

    // @PatchXml
    hr = XmlGetAttributeEx(pixnMspPackage, L"PatchXml", &pPackage->Msp.sczApplicabilityXml);
    ExitOnFailure(hr, "Failed to get @PatchXml.");

    // Read properties.
    hr = MsiEngineParsePropertiesFromXml(pixnMspPackage, &pPackage->Msp.rgProperties, &pPackage->Msp.cProperties);
    ExitOnFailure(hr, "Failed to parse properties from XML.");

LExit:

    return hr;
}

extern "C" void MspEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    )
{
    ReleaseStr(pPackage->Msp.sczPatchCode);
    ReleaseStr(pPackage->Msp.sczApplicabilityXml);

    // free properties
    if (pPackage->Msp.rgProperties)
    {
        for (DWORD i = 0; i < pPackage->Msp.cProperties; ++i)
        {
            BURN_MSIPROPERTY* pProperty = &pPackage->Msp.rgProperties[i];

            ReleaseStr(pProperty->sczId);
            ReleaseStr(pProperty->sczValue);
            ReleaseStr(pProperty->sczRollbackValue);
        }
        MemFree(pPackage->Msp.rgProperties);
    }

    // free target products
    ReleaseMem(pPackage->Msp.rgTargetProducts);

    // clear struct
    memset(&pPackage->Msp, 0, sizeof(pPackage->Msp));
}

extern "C" HRESULT MspEngineDetectInitialize(
    __in BURN_PACKAGES* pPackages
    )
{
    AssertSz(pPackages->cPatchInfo, "MspEngineDetectInitialize() should only be called if there are MSP packages.");

    HRESULT hr = S_OK;
    POSSIBLE_TARGETPRODUCT* rgPossibleTargetProducts = NULL;
    DWORD cPossibleTargetProducts = 0;

#ifdef DEBUG
    // All patch info should be initialized to zero.
    for (DWORD i = 0; i < pPackages->cPatchInfo; ++i)
    {
        BURN_PACKAGE* pPackage = pPackages->rgPatchInfoToPackage[i];
        Assert(!pPackage->Msp.cTargetProductCodes);
        Assert(!pPackage->Msp.rgTargetProducts);
    }
#endif

    // Figure out which product codes to target on the machine. In the worst case all products on the machine
    // will be returned.
    hr = GetPossibleTargetProductCodes(pPackages, &rgPossibleTargetProducts, &cPossibleTargetProducts);
    ExitOnFailure(hr, "Failed to get possible target product codes.");

    // Loop through possible target products, testing the collective patch applicability against each product in
    // the appropriate context. Store the result with the appropriate patch package.
    for (DWORD iSearch = 0; iSearch < cPossibleTargetProducts; ++iSearch)
    {
        const POSSIBLE_TARGETPRODUCT* pPossibleTargetProduct = rgPossibleTargetProducts + iSearch;

        LogId(REPORT_STANDARD, MSG_DETECT_CALCULATE_PATCH_APPLICABILITY, pPossibleTargetProduct->wzProductCode, LoggingMsiInstallContext(pPossibleTargetProduct->context));

        if (pPossibleTargetProduct->pszLocalPackage)
        {
            // Ignores current machine state to determine just patch applicability.
            // Superseded and obsolesced patches will be planned separately.
            hr = WiuDetermineApplicablePatches(pPossibleTargetProduct->pszLocalPackage, pPackages->rgPatchInfo, pPackages->cPatchInfo);
        }
        else
        {
            hr = WiuDeterminePatchSequence(pPossibleTargetProduct->wzProductCode, NULL, pPossibleTargetProduct->context, pPackages->rgPatchInfo, pPackages->cPatchInfo);
        }

        if (SUCCEEDED(hr))
        {
            for (DWORD iPatchInfo = 0; iPatchInfo < pPackages->cPatchInfo; ++iPatchInfo)
            {
                hr = HRESULT_FROM_WIN32(pPackages->rgPatchInfo[iPatchInfo].uStatus);
                BURN_PACKAGE* pMspPackage = pPackages->rgPatchInfoToPackage[iPatchInfo];
                Assert(BURN_PACKAGE_TYPE_MSP == pMspPackage->type);

                if (S_OK == hr)
                {
                    // Note that we do add superseded and obsolete MSP packages. Package Detect and Plan will sort them out later.
                    hr = MspEngineAddDetectedTargetProduct(pPackages, pMspPackage, pPackages->rgPatchInfo[iPatchInfo].dwOrder, pPossibleTargetProduct->wzProductCode, pPossibleTargetProduct->context);
                    ExitOnFailure(hr, "Failed to add target product code to package: %ls", pMspPackage->sczId);
                }
                else
                {
                    LogStringLine(REPORT_DEBUG, "      0x%x: Patch applicability failed for package: %ls", hr, pMspPackage->sczId);
                }
            }
        }
        else
        {
            LogId(REPORT_STANDARD, MSG_DETECT_FAILED_CALCULATE_PATCH_APPLICABILITY, pPossibleTargetProduct->wzProductCode, LoggingMsiInstallContext(pPossibleTargetProduct->context), hr);
        }

        hr = S_OK; // always reset so we test all possible target products.
    }

LExit:
    if (rgPossibleTargetProducts)
    {
        for (DWORD i = 0; i < cPossibleTargetProducts; ++i)
        {
            ReleaseStr(rgPossibleTargetProducts[i].pszLocalPackage);
        }
        MemFree(rgPossibleTargetProducts);
    }

    return hr;
}

extern "C" HRESULT MspEngineAddDetectedTargetProduct(
    __in BURN_PACKAGES* pPackages,
    __in BURN_PACKAGE* pPackage,
    __in DWORD dwOrder,
    __in_z LPCWSTR wzProductCode,
    __in MSIINSTALLCONTEXT context
    )
{
    HRESULT hr = S_OK;
    DWORD dwTargetProductIndex = 0;

    hr = AddDetectedTargetProduct(pPackage, dwOrder, wzProductCode, context, &dwTargetProductIndex);
    ExitOnFailure(hr, "Failed to add detected target product.");

    hr = DeterminePatchChainedTarget(pPackages, pPackage, wzProductCode, dwTargetProductIndex);
    ExitOnFailure(hr, "Failed to determine patch chained target.");

LExit:
    return hr;
}

extern "C" HRESULT MspEngineAddMissingSlipstreamTarget(
    __in BURN_PACKAGE* pMsiPackage,
    __in BURN_SLIPSTREAM_MSP* pSlipstreamMsp
    )
{
    HRESULT hr = S_OK;
    DWORD dwTargetProductIndex = 0;
    BURN_MSPTARGETPRODUCT* pTargetProduct = NULL;
    DWORD dwChainedPatchIndex = 0;

    hr = AddDetectedTargetProduct(pSlipstreamMsp->pMspPackage, 0, pMsiPackage->Msi.sczProductCode, pMsiPackage->fPerMachine ? MSIINSTALLCONTEXT_MACHINE : MSIINSTALLCONTEXT_USERUNMANAGED, &dwTargetProductIndex);
    ExitOnFailure(hr, "Failed to add missing slipstream target.");

    pTargetProduct = pSlipstreamMsp->pMspPackage->Msp.rgTargetProducts + dwTargetProductIndex;
    pTargetProduct->fSlipstream = TRUE;
    pTargetProduct->fSlipstreamRequired = TRUE;
    pTargetProduct->pChainedTargetPackage = pMsiPackage;

    hr = AddMsiChainedPatch(pMsiPackage, pSlipstreamMsp->pMspPackage, dwTargetProductIndex, &dwChainedPatchIndex);
    ExitOnFailure(hr, "Failed to add chained patch.");

    pSlipstreamMsp->dwMsiChainedPatchIndex = dwChainedPatchIndex;

LExit:
    return hr;
}

extern "C" HRESULT MspEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczState = NULL;

    if (pPackage->fCanAffectRegistration)
    {
        pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
    }

    if (0 == pPackage->Msp.cTargetProductCodes)
    {
        pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT;
    }
    else
    {
        // Start the package state at the highest state then loop through all the
        // target product codes and end up setting the current state to the lowest
        // package state applied to the target product codes.
        pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED;

        for (DWORD i = 0; i < pPackage->Msp.cTargetProductCodes; ++i)
        {
            BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + i;

            hr = WiuGetPatchInfoEx(pPackage->Msp.sczPatchCode, pTargetProduct->wzTargetProductCode, NULL, pTargetProduct->context, INSTALLPROPERTY_PATCHSTATE, &sczState);
            if (SUCCEEDED(hr))
            {
                switch (*sczState)
                {
                case '1':
                    pTargetProduct->fInstalled = TRUE;
                    pTargetProduct->patchPackageState = BOOTSTRAPPER_PACKAGE_STATE_PRESENT;
                    break;

                case '2':
                    pTargetProduct->fInstalled = TRUE;
                    pTargetProduct->patchPackageState = BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED;
                    break;

                case '4':
                    pTargetProduct->fInstalled = TRUE;
                    pTargetProduct->patchPackageState = BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE;
                    break;

                default:
                    pTargetProduct->patchPackageState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT;
                    break;
                }
            }
            else if (HRESULT_FROM_WIN32(ERROR_UNKNOWN_PATCH) == hr || HRESULT_FROM_WIN32(ERROR_UNKNOWN_PRODUCT) == hr)
            {
                pTargetProduct->patchPackageState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT;
                hr = S_OK;
            }
            ExitOnFailure(hr, "Failed to get patch information for patch code: %ls, target product code: %ls", pPackage->Msp.sczPatchCode, pTargetProduct->wzTargetProductCode);

            if (pPackage->currentState > pTargetProduct->patchPackageState)
            {
                pPackage->currentState = pTargetProduct->patchPackageState;
            }

            if (pPackage->fCanAffectRegistration)
            {
                pTargetProduct->registrationState = pTargetProduct->fInstalled ? BURN_PACKAGE_REGISTRATION_STATE_PRESENT : BURN_PACKAGE_REGISTRATION_STATE_ABSENT;

                if (pTargetProduct->fInstalled)
                {
                    pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
                }
            }

            hr = UserExperienceOnDetectPatchTarget(pUserExperience, pPackage->sczId, pTargetProduct->wzTargetProductCode, pTargetProduct->patchPackageState);
            ExitOnRootFailure(hr, "BA aborted detect patch target.");
        }
    }

LExit:
    ReleaseStr(sczState);

    return hr;
}

extern "C" HRESULT MspEnginePlanInitializePackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pPackage->Msp.cTargetProductCodes; ++i)
    {
        BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + i;

        if (!pTargetProduct->fInstalled && pTargetProduct->fSlipstreamRequired && BOOTSTRAPPER_REQUEST_STATE_PRESENT > pTargetProduct->pChainedTargetPackage->requested)
        {
            // There's no way to apply the patch if the target isn't installed.
            pTargetProduct->defaultRequested = pTargetProduct->requested = BOOTSTRAPPER_REQUEST_STATE_NONE;
            continue;
        }

        pTargetProduct->defaultRequested = pTargetProduct->requested = pPackage->requested;

        hr = UserExperienceOnPlanPatchTarget(pUserExperience, pPackage->sczId, pTargetProduct->wzTargetProductCode, &pTargetProduct->requested);
        ExitOnRootFailure(hr, "BA aborted plan patch target.");
    }

LExit:
    return hr;
}

//
// PlanCalculate - calculates the execute and rollback state for the requested package state.
//
extern "C" HRESULT MspEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage,
    __in BOOL fInsideMsiTransaction
    )
{
    HRESULT hr = S_OK;
    BOOL fWillUninstallAll = TRUE;

    for (DWORD i = 0; i < pPackage->Msp.cTargetProductCodes; ++i)
    {
        BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + i;

        BOOTSTRAPPER_ACTION_STATE execute = BOOTSTRAPPER_ACTION_STATE_NONE;
        BOOTSTRAPPER_ACTION_STATE rollback = BOOTSTRAPPER_ACTION_STATE_NONE;

        // Calculate the execute action.
        switch (pTargetProduct->patchPackageState)
        {
        case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
            switch (pTargetProduct->requested)
            {
            case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
                execute = BOOTSTRAPPER_ACTION_STATE_REPAIR;
                fWillUninstallAll = FALSE;
                break;

            case BOOTSTRAPPER_REQUEST_STATE_ABSENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_CACHE:
                execute = pPackage->fUninstallable ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
                break;

            case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
                execute = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
                break;

            default:
                execute = BOOTSTRAPPER_ACTION_STATE_NONE;
                fWillUninstallAll = FALSE;
                break;
            }
            break;

        case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
            switch (pTargetProduct->requested)
            {
            case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
                execute = BOOTSTRAPPER_ACTION_STATE_INSTALL;
                fWillUninstallAll = FALSE;
                break;

            default:
                execute = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            }
            break;

        default:
            if (pTargetProduct->fInstalled)
            {
                fWillUninstallAll = FALSE;
            }
            break;
        }

        // Calculate the rollback action if there is an execute action.
        if (BOOTSTRAPPER_ACTION_STATE_NONE != execute && !fInsideMsiTransaction)
        {
            switch (pPackage->currentState)
            {
            case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
                switch (pTargetProduct->requested)
                {
                case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT: __fallthrough;
                case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
                    rollback = BOOTSTRAPPER_ACTION_STATE_INSTALL;
                    break;

                default:
                    rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                    break;
                }
                break;

            case BOOTSTRAPPER_PACKAGE_STATE_ABSENT: __fallthrough;
            case BOOTSTRAPPER_PACKAGE_STATE_CACHED:
                switch (pTargetProduct->requested)
                {
                case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
                case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
                    rollback = pPackage->fUninstallable ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
                    break;

                default:
                    rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                    break;
                }
                break;

            default:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            }
        }

        pTargetProduct->execute = execute;
        pTargetProduct->rollback = rollback;

        // The highest aggregate action state found will be returned.
        if (pPackage->execute < execute)
        {
            pPackage->execute = execute;
        }

        if (pPackage->rollback < rollback)
        {
            pPackage->rollback = rollback;
        }
    }

    // The dependency manager will do the wrong thing if the package level action is UNINSTALL
    // when the patch will still be applied to at least one product.
    if (!fWillUninstallAll && BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pPackage->execute)
    {
        pPackage->execute = BOOTSTRAPPER_ACTION_STATE_NONE;
    }

    return hr;
}

//
// PlanAdd - adds the calculated execute and rollback actions for the package.
//
extern "C" HRESULT MspEnginePlanAddPackage(
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in_opt HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    )
{
    HRESULT hr = S_OK;

    // TODO: need to handle the case where this patch adds itself to an earlier patch's list of target products. That would
    //       essentially bump this patch earlier in the plan and we need to make sure this patch is downloaded.
    // add wait for cache
    if (hCacheEvent)
    {
        hr = PlanExecuteCacheSyncAndRollback(pPlan, pPackage, hCacheEvent, fPlanPackageCacheRollback);
        ExitOnFailure(hr, "Failed to plan package cache syncpoint");
    }

    hr = DependencyPlanPackage(NULL, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to plan package dependency actions.");

    // Plan the actions for each target product code.
    for (DWORD i = 0; i < pPackage->Msp.cTargetProductCodes; ++i)
    {
        BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + i;

        // If the dependency manager changed the action state for the patch, change the target product actions.
        if (pPackage->fDependencyManagerWasHere)
        {
            pTargetProduct->execute = pPackage->execute;
            pTargetProduct->rollback = pPackage->rollback;
        }

        if (BOOTSTRAPPER_ACTION_STATE_NONE != pTargetProduct->execute)
        {
            hr = PlanTargetProduct(display, pUserExperience, FALSE, pPlan, pLog, pVariables, pTargetProduct->execute, pPackage, pTargetProduct, hCacheEvent);
            ExitOnFailure(hr, "Failed to plan target product.");
        }

        if (BOOTSTRAPPER_ACTION_STATE_NONE != pTargetProduct->rollback)
        {
            hr = PlanTargetProduct(display, pUserExperience, TRUE, pPlan, pLog, pVariables, pTargetProduct->rollback, pPackage, pTargetProduct, hCacheEvent);
            ExitOnFailure(hr, "Failed to plan rollback target product.");
        }
    }

LExit:

    return hr;
}

extern "C" HRESULT MspEngineExecutePackage(
    __in_opt HWND hwndParent,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    WIU_MSI_EXECUTE_CONTEXT context = { };
    WIU_RESTART restart = WIU_RESTART_NONE;

    LPWSTR sczCachedDirectory = NULL;
    LPWSTR sczMspPath = NULL;
    LPWSTR sczPatches = NULL;
    LPWSTR sczProperties = NULL;
    LPWSTR sczObfuscatedProperties = NULL;

    // default to "verbose" logging
    DWORD dwLogMode = WIU_LOG_DEFAULT | INSTALLLOGMODE_VERBOSE;

    // get cached MSP paths
    for (DWORD i = 0; i < pExecuteAction->mspTarget.cOrderedPatches; ++i)
    {
        LPCWSTR wzAppend = NULL;
        BURN_PACKAGE* pMspPackage = pExecuteAction->mspTarget.rgOrderedPatches[i].pPackage;
        AssertSz(BURN_PACKAGE_TYPE_MSP == pMspPackage->type, "Invalid package type added to ordered patches.");

        if (BOOTSTRAPPER_ACTION_STATE_INSTALL == pExecuteAction->mspTarget.action)
        {
            hr = CacheGetCompletedPath(pMspPackage->fPerMachine, pMspPackage->sczCacheId, &sczCachedDirectory);
            ExitOnFailure(hr, "Failed to get cached path for MSP package: %ls", pMspPackage->sczId);

            // TODO: Figure out if this makes sense -- the variable is set to the last patch's path only
            // Best effort to set the execute package cache folder variable.
            VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, sczCachedDirectory, TRUE, FALSE);

            hr = PathConcat(sczCachedDirectory, pMspPackage->rgPayloads[0].pPayload->sczFilePath, &sczMspPath);
            ExitOnFailure(hr, "Failed to build MSP path.");

            wzAppend = sczMspPath;
        }
        else // uninstall
        {
            wzAppend = pMspPackage->Msp.sczPatchCode;
        }

        if (NULL != sczPatches)
        {
            hr = StrAllocConcat(&sczPatches, L";", 0);
            ExitOnFailure(hr, "Failed to semi-colon delimit patches.");
        }

        hr = StrAllocConcat(&sczPatches, wzAppend, 0);
        ExitOnFailure(hr, "Failed to append patch.");
    }

    // Best effort to set the execute package action variable.
    VariableSetNumeric(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, pExecuteAction->mspTarget.action, TRUE);

    // Wire up the external UI handler and logging.
    if (pExecuteAction->mspTarget.fDisableExternalUiHandler)
    {
        hr = WiuInitializeInternalUI(pExecuteAction->mspTarget.uiLevel, hwndParent, &context);
        ExitOnFailure(hr, "Failed to initialize internal UI for MSP package.");
    }
    else
    {
        hr = WiuInitializeExternalUI(pfnMessageHandler, pExecuteAction->mspTarget.uiLevel, hwndParent, pvContext, fRollback, &context);
        ExitOnFailure(hr, "Failed to initialize external UI handler.");
    }

    //if (BURN_LOGGING_LEVEL_DEBUG == logLevel)
    //{
    //    dwLogMode | INSTALLLOGMODE_EXTRADEBUG;
    //}

    if (pExecuteAction->mspTarget.sczLogPath && *pExecuteAction->mspTarget.sczLogPath)
    {
        hr = WiuEnableLog(dwLogMode, pExecuteAction->mspTarget.sczLogPath, 0);
        ExitOnFailure(hr, "Failed to enable logging for package: %ls to: %ls", pExecuteAction->mspTarget.pPackage->sczId, pExecuteAction->mspTarget.sczLogPath);
    }

    // set up properties
    hr = MsiEngineConcatProperties(pExecuteAction->mspTarget.pPackage->Msp.rgProperties, pExecuteAction->mspTarget.pPackage->Msp.cProperties, pVariables, fRollback, &sczProperties, FALSE);
    ExitOnFailure(hr, "Failed to add properties to argument string.");

    hr = MsiEngineConcatProperties(pExecuteAction->mspTarget.pPackage->Msp.rgProperties, pExecuteAction->mspTarget.pPackage->Msp.cProperties, pVariables, fRollback, &sczObfuscatedProperties, TRUE);
    ExitOnFailure(hr, "Failed to add properties to obfuscated argument string.");

    hr = MsiEngineConcatActionProperty(pExecuteAction->mspTarget.actionMsiProperty, &sczProperties);
    ExitOnFailure(hr, "Failed to add action property to argument string.");

    hr = MsiEngineConcatActionProperty(pExecuteAction->mspTarget.actionMsiProperty, &sczObfuscatedProperties);
    ExitOnFailure(hr, "Failed to add action property to obfuscated argument string.");

    LogId(REPORT_STANDARD, MSG_APPLYING_PATCH_PACKAGE, pExecuteAction->mspTarget.pPackage->sczId, LoggingActionStateToString(pExecuteAction->mspTarget.action), sczPatches, sczObfuscatedProperties, pExecuteAction->mspTarget.sczTargetProductCode);

    //
    // Do the actual action.
    //
    switch (pExecuteAction->mspTarget.action)
    {
    case BOOTSTRAPPER_ACTION_STATE_INSTALL: __fallthrough;
    case BOOTSTRAPPER_ACTION_STATE_REPAIR:
        hr = StrAllocConcatSecure(&sczProperties, L" PATCH=\"", 0);
        ExitOnFailure(hr, "Failed to add PATCH property on install.");

        hr = StrAllocConcatSecure(&sczProperties, sczPatches, 0);
        ExitOnFailure(hr, "Failed to add patches to PATCH property on install.");

        hr = StrAllocConcatSecure(&sczProperties, L"\" REBOOT=ReallySuppress", 0);
        ExitOnFailure(hr, "Failed to add reboot suppression property on install.");

        hr = WiuConfigureProductEx(pExecuteAction->mspTarget.sczTargetProductCode, INSTALLLEVEL_DEFAULT, INSTALLSTATE_DEFAULT, sczProperties, &restart);
        ExitOnFailure(hr, "Failed to install MSP package.");
        break;

    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        hr = StrAllocConcatSecure(&sczProperties, L" REBOOT=ReallySuppress", 0);
        ExitOnFailure(hr, "Failed to add reboot suppression property on uninstall.");

        // Ignore all dependencies, since the Burn engine already performed the check.
        hr = StrAllocFormattedSecure(&sczProperties, L"%ls %ls=ALL", sczProperties, DEPENDENCY_IGNOREDEPENDENCIES);
        ExitOnFailure(hr, "Failed to add the list of dependencies to ignore to the properties.");

        hr = WiuRemovePatches(sczPatches, pExecuteAction->mspTarget.sczTargetProductCode, sczProperties, &restart);
        ExitOnFailure(hr, "Failed to uninstall MSP package.");
        break;
    }

LExit:
    WiuUninitializeExternalUI(&context);

    ReleaseStr(sczCachedDirectory);
    ReleaseStr(sczMspPath);
    StrSecureZeroFreeString(sczProperties);
    ReleaseStr(sczObfuscatedProperties);
    ReleaseStr(sczPatches);

    switch (restart)
    {
        case WIU_RESTART_NONE:
            *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;
            break;

        case WIU_RESTART_REQUIRED:
            *pRestart = BOOTSTRAPPER_APPLY_RESTART_REQUIRED;
            break;

        case WIU_RESTART_INITIATED:
            *pRestart = BOOTSTRAPPER_APPLY_RESTART_INITIATED;
            break;
    }

    // Best effort to clear the execute package cache folder and action variables.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, NULL, TRUE, FALSE);
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, NULL, TRUE, FALSE);

    return hr;
}

extern "C" void MspEngineUpdateInstallRegistrationState(
    __in BURN_EXECUTE_ACTION* pAction,
    __in HRESULT hrExecute,
    __in BOOL fInsideMsiTransaction
    )
{
    BURN_PACKAGE_REGISTRATION_STATE newState = BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN;

    if (FAILED(hrExecute))
    {
        ExitFunction();
    }

    if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pAction->mspTarget.action)
    {
        newState = BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
    }
    else
    {
        newState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
    }

    for (DWORD i = 0; i < pAction->mspTarget.cOrderedPatches; ++i)
    {
        BURN_ORDERED_PATCHES* pOrderedPatches = pAction->mspTarget.rgOrderedPatches + i;
        BURN_PACKAGE* pPackage = pOrderedPatches->pPackage;
        BURN_MSPTARGETPRODUCT* pTargetProduct = NULL;

        Assert(BURN_PACKAGE_TYPE_MSP == pPackage->type);

        if (!pPackage->fCanAffectRegistration)
        {
            continue;
        }

        for (DWORD j = 0; j < pPackage->Msp.cTargetProductCodes; ++j)
        {
            pTargetProduct = pPackage->Msp.rgTargetProducts + j;
            if (pAction->mspTarget.fPerMachineTarget == (MSIINSTALLCONTEXT_MACHINE == pTargetProduct->context) &&
                CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pAction->mspTarget.sczTargetProductCode, -1, pTargetProduct->wzTargetProductCode, -1))
            {
                break;
            }

            pTargetProduct = NULL;
        }

        if (!pTargetProduct)
        {
            AssertSz(pTargetProduct, "Ordered patch didn't have corresponding target product");
            continue;
        }

        if (fInsideMsiTransaction)
        {
            pTargetProduct->transactionRegistrationState = newState;
        }
        else
        {
            pTargetProduct->registrationState = newState;
        }
    }

LExit:
    return;
}

extern "C" void MspEngineFinalizeInstallRegistrationState(
    __in BURN_PACKAGE* pPackage
    )
{
    if (!pPackage->fCanAffectRegistration)
    {
        ExitFunction();
    }

    if (!pPackage->Msp.cTargetProductCodes)
    {
        pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
    }
    else
    {
        pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN;

        for (DWORD i = 0; i < pPackage->Msp.cTargetProductCodes; ++i)
        {
            BURN_MSPTARGETPRODUCT* pTargetProduct = pPackage->Msp.rgTargetProducts + i;

            if (pPackage->installRegistrationState < pTargetProduct->registrationState)
            {
                pPackage->installRegistrationState = pTargetProduct->registrationState;
            }
        }
    }

LExit:
    return;
}


// internal helper functions

static HRESULT GetPossibleTargetProductCodes(
    __in BURN_PACKAGES* pPackages,
    __deref_inout_ecount_opt(*pcPossibleTargetProducts) POSSIBLE_TARGETPRODUCT** prgPossibleTargetProducts,
    __inout DWORD* pcPossibleTargetProducts
    )
{
    HRESULT hr = S_OK;
    STRINGDICT_HANDLE sdUniquePossibleTargetProductCodes = NULL;
    BOOL fCheckAll = FALSE;
    WCHAR wzPossibleTargetProductCode[MAX_GUID_CHARS + 1];

    // Use a dictionary to ensure we capture unique product codes. Otherwise, we could end up
    // doing patch applicability for the same product code multiple times and that would confuse
    // everything down stream.
    hr = DictCreateStringList(&sdUniquePossibleTargetProductCodes, 5, DICT_FLAG_NONE);
    ExitOnFailure(hr, "Failed to create unique target product codes.");

    // If the patches target a specific set of product/upgrade codes, search only those. This
    // should be much faster than searching all packages on the machine.
    if (pPackages->rgPatchTargetCodes)
    {
        for (DWORD i = 0; i < pPackages->cPatchTargetCodes; ++i)
        {
            BURN_PATCH_TARGETCODE* pTargetCode = pPackages->rgPatchTargetCodes + i;

            // If targeting a product, add the unique product code to the list.
            if (BURN_PATCH_TARGETCODE_TYPE_PRODUCT == pTargetCode->type)
            {
                hr = AddPossibleTargetProduct(sdUniquePossibleTargetProductCodes, pTargetCode->sczTargetCode, MSIINSTALLCONTEXT_NONE, prgPossibleTargetProducts, pcPossibleTargetProducts);
                ExitOnFailure(hr, "Failed to add product code to possible target product codes.");
            }
            else if (BURN_PATCH_TARGETCODE_TYPE_UPGRADE == pTargetCode->type)
            {
                // Enumerate all unique related products to the target upgrade code.
                for (DWORD iProduct = 0; SUCCEEDED(hr); ++iProduct)
                {
                    hr = WiuEnumRelatedProducts(pTargetCode->sczTargetCode, iProduct, wzPossibleTargetProductCode);
                    if (SUCCEEDED(hr))
                    {
                        hr = AddPossibleTargetProduct(sdUniquePossibleTargetProductCodes, wzPossibleTargetProductCode, MSIINSTALLCONTEXT_NONE, prgPossibleTargetProducts, pcPossibleTargetProducts);
                        ExitOnFailure(hr, "Failed to add upgrade product code to possible target product codes.");
                    }
                    else if (E_BADCONFIGURATION == hr)
                    {
                        // Skip product's with bad configuration and continue.
                        LogId(REPORT_STANDARD, MSG_DETECT_BAD_PRODUCT_CONFIGURATION, wzPossibleTargetProductCode);

                        hr = S_OK;
                    }
                }

                if (E_NOMOREITEMS == hr)
                {
                    hr = S_OK;
                }
                ExitOnFailure(hr, "Failed to enumerate all products to patch related to upgrade code: %ls", pTargetCode->sczTargetCode);
            }
            else
            {
                // The element does not target a specific product.
                fCheckAll = TRUE;

                break;
            }
        }
    }
    else
    {
        fCheckAll = TRUE;
    }

    // One or more of the patches do not target a specific product so search everything on the machine.
    if (fCheckAll)
    {
        for (DWORD iProduct = 0; SUCCEEDED(hr); ++iProduct)
        {
            MSIINSTALLCONTEXT context = MSIINSTALLCONTEXT_NONE;

            hr = WiuEnumProductsEx(NULL, NULL, MSIINSTALLCONTEXT_ALL, iProduct, wzPossibleTargetProductCode, &context, NULL, NULL);
            if (SUCCEEDED(hr))
            {
                hr = AddPossibleTargetProduct(sdUniquePossibleTargetProductCodes, wzPossibleTargetProductCode, context, prgPossibleTargetProducts, pcPossibleTargetProducts);
                ExitOnFailure(hr, "Failed to add product code to search product codes.");
            }
            else if (E_BADCONFIGURATION == hr)
            {
                // Skip products with bad configuration and continue.
                LogId(REPORT_STANDARD, MSG_DETECT_BAD_PRODUCT_CONFIGURATION, wzPossibleTargetProductCode);

                hr = S_OK;
            }
        }

        if (E_NOMOREITEMS == hr)
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "Failed to enumerate all products on the machine for patches applicability.");
    }

LExit:
    ReleaseDict(sdUniquePossibleTargetProductCodes);

    return hr;
}

static HRESULT AddPossibleTargetProduct(
    __in STRINGDICT_HANDLE sdUniquePossibleTargetProductCodes,
    __in_z LPCWSTR wzPossibleTargetProductCode,
    __in MSIINSTALLCONTEXT context,
    __deref_inout_ecount_opt(*pcPossibleTargetProducts) POSSIBLE_TARGETPRODUCT** prgPossibleTargetProducts,
    __inout DWORD* pcPossibleTargetProducts
    )
{
    HRESULT hr = S_OK;
    LPWSTR pszLocalPackage = NULL;

    // Only add this possible target code if we haven't queried for it already.
    if (E_NOTFOUND == DictKeyExists(sdUniquePossibleTargetProductCodes, wzPossibleTargetProductCode))
    {
        // If the install context is not known, ask the Windows Installer for it. If we can't get the context
        // then bail.
        if (MSIINSTALLCONTEXT_NONE == context)
        {
            hr = WiuEnumProductsEx(wzPossibleTargetProductCode, NULL, MSIINSTALLCONTEXT_ALL, 0, NULL, &context, NULL, NULL);
            if (FAILED(hr))
            {
                ExitFunction1(hr = S_OK);
            }
        }

        hr = DictAddKey(sdUniquePossibleTargetProductCodes, wzPossibleTargetProductCode);
        ExitOnFailure(hr, "Failed to add possible target code to unique product codes.");

        hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgPossibleTargetProducts), *pcPossibleTargetProducts + 1, sizeof(POSSIBLE_TARGETPRODUCT), 3);
        ExitOnFailure(hr, "Failed to grow array of possible target products.");

        POSSIBLE_TARGETPRODUCT *const pPossibleTargetProduct = *prgPossibleTargetProducts + *pcPossibleTargetProducts;

        hr = ::StringCchCopyW(pPossibleTargetProduct->wzProductCode, countof(pPossibleTargetProduct->wzProductCode), wzPossibleTargetProductCode);
        ExitOnFailure(hr, "Failed to copy possible target product code.");

        // Attempt to get the local package path so we can more quickly determine patch applicability later.
        hr = WiuGetProductInfoEx(wzPossibleTargetProductCode, NULL, context, INSTALLPROPERTY_LOCALPACKAGE, &pszLocalPackage);
        if (SUCCEEDED(hr))
        {
            pPossibleTargetProduct->pszLocalPackage = pszLocalPackage;
            pszLocalPackage = NULL;
        }
        else
        {
            // Will instead call MsiDeterminePatchSequence later.
            hr = S_OK;
        }

        pPossibleTargetProduct->context = context;

        ++(*pcPossibleTargetProducts);
    }

LExit:
    ReleaseStr(pszLocalPackage);

    return hr;
}

static HRESULT AddDetectedTargetProduct(
    __in BURN_PACKAGE* pPackage,
    __in DWORD dwOrder,
    __in_z LPCWSTR wzProductCode,
    __in MSIINSTALLCONTEXT context,
    __out DWORD* pdwTargetProductIndex
    )
{
    HRESULT hr = S_OK;
    BURN_MSPTARGETPRODUCT* pTargetProduct = NULL;

    *pdwTargetProductIndex = BURN_PACKAGE_INVALID_PATCH_INDEX;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPackage->Msp.rgTargetProducts), pPackage->Msp.cTargetProductCodes + 1, sizeof(BURN_MSPTARGETPRODUCT), 5);
    ExitOnFailure(hr, "Failed to ensure enough target product codes were allocated.");

    pTargetProduct = pPackage->Msp.rgTargetProducts + pPackage->Msp.cTargetProductCodes;

    hr = ::StringCchCopyW(pTargetProduct->wzTargetProductCode, countof(pTargetProduct->wzTargetProductCode), wzProductCode);
    ExitOnFailure(hr, "Failed to copy target product code.");

    pTargetProduct->context = context;
    pTargetProduct->dwOrder = dwOrder;

    *pdwTargetProductIndex = pPackage->Msp.cTargetProductCodes;
    ++pPackage->Msp.cTargetProductCodes;

LExit:
    return hr;
}

static HRESULT AddMsiChainedPatch(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PACKAGE* pMspPackage,
    __in DWORD dwMspTargetProductIndex,
    __out DWORD* pdwChainedPatchIndex
    )
{
    HRESULT hr = S_OK;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPackage->Msi.rgChainedPatches), pPackage->Msi.cChainedPatches + 1, sizeof(BURN_CHAINED_PATCH), 5);
    ExitOnFailure(hr, "Failed to ensure enough chained patches were allocated.");

    BURN_CHAINED_PATCH* pChainedPatch = pPackage->Msi.rgChainedPatches + pPackage->Msi.cChainedPatches;
    pChainedPatch->pMspPackage = pMspPackage;
    pChainedPatch->dwMspTargetProductIndex = dwMspTargetProductIndex;

    *pdwChainedPatchIndex = pPackage->Msi.cChainedPatches;
    ++pPackage->Msi.cChainedPatches;
LExit:
    return hr;
}

static HRESULT DeterminePatchChainedTarget(
    __in BURN_PACKAGES* pPackages,
    __in BURN_PACKAGE* pMspPackage,
    __in LPCWSTR wzTargetProductCode,
    __in DWORD dwMspTargetProductIndex
    )
{
    HRESULT hr = S_OK;
    DWORD dwChainedPatchIndex = 0;
    BURN_MSPTARGETPRODUCT* pTargetProduct = pMspPackage->Msp.rgTargetProducts + dwMspTargetProductIndex;

    for (DWORD iPackage = 0; iPackage < pPackages->cPackages; ++iPackage)
    {
        BURN_PACKAGE* pPackage = pPackages->rgPackages + iPackage;

        if (BURN_PACKAGE_TYPE_MSI == pPackage->type && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzTargetProductCode, -1, pPackage->Msi.sczProductCode, -1))
        {
            pTargetProduct->pChainedTargetPackage = pPackage;

            hr = AddMsiChainedPatch(pPackage, pMspPackage, dwMspTargetProductIndex, &dwChainedPatchIndex);
            ExitOnFailure(hr, "Failed to add chained patch.");

            for (DWORD j = 0; j < pPackage->Msi.cSlipstreamMspPackages; ++j)
            {
                BURN_SLIPSTREAM_MSP* pSlipstreamMsp = pPackage->Msi.rgSlipstreamMsps + j;
                if (pSlipstreamMsp->pMspPackage == pMspPackage)
                {
                    AssertSz(BURN_PACKAGE_INVALID_PATCH_INDEX == pSlipstreamMsp->dwMsiChainedPatchIndex, "An MSP should only show up as a slipstreamed patch in an MSI once.");
                    pTargetProduct->fSlipstream = TRUE;
                    pSlipstreamMsp->dwMsiChainedPatchIndex = dwChainedPatchIndex;
                    break;
                }
            }

            break;
        }
    }

LExit:
    return hr;
}

static HRESULT PlanTargetProduct(
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_ACTION_STATE actionState,
    __in BURN_PACKAGE* pPackage,
    __in BURN_MSPTARGETPRODUCT* pTargetProduct,
    __in_opt HANDLE hCacheEvent
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* rgActions = fRollback ? pPlan->rgRollbackActions : pPlan->rgExecuteActions;
    DWORD cActions = fRollback ? pPlan->cRollbackActions : pPlan->cExecuteActions;
    BURN_EXECUTE_ACTION* pAction = NULL;
    DWORD dwInsertSequence = 0;

    // Try to find another MSP action with the exact same action (install or uninstall) targeting
    // the same product in the same machine context (per-user or per-machine).
    for (DWORD i = 0; i < cActions; ++i)
    {
        pAction = rgActions + i;

        if (BURN_EXECUTE_ACTION_TYPE_MSP_TARGET == pAction->type &&
            pAction->mspTarget.action == actionState &&
            pAction->mspTarget.fPerMachineTarget == (MSIINSTALLCONTEXT_MACHINE == pTargetProduct->context) &&
            CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pAction->mspTarget.sczTargetProductCode, -1, pTargetProduct->wzTargetProductCode, -1))
        {
            dwInsertSequence = i;
            break;
        }

        pAction = NULL;
    }

    // If we didn't find an MSP target action already updating the product, create a new action.
    if (!pAction)
    {
        if (fRollback)
        {
            hr = PlanAppendRollbackAction(pPlan, &pAction);
        }
        else
        {
            hr = PlanAppendExecuteAction(pPlan, &pAction);
        }
        ExitOnFailure(hr, "Failed to plan action for target product.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_MSP_TARGET;
        pAction->mspTarget.action = actionState;
        pAction->mspTarget.pPackage = pPackage;
        pAction->mspTarget.fPerMachineTarget = (MSIINSTALLCONTEXT_MACHINE == pTargetProduct->context);
        pAction->mspTarget.pChainedTargetPackage = pTargetProduct->pChainedTargetPackage;
        pAction->mspTarget.fSlipstream = pTargetProduct->fSlipstream;
        hr = StrAllocString(&pAction->mspTarget.sczTargetProductCode, pTargetProduct->wzTargetProductCode, 0);
        ExitOnFailure(hr, "Failed to copy target product code.");

        hr = MsiEngineCalculateInstallUiLevel(display, pUserExperience, pPackage->sczId, !fRollback, pAction->mspTarget.action,
            &pAction->mspTarget.actionMsiProperty, &pAction->mspTarget.uiLevel, &pAction->mspTarget.fDisableExternalUiHandler);
        ExitOnFailure(hr, "Failed to get msp ui options.");

        // If this is a per-machine target product, then the plan needs to be per-machine as well.
        if (pAction->mspTarget.fPerMachineTarget)
        {
            pPlan->fPerMachine = TRUE;
        }

        LoggingSetPackageVariable(pPackage, pAction->mspTarget.sczTargetProductCode, fRollback, pLog, pVariables, &pAction->mspTarget.sczLogPath); // ignore errors.
    }
    else
    {
        if (!fRollback && hCacheEvent)
        {
            // Since a previouse MSP target action is being updated with the new MSP, 
            // insert a wait syncpoint to before this action since we need to cache the current MSI before using it.
            BURN_EXECUTE_ACTION* pWaitSyncPointAction = NULL;
            hr = PlanInsertExecuteAction(dwInsertSequence, pPlan, &pWaitSyncPointAction);
            ExitOnFailure(hr, "Failed to insert execute action.");

            pWaitSyncPointAction->type = BURN_EXECUTE_ACTION_TYPE_WAIT_SYNCPOINT;
            pWaitSyncPointAction->syncpoint.hEvent = hCacheEvent;

            // Since we inserted an action before the MSP target action that we will be updating, need to update the pointer.
            pAction = pPlan->rgExecuteActions + (dwInsertSequence + 1);
        }
    }

    // Add our target product to the array and sort based on their order determined during detection.
    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pAction->mspTarget.rgOrderedPatches), pAction->mspTarget.cOrderedPatches + 1, sizeof(BURN_ORDERED_PATCHES), 2);
    ExitOnFailure(hr, "Failed grow array of ordered patches.");

    pAction->mspTarget.rgOrderedPatches[pAction->mspTarget.cOrderedPatches].pTargetProduct = pTargetProduct;
    pAction->mspTarget.rgOrderedPatches[pAction->mspTarget.cOrderedPatches].pPackage = pPackage;
    ++pAction->mspTarget.cOrderedPatches;

    // Insertion sort to keep the patches ordered.
    for (DWORD i = pAction->mspTarget.cOrderedPatches - 1; i > 0; --i)
    {
        if (pAction->mspTarget.rgOrderedPatches[i].pTargetProduct->dwOrder < pAction->mspTarget.rgOrderedPatches[i - 1].pTargetProduct->dwOrder)
        {
            BURN_ORDERED_PATCHES temp = pAction->mspTarget.rgOrderedPatches[i - 1];
            pAction->mspTarget.rgOrderedPatches[i - 1] = pAction->mspTarget.rgOrderedPatches[i];
            pAction->mspTarget.rgOrderedPatches[i] = temp;
        }
        else // no swap necessary, we're done.
        {
            break;
        }
    }

LExit:
    return hr;
}
