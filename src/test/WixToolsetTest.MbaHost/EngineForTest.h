#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

HRESULT WINAPI EngineForTestProc(
    __in BOOTSTRAPPER_ENGINE_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
);

typedef void(WINAPI *PFN_TEST_LOG_PROC)(
    __in LPCWSTR sczMessage
    );

struct BOOTSTRAPPER_ENGINE_CONTEXT
{
    PFN_TEST_LOG_PROC pfnLog;
};

namespace WixToolsetTest
{
namespace MbaHost
{
namespace Native
{
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Runtime::InteropServices;

    public ref class EngineForTest
    {
    private:
        delegate void LogDelegate(LPCWSTR);
        LogDelegate^ _logDelegate;
        List<String^>^ _messages;

        void Log(LPCWSTR sczMessage)
        {
            String^ message = gcnew String(sczMessage);
            System::Diagnostics::Debug::WriteLine(message);
            _messages->Add(message);
        }
    public:
        EngineForTest()
        {
            _logDelegate = gcnew LogDelegate(this, &EngineForTest::Log);
            _messages = gcnew List<String^>();
        }

        List<String^>^ GetLogMessages()
        {
            return _messages;
        }

        PFN_TEST_LOG_PROC GetTestLogProc()
        {
            IntPtr functionPointer = Marshal::GetFunctionPointerForDelegate(_logDelegate);
            return static_cast<PFN_TEST_LOG_PROC>(functionPointer.ToPointer());
        }
    };
}
}
}