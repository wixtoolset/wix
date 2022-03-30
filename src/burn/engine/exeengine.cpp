// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// function definitions

extern "C" HRESULT ExeEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnExePackage,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;
    BOOL fFoundXml = FALSE;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    LPWSTR scz = NULL;

    // @DetectCondition
    hr = XmlGetAttributeEx(pixnExePackage, L"DetectCondition", &pPackage->Exe.sczDetectCondition);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @DetectCondition.");

    // @InstallArguments
    hr = XmlGetAttributeEx(pixnExePackage, L"InstallArguments", &pPackage->Exe.sczInstallArguments);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @InstallArguments.");

    // @UninstallArguments
    hr = XmlGetAttributeEx(pixnExePackage, L"UninstallArguments", &pPackage->Exe.sczUninstallArguments);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @UninstallArguments.");

    // @RepairArguments
    hr = XmlGetAttributeEx(pixnExePackage, L"RepairArguments", &pPackage->Exe.sczRepairArguments);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @RepairArguments.");

    // @Repairable
    hr = XmlGetYesNoAttribute(pixnExePackage, L"Repairable", &pPackage->Exe.fRepairable);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @Repairable.");

    // @Uninstallable
    hr = XmlGetYesNoAttribute(pixnExePackage, L"Uninstallable", &pPackage->Exe.fUninstallable);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @Uninstallable.");

    // @Bundle
    hr = XmlGetYesNoAttribute(pixnExePackage, L"Bundle", &pPackage->Exe.fBundle);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @Bundle.");

    // @Protocol
    hr = XmlGetAttributeEx(pixnExePackage, L"Protocol", &scz);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @Protocol.");

    if (fFoundXml)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"burn", -1))
        {
            pPackage->Exe.protocol = BURN_EXE_PROTOCOL_TYPE_BURN;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"netfx4", -1))
        {
            pPackage->Exe.protocol = BURN_EXE_PROTOCOL_TYPE_NETFX4;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"none", -1))
        {
            pPackage->Exe.protocol = BURN_EXE_PROTOCOL_TYPE_NONE;
        }
        else
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Invalid protocol type: %ls", scz);
        }
    }

    hr = ExeEngineParseExitCodesFromXml(pixnExePackage, &pPackage->Exe.rgExitCodes, &pPackage->Exe.cExitCodes);
    ExitOnFailure(hr, "Failed to parse exit codes.");

    hr = ExeEngineParseCommandLineArgumentsFromXml(pixnExePackage, &pPackage->Exe.rgCommandLineArguments, &pPackage->Exe.cCommandLineArguments);
    ExitOnFailure(hr, "Failed to parse command lines.");

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

extern "C" void ExeEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    )
{
    ReleaseStr(pPackage->Exe.sczDetectCondition);
    ReleaseStr(pPackage->Exe.sczInstallArguments);
    ReleaseStr(pPackage->Exe.sczRepairArguments);
    ReleaseStr(pPackage->Exe.sczUninstallArguments);
    ReleaseStr(pPackage->Exe.sczIgnoreDependencies);
    ReleaseMem(pPackage->Exe.rgExitCodes);

    // free command-line arguments
    if (pPackage->Exe.rgCommandLineArguments)
    {
        for (DWORD i = 0; i < pPackage->Exe.cCommandLineArguments; ++i)
        {
            ExeEngineCommandLineArgumentUninitialize(pPackage->Exe.rgCommandLineArguments + i);
        }
        MemFree(pPackage->Exe.rgCommandLineArguments);
    }

    // clear struct
    memset(&pPackage->Exe, 0, sizeof(pPackage->Exe));
}

extern "C" void ExeEngineCommandLineArgumentUninitialize(
    __in BURN_EXE_COMMAND_LINE_ARGUMENT* pCommandLineArgument
    )
{
    ReleaseStr(pCommandLineArgument->sczInstallArgument);
    ReleaseStr(pCommandLineArgument->sczUninstallArgument);
    ReleaseStr(pCommandLineArgument->sczRepairArgument);
    ReleaseStr(pCommandLineArgument->sczCondition);
}

