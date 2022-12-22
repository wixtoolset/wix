// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// constants

const DWORD RESTART_RETRIES = 10;

// internal function declarations

static HRESULT InitializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState,
    __in HANDLE hEngineFile
    );
static void UninitializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    );
static HRESULT RunUntrusted(
    __in BURN_ENGINE_STATE* pEngineState
    );
static HRESULT RunNormal(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    );
static HRESULT RunElevated(
    __in HINSTANCE hInstance,
    __in LPCWSTR wzCommandLine,
    __in BURN_ENGINE_STATE* pEngineState
    );
static HRESULT RunEmbedded(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    );
static HRESULT RunRunOnce(
    __in BURN_ENGINE_STATE* pEngineState,
    __in int nCmdShow
    );
static HRESULT RunApplication(
    __in BURN_ENGINE_STATE* pEngineState,
    __out BOOL* pfReloadApp,
    __out BOOL* pfSkipCleanup
    );
static HRESULT ProcessMessage(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in BOOTSTRAPPER_ENGINE_ACTION* pAction
    );
static HRESULT DAPI RedirectLoggingOverPipe(
    __in_z LPCSTR szString,
    __in_opt LPVOID pvContext
    );
static HRESULT LogStringOverPipe(
    __in_z LPCSTR szString,
    __in HANDLE hPipe
    );
static DWORD WINAPI ElevatedLoggingThreadProc(
    __in LPVOID lpThreadParameter
    );
static HRESULT Restart(
    __in BURN_ENGINE_STATE* pEngineState
    );
static void CALLBACK BurnTraceError(
    __in_z LPCSTR szFile,
    __in int iLine,
    __in REPORT_LEVEL rl,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );


// function definitions

extern "C" BOOL EngineInCleanRoom(
    __in_z_opt LPCWSTR wzCommandLine
    )
{
    // Be very careful with the functions you call from here.
    // This function will be called before ::SetDefaultDllDirectories()
    // has been called so dependencies outside of kernel32.dll are
    // very likely to introduce DLL hijacking opportunities.

    static DWORD cchCleanRoomSwitch = lstrlenW(BURN_COMMANDLINE_SWITCH_CLEAN_ROOM);

    // This check is wholly dependent on the clean room command line switch being
    // present at the beginning of the command line. Since Burn is the only thing
    // that should be setting this command line option, that is in our control.
    BOOL fInCleanRoom = (wzCommandLine &&
        (wzCommandLine[0] == L'-' || wzCommandLine[0] == L'/') &&
        CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, wzCommandLine + 1, cchCleanRoomSwitch, BURN_COMMANDLINE_SWITCH_CLEAN_ROOM, cchCleanRoomSwitch)
    );

    return fInCleanRoom;
}

