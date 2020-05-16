// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT TestEngine::Initialize(
    __in LPCWSTR wzBundleFilePath
    )
{
    HRESULT hr = S_OK;
    MSG msg = { };

    LogInitialize(::GetModuleHandleW(NULL));

    hr = LogOpen(NULL, PathFile(wzBundleFilePath), NULL, L"txt", FALSE, FALSE, NULL);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to open log.");

    ::PeekMessageW(&msg, NULL, WM_USER, WM_USER, PM_NOREMOVE);

LExit:
    return hr;
}

HRESULT TestEngine::LoadBA(
    __in LPCWSTR wzBAFilePath
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_COMMAND command = { };
    BOOTSTRAPPER_CREATE_ARGS args = { };
    PFN_BOOTSTRAPPER_APPLICATION_CREATE pfnCreate = NULL;

    if (m_pCreateResults || m_hBAModule)
    {
        ExitFunction1(hr = E_INVALIDSTATE);
    }

    m_pCreateResults = static_cast<BOOTSTRAPPER_CREATE_RESULTS*>(MemAlloc(sizeof(BOOTSTRAPPER_CREATE_RESULTS), TRUE));

    command.cbSize = sizeof(BOOTSTRAPPER_COMMAND);

    hr = PathGetDirectory(wzBAFilePath, &command.wzBootstrapperWorkingFolder);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to allocate wzBootstrapperWorkingFolder");

    hr = PathConcat(command.wzBootstrapperWorkingFolder, L"BootstrapperApplicationData.xml", &command.wzBootstrapperApplicationDataPath);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to allocate wzBootstrapperApplicationDataPath");

    args.cbSize = sizeof(BOOTSTRAPPER_CREATE_ARGS);
    args.pCommand = &command;
    args.pfnBootstrapperEngineProc = TestEngine::EngineProc;
    args.pvBootstrapperEngineProcContext = this;
    args.qwEngineAPIVersion = MAKEQWORDVERSION(0, 0, 0, 1);

    m_pCreateResults->cbSize = sizeof(BOOTSTRAPPER_CREATE_RESULTS);

    m_hBAModule = ::LoadLibraryExW(wzBAFilePath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
    ConsoleExitOnNullWithLastError(m_hBAModule, hr, CONSOLE_COLOR_RED, "Failed to load BA dll.");

    pfnCreate = (PFN_BOOTSTRAPPER_APPLICATION_CREATE)::GetProcAddress(m_hBAModule, "BootstrapperApplicationCreate");
    ConsoleExitOnNull(pfnCreate, hr, E_OUTOFMEMORY, CONSOLE_COLOR_RED, "Failed to get address for BootstrapperApplicationCreate.");

    hr = pfnCreate(&args, m_pCreateResults);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "BA returned failure on BootstrapperApplicationCreate.");

LExit:
    ReleaseStr(command.wzBootstrapperApplicationDataPath);
    ReleaseStr(command.wzBootstrapperWorkingFolder);

    return hr;
}

HRESULT TestEngine::Log(
    __in LPCWSTR wzMessage
    )
{
    LogStringLine(REPORT_STANDARD, "%ls", wzMessage);
    return ConsoleWriteLine(CONSOLE_COLOR_NORMAL, "%ls", wzMessage);
}

HRESULT TestEngine::RunApplication()
{
    HRESULT hr = S_OK;
    MSG msg = { };
    BOOL fRet = FALSE;

    // Enter the message pump.
    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            ConsoleExitOnFailure(hr = E_UNEXPECTED, CONSOLE_COLOR_RED, "Unexpected return value from message pump.");
        }
        else
        {
            ProcessBAMessage(&msg);
        }
    }

LExit:
    return hr;
}

HRESULT TestEngine::SendShutdownEvent(
    __in BOOTSTRAPPER_SHUTDOWN_ACTION defaultAction
    )
{
    HRESULT hr = S_OK;
    BA_ONSHUTDOWN_ARGS shutdownArgs = { };
    BA_ONSHUTDOWN_RESULTS shutdownResults = { };
    shutdownArgs.cbSize = sizeof(BA_ONSHUTDOWN_ARGS);
    shutdownResults.action = defaultAction;
    shutdownResults.cbSize = sizeof(BA_ONSHUTDOWN_RESULTS);
    hr = m_pCreateResults->pfnBootstrapperApplicationProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN, &shutdownArgs, &shutdownResults, m_pCreateResults->pvBootstrapperApplicationProcContext);
    return hr;
}

