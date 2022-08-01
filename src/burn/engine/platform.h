#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#define TraceLog(x, s, ...) ExitTraceSource(BURN_SOURCE_DEFAULT, x, s, __VA_ARGS__)


#if defined(__cplusplus)
extern "C" {
#endif

// forward declare

enum BURN_MODE;
typedef struct _BOOTSTRAPPER_ENGINE_CONTEXT BOOTSTRAPPER_ENGINE_CONTEXT;
typedef struct _BURN_CACHE BURN_CACHE;
typedef struct _BURN_DEPENDENCIES BURN_DEPENDENCIES;
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
