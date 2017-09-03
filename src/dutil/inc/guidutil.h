#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

#define GUID_STRING_LENGTH 39

HRESULT DAPI GuidFixedCreate(
    _Out_z_cap_c_(GUID_STRING_LENGTH) WCHAR* wzGuid
    );

HRESULT DAPI GuidCreate(
    __deref_out_z LPWSTR* psczGuid
    );

#ifdef __cplusplus
}
#endif
