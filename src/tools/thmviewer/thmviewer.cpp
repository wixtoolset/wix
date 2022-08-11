// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR THMVWR_WINDOW_CLASS_MAIN = L"ThmViewerMain";

static THEME* vpTheme = NULL;
static DWORD vdwDisplayThreadId = 0;
static LPWSTR vsczThemeLoadErrors = NULL;

enum THMVWR_CONTROL
{
    // Non-paged controls
    THMVWR_CONTROL_TREE = THEME_FIRST_ASSIGN_CONTROL_ID,
};

// Internal functions

static HRESULT ProcessCommandLine(
    __in_z_opt LPCWSTR wzCommandLine,
    __out_z LPWSTR* psczThemeFile,
    __out_z LPWSTR* psczWxlFile
    );
static HRESULT CreateTheme(
    __in HINSTANCE hInstance,
    __out THEME** ppTheme
    );
static HRESULT CreateMainWindowClass(
    __in HINSTANCE hInstance,
    __in THEME* pTheme,
    __out ATOM* pAtom
    );
static LRESULT CALLBACK MainWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );
static void OnThemeLoadBegin(
    __in_z_opt LPWSTR sczThemeLoadErrors
    );
static void OnThemeLoadError(
    __in THEME* pTheme,
    __in HRESULT hrFailure
    );
static void OnNewTheme(
    __in THEME* pTheme,
    __in HWND hWnd,
    __in HANDLE_THEME* pHandle
    );
static BOOL OnThemeLoadingControl(
    __in const THEME_LOADINGCONTROL_ARGS* pArgs,
    __in THEME_LOADINGCONTROL_RESULTS* pResults
    );
static BOOL OnThemeControlWmNotify(
    __in const THEME_CONTROLWMNOTIFY_ARGS* pArgs,
    __in THEME_CONTROLWMNOTIFY_RESULTS* pResults
    );
