// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR BA_PIPE_NAME_FORMAT_STRING = L"%ls.BA";
static const LPCWSTR ENGINE_PIPE_NAME_FORMAT_STRING = L"%ls.BAEngine";

// internal function declarations

static HRESULT CreateBootstrapperApplicationPipes(
    __in_z LPCWSTR wzBasePipeName,
    __out HANDLE* phBAPipe,
    __out HANDLE* phBAEnginePipe
);
static HRESULT CreateBootstrapperApplicationProcess(
    __in_z LPCWSTR wzBootstrapperApplicationPath,
    __in int nCmdShow,
    __in_z LPCWSTR wzPipeName,
    __in_z LPCWSTR wzSecret,
    __out HANDLE* phProcess
);
static void Disconnect(
    __in BURN_USER_EXPERIENCE* pUserExperience
);
static int FilterResult(
    __in DWORD dwAllowedResults,
    __in int nResult
    );
static HRESULT WaitForBootstrapperApplicationConnect(
    __in HANDLE hBAProcess,
    __in HANDLE hBAPipe,
    __in HANDLE hBAEnginePipe,
    __in_z LPCWSTR wzSecret
);
static HRESULT VerifyPipeSecret(
    __in HANDLE hPipe,
    __in_z LPCWSTR wzSecret
);


// function definitions

EXTERN_C HRESULT BootstrapperApplicationParseFromXml(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in IXMLDOMNode* pixnBundle
)
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixnUserExperienceNode = NULL;
    LPWSTR sczPrimaryId = NULL;
    LPWSTR sczSecondaryId = NULL;
    BOOL fFoundSecondary = FALSE;

    // select UX node
    hr = XmlSelectSingleNode(pixnBundle, L"UX", &pixnUserExperienceNode);
    if (S_FALSE == hr)
    {
        hr = E_NOTFOUND;
    }
    ExitOnFailure(hr, "Failed to select user experience node.");

    // @PrimaryPayloadId
    hr = XmlGetAttributeEx(pixnUserExperienceNode, L"PrimaryPayloadId", &sczPrimaryId);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get @PrimaryPayloadId.");

    // @SecondaryPayloadId
    hr = XmlGetAttributeEx(pixnUserExperienceNode, L"SecondaryPayloadId", &sczSecondaryId);
    ExitOnOptionalXmlQueryFailure(hr, fFoundSecondary, "Failed to get @SecondaryPayloadId.");

    // parse payloads
    hr = PayloadsParseFromXml(&pUserExperience->payloads, NULL, NULL, pixnUserExperienceNode);
    ExitOnFailure(hr, "Failed to parse user experience payloads.");

    // make sure we have at least one payload
    if (0 == pUserExperience->payloads.cPayloads)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Too few UX payloads.");
    }

    // Find the primary and secondary bootstrapper application payloads.
    for (DWORD i = 0; i < pUserExperience->payloads.cPayloads; ++i)
    {
        BURN_PAYLOAD* pPayload = pUserExperience->payloads.rgPayloads + i;

        if (!pUserExperience->pPrimaryExePayload && CSTR_EQUAL == ::CompareStringOrdinal(pPayload->sczKey, -1, sczPrimaryId, -1, FALSE))
        {
            pUserExperience->pPrimaryExePayload = pPayload;
        }
        else if (fFoundSecondary && !pUserExperience->pSecondaryExePayload && CSTR_EQUAL == ::CompareStringOrdinal(pPayload->sczKey, -1, sczSecondaryId, -1, FALSE))
        {
            pUserExperience->pSecondaryExePayload = pPayload;
        }
    }

    if (!pUserExperience->pPrimaryExePayload)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Failed to find primary bootstrapper application payload.");
    }

LExit:
    ReleaseStr(sczSecondaryId);
    ReleaseStr(sczPrimaryId);
    ReleaseObject(pixnUserExperienceNode);

    return hr;
}

EXTERN_C void BootstrapperApplicationUninitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience
)
{
    if (pUserExperience->pEngineContext)
    {
        BAEngineFreeContext(pUserExperience->pEngineContext);
        pUserExperience->pEngineContext = NULL;
    }

    ReleaseStr(pUserExperience->sczTempDirectory);
    PayloadsUninitialize(&pUserExperience->payloads);

    // clear struct
    memset(pUserExperience, 0, sizeof(BURN_USER_EXPERIENCE));
}