extern "C" HRESULT EngineRun(
    __in HINSTANCE hInstance,
    __in HANDLE hEngineFile,
    __in_z_opt LPCWSTR wzCommandLine,
    __in int nCmdShow,
    __out DWORD* pdwExitCode
    )
{
    HRESULT hr = S_OK;
    BOOL fComInitialized = FALSE;
    BOOL fLogInitialized = FALSE;
    BOOL fCrypInitialized = FALSE;
    BOOL fDpiuInitialized = FALSE;
    BOOL fRegInitialized = FALSE;
    BOOL fWiuInitialized = FALSE;
    BOOL fXmlInitialized = FALSE;
    SYSTEM_INFO si = { };
    RTL_OSVERSIONINFOEXW ovix = { };
    LPWSTR sczExePath = NULL;
    BOOL fRunUntrusted = FALSE;
    BOOL fRunNormal = FALSE;
    BOOL fRunElevated = FALSE;
    BOOL fRunRunOnce = FALSE;

    BURN_ENGINE_STATE engineState = { };
    engineState.command.cbSize = sizeof(BOOTSTRAPPER_COMMAND);

    // Always initialize logging first
    LogInitialize(::GetModuleHandleW(NULL));
    DutilInitialize(&BurnTraceError);
    fLogInitialized = TRUE;

    // Ensure that log contains approriate level of information
#ifdef _DEBUG
    LogSetLevel(REPORT_DEBUG, FALSE);
#else
    LogSetLevel(REPORT_VERBOSE, FALSE); // FALSE means don't write an additional text line to the log saying the level changed
#endif

    hr = AppParseCommandLine(wzCommandLine, &engineState.internalCommand.argc, &engineState.internalCommand.argv);
    ExitOnFailure(hr, "Failed to parse command line.");

    hr = InitializeEngineState(&engineState, hEngineFile);
    ExitOnFailure(hr, "Failed to initialize engine state.");

    engineState.command.nCmdShow = nCmdShow;

    if (BURN_MODE_ELEVATED != engineState.internalCommand.mode && BOOTSTRAPPER_DISPLAY_NONE < engineState.command.display)
    {
        SplashScreenCreate(hInstance, NULL, &engineState.command.hwndSplashScreen);
    }

    // initialize platform layer
    PlatformInitialize();

    // initialize COM
    hr = ::CoInitializeEx(NULL, COINIT_MULTITHREADED);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    // Initialize dutil.
    hr = CrypInitialize();
    ExitOnFailure(hr, "Failed to initialize Cryputil.");
    fCrypInitialized = TRUE;

    DpiuInitialize();
    fDpiuInitialized = TRUE;

    hr = RegInitialize();
    ExitOnFailure(hr, "Failed to initialize Regutil.");
    fRegInitialized = TRUE;

    hr = WiuInitialize();
    ExitOnFailure(hr, "Failed to initialize Wiutil.");
    fWiuInitialized = TRUE;

    hr = XmlInitialize();
    ExitOnFailure(hr, "Failed to initialize XML util.");
    fXmlInitialized = TRUE;

    hr = OsRtlGetVersion(&ovix);
    ExitOnFailure(hr, "Failed to get OS info.");

#if defined(_M_ARM64)
    LPCSTR szBurnPlatform = "ARM64";
#elif defined(_M_AMD64)
    LPCSTR szBurnPlatform = "x64";
#else
    LPCSTR szBurnPlatform = "x86";
#endif

    LPCSTR szMachinePlatform = "unknown architecture";
    ::GetNativeSystemInfo(&si);
    switch (si.wProcessorArchitecture)
    {
    case PROCESSOR_ARCHITECTURE_AMD64:
        szMachinePlatform = "x64";
        break;
    case PROCESSOR_ARCHITECTURE_ARM:
        szMachinePlatform = "ARM";
        break;
    case PROCESSOR_ARCHITECTURE_ARM64:
        szMachinePlatform = "ARM64";
        break;
    case PROCESSOR_ARCHITECTURE_INTEL:
        szMachinePlatform = "x86";
        break;
    }

    PathForCurrentProcess(&sczExePath, NULL); // Ignore failure.
    LogId(REPORT_STANDARD, MSG_BURN_INFO, szInformationalVersion, ovix.dwMajorVersion, ovix.dwMinorVersion, ovix.dwBuildNumber, ovix.wServicePackMajor, sczExePath, szBurnPlatform, szMachinePlatform);
    ReleaseNullStr(sczExePath);

    // initialize core
    hr = CoreInitialize(&engineState);
    ExitOnFailure(hr, "Failed to initialize core.");

    // Select run mode.
    switch (engineState.internalCommand.mode)
    {
    case BURN_MODE_UNTRUSTED:
        fRunUntrusted = TRUE;

        hr = RunUntrusted(&engineState);
        ExitOnFailure(hr, "Failed to run untrusted mode.");
        break;

    case BURN_MODE_NORMAL:
        fRunNormal = TRUE;

        hr = RunNormal(hInstance, &engineState);
        ExitOnFailure(hr, "Failed to run per-user mode.");
        break;

    case BURN_MODE_ELEVATED:
        fRunElevated = TRUE;

        hr = RunElevated(hInstance, wzCommandLine, &engineState);
        ExitOnFailure(hr, "Failed to run per-machine mode.");
        break;

    case BURN_MODE_EMBEDDED:
        fRunNormal = TRUE;

        hr = RunEmbedded(hInstance, &engineState);
        ExitOnFailure(hr, "Failed to run embedded mode.");
        break;

    case BURN_MODE_RUNONCE:
        fRunRunOnce = TRUE;

        hr = RunRunOnce(&engineState, nCmdShow);
        ExitOnFailure(hr, "Failed to run RunOnce mode.");
        break;

    default:
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Invalid run mode.");
    }

    *pdwExitCode = engineState.userExperience.dwExitCode;

LExit:
    ReleaseStr(sczExePath);

    // If anything went wrong but the log was never open, try to open a "failure" log
    // and that will dump anything captured in the log memory buffer to the log.
    if (FAILED(hr) && BURN_LOGGING_STATE_CLOSED == engineState.log.state)
    {
        LoggingOpenFailed();
    }

    UserExperienceRemove(&engineState.userExperience);

    CacheRemoveBaseWorkingFolder(&engineState.cache);
    CacheUninitialize(&engineState.cache);

    // If this is a related bundle (but not an update) suppress restart and return the standard restart error code.
    if (engineState.fRestart && BOOTSTRAPPER_RELATION_NONE != engineState.command.relationType && BOOTSTRAPPER_RELATION_UPDATE != engineState.command.relationType)
    {
        LogId(REPORT_STANDARD, MSG_RESTART_ABORTED, LoggingRelationTypeToString(engineState.command.relationType));

        engineState.fRestart = FALSE;
        hr = SUCCEEDED(hr) ? HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED) : HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_REQUIRED);
    }

    if (fRunNormal)
    {
        LogId(REPORT_STANDARD, MSG_EXITING, FAILED(hr) ? (int)hr : *pdwExitCode, LoggingBoolToString(engineState.fRestart));
    }
    else if (fRunUntrusted)
    {
        LogId(REPORT_STANDARD, MSG_EXITING_CLEAN_ROOM, FAILED(hr) ? (int)hr : *pdwExitCode);
    }
    else if (fRunRunOnce)
    {
        LogId(REPORT_STANDARD, MSG_EXITING_RUN_ONCE, FAILED(hr) ? (int)hr : *pdwExitCode);
    }

    if (fLogInitialized)
    {
        // Leave the log open before calling restart so messages can be logged from there.
        // Best effort to make sure all previous messages are written to disk in case the restart causes messages to be lost in buffers.
        LogFlush();
    }

    // end per-machine process if running
    if (!fRunElevated && INVALID_HANDLE_VALUE != engineState.companionConnection.hPipe)
    {
        PipeTerminateChildProcess(&engineState.companionConnection, *pdwExitCode, engineState.fRestart);
    }
    else if (engineState.fRestart)
    {
        LogId(REPORT_STANDARD, MSG_RESTARTING);

        HRESULT hrRestart = Restart(&engineState);
        if (FAILED(hrRestart))
        {
            LogErrorId(hrRestart, MSG_RESTART_FAILED);
        }
    }

    // If the message window is still around, close it.
    UiCloseMessageWindow(&engineState);

    // If the logging thread is still around, close it.
    if (!fRunElevated)
    {
        CoreWaitForUnelevatedLoggingThread(engineState.hUnelevatedLoggingThread);
    }
    else if (fRunElevated)
    {
        CoreCloseElevatedLoggingThread(&engineState);

        // We're done talking to the child so always reset logging now.
        LogRedirect(NULL, NULL);

        // If there was a log message left, try to log it locally.
        if (engineState.elevatedLoggingContext.sczBuffer)
        {
            LogStringWorkRaw(engineState.elevatedLoggingContext.sczBuffer);

            ReleaseStr(engineState.elevatedLoggingContext.sczBuffer);
        }

        // Log the exit code here to make sure it gets in the elevated log.
        LogId(REPORT_STANDARD, MSG_EXITING_ELEVATED, FAILED(hr) ? (int)hr : *pdwExitCode);
    }

    UninitializeEngineState(&engineState);

    if (fXmlInitialized)
    {
        XmlUninitialize();
    }

    if (fWiuInitialized)
    {
        WiuUninitialize();
    }

    if (fRegInitialized)
    {
        RegUninitialize();
    }

    if (fDpiuInitialized)
    {
        DpiuUninitialize();
    }

    if (fCrypInitialized)
    {
        CrypUninitialize();
    }

    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    if (fLogInitialized)
    {
        DutilUninitialize();
        LogUninitialize(FALSE);
    }

    return hr;
}


