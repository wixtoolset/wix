// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBAFunctions.h"
#include "BalBaseBAFunctionsProc.h"

static const LPCWSTR BAFTHMUTILTESTING_WINDOW_CLASS = L"BafThmUtilTesting";

enum BAF_CONTROL
{
    BAF_CONTROL_INSTALL_TEST_BUTTON = BAFUNCTIONS_FIRST_ASSIGN_CONTROL_ID,
};

enum BAFTHMUTILTESTING_CONTROL
{
    BAFTHMUTILTESTING_CONTROL_LISTVIEW_TOP_LEFT = THEME_FIRST_ASSIGN_CONTROL_ID,
    BAFTHMUTILTESTING_CONTROL_LISTVIEW_TOP_RIGHT,
    BAFTHMUTILTESTING_CONTROL_LISTVIEW_BOTTOM_LEFT,
    BAFTHMUTILTESTING_CONTROL_LISTVIEW_BOTTOM_RIGHT,
    BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_STANDARD,
    BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_IMAGE,
};

static THEME_ASSIGN_CONTROL_ID vrgInitControls[] = {
    { BAFTHMUTILTESTING_CONTROL_LISTVIEW_TOP_LEFT, L"ListViewTopLeft" },
    { BAFTHMUTILTESTING_CONTROL_LISTVIEW_TOP_RIGHT, L"ListViewTopRight" },
    { BAFTHMUTILTESTING_CONTROL_LISTVIEW_BOTTOM_LEFT, L"ListViewBottomLeft" },
    { BAFTHMUTILTESTING_CONTROL_LISTVIEW_BOTTOM_RIGHT, L"ListViewBottomRight" },
    { BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_IMAGE, L"ImageProgressBar" },
    { BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_STANDARD, L"StandardProgressBar" },
};

static void CALLBACK BafThmUtilTestingTraceError(
    __in_z LPCSTR szFile,
    __in int iLine,
    __in REPORT_LEVEL rl,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

class CBafThmUtilTesting : public CBalBaseBAFunctions
{
public: // IBAFunctions
    virtual STDMETHODIMP OnThemeControlLoading(
        __in LPCWSTR wzName,
        __inout BOOL* pfProcessed,
        __inout WORD* pwId
        )
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzName, -1, L"InstallTestButton", -1))
        {
            *pfProcessed = TRUE;
            *pwId = BAF_CONTROL_INSTALL_TEST_BUTTON;
        }

        return S_OK;
    }

    virtual STDMETHODIMP OnThemeControlWmCommand(
        __in WPARAM wParam,
        __in LPCWSTR /*wzName*/,
        __in WORD wId,
        __in HWND /*hWnd*/,
        __inout BOOL* pfProcessed,
        __inout LRESULT* plResult
        )
    {
        HRESULT hr = S_OK;

        switch (HIWORD(wParam))
        {
        case BN_CLICKED:
            switch (wId)
            {
            case BAF_CONTROL_INSTALL_TEST_BUTTON:
                OnShowTheme();
                *pfProcessed = TRUE;
                *plResult = 0;
                break;
            }

            break;
        }

        return hr;
    }

