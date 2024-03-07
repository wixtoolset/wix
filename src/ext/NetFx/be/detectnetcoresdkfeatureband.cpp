// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT DetectNetCoreSdkFeatureBand(
    __in NETFX_NET_CORE_PLATFORM platform,
    __in LPCWSTR wzMajorVersion,
    __in LPCWSTR wzMinorVersion,
    __in LPCWSTR wzPatchVersion,
    __in LPCWSTR wzBaseDirectory,
    __inout LPWSTR* psczLatestVersion
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczArguments = NULL;

    hr = StrAllocFormatted(&sczArguments, L"sdkfeatureband %ls %ls %ls", wzMajorVersion, wzMinorVersion, wzPatchVersion);
    BextExitOnFailure(hr, "Failed to build sdkfeatureband netcoresearch.exe arguments.");

    hr = RunNetCoreSearch(platform, wzBaseDirectory, sczArguments, psczLatestVersion);
    BextExitOnFailure(hr, "Failed to run netcoresearch.exe for sdkfeatureband.");

LExit:
    ReleaseStr(sczArguments);

    return hr;
}

HRESULT NetfxPerformDetectNetCoreSdkFeatureBand(
    __in LPCWSTR wzVariable,
    __in NETFX_SEARCH* pSearch,
    __in IBootstrapperExtensionEngine* pEngine,
    __in LPCWSTR wzBaseDirectory
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczLatestVersion = NULL;

    hr = DetectNetCoreSdkFeatureBand(
        pSearch->NetCoreSdkFeatureBandSearch.platform,
        pSearch->NetCoreSdkFeatureBandSearch.sczMajorVersion,
        pSearch->NetCoreSdkFeatureBandSearch.sczMinorVersion,
        pSearch->NetCoreSdkFeatureBandSearch.sczPatchVersion,
        wzBaseDirectory,
        &sczLatestVersion);
    BextExitOnFailure(hr, "DetectNetCoreSdkFeatureBand failed.");

    hr = pEngine->SetVariableVersion(wzVariable, sczLatestVersion);
    BextExitOnFailure(hr, "Failed to set variable '%ls' to '%ls'", wzVariable, sczLatestVersion);

LExit:
    ReleaseStr(sczLatestVersion);

    return hr;
}