// internal function definitions

static HRESULT InitializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState,
    __in HANDLE hEngineFile
    )
{
    HRESULT hr = S_OK;
    HANDLE hSectionFile = hEngineFile;
    HANDLE hSourceEngineFile = INVALID_HANDLE_VALUE;

    pEngineState->hUnelevatedLoggingThread = INVALID_HANDLE_VALUE;
    pEngineState->elevatedLoggingContext.hPipe = INVALID_HANDLE_VALUE;
    pEngineState->elevatedLoggingContext.hThread = INVALID_HANDLE_VALUE;

    ::InitializeCriticalSection(&pEngineState->csRestartState);
    ::InitializeCriticalSection(&pEngineState->elevatedLoggingContext.csBuffer);

    pEngineState->internalCommand.automaticUpdates = BURN_AU_PAUSE_ACTION_IFELEVATED;
    ::InitializeCriticalSection(&pEngineState->userExperience.csEngineActive);
    PipeConnectionInitialize(&pEngineState->companionConnection);
    PipeConnectionInitialize(&pEngineState->embeddedConnection);

    // Retain whether bundle was initially run elevated.
    ProcElevated(::GetCurrentProcess(), &pEngineState->internalCommand.fInitiallyElevated);

    // Parse command line.
    hr = CoreParseCommandLine(&pEngineState->internalCommand, &pEngineState->command, &pEngineState->companionConnection, &pEngineState->embeddedConnection, &hSectionFile, &hSourceEngineFile);
    ExitOnFailure(hr, "Fatal error while parsing command line.");

    hr = SectionInitialize(&pEngineState->section, hSectionFile, hSourceEngineFile);
    ExitOnFailure(hr, "Failed to initialize engine section.");

    hr = CacheInitialize(&pEngineState->cache, &pEngineState->internalCommand);
    ExitOnFailure(hr, "Failed to initialize internal cache functionality.");

LExit:
    return hr;
}

static void UninitializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    if (pEngineState->internalCommand.argv)
    {
        AppFreeCommandLineArgs(pEngineState->internalCommand.argv);
    }

    ReleaseMem(pEngineState->internalCommand.rgSecretArgs);
    ReleaseMem(pEngineState->internalCommand.rgUnknownArgs);

    ReleaseFileHandle(pEngineState->hUnelevatedLoggingThread);
    ReleaseFileHandle(pEngineState->elevatedLoggingContext.hThread);
    ::DeleteCriticalSection(&pEngineState->elevatedLoggingContext.csBuffer);
    ReleaseHandle(pEngineState->elevatedLoggingContext.hLogEvent);
    ReleaseHandle(pEngineState->elevatedLoggingContext.hFinishedEvent);

    PipeConnectionUninitialize(&pEngineState->embeddedConnection);
    PipeConnectionUninitialize(&pEngineState->companionConnection);
    ReleaseStr(pEngineState->sczBundleEngineWorkingPath)

    ReleaseHandle(pEngineState->hMessageWindowThread);

    BurnExtensionUninitialize(&pEngineState->extensions);

    ::DeleteCriticalSection(&pEngineState->userExperience.csEngineActive);
    UserExperienceUninitialize(&pEngineState->userExperience);

    ApprovedExesUninitialize(&pEngineState->approvedExes);
    DependencyUninitialize(&pEngineState->dependencies);
    UpdateUninitialize(&pEngineState->update);
    VariablesUninitialize(&pEngineState->variables);
    SearchesUninitialize(&pEngineState->searches);
    RegistrationUninitialize(&pEngineState->registration);
    PayloadsUninitialize(&pEngineState->payloads);
    PackagesUninitialize(&pEngineState->packages);
    SectionUninitialize(&pEngineState->section);
    ContainersUninitialize(&pEngineState->containers);

    ReleaseStr(pEngineState->command.wzBootstrapperApplicationDataPath);
    ReleaseStr(pEngineState->command.wzBootstrapperWorkingFolder);
    ReleaseStr(pEngineState->command.wzLayoutDirectory);
    ReleaseStr(pEngineState->command.wzCommandLine);

    ReleaseStr(pEngineState->internalCommand.sczActiveParent);
    ReleaseStr(pEngineState->internalCommand.sczAncestors);
    ReleaseStr(pEngineState->internalCommand.sczIgnoreDependencies);
    ReleaseStr(pEngineState->internalCommand.sczLogFile);
    ReleaseStr(pEngineState->internalCommand.sczOriginalSource);
    ReleaseStr(pEngineState->internalCommand.sczSourceProcessPath);
    ReleaseStr(pEngineState->internalCommand.sczEngineWorkingDirectory);

    ReleaseStr(pEngineState->log.sczExtension);
    ReleaseStr(pEngineState->log.sczPrefix);
    ReleaseStr(pEngineState->log.sczPath);
    ReleaseStr(pEngineState->log.sczPathVariable);

    ::DeleteCriticalSection(&pEngineState->csRestartState);

    // clear struct
    memset(pEngineState, 0, sizeof(BURN_ENGINE_STATE));
}

