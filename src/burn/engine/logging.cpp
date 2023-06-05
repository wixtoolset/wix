// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static DWORD vdwPackageSequence = 0;
static const DWORD LOG_OPEN_RETRY_COUNT = 3;
static const DWORD LOG_OPEN_RETRY_WAIT = 2000;
static CONST LPWSTR LOG_FAILED_EVENT_LOG_MESSAGE = L"Burn Engine Fatal Error: failed to open log file.";

// structs



// internal function declarations

static void CheckLoggingPolicy(
    __inout DWORD* pdwAttributes
    );
static HRESULT InitializeLogging(
    __in BURN_LOGGING* pLog,
    __in BURN_ENGINE_COMMAND* pInternalCommand
    );
static HRESULT GetNonSessionSpecificTempFolder(
    __deref_out_z LPWSTR* psczNonSessionTempFolder
    );


// function definitions

extern "C" HRESULT LoggingParseFromXml(
    __in BURN_LOGGING* pLog,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixnLog = NULL;
    BOOL fXmlFound = FALSE;

    // parse the log element, if present.
    hr = XmlSelectSingleNode(pixnBundle, L"Log", &pixnLog);
    ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get Log element.");

    if (fXmlFound)
    {
        hr = XmlGetAttributeEx(pixnLog, L"PathVariable", &pLog->sczPathVariable);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get Log/@PathVariable.");

        hr = XmlGetAttributeEx(pixnLog, L"Prefix", &pLog->sczPrefix);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get Log/@Prefix attribute.");

        hr = XmlGetAttributeEx(pixnLog, L"Extension", &pLog->sczExtension);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get Log/@Extension attribute.");
    }

LExit:
    ReleaseObject(pixnLog);

    return hr;
}

