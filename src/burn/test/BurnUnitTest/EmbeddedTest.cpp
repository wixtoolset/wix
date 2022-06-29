// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


const DWORD TEST_UNKNOWN_MESSAGE_ID = 0xFFFE;
const HRESULT S_TEST_SUCCEEDED = 0x3133;
const DWORD TEST_EXIT_CODE = 666;

struct BUNDLE_RUNNER_CONTEXT
{
    DWORD dwResult;
    BURN_PIPE_CONNECTION connection;
};


static BOOL STDAPICALLTYPE EmbeddedTest_CreateProcessW(
    __in_opt LPCWSTR lpApplicationName,
    __inout_opt LPWSTR lpCommandLine,
    __in_opt LPSECURITY_ATTRIBUTES lpProcessAttributes,
    __in_opt LPSECURITY_ATTRIBUTES lpThreadAttributes,
    __in BOOL bInheritHandles,
    __in DWORD dwCreationFlags,
    __in_opt LPVOID lpEnvironment,
    __in_opt LPCWSTR lpCurrentDirectory,
    __in LPSTARTUPINFOW lpStartupInfo,
    __out LPPROCESS_INFORMATION lpProcessInformation
    );
static DWORD CALLBACK EmbeddedTest_ThreadProc(
    __in LPVOID lpThreadParameter
    );
static int EmbeddedTest_GenericMessageHandler(
    __in GENERIC_EXECUTE_MESSAGE* pMessage,
    __in LPVOID pvContext
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

    public ref class EmbeddedTest : BurnUnitTest
    {
    public:
        EmbeddedTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact]
        void EmbeddedProtocolTest()
        {
            HRESULT hr = S_OK;
            BUNDLE_RUNNER_CONTEXT bundleRunnerContext = { };
            DWORD dwExitCode = 0;

            try
            {
                CoreFunctionOverride(EmbeddedTest_CreateProcessW, ThrdWaitForCompletion);

                //
                // bundle runner setup
                //
                hr = EmbeddedRunBundle(&bundleRunnerContext.connection, L"C:\\ignored\\target.exe", L"\"C:\\ignored\\target.exe\"", NULL, EmbeddedTest_GenericMessageHandler, &bundleRunnerContext, &dwExitCode);
                TestThrowOnFailure(hr, L"Failed to run embedded bundle.");

                // check results
                Assert::Equal<HRESULT>(S_TEST_SUCCEEDED, (HRESULT)bundleRunnerContext.dwResult);
                Assert::Equal<DWORD>(TEST_EXIT_CODE, dwExitCode);
            }
            finally
            {
            }
        }
    };
}
}
}
}
}


static BOOL STDAPICALLTYPE EmbeddedTest_CreateProcessW(
    __in_opt LPCWSTR /*lpApplicationName*/,
    __inout_opt LPWSTR lpCommandLine,
    __in_opt LPSECURITY_ATTRIBUTES /*lpProcessAttributes*/,
    __in_opt LPSECURITY_ATTRIBUTES /*lpThreadAttributes*/,
    __in BOOL /*bInheritHandles*/,
    __in DWORD /*dwCreationFlags*/,
    __in_opt LPVOID /*lpEnvironment*/,
    __in_opt LPCWSTR /*lpCurrentDirectory*/,
    __in LPSTARTUPINFOW /*lpStartupInfo*/,
    __out LPPROCESS_INFORMATION lpProcessInformation
    )
{
    HRESULT hr = S_OK;
    LPWSTR scz = NULL;
    LPCWSTR wzArgs = lpCommandLine + 24; //skip '"C:\ignored\target.exe" '

    hr = StrAllocString(&scz, wzArgs, 0);
    ExitOnFailure(hr, "Failed to copy arguments.");

    // Pretend this thread is the embedded process.
    lpProcessInformation->hProcess = ::CreateThread(NULL, 0, EmbeddedTest_ThreadProc, scz, 0, NULL);
    ExitOnNullWithLastError(lpProcessInformation->hProcess, hr, "Failed to create thread.");

    scz = NULL;

LExit:
    ReleaseStr(scz);

    return SUCCEEDED(hr);
}

static DWORD CALLBACK EmbeddedTest_ThreadProc(
    __in LPVOID lpThreadParameter
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczArguments = (LPWSTR)lpThreadParameter;
    BURN_ENGINE_STATE engineState = { };
    BURN_PIPE_CONNECTION* pConnection = &engineState.embeddedConnection;
    DWORD dwResult = 0;

    engineState.internalCommand.mode = BURN_MODE_EMBEDDED;

    PipeConnectionInitialize(pConnection);

    StrAlloc(&pConnection->sczName, MAX_PATH);
    StrAlloc(&pConnection->sczSecret, MAX_PATH);

    // parse command line arguments
    if (3 != swscanf_s(sczArguments, L"-burn.embedded %s %s %u", pConnection->sczName, MAX_PATH, pConnection->sczSecret, MAX_PATH, &pConnection->dwProcessId))
    {
        ExitWithRootFailure(hr, E_INVALIDARG, "Failed to parse argument string.");
    }

    // set up connection with parent bundle runner
    hr = PipeChildConnect(pConnection, FALSE);
    ExitOnFailure(hr, "Failed to connect to parent bundle runner.");

    // post unknown message
    hr = PipeSendMessage(pConnection->hPipe, TEST_UNKNOWN_MESSAGE_ID, NULL, 0, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to post unknown message to parent bundle runner.");

    if (E_NOTIMPL != dwResult)
    {
        ExitWithRootFailure(hr, E_UNEXPECTED, "Unexpected result from unknown message: %d", dwResult);
    }

    // post known message
    hr = ExternalEngineSendEmbeddedError(&engineState, S_TEST_SUCCEEDED, NULL, 0, reinterpret_cast<int*>(&dwResult));
    ExitOnFailure(hr, "Failed to post known message to parent bundle runner.");

LExit:
    PipeConnectionUninitialize(pConnection);
    ReleaseStr(sczArguments);

    return FAILED(hr) ? (DWORD)hr : dwResult;
}

static int EmbeddedTest_GenericMessageHandler(
    __in GENERIC_EXECUTE_MESSAGE* pMessage,
    __in LPVOID pvContext
    )
{
    BUNDLE_RUNNER_CONTEXT* pContext = reinterpret_cast<BUNDLE_RUNNER_CONTEXT*>(pvContext);
    DWORD dwResult = 0;

    if (GENERIC_EXECUTE_MESSAGE_ERROR == pMessage->type)
    {
        // post unknown message
        HRESULT hr = PipeSendMessage(pContext->connection.hPipe, TEST_UNKNOWN_MESSAGE_ID, NULL, 0, NULL, NULL, &dwResult);
        ExitOnFailure(hr, "Failed to post unknown message to embedded bundle.");

        if (E_NOTIMPL != dwResult)
        {
            ExitWithRootFailure(hr, E_UNEXPECTED, "Unexpected result from unknown message: %d", dwResult);
        }

        pContext->dwResult = pMessage->error.dwErrorCode;
        dwResult = TEST_EXIT_CODE;
    }

LExit:
    return dwResult;
}