static HRESULT RunUntrusted(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCurrentProcessPath = NULL;
    LPWSTR wzCleanRoomBundlePath = NULL;
    LPWSTR sczCachedCleanRoomBundlePath = NULL;
    LPWSTR sczParameters = NULL;
    LPWSTR sczFullCommandLine = NULL;
    PROCESS_INFORMATION pi = { };
    HANDLE hFileAttached = NULL;
    HANDLE hFileSelf = NULL;
    HANDLE hProcess = NULL;

    // Initialize logging.
    hr = LoggingOpen(&pEngineState->log, &pEngineState->internalCommand, &pEngineState->command, &pEngineState->variables, pEngineState->registration.sczDisplayName);
    ExitOnFailure(hr, "Failed to open clean room log.");

    hr = PathForCurrentProcess(&sczCurrentProcessPath, NULL);
    ExitOnFailure(hr, "Failed to get path for current process.");

    // If we're running from the package cache, we're in a secure
    // folder (DLLs cannot be inserted here for hijacking purposes)
    // so just launch the current process's path as the clean room
    // process. Technically speaking, we'd be able to skip creating
    // a clean room process at all (since we're already running from
    // a secure folder) but it makes the code that only wants to run
    // in clean room more complicated if we don't launch an explicit
    // clean room process.
    if (CacheBundleRunningFromCache(&pEngineState->cache))
    {
        wzCleanRoomBundlePath = sczCurrentProcessPath;
    }
    else
    {
        hr = CacheBundleToCleanRoom(&pEngineState->cache, &pEngineState->section, &sczCachedCleanRoomBundlePath);
        ExitOnFailure(hr, "Failed to cache to clean room.");

        wzCleanRoomBundlePath = sczCachedCleanRoomBundlePath;
    }

    hr = CoreCreateCleanRoomCommandLine(&sczParameters, pEngineState, wzCleanRoomBundlePath, sczCurrentProcessPath, &hFileAttached, &hFileSelf);
    ExitOnFailure(hr, "Failed to create clean room command-line.");

    hr = StrAllocFormattedSecure(&sczFullCommandLine, L"\"%ls\" %ls", wzCleanRoomBundlePath, sczParameters);
    ExitOnFailure(hr, "Failed to allocate full command-line.");

    hr = CoreCreateProcess(wzCleanRoomBundlePath, sczFullCommandLine, TRUE, 0, NULL, static_cast<WORD>(pEngineState->command.nCmdShow), &pi);
    ExitOnFailure(hr, "Failed to launch clean room process: %ls", sczFullCommandLine);

    hProcess = pi.hProcess;
    pi.hProcess = NULL;

    hr = ProcWaitForCompletion(hProcess, INFINITE, &pEngineState->userExperience.dwExitCode);
    ExitOnFailure(hr, "Failed to wait for clean room process: %ls", wzCleanRoomBundlePath);

LExit:
    // If the splash screen is still around, close it.
    if (::IsWindow(pEngineState->command.hwndSplashScreen))
    {
        ::PostMessageW(pEngineState->command.hwndSplashScreen, WM_CLOSE, 0, 0);
    }

    ReleaseHandle(pi.hThread);
    ReleaseFileHandle(hFileSelf);
    ReleaseFileHandle(hFileAttached);
    ReleaseHandle(hProcess);
    StrSecureZeroFreeString(sczFullCommandLine);
    StrSecureZeroFreeString(sczParameters);
    ReleaseStr(sczCachedCleanRoomBundlePath);
    ReleaseStr(sczCurrentProcessPath);

    return hr;
}