extern "C" HRESULT LoggingOpen(
    __in BURN_LOGGING* pLog,
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzBundleName
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczLoggingBaseFolder = NULL;
    LPWSTR sczPrefixFormatted = NULL;
    LPCWSTR wzPostfix = NULL;

    switch (pInternalCommand->mode)
    {
    case BURN_MODE_UNTRUSTED:
        wzPostfix = L".cleanroom";
        break;
    case BURN_MODE_ELEVATED:
        wzPostfix = L".elevated";
        break;
    case BURN_MODE_RUNONCE:
        wzPostfix = L".runonce";
        break;
    }

    hr = InitializeLogging(pLog, pInternalCommand);
    ExitOnFailure(hr, "Failed to initialize logging.");

    if ((pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_VERBOSE) || (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_EXTRADEBUG))
    {
        if (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_EXTRADEBUG)
        {
            LogSetLevel(REPORT_DEBUG, FALSE);
        }
        else if (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_VERBOSE)
        {
            LogSetLevel(REPORT_VERBOSE, FALSE);
        }

        // In these modes, make sure a log will be created even if the bundle wasn't configured to create one.
        if ((!pLog->sczPath || !*pLog->sczPath) && (!pLog->sczPrefix || !*pLog->sczPrefix))
        {
            hr = StrAllocString(&pLog->sczPrefix, L"Setup", 0);
            ExitOnFailure(hr, "Failed to copy default log prefix.");

            if (!pLog->sczExtension || !*pLog->sczExtension)
            {
                hr = StrAllocString(&pLog->sczExtension, L"log", 0);
                ExitOnFailure(hr, "Failed to copy default log extension.");
            }
        }
    }

    // Open the log approriately.
    if (pLog->sczPath && *pLog->sczPath)
    {
        DWORD cRetry = 0;

        // Try pretty hard to open the log file when appending.
        do
        {
            if (0 < cRetry)
            {
                ::Sleep(LOG_OPEN_RETRY_WAIT);
            }

            hr = LogOpen(NULL, pLog->sczPath, NULL, NULL, pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_APPEND, FALSE, &pLog->sczPath);
            if (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_APPEND && HRESULT_FROM_WIN32(ERROR_SHARING_VIOLATION) == hr)
            {
                ++cRetry;
            }
        } while (cRetry > 0 && cRetry <= LOG_OPEN_RETRY_COUNT);

        if (FAILED(hr))
        {
            // Log is not open, so note that.
            LogDisable();
            pLog->state = BURN_LOGGING_STATE_DISABLED;

            if (pLog->dwAttributes & BURN_LOGGING_ATTRIBUTE_APPEND)
            {
                // If appending, ignore the failure and continue.
                hr = S_OK;
            }
            else // specifically tried to create a log file so show an error if appropriate and bail.
            {
                HRESULT hrOriginal = hr;

                hr = HRESULT_FROM_WIN32(ERROR_INSTALL_LOG_FAILURE);
                SplashScreenDisplayError(pCommand->display, wzBundleName, hr);

                ExitOnFailure(hrOriginal, "Failed to open log: %ls", pLog->sczPath);
            }
        }
        else
        {
            pLog->state = BURN_LOGGING_STATE_OPEN;
        }
    }
    else
    {
        if (pLog->sczPrefix && *pLog->sczPrefix)
        {
            hr = VariableFormatString(pVariables, pLog->sczPrefix, &sczPrefixFormatted, NULL);
        }

        if (sczPrefixFormatted && *sczPrefixFormatted)
        {
            // Best effort to open default logging.
            LPCWSTR wzPrefix = sczPrefixFormatted;
            LPCWSTR wzPastRoot = PathSkipPastRoot(sczPrefixFormatted, NULL, NULL, NULL);

            // If the log path is rooted and has a file component, then use that path as is.
            if (wzPastRoot && *wzPastRoot)
            {
                hr = PathGetDirectory(sczPrefixFormatted, &sczLoggingBaseFolder);
                ExitOnFailure(hr, "Failed to get parent directory from '%ls'.", sczPrefixFormatted);

                wzPrefix = PathFile(sczPrefixFormatted);
            }
            else
            {
                hr = GetNonSessionSpecificTempFolder(&sczLoggingBaseFolder);
                ExitOnFailure(hr, "Failed to get non-session specific TEMP folder.");
            }

            hr = LogOpen(sczLoggingBaseFolder, wzPrefix, wzPostfix, pLog->sczExtension, FALSE, FALSE, &pLog->sczPath);
            if (FAILED(hr))
            {
                LogDisable();
                pLog->state = BURN_LOGGING_STATE_DISABLED;

                hr = S_OK;
            }
            else
            {
                pLog->state = BURN_LOGGING_STATE_OPEN;
            }
        }
        else // no logging enabled.
        {
            LogDisable();
            pLog->state = BURN_LOGGING_STATE_DISABLED;
        }
    }

    // If the log was opened, write the header info and update the prefix and extension to match
    // the log name so future logs are opened with the same pattern.
    if (BURN_LOGGING_STATE_OPEN == pLog->state)
    {
        LPCWSTR wzExtension = PathExtension(pLog->sczPath);
        if (wzExtension && *wzExtension)
        {
            hr = StrAllocString(&pLog->sczPrefix, pLog->sczPath, wzExtension - pLog->sczPath);
            ExitOnFailure(hr, "Failed to copy log path to prefix.");

            hr = StrAllocString(&pLog->sczExtension, wzExtension + 1, 0);
            ExitOnFailure(hr, "Failed to copy log extension to extension.");
        }
        else
        {
            hr = StrAllocString(&pLog->sczPrefix, pLog->sczPath, 0);
            ExitOnFailure(hr, "Failed to copy full log path to prefix.");
        }

        if (pLog->sczPathVariable && *pLog->sczPathVariable)
        {
            VariableSetString(pVariables, pLog->sczPathVariable, pLog->sczPath, FALSE, FALSE); // Ignore failure.
        }
    }

LExit:
    ReleaseStr(sczLoggingBaseFolder);
    StrSecureZeroFreeString(sczPrefixFormatted);

    return hr;
}

