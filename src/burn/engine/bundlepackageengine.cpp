// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT ExecuteBundle(
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __in BOOTSTRAPPER_ACTION_STATE action,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_PACKAGE* pPackage,
    __in_z_opt LPCWSTR wzIgnoreDependencies,
    __in_z_opt LPCWSTR wzAncestors,
    __in_z_opt LPCWSTR wzEngineWorkingDirectory,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
static BOOTSTRAPPER_RELATION_TYPE ConvertRelationType(
    __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE relationType
    );

// function definitions

extern "C" HRESULT BundlePackageEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnBundlePackage,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BOOL fFoundXml = FALSE;
    LPWSTR scz = NULL;

    // @DetectCondition
    hr = XmlGetAttributeEx(pixnBundlePackage, L"BundleId", &pPackage->Bundle.sczBundleId);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get @BundleId.");

    // @InstallArguments
    hr = XmlGetAttributeEx(pixnBundlePackage, L"InstallArguments", &pPackage->Bundle.sczInstallArguments);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @InstallArguments.");

    // @UninstallArguments
    hr = XmlGetAttributeEx(pixnBundlePackage, L"UninstallArguments", &pPackage->Bundle.sczUninstallArguments);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @UninstallArguments.");

    // @RepairArguments
    hr = XmlGetAttributeEx(pixnBundlePackage, L"RepairArguments", &pPackage->Bundle.sczRepairArguments);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @RepairArguments.");

    // @SupportsBurnProtocol
    hr = XmlGetYesNoAttribute(pixnBundlePackage, L"SupportsBurnProtocol", &pPackage->Bundle.fSupportsBurnProtocol);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @SupportsBurnProtocol.");

    // @Win64
    hr = XmlGetYesNoAttribute(pixnBundlePackage, L"Win64", &pPackage->Bundle.fWin64);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Win64.");

    hr = ExeEngineParseExitCodesFromXml(pixnBundlePackage, &pPackage->Bundle.rgExitCodes, &pPackage->Bundle.cExitCodes);
    ExitOnFailure(hr, "Failed to parse exit codes.");

    hr = ExeEngineParseCommandLineArgumentsFromXml(pixnBundlePackage, &pPackage->Bundle.rgCommandLineArguments, &pPackage->Bundle.cCommandLineArguments);
    ExitOnFailure(hr, "Failed to parse command lines.");

    hr = StrAllocFormatted(&pPackage->Bundle.sczRegistrationKey, L"%ls\\%ls", BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY, pPackage->Bundle.sczBundleId);
    ExitOnFailure(hr, "Failed to build uninstall registry key path.");

LExit:
    ReleaseStr(scz);

    return hr;
}

extern "C" void BundlePackageEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    )
{
    ReleaseStr(pPackage->Bundle.sczBundleId);
    ReleaseStr(pPackage->Bundle.sczRegistrationKey);
    ReleaseStr(pPackage->Bundle.sczInstallArguments);
    ReleaseStr(pPackage->Bundle.sczRepairArguments);
    ReleaseStr(pPackage->Bundle.sczUninstallArguments);
    ReleaseStr(pPackage->Bundle.sczIgnoreDependencies);
    ReleaseMem(pPackage->Bundle.rgExitCodes);

    // free command-line arguments
    if (pPackage->Bundle.rgCommandLineArguments)
    {
        for (DWORD i = 0; i < pPackage->Bundle.cCommandLineArguments; ++i)
        {
            ExeEngineCommandLineArgumentUninitialize(pPackage->Bundle.rgCommandLineArguments + i);
        }
        MemFree(pPackage->Bundle.rgCommandLineArguments);
    }

    // clear struct
    memset(&pPackage->Bundle, 0, sizeof(pPackage->Bundle));
}

