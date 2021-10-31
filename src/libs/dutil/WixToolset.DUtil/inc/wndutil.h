#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

/********************************************************************
 WnduLoadRichEditFromFile - Attach a richedit control to a RTF file.

 *******************************************************************/
HRESULT DAPI WnduLoadRichEditFromFile(
    __in HWND hWnd,
    __in_z LPCWSTR wzFileName,
    __in HMODULE hModule
    );

/********************************************************************
 WnduLoadRichEditFromResource - Attach a richedit control to resource data.

 *******************************************************************/
HRESULT DAPI WnduLoadRichEditFromResource(
    __in HWND hWnd,
    __in_z LPCSTR szResourceName,
    __in HMODULE hModule
    );

/********************************************************************
 WnduGetControlText - gets the text of a control.

*******************************************************************/
HRESULT DAPI WnduGetControlText(
    __in HWND hWnd,
    __inout_z LPWSTR* psczText
    );

#ifdef __cplusplus
}
#endif

