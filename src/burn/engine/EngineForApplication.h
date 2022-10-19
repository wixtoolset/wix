#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

// structs

typedef struct _BOOTSTRAPPER_ENGINE_CONTEXT
{
    BURN_ENGINE_STATE* pEngineState;
    QUEUTIL_QUEUE_HANDLE hQueue;
    HANDLE hQueueSemaphore;
    CRITICAL_SECTION csQueue;
} BOOTSTRAPPER_ENGINE_CONTEXT;

typedef struct _BOOTSTRAPPER_ENGINE_ACTION
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
} BOOTSTRAPPER_ENGINE_ACTION;

// function declarations

HRESULT WINAPI EngineForApplicationProc(
    __in BOOTSTRAPPER_ENGINE_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    );

#if defined(__cplusplus)
}
#endif
