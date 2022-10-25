// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

#define DNC_ENTRY_TYPEW L"WixToolset.Dnc.Host.BootstrapperApplicationFactory"
#define DNC_STATIC_ENTRY_METHODW L"CreateBAFactory"
#define DNC_STATIC_ENTRY_DELEGATEW L"WixToolset.Dnc.Host.StaticEntryDelegate"

// https://github.com/dotnet/runtime/blob/master/src/installer/corehost/error_codes.h
#define InvalidArgFailure 0x80008081
#define HostApiBufferTooSmall 0x80008098
#define HostApiUnsupportedVersion 0x800080a2

// internal function declarations

static HRESULT GetHostfxrPath(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath
    );
static HRESULT LoadHostfxr(
    __in HOSTFXR_STATE* pState
    );
static HRESULT InitializeHostfxr(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzManagedHostPath,
    __in LPCWSTR wzDepsJsonPath,
    __in LPCWSTR wzRuntimeConfigPath
    );
static HRESULT InitializeCoreClr(
    __in HOSTFXR_STATE* pState
    );


// function definitions

HRESULT DnchostLoadRuntime(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath,
    __in LPCWSTR wzManagedHostPath,
    __in LPCWSTR wzDepsJsonPath,
    __in LPCWSTR wzRuntimeConfigPath
    )
{
    HRESULT hr = S_OK;

    hr = GetHostfxrPath(pState, wzNativeHostPath);
    BalExitOnFailure(hr, "Failed to find hostfxr.");

    hr = LoadHostfxr(pState);
    BalExitOnFailure(hr, "Failed to load hostfxr.");

    hr = InitializeHostfxr(pState, wzManagedHostPath, wzDepsJsonPath, wzRuntimeConfigPath);
    BalExitOnFailure(hr, "Failed to initialize hostfxr.");

    hr = InitializeCoreClr(pState);
    BalExitOnFailure(hr, "Failed to initialize coreclr.");

LExit:
    return hr;
}

HRESULT DnchostCreateFactory(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzBaFactoryAssemblyName,
    __out IBootstrapperApplicationFactory** ppAppFactory
    )
{
    HRESULT hr = S_OK;
    PFNCREATEBAFACTORY pfnCreateBAFactory = NULL;
    LPWSTR sczEntryType = NULL;
    LPWSTR sczEntryDelegate = NULL;
    LPSTR sczBaFactoryAssemblyName = NULL;

    hr = StrAllocFormatted(&sczEntryType, L"%ls,%ls", DNC_ENTRY_TYPEW, wzBaFactoryAssemblyName);
    BalExitOnFailure(hr, "Failed to format entry type.");

    hr = StrAllocFormatted(&sczEntryDelegate, L"%ls,%ls", DNC_STATIC_ENTRY_DELEGATEW, wzBaFactoryAssemblyName);
    BalExitOnFailure(hr, "Failed to format entry delegate.");

    hr = pState->pfnGetFunctionPointer(
        sczEntryType,
        DNC_STATIC_ENTRY_METHODW,
        sczEntryDelegate,
        NULL,
        NULL,
        reinterpret_cast<void**>(&pfnCreateBAFactory));
    BalExitOnFailure(hr, "Failed to create delegate through GetFunctionPointer.");

    *ppAppFactory = pfnCreateBAFactory();

LExit:
    ReleaseStr(sczEntryType);
    ReleaseStr(sczEntryDelegate);
    ReleaseStr(sczBaFactoryAssemblyName);

    return hr;
}

static HRESULT GetHostfxrPath(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath
    )
{
    HRESULT hr = S_OK;
    get_hostfxr_parameters getHostfxrParameters = { };
    int nrc = 0;
    size_t cchHostFxrPath = MAX_PATH;

    getHostfxrParameters.size = sizeof(get_hostfxr_parameters);
    getHostfxrParameters.assembly_path = wzNativeHostPath;

    // get_hostfxr_path does a full search on every call, so
    //    minimize the number of calls
    //    need to loop
    for (;;)
    {
        cchHostFxrPath *= 2;
        hr = StrAlloc(&pState->sczHostfxrPath, cchHostFxrPath);
        BalExitOnFailure(hr, "Failed to allocate hostFxrPath.");

        nrc = get_hostfxr_path(pState->sczHostfxrPath, &cchHostFxrPath, &getHostfxrParameters);
        if (HostApiBufferTooSmall != nrc)
        {
            break;
        }
    }
    if (0 != nrc)
    {
        BalExitOnFailure(hr = nrc, "GetHostfxrPath failed");
    }

LExit:
    return hr;
}

