#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#define TraceLog(x, s, ...) ExitTraceSource(BURN_SOURCE_DEFAULT, x, s, __VA_ARGS__)


#if defined(__cplusplus)
extern "C" {
#endif

// constants

enum WM_BURN
{
    WM_BURN_FIRST = WM_APP + 0xFFF, // this enum value must always be first.

    WM_BURN_DETECT,
    WM_BURN_PLAN,
    WM_BURN_ELEVATE,
    WM_BURN_APPLY,
    WM_BURN_LAUNCH_APPROVED_EXE,
    WM_BURN_QUIT,

    WM_BURN_LAST, // this enum value must always be last.
};

// forward declare

enum BURN_MODE;
typedef struct _BAENGINE_CONTEXT BAENGINE_CONTEXT;
typedef struct _BAENGINE_ACTION BAENGINE_ACTION;
typedef struct _BURN_CACHE BURN_CACHE;
typedef struct _BURN_DEPENDENCIES BURN_DEPENDENCIES;
typedef struct _BURN_ENGINE_STATE BURN_ENGINE_STATE;
typedef struct _BURN_ENGINE_COMMAND BURN_ENGINE_COMMAND;
typedef struct  _BURN_LOGGING BURN_LOGGING;
typedef struct  _BURN_PACKAGES BURN_PACKAGES;


// typedefs

typedef BOOL (WINAPI *PFN_INITIATESYSTEMSHUTDOWNEXW)(
    __in_opt LPWSTR lpMachineName,
    __in_opt LPWSTR lpMessage,
    __in DWORD dwTimeout,
    __in BOOL bForceAppsClosed,
    __in BOOL bRebootAfterShutdown,
    __in DWORD dwReason
    );


// variable declarations

extern PFN_INITIATESYSTEMSHUTDOWNEXW vpfnInitiateSystemShutdownExW;


// function declarations

void PlatformInitialize();


#if defined(__cplusplus)
}
#endif
