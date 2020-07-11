#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


namespace WixTest {

    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Linq;
    using namespace Xunit;

    public ref class NativeAssert : WixAssert
    {
    public:
        static void NotNull(LPCWSTR wz)
        {
            if (!wz)
            {
                Assert::NotNull(nullptr);
            }
        }

        // For some reason, naming these NotStringEqual methods "NotEqual" breaks Intellisense in files that call any overload of the NotEqual method.
        static void NotStringEqual(LPCWSTR expected, LPCWSTR actual)
        {
            NativeAssert::NotStringEqual(expected, actual, FALSE);
        }

        static void NotStringEqual(LPCWSTR expected, LPCWSTR actual, BOOL ignoreCase)
        {
            IEqualityComparer<String^>^ comparer = ignoreCase ? StringComparer::InvariantCultureIgnoreCase : StringComparer::InvariantCulture;
            Assert::NotEqual(NativeAssert::LPWSTRToString(expected), NativeAssert::LPWSTRToString(actual), comparer);
        }

        // For some reason, naming these StringEqual methods "Equal" breaks Intellisense in files that call any overload of the Equal method.
        static void StringEqual(LPCWSTR expected, LPCWSTR actual)
        {
            NativeAssert::StringEqual(expected, actual, FALSE);
        }

        static void StringEqual(LPCWSTR expected, LPCWSTR actual, BOOL ignoreCase)
        {
            IEqualityComparer<String^>^ comparer = ignoreCase ? StringComparer::InvariantCultureIgnoreCase : StringComparer::InvariantCulture;
            Assert::Equal(NativeAssert::LPWSTRToString(expected), NativeAssert::LPWSTRToString(actual), comparer);
        }

        static void Succeeded(HRESULT hr, LPCSTR zFormat, LPCSTR zArg, ... array<LPCSTR>^ zArgs)
        {
            array<Object^>^ formatArgs = gcnew array<Object^, 1>(zArgs->Length + 1);
            formatArgs[0] = NativeAssert::LPSTRToString(zArg);
            for (int i = 0; i < zArgs->Length; ++i)
            {
                formatArgs[i + 1] = NativeAssert::LPSTRToString(zArgs[i]);
            }
            WixAssert::Succeeded(hr, gcnew String(zFormat), formatArgs);
        }

        static void Succeeded(HRESULT hr, LPCSTR zFormat, ... array<LPCWSTR>^ wzArgs)
        {
            array<Object^>^ formatArgs = gcnew array<Object^, 1>(wzArgs->Length);
            for (int i = 0; i < wzArgs->Length; ++i)
            {
                formatArgs[i] = NativeAssert::LPWSTRToString(wzArgs[i]);
            }
            WixAssert::Succeeded(hr, gcnew String(zFormat), formatArgs);
        }

        static void ValidReturnCode(HRESULT hr, ... array<HRESULT>^ validReturnCodes)
        {
            Assert::Contains(hr, (IEnumerable<HRESULT>^)validReturnCodes);
        }

    private:
        static String^ LPSTRToString(LPCSTR z)
        {
            return z ? gcnew String(z) : nullptr;
        }
        static String^ LPWSTRToString(LPCWSTR wz)
        {
            return wz ? gcnew String(wz) : nullptr;
        }
    };
}
