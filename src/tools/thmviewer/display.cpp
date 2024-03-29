// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR THMVWR_WINDOW_CLASS_DISPLAY = L"ThmViewerDisplay";

struct DISPLAY_THREAD_CONTEXT
{
    HWND hWnd;
    HINSTANCE hInstance;

    HANDLE hInit;
};

static DWORD WINAPI DisplayThreadProc(
    __in LPVOID pvContext
    );
static LRESULT CALLBACK DisplayWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );
static BOOL DisplayOnThmLoadedControl(
    __in THEME* pTheme,
    __in const THEME_LOADEDCONTROL_ARGS* args,
    __in THEME_LOADEDCONTROL_RESULTS* results
    );


extern "C" HRESULT DisplayStart(
    __in HINSTANCE hInstance,
    __in HWND hWnd,
    __out HANDLE *phThread,
    __out DWORD* pdwThreadId
    )
{
    HRESULT hr = S_OK;
    HANDLE rgHandles[2] = { };
    DISPLAY_THREAD_CONTEXT context = { };

    rgHandles[0] = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(rgHandles[0], hr, "Failed to create load init event.");

    context.hWnd = hWnd;
    context.hInstance = hInstance;
    context.hInit = rgHandles[0];

    rgHandles[1] = ::CreateThread(NULL, 0, DisplayThreadProc, reinterpret_cast<LPVOID>(&context), 0, pdwThreadId);
    ExitOnNullWithLastError(rgHandles[1], hr, "Failed to create display thread.");

    ::WaitForMultipleObjects(countof(rgHandles), rgHandles, FALSE, INFINITE);

    *phThread = rgHandles[1];
    rgHandles[1] = NULL;

LExit:
    ReleaseHandle(rgHandles[1]);
    ReleaseHandle(rgHandles[0]);
    return hr;
}

static DWORD WINAPI DisplayThreadProc(
    __in LPVOID pvContext
    )
{
    HRESULT hr = S_OK;

    DISPLAY_THREAD_CONTEXT* pContext = static_cast<DISPLAY_THREAD_CONTEXT*>(pvContext);
    HINSTANCE hInstance = pContext->hInstance;
    HWND hwndParent = pContext->hWnd;

    // We can signal the initialization event as soon as we have copied the context
    // values into local variables.
    ::SetEvent(pContext->hInit);

    BOOL fComInitialized = FALSE;

    HANDLE_THEME* pCurrentHandle = NULL;
    ATOM atomWc = 0;
    WNDCLASSW wc = { };
    HWND hWnd = NULL;
    RECT rc = { };
    int x = CW_USEDEFAULT;
    int y = CW_USEDEFAULT;

    BOOL fRedoMsg = FALSE;
    BOOL fRet = FALSE;
    MSG msg = { };

    BOOL fCreateIfNecessary = FALSE;

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "Failed to initialize COM on display thread.");
    fComInitialized = TRUE;

    // As long as the parent window is alive and kicking, keep this thread going (with or without a theme to display ).
    while (::IsWindow(hwndParent))
    {
        if (pCurrentHandle && fCreateIfNecessary)
        {
            THEME* pTheme = pCurrentHandle->pTheme;

            if (CW_USEDEFAULT == x && CW_USEDEFAULT == y && ::GetWindowRect(hwndParent, &rc))
            {
                x = rc.left;
                y = rc.bottom + 20;
            }

            hr = ThemeCreateParentWindow(pTheme, 0, wc.lpszClassName, pTheme->sczCaption, pTheme->dwStyle, x, y, hwndParent, hInstance, pCurrentHandle, THEME_WINDOW_INITIAL_POSITION_DEFAULT, &hWnd);
            ExitOnFailure(hr, "Failed to create display window.");

            fCreateIfNecessary = FALSE;
        }

        // message pump
        while (fRedoMsg || 0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
        {
            if (fRedoMsg)
            {
                fRedoMsg = FALSE;
            }

            if (-1 == fRet)
            {
                hr = E_UNEXPECTED;
                ExitOnFailure(hr, "Unexpected return value from display message pump.");
            }
            else if (NULL == msg.hwnd) // Thread message.
            {
                if (WM_THMVWR_NEW_THEME == msg.message)
                {
                    // If there is already a handle, release it.
                    if (pCurrentHandle)
                    {
                        DecrementHandleTheme(pCurrentHandle);
                        pCurrentHandle = NULL;
                    }

                    // If the window was created, remember its window location before we destroy
                    // it so so we can open the new window in the same place.
                    if (::IsWindow(hWnd))
                    {
                        ::GetWindowRect(hWnd, &rc);
                        x = rc.left;
                        y = rc.top;

                        ::DestroyWindow(hWnd); 
                    }

                    // If the display window class was registered, unregister it so we can
                    // reuse the same window class name for the new theme.
                    if (atomWc)
                    {
                        if (!::UnregisterClassW(reinterpret_cast<LPCWSTR>(atomWc), hInstance))
                        {
                            DWORD er = ::GetLastError();
                            er = er;
                        }

                        atomWc = 0;
                    }

                    // If we were provided a new theme handle, create a new window class to
                    // support it.
                    pCurrentHandle = reinterpret_cast<HANDLE_THEME*>(msg.lParam);
                    if (pCurrentHandle)
                    {
                        ThemeInitializeWindowClass(pCurrentHandle->pTheme, &wc, DisplayWndProc, hInstance, THMVWR_WINDOW_CLASS_DISPLAY);

                        atomWc = ::RegisterClassW(&wc);
                        if (!atomWc)
                        {
                            ExitWithLastError(hr, "Failed to register display window class.");
                        }
                    }
                }
                else if (WM_THMVWR_SHOWPAGE == msg.message)
                {
                    if (pCurrentHandle && ::IsWindow(hWnd) && pCurrentHandle->pTheme->hwndParent == hWnd)
                    {
                        DWORD dwPageId = static_cast<DWORD>(msg.lParam);
                        int nCmdShow = static_cast<int>(msg.wParam);

                        // First show/hide the controls not associated with a page.
                        for (DWORD i = 0; i < pCurrentHandle->pTheme->cControls; ++i)
                        {
                            THEME_CONTROL* pControl = pCurrentHandle->pTheme->rgControls + i;
                            if (!pControl->wPageId)
                            {
                                ThemeShowControl(pControl, nCmdShow);
                            }
                        }

                        // If a page id was provided also, show/hide those controls
                        if (dwPageId)
                        {
                            // Ignore error since we aren't using variables and it can only fail when using variables.
                            ThemeShowPage(pCurrentHandle->pTheme, dwPageId, nCmdShow);
                        }
                    }
                    else // display window isn't visible or it doesn't match the current handle.
                    {
                        // Keep the current message around to try again after we break out of this loop
                        // and create the window.
                        fRedoMsg = TRUE;
                        fCreateIfNecessary = TRUE;
                        break;
                    }
                }
            }
            else if (!ThemeHandleKeyboardMessage(pCurrentHandle->pTheme, hwndParent, &msg)) // Window message.
            {
                ::TranslateMessage(&msg);
                ::DispatchMessageW(&msg);
            }
        }
    }

LExit:
    if (::IsWindow(hWnd))
    {
        ::DestroyWindow(hWnd);
    }

    if (atomWc)
    {
        if (!::UnregisterClassW(THMVWR_WINDOW_CLASS_DISPLAY, hInstance))
        {
            DWORD er = ::GetLastError();
            er = er;
        }
    }

    DecrementHandleTheme(pCurrentHandle);

    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return hr;
}

