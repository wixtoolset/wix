// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

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
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath
    );
static HRESULT InitializeCoreClrPre5(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath
    );
static HRESULT LoadCoreClr(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzCoreClrPath
    );
static HRESULT StartCoreClr(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath,
    __in size_t cProperties,
    __in LPCWSTR* propertyKeys,
    __in LPCWSTR* propertyValues
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

    hr = InitializeCoreClr(pState, wzNativeHostPath);
    BalExitOnFailure(hr, "Failed to initialize coreclr.");

LExit:
    return hr;
}

HRESULT DnchostCreateFactory(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzBaFactoryAssemblyName,
    __in LPCWSTR wzBaFactoryAssemblyPath,
    __out IBootstrapperApplicationFactory** ppAppFactory
    )
{
    HRESULT hr = S_OK;
    PFNCREATEBAFACTORY pfnCreateBAFactory = NULL;

    if (pState->pfnGetFunctionPointer)
    {
        hr = pState->pfnGetFunctionPointer(
            DNC_ENTRY_TYPEW,
            DNC_STATIC_ENTRY_METHODW,
            DNC_STATIC_ENTRY_DELEGATEW,
            NULL,
            NULL,
            reinterpret_cast<void**>(&pfnCreateBAFactory));
        BalExitOnFailure(hr, "Failed to create delegate through GetFunctionPointer.");
    }
    else
    {
        hr = pState->pfnCoreclrCreateDelegate(
            pState->pClrHandle,
            pState->dwDomainId,
            DNC_ASSEMBLY_FULL_NAME,
            DNC_ENTRY_TYPE,
            DNC_STATIC_ENTRY_METHOD,
            reinterpret_cast<void**>(&pfnCreateBAFactory));
        BalExitOnFailure(hr, "Failed to create delegate in app domain.");
    }

    *ppAppFactory = pfnCreateBAFactory(wzBaFactoryAssemblyName, wzBaFactoryAssemblyPath);

LExit:
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

    pState->pfnHostfxrGetRuntimeProperties = reinterpret_cast<hostfxr_get_runtime_properties_fn>(::GetProcAddress(hHostfxr, "hostfxr_get_runtime_properties"));
    BalExitOnNullWithLastError(pState->pfnHostfxrGetRuntimeProperties, hr, "Failed to get procedure address for hostfxr_get_runtime_properties.");

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
    BOOTSTRAPPER_LOG_LEVEL level = BOOTSTRAPPER_LOG_LEVEL_ERROR;

    if (CSTR_EQUAL == ::CompareString(LOCALE_INVARIANT, 0, wzMessage, -1, L"The requested delegate type is not available in the target framework.", -1))
    {
        level = BOOTSTRAPPER_LOG_LEVEL_DEBUG;
    }

    BalLog(level, "error from hostfxr: %ls", wzMessage);
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
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath
    )
{
    HRESULT hr = S_OK;

    hr = pState->pfnHostfxrGetRuntimeDelegate(pState->hostContextHandle, hdt_get_function_pointer, reinterpret_cast<void**>(&pState->pfnGetFunctionPointer));
    if (InvalidArgFailure == hr || // old versions of hostfxr don't allow calling GetRuntimeDelegate from InitializeForApp.
        HostApiUnsupportedVersion == hr) // hdt_get_function_pointer was added in .NET 5.
    {
        hr = InitializeCoreClrPre5(pState, wzNativeHostPath);
    }
    else
    {
        ExitOnFailure(hr, "HostfxrGetRuntimeDelegate failed");
    }

LExit:
    return hr;
}

