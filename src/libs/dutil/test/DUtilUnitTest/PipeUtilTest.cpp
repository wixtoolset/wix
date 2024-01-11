// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace System::Security::Principal;
using namespace Xunit;
using namespace WixInternal::TestSupport;
using namespace WixInternal::TestSupport::XunitExtensions;

static DWORD STDAPICALLTYPE _TestPipeClientThreadProc(
    __in LPVOID lpThreadParameter
);

namespace DutilTests
{
    public ref class PipeUtil
    {
    public:
        [Fact]
            void PipeConnectWriteAndRead()
        {
            HRESULT hr = S_OK;
            HANDLE hServerPipe = INVALID_HANDLE_VALUE;
            HANDLE hClientThread = NULL;
            PIPE_MESSAGE msg = { };
            DWORD dwThread = 42;
            DWORD dwTestMessageId = 987654;

            try
            {
                hr = PipeCreate(L"DutilTest", NULL, &hServerPipe);
                NativeAssert::Succeeded(hr, "Failed to create server pipe.");

                hClientThread = ::CreateThread(NULL, 0, _TestPipeClientThreadProc, &dwTestMessageId, 0, NULL);
                if (hClientThread == 0)
                {
                    NativeAssert::Fail("Failed to create client thread.");
                    return;
                }

                hr = PipeServerWaitForClientConnect(hServerPipe);
                NativeAssert::Succeeded(hr, "Failed to wait for client to connect to pipe.");

                hr = PipeReadMessage(hServerPipe, &msg);
                NativeAssert::Succeeded(hr, "Failed to read message from client.");

                NativeAssert::Equal(dwTestMessageId, msg.dwMessageType);

                AppWaitForSingleObject(hClientThread, INFINITE);

                ::GetExitCodeThread(hClientThread, &dwThread);
                NativeAssert::Equal((DWORD)12, dwThread);
            }
            finally
            {
                ReleasePipeMessage(&msg);
                ReleaseHandle(hClientThread);
                ReleasePipeHandle(hServerPipe);
            }
        }
    };
}


static DWORD STDAPICALLTYPE _TestPipeClientThreadProc(
    __in LPVOID lpThreadParameter
)
{
    HRESULT hr = S_OK;
    HANDLE hClientPipe = INVALID_HANDLE_VALUE;

    hr = PipeClientConnect(L"DutilTest", &hClientPipe);
    if (FAILED(hr))
    {
        return hr;
    }

    ::Sleep(200);

    hr = PipeWriteMessage(hClientPipe, *(LPDWORD)lpThreadParameter, NULL, 0);
    if (FAILED(hr))
    {
        return hr;
    }

    ReleasePipeHandle(hClientPipe);
    return 12;
}
