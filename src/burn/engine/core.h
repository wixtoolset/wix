#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

const LPCWSTR BURN_POLICY_REGISTRY_PATH = L"WiX\\Burn";

const LPCWSTR BURN_COMMANDLINE_SWITCH_PARENT = L"parent";
const LPCWSTR BURN_COMMANDLINE_SWITCH_PARENT_NONE = L"parent:none";
const LPCWSTR BURN_COMMANDLINE_SWITCH_CLEAN_ROOM = L"burn.clean.room";
const LPCWSTR BURN_COMMANDLINE_SWITCH_WORKING_DIRECTORY = L"burn.engine.working.directory";
const LPCWSTR BURN_COMMANDLINE_SWITCH_ELEVATED = L"burn.elevated";
const LPCWSTR BURN_COMMANDLINE_SWITCH_EMBEDDED = L"burn.embedded";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RUNONCE = L"burn.runonce";
const LPCWSTR BURN_COMMANDLINE_SWITCH_LOG_APPEND = L"burn.log.append";
const LPCWSTR BURN_COMMANDLINE_SWITCH_LOG_MODE = L"burn.log.mode";
const LPCWSTR BURN_COMMANDLINE_SWITCH_LOG_CONSOLE = L"burn.log.console";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_DETECT = L"burn.related.detect";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_UPGRADE = L"burn.related.upgrade";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_ADDON = L"burn.related.addon";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_DEPENDENT_ADDON = L"burn.related.dependent.addon";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_PATCH = L"burn.related.patch";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_DEPENDENT_PATCH = L"burn.related.dependent.patch";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_UPDATE = L"burn.related.update";
const LPCWSTR BURN_COMMANDLINE_SWITCH_RELATED_CHAIN_PACKAGE = L"burn.related.chain.package";
const LPCWSTR BURN_COMMANDLINE_SWITCH_PASSTHROUGH = L"burn.passthrough";
const LPCWSTR BURN_COMMANDLINE_SWITCH_DISABLE_UNELEVATE = L"burn.disable.unelevate";
const LPCWSTR BURN_COMMANDLINE_SWITCH_IGNOREDEPENDENCIES = L"burn.ignoredependencies";
const LPCWSTR BURN_COMMANDLINE_SWITCH_ANCESTORS = L"burn.ancestors";
const LPCWSTR BURN_COMMANDLINE_SWITCH_FILEHANDLE_ATTACHED = L"burn.filehandle.attached";
const LPCWSTR BURN_COMMANDLINE_SWITCH_FILEHANDLE_SELF = L"burn.filehandle.self";
const LPCWSTR BURN_COMMANDLINE_SWITCH_SPLASH_SCREEN = L"burn.splash.screen";
const LPCWSTR BURN_COMMANDLINE_SWITCH_SYSTEM_COMPONENT = L"burn.system.component";
const LPCWSTR BURN_COMMANDLINE_SWITCH_PREFIX = L"burn.";

const LPCWSTR BURN_BUNDLE_ACTION = L"WixBundleAction";
const LPCWSTR BURN_BUNDLE_ACTIVE_PARENT = L"WixBundleActiveParent";
const LPCWSTR BURN_BUNDLE_COMMAND_LINE_ACTION = L"WixBundleCommandLineAction";
const LPCWSTR BURN_BUNDLE_EXECUTE_PACKAGE_CACHE_FOLDER = L"WixBundleExecutePackageCacheFolder";
const LPCWSTR BURN_BUNDLE_EXECUTE_PACKAGE_ACTION = L"WixBundleExecutePackageAction";
const LPCWSTR BURN_BUNDLE_FORCED_RESTART_PACKAGE = L"WixBundleForcedRestartPackage";
const LPCWSTR BURN_BUNDLE_INSTALLED = L"WixBundleInstalled";
const LPCWSTR BURN_BUNDLE_ELEVATED = L"WixBundleElevated";
const LPCWSTR BURN_BUNDLE_PROVIDER_KEY = L"WixBundleProviderKey";
const LPCWSTR BURN_BUNDLE_SOURCE_PROCESS_PATH = L"WixBundleSourceProcessPath";
const LPCWSTR BURN_BUNDLE_SOURCE_PROCESS_FOLDER = L"WixBundleSourceProcessFolder";
const LPCWSTR BURN_BUNDLE_TAG = L"WixBundleTag";
const LPCWSTR BURN_BUNDLE_UILEVEL = L"WixBundleUILevel";
const LPCWSTR BURN_BUNDLE_VERSION = L"WixBundleVersion";

