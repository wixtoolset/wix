// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

enum class NETCORESEARCHTYPE
{
    None,
    Runtime,
    Sdk,
    SdkFeatureBand,
};

struct NETCORESEARCH_STATE
{
    NETCORESEARCHTYPE type;
    HRESULT hrSearch;
    VERUTIL_VERSION* pVersion;

    struct
    {
        LPCWSTR wzTargetName;
        DWORD dwMajorVersion;
    } Runtime;
    struct
    {
        DWORD dwMajorVersion;
    } Sdk;
    struct
    {
        DWORD dwMajorVersion;
        DWORD dwMinorVersion;
        DWORD dwPatchVersion;
    } SdkFeatureBand;
};

static HRESULT GetSearchStateFromArguments(
    __in int argc,
    __in LPWSTR argv[],
    __in NETCORESEARCH_STATE* pSearchState
    );
static HRESULT GetDotnetEnvironmentInfo(
    __in NETCORESEARCH_STATE* pSearchState
    );
static void HOSTFXR_CALLTYPE GetDotnetEnvironmentInfoResult(
    __in const hostfxr_dotnet_environment_info* pInfo,
    __in LPVOID pvContext
    );

int __cdecl wmain(int argc, LPWSTR argv[])
{
    HRESULT hr = S_OK;
    NETCORESEARCH_STATE searchState = { };

    ConsoleInitialize();

    hr = GetSearchStateFromArguments(argc, argv, &searchState);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to parse arguments.");

    hr = GetDotnetEnvironmentInfo(&searchState);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to search.");

    if (searchState.pVersion)
    {
        ConsoleWriteW(CONSOLE_COLOR_NORMAL, searchState.pVersion->sczVersion);
    }

LExit:
    ReleaseVerutilVersion(searchState.pVersion);
    ConsoleUninitialize();
    return hr;
}

HRESULT GetSearchStateFromArguments(
    __in int argc,
    __in LPWSTR argv[],
    __in NETCORESEARCH_STATE* pSearchState
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzSearchKind = NULL;

    if (argc < 2)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    wzSearchKind = argv[1];

    if (CSTR_EQUAL == ::CompareStringOrdinal(wzSearchKind, -1, L"runtime", -1, TRUE))
    {
        if (argc != 4)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }

        LPCWSTR wzMajorVersion = argv[2];
        LPCWSTR wzTargetName = argv[3];

        pSearchState->type = NETCORESEARCHTYPE::Runtime;

        hr = StrStringToUInt32(wzMajorVersion, 0, reinterpret_cast<UINT*>(&pSearchState->Runtime.dwMajorVersion));
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get target version from: %ls", wzMajorVersion);

        pSearchState->Runtime.wzTargetName = wzTargetName;
    }
    else if (CSTR_EQUAL == ::CompareStringOrdinal(wzSearchKind, -1, L"sdk", -1, TRUE))
    {
        if (argc != 3)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }

        LPCWSTR wzMajorVersion = argv[2];

        pSearchState->type = NETCORESEARCHTYPE::Sdk;

        hr = StrStringToUInt32(wzMajorVersion, 0, reinterpret_cast<UINT*>(&pSearchState->Sdk.dwMajorVersion));
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get sdk major version from: %ls", wzMajorVersion);
    }
    else if (CSTR_EQUAL == ::CompareStringOrdinal(wzSearchKind, -1, L"sdkfeatureband", -1, TRUE))
    {
        if (argc != 5)
        {
            ExitFunction1(hr = E_INVALIDARG);
        }

        LPCWSTR wzMajorVersion = argv[2];
        LPCWSTR wzMinorVersion = argv[3];
        LPCWSTR wzPatchVersion = argv[4];

        pSearchState->type = NETCORESEARCHTYPE::SdkFeatureBand;

        hr = StrStringToUInt32(wzMajorVersion, 0, reinterpret_cast<UINT*>(&pSearchState->SdkFeatureBand.dwMajorVersion));
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get major version from: %ls", wzMajorVersion);

        hr = StrStringToUInt32(wzMinorVersion, 0, reinterpret_cast<UINT*>(&pSearchState->SdkFeatureBand.dwMinorVersion));
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get minor version from: %ls", wzMinorVersion);

        hr = StrStringToUInt32(wzPatchVersion, 0, reinterpret_cast<UINT*>(&pSearchState->SdkFeatureBand.dwPatchVersion));
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get patch version from: %ls", wzPatchVersion);
    }
    else
    {
        pSearchState->type = NETCORESEARCHTYPE::None;
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    return hr;
}

static HRESULT GetDotnetEnvironmentInfo(
    __in NETCORESEARCH_STATE* pState
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

    hr = pfnGetDotnetEnvironmentInfo(NULL, NULL, GetDotnetEnvironmentInfoResult, pState);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to get .NET Core environment info.");

    hr = pState->hrSearch;
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to process .NET Core environment info.");

LExit:
    ReleaseStr(sczHostfxrPath);
    ReleaseStr(sczProcessPath);

    if (hModule)
    {
        ::FreeLibrary(hModule);
    }

    return hr;
}