EXTERN_C HRESULT BootstrapperApplicationStart(
    __in BURN_ENGINE_STATE* pEngineState,
    __in BOOL fSecondary
)
{
    HRESULT hr = S_OK;
    LPWSTR sczBasePipeName = NULL;
    LPWSTR sczSecret = NULL;
    HANDLE hBAPipe = INVALID_HANDLE_VALUE;
    HANDLE hBAEnginePipe = INVALID_HANDLE_VALUE;
    BAENGINE_CONTEXT* pEngineContext = NULL;

    BURN_USER_EXPERIENCE* pUserExperience = &pEngineState->userExperience;
    BOOTSTRAPPER_COMMAND* pCommand = &pEngineState->command;
    LPCWSTR wzBootstrapperApplicationPath = fSecondary && pUserExperience->pSecondaryExePayload ? pUserExperience->pSecondaryExePayload->sczLocalFilePath : pUserExperience->pPrimaryExePayload->sczLocalFilePath;

    if (!wzBootstrapperApplicationPath)
    {
        hr = E_UNEXPECTED;
        ExitOnFailure(hr, "Failed to find bootstrapper application path.");
    }

    hr = BurnPipeCreateNameAndSecret(&sczBasePipeName, &sczSecret);
    ExitOnFailure(hr, "Failed to create bootstrapper application pipename and secret");

    hr = CreateBootstrapperApplicationPipes(sczBasePipeName, &hBAPipe, &hBAEnginePipe);
    ExitOnFailure(hr, "Failed to create bootstrapper application pipes");

    hr = CreateBootstrapperApplicationProcess(wzBootstrapperApplicationPath, pCommand->nCmdShow, sczBasePipeName, sczSecret, &pUserExperience->hBAProcess);
    ExitOnFailure(hr, "Failed to create bootstrapper application process: %ls", wzBootstrapperApplicationPath);

    hr = WaitForBootstrapperApplicationConnect(pUserExperience->hBAProcess, hBAPipe, hBAEnginePipe, sczSecret);
    ExitOnFailure(hr, "Failed while waiting for bootstrapper application to connect.");

    hr = BAEngineCreateContext(pEngineState, &pEngineContext);
    ExitOnFailure(hr, "Failed to create bootstrapper application engine context.");

    pUserExperience->pEngineContext = pEngineContext;
    pEngineContext = NULL;

    PipeRpcInitialize(&pUserExperience->hBARpcPipe, hBAPipe, TRUE);
    hBAPipe = INVALID_HANDLE_VALUE;

    hr = BAEngineStartListening(pUserExperience->pEngineContext, hBAEnginePipe);
    ExitOnFailure(hr, "Failed to start listening to bootstrapper application engine pipe.");

    hBAEnginePipe = INVALID_HANDLE_VALUE;

    hr = BACallbackOnCreate(pUserExperience, pCommand);
    ExitOnFailure(hr, "Failed to create bootstrapper application");

LExit:
    if (pEngineContext)
    {
        BAEngineFreeContext(pEngineContext);
        pEngineContext = NULL;
    }

    ReleasePipeHandle(hBAEnginePipe);
    ReleasePipeHandle(hBAPipe);
    ReleaseStr(sczSecret);
    ReleaseStr(sczBasePipeName);

    return hr;
}

EXTERN_C HRESULT BootstrapperApplicationStop(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __inout BOOL* pfReload
)
{
    HRESULT hr = S_OK;
    DWORD dwExitCode = ERROR_SUCCESS;

    BACallbackOnDestroy(pUserExperience, *pfReload);

    Disconnect(pUserExperience);

    if (pUserExperience->pEngineContext)
    {
        BAEngineStopListening(pUserExperience->pEngineContext);
    }

    if (pUserExperience->hBAProcess)
    {
        hr = AppWaitForSingleObject(pUserExperience->hBAProcess, INFINITE);

        ::GetExitCodeProcess(pUserExperience->hBAProcess, &dwExitCode);

        ReleaseHandle(pUserExperience->hBAProcess);
    }

    // If the bootstrapper application process has already requested to reload, no need
    // to check any further. But if the bootstrapper application process exited
    // with anything but success then fallback to the other bootstrapper application.
    // This should enable bootstrapper applications that fail to start due to missing
    // prerequisites to fallback to the prerequisite bootstrapper application to install
    // the necessary prerequisites.
    if (!*pfReload)
    {
        *pfReload = (ERROR_SUCCESS != dwExitCode);
    }

    return hr;
}

EXTERN_C int BootstrapperApplicationCheckExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    // Do not allow canceling while rolling back.
    if (fRollback && (IDCANCEL == nResult || IDABORT == nResult))
    {
        nResult = IDNOACTION;
    }
    else if (FAILED(pUserExperience->hrApplyError) && !fRollback) // if we failed cancel except not during rollback.
    {
        nResult = IDCANCEL;
    }

    nResult = FilterResult(dwAllowedResults, nResult);

    return nResult;
}

EXTERN_C HRESULT BootstrapperApplicationInterpretExecuteResult(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOL fRollback,
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    HRESULT hr = S_OK;

    // If we failed return that error unless this is rollback which should roll on.
    if (FAILED(pUserExperience->hrApplyError) && !fRollback)
    {
        hr = pUserExperience->hrApplyError;
    }
    else
    {
        int nCheckedResult = BootstrapperApplicationCheckExecuteResult(pUserExperience, fRollback, dwAllowedResults, nResult);
        hr = IDOK == nCheckedResult || IDNOACTION == nCheckedResult ? S_OK : IDCANCEL == nCheckedResult || IDABORT == nCheckedResult ? HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) : HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE);
    }

    return hr;
}

EXTERN_C HRESULT BootstrapperApplicationEnsureWorkingFolder(
    __in BOOL fElevated,
    __in BURN_CACHE* pCache,
    __deref_out_z LPWSTR* psczUserExperienceWorkingFolder
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;

    hr = CacheEnsureBaseWorkingFolder(fElevated, pCache, &sczWorkingFolder);
    ExitOnFailure(hr, "Failed to create working folder.");

    hr = StrAllocFormatted(psczUserExperienceWorkingFolder, L"%ls%ls\\", sczWorkingFolder, L".ba");
    ExitOnFailure(hr, "Failed to calculate the bootstrapper application working path.");

    hr = DirEnsureExists(*psczUserExperienceWorkingFolder, NULL);
    ExitOnFailure(hr, "Failed create bootstrapper application working folder.");

LExit:
    ReleaseStr(sczWorkingFolder);

    return hr;
}


EXTERN_C HRESULT BootstrapperApplicationRemove(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    HRESULT hr = S_OK;

    // Release any open file handles so we can try to recursively delete the temp folder.
    for (DWORD i = 0; i < pUserExperience->payloads.cPayloads; ++i)
    {
        BURN_PAYLOAD* pPayload = pUserExperience->payloads.rgPayloads + i;

        ReleaseFileHandle(pPayload->hLocalFile);
    }

    // Remove temporary UX directory
    if (pUserExperience->sczTempDirectory)
    {
        hr = DirEnsureDeleteEx(pUserExperience->sczTempDirectory, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
        TraceError(hr, "Could not delete bootstrapper application folder. Some files will be left in the temp folder.");
    }

//LExit:
    return hr;
}

EXTERN_C int BootstrapperApplicationSendError(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BOOTSTRAPPER_ERROR_TYPE errorType,
    __in_z_opt LPCWSTR wzPackageId,
    __in HRESULT hrCode,
    __in_z_opt LPCWSTR wzError,
    __in DWORD uiFlags,
    __in int nRecommendation
    )
{
    int nResult = nRecommendation;
    DWORD dwCode = HRESULT_CODE(hrCode);
    LPWSTR sczError = NULL;

    // If no error string was provided, try to get the error string from the HRESULT.
    if (!wzError)
    {
        if (SUCCEEDED(StrAllocFromError(&sczError, hrCode, NULL)))
        {
            wzError = sczError;
        }
    }

    BACallbackOnError(pUserExperience, errorType, wzPackageId, dwCode, wzError, uiFlags, 0, NULL, &nResult); // ignore return value.

    ReleaseStr(sczError);
    return nResult;
}

EXTERN_C void BootstrapperApplicationActivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    ::EnterCriticalSection(&pUserExperience->csEngineActive);
    AssertSz(!pUserExperience->fEngineActive, "Engine should have been deactivated before activating it.");
    pUserExperience->fEngineActive = TRUE;
    ::LeaveCriticalSection(&pUserExperience->csEngineActive);
}

EXTERN_C void BootstrapperApplicationDeactivateEngine(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    ::EnterCriticalSection(&pUserExperience->csEngineActive);
    AssertSz(pUserExperience->fEngineActive, "Engine should have been active before deactivating it.");
    pUserExperience->fEngineActive = FALSE;
    ::LeaveCriticalSection(&pUserExperience->csEngineActive);
}

