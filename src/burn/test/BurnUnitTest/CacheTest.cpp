// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT CALLBACK CacheTestEventRoutine(
    __in BURN_CACHE_MESSAGE* pMessage,
    __in LPVOID pvContext
    );

static DWORD CALLBACK CacheTestProgressRoutine(
    __in LARGE_INTEGER TotalFileSize,
    __in LARGE_INTEGER TotalBytesTransferred,
    __in LARGE_INTEGER StreamSize,
    __in LARGE_INTEGER StreamBytesTransferred,
    __in DWORD dwStreamNumber,
    __in DWORD dwCallbackReason,
    __in HANDLE hSourceFile,
    __in HANDLE hDestinationFile,
    __in_opt LPVOID lpData
    );

typedef struct _CACHE_TEST_CONTEXT
{
} CACHE_TEST_CONTEXT;

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

        [Fact]
        void CacheSignatureTest()
        {
            HRESULT hr = S_OK;
            BURN_PACKAGE package = { };
            BURN_PAYLOAD payload = { };
            LPWSTR sczPayloadPath = NULL;
            BYTE* pb = NULL;
            DWORD cb = NULL;
            CACHE_TEST_CONTEXT context = { };

            try
            {
                pin_ptr<const wchar_t> dataDirectory = PtrToStringChars(this->TestContext->TestDirectory);
                hr = PathConcat(dataDirectory, L"TestData\\CacheTest\\CacheSignatureTest.File", &sczPayloadPath);
                Assert::True(S_OK == hr, "Failed to get path to test file.");
                Assert::True(FileExistsEx(sczPayloadPath, NULL), "Test file does not exist.");

                hr = StrAllocHexDecode(L"25e61cd83485062b70713aebddd3fe4992826cb121466fddc8de3eacb1e42f39d4bdd8455d95eec8c9529ced4c0296ab861931fe2c86df2f2b4e8d259a6d9223", &pb, &cb);
                Assert::Equal(S_OK, hr);

                package.fPerMachine = FALSE;
                package.sczCacheId = L"Bootstrapper.CacheTest.CacheSignatureTest";
                payload.sczKey = L"CacheSignatureTest.PayloadKey";
                payload.sczFilePath = L"CacheSignatureTest.File";
                payload.pbHash = pb;
                payload.cbHash = cb;

                hr = CacheCompletePayload(package.fPerMachine, &payload, package.sczCacheId, sczPayloadPath, FALSE, CacheTestEventRoutine, CacheTestProgressRoutine, &context);
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

static HRESULT CALLBACK CacheTestEventRoutine(
    __in BURN_CACHE_MESSAGE* /*pMessage*/,
    __in LPVOID /*pvContext*/
    )
{
    return S_OK;
}

static DWORD CALLBACK CacheTestProgressRoutine(
    __in LARGE_INTEGER /*TotalFileSize*/,
    __in LARGE_INTEGER /*TotalBytesTransferred*/,
    __in LARGE_INTEGER /*StreamSize*/,
    __in LARGE_INTEGER /*StreamBytesTransferred*/,
    __in DWORD /*dwStreamNumber*/,
    __in DWORD /*dwCallbackReason*/,
    __in HANDLE /*hSourceFile*/,
    __in HANDLE /*hDestinationFile*/,
    __in_opt LPVOID /*lpData*/
    )
{
    return PROGRESS_QUIET;
}
