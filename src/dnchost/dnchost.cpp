// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static DNCSTATE vstate = { };


// internal function declarations

static HRESULT LoadModulePaths(
    __in DNCSTATE* pState
    );
static HRESULT LoadDncConfiguration(
    __in DNCSTATE* pState,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs
    );
static HRESULT LoadRuntime(
    __in DNCSTATE* pState
    );
static HRESULT LoadManagedBootstrapperApplicationFactory(
    __in DNCSTATE* pState
    );
static HRESULT CreatePrerequisiteBA(
    __in HRESULT hrHostInitialization,
    __in IBootstrapperEngine* pEngine,
    __in LPCWSTR wzAppBase,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    );


// function definitions

extern "C" BOOL WINAPI DllMain(
    IN HINSTANCE hInstance,
    IN DWORD dwReason,
    IN LPVOID /* pvReserved */
    )
{
    switch (dwReason)
    {
    case DLL_PROCESS_ATTACH:
        ::DisableThreadLibraryCalls(hInstance);
        vstate.hInstance = hInstance;
        break;

    case DLL_PROCESS_DETACH:
        vstate.hInstance = NULL;
        break;
    }

    return TRUE;
}

extern "C" HRESULT WINAPI BootstrapperApplicationCreate(
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    HRESULT hrHostInitialization = S_OK;
    IBootstrapperEngine* pEngine = NULL;

    // coreclr.dll doesn't support unloading, so the rest of the .NET Core hosting stack doesn't support it either.
    // This means we also can't unload.
    pResults->fDisableUnloading = TRUE;

    hr = BalInitializeFromCreateArgs(pArgs, &pEngine);
    ExitOnFailure(hr, "Failed to initialize Bal.");

    if (!vstate.fInitialized)
    {
        hr = XmlInitialize();
        BalExitOnFailure(hr, "Failed to initialize XML.");

        hr = LoadModulePaths(&vstate);
        BalExitOnFailure(hr, "Failed to get the host base path.");

        hr = LoadDncConfiguration(&vstate, pArgs);
        BalExitOnFailure(hr, "Failed to get the dnc configuration.");

        vstate.fInitialized = TRUE;
    }

    if (!vstate.fInitializedRuntime)
    {
        hr = LoadRuntime(&vstate);

        vstate.fInitializedRuntime = SUCCEEDED(hr);
    }

    if (vstate.fInitializedRuntime)
    {
        if (!vstate.pAppFactory)
        {
            hr = LoadManagedBootstrapperApplicationFactory(&vstate);
            BalExitOnFailure(hr, "Failed to create the .NET Core bootstrapper application factory.");
        }

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Loading .NET Core %ls bootstrapper application.", DNCHOSTTYPE_FDD == vstate.type ? L"FDD" : L"SCD");

        hr = vstate.pAppFactory->Create(pArgs, pResults);
        BalExitOnFailure(hr, "Failed to create the .NET Core bootstrapper application.");
    }
    else // fallback to the prerequisite BA.
    {
        if (DNCHOSTTYPE_SCD == vstate.type)
        {
            hrHostInitialization = E_DNCHOST_SCD_RUNTIME_FAILURE;
            BalLogError(hr, "The self-contained .NET Core runtime failed to load. This is an unrecoverable error.");
        }
        else
        {
            hrHostInitialization = S_OK;
        }
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Loading prerequisite bootstrapper application because .NET Core host could not be loaded, error: 0x%08x.", hr);

        hr = CreatePrerequisiteBA(hrHostInitialization, pEngine, vstate.sczAppBase, pArgs, pResults);
        BalExitOnFailure(hr, "Failed to create the pre-requisite bootstrapper application.");
    }

LExit:
    ReleaseNullObject(pEngine);

    return hr;
}

