// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

namespace DutilTests
{
    using namespace System;
    using namespace Xunit;
    using namespace WixTest;

    public ref class CondUtil
    {
    public:
        [NamedFact(Skip = "condutil Not Implemented Yet.")]
        void CondEvaluateTest()
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
    };
}