// The following well-known variables are settable by the BA.
const LPCWSTR BURN_BUNDLE_LAYOUT_DIRECTORY = L"WixBundleLayoutDirectory";
const LPCWSTR BURN_BUNDLE_NAME = L"WixBundleName";
const LPCWSTR BURN_BUNDLE_INPROGRESS_NAME = L"WixBundleInProgressName";
const LPCWSTR BURN_BUNDLE_MANUFACTURER = L"WixBundleManufacturer";
const LPCWSTR BURN_BUNDLE_ORIGINAL_SOURCE = L"WixBundleOriginalSource";
const LPCWSTR BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER = L"WixBundleOriginalSourceFolder";
const LPCWSTR BURN_BUNDLE_LAST_USED_SOURCE = L"WixBundleLastUsedSource";


// enums

enum BURN_MODE
{
    BURN_MODE_UNKNOWN,
    BURN_MODE_UNTRUSTED,
    BURN_MODE_NORMAL,
    BURN_MODE_ELEVATED,
    BURN_MODE_EMBEDDED,
    BURN_MODE_RUNONCE,
};

enum BURN_AU_PAUSE_ACTION
{
    BURN_AU_PAUSE_ACTION_NONE,
    BURN_AU_PAUSE_ACTION_IFELEVATED,
    BURN_AU_PAUSE_ACTION_IFELEVATED_NORESUME,
};

enum BURN_RESTART_STATE
{
    BURN_RESTART_STATE_NONE,
    BURN_RESTART_STATE_REQUESTING,
    BURN_RESTART_STATE_REQUESTED,
    BURN_RESTART_STATE_INITIATING,
    BURN_RESTART_STATE_INITIATED,
    BURN_RESTART_STATE_BLOCKED,
};


// structs

typedef struct _BURN_ENGINE_COMMAND
{
    int argc;
    LPWSTR* argv;
    DWORD cSecretArgs;
    int* rgSecretArgs;
    DWORD cUnknownArgs;
    int* rgUnknownArgs;
    BOOL fInvalidCommandLine;

    BURN_MODE mode;
    BURN_AU_PAUSE_ACTION automaticUpdates;
    BOOL fArpSystemComponent;
    BOOL fDisableSystemRestore;
    BOOL fInitiallyElevated;

    LPWSTR sczActiveParent;
    LPWSTR sczAncestors;
    LPWSTR sczIgnoreDependencies;

    LPWSTR sczSourceProcessPath;
    LPWSTR sczOriginalSource;
    LPWSTR sczEngineWorkingDirectory;

    DWORD dwLoggingAttributes;
    LPWSTR sczLogFile;
} BURN_ENGINE_COMMAND;

typedef struct _BURN_REDIRECTED_LOGGING_CONTEXT
{
    CRITICAL_SECTION csBuffer;
    LPSTR sczBuffer;
    HANDLE hPipe;
    HANDLE hLogEvent;
    HANDLE hFinishedEvent;
    HANDLE hThread;
} BURN_REDIRECTED_LOGGING_CONTEXT;

