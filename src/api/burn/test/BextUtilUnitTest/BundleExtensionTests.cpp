// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;
using namespace WixBuildTools::TestSupport::XunitExtensions;

namespace BextUtilTests
{
    public ref class BundleExtension
    {
    public:
        [Fact]
        void CanCreateTestBundleExtension()
        {
            HRESULT hr = S_OK;
            BUNDLE_EXTENSION_CREATE_ARGS args = { };
            BUNDLE_EXTENSION_CREATE_RESULTS results = { };
            IBundleExtensionEngine* pEngine = NULL;
            IBundleExtension* pBundleExtension = NULL;

            args.cbSize = sizeof(args);
            args.wzBundleExtensionDataPath = L"test.xml";

            results.cbSize = sizeof(results);

            try
            {
                hr = BextInitializeFromCreateArgs(&args, &pEngine);
                NativeAssert::Succeeded(hr, "Failed to create engine.");

                hr = TestBundleExtensionCreate(pEngine, &args, &results, &pBundleExtension);
                NativeAssert::Succeeded(hr, "Failed to create BootstrapperApplication.");
            }
            finally
            {
                ReleaseObject(pEngine);
                ReleaseObject(pBundleExtension);
            }
        }
    };
}