static void CALLBACK ThmviewerTraceError(
    __in_z LPCSTR szFile,
    __in int iLine,
    __in REPORT_LEVEL rl,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

static COMDLG_FILTERSPEC vrgFilters[] =
{
    { L"Theme Files (*.thm)", L"*.thm" },
    { L"XML Files (*.xml)", L"*.xml" },
    { L"All Files (*.*)", L"*.*" },
};


int WINAPI wWinMain(
    __in HINSTANCE hInstance,
    __in_opt HINSTANCE /* hPrevInstance */,
    __in_z LPWSTR lpCmdLine,
    __in int /*nCmdShow*/
    )
{
    ::HeapSetInformation(NULL, HeapEnableTerminationOnCorruption, NULL, 0);

    HRESULT hr = S_OK;
    BOOL fComInitialized = FALSE;
    LPWSTR sczThemeFile = NULL;
    LPWSTR sczWxlFile = NULL;
    ATOM atom = 0;
    HWND hWnd = NULL;

    HANDLE hDisplayThread = NULL;
    HANDLE hLoadThread = NULL;

    BOOL fRet = FALSE;
    MSG msg = { };

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    DutilInitialize(&ThmviewerTraceError);

    hr = ProcessCommandLine(lpCmdLine, &sczThemeFile, &sczWxlFile);
    ExitOnFailure(hr, "Failed to process command line.");

    hr = CreateTheme(hInstance, &vpTheme);
    ExitOnFailure(hr, "Failed to create theme.");

    hr = CreateMainWindowClass(hInstance, vpTheme, &atom);
    ExitOnFailure(hr, "Failed to create main window.");

    hr = ThemeCreateParentWindow(vpTheme, 0, reinterpret_cast<LPCWSTR>(atom), vpTheme->sczCaption, vpTheme->dwStyle, CW_USEDEFAULT, CW_USEDEFAULT, HWND_DESKTOP, hInstance, NULL, THEME_WINDOW_INITIAL_POSITION_DEFAULT, &hWnd);
    ExitOnFailure(hr, "Failed to create window.");

    if (!sczThemeFile)
    {
        // Prompt for a path to the theme file.
        hr = WnduShowOpenFileDialog(hWnd, TRUE, TRUE, vpTheme->sczCaption, vrgFilters, countof(vrgFilters), 1, NULL, &sczThemeFile);
        if (FAILED(hr))
        {
            ::MessageBoxW(hWnd, L"Must specify a path to theme file.", vpTheme->sczCaption, MB_OK | MB_ICONERROR);
            ExitFunction1(hr = E_INVALIDARG);
        }
    }

    hr = DisplayStart(hInstance, hWnd, &hDisplayThread, &vdwDisplayThreadId);
    ExitOnFailure(hr, "Failed to start display.");

    hr = LoadStart(sczThemeFile, sczWxlFile, hWnd, &hLoadThread);
    ExitOnFailure(hr, "Failed to start load.");

    // message pump
    while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
    {
        if (-1 == fRet)
        {
            hr = E_UNEXPECTED;
            ExitOnFailure(hr, "Unexpected return value from message pump.");
        }
        else if (!ThemeHandleKeyboardMessage(vpTheme, msg.hwnd, &msg))
        {
            ::TranslateMessage(&msg);
            ::DispatchMessageW(&msg);
        }
    }

LExit:
    if (::IsWindow(hWnd))
    {
        ::DestroyWindow(hWnd);
    }

    if (hDisplayThread)
    {
        ::PostThreadMessageW(vdwDisplayThreadId, WM_QUIT, 0, 0);
        ::WaitForSingleObject(hDisplayThread, 10000);
        ::CloseHandle(hDisplayThread);
    }

    // TODO: come up with a good way to kill the load thread, probably need to switch
    // the ReadDirectoryW() to overlapped mode.
    ReleaseHandle(hLoadThread);

    if (atom && !::UnregisterClassW(reinterpret_cast<LPCWSTR>(atom), hInstance))
    {
        DWORD er = ::GetLastError();
        er = er;
    }

    ThemeFree(vpTheme);
    ThemeUninitialize();
    DutilUninitialize();

    // uninitialize COM
    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    ReleaseNullStr(vsczThemeLoadErrors);
    ReleaseStr(sczThemeFile);
    ReleaseStr(sczWxlFile);
    return hr;
}

static void CALLBACK ThmviewerTraceError(
    __in_z LPCSTR /*szFile*/,
    __in int /*iLine*/,
    __in REPORT_LEVEL /*rl*/,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    HRESULT hr = S_OK;
    LPSTR sczFormattedAnsi = NULL;
    LPWSTR sczMessage = NULL;

    if (DUTIL_SOURCE_THMUTIL != source)
    {
        ExitFunction();
    }

    hr = StrAnsiAllocFormattedArgs(&sczFormattedAnsi, szFormat, args);
    ExitOnFailure(hr, "Failed to format error log string.");

    hr = StrAllocFormatted(&sczMessage, L"Error 0x%08x: %S\r\n", hrError, sczFormattedAnsi);
    ExitOnFailure(hr, "Failed to prepend error number to error log string.");

    hr = StrAllocConcat(&vsczThemeLoadErrors, sczMessage, 0);
    ExitOnFailure(hr, "Failed to append theme load error.");

LExit:
    ReleaseStr(sczFormattedAnsi);
    ReleaseStr(sczMessage);
}


//
// ProcessCommandLine - process the provided command line arguments.
//
static HRESULT ProcessCommandLine(
    __in_z_opt LPCWSTR wzCommandLine,
    __out_z LPWSTR* psczThemeFile,
    __out_z LPWSTR* psczWxlFile
    )
{
    HRESULT hr = S_OK;
    int argc = 0;
    LPWSTR* argv = NULL;

    if (wzCommandLine && *wzCommandLine)
    {
        hr = AppParseCommandLine(wzCommandLine, &argc, &argv);
        ExitOnFailure(hr, "Failed to parse command line.");

        for (int i = 0; i < argc; ++i)
        {
            if (argv[i][0] == L'-' || argv[i][0] == L'/')
            {
                if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"lang", -1))
                {
                    if (i + 1 >= argc)
                    {
                        ExitOnRootFailure(hr = E_INVALIDARG, "Must specify a language.");
                    }

                    ++i;
                }
            }
            else
            {
                LPCWSTR wzExtension = PathExtension(argv[i]);
                if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzExtension, -1, L".wxl", -1))
                {
                    hr = StrAllocString(psczWxlFile, argv[i], 0);
                }
                else
                {
                    hr = StrAllocString(psczThemeFile, argv[i], 0);
                }
                ExitOnFailure(hr, "Failed to copy path to file.");
            }
        }
    }

