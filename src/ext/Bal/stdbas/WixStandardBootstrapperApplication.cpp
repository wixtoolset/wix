// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR WIXBUNDLE_VARIABLE_CANRESTART = L"WixCanRestart";
static const LPCWSTR WIXBUNDLE_VARIABLE_ELEVATED = L"WixBundleElevated";

static const LPCWSTR WIXSTDBA_WINDOW_CLASS = L"WixStdBA";

static const LPCWSTR WIXSTDBA_VARIABLE_INSTALL_FOLDER = L"InstallFolder";
static const LPCWSTR WIXSTDBA_VARIABLE_LAUNCH_TARGET_PATH = L"LaunchTarget";
static const LPCWSTR WIXSTDBA_VARIABLE_LAUNCH_TARGET_ELEVATED_ID = L"LaunchTargetElevatedId";
static const LPCWSTR WIXSTDBA_VARIABLE_LAUNCH_ARGUMENTS = L"LaunchArguments";
static const LPCWSTR WIXSTDBA_VARIABLE_LAUNCH_HIDDEN = L"LaunchHidden";
static const LPCWSTR WIXSTDBA_VARIABLE_LAUNCH_WORK_FOLDER = L"LaunchWorkingFolder";

static const DWORD WIXSTDBA_ACQUIRE_PERCENTAGE = 30;

static const LPCWSTR WIXSTDBA_VARIABLE_BUNDLE_FILE_VERSION = L"WixBundleFileVersion";
static const LPCWSTR WIXSTDBA_VARIABLE_LANGUAGE_ID = L"WixStdBALanguageId";
static const LPCWSTR WIXSTDBA_VARIABLE_RESTART_REQUIRED = L"WixStdBARestartRequired";
static const LPCWSTR WIXSTDBA_VARIABLE_SHOW_VERSION = L"WixStdBAShowVersion";
static const LPCWSTR WIXSTDBA_VARIABLE_SUPPRESS_OPTIONS_UI = L"WixStdBASuppressOptionsUI";
static const LPCWSTR WIXSTDBA_VARIABLE_UPDATE_AVAILABLE = L"WixStdBAUpdateAvailable";

enum WIXSTDBA_STATE
{
    WIXSTDBA_STATE_INITIALIZING,
    WIXSTDBA_STATE_INITIALIZED,
    WIXSTDBA_STATE_HELP,
    WIXSTDBA_STATE_DETECTING,
    WIXSTDBA_STATE_DETECTED,
    WIXSTDBA_STATE_PLANNING_PREREQS,
    WIXSTDBA_STATE_PLANNED_PREREQS,
    WIXSTDBA_STATE_PLANNING,
    WIXSTDBA_STATE_PLANNED,
    WIXSTDBA_STATE_APPLYING,
    WIXSTDBA_STATE_CACHING,
    WIXSTDBA_STATE_CACHED,
    WIXSTDBA_STATE_EXECUTING,
    WIXSTDBA_STATE_EXECUTED,
    WIXSTDBA_STATE_APPLIED,
    WIXSTDBA_STATE_FAILED,
};

enum WM_WIXSTDBA
{
    WM_WIXSTDBA_SHOW_HELP = WM_APP + 100,
    WM_WIXSTDBA_DETECT_PACKAGES,
    WM_WIXSTDBA_PLAN_PACKAGES,
    WM_WIXSTDBA_APPLY_PACKAGES,
    WM_WIXSTDBA_CHANGE_STATE,
    WM_WIXSTDBA_SHOW_FAILURE,
    WM_WIXSTDBA_PLAN_PREREQS,
};

// This enum must be kept in the same order as the vrgwzPageNames array.
enum WIXSTDBA_PAGE
{
    WIXSTDBA_PAGE_LOADING,
    WIXSTDBA_PAGE_HELP,
    WIXSTDBA_PAGE_INSTALL,
    WIXSTDBA_PAGE_MODIFY,
    WIXSTDBA_PAGE_PROGRESS,
    WIXSTDBA_PAGE_PROGRESS_PASSIVE,
    WIXSTDBA_PAGE_SUCCESS,
    WIXSTDBA_PAGE_FAILURE,
    COUNT_WIXSTDBA_PAGE,
};

// This array must be kept in the same order as the WIXSTDBA_PAGE enum.
static LPCWSTR vrgwzPageNames[] = {
    L"Loading",
    L"Help",
    L"Install",
    L"Modify",
    L"Progress",
    L"ProgressPassive",
    L"Success",
    L"Failure",
};

// The range [0, 100) is unused to avoid collisions with system ids,
// the range [100, 0x4000) is unused to avoid collisions with thmutil,
// the range [0x4000, 0x8000) is unused to avoid collisions with BAFunctions.
const WORD WIXSTDBA_FIRST_ASSIGN_CONTROL_ID = 0x8000;

enum WIXSTDBA_CONTROL
{
    // Welcome page
    WIXSTDBA_CONTROL_INSTALL_BUTTON = WIXSTDBA_FIRST_ASSIGN_CONTROL_ID,
    WIXSTDBA_CONTROL_EULA_RICHEDIT,
    WIXSTDBA_CONTROL_EULA_LINK,
    WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX,

    // Modify page
    WIXSTDBA_CONTROL_REPAIR_BUTTON,
    WIXSTDBA_CONTROL_UNINSTALL_BUTTON,

    // Updates
    WIXSTDBA_CONTROL_CHECKING_FOR_UPDATES_LABEL,
    WIXSTDBA_CONTROL_INSTALL_UPDATE_BUTTON,
    WIXSTDBA_CONTROL_MODIFY_UPDATE_BUTTON,

    // Progress page
    WIXSTDBA_CONTROL_CACHE_PROGRESS_PACKAGE_TEXT,
    WIXSTDBA_CONTROL_CACHE_PROGRESS_BAR,
    WIXSTDBA_CONTROL_CACHE_PROGRESS_TEXT,

    WIXSTDBA_CONTROL_EXECUTE_PROGRESS_PACKAGE_TEXT,
    WIXSTDBA_CONTROL_EXECUTE_PROGRESS_BAR,
    WIXSTDBA_CONTROL_EXECUTE_PROGRESS_TEXT,
    WIXSTDBA_CONTROL_EXECUTE_PROGRESS_ACTIONDATA_TEXT,

    WIXSTDBA_CONTROL_OVERALL_PROGRESS_PACKAGE_TEXT,
    WIXSTDBA_CONTROL_OVERALL_PROGRESS_BAR,
    WIXSTDBA_CONTROL_OVERALL_CALCULATED_PROGRESS_BAR,
    WIXSTDBA_CONTROL_OVERALL_PROGRESS_TEXT,

    WIXSTDBA_CONTROL_PROGRESS_CANCEL_BUTTON,

    // Success page
    WIXSTDBA_CONTROL_LAUNCH_BUTTON,
    WIXSTDBA_CONTROL_SUCCESS_RESTART_BUTTON,

    // Failure page
    WIXSTDBA_CONTROL_FAILURE_LOGFILE_LINK,
    WIXSTDBA_CONTROL_FAILURE_MESSAGE_TEXT,
    WIXSTDBA_CONTROL_FAILURE_RESTART_BUTTON,

    LAST_WIXSTDBA_CONTROL,
};


static HRESULT DAPI EvaluateVariableConditionCallback(
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf,
    __in_opt LPVOID pvContext
    );
static HRESULT DAPI FormatVariableStringCallback(
    __in_z LPCWSTR wzFormat,
    __inout LPWSTR* psczOut,
    __in_opt LPVOID pvContext
    );
static HRESULT DAPI GetVariableNumericCallback(
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue,
    __in_opt LPVOID pvContext
    );
static HRESULT DAPI SetVariableNumericCallback(
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue,
    __in_opt LPVOID pvContext
    );
static HRESULT DAPI GetVariableStringCallback(
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue,
    __in_opt LPVOID pvContext
    );
static HRESULT DAPI SetVariableStringCallback(
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in BOOL fFormatted,
    __in_opt LPVOID pvContext
    );
static LPCSTR LoggingBoolToString(
    __in BOOL f
    );
static LPCSTR LoggingRequestStateToString(
    __in BOOTSTRAPPER_REQUEST_STATE requestState
    );
static LPCSTR LoggingPlanRelationTypeToString(
    __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE type
    );
static LPCSTR LoggingMsiFeatureStateToString(
    __in BOOTSTRAPPER_FEATURE_STATE featureState
    );


class CWixStandardBootstrapperApplication : public CBootstrapperApplicationBase
{
public: // IBootstrapperApplication
    virtual STDMETHODIMP OnCreate(
        __in IBootstrapperEngine* pEngine,
        __in BOOTSTRAPPER_COMMAND* pCommand
    )
    {
        HRESULT hr = S_OK;

        hr = __super::OnCreate(pEngine, pCommand);
        BalExitOnFailure(hr, "CBootstrapperApplicationBase initialization failed.");

        m_commandAction = pCommand->action;
        m_commandDisplay = pCommand->display;
        m_commandResumeType = pCommand->resumeType;
        m_commandRelationType = pCommand->relationType;
        m_hwndSplashScreen = pCommand->hwndSplashScreen;

        hr = BalGetStringVariable(L"WixBundleVersion", &m_sczBundleVersion);
        BalExitOnFailure(hr, "CWixStandardBootstrapperApplication initialization failed.");

        hr = InitializeData(pCommand);
        BalExitOnFailure(hr, "Failed to initialize data in bootstrapper application.");

    LExit:
        return hr;
    }

    STDMETHODIMP OnDestroy(
        __in BOOL fReload
    )
    {
        if (m_hBAFModule)
        {
            BA_FUNCTIONS_DESTROY_ARGS args = { };
            BA_FUNCTIONS_DESTROY_RESULTS results = { };

            args.cbSize = sizeof(BA_FUNCTIONS_DESTROY_ARGS);
            args.fReload = fReload;

            results.cbSize = sizeof(BA_FUNCTIONS_DESTROY_RESULTS);

            PFN_BA_FUNCTIONS_DESTROY pfnBAFunctionsDestroy = reinterpret_cast<PFN_BA_FUNCTIONS_DESTROY>(::GetProcAddress(m_hBAFModule, "BAFunctionsDestroy"));
            if (pfnBAFunctionsDestroy)
            {
                pfnBAFunctionsDestroy(&args, &results);
            }

            if (!results.fDisableUnloading)
            {
                m_pfnBAFunctionsProc = NULL;
                m_pvBAFunctionsProcContext = NULL;

                ::FreeLibrary(m_hBAFModule);
                m_hBAFModule = NULL;
            }
        }

        return __super::OnDestroy(fReload);
    }

    STDMETHODIMP OnStartup()
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


    STDMETHODIMP OnShutdown(
        __inout BOOTSTRAPPER_SHUTDOWN_ACTION* pAction
        )
    {
        HRESULT hr = S_OK;

        // wait for UI thread to terminate
        if (m_hUiThread)
        {
            ::WaitForSingleObject(m_hUiThread, INFINITE);
            ReleaseHandle(m_hUiThread);
        }

        // If a restart was required.
        if (m_fRestartRequired)
        {
            if (m_fShouldRestart && m_fAllowRestart)
            {
                *pAction = BOOTSTRAPPER_SHUTDOWN_ACTION_RESTART;
            }

            if (m_fPrereq)
            {
                BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, BOOTSTRAPPER_SHUTDOWN_ACTION_RESTART == *pAction
                    ? "The prerequisites scheduled a restart. The bootstrapper application will be reloaded after the computer is restarted."
                    : "A restart is required by the prerequisites but the user delayed it. The bootstrapper application will be reloaded after the computer is restarted.");
            }
        }
        else if (m_fPrereqInstalled || m_fPrereqSkipped)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, m_fPrereqInstalled
                ? "The prerequisites were successfully installed. The bootstrapper application will be reloaded."
                : "The prerequisites were already installed. The bootstrapper application will be reloaded.");
            *pAction = BOOTSTRAPPER_SHUTDOWN_ACTION_RELOAD_BOOTSTRAPPER;
        }
        else if (m_fPrereq)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "The prerequisites were not successfully installed, error: 0x%x. The bootstrapper application will be not reloaded.", m_hrFinal);
        }

        return hr;
    }

    virtual STDMETHODIMP OnDetectBegin(
        __in BOOL fCached,
        __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType,
        __in DWORD cPackages,
        __inout BOOL* pfCancel
        )
    {
        HRESULT hr = S_OK;
        BOOL fInstalled = BOOTSTRAPPER_REGISTRATION_TYPE_FULL == registrationType;

        if (m_fPrereq)
        {
            // Pre-requisite command action is set during initialization.
        }
        else if (BOOTSTRAPPER_DISPLAY_FULL <= m_commandDisplay) // only modify the action state if showing full UI.
        {
            // Maybe modify the action state if the bundle is or is not already installed.
            if (fInstalled && BOOTSTRAPPER_RESUME_TYPE_REBOOT != m_commandResumeType && BOOTSTRAPPER_ACTION_INSTALL == m_commandAction)
            {
                m_commandAction = BOOTSTRAPPER_ACTION_MODIFY;
            }
            else if (!fInstalled && (BOOTSTRAPPER_ACTION_MODIFY == m_commandAction || BOOTSTRAPPER_ACTION_REPAIR == m_commandAction))
            {
                m_commandAction = BOOTSTRAPPER_ACTION_INSTALL;
            }
        }

        // When resuming from restart doing some install-like operation, try to find the package that forced the
        // restart. We'll use this information during planning.
        if (BOOTSTRAPPER_RESUME_TYPE_REBOOT == m_commandResumeType && BOOTSTRAPPER_ACTION_UNINSTALL < m_commandAction)
        {
            // Ensure the forced restart package variable is null when it is an empty string.
            hr = BalGetStringVariable(L"WixBundleForcedRestartPackage", &m_sczAfterForcedRestartPackage);
            if (FAILED(hr) || !m_sczAfterForcedRestartPackage || !*m_sczAfterForcedRestartPackage)
            {
                ReleaseNullStr(m_sczAfterForcedRestartPackage);
            }

            hr = S_OK;
        }

        if (!m_fPreplanPrereqs)
        {
            // If the UI should be visible, display it now and hide the splash screen
            if (BOOTSTRAPPER_DISPLAY_NONE < m_commandDisplay)
            {
                ::ShowWindow(m_pTheme->hwndParent, SW_SHOW);
            }

            m_pEngine->CloseSplashScreen();
        }

        return __super::OnDetectBegin(fCached, registrationType, cPackages, pfCancel);
    }

    virtual STDMETHODIMP OnDetectRelatedBundle(
        __in LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_RELATION_TYPE relationType,
        __in LPCWSTR wzBundleTag,
        __in BOOL fPerMachine,
        __in LPCWSTR wzVersion,
        __in BOOL fMissingFromCache,
        __inout BOOL* pfCancel
        )
    {
        BAL_INFO_PACKAGE* pPackage = NULL;

        if (!fMissingFromCache)
        {
            BalInfoAddRelatedBundleAsPackage(&m_Bundle.packages, wzBundleId, relationType, fPerMachine, &pPackage);
            // Best effort
        }

        if (BOOTSTRAPPER_ACTION_INSTALL == m_commandAction && BOOTSTRAPPER_RELATION_UPGRADE != m_commandRelationType && BOOTSTRAPPER_RELATION_UPGRADE == relationType)
        {
            int nResult = 0;
            HRESULT hr = VerCompareStringVersions(m_sczBundleVersion, wzVersion, TRUE/*fStrict*/, &nResult);
            BalExitOnFailure(hr, "Failed to compare bundle version: %ls to related bundle version: %ls.", m_sczBundleVersion, wzVersion);

            if (0 > nResult)
            {
                m_fDowngrading = TRUE;

                BalLog(BOOTSTRAPPER_LOG_LEVEL_VERBOSE, "Related bundle version: %ls is a downgrade for bundle version: %ls.", wzVersion, m_sczBundleVersion);
            }
        }

    LExit:
        return CBootstrapperApplicationBase::OnDetectRelatedBundle(wzBundleId, relationType, wzBundleTag, fPerMachine, wzVersion, fMissingFromCache, pfCancel);
    }


    virtual STDMETHODIMP OnDetectUpdateBegin(
        __in_z LPCWSTR wzUpdateLocation,
        __inout BOOL* pfCancel,
        __inout BOOL* pfSkip
    )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnDetectUpdateBegin() - update location: %ls", wzUpdateLocation);
#endif

        // Try update detection only if we have a potential update source and are in full UI mode.
        *pfSkip = !wzUpdateLocation
            || !*wzUpdateLocation
            || BOOTSTRAPPER_DISPLAY_FULL != m_commandDisplay;

        ThemeShowControl(m_pControlCheckingForUpdatesLabel, *pfSkip ? SW_HIDE : SW_SHOW);

        return __super::OnDetectUpdateBegin(wzUpdateLocation, pfCancel, pfSkip);
    }


    virtual STDMETHODIMP OnDetectUpdate(
        __in_z LPCWSTR wzUpdateLocation,
        __in DWORD64 dw64Size,
        __in_z_opt LPCWSTR wzHash,
        __in BOOTSTRAPPER_UPDATE_HASH_TYPE hashAlgorithm,
        __in LPCWSTR wzUpdateVersion,
        __in_z LPCWSTR wzTitle,
        __in_z LPCWSTR wzSummary,
        __in_z LPCWSTR wzContentType,
        __in_z LPCWSTR wzContent,
        __inout BOOL* pfCancel,
        __inout BOOL* pfStopProcessingUpdates
    )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnDetectUpdate() - update location: %ls, version: %ls", wzUpdateLocation, wzUpdateVersion);
