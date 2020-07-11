// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#undef GetTempPath
#undef GetEnvironmentVariable

using namespace System;
using namespace Xunit;
using namespace WixTest;

namespace DutilTests
{
    typedef struct _VarUtilContext
    {
        DWORD dw;
        LPWSTR scz;
    } VarUtilContext;

    void FreeValueContext(LPVOID pvContext)
    {
        if (pvContext)
        {
            MemFree(pvContext);
        }
    }

    public ref class VarUtil
    {
    public:
        [NamedFact(Skip = "varutil Not Implemented Yet.")]
        void VarUtilBasicTest()
        {
            HRESULT hr = S_OK;
            VARIABLES_HANDLE pVariables = NULL;

            try
            {
                hr = VarCreate(&pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                // set variables
                VarSetStringHelper(pVariables, L"PROP1", L"VAL1");
                VarSetNumericHelper(pVariables, L"PROP2", 2);
                VarSetStringHelper(pVariables, L"PROP5", L"VAL5");
                VarSetStringHelper(pVariables, L"PROP3", L"VAL3");
                VarSetStringHelper(pVariables, L"PROP4", L"VAL4");
                VarSetStringHelper(pVariables, L"PROP6", L"VAL6");
                VarSetStringHelper(pVariables, L"PROP7", L"7");
                VarSetVersionHelper(pVariables, L"PROP8", MAKEQWORDVERSION(1, 1, 0, 0));

                // set overwritten variables
                VarSetStringHelper(pVariables, L"OVERWRITTEN_STRING", L"ORIGINAL");
                VarSetNumericHelper(pVariables, L"OVERWRITTEN_STRING", 42);

                VarSetNumericHelper(pVariables, L"OVERWRITTEN_NUMBER", 5);
                VarSetStringHelper(pVariables, L"OVERWRITTEN_NUMBER", L"NEW");

                // get and verify variable values
                VarGetStringHelper(pVariables, L"PROP1", L"VAL1");
                VarGetNumericHelper(pVariables, L"PROP2", 2);
                VarGetStringHelper(pVariables, L"PROP2", L"2");
                VarGetStringHelper(pVariables, L"PROP3", L"VAL3");
                VarGetStringHelper(pVariables, L"PROP4", L"VAL4");
                VarGetStringHelper(pVariables, L"PROP5", L"VAL5");
                VarGetStringHelper(pVariables, L"PROP6", L"VAL6");
                VarGetNumericHelper(pVariables, L"PROP7", 7);
                VarGetVersionHelper(pVariables, L"PROP8", MAKEQWORDVERSION(1, 1, 0, 0));
                VarGetStringHelper(pVariables, L"PROP8", L"1.1.0.0");

                VarGetNumericHelper(pVariables, L"OVERWRITTEN_STRING", 42);
                VarGetStringHelper(pVariables, L"OVERWRITTEN_NUMBER", L"NEW");
            }
            finally
            {
                ReleaseVariables(pVariables);
            }
        }

        [NamedFact(Skip = "varutil Not Implemented Yet.")]
        void VarUtilFormatTest()
        {
            HRESULT hr = S_OK;
            VARIABLES_HANDLE pVariables = NULL;
            LPWSTR scz = NULL;
            DWORD cch = 0;
            try
            {
                hr = VarCreate(&pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                // set variables
                VarSetStringHelper(pVariables, L"PROP1", L"VAL1");
                VarSetStringHelper(pVariables, L"PROP2", L"VAL2");
                VarSetNumericHelper(pVariables, L"PROP3", 3);

                // test string formatting
                VarFormatStringHelper(pVariables, L"NOPROP", L"NOPROP");
                VarFormatStringHelper(pVariables, L"[PROP1]", L"VAL1");
                VarFormatStringHelper(pVariables, L" [PROP1] ", L" VAL1 ");
                VarFormatStringHelper(pVariables, L"PRE [PROP1]", L"PRE VAL1");
                VarFormatStringHelper(pVariables, L"[PROP1] POST", L"VAL1 POST");
                VarFormatStringHelper(pVariables, L"PRE [PROP1] POST", L"PRE VAL1 POST");
                VarFormatStringHelper(pVariables, L"[PROP1] MID [PROP2]", L"VAL1 MID VAL2");
                VarFormatStringHelper(pVariables, L"[NONE]", L"");
                VarFormatStringHelper(pVariables, L"[prop1]", L"");
                VarFormatStringHelper(pVariables, L"[\\[]", L"[");
                VarFormatStringHelper(pVariables, L"[\\]]", L"]");
                VarFormatStringHelper(pVariables, L"[]", L"[]");
                VarFormatStringHelper(pVariables, L"[NONE", L"[NONE");
                VarGetFormattedHelper(pVariables, L"PROP2", L"VAL2");
                VarGetFormattedHelper(pVariables, L"PROP3", L"3");

                hr = VarFormatString(pVariables, L"PRE [PROP1] POST", &scz, &cch);
                NativeAssert::Succeeded(hr, "Failed to format string.");

                Assert::Equal<DWORD>(lstrlenW(scz), cch);

                hr = VarFormatString(pVariables, L"PRE [PROP1] POST", NULL, &cch);
                NativeAssert::Succeeded(hr, "Failed to format string.");

                Assert::Equal<DWORD>(lstrlenW(scz), cch);
            }
            finally
            {
                ReleaseVariables(pVariables);
                ReleaseStr(scz);
            }
        }

        [NamedFact(Skip = "varutil Not Implemented Yet.")]
        void VarUtilEscapeTest()
        {
            // test string escaping
            VarEscapeStringHelper(L"[", L"[\\[]");
            VarEscapeStringHelper(L"]", L"[\\]]");
            VarEscapeStringHelper(L" [TEXT] ", L" [\\[]TEXT[\\]] ");
        }

        [NamedFact(Skip = "varutil Not Implemented Yet.")]
        void VarUtilConditionTest()
        {
            HRESULT hr = S_OK;
            VARIABLES_HANDLE pVariables = NULL;

            try
            {
                hr = VarCreate(&pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                // set variables
                VarSetStringHelper(pVariables, L"PROP1", L"VAL1");
                VarSetStringHelper(pVariables, L"PROP2", L"VAL2");
                VarSetStringHelper(pVariables, L"PROP3", L"VAL3");
                VarSetStringHelper(pVariables, L"PROP4", L"BEGIN MID END");
                VarSetNumericHelper(pVariables, L"PROP5", 5);
                VarSetNumericHelper(pVariables, L"PROP6", 6);
                VarSetStringHelper(pVariables, L"PROP7", L"");
                VarSetNumericHelper(pVariables, L"PROP8", 0);
                VarSetStringHelper(pVariables, L"_PROP9", L"VAL9");
                VarSetNumericHelper(pVariables, L"PROP10", -10);
                VarSetNumericHelper(pVariables, L"PROP11", 9223372036854775807ll);
                VarSetNumericHelper(pVariables, L"PROP12", -9223372036854775808ll);
                VarSetNumericHelper(pVariables, L"PROP13", 0x00010000);
                VarSetNumericHelper(pVariables, L"PROP14", 0x00000001);
                VarSetNumericHelper(pVariables, L"PROP15", 0x00010001);
                VarSetVersionHelper(pVariables, L"PROP16", MAKEQWORDVERSION(0, 0, 0, 0));
                VarSetVersionHelper(pVariables, L"PROP17", MAKEQWORDVERSION(1, 0, 0, 0));
                VarSetVersionHelper(pVariables, L"PROP18", MAKEQWORDVERSION(1, 1, 0, 0));
                VarSetVersionHelper(pVariables, L"PROP19", MAKEQWORDVERSION(1, 1, 1, 0));
                VarSetVersionHelper(pVariables, L"PROP20", MAKEQWORDVERSION(1, 1, 1, 1));
                VarSetNumericHelper(pVariables, L"vPROP21", 1);
                VarSetVersionHelper(pVariables, L"PROP22", MAKEQWORDVERSION(65535, 65535, 65535, 65535));
                VarSetStringHelper(pVariables, L"PROP23", L"1.1.1");

                // test conditions
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP5"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP7"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP8"));
                Assert::True(EvaluateConditionHelper(pVariables, L"_PROP9"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP16"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP17"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 = \"VAL1\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"NONE = \"NOT\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP1 <> \"VAL1\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"NONE <> \"NOT\""));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 ~= \"val1\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP1 = \"val1\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP1 ~<> \"val1\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 <> \"val1\""));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP5 = 5"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP5 = 0"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP5 <> 5"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP5 <> 0"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP10 = -10"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP10 <> -10"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP17 = v1"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP17 = v0"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP17 <> v1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP17 <> v0"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP16 = v0"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP17 = v1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP18 = v1.1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP19 = v1.1.1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP20 = v1.1.1.1"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"PROP20 = v1.1.1.1.0"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"PROP20 = v1.1.1.1.1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"vPROP21 = 1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP23 = v1.1.1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"v1.1.1 = PROP23"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 <> v1.1.1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"v1.1.1 <> PROP1"));

                Assert::False(EvaluateConditionHelper(pVariables, L"PROP11 = 9223372036854775806"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP11 = 9223372036854775807"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"PROP11 = 9223372036854775808"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"PROP11 = 92233720368547758070000"));

                Assert::False(EvaluateConditionHelper(pVariables, L"PROP12 = -9223372036854775807"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP12 = -9223372036854775808"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"PROP12 = -9223372036854775809"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"PROP12 = -92233720368547758080000"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP22 = v65535.65535.65535.65535"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"PROP22 = v65536.65535.65535.65535"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"PROP22 = v65535.655350000.65535.65535"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP5 < 6"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP5 < 5"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP5 > 4"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP5 > 5"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP5 <= 6"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP5 <= 5"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP5 <= 4"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP5 >= 4"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP5 >= 5"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP5 >= 6"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP4 << \"BEGIN\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP4 << \"END\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP4 >> \"END\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP4 >> \"BEGIN\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP4 >< \"MID\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP4 >< \"NONE\""));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP16 < v1.1"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP16 < v0"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP17 > v0.12"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP17 > v1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP18 >= v1.0"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP18 >= v1.1"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP18 >= v2.1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP19 <= v1.1234.1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP19 <= v1.1.1"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP19 <= v1.0.123"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP6 = \"6\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"\"6\" = PROP6"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP6 = \"ABC\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"\"ABC\" = PROP6"));
                Assert::False(EvaluateConditionHelper(pVariables, L"\"ABC\" = PROP6"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP13 << 1"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP13 << 0"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP14 >> 1"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP14 >> 0"));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP15 >< 65537"));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP15 >< 0"));

                Assert::False(EvaluateConditionHelper(pVariables, L"NOT PROP1"));
                Assert::True(EvaluateConditionHelper(pVariables, L"NOT (PROP1 <> \"VAL1\")"));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"VAL2\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"NOT\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP1 = \"NOT\" AND PROP2 = \"VAL2\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP1 = \"NOT\" AND PROP2 = \"NOT\""));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 = \"VAL1\" OR PROP2 = \"VAL2\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 = \"VAL1\" OR PROP2 = \"NOT\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 = \"NOT\" OR PROP2 = \"VAL2\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP1 = \"NOT\" OR PROP2 = \"NOT\""));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"VAL2\" OR PROP3 = \"NOT\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"NOT\" OR PROP3 = \"VAL3\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP1 = \"VAL1\" AND PROP2 = \"NOT\" OR PROP3 = \"NOT\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP1 = \"VAL1\" AND (PROP2 = \"NOT\" OR PROP3 = \"VAL3\")"));
                Assert::True(EvaluateConditionHelper(pVariables, L"(PROP1 = \"VAL1\" AND PROP2 = \"VAL2\") OR PROP3 = \"NOT\""));

                Assert::True(EvaluateConditionHelper(pVariables, L"PROP3 = \"NOT\" OR PROP1 = \"VAL1\" AND PROP2 = \"VAL2\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP3 = \"VAL3\" OR PROP1 = \"VAL1\" AND PROP2 = \"NOT\""));
                Assert::False(EvaluateConditionHelper(pVariables, L"PROP3 = \"NOT\" OR PROP1 = \"VAL1\" AND PROP2 = \"NOT\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"(PROP3 = \"NOT\" OR PROP1 = \"VAL1\") AND PROP2 = \"VAL2\""));
                Assert::True(EvaluateConditionHelper(pVariables, L"PROP3 = \"NOT\" OR (PROP1 = \"VAL1\" AND PROP2 = \"VAL2\")"));

                Assert::True(EvaluateFailureConditionHelper(pVariables, L"="));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"(PROP1"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"(PROP1 = \""));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"1A"));
                Assert::True(EvaluateFailureConditionHelper(pVariables, L"*"));

                Assert::True(EvaluateFailureConditionHelper(pVariables, L"1 == 1"));
            }
            finally
            {
                ReleaseVariables(pVariables);
            }
        }

        [NamedFact(Skip = "varutil Not Implemented Yet.")]
        void VarUtilValueTest()
        {
            HRESULT hr = S_OK;
            VARIABLES_HANDLE pVariables = NULL;
            VARIABLE_VALUE values[8];

            try
            {
                hr = VarCreate(&pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                // set variables
                InitNumericValue(pVariables, values + 0, 2, FALSE, 1, L"PROP1");
                InitStringValue(pVariables, values + 1, L"VAL2", FALSE, 2, L"PROP2");
                InitVersionValue(pVariables, values + 2, MAKEQWORDVERSION(1, 1, 0, 0), FALSE, 3, L"PROP3");
                InitNoneValue(pVariables, values + 3, FALSE, 4, L"PROP4");
                InitNoneValue(pVariables, values + 4, TRUE, 5, L"PROP5");
                InitVersionValue(pVariables, values + 5, MAKEQWORDVERSION(1, 1, 1, 0), TRUE, 6, L"PROP6");
                InitStringValue(pVariables, values + 6, L"7", TRUE, 7, L"PROP7");
                InitNumericValue(pVariables, values + 7, 11, TRUE, 8, L"PROP8");

                for (DWORD i = 0; i < 8; i++)
                {
                    VerifyValue(pVariables, values + i);
                }
            }
            finally
            {
                VarDestroy(pVariables, FreeValueContext);
            }
        }

        [NamedFact(Skip = "varutil Not Implemented Yet.")]
        void VarUtilEnumTest()
        {
            HRESULT hr = S_OK;
            const DWORD dwIndex = 8;
            VARIABLES_HANDLE pVariables = NULL;
            VARIABLE_ENUM_HANDLE pEnum = NULL;
            VARIABLE_VALUE values[dwIndex];
            VARIABLE_VALUE* pValue = NULL;

            try
            {
                hr = VarCreate(&pVariables);
                NativeAssert::Succeeded(hr, "Failed to initialize variables.");

                hr = VarStartEnum(pVariables, &pEnum, &pValue);
                NativeAssert::ValidReturnCode(hr, E_NOMOREITEMS);

                // set variables
                InitNumericValue(pVariables, values + 0, 2, FALSE, 0, L"PROP1");
                InitStringValue(pVariables, values + 1, L"VAL2", FALSE, 0, L"PROP2");
                InitVersionValue(pVariables, values + 2, MAKEQWORDVERSION(1, 1, 0, 0), FALSE, 0, L"PROP3");
                InitNoneValue(pVariables, values + 3, FALSE, 0, L"PROP4");
                InitNoneValue(pVariables, values + 4, TRUE, 0, L"PROP5");
                InitVersionValue(pVariables, values + 5, MAKEQWORDVERSION(1, 1, 1, 0), TRUE, 0, L"PROP6");
                InitStringValue(pVariables, values + 6, L"7", TRUE, 0, L"PROP7");
                InitNumericValue(pVariables, values + 7, 11, TRUE, 0, L"PROP8");

                hr = VarStartEnum(pVariables, &pEnum, &pValue);
                
                for (DWORD i = dwIndex - 1; i; --i)
                {
                    NativeAssert::ValidReturnCode(hr, S_OK);

                    VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(pValue->pvContext);
                    pContext->dw += 1;

                    hr = VarNextVariable(pEnum, &pValue);
                }

                NativeAssert::ValidReturnCode(hr, E_NOMOREITEMS);

                for (DWORD j = 0; j < dwIndex; j++)
                {
                    VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(values[j].pvContext);
                    NativeAssert::Equal<DWORD>(1, pContext->dw);
                }

                VarFinishEnum(pEnum);
                pEnum = NULL;

                hr = VarStartEnum(pVariables, &pEnum, &pValue);

                for (DWORD i = dwIndex - 1; i; --i)
                {
                    NativeAssert::ValidReturnCode(hr, S_OK);

                    VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(pValue->pvContext);
                    pContext->dw += 1;

                    hr = VarNextVariable(pEnum, &pValue);
                }

                NativeAssert::ValidReturnCode(hr, E_NOMOREITEMS);

                for (DWORD j = 0; j < dwIndex; j++)
                {
                    VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(values[j].pvContext);
                    NativeAssert::Equal<DWORD>(2, pContext->dw);
                }
            }
            finally
            {
                VarFinishEnum(pEnum);
                ReleaseVariableValue(pValue);
                VarDestroy(pVariables, FreeValueContext);
            }
        }

    private:
        void InitNoneValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pValue, BOOL fHidden, DWORD dw, LPCWSTR wz)
        {
            pValue->type = VARIABLE_VALUE_TYPE_NONE;
            pValue->fHidden = fHidden;

            InitValueContext(pValue, dw, wz);

            HRESULT hr = VarSetValue(pVariables, wz, pValue);
            NativeAssert::Succeeded(hr, "Failed to set value for variable {0}", wz);
        }

        void InitNumericValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pValue, LONGLONG llValue, BOOL fHidden, DWORD dw, LPCWSTR wz)
        {
            pValue->type = VARIABLE_VALUE_TYPE_NUMERIC;
            pValue->fHidden = fHidden;

            pValue->llValue = llValue;

            InitValueContext(pValue, dw, wz);

            HRESULT hr = VarSetValue(pVariables, wz, pValue);
            NativeAssert::Succeeded(hr, "Failed to set value for variable {0}", wz);
        }

        void InitStringValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pValue, LPWSTR wzValue, BOOL fHidden, DWORD dw, LPCWSTR wz)
        {
            pValue->type = VARIABLE_VALUE_TYPE_STRING;
            pValue->fHidden = fHidden;

            HRESULT hr = StrAllocString(&pValue->sczValue, wzValue, 0);
            NativeAssert::Succeeded(hr, "Failed to alloc string: {0}", wzValue);

            InitValueContext(pValue, dw, wz);

            hr = VarSetValue(pVariables, wz, pValue);
            NativeAssert::Succeeded(hr, "Failed to set value for variable {0}", wz);
        }

        void InitVersionValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pValue, DWORD64 qwValue, BOOL fHidden, DWORD dw, LPCWSTR wz)
        {
            pValue->type = VARIABLE_VALUE_TYPE_VERSION;
            pValue->fHidden = fHidden;

            pValue->qwValue = qwValue;

            InitValueContext(pValue, dw, wz);

            HRESULT hr = VarSetValue(pVariables, wz, pValue);
            NativeAssert::Succeeded(hr, "Failed to set value for variable {0}", wz);
        }

        void InitValueContext(VARIABLE_VALUE* pValue, DWORD dw, LPCWSTR wz)
        {
            pValue->pvContext = MemAlloc(sizeof(VarUtilContext), TRUE);
            VarUtilContext* pContext = reinterpret_cast<VarUtilContext*>(pValue->pvContext);
            if (!pContext)
            {
                throw gcnew OutOfMemoryException();
            }

            pContext->dw = dw;

            HRESULT hr = StrAllocString(&pContext->scz, wz, 0);
            NativeAssert::Succeeded(hr, "Failed to alloc string: {0}", wz);
        }

        void VerifyValue(VARIABLES_HANDLE pVariables, VARIABLE_VALUE* pExpectedValue)
        {
            VARIABLE_VALUE* pActualValue = NULL;

            try
            {
                VarUtilContext* pExpectedContext = reinterpret_cast<VarUtilContext*>(pExpectedValue->pvContext);
                NativeAssert::True(NULL != pExpectedContext);

                HRESULT hr = VarGetValue(pVariables, pExpectedContext->scz, &pActualValue);
                NativeAssert::Succeeded(hr, "Failed to get value: {0}", pExpectedContext->scz);

                NativeAssert::Equal<DWORD>(pExpectedValue->type, pActualValue->type);
                NativeAssert::InRange<DWORD>(pExpectedValue->type, VARIABLE_VALUE_TYPE_NONE, VARIABLE_VALUE_TYPE_STRING);

                switch (pExpectedValue->type)
                {
                case VARIABLE_VALUE_TYPE_NONE:
                case VARIABLE_VALUE_TYPE_VERSION:
                    NativeAssert::Equal(pExpectedValue->qwValue, pActualValue->qwValue);
                    break;
                case VARIABLE_VALUE_TYPE_NUMERIC:
                    NativeAssert::Equal(pExpectedValue->llValue, pActualValue->llValue);
                    break;
                case VARIABLE_VALUE_TYPE_STRING:
                    NativeAssert::StringEqual(pExpectedValue->sczValue, pActualValue->sczValue);
                    break;
                }

                NativeAssert::Equal(pExpectedValue->fHidden, pActualValue->fHidden);
                NativeAssert::True(pExpectedValue->pvContext == pActualValue->pvContext);
            }
            finally
            {
                ReleaseVariableValue(pActualValue);
            }
        }
    };
}
