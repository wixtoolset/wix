// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBAFunctions.h"
#include "BalBaseBAFunctionsProc.h"

class CPrereqBaf : public CBalBaseBAFunctions
{
public: // IBAFunctions

public: //IBootstrapperApplication
    STDMETHODIMP OnCreate(
        __in IBootstrapperEngine* pEngine,
        __in BOOTSTRAPPER_COMMAND* pCommand
    )
    {
        HRESULT hr = S_OK;

        hr = __super::OnCreate(pEngine, pCommand);
        ExitOnFailure(hr, "CBalBaseBootstrapperApplication initialization failed.");

        hr = StrAllocString(&m_sczBARuntimeDirectory, pCommand->wzBootstrapperWorkingFolder, 0);
        ExitOnFailure(hr, "Failed to copy working folder");

    LExit:
        return hr;
    }

    virtual STDMETHODIMP OnDetectBegin(
        __in BOOL /*fCached*/,
        __in BOOTSTRAPPER_REGISTRATION_TYPE /*registrationType*/,
        __in DWORD /*cPackages*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        HRESULT hr = S_OK;

        hr = m_pEngine->SetVariableString(L"BARuntimeDirectory", m_sczBARuntimeDirectory, FALSE);
        ExitOnFailure(hr, "Failed to set BARuntimeDirectory");

    LExit:
        return hr;
    }

private:

public:
    //
    // Constructor - initialize member variables.
    //
    CPrereqBaf(
        __in HMODULE hModule
        ) : CBalBaseBAFunctions(hModule)
    {
        m_sczBARuntimeDirectory = NULL;
    }

    //
    // Destructor - release member variables.
    //
    ~CPrereqBaf()
    {
        ReleaseNullStr(m_sczBARuntimeDirectory);
    }

private:
    LPWSTR m_sczBARuntimeDirectory;
};


HRESULT WINAPI CreateBAFunctions(
    __in HMODULE hModule,
    __in const BA_FUNCTIONS_CREATE_ARGS* pArgs,
    __inout BA_FUNCTIONS_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    CPrereqBaf* pBAFunctions = NULL;

    BalInitialize(pArgs->pEngine);

    pBAFunctions = new CPrereqBaf(hModule);
    ExitOnNull(pBAFunctions, hr, E_OUTOFMEMORY, "Failed to create new CPrereqBaf object.");

    hr = pBAFunctions->OnCreate(pArgs->pEngine, pArgs->pCommand);
    ExitOnFailure(hr, "Failed to call OnCreate CPrereqBaf.");

    pResults->pfnBAFunctionsProc = BalBaseBAFunctionsProc;
    pResults->pvBAFunctionsProcContext = pBAFunctions;
    pBAFunctions = NULL;

LExit:
    ReleaseObject(pBAFunctions);

    return hr;
}
