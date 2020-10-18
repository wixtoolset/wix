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

    public ref class VariantTest : BurnUnitTest
    {
    public:
        VariantTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact]
        void VariantBasicTest()
        {
            BURN_VARIANT expectedVariants[10];
            BURN_VARIANT actualVariants[10];
            for (DWORD i = 0; i < 10; i++)
            {
                BVariantUninitialize(expectedVariants + i);
                BVariantUninitialize(actualVariants + i);
            }

            try
            {
                InitNumericValue(expectedVariants + 0, 2, FALSE, L"PROP1", actualVariants + 0);
                InitStringValue(expectedVariants + 1, L"VAL2", FALSE, L"PROP2", actualVariants + 1);
                InitVersionValue(expectedVariants + 2, L"1.1.0.0", FALSE, L"PROP3", actualVariants + 2);
                InitNoneValue(expectedVariants + 3, FALSE, L"PROP4", actualVariants + 3);
                InitNoneValue(expectedVariants + 4, TRUE, L"PROP5", actualVariants + 4);
                InitVersionValue(expectedVariants + 5, L"1.1.1.0", TRUE, L"PROP6", actualVariants + 5);
                InitStringValue(expectedVariants + 6, L"7", TRUE, L"PROP7", actualVariants + 6);
                InitNumericValue(expectedVariants + 7, 11, TRUE, L"PROP8", actualVariants + 7);
                InitFormattedValue(expectedVariants + 8, L"VAL9", FALSE, L"PROP9", actualVariants + 8);
                InitFormattedValue(expectedVariants + 9, L"VAL10", TRUE, L"PROP10", actualVariants + 9);

                VerifyNumericValue(expectedVariants + 0, actualVariants + 0);
                VerifyStringValue(expectedVariants + 1, actualVariants + 1);
                VerifyVersionValue(expectedVariants + 2, actualVariants + 2);
                VerifyNoneValue(expectedVariants + 3, actualVariants + 3);
                VerifyNoneValue(expectedVariants + 4, actualVariants + 4);
                VerifyVersionValue(expectedVariants + 5, actualVariants + 5);
                VerifyStringValue(expectedVariants + 6, actualVariants + 6);
                VerifyNumericValue(expectedVariants + 7, actualVariants + 7);
                VerifyFormattedValue(expectedVariants + 8, actualVariants + 8);
                VerifyFormattedValue(expectedVariants + 9, actualVariants + 9);
            }
            finally
            {
                for (DWORD i = 0; i < 10; i++)
                {
                    BVariantUninitialize(expectedVariants + i);
                    BVariantUninitialize(actualVariants + i);
                }
            }
        }

    private:
        void InitFormattedValue(BURN_VARIANT* pValue, LPWSTR wzValue, BOOL fHidden, LPCWSTR wz, BURN_VARIANT* pActualValue)
        {
            HRESULT hr = S_OK;
            pValue->Type = BURN_VARIANT_TYPE_FORMATTED;

            hr = StrAllocString(&pValue->sczValue, wzValue, 0);
            NativeAssert::Succeeded(hr, "Failed to alloc string: {0}", wzValue);

            hr = BVariantCopy(pValue, pActualValue);
            NativeAssert::Succeeded(hr, "Failed to copy variant {0}", wz);

            if (fHidden)
            {
                hr = BVariantSetEncryption(pActualValue, TRUE);
                NativeAssert::Succeeded(hr, "Failed to encrypt variant {0}", wz);

                NativeAssert::True(pActualValue->fEncryptString);
            }
        }

        void InitNoneValue(BURN_VARIANT* pValue, BOOL fHidden, LPCWSTR wz, BURN_VARIANT* pActualValue)
        {
            HRESULT hr = S_OK;
            pValue->Type = BURN_VARIANT_TYPE_NONE;

            hr = BVariantCopy(pValue, pActualValue);
            NativeAssert::Succeeded(hr, "Failed to copy variant {0}", wz);

            if (fHidden)
            {
                hr = BVariantSetEncryption(pActualValue, TRUE);
                NativeAssert::Succeeded(hr, "Failed to encrypt variant {0}", wz);

                NativeAssert::True(pActualValue->fEncryptString);
            }
        }

        void InitNumericValue(BURN_VARIANT* pValue, LONGLONG llValue, BOOL fHidden, LPCWSTR wz, BURN_VARIANT* pActualValue)
        {
            HRESULT hr = S_OK;
            pValue->Type = BURN_VARIANT_TYPE_NUMERIC;
            pValue->llValue = llValue;

            hr = BVariantCopy(pValue, pActualValue);
            NativeAssert::Succeeded(hr, "Failed to copy variant {0}", wz);

            if (fHidden)
            {
                hr = BVariantSetEncryption(pActualValue, TRUE);
                NativeAssert::Succeeded(hr, "Failed to encrypt variant {0}", wz);

                NativeAssert::True(pActualValue->fEncryptString);
            }
        }

        void InitStringValue(BURN_VARIANT* pValue, LPWSTR wzValue, BOOL fHidden, LPCWSTR wz, BURN_VARIANT* pActualValue)
        {
            HRESULT hr = S_OK;
            pValue->Type = BURN_VARIANT_TYPE_STRING;

            hr = StrAllocString(&pValue->sczValue, wzValue, 0);
            NativeAssert::Succeeded(hr, "Failed to alloc string: {0}", wzValue);

            hr = BVariantCopy(pValue, pActualValue);
            NativeAssert::Succeeded(hr, "Failed to copy variant {0}", wz);

            if (fHidden)
            {
                hr = BVariantSetEncryption(pActualValue, TRUE);
                NativeAssert::Succeeded(hr, "Failed to encrypt variant {0}", wz);

                NativeAssert::True(pActualValue->fEncryptString);
            }
        }

        void InitVersionValue(BURN_VARIANT* pValue, LPCWSTR wzValue, BOOL fHidden, LPCWSTR wz, BURN_VARIANT* pActualValue)
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pVersion = NULL;

            try
            {
                hr = VerParseVersion(wzValue, 0, FALSE, &pVersion);
                NativeAssert::Succeeded(hr, "Failed to parse version {0}", wzValue);

                pValue->Type = BURN_VARIANT_TYPE_VERSION;
                pValue->pValue = pVersion;
                pVersion = NULL;

                hr = BVariantCopy(pValue, pActualValue);
                NativeAssert::Succeeded(hr, "Failed to copy variant {0}", wz);

                if (fHidden)
                {
                    hr = BVariantSetEncryption(pActualValue, TRUE);
                    NativeAssert::Succeeded(hr, "Failed to encrypt variant {0}", wz);

                    NativeAssert::True(pActualValue->fEncryptString);
                }
            }
            finally
            {
                ReleaseVerutilVersion(pVersion);
            }
        }

        void VerifyFormattedValue(BURN_VARIANT* pExpectedValue, BURN_VARIANT* pActualValue)
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_FORMATTED, pExpectedValue->Type);
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_FORMATTED, pActualValue->Type);

            try
            {
                hr = BVariantGetString(pActualValue, &sczValue);
                NativeAssert::Succeeded(hr, "Failed to get string value");

                NativeAssert::StringEqual(pExpectedValue->sczValue, sczValue);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        void VerifyNumericValue(BURN_VARIANT* pExpectedValue, BURN_VARIANT* pActualValue)
        {
            HRESULT hr = S_OK;
            LONGLONG llValue = 0;
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_NUMERIC, pExpectedValue->Type);
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_NUMERIC, pActualValue->Type);

            hr = BVariantGetNumeric(pActualValue, &llValue);
            NativeAssert::Succeeded(hr, "Failed to get numeric value");

            NativeAssert::Equal<LONGLONG>(pExpectedValue->llValue, llValue);
        }

        void VerifyNoneValue(BURN_VARIANT* pExpectedValue, BURN_VARIANT* pActualValue)
        {
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_NONE, pExpectedValue->Type);
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_NONE, pActualValue->Type);
            NativeAssert::Equal<LONGLONG>(pExpectedValue->llValue, pActualValue->llValue);
        }

        void VerifyStringValue(BURN_VARIANT* pExpectedValue, BURN_VARIANT* pActualValue)
        {
            HRESULT hr = S_OK;
            LPWSTR sczValue = NULL;
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_STRING, pExpectedValue->Type);
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_STRING, pActualValue->Type);

            try
            {
                hr = BVariantGetString(pActualValue, &sczValue);
                NativeAssert::Succeeded(hr, "Failed to get string value");

                NativeAssert::StringEqual(pExpectedValue->sczValue, sczValue);
            }
            finally
            {
                ReleaseStr(sczValue);
            }
        }

        void VerifyVersionValue(BURN_VARIANT* pExpectedValue, BURN_VARIANT* pActualValue)
        {
            HRESULT hr = S_OK;
            VERUTIL_VERSION* pValue = NULL;
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_VERSION, pExpectedValue->Type);
            NativeAssert::Equal<DWORD>(BURN_VARIANT_TYPE_VERSION, pActualValue->Type);

            try
            {
                hr = BVariantGetVersion(pActualValue, &pValue);
                NativeAssert::Succeeded(hr, "Failed to get version value");

                NativeAssert::StringEqual(pExpectedValue->pValue->sczVersion, pActualValue->pValue->sczVersion);
            }
            finally
            {
                ReleaseVerutilVersion(pValue);
            }
        }
    };
}
}
}
}
}
