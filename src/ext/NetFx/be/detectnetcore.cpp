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
    LPCWSTR wzPlatformName = NULL;
    LPWSTR sczExePath = NULL;
    LPWSTR sczCommandLine = NULL;
    HANDLE hProcess = NULL;
    HANDLE hStdOutErr = INVALID_HANDLE_VALUE;
    BYTE* rgbOutput = NULL;
    DWORD cbOutput = 0;
    DWORD cbTotalRead = 0;
    DWORD cbRead = 0;
    DWORD dwExitCode = 0;

    ReleaseNullStr(*psczLatestVersion);

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

    switch (platform)
    {
    case NETFX_NET_CORE_PLATFORM_ARM64:
        wzPlatformName = L"arm64";
        break;
    case NETFX_NET_CORE_PLATFORM_X64:
        wzPlatformName = L"x64";
        break;
    case NETFX_NET_CORE_PLATFORM_X86:
        wzPlatformName = L"x86";
        break;
    default:
        BextExitWithRootFailure(hr, E_INVALIDARG, "Unknown platform: %u", platform);
        break;
    }

    hr = StrAllocFormatted(&sczExePath, L"%ls%ls\\netcoresearch.exe", wzBaseDirectory, wzPlatformName);
    BextExitOnFailure(hr, "Failed to build netcoresearch.exe path.");

    hr = StrAllocFormatted(&sczCommandLine, L"\"%ls\" %ls %ls", sczExePath, wzMajorVersion, wzRuntimeType);
    BextExitOnFailure(hr, "Failed to build netcoresearch.exe command line.");

    hr = ProcExecute(sczExePath, sczCommandLine, &hProcess, NULL, &hStdOutErr);
    if (HRESULT_FROM_WIN32(ERROR_EXE_MACHINE_TYPE_MISMATCH) == hr)
    {
        ExitFunction1(hr = S_FALSE);
    }
    BextExitOnFailure(hr, "Failed to run: %ls", sczCommandLine);

    cbOutput = 64;

    rgbOutput = reinterpret_cast<BYTE*>(MemAlloc(cbOutput, TRUE));
    BextExitOnNull(rgbOutput, hr, E_OUTOFMEMORY, "Failed to alloc output string.");

    while (::ReadFile(hStdOutErr, rgbOutput + cbTotalRead, cbOutput - cbTotalRead, &cbRead, NULL))
    {
        cbTotalRead += cbRead;

        if (cbTotalRead == cbOutput)
        {
            cbOutput *= 2;

            LPVOID pvNew = MemReAlloc(rgbOutput, cbOutput, TRUE);
            BextExitOnNull(pvNew, hr, E_OUTOFMEMORY, "Failed to realloc output string.");

            rgbOutput = reinterpret_cast<BYTE*>(pvNew);
        }
    }

    if (ERROR_BROKEN_PIPE != ::GetLastError())
    {
        BextExitWithLastError(hr, "Failed to read netcoresearch.exe output.");
    }

    hr = ProcWaitForCompletion(hProcess, INFINITE, &dwExitCode);
    BextExitOnFailure(hr, "Failed to wait for netcoresearch.exe to exit.");

    if (0 != dwExitCode)
    {
        BextExitWithRootFailure(hr, E_UNEXPECTED, "netcoresearch.exe failed with exit code: 0x%x\r\nOutput:\r\n%hs", dwExitCode, rgbOutput);
    }

    if (*rgbOutput)
    {
        hr = StrAllocStringAnsi(psczLatestVersion, reinterpret_cast<LPSTR>(rgbOutput), 0, CP_UTF8);
        BextExitOnFailure(hr, "Failed to widen output string: %hs", rgbOutput);
    }

LExit:
    ReleaseFileHandle(hStdOutErr);
    ReleaseHandle(hProcess);
    ReleaseMem(rgbOutput);
    ReleaseStr(sczCommandLine);
    ReleaseStr(sczExePath);

    return hr;
}

HRESULT NetfxPerformDetectNetCore(
    __in LPCWSTR wzVariable,
    __in NETFX_SEARCH* pSearch,
    __in IBundleExtensionEngine* pEngine,
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
    return hr;
}
