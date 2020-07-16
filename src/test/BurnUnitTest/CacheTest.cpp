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
    using namespace System::IO;
    using namespace Xunit;

    public ref class CacheTest : BurnUnitTest
    {
    public:
        CacheTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact(Skip = "Currently fails")]
        void CacheSignatureTest()
        {
            HRESULT hr = S_OK;
            BURN_PACKAGE package = { };
            BURN_PAYLOAD payload = { };
            LPWSTR sczPayloadPath = NULL;
            BYTE* pb = NULL;
            DWORD cb = NULL;

            try
            {
                pin_ptr<const wchar_t> dataDirectory = PtrToStringChars(this->TestContext->DataDirectory);
                hr = PathConcat(dataDirectory, L"BurnTestPayloads\\Products\\TestExe\\TestExe.exe", &sczPayloadPath);
                Assert::True(S_OK == hr, "Failed to get path to test file.");
                Assert::True(FileExistsEx(sczPayloadPath, NULL), "Test file does not exist.");

                hr = StrAllocHexDecode(L"232BD16B78C1926F95D637731E1EE5379A3C4222", &pb, &cb);
                Assert::Equal(S_OK, hr);

                package.fPerMachine = FALSE;
                package.sczCacheId = L"Bootstrapper.CacheTest.CacheSignatureTest";
                payload.sczKey = L"CacheSignatureTest.PayloadKey";
                payload.sczFilePath = L"CacheSignatureTest.File";
                payload.pbHash = pb;
                payload.cbHash = cb;

                hr = CacheCompletePayload(package.fPerMachine, &payload, package.sczCacheId, sczPayloadPath, FALSE);
                Assert::Equal(S_OK, hr);
            }
            finally
            {
                ReleaseMem(pb);
                ReleaseStr(sczPayloadPath);

                String^ filePath = Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), "Package Cache\\Bootstrapper.CacheTest.CacheSignatureTest\\CacheSignatureTest.File");
                if (File::Exists(filePath))
                {
                    File::SetAttributes(filePath, FileAttributes::Normal);
                    File::Delete(filePath);
                }
            }
        }
    };
}
}
}
}
}