#endif

        HRESULT hr = S_OK;
        int nResult = 0;
        UUID guid = { };
        WCHAR wzUpdatePackageId[39];
        RPC_STATUS rs = RPC_S_OK;

        hr = VerCompareStringVersions(m_sczBundleVersion, wzUpdateVersion, TRUE/*fStrict*/, &nResult);
        BalExitOnFailure(hr, "Failed to compare bundle version: %ls to update version: %ls.", m_sczBundleVersion, wzUpdateVersion);

        // Burn sends the feed in descending version order so we need only the first one.
        *pfStopProcessingUpdates = TRUE;

        if (0 <= nResult)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_VERBOSE, "WIXSTDBA: Update version: %ls is a match or downgrade for bundle version: %ls.", wzUpdateVersion, m_sczBundleVersion);
        }
        else
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: Update v%ls for bundle v%ls available from: %ls.", wzUpdateVersion, m_sczBundleVersion, wzUpdateLocation);

            rs = ::UuidCreate(&guid);
            hr = HRESULT_FROM_RPC(rs);
            ExitOnFailure(hr, "Failed to generate bundle update package id.");

            if (!::StringFromGUID2(guid, wzUpdatePackageId, countof(wzUpdatePackageId)))
            {
                hr = E_OUTOFMEMORY;
                ExitOnRootFailure(hr, "Failed to create string from bundle update package id.");
            }

            hr = BalSetVersionVariable(WIXSTDBA_VARIABLE_UPDATE_AVAILABLE, wzUpdateVersion);
            BalExitOnFailure(hr, "Failed to set WixStdBAUpdateAvailable value: %ls.", wzUpdateVersion);

            hr = m_pEngine->SetUpdate(NULL, wzUpdateLocation, dw64Size, hashAlgorithm, wzHash, wzUpdatePackageId);
            BalExitOnFailure(hr, "Failed to set update location: %ls.", wzUpdateLocation);

            BalInfoAddUpdateBundleAsPackage(&m_Bundle.packages, wzUpdatePackageId, NULL);
        }

    LExit:
        return __super::OnDetectUpdate(wzUpdateLocation, dw64Size, wzHash, hashAlgorithm, wzUpdateVersion, wzTitle, wzSummary, wzContentType, wzContent, pfCancel, pfStopProcessingUpdates);
    }


    virtual STDMETHODIMP OnDetectUpdateComplete(
        __in HRESULT /*hrStatus*/,
        __inout BOOL* pfIgnoreError
    )
    {
        // A failed update is very sad indeed, but shouldn't be fatal.
        *pfIgnoreError = TRUE;

        return S_OK;
    }

    virtual STDMETHODIMP OnDetectComplete(
        __in HRESULT hrStatus,
        __in BOOL /*fEligibleForCleanup*/
        )
    {
        HRESULT hr = S_OK;

        if (m_fSuppressDowngradeFailure && m_fDowngrading)
        {
            SetState(WIXSTDBA_STATE_APPLIED, hrStatus);

            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Bundle downgrade was attempted but downgrade failure has been suppressed.");

            ExitFunction();
        }

        // If we're not interacting with the user or we're doing a layout or we're resuming just after a force restart
        // then automatically start planning.
        BOOL fSkipToPlan = SUCCEEDED(hrStatus) &&
                           (BOOTSTRAPPER_DISPLAY_FULL > m_commandDisplay ||
                            BOOTSTRAPPER_ACTION_LAYOUT == m_commandAction ||
                            BOOTSTRAPPER_RESUME_TYPE_REBOOT == m_commandResumeType);

        // If we're requiring user input (which currently means Install, Repair, or Uninstall)
        // or if we're skipping to an action that modifies machine state
        // then evaluate conditions.
        BOOL fEvaluateConditions = SUCCEEDED(hrStatus) &&
            (!fSkipToPlan || BOOTSTRAPPER_ACTION_LAYOUT < m_commandAction && BOOTSTRAPPER_ACTION_UPDATE_REPLACE > m_commandAction);

        if (fEvaluateConditions)
        {
            hrStatus = EvaluateConditions();
        }

        SetState(WIXSTDBA_STATE_DETECTED, hrStatus);

        if (SUCCEEDED(hrStatus))
        {
            if (m_fPreplanPrereqs)
            {
                ::PostMessageW(m_hWnd, WM_WIXSTDBA_PLAN_PREREQS, 0, BOOTSTRAPPER_ACTION_INSTALL);
            }
            else if (fSkipToPlan)
            {
                ::PostMessageW(m_hWnd, WM_WIXSTDBA_PLAN_PACKAGES, 0, m_commandAction);
            }
        }

    LExit:
        return hr;
    }


    virtual STDMETHODIMP OnPlanBegin(
        __in DWORD cPackages,
        __in BOOL* pfCancel
        )
    {
        m_fPrereqPackagePlanned = FALSE;

        return __super::OnPlanBegin(cPackages, pfCancel);
    }


    virtual STDMETHODIMP OnPlanRelatedBundleType(
        __in_z LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE recommendedType,
        __inout BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE* pRequestedType,
        __inout BOOL* pfCancel
        )
    {
        // If we're only installing prerequisites, do not touch related bundles.
        if (m_fPrereq)
        {
            *pRequestedType = BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_NONE;
        }

        return CBootstrapperApplicationBase::OnPlanRelatedBundleType(wzBundleId, recommendedType, pRequestedType, pfCancel);
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

        // If we're planning to install prerequisites, install them. The prerequisites need to be installed
        // in all cases (even uninstall!) so the BA can load next.
        if (m_fPrereq)
        {
            // Only install prerequisite packages, and check the InstallCondition on them.
            BOOL fInstall = FALSE;

            hr = BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage);
            if (SUCCEEDED(hr) && pPackage->fPrereqPackage)
            {
                fInstall = BOOTSTRAPPER_PACKAGE_CONDITION_FALSE != installCondition;
            }

            if (fInstall)
            {
                *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
            }
            else
            {
                *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
            }

            // Don't force cache packages while installing prerequisites.
            if (BOOTSTRAPPER_CACHE_TYPE_FORCE == *pRequestedCacheType)
            {
                *pRequestedCacheType = BOOTSTRAPPER_CACHE_TYPE_KEEP;
            }
        }
        else if (m_sczAfterForcedRestartPackage) // after force restart, skip packages until after the package that caused the restart.
        {
            // After restart we need to finish the dependency registration for our package so allow the package
            // to go present.
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzPackageId, -1, m_sczAfterForcedRestartPackage, -1))
            {
                // Do not allow a repair because that could put us in a perpetual restart loop.
                if (BOOTSTRAPPER_REQUEST_STATE_REPAIR == *pRequestState)
                {
                    *pRequestState = BOOTSTRAPPER_REQUEST_STATE_PRESENT;
                }

                ReleaseNullStr(m_sczAfterForcedRestartPackage); // no more skipping now.
            }
            else // not the matching package, so skip it.
            {
                BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Skipping package: %ls, after restart because it was applied before the restart.", wzPackageId);

                *pRequestState = BOOTSTRAPPER_REQUEST_STATE_NONE;
            }
        }

        return CBootstrapperApplicationBase::OnPlanPackageBegin(wzPackageId, state, fCached, installCondition, repairCondition, recommendedState, recommendedCacheType, pRequestState, pRequestedCacheType, pfCancel);
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
        HRESULT hr = S_OK;
        BAL_INFO_PACKAGE* pPackage = NULL;
        BOOL fShowInternalUI = FALSE;
        INSTALLUILEVEL uiLevel = INSTALLUILEVEL_NOCHANGE;

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
            hr = BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage);
            if (SUCCEEDED(hr) && pPackage->sczDisplayInternalUICondition)
            {
                hr = BalEvaluateCondition(pPackage->sczDisplayInternalUICondition, &fShowInternalUI);
                BalExitOnFailure(hr, "Failed to evaluate condition for package '%ls': %ls", wzPackageId, pPackage->sczDisplayInternalUICondition);

                if (fShowInternalUI)
                {
                    *pUiLevel = uiLevel;
                }
            }
        }

    LExit:
        return __super::OnPlanMsiPackage(wzPackageId, fExecute, action, recommendedFileVersioning, pfCancel, pActionMsiProperty, pUiLevel, pfDisableExternalUiHandler, pFileVersioning);
    }


    virtual STDMETHODIMP OnPlannedPackage(
        __in_z LPCWSTR wzPackageId,
        __in BOOTSTRAPPER_ACTION_STATE execute,
        __in BOOTSTRAPPER_ACTION_STATE rollback,
        __in BOOL fPlannedCache,
        __in BOOL fPlannedUncache
        )
    {
        if (m_fPrereq && BOOTSTRAPPER_ACTION_STATE_NONE != execute)
        {
            m_fPrereqPackagePlanned = TRUE;
        }

        return __super::OnPlannedPackage(wzPackageId, execute, rollback, fPlannedCache, fPlannedUncache);
    }


    virtual STDMETHODIMP OnPlanComplete(
        __in HRESULT hrStatus
        )
    {
        HRESULT hr = S_OK;
        BOOL fPreplannedPrereqs = WIXSTDBA_STATE_PLANNING_PREREQS == m_state;
        WIXSTDBA_STATE completedState = WIXSTDBA_STATE_PLANNED;
        BOOL fApply = TRUE;

        if (fPreplannedPrereqs)
        {
            if (SUCCEEDED(hrStatus) && !m_fPrereqPackagePlanned)
            {
                // Nothing to do, so close and let the parent BA take over.
                m_fPrereqSkipped = TRUE;
                SetState(WIXSTDBA_STATE_APPLIED, S_OK);
                ExitFunction();
            }
            else if (BOOTSTRAPPER_ACTION_HELP == m_commandAction)
            {
                // If prereq packages were planned then the managed BA probably can't be loaded, so show the help from this BA.

                // Need to force the state change since normally moving backwards is prevented.
                ::PostMessageW(m_hWnd, WM_WIXSTDBA_CHANGE_STATE, 0, WIXSTDBA_STATE_HELP);

                ::PostMessageW(m_hWnd, WM_WIXSTDBA_SHOW_HELP, 0, 0);

                ExitFunction();
            }

            completedState = WIXSTDBA_STATE_PLANNED_PREREQS;
        }

        SetState(completedState, hrStatus);

        if (FAILED(hrStatus))
        {
            ExitFunction();
        }

        if (fPreplannedPrereqs)
        {
            // If the UI should be visible, display it now and hide the splash screen
            if (BOOTSTRAPPER_DISPLAY_NONE < m_commandDisplay)
            {
                ::ShowWindow(m_pTheme->hwndParent, SW_SHOW);
            }

            m_pEngine->CloseSplashScreen();

            fApply = BOOTSTRAPPER_DISPLAY_FULL > m_commandDisplay ||
                     BOOTSTRAPPER_RESUME_TYPE_REBOOT == m_commandResumeType;
        }

        if (fApply)
        {
            ::PostMessageW(m_hWnd, WM_WIXSTDBA_APPLY_PACKAGES, 0, 0);
        }

    LExit:
        return hr;
    }


    virtual STDMETHODIMP OnApplyBegin(
        __in DWORD dwPhaseCount,
        __in BOOL* pfCancel
        )
    {
        m_fStartedExecution = FALSE;
        m_dwCalculatedCacheProgress = 0;
        m_dwCalculatedExecuteProgress = 0;
        m_nLastMsiFilesInUseResult = IDNOACTION;
        m_nLastNetfxFilesInUseResult = IDNOACTION;

        return __super::OnApplyBegin(dwPhaseCount, pfCancel);
    }


    virtual STDMETHODIMP OnPauseAutomaticUpdatesBegin(
        )
    {
        HRESULT hr = S_OK;
        LOC_STRING* pLocString = NULL;
        LPWSTR sczFormattedString = NULL;
        LPCWSTR wz = NULL;

        hr = __super::OnPauseAutomaticUpdatesBegin();

        LocGetString(m_pWixLoc, L"#(loc.PauseAutomaticUpdatesMessage)", &pLocString);

        if (pLocString)
        {
            BalFormatString(pLocString->wzText, &sczFormattedString);
        }

        wz = sczFormattedString ? sczFormattedString : L"Pausing Windows automatic updates";

        ThemeSetTextControl(m_pControlOverallProgressPackageText, wz);

        ReleaseStr(sczFormattedString);
        return hr;
    }


    virtual STDMETHODIMP OnSystemRestorePointBegin(
        )
    {
        HRESULT hr = S_OK;
        LOC_STRING* pLocString = NULL;
        LPWSTR sczFormattedString = NULL;
        LPCWSTR wz = NULL;

        hr = __super::OnSystemRestorePointBegin();

        LocGetString(m_pWixLoc, L"#(loc.SystemRestorePointMessage)", &pLocString);

        if (pLocString)
        {
            BalFormatString(pLocString->wzText, &sczFormattedString);
        }

        wz = sczFormattedString ? sczFormattedString : L"Creating system restore point";

        ThemeSetTextControl(m_pControlOverallProgressPackageText, wz);

        ReleaseStr(sczFormattedString);
        return hr;
    }


    virtual STDMETHODIMP OnCachePackageBegin(
        __in_z LPCWSTR wzPackageId,
        __in DWORD cCachePayloads,
        __in DWORD64 dw64PackageCacheSize,
        __in BOOL fVital,
        __inout BOOL* pfCancel
        )
    {
        if (wzPackageId && *wzPackageId)
        {
            BAL_INFO_PACKAGE* pPackage = NULL;
            HRESULT hr = BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage);
            LPCWSTR wz = (SUCCEEDED(hr) && pPackage->sczDisplayName) ? pPackage->sczDisplayName : wzPackageId;

            ThemeSetTextControl(m_pControlCacheProgressPackageText, wz);

            // If something started executing, leave it in the overall progress text.
            if (!m_fStartedExecution)
            {
                ThemeSetTextControl(m_pControlOverallProgressPackageText, wz);
            }
        }

        return __super::OnCachePackageBegin(wzPackageId, cCachePayloads, dw64PackageCacheSize, fVital, pfCancel);
    }


    virtual STDMETHODIMP OnCacheAcquireProgress(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage,
        __inout BOOL* pfCancel
        )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnCacheAcquireProgress() - container/package: %ls, payload: %ls, progress: %I64u, total: %I64u, overall progress: %u%%", wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage);
#endif

        UpdateCacheProgress(dwOverallPercentage);

        return __super::OnCacheAcquireProgress(wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage, pfCancel);
    }


    virtual STDMETHODIMP OnCacheContainerOrPayloadVerifyProgress(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage,
        __inout BOOL* pfCancel
        )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnCacheContainerOrPayloadVerifyProgress() - container/package: %ls, payload: %ls, progress: %I64u, total: %I64u, overall progress: %u%%", wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage);
#endif

        UpdateCacheProgress(dwOverallPercentage);

        return __super::OnCacheContainerOrPayloadVerifyProgress(wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage, pfCancel);
    }


    virtual STDMETHODIMP OnCachePayloadExtractProgress(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage,
        __inout BOOL* pfCancel
        )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnCachePayloadExtractProgress() - container/package: %ls, payload: %ls, progress: %I64u, total: %I64u, overall progress: %u%%", wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage);
#endif

        UpdateCacheProgress(dwOverallPercentage);

        return __super::OnCachePayloadExtractProgress(wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage, pfCancel);
    }


    virtual STDMETHODIMP OnCacheVerifyProgress(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in DWORD64 dw64Progress,
        __in DWORD64 dw64Total,
        __in DWORD dwOverallPercentage,
        __in BOOTSTRAPPER_CACHE_VERIFY_STEP verifyStep,
        __inout BOOL* pfCancel
        )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnCacheVerifyProgress() - container/package: %ls, payload: %ls, progress: %I64u, total: %I64u, overall progress: %u%%, step: %u", wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage, verifyStep);
#endif

        UpdateCacheProgress(dwOverallPercentage);

        return __super::OnCacheVerifyProgress(wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage, verifyStep, pfCancel);
    }


    virtual STDMETHODIMP OnCacheAcquireComplete(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION* pAction
        )
    {
        SetProgressState(hrStatus);
        return __super::OnCacheAcquireComplete(wzPackageOrContainerId, wzPayloadId, hrStatus, recommendation, pAction);
    }


    virtual STDMETHODIMP OnCacheContainerOrPayloadVerifyComplete(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus
        )
    {
        SetProgressState(hrStatus);
        return __super::OnCacheContainerOrPayloadVerifyComplete(wzPackageOrContainerId, wzPayloadId, hrStatus);
    }


    virtual STDMETHODIMP OnCachePayloadExtractComplete(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus
        )
    {
        SetProgressState(hrStatus);
        return __super::OnCachePayloadExtractComplete(wzPackageOrContainerId, wzPayloadId, hrStatus);
    }


    virtual STDMETHODIMP OnCacheVerifyComplete(
        __in_z LPCWSTR wzPackageId,
        __in_z LPCWSTR wzPayloadId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION* pAction
        )
    {
        SetProgressState(hrStatus);
        return __super::OnCacheVerifyComplete(wzPackageId, wzPayloadId, hrStatus, recommendation, pAction);
    }


    virtual STDMETHODIMP OnCacheComplete(
        __in HRESULT hrStatus
        )
    {
        UpdateCacheProgress(SUCCEEDED(hrStatus) ? 100 : 0);
        ThemeSetTextControl(m_pControlCacheProgressPackageText, L"");
        SetState(WIXSTDBA_STATE_CACHED, S_OK); // we always return success here and let OnApplyComplete() deal with the error.
        return __super::OnCacheComplete(hrStatus);
    }


    virtual STDMETHODIMP OnError(
        __in BOOTSTRAPPER_ERROR_TYPE errorType,
        __in LPCWSTR wzPackageId,
        __in DWORD dwCode,
        __in_z LPCWSTR wzError,
        __in DWORD dwUIHint,
        __in DWORD cData,
        __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
        __in int nRecommendation,
        __inout int* pResult
        )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnError() - package: %ls, code: %d, ui hint: %d, message: %ls", wzPackageId, dwCode, dwUIHint, wzError);
#endif

        HRESULT hr = S_OK;
        int nResult = *pResult;
        LPWSTR sczError = NULL;

        if (BOOTSTRAPPER_DISPLAY_EMBEDDED == m_commandDisplay)
        {
            hr = m_pEngine->SendEmbeddedError(dwCode, wzError, dwUIHint, &nResult);
            if (FAILED(hr))
            {
                nResult = IDERROR;
            }
        }
        else if (BOOTSTRAPPER_DISPLAY_FULL == m_commandDisplay)
        {
            // If this is an authentication failure, let the engine try to handle it for us.
            if (BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_SERVER == errorType || BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_PROXY == errorType)
            {
                nResult = IDTRYAGAIN;
            }
            else // show a generic error message box.
            {
                BalRetryErrorOccurred(wzPackageId, dwCode);

                if (!m_fShowingInternalUiThisPackage)
                {
                    // If no error message was provided, use the error code to try and get an error message.
                    if (!wzError || !*wzError || BOOTSTRAPPER_ERROR_TYPE_WINDOWS_INSTALLER != errorType)
                    {
                        hr = StrAllocFromError(&sczError, dwCode, NULL);
                        if (FAILED(hr) || !sczError || !*sczError)
                        {
                            StrAllocFormatted(&sczError, L"0x%x", dwCode);
                        }

                        hr = S_OK;
                    }

                    nResult = ::MessageBoxW(m_hWnd, sczError ? sczError : wzError, m_pTheme->sczCaption, dwUIHint);
                }
            }

            SetProgressState(HRESULT_FROM_WIN32(dwCode));
        }
        else // just take note of the error code and let things continue.
        {
            BalRetryErrorOccurred(wzPackageId, dwCode);
        }

        ReleaseStr(sczError);

        *pResult = nResult;

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnError() - package: %ls, hr: 0x%0x, result: %d", wzPackageId, hr, nResult);
#endif

        return FAILED(hr) ? hr : __super::OnError(errorType, wzPackageId, dwCode, wzError, dwUIHint, cData, rgwzData, nRecommendation, pResult);
    }


    virtual STDMETHODIMP OnExecuteMsiMessage(
        __in_z LPCWSTR wzPackageId,
        __in INSTALLMESSAGE messageType,
        __in DWORD dwUIHint,
        __in_z LPCWSTR wzMessage,
        __in DWORD cData,
        __in_ecount_z_opt(cData) LPCWSTR* rgwzData,
        __in int nRecommendation,
        __inout int* pResult
        )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnExecuteMsiMessage() - package: %ls, message: %ls", wzPackageId, wzMessage);
