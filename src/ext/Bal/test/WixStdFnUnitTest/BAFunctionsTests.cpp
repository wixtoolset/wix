// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixInternal::TestSupport;
using namespace WixInternal::TestSupport::XunitExtensions;

namespace BalUtilTests
{
    public ref class BAFunctions
    {
    public:
        [Fact(Skip = "Need a mock implementation of IBootstrapperEngine to test BAFunctions.")]
        void CanCreateTestBAFunctions()
        {
            HRESULT hr = S_OK;
            BA_FUNCTIONS_CREATE_ARGS args = { };
            BA_FUNCTIONS_CREATE_RESULTS results = { };
            IBootstrapperEngine* pEngine = NULL;
            BOOTSTRAPPER_COMMAND command = { };
            IBAFunctions* pBAFunctions = NULL;

            args.cbSize = sizeof(args);
            args.pEngine = pEngine;
            args.pCommand = &command;

            results.cbSize = sizeof(results);

            try
            {
                BalInitialize(pEngine);

                hr = CreateBAFunctions(NULL, &args, &results);
                NativeAssert::Succeeded(hr, "Failed to create BAFunctions.");

                pBAFunctions = reinterpret_cast<IBAFunctions*>(results.pvBAFunctionsProcContext);
            }
            finally
            {
                ReleaseObject(pEngine);
                ReleaseObject(pBAFunctions);
            }
        }
    };
}
