// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixTest;

namespace DutilTests
{
    struct ArrayValue
    {
        DWORD dwNum;
        void *pvNull1;
        LPWSTR sczString;
        void *pvNull2;
    };

    public ref class MemUtil
    {
    public:
        [Fact]
        void MemUtilAppendTest()
        {
            HRESULT hr = S_OK;
            DWORD dwSize;
            ArrayValue *rgValues = NULL;
            DWORD cValues = 0;

            try
            {
                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 1");
                ++cValues;
                SetItem(rgValues + 0, 0);

                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 2");
                ++cValues;
                SetItem(rgValues + 1, 1);

                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 3");
                ++cValues;
                SetItem(rgValues + 2, 2);

                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 4");
                ++cValues;
                SetItem(rgValues + 3, 3);

                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 5");
                ++cValues;
                SetItem(rgValues + 4, 4);

                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 6");
                ++cValues;
                SetItem(rgValues + 5, 5);

                // OK, we used growth size 5, so let's try ensuring we have space for 6 (5 + first item) items
                // and make sure it doesn't grow since we already have enough space
                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to ensure array size matches what it should already be");
                dwSize = MemSize(rgValues);
                if (dwSize != 6 * sizeof(ArrayValue))
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "MemEnsureArraySize is growing an array that is already big enough!");
                }

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 7");
                ++cValues;
                SetItem(rgValues + 6, 6);

                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 7");
                ++cValues;
                SetItem(rgValues + 7, 7);

                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 7");
                ++cValues;
                SetItem(rgValues + 8, 8);

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }
            }
            finally
            {
                ReleaseMem(rgValues);
            }

        LExit:
            return;
        }

        [Fact]
        void MemUtilInsertTest()
        {
            HRESULT hr = S_OK;
            ArrayValue *rgValues = NULL;
            DWORD cValues = 0;

            try
            {
                hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to insert into beginning of empty array");
                ++cValues;
                CheckNullItem(rgValues + 0);
                SetItem(rgValues + 0, 5);

                hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 1, 1, cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to insert at end of array");
                ++cValues;
                CheckNullItem(rgValues + 1);
                SetItem(rgValues + 1, 6);

                hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to insert into beginning of array");
                ++cValues;
                CheckNullItem(rgValues + 0);
                SetItem(rgValues + 0, 4);

                hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to insert into beginning of array");
                ++cValues;
                CheckNullItem(rgValues + 0);
                SetItem(rgValues + 0, 3);

                hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to insert into beginning of array");
                ++cValues;
                CheckNullItem(rgValues + 0);
                SetItem(rgValues + 0, 1);

                hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 1, 1, cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to insert into beginning of array");
                ++cValues;
                CheckNullItem(rgValues + 1);
                SetItem(rgValues + 1, 2);

                hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 0, 1, cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to insert into beginning of array");
                ++cValues;
                CheckNullItem(rgValues + 0);
                SetItem(rgValues + 0, 0);

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 7");
                ++cValues;
                CheckNullItem(rgValues + 7);
                SetItem(rgValues + 7, 7);

                hr = MemInsertIntoArray(reinterpret_cast<LPVOID*>(&rgValues), 8, 1, cValues + 1, sizeof(ArrayValue), 5);
                NativeAssert::Succeeded(hr, "Failed to insert into beginning of array");
                ++cValues;
                CheckNullItem(rgValues + 8);
                SetItem(rgValues + 8, 8);

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }
            }
            finally
            {
                ReleaseMem(rgValues);
            }
        }

        [Fact]
        void MemUtilRemovePreserveOrderTest()
        {
            HRESULT hr = S_OK;
            ArrayValue *rgValues = NULL;
            DWORD cValues = 0;

            try
            {
                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), 10, sizeof(ArrayValue), 10);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 10");

                cValues = 10;
                for (DWORD i = 0; i < cValues; ++i)
                {
                    SetItem(rgValues + i, i);
                }

                // Remove last item
                MemRemoveFromArray(rgValues, 9, 1, cValues, sizeof(ArrayValue), TRUE);
                --cValues;

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                // Remove last two items
                MemRemoveFromArray(rgValues, 7, 2, cValues, sizeof(ArrayValue), TRUE);
                cValues -= 2;

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                // Remove first item
                MemRemoveFromArray(rgValues, 0, 1, cValues, sizeof(ArrayValue), TRUE);
                --cValues;

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i + 1);
                }


                // Remove first two items
                MemRemoveFromArray(rgValues, 0, 2, cValues, sizeof(ArrayValue), TRUE);
                cValues -= 2;

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i + 3);
                }

                // Remove middle two items
                MemRemoveFromArray(rgValues, 1, 2, cValues, sizeof(ArrayValue), TRUE);
                cValues -= 2;

                CheckItem(rgValues, 3);
                CheckItem(rgValues + 1, 6);

                // Remove last 2 items to ensure we don't crash
                MemRemoveFromArray(rgValues, 0, 2, cValues, sizeof(ArrayValue), TRUE);
                cValues -= 2;
            }
            finally
            {
                ReleaseMem(rgValues);
            }
        }

        [Fact]
        void MemUtilRemoveFastTest()
        {
            HRESULT hr = S_OK;
            ArrayValue *rgValues = NULL;
            DWORD cValues = 0;

            try
            {
                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), 10, sizeof(ArrayValue), 10);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 10");

                cValues = 10;
                for (DWORD i = 0; i < cValues; ++i)
                {
                    SetItem(rgValues + i, i);
                }

                // Remove last item
                MemRemoveFromArray(rgValues, 9, 1, cValues, sizeof(ArrayValue), FALSE);
                --cValues;

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                // Remove last two items
                MemRemoveFromArray(rgValues, 7, 2, cValues, sizeof(ArrayValue), FALSE);
                cValues -= 2;

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                // Remove first item
                MemRemoveFromArray(rgValues, 0, 1, cValues, sizeof(ArrayValue), FALSE);
                --cValues;

                CheckItem(rgValues, 6);
                CheckItem(rgValues + 1, 1);
                CheckItem(rgValues + 2, 2);
                CheckItem(rgValues + 3, 3);
                CheckItem(rgValues + 4, 4);
                CheckItem(rgValues + 5, 5);

                // Remove first two items
                MemRemoveFromArray(rgValues, 0, 2, cValues, sizeof(ArrayValue), FALSE);
                cValues -= 2;

                CheckItem(rgValues, 4);
                CheckItem(rgValues + 1, 5);
                CheckItem(rgValues + 2, 2);
                CheckItem(rgValues + 3, 3);


                // Remove middle two items
                MemRemoveFromArray(rgValues, 1, 2, cValues, sizeof(ArrayValue), FALSE);
                cValues -= 2;

                CheckItem(rgValues, 4);
                CheckItem(rgValues + 1, 3);

                // Remove last 2 items to ensure we don't crash
                MemRemoveFromArray(rgValues, 0, 2, cValues, sizeof(ArrayValue), FALSE);
                cValues -= 2;
            }
            finally
            {
                ReleaseMem(rgValues);
            }
        }

        [Fact]
        void MemUtilSwapTest()
        {
            HRESULT hr = S_OK;
            ArrayValue *rgValues = NULL;
            DWORD cValues = 0;

            try
            {
                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&rgValues), 10, sizeof(ArrayValue), 10);
                NativeAssert::Succeeded(hr, "Failed to grow array size to 10");

                cValues = 10;
                for (DWORD i = 0; i < cValues; ++i)
                {
                    SetItem(rgValues + i, i);
                }

                // Swap first two
                MemArraySwapItems(rgValues, 0, 1, sizeof(ArrayValue));
                --cValues;

                CheckItem(rgValues, 1);
                CheckItem(rgValues + 1, 0);
                for (DWORD i = 2; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                // Swap them back
                MemArraySwapItems(rgValues, 0, 1, sizeof(ArrayValue));
                --cValues;

                for (DWORD i = 0; i < cValues; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                // Swap first and last items (index 0 and 9)
                MemArraySwapItems(rgValues, 0, 9, sizeof(ArrayValue));
                --cValues;

                CheckItem(rgValues, 9);
                CheckItem(rgValues + 9, 0);
                for (DWORD i = 1; i < cValues - 1; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                // Swap index 1 and 8
                MemArraySwapItems(rgValues, 1, 8, sizeof(ArrayValue));
                --cValues;

                CheckItem(rgValues, 9);
                CheckItem(rgValues + 1, 8);
                CheckItem(rgValues + 8, 1);
                CheckItem(rgValues + 9, 0);
                for (DWORD i = 2; i < cValues - 2; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                // Swap index 2 and 7
                MemArraySwapItems(rgValues, 2, 7, sizeof(ArrayValue));
                --cValues;

                CheckItem(rgValues, 9);
                CheckItem(rgValues + 1, 8);
                CheckItem(rgValues + 2, 7);
                CheckItem(rgValues + 7, 2);
                CheckItem(rgValues + 8, 1);
                CheckItem(rgValues + 9, 0);
                for (DWORD i = 3; i < cValues - 3; ++i)
                {
                    CheckItem(rgValues + i, i);
                }

                // Swap index 0 and 1
                MemArraySwapItems(rgValues, 0, 1, sizeof(ArrayValue));
                --cValues;

                CheckItem(rgValues, 8);
                CheckItem(rgValues + 1, 9);
                CheckItem(rgValues + 2, 7);
                CheckItem(rgValues + 7, 2);
                CheckItem(rgValues + 8, 1);
                CheckItem(rgValues + 9, 0);
                for (DWORD i = 3; i < cValues - 3; ++i)
                {
                    CheckItem(rgValues + i, i);
                }
            }
            finally
            {
                ReleaseMem(rgValues);
            }
        }

    private:
        void SetItem(ArrayValue *pValue, DWORD dwValue)
        {
            HRESULT hr = S_OK;
            pValue->dwNum = dwValue;

            hr = StrAllocFormatted(&pValue->sczString, L"%u", dwValue);
            NativeAssert::Succeeded(hr, "Failed to allocate string");
        }

        void CheckItem(ArrayValue *pValue, DWORD dwValue)
        {
            HRESULT hr = S_OK;
            LPWSTR sczTemp = NULL;

            try
            {
                NativeAssert::Equal(dwValue, pValue->dwNum);

                hr = StrAllocFormatted(&sczTemp, L"%u", dwValue);
                NativeAssert::Succeeded(hr, "Failed to allocate temp string");

                NativeAssert::StringEqual(sczTemp, pValue->sczString, TRUE);

                if (pValue->pvNull1 || pValue->pvNull2)
                {
                    hr = E_FAIL;
                    ExitOnFailure(hr, "One of the expected NULL values wasn't NULL!");
                }
            }
            finally
            {
                ReleaseStr(sczTemp);
            }

        LExit:
            return;
        }

        void CheckNullItem(ArrayValue *pValue)
        {
            HRESULT hr = S_OK;

            NativeAssert::Equal<DWORD>(0, pValue->dwNum);

            if (pValue->sczString)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "Item found isn't NULL!");
            }

            if (pValue->pvNull1 || pValue->pvNull2)
            {
                hr = E_FAIL;
                ExitOnFailure(hr, "One of the expected NULL values wasn't NULL!");
            }

        LExit:
            return;
        }
    };
}
