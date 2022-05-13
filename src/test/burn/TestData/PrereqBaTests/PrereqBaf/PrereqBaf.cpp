// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBAFunctions.h"
#include "BalBaseBAFunctionsProc.h"

class CPrereqBaf : public CBalBaseBAFunctions
{
public: // IBAFunctions

public: //IBootstrapperApplication

    virtual STDMETHODIMP OnDetectBegin(
        __in BOOL /*fCached*/,
        __in BOOTSTRAPPER_REGISTRATION_TYPE /*registrationType*/,
        __in DWORD /*cPackages*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        HRESULT hr = S_OK;

        hr = m_pEngine->SetVariableString(L"BARuntimeDirectory", m_command.wzBootstrapperWorkingFolder, FALSE);
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
        __in HMODULE hModule,
        __in IBootstrapperEngine* pEngine,
        __in const BA_FUNCTIONS_CREATE_ARGS* pArgs
        ) : CBalBaseBAFunctions(hModule, pEngine, pArgs)
    {
    }

    //
    // Destructor - release member variables.
    //
    ~CPrereqBaf()
    {
    }

private:
};


HRESULT WINAPI CreateBAFunctions(
    __in HMODULE hModule,
    __in const BA_FUNCTIONS_CREATE_ARGS* pArgs,
    __inout BA_FUNCTIONS_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    CPrereqBaf* pBAFunctions = NULL;
    IBootstrapperEngine* pEngine = NULL;

    hr = BalInitializeFromCreateArgs(pArgs->pBootstrapperCreateArgs, &pEngine);
    ExitOnFailure(hr, "Failed to initialize Bal.");

    pBAFunctions = new CPrereqBaf(hModule, pEngine, pArgs);
    ExitOnNull(pBAFunctions, hr, E_OUTOFMEMORY, "Failed to create new CPrereqBaf object.");

    pResults->pfnBAFunctionsProc = BalBaseBAFunctionsProc;
    pResults->pvBAFunctionsProcContext = pBAFunctions;
    pBAFunctions = NULL;

LExit:
    ReleaseObject(pBAFunctions);
    ReleaseObject(pEngine);

    return hr;
}
