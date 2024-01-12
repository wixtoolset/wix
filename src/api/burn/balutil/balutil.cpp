// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const DWORD VARIABLE_GROW_FACTOR = 80;
static DWORD vdwDebuggerCheck = 0;
static IBootstrapperEngine* vpEngine = NULL;

static HRESULT ParseCommandLine(
    __inout_z LPWSTR *psczPipeBaseName,
    __inout_z LPWSTR *psczPipeSecret,
    __out DWORD64 *pqwEngineAPIVersion
    );
static HRESULT ConnectToEngine(
    __in_z LPCWSTR wzPipeBaseName,
    __in_z LPCWSTR wzPipeSecret,
    __out HANDLE *phBAPipe,
    __out HANDLE *phEnginePipe
    );
static HRESULT ConnectAndVerify(
    __in_z LPCWSTR wzPipeName,
    __in_z LPCWSTR wzPipeSecret,
    __in DWORD cbPipeSecret,
    __out HANDLE *phPipe
    );
static HRESULT PumpMessages(
    __in HANDLE hPipe,
    __in IBootstrapperApplication* pApplication,
    __in IBootstrapperEngine* pEngine
    );
static void MsgProc(
    __in BOOTSTRAPPER_APPLICATION_MESSAGE messageType,
    __in_bcount(cbData) LPVOID pvData,
    __in DWORD cbData,
    __in IBootstrapperApplication* pApplication,
    __in IBootstrapperEngine* pEngine
    );

// prototypes

DAPI_(void) BalInitialize(
    __in IBootstrapperEngine* pEngine
    )
{
    pEngine->AddRef();

    ReleaseObject(vpEngine);
    vpEngine = pEngine;
}

DAPI_(void) BalUninitialize()
{
    ReleaseNullObject(vpEngine);
}

DAPI_(HRESULT) BootstrapperApplicationRun(
    __in IBootstrapperApplication* pApplication
    )
{
    HRESULT hr = S_OK;
    BOOL fComInitialized = FALSE;
    DWORD64 qwEngineAPIVersion = 0;
    LPWSTR sczPipeBaseName = NULL;
    LPWSTR sczPipeSecret = NULL;
    HANDLE hBAPipe = INVALID_HANDLE_VALUE;
    HANDLE hEnginePipe = INVALID_HANDLE_VALUE;
    IBootstrapperEngine* pEngine = NULL;
    BOOL fInitializedBal = FALSE;

    // initialize COM
    hr = ::CoInitializeEx(NULL, COINIT_MULTITHREADED);
    ExitOnFailure(hr, "Failed to initialize COM.");
    fComInitialized = TRUE;

    hr = ParseCommandLine(&sczPipeBaseName, &sczPipeSecret, &qwEngineAPIVersion);
    BalExitOnFailure(hr, "Failed to parse command line.");

    // TODO: Validate the engine API version.

    hr = ConnectToEngine(sczPipeBaseName, sczPipeSecret, &hBAPipe, &hEnginePipe);
    BalExitOnFailure(hr, "Failed to connect to engine.");

    hr = BalBootstrapperEngineCreate(hEnginePipe, &pEngine);
    BalExitOnFailure(hr, "Failed to create bootstrapper engine.");

    BalInitialize(pEngine);
    fInitializedBal = TRUE;

    BootstrapperApplicationDebuggerCheck();

    hr = MsgPump(hBAPipe, pApplication, pEngine);
    BalExitOnFailure(hr, "Failed while pumping messages.");

LExit:
    if (fInitializedBal)
    {
        BalUninitialize();
    }

    ReleaseNullObject(pEngine);
    ReleasePipeHandle(hEnginePipe);
    ReleasePipeHandle(hBAPipe);
    ReleaseStr(sczPipeSecret);
    ReleaseStr(sczPipeBaseName);

    if (fComInitialized)
    {
        ::CoUninitialize();
    }

    return hr;
}