extern "C" HRESULT BundlePackageEngineDetectPackage(
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    HKEY hkRegistration = NULL;
    DWORD dwInstalled = 0;
    BOOL fDetected = FALSE;
    HKEY hkRoot = pPackage->fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;
    REG_KEY_BITNESS bitness = pPackage->Bundle.fWin64 ? REG_KEY_64BIT : REG_KEY_32BIT;

    // TODO: detect all related bundles, so that the Obsolete state can be detected.
    hr = RegOpenEx(hkRoot, pPackage->Bundle.sczRegistrationKey, KEY_QUERY_VALUE, bitness, &hkRegistration);
    if (SUCCEEDED(hr))
    {
        hr = RegReadNumber(hkRegistration, REGISTRY_BUNDLE_INSTALLED, &dwInstalled);
    }

    // Not finding the key or value is okay.
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        hr = S_OK;
    }

    fDetected = (1 == dwInstalled);

    // update detect state
    pPackage->currentState = fDetected ? BOOTSTRAPPER_PACKAGE_STATE_PRESENT : BOOTSTRAPPER_PACKAGE_STATE_ABSENT;

    if (pPackage->fCanAffectRegistration)
    {
        pPackage->installRegistrationState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT < pPackage->currentState ? BURN_PACKAGE_REGISTRATION_STATE_PRESENT : BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
    }

    ReleaseRegKey(hkRegistration);
    return hr;
}

//
// PlanCalculate - calculates the execute and rollback state for the requested package state.
//
extern "C" HRESULT BundlePackageEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ACTION_STATE execute = BOOTSTRAPPER_ACTION_STATE_NONE;
    BOOTSTRAPPER_ACTION_STATE rollback = BOOTSTRAPPER_ACTION_STATE_NONE;

    // execute action
    switch (pPackage->currentState)
    {
    case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
        switch (pPackage->requested)
        {
        case BOOTSTRAPPER_REQUEST_STATE_PRESENT:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = BOOTSTRAPPER_ACTION_STATE_REPAIR;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_ABSENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_CACHE:
            execute = !pPackage->fPermanent ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
            execute = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT:
            execute = BOOTSTRAPPER_ACTION_STATE_INSTALL;
            break;
        default:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        }
        break;

    case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
        switch (pPackage->requested)
        {
        case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = BOOTSTRAPPER_ACTION_STATE_INSTALL;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
            execute = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
            break;
        default:
            execute = BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Invalid package current state: %d.", pPackage->currentState);
    }

    // Calculate the rollback action if there is an execute action.
    if (BOOTSTRAPPER_ACTION_STATE_NONE != execute)
    {
        switch (pPackage->currentState)
        {
        case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
            switch (pPackage->requested)
            {
            case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
                rollback = BOOTSTRAPPER_ACTION_STATE_INSTALL;
                break;
            default:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            }
            break;

        case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
            switch (pPackage->requested)
            {
            case BOOTSTRAPPER_REQUEST_STATE_PRESENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
                rollback = !pPackage->fPermanent ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT: __fallthrough;
            case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            default:
                rollback = BOOTSTRAPPER_ACTION_STATE_NONE;
                break;
            }
            break;

        default:
            hr = E_INVALIDARG;
            ExitOnRootFailure(hr, "Invalid package expected state.");
        }
    }

    // return values
    pPackage->execute = execute;
    pPackage->rollback = rollback;

LExit:
    return hr;
}

//
// PlanAdd - adds the calculated execute and rollback actions for the package.
//
extern "C" HRESULT BundlePackageEnginePlanAddPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;

    hr = DependencyPlanPackage(NULL, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to plan package dependency actions.");

    // add rollback action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback)
    {
        hr = PlanAppendRollbackAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_BUNDLE_PACKAGE;
        pAction->bundlePackage.pPackage = pPackage;
        pAction->bundlePackage.action = pPackage->rollback;

        if (pPackage->Bundle.wzAncestors)
        {
            hr = StrAllocString(&pAction->bundlePackage.sczAncestors, pPackage->Bundle.wzAncestors, 0);
            ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
        }

        if (pPackage->Bundle.wzEngineWorkingDirectory)
        {
            hr = StrAllocString(&pAction->bundlePackage.sczEngineWorkingDirectory, pPackage->Bundle.wzEngineWorkingDirectory, 0);
            ExitOnFailure(hr, "Failed to allocate the custom working directory.");
        }

        LoggingSetPackageVariable(pPackage, NULL, TRUE, pLog, pVariables, NULL); // ignore errors.

        hr = PlanExecuteCheckpoint(pPlan);
        ExitOnFailure(hr, "Failed to append execute checkpoint.");
    }

    // add execute action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute)
    {
        hr = PlanAppendExecuteAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append execute action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_BUNDLE_PACKAGE;
        pAction->bundlePackage.pPackage = pPackage;
        pAction->bundlePackage.action = pPackage->execute;

        if (pPackage->Bundle.wzAncestors)
        {
            hr = StrAllocString(&pAction->bundlePackage.sczAncestors, pPackage->Bundle.wzAncestors, 0);
            ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
        }

        if (pPackage->Bundle.wzEngineWorkingDirectory)
        {
            hr = StrAllocString(&pAction->bundlePackage.sczEngineWorkingDirectory, pPackage->Bundle.wzEngineWorkingDirectory, 0);
            ExitOnFailure(hr, "Failed to allocate the custom working directory.");
        }

        LoggingSetPackageVariable(pPackage, NULL, FALSE, pLog, pVariables, NULL); // ignore errors.
    }

LExit:
    return hr;
}

