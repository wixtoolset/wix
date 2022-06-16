// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;
using namespace WixBuildTools::TestSupport::XunitExtensions;

namespace BalUtilTests
{
    public ref class BAFunctions
    {
    public:
        [Fact]
        void CanCreateTestBAFunctions()
        {
            HRESULT hr = S_OK;
            BOOTSTRAPPER_CREATE_ARGS bootstrapperArgs = { };
            BOOTSTRAPPER_COMMAND bootstrapperCommand = { };
            BA_FUNCTIONS_CREATE_ARGS args = { };
            BA_FUNCTIONS_CREATE_RESULTS results = { };
            IBootstrapperEngine* pEngine = NULL;
            IBAFunctions* pBAFunctions = NULL;

            bootstrapperArgs.cbSize = sizeof(bootstrapperArgs);
            bootstrapperArgs.pCommand = &bootstrapperCommand;

            args.cbSize = sizeof(args);
            args.pBootstrapperCreateArgs = &bootstrapperArgs;

            results.cbSize = sizeof(results);

            try
            {
                hr = BalInitializeFromCreateArgs(&bootstrapperArgs, &pEngine);
                NativeAssert::Succeeded(hr, "Failed to create engine.");

                hr = CreateBAFunctions(NULL, pEngine, &args, &results, &pBAFunctions);
                NativeAssert::Succeeded(hr, "Failed to create BAFunctions.");
            }
            finally
            {
                ReleaseObject(pEngine);
                ReleaseObject(pBAFunctions);
            }
        }
    };
}
