#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


DECLARE_INTERFACE_IID_(IBAFunctions, IBootstrapperApplication, "0FB445ED-17BD-49C7-BE19-479776F8AE96")
{
    // OnThemeLoaded - Called after the BA finished loading all the controls for the theme.
    //
    STDMETHOD(OnThemeLoaded)(
        __in HWND hWnd
        ) = 0;

    // WndProc - Called if the BA hasn't handled the message.
    //
    STDMETHOD(WndProc)(
        __in HWND hWnd,
        __in UINT uMsg,
        __in WPARAM wParam,
        __in LPARAM lParam,
        __inout BOOL* pfProcessed,
        __inout LRESULT* plResult
        ) = 0;

    // BAFunctionsProc - The PFN_BA_FUNCTIONS_PROC can call this method to give the BAFunctions raw access to the callback from WixStdBA.
    //                   This might be used to help the BAFunctions support more than one version of the engine/WixStdBA.
    STDMETHOD(BAFunctionsProc)(
        __in BA_FUNCTIONS_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __in_opt LPVOID pvContext
        ) = 0;

    // OnThemeControlLoading - Called while creating a control for the theme.
    //
    STDMETHOD(OnThemeControlLoading)(
        __in LPCWSTR wzName,
        __inout BOOL* pfProcessed,
        __inout WORD* pwId
        ) = 0;

    // OnThemeControlWmCommand - Called when WM_COMMAND is received for a control.
    //
    STDMETHOD(OnThemeControlWmCommand)(
        __in WPARAM wParam,
        __in LPCWSTR wzName,
        __in WORD wId,
        __in HWND hWnd,
        __inout BOOL* pfProcessed,
        __inout LRESULT* plResult
        ) = 0;

    // OnThemeControlWmNotify - Called when WM_NOTIFY is received for a control.
    //
    STDMETHOD(OnThemeControlWmNotify)(
        __in LPNMHDR lParam,
        __in LPCWSTR wzName,
        __in WORD wId,
        __in HWND hWnd,
        __inout BOOL* pfProcessed,
        __inout LRESULT* plResult
        ) = 0;

    // OnThemeControlLoaded - Called after a control was created for the theme.
    //
    STDMETHOD(OnThemeControlLoaded)(
        __in LPCWSTR wzName,
        __in WORD wId,
        __in HWND hWnd,
        __inout BOOL* pfProcessed
        ) = 0;
};
