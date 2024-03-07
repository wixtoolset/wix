// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT DetectNetCore(
    __in NETFX_NET_CORE_PLATFORM platform,
    __in NETFX_NET_CORE_RUNTIME_TYPE runtimeType,
    __in LPCWSTR wzMajorVersion,
    __in LPCWSTR wzBaseDirectory,
    __inout LPWSTR* psczLatestVersion
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzRuntimeType = NULL;
    LPWSTR sczArguments = NULL;

    switch (runtimeType)
    {
    case NETFX_NET_CORE_RUNTIME_TYPE_ASPNET:
        wzRuntimeType = L"Microsoft.AspNetCore.App";
        break;
    case NETFX_NET_CORE_RUNTIME_TYPE_CORE:
        wzRuntimeType = L"Microsoft.NETCore.App";
        break;
    case NETFX_NET_CORE_RUNTIME_TYPE_DESKTOP:
        wzRuntimeType = L"Microsoft.WindowsDesktop.App";
        break;
    default:
        BextExitWithRootFailure(hr, E_INVALIDARG, "Unknown runtime type: %u", runtimeType);
        break;
    }

    hr = StrAllocFormatted(&sczArguments, L"runtime %ls %ls", wzMajorVersion, wzRuntimeType);
    BextExitOnFailure(hr, "Failed to build runtime netcoresearch.exe arguments.");

    hr = RunNetCoreSearch(platform, wzBaseDirectory, sczArguments, psczLatestVersion);
    BextExitOnFailure(hr, "Failed to run netcoresearch.exe for runtime.");

LExit:
    ReleaseStr(sczArguments);

    return hr;
}

HRESULT NetfxPerformDetectNetCore(
    __in LPCWSTR wzVariable,
    __in NETFX_SEARCH* pSearch,
    __in IBootstrapperExtensionEngine* pEngine,
    __in LPCWSTR wzBaseDirectory
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczLatestVersion = FALSE;

    hr = DetectNetCore(pSearch->NetCoreSearch.platform, pSearch->NetCoreSearch.runtimeType, pSearch->NetCoreSearch.sczMajorVersion, wzBaseDirectory, &sczLatestVersion);
    BextExitOnFailure(hr, "DetectNetCore failed.");

    hr = pEngine->SetVariableVersion(wzVariable, sczLatestVersion);
    BextExitOnFailure(hr, "Failed to set variable '%ls' to '%ls'", wzVariable, sczLatestVersion);

LExit:
    ReleaseStr(sczLatestVersion);

    return hr;
}