#endif

        if (BOOTSTRAPPER_DISPLAY_FULL == m_commandDisplay && (INSTALLMESSAGE_WARNING == messageType || INSTALLMESSAGE_USER == messageType))
        {
            if (!m_fShowingInternalUiThisPackage)
            {
                int nResult = ::MessageBoxW(m_hWnd, wzMessage, m_pTheme->sczCaption, dwUIHint);

                *pResult = nResult;

                return __super::OnExecuteMsiMessage(wzPackageId, messageType, dwUIHint, wzMessage, cData, rgwzData, nRecommendation, pResult);
            }
        }

        if (INSTALLMESSAGE_ACTIONSTART == messageType)
        {
            ThemeSetTextControl(m_pControlExecuteProgressActionDataText, wzMessage);
        }

        return __super::OnExecuteMsiMessage(wzPackageId, messageType, dwUIHint, wzMessage, cData, rgwzData, nRecommendation, pResult);
    }


    virtual STDMETHODIMP OnProgress(
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallProgressPercentage,
        __inout BOOL* pfCancel
        )
    {
        WCHAR wzProgress[5] = { };

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnProgress() - progress: %u%%, overall progress: %u%%", dwProgressPercentage, dwOverallProgressPercentage);
#endif

        ::StringCchPrintfW(wzProgress, countof(wzProgress), L"%u%%", dwOverallProgressPercentage);
        ThemeSetTextControl(m_pControlOverallProgressText, wzProgress);

        ThemeSetProgressControl(m_pControlOverallProgressbar, dwOverallProgressPercentage);
        SetTaskbarButtonProgress(dwOverallProgressPercentage);

        return __super::OnProgress(dwProgressPercentage, dwOverallProgressPercentage, pfCancel);
    }


    virtual STDMETHODIMP OnExecutePackageBegin(
        __in_z LPCWSTR wzPackageId,
        __in BOOL fExecute,
        __in BOOTSTRAPPER_ACTION_STATE action,
        __in INSTALLUILEVEL uiLevel,
        __in BOOL fDisableExternalUiHandler,
        __inout BOOL* pfCancel
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczFormattedString = NULL;
        BOOL fShowingInternalUiThisPackage = FALSE;

        m_fStartedExecution = TRUE;

        if (wzPackageId && *wzPackageId)
        {
            BAL_INFO_PACKAGE* pPackage = NULL;
            BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage);

            LPCWSTR wz = wzPackageId;
            if (pPackage)
            {
                LOC_STRING* pLocString = NULL;

                switch (pPackage->type)
                {
                case BAL_INFO_PACKAGE_TYPE_BUNDLE_ADDON:
                    LocGetString(m_pWixLoc, L"#(loc.ExecuteAddonRelatedBundleMessage)", &pLocString);
                    break;

                case BAL_INFO_PACKAGE_TYPE_BUNDLE_PATCH:
                    LocGetString(m_pWixLoc, L"#(loc.ExecutePatchRelatedBundleMessage)", &pLocString);
                    break;

                case BAL_INFO_PACKAGE_TYPE_BUNDLE_UPGRADE:
                    LocGetString(m_pWixLoc, L"#(loc.ExecuteUpgradeRelatedBundleMessage)", &pLocString);
                    break;
                }

                if (pLocString)
                {
                    // If the wix developer is showing a hidden variable in the UI, then obviously they don't care about keeping it safe
                    // so don't go down the rabbit hole of making sure that this is securely freed.
                    BalFormatString(pLocString->wzText, &sczFormattedString);
                }

                wz = sczFormattedString ? sczFormattedString : pPackage->sczDisplayName ? pPackage->sczDisplayName : wzPackageId;
            }

            fShowingInternalUiThisPackage = INSTALLUILEVEL_NONE != (INSTALLUILEVEL_NONE & uiLevel);

            ThemeSetTextControl(m_pControlExecuteProgressPackageText, wz);
            ThemeSetTextControl(m_pControlOverallProgressPackageText, wz);
        }

        ::EnterCriticalSection(&m_csShowingInternalUiThisPackage);
        m_fShowingInternalUiThisPackage = fShowingInternalUiThisPackage;
        hr = __super::OnExecutePackageBegin(wzPackageId, fExecute, action, uiLevel, fDisableExternalUiHandler, pfCancel);
        ::LeaveCriticalSection(&m_csShowingInternalUiThisPackage);

        ReleaseStr(sczFormattedString);
        return hr;
    }


    virtual STDMETHODIMP OnExecuteProgress(
        __in_z LPCWSTR wzPackageId,
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallProgressPercentage,
        __inout BOOL* pfCancel
        )
    {
        WCHAR wzProgress[5] = { };

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: OnExecuteProgress() - package: %ls, progress: %u%%, overall progress: %u%%", wzPackageId, dwProgressPercentage, dwOverallProgressPercentage);
#endif

        ::StringCchPrintfW(wzProgress, countof(wzProgress), L"%u%%", dwOverallProgressPercentage);
        ThemeSetTextControl(m_pControlExecuteProgressText, wzProgress);

        ThemeSetProgressControl(m_pControlExecuteProgressbar, dwOverallProgressPercentage);

        m_dwCalculatedExecuteProgress = dwOverallProgressPercentage * (100 - WIXSTDBA_ACQUIRE_PERCENTAGE) / 100;
        ThemeSetProgressControl(m_pControlOverallCalculatedProgressbar, m_dwCalculatedCacheProgress + m_dwCalculatedExecuteProgress);

        SetTaskbarButtonProgress(m_dwCalculatedCacheProgress + m_dwCalculatedExecuteProgress);

        return __super::OnExecuteProgress(wzPackageId, dwProgressPercentage, dwOverallProgressPercentage, pfCancel);
    }


    virtual STDMETHODIMP OnExecuteFilesInUse(
        __in_z LPCWSTR wzPackageId,
        __in DWORD cFiles,
        __in_ecount_z(cFiles) LPCWSTR* rgwzFiles,
        __in int nRecommendation,
        __in BOOTSTRAPPER_FILES_IN_USE_TYPE source,
        __inout int* pResult
        )
    {
        HRESULT hr = S_OK;
        BAL_INFO_PACKAGE* pPackage = NULL;
        BOOL fShowFilesInUseDialog = TRUE;

        if (!m_fShowingInternalUiThisPackage && wzPackageId && *wzPackageId)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_VERBOSE, "Package %ls has %d applications holding files in use.", wzPackageId, cFiles);

            hr = BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage);
            if (SUCCEEDED(hr) && pPackage->sczDisplayFilesInUseDialogCondition)
            {
                hr = BalEvaluateCondition(pPackage->sczDisplayFilesInUseDialogCondition, &fShowFilesInUseDialog);
                BalExitOnFailure(hr, "Failed to evaluate condition for package '%ls': %ls", wzPackageId, pPackage->sczDisplayFilesInUseDialogCondition);
            }

            if (fShowFilesInUseDialog)
            {
                switch (source)
                {
                case BOOTSTRAPPER_FILES_IN_USE_TYPE_MSI:
                    if (m_fShowStandardFilesInUse)
                    {
                        return ShowMsiFilesInUse(cFiles, rgwzFiles, source, pResult);
                    }
                    break;
                case BOOTSTRAPPER_FILES_IN_USE_TYPE_MSI_RM:
                    if (m_fShowRMFilesInUse)
                    {
                        return ShowMsiFilesInUse(cFiles, rgwzFiles, source, pResult);
                    }
                    break;
                case BOOTSTRAPPER_FILES_IN_USE_TYPE_NETFX:
                    if (m_fShowNetfxFilesInUse)
                    {
                        return ShowNetfxFilesInUse(cFiles, rgwzFiles, pResult);
                    }
                    break;
                }
            }
            else
            {
                *pResult = IDIGNORE;
            }
        }

    LExit:
        return __super::OnExecuteFilesInUse(wzPackageId, cFiles, rgwzFiles, nRecommendation, source, pResult);
    }


    virtual STDMETHODIMP OnExecutePackageComplete(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_APPLY_RESTART restart,
        __in BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION* pAction
        )
    {
        HRESULT hr = S_OK;
        SetProgressState(hrStatus);

        hr = __super::OnExecutePackageComplete(wzPackageId, hrStatus, restart, recommendation, pAction);

        if (m_fPrereq && BOOTSTRAPPER_APPLY_RESTART_NONE != restart)
        {
            BAL_INFO_PACKAGE* pPackage = NULL;
            HRESULT hrPrereq = BalInfoFindPackageById(&m_Bundle.packages, wzPackageId, &pPackage);

            // If the prerequisite required a restart (any restart) then do an immediate
            // restart to ensure that the bundle will get launched again post reboot.
            if (SUCCEEDED(hrPrereq) && pPackage->fPrereqPackage)
            {
                *pAction = BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION_RESTART;
            }
        }

        return hr;
    }


    virtual STDMETHODIMP OnExecuteComplete(
        __in HRESULT hrStatus
        )
    {
        HRESULT hr = S_OK;

        ThemeSetTextControl(m_pControlExecuteProgressPackageText, L"");
        ThemeSetTextControl(m_pControlExecuteProgressActionDataText, L"");
        ThemeSetTextControl(m_pControlOverallProgressPackageText, L"");
        ThemeControlEnable(m_pControlProgressCancelButton, FALSE); // no more cancel.
        m_fShowingInternalUiThisPackage = FALSE;

        SetState(WIXSTDBA_STATE_EXECUTED, S_OK); // we always return success here and let OnApplyComplete() deal with the error.
        SetProgressState(hrStatus);

        return hr;
    }


    virtual STDMETHODIMP OnCacheAcquireResolving(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR* rgSearchPaths,
        __in DWORD /*cSearchPaths*/,
        __in BOOL /*fFoundLocal*/,
        __in DWORD dwRecommendedSearchPath,
        __in_z_opt LPCWSTR /*wzDownloadUrl*/,
        __in_z_opt LPCWSTR /*wzPayloadContainerId*/,
        __in BOOTSTRAPPER_CACHE_RESOLVE_OPERATION /*recommendation*/,
        __inout DWORD* /*pdwChosenSearchPath*/,
        __inout BOOTSTRAPPER_CACHE_RESOLVE_OPERATION* pAction,
        __inout BOOL* pfCancel
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczPath = NULL;

        if (BOOTSTRAPPER_CACHE_RESOLVE_NONE == *pAction && BOOTSTRAPPER_DISPLAY_FULL == m_commandDisplay) // prompt to change the source location.
        {
            static COMDLG_FILTERSPEC vrgFilters[] =
            {
                { L"All Files", L"*.*" },
            };

            hr = WnduShowOpenFileDialog(m_hWnd, TRUE, TRUE, m_pTheme->sczCaption, vrgFilters, countof(vrgFilters), 1, rgSearchPaths[dwRecommendedSearchPath], &sczPath);
            if (SUCCEEDED(hr))
            {
                hr = m_pEngine->SetLocalSource(wzPackageOrContainerId, wzPayloadId, sczPath);
                *pAction = BOOTSTRAPPER_CACHE_RESOLVE_RETRY;
            }
            else
            {
                *pfCancel = TRUE;
            }
        }
        // else there's nothing more we can do in non-interactive mode

        *pfCancel |= CheckCanceled();

        ReleaseStr(sczPath);

        return hr;
    }

    virtual STDMETHODIMP OnApplyComplete(
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_APPLY_RESTART restart,
        __in BOOTSTRAPPER_APPLYCOMPLETE_ACTION recommendation,
        __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* pAction
        )
    {
        HRESULT hr = S_OK;

        __super::OnApplyComplete(hrStatus, restart, recommendation, pAction);

        m_restartResult = restart; // remember the restart result so we return the correct error code no matter what the user chooses to do in the UI.
        m_fRestartRequired = BOOTSTRAPPER_APPLY_RESTART_NONE != restart;
        BalSetStringVariable(WIXSTDBA_VARIABLE_RESTART_REQUIRED, m_fRestartRequired ? L"1" : NULL, FALSE);

        m_fShouldRestart = m_fRestartRequired && BAL_INFO_RESTART_NEVER < m_BalInfoCommand.restart;

        // Automatically restart if we're not displaying a UI or the command line said to always allow restarts.
        m_fAllowRestart = m_fShouldRestart && (BOOTSTRAPPER_DISPLAY_FULL > m_commandDisplay || BAL_INFO_RESTART_PROMPT < m_BalInfoCommand.restart);

        if (m_fPrereq)
        {
            m_fPrereqInstalled = SUCCEEDED(hrStatus);
        }

        // If we are showing UI, wait a beat before moving to the final screen.
        if (BOOTSTRAPPER_DISPLAY_NONE < m_commandDisplay)
        {
            ::Sleep(250);
        }

        SetState(WIXSTDBA_STATE_APPLIED, hrStatus);
        SetTaskbarButtonProgress(100); // show full progress bar, green, yellow, or red

        *pAction = BOOTSTRAPPER_APPLYCOMPLETE_ACTION_NONE;

        return hr;
    }

    virtual STDMETHODIMP OnLaunchApprovedExeComplete(
        __in HRESULT hrStatus,
        __in DWORD /*processId*/
        )
    {
        HRESULT hr = S_OK;

        if (HRESULT_FROM_WIN32(ERROR_ACCESS_DENIED) == hrStatus)
        {
            //try with ShelExec next time
            OnClickLaunchButton();
        }
        else
        {
            ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);
        }

        return hr;
    }


    virtual STDMETHODIMP OnElevateComplete(
        __in HRESULT hrStatus
        )
    {
        if (m_fElevatingForRestart)
        {
            m_fElevatingForRestart = FALSE;

            if (SUCCEEDED(hrStatus))
            {
                m_fAllowRestart = TRUE;

                ::SendMessageW(m_hWnd, WM_CLOSE, 0, 0);
            }
            // else if failed then OnError showed the user an error message box
        }

        return __super::OnElevateComplete(hrStatus);
    }


    virtual STDMETHODIMP_(void) BAProcFallback(
        __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __inout HRESULT* phr
    )
    {
        if (!m_pfnBAFunctionsProc || FAILED(*phr))
        {
            return;
        }

        // Always log before and after so we don't get blamed when BAFunctions changes something.
        switch (message)
        {
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCREATE:
            // Functions do not get ONCREATE, they get these parameters when created directly.
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDESTROY:
            OnDestroyFallback(reinterpret_cast<BA_ONDESTROY_ARGS*>(pvArgs), reinterpret_cast<BA_ONDESTROY_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP:
            OnStartupFallback(reinterpret_cast<BA_ONSTARTUP_ARGS*>(pvArgs), reinterpret_cast<BA_ONSTARTUP_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN:
            OnShutdownFallback(reinterpret_cast<BA_ONSHUTDOWN_ARGS*>(pvArgs), reinterpret_cast<BA_ONSHUTDOWN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN:
            OnDetectBeginFallback(reinterpret_cast<BA_ONDETECTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE:
            OnDetectCompleteFallback(reinterpret_cast<BA_ONDETECTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN:
            OnPlanBeginFallback(reinterpret_cast<BA_ONPLANBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE:
            OnPlanCompleteFallback(reinterpret_cast<BA_ONPLANCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE:
            OnDetectForwardCompatibleBundleFallback(reinterpret_cast<BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATEBEGIN:
            OnDetectUpdateBeginFallback(reinterpret_cast<BA_ONDETECTUPDATEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATE:
            OnDetectUpdateFallback(reinterpret_cast<BA_ONDETECTUPDATE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATECOMPLETE:
            OnDetectUpdateCompleteFallback(reinterpret_cast<BA_ONDETECTUPDATECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLE:
            OnDetectRelatedBundleFallback(reinterpret_cast<BA_ONDETECTRELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGEBEGIN:
            OnDetectPackageBeginFallback(reinterpret_cast<BA_ONDETECTPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDMSIPACKAGE:
            OnDetectRelatedMsiPackageFallback(reinterpret_cast<BA_ONDETECTRELATEDMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPATCHTARGET:
            OnDetectPatchTargetFallback(reinterpret_cast<BA_ONDETECTPATCHTARGET_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPATCHTARGET_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTMSIFEATURE:
            OnDetectMsiFeatureFallback(reinterpret_cast<BA_ONDETECTMSIFEATURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTMSIFEATURE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGECOMPLETE:
            OnDetectPackageCompleteFallback(reinterpret_cast<BA_ONDETECTPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE:
            OnPlanRelatedBundleFallback(reinterpret_cast<BA_ONPLANRELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANRELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN:
            OnPlanPackageBeginFallback(reinterpret_cast<BA_ONPLANPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPATCHTARGET:
            OnPlanPatchTargetFallback(reinterpret_cast<BA_ONPLANPATCHTARGET_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPATCHTARGET_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIFEATURE:
            OnPlanMsiFeatureFallback(reinterpret_cast<BA_ONPLANMSIFEATURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANMSIFEATURE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGECOMPLETE:
            OnPlanPackageCompleteFallback(reinterpret_cast<BA_ONPLANPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYBEGIN:
            OnApplyBeginFallback(reinterpret_cast<BA_ONAPPLYBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONAPPLYBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATEBEGIN:
            OnElevateBeginFallback(reinterpret_cast<BA_ONELEVATEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONELEVATEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATECOMPLETE:
            OnElevateCompleteFallback(reinterpret_cast<BA_ONELEVATECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONELEVATECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPROGRESS:
            OnProgressFallback(reinterpret_cast<BA_ONPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONERROR:
            OnErrorFallback(reinterpret_cast<BA_ONERROR_ARGS*>(pvArgs), reinterpret_cast<BA_ONERROR_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERBEGIN:
            OnRegisterBeginFallback(reinterpret_cast<BA_ONREGISTERBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONREGISTERBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERCOMPLETE:
            OnRegisterCompleteFallback(reinterpret_cast<BA_ONREGISTERCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONREGISTERCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEBEGIN:
            OnCacheBeginFallback(reinterpret_cast<BA_ONCACHEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGEBEGIN:
            OnCachePackageBeginFallback(reinterpret_cast<BA_ONCACHEPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREBEGIN:
            OnCacheAcquireBeginFallback(reinterpret_cast<BA_ONCACHEACQUIREBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIREBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREPROGRESS:
            OnCacheAcquireProgressFallback(reinterpret_cast<BA_ONCACHEACQUIREPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIREPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRERESOLVING:
            OnCacheAcquireResolvingFallback(reinterpret_cast<BA_ONCACHEACQUIRERESOLVING_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIRERESOLVING_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRECOMPLETE:
            OnCacheAcquireCompleteFallback(reinterpret_cast<BA_ONCACHEACQUIRECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIRECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYBEGIN:
            OnCacheVerifyBeginFallback(reinterpret_cast<BA_ONCACHEVERIFYBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEVERIFYBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYCOMPLETE:
            OnCacheVerifyCompleteFallback(reinterpret_cast<BA_ONCACHEVERIFYCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEVERIFYCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGECOMPLETE:
            OnCachePackageCompleteFallback(reinterpret_cast<BA_ONCACHEPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECOMPLETE:
            OnCacheCompleteFallback(reinterpret_cast<BA_ONCACHECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEBEGIN:
            OnExecuteBeginFallback(reinterpret_cast<BA_ONEXECUTEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGEBEGIN:
            OnExecutePackageBeginFallback(reinterpret_cast<BA_ONEXECUTEPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPATCHTARGET:
            OnExecutePatchTargetFallback(reinterpret_cast<BA_ONEXECUTEPATCHTARGET_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPATCHTARGET_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROGRESS:
            OnExecuteProgressFallback(reinterpret_cast<BA_ONEXECUTEPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEMSIMESSAGE:
            OnExecuteMsiMessageFallback(reinterpret_cast<BA_ONEXECUTEMSIMESSAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEMSIMESSAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEFILESINUSE:
            OnExecuteFilesInUseFallback(reinterpret_cast<BA_ONEXECUTEFILESINUSE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEFILESINUSE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGECOMPLETE:
            OnExecutePackageCompleteFallback(reinterpret_cast<BA_ONEXECUTEPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTECOMPLETE:
            OnExecuteCompleteFallback(reinterpret_cast<BA_ONEXECUTECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERBEGIN:
            OnUnregisterBeginFallback(reinterpret_cast<BA_ONUNREGISTERBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONUNREGISTERBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERCOMPLETE:
            OnUnregisterCompleteFallback(reinterpret_cast<BA_ONUNREGISTERCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONUNREGISTERCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYCOMPLETE:
            OnApplyCompleteFallback(reinterpret_cast<BA_ONAPPLYCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONAPPLYCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXEBEGIN:
            OnLaunchApprovedExeBeginFallback(reinterpret_cast<BA_ONLAUNCHAPPROVEDEXEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONLAUNCHAPPROVEDEXEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXECOMPLETE:
            OnLaunchApprovedExeCompleteFallback(reinterpret_cast<BA_ONLAUNCHAPPROVEDEXECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONLAUNCHAPPROVEDEXECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIPACKAGE:
            OnPlanMsiPackageFallback(reinterpret_cast<BA_ONPLANMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONBEGIN:
            OnBeginMsiTransactionBeginFallback(reinterpret_cast<BA_ONBEGINMSITRANSACTIONBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONBEGINMSITRANSACTIONBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONCOMPLETE:
            OnBeginMsiTransactionCompleteFallback(reinterpret_cast<BA_ONBEGINMSITRANSACTIONCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONBEGINMSITRANSACTIONCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONBEGIN:
            OnCommitMsiTransactionBeginFallback(reinterpret_cast<BA_ONCOMMITMSITRANSACTIONBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCOMMITMSITRANSACTIONBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONCOMPLETE:
            OnCommitMsiTransactionCompleteFallback(reinterpret_cast<BA_ONCOMMITMSITRANSACTIONCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCOMMITMSITRANSACTIONCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONBEGIN:
            OnRollbackMsiTransactionBeginFallback(reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONCOMPLETE:
            OnRollbackMsiTransactionCompleteFallback(reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESBEGIN:
            OnPauseAutomaticUpdatesBeginFallback(reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESCOMPLETE:
            OnPauseAutomaticUpdatesCompleteFallback(reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTBEGIN:
            OnSystemRestorePointBeginFallback(reinterpret_cast<BA_ONSYSTEMRESTOREPOINTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONSYSTEMRESTOREPOINTBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTCOMPLETE:
            OnSystemRestorePointCompleteFallback(reinterpret_cast<BA_ONSYSTEMRESTOREPOINTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONSYSTEMRESTOREPOINTCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDPACKAGE:
            OnPlannedPackageFallback(reinterpret_cast<BA_ONPLANNEDPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANNEDPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYPROGRESS:
            OnCacheVerifyProgressFallback(reinterpret_cast<BA_ONCACHEVERIFYPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEVERIFYPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYBEGIN:
            OnCacheContainerOrPayloadVerifyBeginFallback(reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE:
            OnCacheContainerOrPayloadVerifyCompleteFallback(reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS:
            OnCacheContainerOrPayloadVerifyProgressFallback(reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTBEGIN:
            OnCachePayloadExtractBeginFallback(reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTCOMPLETE:
            OnCachePayloadExtractCompleteFallback(reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTPROGRESS:
            OnCachePayloadExtractProgressFallback(reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANROLLBACKBOUNDARY:
            OnPlanRollbackBoundaryFallback(reinterpret_cast<BA_ONPLANROLLBACKBOUNDARY_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANROLLBACKBOUNDARY_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE:
            OnDetectCompatibleMsiPackageFallback(reinterpret_cast<BA_ONDETECTCOMPATIBLEMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTCOMPATIBLEMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN:
            OnPlanCompatibleMsiPackageBeginFallback(reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE:
            OnPlanCompatibleMsiPackageCompleteFallback(reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDCOMPATIBLEPACKAGE:
            OnPlannedCompatiblePackageFallback(reinterpret_cast<BA_ONPLANNEDCOMPATIBLEPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANNEDCOMPATIBLEPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRESTORERELATEDBUNDLE:
            OnPlanRestoreRelatedBundleFallback(reinterpret_cast<BA_ONPLANRESTORERELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANRESTORERELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLETYPE:
            OnPlanRelatedBundleTypeFallback(reinterpret_cast<BA_ONPLANRELATEDBUNDLETYPE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANRELATEDBUNDLETYPE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYDOWNGRADE:
            OnApplyDowngradeFallback(reinterpret_cast<BA_ONAPPLYDOWNGRADE_ARGS*>(pvArgs), reinterpret_cast<BA_ONAPPLYDOWNGRADE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLEPACKAGE:
            OnDetectRelatedBundlePackageFallback(reinterpret_cast<BA_ONDETECTRELATEDBUNDLEPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDBUNDLEPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGENONVITALVALIDATIONFAILURE:
            OnCachePackageNonVitalValidationFailureFallback(reinterpret_cast<BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_RESULTS*>(pvResults));
            break;
        default:
#ifdef DEBUG
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: Forwarding unknown BA message: %d", message);
#endif
            m_pfnBAFunctionsProc((BA_FUNCTIONS_MESSAGE)message, pvArgs, pvResults, m_pvBAFunctionsProcContext);
            break;
        }
    }


private: // privates
    void OnDestroyFallback(
        __in BA_ONDESTROY_ARGS* pArgs,
        __inout BA_ONDESTROY_RESULTS* pResults
    )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDESTROY, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnStartupFallback(
        __in BA_ONSTARTUP_ARGS* pArgs,
        __inout BA_ONSTARTUP_RESULTS* pResults
    )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONSTARTUP, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnShutdownFallback(
        __in BA_ONSHUTDOWN_ARGS* pArgs,
        __inout BA_ONSHUTDOWN_RESULTS* pResults
    )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONSHUTDOWN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectBeginFallback(
        __in BA_ONDETECTBEGIN_ARGS* pArgs,
        __inout BA_ONDETECTBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectCompleteFallback(
        __in BA_ONDETECTCOMPLETE_ARGS* pArgs,
        __inout BA_ONDETECTCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlanBeginFallback(
        __in BA_ONPLANBEGIN_ARGS* pArgs,
        __inout BA_ONPLANBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlanCompleteFallback(
        __in BA_ONPLANCOMPLETE_ARGS* pArgs,
        __inout BA_ONPLANCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectForwardCompatibleBundleFallback(
        __in BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS* pArgs,
        __inout BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectUpdateBeginFallback(
        __in BA_ONDETECTUPDATEBEGIN_ARGS* pArgs,
        __inout BA_ONDETECTUPDATEBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTUPDATEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectUpdateFallback(
        __in BA_ONDETECTUPDATE_ARGS* pArgs,
        __inout BA_ONDETECTUPDATE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTUPDATE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectUpdateCompleteFallback(
        __in BA_ONDETECTUPDATECOMPLETE_ARGS* pArgs,
        __inout BA_ONDETECTUPDATECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTUPDATECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectRelatedBundleFallback(
        __in BA_ONDETECTRELATEDBUNDLE_ARGS* pArgs,
        __inout BA_ONDETECTRELATEDBUNDLE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTRELATEDBUNDLE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectPackageBeginFallback(
        __in BA_ONDETECTPACKAGEBEGIN_ARGS* pArgs,
        __inout BA_ONDETECTPACKAGEBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTPACKAGEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectRelatedMsiPackageFallback(
        __in BA_ONDETECTRELATEDMSIPACKAGE_ARGS* pArgs,
        __inout BA_ONDETECTRELATEDMSIPACKAGE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTRELATEDMSIPACKAGE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectPatchTargetFallback(
        __in BA_ONDETECTPATCHTARGET_ARGS* pArgs,
        __inout BA_ONDETECTPATCHTARGET_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTPATCHTARGET, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectMsiFeatureFallback(
        __in BA_ONDETECTMSIFEATURE_ARGS* pArgs,
        __inout BA_ONDETECTMSIFEATURE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTMSIFEATURE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectPackageCompleteFallback(
        __in BA_ONDETECTPACKAGECOMPLETE_ARGS* pArgs,
        __inout BA_ONDETECTPACKAGECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTPACKAGECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlanRelatedBundleFallback(
        __in BA_ONPLANRELATEDBUNDLE_ARGS* pArgs,
        __inout BA_ONPLANRELATEDBUNDLE_RESULTS* pResults
        )
    {
        BOOTSTRAPPER_REQUEST_STATE requestedState = pResults->requestedState;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANRELATEDBUNDLE, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_RELATED_BUNDLE, m_hModule, pArgs->wzBundleId, LoggingRequestStateToString(requestedState), LoggingRequestStateToString(pResults->requestedState));
    }

    void OnPlanRelatedBundleTypeFallback(
        __in BA_ONPLANRELATEDBUNDLETYPE_ARGS* pArgs,
        __inout BA_ONPLANRELATEDBUNDLETYPE_RESULTS* pResults
        )
    {
        BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE requestedType = pResults->requestedType;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANRELATEDBUNDLETYPE, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_RELATED_BUNDLE_TYPE, m_hModule, pArgs->wzBundleId, LoggingPlanRelationTypeToString(requestedType), LoggingPlanRelationTypeToString(pResults->requestedType));
    }

    void OnPlanPackageBeginFallback(
        __in BA_ONPLANPACKAGEBEGIN_ARGS* pArgs,
        __inout BA_ONPLANPACKAGEBEGIN_RESULTS* pResults
        )
    {
        BOOTSTRAPPER_REQUEST_STATE requestedState = pResults->requestedState;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANPACKAGEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_PACKAGE, m_hModule, pArgs->wzPackageId, LoggingRequestStateToString(requestedState), LoggingRequestStateToString(pResults->requestedState));
    }

    void OnPlanPatchTargetFallback(
        __in BA_ONPLANPATCHTARGET_ARGS* pArgs,
        __inout BA_ONPLANPATCHTARGET_RESULTS* pResults
        )
    {
        BOOTSTRAPPER_REQUEST_STATE requestedState = pResults->requestedState;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANPATCHTARGET, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_TARGET_MSI_PACKAGE, m_hModule, pArgs->wzPackageId, pArgs->wzProductCode, LoggingRequestStateToString(requestedState), LoggingRequestStateToString(pResults->requestedState));
    }

    void OnPlanMsiFeatureFallback(
        __in BA_ONPLANMSIFEATURE_ARGS* pArgs,
        __inout BA_ONPLANMSIFEATURE_RESULTS* pResults
        )
    {
        BOOTSTRAPPER_FEATURE_STATE requestedState = pResults->requestedState;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANMSIFEATURE, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_MSI_FEATURE, m_hModule, pArgs->wzPackageId, pArgs->wzFeatureId, LoggingMsiFeatureStateToString(requestedState), LoggingMsiFeatureStateToString(pResults->requestedState));
    }

    void OnPlanPackageCompleteFallback(
        __in BA_ONPLANPACKAGECOMPLETE_ARGS* pArgs,
        __inout BA_ONPLANPACKAGECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANPACKAGECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlannedCompatiblePackageFallback(
        __in BA_ONPLANNEDCOMPATIBLEPACKAGE_ARGS* pArgs,
        __inout BA_ONPLANNEDCOMPATIBLEPACKAGE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANNEDCOMPATIBLEPACKAGE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlannedPackageFallback(
        __in BA_ONPLANNEDPACKAGE_ARGS* pArgs,
        __inout BA_ONPLANNEDPACKAGE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANNEDPACKAGE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnApplyBeginFallback(
        __in BA_ONAPPLYBEGIN_ARGS* pArgs,
        __inout BA_ONAPPLYBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONAPPLYBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnElevateBeginFallback(
        __in BA_ONELEVATEBEGIN_ARGS* pArgs,
        __inout BA_ONELEVATEBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONELEVATEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnElevateCompleteFallback(
        __in BA_ONELEVATECOMPLETE_ARGS* pArgs,
        __inout BA_ONELEVATECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONELEVATECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnProgressFallback(
        __in BA_ONPROGRESS_ARGS* pArgs,
        __inout BA_ONPROGRESS_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPROGRESS, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnErrorFallback(
        __in BA_ONERROR_ARGS* pArgs,
        __inout BA_ONERROR_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONERROR, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnRegisterBeginFallback(
        __in BA_ONREGISTERBEGIN_ARGS* pArgs,
        __inout BA_ONREGISTERBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONREGISTERBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnRegisterCompleteFallback(
        __in BA_ONREGISTERCOMPLETE_ARGS* pArgs,
        __inout BA_ONREGISTERCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONREGISTERCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheBeginFallback(
        __in BA_ONCACHEBEGIN_ARGS* pArgs,
        __inout BA_ONCACHEBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCachePackageBeginFallback(
        __in BA_ONCACHEPACKAGEBEGIN_ARGS* pArgs,
        __inout BA_ONCACHEPACKAGEBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEPACKAGEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheAcquireBeginFallback(
        __in BA_ONCACHEACQUIREBEGIN_ARGS* pArgs,
        __inout BA_ONCACHEACQUIREBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEACQUIREBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheAcquireProgressFallback(
        __in BA_ONCACHEACQUIREPROGRESS_ARGS* pArgs,
        __inout BA_ONCACHEACQUIREPROGRESS_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEACQUIREPROGRESS, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheAcquireResolvingFallback(
        __in BA_ONCACHEACQUIRERESOLVING_ARGS* pArgs,
        __inout BA_ONCACHEACQUIRERESOLVING_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEACQUIRERESOLVING, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheAcquireCompleteFallback(
        __in BA_ONCACHEACQUIRECOMPLETE_ARGS* pArgs,
        __inout BA_ONCACHEACQUIRECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEACQUIRECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheVerifyBeginFallback(
        __in BA_ONCACHEVERIFYBEGIN_ARGS* pArgs,
        __inout BA_ONCACHEVERIFYBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEVERIFYBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheVerifyCompleteFallback(
        __in BA_ONCACHEVERIFYCOMPLETE_ARGS* pArgs,
        __inout BA_ONCACHEVERIFYCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEVERIFYCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCachePackageCompleteFallback(
        __in BA_ONCACHEPACKAGECOMPLETE_ARGS* pArgs,
        __inout BA_ONCACHEPACKAGECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEPACKAGECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheCompleteFallback(
        __in BA_ONCACHECOMPLETE_ARGS* pArgs,
        __inout BA_ONCACHECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnExecuteBeginFallback(
        __in BA_ONEXECUTEBEGIN_ARGS* pArgs,
        __inout BA_ONEXECUTEBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONEXECUTEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnExecutePackageBeginFallback(
        __in BA_ONEXECUTEPACKAGEBEGIN_ARGS* pArgs,
        __inout BA_ONEXECUTEPACKAGEBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONEXECUTEPACKAGEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnExecutePatchTargetFallback(
        __in BA_ONEXECUTEPATCHTARGET_ARGS* pArgs,
        __inout BA_ONEXECUTEPATCHTARGET_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONEXECUTEPATCHTARGET, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnExecuteProgressFallback(
        __in BA_ONEXECUTEPROGRESS_ARGS* pArgs,
        __inout BA_ONEXECUTEPROGRESS_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONEXECUTEPROGRESS, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnExecuteMsiMessageFallback(
        __in BA_ONEXECUTEMSIMESSAGE_ARGS* pArgs,
        __inout BA_ONEXECUTEMSIMESSAGE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONEXECUTEMSIMESSAGE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnExecuteFilesInUseFallback(
        __in BA_ONEXECUTEFILESINUSE_ARGS* pArgs,
        __inout BA_ONEXECUTEFILESINUSE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONEXECUTEFILESINUSE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnExecutePackageCompleteFallback(
        __in BA_ONEXECUTEPACKAGECOMPLETE_ARGS* pArgs,
        __inout BA_ONEXECUTEPACKAGECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONEXECUTEPACKAGECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnExecuteCompleteFallback(
        __in BA_ONEXECUTECOMPLETE_ARGS* pArgs,
        __inout BA_ONEXECUTECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONEXECUTECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnUnregisterBeginFallback(
        __in BA_ONUNREGISTERBEGIN_ARGS* pArgs,
        __inout BA_ONUNREGISTERBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONUNREGISTERBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnUnregisterCompleteFallback(
        __in BA_ONUNREGISTERCOMPLETE_ARGS* pArgs,
        __inout BA_ONUNREGISTERCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONUNREGISTERCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnApplyCompleteFallback(
        __in BA_ONAPPLYCOMPLETE_ARGS* pArgs,
        __inout BA_ONAPPLYCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONAPPLYCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnLaunchApprovedExeBeginFallback(
        __in BA_ONLAUNCHAPPROVEDEXEBEGIN_ARGS* pArgs,
        __inout BA_ONLAUNCHAPPROVEDEXEBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONLAUNCHAPPROVEDEXEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnLaunchApprovedExeCompleteFallback(
        __in BA_ONLAUNCHAPPROVEDEXECOMPLETE_ARGS* pArgs,
        __inout BA_ONLAUNCHAPPROVEDEXECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONLAUNCHAPPROVEDEXECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlanMsiPackageFallback(
        __in BA_ONPLANMSIPACKAGE_ARGS* pArgs,
        __inout BA_ONPLANMSIPACKAGE_RESULTS* pResults
        )
    {
        BURN_MSI_PROPERTY actionMsiProperty = pResults->actionMsiProperty;
        INSTALLUILEVEL uiLevel = pResults->uiLevel;
        BOOL fDisableExternalUiHandler = pResults->fDisableExternalUiHandler;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANMSIPACKAGE, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_MSI_PACKAGE, m_hModule, pArgs->wzPackageId, actionMsiProperty, uiLevel, fDisableExternalUiHandler ? "yes" : "no", pResults->actionMsiProperty, pResults->uiLevel, pResults->fDisableExternalUiHandler ? "yes" : "no");
    }

    void OnBeginMsiTransactionBeginFallback(
        __in BA_ONBEGINMSITRANSACTIONBEGIN_ARGS* pArgs,
        __inout BA_ONBEGINMSITRANSACTIONBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONBEGINMSITRANSACTIONBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnBeginMsiTransactionCompleteFallback(
        __in BA_ONBEGINMSITRANSACTIONCOMPLETE_ARGS* pArgs,
        __inout BA_ONBEGINMSITRANSACTIONCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONBEGINMSITRANSACTIONCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCommitMsiTransactionBeginFallback(
        __in BA_ONCOMMITMSITRANSACTIONBEGIN_ARGS* pArgs,
        __inout BA_ONCOMMITMSITRANSACTIONBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCOMMITMSITRANSACTIONBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCommitMsiTransactionCompleteFallback(
        __in BA_ONCOMMITMSITRANSACTIONCOMPLETE_ARGS* pArgs,
        __inout BA_ONCOMMITMSITRANSACTIONCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCOMMITMSITRANSACTIONCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnRollbackMsiTransactionBeginFallback(
        __in BA_ONROLLBACKMSITRANSACTIONBEGIN_ARGS* pArgs,
        __inout BA_ONROLLBACKMSITRANSACTIONBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONROLLBACKMSITRANSACTIONBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnRollbackMsiTransactionCompleteFallback(
        __in BA_ONROLLBACKMSITRANSACTIONCOMPLETE_ARGS* pArgs,
        __inout BA_ONROLLBACKMSITRANSACTIONCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONROLLBACKMSITRANSACTIONCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPauseAutomaticUpdatesBeginFallback(
        __in BA_ONPAUSEAUTOMATICUPDATESBEGIN_ARGS* pArgs,
        __inout BA_ONPAUSEAUTOMATICUPDATESBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPAUSEAUTOMATICUPDATESBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPauseAutomaticUpdatesCompleteFallback(
        __in BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_ARGS* pArgs,
        __inout BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPAUSEAUTOMATICUPDATESCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnSystemRestorePointBeginFallback(
        __in BA_ONSYSTEMRESTOREPOINTBEGIN_ARGS* pArgs,
        __inout BA_ONSYSTEMRESTOREPOINTBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONSYSTEMRESTOREPOINTBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnSystemRestorePointCompleteFallback(
        __in BA_ONSYSTEMRESTOREPOINTCOMPLETE_ARGS* pArgs,
        __inout BA_ONSYSTEMRESTOREPOINTCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONSYSTEMRESTOREPOINTCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlanForwardCompatibleBundleFallback(
        __in BA_ONPLANFORWARDCOMPATIBLEBUNDLE_ARGS* pArgs,
        __inout BA_ONPLANFORWARDCOMPATIBLEBUNDLE_RESULTS* pResults
        )
    {
        BOOL fIgnoreBundle = pResults->fIgnoreBundle;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANFORWARDCOMPATIBLEBUNDLE, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_FORWARD_COMPATIBLE_BUNDLE, m_hModule, pArgs->wzBundleId, fIgnoreBundle ? "ignore" : "enable", pResults->fIgnoreBundle ? "ignore" : "enable");
    }

    void OnCacheVerifyProgressFallback(
        __in BA_ONCACHEVERIFYPROGRESS_ARGS* pArgs,
        __inout BA_ONCACHEVERIFYPROGRESS_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEVERIFYPROGRESS, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheContainerOrPayloadVerifyBeginFallback(
        __in BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_ARGS* pArgs,
        __inout BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheContainerOrPayloadVerifyCompleteFallback(
        __in BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_ARGS* pArgs,
        __inout BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCacheContainerOrPayloadVerifyProgressFallback(
        __in BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_ARGS* pArgs,
        __inout BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCachePayloadExtractBeginFallback(
        __in BA_ONCACHEPAYLOADEXTRACTBEGIN_ARGS* pArgs,
        __inout BA_ONCACHEPAYLOADEXTRACTBEGIN_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEPAYLOADEXTRACTBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCachePayloadExtractCompleteFallback(
        __in BA_ONCACHEPAYLOADEXTRACTCOMPLETE_ARGS* pArgs,
        __inout BA_ONCACHEPAYLOADEXTRACTCOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEPAYLOADEXTRACTCOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCachePayloadExtractProgressFallback(
        __in BA_ONCACHEPAYLOADEXTRACTPROGRESS_ARGS* pArgs,
        __inout BA_ONCACHEPAYLOADEXTRACTPROGRESS_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEPAYLOADEXTRACTPROGRESS, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlanRollbackBoundaryFallback(
        __in BA_ONPLANROLLBACKBOUNDARY_ARGS* pArgs,
        __inout BA_ONPLANROLLBACKBOUNDARY_RESULTS* pResults
        )
    {
        BOOL fTransaction = pResults->fTransaction;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANROLLBACKBOUNDARY, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_ROLLBACK_BOUNDARY, m_hModule, pArgs->wzRollbackBoundaryId, LoggingBoolToString(fTransaction), LoggingBoolToString(pResults->fTransaction));
    }

    void OnDetectCompatibleMsiPackageFallback(
        __in BA_ONDETECTCOMPATIBLEMSIPACKAGE_ARGS* pArgs,
        __inout BA_ONDETECTCOMPATIBLEMSIPACKAGE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlanCompatibleMsiPackageBeginFallback(
        __in BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_ARGS* pArgs,
        __inout BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_RESULTS* pResults
        )
    {
        BOOL fRequestRemove = pResults->fRequestRemove;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_COMPATIBLE_MSI_PACKAGE, m_hModule, pArgs->wzPackageId, pArgs->wzCompatiblePackageId, LoggingBoolToString(fRequestRemove), LoggingBoolToString(pResults->fRequestRemove));
    }

    void OnPlanCompatibleMsiPackageCompleteFallback(
        __in BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_ARGS* pArgs,
        __inout BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnPlanRestoreRelatedBundleFallback(
        __in BA_ONPLANRESTORERELATEDBUNDLE_ARGS* pArgs,
        __inout BA_ONPLANRESTORERELATEDBUNDLE_RESULTS* pResults
        )
    {
        BOOTSTRAPPER_REQUEST_STATE requestedState = pResults->requestedState;
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONPLANRESTORERELATEDBUNDLE, pArgs, pResults, m_pvBAFunctionsProcContext);
        BalLogId(BOOTSTRAPPER_LOG_LEVEL_STANDARD, MSG_WIXSTDBA_PLANNED_RESTORE_RELATED_BUNDLE, m_hModule, pArgs->wzBundleId, LoggingRequestStateToString(requestedState), LoggingRequestStateToString(pResults->requestedState));
    }

    void OnApplyDowngradeFallback(
        __in BA_ONAPPLYDOWNGRADE_ARGS* pArgs,
        __inout BA_ONAPPLYDOWNGRADE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONAPPLYDOWNGRADE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnExecuteProcessCancelFallback(
        __in BA_ONEXECUTEPROCESSCANCEL_ARGS* pArgs,
        __inout BA_ONEXECUTEPROCESSCANCEL_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONEXECUTEPROCESSCANCEL, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnDetectRelatedBundlePackageFallback(
        __in BA_ONDETECTRELATEDBUNDLEPACKAGE_ARGS* pArgs,
        __inout BA_ONDETECTRELATEDBUNDLEPACKAGE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONDETECTRELATEDBUNDLEPACKAGE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }

    void OnCachePackageNonVitalValidationFailureFallback(
        __in BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_ARGS* pArgs,
        __inout BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_RESULTS* pResults
        )
    {
        m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONCACHEPACKAGENONVITALVALIDATIONFAILURE, pArgs, pResults, m_pvBAFunctionsProcContext);
    }


    HRESULT ShowMsiFilesInUse(
        __in DWORD cFiles,
        __in_ecount_z(cFiles) LPCWSTR* rgwzFiles,
        __in BOOTSTRAPPER_FILES_IN_USE_TYPE source,
        __inout int* pResult
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczFilesInUse = NULL;
        DWORD_PTR cchLen = 0;
        int nResult = IDERROR;

        // If the user has chosen to ignore on a previously displayed "files in use" page,
        // we will return the same result for other cases. No need to display the page again.
        if (IDIGNORE == m_nLastMsiFilesInUseResult)
        {
            nResult = m_nLastMsiFilesInUseResult;
        }
        else if (BOOTSTRAPPER_DISPLAY_FULL == m_commandDisplay) // Only show files in use when using full display mode.
        {
            // See https://docs.microsoft.com/en-us/windows/win32/msi/sending-messages-to-windows-installer-using-msiprocessmessage for details.
            for (DWORD i = 1; i < cFiles; i += 2)
            {
                hr = ::StringCchLengthW(rgwzFiles[i], STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchLen));
                BalExitOnFailure(hr, "Failed to calculate length of string.");

                if (cchLen)
                {
                    hr = StrAllocConcatFormatted(&sczFilesInUse, L"%ls\r\n", rgwzFiles[i]);
                    BalExitOnFailure(hr, "Failed to concat files in use.");
                }
            }

            // Show applications using the files.
            if (BOOTSTRAPPER_FILES_IN_USE_TYPE_MSI_RM == source)
            {
                hr = ShowRestartManagerMsiFilesInUseDialog(sczFilesInUse, &nResult);
                ExitOnFailure(hr, "Failed to show RM files-in-use dialog.");
            }
            else
            {
                hr = ShowStandardMsiFilesInUseDialog(sczFilesInUse, &nResult);
                ExitOnFailure(hr, "Failed to show files-in-use dialog.");
            }
        }
        else
        {
            // Silent UI level installations always shut down applications and services,
            // and on Windows Vista and later, use Restart Manager unless disabled.
            nResult = IDOK;
        }

    LExit:
        ReleaseStr(sczFilesInUse);

        // Remember the answer from the user.
        m_nLastMsiFilesInUseResult = FAILED(hr) ? IDERROR : nResult;
        *pResult = m_nLastMsiFilesInUseResult;

        return hr;
    }


    int ShowRestartManagerMsiFilesInUseDialog(
        __in_z_opt LPCWSTR sczFilesInUse,
        __out int* pnResult
        )
    {
        HRESULT hr = S_OK;
        TASKDIALOGCONFIG config = { };
        LPWSTR sczTitle = NULL;
        LPWSTR sczLabel = NULL;
        LPWSTR sczCloseRadioButton = NULL;
        LPWSTR sczDontCloseRadioButton = NULL;
        int nButton = 0;
        int nRadioButton = 0;

        hr = BalFormatString(m_pFilesInUseTitleLoc->wzText, &sczTitle);
        BalExitOnFailure(hr, "Failed to format FilesInUseTitle loc string.");

        hr = BalFormatString(m_pFilesInUseLabelLoc->wzText, &sczLabel);
        BalExitOnFailure(hr, "Failed to format FilesInUseLabel loc string.");

        hr = BalFormatString(m_pFilesInUseCloseRadioButtonLoc->wzText, &sczCloseRadioButton);
        BalExitOnFailure(hr, "Failed to format FilesInUseCloseRadioButton loc string.");

        hr = BalFormatString(m_pFilesInUseDontCloseRadioButtonLoc->wzText, &sczDontCloseRadioButton);
        BalExitOnFailure(hr, "Failed to format FilesInUseDontCloseRadioButton loc string.");

        const TASKDIALOG_BUTTON rgRadioButtons[] = {
            { IDOK, sczCloseRadioButton },
            { IDIGNORE, sczDontCloseRadioButton },
        };

        config.cbSize = sizeof(config);
        config.hwndParent = m_hWnd;
        config.hInstance = m_hModule;
        config.dwFlags = TDF_SIZE_TO_CONTENT | TDF_POSITION_RELATIVE_TO_WINDOW;
        config.dwCommonButtons = TDCBF_OK_BUTTON | TDCBF_CANCEL_BUTTON;
        config.pszWindowTitle = sczTitle;
        config.pszMainInstruction = sczLabel;
        config.pszContent = sczFilesInUse ? sczFilesInUse : L"";
        config.nDefaultButton = IDOK;
        config.pRadioButtons = rgRadioButtons;
        config.cRadioButtons = countof(rgRadioButtons);
        config.nDefaultRadioButton = IDOK;

        hr = ::TaskDialogIndirect(&config, &nButton, &nRadioButton, NULL);
        BalExitOnFailure(hr, "Failed to show RM files-in-use task dialog.");

        *pnResult = IDOK == nButton ? nRadioButton : nButton;

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: RMFilesInUse task dialog result: button - %d, radio button - %d, result - %d", nButton, nRadioButton, *pnResult);
#endif

    LExit:
        ReleaseStr(sczTitle);
        ReleaseStr(sczLabel);
        ReleaseStr(sczCloseRadioButton);
        ReleaseStr(sczDontCloseRadioButton);

        return hr;
    }


    int ShowStandardMsiFilesInUseDialog(
        __in_z_opt LPCWSTR sczFilesInUse,
        __out int* pnResult
        )
    {
        HRESULT hr = S_OK;
        TASKDIALOGCONFIG config = { };
        LPWSTR sczTitle = NULL;
        LPWSTR sczLabel = NULL;
        LPWSTR sczRetryButton = NULL;
        LPWSTR sczIgnoreButton = NULL;
        LPWSTR sczExitButton = NULL;

        hr = BalFormatString(m_pFilesInUseTitleLoc->wzText, &sczTitle);
        BalExitOnFailure(hr, "Failed to format FilesInUseTitle loc string.");

        hr = BalFormatString(m_pFilesInUseLabelLoc->wzText, &sczLabel);
        BalExitOnFailure(hr, "Failed to format FilesInUseLabel loc string.");

        hr = BalFormatString(m_pFilesInUseRetryButtonLoc->wzText, &sczRetryButton);
        BalExitOnFailure(hr, "Failed to format FilesInUseRetryButton loc string.");

        hr = BalFormatString(m_pFilesInUseIgnoreButtonLoc->wzText, &sczIgnoreButton);
        BalExitOnFailure(hr, "Failed to format FilesInUseIgnoreButton loc string.");

        hr = BalFormatString(m_pFilesInUseExitButtonLoc->wzText, &sczExitButton);
        BalExitOnFailure(hr, "Failed to format FilesInUseExitButton loc string.");

        const TASKDIALOG_BUTTON rgButtons[] = {
            { IDRETRY, sczRetryButton },
            { IDIGNORE, sczIgnoreButton },
            { IDCANCEL, sczExitButton },
        };

        config.cbSize = sizeof(config);
        config.hwndParent = m_hWnd;
        config.hInstance = m_hModule;
        config.dwFlags = TDF_SIZE_TO_CONTENT | TDF_POSITION_RELATIVE_TO_WINDOW;
        config.pszWindowTitle = sczTitle;
        config.pszMainInstruction = sczLabel;
        config.pszContent = sczFilesInUse ? sczFilesInUse : L"";
        config.pButtons = rgButtons;
        config.cButtons = countof(rgButtons);

        hr = ::TaskDialogIndirect(&config, pnResult, NULL, NULL);
        BalExitOnFailure(hr, "Failed to show files-in-use task dialog.");

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: FilesInUse task dialog result: %d", *pnResult);
#endif

    LExit:
        ReleaseStr(sczTitle);
        ReleaseStr(sczLabel);
        ReleaseStr(sczRetryButton);
        ReleaseStr(sczIgnoreButton);
        ReleaseStr(sczExitButton);

        return hr;
    }


    HRESULT ShowNetfxFilesInUse(
        __in DWORD cFiles,
        __in_ecount_z(cFiles) LPCWSTR* rgwzFiles,
        __inout int* pResult
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczFilesInUse = NULL;
        DWORD_PTR cchLen = 0;
        int nResult = IDERROR;

        // If the user has chosen to ignore on a previously displayed "files in use" page,
        // we will return the same result for other cases. No need to display the page again.
        if (IDNO == m_nLastNetfxFilesInUseResult)
        {
            nResult = m_nLastNetfxFilesInUseResult;
        }
        else if (BOOTSTRAPPER_DISPLAY_FULL != m_commandDisplay) // Only show files in use when using full display mode.
        {
            nResult = IDYES;
        }
        else
        {
            for (DWORD i = 0; i < cFiles; ++i)
            {
                hr = ::StringCchLengthW(rgwzFiles[i], STRSAFE_MAX_CCH, reinterpret_cast<UINT_PTR*>(&cchLen));
                BalExitOnFailure(hr, "Failed to calculate length of string.");

                if (cchLen)
                {
                    hr = StrAllocConcatFormatted(&sczFilesInUse, L"%ls\r\n", rgwzFiles[i]);
                    BalExitOnFailure(hr, "Failed to concat files in use.");
                }
            }

            // Show applications using the files.
            hr = ShowNetfxFilesInUseDialog(sczFilesInUse, &nResult);
            ExitOnFailure(hr, "Failed to show Netfx files-in-use dialog.");
        }

    LExit:
        ReleaseStr(sczFilesInUse);

        // Remember the answer from the user.
        m_nLastNetfxFilesInUseResult = FAILED(hr) ? IDERROR : nResult;
        *pResult = m_nLastNetfxFilesInUseResult;

        return hr;
    }


    int ShowNetfxFilesInUseDialog(
        __in_z_opt LPCWSTR sczFilesInUse,
        __out int* pnResult
        )
    {
        HRESULT hr = S_OK;
        TASKDIALOGCONFIG config = { };
        LPWSTR sczTitle = NULL;
        LPWSTR sczLabel = NULL;
        LPWSTR sczNetfxCloseRadioButton = NULL;
        LPWSTR sczDontCloseRadioButton = NULL;
        int nButton = 0;
        int nRadioButton = 0;

        hr = BalFormatString(m_pFilesInUseTitleLoc->wzText, &sczTitle);
        BalExitOnFailure(hr, "Failed to format FilesInUseTitle loc string.");

        hr = BalFormatString(m_pFilesInUseLabelLoc->wzText, &sczLabel);
        BalExitOnFailure(hr, "Failed to format FilesInUseLabel loc string.");

        hr = BalFormatString(m_pFilesInUseNetfxCloseRadioButtonLoc->wzText, &sczNetfxCloseRadioButton);
        BalExitOnFailure(hr, "Failed to format FilesInUseNetfxCloseRadioButton loc string.");

        hr = BalFormatString(m_pFilesInUseDontCloseRadioButtonLoc->wzText, &sczDontCloseRadioButton);
        BalExitOnFailure(hr, "Failed to format FilesInUseDontCloseRadioButton loc string.");

        const TASKDIALOG_BUTTON rgRadioButtons[] = {
            { IDYES, sczNetfxCloseRadioButton },
            { IDNO, sczDontCloseRadioButton },
        };

        config.cbSize = sizeof(config);
        config.hwndParent = m_hWnd;
        config.hInstance = m_hModule;
        config.dwFlags = TDF_SIZE_TO_CONTENT | TDF_POSITION_RELATIVE_TO_WINDOW;
        config.dwCommonButtons = TDCBF_RETRY_BUTTON | TDCBF_OK_BUTTON | TDCBF_CANCEL_BUTTON;
        config.pszWindowTitle = sczTitle;
        config.pszMainInstruction = sczLabel;
        config.pszContent = sczFilesInUse ? sczFilesInUse : L"";
        config.nDefaultButton = IDRETRY;
        config.pRadioButtons = rgRadioButtons;
        config.cRadioButtons = countof(rgRadioButtons);
        config.nDefaultRadioButton = IDYES;

        hr = ::TaskDialogIndirect(&config, &nButton, &nRadioButton, NULL);
        BalExitOnFailure(hr, "Failed to show Netfx files-in-use task dialog.");

        *pnResult = IDOK == nButton ? nRadioButton : nButton;

#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: NetfxFilesInUse task dialog result: button - %d, radio button - %d, result - %d", nButton, nRadioButton, *pnResult);
#endif

    LExit:
        ReleaseStr(sczTitle);
        ReleaseStr(sczLabel);
        ReleaseStr(sczNetfxCloseRadioButton);
        ReleaseStr(sczDontCloseRadioButton);

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
        CWixStandardBootstrapperApplication* pThis = (CWixStandardBootstrapperApplication*)pvContext;
        BOOL fComInitialized = FALSE;
        BOOL fRet = FALSE;
        MSG msg = { };
        DWORD dwQuit = 0;
        WM_WIXSTDBA firstAction = WM_WIXSTDBA_DETECT_PACKAGES;

        // Initialize COM and theme.
        hr = ::CoInitialize(NULL);
        BalExitOnFailure(hr, "Failed to initialize COM.");
        fComInitialized = TRUE;

        hr = pThis->InitializeTheme();
        BalExitOnFailure(hr, "Failed to initialize theme.");

        // Create main window.
        pThis->InitializeTaskbarButton();
        hr = pThis->CreateMainWindow();
        BalExitOnFailure(hr, "Failed to create wixstdba main window.");

        if (FAILED(pThis->m_hrFinal))
        {
            pThis->SetState(WIXSTDBA_STATE_FAILED, hr);
            firstAction = WM_WIXSTDBA_SHOW_FAILURE;
        }
        else
        {
            // Okay, we're ready for packages now.
            pThis->SetState(WIXSTDBA_STATE_INITIALIZED, hr);

            if (pThis->m_fHandleHelp && BOOTSTRAPPER_ACTION_HELP == pThis->m_commandAction)
            {
                firstAction = WM_WIXSTDBA_SHOW_HELP;
            }
        }

        ::PostMessageW(pThis->m_hWnd, firstAction, 0, 0);

        // message pump
        while (0 != (fRet = ::GetMessageW(&msg, NULL, 0, 0)))
        {
            if (-1 == fRet)
            {
                hr = E_UNEXPECTED;
                BalExitOnFailure(hr, "Unexpected return value from message pump.");
            }
            else if (!ThemeHandleKeyboardMessage(pThis->m_pTheme, msg.hwnd, &msg))
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
        pThis->UninitializeTaskbarButton();

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

        ReleaseTheme(pThis->m_pTheme);
        ThemeUninitialize();

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
    HRESULT InitializeData(
        __in BOOTSTRAPPER_COMMAND* pCommand
    )
    {
        HRESULT hr = S_OK;
        IXMLDOMDocument* pixdManifest = NULL;

        hr = XmlInitialize();
        BalExitOnFailure(hr, "Failed to initialize XML.");

        hr = BalManifestLoad(m_hModule, &pixdManifest);
        BalExitOnFailure(hr, "Failed to load bootstrapper application manifest.");

        hr = BalInfoParseFromXml(&m_Bundle, pixdManifest);
        BalExitOnFailure(hr, "Failed to load bundle information.");

        hr = ProcessCommandLine(&m_sczLanguage);
        ExitOnFailure(hr, "Unknown commandline parameters.");

        hr = BalConditionsParseFromXml(&m_Conditions, pixdManifest, m_pWixLoc);
        BalExitOnFailure(hr, "Failed to load conditions from XML.");

        hr = LoadBAFunctions(pixdManifest, pCommand);
        BalExitOnFailure(hr, "Failed to load bootstrapper functions.");

        GetBundleFileVersion();
        // don't fail if we couldn't get the version info; best-effort only

        if (m_fPrereq)
        {
            hr = InitializePrerequisiteInformation(pixdManifest);
            BalExitOnFailure(hr, "Failed to initialize prerequisite information.");
        }
        else
        {
            hr = ParseBootstrapperApplicationDataFromXml(pixdManifest);
            BalExitOnFailure(hr, "Failed to read bootstrapper application data.");
        }

        if (m_fRequestedCacheOnly)
        {
            if (m_fSupportCacheOnly)
            {
                // Doesn't make sense to prompt the user if cache only is requested.
                if (BOOTSTRAPPER_DISPLAY_PASSIVE < m_commandDisplay)
                {
                    m_commandDisplay = BOOTSTRAPPER_DISPLAY_PASSIVE;
                }

                m_commandAction = BOOTSTRAPPER_ACTION_CACHE;
            }
            else
            {
                BalLog(BOOTSTRAPPER_LOG_LEVEL_ERROR, "Ignoring attempt to only cache a bundle that does not explicitly support it.");
            }
        }

    LExit:
        ReleaseObject(pixdManifest);

        return hr;
    }


    //
    // InitializeTheme - initializes the theme information.
    //
    HRESULT InitializeTheme()
    {
        HRESULT hr = S_OK;
        LPWSTR sczModulePath = NULL;

        hr = ThemeInitialize(m_hModule);
        BalExitOnFailure(hr, "Failed to initialize theme manager.");

        hr = PathRelativeToModule(&sczModulePath, NULL, m_hModule);
        BalExitOnFailure(hr, "Failed to get module path.");

        hr = LoadLocalization(sczModulePath, m_sczLanguage);
        ExitOnFailure(hr, "Failed to load localization.");

        LoadFilesInUse();

        hr = LoadTheme(sczModulePath, m_sczLanguage);
        ExitOnFailure(hr, "Failed to load theme.");

    LExit:
        ReleaseStr(sczModulePath);

        return hr;
    }

    //
    // ProcessCommandLine - process the provided command line arguments.
    //
    HRESULT ProcessCommandLine(
        __inout LPWSTR* psczLanguage
        )
    {
        HRESULT hr = S_OK;
        int argc = 0;
        LPWSTR* argv = NULL;
        BOOL fUnknownArg = FALSE;

        argc = m_BalInfoCommand.cUnknownArgs;
        argv = m_BalInfoCommand.rgUnknownArgs;

        if (argc)
        {
            for (int i = 0; i < argc; ++i)
            {
                fUnknownArg = FALSE;

                if (argv[i][0] == L'-' || argv[i][0] == L'/')
                {
                    if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"lang", -1))
                    {
                        if (i + 1 >= argc)
                        {
                            hr = E_INVALIDARG;
                            BalExitOnFailure(hr, "Must specify a language.");
                        }

                        ++i;

                        hr = StrAllocString(psczLanguage, &argv[i][0], 0);
                        BalExitOnFailure(hr, "Failed to copy language.");
                    }
                    else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"cache", -1))
                    {
                        m_fRequestedCacheOnly = TRUE;
                    }
                    else
                    {
                        fUnknownArg = TRUE;
                    }
                }
                else
                {
                    fUnknownArg = TRUE;
                }

                if (fUnknownArg)
                {
                    BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Ignoring unknown argument: %ls", argv[i]);
                }
            }
        }

        hr = BalSetOverridableVariablesFromEngine(&m_Bundle.overridableVariables, &m_BalInfoCommand, m_pEngine);
        BalExitOnFailure(hr, "Failed to set overridable variables from the command line.");

    LExit:
        return hr;
    }

    HRESULT LoadLocalization(
        __in_z LPCWSTR wzModulePath,
        __in_z_opt LPCWSTR wzLanguage
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczLocPath = NULL;
        LPWSTR sczFormatted = NULL;
        LPCWSTR wzLocFileName = m_fPrereq ? L"wixpreq.wxl" : L"thm.wxl";

        // Find and load .wxl file.
        hr = LocProbeForFile(wzModulePath, wzLocFileName, wzLanguage, &sczLocPath);
        BalExitOnFailure(hr, "Failed to probe for loc file: %ls in path: %ls", wzLocFileName, wzModulePath);

        hr = LocLoadFromFile(sczLocPath, &m_pWixLoc);
        BalExitOnFailure(hr, "Failed to load loc file from path: %ls", sczLocPath);

        // Set WixStdBALanguageId to .wxl language id.
        if (WIX_LOCALIZATION_LANGUAGE_NOT_SET != m_pWixLoc->dwLangId)
        {
            ::SetThreadLocale(m_pWixLoc->dwLangId);

            hr = m_pEngine->SetVariableNumeric(WIXSTDBA_VARIABLE_LANGUAGE_ID, m_pWixLoc->dwLangId);
            BalExitOnFailure(hr, "Failed to set WixStdBALanguageId variable.");
        }

        // Load ConfirmCancelMessage.
        hr = StrAllocString(&m_sczConfirmCloseMessage, L"#(loc.ConfirmCancelMessage)", 0);
        ExitOnFailure(hr, "Failed to initialize confirm message loc identifier.");

        hr = LocLocalizeString(m_pWixLoc, &m_sczConfirmCloseMessage);
        BalExitOnFailure(hr, "Failed to localize confirm close message: %ls", m_sczConfirmCloseMessage);

        hr = BalFormatString(m_sczConfirmCloseMessage, &sczFormatted);
        if (SUCCEEDED(hr))
        {
            ReleaseStr(m_sczConfirmCloseMessage);
            m_sczConfirmCloseMessage = sczFormatted;
            sczFormatted = NULL;
        }

    LExit:
        ReleaseStr(sczFormatted);
        ReleaseStr(sczLocPath);

        return hr;
    }


    HRESULT LoadIndividualLocString(
        __in_z LPCWSTR wzId,
        __out LOC_STRING** ppLocString
        )
    {
        HRESULT hr = LocGetString(m_pWixLoc, wzId, ppLocString);

        if (E_NOTFOUND == hr)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_DEBUG, "WIXSTDBA: Missing loc string '%ls'.", wzId);
        }

        return hr;
    }


    void LoadFilesInUse()
    {
        // Get the loc strings for the files-in-use dialogs.
        LoadIndividualLocString(L"#(loc.FilesInUseTitle)", &m_pFilesInUseTitleLoc);
        LoadIndividualLocString(L"#(loc.FilesInUseLabel)", &m_pFilesInUseLabelLoc);
        LoadIndividualLocString(L"#(loc.FilesInUseCloseRadioButton)", &m_pFilesInUseCloseRadioButtonLoc);
        LoadIndividualLocString(L"#(loc.FilesInUseDontCloseRadioButton)", &m_pFilesInUseDontCloseRadioButtonLoc);
        LoadIndividualLocString(L"#(loc.FilesInUseRetryButton)", &m_pFilesInUseRetryButtonLoc);
        LoadIndividualLocString(L"#(loc.FilesInUseIgnoreButton)", &m_pFilesInUseIgnoreButtonLoc);
        LoadIndividualLocString(L"#(loc.FilesInUseExitButton)", &m_pFilesInUseExitButtonLoc);
        LoadIndividualLocString(L"#(loc.FilesInUseNetfxCloseRadioButton)", &m_pFilesInUseNetfxCloseRadioButtonLoc);

        m_fShowRMFilesInUse = m_pFilesInUseTitleLoc && m_pFilesInUseLabelLoc && m_pFilesInUseCloseRadioButtonLoc && m_pFilesInUseDontCloseRadioButtonLoc;
        m_fShowStandardFilesInUse = m_pFilesInUseTitleLoc && m_pFilesInUseLabelLoc && m_pFilesInUseRetryButtonLoc && m_pFilesInUseIgnoreButtonLoc && m_pFilesInUseExitButtonLoc;
        m_fShowNetfxFilesInUse = m_pFilesInUseTitleLoc && m_pFilesInUseLabelLoc && m_pFilesInUseNetfxCloseRadioButtonLoc && m_pFilesInUseDontCloseRadioButtonLoc;
    }


    HRESULT LoadTheme(
        __in_z LPCWSTR wzModulePath,
        __in_z_opt LPCWSTR wzLanguage
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczThemePath = NULL;
        LPCWSTR wzThemeFileName = m_fPrereq ? L"wixpreq.thm" : L"thm.xml";

        hr = LocProbeForFile(wzModulePath, wzThemeFileName, wzLanguage, &sczThemePath);
        BalExitOnFailure(hr, "Failed to probe for theme file: %ls in path: %ls", wzThemeFileName, wzModulePath);

        hr = ThemeLoadFromFile(sczThemePath, &m_pTheme);
        BalExitOnFailure(hr, "Failed to load theme from path: %ls", sczThemePath);

        hr = ThemeRegisterVariableCallbacks(m_pTheme, EvaluateVariableConditionCallback, FormatVariableStringCallback, GetVariableNumericCallback, SetVariableNumericCallback, GetVariableStringCallback, SetVariableStringCallback, NULL);
        BalExitOnFailure(hr, "Failed to register variable theme callbacks.");

        C_ASSERT(COUNT_WIXSTDBA_PAGE == countof(vrgwzPageNames));
        C_ASSERT(countof(m_rgdwPageIds) == countof(vrgwzPageNames));

        ThemeGetPageIds(m_pTheme, vrgwzPageNames, m_rgdwPageIds, countof(m_rgdwPageIds));

        hr = ThemeLocalize(m_pTheme, m_pWixLoc);
        BalExitOnFailure(hr, "Failed to localize theme: %ls", sczThemePath);

    LExit:
        ReleaseStr(sczThemePath);

        return hr;
    }


    HRESULT InitializePrerequisiteInformation(
        __in IXMLDOMDocument* pixdManifest
        )
    {
        HRESULT hr = S_OK;
        BAL_INFO_PACKAGE* pPackage = NULL;
        IXMLDOMNode* pNode = NULL;
        BOOL fXmlFound = FALSE;
        DWORD dwBool = 0;
        BOOL fHandleLayout = FALSE;

        // Read any prereq BA data from the BA manifest.
        hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixPrereqOptions", &pNode);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to read prereq options from BootstrapperApplication.xml manifest.");

        if (fXmlFound)
        {
            hr = XmlGetAttributeNumber(pNode, L"Primary", &dwBool);
            BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get WixPrereqOptions/@Primary value.");

            m_fPreplanPrereqs = fXmlFound && (0 != dwBool);

            hr = XmlGetAttributeNumber(pNode, L"HandleHelp", &dwBool);
            BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get WixPrereqOptions/@HandleHelp value.");

            m_fHandleHelp = fXmlFound && (0 != dwBool);

            hr = XmlGetAttributeNumber(pNode, L"HandleLayout", &dwBool);
            BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get WixPrereqOptions/@HandleLayout value.");

            fHandleLayout = fXmlFound && (0 != dwBool);
        }

        // If pre-req BA has requested that this BA be in charge of layout.
        if (fHandleLayout && BOOTSTRAPPER_ACTION_LAYOUT == m_commandAction)
        {
            m_fPrereq = FALSE;
            ExitFunction();
        }

        // Pre-req BA should only show help or do an install (to launch the parent BA which can then do the right action).
        if (BOOTSTRAPPER_ACTION_HELP != m_commandAction)
        {
            m_commandAction = BOOTSTRAPPER_ACTION_INSTALL;
        }

        for (DWORD i = 0; i < m_Bundle.packages.cPackages; ++i)
        {
            pPackage = &m_Bundle.packages.rgPackages[i];
            if (!pPackage->fPrereqPackage)
            {
                continue;
            }

            if (pPackage->sczPrereqLicenseFile)
            {
                if (m_sczLicenseFile)
                {
                    hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
                    BalExitOnFailure(hr, "More than one license file specified in prerequisite info.");
                }

                hr = StrAllocString(&m_sczLicenseFile, pPackage->sczPrereqLicenseFile, 0);
                BalExitOnFailure(hr, "Failed to copy license file location from prereq package.");
            }

            if (pPackage->sczPrereqLicenseUrl)
            {
                if (m_sczLicenseUrl)
                {
                    hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
                    BalExitOnFailure(hr, "More than one license URL specified in prerequisite info.");
                }

                hr = StrAllocString(&m_sczLicenseUrl, pPackage->sczPrereqLicenseUrl, 0);
                BalExitOnFailure(hr, "Failed to copy license URL from prereq package.");
            }
        }

    LExit:
        return hr;
    }


    HRESULT ParseBootstrapperApplicationDataFromXml(
        __in IXMLDOMDocument* pixdManifest
        )
    {
        HRESULT hr = S_OK;
        IXMLDOMNode* pNode = NULL;
        DWORD dwBool = 0;
        BOOL fXmlFound = FALSE;

        hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixStdbaInformation", &pNode);
        BalExitOnRequiredXmlQueryFailure(hr, "BootstrapperApplication.xml manifest is missing wixstdba information.");

        hr = XmlGetAttributeEx(pNode, L"LicenseFile", &m_sczLicenseFile);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get license file.");

        hr = XmlGetAttributeEx(pNode, L"LicenseUrl", &m_sczLicenseUrl);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get license URL.");

        ReleaseObject(pNode);

        hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixStdbaOptions", &pNode);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to read wixstdba options from BootstrapperApplication.xml manifest.");

        if (!fXmlFound)
        {
            ExitFunction();
        }

        hr = XmlGetAttributeNumber(pNode, L"SuppressOptionsUI", &dwBool);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get SuppressOptionsUI value.");

        if (fXmlFound && dwBool)
        {
            hr = BalSetNumericVariable(WIXSTDBA_VARIABLE_SUPPRESS_OPTIONS_UI, 1);
            BalExitOnFailure(hr, "Failed to set '%ls' variable.", WIXSTDBA_VARIABLE_SUPPRESS_OPTIONS_UI);
        }

        dwBool = 0;
        hr = XmlGetAttributeNumber(pNode, L"SuppressDowngradeFailure", &dwBool);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get SuppressDowngradeFailure value.");

        if (fXmlFound)
        {
            m_fSuppressDowngradeFailure = 0 < dwBool;
        }

        dwBool = 0;
        hr = XmlGetAttributeNumber(pNode, L"SuppressRepair", &dwBool);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get SuppressRepair value.");

        if (fXmlFound)
        {
            m_fSuppressRepair = 0 < dwBool;
        }

        hr = XmlGetAttributeNumber(pNode, L"ShowVersion", &dwBool);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get ShowVersion value.");

        if (fXmlFound && dwBool)
        {
            hr = BalSetNumericVariable(WIXSTDBA_VARIABLE_SHOW_VERSION, 1);
            BalExitOnFailure(hr, "Failed to set '%ls' variable.", WIXSTDBA_VARIABLE_SHOW_VERSION);
        }

        hr = XmlGetAttributeNumber(pNode, L"SupportCacheOnly", &dwBool);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get SupportCacheOnly value.");

        if (fXmlFound)
        {
            m_fSupportCacheOnly = 0 < dwBool;
        }

    LExit:
        ReleaseObject(pNode);
        return hr;
    }


    //
    // Get the file version of the bootstrapper and record in bootstrapper log file
    //
    HRESULT GetBundleFileVersion()
    {
        HRESULT hr = S_OK;
        ULARGE_INTEGER uliVersion = { };
        LPWSTR sczCurrentPath = NULL;
        VERUTIL_VERSION* pVersion = NULL;

        hr = PathForCurrentProcess(&sczCurrentPath, NULL);
        BalExitOnFailure(hr, "Failed to get bundle path.");

        hr = FileVersion(sczCurrentPath, &uliVersion.HighPart, &uliVersion.LowPart);
        BalExitOnFailure(hr, "Failed to get bundle file version.");

        hr = VerVersionFromQword(uliVersion.QuadPart, &pVersion);
        BalExitOnFailure(hr, "Failed to create bundle file version.");

        hr = m_pEngine->SetVariableVersion(WIXSTDBA_VARIABLE_BUNDLE_FILE_VERSION, pVersion->sczVersion);
        BalExitOnFailure(hr, "Failed to set WixBundleFileVersion variable.");

    LExit:
        ReleaseVerutilVersion(pVersion);
        ReleaseStr(sczCurrentPath);

        return hr;
    }


    //
    // CreateMainWindow - creates the main install window.
    //
    HRESULT CreateMainWindow()
    {
        HRESULT hr = S_OK;
        WNDCLASSW wc = { };
        DWORD dwWindowStyle = 0;
        int x = CW_USEDEFAULT;
        int y = CW_USEDEFAULT;
        POINT ptCursor = { };

        ThemeInitializeWindowClass(m_pTheme, &wc, CWixStandardBootstrapperApplication::WndProc, m_hModule, WIXSTDBA_WINDOW_CLASS);

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

        // Calculate the window style based on the theme style and command display value.
        dwWindowStyle = m_pTheme->dwStyle;
        if (BOOTSTRAPPER_DISPLAY_NONE >= m_commandDisplay || m_fPreplanPrereqs)
        {
            dwWindowStyle &= ~WS_VISIBLE;
        }

        // Don't show the window if there is a splash screen (it will be made visible when the splash screen is hidden)
        if (::IsWindow(m_hwndSplashScreen))
        {
            dwWindowStyle &= ~WS_VISIBLE;
        }

        // Center the window on the monitor with the mouse.
        if (::GetCursorPos(&ptCursor))
        {
            x = ptCursor.x;
            y = ptCursor.y;
        }

        hr = ThemeCreateParentWindow(m_pTheme, 0, wc.lpszClassName, m_pTheme->sczCaption, dwWindowStyle, x, y, HWND_DESKTOP, m_hModule, this, THEME_WINDOW_INITIAL_POSITION_CENTER_MONITOR_FROM_COORDINATES, &m_hWnd);
        ExitOnFailure(hr, "Failed to create wixstdba theme window.");

        OnThemeLoaded();

        hr = S_OK;

    LExit:
        return hr;
    }


    //
    // InitializeTaskbarButton - initializes taskbar button for progress.
    //
    void InitializeTaskbarButton()
    {
        HRESULT hr = S_OK;

        hr = ::CoCreateInstance(CLSID_TaskbarList, NULL, CLSCTX_ALL, __uuidof(ITaskbarList3), reinterpret_cast<LPVOID*>(&m_pTaskbarList));
        if (REGDB_E_CLASSNOTREG == hr) // not supported before Windows 7
        {
            ExitFunction1(hr = S_OK);
        }
        BalExitOnFailure(hr, "Failed to create ITaskbarList3. Continuing.");

        m_uTaskbarButtonCreatedMessage = ::RegisterWindowMessageW(L"TaskbarButtonCreated");
        BalExitOnNullWithLastError(m_uTaskbarButtonCreatedMessage, hr, "Failed to get TaskbarButtonCreated message. Continuing.");

    LExit:
        return;
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
            m_fTaskbarButtonOK = FALSE;
        }

        if (m_fRegistered)
        {
            ::UnregisterClassW(WIXSTDBA_WINDOW_CLASS, m_hModule);
            m_fRegistered = FALSE;
        }
    }


    //
    // UninitializeTaskbarButton - clean up the taskbar registration.
    //
    void UninitializeTaskbarButton()
    {
        m_fTaskbarButtonOK = FALSE;
        ReleaseNullObject(m_pTaskbarList);
    }


    static LRESULT CallDefaultWndProc(
        __in CWixStandardBootstrapperApplication* pBA,
        __in HWND hWnd,
        __in UINT uMsg,
        __in WPARAM wParam,
        __in LPARAM lParam
        )
    {
        LRESULT lres = NULL;
        THEME* pTheme = NULL;
        HRESULT hr = S_OK;
        BA_FUNCTIONS_WNDPROC_ARGS wndProcArgs = { };
        BA_FUNCTIONS_WNDPROC_RESULTS wndProcResults = { };

        if (pBA)
        {
            pTheme = pBA->m_pTheme;

            if (pBA->m_pfnBAFunctionsProc)
            {
                wndProcArgs.cbSize = sizeof(wndProcArgs);
                wndProcArgs.hWnd = hWnd;
                wndProcArgs.uMsg = uMsg;
                wndProcArgs.wParam = wParam;
                wndProcArgs.lParam = lParam;
                wndProcResults.cbSize = sizeof(wndProcResults);

                hr = pBA->m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_WNDPROC, &wndProcArgs, &wndProcResults, pBA->m_pvBAFunctionsProcContext);

                if (E_NOTIMPL == hr)
                {
                    hr = S_OK;
                }
                else
                {
                    BalExitOnFailure(hr, "BAFunctions WndProc failed.");

                    if (wndProcResults.fProcessed)
                    {
                        lres = wndProcResults.lResult;
                        ExitFunction();
                    }
                }
            }
        }

        lres = ThemeDefWindowProc(pTheme, hWnd, uMsg, wParam, lParam);

    LExit:
        return lres;
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
        CWixStandardBootstrapperApplication* pBA = reinterpret_cast<CWixStandardBootstrapperApplication*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

        switch (uMsg)
        {
        case WM_NCCREATE:
        {
            LPCREATESTRUCT lpcs = reinterpret_cast<LPCREATESTRUCT>(lParam);
            pBA = reinterpret_cast<CWixStandardBootstrapperApplication*>(lpcs->lpCreateParams);
#pragma warning(suppress:4244)
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pBA));
        }
        break;

        case WM_NCDESTROY:
        {
            LRESULT lres = CallDefaultWndProc(pBA, hWnd, uMsg, wParam, lParam);
            ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
            ::PostQuitMessage(0);
            return lres;
        }

        case WM_THMUTIL_LOADING_CONTROL:
            return pBA->OnThemeLoadingControl(reinterpret_cast<THEME_LOADINGCONTROL_ARGS*>(wParam), reinterpret_cast<THEME_LOADINGCONTROL_RESULTS*>(lParam));

        case WM_THMUTIL_LOADED_CONTROL:
            return pBA->OnThemeLoadedControl(reinterpret_cast<THEME_LOADEDCONTROL_ARGS*>(wParam), reinterpret_cast<THEME_LOADEDCONTROL_RESULTS*>(lParam));

        case WM_CLOSE:
            // If the user chose not to close, do *not* let the default window proc handle the message.
            if (!pBA->OnClose())
            {
                return 0;
            }
            break;

        case WM_WIXSTDBA_SHOW_HELP:
            pBA->OnShowHelp();
            return 0;

        case WM_WIXSTDBA_DETECT_PACKAGES:
            pBA->OnDetect();
            return 0;

        case WM_WIXSTDBA_PLAN_PACKAGES:
            pBA->OnPlan(static_cast<BOOTSTRAPPER_ACTION>(lParam));
            return 0;

        case WM_WIXSTDBA_APPLY_PACKAGES:
            pBA->OnApply();
            return 0;

        case WM_WIXSTDBA_CHANGE_STATE:
            pBA->OnChangeState(static_cast<WIXSTDBA_STATE>(lParam));
            return 0;

        case WM_WIXSTDBA_SHOW_FAILURE:
            pBA->OnShowFailure();
            return 0;

        case WM_WIXSTDBA_PLAN_PREREQS:
            pBA->OnPlanPrereqs(static_cast<BOOTSTRAPPER_ACTION>(lParam));
            return 0;

        case WM_THMUTIL_CONTROL_WM_COMMAND:
            return pBA->OnThemeControlWmCommand(reinterpret_cast<THEME_CONTROLWMCOMMAND_ARGS*>(wParam), reinterpret_cast<THEME_CONTROLWMCOMMAND_RESULTS*>(lParam));

        case WM_THMUTIL_CONTROL_WM_NOTIFY:
            return pBA->OnThemeControlWmNotify(reinterpret_cast<THEME_CONTROLWMNOTIFY_ARGS*>(wParam), reinterpret_cast<THEME_CONTROLWMNOTIFY_RESULTS*>(lParam));
        }

        if (pBA && pBA->m_pTaskbarList && uMsg == pBA->m_uTaskbarButtonCreatedMessage)
        {
            pBA->m_fTaskbarButtonOK = TRUE;
            return 0;
        }

        return CallDefaultWndProc(pBA, hWnd, uMsg, wParam, lParam);
    }


    //
    // OnThemeLoaded - finishes loading the theme.
    //
    BOOL OnThemeLoaded()
    {
        HRESULT hr = S_OK;
        BA_FUNCTIONS_ONTHEMELOADED_ARGS themeLoadedArgs = { };
        BA_FUNCTIONS_ONTHEMELOADED_RESULTS themeLoadedResults = { };

        if (m_pfnBAFunctionsProc)
        {
            themeLoadedArgs.cbSize = sizeof(themeLoadedArgs);
            themeLoadedArgs.hWnd = m_pTheme->hwndParent;
            themeLoadedResults.cbSize = sizeof(themeLoadedResults);
            hr = m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONTHEMELOADED, &themeLoadedArgs, &themeLoadedResults, m_pvBAFunctionsProcContext);
            BalExitOnFailure(hr, "BAFunctions OnThemeLoaded failed.");
        }

    LExit:
        return SUCCEEDED(hr);
    }

    BOOL OnThemeLoadingControl(
        __in const THEME_LOADINGCONTROL_ARGS* pArgs,
        __in THEME_LOADINGCONTROL_RESULTS* pResults
        )
    {
        HRESULT hr = S_OK;
        BOOL fProcessed = FALSE;
        BA_FUNCTIONS_ONTHEMECONTROLLOADING_ARGS themeControlLoadingArgs = { };
        BA_FUNCTIONS_ONTHEMECONTROLLOADING_RESULTS themeControlLoadingResults = { };

        for (DWORD iAssignControl = 0; iAssignControl < countof(m_rgInitControls); ++iAssignControl)
        {
            THEME_ASSIGN_CONTROL_ID* pAssignControl = m_rgInitControls + iAssignControl;
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pArgs->pThemeControl->sczName, -1, pAssignControl->wzName, -1))
            {
                if (!pAssignControl->ppControl)
                {
                    BalExitWithRootFailure(hr, E_INVALIDSTATE, "Control '%ls' has no member variable", pAssignControl->wzName);
                }

                if (*pAssignControl->ppControl)
                {
                    BalLog(BOOTSTRAPPER_LOG_LEVEL_ERROR, "Duplicate control name: %ls", pAssignControl->wzName);
                }
                else
                {
                    *pAssignControl->ppControl = pArgs->pThemeControl;
                }

                fProcessed = TRUE;
                pResults->wId = pAssignControl->wId;
                pResults->dwAutomaticBehaviorType = pAssignControl->dwAutomaticBehaviorType;
                ExitFunction();
            }
        }

        if (m_pfnBAFunctionsProc)
        {
            themeControlLoadingArgs.cbSize = sizeof(themeControlLoadingArgs);
            themeControlLoadingArgs.wzName = pArgs->pThemeControl->sczName;

            themeControlLoadingResults.cbSize = sizeof(themeControlLoadingResults);
            themeControlLoadingResults.wId = pResults->wId;
            themeControlLoadingResults.dwAutomaticBehaviorType = pResults->dwAutomaticBehaviorType;

            hr = m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONTHEMECONTROLLOADING, &themeControlLoadingArgs, &themeControlLoadingResults, m_pvBAFunctionsProcContext);

            if (E_NOTIMPL == hr)
            {
                hr = S_OK;
            }
            else
            {
                BalExitOnFailure(hr, "BAFunctions OnThemeControlLoading failed.");

                if (themeControlLoadingResults.fProcessed)
                {
                    fProcessed = TRUE;
                    pResults->wId = themeControlLoadingResults.wId;
                    pResults->dwAutomaticBehaviorType = themeControlLoadingResults.dwAutomaticBehaviorType;
                }
            }
        }

    LExit:
        pResults->hr = hr;
        return fProcessed || FAILED(hr);
    }

    BOOL OnThemeLoadedControl(
        __in const THEME_LOADEDCONTROL_ARGS* pArgs,
        __in THEME_LOADEDCONTROL_RESULTS* pResults
        )
    {
        HRESULT hr = S_OK;
        BOOL fProcessed = FALSE;
        BA_FUNCTIONS_ONTHEMECONTROLLOADED_ARGS themeControlLoadedArgs = { };
        BA_FUNCTIONS_ONTHEMECONTROLLOADED_RESULTS themeControlLoadedResults = { };

        if (WIXSTDBA_CONTROL_EULA_RICHEDIT == pArgs->pThemeControl->wId)
        {
            // Best effort to load the RTF EULA control with text.
            OnLoadedEulaRtfControl(pArgs->pThemeControl);
            fProcessed = TRUE;
            ExitFunction();
        }

        if (m_pfnBAFunctionsProc)
        {
            themeControlLoadedArgs.cbSize = sizeof(themeControlLoadedArgs);
            themeControlLoadedArgs.wzName = pArgs->pThemeControl->sczName;
            themeControlLoadedArgs.wId = pArgs->pThemeControl->wId;
            themeControlLoadedArgs.hWnd = pArgs->pThemeControl->hWnd;

            themeControlLoadedResults.cbSize = sizeof(themeControlLoadedResults);

            hr = m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONTHEMECONTROLLOADED, &themeControlLoadedArgs, &themeControlLoadedResults, m_pvBAFunctionsProcContext);

            if (E_NOTIMPL == hr)
            {
                hr = S_OK;
            }
            else
            {
                BalExitOnFailure(hr, "BAFunctions OnThemeControlLoaded failed.");

                if (themeControlLoadedResults.fProcessed)
                {
                    fProcessed = TRUE;
                }
            }
        }

    LExit:
        pResults->hr = hr;
        return fProcessed || FAILED(hr);
    }

    HRESULT OnLoadedEulaRtfControl(
        const THEME_CONTROL* pThemeControl
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczLicenseFormatted = NULL;
        LPWSTR sczLicensePath = NULL;
        LPWSTR sczLicenseDirectory = NULL;
        LPWSTR sczLicenseFilename = NULL;

        if (!m_sczLicenseFile || !*m_sczLicenseFile)
        {
            ExitWithRootFailure(hr, E_INVALIDDATA, "No license file in manifest.");
        }

        hr = StrAllocString(&sczLicenseFormatted, m_sczLicenseFile, 0);
        ExitOnFailure(hr, "Failed to copy manifest license file.");

        hr = LocLocalizeString(m_pWixLoc, &sczLicenseFormatted);
        ExitOnFailure(hr, "Failed to localize manifest license file.");

        hr = BalFormatString(sczLicenseFormatted, &sczLicenseFormatted);
        ExitOnFailure(hr, "Failed to expand localized manifest license file.");

        hr = PathRelativeToModule(&sczLicensePath, sczLicenseFormatted, m_hModule);
        ExitOnFailure(hr, "Failed to get relative path for license file.");

        hr = PathGetDirectory(sczLicensePath, &sczLicenseDirectory);
        ExitOnFailure(hr, "Failed to get license file directory.");

        hr = StrAllocString(&sczLicenseFilename, PathFile(sczLicenseFormatted), 0);
        ExitOnFailure(hr, "Failed to copy license file name.");

        hr = LocProbeForFile(sczLicenseDirectory, sczLicenseFilename, m_sczLanguage, &sczLicensePath);
        ExitOnFailure(hr, "Failed to probe for localized license file.");

        hr = ThemeLoadRichEditFromFile(pThemeControl, sczLicensePath, m_hModule);
        ExitOnFailure(hr, "Failed to load license file into richedit control.");

    LExit:
        if (FAILED(hr))
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_ERROR, "Failed to load file into license richedit control from path '%ls' manifest value: %ls", sczLicensePath, m_sczLicenseFile);
        }

        ReleaseStr(sczLicenseFilename);
        ReleaseStr(sczLicenseDirectory);
        ReleaseStr(sczLicensePath);
        ReleaseStr(sczLicenseFormatted);

        return hr;
    }


    //
    // OnShowFailure - display the failure page.
    //
    void OnShowFailure()
    {
        SetState(WIXSTDBA_STATE_FAILED, S_OK);

        // If the UI should be visible, display it now and hide the splash screen
        if (BOOTSTRAPPER_DISPLAY_NONE < m_commandDisplay)
        {
            ::ShowWindow(m_pTheme->hwndParent, SW_SHOW);
        }

        m_pEngine->CloseSplashScreen();
    }


    //
    // OnShowHelp - display the help page.
    //
    void OnShowHelp()
    {
        SetState(WIXSTDBA_STATE_HELP, S_OK);

        // If the UI should be visible, display it now and hide the splash screen
        if (BOOTSTRAPPER_DISPLAY_NONE < m_commandDisplay)
        {
            ::ShowWindow(m_pTheme->hwndParent, SW_SHOW);
        }

        m_pEngine->CloseSplashScreen();
    }


    //
    // OnDetect - start the processing of packages.
    //
    void OnDetect()
    {
        HRESULT hr = S_OK;

        SetState(WIXSTDBA_STATE_DETECTING, hr);

        // Tell the core we're ready for the packages to be processed now.
        hr = m_pEngine->Detect();
        BalExitOnFailure(hr, "Failed to start detecting chain.");

    LExit:
        if (FAILED(hr))
        {
            SetState(WIXSTDBA_STATE_DETECTING, hr);
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

        SetState(WIXSTDBA_STATE_PLANNING, hr);

        hr = m_pEngine->Plan(action);
        BalExitOnFailure(hr, "Failed to start planning packages.");

    LExit:
        if (FAILED(hr))
        {
            SetState(WIXSTDBA_STATE_PLANNING, hr);
        }
    }


    //
    // OnPlanPrereqs - preplan the packages to see if the preqba can be skipped.
    //
    void OnPlanPrereqs(
        __in BOOTSTRAPPER_ACTION action
        )
    {
        HRESULT hr = S_OK;

        m_plannedAction = action;

        SetState(WIXSTDBA_STATE_PLANNING_PREREQS, hr);

        hr = m_pEngine->Plan(action);
        BalExitOnFailure(hr, "Failed to start planning prereq packages.");

    LExit:
        if (FAILED(hr))
        {
            SetState(WIXSTDBA_STATE_PLANNING_PREREQS, hr);
        }
    }


    //
    // OnApply - apply the packages.
    //
    void OnApply()
    {
        HRESULT hr = S_OK;

        SetState(WIXSTDBA_STATE_APPLYING, hr);
        SetProgressState(hr);
        SetTaskbarButtonProgress(0);

        hr = m_pEngine->Apply(m_hWnd);
        BalExitOnFailure(hr, "Failed to start applying packages.");

        ThemeControlEnable(m_pControlProgressCancelButton, TRUE); // ensure the cancel button is enabled before starting.

    LExit:
        if (FAILED(hr))
        {
            SetState(WIXSTDBA_STATE_APPLYING, hr);
        }
    }


    //
    // OnChangeState - change state.
    //
    void OnChangeState(
        __in WIXSTDBA_STATE state
        )
    {
        WIXSTDBA_STATE stateOld = m_state;
        DWORD dwOldPageId = 0;
        DWORD dwNewPageId = 0;
        LPWSTR sczText = NULL;
        LPWSTR sczUnformattedText = NULL;
        LPWSTR sczControlState = NULL;
        LPWSTR sczControlName = NULL;

        m_state = state;

        // If our install is at the end (success or failure) and we're not showing full UI or not updating or
        // we successfully installed the prerequisite(s) and either no restart is required or can automatically restart
        // then exit.
        if ((WIXSTDBA_STATE_APPLIED <= m_state && (BOOTSTRAPPER_DISPLAY_FULL > m_commandDisplay || BOOTSTRAPPER_ACTION_UPDATE_REPLACE == m_plannedAction)) ||
            (WIXSTDBA_STATE_APPLIED == m_state && m_fPrereq && (!m_fRestartRequired || m_fShouldRestart && m_fAllowRestart)))
        {
            // Quietly exit.
            ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);
        }
        else // try to change the pages.
        {
            DeterminePageId(stateOld, &dwOldPageId);
            DeterminePageId(m_state, &dwNewPageId);

            if (dwOldPageId != dwNewPageId)
            {
                LONGLONG llCanRestart = 0;
                LONGLONG llElevated = 0;

                BalGetNumericVariable(WIXBUNDLE_VARIABLE_CANRESTART, &llCanRestart);
                BalGetNumericVariable(WIXBUNDLE_VARIABLE_ELEVATED, &llElevated);
                m_fRestartRequiresElevation = !llCanRestart && !llElevated;

                // Enable disable controls per-page.
                if (m_rgdwPageIds[WIXSTDBA_PAGE_INSTALL] == dwNewPageId) // on the "Install" page, ensure the install button is enabled/disabled correctly.
                {
                    ThemeControlElevates(m_pControlInstallButton, (m_Bundle.fPerMachine && !llElevated));

                    // If the EULA control exists, show it only if a license URL is provided as well.
                    if (m_pControlEulaHyperlink)
                    {
                        BOOL fEulaLink = (m_sczLicenseUrl && *m_sczLicenseUrl);
                        ThemeControlEnable(m_pControlEulaHyperlink, fEulaLink);
                        ThemeControlEnable(m_pControlEulaAcceptCheckbox, fEulaLink);
                    }

                    BOOL fAcceptedLicense = !m_pControlEulaAcceptCheckbox || !ThemeControlEnabled(m_pControlEulaAcceptCheckbox) || ThemeIsControlChecked(m_pControlEulaAcceptCheckbox);
                    ThemeControlEnable(m_pControlInstallButton, fAcceptedLicense);
                }
                else if (m_rgdwPageIds[WIXSTDBA_PAGE_MODIFY] == dwNewPageId)
                {
                    ThemeControlElevates(m_pControlRepairButton, (m_Bundle.fPerMachine && !llElevated));
                    ThemeControlElevates(m_pControlUninstallButton, (m_Bundle.fPerMachine && !llElevated));

                    ThemeControlEnable(m_pControlRepairButton, !m_fSuppressRepair);
                }
                else if (m_rgdwPageIds[WIXSTDBA_PAGE_SUCCESS] == dwNewPageId) // on the "Success" page, check if the restart or launch button should be enabled.
                {
                    BOOL fEnableRestartButton = FALSE;
                    BOOL fLaunchTargetExists = FALSE;

                    ThemeControlElevates(m_pControlSuccessRestartButton, m_fRestartRequiresElevation);

                    if (m_fShouldRestart)
                    {
                        if (BAL_INFO_RESTART_PROMPT == m_BalInfoCommand.restart)
                        {
                            fEnableRestartButton = TRUE;
                        }
                    }
                    else if (m_pControlLaunchButton)
                    {
                        fLaunchTargetExists = BalVariableExists(WIXSTDBA_VARIABLE_LAUNCH_TARGET_PATH);
                    }

                    ThemeControlEnable(m_pControlLaunchButton, fLaunchTargetExists && BOOTSTRAPPER_ACTION_UNINSTALL < m_plannedAction);
                    ThemeControlEnable(m_pControlSuccessRestartButton, fEnableRestartButton);
                }
                else if (m_rgdwPageIds[WIXSTDBA_PAGE_FAILURE] == dwNewPageId) // on the "Failure" page, show error message and check if the restart button should be enabled.
                {
                    BOOL fShowLogLink = (m_Bundle.sczLogVariable && *m_Bundle.sczLogVariable); // if there is a log file variable then we'll assume the log file exists.
                    BOOL fShowErrorMessage = FALSE;
                    BOOL fEnableRestartButton = FALSE;

                    ThemeControlElevates(m_pControlFailureRestartButton, m_fRestartRequiresElevation);

                    if (FAILED(m_hrFinal))
                    {
                        // If we know the failure message, use that.
                        if (m_sczFailedMessage && *m_sczFailedMessage)
                        {
                            StrAllocString(&sczUnformattedText, m_sczFailedMessage, 0);
                        }
                        else if (E_PREREQBA_INFINITE_LOOP == m_hrFinal)
                        {
                            HRESULT hr = StrAllocString(&sczUnformattedText, L"#(loc.PREREQBAINFINITELOOPErrorMessage)", 0);
                            if (FAILED(hr))
                            {
                                BalLogError(hr, "Failed to initialize PREREQBAINFINITELOOPErrorMessage loc identifier.");
                            }
                            else
                            {
                                hr = LocLocalizeString(m_pWixLoc, &sczUnformattedText);
                                if (FAILED(hr))
                                {
                                    BalLogError(hr, "Failed to localize PREREQBAINFINITELOOPErrorMessage: %ls", sczUnformattedText);
                                    ReleaseNullStr(sczUnformattedText);
                                }
                            }
                        }
                        else // try to get the error message from the error code.
                        {
                            StrAllocFromError(&sczUnformattedText, m_hrFinal, NULL);
                            if (!sczUnformattedText || !*sczUnformattedText)
                            {
                                StrAllocFromError(&sczUnformattedText, E_FAIL, NULL);
                            }
                        }

                        if (E_WIXSTDBA_CONDITION_FAILED == m_hrFinal)
                        {
                            if (sczUnformattedText)
                            {
                                StrAllocString(&sczText, sczUnformattedText, 0);
                            }
                        }
                        else if (E_PREREQBA_INFINITE_LOOP == m_hrFinal)
                        {
                            if (sczUnformattedText)
                            {
                                BalFormatString(sczUnformattedText, &sczText);
                            }
                        }
                        else
                        {
                            StrAllocFormatted(&sczText, L"0x%08x - %ls", m_hrFinal, sczUnformattedText);
                        }

                        ThemeSetTextControl(m_pControlFailureMessageText, sczText);
                        fShowErrorMessage = TRUE;
                    }

                    if (m_fShouldRestart)
                    {
                        if (BAL_INFO_RESTART_PROMPT == m_BalInfoCommand.restart)
                        {
                            fEnableRestartButton = TRUE;
                        }
                    }

                    ThemeControlEnable(m_pControlFailureLogFileLink, fShowLogLink);
                    ThemeControlEnable(m_pControlFailureMessageText, fShowErrorMessage);
                    ThemeControlEnable(m_pControlFailureRestartButton, fEnableRestartButton);
                }

                HRESULT hr = ThemeShowPage(m_pTheme, dwOldPageId, SW_HIDE);
                if (FAILED(hr))
                {
                    BalLogError(hr, "Failed to hide page: %u", dwOldPageId);
                }

                hr = ThemeShowPage(m_pTheme, dwNewPageId, SW_SHOW);
                if (FAILED(hr))
                {
                    BalLogError(hr, "Failed to show page: %u", dwNewPageId);
                }

                // On the install page set the focus to the install button or the next enabled control if install is disabled.
                if (m_rgdwPageIds[WIXSTDBA_PAGE_INSTALL] == dwNewPageId)
                {
                    ThemeSetFocus(m_pControlInstallButton);
                }
            }
        }

        ReleaseStr(sczText);
        ReleaseStr(sczUnformattedText);
        ReleaseStr(sczControlState);
        ReleaseStr(sczControlName);
    }


    //
    // OnClose - called when the window is trying to be closed.
    //
    BOOL OnClose()
    {
        BOOL fClose = FALSE;
        BOOL fCancel = FALSE;

        // If we've already succeeded or failed or showing the help page, just close (prompts are annoying if the bootstrapper is done).
        if (WIXSTDBA_STATE_APPLIED <= m_state || WIXSTDBA_STATE_HELP == m_state)
        {
            fClose = TRUE;
        }
        else // prompt the user or force the cancel if there is no UI.
        {
            ::EnterCriticalSection(&m_csShowingInternalUiThisPackage);
            fClose = PromptCancel(
                m_hWnd,
                BOOTSTRAPPER_DISPLAY_FULL != m_commandDisplay || m_fShowingInternalUiThisPackage,
                m_sczConfirmCloseMessage ? m_sczConfirmCloseMessage : L"Are you sure you want to cancel?",
                m_pTheme->sczCaption);
            ::LeaveCriticalSection(&m_csShowingInternalUiThisPackage);

            fCancel = fClose;
        }

        // If we're doing progress then we never close, we just cancel to let rollback occur.
        if (WIXSTDBA_STATE_APPLYING <= m_state && WIXSTDBA_STATE_APPLIED > m_state)
        {
            // If we canceled, disable cancel button since clicking it again is silly.
            if (fClose)
            {
                ThemeControlEnable(m_pControlProgressCancelButton, FALSE);
            }

            fClose = FALSE;
        }

        if (fClose)
        {
            DWORD dwCurrentPageId = 0;
            DeterminePageId(m_state, &dwCurrentPageId);

            // Hide the current page to let thmutil do its thing with variables.
            ThemeShowPageEx(m_pTheme, dwCurrentPageId, SW_HIDE, fCancel ? THEME_SHOW_PAGE_REASON_CANCEL : THEME_SHOW_PAGE_REASON_DEFAULT);
        }

        return fClose;
    }


    //
    // OnClickAcceptCheckbox - allow the install to continue.
    //
    void OnClickAcceptCheckbox()
    {
        BOOL fAcceptedLicense = ThemeIsControlChecked(m_pControlEulaAcceptCheckbox);
        ThemeControlEnable(m_pControlInstallButton, fAcceptedLicense);
    }


    //
    // OnClickInstallButton - start the install by planning the packages.
    //
    void OnClickInstallButton()
    {
        this->OnPlan(BOOTSTRAPPER_ACTION_INSTALL);
    }


    //
    // OnClickRepairButton - start the repair.
    //
    void OnClickRepairButton()
    {
        this->OnPlan(BOOTSTRAPPER_ACTION_REPAIR);
    }


    //
    // OnClickUninstallButton - start the uninstall.
    //
    void OnClickUninstallButton()
    {
        this->OnPlan(BOOTSTRAPPER_ACTION_UNINSTALL);
    }


    //
    // OnClickUpdateButton - start the update process.
    //
    void OnClickUpdateButton()
    {
        this->OnPlan(BOOTSTRAPPER_ACTION_UPDATE_REPLACE);
    }


    //
    // OnClickCloseButton - close the application.
    //
    void OnClickCloseButton()
    {
        ::SendMessageW(m_hWnd, WM_CLOSE, 0, 0);
    }


    //
    // OnClickEulaLink - show the end user license agreement.
    //
    void OnClickEulaLink()
    {
        HRESULT hr = S_OK;
        LPWSTR sczLicenseUrl = NULL;
        LPWSTR sczLicensePath = NULL;
        LPWSTR sczLicenseDirectory = NULL;
        LPWSTR sczLicenseFilename = NULL;
        URI_PROTOCOL protocol = URI_PROTOCOL_UNKNOWN;

        hr = StrAllocString(&sczLicenseUrl, m_sczLicenseUrl, 0);
        BalExitOnFailure(hr, "Failed to copy license URL: %ls", m_sczLicenseUrl);

        hr = LocLocalizeString(m_pWixLoc, &sczLicenseUrl);
        BalExitOnFailure(hr, "Failed to localize license URL: %ls", m_sczLicenseUrl);

        // Assume there is no hidden variables to be formatted
        // so don't worry about securely freeing it.
        hr = BalFormatString(sczLicenseUrl, &sczLicenseUrl);
        BalExitOnFailure(hr, "Failed to get formatted license URL: %ls", m_sczLicenseUrl);

        hr = UriProtocol(sczLicenseUrl, &protocol);
        if (FAILED(hr) || URI_PROTOCOL_UNKNOWN == protocol)
        {
            // Probe for localized license file
            hr = PathRelativeToModule(&sczLicensePath, sczLicenseUrl, m_hModule);
            if (SUCCEEDED(hr))
            {
                hr = PathGetDirectory(sczLicensePath, &sczLicenseDirectory);
                if (SUCCEEDED(hr))
                {
                    hr = LocProbeForFile(sczLicenseDirectory, PathFile(sczLicenseUrl), m_sczLanguage, &sczLicensePath);
                }
            }
        }

        hr = ShelExecUnelevated(sczLicensePath ? sczLicensePath : sczLicenseUrl, NULL, L"open", NULL, SW_SHOWDEFAULT);
        BalExitOnFailure(hr, "Failed to launch URL to EULA.");

    LExit:
        ReleaseStr(sczLicensePath);
        ReleaseStr(sczLicenseUrl);
        ReleaseStr(sczLicenseDirectory);
        ReleaseStr(sczLicenseFilename);
    }


    //
    // OnClickLaunchButton - launch the app from the success page.
    //
    void OnClickLaunchButton()
    {
        HRESULT hr = S_OK;
        LPWSTR sczUnformattedLaunchTarget = NULL;
        LPWSTR sczLaunchTarget = NULL;
        LPWSTR sczLaunchTargetElevatedId = NULL;
        LPWSTR sczUnformattedArguments = NULL;
        LPWSTR sczArguments = NULL;
        LPWSTR sczUnformattedLaunchFolder = NULL;
        LPWSTR sczLaunchFolder = NULL;
        int nCmdShow = SW_SHOWNORMAL;

        hr = BalGetStringVariable(WIXSTDBA_VARIABLE_LAUNCH_TARGET_PATH, &sczUnformattedLaunchTarget);
        BalExitOnFailure(hr, "Failed to get launch target variable '%ls'.", WIXSTDBA_VARIABLE_LAUNCH_TARGET_PATH);

        hr = BalFormatString(sczUnformattedLaunchTarget, &sczLaunchTarget);
        BalExitOnFailure(hr, "Failed to format launch target variable: %ls", sczUnformattedLaunchTarget);

        if (BalVariableExists(WIXSTDBA_VARIABLE_LAUNCH_TARGET_ELEVATED_ID))
        {
            hr = BalGetStringVariable(WIXSTDBA_VARIABLE_LAUNCH_TARGET_ELEVATED_ID, &sczLaunchTargetElevatedId);
            BalExitOnFailure(hr, "Failed to get launch target elevated id '%ls'.", WIXSTDBA_VARIABLE_LAUNCH_TARGET_ELEVATED_ID);
        }

        if (BalVariableExists(WIXSTDBA_VARIABLE_LAUNCH_ARGUMENTS))
        {
            hr = BalGetStringVariable(WIXSTDBA_VARIABLE_LAUNCH_ARGUMENTS, &sczUnformattedArguments);
            BalExitOnFailure(hr, "Failed to get launch arguments '%ls'.", WIXSTDBA_VARIABLE_LAUNCH_ARGUMENTS);
        }

        if (BalVariableExists(WIXSTDBA_VARIABLE_LAUNCH_HIDDEN))
        {
            nCmdShow = SW_HIDE;
        }

        if (BalVariableExists(WIXSTDBA_VARIABLE_LAUNCH_WORK_FOLDER))
        {
            hr = BalGetStringVariable(WIXSTDBA_VARIABLE_LAUNCH_WORK_FOLDER, &sczUnformattedLaunchFolder);
            BalExitOnFailure(hr, "Failed to get launch working directory variable '%ls'.", WIXSTDBA_VARIABLE_LAUNCH_WORK_FOLDER);
        }

        if (sczLaunchTargetElevatedId && !m_fTriedToLaunchElevated)
        {
            m_fTriedToLaunchElevated = TRUE;
            hr = m_pEngine->LaunchApprovedExe(m_hWnd, sczLaunchTargetElevatedId, sczUnformattedArguments, 0);
            if (FAILED(hr))
            {
                BalLogError(hr, "Failed to launch elevated target: %ls", sczLaunchTargetElevatedId);

                //try with ShelExec next time
                OnClickLaunchButton();
            }
        }
        else
        {
            if (sczUnformattedArguments)
            {
                hr = BalFormatString(sczUnformattedArguments, &sczArguments);
                BalExitOnFailure(hr, "Failed to format launch arguments variable: %ls", sczUnformattedArguments);
            }

            if (sczUnformattedLaunchFolder)
            {
                hr = BalFormatString(sczUnformattedLaunchFolder, &sczLaunchFolder);
                BalExitOnFailure(hr, "Failed to format launch working directory variable: %ls", sczUnformattedLaunchFolder);
            }

            hr = ShelExec(sczLaunchTarget, sczArguments, L"open", sczLaunchFolder, nCmdShow, m_hWnd, NULL);
            BalExitOnFailure(hr, "Failed to launch target: %ls", sczLaunchTarget);

            ::PostMessageW(m_hWnd, WM_CLOSE, 0, 0);
        }

    LExit:
        StrSecureZeroFreeString(sczLaunchFolder);
        ReleaseStr(sczUnformattedLaunchFolder);
        StrSecureZeroFreeString(sczArguments);
        ReleaseStr(sczUnformattedArguments);
        ReleaseStr(sczLaunchTargetElevatedId);
        StrSecureZeroFreeString(sczLaunchTarget);
        ReleaseStr(sczUnformattedLaunchTarget);
    }


    //
    // OnClickRestartButton - allows the restart and closes the app.
    //
    void OnClickRestartButton()
    {
        AssertSz(m_fRestartRequired, "Restart must be requested to be able to click on the restart button.");

        if (m_fRestartRequiresElevation)
        {
            m_fElevatingForRestart = TRUE;
            ThemeControlEnable(m_pControlFailureRestartButton, FALSE);
            ThemeControlEnable(m_pControlSuccessRestartButton, FALSE);

            m_pEngine->Elevate(m_hWnd);
        }
        else
        {
            m_fAllowRestart = TRUE;

            ::SendMessageW(m_hWnd, WM_CLOSE, 0, 0);
        }
    }


    //
    // OnClickLogFileLink - show the log file.
    //
    void OnClickLogFileLink()
    {
        HRESULT hr = S_OK;
        LPWSTR sczLogFile = NULL;

        hr = BalGetStringVariable(m_Bundle.sczLogVariable, &sczLogFile);
        BalExitOnFailure(hr, "Failed to get log file variable '%ls'.", m_Bundle.sczLogVariable);

        hr = ShelExecUnelevated(L"notepad.exe", sczLogFile, L"open", NULL, SW_SHOWDEFAULT);
        BalExitOnFailure(hr, "Failed to open log file target: %ls", sczLogFile);

    LExit:
        ReleaseStr(sczLogFile);
    }

    BOOL OnThemeControlWmCommand(
        __in const THEME_CONTROLWMCOMMAND_ARGS* pArgs,
        __in THEME_CONTROLWMCOMMAND_RESULTS* pResults
        )
    {
        HRESULT hr = S_OK;
        BOOL fProcessed = FALSE;
        BA_FUNCTIONS_ONTHEMECONTROLWMCOMMAND_ARGS themeControlWmCommandArgs = { };
        BA_FUNCTIONS_ONTHEMECONTROLWMCOMMAND_RESULTS themeControlWmCommandResults = { };

        switch (HIWORD(pArgs->wParam))
        {
        case BN_CLICKED:
            switch (pArgs->pThemeControl->wId)
            {
            case WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX:
                OnClickAcceptCheckbox();
                fProcessed = TRUE;
                pResults->lResult = 0;
                ExitFunction();

            case WIXSTDBA_CONTROL_INSTALL_BUTTON:
                OnClickInstallButton();
                fProcessed = TRUE;
                pResults->lResult = 0;
                ExitFunction();

            case WIXSTDBA_CONTROL_REPAIR_BUTTON:
                OnClickRepairButton();
                fProcessed = TRUE;
                pResults->lResult = 0;
                ExitFunction();

            case WIXSTDBA_CONTROL_UNINSTALL_BUTTON:
                OnClickUninstallButton();
                fProcessed = TRUE;
                pResults->lResult = 0;
                ExitFunction();

            case WIXSTDBA_CONTROL_INSTALL_UPDATE_BUTTON:
            case WIXSTDBA_CONTROL_MODIFY_UPDATE_BUTTON:
                OnClickUpdateButton();
                fProcessed = TRUE;
                pResults->lResult = 0;
                ExitFunction();

            case WIXSTDBA_CONTROL_LAUNCH_BUTTON:
                OnClickLaunchButton();
                fProcessed = TRUE;
                pResults->lResult = 0;
                ExitFunction();

            case WIXSTDBA_CONTROL_SUCCESS_RESTART_BUTTON: __fallthrough;
            case WIXSTDBA_CONTROL_FAILURE_RESTART_BUTTON:
                OnClickRestartButton();
                fProcessed = TRUE;
                pResults->lResult = 0;
                ExitFunction();

            case WIXSTDBA_CONTROL_PROGRESS_CANCEL_BUTTON:
                OnClickCloseButton();
                fProcessed = TRUE;
                pResults->lResult = 0;
                ExitFunction();
            }
            break;
        }

        if (m_pfnBAFunctionsProc)
        {
            themeControlWmCommandArgs.cbSize = sizeof(themeControlWmCommandArgs);
            themeControlWmCommandArgs.wParam = pArgs->wParam;
            themeControlWmCommandArgs.wzName = pArgs->pThemeControl->sczName;
            themeControlWmCommandArgs.wId = pArgs->pThemeControl->wId;
            themeControlWmCommandArgs.hWnd = pArgs->pThemeControl->hWnd;

            themeControlWmCommandResults.cbSize = sizeof(themeControlWmCommandResults);
            themeControlWmCommandResults.lResult = pResults->lResult;

            hr = m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONTHEMECONTROLWMCOMMAND, &themeControlWmCommandArgs, &themeControlWmCommandResults, m_pvBAFunctionsProcContext);
            if (E_NOTIMPL != hr)
            {
                BalExitOnFailure(hr, "BAFunctions OnThemeControlWmCommand failed.");

                if (themeControlWmCommandResults.fProcessed)
                {
                    fProcessed = TRUE;
                    pResults->lResult = themeControlWmCommandResults.lResult;
                    ExitFunction();
                }
            }
        }

LExit:
        return fProcessed;
    }

    BOOL OnThemeControlWmNotify(
        __in const THEME_CONTROLWMNOTIFY_ARGS* pArgs,
        __in THEME_CONTROLWMNOTIFY_RESULTS* pResults
        )
    {
        HRESULT hr = S_OK;
        BOOL fProcessed = FALSE;
        BA_FUNCTIONS_ONTHEMECONTROLWMNOTIFY_ARGS themeControlWmNotifyArgs = { };
        BA_FUNCTIONS_ONTHEMECONTROLWMNOTIFY_RESULTS themeControlWmNotifyResults = { };

        switch (pArgs->lParam->code)
        {
        case NM_CLICK: __fallthrough;
        case NM_RETURN:
            switch (pArgs->pThemeControl->wId)
            {
            case WIXSTDBA_CONTROL_EULA_LINK:
                OnClickEulaLink();
                fProcessed = TRUE;
                pResults->lResult = 1;
                ExitFunction();
            case WIXSTDBA_CONTROL_FAILURE_LOGFILE_LINK:
                OnClickLogFileLink();
                fProcessed = TRUE;
                pResults->lResult = 1;
                ExitFunction();
            }
        }

        if (m_pfnBAFunctionsProc)
        {
            themeControlWmNotifyArgs.cbSize = sizeof(themeControlWmNotifyArgs);
            themeControlWmNotifyArgs.lParam = pArgs->lParam;
            themeControlWmNotifyArgs.wzName = pArgs->pThemeControl->sczName;
            themeControlWmNotifyArgs.wId = pArgs->pThemeControl->wId;
            themeControlWmNotifyArgs.hWnd = pArgs->pThemeControl->hWnd;

            themeControlWmNotifyResults.cbSize = sizeof(themeControlWmNotifyResults);
            themeControlWmNotifyResults.lResult = pResults->lResult;

            hr = m_pfnBAFunctionsProc(BA_FUNCTIONS_MESSAGE_ONTHEMECONTROLWMCOMMAND, &themeControlWmNotifyArgs, &themeControlWmNotifyResults, m_pvBAFunctionsProcContext);
            if (E_NOTIMPL != hr)
            {
                BalExitOnFailure(hr, "BAFunctions OnThemeControlWmNotify failed.");

                if (themeControlWmNotifyResults.fProcessed)
                {
                    fProcessed = TRUE;
                    pResults->lResult = themeControlWmNotifyResults.lResult;
                    ExitFunction();
                }
            }
        }

LExit:
        return fProcessed;
    }


    //
    // SetState
    //
    void SetState(
        __in WIXSTDBA_STATE state,
        __in HRESULT hrStatus
        )
    {
#ifdef DEBUG
        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: SetState() - setting state to %u with status 0x%08x.", state, hrStatus);
#endif

        if (FAILED(hrStatus))
        {
            m_hrFinal = hrStatus;
        }

        if (FAILED(m_hrFinal))
        {
            state = WIXSTDBA_STATE_FAILED;
        }

        if (m_state < state)
        {
            ::PostMessageW(m_hWnd, WM_WIXSTDBA_CHANGE_STATE, 0, state);
        }
    }


    void DeterminePageId(
        __in WIXSTDBA_STATE state,
        __out DWORD* pdwPageId
        )
    {
        if (BOOTSTRAPPER_DISPLAY_PASSIVE == m_commandDisplay)
        {
            switch (state)
            {
            case WIXSTDBA_STATE_INITIALIZED:
                *pdwPageId = BOOTSTRAPPER_ACTION_HELP == m_commandAction ? m_rgdwPageIds[WIXSTDBA_PAGE_HELP] : m_rgdwPageIds[WIXSTDBA_PAGE_LOADING];
                break;

            case WIXSTDBA_STATE_HELP:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_HELP];
                break;

            case WIXSTDBA_STATE_DETECTING:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_LOADING] ? m_rgdwPageIds[WIXSTDBA_PAGE_LOADING] : m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS_PASSIVE] ? m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS_PASSIVE] : m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS];
                break;

            case WIXSTDBA_STATE_DETECTED: __fallthrough;
            case WIXSTDBA_STATE_PLANNING_PREREQS: __fallthrough;
            case WIXSTDBA_STATE_PLANNED_PREREQS: __fallthrough;
            case WIXSTDBA_STATE_PLANNING: __fallthrough;
            case WIXSTDBA_STATE_PLANNED: __fallthrough;
            case WIXSTDBA_STATE_APPLYING: __fallthrough;
            case WIXSTDBA_STATE_CACHING: __fallthrough;
            case WIXSTDBA_STATE_CACHED: __fallthrough;
            case WIXSTDBA_STATE_EXECUTING: __fallthrough;
            case WIXSTDBA_STATE_EXECUTED:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS_PASSIVE] ? m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS_PASSIVE] : m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS];
                break;

            default:
                *pdwPageId = 0;
                break;
            }
        }
        else if (BOOTSTRAPPER_DISPLAY_FULL == m_commandDisplay)
        {
            switch (state)
            {
            case WIXSTDBA_STATE_INITIALIZING:
                *pdwPageId = 0;
                break;

            case WIXSTDBA_STATE_INITIALIZED:
                *pdwPageId = BOOTSTRAPPER_ACTION_HELP == m_commandAction ? m_rgdwPageIds[WIXSTDBA_PAGE_HELP] : m_rgdwPageIds[WIXSTDBA_PAGE_LOADING];
                break;

            case WIXSTDBA_STATE_HELP:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_HELP];
                break;

            case WIXSTDBA_STATE_DETECTING:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_LOADING];
                break;

            case WIXSTDBA_STATE_DETECTED:
            case WIXSTDBA_STATE_PLANNING_PREREQS: __fallthrough;
            case WIXSTDBA_STATE_PLANNED_PREREQS: __fallthrough;
                switch (m_commandAction)
                {
                case BOOTSTRAPPER_ACTION_INSTALL:
                    *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_INSTALL];
                    break;

                case BOOTSTRAPPER_ACTION_MODIFY: __fallthrough;
                case BOOTSTRAPPER_ACTION_REPAIR: __fallthrough;
                case BOOTSTRAPPER_ACTION_UNINSTALL:
                    *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_MODIFY];
                    break;
                }
                break;

            case WIXSTDBA_STATE_PLANNING: __fallthrough;
            case WIXSTDBA_STATE_PLANNED: __fallthrough;
            case WIXSTDBA_STATE_APPLYING: __fallthrough;
            case WIXSTDBA_STATE_CACHING: __fallthrough;
            case WIXSTDBA_STATE_CACHED: __fallthrough;
            case WIXSTDBA_STATE_EXECUTING: __fallthrough;
            case WIXSTDBA_STATE_EXECUTED:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_PROGRESS];
                break;

            case WIXSTDBA_STATE_APPLIED:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_SUCCESS];
                break;

            case WIXSTDBA_STATE_FAILED:
                *pdwPageId = m_rgdwPageIds[WIXSTDBA_PAGE_FAILURE];
                break;
            }
        }
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

    void UpdateCacheProgress(
        __in DWORD dwOverallPercentage
        )
    {
        WCHAR wzProgress[5] = { };

        ::StringCchPrintfW(wzProgress, countof(wzProgress), L"%u%%", dwOverallPercentage);
        ThemeSetTextControl(m_pControlCacheProgressText, wzProgress);

        ThemeSetProgressControl(m_pControlCacheProgressbar, dwOverallPercentage);

        m_dwCalculatedCacheProgress = dwOverallPercentage * WIXSTDBA_ACQUIRE_PERCENTAGE / 100;
        ThemeSetProgressControl(m_pControlOverallCalculatedProgressbar, m_dwCalculatedCacheProgress + m_dwCalculatedExecuteProgress);

        SetTaskbarButtonProgress(m_dwCalculatedCacheProgress + m_dwCalculatedExecuteProgress);
    }


    void SetTaskbarButtonProgress(
        __in DWORD dwOverallPercentage
        )
    {
        HRESULT hr = S_OK;

        if (m_fTaskbarButtonOK)
        {
            hr = m_pTaskbarList->SetProgressValue(m_hWnd, dwOverallPercentage, 100UL);
            BalExitOnFailure(hr, "Failed to set taskbar button progress to: %d%%.", dwOverallPercentage);
        }

    LExit:
        return;
    }


    void SetTaskbarButtonState(
        __in TBPFLAG tbpFlags
        )
    {
        HRESULT hr = S_OK;

        if (m_fTaskbarButtonOK)
        {
            hr = m_pTaskbarList->SetProgressState(m_hWnd, tbpFlags);
            BalExitOnFailure(hr, "Failed to set taskbar button state: %d.", tbpFlags);
        }

    LExit:
        return;
    }


    void SetProgressState(
        __in HRESULT hrStatus
        )
    {
        TBPFLAG flag = TBPF_NORMAL;

        if (IsCanceled() || HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) == hrStatus)
        {
            flag = TBPF_PAUSED;
        }
        else if (IsRollingBack() || FAILED(hrStatus))
        {
            flag = TBPF_ERROR;
        }

        SetTaskbarButtonState(flag);
    }


    HRESULT LoadBAFunctions(
        __in IXMLDOMDocument* pixdManifest,
        __in BOOTSTRAPPER_COMMAND* pCommand
        )
    {
        HRESULT hr = S_OK;
        IXMLDOMNode* pBAFunctionsNode = NULL;
        LPWSTR sczBafName = NULL;
        LPWSTR sczBafPath = NULL;
        BA_FUNCTIONS_CREATE_ARGS bafCreateArgs = { };
        BA_FUNCTIONS_CREATE_RESULTS bafCreateResults = { };
        BOOL fXmlFound = FALSE;

        hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixBalBAFunctions", &pBAFunctionsNode);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to read WixBalBAFunctions node from BootstrapperApplicationData.xml.");

        if (!fXmlFound)
        {
            ExitFunction();
        }

        hr = XmlGetAttributeEx(pBAFunctionsNode, L"FilePath", &sczBafName);
        BalExitOnRequiredXmlQueryFailure(hr, "Failed to get BAFunctions FilePath.");

        hr = PathRelativeToModule(&sczBafPath, sczBafName, m_hModule);
        BalExitOnFailure(hr, "Failed to get path to BAFunctions DLL.");

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "WIXSTDBA: LoadBAFunctions() - BAFunctions DLL %ls", sczBafPath);

        m_hBAFModule = ::LoadLibraryExW(sczBafPath, NULL, LOAD_WITH_ALTERED_SEARCH_PATH);
        BalExitOnNullWithLastError(m_hBAFModule, hr, "WIXSTDBA: LoadBAFunctions() - Failed to load DLL %ls", sczBafPath);

        PFN_BA_FUNCTIONS_CREATE pfnBAFunctionsCreate = reinterpret_cast<PFN_BA_FUNCTIONS_CREATE>(::GetProcAddress(m_hBAFModule, "BAFunctionsCreate"));
        BalExitOnNullWithLastError(pfnBAFunctionsCreate, hr, "Failed to get BAFunctionsCreate entry-point from: %ls", sczBafPath);

        bafCreateArgs.cbSize = sizeof(bafCreateArgs);
        bafCreateArgs.qwBAFunctionsAPIVersion = MAKEQWORDVERSION(2024, 1, 1, 0);
        bafCreateArgs.pEngine = m_pEngine;
        bafCreateArgs.pCommand = pCommand;

        bafCreateResults.cbSize = sizeof(bafCreateResults);

        hr = pfnBAFunctionsCreate(&bafCreateArgs, &bafCreateResults);
        BalExitOnFailure(hr, "Failed to create BAFunctions.");

        m_pfnBAFunctionsProc = bafCreateResults.pfnBAFunctionsProc;
        m_pvBAFunctionsProcContext = bafCreateResults.pvBAFunctionsProcContext;

    LExit:
        if (m_hBAFModule && !m_pfnBAFunctionsProc)
        {
            ::FreeLibrary(m_hBAFModule);
            m_hBAFModule = NULL;
        }
        ReleaseStr(sczBafPath);
        ReleaseStr(sczBafName);
        ReleaseObject(pBAFunctionsNode);

        return hr;
    }


public:
    //
    // Constructor - initialize member variables.
    //
    CWixStandardBootstrapperApplication(
        __in HMODULE hModule,
        __in BOOL fRunAsPrereqBA
        ) : CBootstrapperApplicationBase(3, 3000)
    {
        THEME_ASSIGN_CONTROL_ID* pAssignControl = NULL;
        DWORD dwAutomaticBehaviorType = THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_ENABLED | THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_VISIBLE | THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_ACTION | THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_VALUE;

        m_hModule = hModule;
        m_commandAction = BOOTSTRAPPER_ACTION_UNKNOWN;
        m_commandDisplay = BOOTSTRAPPER_DISPLAY_UNKNOWN;
        m_commandResumeType = BOOTSTRAPPER_RESUME_TYPE_NONE;
        m_commandRelationType = BOOTSTRAPPER_RELATION_NONE;
        m_hwndSplashScreen = NULL;

        m_plannedAction = BOOTSTRAPPER_ACTION_UNKNOWN;

        m_sczAfterForcedRestartPackage = NULL;
        m_sczBundleVersion = NULL;

        m_pWixLoc = NULL;
        m_Bundle = { };
        m_Conditions = { };
        m_sczConfirmCloseMessage = NULL;
        m_sczFailedMessage = NULL;

        m_sczLanguage = NULL;
        m_pTheme = NULL;
        memset(m_rgdwPageIds, 0, sizeof(m_rgdwPageIds));
        m_hUiThread = NULL;
        m_fRegistered = FALSE;
        m_hWnd = NULL;

        m_state = WIXSTDBA_STATE_INITIALIZING;
        m_hrFinal = S_OK;

        m_restartResult = BOOTSTRAPPER_APPLY_RESTART_NONE;
        m_fRestartRequired = FALSE;
        m_fShouldRestart = FALSE;
        m_fAllowRestart = FALSE;
        m_fRestartRequiresElevation = FALSE;
        m_fElevatingForRestart = FALSE;

        m_sczLicenseFile = NULL;
        m_sczLicenseUrl = NULL;
        m_fSuppressDowngradeFailure = FALSE;
        m_fDowngrading = FALSE;
        m_fSuppressRepair = FALSE;
        m_fSupportCacheOnly = FALSE;
        m_fRequestedCacheOnly = FALSE;

        m_pTaskbarList = NULL;
        m_uTaskbarButtonCreatedMessage = UINT_MAX;
        m_fTaskbarButtonOK = FALSE;
        ::InitializeCriticalSection(&m_csShowingInternalUiThisPackage);
        m_fShowingInternalUiThisPackage = FALSE;
        m_fTriedToLaunchElevated = FALSE;

        m_fPrereq = fRunAsPrereqBA;
        m_fHandleHelp = FALSE;
        m_fPreplanPrereqs = FALSE;
        m_fPrereqPackagePlanned = FALSE;
        m_fPrereqInstalled = FALSE;
        m_fPrereqSkipped = FALSE;

        m_fShowStandardFilesInUse = FALSE;
        m_fShowRMFilesInUse = FALSE;
        m_fShowNetfxFilesInUse = FALSE;
        m_nLastMsiFilesInUseResult = IDNOACTION;
        m_nLastNetfxFilesInUseResult = IDNOACTION;
        m_pFilesInUseTitleLoc = NULL;
        m_pFilesInUseLabelLoc = NULL;
        m_pFilesInUseCloseRadioButtonLoc = NULL;
        m_pFilesInUseNetfxCloseRadioButtonLoc = NULL;
        m_pFilesInUseDontCloseRadioButtonLoc = NULL;
        m_pFilesInUseRetryButtonLoc = NULL;
        m_pFilesInUseIgnoreButtonLoc = NULL;
        m_pFilesInUseExitButtonLoc = NULL;

        m_hBAFModule = NULL;
        m_pfnBAFunctionsProc = NULL;
        m_pvBAFunctionsProcContext = NULL;

        C_ASSERT(0 == WIXSTDBA_CONTROL_INSTALL_BUTTON - WIXSTDBA_FIRST_ASSIGN_CONTROL_ID);
        pAssignControl = m_rgInitControls;

        pAssignControl->wId = WIXSTDBA_CONTROL_INSTALL_BUTTON;
        pAssignControl->wzName = L"InstallButton";
        pAssignControl->ppControl = &m_pControlInstallButton;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlInstallButton = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_EULA_RICHEDIT;
        pAssignControl->wzName = L"EulaRichedit";
        pAssignControl->ppControl = &m_pControlEulaRichedit;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlEulaRichedit = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_EULA_LINK;
        pAssignControl->wzName = L"EulaHyperlink";
        pAssignControl->ppControl = &m_pControlEulaHyperlink;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlEulaHyperlink = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_EULA_ACCEPT_CHECKBOX;
        pAssignControl->wzName = L"EulaAcceptCheckbox";
        pAssignControl->ppControl = &m_pControlEulaAcceptCheckbox;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlEulaAcceptCheckbox = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_REPAIR_BUTTON;
        pAssignControl->wzName = L"RepairButton";
        pAssignControl->ppControl = &m_pControlRepairButton;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlRepairButton = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_UNINSTALL_BUTTON;
        pAssignControl->wzName = L"UninstallButton";
        pAssignControl->ppControl = &m_pControlUninstallButton;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlUninstallButton = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_CHECKING_FOR_UPDATES_LABEL;
        pAssignControl->wzName = L"CheckingForUpdatesLabel";
        pAssignControl->ppControl = &m_pControlCheckingForUpdatesLabel;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlCheckingForUpdatesLabel = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_INSTALL_UPDATE_BUTTON;
        pAssignControl->wzName = L"InstallUpdateButton";
        pAssignControl->ppControl = &m_pControlInstallUpdateButton;
        pAssignControl->dwAutomaticBehaviorType = THEME_CONTROL_AUTOMATIC_BEHAVIOR_ALL;
        m_pControlInstallUpdateButton = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_MODIFY_UPDATE_BUTTON;
        pAssignControl->wzName = L"ModifyUpdateButton";
        pAssignControl->ppControl = &m_pControlModifyUpdateButton;
        pAssignControl->dwAutomaticBehaviorType = THEME_CONTROL_AUTOMATIC_BEHAVIOR_ALL;
        m_pControlModifyUpdateButton = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_CACHE_PROGRESS_PACKAGE_TEXT;
        pAssignControl->wzName = L"CacheProgressPackageText";
        pAssignControl->ppControl = &m_pControlCacheProgressPackageText;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlCacheProgressPackageText = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_CACHE_PROGRESS_BAR;
        pAssignControl->wzName = L"CacheProgressbar";
        pAssignControl->ppControl = &m_pControlCacheProgressbar;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlCacheProgressbar = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_CACHE_PROGRESS_TEXT;
        pAssignControl->wzName = L"CacheProgressText";
        pAssignControl->ppControl = &m_pControlCacheProgressText;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlCacheProgressText = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_EXECUTE_PROGRESS_PACKAGE_TEXT;
        pAssignControl->wzName = L"ExecuteProgressPackageText";
        pAssignControl->ppControl = &m_pControlExecuteProgressPackageText;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlExecuteProgressPackageText = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_EXECUTE_PROGRESS_BAR;
        pAssignControl->wzName = L"ExecuteProgressbar";
        pAssignControl->ppControl = &m_pControlExecuteProgressbar;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlExecuteProgressbar = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_EXECUTE_PROGRESS_TEXT;
        pAssignControl->wzName = L"ExecuteProgressText";
        pAssignControl->ppControl = &m_pControlExecuteProgressText;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlExecuteProgressText = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_EXECUTE_PROGRESS_ACTIONDATA_TEXT;
        pAssignControl->wzName = L"ExecuteProgressActionDataText";
        pAssignControl->ppControl = &m_pControlExecuteProgressActionDataText;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlExecuteProgressActionDataText = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_OVERALL_PROGRESS_PACKAGE_TEXT;
        pAssignControl->wzName = L"OverallProgressPackageText";
        pAssignControl->ppControl = &m_pControlOverallProgressPackageText;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlOverallProgressPackageText = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_OVERALL_PROGRESS_BAR;
        pAssignControl->wzName = L"OverallProgressbar";
        pAssignControl->ppControl = &m_pControlOverallProgressbar;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlOverallProgressbar = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_OVERALL_CALCULATED_PROGRESS_BAR;
        pAssignControl->wzName = L"OverallCalculatedProgressbar";
        pAssignControl->ppControl = &m_pControlOverallCalculatedProgressbar;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlOverallCalculatedProgressbar = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_OVERALL_PROGRESS_TEXT;
        pAssignControl->wzName = L"OverallProgressText";
        pAssignControl->ppControl = &m_pControlOverallProgressText;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlOverallProgressText = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_PROGRESS_CANCEL_BUTTON;
        pAssignControl->wzName = L"ProgressCancelButton";
        pAssignControl->ppControl = &m_pControlProgressCancelButton;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlProgressCancelButton = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_LAUNCH_BUTTON;
        pAssignControl->wzName = L"LaunchButton";
        pAssignControl->ppControl = &m_pControlLaunchButton;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlLaunchButton = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_SUCCESS_RESTART_BUTTON;
        pAssignControl->wzName = L"SuccessRestartButton";
        pAssignControl->ppControl = &m_pControlSuccessRestartButton;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlSuccessRestartButton = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_FAILURE_LOGFILE_LINK;
        pAssignControl->wzName = L"FailureLogFileLink";
        pAssignControl->ppControl = &m_pControlFailureLogFileLink;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlFailureLogFileLink = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_FAILURE_MESSAGE_TEXT;
        pAssignControl->wzName = L"FailureMessageText";
        pAssignControl->ppControl = &m_pControlFailureMessageText;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlFailureMessageText = NULL;
        ++pAssignControl;

        pAssignControl->wId = WIXSTDBA_CONTROL_FAILURE_RESTART_BUTTON;
        pAssignControl->wzName = L"FailureRestartButton";
        pAssignControl->ppControl = &m_pControlFailureRestartButton;
        pAssignControl->dwAutomaticBehaviorType = dwAutomaticBehaviorType;
        m_pControlFailureRestartButton = NULL;

        C_ASSERT(LAST_WIXSTDBA_CONTROL == WIXSTDBA_CONTROL_FAILURE_RESTART_BUTTON + 1);
    }


    //
    // Destructor - release member variables.
    //
    ~CWixStandardBootstrapperApplication()
    {
        AssertSz(!::IsWindow(m_hWnd), "Window should have been destroyed before destructor.");
        AssertSz(!m_pTaskbarList, "Taskbar should have been released before destructor.");
        AssertSz(!m_pTheme, "Theme should have been released before destructor.");

        ::DeleteCriticalSection(&m_csShowingInternalUiThisPackage);
        ReleaseStr(m_sczFailedMessage);
        ReleaseStr(m_sczConfirmCloseMessage);
        BalConditionsUninitialize(&m_Conditions);
        BalInfoUninitialize(&m_Bundle);
        LocFree(m_pWixLoc);

        ReleaseStr(m_sczLanguage);
        ReleaseStr(m_sczLicenseFile);
        ReleaseStr(m_sczLicenseUrl);
        ReleaseStr(m_sczBundleVersion);
        ReleaseStr(m_sczAfterForcedRestartPackage);
    }

private:
    HMODULE m_hModule;
    BOOTSTRAPPER_ACTION m_commandAction;
    BOOTSTRAPPER_DISPLAY m_commandDisplay;
    BOOTSTRAPPER_RESUME_TYPE m_commandResumeType;
    BOOTSTRAPPER_RELATION_TYPE m_commandRelationType;
    HWND m_hwndSplashScreen;

    BOOTSTRAPPER_ACTION m_plannedAction;

    LPWSTR m_sczAfterForcedRestartPackage;
    LPWSTR m_sczBundleVersion;

    WIX_LOCALIZATION* m_pWixLoc;
    BAL_INFO_BUNDLE m_Bundle;
    BAL_CONDITIONS m_Conditions;
    LPWSTR m_sczFailedMessage;
    LPWSTR m_sczConfirmCloseMessage;

    LPWSTR m_sczLanguage;
    THEME* m_pTheme;
    THEME_ASSIGN_CONTROL_ID m_rgInitControls[LAST_WIXSTDBA_CONTROL - WIXSTDBA_FIRST_ASSIGN_CONTROL_ID];
    DWORD m_rgdwPageIds[countof(vrgwzPageNames)];
    HANDLE m_hUiThread;
    BOOL m_fRegistered;
    HWND m_hWnd;

    // Welcome page
    const THEME_CONTROL* m_pControlInstallButton;
    const THEME_CONTROL* m_pControlEulaRichedit;
    const THEME_CONTROL* m_pControlEulaHyperlink;
    const THEME_CONTROL* m_pControlEulaAcceptCheckbox;

    // Modify page
    const THEME_CONTROL* m_pControlRepairButton;
    const THEME_CONTROL* m_pControlUninstallButton;

    // Update/loading pages
    const THEME_CONTROL* m_pControlCheckingForUpdatesLabel;
    const THEME_CONTROL* m_pControlInstallUpdateButton;
    const THEME_CONTROL* m_pControlModifyUpdateButton;

    // Progress page
    const THEME_CONTROL* m_pControlCacheProgressPackageText;
    const THEME_CONTROL* m_pControlCacheProgressbar;
    const THEME_CONTROL* m_pControlCacheProgressText;

    const THEME_CONTROL* m_pControlExecuteProgressPackageText;
    const THEME_CONTROL* m_pControlExecuteProgressbar;
    const THEME_CONTROL* m_pControlExecuteProgressText;
    const THEME_CONTROL* m_pControlExecuteProgressActionDataText;

    const THEME_CONTROL* m_pControlOverallProgressPackageText;
    const THEME_CONTROL* m_pControlOverallProgressbar;
    const THEME_CONTROL* m_pControlOverallCalculatedProgressbar;
    const THEME_CONTROL* m_pControlOverallProgressText;

    const THEME_CONTROL* m_pControlProgressCancelButton;

    // Success page
    const THEME_CONTROL* m_pControlLaunchButton;
    const THEME_CONTROL* m_pControlSuccessRestartButton;

    // Failure page
    const THEME_CONTROL* m_pControlFailureLogFileLink;
    const THEME_CONTROL* m_pControlFailureMessageText;
    const THEME_CONTROL* m_pControlFailureRestartButton;

    WIXSTDBA_STATE m_state;
    HRESULT m_hrFinal;

    BOOL m_fStartedExecution;
    DWORD m_dwCalculatedCacheProgress;
    DWORD m_dwCalculatedExecuteProgress;

    BOOTSTRAPPER_APPLY_RESTART m_restartResult;
    BOOL m_fRestartRequired;
    BOOL m_fShouldRestart;
    BOOL m_fAllowRestart;
    BOOL m_fRestartRequiresElevation;
    BOOL m_fElevatingForRestart;

    LPWSTR m_sczLicenseFile;
    LPWSTR m_sczLicenseUrl;
    BOOL m_fSuppressDowngradeFailure;
    BOOL m_fDowngrading;
    BOOL m_fSuppressRepair;
    BOOL m_fSupportCacheOnly;
    BOOL m_fRequestedCacheOnly;

    BOOL m_fPrereq;
    BOOL m_fPreplanPrereqs;
    BOOL m_fHandleHelp;
    BOOL m_fPrereqPackagePlanned;
    BOOL m_fPrereqInstalled;
    BOOL m_fPrereqSkipped;

    ITaskbarList3* m_pTaskbarList;
    UINT m_uTaskbarButtonCreatedMessage;
    BOOL m_fTaskbarButtonOK;
    CRITICAL_SECTION m_csShowingInternalUiThisPackage;
    BOOL m_fShowingInternalUiThisPackage;
    BOOL m_fTriedToLaunchElevated;

    BOOL m_fShowStandardFilesInUse;
    BOOL m_fShowRMFilesInUse;
    BOOL m_fShowNetfxFilesInUse;
    int m_nLastMsiFilesInUseResult;
    int m_nLastNetfxFilesInUseResult;
    LOC_STRING* m_pFilesInUseTitleLoc;
    LOC_STRING* m_pFilesInUseLabelLoc;
    LOC_STRING* m_pFilesInUseCloseRadioButtonLoc;
    LOC_STRING* m_pFilesInUseNetfxCloseRadioButtonLoc;
    LOC_STRING* m_pFilesInUseDontCloseRadioButtonLoc;
    LOC_STRING* m_pFilesInUseRetryButtonLoc;
    LOC_STRING* m_pFilesInUseIgnoreButtonLoc;
    LOC_STRING* m_pFilesInUseExitButtonLoc;

    HMODULE m_hBAFModule;
    PFN_BA_FUNCTIONS_PROC m_pfnBAFunctionsProc;
    LPVOID m_pvBAFunctionsProcContext;
};


static HRESULT DAPI EvaluateVariableConditionCallback(
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf,
    __in_opt LPVOID /*pvContext*/
    )
{
    return BalEvaluateCondition(wzCondition, pf);
}


static HRESULT DAPI FormatVariableStringCallback(
    __in_z LPCWSTR wzFormat,
    __inout LPWSTR* psczOut,
    __in_opt LPVOID /*pvContext*/
    )
{
    return BalFormatString(wzFormat, psczOut);
}


static HRESULT DAPI GetVariableNumericCallback(
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue,
    __in_opt LPVOID /*pvContext*/
    )
{
    return BalGetNumericVariable(wzVariable, pllValue);
}


static HRESULT DAPI SetVariableNumericCallback(
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue,
    __in_opt LPVOID /*pvContext*/
    )
{
    return BalSetNumericVariable(wzVariable, llValue);
}


static HRESULT DAPI GetVariableStringCallback(
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue,
    __in_opt LPVOID /*pvContext*/
    )
{
    return BalGetStringVariable(wzVariable, psczValue);
}


static HRESULT DAPI SetVariableStringCallback(
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in BOOL fFormatted,
    __in_opt LPVOID /*pvContext*/
    )
{
    return BalSetStringVariable(wzVariable, wzValue, fFormatted);
}

static LPCSTR LoggingBoolToString(
    __in BOOL f
    )
{
    if (f)
    {
        return "Yes";
    }

    return "No";
}

static LPCSTR LoggingRequestStateToString(
    __in BOOTSTRAPPER_REQUEST_STATE requestState
    )
{
    switch (requestState)
    {
    case BOOTSTRAPPER_REQUEST_STATE_NONE:
        return "None";
    case BOOTSTRAPPER_REQUEST_STATE_FORCE_ABSENT:
        return "ForceAbsent";
    case BOOTSTRAPPER_REQUEST_STATE_ABSENT:
        return "Absent";
    case BOOTSTRAPPER_REQUEST_STATE_CACHE:
        return "Cache";
    case BOOTSTRAPPER_REQUEST_STATE_PRESENT:
        return "Present";
    case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
        return "Repair";
    default:
        return "Invalid";
    }
}

static LPCSTR LoggingPlanRelationTypeToString(
    __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE type
    )
{
    switch (type)
    {
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_NONE:
        return "None";
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DOWNGRADE:
        return "Downgrade";
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_UPGRADE:
        return "Upgrade";
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_ADDON:
        return "Addon";
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_PATCH:
        return "Patch";
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_ADDON:
        return "DependentAddon";
    case BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE_DEPENDENT_PATCH:
        return "DependentPatch";
    default:
        return "Invalid";
    }
}

static LPCSTR LoggingMsiFeatureStateToString(
    __in BOOTSTRAPPER_FEATURE_STATE featureState
    )
{
    switch (featureState)
    {
    case BOOTSTRAPPER_FEATURE_STATE_UNKNOWN:
        return "Unknown";
    case BOOTSTRAPPER_FEATURE_STATE_ABSENT:
        return "Absent";
    case BOOTSTRAPPER_FEATURE_STATE_ADVERTISED:
        return "Advertised";
    case BOOTSTRAPPER_FEATURE_STATE_LOCAL:
        return "Local";
    case BOOTSTRAPPER_FEATURE_STATE_SOURCE:
        return "Source";
    default:
        return "Invalid";
    }
}

EXTERN_C HRESULT CreateWixPrerequisiteBootstrapperApplication(
    __in HINSTANCE hInstance,
    __out IBootstrapperApplication** ppApplication
    )
{
    HRESULT hr = S_OK;

    CWixStandardBootstrapperApplication* pApplication = new CWixStandardBootstrapperApplication(hInstance, TRUE);
    ExitOnNull(pApplication, hr, E_OUTOFMEMORY, "Failed to create new bootstrapper application.");

    hr = pApplication->QueryInterface(IID_PPV_ARGS(ppApplication));
    ExitOnRootFailure(hr, "Failed to query for IBootstrapperApplication.");

LExit:
    ReleaseObject(pApplication);

    return hr;
}

EXTERN_C HRESULT CreateWixStandardBootstrapperApplication(
    __in HINSTANCE hInstance,
    __out IBootstrapperApplication** ppApplication
    )
{
    HRESULT hr = S_OK;

    CWixStandardBootstrapperApplication* pApplication = new CWixStandardBootstrapperApplication(hInstance, FALSE);
    ExitOnNull(pApplication, hr, E_OUTOFMEMORY, "Failed to create new bootstrapper application.");

    hr = pApplication->QueryInterface(IID_PPV_ARGS(ppApplication));
    ExitOnRootFailure(hr, "Failed to query for IBootstrapperApplication.");

LExit:
    ReleaseObject(pApplication);

    return hr;
}