static HRESULT LoadHostfxr(
    __in HOSTFXR_STATE* pState
    )
{
    HRESULT hr = S_OK;
    HMODULE hHostfxr;

    hHostfxr = ::LoadLibraryExW(pState->sczHostfxrPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    BalExitOnNullWithLastError(hHostfxr, hr, "Failed to load hostfxr from '%ls'.", pState->sczHostfxrPath);

    pState->pfnHostfxrInitializeForApp = reinterpret_cast<hostfxr_initialize_for_dotnet_command_line_fn>(::GetProcAddress(hHostfxr, "hostfxr_initialize_for_dotnet_command_line"));
    BalExitOnNullWithLastError(pState->pfnHostfxrInitializeForApp, hr, "Failed to get procedure address for hostfxr_initialize_for_dotnet_command_line.");

    pState->pfnHostfxrSetErrorWriter = reinterpret_cast<hostfxr_set_error_writer_fn>(::GetProcAddress(hHostfxr, "hostfxr_set_error_writer"));
    BalExitOnNullWithLastError(pState->pfnHostfxrSetErrorWriter, hr, "Failed to get procedure address for hostfxr_set_error_writer.");

    pState->pfnHostfxrClose = reinterpret_cast<hostfxr_close_fn>(::GetProcAddress(hHostfxr, "hostfxr_close"));
    BalExitOnNullWithLastError(pState->pfnHostfxrClose, hr, "Failed to get procedure address for hostfxr_close.");

    pState->pfnHostfxrGetRuntimeDelegate = reinterpret_cast<hostfxr_get_runtime_delegate_fn>(::GetProcAddress(hHostfxr, "hostfxr_get_runtime_delegate"));
    BalExitOnNullWithLastError(pState->pfnHostfxrGetRuntimeDelegate, hr, "Failed to get procedure address for hostfxr_get_runtime_delegate.");

LExit:
    // Never unload the module since it isn't meant to be unloaded.

    return hr;
}

static void HOSTFXR_CALLTYPE DnchostErrorWriter(
    __in LPCWSTR wzMessage
    )
{
    BalLog(BOOTSTRAPPER_LOG_LEVEL_ERROR, "error from hostfxr: %ls", wzMessage);
}

static HRESULT InitializeHostfxr(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzManagedHostPath,
    __in LPCWSTR wzDepsJsonPath,
    __in LPCWSTR wzRuntimeConfigPath
    )
{
    HRESULT hr = S_OK;

    pState->pfnHostfxrSetErrorWriter(static_cast<hostfxr_error_writer_fn>(&DnchostErrorWriter));

    LPCWSTR argv[] = {
        L"exec",
        L"--depsfile",
        wzDepsJsonPath,
        L"--runtimeconfig",
        wzRuntimeConfigPath,
        wzManagedHostPath,
    };
    hr = pState->pfnHostfxrInitializeForApp(sizeof(argv)/sizeof(LPWSTR), argv, NULL, &pState->hostContextHandle);
    BalExitOnFailure(hr, "HostfxrInitializeForApp failed");

LExit:
    return hr;
}

static HRESULT InitializeCoreClr(
    __in HOSTFXR_STATE* pState
    )
{
    HRESULT hr = S_OK;

    hr = pState->pfnHostfxrGetRuntimeDelegate(pState->hostContextHandle, hdt_get_function_pointer, reinterpret_cast<void**>(&pState->pfnGetFunctionPointer));
    if (InvalidArgFailure == hr || // old versions of hostfxr don't allow calling GetRuntimeDelegate from InitializeForApp.
        HostApiUnsupportedVersion == hr) // hdt_get_function_pointer was added in .NET 5.
    {
        BalExitOnFailure(hr, "HostfxrGetRuntimeDelegate failed, most likely because the target framework is older than .NET 5.");
    }
    else
    {
        BalExitOnFailure(hr, "HostfxrGetRuntimeDelegate failed");
    }

LExit:
    return hr;
}