extern "C" HRESULT ExeEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    BOOL fDetected = FALSE;

    // evaluate detect condition
    if (pPackage->Exe.sczDetectCondition && *pPackage->Exe.sczDetectCondition)
    {
        hr = ConditionEvaluate(pVariables, pPackage->Exe.sczDetectCondition, &fDetected);
        ExitOnFailure(hr, "Failed to evaluate executable package detect condition.");
    }

    // update detect state
    pPackage->currentState = fDetected ? BOOTSTRAPPER_PACKAGE_STATE_PRESENT : BOOTSTRAPPER_PACKAGE_STATE_ABSENT;

    if (pPackage->fCanAffectRegistration)
    {
        pPackage->installRegistrationState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT < pPackage->currentState ? BURN_PACKAGE_REGISTRATION_STATE_PRESENT : BURN_PACKAGE_REGISTRATION_STATE_ABSENT;
    }

    hr = DependencyDetectChainPackage(pPackage, pRegistration);
    ExitOnFailure(hr, "Failed to detect dependencies for EXE package.");

LExit:
    return hr;
}

//
// PlanCalculate - calculates the execute and rollback state for the requested package state.
//
extern "C" HRESULT ExeEnginePlanCalculatePackage(
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
            execute = pPackage->Exe.fRepairable ? BOOTSTRAPPER_ACTION_STATE_REPAIR : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_ABSENT: __fallthrough;
        case BOOTSTRAPPER_REQUEST_STATE_CACHE:
            execute = !pPackage->fPermanent ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
            break;
        case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
            execute = pPackage->Exe.fUninstallable ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
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
            execute = pPackage->Exe.fUninstallable ? BOOTSTRAPPER_ACTION_STATE_UNINSTALL : BOOTSTRAPPER_ACTION_STATE_NONE;
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
extern "C" HRESULT ExeEnginePlanAddPackage(
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

        pAction->type = BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE;
        pAction->exePackage.pPackage = pPackage;
        pAction->exePackage.action = pPackage->rollback;

        if (pPackage->Exe.sczIgnoreDependencies)
        {
            hr = StrAllocString(&pAction->exePackage.sczIgnoreDependencies, pPackage->Exe.sczIgnoreDependencies, 0);
            ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
        }

        if (pPackage->Exe.wzAncestors)
        {
            hr = StrAllocString(&pAction->exePackage.sczAncestors, pPackage->Exe.wzAncestors, 0);
            ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
        }

        if (pPackage->Exe.wzEngineWorkingDirectory)
        {
            hr = StrAllocString(&pAction->exePackage.sczEngineWorkingDirectory, pPackage->Exe.wzEngineWorkingDirectory, 0);
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

        pAction->type = BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE;
        pAction->exePackage.pPackage = pPackage;
        pAction->exePackage.action = pPackage->execute;

        if (pPackage->Exe.sczIgnoreDependencies)
        {
            hr = StrAllocString(&pAction->exePackage.sczIgnoreDependencies, pPackage->Exe.sczIgnoreDependencies, 0);
            ExitOnFailure(hr, "Failed to allocate the list of dependencies to ignore.");
        }

        if (pPackage->Exe.wzAncestors)
        {
            hr = StrAllocString(&pAction->exePackage.sczAncestors, pPackage->Exe.wzAncestors, 0);
            ExitOnFailure(hr, "Failed to allocate the list of ancestors.");
        }

        if (pPackage->Exe.wzEngineWorkingDirectory)
        {
            hr = StrAllocString(&pAction->exePackage.sczEngineWorkingDirectory, pPackage->Exe.wzEngineWorkingDirectory, 0);
            ExitOnFailure(hr, "Failed to allocate the custom working directory.");
        }

        LoggingSetPackageVariable(pPackage, NULL, FALSE, pLog, pVariables, NULL); // ignore errors.
    }

LExit:
    return hr;
}

extern "C" HRESULT ExeEngineExecutePackage(
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
    LPCWSTR wzArguments = NULL;
    LPWSTR sczCachedDirectory = NULL;
    LPWSTR sczExecutablePath = NULL;
    LPWSTR sczBaseCommand = NULL;
    LPWSTR sczUnformattedUserArgs = NULL;
    LPWSTR sczUserArgs = NULL;
    LPWSTR sczUserArgsObfuscated = NULL;
    LPWSTR sczCommandObfuscated = NULL;
    HANDLE hExecutableFile = INVALID_HANDLE_VALUE;
    DWORD dwExitCode = 0;
    BURN_PACKAGE* pPackage = pExecuteAction->exePackage.pPackage;
    BURN_PAYLOAD* pPackagePayload = pPackage->payloads.rgItems[0].pPayload;

    // get cached executable path
    hr = CacheGetCompletedPath(pCache, pPackage->fPerMachine, pPackage->sczCacheId, &sczCachedDirectory);
    ExitOnFailure(hr, "Failed to get cached path for package: %ls", pPackage->sczId);

    // Best effort to set the execute package cache folder and action variables.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, sczCachedDirectory, TRUE, FALSE);
    VariableSetNumeric(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, pExecuteAction->exePackage.action, TRUE);

    hr = PathConcat(sczCachedDirectory, pPackagePayload->sczFilePath, &sczExecutablePath);
    ExitOnFailure(hr, "Failed to build executable path.");

    // pick arguments
    switch (pExecuteAction->exePackage.action)
    {
    case BOOTSTRAPPER_ACTION_STATE_INSTALL:
        wzArguments = pPackage->Exe.sczInstallArguments;
        break;

    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        wzArguments = pPackage->Exe.sczUninstallArguments;
        break;

    case BOOTSTRAPPER_ACTION_STATE_REPAIR:
        wzArguments = pPackage->Exe.sczRepairArguments;
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Invalid Exe package action: %d.", pExecuteAction->exePackage.action);
    }

    // now add optional arguments
    hr = StrAllocString(&sczUnformattedUserArgs, wzArguments && *wzArguments ? wzArguments : L"", 0);
    ExitOnFailure(hr, "Failed to copy package arguments.");

    for (DWORD i = 0; i < pPackage->Exe.cCommandLineArguments; ++i)
    {
        BURN_EXE_COMMAND_LINE_ARGUMENT* commandLineArgument = &pPackage->Exe.rgCommandLineArguments[i];
        BOOL fCondition = FALSE;

        hr = ConditionEvaluate(pVariables, commandLineArgument->sczCondition, &fCondition);
        ExitOnFailure(hr, "Failed to evaluate executable package command-line condition.");

        if (fCondition)
        {
            hr = StrAllocConcat(&sczUnformattedUserArgs, L" ", 0);
            ExitOnFailure(hr, "Failed to separate command-line arguments.");

            switch (pExecuteAction->exePackage.action)
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
                ExitOnFailure(hr, "Invalid Exe package action: %d.", pExecuteAction->exePackage.action);
            }
        }
    }

    // build base command
    hr = StrAllocFormatted(&sczBaseCommand, L"\"%ls\"", sczExecutablePath);
    ExitOnFailure(hr, "Failed to allocate base command.");

    if (pPackage->Exe.fBundle)
    {
        hr = StrAllocConcat(&sczBaseCommand, L" -norestart", 0);
        ExitOnFailure(hr, "Failed to append norestart argument.");

        // Add the list of dependencies to ignore, if any, to the burn command line.
        if (pExecuteAction->exePackage.sczIgnoreDependencies)
        {
            hr = StrAllocConcatFormatted(&sczBaseCommand, L" -%ls=%ls", BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES, pExecuteAction->exePackage.sczIgnoreDependencies);
            ExitOnFailure(hr, "Failed to append the list of dependencies to ignore to the command line.");
        }

        // Add the list of ancestors, if any, to the burn command line.
        if (pExecuteAction->exePackage.sczAncestors)
        {
            hr = StrAllocConcatFormatted(&sczBaseCommand, L" -%ls=%ls", BURN_COMMANDLINE_SWITCH_ANCESTORS, pExecuteAction->exePackage.sczAncestors);
            ExitOnFailure(hr, "Failed to append the list of ancestors to the command line.");
        }

        if (pExecuteAction->exePackage.sczEngineWorkingDirectory)
        {
            hr = CoreAppendEngineWorkingDirectoryToCommandLine(pExecuteAction->exePackage.sczEngineWorkingDirectory, &sczBaseCommand, NULL);
            ExitOnFailure(hr, "Failed to append the custom working directory to the exepackage command line.");
        }

        hr = CoreAppendFileHandleSelfToCommandLine(sczExecutablePath, &hExecutableFile, &sczBaseCommand, NULL);
        ExitOnFailure(hr, "Failed to append %ls", BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF);
    }

    // build user args
    if (sczUnformattedUserArgs && *sczUnformattedUserArgs)
    {
        hr = VariableFormatString(pVariables, sczUnformattedUserArgs, &sczUserArgs, NULL);
        ExitOnFailure(hr, "Failed to format argument string.");

        hr = VariableFormatStringObfuscated(pVariables, sczUnformattedUserArgs, &sczUserArgsObfuscated, NULL);
        ExitOnFailure(hr, "Failed to format obfuscated argument string.");

        hr = StrAllocFormatted(&sczCommandObfuscated, L"%ls %ls", sczBaseCommand, sczUserArgsObfuscated);
        ExitOnFailure(hr, "Failed to allocate obfuscated exe command.");
    }

    // Log obfuscated command, which won't include raw hidden variable values or protocol specific arguments to avoid exposing secrets.
    LogId(REPORT_STANDARD, MSG_APPLYING_PACKAGE, LoggingRollbackOrExecute(fRollback), pPackage->sczId, LoggingActionStateToString(pExecuteAction->exePackage.action), sczExecutablePath, sczCommandObfuscated ? sczCommandObfuscated : sczBaseCommand);

    if (!pPackage->Exe.fFireAndForget && BURN_EXE_PROTOCOL_TYPE_BURN == pPackage->Exe.protocol)
    {
        hr = EmbeddedRunBundle(sczExecutablePath, sczBaseCommand, sczUserArgs, pfnGenericMessageHandler, pvContext, &dwExitCode);
        ExitOnFailure(hr, "Failed to run exe with Burn protocol from path: %ls", sczExecutablePath);
    }
    else if (!pPackage->Exe.fFireAndForget && BURN_EXE_PROTOCOL_TYPE_NETFX4 == pPackage->Exe.protocol)
    {
        hr = NetFxRunChainer(sczExecutablePath, sczBaseCommand, sczUserArgs, pfnGenericMessageHandler, pvContext, &dwExitCode);
        ExitOnFailure(hr, "Failed to run netfx chainer: %ls", sczExecutablePath);
    }
    else
    {
        hr = ExeEngineRunProcess(pfnGenericMessageHandler, pvContext, pPackage, sczExecutablePath, sczBaseCommand, sczUserArgs, sczCachedDirectory, &dwExitCode);
        ExitOnFailure(hr, "Failed to run EXE process");
    }

    hr = ExeEngineHandleExitCode(pPackage->Exe.rgExitCodes, pPackage->Exe.cExitCodes, dwExitCode, pRestart);
    ExitOnRootFailure(hr, "Process returned error: 0x%x", dwExitCode);

LExit:
    ReleaseStr(sczCachedDirectory);
    ReleaseStr(sczExecutablePath);
    ReleaseStr(sczBaseCommand);
    ReleaseStr(sczUnformattedUserArgs);
    StrSecureZeroFreeString(sczUserArgs);
    ReleaseStr(sczUserArgsObfuscated);
    ReleaseStr(sczCommandObfuscated);

    ReleaseFileHandle(hExecutableFile);

    // Best effort to clear the execute package cache folder and action variables.
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER, NULL, TRUE, FALSE);
    VariableSetString(pVariables, BURN_BUNDLE_EXECUTE_PACKAGE_ACTION, NULL, TRUE, FALSE);

    return hr;
}