static HRESULT InitializeCoreClrPre5(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath
    )
{
    HRESULT hr = S_OK;
    int32_t rc = 0;
    LPCWSTR* rgPropertyKeys = NULL;
    LPCWSTR* rgPropertyValues = NULL;
    size_t cProperties = 0;
    LPWSTR* rgDirectories = NULL;
    UINT cDirectories = 0;
    LPWSTR sczCoreClrPath = NULL;

    // We are not using hostfxr as it was intended to be used. We need to initialize hostfxr so that it properly initializes hostpolicy -
    // there are pieces of the framework such as AssemblyDependencyResolver that won't work without that. We also need hostfxr to find a
    // compatible framework for framework-dependent deployed BAs. We had to use hostfxr_initialize_for_dotnet_command_line since
    // hostfxr_initialize_for_runtime_config doesn't currently (3.x) support self-contained deployed BAs. That means we're supposed to
    // start the runtime through hostfxr_run_app, but that method shuts down the runtime before returning. We actually want to call
    // hostfxr_get_runtime_delegate, but that method currently requires hostfxr to be initialized through
    // hostfxr_initialize_for_runtime_config. So we're forced to locate coreclr.dll and manually load the runtime ourselves.

    // Unfortunately, that's not the only problem. hostfxr has global state that tracks whether it started the runtime. While we keep our
    // hostfxr_handle open, everyone that calls the hostfxr_initialize_* methods will block until we have started the runtime through
    // hostfxr or closed our handle. If we close the handle, then hostfxr could potentially try to load a second runtime into the
    // process, which is not supported. We're going to just keep our handle open since no one else in the process should be trying to
    // start the runtime anyway.

    rc = pState->pfnHostfxrGetRuntimeProperties(pState->hostContextHandle, &cProperties, rgPropertyKeys, rgPropertyValues);
    if (HostApiBufferTooSmall != rc)
    {
        BalExitOnFailure(hr = rc, "HostfxrGetRuntimeProperties failed to return required size.");
    }

    rgPropertyKeys = static_cast<LPCWSTR*>(MemAlloc(sizeof(LPWSTR) * cProperties, TRUE));
    rgPropertyValues = static_cast<LPCWSTR*>(MemAlloc(sizeof(LPWSTR) * cProperties, TRUE));
    if (!rgPropertyKeys || !rgPropertyValues)
    {
        BalExitOnFailure(hr = E_OUTOFMEMORY, "Failed to allocate buffers for runtime properties.");
    }

    hr = pState->pfnHostfxrGetRuntimeProperties(pState->hostContextHandle, &cProperties, rgPropertyKeys, rgPropertyValues);
    BalExitOnFailure(hr, "HostfxrGetRuntimeProperties failed.");

    for (DWORD i = 0; i < cProperties; ++i)
    {
        if (CSTR_EQUAL == ::CompareString(LOCALE_INVARIANT, 0, rgPropertyKeys[i], -1, L"NATIVE_DLL_SEARCH_DIRECTORIES", -1))
        {
            hr = StrSplitAllocArray(&rgDirectories, &cDirectories, rgPropertyValues[i], L";");
            BalExitOnFailure(hr, "Failed to split NATIVE_DLL_SEARCH_DIRECTORIES '%ls'", rgPropertyValues[i]);
        }
    }

    for (DWORD i = 0; i < cDirectories; ++i)
    {
        hr = PathConcat(rgDirectories[i], L"coreclr.dll", &sczCoreClrPath);
        BalExitOnFailure(hr, "Failed to allocate path to coreclr.");

        if (::PathFileExists(sczCoreClrPath))
        {
            break;
        }
        else
        {
            ReleaseNullStr(sczCoreClrPath);
        }
    }

    if (!sczCoreClrPath)
    {
        for (DWORD i = 0; i < cProperties; ++i)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_ERROR, "%ls: %ls", rgPropertyKeys[i], rgPropertyValues[i]);
        }
        BalExitOnFailure(hr = E_FILENOTFOUND, "Failed to locate coreclr.dll.");
    }

    hr = LoadCoreClr(pState, sczCoreClrPath);
    BalExitOnFailure(hr, "Failed to load coreclr.");

    hr = StartCoreClr(pState, wzNativeHostPath, cProperties, rgPropertyKeys, rgPropertyValues);
    BalExitOnFailure(hr, "Failed to start coreclr.");