static HRESULT RunNormal(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczOriginalSource = NULL;
    LPWSTR sczCopiedOriginalSource = NULL;
    BOOL fContinueExecution = TRUE;
    BOOL fReloadApp = FALSE;
    BOOL fSkipCleanup = FALSE;
    BURN_EXTENSION_ENGINE_CONTEXT extensionEngineContext = { };

    // Initialize logging.
    hr = LoggingOpen(&pEngineState->log, &pEngineState->internalCommand, &pEngineState->command, &pEngineState->variables, pEngineState->registration.sczDisplayName);
    ExitOnFailure(hr, "Failed to open log.");

    // Ensure we're on a supported operating system.
    hr = ConditionGlobalCheck(&pEngineState->variables, &pEngineState->condition, pEngineState->command.display, pEngineState->registration.sczDisplayName, &pEngineState->userExperience.dwExitCode, &fContinueExecution);
    ExitOnFailure(hr, "Failed to check global conditions");

    if (!fContinueExecution)
    {
        LogId(REPORT_STANDARD, MSG_FAILED_CONDITION_CHECK);

        // If the block told us to abort, abort!
        ExitFunction1(hr = S_OK);
    }

    // Create a top-level window to handle system messages.
    hr = UiCreateMessageWindow(hInstance, pEngineState);
    ExitOnFailure(hr, "Failed to create the message window.");

    // Query registration state.
    hr = CoreQueryRegistration(pEngineState);
    ExitOnFailure(hr, "Failed to query registration.");

    // Best effort to set the source of attached containers to BURN_BUNDLE_ORIGINAL_SOURCE.
    hr = VariableGetString(&pEngineState->variables, BURN_BUNDLE_ORIGINAL_SOURCE, &sczOriginalSource);
    if (SUCCEEDED(hr))
    {
        for (DWORD i = 0; i < pEngineState->containers.cContainers; ++i)
        {
            BURN_CONTAINER* pContainer = pEngineState->containers.rgContainers + i;
            if (pContainer->fAttached)
            {
                hr = StrAllocString(&sczCopiedOriginalSource, sczOriginalSource, 0);
                if (SUCCEEDED(hr))
                {
                    ReleaseNullStr(pContainer->sczSourcePath);
                    pContainer->sczSourcePath = sczCopiedOriginalSource;
                    sczCopiedOriginalSource = NULL;
                }
            }
        }
    }
    hr = S_OK;

    // Set some built-in variables before loading the BA.
    hr = VariableSetNumeric(&pEngineState->variables, BURN_BUNDLE_COMMAND_LINE_ACTION, pEngineState->command.action, TRUE);
    ExitOnFailure(hr, "Failed to set command line action variable.");

    hr = RegistrationSetVariables(&pEngineState->registration, &pEngineState->variables);
    ExitOnFailure(hr, "Failed to set registration variables.");

    // If a layout directory was specified on the command-line, set it as a well-known variable.
    if (pEngineState->command.wzLayoutDirectory && *pEngineState->command.wzLayoutDirectory)
    {
        hr = VariableSetString(&pEngineState->variables, BURN_BUNDLE_LAYOUT_DIRECTORY, pEngineState->command.wzLayoutDirectory, FALSE, FALSE);
        ExitOnFailure(hr, "Failed to set layout directory variable to value provided from command-line.");
    }

    // Setup the extension engine.
    extensionEngineContext.pEngineState = pEngineState;

    // Load the extensions.
    hr = BurnExtensionLoad(&pEngineState->extensions, &extensionEngineContext);
    ExitOnFailure(hr, "Failed to load BundleExtensions.");

    do
    {
        fReloadApp = FALSE;
        pEngineState->fQuit = FALSE;

        hr = RunApplication(pEngineState, &fReloadApp, &fSkipCleanup);
        ExitOnFailure(hr, "Failed while running ");
    } while (fReloadApp);

LExit:
    if (!fSkipCleanup)
    {
        CoreCleanup(pEngineState);
    }

    BurnExtensionUnload(&pEngineState->extensions);

    VariablesDump(&pEngineState->variables);

    // If the splash screen is still around, close it.
    if (::IsWindow(pEngineState->command.hwndSplashScreen))
    {
        ::PostMessageW(pEngineState->command.hwndSplashScreen, WM_CLOSE, 0, 0);
    }

    ReleaseStr(sczOriginalSource);
    ReleaseStr(sczCopiedOriginalSource);

    return hr;
}

static HRESULT RunElevated(
    __in HINSTANCE hInstance,
    __in LPCWSTR /*wzCommandLine*/,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    HANDLE hLock = NULL;
    BURN_REDIRECTED_LOGGING_CONTEXT* pLoggingContext = &pEngineState->elevatedLoggingContext;

    // Initialize logging.
    hr = LoggingOpen(&pEngineState->log, &pEngineState->internalCommand, &pEngineState->command, &pEngineState->variables, pEngineState->registration.sczDisplayName);
    ExitOnFailure(hr, "Failed to open elevated log.");

    // connect to per-user process
    hr = PipeChildConnect(&pEngineState->companionConnection, TRUE);
    ExitOnFailure(hr, "Failed to connect to unelevated process.");

    // Set up the context for the logging thread then
    // override logging to write over the pipe.
    pLoggingContext->hLogEvent = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(pLoggingContext->hLogEvent, hr, "Failed to create log event for logging thread.");

    pLoggingContext->hFinishedEvent = ::CreateEventW(NULL, TRUE, FALSE, NULL);
    ExitOnNullWithLastError(pLoggingContext->hFinishedEvent, hr, "Failed to create finished event for logging thread.");

    pLoggingContext->hPipe = pEngineState->companionConnection.hLoggingPipe;

    pLoggingContext->hThread = ::CreateThread(NULL, 0, ElevatedLoggingThreadProc, pLoggingContext, 0, NULL);
    ExitOnNullWithLastError(pLoggingContext->hThread, hr, "Failed to create elevated logging thread.");

    LogRedirect(RedirectLoggingOverPipe, pLoggingContext);

    if (!pEngineState->internalCommand.fInitiallyElevated)
    {
        LogId(REPORT_ERROR, MSG_ELEVATED_ENGINE_NOT_ELEVATED);
    }

    // Create a top-level window to prevent shutting down the elevated process.
    hr = UiCreateMessageWindow(hInstance, pEngineState);
    ExitOnFailure(hr, "Failed to create the message window.");

    SrpInitialize(TRUE);

    // Pump messages from parent process.
    hr = ElevationChildPumpMessages(pEngineState->companionConnection.hPipe, pEngineState->companionConnection.hCachePipe, &pEngineState->approvedExes, &pEngineState->cache, &pEngineState->containers, &pEngineState->packages, &pEngineState->payloads, &pEngineState->variables, &pEngineState->registration, &pEngineState->userExperience, &hLock, &pEngineState->userExperience.dwExitCode, &pEngineState->fRestart, &pEngineState->plan.fApplying);
    ExitOnFailure(hr, "Failed to pump messages from parent process.");

LExit:
    if (hLock)
    {
        ::ReleaseMutex(hLock);
        ::CloseHandle(hLock);
    }

    return hr;
}