typedef struct _BURN_ENGINE_STATE
{
    // UX flow control
    BOOL fDetected;
    BOOL fPlanned;
    BOOL fQuit;
    //BOOL fSuspend;             // Is TRUE when UX made Suspend() call on core.
    //BOOL fForcedReboot;        // Is TRUE when UX made Reboot() call on core.
    //BOOL fCancelled;           // Is TRUE when UX return cancel on UX OnXXX() methods.
    //BOOL fReboot;              // Is TRUE when UX confirms OnRestartRequried().
    BOOL fRestart;               // Set TRUE when UX returns IDRESTART during Apply().

    // engine data
    BOOTSTRAPPER_COMMAND command;
    BURN_SECTION section;
    BURN_VARIABLES variables;
    BURN_CONDITION condition;
    BURN_SEARCHES searches;
    BURN_USER_EXPERIENCE userExperience;
    BURN_REGISTRATION registration;
    BURN_CONTAINERS containers;
    BURN_PAYLOADS payloads;
    BURN_PACKAGES packages;
    BURN_UPDATE update;
    BURN_APPROVED_EXES approvedExes;
    BURN_CACHE cache;
    BURN_DEPENDENCIES dependencies;
    BURN_EXTENSIONS extensions;

    HWND hMessageWindow;
    HANDLE hMessageWindowThread;

    BOOL fDisableRollback;
    BOOL fParallelCacheAndExecute;

    BURN_LOGGING log;

    BURN_PAYLOAD_GROUP layoutPayloads;

    BURN_PLAN plan;

    BURN_REDIRECTED_LOGGING_CONTEXT elevatedLoggingContext;
    HANDLE hUnelevatedLoggingThread;

    LPWSTR sczBundleEngineWorkingPath;
    BURN_PIPE_CONNECTION companionConnection;
    BURN_PIPE_CONNECTION embeddedConnection;

    CRITICAL_SECTION csRestartState;
    BOOL fRestarting;
    BURN_RESTART_STATE restartState;

    BOOL fCriticalShutdownInitiated;
    BURN_RESUME_MODE resumeMode;
    LPCWSTR wzRestartInitiatedPackageId;

    BURN_ENGINE_COMMAND internalCommand;
} BURN_ENGINE_STATE;

typedef struct _BURN_APPLY_CONTEXT
{
    CRITICAL_SECTION csApply;
    DWORD cOverallProgressTicks;
    HANDLE hCacheThread;
    DWORD dwCacheCheckpoint;
} BURN_APPLY_CONTEXT;

typedef BOOL (STDAPICALLTYPE *PFN_CREATEPROCESSW)(
    __in_opt LPCWSTR lpApplicationName,
    __inout_opt LPWSTR lpCommandLine,
    __in_opt LPSECURITY_ATTRIBUTES lpProcessAttributes,
    __in_opt LPSECURITY_ATTRIBUTES lpThreadAttributes,
    __in BOOL bInheritHandles,
    __in DWORD dwCreationFlags,
    __in_opt LPVOID lpEnvironment,
    __in_opt LPCWSTR lpCurrentDirectory,
    __in LPSTARTUPINFOW lpStartupInfo,
    __out LPPROCESS_INFORMATION lpProcessInformation
    );

typedef HRESULT (DAPI *PFN_PROCWAITFORCOMPLETION)(
    __in HANDLE hProcess,
    __in DWORD dwTimeout,
    __out DWORD* pReturnCode
    );


// function declarations

HRESULT CoreInitialize(
    __in BURN_ENGINE_STATE* pEngineState
    );
HRESULT CoreInitializeConstants(
    __in BURN_ENGINE_STATE* pEngineState
    );
HRESULT CoreSerializeEngineState(
    __in BURN_ENGINE_STATE* pEngineState,
    __inout BYTE** ppbBuffer,
    __inout SIZE_T* piBuffer
    );
HRESULT CoreQueryRegistration(
    __in BURN_ENGINE_STATE* pEngineState
    );
//HRESULT CoreDeserializeEngineState(
//    __in BURN_ENGINE_STATE* pEngineState,
//    __in_bcount(cbBuffer) BYTE* pbBuffer,
//    __in SIZE_T cbBuffer
//    );
HRESULT CoreDetect(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    );
HRESULT CorePlan(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BOOTSTRAPPER_ACTION action
    );
