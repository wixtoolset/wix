// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

namespace WixToolsetTest
{
namespace MbaHost
{
namespace Native
{
    using namespace System;
    using namespace Xunit;

    public ref class MbaHostFixture
    {
    public:
        [Fact]
        void CanLoadManagedBootstrapperApplication()
        {
            HMODULE hBAModule = NULL;
            PFN_BOOTSTRAPPER_APPLICATION_CREATE pfnCreate = NULL;
            HRESULT hr = S_OK;

            EngineForTest^ engine = gcnew EngineForTest();
            BOOTSTRAPPER_ENGINE_CONTEXT engineContext = { };
            engineContext.pfnLog = engine->GetTestLogProc();

            LogInitialize(::GetModuleHandleW(NULL));

            hr = LogOpen(NULL, L"MbaHostUnitTest", NULL, L"txt", FALSE, FALSE, NULL);
            Assert::Equal(S_OK, hr);

            BOOTSTRAPPER_COMMAND command = { };
            BOOTSTRAPPER_CREATE_ARGS args = { };
            BOOTSTRAPPER_CREATE_RESULTS results = { };

            args.cbSize = sizeof(BOOTSTRAPPER_CREATE_ARGS);
            args.pCommand = &command;
            args.pfnBootstrapperEngineProc = EngineForTestProc;
            args.pvBootstrapperEngineProcContext = &engineContext;
            args.qwEngineAPIVersion = MAKEQWORDVERSION(0, 0, 0, 1);

            results.cbSize = sizeof(BOOTSTRAPPER_CREATE_RESULTS);

            hBAModule = ::LoadLibraryExW(L"mbahost.dll", NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
            Assert::NotEqual(NULL, (int)hBAModule);

            pfnCreate = (PFN_BOOTSTRAPPER_APPLICATION_CREATE)::GetProcAddress(hBAModule, "BootstrapperApplicationCreate");
            Assert::NotEqual(NULL, (int)pfnCreate);

            hr = pfnCreate(&args, &results);
            Assert::Equal(S_OK, hr);

            BA_ONSHUTDOWN_ARGS shutdownArgs = { };
            BA_ONSHUTDOWN_RESULTS shutdownResults = { };
            shutdownArgs.cbSize = sizeof(BA_ONSHUTDOWN_ARGS);
            shutdownResults.action = BOOTSTRAPPER_SHUTDOWN_ACTION_RELOAD_BOOTSTRAPPER;
            shutdownResults.cbSize = sizeof(BA_ONSHUTDOWN_RESULTS);
            hr = results.pfnBootstrapperApplicationProc(BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN, &shutdownArgs, &shutdownResults, results.pvBootstrapperApplicationProcContext);
            Assert::Equal(S_OK, hr);

            List<String^>^ logMessages = engine->GetLogMessages();
            Assert::Equal(2, logMessages->Count);
            Assert::Equal("Loading managed bootstrapper application.", logMessages[0]);
            Assert::Equal("Shutdown,ReloadBootstrapper,0", logMessages[1]);
        }
    };
}
}
}
