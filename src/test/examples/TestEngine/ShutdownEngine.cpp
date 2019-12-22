// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT RunShutdownEngine(
    __in LPCWSTR wzBAFilePath
    )
{
    HRESULT hr = S_OK;
    TestEngine* pTestEngine = NULL;

    pTestEngine = new TestEngine();
    ConsoleExitOnNull(pTestEngine, hr, E_OUTOFMEMORY, CONSOLE_COLOR_RED, "Failed to create new test engine.");

    hr = pTestEngine->LoadBA(wzBAFilePath);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to load BA.");

    hr = pTestEngine->SendShutdownEvent(BOOTSTRAPPER_SHUTDOWN_ACTION_RELOAD_BOOTSTRAPPER);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "BA returned failure for OnShutdown.");

LExit:
    return hr;
}
