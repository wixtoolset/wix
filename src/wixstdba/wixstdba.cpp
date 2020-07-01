// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HINSTANCE vhInstance = NULL;
static IBootstrapperApplication* vpApplication = NULL;

static void CALLBACK WixstdbaTraceError(
    __in_z LPCSTR szFile,
    __in int iLine,
    __in REPORT_LEVEL rl,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

extern "C" BOOL WINAPI DllMain(
    IN HINSTANCE hInstance,
    IN DWORD dwReason,
    IN LPVOID /* pvReserved */
    )
{
    switch(dwReason)
    {
    case DLL_PROCESS_ATTACH:
        ::DisableThreadLibraryCalls(hInstance);
        vhInstance = hInstance;
        break;

    case DLL_PROCESS_DETACH:
        vhInstance = NULL;
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

    DutilInitialize(&WixstdbaTraceError);

    hr = BalInitializeFromCreateArgs(pArgs, &pEngine);
    ExitOnFailure(hr, "Failed to initialize Bal.");

    hr = CreateBootstrapperApplication(vhInstance, FALSE, S_OK, pEngine, pArgs, pResults, &vpApplication);
    BalExitOnFailure(hr, "Failed to create bootstrapper application interface.");

LExit:
    ReleaseObject(pEngine);

    return hr;
}


extern "C" void WINAPI BootstrapperApplicationDestroy()
{
    ReleaseNullObject(vpApplication);
    BalUninitialize();
    DutilUninitialize();
}


extern "C" HRESULT WINAPI DncPrereqBootstrapperApplicationCreate(
    __in HRESULT hrHostInitialization,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;

    DutilInitialize(&WixstdbaTraceError);

    BalInitialize(pEngine);

    hr = CreateBootstrapperApplication(vhInstance, TRUE, hrHostInitialization, pEngine, pArgs, pResults, &vpApplication);
    BalExitOnFailure(hr, "Failed to create .NET Core prerequisite bootstrapper application interface.");

LExit:
    return hr;
}


extern "C" void WINAPI DncPrereqBootstrapperApplicationDestroy()
{
    ReleaseNullObject(vpApplication);
    BalUninitialize();
    DutilUninitialize();
}


extern "C" HRESULT WINAPI MbaPrereqBootstrapperApplicationCreate(
    __in HRESULT hrHostInitialization,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;

    DutilInitialize(&WixstdbaTraceError);

    BalInitialize(pEngine);

    hr = CreateBootstrapperApplication(vhInstance, TRUE, hrHostInitialization, pEngine, pArgs, pResults, &vpApplication);
    BalExitOnFailure(hr, "Failed to create managed prerequisite bootstrapper application interface.");

LExit:
    return hr;
}


extern "C" void WINAPI MbaPrereqBootstrapperApplicationDestroy()
{
    ReleaseNullObject(vpApplication);
    BalUninitialize();
    DutilUninitialize();
}

static void CALLBACK WixstdbaTraceError(
    __in_z LPCSTR /*szFile*/,
    __in int /*iLine*/,
    __in REPORT_LEVEL /*rl*/,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    // BalLogError currently uses the Exit... macros,
    // so if expanding the scope need to ensure this doesn't get called recursively.
    if (DUTIL_SOURCE_THMUTIL == source)
    {
        BalLogErrorArgs(hrError, szFormat, args);
    }
}
