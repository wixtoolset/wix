// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT TestEngine::Initialize(
    __in LPCWSTR wzBundleFilePath
    )
{
    HRESULT hr = S_OK;

    LogInitialize(::GetModuleHandleW(NULL));

    hr = LogOpen(NULL, PathFile(wzBundleFilePath), NULL, L"txt", FALSE, FALSE, NULL);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Failed to open log.");

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
    ExitOnNullWithLastError(m_hBAModule, hr, "Failed to load BA dll.");

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
    __in TestEngine* pContext,
    __in BAENGINE_LOG_ARGS* pArgs,
    __in BAENGINE_LOG_RESULTS* /*pResults*/
    )
{
    return pContext->Log(pArgs->wzMessage);
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
        hr = BAEngineLog(pContext, reinterpret_cast<BAENGINE_LOG_ARGS*>(pvArgs), reinterpret_cast<BAENGINE_LOG_RESULTS*>(pvResults));
        break;
    default:
        hr = E_NOTIMPL;
        break;
    }

LExit:
    return hr;
}

TestEngine::TestEngine()
{
    m_hBAModule = NULL;
    m_pCreateResults = NULL;
}

TestEngine::~TestEngine()
{
    ReleaseMem(m_pCreateResults);
}