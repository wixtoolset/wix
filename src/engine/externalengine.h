#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#define ValidateMessageParameter(x, pv, type) { x = ExternalEngineValidateMessageParameter(pv, offsetof(type, cbSize), sizeof(type)); if (FAILED(x)) { goto LExit; }}
#define ValidateMessageArgs(x, pv, type, identifier) ValidateMessageParameter(x, pv, type); const type* identifier = reinterpret_cast<type*>(pv); UNREFERENCED_PARAMETER(identifier)
#define ValidateMessageResults(x, pv, type, identifier) ValidateMessageParameter(x, pv, type); type* identifier = reinterpret_cast<type*>(pv); UNREFERENCED_PARAMETER(identifier)


#if defined(__cplusplus)
extern "C" {
#endif

HRESULT WINAPI ExternalEngineValidateMessageParameter(
    __in_opt const LPVOID pv,
    __in SIZE_T cbSizeOffset,
    __in DWORD dwMinimumSize
    );

#if defined(__cplusplus)
}
#endif