private:
    HRESULT OnShowTheme()
    {
        HRESULT hr = S_OK;
        BOOL fRet = FALSE;
        MSG msg = { };

        hr = ThemeLoadFromResource(m_hModule, MAKEINTRESOURCEA(1), &m_pBafTheme);
        BalExitOnFailure(hr, "Failed to load BafThmUtilTesting theme.");

        hr = CreateTestingWindow();
        BalExitOnFailure(hr, "Failed to create BafThmUtilTesting window.");

        ::EnableWindow(m_pTheme->hwndParent, FALSE);

        // message pump
        while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
        {
            if (-1 == fRet)
            {
                hr = E_UNEXPECTED;
                BalExitOnFailure(hr, "Unexpected return value from message pump.");
            }
            else if (!ThemeHandleKeyboardMessage(m_pBafTheme, msg.hwnd, &msg))
            {
                ::TranslateMessage(&msg);
                ::DispatchMessageW(&msg);
            }
        }

    LExit:
        ::EnableWindow(m_pTheme->hwndParent, TRUE);

        DestroyTestingWindow();

        ReleaseTheme(m_pBafTheme);

        return hr;
    }

    HRESULT CreateTestingWindow()
    {
        HRESULT hr = S_OK;
        HICON hIcon = reinterpret_cast<HICON>(m_pTheme->hIcon);
        WNDCLASSW wc = { };
        int x = CW_USEDEFAULT;
        int y = CW_USEDEFAULT;
        POINT ptCursor = { };

        // If the theme did not provide an icon, try using the icon from the bundle engine.
        if (!hIcon)
        {
            HMODULE hBootstrapperEngine = ::GetModuleHandleW(NULL);
            if (hBootstrapperEngine)
            {
                hIcon = ::LoadIconW(hBootstrapperEngine, MAKEINTRESOURCEW(1));
            }
        }

        // Register the window class and create the window.
        wc.lpfnWndProc = CBafThmUtilTesting::TestingWndProc;
        wc.hInstance = m_hModule;
        wc.hIcon = hIcon;
        wc.hCursor = ::LoadCursorW(NULL, (LPCWSTR)IDC_ARROW);
        wc.hbrBackground = m_pTheme->rgFonts[m_pBafTheme->dwFontId].hBackground;
        wc.lpszMenuName = NULL;
        wc.lpszClassName = BAFTHMUTILTESTING_WINDOW_CLASS;
        if (!::RegisterClassW(&wc))
        {
            ExitWithLastError(hr, "Failed to register window.");
        }

        m_fRegistered = TRUE;

        // Center the window on the monitor with the mouse.
        if (::GetCursorPos(&ptCursor))
        {
            x = ptCursor.x;
            y = ptCursor.y;
        }

        hr = ThemeCreateParentWindow(m_pBafTheme, 0, wc.lpszClassName, m_pBafTheme->sczCaption, m_pBafTheme->dwStyle, x, y, m_pTheme->hwndParent, m_hModule, this, THEME_WINDOW_INITIAL_POSITION_CENTER_MONITOR_FROM_COORDINATES, &m_hWndBaf);
        ExitOnFailure(hr, "Failed to create window.");

        hr = S_OK;

    LExit:
        return hr;
    }

    void DestroyTestingWindow()
    {
        if (::IsWindow(m_hWndBaf))
        {
            ::DestroyWindow(m_hWndBaf);
            m_hWndBaf = NULL;
        }

        if (m_fRegistered)
        {
            ::UnregisterClassW(BAFTHMUTILTESTING_WINDOW_CLASS, m_hModule);
            m_fRegistered = FALSE;
        }
    }

    static LRESULT CALLBACK TestingWndProc(
        __in HWND hWnd,
        __in UINT uMsg,
        __in WPARAM wParam,
        __in LPARAM lParam
        )
    {
#pragma warning(suppress:4312)
        CBafThmUtilTesting* pBaf = reinterpret_cast<CBafThmUtilTesting*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

        switch (uMsg)
        {
        case WM_NCCREATE:
        {
            LPCREATESTRUCT lpcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
            pBaf = reinterpret_cast<CBafThmUtilTesting*>(lpcs->lpCreateParams);
#pragma warning(suppress:4244)
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pBaf));
            break;
        }

        case WM_CLOSE:
            if (pBaf)
            {
                ::EnableWindow(pBaf->m_pTheme->hwndParent, TRUE);
            }

            break;

        case WM_NCDESTROY:
        {
            LRESULT lres = ThemeDefWindowProc(pBaf ? pBaf->m_pBafTheme : NULL, hWnd, uMsg, wParam, lParam);
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);

            ::PostQuitMessage(0);
            return lres;
        }

        case WM_CREATE:
            if (!pBaf->OnCreate(hWnd))
            {
                return -1;
            }
            break;

        case WM_THMUTIL_LOADING_CONTROL:
            return pBaf->OnThemeLoadingControl(reinterpret_cast<THEME_LOADINGCONTROL_ARGS*>(wParam), reinterpret_cast<THEME_LOADINGCONTROL_RESULTS*>(lParam));

        case WM_TIMER:
            if (!lParam && pBaf)
            {
                pBaf->UpdateProgressBarProgress();

                return 0;
            }
            break;
        }

        return ThemeDefWindowProc(pBaf ? pBaf->m_pBafTheme : NULL, hWnd, uMsg, wParam, lParam);
    }

    BOOL OnCreate(
        __in HWND hWnd
        )
    {
        HRESULT hr = S_OK;
        LVITEMW lvitem = { };
        LVGROUP lvgroup = { };
        static UINT puColumns[] = { 0, 1, 2 };
        HWND hwndTopLeft = NULL;
        HWND hwndTopRight = NULL;
        HWND hwndBottomLeft = NULL;
        HWND hwndBottomRight = NULL;

        hr = ThemeLoadControls(m_pBafTheme);
        BalExitOnFailure(hr, "Failed to load theme controls.");

        hwndTopLeft = ::GetDlgItem(m_pBafTheme->hwndParent, BAFTHMUTILTESTING_CONTROL_LISTVIEW_TOP_LEFT);
        BalExitOnNull(hwndTopLeft, hr, E_INVALIDSTATE, "Failed to get top left list view hWnd.");

        hwndTopRight = ::GetDlgItem(m_pBafTheme->hwndParent, BAFTHMUTILTESTING_CONTROL_LISTVIEW_TOP_RIGHT);
        BalExitOnNull(hwndTopRight, hr, E_INVALIDSTATE, "Failed to get top right list view hWnd.");

        hwndBottomLeft = ::GetDlgItem(m_pBafTheme->hwndParent, BAFTHMUTILTESTING_CONTROL_LISTVIEW_BOTTOM_LEFT);
        BalExitOnNull(hwndBottomLeft, hr, E_INVALIDSTATE, "Failed to get bottom left list view hWnd.");

        hwndBottomRight = ::GetDlgItem(m_pBafTheme->hwndParent, BAFTHMUTILTESTING_CONTROL_LISTVIEW_BOTTOM_RIGHT);
        BalExitOnNull(hwndBottomRight, hr, E_INVALIDSTATE, "Failed to get bottom right list view hWnd.");

        lvgroup.cbSize = sizeof(LVGROUP);
        lvgroup.mask = LVGF_GROUPID | LVGF_TITLEIMAGE | LVGF_DESCRIPTIONTOP | LVGF_HEADER;

        for (int i = 0; i < 3; ++i)
        {
            lvgroup.iGroupId = i;
            lvgroup.iTitleImage = i;

            hr = StrAllocFormatted(&lvgroup.pszDescriptionTop, L"DescriptionTop_%d", i);
            BalExitOnFailure(hr, "Failed to alloc list view group description.");

            hr = StrAllocFormatted(&lvgroup.pszHeader, L"Header_%d", i);
            BalExitOnFailure(hr, "Failed to alloc list view group header.");

            ListView_InsertGroup(hwndTopLeft, -1, &lvgroup);
            ListView_InsertGroup(hwndTopRight, -1, &lvgroup);
            ListView_InsertGroup(hwndBottomLeft, -1, &lvgroup);
            ListView_InsertGroup(hwndBottomRight, -1, &lvgroup);

            lvitem.mask = LVIF_COLUMNS | LVIF_GROUPID | LVIF_IMAGE | LVIF_TEXT;
            lvitem.iItem = i;
            lvitem.iSubItem = 0;

            hr = StrAllocFormatted(&lvitem.pszText, L"ListViewItem_%d", i);
            BalExitOnFailure(hr, "Failed to alloc list view item text.");

            lvitem.iImage = i;
            lvitem.iGroupId = i;
            lvitem.cColumns = countof(puColumns);
            lvitem.puColumns = puColumns;

            ListView_InsertItem(hwndTopLeft, &lvitem);
            ListView_InsertItem(hwndTopRight, &lvitem);
            ListView_InsertItem(hwndBottomLeft, &lvitem);
            ListView_InsertItem(hwndBottomRight, &lvitem);

            for (int j = 0; j < 3; ++j)
            {
                lvitem.mask = LVIF_TEXT;
                lvitem.iSubItem = j + 1;

                hr = StrAllocFormatted(&lvitem.pszText, L"%d_%d", j, i);
                BalExitOnFailure(hr, "Failed to alloc list view subitem text.");

                ListView_InsertItem(hwndTopLeft, &lvitem);
                ListView_InsertItem(hwndTopRight, &lvitem);
                ListView_InsertItem(hwndBottomLeft, &lvitem);
                ListView_InsertItem(hwndBottomRight, &lvitem);
            }
        }

        ListView_EnableGroupView(hwndTopRight, TRUE);

        ::SetTimer(hWnd, BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_IMAGE, 500, NULL);

    LExit:
        ReleaseStr(lvgroup.pszDescriptionTop);
        ReleaseStr(lvgroup.pszHeader);
        ReleaseStr(lvitem.pszText);

        return SUCCEEDED(hr);
    }

    BOOL OnThemeLoadingControl(
        __in const THEME_LOADINGCONTROL_ARGS* pArgs,
        __in THEME_LOADINGCONTROL_RESULTS* pResults
        )
    {
        for (DWORD iAssignControl = 0; iAssignControl < countof(vrgInitControls); ++iAssignControl)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pArgs->pThemeControl->sczName, -1, vrgInitControls[iAssignControl].wzName, -1))
            {
                pResults->wId = vrgInitControls[iAssignControl].wId;
                break;
            }
        }

        pResults->hr = S_OK;
        return TRUE;
    }

    void UpdateProgressBarProgress()
    {
        static DWORD dwProgress = 0;
        DWORD dwCurrent = dwProgress < 100 ? dwProgress : 200 - dwProgress;

        if (0 == dwProgress || 100 == dwProgress)
        {
            ThemeSetProgressControlColor(m_pBafTheme, BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_IMAGE, 100 == dwProgress ? 1 : 0);
        }

        dwProgress = (dwProgress + 10) % 200;

        ThemeSetProgressControl(m_pBafTheme, BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_IMAGE, dwCurrent);
        ThemeSetProgressControl(m_pBafTheme, BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_STANDARD, dwCurrent);
    }

