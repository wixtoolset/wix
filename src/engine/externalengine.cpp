// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// function definitions

// TODO: callers need to provide the original size (at the time of first public release) of the struct instead of the current size.
HRESULT WINAPI ExternalEngineValidateMessageParameter(
    __in_opt const LPVOID pv,
    __in SIZE_T cbSizeOffset,
    __in DWORD dwMinimumSize
    )
{
    HRESULT hr = S_OK;

    if (!pv)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    DWORD cbSize = *(DWORD*)((BYTE*)pv + cbSizeOffset);
    if (dwMinimumSize < cbSize)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

LExit:
    return hr;
}
