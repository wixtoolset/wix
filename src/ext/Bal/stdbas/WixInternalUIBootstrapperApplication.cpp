// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBootstrapperApplication.h"

static const LPCWSTR WIXIUIBA_WINDOW_CLASS = L"WixInternalUIBA";

enum WM_WIXIUIBA
{
    WM_WIXIUIBA_DETECT_PACKAGES = WM_APP + 100,
    WM_WIXIUIBA_PLAN_PACKAGES,
    WM_WIXIUIBA_APPLY_PACKAGES,
    WM_WIXIUIBA_DETECT_FOR_CLEANUP,
    WM_WIXIUIBA_PLAN_PACKAGES_FOR_CLEANUP,
};


class CWixInternalUIBootstrapperApplication : public CBalBaseBootstrapperApplication
{
public: // IBootstrapperApplication
    STDMETHODIMP OnCreate(
        __in IBootstrapperEngine* pEngine,
        __in BOOTSTRAPPER_COMMAND* pCommand
    )
    {
        HRESULT hr = S_OK;

        hr = __super::OnCreate(pEngine, pCommand);
        BalExitOnFailure(hr, "CBalBaseBootstrapperApplication initialization failed.");

        m_commandAction = pCommand->action;
        m_commandDisplay = pCommand->display;

        hr = InitializeData();
        BalExitOnFailure(hr, "Failed to initialize data in internal UI bootstrapper application.");

    LExit:
        return hr;
    }

    virtual STDMETHODIMP OnStartup()
    {
        HRESULT hr = S_OK;
        DWORD dwUIThreadId = 0;

        // create UI thread
        m_hUiThread = ::CreateThread(NULL, 0, UiThreadProc, this, 0, &dwUIThreadId);
        if (!m_hUiThread)
        {
            BalExitWithLastError(hr, "Failed to create UI thread.");
        }

    LExit:
        return hr;
    }


    virtual STDMETHODIMP OnShutdown(
        __inout BOOTSTRAPPER_SHUTDOWN_ACTION* pAction
        )
    {
        // wait for UI thread to terminate
        if (m_hUiThread)
        {
            ::WaitForSingleObject(m_hUiThread, INFINITE);
            ReleaseHandle(m_hUiThread);
        }

        if (m_fFailedToLoadPackage)
        {
            Assert(FAILED(m_hrFinal));
            // TODO: Should we really do what this error message says? Going back to the prereq BA
            // to show the error dialog is pretty overkill vs. showing an error dialog in this BA.
            BalLog(BOOTSTRAPPER_LOG_LEVEL_ERROR, "Failed to load primary package as the BA. The bootstrapper application will be reloaded to show the error.");
            *pAction = BOOTSTRAPPER_SHUTDOWN_ACTION_RELOAD_BOOTSTRAPPER;
        }

        return S_OK;
    }


    virtual STDMETHODIMP OnDetectPackageComplete(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_PACKAGE_STATE state,
        __in BOOL fCached
        )
    {
        BAL_INFO_PACKAGE* pPackage = NULL;

        if (SUCCEEDED(hrStatus) && SUCCEEDED(BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage)) &&
            BAL_INFO_PRIMARY_PACKAGE_TYPE_DEFAULT == pPackage->primaryPackageType)
        {
            BOOL fInstalled = BOOTSTRAPPER_PACKAGE_STATE_ABSENT < state;

            // Maybe modify the action state if the primary package is or is not already installed.
            if (fInstalled && BOOTSTRAPPER_ACTION_INSTALL == m_commandAction)
            {
                m_commandAction = BOOTSTRAPPER_ACTION_MODIFY;
            }
            else if (!fInstalled && (BOOTSTRAPPER_ACTION_MODIFY == m_commandAction || BOOTSTRAPPER_ACTION_REPAIR == m_commandAction))
            {
                m_commandAction = BOOTSTRAPPER_ACTION_INSTALL;
            }

            if (m_fApplied && !fInstalled && fCached)
            {
                m_fAutomaticRemoval = TRUE;
            }
        }

