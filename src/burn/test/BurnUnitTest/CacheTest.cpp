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

namespace WixToolset
{
namespace Test
{
namespace Bootstrapper
{
    using namespace System;
    using namespace System::IO;
    using namespace Xunit;

    public ref class CacheTest : BurnUnitTest, IClassFixture<TestRegistryFixture^>
    {
    private:
        TestRegistryFixture^ testRegistry;
    public:
        CacheTest(BurnTestFixture^ fixture, TestRegistryFixture^ registryFixture) : BurnUnitTest(fixture)
        {
            this->testRegistry = registryFixture;
        }

        [Fact]
        void CacheElevatedTempFallbacksTest()
        {
            HRESULT hr = S_OK;
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            HKEY hkSystemEnvironment = NULL;
            HKEY hkBurnPolicy = NULL;

            internalCommand.fInitiallyElevated = TRUE;

            try
            {
                this->testRegistry->SetUp();

                // No registry keys, so should fallback to %windir%\TEMP.
                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");
                Assert::NotEqual<DWORD>(0, cache.cPotentialBaseWorkingFolders);
                VerifyBaseWorkingFolder(L"%windir%\\TEMP\\", cache.rgsczPotentialBaseWorkingFolders[0]);
                CacheUninitialize(&cache);

                hr = RegCreate(HKEY_LOCAL_MACHINE, L"System\\CurrentControlSet\\Control\\Session Manager\\Environment", GENERIC_WRITE, &hkSystemEnvironment);
                NativeAssert::Succeeded(hr, "Failed to create system environment key.");

                // Third fallback is system-level %TEMP%.
                hr = RegWriteExpandString(hkSystemEnvironment, L"TEMP", L"A:\\TEST\\TEMP");
                NativeAssert::Succeeded(hr, "Failed to write TEMP system environment value.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");
                Assert::NotEqual<DWORD>(0, cache.cPotentialBaseWorkingFolders);
                VerifyBaseWorkingFolder(L"A:\\TEST\\TEMP\\", cache.rgsczPotentialBaseWorkingFolders[0]);
                CacheUninitialize(&cache);

                // Second fallback is system-level %TMP%.
                hr = RegWriteString(hkSystemEnvironment, L"TMP", L"B:\\TEST\\TMP\\");
                NativeAssert::Succeeded(hr, "Failed to write TEMP system environment value.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");
                Assert::NotEqual<DWORD>(0, cache.cPotentialBaseWorkingFolders);
                VerifyBaseWorkingFolder(L"B:\\TEST\\TMP\\", cache.rgsczPotentialBaseWorkingFolders[0]);
                CacheUninitialize(&cache);

                // First fallback is impractical to mock out - %windir%\SystemTemp on Win11 when running as SYSTEM.

                hr = RegCreate(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Policies\\WiX\\Burn", GENERIC_WRITE, &hkBurnPolicy);
                NativeAssert::Succeeded(hr, "Failed to create Burn policy key.");

                // Default source is Burn policy.
                hr = RegWriteExpandString(hkBurnPolicy, L"EngineWorkingDirectory", L"D:\\TEST\\POLICY\\");
                NativeAssert::Succeeded(hr, "Failed to write EngineWorkingDirectory Burn policy value.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");
                Assert::NotEqual<DWORD>(0, cache.cPotentialBaseWorkingFolders);
                VerifyBaseWorkingFolder(L"D:\\TEST\\POLICY\\", cache.rgsczPotentialBaseWorkingFolders[0]);
                CacheUninitialize(&cache);

                // Command line parameter overrides everything else.
                hr = StrAllocString(&internalCommand.sczEngineWorkingDirectory, L"E:\\TEST\\COMMANDLINE\\", 0);
                NativeAssert::Succeeded(hr, "Failed to copy command line working directory.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");
                Assert::NotEqual<DWORD>(0, cache.cPotentialBaseWorkingFolders);
                VerifyBaseWorkingFolder(L"E:\\TEST\\COMMANDLINE\\", cache.rgsczPotentialBaseWorkingFolders[0]);
                CacheUninitialize(&cache);
            }
            finally
            {
                ReleaseRegKey(hkBurnPolicy);
                ReleaseRegKey(hkSystemEnvironment);
                ReleaseStr(internalCommand.sczEngineWorkingDirectory);

                CacheUninitialize(&cache);

                this->testRegistry->TearDown();
            }
        }

        void VerifyBaseWorkingFolder(LPCWSTR wzExpectedUnexpanded, LPCWSTR wzActual)
        {
            String^ expected = Environment::ExpandEnvironmentVariables(gcnew String(wzExpectedUnexpanded));
            WixAssert::StringEqual(expected, gcnew String(wzActual), true);
        }

        [Fact]
        void CacheSignatureTest()
        {
            HRESULT hr = S_OK;
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
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

                package.scope = BOOTSTRAPPER_PACKAGE_SCOPE_PER_USER;
                package.sczCacheId = L"Bootstrapper.CacheTest.CacheSignatureTest";
                payload.sczKey = L"CacheSignatureTest.PayloadKey";
                payload.sczFilePath = L"CacheSignatureTest.File";
                payload.pbHash = pb;
                payload.cbHash = cb;
                payload.qwFileSize = 27;
                payload.verification = BURN_PAYLOAD_VERIFICATION_HASH;

                hr = CacheInitialize(&cache, &internalCommand);
                TestThrowOnFailure(hr, L"Failed initialize cache.");

                hr = CacheCompletePayload(&cache, package.fPerMachine, &payload, package.sczCacheId, sczPayloadPath, FALSE, CacheTestEventRoutine, CacheTestProgressRoutine, &context);
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

                CacheUninitialize(&cache);
            }
        }
    };
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
