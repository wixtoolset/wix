#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


DECLARE_INTERFACE_IID_(IBundleExtensionEngine, IUnknown, "9D027A39-F6B6-42CC-9737-C185089EB263")
{
    STDMETHOD(EscapeString)(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout SIZE_T* pcchOut
        ) = 0;

    STDMETHOD(EvaluateCondition)(
        __in_z LPCWSTR wzCondition,
        __out BOOL* pf
        ) = 0;

    STDMETHOD(FormatString)(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout SIZE_T* pcchOut
        ) = 0;

    STDMETHOD(GetVariableNumeric)(
        __in_z LPCWSTR wzVariable,
        __out LONGLONG* pllValue
        ) = 0;

    STDMETHOD(GetVariableString)(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout SIZE_T* pcchValue
        ) = 0;

    STDMETHOD(GetVariableVersion)(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout SIZE_T* pcchValue
        ) = 0;

    STDMETHOD(Log)(
        __in BUNDLE_EXTENSION_LOG_LEVEL level,
        __in_z LPCWSTR wzMessage
        ) = 0;

    STDMETHOD(SetVariableNumeric)(
        __in_z LPCWSTR wzVariable,
        __in LONGLONG llValue
        ) = 0;

    STDMETHOD(SetVariableString)(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue,
        __in BOOL fFormatted
        ) = 0;

    STDMETHOD(SetVariableVersion)(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue
        ) = 0;

    STDMETHOD(CompareVersions)(
        __in_z LPCWSTR wzVersion1,
        __in_z LPCWSTR wzVersion2,
        __out int* pnResult
        ) = 0;
};