//
// PlanAdd - adds the calculated execute and rollback actions for the related bundle.
//
extern "C" HRESULT BundlePackageEnginePlanAddRelatedBundle(
    __in_opt DWORD *pdwInsertSequence,
    __in BURN_RELATED_BUNDLE* pRelatedBundle,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BURN_EXECUTE_ACTION* pAction = NULL;
    BURN_PACKAGE* pPackage = &pRelatedBundle->package;

    hr = DependencyPlanPackage(pdwInsertSequence, pPackage, pPlan);
    ExitOnFailure(hr, "Failed to plan related bundle dependency actions.");

    // add execute action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->execute)
    {
        if (pdwInsertSequence)
        {
            hr = PlanInsertExecuteAction(*pdwInsertSequence, pPlan, &pAction);
            ExitOnFailure(hr, "Failed to insert execute action.");
        }
        else
        {
            hr = PlanAppendExecuteAction(pPlan, &pAction);
            ExitOnFailure(hr, "Failed to append execute action.");
        }

        pAction->type = BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE;
        pAction->relatedBundle.pRelatedBundle = pRelatedBundle;
        pAction->relatedBundle.action = pPackage->execute;

        if (pPackage->Bundle.sczIgnoreDependencies)
        {
            hr = StrAllocString(&pAction->relatedBundle.sczIgnoreDependencies, pPackage->Bundle.sczIgnoreDependencies, 0);
            ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
        }

        if (pPackage->Bundle.wzAncestors)
        {
            hr = StrAllocString(&pAction->relatedBundle.sczAncestors, pPackage->Bundle.wzAncestors, 0);
            ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
        }

        if (pPackage->Bundle.wzEngineWorkingDirectory)
        {
            hr = StrAllocString(&pAction->relatedBundle.sczEngineWorkingDirectory, pPackage->Bundle.wzEngineWorkingDirectory, 0);
            ExitOnFailure(hr, "Failed to allocate the custom working directory.");
        }

        LoggingSetPackageVariable(pPackage, NULL, FALSE, pLog, pVariables, NULL); // ignore errors.
    }

    // add rollback action
    if (BOOTSTRAPPER_ACTION_STATE_NONE != pPackage->rollback)
    {
        hr = PlanAppendRollbackAction(pPlan, &pAction);
        ExitOnFailure(hr, "Failed to append rollback action.");

        pAction->type = BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE;
        pAction->relatedBundle.pRelatedBundle = pRelatedBundle;
        pAction->relatedBundle.action = pPackage->rollback;

        if (pPackage->Bundle.sczIgnoreDependencies)
        {
            hr = StrAllocString(&pAction->relatedBundle.sczIgnoreDependencies, pPackage->Bundle.sczIgnoreDependencies, 0);
            ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
        }

        if (pPackage->Bundle.wzAncestors)
        {
            hr = StrAllocString(&pAction->relatedBundle.sczAncestors, pPackage->Bundle.wzAncestors, 0);
            ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
        }

        if (pPackage->Bundle.wzEngineWorkingDirectory)
        {
            hr = StrAllocString(&pAction->relatedBundle.sczEngineWorkingDirectory, pPackage->Bundle.wzEngineWorkingDirectory, 0);
            ExitOnFailure(hr, "Failed to allocate the custom working directory.");
        }

        LoggingSetPackageVariable(pPackage, NULL, TRUE, pLog, pVariables, NULL); // ignore errors.
    }

LExit:
    return hr;
}

