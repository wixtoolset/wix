// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const DWORD TEST_CHILD_SENT_MESSAGE_ID = 0xFFFE;
const DWORD TEST_PARENT_SENT_MESSAGE_ID = 0xFFFF;
const HRESULT S_TEST_SUCCEEDED = 0x3133;
const char TEST_MESSAGE_DATA[] = "{94949868-7EAE-4ac5-BEAC-AFCA2821DE01}";


static BOOL STDAPICALLTYPE ElevateTest_ShellExecuteExW(
    __inout LPSHELLEXECUTEINFOW lpExecInfo
    );
static DWORD CALLBACK ElevateTest_ThreadProc(
    __in LPVOID lpThreadParameter
    );
static HRESULT ProcessParentMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    );
static HRESULT ProcessChildMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    );

namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{
    using namespace System;
    using namespace System::IO;
    using namespace System::Threading;
    using namespace Xunit;

    public ref class ElevationTest : BurnUnitTest
    {
    public:
        ElevationTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact]
        void ElevateTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_PIPE_CONNECTION* pConnection = &engineState.companionConnection;
            DWORD dwResult = S_OK;

            engineState.cache.sczBundleEngineWorkingPath = L"tests\\ignore\\this\\path\\to\\burn.exe";

            try
            {
                ShelFunctionOverride(ElevateTest_ShellExecuteExW);
                CoreFunctionOverride(NULL, ThrdWaitForCompletion);

                BurnPipeConnectionInitialize(pConnection);

                //
                // per-user side setup
                //
                hr = ElevationElevate(&engineState, WM_BURN_ELEVATE, NULL);
                TestThrowOnFailure(hr, L"Failed to elevate.");

                // post execute message
                hr = BurnPipeSendMessage(pConnection->hPipe, TEST_PARENT_SENT_MESSAGE_ID, NULL, 0, ProcessParentMessages, NULL, &dwResult);
                TestThrowOnFailure(hr, "Failed to post execute message to per-machine process.");

                //
                // initiate termination
                //
                hr = BurnPipeTerminateChildProcess(pConnection, 666, FALSE);
                TestThrowOnFailure(hr, L"Failed to terminate elevated process.");

                // check flags
                Assert::Equal(S_TEST_SUCCEEDED, (HRESULT)dwResult);
            }
            finally
            {
                BurnPipeConnectionUninitialize(pConnection);
            }
        }
    };
}
}
}
}
}


static BOOL STDAPICALLTYPE ElevateTest_ShellExecuteExW(
    __inout LPSHELLEXECUTEINFOW lpExecInfo
    )
{
    HRESULT hr = S_OK;
    LPWSTR scz = NULL;

    hr = StrAllocString(&scz, lpExecInfo->lpParameters, 0);
    ExitOnFailure(hr, "Failed to copy arguments.");

    // Pretend this thread is the elevated process.
    lpExecInfo->hProcess = ::CreateThread(NULL, 0, ElevateTest_ThreadProc, scz, 0, NULL);
    ExitOnNullWithLastError(lpExecInfo->hProcess, hr, "Failed to create thread.");
    scz = NULL;

LExit:
    ReleaseStr(scz);

    return SUCCEEDED(hr);
}

static DWORD CALLBACK ElevateTest_ThreadProc(
    __in LPVOID lpThreadParameter
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczArguments = (LPWSTR)lpThreadParameter;
    BURN_PIPE_CONNECTION connection = { };
    BURN_PIPE_RESULT result = { };

    BurnPipeConnectionInitialize(&connection);

    StrAlloc(&connection.sczName, MAX_PATH);
    StrAlloc(&connection.sczSecret, MAX_PATH);

    // parse command line arguments
    if (3 != swscanf_s(sczArguments, L"-q -burn.elevated %s %s %u", connection.sczName, MAX_PATH, connection.sczSecret, MAX_PATH, &connection.dwProcessId))
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Failed to parse argument string.");
    }

    // set up connection with per-user process
    hr = BurnPipeChildConnect(&connection, TRUE);
    ExitOnFailure(hr, "Failed to connect to per-user process.");

    // pump messages
    hr = BurnPipePumpMessages(connection.hPipe, ProcessChildMessages, static_cast<LPVOID>(connection.hPipe), &result);
    ExitOnFailure(hr, "Failed while pumping messages in child 'process'.");

LExit:
    BurnPipeConnectionUninitialize(&connection);
    ReleaseStr(sczArguments);

    return FAILED(hr) ? (DWORD)hr : result.dwResult;
}

static HRESULT ProcessParentMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID /*pvContext*/,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    HRESULT hrResult = E_INVALIDDATA;

    // Process the message.
    switch (pMsg->dwMessageType)
    {
    case TEST_CHILD_SENT_MESSAGE_ID:
        if (sizeof(TEST_MESSAGE_DATA) == pMsg->cbData && 0 == memcmp(TEST_MESSAGE_DATA, pMsg->pvData, sizeof(TEST_MESSAGE_DATA)))
        {
            hrResult = S_TEST_SUCCEEDED;
        }
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Unexpected elevated message sent to parent process, msg: %u", pMsg->dwMessageType);
    }

    *pdwResult = static_cast<DWORD>(hrResult);

LExit:
    return hr;
}

static HRESULT ProcessChildMessages(
    __in PIPE_MESSAGE* pMsg,
    __in_opt LPVOID pvContext,
    __out DWORD* pdwResult
    )
{
    HRESULT hr = S_OK;
    HANDLE hPipe = static_cast<HANDLE>(pvContext);
    DWORD dwResult = 0;

    // Process the message.
    switch (pMsg->dwMessageType)
    {
    case TEST_PARENT_SENT_MESSAGE_ID:
        // send test message
        hr = BurnPipeSendMessage(hPipe, TEST_CHILD_SENT_MESSAGE_ID, (LPVOID)TEST_MESSAGE_DATA, sizeof(TEST_MESSAGE_DATA), NULL, NULL, &dwResult);
        ExitOnFailure(hr, "Failed to send message to per-machine process.");
        break;

    default:
        hr = E_INVALIDARG;
        ExitOnRootFailure(hr, "Unexpected elevated message sent to child process, msg: %u", pMsg->dwMessageType);
    }

    *pdwResult = dwResult;

LExit:
    return hr;
}
