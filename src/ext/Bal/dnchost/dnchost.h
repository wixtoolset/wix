#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


enum DNCHOSTTYPE
{
    DNCHOSTTYPE_UNKNOWN,
    DNCHOSTTYPE_FDD,
    DNCHOSTTYPE_SCD,
};

extern "C" typedef HRESULT(WINAPI* PFN_DNCPREQ_BOOTSTRAPPER_APPLICATION_CREATE)(
    __in HRESULT hrHostInitialization,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    );

struct DNCSTATE
{
    BOOL fInitialized;
    BOOL fInitializedRuntime;
    HINSTANCE hInstance;
    LPWSTR sczModuleFullPath;
    LPWSTR sczAppBase;
    LPWSTR sczManagedHostPath;
    LPWSTR sczBaFactoryAssemblyName;
    LPWSTR sczBaFactoryAssemblyPath;
    LPWSTR sczBaFactoryDepsJsonPath;
    LPWSTR sczBaFactoryRuntimeConfigPath;
    DNCHOSTTYPE type;
    HOSTFXR_STATE hostfxrState;
    IBootstrapperApplicationFactory* pAppFactory;
    HMODULE hMbapreqModule;
};
