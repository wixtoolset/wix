// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT DetectNetCoreSdk(
    __in NETFX_NET_CORE_PLATFORM platform,
    __in LPCWSTR wzMajorVersion,
    __in LPCWSTR wzBaseDirectory,
    __inout LPWSTR* psczLatestVersion
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczArguments = NULL;

    hr = StrAllocFormatted(&sczArguments, L"sdk %ls", wzMajorVersion);
    BextExitOnFailure(hr, "Failed to build sdk netcoresearch.exe arguments.");

    hr = RunNetCoreSearch(platform, wzBaseDirectory, sczArguments, psczLatestVersion);
    BextExitOnFailure(hr, "Failed to run netcoresearch.exe for sdk.");

LExit:
    ReleaseStr(sczArguments);

    return hr;
}

HRESULT NetfxPerformDetectNetCoreSdk(
    __in LPCWSTR wzVariable,
    __in NETFX_SEARCH* pSearch,
    __in IBootstrapperExtensionEngine* pEngine,
    __in LPCWSTR wzBaseDirectory
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczLatestVersion = NULL;

    hr = DetectNetCoreSdk(pSearch->NetCoreSdkSearch.platform, pSearch->NetCoreSdkSearch.sczMajorVersion, wzBaseDirectory, &sczLatestVersion);
    BextExitOnFailure(hr, "DetectNetCoreSdk failed.");

    hr = pEngine->SetVariableVersion(wzVariable, sczLatestVersion);
    BextExitOnFailure(hr, "Failed to set variable '%ls' to '%ls'", wzVariable, sczLatestVersion);

LExit:
    ReleaseStr(sczLatestVersion);

    return hr;
}