EXTERN_C HRESULT BootstrapperApplicationEnsureEngineInactive(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    // Make a slight optimization here by ignoring the critical section, because all callers should have needed to enter it for their operation anyway.
    HRESULT hr = pUserExperience->fEngineActive ? HRESULT_FROM_WIN32(ERROR_BUSY) : S_OK;
    ExitOnRootFailure(hr, "Engine is active, cannot proceed.");

LExit:
    return hr;
}

EXTERN_C void BootstrapperApplicationExecuteReset(
    __in BURN_USER_EXPERIENCE* pUserExperience
    )
{
    pUserExperience->hrApplyError = S_OK;
    pUserExperience->hwndApply = NULL;
}

EXTERN_C void BootstrapperApplicationExecutePhaseComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in HRESULT hrResult
    )
{
    if (FAILED(hrResult))
    {
        pUserExperience->hrApplyError = hrResult;
    }
}


// internal function definitions

static HRESULT CreateBootstrapperApplicationPipes(
    __in_z LPCWSTR wzBasePipeName,
    __out HANDLE* phBAPipe,
    __out HANDLE* phBAEnginePipe
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPipeName = NULL;
    HANDLE hBAPipe = INVALID_HANDLE_VALUE;
    HANDLE hBAEnginePipe = INVALID_HANDLE_VALUE;

    // Create the bootstrapper application pipe.
    hr = StrAllocFormatted(&sczPipeName, BA_PIPE_NAME_FORMAT_STRING, wzBasePipeName);
    ExitOnFailure(hr, "Failed to allocate full name of bootstrapper pipe: %ls", wzBasePipeName);

    hr = PipeCreate(sczPipeName, NULL, &hBAPipe);
    ExitOnFailure(hr, "Failed to create cache pipe: %ls", sczPipeName);

    // Create the bootstrapper application's engine pipe.
    hr = StrAllocFormatted(&sczPipeName, ENGINE_PIPE_NAME_FORMAT_STRING, wzBasePipeName);
    ExitOnFailure(hr, "Failed to allocate full name of bootstrapper application engine pipe: %ls", wzBasePipeName);

    hr = PipeCreate(sczPipeName, NULL, &hBAEnginePipe);
    ExitOnFailure(hr, "Failed to create cache pipe: %ls", sczPipeName);

    *phBAEnginePipe = hBAEnginePipe;
    hBAEnginePipe = INVALID_HANDLE_VALUE;

    *phBAPipe = hBAPipe;
    hBAPipe = INVALID_HANDLE_VALUE;

LExit:
    ReleasePipeHandle(hBAEnginePipe);
    ReleasePipeHandle(hBAPipe);

    return hr;
}

static HRESULT CreateBootstrapperApplicationProcess(
    __in_z LPCWSTR wzBootstrapperApplicationPath,
    __in int nCmdShow,
    __in_z LPCWSTR wzPipeName,
    __in_z LPCWSTR wzSecret,
    __out HANDLE* phProcess
)
{
    HRESULT hr = S_OK;
    LPWSTR sczParameters = NULL;
    LPWSTR sczFullCommandLine = NULL;
    PROCESS_INFORMATION pi = { };

    hr = StrAllocFormatted(&sczParameters, L"-%ls %llu -%ls %ls %ls", BOOTSTRAPPER_APPLICATION_COMMANDLINE_SWITCH_API_VERSION, BOOTSTRAPPER_APPLICATION_API_VERSION, BOOTSTRAPPER_APPLICATION_COMMANDLINE_SWITCH_PIPE_NAME, wzPipeName, wzSecret);
    ExitOnFailure(hr, "Failed to allocate parameters for bootstrapper application process.");

    hr = StrAllocFormattedSecure(&sczFullCommandLine, L"\"%ls\" %ls", wzBootstrapperApplicationPath, sczParameters);
    ExitOnFailure(hr, "Failed to allocate full command-line for bootstrapper application process.");

    hr = CoreCreateProcess(wzBootstrapperApplicationPath, sczFullCommandLine, FALSE, 0, NULL, static_cast<WORD>(nCmdShow), &pi);
    ExitOnFailure(hr, "Failed to launch bootstrapper application process: %ls", sczFullCommandLine);

    *phProcess = pi.hProcess;
    pi.hProcess = NULL;

LExit:
    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);
    StrSecureZeroFreeString(sczFullCommandLine);
    StrSecureZeroFreeString(sczParameters);

    return hr;
}

static void Disconnect(
    __in BURN_USER_EXPERIENCE* pUserExperience
)
{
    if (PipeRpcInitialized(&pUserExperience->hBARpcPipe))
    {
        PipeWriteDisconnect(pUserExperience->hBARpcPipe.hPipe);

        PipeRpcUninitiailize(&pUserExperience->hBARpcPipe);
    }
}