        return __super::OnDetectPackageComplete(wzPackageId, hrStatus, state, fCached);
    }


    virtual STDMETHODIMP OnDetectComplete(
        __in HRESULT hrStatus,
        __in BOOL fEligibleForCleanup
        )
    {
        if (m_fAutomaticRemoval && SUCCEEDED(hrStatus))
        {
            ::PostMessageW(m_hWnd, WM_WIXIUIBA_PLAN_PACKAGES_FOR_CLEANUP, 0, BOOTSTRAPPER_ACTION_UNINSTALL);
            ExitFunction();
        }
        else if (m_fApplied)
        {
            ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);
            ExitFunction();
        }

        // If we're performing an action that modifies machine state then evaluate conditions.
        BOOL fEvaluateConditions = SUCCEEDED(hrStatus) &&
            (BOOTSTRAPPER_ACTION_LAYOUT < m_commandAction && BOOTSTRAPPER_ACTION_UPDATE_REPLACE > m_commandAction);

        if (fEvaluateConditions)
        {
            hrStatus = EvaluateConditions();
        }

        if (SUCCEEDED(hrStatus))
        {
            ::PostMessageW(m_hWnd, WM_WIXIUIBA_PLAN_PACKAGES, 0, m_commandAction);
        }
        else
        {
            SetLoadPackageFailure(hrStatus);
        }

    LExit:
        return __super::OnDetectComplete(hrStatus, fEligibleForCleanup);
    }


    virtual STDMETHODIMP OnPlanPackageBegin(
        __in_z LPCWSTR wzPackageId,
        __in BOOTSTRAPPER_PACKAGE_STATE state,
        __in BOOL fCached,
        __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition,
        __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT repairCondition,
        __in BOOTSTRAPPER_REQUEST_STATE recommendedState,
        __in BOOTSTRAPPER_CACHE_TYPE recommendedCacheType,
        __inout BOOTSTRAPPER_REQUEST_STATE* pRequestState,
        __inout BOOTSTRAPPER_CACHE_TYPE* pRequestedCacheType,
        __inout BOOL* pfCancel
        )
    {
        HRESULT hr = S_OK;
        BAL_INFO_PACKAGE* pPackage = NULL;

        hr = BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage);
        if (FAILED(hr))
        {
            // Non-chain package, keep default.
        }
        else if (BAL_INFO_PRIMARY_PACKAGE_TYPE_DEFAULT != pPackage->primaryPackageType)
        {
            // Only the primary package should be cached or executed.
            if (BOOTSTRAPPER_CACHE_TYPE_FORCE == *pRequestedCacheType)
            {
                *pRequestedCacheType = BOOTSTRAPPER_CACHE_TYPE_KEEP;
            }

            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
        }
        else if (BOOTSTRAPPER_DISPLAY_FULL == m_commandDisplay && !m_fAutomaticRemoval)
        {
            // Make sure the MSI UI is shown regardless of the current state of the package.
            *pRequestState = BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT;
        }

        return __super::OnPlanPackageBegin(wzPackageId, state, fCached, installCondition, repairCondition, recommendedState, recommendedCacheType, pRequestState, pRequestedCacheType, pfCancel);
    }


    virtual STDMETHODIMP OnPlanMsiPackage(
        __in_z LPCWSTR wzPackageId,
        __in BOOL fExecute,
        __in BOOTSTRAPPER_ACTION_STATE action,
        __in BOOTSTRAPPER_MSI_FILE_VERSIONING recommendedFileVersioning,
        __inout BOOL* pfCancel,
        __inout BURN_MSI_PROPERTY* pActionMsiProperty,
        __inout INSTALLUILEVEL* pUiLevel,
        __inout BOOL* pfDisableExternalUiHandler,
        __inout BOOTSTRAPPER_MSI_FILE_VERSIONING* pFileVersioning
        )
    {
        INSTALLUILEVEL uiLevel = INSTALLUILEVEL_NOCHANGE;

        if (m_fAutomaticRemoval)
        {
            ExitFunction();
        }

        switch (m_commandDisplay)
        {
        case BOOTSTRAPPER_DISPLAY_FULL:
            uiLevel = INSTALLUILEVEL_FULL;
            break;

        case BOOTSTRAPPER_DISPLAY_PASSIVE:
            uiLevel = INSTALLUILEVEL_REDUCED;
            break;
        }

        if (INSTALLUILEVEL_NOCHANGE != uiLevel)
        {
            *pUiLevel = uiLevel;
        }

        *pActionMsiProperty = BURN_MSI_PROPERTY_NONE;
        *pfDisableExternalUiHandler = TRUE;

    LExit:
        return __super::OnPlanMsiPackage(wzPackageId, fExecute, action, recommendedFileVersioning, pfCancel, pActionMsiProperty, pUiLevel, pfDisableExternalUiHandler, pFileVersioning);
    }


    virtual STDMETHODIMP OnPlanComplete(
        __in HRESULT hrStatus
        )
    {
        if (SUCCEEDED(hrStatus))
        {
            ::PostMessageW(m_hWnd, WM_WIXIUIBA_APPLY_PACKAGES, 0, 0);
        }
        else if (m_fAutomaticRemoval)
        {
            ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);
        }
        else
        {
            SetLoadPackageFailure(hrStatus);
        }

        return __super::OnPlanComplete(hrStatus);
    }


    virtual STDMETHODIMP OnApplyBegin(
        __in DWORD dwPhaseCount,
        __inout BOOL* pfCancel
        )
    {
        m_fApplying = TRUE;
        return __super::OnApplyBegin(dwPhaseCount, pfCancel);
    }


    virtual STDMETHODIMP OnCacheComplete(
        __in HRESULT hrStatus
        )
    {
        if (FAILED(hrStatus) && !m_fAutomaticRemoval)
        {
            SetLoadPackageFailure(hrStatus);
        }

        return __super::OnCacheComplete(hrStatus);
    }


    virtual STDMETHODIMP OnExecuteBegin(
        __in DWORD cExecutingPackages,
        __in BOOL* pfCancel
        )
    {
        m_pEngine->CloseSplashScreen();

        return __super::OnExecuteBegin(cExecutingPackages, pfCancel);
    }


    virtual STDMETHODIMP OnApplyComplete(
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_APPLY_RESTART restart,
        __in BOOTSTRAPPER_APPLYCOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* pAction
        )
    {
        HRESULT hr = __super::OnApplyComplete(hrStatus, restart, recommendation, pAction);

        *pAction = BOOTSTRAPPER_APPLYCOMPLETE_ACTION_NONE;
        m_fApplying = FALSE;

        if (m_fAutomaticRemoval)
        {
            ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);
        }
        else
        {
            m_restartResult = restart; // remember the restart result so we return the correct error code.
            m_fApplied = TRUE;

            if (FAILED(hrStatus))
            {
                m_hrFinal = hrStatus;
            }

            ::PostMessageW(m_hWnd, WM_WIXIUIBA_DETECT_FOR_CLEANUP, 0, 0);
        }

        return hr;
    }