extern "C" void LoggingOpenFailed()
{
    HRESULT hr = S_OK;
    HANDLE hEventLog = NULL;
    LPCWSTR* lpStrings = const_cast<LPCWSTR*>(&LOG_FAILED_EVENT_LOG_MESSAGE);
    WORD wNumStrings = 1;

    hr = LogOpen(NULL, L"Setup", L"_Failed", L"log", FALSE, FALSE, NULL);
    if (SUCCEEDED(hr))
    {
        ExitFunction();
    }

    // If opening the "failure" log failed, then attempt to record that in the Application event log.
    hEventLog = ::OpenEventLogW(NULL, L"Application");
    ExitOnNullWithLastError(hEventLog, hr, "Failed to open Application event log");

    hr = ::ReportEventW(hEventLog, EVENTLOG_ERROR_TYPE, 1, 1, NULL, wNumStrings, 0, lpStrings, NULL);
    ExitOnNullWithLastError(hEventLog, hr, "Failed to write event log entry");

LExit:
    if (hEventLog)
    {
        ::CloseEventLog(hEventLog);
    }
}

extern "C" void LoggingIncrementPackageSequence()
{
    ++vdwPackageSequence;
}

extern "C" HRESULT LoggingSetCompatiblePackageVariable(
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __out_opt LPWSTR* psczLogPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczLogPath = NULL;

    // Make sure that no package log files are created when logging has been disabled via Log element.
    if (BURN_LOGGING_STATE_DISABLED == pLog->state)
    {
        ExitFunction();
    }

    if (pPackage->sczCompatibleLogPathVariable && *pPackage->sczCompatibleLogPathVariable)
    {
        hr = StrAllocFormatted(&sczLogPath, L"%ls_%03u_%ls_%ls.%ls", pLog->sczPrefix, vdwPackageSequence, pPackage->sczId, pPackage->compatiblePackage.compatibleEntry.sczId, pLog->sczExtension);
        ExitOnFailure(hr, "Failed to allocate path for package log.");

        hr = VariableSetString(pVariables, pPackage->sczCompatibleLogPathVariable, sczLogPath, FALSE, FALSE);
        ExitOnFailure(hr, "Failed to set log path into variable.");

        if (psczLogPath)
        {
            hr = StrAllocString(psczLogPath, sczLogPath, 0);
            ExitOnFailure(hr, "Failed to copy package log path.");
        }
    }

LExit:
    ReleaseStr(sczLogPath);

    return hr;
}

extern "C" HRESULT LoggingSetPackageVariable(
    __in BURN_PACKAGE* pPackage,
    __in_z_opt LPCWSTR wzSuffix,
    __in BOOL fRollback,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __out_opt LPWSTR* psczLogPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczLogPath = NULL;

    // Make sure that no package log files are created when logging has been disabled via Log element.
    if (BURN_LOGGING_STATE_DISABLED == pLog->state)
    {
        ExitFunction();
    }

    // For burn packages we'll add logging even it it wasn't explictly specified
    if (BURN_PACKAGE_TYPE_BUNDLE == pPackage->type || (BURN_PACKAGE_TYPE_EXE == pPackage->type && BURN_EXE_PROTOCOL_TYPE_BURN == pPackage->Exe.protocol))
    {
        if (!fRollback && (!pPackage->sczLogPathVariable || !*pPackage->sczLogPathVariable))
        {
            StrAllocFormatted(&pPackage->sczLogPathVariable, L"WixBundleLog_%ls", pPackage->sczId);
        }
        else if (fRollback && (!pPackage->sczRollbackLogPathVariable || !*pPackage->sczRollbackLogPathVariable))
        {
            StrAllocFormatted(&pPackage->sczRollbackLogPathVariable, L"WixBundleRollbackLog_%ls", pPackage->sczId);
        }
    }

    if ((!fRollback && pPackage->sczLogPathVariable && *pPackage->sczLogPathVariable) ||
        (fRollback && pPackage->sczRollbackLogPathVariable && *pPackage->sczRollbackLogPathVariable))
    {
        hr = StrAllocFormatted(&sczLogPath, L"%ls%hs%ls_%03u_%ls%ls.%ls", pLog->sczPrefix, wzSuffix && *wzSuffix ? "_" : "", wzSuffix && *wzSuffix ? wzSuffix : L"", vdwPackageSequence, pPackage->sczId, fRollback ? L"_rollback" : L"", pLog->sczExtension);
        ExitOnFailure(hr, "Failed to allocate path for package log.");

        hr = VariableSetString(pVariables, fRollback ? pPackage->sczRollbackLogPathVariable : pPackage->sczLogPathVariable, sczLogPath, FALSE, FALSE);
        ExitOnFailure(hr, "Failed to set log path into variable.");

        if (psczLogPath)
        {
            hr = StrAllocString(psczLogPath, sczLogPath, 0);
            ExitOnFailure(hr, "Failed to copy package log path.");
        }
    }

LExit:
    ReleaseStr(sczLogPath);

    return hr;
}

