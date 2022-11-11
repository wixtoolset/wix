// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BalBaseBAFunctions.h"
#include "BalBaseBAFunctionsProc.h"

const LPCWSTR STRING_VARIABLE = L"AString";
const LPCWSTR NUMBER_VARIABLE = L"ANumber";

class CBafRelatedBundleVariableTesting : public CBalBaseBAFunctions
{
public: // IBAFunctions


public: //IBootstrapperApplication
    virtual STDMETHODIMP OnDetectRelatedBundle(
        __in_z LPCWSTR wzBundleId,
        __in BOOTSTRAPPER_RELATION_TYPE relationType,
        __in_z LPCWSTR wzBundleTag,
        __in BOOL fPerMachine,
        __in LPCWSTR wzVersion,
        __in BOOL fMissingFromCache,
        __inout BOOL* pfCancel
        )
    {

        HRESULT hr = S_OK;
        LPWSTR wzValue = NULL;

        hr = BalGetRelatedBundleVariable(wzBundleId, STRING_VARIABLE, &wzValue);

        ExitOnFailure(hr, "Failed to get related bundle string variable.");

        if (wzValue)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Retrieved related bundle variable with BAFunctions: AString = %ws", wzValue);
        }

        hr = BalGetRelatedBundleVariable(wzBundleId, NUMBER_VARIABLE, &wzValue);

        if (wzValue)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Retrieved related bundle variable with BAFunctions: ANumber = %ws", wzValue);
        }

        hr = __super::OnDetectRelatedBundle(wzBundleId, relationType, wzBundleTag, fPerMachine, wzVersion, fMissingFromCache, pfCancel);
    LExit:
        ReleaseStr(wzValue);
        return hr;        
    }
private:
 

public:
    //
    // Constructor - initialize member variables.
    //
    CBafRelatedBundleVariableTesting(
        __in HMODULE hModule,
        __in IBootstrapperEngine* pEngine,
        __in const BA_FUNCTIONS_CREATE_ARGS* pArgs
        ) : CBalBaseBAFunctions(hModule, pEngine, pArgs)
    {
    }

    //
    // Destructor - release member variables.
    //
    ~CBafRelatedBundleVariableTesting()
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
    CBafRelatedBundleVariableTesting* pBAFunctions = NULL;
    IBootstrapperEngine* pEngine = NULL;

    hr = BalInitializeFromCreateArgs(pArgs->pBootstrapperCreateArgs, &pEngine);
    ExitOnFailure(hr, "Failed to initialize Bal.");

    pBAFunctions = new CBafRelatedBundleVariableTesting(hModule, pEngine, pArgs);
    ExitOnNull(pBAFunctions, hr, E_OUTOFMEMORY, "Failed to create new CBafRelatedBundleVariableTesting object.");

    pResults->pfnBAFunctionsProc = BalBaseBAFunctionsProc;
    pResults->pvBAFunctionsProcContext = pBAFunctions;
    pBAFunctions = NULL;

LExit:
    ReleaseObject(pBAFunctions);
    ReleaseObject(pEngine);

    return hr;
}