private:
    //
    // UiThreadProc - entrypoint for UI thread.
    //
    static DWORD WINAPI UiThreadProc(
        __in LPVOID pvContext
        )
    {
        HRESULT hr = S_OK;
        CWixInternalUIBootstrapperApplication* pThis = (CWixInternalUIBootstrapperApplication*)pvContext;
        BOOL fComInitialized = FALSE;
        BOOL fRet = FALSE;
        MSG msg = { };
        DWORD dwQuit = 0;

        // Initialize COM.
        hr = ::CoInitialize(NULL);
        BalExitOnFailure(hr, "Failed to initialize COM.");
        fComInitialized = TRUE;

        // Create main window.
        hr = pThis->CreateMainWindow();
        BalExitOnFailure(hr, "Failed to create internal UI bootstrapper application main window.");

        ::PostMessageW(pThis->m_hWnd, WM_WIXIUIBA_DETECT_PACKAGES, 0, 0);

        // message pump
        while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
        {
            if (-1 == fRet)
            {
                hr = E_UNEXPECTED;
                BalExitOnFailure(hr, "Unexpected return value from message pump.");
            }
            else if (!::IsDialogMessageW(pThis->m_hWnd, &msg))
            {
                ::TranslateMessage(&msg);
                ::DispatchMessageW(&msg);
            }
        }

        // Succeeded thus far, check to see if anything went wrong while actually
        // executing changes.
        if (FAILED(pThis->m_hrFinal))
        {
            hr = pThis->m_hrFinal;
        }
        else if (pThis->CheckCanceled())
        {
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
        }

    LExit:
        // destroy main window
        pThis->DestroyMainWindow();

        if (BOOTSTRAPPER_APPLY_RESTART_INITIATED == pThis->m_restartResult)
        {
            dwQuit = SUCCEEDED(hr) ? ERROR_SUCCESS_REBOOT_INITIATED : ERROR_FAIL_REBOOT_INITIATED;
        }
        else if (BOOTSTRAPPER_APPLY_RESTART_REQUIRED == pThis->m_restartResult)
        {
            dwQuit = SUCCEEDED(hr) ? ERROR_SUCCESS_REBOOT_REQUIRED : ERROR_FAIL_REBOOT_REQUIRED;
        }
        else if (SEVERITY_ERROR == HRESULT_SEVERITY(hr) && FACILITY_WIN32 == HRESULT_FACILITY(hr))
        {
            // Convert Win32 HRESULTs back to the error code.
            dwQuit = HRESULT_CODE(hr);
        }
        else
        {
            dwQuit = hr;
        }

        // initiate engine shutdown
        pThis->m_pEngine->Quit(dwQuit);

        // uninitialize COM
        if (fComInitialized)
        {
            ::CoUninitialize();
        }

        return hr;
    }


    //
    // InitializeData - initializes all the package and prerequisite information.
    //
    HRESULT InitializeData()
    {
        HRESULT hr = S_OK;
        IXMLDOMDocument* pixdManifest = NULL;

        hr = XmlInitialize();
        BalExitOnFailure(hr, "Failed to initialize XML.");

        hr = BalManifestLoad(m_hModule, &pixdManifest);
        BalExitOnFailure(hr, "Failed to load bootstrapper application manifest.");

        hr = BalInfoParseFromXml(&m_Bundle, pixdManifest);
        BalExitOnFailure(hr, "Failed to load bundle information.");

        hr = EnsureSinglePrimaryPackage();
        BalExitOnFailure(hr, "Failed to ensure single primary package.");

        hr = ProcessCommandLine();
        ExitOnFailure(hr, "Unknown commandline parameters.");

        hr = BalConditionsParseFromXml(&m_Conditions, pixdManifest, NULL);
        BalExitOnFailure(hr, "Failed to load conditions from XML.");

    LExit:
        ReleaseObject(pixdManifest);

        return hr;
    }


    //
    // ProcessCommandLine - process the provided command line arguments.
    //
    HRESULT ProcessCommandLine()
    {
        HRESULT hr = S_OK;
        int argc = 0;
        LPWSTR* argv = NULL;

        argc = m_BalInfoCommand.cUnknownArgs;
        argv = m_BalInfoCommand.rgUnknownArgs;

        for (int i = 0; i < argc; ++i)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Ignoring unknown argument: %ls", argv[i]);
        }

        hr = BalSetOverridableVariablesFromEngine(&m_Bundle.overridableVariables, &m_BalInfoCommand, m_pEngine);
        BalExitOnFailure(hr, "Failed to set overridable variables from the command line.");

    LExit:
        return hr;
    }

    HRESULT EnsureSinglePrimaryPackage()
    {
        HRESULT hr = S_OK;
        BAL_INFO_PACKAGE* pDefaultPackage = NULL;
        BOOL fPrimaryArchSpecific = FALSE;
        USHORT usNativeMachine = 0;
        BAL_INFO_PRIMARY_PACKAGE_TYPE nativeType = BAL_INFO_PRIMARY_PACKAGE_TYPE_NONE;

        hr = ProcNativeMachine(::GetCurrentProcess(), &usNativeMachine);
        BalExitOnFailure(hr, "Failed to get native machine value.");

        if (S_FALSE != hr)
        {
            switch (usNativeMachine)
            {
            case IMAGE_FILE_MACHINE_I386:
                nativeType = BAL_INFO_PRIMARY_PACKAGE_TYPE_X86;
                break;
            case IMAGE_FILE_MACHINE_AMD64:
                nativeType = BAL_INFO_PRIMARY_PACKAGE_TYPE_X64;
                break;
            case IMAGE_FILE_MACHINE_ARM64:
                nativeType = BAL_INFO_PRIMARY_PACKAGE_TYPE_ARM64;
                break;
            }
        }
        else
        {
#if !defined(_WIN64)
            BOOL fIsWow64 = FALSE;

            ProcWow64(::GetCurrentProcess(), &fIsWow64);
            if (!fIsWow64)
            {
                nativeType = BAL_INFO_PRIMARY_PACKAGE_TYPE_X86;
            }
            else
#endif
            {
                nativeType = BAL_INFO_PRIMARY_PACKAGE_TYPE_X64;
            }
        }

        for (DWORD i = 0; i < m_Bundle.packages.cPackages; ++i)
        {
            BAL_INFO_PACKAGE* pPackage = m_Bundle.packages.rgPackages + i;

            if (BAL_INFO_PRIMARY_PACKAGE_TYPE_NONE == pPackage->primaryPackageType)
            {
                // Skip.
            }
            else if (nativeType == pPackage->primaryPackageType)
            {
                if (fPrimaryArchSpecific)
                {
                    BalExitWithRootFailure(hr, E_INVALIDDATA, "Bundle contains multiple primary packages for same architecture: %u.", nativeType);
                }

                pPackage->primaryPackageType = BAL_INFO_PRIMARY_PACKAGE_TYPE_DEFAULT;
                fPrimaryArchSpecific = TRUE;
            }
            else if (BAL_INFO_PRIMARY_PACKAGE_TYPE_DEFAULT == pPackage->primaryPackageType)
            {
                if (pDefaultPackage)
                {
                    BalExitWithRootFailure(hr, E_INVALIDDATA, "Bundle contains multiple default primary packages.");
                }

                pDefaultPackage = pPackage;
            }
        }

        BalExitOnNull(pDefaultPackage, hr, E_INVALIDSTATE, "Bundle did not contain default primary package.");

        if (fPrimaryArchSpecific)
        {
            pDefaultPackage->primaryPackageType = BAL_INFO_PRIMARY_PACKAGE_TYPE_NONE;
        }

    LExit:
        return hr;
    }


    //
    // CreateMainWindow - creates the main install window.
    //
    HRESULT CreateMainWindow()
    {
        HRESULT hr = S_OK;
        WNDCLASSW wc = { };
        DWORD dwWindowStyle = WS_POPUP;

        wc.lpfnWndProc = CWixInternalUIBootstrapperApplication::WndProc;
        wc.hInstance = m_hModule;
        wc.lpszClassName = WIXIUIBA_WINDOW_CLASS;

        if (!::RegisterClassW(&wc))
        {
            ExitWithLastError(hr, "Failed to register window.");
        }

        m_fRegistered = TRUE;

        // If the UI should be visible, allow it to be visible and activated so we are the foreground window.
        // This allows the UAC prompt and MSI UI to automatically be activated.
        if (BOOTSTRAPPER_DISPLAY_NONE < m_commandDisplay)
        {
            dwWindowStyle |= WS_VISIBLE;
        }

        m_hWnd = ::CreateWindowExW(WS_EX_TOOLWINDOW, wc.lpszClassName, NULL, dwWindowStyle, 0, 0, 0, 0, HWND_DESKTOP, NULL, m_hModule, this);
        ExitOnNullWithLastError(m_hWnd, hr, "Failed to create internal UI main window.");

    LExit:
        return hr;
    }

    //
    // DestroyMainWindow - clean up all the window registration.
    //
    void DestroyMainWindow()
    {
        if (::IsWindow(m_hWnd))
        {
            ::DestroyWindow(m_hWnd);
            m_hWnd = NULL;
        }

        if (m_fRegistered)
        {
            ::UnregisterClassW(WIXIUIBA_WINDOW_CLASS, m_hModule);
            m_fRegistered = FALSE;
        }
    }

    //
    // WndProc - standard windows message handler.
    //
    static LRESULT CALLBACK WndProc(
        __in HWND hWnd,
        __in UINT uMsg,
        __in WPARAM wParam,
        __in LPARAM lParam
        )
    {
#pragma warning(suppress:4312)
        CWixInternalUIBootstrapperApplication* pBA = reinterpret_cast<CWixInternalUIBootstrapperApplication*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

        switch (uMsg)
        {
        case WM_NCCREATE:
        {
            LPCREATESTRUCT lpcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
            pBA = reinterpret_cast<CWixInternalUIBootstrapperApplication*>(lpcs->lpCreateParams);
#pragma warning(suppress:4244)
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pBA));
        }
        break;

        case WM_NCDESTROY:
        {
            LRESULT lres = ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
            ::PostQuitMessage(0);
            return lres;
        }

        case WM_CLOSE:
            // If the user chose not to close, do *not* let the default window proc handle the message.
            if (!pBA->OnClose())
            {
                return 0;
            }
            break;

        case WM_WIXIUIBA_DETECT_PACKAGES: __fallthrough;
        case WM_WIXIUIBA_DETECT_FOR_CLEANUP:
            pBA->OnDetect();
            return 0;

        case WM_WIXIUIBA_PLAN_PACKAGES:
        case WM_WIXIUIBA_PLAN_PACKAGES_FOR_CLEANUP:
            pBA->OnPlan(static_cast<BOOTSTRAPPER_ACTION>(lParam));
            return 0;

        case WM_WIXIUIBA_APPLY_PACKAGES:
            pBA->OnApply();
            return 0;
        }

        return ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
    }


    //
    // OnDetect - start the processing of packages.
    //
    void OnDetect()
    {
        HRESULT hr = S_OK;

        hr = m_pEngine->Detect();
        BalExitOnFailure(hr, "Failed to start detecting chain.");

    LExit:
        if (FAILED(hr))
        {
            SetLoadPackageFailure(hr);
        }
    }


    //
    // OnPlan - plan the detected changes.
    //
    void OnPlan(
        __in BOOTSTRAPPER_ACTION action
        )
    {
        HRESULT hr = S_OK;

        m_plannedAction = action;

        hr = m_pEngine->Plan(action);
        BalExitOnFailure(hr, "Failed to start planning packages.");

    LExit:
        if (FAILED(hr))
        {
            SetLoadPackageFailure(hr);
        }
    }


    //
    // OnApply - apply the packages.
    //
    void OnApply()
    {
        HRESULT hr = S_OK;

        hr = m_pEngine->Apply(m_hWnd);
        BalExitOnFailure(hr, "Failed to start applying packages.");

    LExit:
        if (FAILED(hr))
        {
            SetLoadPackageFailure(hr);
        }
    }


    //
    // OnClose - called when the window is trying to be closed.
    //
    BOOL OnClose()
    {
        BOOL fClose = FALSE;

        // If we've already applied, just close.
        if (m_fApplied)
        {
            fClose = TRUE;
        }
        else
        {
            PromptCancel(m_hWnd, TRUE, NULL, NULL);

            // If we're inside Apply then we never close, we just cancel to let rollback occur.
            fClose = !m_fApplying;
        }

        return fClose;
    }


    HRESULT EvaluateConditions()
    {
        HRESULT hr = S_OK;
        BOOL fResult = FALSE;

        for (DWORD i = 0; i < m_Conditions.cConditions; ++i)
        {
            BAL_CONDITION* pCondition = m_Conditions.rgConditions + i;

            hr = BalConditionEvaluate(pCondition, m_pEngine, &fResult, &m_sczFailedMessage);
            BalExitOnFailure(hr, "Failed to evaluate condition.");

            if (!fResult)
            {
                hr = E_WIXSTDBA_CONDITION_FAILED;
                BalExitOnFailure(hr, "%ls", m_sczFailedMessage);
            }
        }

        ReleaseNullStrSecure(m_sczFailedMessage);

    LExit:
        return hr;
    }


    void SetLoadPackageFailure(
        __in HRESULT hrStatus
        )
    {
        Assert(FAILED(hrStatus));

        if (!m_fApplied)
        {
            m_hrFinal = hrStatus;
            m_fFailedToLoadPackage = TRUE;
        }

        // Quietly exit.
        ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);
    }


