// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace System::Security::Principal;
using namespace Xunit;
using namespace WixBuildTools::TestSupport;

namespace DutilTests
{
    public ref class ProcUtil
    {
    public:
        [Fact]
        void ProcTokenUserTest()
        {
            HRESULT hr = S_OK;
            TOKEN_USER* pTokenUser = NULL;
            LPWSTR sczSid = NULL;

            try
            {
                hr = ProcTokenUser(::GetCurrentProcess(), &pTokenUser);
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
    };
}