LExit:
    if (argv)
    {
        AppFreeCommandLineArgs(argv);
    }

    return hr;
}

static HRESULT CreateTheme(
    __in HINSTANCE hInstance,
    __out THEME** ppTheme
    )
{
    HRESULT hr = S_OK;

    hr = ThemeInitialize(hInstance);
    ExitOnFailure(hr, "Failed to initialize theme manager.");

    hr = ThemeLoadFromResource(hInstance, MAKEINTRESOURCEA(THMVWR_RES_THEME_FILE), ppTheme);
    ExitOnFailure(hr, "Failed to load theme from thmviewer.thm.");

LExit:
    return hr;
}

static HRESULT CreateMainWindowClass(
    __in HINSTANCE hInstance,
    __in THEME* pTheme,
    __out ATOM* pAtom
    )
{
    HRESULT hr = S_OK;
    ATOM atom = 0;
    WNDCLASSW wc = { };

    ThemeInitializeWindowClass(pTheme, &wc, MainWndProc, hInstance, THMVWR_WINDOW_CLASS_MAIN);

    atom = ::RegisterClassW(&wc);
    if (!atom)
    {
        ExitWithLastError(hr, "Failed to register main windowclass .");
    }

    *pAtom = atom;

LExit:
    return hr;
}

static LRESULT CALLBACK MainWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    HANDLE_THEME* pHandleTheme = reinterpret_cast<HANDLE_THEME*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

    switch (uMsg)
    {
    case WM_NCCREATE:
        {
        //LPCREATESTRUCT lpcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
        //pBA = reinterpret_cast<CWixStandardBootstrapperApplication*>(lpcs->lpCreateParams);
        //::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pBA));
        }
        break;

    case WM_NCDESTROY:
        DecrementHandleTheme(pHandleTheme);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
        ::PostQuitMessage(0);
        break;

    case WM_THMVWR_THEME_LOAD_BEGIN:
        OnThemeLoadBegin(vsczThemeLoadErrors);
        return 0;

    case WM_THMVWR_THEME_LOAD_ERROR:
        OnThemeLoadError(vpTheme, lParam);
        return 0;

    case WM_THMVWR_NEW_THEME:
        OnNewTheme(vpTheme, hWnd, reinterpret_cast<HANDLE_THEME*>(lParam));
        return 0;

    case WM_THMUTIL_LOADING_CONTROL:
        return OnThemeLoadingControl(reinterpret_cast<THEME_LOADINGCONTROL_ARGS*>(wParam), reinterpret_cast<THEME_LOADINGCONTROL_RESULTS*>(lParam));

    case WM_THMUTIL_CONTROL_WM_NOTIFY:
        return OnThemeControlWmNotify(reinterpret_cast<THEME_CONTROLWMNOTIFY_ARGS*>(wParam), reinterpret_cast<THEME_CONTROLWMNOTIFY_RESULTS*>(lParam));
    }

    return ThemeDefWindowProc(vpTheme, hWnd, uMsg, wParam, lParam);
}

static void OnThemeLoadBegin(
    __in_z_opt LPWSTR sczThemeLoadErrors
    )
{
    ReleaseNullStr(sczThemeLoadErrors);
}