extern "C" HRESULT ExeEngineRunProcess(
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __in BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzExecutablePath,
    __in_z LPWSTR sczBaseCommand,
    __in_z_opt LPCWSTR wzUserArgs,
    __in_z_opt LPCWSTR wzCachedDirectory,
    __inout DWORD* pdwExitCode
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCommand = NULL;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };
    GENERIC_EXECUTE_MESSAGE message = { };
    int nResult = IDNOACTION;
    DWORD dwProcessId = 0;
    BOOL fDelayedCancel = FALSE;
    BOOL fFireAndForget = BURN_PACKAGE_TYPE_EXE == pPackage->type && pPackage->Exe.fFireAndForget;
    BOOL fInheritHandles = BURN_PACKAGE_TYPE_BUNDLE == pPackage->type;

    // Always add user supplied arguments last.
    if (wzUserArgs)
    {
        hr = StrAllocFormattedSecure(&sczCommand, L"%ls %ls", sczBaseCommand, wzUserArgs);
        ExitOnFailure(hr, "Failed to append user args.");
    }

    // Make the cache location of the executable the current directory to help those executables
    // that expect stuff to be relative to them.
    si.cb = sizeof(si);
    if (!::CreateProcessW(wzExecutablePath, sczCommand ? sczCommand : sczBaseCommand, NULL, NULL, fInheritHandles, CREATE_NO_WINDOW, NULL, wzCachedDirectory, &si, &pi))
    {
        ExitWithLastError(hr, "Failed to CreateProcess on path: %ls", wzExecutablePath);
    }

    message.type = GENERIC_EXECUTE_MESSAGE_PROCESS_STARTED;
    message.dwUIHint = MB_OK;
    pfnGenericMessageHandler(&message, pvContext);

    if (fFireAndForget)
    {
        ::WaitForInputIdle(pi.hProcess, 5000);
        ExitFunction();
    }

    dwProcessId = ::GetProcessId(pi.hProcess);

    // Wait for the executable process while sending fake progress to allow cancel.
    do
    {
        memset(&message, 0, sizeof(message));
        message.type = GENERIC_EXECUTE_MESSAGE_PROGRESS;
        message.dwUIHint = MB_OKCANCEL;
        message.progress.dwPercentage = 50;
        nResult = pfnGenericMessageHandler(&message, pvContext);

        if (IDCANCEL == nResult)
        {
            memset(&message, 0, sizeof(message));
            message.type = GENERIC_EXECUTE_MESSAGE_PROCESS_CANCEL;
            message.dwUIHint = MB_ABORTRETRYIGNORE;
            message.processCancel.dwProcessId = dwProcessId;
            nResult = pfnGenericMessageHandler(&message, pvContext);

            if (IDIGNORE == nResult) // abandon
            {
                nResult = IDCANCEL;
                fDelayedCancel = FALSE;
            }
            //else if (IDABORT == nResult) // kill
            else // wait
            {
                if (!fDelayedCancel)
                {
                    fDelayedCancel = TRUE;

                    LogId(REPORT_STANDARD, MSG_EXECUTE_PROCESS_DELAYED_CANCEL_REQUESTED, pPackage->sczId);
                }

                nResult = IDNOACTION;
            }
        }

        hr = (IDOK == nResult || IDNOACTION == nResult) ? S_OK : IDCANCEL == nResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
        ExitOnRootFailure(hr, "Bootstrapper application aborted during package process progress.");

        hr = ProcWaitForCompletion(pi.hProcess, 500, pdwExitCode);
        if (HRESULT_FROM_WIN32(WAIT_TIMEOUT) != hr)
        {
            ExitOnFailure(hr, "Failed to wait for executable to complete: %ls", wzExecutablePath);
        }
    } while (HRESULT_FROM_WIN32(WAIT_TIMEOUT) == hr);

    memset(&message, 0, sizeof(message));
    message.type = GENERIC_EXECUTE_MESSAGE_PROCESS_COMPLETED;
    message.dwUIHint = MB_OK;
    pfnGenericMessageHandler(&message, pvContext);

    if (fDelayedCancel)
    {
        ExitWithRootFailure(hr, HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT), "Bootstrapper application cancelled during package process progress, exit code: 0x%x", *pdwExitCode);
    }