extern "C" void WINAPI BootstrapperApplicationDestroy()
{
    if (vstate.hMbapreqModule)
    {
        PFN_BOOTSTRAPPER_APPLICATION_DESTROY pfnDestroy = reinterpret_cast<PFN_BOOTSTRAPPER_APPLICATION_DESTROY>(::GetProcAddress(vstate.hMbapreqModule, "DncPrereqBootstrapperApplicationDestroy"));
        if (pfnDestroy)
        {
            (*pfnDestroy)();
        }

        ::FreeLibrary(vstate.hMbapreqModule);
        vstate.hMbapreqModule = NULL;
    }

    BalUninitialize();
}

static HRESULT LoadModulePaths(
    __in DNCSTATE* pState
    )
{
    HRESULT hr = S_OK;

    hr = PathForCurrentProcess(&pState->sczModuleFullPath, pState->hInstance);
    BalExitOnFailure(hr, "Failed to get the full host path.");

    hr = PathGetDirectory(pState->sczModuleFullPath, &pState->sczAppBase);
    BalExitOnFailure(hr, "Failed to get the directory of the full process path.");

    hr = PathConcat(pState->sczAppBase, DNC_ASSEMBLY_FILE_NAME, &pState->sczManagedHostPath);
    BalExitOnFailure(hr, "Failed to create managed host path.");

LExit:
    return hr;
}

static HRESULT LoadDncConfiguration(
    __in DNCSTATE* pState,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs
    )
{
    HRESULT hr = S_OK;
    IXMLDOMDocument* pixdManifest = NULL;
    IXMLDOMNode* pixnHost = NULL;
    IXMLDOMNode* pixnPayload = NULL;
    LPWSTR sczPayloadId = NULL;
    LPWSTR sczPayloadXPath = NULL;
    LPWSTR sczPayloadName = NULL;
    DWORD dwBool = 0;

    hr = XmlLoadDocumentFromFile(pArgs->pCommand->wzBootstrapperApplicationDataPath, &pixdManifest);
    BalExitOnFailure(hr, "Failed to load BalManifest '%ls'", pArgs->pCommand->wzBootstrapperApplicationDataPath);

    hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixBalBAFactoryAssembly", &pixnHost);
    BalExitOnFailure(hr, "Failed to get WixBalBAFactoryAssembly element.");

    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
        BalExitOnRootFailure(hr, "Failed to find WixBalBAFactoryAssembly element in bootstrapper application config.");
    }

    hr = XmlGetAttributeEx(pixnHost, L"PayloadId", &sczPayloadId);
    BalExitOnFailure(hr, "Failed to get WixBalBAFactoryAssembly/@PayloadId.");

    hr = StrAllocFormatted(&sczPayloadXPath, L"/BootstrapperApplicationData/WixPayloadProperties[@Payload='%ls']", sczPayloadId);
    BalExitOnFailure(hr, "Failed to format BAFactoryAssembly payload XPath.");

    hr = XmlSelectSingleNode(pixdManifest, sczPayloadXPath, &pixnPayload);
    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
    }
    BalExitOnFailure(hr, "Failed to find WixPayloadProperties node for BAFactoryAssembly PayloadId: %ls.", sczPayloadId);

    hr = XmlGetAttributeEx(pixnPayload, L"Name", &sczPayloadName);
    BalExitOnFailure(hr, "Failed to get BAFactoryAssembly payload Name.");

    hr = PathConcat(pArgs->pCommand->wzBootstrapperWorkingFolder, sczPayloadName, &pState->sczBaFactoryAssemblyPath);
    BalExitOnFailure(hr, "Failed to create BaFactoryAssemblyPath.");

    LPCWSTR wzFileName = PathFile(pState->sczBaFactoryAssemblyPath);
    LPCWSTR wzExtension = PathExtension(pState->sczBaFactoryAssemblyPath);
    if (!wzExtension)
    {
        BalExitOnFailure(hr = E_FAIL, "BaFactoryAssemblyPath has no extension.");
    }

    hr = StrAllocString(&pState->sczBaFactoryAssemblyName, wzFileName, wzExtension - wzFileName);
    BalExitOnFailure(hr, "Failed to copy BAFactoryAssembly payload Name.");

    hr = StrAllocString(&pState->sczBaFactoryDepsJsonPath, pState->sczBaFactoryAssemblyPath, wzExtension - pState->sczBaFactoryAssemblyPath);
    BalExitOnFailure(hr, "Failed to initialize deps json path.");

    hr = StrAllocString(&pState->sczBaFactoryRuntimeConfigPath, pState->sczBaFactoryDepsJsonPath, 0);
    BalExitOnFailure(hr, "Failed to initialize runtime config path.");

    hr = StrAllocConcat(&pState->sczBaFactoryDepsJsonPath, L".deps.json", 0);
    BalExitOnFailure(hr, "Failed to concat extension to deps json path.");

    hr = StrAllocConcat(&pState->sczBaFactoryRuntimeConfigPath, L".runtimeconfig.json", 0);
    BalExitOnFailure(hr, "Failed to concat extension to runtime config path.");

    pState->type = DNCHOSTTYPE_FDD;

    hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixDncOptions", &pixnHost);
    if (S_FALSE == hr)
    {
        ExitFunction1(hr = S_OK);
    }
    BalExitOnFailure(hr, "Failed to find WixDncOptions element in bootstrapper application config.");

    hr = XmlGetAttributeNumber(pixnHost, L"SelfContainedDeployment", &dwBool);
    if (S_FALSE == hr)
    {
        hr = S_OK;
    }
    else if (SUCCEEDED(hr) && dwBool)
    {
        pState->type = DNCHOSTTYPE_SCD;
    }
    BalExitOnFailure(hr, "Failed to get SelfContainedDeployment value.");