public:
    //
    // Constructor - initialize member variables.
    //
    CWixInternalUIBootstrapperApplication(
        __in HMODULE hModule
        ) : CBalBaseBootstrapperApplication(3, 3000)
    {
        m_hModule = hModule;
        m_commandAction = BOOTSTRAPPER_ACTION_UNKNOWN;
        m_commandDisplay = BOOTSTRAPPER_DISPLAY_UNKNOWN;

        m_plannedAction = BOOTSTRAPPER_ACTION_UNKNOWN;

        m_Bundle = { };
        m_Conditions = { };
        m_sczConfirmCloseMessage = NULL;
        m_sczFailedMessage = NULL;

        m_hUiThread = NULL;
        m_fRegistered = FALSE;
        m_hWnd = NULL;

        m_hrFinal = S_OK;

        m_restartResult = BOOTSTRAPPER_APPLY_RESTART_NONE;

        m_fApplying = FALSE;
        m_fApplied = FALSE;
        m_fAutomaticRemoval = FALSE;
        m_fFailedToLoadPackage = FALSE;
    }


    //
    // Destructor - release member variables.
    //
    ~CWixInternalUIBootstrapperApplication()
    {
        ReleaseStr(m_sczFailedMessage);
        ReleaseStr(m_sczConfirmCloseMessage);
        BalConditionsUninitialize(&m_Conditions);
        BalInfoUninitialize(&m_Bundle);

        ReleaseNullObject(m_pEngine);
    }

