// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;

namespace DutilTests
{
    public ref class ApupUtil
    {
    public:
        [Fact]
        void AllocChainFromAtomSortsDescending()
        {
            HRESULT hr = S_OK;
            ATOM_FEED* pFeed = NULL;
            APPLICATION_UPDATE_CHAIN* pChain = NULL;

            DutilInitialize(&DutilTestTraceError);

            try
            {
                XmlInitialize();
                NativeAssert::Succeeded(hr, "Failed to initialize Xml.");

                pin_ptr<const wchar_t> feedFilePath = PtrToStringChars(TestData::Get("TestData", "ApupUtilTests", "FeedBv2.0.xml"));
                hr = AtomParseFromFile(feedFilePath, &pFeed);
                NativeAssert::Succeeded(hr, "Failed to parse feed: {0}", feedFilePath);

                hr = ApupAllocChainFromAtom(pFeed, &pChain);
                NativeAssert::Succeeded(hr, "Failed to get chain from feed.");

                Assert::Equal(3ul, pChain->cEntries);
                NativeAssert::StringEqual(L"Bundle v2.0", pChain->rgEntries[0].wzTitle);
                NativeAssert::StringEqual(L"Bundle v1.0", pChain->rgEntries[1].wzTitle);
                NativeAssert::StringEqual(L"Bundle v1.0-preview", pChain->rgEntries[2].wzTitle);
            }
            finally
            {
                DutilUninitialize();
            }
        }
    };
}