static HRESULT PerformRuntimeSearch(
    __in const hostfxr_dotnet_environment_info* pInfo,
    __in DWORD dwMajorVersion,
    __in LPCWSTR wzTargetName,
    __inout VERUTIL_VERSION** ppVersion
    )
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pFrameworkVersion = NULL;
    int nCompare = 0;

    for (size_t i = 0; i < pInfo->framework_count; ++i)
    {
        const hostfxr_dotnet_environment_framework_info* pFrameworkInfo = pInfo->frameworks + i;
        ReleaseVerutilVersion(pFrameworkVersion);

        if (CSTR_EQUAL != ::CompareStringOrdinal(wzTargetName, -1, pFrameworkInfo->name, -1, TRUE))
        {
            continue;
        }

        hr = VerParseVersion(pFrameworkInfo->version, 0, FALSE, &pFrameworkVersion);
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to parse framework version: %ls", pFrameworkInfo->version);

        if (pFrameworkVersion->dwMajor != dwMajorVersion)
        {
            continue;
        }

        if (*ppVersion)
        {
            hr = VerCompareParsedVersions(*ppVersion, pFrameworkVersion, &nCompare);
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to compare versions.");

            if (nCompare > -1)
            {
                continue;
            }
        }

        ReleaseVerutilVersion(*ppVersion);
        *ppVersion = pFrameworkVersion;
        pFrameworkVersion = NULL;
    }

LExit:
    ReleaseVerutilVersion(pFrameworkVersion);

    return hr;
}

static HRESULT PerformSdkSearch(
    __in const hostfxr_dotnet_environment_info* pInfo,
    __in BOOL fFeatureBand,
    __in DWORD dwMajorVersion,
    __in DWORD dwMinorVersion,
    __in DWORD dwPatchVersion,
    __inout VERUTIL_VERSION** ppVersion
    )
{
    HRESULT hr = S_OK;
    VERUTIL_VERSION* pSdkVersion = NULL;
    int nCompare = 0;
    DWORD dwRequestedBand = dwPatchVersion / 100;

    for (size_t i = 0; i < pInfo->sdk_count; ++i)
    {
        const hostfxr_dotnet_environment_sdk_info* pSdkInfo = pInfo->sdks + i;
        ReleaseVerutilVersion(pSdkVersion);

        hr = VerParseVersion(pSdkInfo->version, 0, FALSE, &pSdkVersion);
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to parse sdk version: %ls", pSdkInfo->version);

        if (pSdkVersion->dwMajor != dwMajorVersion)
        {
            continue;
        }

        if (fFeatureBand)
        {
            if (pSdkVersion->dwMinor != dwMinorVersion)
            {
                continue;
            }

            if ((pSdkVersion->dwPatch / 100) != dwRequestedBand)
            {
                continue;
            }
        }

        if (*ppVersion)
        {
            hr = VerCompareParsedVersions(*ppVersion, pSdkVersion, &nCompare);
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to compare versions.");

            if (nCompare > -1)
            {
                continue;
            }
        }

        ReleaseVerutilVersion(*ppVersion);
        *ppVersion = pSdkVersion;
        pSdkVersion = NULL;
    }

LExit:
    ReleaseVerutilVersion(pSdkVersion);

    return hr;
}

static void HOSTFXR_CALLTYPE GetDotnetEnvironmentInfoResult(
    __in const hostfxr_dotnet_environment_info* pInfo,
    __in LPVOID pvContext
    )
{
    NETCORESEARCH_STATE* pState = static_cast<NETCORESEARCH_STATE*>(pvContext);
    HRESULT hr = S_OK;

    if (pState->type == NETCORESEARCHTYPE::Sdk)
    {
        hr = PerformSdkSearch(pInfo, FALSE, pState->Sdk.dwMajorVersion, 0, 0, &pState->pVersion);
    }
    else if (pState->type == NETCORESEARCHTYPE::SdkFeatureBand)
    {
        hr = PerformSdkSearch(pInfo, TRUE, pState->SdkFeatureBand.dwMajorVersion, pState->SdkFeatureBand.dwMinorVersion, pState->SdkFeatureBand.dwPatchVersion, &pState->pVersion);
    }
    else if (pState->type == NETCORESEARCHTYPE::Runtime)
    {
        hr = PerformRuntimeSearch(pInfo, pState->Runtime.dwMajorVersion, pState->Runtime.wzTargetName, &pState->pVersion);
    }
    else
    {
        hr = E_INVALIDARG;
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Invalid NETCORESEARCHTYPE.");
    }

LExit:
    pState->hrSearch = hr;
}
