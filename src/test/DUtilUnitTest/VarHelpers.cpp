// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


namespace DutilTests
{
    using namespace System;
    using namespace WixTest;

    void VarSetStringHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LPCWSTR wzValue)
    {
        HRESULT hr = S_OK;

        hr = VarSetString(pVariables, wzVariable, wzValue);
        NativeAssert::Succeeded(hr, "Failed to set {0} to: {1}", wzVariable, wzValue);
    }

    void VarSetNumericHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LONGLONG llValue)
    {
        HRESULT hr = S_OK;

        hr = VarSetNumeric(pVariables, wzVariable, llValue);
        NativeAssert::Succeeded(hr, gcnew String("Failed to set {0} to: {1}"), gcnew String(wzVariable), llValue);
    }

    void VarSetVersionHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, DWORD64 qwValue)
    {
        HRESULT hr = S_OK;

        hr = VarSetVersion(pVariables, wzVariable, qwValue);
        NativeAssert::Succeeded(hr, gcnew String("Failed to set {0} to: 0x{1:X8}"), gcnew String(wzVariable), qwValue);
    }

    void VarGetStringHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LPCWSTR wzExpectedValue)
    {
        HRESULT hr = S_OK;
        LPWSTR scz = NULL;

        try
        {
            hr = VarGetString(pVariables, wzVariable, &scz);
            NativeAssert::Succeeded(hr, "Failed to get: {0}", wzVariable);
            NativeAssert::StringEqual(wzExpectedValue, scz);
        }
        finally
        {
            ReleaseStr(scz);
        }
    }

    void VarGetNumericHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LONGLONG llExpectedValue)
    {
        HRESULT hr = S_OK;
        LONGLONG llValue = 0;

        hr = VarGetNumeric(pVariables, wzVariable, &llValue);
        NativeAssert::Succeeded(hr, "Failed to get: {0}", wzVariable);
        NativeAssert::Equal(llExpectedValue, llValue);
    }

    void VarGetVersionHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, DWORD64 qwExpectedValue)
    {
        HRESULT hr = S_OK;
        DWORD64 qwValue = 0;

        hr = VarGetVersion(pVariables, wzVariable, &qwValue);
        NativeAssert::Succeeded(hr, "Failed to get: {0}", wzVariable);
        NativeAssert::Equal(qwExpectedValue, qwValue);
    }

    void VarGetFormattedHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzVariable, LPCWSTR wzExpectedValue)
    {
        HRESULT hr = S_OK;
        LPWSTR scz = NULL;

        try
        {
            hr = VarGetFormatted(pVariables, wzVariable, &scz);
            NativeAssert::Succeeded(hr, "Failed to get formatted: {0}", wzVariable);
            NativeAssert::StringEqual(wzExpectedValue, scz);
        }
        finally
        {
            ReleaseStr(scz);
        }
    }

    void VarFormatStringHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzIn, LPCWSTR wzExpectedValue)
    {
        HRESULT hr = S_OK;
        LPWSTR scz = NULL;

        try
        {
            hr = VarFormatString(pVariables, wzIn, &scz, NULL);
            NativeAssert::Succeeded(hr, "Failed to format string: '{0}'", wzIn);
            NativeAssert::StringEqual(wzExpectedValue, scz);
        }
        finally
        {
            ReleaseStr(scz);
        }
    }

    void VarEscapeStringHelper(LPCWSTR wzIn, LPCWSTR wzExpectedValue)
    {
        HRESULT hr = S_OK;
        LPWSTR scz = NULL;

        try
        {
            hr = VarEscapeString(wzIn, &scz);
            NativeAssert::Succeeded(hr, "Failed to escape string: '{0}'", wzIn);
            NativeAssert::StringEqual(wzExpectedValue, scz);
        }
        finally
        {
            ReleaseStr(scz);
        }
    }

    bool EvaluateConditionHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzCondition)
    {
        HRESULT hr = S_OK;
        BOOL f = FALSE;

        hr = CondEvaluate(pVariables, wzCondition, &f);
        NativeAssert::Succeeded(hr, "Failed to evaluate condition: '{0}'", wzCondition);

        return f ? true : false;
    }

    bool EvaluateFailureConditionHelper(VARIABLES_HANDLE pVariables, LPCWSTR wzCondition)
    {
        HRESULT hr = S_OK;
        BOOL f = FALSE;

        hr = CondEvaluate(pVariables, wzCondition, &f);
        if (E_INVALIDDATA != hr)
        {
            NativeAssert::Succeeded(hr, "Failed to evaluate condition: '{0}'", wzCondition);
        }

        return E_INVALIDDATA == hr ? true : false;
    }
}