static int FilterResult(
    __in DWORD dwAllowedResults,
    __in int nResult
    )
{
    if (IDNOACTION == nResult || IDERROR == nResult) // do nothing and errors pass through.
    {
    }
    else
    {
        switch (dwAllowedResults)
        {
        case MB_OK:
            nResult = IDOK;
            break;

        case MB_OKCANCEL:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDOK;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult || IDNO == nResult)
            {
                nResult = IDCANCEL;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_ABORTRETRYIGNORE:
            if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDIGNORE == nResult)
            {
                nResult = IDIGNORE;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_YESNO:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDYES;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult || IDNO == nResult)
            {
                nResult = IDNO;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_YESNOCANCEL:
            if (IDOK == nResult || IDYES == nResult)
            {
                nResult = IDYES;
            }
            else if (IDNO == nResult)
            {
                nResult = IDNO;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDCANCEL;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_RETRYCANCEL:
            if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case MB_CANCELTRYCONTINUE:
            if (IDCANCEL == nResult || IDABORT == nResult)
            {
                nResult = IDABORT;
            }
            else if (IDRETRY == nResult || IDTRYAGAIN == nResult)
            {
                nResult = IDRETRY;
            }
            else if (IDCONTINUE == nResult || IDIGNORE == nResult)
            {
                nResult = IDCONTINUE;
            }
            else
            {
                nResult = IDNOACTION;
            }
            break;

        case BURN_MB_RETRYTRYAGAIN: // custom return code.
            if (IDRETRY != nResult && IDTRYAGAIN != nResult)
            {
                nResult = IDNOACTION;
            }
            break;

        default:
            AssertSz(FALSE, "Unknown allowed results.");
            break;
        }
    }

    return nResult;
}

static HRESULT WaitForBootstrapperApplicationConnect(
    __in HANDLE hBAProcess,
    __in HANDLE hBAPipe,
    __in HANDLE hBAEnginePipe,
    __in_z LPCWSTR wzSecret
)
{
    HRESULT hr = S_OK;
    HANDLE hPipes[2] = { hBAPipe, hBAEnginePipe };

    for (DWORD i = 0; i < countof(hPipes); ++i)
    {
        HANDLE hPipe = hPipes[i];

        hr = PipeServerWaitForClientConnect(hBAProcess, hPipe);
        ExitOnFailure(hr, "Failed to wait for bootstrapper application to connect to pipe.");

        hr = VerifyPipeSecret(hPipe, wzSecret);
        ExitOnFailure(hr, "Failed to verify bootstrapper application pipe");
    }

LExit:
    return hr;
}

static HRESULT VerifyPipeSecret(
    __in HANDLE hPipe,
    __in_z LPCWSTR wzSecret
)
{
    HRESULT hr = S_OK;
    HRESULT hrResponse = S_OK;
    LPWSTR sczVerificationSecret = NULL;
    DWORD cbVerificationSecret = 0;

    // Read the verification secret.
    hr = FileReadHandle(hPipe, reinterpret_cast<LPBYTE>(&cbVerificationSecret), sizeof(cbVerificationSecret));
    ExitOnFailure(hr, "Failed to read size of verification secret from bootstrapper application pipe.");

    if (255 < cbVerificationSecret / sizeof(WCHAR))
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Verification secret from bootstrapper application is too big.");
    }

    hr = StrAlloc(&sczVerificationSecret, cbVerificationSecret / sizeof(WCHAR) + 1);
    ExitOnFailure(hr, "Failed to allocate buffer for bootstrapper application verification secret.");

    hr = FileReadHandle(hPipe, reinterpret_cast<LPBYTE>(sczVerificationSecret), cbVerificationSecret);
    ExitOnFailure(hr, "Failed to read verification secret from bootstrapper application pipe.");

    // Verify the secrets match.
    if (CSTR_EQUAL != ::CompareStringOrdinal(sczVerificationSecret, -1, wzSecret, -1, FALSE))
    {
        hrResponse = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
    }

    // Send the response.
    hr = FileWriteHandle(hPipe, reinterpret_cast<LPBYTE>(&hrResponse), sizeof(hrResponse));
    ExitOnFailure(hr, "Failed to write response to pipe.");

    if (FAILED(hrResponse))
    {
        hr = hrResponse;
        ExitOnRootFailure(hr, "Verification secret from bootstrapper application does not match.");
    }

LExit:

    ReleaseStr(sczVerificationSecret);

    return hr;
}
