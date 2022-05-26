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
    __in DNCSTATE* pState,
    __in IBootstrapperEngine* pEngine,
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
    IBootstrapperEngine* pEngine = NULL;

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

    if (vstate.prereqData.fAlwaysInstallPrereqs && !vstate.prereqData.fCompleted)
    {
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Loading prerequisite bootstrapper application since it's configured to always run before loading the runtime.");

        hr = CreatePrerequisiteBA(&vstate, pEngine, pArgs, pResults);
        BalExitOnFailure(hr, "Failed to create the pre-requisite bootstrapper application.");

        ExitFunction();
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
            vstate.prereqData.hrHostInitialization = E_DNCHOST_SCD_RUNTIME_FAILURE;
            BalLogError(hr, "The self-contained .NET Core runtime failed to load. This is an unrecoverable error.");
        }
        else if (vstate.prereqData.fCompleted)
        {
            hr = E_PREREQBA_INFINITE_LOOP;
            BalLogError(hr, "The prerequisites were already installed. The bootstrapper application will not be reloaded to prevent an infinite loop.");
            vstate.prereqData.hrHostInitialization = hr;
        }
        else
        {
            vstate.prereqData.hrHostInitialization = S_OK;
        }
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Loading prerequisite bootstrapper application because .NET Core host could not be loaded, error: 0x%08x.", hr);

        hr = CreatePrerequisiteBA(&vstate, pEngine, pArgs, pResults);
        BalExitOnFailure(hr, "Failed to create the pre-requisite bootstrapper application.");
    }

LExit:
    ReleaseNullObject(pEngine);

    return hr;
}

extern "C" void WINAPI BootstrapperApplicationDestroy(
    __in const BOOTSTRAPPER_DESTROY_ARGS* pArgs,
    __in BOOTSTRAPPER_DESTROY_RESULTS* pResults
    )
{
    BOOTSTRAPPER_DESTROY_RESULTS childResults = { };

    childResults.cbSize = sizeof(BOOTSTRAPPER_DESTROY_RESULTS);

    if (vstate.hMbapreqModule)
    {
        PFN_BOOTSTRAPPER_APPLICATION_DESTROY pfnDestroy = reinterpret_cast<PFN_BOOTSTRAPPER_APPLICATION_DESTROY>(::GetProcAddress(vstate.hMbapreqModule, "PrereqBootstrapperApplicationDestroy"));
        if (pfnDestroy)
        {
            (*pfnDestroy)(pArgs, &childResults);
        }

        ::FreeLibrary(vstate.hMbapreqModule);
        vstate.hMbapreqModule = NULL;
    }

    BalUninitialize();

    // Need to keep track of state between reloads.
    pResults->fDisableUnloading = TRUE;
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
    LPWSTR sczPayloadName = NULL;
    DWORD dwBool = 0;
    BOOL fXmlFound = FALSE;

    hr = XmlLoadDocumentFromFile(pArgs->pCommand->wzBootstrapperApplicationDataPath, &pixdManifest);
    BalExitOnFailure(hr, "Failed to load BalManifest '%ls'", pArgs->pCommand->wzBootstrapperApplicationDataPath);

    hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixBalBAFactoryAssembly", &pixnHost);
    BalExitOnRequiredXmlQueryFailure(hr, "Failed to get WixBalBAFactoryAssembly element.");

    hr = XmlGetAttributeEx(pixnHost, L"FilePath", &sczPayloadName);
    BalExitOnRequiredXmlQueryFailure(hr, "Failed to get WixBalBAFactoryAssembly/@FilePath.");

    hr = PathConcatRelativeToBase(pArgs->pCommand->wzBootstrapperWorkingFolder, sczPayloadName, &pState->sczBaFactoryAssemblyPath);
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

    hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixMbaPrereqOptions", &pixnHost);
    BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to find WixMbaPrereqOptions element in bootstrapper application config.");

    if (fXmlFound)
    {
        hr = XmlGetAttributeNumber(pixnHost, L"AlwaysInstallPrereqs", reinterpret_cast<DWORD*>(&pState->prereqData.fAlwaysInstallPrereqs));
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get AlwaysInstallPrereqs value.");
    }

    pState->type = DNCHOSTTYPE_FDD;

    hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixDncOptions", &pixnHost);
    BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to find WixDncOptions element in bootstrapper application config.");

    if (!fXmlFound)
    {
        ExitFunction();
    }

    hr = XmlGetAttributeNumber(pixnHost, L"SelfContainedDeployment", &dwBool);
    BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get SelfContainedDeployment value.");

    if (fXmlFound && dwBool)
    {
        pState->type = DNCHOSTTYPE_SCD;
    }

LExit:
    ReleaseStr(sczPayloadName);
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
        pState->sczBaFactoryAssemblyPath,
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
    __in DNCSTATE* pState,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDncpreqPath = NULL;
    HMODULE hModule = NULL;

    hr = PathConcat(pState->sczAppBase, L"dncpreq.dll", &sczDncpreqPath);
    BalExitOnFailure(hr, "Failed to get path to pre-requisite BA.");

    hModule = ::LoadLibraryExW(sczDncpreqPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    BalExitOnNullWithLastError(hModule, hr, "Failed to load pre-requisite BA DLL.");

    PFN_PREQ_BOOTSTRAPPER_APPLICATION_CREATE pfnCreate = reinterpret_cast<PFN_PREQ_BOOTSTRAPPER_APPLICATION_CREATE>(::GetProcAddress(hModule, "PrereqBootstrapperApplicationCreate"));
    BalExitOnNullWithLastError(pfnCreate, hr, "Failed to get PrereqBootstrapperApplicationCreate entry-point from: %ls", sczDncpreqPath);

    hr = pfnCreate(&pState->prereqData, pEngine, pArgs, pResults);
    BalExitOnFailure(hr, "Failed to create prequisite bootstrapper app.");

    pState->hMbapreqModule = hModule;
    hModule = NULL;

LExit:
    if (hModule)
    {
        ::FreeLibrary(hModule);
    }
    ReleaseStr(sczDncpreqPath);

    return hr;
}
