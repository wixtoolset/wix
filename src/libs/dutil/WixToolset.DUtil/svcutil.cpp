// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define SvcExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_SVCUTIL, x, s, __VA_ARGS__)
#define SvcExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_SVCUTIL, x, s, __VA_ARGS__)
#define SvcExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_SVCUTIL, x, s, __VA_ARGS__)
#define SvcExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_SVCUTIL, x, s, __VA_ARGS__)
#define SvcExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_SVCUTIL, x, s, __VA_ARGS__)
#define SvcExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_SVCUTIL, x, s, __VA_ARGS__)
#define SvcExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_SVCUTIL, p, x, e, s, __VA_ARGS__)
#define SvcExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_SVCUTIL, p, x, s, __VA_ARGS__)
#define SvcExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_SVCUTIL, p, x, e, s, __VA_ARGS__)
#define SvcExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_SVCUTIL, p, x, s, __VA_ARGS__)
#define SvcExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_SVCUTIL, e, x, s, __VA_ARGS__)
#define SvcExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_SVCUTIL, g, x, s, __VA_ARGS__)

/********************************************************************
SvcQueryConfig - queries the configuration of a service

********************************************************************/
extern "C" HRESULT DAPI SvcQueryConfig(
    __in SC_HANDLE sch,
    __out QUERY_SERVICE_CONFIGW** ppConfig
    )
{
    HRESULT hr = S_OK;
    QUERY_SERVICE_CONFIGW* pConfig = NULL;
    DWORD cbConfig = 0;

    if (!::QueryServiceConfigW(sch, NULL, 0, &cbConfig))
    {
        DWORD er = ::GetLastError();
        if (ERROR_INSUFFICIENT_BUFFER == er)
        {
            pConfig = static_cast<QUERY_SERVICE_CONFIGW*>(MemAlloc(cbConfig, TRUE));
            SvcExitOnNull(pConfig, hr, E_OUTOFMEMORY, "Failed to allocate memory to get configuration.");

            if (!::QueryServiceConfigW(sch, pConfig, cbConfig, &cbConfig))
            {
                SvcExitWithLastError(hr, "Failed to read service configuration.");
            }
        }
        else
        {
            SvcExitOnWin32Error(er, hr, "Failed to query service configuration.");
        }
    }

    *ppConfig = pConfig;
    pConfig = NULL;

LExit:
    ReleaseMem(pConfig);

    return hr;
}
