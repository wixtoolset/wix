// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

namespace WixToolset
{
namespace Test
{
namespace Bootstrapper
{
    using namespace System;
    using namespace System::IO;
    using namespace Xunit;

    public ref class ApprovedExeTest : BurnUnitTest
    {
    public:
        ApprovedExeTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact]
        void ApprovedExesVerifyPFilesTest()
        {
            HRESULT hr = S_OK;
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            BURN_VARIABLES variables = { };
            LPWSTR scz = NULL;
            LPWSTR scz2 = NULL;

            try
            {
                hr = VariableInitialize(&variables);
                NativeAssert::Succeeded(hr, L"Failed to initialize variables.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");

                hr = VariableGetString(&variables, L"ProgramFilesFolder", &scz);
                NativeAssert::Succeeded(hr, "Failed to get variable ProgramFilesFolder.");

                hr = PathConcat(scz, L"a.exe", &scz2);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = ApprovedExesVerifySecureLocation(&cache, &variables, scz2, 0, NULL);
                NativeAssert::Succeeded(hr, "Failed to test secure location under ProgramFilesFolder");
                Assert::True((hr == S_OK), "Path under ProgramFilesFolder was expected to be safe");
            }
            finally
            {
                ReleaseStr(internalCommand.sczEngineWorkingDirectory);
                ReleaseStr(scz);
                ReleaseStr(scz2);

                CacheUninitialize(&cache);
                VariablesUninitialize(&variables);
            }
        }

        [Fact]
        void ApprovedExesVerifyPFilesWithRelativeTest()
        {
            HRESULT hr = S_OK;
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            BURN_VARIABLES variables = { };
            LPWSTR scz = NULL;
            LPWSTR scz2 = NULL;

            try
            {
                hr = VariableInitialize(&variables);
                NativeAssert::Succeeded(hr, L"Failed to initialize variables.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");
                cache.fPerMachineCacheRootVerified = TRUE;
                cache.fOriginalPerMachineCacheRootVerified = TRUE;

                hr = VariableGetString(&variables, L"ProgramFilesFolder", &scz);
                NativeAssert::Succeeded(hr, "Failed to get variable ProgramFilesFolder.");

                hr = PathConcat(scz, L"..\\a.exe", &scz2);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = ApprovedExesVerifySecureLocation(&cache, &variables, scz2, 0, NULL);
                NativeAssert::Succeeded(hr, "Failed to test secure location under ProgramFilesFolder");
                Assert::True((hr == S_FALSE), "Path pretending to be under ProgramFilesFolder was expected to be unsafe");
            }
            finally
            {
                ReleaseStr(internalCommand.sczEngineWorkingDirectory);
                ReleaseStr(scz);
                ReleaseStr(scz2);

                CacheUninitialize(&cache);
                VariablesUninitialize(&variables);
            }
        }

        [Fact]
        void ApprovedExesVerifyPFiles64Test()
        {
            HRESULT hr = S_OK;
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            BURN_VARIABLES variables = { };
            LPWSTR scz = NULL;
            LPWSTR scz2 = NULL;

            try
            {
                hr = VariableInitialize(&variables);
                NativeAssert::Succeeded(hr, L"Failed to initialize variables.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");

                hr = VariableGetString(&variables, L"ProgramFiles64Folder", &scz);
                NativeAssert::Succeeded(hr, "Failed to get variable ProgramFiles64Folder.");

                hr = PathConcat(scz, L"a.exe", &scz2);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = ApprovedExesVerifySecureLocation(&cache, &variables, scz2, 0, NULL);
                NativeAssert::Succeeded(hr, "Failed to test secure location under ProgramFiles64Folder");
                Assert::True((hr == S_OK), "Path under ProgramFiles64Folder was expected to be safe");
            }
            finally
            {
                ReleaseStr(internalCommand.sczEngineWorkingDirectory);
                ReleaseStr(scz);
                ReleaseStr(scz2);

                CacheUninitialize(&cache);
                VariablesUninitialize(&variables);
            }
        }

        [Fact]
        void ApprovedExesVerifySys64FolderTest()
        {
            HRESULT hr = S_OK;
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            BURN_VARIABLES variables = { };
            LPWSTR scz = NULL;
            LPWSTR scz2 = NULL;

            try
            {
                hr = VariableInitialize(&variables);
                NativeAssert::Succeeded(hr, L"Failed to initialize variables.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");
                cache.fPerMachineCacheRootVerified = TRUE;
                cache.fOriginalPerMachineCacheRootVerified = TRUE;

                hr = VariableGetString(&variables, L"System64Folder", &scz);
                NativeAssert::Succeeded(hr, "Failed to get variable System64Folder.");

                hr = PathConcat(scz, L"a.exe", &scz2);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = ApprovedExesVerifySecureLocation(&cache, &variables, scz2, 0, NULL);
                NativeAssert::Succeeded(hr, "Failed to test secure location under System64Folder");
                Assert::True((hr == S_FALSE), "Path under System64Folder was expected to be unsafe");
            }
            finally
            {
                ReleaseStr(internalCommand.sczEngineWorkingDirectory);
                ReleaseStr(scz);
                ReleaseStr(scz2);

                CacheUninitialize(&cache);
                VariablesUninitialize(&variables);
            }
        }