HRESULT TestEngine::SendStartupEvent()
{
    HRESULT hr = S_OK;
    BA_ONSTARTUP_ARGS startupArgs = { };
    BA_ONSTARTUP_RESULTS startupResults = { };
    startupArgs.cbSize = sizeof(BA_ONSTARTUP_ARGS);
    startupResults.cbSize = sizeof(BA_ONSTARTUP_RESULTS);
    hr = m_pCreateResults->pfnBootstrapperApplicationProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP, &startupArgs, &startupResults, m_pCreateResults->pvBootstrapperApplicationProcContext);
    return hr;
}

HRESULT TestEngine::SimulateQuit(
    __in DWORD dwExitCode
    )
{
    BAENGINE_QUIT_ARGS args = { };
    BAENGINE_QUIT_RESULTS results = { };

    args.cbSize = sizeof(BAENGINE_QUIT_ARGS);
    args.dwExitCode = dwExitCode;

    results.cbSize = sizeof(BAENGINE_QUIT_RESULTS);

    return BAEngineQuit(&args, &results);
}

void TestEngine::UnloadBA()
{
    PFN_BOOTSTRAPPER_APPLICATION_DESTROY pfnDestroy = NULL;
    BOOL fDisableUnloading = m_pCreateResults && m_pCreateResults->fDisableUnloading;

    ReleaseNullMem(m_pCreateResults);

    pfnDestroy = (PFN_BOOTSTRAPPER_APPLICATION_DESTROY)::GetProcAddress(m_hBAModule, "BootstrapperApplicationDestroy");

    if (pfnDestroy)
    {
        pfnDestroy();
    }

    if (m_hBAModule)
    {
        if (!fDisableUnloading)
        {
            ::FreeLibrary(m_hBAModule);
        }

        m_hBAModule = NULL;
    }
}

HRESULT TestEngine::BAEngineLog(
    __in BAENGINE_LOG_ARGS* pArgs,
    __in BAENGINE_LOG_RESULTS* /*pResults*/
    )
{
    return Log(pArgs->wzMessage);
}

HRESULT TestEngine::BAEngineQuit(
    __in BAENGINE_QUIT_ARGS* pArgs,
    __in BAENGINE_QUIT_RESULTS* /*pResults*/
    )
{
    HRESULT hr = S_OK;

    if (!::PostThreadMessageW(m_dwThreadId, WM_TESTENG_QUIT, static_cast<WPARAM>(pArgs->dwExitCode), 0))
    {
        ConsoleExitWithLastError(hr, CONSOLE_COLOR_RED, "Failed to post shutdown message.");
    }

LExit:
    return hr;
}

HRESULT WINAPI TestEngine::EngineProc(
    __in BOOTSTRAPPER_ENGINE_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    TestEngine* pContext = (TestEngine*)pvContext;

    if (!pContext || !pvArgs || !pvResults)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    switch (message)
    {
    case BOOTSTRAPPER_ENGINE_MESSAGE_LOG:
        hr = pContext->BAEngineLog(reinterpret_cast<BAENGINE_LOG_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_LOG_RESULTS*>(pvResults));
        break;
    case BOOTSTRAPPER_ENGINE_MESSAGE_QUIT:
        hr = pContext->BAEngineQuit(reinterpret_cast<BAENGINE_QUIT_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_QUIT_RESULTS*>(pvResults));
    default:
        hr = E_NOTIMPL;
        break;
    }

LExit:
    return hr;
}

HRESULT TestEngine::ProcessBAMessage(
    __in const MSG* pmsg
    )
{
    HRESULT hr = S_OK;

    switch (pmsg->message)
    {
    case WM_TESTENG_QUIT:
        ::PostQuitMessage(static_cast<int>(pmsg->wParam)); // go bye-bye.
        break;
    }

    return hr;
}

TestEngine::TestEngine()
{
    m_hBAModule = NULL;
    m_pCreateResults = NULL;
    m_dwThreadId = ::GetCurrentThreadId();
}

TestEngine::~TestEngine()
{
    ReleaseMem(m_pCreateResults);
}
