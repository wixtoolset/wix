// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

struct NETCORESEARCH_STATE
{
    LPCWSTR wzTargetName;
    DWORD dwMajorVersion;
    VERUTIL_VERSION* pVersion;
};

static HRESULT GetDotnetEnvironmentInfo(
    __in DWORD dwMajorVersion,
    __in_z LPCWSTR wzTargetName,
    __inout VERUTIL_VERSION** ppVersion
    );
static void HOSTFXR_CALLTYPE GetDotnetEnvironmentInfoResult(
    __in const hostfxr_dotnet_environment_info* pInfo,
    __in LPVOID pvContext
    );

int __cdecl wmain(int argc, LPWSTR argv[])
{
    HRESULT hr = S_OK;
    DWORD dwMajorVersion = 0;
    VERUTIL_VERSION* pVersion = NULL;
    LPSTR pszVersion = NULL;

    ::SetConsoleCP(CP_UTF8);

    ConsoleInitialize();

    if (argc != 3)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    hr = StrStringToUInt32(argv[1], 0, reinterpret_cast<UINT*>(&dwMajorVersion));
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get target version from: %ls", argv[1]);

    hr = GetDotnetEnvironmentInfo(dwMajorVersion, argv[2], &pVersion);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to search for .NET Core.");

    if (pVersion)
    {
        hr = StrAnsiAllocString(&pszVersion, pVersion->sczVersion, 0, CP_UTF8);
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to convert version to UTF-8.");

        ConsoleWrite(CONSOLE_COLOR_NORMAL, "%hs", pszVersion);
    }

LExit:
    ReleaseStr(pszVersion);
    ReleaseVerutilVersion(pVersion);
    ConsoleUninitialize();
    return hr;
}

static HRESULT GetDotnetEnvironmentInfo(
    __in DWORD dwMajorVersion,
    __in_z LPCWSTR wzTargetName,
    __inout VERUTIL_VERSION** ppVersion
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczProcessPath = NULL;
    LPWSTR sczHostfxrPath = NULL;
    HMODULE hModule = NULL;
    hostfxr_get_dotnet_environment_info_fn pfnGetDotnetEnvironmentInfo = NULL;
    NETCORESEARCH_STATE state = { };

    state.dwMajorVersion = dwMajorVersion;
    state.wzTargetName = wzTargetName;

    hr = PathForCurrentProcess(&sczProcessPath, NULL);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get process path.");

    hr = PathGetDirectory(sczProcessPath, &sczHostfxrPath);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get process directory.");

    hr = StrAllocConcat(&sczHostfxrPath, L"hostfxr.dll", 0);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to build hostfxr path.");

    hModule = ::LoadLibraryExW(sczHostfxrPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    ConsoleExitOnNullWithLastError(hModule, hr, CONSOLE_COLOR_RED, "Failed to load hostfxr.");

    pfnGetDotnetEnvironmentInfo = (hostfxr_get_dotnet_environment_info_fn)::GetProcAddress(hModule, "hostfxr_get_dotnet_environment_info");
    ConsoleExitOnNullWithLastError(pfnGetDotnetEnvironmentInfo, hr, CONSOLE_COLOR_RED, "Failed to get address for hostfxr_get_dotnet_environment_info.");

    hr = pfnGetDotnetEnvironmentInfo(NULL, NULL, GetDotnetEnvironmentInfoResult, &state);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get .NET Core environment info.");

    if (state.pVersion)
    {
        *ppVersion = state.pVersion;
        state.pVersion = NULL;
    }

LExit:
    ReleaseVerutilVersion(state.pVersion);
    ReleaseStr(sczHostfxrPath);
    ReleaseStr(sczProcessPath);

    if (hModule)
    {
        ::FreeLibrary(hModule);
    }

    return hr;
}

static void HOSTFXR_CALLTYPE GetDotnetEnvironmentInfoResult(
    __in const hostfxr_dotnet_environment_info* pInfo,
    __in LPVOID pvContext
    )
{
    NETCORESEARCH_STATE* pState = reinterpret_cast<NETCORESEARCH_STATE*>(pvContext);
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pFrameworkVersion = NULL;
    int nCompare = 0;

    for (size_t i = 0; i < pInfo->framework_count; ++i)
    {
        const hostfxr_dotnet_environment_framework_info* pFrameworkInfo = pInfo->frameworks + i;
        ReleaseVerutilVersion(pFrameworkVersion);

        if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, pState->wzTargetName, -1, pFrameworkInfo->name, -1))
        {
            continue;
        }

        hr = VerParseVersion(pFrameworkInfo->version, 0, FALSE, &pFrameworkVersion);
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to parse framework version: %ls", pFrameworkInfo->version);

        if (pFrameworkVersion->dwMajor != pState->dwMajorVersion)
        {
            continue;
        }

        if (pState->pVersion)
        {
            hr = VerCompareParsedVersions(pState->pVersion, pFrameworkVersion, &nCompare);
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to compare versions.");

            if (nCompare > -1)
            {
                continue;
            }
        }

        ReleaseVerutilVersion(pState->pVersion);
        pState->pVersion = pFrameworkVersion;
        pFrameworkVersion = NULL;
    }

LExit:
    ReleaseVerutilVersion(pFrameworkVersion);
}