DAPI_(VOID) BootstrapperApplicationDebuggerCheck()
{
    HRESULT hr = S_OK;
    HKEY hk = NULL;
    BOOL fDebug = FALSE;
    LPWSTR sczDebugBootstrapperApplications = NULL;
    LPWSTR sczDebugBootstrapperApplication = NULL;
    LPWSTR sczModulePath = NULL;
    LPCWSTR wzModuleFilename = NULL;
    WCHAR wzMessage[1024] = { };

    if (0 == vdwDebuggerCheck)
    {
        ++vdwDebuggerCheck;

        hr = RegOpen(HKEY_LOCAL_MACHINE, L"System\\CurrentControlSet\\Control\\Session Manager\\Environment", KEY_QUERY_VALUE, &hk);
        if (SUCCEEDED(hr))
        {
            hr = RegReadString(hk, L"WixDebugBootstrapperApplications", &sczDebugBootstrapperApplications);
            if (SUCCEEDED(hr) && sczDebugBootstrapperApplications && *sczDebugBootstrapperApplications &&
                sczDebugBootstrapperApplications[0] != L'0' && !sczDebugBootstrapperApplications[1])
            {
                hr = PathForCurrentProcess(&sczModulePath, NULL);
                if (SUCCEEDED(hr) && sczModulePath && *sczModulePath)
                {
                    wzModuleFilename = PathFile(sczModulePath);
                    if (wzModuleFilename)
                    {
                        fDebug = TRUE;
                    }
                }
            }
            else
            {
                hr = RegReadString(hk, L"WixDebugBootstrapperApplication", &sczDebugBootstrapperApplication);
                if (SUCCEEDED(hr) && sczDebugBootstrapperApplication && *sczDebugBootstrapperApplication)
                {
                    hr = PathForCurrentProcess(&sczModulePath, NULL);
                    if (SUCCEEDED(hr) && sczModulePath && *sczModulePath)
                    {
                        wzModuleFilename = PathFile(sczModulePath);
                        if (wzModuleFilename && CSTR_EQUAL == ::CompareStringOrdinal(sczDebugBootstrapperApplication, -1, wzModuleFilename, -1, TRUE))
                        {
                            fDebug = TRUE;
                        }
                    }
                }
            }

            if (fDebug)
            {
                hr = ::StringCchPrintfW(wzMessage, countof(wzMessage), L"To debug the boostrapper application process %ls\n\nSet breakpoints and attach a debugger to process id: %d (0x%x)", wzModuleFilename, ::GetCurrentProcessId(), ::GetCurrentProcessId());

                if (SUCCEEDED(hr))
                {
                    ::MessageBoxW(NULL, wzMessage, L"WiX Bootstrapper Application", MB_SERVICE_NOTIFICATION | MB_TOPMOST | MB_ICONQUESTION | MB_OK | MB_SYSTEMMODAL);
                }
            }
        }
    }

    ReleaseRegKey(hk);
    ReleaseStr(sczModulePath);
    ReleaseStr(sczDebugBootstrapperApplication);
    ReleaseStr(sczDebugBootstrapperApplications);
}

DAPI_(HRESULT) BalManifestLoad(
    __in HMODULE hBootstrapperApplicationModule,
    __out IXMLDOMDocument** ppixdManifest
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;

    hr = PathRelativeToModule(&sczPath, BAL_MANIFEST_FILENAME, hBootstrapperApplicationModule);
    ExitOnFailure(hr, "Failed to get path to bootstrapper application manifest: %ls", BAL_MANIFEST_FILENAME);

    hr = XmlLoadDocumentFromFile(sczPath, ppixdManifest);
    ExitOnFailure(hr, "Failed to load bootstrapper application manifest '%ls' from path: %ls", BAL_MANIFEST_FILENAME, sczPath);

LExit:
    ReleaseStr(sczPath);
    return hr;
}


DAPI_(HRESULT) BalEvaluateCondition(
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = vpEngine->EvaluateCondition(wzCondition, pf);

LExit:
    return hr;
}


DAPI_(HRESULT) BalEscapeString(
    __in_z LPCWSTR wzIn,
    __inout LPWSTR* psczOut
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = BalEscapeStringFromEngine(vpEngine, wzIn, psczOut);

LExit:
    return hr;
}