static void OnThemeLoadError(
    __in THEME* pTheme,
    __in HRESULT hrFailure
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczMessage = NULL;
    LPWSTR* psczErrors = NULL;
    UINT cErrors = 0;
    TVINSERTSTRUCTW tvi = { };
    const THEME_CONTROL* pTreeControl = NULL;

    if (!ThemeControlExistsById(pTheme, THMVWR_CONTROL_TREE, &pTreeControl))
    {
        ExitWithRootFailure(hr, E_INVALIDSTATE, "THMVWR_CONTROL_TREE control doesn't exist.");
    }

    // Add the application node.
    tvi.hParent = NULL;
    tvi.hInsertAfter = TVI_ROOT;
    tvi.item.mask = TVIF_TEXT | TVIF_PARAM;
    tvi.item.lParam = 0;
    tvi.item.pszText = L"Failed to load theme.";
    tvi.hParent = reinterpret_cast<HTREEITEM>(::SendMessage(pTreeControl->hWnd, TVM_INSERTITEMW, 0, reinterpret_cast<LPARAM>(&tvi)));

    if (!vsczThemeLoadErrors)
    {
        hr = StrAllocFormatted(&sczMessage, L"Error 0x%08x.", hrFailure);
        ExitOnFailure(hr, "Failed to format error message.");

        tvi.item.pszText = sczMessage;
        ::SendMessage(pTreeControl->hWnd, TVM_INSERTITEMW, 0, reinterpret_cast<LPARAM>(&tvi));

        hr = StrAllocFromError(&sczMessage, hrFailure, NULL);
        ExitOnFailure(hr, "Failed to format error message text.");

        tvi.item.pszText = sczMessage;
        ::SendMessage(pTreeControl->hWnd, TVM_INSERTITEMW, 0, reinterpret_cast<LPARAM>(&tvi));
    }
    else
    {
        hr = StrSplitAllocArray(&psczErrors, &cErrors, vsczThemeLoadErrors, L"\r\n");
        ExitOnFailure(hr, "Failed to split theme load errors.");

        for (DWORD i = 0; i < cErrors; ++i)
        {
            tvi.item.pszText = psczErrors[i];
            ::SendMessage(pTreeControl->hWnd, TVM_INSERTITEMW, 0, reinterpret_cast<LPARAM>(&tvi));
        }
    }

    ::SendMessage(pTreeControl->hWnd, TVM_EXPAND, TVE_EXPAND, reinterpret_cast<LPARAM>(tvi.hParent));

LExit:
    ReleaseStr(sczMessage);
    ReleaseMem(psczErrors);
}


