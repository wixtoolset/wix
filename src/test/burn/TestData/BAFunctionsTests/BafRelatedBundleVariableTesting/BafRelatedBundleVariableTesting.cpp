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
        __in_z LPCWSTR wzBundleCode,
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

        hr = BalGetRelatedBundleVariable(wzBundleCode, STRING_VARIABLE, &wzValue);

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Retrieved related bundle variable with BAFunctions: AString = %ws, Error: 0x%x", wzValue, hr);

        hr = BalGetRelatedBundleVariable(wzBundleCode, NUMBER_VARIABLE, &wzValue);

        BalLog(BOOTSTRAPPER_LOG_LEVEL_STANDARD, "Retrieved related bundle variable with BAFunctions: ANumber = %ws, Error: 0x%x", wzValue, hr);

        hr = __super::OnDetectRelatedBundle(wzBundleCode, relationType, wzBundleTag, fPerMachine, wzVersion, fMissingFromCache, pfCancel);

        ReleaseStr(wzValue);
        return hr;
    }

private:


public:
    //
    // Constructor - initialize member variables.
    //
    CBafRelatedBundleVariableTesting(
        __in HMODULE hModule
        ) : CBalBaseBAFunctions(hModule)
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

    BalInitialize(pArgs->pEngine);

    pBAFunctions = new CBafRelatedBundleVariableTesting(hModule);
    ExitOnNull(pBAFunctions, hr, E_OUTOFMEMORY, "Failed to create new CBafRelatedBundleVariableTesting object.");

    hr = pBAFunctions->OnCreate(pArgs->pEngine, pArgs->pCommand);
    ExitOnFailure(hr, "Failed to create BA function");

    pResults->pfnBAFunctionsProc = BalBaseBAFunctionsProc;
    pResults->pvBAFunctionsProcContext = pBAFunctions;
    pBAFunctions = NULL;

LExit:
    ReleaseObject(pBAFunctions);

    return hr;
}