DAPI_(HRESULT) BalEscapeStringFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzIn,
    __inout LPWSTR* psczOut
    )
{
    HRESULT hr = S_OK;
    SIZE_T cch = 0;

    if (*psczOut)
    {
        hr = StrMaxLength(*psczOut, &cch);
        ExitOnFailure(hr, "Failed to determine length of value.");
    }
    else
    {
        hr = ::StringCchLengthW(wzIn, STRSAFE_MAX_LENGTH, reinterpret_cast<size_t*>(&cch));
        ExitOnFailure(hr, "Failed to determine length of source.");

        cch = min(STRSAFE_MAX_LENGTH, cch + VARIABLE_GROW_FACTOR);
        hr = StrAlloc(psczOut, cch);
        ExitOnFailure(hr, "Failed to pre-allocate value.");
    }

    hr = pEngine->EscapeString(wzIn, *psczOut, &cch);
    if (E_MOREDATA == hr)
    {
        ++cch;

        hr = StrAllocSecure(psczOut, cch);
        ExitOnFailure(hr, "Failed to allocate value.");

        hr = pEngine->EscapeString(wzIn, *psczOut, &cch);
    }

LExit:
    return hr;
}


// The contents of psczOut may be sensitive, should keep encrypted and SecureZeroFree.
DAPI_(HRESULT) BalFormatString(
    __in_z LPCWSTR wzFormat,
    __inout LPWSTR* psczOut
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = BalFormatStringFromEngine(vpEngine, wzFormat, psczOut);

LExit:
    return hr;
}


// The contents of psczOut may be sensitive, should keep encrypted and SecureZeroFree.
DAPI_(HRESULT) BalFormatStringFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzFormat,
    __inout LPWSTR* psczOut
    )
{
    HRESULT hr = S_OK;
    SIZE_T cch = 0;

    if (*psczOut)
    {
        hr = StrMaxLength(*psczOut, &cch);
        ExitOnFailure(hr, "Failed to determine length of value.");
    }
    else
    {
        hr = ::StringCchLengthW(wzFormat, STRSAFE_MAX_LENGTH, reinterpret_cast<size_t*>(&cch));
        ExitOnFailure(hr, "Failed to determine length of source.");

        cch = min(STRSAFE_MAX_LENGTH, cch + VARIABLE_GROW_FACTOR);
        hr = StrAlloc(psczOut, cch);
        ExitOnFailure(hr, "Failed to pre-allocate value.");
    }

    hr = pEngine->FormatString(wzFormat, *psczOut, &cch);
    if (E_MOREDATA == hr)
    {
        ++cch;

        hr = StrAllocSecure(psczOut, cch);
        ExitOnFailure(hr, "Failed to allocate value.");

        hr = pEngine->FormatString(wzFormat, *psczOut, &cch);
    }

LExit:
    return hr;
}


// The contents of pllValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroMemory.
DAPI_(HRESULT) BalGetNumericVariable(
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = vpEngine->GetVariableNumeric(wzVariable, pllValue);

LExit:
    return hr;
}


DAPI_(HRESULT) BalSetNumericVariable(
    __in_z LPCWSTR wzVariable,
    __in LONGLONG llValue
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = vpEngine->SetVariableNumeric(wzVariable, llValue);

LExit:
    return hr;
}


DAPI_(BOOL) BalVariableExists(
    __in_z LPCWSTR wzVariable
    )
{
    HRESULT hr = S_OK;
    BOOL fExists = FALSE;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    fExists = BalVariableExistsFromEngine(vpEngine, wzVariable);

LExit:
    return fExists;
}


DAPI_(BOOL) BalVariableExistsFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzVariable
    )
{
    HRESULT hr = S_OK;
    SIZE_T cch = 0;

    hr = pEngine->GetVariableString(wzVariable, NULL, &cch);

    return E_NOTFOUND != hr;
}


// The contents of psczValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroFree.
DAPI_(HRESULT) BalGetStringVariable(
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = BalGetStringVariableFromEngine(vpEngine, wzVariable, psczValue);

LExit:
    return hr;
}


