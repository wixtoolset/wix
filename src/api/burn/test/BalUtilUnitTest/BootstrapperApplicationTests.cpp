// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;
using namespace WixBuildTools::TestSupport::XunitExtensions;

namespace BalUtilTests
{
    public ref class BootstrapperApplication
    {
    public:
        [Fact]
        void CanCreateTestBootstrapperApplication()
        {
            HRESULT hr = S_OK;
            BOOTSTRAPPER_CREATE_ARGS args = { };
            BOOTSTRAPPER_COMMAND command = { };
            BOOTSTRAPPER_CREATE_RESULTS results = { };
            IBootstrapperEngine* pEngine = NULL;
            IBootstrapperApplication* pApplication = NULL;

            args.cbSize = sizeof(args);
            args.pCommand = &command;

            results.cbSize = sizeof(results);

            try
            {
                hr = BalInitializeFromCreateArgs(&args, &pEngine);
                NativeAssert::Succeeded(hr, "Failed to create engine.");

                hr = CreateBootstrapperApplication(pEngine, &args, &results, &pApplication);
                NativeAssert::Succeeded(hr, "Failed to create BootstrapperApplication.");
            }
            finally
            {
                ReleaseObject(pEngine);
                ReleaseObject(pApplication);
            }
        }
    };
}