static void OnNewTheme(
    __in THEME* pTheme,
    __in HWND hWnd,
    __in HANDLE_THEME* pHandle
    )
{
    const THEME_CONTROL* pTreeControl = NULL;
    HANDLE_THEME* pOldHandle = reinterpret_cast<HANDLE_THEME*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));
    THEME* pNewTheme = pHandle->pTheme;

    WCHAR wzSelectedPage[MAX_PATH] = { };
    HTREEITEM htiSelected = NULL;
    TVINSERTSTRUCTW tvi = { };
    TVITEMW item = { };

    if (pOldHandle)
    {
        DecrementHandleTheme(pOldHandle);
        pOldHandle = NULL;
    }

    // Pass the new theme handle to the display thread so it can get the display window prepared
    // to show the new theme.
    IncrementHandleTheme(pHandle);
    ::PostThreadMessageW(vdwDisplayThreadId, WM_THMVWR_NEW_THEME, 0, reinterpret_cast<LPARAM>(pHandle));

    ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pHandle));

    if (!ThemeControlExistsById(pTheme, THMVWR_CONTROL_TREE, &pTreeControl))
    {
        TraceError(E_INVALIDSTATE, "Tree control doesn't exist.");
        return;
    }

    // Remember the currently selected item by name so we can try to automatically select it later.
    // Otherwise, the user would see their window destroyed after every save of their theme file and
    // have to click to get the window back.
    item.mask = TVIF_TEXT;
    item.pszText = wzSelectedPage;
    item.cchTextMax = countof(wzSelectedPage);
    item.hItem = reinterpret_cast<HTREEITEM>(::SendMessage(pTreeControl->hWnd, TVM_GETNEXTITEM, TVGN_CARET, NULL));
    ::SendMessage(pTreeControl->hWnd, TVM_GETITEM, 0, reinterpret_cast<LPARAM>(&item));

    // Remove the previous items in the tree.
    ::SendMessage(pTreeControl->hWnd, TVM_DELETEITEM, 0, reinterpret_cast<LPARAM>(TVI_ROOT));

    // Add the application node.
    tvi.hParent = NULL;
    tvi.hInsertAfter = TVI_ROOT;
    tvi.item.mask = TVIF_TEXT | TVIF_PARAM;
    tvi.item.lParam = 0;
    tvi.item.pszText = pHandle && pHandle->pTheme && pHandle->pTheme->sczCaption ? pHandle->pTheme->sczCaption : L"Window";

    // Add the pages.
    tvi.hParent = reinterpret_cast<HTREEITEM>(::SendMessage(pTreeControl->hWnd, TVM_INSERTITEMW, 0, reinterpret_cast<LPARAM>(&tvi)));
    tvi.hInsertAfter = TVI_SORT;
    for (DWORD i = 0; i < pNewTheme->cPages; ++i)
    {
        THEME_PAGE* pPage = pNewTheme->rgPages + i;
        if (pPage->sczName && *pPage->sczName)
        {
            tvi.item.pszText = pPage->sczName;
            tvi.item.lParam = i + 1; //prgdwPageIds[i]; - TODO: do the right thing here by calling ThemeGetPageIds(), should not assume we know how the page ids will be calculated.

            HTREEITEM hti = reinterpret_cast<HTREEITEM>(::SendMessage(pTreeControl->hWnd, TVM_INSERTITEMW, 0, reinterpret_cast<LPARAM>(&tvi)));
            if (*wzSelectedPage && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pPage->sczName, -1, wzSelectedPage, -1))
            {
                htiSelected = hti;
            }
        }
    }

    if (*wzSelectedPage && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Application", -1, wzSelectedPage, -1))
    {
        htiSelected = tvi.hParent;
    }

    ::SendMessage(pTreeControl->hWnd, TVM_EXPAND, TVE_EXPAND, reinterpret_cast<LPARAM>(tvi.hParent));
    if (htiSelected)
    {
        ::SendMessage(pTreeControl->hWnd, TVM_SELECTITEM, TVGN_CARET, reinterpret_cast<LPARAM>(htiSelected));
    }
}

static BOOL OnThemeLoadingControl(
    __in const THEME_LOADINGCONTROL_ARGS* pArgs,
    __in THEME_LOADINGCONTROL_RESULTS* pResults
    )
{
    if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pArgs->pThemeControl->sczName, -1, L"Tree", -1))
    {
        pResults->wId = THMVWR_CONTROL_TREE;
    }

    pResults->hr = S_OK;
    return TRUE;
}

static BOOL OnThemeControlWmNotify(
    __in const THEME_CONTROLWMNOTIFY_ARGS* pArgs,
    __in THEME_CONTROLWMNOTIFY_RESULTS* /*pResults*/
    )
{
    BOOL fProcessed = FALSE;

    switch (pArgs->lParam->code)
    {
    case TVN_SELCHANGEDW:
        switch (pArgs->pThemeControl->wId)
        {
        case THMVWR_CONTROL_TREE:
            NMTREEVIEWW* ptv = reinterpret_cast<NMTREEVIEWW*>(pArgs->lParam);
            ::PostThreadMessageW(vdwDisplayThreadId, WM_THMVWR_SHOWPAGE, SW_HIDE, ptv->itemOld.lParam);
            ::PostThreadMessageW(vdwDisplayThreadId, WM_THMVWR_SHOWPAGE, SW_SHOW, ptv->itemNew.lParam);

            fProcessed = TRUE;
            break;
        }
        break;
    }

    return fProcessed;
}
