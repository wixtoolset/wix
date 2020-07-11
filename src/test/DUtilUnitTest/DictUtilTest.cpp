// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

using namespace System;
using namespace Xunit;
using namespace WixTest;

const DWORD numIterations = 100000;

namespace DutilTests
{
    struct Value
    {
        DWORD dwNum;
        LPWSTR sczKey;
    };

    public ref class DictUtil
    {
    public:
        [Fact]
        void DictUtilTest()
        {
            EmbeddedKeyTestHelper(DICT_FLAG_NONE, numIterations);

            EmbeddedKeyTestHelper(DICT_FLAG_CASEINSENSITIVE, numIterations);

            StringListTestHelper(DICT_FLAG_NONE, numIterations);

            StringListTestHelper(DICT_FLAG_CASEINSENSITIVE, numIterations);
        }

    private:
        void EmbeddedKeyTestHelper(DICT_FLAG dfFlags, DWORD dwNumIterations)
        {
            HRESULT hr = S_OK;
            Value *rgValues = NULL;
            Value *valueFound = NULL;
            DWORD cValues = 0;
            LPWSTR sczExpectedKey = NULL;
            STRINGDICT_HANDLE sdValues = NULL;

            try
            {
                hr = DictCreateWithEmbeddedKey(&sdValues, 0, (void **)&rgValues, offsetof(Value, sczKey), dfFlags);
                NativeAssert::Succeeded(hr, "Failed to create dictionary of values");

                for (DWORD i = 0; i < dwNumIterations; ++i)
                {
                    cValues++;

                    hr = MemEnsureArraySize((void **)&rgValues, cValues, sizeof(Value), 5);
                    NativeAssert::Succeeded(hr, "Failed to grow value array");

                    hr = StrAllocFormatted(&rgValues[i].sczKey, L"%u_a_%u", i, i);
                    NativeAssert::Succeeded(hr, "Failed to allocate key for value {0}", i);

                    hr = DictAddValue(sdValues, rgValues + i);
                    NativeAssert::Succeeded(hr, "Failed to add item {0} to dict", i);
                }

                for (DWORD i = 0; i < dwNumIterations; ++i)
                {
                    hr = StrAllocFormatted(&sczExpectedKey, L"%u_a_%u", i, i);
                    NativeAssert::Succeeded(hr, "Failed to allocate expected key {0}", i);

                    hr = DictGetValue(sdValues, sczExpectedKey, (void **)&valueFound);
                    NativeAssert::Succeeded(hr, "Failed to find value {0}", sczExpectedKey);

                    NativeAssert::StringEqual(sczExpectedKey, valueFound->sczKey);

                    hr = StrAllocFormatted(&sczExpectedKey, L"%u_A_%u", i, i);
                    NativeAssert::Succeeded(hr, "Failed to allocate uppercase expected key {0}", i);

                    hr = DictGetValue(sdValues, sczExpectedKey, (void **)&valueFound);

                    if (dfFlags & DICT_FLAG_CASEINSENSITIVE)
                    {
                        NativeAssert::Succeeded(hr, "Failed to find value {0}", sczExpectedKey);

                        NativeAssert::StringEqual(sczExpectedKey, valueFound->sczKey, TRUE);
                    }
                    else
                    {
                        if (E_NOTFOUND != hr)
                        {
                            hr = E_FAIL;
                            ExitOnFailure(hr, "This embedded key is case sensitive, but it seemed to have found something case using case insensitivity!: %ls", sczExpectedKey);
                        }
                    }

                    hr = StrAllocFormatted(&sczExpectedKey, L"%u_b_%u", i, i);
                    NativeAssert::Succeeded(hr, "Failed to allocate unexpected key {0}", i);

                    hr = DictGetValue(sdValues, sczExpectedKey, (void **)&valueFound);
                    if (E_NOTFOUND != hr)
                    {
                        hr = E_FAIL;
                        ExitOnFailure(hr, "Item shouldn't have been found in dictionary: %ls", sczExpectedKey);
                    }
                }
            }
            finally
            {
                for (DWORD i = 0; i < cValues; ++i)
                {
                    ReleaseStr(rgValues[i].sczKey);
                }
                ReleaseMem(rgValues);
                ReleaseStr(sczExpectedKey);
                ReleaseDict(sdValues);
            }

        LExit:
            return;
        }

        void StringListTestHelper(DICT_FLAG dfFlags, DWORD dwNumIterations)
        {
            HRESULT hr = S_OK;
            LPWSTR sczKey = NULL;
            LPWSTR sczExpectedKey = NULL;
            STRINGDICT_HANDLE sdValues = NULL;

            try
            {
                hr = DictCreateStringList(&sdValues, 0, dfFlags);
                NativeAssert::Succeeded(hr, "Failed to create dictionary of keys");

                for (DWORD i = 0; i < dwNumIterations; ++i)
                {
                    hr = StrAllocFormatted(&sczKey, L"%u_a_%u", i, i);
                    NativeAssert::Succeeded(hr, "Failed to allocate key for value {0}", i);

                    hr = DictAddKey(sdValues, sczKey);
                    NativeAssert::Succeeded(hr, "Failed to add key {0} to dict", i);
                }

                for (DWORD i = 0; i < dwNumIterations; ++i)
                {
                    hr = StrAllocFormatted(&sczExpectedKey, L"%u_a_%u", i, i);
                    NativeAssert::Succeeded(hr, "Failed to allocate expected key {0}", i);

                    hr = DictKeyExists(sdValues, sczExpectedKey);
                    NativeAssert::Succeeded(hr, "Failed to find value {0}", sczExpectedKey);

                    hr = StrAllocFormatted(&sczExpectedKey, L"%u_A_%u", i, i);
                    NativeAssert::Succeeded(hr, "Failed to allocate uppercase expected key {0}", i);

                    hr = DictKeyExists(sdValues, sczExpectedKey);
                    if (dfFlags & DICT_FLAG_CASEINSENSITIVE)
                    {
                        NativeAssert::Succeeded(hr, "Failed to find value {0}", sczExpectedKey);
                    }
                    else
                    {
                        if (E_NOTFOUND != hr)
                        {
                            hr = E_FAIL;
                            ExitOnFailure(hr, "This stringlist dict is case sensitive, but it seemed to have found something case using case insensitivity!: %ls", sczExpectedKey);
                        }
                    }

                    hr = StrAllocFormatted(&sczExpectedKey, L"%u_b_%u", i, i);
                    NativeAssert::Succeeded(hr, "Failed to allocate unexpected key {0}", i);

                    hr = DictKeyExists(sdValues, sczExpectedKey);
                    if (E_NOTFOUND != hr)
                    {
                        hr = E_FAIL;
                        ExitOnFailure(hr, "Item shouldn't have been found in dictionary: %ls", sczExpectedKey);
                    }
                }                
            }
            finally
            {
                ReleaseStr(sczKey);
                ReleaseStr(sczExpectedKey);
                ReleaseDict(sdValues);
            }

        LExit:
            return;
        }
    };
}
