// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static INTERNAL_UI_BA_STATE vstate = { };


// internal function declarations

static HRESULT LoadModulePaths(
    __in INTERNAL_UI_BA_STATE* pState
    );
static HRESULT LoadInternalUIBAConfiguration(
    __in INTERNAL_UI_BA_STATE* pState,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs
    );
static HRESULT CreatePrerequisiteBA(
    __in INTERNAL_UI_BA_STATE* pState,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    );


// function definitions

extern "C" BOOL WINAPI DllMain(
    __in HINSTANCE hInstance,
    __in DWORD dwReason,
    __in LPVOID /*pvReserved*/
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

// Note: This function assumes that COM was already initialized on the thread.
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
        BalExitOnFailure(hr, "Failed to load the module paths.");

        hr = LoadInternalUIBAConfiguration(&vstate, pArgs);
        BalExitOnFailure(hr, "Failed to get the InternalUIBA configuration.");

        vstate.fInitialized = TRUE;
    }

    if (vstate.prereqData.fAlwaysInstallPrereqs && !vstate.prereqData.fCompleted ||
        FAILED(vstate.prereqData.hrFatalError))
    {
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Loading prerequisite bootstrapper application.");

        hr = CreatePrerequisiteBA(&vstate, pEngine, pArgs, pResults);
        BalExitOnFailure(hr, "Failed to create the pre-requisite bootstrapper application.");
    }
    else
    {
        hr = CreateBootstrapperApplication(vstate.hInstance, &vstate.prereqData, pEngine, pArgs, pResults, &vstate.pApplication);
        BalExitOnFailure(hr, "Failed to create bootstrapper application interface.");
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

    if (vstate.hPrereqModule)
    {
        PFN_BOOTSTRAPPER_APPLICATION_DESTROY pfnDestroy = reinterpret_cast<PFN_BOOTSTRAPPER_APPLICATION_DESTROY>(::GetProcAddress(vstate.hPrereqModule, "PrereqBootstrapperApplicationDestroy"));
        if (pfnDestroy)
        {
            (*pfnDestroy)(pArgs, &childResults);
        }

        ::FreeLibrary(vstate.hPrereqModule);
        vstate.hPrereqModule = NULL;
    }

    if (vstate.pApplication)
    {
        DestroyBootstrapperApplication(vstate.pApplication, pArgs, pResults);
        ReleaseNullObject(vstate.pApplication);
    }

    BalUninitialize();

    // Need to keep track of state between reloads.
    pResults->fDisableUnloading = TRUE;
}

static HRESULT LoadModulePaths(
    __in INTERNAL_UI_BA_STATE* pState
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFullPath = NULL;

    hr = PathForCurrentProcess(&sczFullPath, pState->hInstance);
    ExitOnFailure(hr, "Failed to get the full host path.");

    hr = PathGetDirectory(sczFullPath, &pState->sczAppBase);
    ExitOnFailure(hr, "Failed to get the directory of the full process path.");

LExit:
    ReleaseStr(sczFullPath);

    return hr;
}

static HRESULT LoadInternalUIBAConfiguration(
    __in INTERNAL_UI_BA_STATE* pState,
    __in const BOOTSTRAPPER_CREATE_ARGS* /*pArgs*/
    )
{
    HRESULT hr = S_OK;

    pState->prereqData.fAlwaysInstallPrereqs = TRUE;
    pState->prereqData.fPerformHelp = TRUE;
    pState->prereqData.fPerformLayout = TRUE;

    return hr;
}

static HRESULT CreatePrerequisiteBA(
    __in INTERNAL_UI_BA_STATE* pState,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPrereqPath = NULL;
    HMODULE hModule = NULL;

    hr = PathConcat(pState->sczAppBase, L"prereqba.dll", &sczPrereqPath);
    BalExitOnFailure(hr, "Failed to get path to pre-requisite BA.");

    hModule = ::LoadLibraryExW(sczPrereqPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    ExitOnNullWithLastError(hModule, hr, "Failed to load pre-requisite BA DLL.");

    PFN_PREQ_BOOTSTRAPPER_APPLICATION_CREATE pfnCreate = reinterpret_cast<PFN_PREQ_BOOTSTRAPPER_APPLICATION_CREATE>(::GetProcAddress(hModule, "PrereqBootstrapperApplicationCreate"));
    ExitOnNullWithLastError(pfnCreate, hr, "Failed to get PrereqBootstrapperApplicationCreate entry-point from: %ls", sczPrereqPath);

    hr = pfnCreate(&pState->prereqData, pEngine, pArgs, pResults);
    ExitOnFailure(hr, "Failed to create prequisite bootstrapper app.");

    pState->hPrereqModule = hModule;
    hModule = NULL;

LExit:
    if (hModule)
    {
        ::FreeLibrary(hModule);
    }
    ReleaseStr(sczPrereqPath);

    return hr;
}