static HRESULT RunEmbedded(
    __in HINSTANCE hInstance,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;

    // Connect to parent process.
    hr = PipeChildConnect(&pEngineState->embeddedConnection, FALSE);
    ExitOnFailure(hr, "Failed to connect to parent of embedded process.");

    // Do not register the bundle to automatically restart if embedded.
    if (BOOTSTRAPPER_DISPLAY_EMBEDDED == pEngineState->command.display)
    {
        pEngineState->registration.fDisableResume = TRUE;
    }

    // Now run the application like normal.
    hr = RunNormal(hInstance, pEngineState);
    ExitOnFailure(hr, "Failed to run bootstrapper application embedded.");

LExit:
    return hr;
}

static HRESULT RunRunOnce(
    __in BURN_ENGINE_STATE* pEngineState,
    __in int nCmdShow
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczNewCommandLine = NULL;
    LPWSTR sczBurnPath = NULL;
    HANDLE hProcess = NULL;

    // Initialize logging.
    hr = LoggingOpen(&pEngineState->log, &pEngineState->internalCommand, &pEngineState->command, &pEngineState->variables, pEngineState->registration.sczDisplayName);
    ExitOnFailure(hr, "Failed to open run once log.");

    hr = RegistrationGetResumeCommandLine(&pEngineState->registration, &sczNewCommandLine);
    ExitOnFailure(hr, "Unable to get resume command line from the registry");

    // and re-launch
    hr = PathForCurrentProcess(&sczBurnPath, NULL);
    ExitOnFailure(hr, "Failed to get current process path.");

    hr = ProcExec(sczBurnPath, 0 < sczNewCommandLine ? sczNewCommandLine : L"", nCmdShow, &hProcess);
    ExitOnFailure(hr, "Failed to re-launch bundle process after RunOnce: %ls", sczBurnPath);

LExit:
    ReleaseHandle(hProcess);
    ReleaseStr(sczNewCommandLine);
    ReleaseStr(sczBurnPath);

    return hr;
}

static void CALLBACK FreeQueueItem(
    __in void* pvValue,
    __in void* /*pvContext*/
    )
{
    BOOTSTRAPPER_ENGINE_ACTION* pAction = reinterpret_cast<BOOTSTRAPPER_ENGINE_ACTION*>(pvValue);

    LogId(REPORT_WARNING, MSG_IGNORE_OPERATION_AFTER_QUIT, LoggingBurnMessageToString(pAction->dwMessage));

    CoreBootstrapperEngineActionUninitialize(pAction);
    MemFree(pAction);
}