LExit:
    StrSecureZeroFreeString(sczCommand);
    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);

    return hr;
}

extern "C" void ExeEngineUpdateInstallRegistrationState(
    __in BURN_EXECUTE_ACTION* pAction,
    __in HRESULT hrExecute
    )
{
    BURN_PACKAGE* pPackage = pAction->exePackage.pPackage;

    if (FAILED(hrExecute) || !pPackage->fCanAffectRegistration)
    {
        ExitFunction();
    }

    if (BOOTSTRAPPER_ACTION_STATE_UNINSTALL == pAction->exePackage.action)
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

extern "C" HRESULT ExeEngineParseExitCodesFromXml(
    __in IXMLDOMNode* pixnPackage,
    __inout BURN_EXE_EXIT_CODE** prgExitCodes,
    __inout DWORD* pcExitCodes
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // select exit code nodes
    hr = XmlSelectNodes(pixnPackage, L"ExitCode", &pixnNodes);
    ExitOnFailure(hr, "Failed to select exit code nodes.");

    // get exit code node count
    hr = pixnNodes->get_length((long*) &cNodes);
    ExitOnFailure(hr, "Failed to get exit code node count.");

    if (cNodes)
    {
        // allocate memory for exit codes
        *prgExitCodes = (BURN_EXE_EXIT_CODE*) MemAlloc(sizeof(BURN_EXE_EXIT_CODE) * cNodes, TRUE);
        ExitOnNull(*prgExitCodes, hr, E_OUTOFMEMORY, "Failed to allocate memory for exit code structs.");

        *pcExitCodes = cNodes;

        // parse package elements
        for (DWORD i = 0; i < cNodes; ++i)
        {
            BURN_EXE_EXIT_CODE* pExitCode = *prgExitCodes + i;

            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnFailure(hr, "Failed to get next node.");

            // @Type
            hr = XmlGetAttributeNumber(pixnNode, L"Type", (DWORD*)&pExitCode->type);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Type.");

            // @Code
            hr = XmlGetAttributeEx(pixnNode, L"Code", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Code.");

            if (L'*' == scz[0])
            {
                pExitCode->fWildcard = TRUE;
            }
            else
            {
                hr = StrStringToInt32(scz, 0, reinterpret_cast<INT*>(&pExitCode->dwCode));
                ExitOnFailure(hr, "Failed to parse @Code value: %ls", scz);
            }

            // prepare next iteration
            ReleaseNullObject(pixnNode);
        }
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

extern "C" HRESULT ExeEngineParseCommandLineArgumentsFromXml(
    __in IXMLDOMNode* pixnPackage,
    __inout BURN_EXE_COMMAND_LINE_ARGUMENT** prgCommandLineArguments,
    __inout DWORD* pcCommandLineArguments
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // Select command-line argument nodes.
    hr = XmlSelectNodes(pixnPackage, L"CommandLine", &pixnNodes);
    ExitOnFailure(hr, "Failed to select command-line argument nodes.");

    // Get command-line argument node count.
    hr = pixnNodes->get_length((long*) &cNodes);
    ExitOnFailure(hr, "Failed to get command-line argument count.");

    if (cNodes)
    {
        *prgCommandLineArguments = (BURN_EXE_COMMAND_LINE_ARGUMENT*) MemAlloc(sizeof(BURN_EXE_COMMAND_LINE_ARGUMENT) * cNodes, TRUE);
        ExitOnNull(*prgCommandLineArguments, hr, E_OUTOFMEMORY, "Failed to allocate memory for command-line argument structs.");

        *pcCommandLineArguments = cNodes;

        // Parse command-line argument elements.
        for (DWORD i = 0; i < cNodes; ++i)
        {
            BURN_EXE_COMMAND_LINE_ARGUMENT* pCommandLineArgument = *prgCommandLineArguments + i;
            BOOL fFoundXml = FALSE;

            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnFailure(hr, "Failed to get next command-line argument node.");

            // @InstallArgument
            hr = XmlGetAttributeEx(pixnNode, L"InstallArgument", &pCommandLineArgument->sczInstallArgument);
            ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @InstallArgument.");

            // @UninstallArgument
            hr = XmlGetAttributeEx(pixnNode, L"UninstallArgument", &pCommandLineArgument->sczUninstallArgument);
            ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @UninstallArgument.");

            // @RepairArgument
            hr = XmlGetAttributeEx(pixnNode, L"RepairArgument", &pCommandLineArgument->sczRepairArgument);
            ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @RepairArgument.");

            // @Condition
            hr = XmlGetAttributeEx(pixnNode, L"Condition", &pCommandLineArgument->sczCondition);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Condition.");

            // Prepare next iteration.
            ReleaseNullObject(pixnNode);
        }
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

extern "C" HRESULT ExeEngineHandleExitCode(
    __in BURN_EXE_EXIT_CODE* rgCustomExitCodes,
    __in DWORD cCustomExitCodes,
    __in DWORD dwExitCode,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    )
{
    HRESULT hr = S_OK;
    BURN_EXE_EXIT_CODE_TYPE typeCode = BURN_EXE_EXIT_CODE_TYPE_NONE;

    for (DWORD i = 0; i < cCustomExitCodes; ++i)
    {
        BURN_EXE_EXIT_CODE* pExitCode = rgCustomExitCodes + i;

        // If this is a wildcard, use the last one we come across.
        if (pExitCode->fWildcard)
        {
            typeCode = pExitCode->type;
        }
        else if (dwExitCode == pExitCode->dwCode) // If we have an exact match on the error code use that and stop looking.
        {
            typeCode = pExitCode->type;
            break;
        }
    }

    // If we didn't find a matching code then treat 0 as success, the standard restarts codes as restarts
    // and everything else as an error.
    if (BURN_EXE_EXIT_CODE_TYPE_NONE == typeCode)
    {
        if (0 == dwExitCode)
        {
            typeCode = BURN_EXE_EXIT_CODE_TYPE_SUCCESS;
        }
        else if (ERROR_SUCCESS_REBOOT_REQUIRED == dwExitCode ||
                 HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED) == static_cast<HRESULT>(dwExitCode) ||
                 ERROR_SUCCESS_RESTART_REQUIRED == dwExitCode ||
                 HRESULT_FROM_WIN32(ERROR_SUCCESS_RESTART_REQUIRED) == static_cast<HRESULT>(dwExitCode))
        {
            typeCode = BURN_EXE_EXIT_CODE_TYPE_SCHEDULE_REBOOT;
        }
        else if (ERROR_SUCCESS_REBOOT_INITIATED == dwExitCode ||
                 HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_INITIATED) == static_cast<HRESULT>(dwExitCode))
        {
            typeCode = BURN_EXE_EXIT_CODE_TYPE_FORCE_REBOOT;
        }
        else
        {
            typeCode = BURN_EXE_EXIT_CODE_TYPE_ERROR;
        }
    }

    switch (typeCode)
    {
    case BURN_EXE_EXIT_CODE_TYPE_SUCCESS:
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;
        hr = S_OK;
        break;

    case BURN_EXE_EXIT_CODE_TYPE_ERROR:
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_NONE;
        hr = HRESULT_FROM_WIN32(dwExitCode);
        if (SUCCEEDED(hr))
        {
            hr = E_FAIL;
        }
        break;

    case BURN_EXE_EXIT_CODE_TYPE_SCHEDULE_REBOOT:
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_REQUIRED;
        hr = S_OK;
        break;

    case BURN_EXE_EXIT_CODE_TYPE_FORCE_REBOOT:
        *pRestart = BOOTSTRAPPER_APPLY_RESTART_INITIATED;
        hr = S_OK;
        break;

    default:
        hr = E_UNEXPECTED;
        break;
    }

//LExit:
    return hr;
}
