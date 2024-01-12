// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixInternal::TestSupport;
using namespace WixInternal::TestSupport::XunitExtensions;

namespace BalUtilTests
{
    public ref class BootstrapperApplication
    {
    public:
        [Fact(Skip = "Need a mock implementation of IBootstrapperEngine to test BootstrapperApplication.")]
        void CanCreateTestBootstrapperApplication()
        {
            HRESULT hr = S_OK;
            IBootstrapperApplication* pApplication = NULL;
            IBootstrapperEngine* pEngine = NULL;
            BOOTSTRAPPER_COMMAND command = { };

            try
            {
                hr = CreateBootstrapperApplication(&pApplication);
                NativeAssert::Succeeded(hr, "Failed to create BootstrapperApplication.");

                hr = pApplication->OnCreate(pEngine, &command);
                NativeAssert::Succeeded(hr, "Failed to initialize BootstrapperApplication.");
            }
            finally
            {
                ReleaseObject(pEngine);
                ReleaseObject(pApplication);
            }
        }
    };
}