extern "C" HRESULT LoggingSetTransactionVariable(
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in_z_opt LPCWSTR wzSuffix,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;

    // Make sure that no log files are created when logging has been disabled via Log element.
    if (BURN_LOGGING_STATE_DISABLED == pLog->state)
    {
        ExitFunction();
    }

    if (pRollbackBoundary && pRollbackBoundary->sczLogPathVariable && *pRollbackBoundary->sczLogPathVariable)
    {
        hr = StrAllocFormatted(&pRollbackBoundary->sczLogPath, L"%ls%hs%ls_%03u_%ls.%ls", pLog->sczPrefix, wzSuffix && *wzSuffix ? "_" : "", wzSuffix && *wzSuffix ? wzSuffix : L"", vdwPackageSequence, pRollbackBoundary->sczId, pLog->sczExtension);
        ExitOnFailure(hr, "Failed to allocate path for transaction log.");

        hr = VariableSetString(pVariables, pRollbackBoundary->sczLogPathVariable, pRollbackBoundary->sczLogPath, FALSE, FALSE);
        ExitOnFailure(hr, "Failed to set log path into variable.");
    }

LExit:
    ++vdwPackageSequence;

    return hr;
}

extern "C" LPCSTR LoggingBurnActionToString(
    __in BOOTSTRAPPER_ACTION action
    )
{
    switch (action)
    {
    case BOOTSTRAPPER_ACTION_UNKNOWN:
        return "Unknown";
    case BOOTSTRAPPER_ACTION_HELP:
        return "Help";
    case BOOTSTRAPPER_ACTION_LAYOUT:
        return "Layout";
    case BOOTSTRAPPER_ACTION_CACHE:
        return "Cache";
    case BOOTSTRAPPER_ACTION_UNSAFE_UNINSTALL:
        return "UnsafeUninstall";
    case BOOTSTRAPPER_ACTION_UNINSTALL:
        return "Uninstall";
    case BOOTSTRAPPER_ACTION_INSTALL:
        return "Install";
    case BOOTSTRAPPER_ACTION_MODIFY:
        return "Modify";
    case BOOTSTRAPPER_ACTION_REPAIR:
        return "Repair";
    case BOOTSTRAPPER_ACTION_UPDATE_REPLACE:
        return "UpdateReplace";
    case BOOTSTRAPPER_ACTION_UPDATE_REPLACE_EMBEDDED:
        return "UpdateReplaceEmbedded";
    default:
        return "Invalid";
    }
}

