#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

// functions

/********************************************************************
AppFreeCommandLineArgs - frees argv from AppParseCommandLine.

********************************************************************/
void DAPI AppFreeCommandLineArgs(
    __in LPWSTR* argv
    );

/********************************************************************
AppInitialize - initializes the standard safety precautions for an
                installation application.

********************************************************************/
void DAPI AppInitialize(
    __in_ecount(cSafelyLoadSystemDlls) LPCWSTR rgsczSafelyLoadSystemDlls[],
    __in DWORD cSafelyLoadSystemDlls
    );

/********************************************************************
AppInitializeUnsafe - initializes without the full standard safety
                      precautions for an application.

********************************************************************/
void DAPI AppInitializeUnsafe();

/********************************************************************
AppParseCommandLine - parses the command line using CommandLineToArgvW.
                      The caller must free the value of pArgv on success
                      by calling AppFreeCommandLineArgs.

********************************************************************/
HRESULT DAPI AppParseCommandLine(
    __in LPCWSTR wzCommandLine,
    __in int* argc,
    __in LPWSTR** pArgv
    );

/*******************************************************************
 AppAppendCommandLineArgument - appends a command line argument on to a
    string such that ::CommandLineToArgv() will shred them correctly
    (i.e. quote arguments with spaces in them).
********************************************************************/
HRESULT DAPI AppAppendCommandLineArgument(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in_z LPCWSTR wzArgument
    );

HRESULT DAPIV AppAppendCommandLineArgumentFormatted(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in __format_string LPCWSTR wzFormat,
    ...
    );

HRESULT DAPI AppAppendCommandLineArgumentFormattedArgs(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in __format_string LPCWSTR wzFormat,
    __in va_list args
    );

/********************************************************************
AppEscapeCommandLineArgumentFormatted - formats a string and then
    escapes it such that ::CommandLineToArgv() will parse it back unaltered.

********************************************************************/
HRESULT DAPIV AppEscapeCommandLineArgumentFormatted(
    __deref_inout_z LPWSTR* psczEscapedArgument,
    __in __format_string LPCWSTR wzFormat,
    ...
    );

HRESULT DAPI AppEscapeCommandLineArgumentFormattedArgs(
    __deref_inout_z LPWSTR* psczEscapedArgument,
    __in __format_string LPCWSTR wzFormat,
    __in va_list args
    );

/********************************************************************
AppWaitForSingleObject - wrapper for ::WaitForSingleObject.

********************************************************************/
HRESULT DAPI AppWaitForSingleObject(
    __in HANDLE hHandle,
    __in DWORD dwMilliseconds
    );

/********************************************************************
AppWaitForMultipleObjects - wrapper for ::WaitForMultipleObjects.

********************************************************************/
HRESULT DAPI AppWaitForMultipleObjects(
    __in DWORD dwCount,
    __in const HANDLE* rghHandles,
    __in BOOL fWaitAll,
    __in DWORD dwMilliseconds,
    __out_opt DWORD* pdwSignaledIndex
    );

#ifdef __cplusplus
}
#endif