private:
    HMODULE m_hModule;
    BOOTSTRAPPER_ACTION m_commandAction;
    BOOTSTRAPPER_DISPLAY m_commandDisplay;

    BOOTSTRAPPER_ACTION m_plannedAction;

    BAL_INFO_BUNDLE m_Bundle;
    BAL_CONDITIONS m_Conditions;
    LPWSTR m_sczFailedMessage;
    LPWSTR m_sczConfirmCloseMessage;

    HANDLE m_hUiThread;
    BOOL m_fRegistered;
    HWND m_hWnd;

    HRESULT m_hrFinal;

    BOOTSTRAPPER_APPLY_RESTART m_restartResult;

    BOOL m_fApplying;
    BOOL m_fApplied;
    BOOL m_fAutomaticRemoval;
    BOOL m_fFailedToLoadPackage;
};


//
// CreateBootstrapperApplication - creates a new IBootstrapperApplication object.
//
EXTERN_C HRESULT CreateWixInternalUIBootstrapperApplication(
    __in HMODULE hInstance,
    __out IBootstrapperApplication** ppApplication
)
{
    HRESULT hr = S_OK;

    CWixInternalUIBootstrapperApplication* pApplication = new CWixInternalUIBootstrapperApplication(hInstance);
    ExitOnNull(pApplication, hr, E_OUTOFMEMORY, "Failed to create new internal UI bootstrapper application.");

    hr = pApplication->QueryInterface(IID_PPV_ARGS(ppApplication));
    ExitOnRootFailure(hr, "Failed to query for IBootstrapperApplication.");

LExit:
    ReleaseObject(pApplication);

    return hr;
}