// The contents of psczValue may be sensitive, if variable is hidden should keep value encrypted and SecureZeroFree.
DAPI_(HRESULT) BalGetStringVariableFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    SIZE_T cch = 0;

    if (*psczValue)
    {
        hr = StrMaxLength(*psczValue, &cch);
        ExitOnFailure(hr, "Failed to determine length of value.");
    }
    else
    {
        cch = VARIABLE_GROW_FACTOR;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to pre-allocate value.");
    }

    hr = pEngine->GetVariableString(wzVariable, *psczValue, &cch);
    if (E_MOREDATA == hr)
    {
        ++cch;

        hr = StrAllocSecure(psczValue, cch);
        ExitOnFailure(hr, "Failed to allocate value.");

        hr = pEngine->GetVariableString(wzVariable, *psczValue, &cch);
    }

LExit:
    return hr;
}

DAPI_(HRESULT) BalSetStringVariable(
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in BOOL fFormatted
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = vpEngine->SetVariableString(wzVariable, wzValue, fFormatted);

LExit:
    return hr;
}


DAPI_(HRESULT) BalGetVersionVariable(
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = BalGetVersionVariableFromEngine(vpEngine, wzVariable, psczValue);

LExit:
    return hr;
}


DAPI_(HRESULT) BalGetVersionVariableFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;
    SIZE_T cch = 0;

    if (*psczValue)
    {
        hr = StrMaxLength(*psczValue, &cch);
        ExitOnFailure(hr, "Failed to determine length of value.");
    }
    else
    {
        cch = VARIABLE_GROW_FACTOR;
        hr = StrAlloc(psczValue, cch);
        ExitOnFailure(hr, "Failed to pre-allocate value.");
    }

    hr = pEngine->GetVariableVersion(wzVariable, *psczValue, &cch);
    if (E_MOREDATA == hr)
    {
        ++cch;

        hr = StrAllocSecure(psczValue, cch);
        ExitOnFailure(hr, "Failed to allocate value.");

        hr = pEngine->GetVariableVersion(wzVariable, *psczValue, &cch);
    }

LExit:
    return hr;
}

DAPI_(HRESULT) BalGetRelatedBundleVariable(
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
)
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = BalGetRelatedBundleVariableFromEngine(vpEngine, wzBundleId, wzVariable, psczValue);

LExit:
    return hr;
}

DAPI_(HRESULT) BalGetRelatedBundleVariableFromEngine(
    __in IBootstrapperEngine* pEngine,
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzVariable,
    __inout LPWSTR* psczValue
)
{
    HRESULT hr = S_OK;
    SIZE_T cch = 0;

    if (*psczValue)
    {
        hr = StrMaxLength(*psczValue, reinterpret_cast<DWORD_PTR*>(&cch));
        ExitOnFailure(hr, "Failed to determine length of value.");
    }

    hr = pEngine->GetRelatedBundleVariable(wzBundleId, wzVariable, *psczValue, &cch);
    if (E_MOREDATA == hr)
    {
        ++cch;

        hr = StrAllocSecure(psczValue, cch);
        ExitOnFailure(hr, "Failed to allocate value.");

        hr = pEngine->GetRelatedBundleVariable(wzBundleId, wzVariable, *psczValue, &cch);
    }

LExit:
    return hr;
}

DAPI_(HRESULT) BalSetVersionVariable(
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    )
{
    HRESULT hr = S_OK;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = vpEngine->SetVariableVersion(wzVariable, wzValue);

LExit:
    return hr;
}


DAPIV_(HRESULT) BalLog(
    __in BOOTSTRAPPER_LOG_LEVEL level,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    va_start(args, szFormat);
    hr = BalLogArgs(level, szFormat, args);
    va_end(args);

LExit:
    return hr;
}


DAPI_(HRESULT) BalLogArgs(
    __in BOOTSTRAPPER_LOG_LEVEL level,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    HRESULT hr = S_OK;
    LPSTR sczFormattedAnsi = NULL;
    LPWSTR sczMessage = NULL;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = StrAnsiAllocFormattedArgs(&sczFormattedAnsi, szFormat, args);
    ExitOnFailure(hr, "Failed to format log string.");

    hr = StrAllocStringAnsi(&sczMessage, sczFormattedAnsi, 0, CP_UTF8);
    ExitOnFailure(hr, "Failed to convert log string to Unicode.");

    hr = vpEngine->Log(level, sczMessage);

LExit:
    ReleaseStr(sczMessage);
    ReleaseStr(sczFormattedAnsi);
    return hr;
}


