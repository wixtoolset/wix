// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define PathExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_PATHUTIL, x, e, s, __VA_ARGS__)
#define PathExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_PATHUTIL, x, s, __VA_ARGS__)
#define PathExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_PATHUTIL, p, x, e, s, __VA_ARGS__)
#define PathExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_PATHUTIL, p, x, s, __VA_ARGS__)
#define PathExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_PATHUTIL, p, x, e, s, __VA_ARGS__)
#define PathExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_PATHUTIL, p, x, s, __VA_ARGS__)
#define PathExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_PATHUTIL, e, x, s, __VA_ARGS__)
#define PathExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_PATHUTIL, g, x, s, __VA_ARGS__)

static HRESULT GetTempPathFromSystemEnvironmentVariable(
    __in HKEY hKey,
    __in_z LPCWSTR wzName,
    __out_z LPWSTR* psczPath
    );

DAPI_(HRESULT) PathGetSystemTempPaths(
    __inout_z LPWSTR** prgsczSystemTempPaths,
    __inout DWORD* pcSystemTempPaths
    )
{
    HRESULT hr = S_OK;
    HMODULE hModule = NULL;
    BOOL fSystem = FALSE;
    HKEY hKey = NULL;
    LPWSTR sczTemp = NULL;

    // Follow documented precedence rules for SystemTemp/%TMP%/%TEMP% from ::GetTempPath2.
    hr = LoadSystemLibrary(L"kernel32.dll", &hModule);
    PathExitOnFailure(hr, "Failed to load kernel32.dll");

    // The SystemTemp folder was added at the same time as ::GetTempPath2.
    if (::GetProcAddress(hModule, "GetTempPath2W"))
    {
        hr = ProcSystem(::GetCurrentProcess(), &fSystem);
        PathExitOnFailure(hr, "Failed to check if running as system.");

        if (fSystem)
        {
            hr = PathSystemWindowsSubdirectory(L"SystemTemp", &sczTemp);
            PathExitOnFailure(hr, "Failed to get system Windows subdirectory path SystemTemp.");

            hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(prgsczSystemTempPaths), *pcSystemTempPaths, 1, sizeof(LPWSTR), 4);
            PathExitOnFailure(hr, "Failed to ensure array size for Windows\\SystemTemp value.");

            (*prgsczSystemTempPaths)[*pcSystemTempPaths] = sczTemp;
            sczTemp = NULL;
            *pcSystemTempPaths += 1;
        }
    }

    // There is no documented API to get system environment variables, so read them from the registry.
    hr = RegOpen(HKEY_LOCAL_MACHINE, L"System\\CurrentControlSet\\Control\\Session Manager\\Environment", KEY_READ, &hKey);
    if (E_FILENOTFOUND != hr)
    {
        PathExitOnFailure(hr, "Failed to open system environment registry key.");

        hr = GetTempPathFromSystemEnvironmentVariable(hKey, L"TMP", &sczTemp);
        PathExitOnFailure(hr, "Failed to get temp path from system TMP.");

        if (S_FALSE != hr)
        {
            hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(prgsczSystemTempPaths), *pcSystemTempPaths, 1, sizeof(LPWSTR), 3);
            PathExitOnFailure(hr, "Failed to ensure array size for system TMP value.");

            (*prgsczSystemTempPaths)[*pcSystemTempPaths] = sczTemp;
            sczTemp = NULL;
            *pcSystemTempPaths += 1;
        }

        hr = GetTempPathFromSystemEnvironmentVariable(hKey, L"TEMP", &sczTemp);
        PathExitOnFailure(hr, "Failed to get temp path from system TEMP.");

        if (S_FALSE != hr)
        {
            hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(prgsczSystemTempPaths), *pcSystemTempPaths, 1, sizeof(LPWSTR), 2);
            PathExitOnFailure(hr, "Failed to ensure array size for system TEMP value.");

            (*prgsczSystemTempPaths)[*pcSystemTempPaths] = sczTemp;
            sczTemp = NULL;
            *pcSystemTempPaths += 1;
        }
    }

    hr = PathSystemWindowsSubdirectory(L"TEMP", &sczTemp);
    PathExitOnFailure(hr, "Failed to get system Windows subdirectory path TEMP.");

    hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(prgsczSystemTempPaths), *pcSystemTempPaths, 1, sizeof(LPWSTR), 1);
    PathExitOnFailure(hr, "Failed to ensure array size for Windows\\TEMP value.");

    (*prgsczSystemTempPaths)[*pcSystemTempPaths] = sczTemp;
    sczTemp = NULL;
    *pcSystemTempPaths += 1;

LExit:
    ReleaseRegKey(hKey);
    ReleaseStr(sczTemp);

    return hr;
}

static HRESULT GetTempPathFromSystemEnvironmentVariable(
    __in HKEY hKey,
    __in_z LPCWSTR wzName,
    __out_z LPWSTR* psczPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;
    BOOL fNeedsExpansion = FALSE;

    // Read the value unexpanded so that it can be expanded with system environment variables.
    hr = RegReadUnexpandedString(hKey, wzName, &fNeedsExpansion, &sczValue);
    if (E_FILENOTFOUND == hr)
    {
        ExitFunction1(hr = S_FALSE);
    }
    PathExitOnFailure(hr, "Failed to get system '%ls' value.", wzName);

    if (fNeedsExpansion)
    {
        hr = EnvExpandEnvironmentStringsForUser(NULL, sczValue, psczPath, NULL);
        PathExitOnFailure(hr, "Failed to expand environment variables for system in string: %ls", sczValue);
    }

    hr = PathBackslashTerminate(psczPath);
    PathExitOnFailure(hr, "Failed to backslash terminate system '%ls' value.", wzName);

LExit:
    ReleaseStr(sczValue);

    return hr;
}