HRESULT CoreElevate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in WM_BURN reason,
    __in_opt HWND hwndParent
    );
HRESULT CoreApply(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_opt HWND hwndParent
    );
HRESULT CoreLaunchApprovedExe(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    );
void CoreQuit(
    __in BOOTSTRAPPER_ENGINE_CONTEXT* pEngineContext,
    __in DWORD dwExitCode
    );
HRESULT CoreSaveEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    );
LPCWSTR CoreRelationTypeToCommandLineString(
    __in BOOTSTRAPPER_RELATION_TYPE relationType
    );
HRESULT CoreCreateCleanRoomCommandLine(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzCleanRoomBundlePath,
    __in_z LPCWSTR wzCurrentProcessPath,
    __inout HANDLE* phFileAttached,
    __inout HANDLE* phFileSelf
    );
HRESULT CoreCreatePassthroughBundleCommandLine(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in BOOTSTRAPPER_COMMAND* pCommand
    );
HRESULT CoreCreateResumeCommandLine(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog
    );
HRESULT CoreCreateUpdateBundleCommandLine(
    __deref_inout_z LPWSTR* psczCommandLine,
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in BOOTSTRAPPER_COMMAND* pCommand
    );
HRESULT CoreAppendFileHandleAttachedToCommandLine(
    __in HANDLE hFileWithAttachedContainer,
    __out HANDLE* phExecutableFile,
    __deref_inout_z LPWSTR* psczCommandLine
    );
HRESULT CoreAppendFileHandleSelfToCommandLine(
    __in LPCWSTR wzExecutablePath,
    __out HANDLE* phExecutableFile,
    __deref_inout_z LPWSTR* psczCommandLine,
    __deref_inout_z_opt LPWSTR* psczObfuscatedCommandLine
    );
HRESULT CoreAppendSplashScreenWindowToCommandLine(
    __in_opt HWND hwndSplashScreen,
    __deref_inout_z LPWSTR* psczCommandLine
    );
HRESULT CoreAppendEngineWorkingDirectoryToCommandLine(
    __in_z_opt LPCWSTR wzEngineWorkingDirectory,
    __deref_inout_z LPWSTR* psczCommandLine,
    __deref_inout_z_opt LPWSTR* psczObfuscatedCommandLine
    );
void CoreCleanup(
    __in BURN_ENGINE_STATE* pEngineState
    );
HRESULT CoreParseCommandLine(
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_PIPE_CONNECTION* pCompanionConnection,
    __in BURN_PIPE_CONNECTION* pEmbeddedConnection,
    __inout HANDLE* phSectionFile,
    __inout HANDLE* phSourceEngineFile
    );
void CoreUpdateRestartState(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BURN_RESTART_STATE restartState
    );
void CoreFunctionOverride(
    __in_opt PFN_CREATEPROCESSW pfnCreateProcessW,
    __in_opt PFN_PROCWAITFORCOMPLETION pfnProcWaitForCompletion
    );
HRESULT CoreCreateProcess(
    __in_opt LPCWSTR wzApplicationName,
    __inout_opt LPWSTR sczCommandLine,
    __in BOOL fInheritHandles,
    __in DWORD dwCreationFlags,
    __in_opt LPCWSTR wzCurrentDirectory,
    __in WORD wShowWindow,
    __out LPPROCESS_INFORMATION pProcessInformation
    );
HRESULT DAPI CoreWaitForProcCompletion(
    __in HANDLE hProcess,
    __in DWORD dwTimeout,
    __out_opt DWORD* pdwReturnCode
    );
HRESULT DAPI CoreCloseElevatedLoggingThread(
    __in BURN_ENGINE_STATE* pEngineState
    );
HRESULT DAPI CoreWaitForUnelevatedLoggingThread(
    __in HANDLE hUnelevatedLoggingThread
    );
void DAPI CoreBootstrapperEngineActionUninitialize(
    __in BOOTSTRAPPER_ENGINE_ACTION* pAction
    );

#if defined(__cplusplus)
}
#endif