static LRESULT CALLBACK DisplayWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    static DWORD dwProgress = 0;
    HANDLE_THEME* pHandleTheme = reinterpret_cast<HANDLE_THEME*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

    switch (uMsg)
    {
    case WM_NCCREATE:
        {
            LPCREATESTRUCT lpcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
            pHandleTheme = reinterpret_cast<HANDLE_THEME*>(lpcs->lpCreateParams);
            IncrementHandleTheme(pHandleTheme);
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pHandleTheme));
        }
        break;

    case WM_TIMER:
        if (!lParam && SUCCEEDED(ThemeSetProgressControl(reinterpret_cast<THEME_CONTROL*>(wParam), dwProgress)))
        {
            dwProgress += rand() % 10 + 1;
            if (dwProgress > 100)
            {
                dwProgress = 0;
            }

            return 0;
        }
        break;

    case WM_COMMAND:
        {
            WCHAR wzText[1024];
            ::StringCchPrintfW(wzText, countof(wzText), L"Command %u\r\n", LOWORD(wParam));
            OutputDebugStringW(wzText);
            //::MessageBoxW(hWnd, wzText, L"Command fired", MB_OK);
        }
        break;

    case WM_SYSCOMMAND:
        {
            WCHAR wzText[1024];
            ::StringCchPrintfW(wzText, countof(wzText), L"SysCommand %u\r\n", LOWORD(wParam));
            OutputDebugStringW(wzText);
            //::MessageBoxW(hWnd, wzText, L"Command fired", MB_OK);
        }
        break;

    case WM_NCDESTROY:
        DecrementHandleTheme(pHandleTheme);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
        ::PostQuitMessage(0);
        break;

    case WM_THMUTIL_LOADED_CONTROL:
        if (pHandleTheme)
        {
            return DisplayOnThmLoadedControl(pHandleTheme->pTheme, reinterpret_cast<THEME_LOADEDCONTROL_ARGS*>(wParam), reinterpret_cast<THEME_LOADEDCONTROL_RESULTS*>(lParam));
        }
    }

    return ThemeDefWindowProc(pHandleTheme ? pHandleTheme->pTheme : NULL, hWnd, uMsg, wParam, lParam);
}

static BOOL DisplayOnThmLoadedControl(
    __in THEME* pTheme,
    __in const THEME_LOADEDCONTROL_ARGS* args,
    __in THEME_LOADEDCONTROL_RESULTS* results
    )
{
    HRESULT hr = S_OK;
    const THEME_CONTROL* pControl = args->pThemeControl;

    // Pre-populate some control types with data.
    if (THEME_CONTROL_TYPE_RICHEDIT == pControl->type)
    {
        hr = WnduLoadRichEditFromResource(pControl->hWnd, MAKEINTRESOURCEA(THMVWR_RES_RICHEDIT_FILE), ::GetModuleHandleW(NULL));
        ExitOnFailure(hr, "Failed to load richedit text.");
    }
    else if (THEME_CONTROL_TYPE_PROGRESSBAR == pControl->type)
    {
        UINT_PTR timerId = reinterpret_cast<UINT_PTR>(pControl);
        UINT_PTR id = ::SetTimer(pTheme->hwndParent, timerId, 500, NULL);
        id = id; // prevents warning in "ship" build.
        Assert(id == timerId);
    }

LExit:
    results->hr = hr;
    return TRUE;
}
