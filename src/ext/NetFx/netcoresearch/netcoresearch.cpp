// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

enum class NETCORESEARCHKIND
{
    None,
    Runtime,
    Sdk,
};

struct NETCORESEARCH_STATE
{
    NETCORESEARCHKIND Kind = NETCORESEARCHKIND::None;
    union
    {
        struct
        {
            LPCWSTR wzTargetName;
            DWORD dwMajorVersion;
        } Runtime;
        struct
        {
            DWORD dwMajorVersion;
            DWORD dwMinorVersion;
            DWORD dwFeatureBand;
        }
         Sdk;
    } Data;
    VERUTIL_VERSION* pVersion;
};

static HRESULT GetDotnetEnvironmentInfo(
    __in NETCORESEARCH_STATE& pSearchState,
    __inout VERUTIL_VERSION** ppVersion
    );
static void HOSTFXR_CALLTYPE GetDotnetEnvironmentInfoResult(
    __in const hostfxr_dotnet_environment_info* pInfo,
    __in LPVOID pvContext
    );

bool string_equal_invariant(__in PCWSTR const x,__in  PCWSTR const y) { return CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, x, -1, y, -1); }

HRESULT get_search_state_from_arguments(__in int argc, __in LPWSTR argv[], __out NETCORESEARCH_STATE& searchState);

int __cdecl wmain(int argc, LPWSTR argv[])
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pVersion = NULL;
    NETCORESEARCH_STATE searchState = {};

    ::SetConsoleCP(CP_UTF8);

    ConsoleInitialize();

    hr = get_search_state_from_arguments(argc, argv, OUT searchState);
    if (FAILED(hr))
    {
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to parse arguments.");
    }

    hr = GetDotnetEnvironmentInfo(searchState, &pVersion);


    if (pVersion)
    {
        ConsoleWriteW(CONSOLE_COLOR_NORMAL, pVersion->sczVersion);
    }

LExit:
    ReleaseVerutilVersion(pVersion);
    ConsoleUninitialize();
    return hr;
}

HRESULT get_search_state_from_arguments(int argc, LPWSTR argv[], __out NETCORESEARCH_STATE& searchState)
{
    HRESULT hr = S_OK;
    searchState = {};
    const auto searchKind = argv[1];

    if (argc < 3)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }


    if (string_equal_invariant(searchKind, L"runtime"))
    {
        if (argc != 4)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }
        searchState.Kind = NETCORESEARCHKIND::Runtime;

        const PCWSTR majorVersion = argv[2];
        const PCWSTR targetName = argv[3];

        auto& data = searchState.Data.Runtime;

        data.wzTargetName = targetName;
        hr = StrStringToUInt32(majorVersion, 0, reinterpret_cast<UINT*>(&data.dwMajorVersion));
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get target version from: %ls", majorVersion);
    }
    else if(string_equal_invariant(searchKind, L"sdk"))
    {
        searchState.Kind = NETCORESEARCHKIND::Sdk;

        const PCWSTR version = argv[2];

        VERUTIL_VERSION* sdkVersion = nullptr;
        hr = VerParseVersion(version, 0, FALSE, &sdkVersion);
        if (FAILED(hr))
        {
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to parse version from: %ls", version);
        }

        auto& data = searchState.Data.Sdk;

        data.dwMajorVersion = sdkVersion->dwMajor;
        data.dwMinorVersion = sdkVersion->dwMinor;
        data.dwFeatureBand = sdkVersion->dwPatch;

        VerFreeVersion(sdkVersion);
    }

LExit:
    return hr;
}



static HRESULT GetDotnetEnvironmentInfo(
    __in NETCORESEARCH_STATE& state,
    __inout VERUTIL_VERSION** ppVersion
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczProcessPath = NULL;
    LPWSTR sczHostfxrPath = NULL;
    HMODULE hModule = NULL;
    hostfxr_get_dotnet_environment_info_fn pfnGetDotnetEnvironmentInfo = NULL;

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

bool matches_feature_band(const int requested, const int actual)
{
    // we have not requested a match on feature band, so skip the check
    if (requested == 0) return true;

    const int requestedBand = requested / 100;
    const int actualBand = actual  / 100;

    if (actualBand != requestedBand) return false;

    return actual >= requested;
}

static void HOSTFXR_CALLTYPE GetDotnetEnvironmentInfoResult(
    __in const hostfxr_dotnet_environment_info* pInfo,
    __in LPVOID pvContext
    )
{
    NETCORESEARCH_STATE* pState = static_cast<NETCORESEARCH_STATE*>(pvContext);
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pDotnetVersion = nullptr;
    int nCompare = 0;


    if (pState->Kind == NETCORESEARCHKIND::Sdk)
    {
        auto& sdkData = pState->Data.Sdk;
        for (size_t i = 0; i < pInfo->sdk_count; ++i)
        {
            const hostfxr_dotnet_environment_sdk_info* pSdkInfo = pInfo->sdks + i;
            ReleaseVerutilVersion(pDotnetVersion);

            hr = VerParseVersion(pSdkInfo->version, 0, FALSE, &pDotnetVersion);
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to parse sdk version: %ls", pSdkInfo->version);

            if (pDotnetVersion->dwMajor != sdkData.dwMajorVersion)
            {
                continue;
            }
            if (!matches_feature_band(sdkData.dwFeatureBand, pDotnetVersion->dwPatch))
            {
                continue;
            }

            if (pState->pVersion)
            {
                hr = VerCompareParsedVersions(pState->pVersion, pDotnetVersion, &nCompare);
                ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to compare versions.");

                if (nCompare > -1)
                {
                    continue;
                }
            }

            ReleaseVerutilVersion(pState->pVersion);
            pState->pVersion = pDotnetVersion;
            pDotnetVersion = nullptr;
        }
    }
    else if(pState->Kind == NETCORESEARCHKIND::Runtime)
    {
        auto& runtimeData = pState->Data.Runtime;
        for (size_t i = 0; i < pInfo->framework_count; ++i)
        {
            const hostfxr_dotnet_environment_framework_info* pFrameworkInfo = pInfo->frameworks + i;
            ReleaseVerutilVersion(pDotnetVersion);

            if (string_equal_invariant(runtimeData.wzTargetName, pFrameworkInfo->name))
            {
                continue;
            }

            hr = VerParseVersion(pFrameworkInfo->version, 0, FALSE, &pDotnetVersion);
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to parse framework version: %ls", pFrameworkInfo->version);

            if (pDotnetVersion->dwMajor != runtimeData.dwMajorVersion)
            {
                continue;
            }

            if (pState->pVersion)
            {
                hr = VerCompareParsedVersions(pState->pVersion, pDotnetVersion, &nCompare);
                ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to compare versions.");

                if (nCompare > -1)
                {
                    continue;
                }
            }

            ReleaseVerutilVersion(pState->pVersion);
            pState->pVersion = pDotnetVersion;
            pDotnetVersion = nullptr;
        }
    }
    else
    {
        ConsoleWriteError(E_INVALIDARG, CONSOLE_COLOR_RED, "Invalid NETCORESEARCHKIND.");
    }

LExit:
    ReleaseVerutilVersion(pDotnetVersion);
}