LExit:
    MemFree(rgDirectories);
    MemFree(rgPropertyValues);
    MemFree(rgPropertyKeys);
    ReleaseStr(sczCoreClrPath);

    return hr;
}

static HRESULT LoadCoreClr(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzCoreClrPath
    )
{
    HRESULT hr = S_OK;
    HMODULE hModule = NULL;

    hModule = ::LoadLibraryExW(wzCoreClrPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    BalExitOnNullWithLastError(hModule, hr, "Failed to load coreclr.dll from '%ls'.", wzCoreClrPath);

    pState->pfnCoreclrInitialize = reinterpret_cast<coreclr_initialize_ptr>(::GetProcAddress(hModule, "coreclr_initialize"));
    BalExitOnNullWithLastError(pState->pfnCoreclrInitialize, hr, "Failed to get procedure address for coreclr_initialize.");

    pState->pfnCoreclrCreateDelegate = reinterpret_cast<coreclr_create_delegate_ptr>(::GetProcAddress(hModule, "coreclr_create_delegate"));
    BalExitOnNullWithLastError(pState->pfnCoreclrCreateDelegate, hr, "Failed to get procedure address for coreclr_create_delegate.");

LExit:
    // Never unload the module since coreclr doesn't support it.

    return hr;
}

static HRESULT StartCoreClr(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath,
    __in size_t cProperties,
    __in LPCWSTR* propertyKeys,
    __in LPCWSTR* propertyValues
    )
{
    HRESULT hr = S_OK;
    LPSTR szNativeHostPath = NULL;
    LPSTR* rgPropertyKeys = NULL;
    LPSTR* rgPropertyValues = NULL;
    
    rgPropertyKeys = static_cast<LPSTR*>(MemAlloc(sizeof(LPSTR) * cProperties, TRUE));
    rgPropertyValues = static_cast<LPSTR*>(MemAlloc(sizeof(LPSTR) * cProperties, TRUE));
    if (!rgPropertyKeys || !rgPropertyValues)
    {
        BalExitOnFailure(hr = E_OUTOFMEMORY, "Failed to allocate buffers for runtime properties.");
    }

    hr = StrAnsiAllocString(&szNativeHostPath, wzNativeHostPath, 0, CP_UTF8);
    BalExitOnFailure(hr, "Failed to convert module path to UTF8: %ls", wzNativeHostPath);

    for (DWORD i = 0; i < cProperties; ++i)
    {
        hr = StrAnsiAllocString(&rgPropertyKeys[i], propertyKeys[i], 0, CP_UTF8);
        BalExitOnFailure(hr, "Failed to convert property key to UTF8: %ls", propertyKeys[i]);

        hr = StrAnsiAllocString(&rgPropertyValues[i], propertyValues[i], 0, CP_UTF8);
        BalExitOnFailure(hr, "Failed to convert property value to UTF8: %ls", propertyValues[i]);
    }

    hr = pState->pfnCoreclrInitialize(szNativeHostPath, "MBA", cProperties, (LPCSTR*)rgPropertyKeys, (LPCSTR*)rgPropertyValues, &pState->pClrHandle, &pState->dwDomainId);
    BalExitOnFailure(hr, "CoreclrInitialize failed.");

LExit:
    for (DWORD i = 0; i < cProperties; ++i)
    {
        if (rgPropertyKeys)
        {
            ReleaseStr(rgPropertyKeys[i]);
        }

        if (rgPropertyValues)
        {
            ReleaseStr(rgPropertyValues[i]);
        }
    }
    ReleaseMem(rgPropertyValues);
    ReleaseMem(rgPropertyKeys);
    ReleaseStr(szNativeHostPath);

    return hr;
}
