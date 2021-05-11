// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT RunReloadEngine(
    __in LPCWSTR wzBundleFilePath,
    __in LPCWSTR wzBAFilePath
    )
{
    HRESULT hr = S_OK;
    TestEngine* pTestEngine = NULL;

    pTestEngine = new TestEngine();
    ConsoleExitOnNull(pTestEngine, hr, E_OUTOFMEMORY, CONSOLE_COLOR_RED, "Failed to create new test engine.");

    hr = pTestEngine->Initialize(wzBundleFilePath);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to initialize engine.");

    hr = pTestEngine->LoadBA(wzBAFilePath);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to load BA.");

    hr = pTestEngine->SendStartupEvent();
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "BA returned failure for OnStartup.");

    hr = pTestEngine->SimulateQuit(0);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to simulate quit.");

    hr = pTestEngine->RunApplication();
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to run engine.");

    hr = pTestEngine->SendShutdownEvent(BOOTSTRAPPER_SHUTDOWN_ACTION_RELOAD_BOOTSTRAPPER);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "BA returned failure for OnShutdown.");

    pTestEngine->UnloadBA();

    hr = pTestEngine->LoadBA(wzBAFilePath);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to load BA.");

    hr = pTestEngine->SendStartupEvent();
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "BA returned failure for OnStartup.");

    hr = pTestEngine->SimulateQuit(0);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to simulate quit.");

    hr = pTestEngine->RunApplication();
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to run engine.");

    hr = pTestEngine->SendShutdownEvent(BOOTSTRAPPER_SHUTDOWN_ACTION_RESTART);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "BA returned failure for OnShutdown.");

    pTestEngine->UnloadBA();

LExit:
    return hr;
}
