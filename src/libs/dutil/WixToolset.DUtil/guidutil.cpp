// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define GuidExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_GUIDUTIL, x, s, __VA_ARGS__)
#define GuidExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_GUIDUTIL, x, s, __VA_ARGS__)
#define GuidExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_GUIDUTIL, x, s, __VA_ARGS__)
#define GuidExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_GUIDUTIL, x, s, __VA_ARGS__)
#define GuidExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_GUIDUTIL, x, s, __VA_ARGS__)
#define GuidExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_GUIDUTIL, x, s, __VA_ARGS__)
#define GuidExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_GUIDUTIL, p, x, e, s, __VA_ARGS__)
#define GuidExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_GUIDUTIL, p, x, s, __VA_ARGS__)
#define GuidExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_GUIDUTIL, p, x, e, s, __VA_ARGS__)
#define GuidExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_GUIDUTIL, p, x, s, __VA_ARGS__)
#define GuidExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_GUIDUTIL, e, x, s, __VA_ARGS__)
#define GuidExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_GUIDUTIL, g, x, s, __VA_ARGS__)

extern "C" HRESULT DAPI GuidFixedCreate(
    _Out_z_cap_c_(GUID_STRING_LENGTH) WCHAR* wzGuid
    )
{
    HRESULT hr = S_OK;
    UUID guid = { };

    hr = HRESULT_FROM_RPC(::UuidCreate(&guid));
    GuidExitOnFailure(hr, "UuidCreate failed.");

    if (!::StringFromGUID2(guid, wzGuid, GUID_STRING_LENGTH))
    {
        hr = E_OUTOFMEMORY;
        GuidExitOnRootFailure(hr, "Failed to convert guid into string.");
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
    GuidExitOnFailure(hr, "Failed to allocate space for guid");

    hr = GuidFixedCreate(*psczGuid);
    GuidExitOnFailure(hr, "Failed to create new guid.");

LExit:
    return hr;
}