DAPIV_(HRESULT) BalLogError(
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    va_start(args, szFormat);
    hr = BalLogErrorArgs(hrError, szFormat, args);
    va_end(args);

LExit:
    return hr;
}


DAPI_(HRESULT) BalLogErrorArgs(
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    HRESULT hr = S_OK;
    LPSTR sczFormattedAnsi = NULL;
    LPWSTR sczMessage = NULL;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    hr = StrAnsiAllocFormattedArgs(&sczFormattedAnsi, szFormat, args);
    ExitOnFailure(hr, "Failed to format error log string.");

    hr = StrAllocFormatted(&sczMessage, L"Error 0x%08x: %S", hrError, sczFormattedAnsi);
    ExitOnFailure(hr, "Failed to prepend error number to error log string.");

    hr = vpEngine->Log(BOOTSTRAPPER_LOG_LEVEL_ERROR, sczMessage);

LExit:
    ReleaseStr(sczMessage);
    ReleaseStr(sczFormattedAnsi);
    return hr;
}

DAPIV_(HRESULT) BalLogId(
    __in BOOTSTRAPPER_LOG_LEVEL level,
    __in DWORD dwLogId,
    __in HMODULE hModule,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    va_start(args, hModule);
    hr = BalLogIdArgs(level, dwLogId, hModule, args);
    va_end(args);

LExit:
    return hr;
}

DAPI_(HRESULT) BalLogIdArgs(
    __in BOOTSTRAPPER_LOG_LEVEL level,
    __in DWORD dwLogId,
    __in HMODULE hModule,
    __in va_list args
    )
{

    HRESULT hr = S_OK;
    LPWSTR pwz = NULL;
    DWORD cch = 0;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BalInitialize() must be called first.");
    }

    // Get the string for the id.
#pragma prefast(push)
#pragma prefast(disable:25028)
#pragma prefast(disable:25068)
    cch = ::FormatMessageW(FORMAT_MESSAGE_ALLOCATE_BUFFER | FORMAT_MESSAGE_FROM_HMODULE,
        static_cast<LPCVOID>(hModule), dwLogId, 0, reinterpret_cast<LPWSTR>(&pwz), 0, &args);
#pragma prefast(pop)

    if (0 == cch)
    {
        ExitOnLastError(hr, "Failed to log id: %d", dwLogId);
    }

    if (2 <= cch && L'\r' == pwz[cch - 2] && L'\n' == pwz[cch - 1])
    {
        pwz[cch - 2] = L'\0'; // remove newline from message table.
    }

    hr = vpEngine->Log(level, pwz);

LExit:
    if (pwz)
    {
        ::LocalFree(pwz);
    }

    return hr;
}


static HRESULT ParseCommandLine(
    __inout_z LPWSTR *psczPipeBaseName,
    __inout_z LPWSTR *psczPipeSecret,
    __out DWORD64 *pqwEngineAPIVersion
    )
{
    HRESULT hr = S_OK;
    LPWSTR wzCommandLine = ::GetCommandLineW();
    int argc = 0;
    LPWSTR* argv = NULL;

    *pqwEngineAPIVersion = 0;

    hr = AppParseCommandLine(wzCommandLine, &argc, &argv);
    ExitOnFailure(hr, "Failed to parse command line.");

    // Skip the executable full path in argv[0].
    for (int i = 1; i < argc; ++i)
    {
        if (argv[i][0] == L'-')
        {
            if (CSTR_EQUAL == ::CompareStringOrdinal(&argv[i][1], -1, BOOTSTRAPPER_APPLICATION_COMMANDLINE_SWITCH_API_VERSION, -1, TRUE))
            {
                if (i + 1 >= argc)
                {
                    BalExitOnRootFailure(hr = E_INVALIDARG, "Must specify an api version.");
                }

                ++i;

                hr = StrStringToUInt64(argv[i], 0, pqwEngineAPIVersion);
                BalExitOnFailure(hr, "Failed to parse api version: %ls", argv[i]);
            }
            else if (CSTR_EQUAL == ::CompareStringOrdinal(&argv[i][1], -1, BOOTSTRAPPER_APPLICATION_COMMANDLINE_SWITCH_PIPE_NAME, -1, TRUE))
            {
                if (i + 2 >= argc)
                {
                    BalExitOnRootFailure(hr = E_INVALIDARG, "Must specify a pipe name and pipe secret.");
                }

                ++i;

                hr = StrAllocString(psczPipeBaseName, argv[i], 0);
                BalExitOnFailure(hr, "Failed to copy pipe name.");

                ++i;

                hr = StrAllocString(psczPipeSecret, argv[i], 0);
                BalExitOnFailure(hr, "Failed to copy pipe secret.");
            }
        }
        else
        {
            BalExitWithRootFailure(hr, E_INVALIDARG, "Invalid argument: %ls", argv[i]);
        }
    }

LExit:
    if (argv)
    {
        AppFreeCommandLineArgs(argv);
    }

    return hr;
}

