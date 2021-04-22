// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define WuaExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_WUAUTIL, x, s, __VA_ARGS__)
#define WuaExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_WUAUTIL, x, s, __VA_ARGS__)
#define WuaExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_WUAUTIL, x, s, __VA_ARGS__)
#define WuaExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_WUAUTIL, x, s, __VA_ARGS__)
#define WuaExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_WUAUTIL, x, s, __VA_ARGS__)
#define WuaExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_WUAUTIL, x, s, __VA_ARGS__)
#define WuaExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_WUAUTIL, p, x, e, s, __VA_ARGS__)
#define WuaExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_WUAUTIL, p, x, s, __VA_ARGS__)
#define WuaExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_WUAUTIL, p, x, e, s, __VA_ARGS__)
#define WuaExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_WUAUTIL, p, x, s, __VA_ARGS__)
#define WuaExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_WUAUTIL, e, x, s, __VA_ARGS__)
#define WuaExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_WUAUTIL, g, x, s, __VA_ARGS__)


// internal function declarations

static HRESULT GetAutomaticUpdatesService(
    __out IAutomaticUpdates **ppAutomaticUpdates
    );


// function definitions

extern "C" HRESULT DAPI WuaPauseAutomaticUpdates()
{
    HRESULT hr = S_OK;
    IAutomaticUpdates *pAutomaticUpdates = NULL;

    hr = GetAutomaticUpdatesService(&pAutomaticUpdates);
    WuaExitOnFailure(hr, "Failed to get the Automatic Updates service.");

    hr = pAutomaticUpdates->Pause();
    WuaExitOnFailure(hr, "Failed to pause the Automatic Updates service.");

LExit:
    ReleaseObject(pAutomaticUpdates);

    return hr;
}

extern "C" HRESULT DAPI WuaResumeAutomaticUpdates()
{
    HRESULT hr = S_OK;
    IAutomaticUpdates *pAutomaticUpdates = NULL;

    hr = GetAutomaticUpdatesService(&pAutomaticUpdates);
    WuaExitOnFailure(hr, "Failed to get the Automatic Updates service.");

    hr = pAutomaticUpdates->Resume();
    WuaExitOnFailure(hr, "Failed to resume the Automatic Updates service.");

LExit:
    ReleaseObject(pAutomaticUpdates);

    return hr;
}

extern "C" HRESULT DAPI WuaRestartRequired(
    __out BOOL* pfRestartRequired
    )
{
    HRESULT hr = S_OK;
    ISystemInformation* pSystemInformation = NULL;
    VARIANT_BOOL bRestartRequired;

    hr = ::CoCreateInstance(__uuidof(SystemInformation), NULL, CLSCTX_INPROC_SERVER, __uuidof(ISystemInformation), reinterpret_cast<LPVOID*>(&pSystemInformation));
    WuaExitOnRootFailure(hr, "Failed to get WUA system information interface.");

    hr = pSystemInformation->get_RebootRequired(&bRestartRequired);
    WuaExitOnRootFailure(hr, "Failed to determine if restart is required from WUA.");

    *pfRestartRequired = (VARIANT_FALSE != bRestartRequired);

LExit:
    ReleaseObject(pSystemInformation);

    return hr;
}


// internal function definitions

static HRESULT GetAutomaticUpdatesService(
    __out IAutomaticUpdates **ppAutomaticUpdates
    )
{
    HRESULT hr = S_OK;
    CLSID clsidAutomaticUpdates = { };

    hr = ::CLSIDFromProgID(L"Microsoft.Update.AutoUpdate", &clsidAutomaticUpdates);
    WuaExitOnFailure(hr, "Failed to get CLSID for Microsoft.Update.AutoUpdate.");

    hr = ::CoCreateInstance(clsidAutomaticUpdates, NULL, CLSCTX_INPROC_SERVER, IID_IAutomaticUpdates, reinterpret_cast<LPVOID*>(ppAutomaticUpdates));
    WuaExitOnFailure(hr, "Failed to create instance of Microsoft.Update.AutoUpdate.");

LExit:
    return hr;
}