static HRESULT RunApplication(
    __in BURN_ENGINE_STATE* pEngineState,
    __out BOOL* pfReloadApp,
    __out BOOL* pfSkipCleanup
    )
{
    HRESULT hr = S_OK;
    BOOTSTRAPPER_ENGINE_CONTEXT engineContext = { };
    BOOL fStartupCalled = FALSE;
    BOOTSTRAPPER_SHUTDOWN_ACTION shutdownAction = BOOTSTRAPPER_SHUTDOWN_ACTION_NONE;
    BOOTSTRAPPER_ENGINE_ACTION* pAction = NULL;

    // Setup the bootstrapper engine.
    engineContext.pEngineState = pEngineState;

    ::InitializeCriticalSection(&engineContext.csQueue);

    engineContext.hQueueSemaphore = ::CreateSemaphoreW(NULL, 0, LONG_MAX, NULL);
    ExitOnNullWithLastError(engineContext.hQueueSemaphore, hr, "Failed to create semaphore for queue.");

    hr = QueCreate(&engineContext.hQueue);
    ExitOnFailure(hr, "Failed to create queue for bootstrapper engine.");

    // Load the bootstrapper application.
    hr = UserExperienceLoad(&pEngineState->userExperience, &engineContext, &pEngineState->command);
    ExitOnFailure(hr, "Failed to load BA.");

    fStartupCalled = TRUE;
    hr = UserExperienceOnStartup(&pEngineState->userExperience);
    ExitOnFailure(hr, "Failed to start bootstrapper application.");

    while (!pEngineState->fQuit)
    {
        hr = AppWaitForSingleObject(engineContext.hQueueSemaphore, INFINITE);
        ExitOnFailure(hr, "Failed to wait on queue event.");

        ::EnterCriticalSection(&engineContext.csQueue);

        hr = QueDequeue(engineContext.hQueue, reinterpret_cast<void**>(&pAction));

        ::LeaveCriticalSection(&engineContext.csQueue);

        ExitOnFailure(hr, "Failed to dequeue action.");

        ProcessMessage(&engineContext, pAction);

        CoreBootstrapperEngineActionUninitialize(pAction);
        MemFree(pAction);
    }

LExit:
    if (fStartupCalled)
    {
        UserExperienceOnShutdown(&pEngineState->userExperience, &shutdownAction);
        if (BOOTSTRAPPER_SHUTDOWN_ACTION_RESTART == shutdownAction)
        {
            LogId(REPORT_STANDARD, MSG_BA_REQUESTED_RESTART, LoggingBoolToString(pEngineState->fRestart));
            pEngineState->fRestart = TRUE;
        }
        else if (BOOTSTRAPPER_SHUTDOWN_ACTION_RELOAD_BOOTSTRAPPER == shutdownAction)
        {
            LogId(REPORT_STANDARD, MSG_BA_REQUESTED_RELOAD);
            *pfReloadApp = SUCCEEDED(hr);
        }
        else if (BOOTSTRAPPER_SHUTDOWN_ACTION_SKIP_CLEANUP == shutdownAction)
        {
            LogId(REPORT_STANDARD, MSG_BA_REQUESTED_SKIP_CLEANUP);
            *pfSkipCleanup = TRUE;
        }
    }

    // Unload BA.
    UserExperienceUnload(&pEngineState->userExperience, *pfReloadApp);

    ::DeleteCriticalSection(&engineContext.csQueue);
    ReleaseHandle(engineContext.hQueueSemaphore);
    ReleaseQueue(engineContext.hQueue, FreeQueueItem, &engineContext);

    return hr;
}

static HRESULT ProcessMessage(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in BOOTSTRAPPER_ENGINE_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    BURN_ENGINE_STATE* pEngineState = pEngineContext->pEngineState;

    UserExperienceActivateEngine(&pEngineState->userExperience);

    switch (pAction->dwMessage)
    {
    case WM_BURN_DETECT:
        hr = CoreDetect(pEngineState, pAction->detect.hwndParent);
        break;

    case WM_BURN_PLAN:
        hr = CorePlan(pEngineState, pAction->plan.action);
        break;

    case WM_BURN_ELEVATE:
        hr = CoreElevate(pEngineState, WM_BURN_ELEVATE, pAction->elevate.hwndParent);
        break;

    case WM_BURN_APPLY:
        hr = CoreApply(pEngineState, pAction->apply.hwndParent);
        break;

    case WM_BURN_LAUNCH_APPROVED_EXE:
        hr = CoreLaunchApprovedExe(pEngineState, &pAction->launchApprovedExe);
        break;

    case WM_BURN_QUIT:
        CoreQuit(pEngineContext, pAction->quit.dwExitCode);
        break;
    }

    UserExperienceDeactivateEngine(&pEngineState->userExperience);

    return hr;
}

static HRESULT DAPI RedirectLoggingOverPipe(
    __in_z LPCSTR szString,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    BURN_REDIRECTED_LOGGING_CONTEXT* pContext = static_cast<BURN_REDIRECTED_LOGGING_CONTEXT*>(pvContext);

    ::EnterCriticalSection(&pContext->csBuffer);

    hr = StrAnsiAllocConcat(&pContext->sczBuffer, szString, 0);

    if (SUCCEEDED(hr) && !::SetEvent(pContext->hLogEvent))
    {
        HRESULT hrSet = HRESULT_FROM_WIN32(::GetLastError());
        if (FAILED(hrSet))
        {
            TraceError(hrSet, "Failed to set log event.");
        }
    }

    ::LeaveCriticalSection(&pContext->csBuffer);

    return hr;
}

static HRESULT LogStringOverPipe(
    __in_z LPCSTR szString,
    __in HANDLE hPipe
    )
{
    HRESULT hr = S_OK;
    BYTE* pbData = NULL;
    SIZE_T cbData = 0;
    DWORD dwResult = 0;

    hr = BuffWriteStringAnsi(&pbData, &cbData, szString);
    ExitOnFailure(hr, "Failed to prepare logging pipe message.");

    hr = PipeSendMessage(hPipe, static_cast<DWORD>(BURN_PIPE_MESSAGE_TYPE_LOG), pbData, cbData, NULL, NULL, &dwResult);
    ExitOnFailure(hr, "Failed to send logging message over the pipe.");

    hr = (HRESULT)dwResult;

LExit:
    ReleaseBuffer(pbData);

    return hr;
}

