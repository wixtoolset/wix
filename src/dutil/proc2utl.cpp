// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define ProcExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_PROCUTIL, x, s, __VA_ARGS__)
#define ProcExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_PROCUTIL, p, x, e, s, __VA_ARGS__)
#define ProcExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_PROCUTIL, p, x, s, __VA_ARGS__)
#define ProcExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_PROCUTIL, p, x, e, s, __VA_ARGS__)
#define ProcExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_PROCUTIL, p, x, s, __VA_ARGS__)
#define ProcExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_PROCUTIL, e, x, s, __VA_ARGS__)
#define ProcExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_PROCUTIL, g, x, s, __VA_ARGS__)

/********************************************************************
 ProcFindAllIdsFromExeName() - returns an array of process ids that are running specified executable.

*******************************************************************/
extern "C" HRESULT DAPI ProcFindAllIdsFromExeName(
    __in_z LPCWSTR wzExeName,
    __out DWORD** ppdwProcessIds,
    __out DWORD* pcProcessIds
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HANDLE hSnap = INVALID_HANDLE_VALUE;
    BOOL fContinue = FALSE;
    PROCESSENTRY32W peData = { sizeof(peData) };
    
    hSnap = ::CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
    if (INVALID_HANDLE_VALUE == hSnap)
    {
        ProcExitWithLastError(hr, "Failed to create snapshot of processes on system");
    }

    fContinue = ::Process32FirstW(hSnap, &peData);

    while (fContinue)
    {
        if (0 == lstrcmpiW((LPCWSTR)&(peData.szExeFile), wzExeName))
        {
            if (!*ppdwProcessIds)
            {
                *ppdwProcessIds = static_cast<DWORD*>(MemAlloc(sizeof(DWORD), TRUE));
                ProcExitOnNull(ppdwProcessIds, hr, E_OUTOFMEMORY, "Failed to allocate array for returned process IDs.");
            }
            else
            {
                DWORD* pdwReAllocReturnedPids = NULL;
                pdwReAllocReturnedPids = static_cast<DWORD*>(MemReAlloc(*ppdwProcessIds, sizeof(DWORD) * ((*pcProcessIds) + 1), TRUE));
                ProcExitOnNull(pdwReAllocReturnedPids, hr, E_OUTOFMEMORY, "Failed to re-allocate array for returned process IDs.");

                *ppdwProcessIds = pdwReAllocReturnedPids;
            }
            
            (*ppdwProcessIds)[*pcProcessIds] = peData.th32ProcessID;
            ++(*pcProcessIds);
        }

        fContinue = ::Process32NextW(hSnap, &peData);
    }

    er = ::GetLastError();
    if (ERROR_NO_MORE_FILES == er)
    {
        hr = S_OK;
    }
    else
    {
        hr = HRESULT_FROM_WIN32(er);
    }

LExit:
    ReleaseFile(hSnap);

    return hr;
}
