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

static HRESULT LogUserSid();
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
        __inout WORD* pwId,
        __inout BOOL* /*pfDisableAutomaticFunctionality*/
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

    virtual STDMETHODIMP WndProc(
        __in HWND hWnd,
        __in UINT uMsg,
        __in WPARAM /*wParam*/,
        __in LPARAM lParam,
        __inout BOOL* pfProcessed,
        __inout LRESULT* plResult
        )
    {
        switch (uMsg)
        {
        case WM_QUERYENDSESSION:
            if (BOOTSTRAPPER_DISPLAY_FULL <= m_command.display)
            {
                DWORD dwEndSession = static_cast<DWORD>(lParam);
                if (ENDSESSION_CRITICAL & dwEndSession)
                {
                    // Return false to get the WM_ENDSESSION message so that critical shutdowns can be delayed.
                    *plResult = FALSE;
                    *pfProcessed = TRUE;
                }
            }
            break;
        case WM_ENDSESSION:
            if (BOOTSTRAPPER_DISPLAY_FULL <= m_command.display)
            {
                ::MessageBoxW(hWnd, L"WM_ENDSESSION", L"BAFunctions WndProc", MB_OK);
            }
            break;
        }
        return S_OK;
    }

public: //IBootstrapperApplication
    virtual STDMETHODIMP OnExecuteBegin(
        __in DWORD /*cExecutingPackages*/,
        __inout BOOL* pfCancel
        )
    {
        if (BOOTSTRAPPER_DISPLAY_FULL <= m_command.display)
        {
            if (IDCANCEL == ::MessageBoxW(m_hwndParent, L"Shutdown requests should be denied right now.", L"OnExecuteBegin", MB_OKCANCEL))
            {
                *pfCancel = TRUE;
            }
        }

        return S_OK;
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

        ::EnableWindow(m_hwndParent, FALSE);

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
        ::EnableWindow(m_hwndParent, TRUE);

        DestroyTestingWindow();

        ReleaseTheme(m_pBafTheme);

        return hr;
    }

    HRESULT CreateTestingWindow()
    {
        HRESULT hr = S_OK;
        WNDCLASSW wc = { };
        int x = CW_USEDEFAULT;
        int y = CW_USEDEFAULT;
        POINT ptCursor = { };

        ThemeInitializeWindowClass(m_pBafTheme, &wc, CBafThmUtilTesting::TestingWndProc, m_hModule, BAFTHMUTILTESTING_WINDOW_CLASS);

        Assert(wc.lpszClassName);

        // If the theme did not provide an icon, try using the icon from the bundle engine.
        if (!wc.hIcon)
        {
            HMODULE hBootstrapperEngine = ::GetModuleHandleW(NULL);
            if (hBootstrapperEngine)
            {
                wc.hIcon = ::LoadIconW(hBootstrapperEngine, MAKEINTRESOURCEW(1));
            }
        }

        // Register the window class and create the window.
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

        hr = ThemeCreateParentWindow(m_pBafTheme, 0, wc.lpszClassName, m_pBafTheme->sczCaption, m_pBafTheme->dwStyle, x, y, m_hwndParent, m_hModule, this, THEME_WINDOW_INITIAL_POSITION_CENTER_MONITOR_FROM_COORDINATES, &m_hWndBaf);
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
                ::EnableWindow(pBaf->m_hwndParent, TRUE);
            }

            break;

        case WM_NCDESTROY:
        {
            LRESULT lres = ThemeDefWindowProc(pBaf ? pBaf->m_pBafTheme : NULL, hWnd, uMsg, wParam, lParam);
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);

            ::PostQuitMessage(0);
            return lres;
        }

        case WM_THMUTIL_LOADING_CONTROL:
            return pBaf->OnThemeLoadingControl(reinterpret_cast<THEME_LOADINGCONTROL_ARGS*>(wParam), reinterpret_cast<THEME_LOADINGCONTROL_RESULTS*>(lParam));

        case WM_THMUTIL_LOADED_CONTROL:
            return pBaf->OnThemeLoadedControl(hWnd, reinterpret_cast<THEME_LOADEDCONTROL_ARGS*>(wParam), reinterpret_cast<THEME_LOADEDCONTROL_RESULTS*>(lParam));

        case WM_TIMER:
            if (!lParam && BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_IMAGE == wParam && pBaf)
            {
                pBaf->UpdateProgressBarProgress();

                return 0;
            }
            break;
        }

        return ThemeDefWindowProc(pBaf ? pBaf->m_pBafTheme : NULL, hWnd, uMsg, wParam, lParam);
    }

    HRESULT OnCreatedListView(
        __in HWND hWndListView
        )
    {
        HRESULT hr = S_OK;
        LVITEMW lvitem = { };
        LVGROUP lvgroup = { };
        static UINT puColumns[] = { 0, 1, 2 };

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

            ListView_InsertGroup(hWndListView, -1, &lvgroup);

            lvitem.mask = LVIF_COLUMNS | LVIF_GROUPID | LVIF_IMAGE | LVIF_TEXT;
            lvitem.iItem = i;
            lvitem.iSubItem = 0;

            hr = StrAllocFormatted(&lvitem.pszText, L"ListViewItem_%d", i);
            BalExitOnFailure(hr, "Failed to alloc list view item text.");

            lvitem.iImage = i;
            lvitem.iGroupId = i;
            lvitem.cColumns = countof(puColumns);
            lvitem.puColumns = puColumns;

            ListView_InsertItem(hWndListView, &lvitem);

            for (int j = 0; j < 3; ++j)
            {
                lvitem.mask = LVIF_TEXT;
                lvitem.iSubItem = j + 1;

                hr = StrAllocFormatted(&lvitem.pszText, L"%d_%d", j, i);
                BalExitOnFailure(hr, "Failed to alloc list view subitem text.");

                ListView_InsertItem(hWndListView, &lvitem);
            }
        }

    LExit:
        ReleaseStr(lvgroup.pszDescriptionTop);
        ReleaseStr(lvgroup.pszHeader);
        ReleaseStr(lvitem.pszText);

        return hr;
    }

    BOOL OnThemeLoadingControl(
        __in const THEME_LOADINGCONTROL_ARGS* pArgs,
        __in THEME_LOADINGCONTROL_RESULTS* pResults
        )
    {
        HRESULT hr = S_OK;
        BOOL fProcessed = FALSE;
        
        for (DWORD iAssignControl = 0; iAssignControl < countof(vrgInitControls); ++iAssignControl)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pArgs->pThemeControl->sczName, -1, vrgInitControls[iAssignControl].wzName, -1))
            {
                fProcessed = TRUE;
                pResults->wId = vrgInitControls[iAssignControl].wId;
                break;
            }
        }

        pResults->hr = hr;
        return fProcessed || FAILED(hr);
    }

    BOOL OnThemeLoadedControl(
        __in HWND hWndParent,
        __in const THEME_LOADEDCONTROL_ARGS* pArgs,
        __in THEME_LOADEDCONTROL_RESULTS* pResults
        )
    {
        HRESULT hr = S_OK;
        BOOL fProcessed = FALSE;

        switch (pArgs->pThemeControl->wId)
        {
        case BAFTHMUTILTESTING_CONTROL_LISTVIEW_TOP_LEFT:
        case BAFTHMUTILTESTING_CONTROL_LISTVIEW_TOP_RIGHT:
        case BAFTHMUTILTESTING_CONTROL_LISTVIEW_BOTTOM_LEFT:
        case BAFTHMUTILTESTING_CONTROL_LISTVIEW_BOTTOM_RIGHT:
            fProcessed = TRUE;

            hr = OnCreatedListView(pArgs->pThemeControl->hWnd);
            ExitOnFailure(hr, "Failed to populate list view.");

            if (BAFTHMUTILTESTING_CONTROL_LISTVIEW_TOP_RIGHT == pArgs->pThemeControl->wId)
            {
                ListView_EnableGroupView(pArgs->pThemeControl->hWnd, TRUE);
            }

            break;
            
        case BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_STANDARD:
            fProcessed = TRUE;

            ::SetTimer(hWndParent, BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_IMAGE, 500, NULL);
            break;
        }

    LExit:
        pResults->hr = hr;
        return fProcessed || FAILED(hr);
    }

    void UpdateProgressBarProgress()
    {
        const THEME_CONTROL* pControlProgressbarImage = NULL;
        const THEME_CONTROL* pControlProgressbarStandard = NULL;
        static DWORD dwProgress = 0;
        DWORD dwCurrent = dwProgress < 100 ? dwProgress : 200 - dwProgress;

        ThemeControlExistsById(m_pBafTheme, BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_IMAGE, &pControlProgressbarImage);
        ThemeControlExistsById(m_pBafTheme, BAFTHMUTILTESTING_CONTROL_PROGRESSBAR_STANDARD, &pControlProgressbarStandard);

        if (0 == dwProgress || 100 == dwProgress)
        {
            ThemeSetProgressControlColor(pControlProgressbarImage, 100 == dwProgress ? 1 : 0);
        }

        dwProgress = (dwProgress + 10) % 200;

        ThemeSetProgressControl(pControlProgressbarImage, dwCurrent);
        ThemeSetProgressControl(pControlProgressbarStandard, dwCurrent);
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

    LogUserSid();

LExit:
    ReleaseObject(pBAFunctions);
    ReleaseObject(pEngine);

    return hr;
}

static HRESULT LogUserSid()
{
    HRESULT hr = S_OK;
    TOKEN_USER* pTokenUser = NULL;
    LPWSTR sczSid = NULL;

    hr = ProcTokenUser(::GetCurrentProcess(), &pTokenUser);
    BalExitOnFailure(hr, "Failed to get user from process token.");

    if (!::ConvertSidToStringSidW(pTokenUser->User.Sid, &sczSid))
    {
        BalExitWithLastError(hr, "Failed to convert sid to string.");
    }

    BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Current User SID: %ls", sczSid);

LExit:
    ReleaseMem(pTokenUser);

    if (sczSid)
    {
        ::LocalFree(sczSid);
    }

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