LExit:
    ReleaseStr(sczPayloadName);
    ReleaseObject(pixnPayload);
    ReleaseStr(sczPayloadXPath);
    ReleaseStr(sczPayloadId);
    ReleaseObject(pixnHost);
    ReleaseObject(pixdManifest);

    return hr;
}

static HRESULT LoadRuntime(
    __in DNCSTATE* pState
    )
{
    HRESULT hr = S_OK;

    hr = DnchostLoadRuntime(
        &pState->hostfxrState,
        pState->sczModuleFullPath,
        pState->sczManagedHostPath,
        pState->sczBaFactoryDepsJsonPath,
        pState->sczBaFactoryRuntimeConfigPath);

    return hr;
}

static HRESULT LoadManagedBootstrapperApplicationFactory(
    __in DNCSTATE* pState
    )
{
    HRESULT hr = S_OK;

    hr = DnchostCreateFactory(
        &pState->hostfxrState,
        pState->sczBaFactoryAssemblyName,
        pState->sczBaFactoryAssemblyPath,
        &pState->pAppFactory);

    return hr;
}

static HRESULT CreatePrerequisiteBA(
    __in HRESULT hrHostInitialization,
    __in IBootstrapperEngine* pEngine,
    __in LPCWSTR wzAppBase,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDncpreqPath = NULL;
    HMODULE hModule = NULL;

    hr = PathConcat(wzAppBase, L"dncpreq.dll", &sczDncpreqPath);
    BalExitOnFailure(hr, "Failed to get path to pre-requisite BA.");

    hModule = ::LoadLibraryW(sczDncpreqPath);
    BalExitOnNullWithLastError(hModule, hr, "Failed to load pre-requisite BA DLL.");

    PFN_DNCPREQ_BOOTSTRAPPER_APPLICATION_CREATE pfnCreate = reinterpret_cast<PFN_DNCPREQ_BOOTSTRAPPER_APPLICATION_CREATE>(::GetProcAddress(hModule, "DncPrereqBootstrapperApplicationCreate"));
    BalExitOnNullWithLastError(pfnCreate, hr, "Failed to get DncPrereqBootstrapperApplicationCreate entry-point from: %ls", sczDncpreqPath);

    hr = pfnCreate(hrHostInitialization, pEngine, pArgs, pResults);
    BalExitOnFailure(hr, "Failed to create prequisite bootstrapper app.");

    vstate.hMbapreqModule = hModule;
    hModule = NULL;

LExit:
    if (hModule)
    {
        ::FreeLibrary(hModule);
    }
    ReleaseStr(sczDncpreqPath);

    return hr;
}
