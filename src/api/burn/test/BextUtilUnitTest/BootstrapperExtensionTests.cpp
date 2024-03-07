// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixInternal::TestSupport;
using namespace WixInternal::TestSupport::XunitExtensions;

namespace BextUtilTests
{
    public ref class BootstrapperExtension
    {
    public:
        [Fact]
        void CanCreateTestBootstrapperExtension()
        {
            HRESULT hr = S_OK;
            BOOTSTRAPPER_EXTENSION_CREATE_ARGS args = { };
            BOOTSTRAPPER_EXTENSION_CREATE_RESULTS results = { };
            IBootstrapperExtensionEngine* pEngine = NULL;
            IBootstrapperExtension* pBootstrapperExtension = NULL;

            args.cbSize = sizeof(args);
            args.wzBootstrapperExtensionDataPath = L"test.xml";

            results.cbSize = sizeof(results);

            try
            {
                hr = BextInitializeFromCreateArgs(&args, &pEngine);
                NativeAssert::Succeeded(hr, "Failed to create engine.");

                hr = TestBootstrapperExtensionCreate(pEngine, &args, &results, &pBootstrapperExtension);
                NativeAssert::Succeeded(hr, "Failed to create BootstrapperApplication.");
            }
            finally
            {
                ReleaseObject(pEngine);
                ReleaseObject(pBootstrapperExtension);
            }
        }
    };
}