extern "C" HRESULT BundlePackageEngineExecutePackage(
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    BOOTSTRAPPER_ACTION_STATE action = pExecuteAction->bundlePackage.action;
    LPCWSTR wzIgnoreDependencies = pExecuteAction->bundlePackage.sczIgnoreDependencies;
    LPCWSTR wzAncestors = pExecuteAction->bundlePackage.sczAncestors;
    LPCWSTR wzEngineWorkingDirectory = pExecuteAction->bundlePackage.sczEngineWorkingDirectory;
    BOOTSTRAPPER_RELATION_TYPE relationType = BOOTSTRAPPER_RELATION_CHAIN_PACKAGE;
    BURN_PACKAGE* pPackage = pExecuteAction->bundlePackage.pPackage;

    return ExecuteBundle(pCache, pVariables, fRollback, pfnGenericMessageHandler, pvContext, action, relationType, pPackage, wzIgnoreDependencies, wzAncestors, wzEngineWorkingDirectory, pRestart);
}

extern "C" HRESULT BundlePackageEngineExecuteRelatedBundle(
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    BOOTSTRAPPER_ACTION_STATE action = pExecuteAction->relatedBundle.action;
    LPCWSTR wzIgnoreDependencies = pExecuteAction->relatedBundle.sczIgnoreDependencies;
    LPCWSTR wzAncestors = pExecuteAction->relatedBundle.sczAncestors;
    LPCWSTR wzEngineWorkingDirectory = pExecuteAction->relatedBundle.sczEngineWorkingDirectory;
    BURN_RELATED_BUNDLE* pRelatedBundle = pExecuteAction->relatedBundle.pRelatedBundle;
    BOOTSTRAPPER_RELATION_TYPE relationType = ConvertRelationType(pRelatedBundle->planRelationType);
    BURN_PACKAGE* pPackage = &pRelatedBundle->package;

    return ExecuteBundle(pCache, pVariables, fRollback, pfnGenericMessageHandler, pvContext, action, relationType, pPackage, wzIgnoreDependencies, wzAncestors, wzEngineWorkingDirectory, pRestart);
}

extern "C" void BundlePackageEngineUpdateInstallRegistrationState(
    __in BURN_EXECUTE_ACTION* pAction,
    __in HRESULT hrExecute
    )
{
    BURN_PACKAGE* pPackage = pAction->bundlePackage.pPackage;

    if (FAILED(hrExecute) || !pPackage->fCanAffectRegistration)
    {
        ExitFunction();
    }

    if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pAction->bundlePackage.action)
    {
        pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
    }
    else
    {
        pPackage->installRegistrationState = BURN_PACKAGE_REGISTRATION_STATE_PRESENT;
    }

LExit:
    return;
}

