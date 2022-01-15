#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// function declarations

HRESULT ExeEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnExePackage,
    __in BURN_PACKAGE* pPackage
    );
void ExeEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    );
void ExeEngineCommandLineArgumentUninitialize(
    __in BURN_EXE_COMMAND_LINE_ARGUMENT* pCommandLineArgument
    );
HRESULT ExeEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    );
HRESULT ExeEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage
    );
HRESULT ExeEnginePlanAddPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    );
HRESULT ExeEngineExecutePackage(
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericExecuteProgress,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
void ExeEngineUpdateInstallRegistrationState(
    __in BURN_EXECUTE_ACTION* pAction,
    __in HRESULT hrExecute
    );
HRESULT ExeEngineParseExitCodesFromXml(
    __in IXMLDOMNode* pixnPackage,
    __inout BURN_EXE_EXIT_CODE** prgExitCodes,
    __inout DWORD* pcExitCodes
    );
HRESULT ExeEngineParseCommandLineArgumentsFromXml(
    __in IXMLDOMNode* pixnPackage,
    __inout BURN_EXE_COMMAND_LINE_ARGUMENT** prgCommandLineArguments,
    __inout DWORD* pcCommandLineArguments
    );
HRESULT ExeEngineHandleExitCode(
    __in BURN_EXE_EXIT_CODE* rgCustomExitCodes,
    __in DWORD cCustomExitCodes,
    __in DWORD dwExitCode,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );


#if defined(__cplusplus)
}
#endif