static HRESULT ConnectToEngine(
    __in_z LPCWSTR wzPipeBaseName,
    __in_z LPCWSTR wzPipeSecret,
    __out HANDLE *phBAPipe,
    __out HANDLE *phEnginePipe
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczBAPipeName = NULL;
    LPWSTR sczEnginePipeName = NULL;
    HANDLE hBAPipe = INVALID_HANDLE_VALUE;
    HANDLE hEnginePipe = INVALID_HANDLE_VALUE;

    DWORD cbPipeSecret = lstrlenW(wzPipeSecret) * sizeof(WCHAR);

    hr = StrAllocFormatted(&sczBAPipeName, L"%ls%ls", wzPipeBaseName, L".BA");
    ExitOnFailure(hr, "Failed to allocate BA pipe name.");

    hr = StrAllocFormatted(&sczEnginePipeName, L"%ls%ls", wzPipeBaseName, L".BAEngine");
    ExitOnFailure(hr, "Failed to allocate BA engine pipe name.");

    hr = ConnectAndVerify(sczBAPipeName, wzPipeSecret, cbPipeSecret, &hBAPipe);
    BalExitOnFailure(hr, "Failed to connect to bootstrapper application pipe.");

    hr = ConnectAndVerify(sczEnginePipeName, wzPipeSecret, cbPipeSecret, &hEnginePipe);
    BalExitOnFailure(hr, "Failed to connect to engine pipe.");

    *phBAPipe = hBAPipe;
    hBAPipe = INVALID_HANDLE_VALUE;

    *phEnginePipe = hEnginePipe;
    hEnginePipe = INVALID_HANDLE_VALUE;

LExit:
    ReleasePipeHandle(hEnginePipe);
    ReleasePipeHandle(hBAPipe);
    ReleaseStr(sczEnginePipeName);
    ReleaseStr(sczBAPipeName);

    return hr;
}

static HRESULT ConnectAndVerify(
    __in_z LPCWSTR wzPipeName,
    __in_z LPCWSTR wzPipeSecret,
    __in DWORD cbPipeSecret,
    __out HANDLE *phPipe
    )
{
    HRESULT hr = S_OK;
    HRESULT hrConnect = S_OK;
    HANDLE hPipe = INVALID_HANDLE_VALUE;

    hr = PipeClientConnect(wzPipeName, &hPipe);
    BalExitOnFailure(hr, "Failed to connect to pipe.");

    hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(&cbPipeSecret), sizeof(cbPipeSecret));
    BalExitOnFailure(hr, "Failed to write secret size to pipe.");

    hr = FileWriteHandle(hPipe, reinterpret_cast<LPCBYTE>(wzPipeSecret), cbPipeSecret);
    BalExitOnFailure(hr, "Failed to write secret size to pipe.");

    FileReadHandle(hPipe, reinterpret_cast<LPBYTE>(&hrConnect), sizeof(hrConnect));
    BalExitOnFailure(hr, "Failed to read connect result from pipe.");

    BalExitOnFailure(hrConnect, "Failed connect result from pipe.");

    *phPipe = hPipe;
    hPipe = INVALID_HANDLE_VALUE;

LExit:
    ReleasePipeHandle(hPipe);

    return hr;
}