static DWORD WINAPI ElevatedLoggingThreadProc(
    __in LPVOID lpThreadParameter
    )
{
    HRESULT hr = S_OK;
    DWORD dwLastError = ERROR_SUCCESS;
    BURN_REDIRECTED_LOGGING_CONTEXT* pContext = static_cast<BURN_REDIRECTED_LOGGING_CONTEXT*>(lpThreadParameter);
    DWORD dwSignaledIndex = 0;
    LPSTR sczBuffer = NULL;
    BURN_PIPE_RESULT result = { };
    HANDLE rghEvents[2] =
    {
        pContext->hLogEvent,
        pContext->hFinishedEvent,
    };

    for (;;)
    {
        hr = AppWaitForMultipleObjects(countof(rghEvents), rghEvents, FALSE, INFINITE, &dwSignaledIndex);
        if (FAILED(hr))
        {
            LogRedirect(NULL, NULL); // reset logging so the next failure gets written locally.
            ExitOnFailure(hr, "Failed to wait for log thread events, signaled: %u.", dwSignaledIndex);
        }

        if (1 == dwSignaledIndex)
        {
            LogRedirect(NULL, NULL); // No more messages will be logged over the pipe.
        }

        dwLastError = ERROR_SUCCESS;

        ::EnterCriticalSection(&pContext->csBuffer);

        sczBuffer = pContext->sczBuffer;
        pContext->sczBuffer = NULL;

        if (0 == dwSignaledIndex && !::ResetEvent(rghEvents[0]))
        {
            dwLastError = ::GetLastError();
        }

        ::LeaveCriticalSection(&pContext->csBuffer);

        if (ERROR_SUCCESS != dwLastError)
        {
            LogRedirect(NULL, NULL); // reset logging so the next failure gets written locally.
            ExitOnWin32Error(dwLastError, hr, "Failed to reset log event.");
        }

        if (sczBuffer)
        {
            hr = LogStringOverPipe(sczBuffer, pContext->hPipe);
            if (FAILED(hr))
            {
                LogRedirect(NULL, NULL); // reset logging so the next failure gets written locally.
                ExitOnFailure(hr, "Failed to wait log message over pipe.");
            }

            ReleaseStr(sczBuffer);
        }

        if (1 == dwSignaledIndex)
        {
            break;
        }
    }

LExit:
    LogRedirect(NULL, NULL); // No more messages will be logged over the pipe.

    {
        HRESULT hrTerminate = PipeTerminateLoggingPipe(pContext->hPipe, hr);
        if (FAILED(hrTerminate))
        {
            TraceError(hrTerminate, "Failed to terminate logging pipe.");
        }
    }

    // Log the message locally if it failed to go over the pipe.
    if (sczBuffer)
    {
        LogStringWorkRaw(sczBuffer);

        ReleaseStr(sczBuffer);
    }

    // Log any remaining message locally.
    if (pContext->sczBuffer)
    {
        AssertSz(FAILED(hr), "Exiting logging thread on success even though there was a leftover message");
        LogStringWorkRaw(pContext->sczBuffer);

        ReleaseStr(pContext->sczBuffer);
    }

    return (DWORD)hr;
}

static HRESULT Restart(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    DWORD dwRetries = 0;

    hr = ProcEnablePrivilege(::GetCurrentProcess(), SE_SHUTDOWN_NAME);
    ExitOnFailure(hr, "Failed to enable shutdown privilege in process token.");

    pEngineState->fRestarting = TRUE;
    CoreUpdateRestartState(pEngineState, BURN_RESTART_STATE_REQUESTING);

    do
    {
        hr = S_OK;

        if (dwRetries)
        {
            // On retry, wait a second to let the OS try to get to a place where the restart can
            // be initiated.
            ::Sleep(1000);
        }

        if (!vpfnInitiateSystemShutdownExW(NULL, NULL, 0, FALSE, TRUE, SHTDN_REASON_MAJOR_APPLICATION | SHTDN_REASON_MINOR_INSTALLATION | SHTDN_REASON_FLAG_PLANNED))
        {
            hr = HRESULT_FROM_WIN32(::GetLastError());
        }
    } while (dwRetries++ < RESTART_RETRIES && (HRESULT_FROM_WIN32(ERROR_MACHINE_LOCKED) == hr || HRESULT_FROM_WIN32(ERROR_NOT_READY) == hr));
    ExitOnRootFailure(hr, "Failed to schedule restart.");

    CoreUpdateRestartState(pEngineState, BURN_RESTART_STATE_REQUESTED);

    // Give the UI thread approximately 15 seconds to get the WM_QUERYENDSESSION message.
    for (DWORD i = 0; i < 60; ++i)
    {
        if (!::IsWindow(pEngineState->hMessageWindow))
        {
            ExitFunction();
        }

        if (BURN_RESTART_STATE_REQUESTED < pEngineState->restartState)
        {
            break;
        }

        ::Sleep(250);
    }

    if (BURN_RESTART_STATE_INITIATING > pEngineState->restartState)
    {
        LogId(REPORT_WARNING, MSG_RESTART_BLOCKED);
        ExitFunction();
    }

    // Give the UI thread the chance to process the WM_ENDSESSION message.
    for (;;)
    {
        if (!::IsWindow(pEngineState->hMessageWindow) || BURN_RESTART_STATE_INITIATING < pEngineState->restartState)
        {
            break;
        }

        ::Sleep(250);
    }

LExit:
    return hr;
}

static void CALLBACK BurnTraceError(
    __in_z LPCSTR /*szFile*/,
    __in int /*iLine*/,
    __in REPORT_LEVEL /*rl*/,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    BOOL fLog = FALSE;

    switch (source)
    {
    case DUTIL_SOURCE_DEFAULT:
        fLog = TRUE;
        break;
    default:
        fLog = REPORT_VERBOSE < LogGetLevel();
        break;
    }

    if (fLog)
    {
        DutilSuppressTraceErrorSource();
        LogErrorStringArgs(hrError, szFormat, args);
        DutilUnsuppressTraceErrorSource();
    }
}