public:
    //
    // Constructor - initialize member variables.
    //
    CBafThmUtilTesting(
        __in HMODULE hModule,
        __in IBootstrapperEngine* pEngine,
        __in const BA_FUNCTIONS_CREATE_ARGS* pArgs
        ) : CBalBaseBAFunctions(hModule, pEngine, pArgs)
    {
        m_pBafTheme = NULL;
        m_fRegistered = FALSE;
        m_hWndBaf = NULL;

        ThemeInitialize(hModule);
    }

    //
    // Destructor - release member variables.
    //
    ~CBafThmUtilTesting()
    {
        Assert(!::IsWindow(m_hWndBaf));
        Assert(!m_pBafTheme);

        ThemeUninitialize();
    }

private:
    THEME* m_pBafTheme;
    BOOL m_fRegistered;
    HWND m_hWndBaf;
};


HRESULT WINAPI CreateBAFunctions(
    __in HMODULE hModule,
    __in const BA_FUNCTIONS_CREATE_ARGS* pArgs,
    __inout BA_FUNCTIONS_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    CBafThmUtilTesting* pBAFunctions = NULL;
    IBootstrapperEngine* pEngine = NULL;

    DutilInitialize(&BafThmUtilTestingTraceError);

    hr = BalInitializeFromCreateArgs(pArgs->pBootstrapperCreateArgs, &pEngine);
    ExitOnFailure(hr, "Failed to initialize Bal.");

    pBAFunctions = new CBafThmUtilTesting(hModule, pEngine, pArgs);
    ExitOnNull(pBAFunctions, hr, E_OUTOFMEMORY, "Failed to create new CBafThmUtilTesting object.");

    pResults->pfnBAFunctionsProc = BalBaseBAFunctionsProc;
    pResults->pvBAFunctionsProcContext = pBAFunctions;
    pBAFunctions = NULL;

LExit:
    ReleaseObject(pBAFunctions);
    ReleaseObject(pEngine);

    return hr;
}

static void CALLBACK BafThmUtilTestingTraceError(
    __in_z LPCSTR /*szFile*/,
    __in int /*iLine*/,
    __in REPORT_LEVEL /*rl*/,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    // BalLogError currently uses the Exit... macros,
    // so if expanding the scope need to ensure this doesn't get called recursively.
    if (DUTIL_SOURCE_THMUTIL == source)
    {
        BalLogErrorArgs(hrError, szFormat, args);
    }
}