static HRESULT ExecuteBundle(
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __in BOOTSTRAPPER_ACTION_STATE action,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_PACKAGE* pPackage,
    __in_z_opt LPCWSTR wzIgnoreDependencies,
    __in_z_opt LPCWSTR wzAncestors,
    __in_z_opt LPCWSTR wzEngineWorkingDirectory,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzArguments = NULL;
    LPWSTR sczCachedDirectory = NULL;
    LPWSTR sczExecutablePath = NULL;
    LPWSTR sczBaseCommand = NULL;
    LPWSTR sczUnformattedUserArgs = NULL;
    LPWSTR sczUserArgs = NULL;
    LPWSTR sczUserArgsObfuscated = NULL;
    LPWSTR sczCommandObfuscated = NULL;
    HANDLE hExecutableFile = INVALID_HANDLE_VALUE;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };
    DWORD dwExitCode = 0;
    GENERIC_EXECUTE_MESSAGE message = { };
    BURN_PAYLOAD* pPackagePayload = pPackage->payloads.rgItems[0].pPayload;
    LPCWSTR wzRelationTypeCommandLine = CoreRelationTypeToCommandLineString(relationType);
    LPCWSTR wzOperationCommandLine = NULL;
    BOOL fRunEmbedded = pPackage->Bundle.fSupportsBurnProtocol;

    // get cached executable path
    hr = CacheGetCompletedPath(pCache, pPackage->fPerMachine, pPackage->sczCacheId, &sczCachedDirectory);
    ExitOnFailure(hr, "Failed to get cached path for package: %ls", pPackage->sczId);

    // Best effort to set the execute package cache folder and action variables.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, sczCachedDirectory, TRUE, FALSE);
    VariableSetNumeric(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, action, TRUE);

    hr = PathConcat(sczCachedDirectory, pPackagePayload->sczFilePath, &sczExecutablePath);
    ExitOnFailure(hr, "Failed to build executable path.");

    // pick arguments
    switch (action)
    {
    case BOOTSTRAPPER_ACTION_STATE_INSTALL:
        wzArguments = pPackage->Bundle.sczInstallArguments;
        break;

    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        wzOperationCommandLine = L"-uninstall";
        wzArguments = pPackage->Bundle.sczUninstallArguments;
        break;

    case BOOTSTRAPPER_ACTION_STATE_REPAIR:
        wzOperationCommandLine = L"-repair";
        wzArguments = pPackage->Bundle.sczRepairArguments;
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid Bundle package action: %d.", action);
    }

    // now add optional arguments
    if (wzArguments && *wzArguments)
    {
        hr = StrAllocString(&sczUnformattedUserArgs, wzArguments, 0);
        ExitOnFailure(hr, "Failed to copy package arguments.");
    }

    for (DWORD i = 0; i < pPackage->Bundle.cCommandLineArguments; ++i)
    {
        BURN_EXE_COMMAND_LINE_ARGUMENT* commandLineArgument = &pPackage->Bundle.rgCommandLineArguments[i];
        BOOL fCondition = FALSE;

        hr = ConditionEvaluate(pVariables, commandLineArgument->sczCondition, &fCondition);
        ExitOnFailure(hr, "Failed to evaluate bundle package command-line condition.");

        if (fCondition)
        {
            if (sczUnformattedUserArgs)
            {
                hr = StrAllocConcat(&sczUnformattedUserArgs, L" ", 0);
                ExitOnFailure(hr, "Failed to separate command-line arguments.");
            }

            switch (action)
            {
            case BOOTSTRAPPER_ACTION_STATE_INSTALL:
                hr = StrAllocConcat(&sczUnformattedUserArgs, commandLineArgument->sczInstallArgument, 0);
                ExitOnFailure(hr, "Failed to get command-line argument for install.");
                break;

            case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
                hr = StrAllocConcat(&sczUnformattedUserArgs, commandLineArgument->sczUninstallArgument, 0);
                ExitOnFailure(hr, "Failed to get command-line argument for uninstall.");
                break;

            case BOOTSTRAPPER_ACTION_STATE_REPAIR:
                hr = StrAllocConcat(&sczUnformattedUserArgs, commandLineArgument->sczRepairArgument, 0);
                ExitOnFailure(hr, "Failed to get command-line argument for repair.");
                break;

            default:
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Invalid Bundle package action: %d.", action);
            }
        }
    }

    // build base command
    hr = StrAllocFormatted(&sczBaseCommand, L"\"%ls\"", sczExecutablePath);
    ExitOnFailure(hr, "Failed to allocate base command.");

    if (!fRunEmbedded)
    {
        hr = StrAllocConcat(&sczBaseCommand, L" -quiet", 0);
        ExitOnFailure(hr, "Failed to append quiet argument.");
    }

    if (wzOperationCommandLine)
    {
        hr = StrAllocConcatFormatted(&sczBaseCommand, L" %ls", wzOperationCommandLine);
        ExitOnFailure(hr, "Failed to append operation argument.");
    }

    if (wzRelationTypeCommandLine)
    {
        hr = StrAllocConcatFormatted(&sczBaseCommand, L" -%ls", wzRelationTypeCommandLine);
        ExitOnFailure(hr, "Failed to append relation type argument.");
    }

    // Add the list of dependencies to ignore, if any, to the burn command line.
    if (BOOTSTRAPPER_RELATION_CHAIN_PACKAGE == relationType)
    {
        hr = StrAllocConcatFormatted(&sczBaseCommand, L" -%ls=ALL", BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES);
        ExitOnFailure(hr, "Failed to append the list of dependencies to ignore to the command line.");
    }
    else if (wzIgnoreDependencies)
    {
        hr = StrAllocConcatFormatted(&sczBaseCommand, L" -%ls=%ls", BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES, wzIgnoreDependencies);
        ExitOnFailure(hr, "Failed to append the list of dependencies to ignore to the command line.");
    }

    // Add the list of ancestors, if any, to the burn command line.
    if (wzAncestors)
    {
        hr = StrAllocConcatFormatted(&sczBaseCommand, L" -%ls=%ls", BURN_COMMANDLINE_SWITCH_ANCESTORS, wzAncestors);
        ExitOnFailure(hr, "Failed to append the list of ancestors to the command line.");
    }

    if (wzEngineWorkingDirectory)
    {
        hr = CoreAppendEngineWorkingDirectoryToCommandLine(wzEngineWorkingDirectory, &sczBaseCommand, NULL);
        ExitOnFailure(hr, "Failed to append the custom working directory to the bundlepackage command line.");
    }

    hr = CoreAppendFileHandleSelfToCommandLine(sczExecutablePath, &hExecutableFile, &sczBaseCommand, NULL);
    ExitOnFailure(hr, "Failed to append %ls", BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF);

    // build user args
    if (sczUnformattedUserArgs && *sczUnformattedUserArgs)
    {
        hr = VariableFormatString(pVariables, sczUnformattedUserArgs, &sczUserArgs, NULL);
        ExitOnFailure(hr, "Failed to format argument string.");

        hr = VariableFormatStringObfuscated(pVariables, sczUnformattedUserArgs, &sczUserArgsObfuscated, NULL);
        ExitOnFailure(hr, "Failed to format obfuscated argument string.");

        hr = StrAllocFormatted(&sczCommandObfuscated, L"%ls %ls", sczBaseCommand, sczUserArgsObfuscated);
        ExitOnFailure(hr, "Failed to allocate obfuscated bundle command.");
    }

    // Log obfuscated command, which won't include raw hidden variable values or protocol specific arguments to avoid exposing secrets.
    LogId(REPORT_STANDARD, MSG_APPLYING_PACKAGE, LoggingRollbackOrExecute(fRollback), pPackage->sczId, LoggingActionStateToString(action), sczExecutablePath, sczCommandObfuscated ? sczCommandObfuscated : sczBaseCommand);

    if (fRunEmbedded)
    {
        hr = EmbeddedRunBundle(sczExecutablePath, sczBaseCommand, sczUserArgs, pfnGenericMessageHandler, pvContext, &dwExitCode);
        ExitOnFailure(hr, "Failed to run bundle as embedded from path: %ls", sczExecutablePath);
    }
    else
    {
        hr = ExeEngineRunProcess(pfnGenericMessageHandler, pvContext, pPackage, sczExecutablePath, sczBaseCommand, sczUserArgs, sczCachedDirectory, &dwExitCode);
        ExitOnFailure(hr, "Failed to run BUNDLE process");
    }

    hr = ExeEngineHandleExitCode(pPackage->Bundle.rgExitCodes, pPackage->Bundle.cExitCodes, dwExitCode, pRestart);
    ExitOnRootFailure(hr, "Process returned error: 0x%x", dwExitCode);

