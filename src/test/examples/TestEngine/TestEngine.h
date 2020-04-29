#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


enum WM_TESTENG
{
    WM_TESTENG_FIRST = WM_APP + 0xFFF, // this enum value must always be first.

    WM_TESTENG_DETECT,
    WM_TESTENG_PLAN,
    WM_TESTENG_ELEVATE,
    WM_TESTENG_APPLY,
    WM_TESTENG_LAUNCH_APPROVED_EXE,
    WM_TESTENG_QUIT,

    WM_TESTENG_LAST, // this enum value must always be last.
};

class TestEngine
{
public:
    HRESULT Initialize(
        __in LPCWSTR wzBundleFilePath
        );

    HRESULT LoadBA(
        __in LPCWSTR wzBAFilePath
        );

    HRESULT Log(
        __in LPCWSTR wzMessage
        );

    HRESULT RunApplication();

    HRESULT SendShutdownEvent(
        __in BOOTSTRAPPER_SHUTDOWN_ACTION defaultAction
        );

    HRESULT SendStartupEvent();

    HRESULT SimulateQuit(
        __in DWORD dwExitCode
        );

    void UnloadBA();

private:
    HRESULT BAEngineLog(
        __in BAENGINE_LOG_ARGS* pArgs,
        __in BAENGINE_LOG_RESULTS* pResults
        );

    HRESULT BAEngineQuit(
        __in BAENGINE_QUIT_ARGS* pArgs,
        __in BAENGINE_QUIT_RESULTS* pResults
        );

    static HRESULT WINAPI EngineProc(
        __in BOOTSTRAPPER_ENGINE_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __in_opt LPVOID pvContext
        );

    HRESULT ProcessBAMessage(
        __in const MSG* pmsg
        );

public:
    TestEngine();

    ~TestEngine();

private:
    HMODULE m_hBAModule;
    BOOTSTRAPPER_CREATE_RESULTS* m_pCreateResults;
    DWORD m_dwThreadId;
};