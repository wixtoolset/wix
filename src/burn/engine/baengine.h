#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

// structs

typedef struct _BAENGINE_CONTEXT
{
    BURN_ENGINE_STATE* pEngineState;
    QUEUTIL_QUEUE_HANDLE hQueue;
    HANDLE hQueueSemaphore;
    CRITICAL_SECTION csQueue;

    PIPE_RPC_HANDLE hRpcPipe;
    HANDLE hThread;
} BAENGINE_CONTEXT;

typedef struct _BAENGINE_ACTION
{
    WM_BURN dwMessage;
    union
    {
        struct
        {
            HWND hwndParent;
        } detect;
        struct
        {
            BOOTSTRAPPER_ACTION action;
        } plan;
        struct
        {
            HWND hwndParent;
        } elevate;
        struct
        {
            HWND hwndParent;
        } apply;
        BURN_LAUNCH_APPROVED_EXE launchApprovedExe;
        struct
        {
            DWORD dwExitCode;
        } quit;
    };
} BAENGINE_ACTION;

// function declarations

HRESULT BAEngineCreateContext(
    __in BURN_ENGINE_STATE* pEngineState,
    __inout BAENGINE_CONTEXT** ppContext
);

void BAEngineFreeContext(
    __in BAENGINE_CONTEXT* pContext
);

void DAPI BAEngineFreeAction(
    __in BAENGINE_ACTION* pAction
);

HRESULT BAEngineStartListening(
    __in BAENGINE_CONTEXT* pContext,
    __in HANDLE hBAEnginePipe
);

HRESULT BAEngineStopListening(
    __in BAENGINE_CONTEXT * pContext
);

HRESULT WINAPI EngineForApplicationProc(
    __in BAENGINE_CONTEXT* pvContext,
    __in BOOTSTRAPPER_ENGINE_MESSAGE message,
    __in_bcount(cbArgs) const LPVOID pvArgs,
    __in DWORD /*cbArgs*/
);

#if defined(__cplusplus)
}
#endif
