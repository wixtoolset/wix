// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBAFunctions.h"
#include "BalBaseBAFunctionsProc.h"

class CWixSampleBAFunctions : public CBalBaseBAFunctions
{
public: // IBootstrapperApplication
    virtual STDMETHODIMP OnDetectBegin(
        __in BOOL fInstalled,
        __in DWORD cPackages,
        __inout BOOL* pfCancel
        )
    {
        HRESULT hr = S_OK;

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running detect begin BA function. fInstalled=%d, cPackages=%u, fCancel=%d", fInstalled, cPackages, *pfCancel);

        //-------------------------------------------------------------------------------------------------
        // YOUR CODE GOES HERE
        BalExitOnFailure(hr, "Change this message to represent real error handling.");
        //-------------------------------------------------------------------------------------------------

    LExit:
        return hr;
    }

public: // IBAFunctions
    virtual STDMETHODIMP OnPlanBegin(
        __in DWORD cPackages,
        __inout BOOL* pfCancel
        )
    {
        HRESULT hr = S_OK;

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Running plan begin BA function. cPackages=%u, fCancel=%d", cPackages, *pfCancel);

        //-------------------------------------------------------------------------------------------------
        // YOUR CODE GOES HERE
        BalExitOnFailure(hr, "Change this message to represent real error handling.");
        //-------------------------------------------------------------------------------------------------

    LExit:
        return hr;
    }

public:
    //
    // Constructor - initialize member variables.
    //
    CWixSampleBAFunctions(
        __in HMODULE hModule,
        __in IBootstrapperEngine* pEngine,
        __in const BA_FUNCTIONS_CREATE_ARGS* pArgs
        ) : CBalBaseBAFunctions(hModule, pEngine, pArgs)
    {
    }

    //
    // Destructor - release member variables.
    //
    ~CWixSampleBAFunctions()
    {
    }
};


HRESULT WINAPI CreateBAFunctions(
    __in HMODULE hModule,
    __in const BA_FUNCTIONS_CREATE_ARGS* pArgs,
    __inout BA_FUNCTIONS_CREATE_RESULTS* pResults
    )
{
    HRESULT hr = S_OK;
    CWixSampleBAFunctions* pBAFunctions = NULL;
    IBootstrapperEngine* pEngine = NULL;

    // This is required to enable logging functions.
    hr = BalInitializeFromCreateArgs(pArgs->pBootstrapperCreateArgs, &pEngine);
    ExitOnFailure(hr, "Failed to initialize Bal.");

    pBAFunctions = new CWixSampleBAFunctions(hModule, pEngine, pArgs);
    ExitOnNull(pBAFunctions, hr, E_OUTOFMEMORY, "Failed to create new CWixSampleBAFunctions object.");

    pResults->pfnBAFunctionsProc = BalBaseBAFunctionsProc;
    pResults->pvBAFunctionsProcContext = pBAFunctions;
    pBAFunctions = NULL;

LExit:
    ReleaseObject(pBAFunctions);
    ReleaseObject(pEngine);

    return hr;
}