LPCSTR LoggingBurnMessageToString(
    __in UINT message
    )
{
    switch (message)
    {
    case WM_BURN_APPLY:
        return "Apply";
    case WM_BURN_DETECT:
        return "Detect";
    case WM_BURN_ELEVATE:
        return "Elevate";
    case WM_BURN_LAUNCH_APPROVED_EXE:
        return "LaunchApprovedExe";
    case WM_BURN_PLAN:
        return "Plan";
    case WM_BURN_QUIT:
        return "Quit";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingActionStateToString(
    __in BOOTSTRAPPER_ACTION_STATE actionState
    )
{
    switch (actionState)
    {
    case BOOTSTRAPPER_ACTION_STATE_NONE:
        return "None";
    case BOOTSTRAPPER_ACTION_STATE_UNINSTALL:
        return "Uninstall";
    case BOOTSTRAPPER_ACTION_STATE_INSTALL:
        return "Install";
    case BOOTSTRAPPER_ACTION_STATE_MODIFY:
        return "Modify";
    case BOOTSTRAPPER_ACTION_STATE_REPAIR:
        return "Repair";
    case BOOTSTRAPPER_ACTION_STATE_MINOR_UPGRADE:
        return "MinorUpgrade";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingCacheTypeToString(
    BOOTSTRAPPER_CACHE_TYPE cacheType
    )
{
    switch (cacheType)
    {
    case BOOTSTRAPPER_CACHE_TYPE_FORCE:
        return "Force";
    case BOOTSTRAPPER_CACHE_TYPE_KEEP:
        return "Keep";
    case BOOTSTRAPPER_CACHE_TYPE_REMOVE:
        return "Remove";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingCachePackageTypeToString(
    BURN_CACHE_PACKAGE_TYPE cachePackageType
    )
{
    switch (cachePackageType)
    {
    case BURN_CACHE_PACKAGE_TYPE_NONE:
        return "None";
    case BURN_CACHE_PACKAGE_TYPE_OPTIONAL:
        return "Optional";
    case BURN_CACHE_PACKAGE_TYPE_REQUIRED:
        return "Required";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingDependencyActionToString(
    BURN_DEPENDENCY_ACTION action
    )
{
    switch (action)
    {
    case BURN_DEPENDENCY_ACTION_NONE:
        return "None";
    case BURN_DEPENDENCY_ACTION_REGISTER:
        return "Register";
    case BURN_DEPENDENCY_ACTION_UNREGISTER:
        return "Unregister";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingBoolToString(
    __in BOOL f
    )
{
    if (f)
    {
        return "Yes";
    }

    return "No";
}

extern "C" LPCSTR LoggingTrueFalseToString(
    __in BOOL f
    )
{
    if (f)
    {
        return "true";
    }

    return "false";
}

extern "C" LPCSTR LoggingExitCodeTypeToString(
    __in BURN_EXE_EXIT_CODE_TYPE exitCodeType
    )
{
    switch (exitCodeType)
    {
    case BURN_EXE_EXIT_CODE_TYPE_SUCCESS:
        return "Success";
    case BURN_EXE_EXIT_CODE_TYPE_ERROR:
        return "Error";
    case BURN_EXE_EXIT_CODE_TYPE_SCHEDULE_REBOOT:
        return "ScheduleReboot";
    case BURN_EXE_EXIT_CODE_TYPE_FORCE_REBOOT:
        return "ForceReboot";
    case BURN_EXE_EXIT_CODE_TYPE_ERROR_SCHEDULE_REBOOT:
        return "ErrorScheduleReboot";
    case BURN_EXE_EXIT_CODE_TYPE_ERROR_FORCE_REBOOT:
        return "ErrorForceReboot";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingPackageStateToString(
    __in BOOTSTRAPPER_PACKAGE_STATE packageState
    )
{
    switch (packageState)
    {
    case BOOTSTRAPPER_PACKAGE_STATE_UNKNOWN:
        return "Unknown";
    case BOOTSTRAPPER_PACKAGE_STATE_OBSOLETE:
        return "Obsolete";
    case BOOTSTRAPPER_PACKAGE_STATE_ABSENT:
        return "Absent";
    case BOOTSTRAPPER_PACKAGE_STATE_PRESENT:
        return "Present";
    case BOOTSTRAPPER_PACKAGE_STATE_SUPERSEDED:
        return "Superseded";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingPackageRegistrationStateToString(
    __in BOOL fCanAffectRegistration,
    __in BURN_PACKAGE_REGISTRATION_STATE registrationState
    )
{
    if (!fCanAffectRegistration)
    {
        return "(permanent)";
    }

    switch (registrationState)
    {
    case BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN:
        return "Unknown";
    case BURN_PACKAGE_REGISTRATION_STATE_IGNORED:
        return "Ignored";
    case BURN_PACKAGE_REGISTRATION_STATE_ABSENT:
        return "Absent";
    case BURN_PACKAGE_REGISTRATION_STATE_PRESENT:
        return "Present";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingMsiFileVersioningToString(
    __in BOOTSTRAPPER_MSI_FILE_VERSIONING fileVersioning
    )
{
    switch (fileVersioning)
    {
    case BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER:
        return "o";
    case BOOTSTRAPPER_MSI_FILE_VERSIONING_MISSING_OR_OLDER_OR_EQUAL:
        return "e";
    case BOOTSTRAPPER_MSI_FILE_VERSIONING_ALL:
        return "a";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingMsiFeatureStateToString(
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

extern "C" LPCSTR LoggingMsiFeatureActionToString(
    __in BOOTSTRAPPER_FEATURE_ACTION featureAction
    )
{
    switch (featureAction)
    {
    case BOOTSTRAPPER_FEATURE_ACTION_NONE:
        return "None";
    case BOOTSTRAPPER_FEATURE_ACTION_ADDLOCAL:
        return "AddLocal";
    case BOOTSTRAPPER_FEATURE_ACTION_ADDSOURCE:
        return "AddSource";
    case BOOTSTRAPPER_FEATURE_ACTION_ADDDEFAULT:
        return "AddDefault";
    case BOOTSTRAPPER_FEATURE_ACTION_REINSTALL:
        return "Reinstall";
    case BOOTSTRAPPER_FEATURE_ACTION_ADVERTISE:
        return "Advertise";
    case BOOTSTRAPPER_FEATURE_ACTION_REMOVE:
        return "Remove";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingMsiInstallContext(
    __in MSIINSTALLCONTEXT context
    )
{
    switch (context)
    {
    case MSIINSTALLCONTEXT_ALL:
        return "All";
    case MSIINSTALLCONTEXT_ALLUSERMANAGED:
        return "AllUserManaged";
    case MSIINSTALLCONTEXT_MACHINE:
        return "Machine";
    case MSIINSTALLCONTEXT_NONE:
        return "None";
    case MSIINSTALLCONTEXT_USERMANAGED:
        return "UserManaged";
    case MSIINSTALLCONTEXT_USERUNMANAGED:
        return "UserUnmanaged";
    default:
        return "Invalid";
    }
}

extern "C" LPCWSTR LoggingBurnMsiPropertyToString(
    __in BURN_MSI_PROPERTY burnMsiProperty
    )
{
    switch (burnMsiProperty)
    {
    case BURN_MSI_PROPERTY_INSTALL:
        return BURNMSIINSTALL_PROPERTY_NAME;
    case BURN_MSI_PROPERTY_MODIFY:
        return BURNMSIMODIFY_PROPERTY_NAME;
    case BURN_MSI_PROPERTY_NONE:
        return L"(none)";
    case BURN_MSI_PROPERTY_REPAIR:
        return BURNMSIREPAIR_PROPERTY_NAME;
    case BURN_MSI_PROPERTY_UNINSTALL:
        return BURNMSIUNINSTALL_PROPERTY_NAME;
    default:
        return L"Invalid";
    }
}

extern "C" LPCSTR LoggingMspTargetActionToString(
    __in BOOTSTRAPPER_ACTION_STATE action,
    __in BURN_PATCH_SKIP_STATE skipState
    )
{
    switch (skipState)
    {
    case BURN_PATCH_SKIP_STATE_NONE:
        return LoggingActionStateToString(action);
    case BURN_PATCH_SKIP_STATE_TARGET_UNINSTALL:
        return "Skipped (target uninstall)";
    case BURN_PATCH_SKIP_STATE_SLIPSTREAM:
        return "Skipped (slipstream)";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingPerMachineToString(
    __in BOOL fPerMachine
    )
{
    if (fPerMachine)
    {
        return "PerMachine";
    }

    return "PerUser";
}

extern "C" LPCSTR LoggingPlannedCacheToString(
    __in const BURN_PACKAGE* pPackage
    )
{
    if (!pPackage->hCacheEvent)
    {
        return "No";
    }

    return pPackage->fCacheVital ? "Vital" : "NonVital";
}

extern "C" LPCSTR LoggingRegistrationTypeToString(
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType
    )
{
    switch (registrationType)
    {
    case BOOTSTRAPPER_REGISTRATION_TYPE_NONE:
        return "None";
    case BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS:
        return "InProgress";
    case BOOTSTRAPPER_REGISTRATION_TYPE_FULL:
        return "Full";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingRestartToString(
    __in BOOTSTRAPPER_APPLY_RESTART restart
    )
{
    switch (restart)
    {
    case BOOTSTRAPPER_APPLY_RESTART_NONE:
        return "None";
    case BOOTSTRAPPER_APPLY_RESTART_REQUIRED:
        return "Required";
    case BOOTSTRAPPER_APPLY_RESTART_INITIATED:
        return "Initiated";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingResumeModeToString(
    __in BURN_RESUME_MODE resumeMode
    )
{
    switch (resumeMode)
    {
    case BURN_RESUME_MODE_NONE:
        return "None";
    case BURN_RESUME_MODE_ACTIVE:
        return "Active";
    case BURN_RESUME_MODE_SUSPEND:
        return "Suspend";
    case BURN_RESUME_MODE_ARP:
        return "ARP";
    case BURN_RESUME_MODE_REBOOT_PENDING:
        return "Reboot Pending";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingPlanRelationTypeToString(
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

extern "C" LPCSTR LoggingRegistrationOptionsToString(
    __in DWORD dwRegistrationOptions
    )
{
    switch (dwRegistrationOptions)
    {
    case BURN_REGISTRATION_ACTION_OPERATIONS_NONE:
        return "None";
    case BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE:
        return "CacheBundle";
    case BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY:
        return "WriteProviderKey";
    case BURN_REGISTRATION_ACTION_OPERATIONS_ARP_SYSTEM_COMPONENT:
        return "ArpSystemComponent";
    case BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE + BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY:
        return "CacheBundle, WriteProviderKey";
    case BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE + BURN_REGISTRATION_ACTION_OPERATIONS_ARP_SYSTEM_COMPONENT:
        return "CacheBundle, ArpSystemComponent";
    case BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY + BURN_REGISTRATION_ACTION_OPERATIONS_ARP_SYSTEM_COMPONENT:
        return "WriteProviderKey, ArpSystemComponent";
    case BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE + BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY + BURN_REGISTRATION_ACTION_OPERATIONS_ARP_SYSTEM_COMPONENT:
        return "CacheBundle, WriteProviderKey, ArpSystemComponent";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingRelationTypeToString(
    __in BOOTSTRAPPER_RELATION_TYPE type
    )
{
    switch (type)
    {
    case BOOTSTRAPPER_RELATION_NONE:
        return "None";
    case BOOTSTRAPPER_RELATION_DETECT:
        return "Detect";
    case BOOTSTRAPPER_RELATION_UPGRADE:
        return "Upgrade";
    case BOOTSTRAPPER_RELATION_ADDON:
        return "Addon";
    case BOOTSTRAPPER_RELATION_PATCH:
        return "Patch";
    case BOOTSTRAPPER_RELATION_DEPENDENT_ADDON:
        return "DependentAddon";
    case BOOTSTRAPPER_RELATION_DEPENDENT_PATCH:
        return "DependentPatch";
    case BOOTSTRAPPER_RELATION_UPDATE:
        return "Update";
    case BOOTSTRAPPER_RELATION_CHAIN_PACKAGE:
        return "ChainPackage";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingRelatedOperationToString(
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    )
{
    switch (operation)
    {
    case BOOTSTRAPPER_RELATED_OPERATION_NONE:
        return "None";
    case BOOTSTRAPPER_RELATED_OPERATION_DOWNGRADE:
        return "Downgrade";
    case BOOTSTRAPPER_RELATED_OPERATION_MINOR_UPDATE:
        return "MinorUpdate";
    case BOOTSTRAPPER_RELATED_OPERATION_MAJOR_UPGRADE:
        return "MajorUpgrade";
    case BOOTSTRAPPER_RELATED_OPERATION_REMOVE:
        return "Remove";
    case BOOTSTRAPPER_RELATED_OPERATION_INSTALL:
        return "Install";
    case BOOTSTRAPPER_RELATED_OPERATION_REPAIR:
        return "Repair";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingRequestStateToString(
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
    case BOOTSTRAPPER_REQUEST_STATE_FORCE_PRESENT:
        return "ForcePresent";
    case BOOTSTRAPPER_REQUEST_STATE_REPAIR:
        return "Repair";
    default:
        return "Invalid";
    }
}

extern "C" LPCSTR LoggingRollbackOrExecute(
    __in BOOL fRollback
    )
{
    return fRollback ? "rollback" : "execute";
}

extern "C" LPWSTR LoggingStringOrUnknownIfNull(
    __in LPCWSTR wz
    )
{
    return wz ? wz : L"Unknown";
}


// internal function declarations

static void CheckLoggingPolicy(
    __inout DWORD *pdwAttributes
    )
{
    HRESULT hr = S_OK;
    HKEY hk = NULL;
    LPWSTR sczLoggingPolicy = NULL;

    hr = RegOpen(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Policies\\Microsoft\\Windows\\Installer", KEY_READ, &hk);
    if (SUCCEEDED(hr))
    {
        hr = RegReadString(hk, L"Logging", &sczLoggingPolicy);
        if (SUCCEEDED(hr))
        {
            LPCWSTR wz = sczLoggingPolicy;
            while (*wz)
            {
                if (L'v' == *wz || L'V' == *wz)
                {
                    *pdwAttributes |= BURN_LOGGING_ATTRIBUTE_VERBOSE;
                }
                else if (L'x' == *wz || L'X' == *wz)
                {
                    *pdwAttributes |= BURN_LOGGING_ATTRIBUTE_EXTRADEBUG;
                }

                ++wz;
            }
        }
    }

    ReleaseStr(sczLoggingPolicy);
    ReleaseRegKey(hk);
}

static HRESULT InitializeLogging(
    __in BURN_LOGGING* pLog,
    __in BURN_ENGINE_COMMAND* pInternalCommand
    )
{
    HRESULT hr = S_OK;

    // Check if the logging policy is set and configure the logging appropriately.
    CheckLoggingPolicy(&pLog->dwAttributes);

    pLog->dwAttributes |= pInternalCommand->dwLoggingAttributes;

    // The untrusted process needs a separate log file.
    // TODO: Burn crashes if they do try to use the same log file.
    if (pInternalCommand->sczLogFile && BURN_MODE_UNTRUSTED != pInternalCommand->mode)
    {
        hr = StrAllocString(&pLog->sczPath, pInternalCommand->sczLogFile, 0);
        ExitOnFailure(hr, "Failed to copy log file path from command line.");
    }

LExit:
    return hr;
}

static HRESULT GetNonSessionSpecificTempFolder(
    __deref_out_z LPWSTR* psczNonSessionTempFolder
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczTempFolder = NULL;
    SIZE_T cchTempFolder = 0;
    DWORD dwSessionId = 0;
    LPWSTR sczSessionId = 0;
    SIZE_T cchSessionId = 0;

    hr = PathGetTempPath(&sczTempFolder, &cchTempFolder);
    ExitOnFailure(hr, "Failed to get temp folder.");

    // If our session id is in the TEMP path then remove that part so we get the non-session
    // specific temporary folder.
    if (::ProcessIdToSessionId(::GetCurrentProcessId(), &dwSessionId))
    {
        hr = StrAllocFormatted(&sczSessionId, L"%u\\", dwSessionId);
        ExitOnFailure(hr, "Failed to format session id as a string.");

        hr = ::StringCchLengthW(sczSessionId, STRSAFE_MAX_CCH, reinterpret_cast<size_t*>(&cchSessionId));
        ExitOnFailure(hr, "Failed to get length of session id string.");

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, sczTempFolder + cchTempFolder - cchSessionId, static_cast<DWORD>(cchSessionId), sczSessionId, static_cast<DWORD>(cchSessionId)))
        {
            cchTempFolder -= cchSessionId;
        }
    }

    hr = StrAllocString(psczNonSessionTempFolder, sczTempFolder, cchTempFolder);
    ExitOnFailure(hr, "Failed to copy temp folder.");

LExit:
    ReleaseStr(sczSessionId);
    ReleaseStr(sczTempFolder);

    return hr;
}