        [Fact]
        void ApprovedExesVerifySys64Rundll32UnsafeTest()
        {
            HRESULT hr = S_OK;
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            BURN_VARIABLES variables = { };
            LPWSTR scz = NULL;
            LPWSTR scz2 = NULL;
            LPWSTR szArgs = NULL;

            try
            {
                hr = VariableInitialize(&variables);
                NativeAssert::Succeeded(hr, L"Failed to initialize variables.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");
                cache.fPerMachineCacheRootVerified = TRUE;
                cache.fOriginalPerMachineCacheRootVerified = TRUE;

                hr = VariableGetString(&variables, L"System64Folder", &scz);
                NativeAssert::Succeeded(hr, "Failed to get variable System64Folder.");

                hr = PathConcat(scz, L"rundll32.exe", &scz2);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = ApprovedExesVerifySecureLocation(&cache, &variables, scz2, 1, const_cast<LPCWSTR*>(&scz2));
                NativeAssert::Succeeded(hr, "Failed to test secure location under System64Folder");
                Assert::True((hr == S_FALSE), "Path under System64Folder was expected to be unsafe for rundll32 target");
            }
            finally
            {
                ReleaseStr(internalCommand.sczEngineWorkingDirectory);
                ReleaseStr(scz);
                ReleaseStr(scz2);
                ReleaseStr(szArgs);

                CacheUninitialize(&cache);
                VariablesUninitialize(&variables);
            }
        }

        [Fact]
        void ApprovedExesVerifySys64Rundll32SafeTest()
        {
            HRESULT hr = S_OK;
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            BURN_VARIABLES variables = { };
            LPWSTR scz = NULL;
            LPWSTR scz2 = NULL;
            LPWSTR scz3 = NULL;

            try
            {
                hr = VariableInitialize(&variables);
                NativeAssert::Succeeded(hr, L"Failed to initialize variables.");

                hr = CacheInitialize(&cache, &internalCommand);
                NativeAssert::Succeeded(hr, "Failed to initialize cache.");
                cache.fPerMachineCacheRootVerified = TRUE;
                cache.fOriginalPerMachineCacheRootVerified = TRUE;

                // System64Folder
                hr = VariableGetString(&variables, L"System64Folder", &scz);
                NativeAssert::Succeeded(hr, "Failed to get variable System64Folder.");

                hr = PathConcat(scz, L"rundll32.exe", &scz2);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = VariableGetString(&variables, L"ProgramFiles64Folder", &scz);
                NativeAssert::Succeeded(hr, "Failed to get variable ProgramFiles64Folder.");

                hr = PathConcat(scz, L"a.dll", &scz3);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = ApprovedExesVerifySecureLocation(&cache, &variables, scz2, 1, const_cast<LPCWSTR*>(&scz3));
                NativeAssert::Succeeded(hr, "Failed to test secure location under ProgramFiles64Folder for System64Folder/rundll32 target");
                Assert::True((hr == S_OK), "Path under ProgramFiles64Folder was expected to be safe for System64Folder/rundll32 target");

                hr = PathConcat(scz, L"a.dll,somthing else", &scz3);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = ApprovedExesVerifySecureLocation(&cache, &variables, scz2, 1, const_cast<LPCWSTR*>(&scz3));
                NativeAssert::Succeeded(hr, "Failed to test secure location under ProgramFiles64Folder for rundll32 target");
                Assert::True((hr == S_OK), "Path under ProgramFiles64Folder was expected to be safe for System64Folder/rundll32 target");

                // SystemFolder
                hr = VariableGetString(&variables, L"SystemFolder", &scz);
                NativeAssert::Succeeded(hr, "Failed to get variable System64Folder.");

                hr = PathConcat(scz, L"rundll32.exe", &scz2);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = ApprovedExesVerifySecureLocation(&cache, &variables, scz2, 1, const_cast<LPCWSTR*>(&scz3));
                NativeAssert::Succeeded(hr, "Failed to test secure location under ProgramFiles64Folder for SystemFolder/rundll32 target");
                Assert::True((hr == S_OK), "Path under ProgramFiles64Folder was expected to be safe for SystemFolder/rundll32 target");

                // Sysnative
                hr = PathSystemWindowsSubdirectory(L"SysNative\\", &scz);
                NativeAssert::Succeeded(hr, "Failed to get SysNative Folder.");

                hr = PathConcat(scz, L"rundll32.exe", &scz2);
                NativeAssert::Succeeded(hr, "Failed to combine paths");

                hr = ApprovedExesVerifySecureLocation(&cache, &variables, scz2, 1, const_cast<LPCWSTR*>(&scz3));
                NativeAssert::Succeeded(hr, "Failed to test secure location under ProgramFiles64Folder for Sysnative/rundll32 target");
                Assert::True((hr == S_OK), "Path under ProgramFiles64Folder was expected to be safe for Sysnative/rundll32 target");
            }
            finally
            {
                ReleaseStr(internalCommand.sczEngineWorkingDirectory);
                ReleaseStr(scz);
                ReleaseStr(scz2);
                ReleaseStr(scz3);

                CacheUninitialize(&cache);
                VariablesUninitialize(&variables);
            }
        }
    };
}
}
}
