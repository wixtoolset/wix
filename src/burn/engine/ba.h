#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#define BAAPI HRESULT __stdcall

#if defined(__cplusplus)
extern "C" {
#endif


// constants

const DWORD BURN_MB_RETRYTRYAGAIN = 0x10;
const DWORD64 BOOTSTRAPPER_APPLICATION_API_VERSION = MAKEQWORDVERSION(2024, 1, 1, 0);


// structs

typedef struct _BURN_USER_EXPERIENCE
{
    BURN_PAYLOADS payloads;

    BURN_PAYLOAD* pPrimaryExePayload;
    BURN_PAYLOAD* pSecondaryExePayload;

    //HMODULE hUXModule;
    //PFN_BOOTSTRAPPER_APPLICATION_PROC pfnBAProc;
    //LPVOID pvBAProcContext;
    HANDLE hBAProcess;
    PIPE_RPC_HANDLE hBARpcPipe;
    BAENGINE_CONTEXT* pEngineContext;

    LPWSTR sczTempDirectory;

    CRITICAL_SECTION csEngineActive;    // Changing the engine active state in the user experience must be
                                        // syncronized through this critical section.
                                        // Note: The engine must never do a UX callback while in this critical section.

    BOOL fEngineActive;                 // Indicates that the engine is currently active with one of the execution
                                        // steps (detect, plan, apply), and cannot accept requests from the UX.
                                        // This flag should be cleared by the engine prior to UX callbacks that
                                        // allow altering of the engine state.

    HRESULT hrApplyError;               // Tracks if an error occurs during apply that requires the cache or
                                        // execute threads to bail.

    HWND hwndApply;                     // The window handle provided at the beginning of Apply(). Only valid
                                        // during apply.

    HWND hwndDetect;                    // The window handle provided at the beginning of Detect(). Only valid
                                        // during Detect.

    DWORD dwExitCode;                   // Exit code returned by the user experience for the engine overall.
} BURN_USER_EXPERIENCE;


// functions

/*******************************************************************
 BootstrapperApplicationParseFromXml - parses the bootstrapper application
    data embedded in the bundle.

*******************************************************************/
HRESULT BootstrapperApplicationParseFromXml(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IXMLDOMNode* pixnBundle
);

/*******************************************************************
 BootstrapperApplicationUninitialize - uninitializes the bootstrapper
    application data.

*******************************************************************/
void BootstrapperApplicationUninitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience
);

/*******************************************************************
 BootstrapperApplicationStart - starts the bootstrapper application
    process and creates the bootstrapper application in it.

*******************************************************************/
HRESULT BootstrapperApplicationStart(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BOOL fSecondary
);

/*******************************************************************
 BootstrapperApplicationStop - destroys the bootstrapper application
    in the bootstrapper application process, disconnects and waits
    for the process to exit.

*******************************************************************/
HRESULT BootstrapperApplicationStop(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOL* pfReload
);

int BootstrapperApplicationCheckExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
);

HRESULT BootstrapperApplicationInterpretExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
);

HRESULT BootstrapperApplicationEnsureWorkingFolder(
    __in BOOL fElevated,
    __in BURN_CACHE* pCache,
    __deref_out_z LPWSTR* psczUserExperienceWorkingFolder
);

HRESULT BootstrapperApplicationRemove(
    __in BURN_USER_EXPERIENCE* pUserExperience
);

int BootstrapperApplicationSendError(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in HRESULT hrCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD uiFlags,
    __in int nRecommendation
);

void BootstrapperApplicationActivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
);

void BootstrapperApplicationDeactivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
);

/********************************************************************
 BootstrapperApplicationEnsureEngineInactive - Verifies the engine is inactive.
   The caller MUST enter the csActive critical section before calling.

*********************************************************************/
HRESULT BootstrapperApplicationEnsureEngineInactive(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );

void BootstrapperApplicationExecuteReset(
    __in BURN_USER_EXPERIENCE* pUserExperience
    );

void BootstrapperApplicationExecutePhaseComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrResult
    );

#if defined(__cplusplus)
}
#endif
