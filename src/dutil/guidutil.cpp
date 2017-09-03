// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

extern "C" HRESULT DAPI GuidFixedCreate(
    _Out_z_cap_c_(GUID_STRING_LENGTH) WCHAR* wzGuid
    )
{
    HRESULT hr = S_OK;
    UUID guid = { };

    hr = HRESULT_FROM_RPC(::UuidCreate(&guid));
    ExitOnFailure(hr, "UuidCreate failed.");

    if (!::StringFromGUID2(guid, wzGuid, GUID_STRING_LENGTH))
    {
        hr = E_OUTOFMEMORY;
        ExitOnRootFailure(hr, "Failed to convert guid into string.");
    }

LExit:
    return hr;
}

extern "C" HRESULT DAPI GuidCreate(
    __deref_out_z LPWSTR* psczGuid
    )
{
    HRESULT hr = S_OK;

    hr = StrAlloc(psczGuid, GUID_STRING_LENGTH);
    ExitOnFailure(hr, "Failed to allocate space for guid");

    hr = GuidFixedCreate(*psczGuid);
    ExitOnFailure(hr, "Failed to create new guid.");

LExit:
    return hr;
}
