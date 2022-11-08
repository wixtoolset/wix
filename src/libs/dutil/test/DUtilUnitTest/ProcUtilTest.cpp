// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace System::Security::Principal;
using namespace Xunit;
using namespace WixInternal::TestSupport;
using namespace WixInternal::TestSupport::XunitExtensions;

namespace DutilTests
{
    public ref class ProcUtil
    {
    public:
        [Fact]
        void ProcGetTokenInformationTest()
        {
            HRESULT hr = S_OK;
            TOKEN_USER* pTokenUser = NULL;
            LPWSTR sczSid = NULL;

            try
            {
                hr = ProcGetTokenInformation(::GetCurrentProcess(), TokenUser, reinterpret_cast<LPVOID*>(&pTokenUser));
                NativeAssert::Succeeded(hr, "Failed to get TokenUser for current process.");

                if (!::ConvertSidToStringSidW(pTokenUser->User.Sid, &sczSid))
                {
                    hr = HRESULT_FROM_WIN32(::GetLastError());
                    NativeAssert::Succeeded(hr, "Failed to get string SID from TokenUser SID.");
                }

                Assert::Equal<String^>(WindowsIdentity::GetCurrent()->User->Value, gcnew String(sczSid));
            }
            finally
            {
                ReleaseMem(pTokenUser);
                ReleaseStr(sczSid);
            }
        }

        [SkippableFact]
        void ProcHasPrivilegeTest()
        {
            HRESULT hr = S_OK;
            BOOL fHasPrivilege = FALSE;

            hr = ProcHasPrivilege(::GetCurrentProcess(), SE_CREATE_TOKEN_NAME, &fHasPrivilege);
            NativeAssert::Succeeded(hr, "Failed to check privilege for current process.");

            if (fHasPrivilege)
            {
                WixAssert::Skip("Didn't expect process to have SE_CREATE_TOKEN_NAME privilege");
            }

            hr = ProcHasPrivilege(::GetCurrentProcess(), SE_INC_WORKING_SET_NAME, &fHasPrivilege);
            NativeAssert::Succeeded(hr, "Failed to check privilege for current process.");

            if (!fHasPrivilege)
            {
                WixAssert::Skip("Expected process to have SE_INC_WORKING_SET_NAME privilege");
            }
        }
    };
}
