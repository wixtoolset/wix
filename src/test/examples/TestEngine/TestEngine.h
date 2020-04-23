#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

class TestEngine
{
public:
    HRESULT LoadBA(
        __in LPCWSTR wzBundleFilePath,
        __in LPCWSTR wzBAFilePath
        );

    HRESULT Log(
        __in LPCWSTR wzMessage
        );

    HRESULT SendShutdownEvent(
        __in BOOTSTRAPPER_SHUTDOWN_ACTION defaultAction
        );

    HRESULT SendStartupEvent();

    void UnloadBA();

private:
    static HRESULT BAEngineLog(
        __in TestEngine* pContext,
        __in BAENGINE_LOG_ARGS* pArgs,
        __in BAENGINE_LOG_RESULTS* /*pResults*/
        );

    static HRESULT WINAPI EngineProc(
        __in BOOTSTRAPPER_ENGINE_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __in_opt LPVOID pvContext
        );

public:
    TestEngine();

    ~TestEngine();

private:
    HMODULE m_hBAModule;
    BOOTSTRAPPER_CREATE_RESULTS* m_pCreateResults;
};