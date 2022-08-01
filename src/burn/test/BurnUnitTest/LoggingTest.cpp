// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{
    using namespace System;
    using namespace Xunit;

    public ref class LoggingTest : BurnUnitTest
    {
    public:
        LoggingTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact]
        void LoggingLoadXmlTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_ENGINE_STATE engineState = { };
            try
            {
                LPCWSTR wzDocument =
                    L"<BurnManifest>"
                    L"    <Log PathVariable='WixBundleLog' Prefix='BundleA' Extension='.log' />"
                    L"</BurnManifest>";

                // logutil is static so there can only be one log active at a time.
                // This test needs to open a log so need to close the default one for the tests and then open a new one at the end for the tests that run after this one.
                LogClose(FALSE);

                VariableInitialize(&engineState.variables);

                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = LoggingParseFromXml(&engineState.log, pixeBundle);
                NativeAssert::Succeeded(hr, L"Failed to parse logging from XML.");

                engineState.internalCommand.mode = BURN_MODE_NORMAL;

                hr = LoggingOpen(&engineState.log, &engineState.internalCommand, &engineState.command, &engineState.variables, L"BundleA");
                NativeAssert::Succeeded(hr, L"Failed to open logging.");

                Assert::True(VariableExistsHelper(&engineState.variables, L"WixBundleLog"));
            }
            finally
            {
                ReleaseObject(pixeBundle);
                LogClose(FALSE);
                LogOpen(NULL, L"BurnUnitTest", NULL, L"txt", FALSE, FALSE, NULL);
            }
        }
    };
}
}
}
}
}
