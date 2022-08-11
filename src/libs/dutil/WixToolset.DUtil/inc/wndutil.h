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

/********************************************************************
 WnduShowOpenFileDialog - shows the system dialog to select a file for opening.

*******************************************************************/
HRESULT DAPI WnduShowOpenFileDialog(
    __in_opt HWND hwndParent,
    __in BOOL fForcePathExists,
    __in BOOL fForceFileExists,
    __in_opt LPCWSTR wzTitle,
    __in_opt COMDLG_FILTERSPEC* rgFilters,
    __in DWORD cFilters,
    __in DWORD dwDefaultFilter,
    __in_opt LPCWSTR wzDefaultPath,
    __inout LPWSTR* psczPath
    );

/********************************************************************
 WnduShowOpenFolderDialog - shows the system dialog to select a folder.

*******************************************************************/
HRESULT DAPI WnduShowOpenFolderDialog(
    __in_opt HWND hwndParent,
    __in BOOL fForceFileSystem,
    __in_opt LPCWSTR wzTitle,
    __inout LPWSTR* psczPath
    );

#ifdef __cplusplus
}
#endif

