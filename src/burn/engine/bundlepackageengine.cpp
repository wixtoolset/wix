// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"



// function definitions

extern "C" void BundlePackageEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    )
{
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
            execute = pPackage->Bundle.fPseudoBundle ? BOOTSTRAPPER_ACTION_STATE_INSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = pPackage->Bundle.fRepairable ? BOOTSTRAPPER_ACTION_STATE_REPAIR : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_ABSENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_CACHE:
            execute = !pPackage->fPermanent ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
            execute = BOOTSTRAPPER_ACTION_STATE_UNINSTALL;
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
        case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
            execute = BOOTSTRAPPER_ACTION_STATE_INSTALL;
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
    ExitOnFailure(hr, "Failed to plan package dependency actions.");

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
    HRESULT hr = S_OK;
    int nResult = IDNOACTION;
    LPCWSTR wzArguments = NULL;
    LPWSTR sczArguments = NULL;
    LPWSTR sczArgumentsFormatted = NULL;
    LPWSTR sczArgumentsObfuscated = NULL;
    LPWSTR sczCachedDirectory = NULL;
    LPWSTR sczExecutablePath = NULL;
    LPWSTR sczCommand = NULL;
    LPWSTR sczCommandObfuscated = NULL;
    HANDLE hExecutableFile = INVALID_HANDLE_VALUE;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };
    DWORD dwExitCode = 0;
    GENERIC_EXECUTE_MESSAGE message = { };
    BOOTSTRAPPER_ACTION_STATE action = pExecuteAction->relatedBundle.action;
    BURN_RELATED_BUNDLE* pRelatedBundle = pExecuteAction->relatedBundle.pRelatedBundle;
    BOOTSTRAPPER_RELATION_TYPE relationType = pRelatedBundle->relationType;
    BURN_PACKAGE* pPackage = &pRelatedBundle->package;
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
        hr = StrAllocString(&sczArguments, wzArguments, 0);
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
            if (sczArguments)
            {
                hr = StrAllocConcat(&sczArguments, L" ", 0);
                ExitOnFailure(hr, "Failed to separate command-line arguments.");
            }

            switch (action)
            {
            case BOOTSTRAPPER_ACTION_STATE_INSTALL:
                hr = StrAllocConcat(&sczArguments, commandLineArgument->sczInstallArgument, 0);
                ExitOnFailure(hr, "Failed to get command-line argument for install.");
                break;

            case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
                hr = StrAllocConcat(&sczArguments, commandLineArgument->sczUninstallArgument, 0);
                ExitOnFailure(hr, "Failed to get command-line argument for uninstall.");
                break;

            case BOOTSTRAPPER_ACTION_STATE_REPAIR:
                hr = StrAllocConcat(&sczArguments, commandLineArgument->sczRepairArgument, 0);
                ExitOnFailure(hr, "Failed to get command-line argument for repair.");
                break;

            default:
                hr = E_INVALIDARG;
                ExitOnFailure(hr, "Invalid Bundle package action: %d.", action);
            }
        }
    }

    // build command
    AppAppendCommandLineArgument(&sczCommand, sczExecutablePath);
    ExitOnFailure(hr, "Failed to create executable command.");

    if (!fRunEmbedded)
    {
        hr = StrAllocConcat(&sczCommand, L" -quiet", 0);
        ExitOnFailure(hr, "Failed to append quiet argument.");
    }

    if (wzOperationCommandLine)
    {
        hr = StrAllocConcatFormatted(&sczCommand, L" %ls", wzOperationCommandLine);
        ExitOnFailure(hr, "Failed to append operation argument.");
    }

    if (wzRelationTypeCommandLine)
    {
        hr = StrAllocConcatFormatted(&sczCommand, L" -%ls", wzRelationTypeCommandLine);
        ExitOnFailure(hr, "Failed to append relation type argument.");
    }

    // Add the list of dependencies to ignore, if any, to the burn command line.
    if (pExecuteAction->relatedBundle.sczIgnoreDependencies)
    {
        hr = StrAllocConcatFormatted(&sczCommand, L" -%ls=%ls", BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES, pExecuteAction->relatedBundle.sczIgnoreDependencies);
        ExitOnFailure(hr, "Failed to append the list of dependencies to ignore to the command line.");
    }

    // Add the list of ancestors, if any, to the burn command line.
    if (pExecuteAction->relatedBundle.sczAncestors)
    {
        hr = StrAllocConcatFormatted(&sczCommand, L" -%ls=%ls", BURN_COMMANDLINE_SWITCH_ANCESTORS, pExecuteAction->relatedBundle.sczAncestors);
        ExitOnFailure(hr, "Failed to append the list of ancestors to the command line.");
    }

    hr = CoreAppendEngineWorkingDirectoryToCommandLine(pExecuteAction->relatedBundle.sczEngineWorkingDirectory, &sczCommand, NULL);
    ExitOnFailure(hr, "Failed to append the custom working directory to the bundlepackage command line.");

    hr = CoreAppendFileHandleSelfToCommandLine(sczExecutablePath, &hExecutableFile, &sczCommand, NULL);
    ExitOnFailure(hr, "Failed to append %ls", BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF);

    // Always add user supplied arguments last.
    if (sczArguments && *sczArguments)
    {
        hr = VariableFormatString(pVariables, sczArguments, &sczArgumentsFormatted, NULL);
        ExitOnFailure(hr, "Failed to format argument string.");

        hr = VariableFormatStringObfuscated(pVariables, sczArguments, &sczArgumentsObfuscated, NULL);
        ExitOnFailure(hr, "Failed to format obfuscated argument string.");

        hr = StrAllocFormatted(&sczCommandObfuscated, L"%ls %ls", sczCommand, sczArgumentsObfuscated);
        ExitOnFailure(hr, "Failed to copy obfuscated formatted arguments.");

        hr = StrAllocConcatFormattedSecure(&sczCommand, L" %ls", sczArgumentsFormatted);
        ExitOnFailure(hr, "Failed to copy formatted arguments.");
    }

    // Log before we add the secret pipe name and client token for embedded processes.
    LogId(REPORT_STANDARD, MSG_APPLYING_PACKAGE, LoggingRollbackOrExecute(fRollback), pPackage->sczId, LoggingActionStateToString(action), sczExecutablePath, sczCommandObfuscated);

    if (fRunEmbedded)
    {
        hr = EmbeddedRunBundle(sczExecutablePath, sczCommand, pfnGenericMessageHandler, pvContext, &dwExitCode);
        ExitOnFailure(hr, "Failed to run bundle as embedded from path: %ls", sczExecutablePath);
    }
    else // create and wait for the executable process while sending fake progress to allow cancel.
    {
        // Make the cache location of the executable the current directory to help those executables
        // that expect stuff to be relative to them.
        si.cb = sizeof(si);
        if (!::CreateProcessW(sczExecutablePath, sczCommand, NULL, NULL, TRUE, CREATE_NO_WINDOW, NULL, sczCachedDirectory, &si, &pi))
        {
            ExitWithLastError(hr, "Failed to CreateProcess on path: %ls", sczExecutablePath);
        }

        do
        {
            message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
            message.dwUIHint = MB_OKCANCEL;
            message.progress.dwPercentage = 50;
            nResult = pfnGenericMessageHandler(&message, pvContext);
            hr = (IDOK == nResult || IDNOACTION == nResult) ? S_OK : IDCANCEL == nResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
            ExitOnRootFailure(hr, "Bootstrapper application aborted during BUNDLE progress.");

            hr = ProcWaitForCompletion(pi.hProcess, 500, &dwExitCode);
            if (HRESULT_FROM_WIN32(WAIT_TIMEOUT) != hr)
            {
                ExitOnFailure(hr, "Failed to wait for executable to complete: %ls", sczExecutablePath);
            }
        } while (HRESULT_FROM_WIN32(WAIT_TIMEOUT) == hr);
    }

    hr = ExeEngineHandleExitCode(pPackage->Bundle.rgExitCodes, pPackage->Bundle.cExitCodes, dwExitCode, pRestart);
    ExitOnRootFailure(hr, "Process returned error: 0x%x", dwExitCode);

LExit:
    StrSecureZeroFreeString(sczArguments);
    StrSecureZeroFreeString(sczArgumentsFormatted);
    ReleaseStr(sczArgumentsObfuscated);
    ReleaseStr(sczCachedDirectory);
    ReleaseStr(sczExecutablePath);
    StrSecureZeroFreeString(sczCommand);
    ReleaseStr(sczCommandObfuscated);

    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);
    ReleaseFileHandle(hExecutableFile);

    // Best effort to clear the execute package cache folder and action variables.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, NULL, TRUE, FALSE);
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, NULL, TRUE, FALSE);

    return hr;
}