LExit:
    ReleaseStr(sczCachedDirectory);
    ReleaseStr(sczExecutablePath);
    ReleaseStr(sczBaseCommand);
    ReleaseStr(sczUnformattedUserArgs);
    StrSecureZeroFreeString(sczUserArgs);
    ReleaseStr(sczUserArgsObfuscated);
    ReleaseStr(sczCommandObfuscated);

    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);
    ReleaseFileHandle(hExecutableFile);

    // Best effort to clear the execute package cache folder and action variables.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, NULL, TRUE, FALSE);
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, NULL, TRUE, FALSE);

    return hr;
}

static BOOTSTRAPPER_RELATION_TYPE ConvertRelationType(
    __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE relationType
    )
{
    switch (relationType)
    {
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DOWNGRADE: __fallthrough;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_UPGRADE:
        return BOOTSTRAPPER_RELATION_UPGRADE;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_ADDON:
        return BOOTSTRAPPER_RELATION_ADDON;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_PATCH:
        return BOOTSTRAPPER_RELATION_PATCH;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_ADDON:
        return BOOTSTRAPPER_RELATION_DEPENDENT_ADDON;
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_PATCH:
        return BOOTSTRAPPER_RELATION_DEPENDENT_PATCH;
    default:
        AssertSz(BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_NONE == relationType, "Unknown BUNDLE_RELATION_TYPE");
        return BOOTSTRAPPER_RELATION_NONE;
    }
}
