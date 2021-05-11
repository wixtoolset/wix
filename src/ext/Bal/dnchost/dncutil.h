#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

typedef IBootstrapperApplicationFactory* (STDMETHODCALLTYPE* PFNCREATEBAFACTORY)(
    __in LPCWSTR wzBaFactoryAssemblyName,
    __in LPCWSTR wzBaFactoryAssemblyPath
    );

struct HOSTFXR_STATE
{
    LPWSTR sczHostfxrPath;
    hostfxr_handle hostContextHandle;
    hostfxr_initialize_for_dotnet_command_line_fn pfnHostfxrInitializeForApp;
    hostfxr_get_runtime_properties_fn pfnHostfxrGetRuntimeProperties;
    hostfxr_set_error_writer_fn pfnHostfxrSetErrorWriter;
    hostfxr_close_fn pfnHostfxrClose;
    hostfxr_get_runtime_delegate_fn pfnHostfxrGetRuntimeDelegate;
    get_function_pointer_fn pfnGetFunctionPointer;
    coreclr_initialize_ptr pfnCoreclrInitialize;
    coreclr_create_delegate_ptr pfnCoreclrCreateDelegate;
    void* pClrHandle;
    UINT dwDomainId;
};

HRESULT DnchostLoadRuntime(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzNativeHostPath,
    __in LPCWSTR wzManagedHostPath,
    __in LPCWSTR wzDepsJsonPath,
    __in LPCWSTR wzRuntimeConfigPath
    );

HRESULT DnchostCreateFactory(
    __in HOSTFXR_STATE* pState,
    __in LPCWSTR wzBaFactoryAssemblyName,
    __in LPCWSTR wzBaFactoryAssemblyPath,
    __out IBootstrapperApplicationFactory** ppAppFactory
    );
