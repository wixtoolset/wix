// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


class CBextBootstrapperExtensionEngine : public IBootstrapperExtensionEngine
{
public: // IUnknown
    virtual STDMETHODIMP QueryInterface(
        __in REFIID riid,
        __out LPVOID *ppvObject
        )
    {
        if (!ppvObject)
        {
            return E_INVALIDARG;
        }

        *ppvObject = NULL;

        if (::IsEqualIID(__uuidof(IBootstrapperExtensionEngine), riid))
        {
            *ppvObject = static_cast<IBootstrapperExtensionEngine*>(this);
        }
        else if (::IsEqualIID(IID_IUnknown, riid))
        {
            *ppvObject = reinterpret_cast<IUnknown*>(this);
        }
        else // no interface for requested iid
        {
            return E_NOINTERFACE;
        }

        AddRef();
        return S_OK;
    }

    virtual STDMETHODIMP_(ULONG) AddRef()
    {
        return ::InterlockedIncrement(&this->m_cReferences);
    }

    virtual STDMETHODIMP_(ULONG) Release()
    {
        long l = ::InterlockedDecrement(&this->m_cReferences);
        if (0 < l)
        {
            return l;
        }

        delete this;
        return 0;
    }

public: // IBootstrapperExtensionEngine
    virtual STDMETHODIMP EscapeString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout SIZE_T* pcchOut
        )
    {
        HRESULT hr = S_OK;
        BOOTSTRAPPER_EXTENSION_ENGINE_ESCAPESTRING_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_ESCAPESTRING_RESULTS results = { };

        ExitOnNull(pcchOut, hr, E_INVALIDARG, "pcchOut is required");

        args.cbSize = sizeof(args);
        args.wzIn = wzIn;

        results.cbSize = sizeof(results);
        results.wzOut = wzOut;
        results.cchOut = *pcchOut;

        hr = m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_ESCAPESTRING, &args, &results, m_pvBootstrapperExtensionEngineProcContext);

        *pcchOut = results.cchOut;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP EvaluateCondition(
        __in_z LPCWSTR wzCondition,
        __out BOOL* pf
        )
    {
        HRESULT hr = S_OK;
        BOOTSTRAPPER_EXTENSION_ENGINE_EVALUATECONDITION_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_EVALUATECONDITION_RESULTS results = { };

        ExitOnNull(pf, hr, E_INVALIDARG, "pf is required");

        args.cbSize = sizeof(args);
        args.wzCondition = wzCondition;

        results.cbSize = sizeof(results);

        hr = m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_EVALUATECONDITION, &args, &results, m_pvBootstrapperExtensionEngineProcContext);

        *pf = results.f;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP FormatString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout SIZE_T* pcchOut
        )
    {
        HRESULT hr = S_OK;
        BOOTSTRAPPER_EXTENSION_ENGINE_FORMATSTRING_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_FORMATSTRING_RESULTS results = { };

        ExitOnNull(pcchOut, hr, E_INVALIDARG, "pcchOut is required");

        args.cbSize = sizeof(args);
        args.wzIn = wzIn;

        results.cbSize = sizeof(results);
        results.wzOut = wzOut;
        results.cchOut = *pcchOut;

        hr = m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_FORMATSTRING, &args, &results, m_pvBootstrapperExtensionEngineProcContext);

        *pcchOut = results.cchOut;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP GetVariableNumeric(
        __in_z LPCWSTR wzVariable,
        __out LONGLONG* pllValue
        )
    {
        HRESULT hr = S_OK;
        BOOTSTRAPPER_EXTENSION_ENGINE_GETVARIABLENUMERIC_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_GETVARIABLENUMERIC_RESULTS results = { };

        ExitOnNull(pllValue, hr, E_INVALIDARG, "pllValue is required");

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);

        hr = m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_GETVARIABLENUMERIC, &args, &results, m_pvBootstrapperExtensionEngineProcContext);

        *pllValue = results.llValue;

    LExit:
        SecureZeroMemory(&results, sizeof(results));
        return hr;
    }

    virtual STDMETHODIMP GetVariableString(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout SIZE_T* pcchValue
        )
    {
        HRESULT hr = S_OK;
        BOOTSTRAPPER_EXTENSION_ENGINE_GETVARIABLESTRING_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_GETVARIABLESTRING_RESULTS results = { };

        ExitOnNull(pcchValue, hr, E_INVALIDARG, "pcchValue is required");

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);
        results.wzValue = wzValue;
        results.cchValue = *pcchValue;

        hr = m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_GETVARIABLESTRING, &args, &results, m_pvBootstrapperExtensionEngineProcContext);

        *pcchValue = results.cchValue;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP GetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout SIZE_T* pcchValue
        )
    {
        HRESULT hr = S_OK;
        BOOTSTRAPPER_EXTENSION_ENGINE_GETVARIABLEVERSION_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_GETVARIABLEVERSION_RESULTS results = { };

        ExitOnNull(pcchValue, hr, E_INVALIDARG, "pcchValue is required");

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);
        results.wzValue = wzValue;
        results.cchValue = *pcchValue;

        hr = m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_GETVARIABLEVERSION, &args, &results, m_pvBootstrapperExtensionEngineProcContext);

        *pcchValue = results.cchValue;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP Log(
        __in BOOTSTRAPPER_EXTENSION_LOG_LEVEL level,
        __in_z LPCWSTR wzMessage
        )
    {
        BOOTSTRAPPER_EXTENSION_ENGINE_LOG_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_LOG_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.level = level;
        args.wzMessage = wzMessage;

        results.cbSize = sizeof(results);

        return m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_LOG, &args, &results, m_pvBootstrapperExtensionEngineProcContext);
    }

    virtual STDMETHODIMP SetVariableNumeric(
        __in_z LPCWSTR wzVariable,
        __in LONGLONG llValue
        )
    {
        BOOTSTRAPPER_EXTENSION_ENGINE_SETVARIABLENUMERIC_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_SETVARIABLENUMERIC_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;
        args.llValue = llValue;

        results.cbSize = sizeof(results);

        return m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_SETVARIABLENUMERIC, &args, &results, m_pvBootstrapperExtensionEngineProcContext);
    }

    virtual STDMETHODIMP SetVariableString(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue,
        __in BOOL fFormatted
        )
    {
        BOOTSTRAPPER_EXTENSION_ENGINE_SETVARIABLESTRING_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_SETVARIABLESTRING_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;
        args.wzValue = wzValue;
        args.fFormatted = fFormatted;

        results.cbSize = sizeof(results);

        return m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_SETVARIABLESTRING, &args, &results, m_pvBootstrapperExtensionEngineProcContext);
    }

    virtual STDMETHODIMP SetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue
        )
    {
        BOOTSTRAPPER_EXTENSION_ENGINE_SETVARIABLEVERSION_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_SETVARIABLEVERSION_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;
        args.wzValue = wzValue;

        results.cbSize = sizeof(results);

        return m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_SETVARIABLEVERSION, &args, &results, m_pvBootstrapperExtensionEngineProcContext);
    }

    virtual STDMETHODIMP CompareVersions(
        __in_z LPCWSTR wzVersion1,
        __in_z LPCWSTR wzVersion2,
        __out int* pnResult
        )
    {
        HRESULT hr = S_OK;
        BOOTSTRAPPER_EXTENSION_ENGINE_COMPAREVERSIONS_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_COMPAREVERSIONS_RESULTS results = { };

        ExitOnNull(pnResult, hr, E_INVALIDARG, "pnResult is required");

        args.cbSize = sizeof(args);
        args.wzVersion1 = wzVersion1;
        args.wzVersion2 = wzVersion2;

        results.cbSize = sizeof(results);

        hr = m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_COMPAREVERSIONS, &args, &results, m_pvBootstrapperExtensionEngineProcContext);

        *pnResult = results.nResult;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP GetRelatedBundleVariable(
        __in_z LPCWSTR wzBundleId,
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout SIZE_T* pcchValue
    )
    {
        HRESULT hr = S_OK;
        BOOTSTRAPPER_EXTENSION_ENGINE_GETRELATEDBUNDLEVARIABLE_ARGS args = { };
        BOOTSTRAPPER_EXTENSION_ENGINE_GETRELATEDBUNDLEVARIABLE_RESULTS results = { };

        ExitOnNull(pcchValue, hr, E_INVALIDARG, "pcchValue is required");

        args.cbSize = sizeof(args);
        args.wzBundleId = wzBundleId;
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);
        results.wzValue = wzValue;
        results.cchValue = *pcchValue;

        hr = m_pfnBootstrapperExtensionEngineProc(BOOTSTRAPPER_EXTENSION_ENGINE_MESSAGE_GETRELATEDBUNDLEVARIABLE, &args, &results, m_pvBootstrapperExtensionEngineProcContext);

        *pcchValue = results.cchValue;

    LExit:
        return hr;
    }

public:
    CBextBootstrapperExtensionEngine(
        __in PFN_BOOTSTRAPPER_EXTENSION_ENGINE_PROC pfnBootstrapperExtensionEngineProc,
        __in_opt LPVOID pvBootstrapperExtensionEngineProcContext
        )
    {
        m_cReferences = 1;
        m_pfnBootstrapperExtensionEngineProc = pfnBootstrapperExtensionEngineProc;
        m_pvBootstrapperExtensionEngineProcContext = pvBootstrapperExtensionEngineProcContext;
    }

private:
    long m_cReferences;
    PFN_BOOTSTRAPPER_EXTENSION_ENGINE_PROC m_pfnBootstrapperExtensionEngineProc;
    LPVOID m_pvBootstrapperExtensionEngineProcContext;
};

HRESULT BextBootstrapperExtensionEngineCreate(
    __in PFN_BOOTSTRAPPER_EXTENSION_ENGINE_PROC pfnBootstrapperExtensionEngineProc,
    __in_opt LPVOID pvBootstrapperExtensionEngineProcContext,
    __out IBootstrapperExtensionEngine** ppEngineForExtension
    )
{
    HRESULT hr = S_OK;
    CBextBootstrapperExtensionEngine* pBootstrapperExtensionEngine = NULL;

    pBootstrapperExtensionEngine = new CBextBootstrapperExtensionEngine(pfnBootstrapperExtensionEngineProc, pvBootstrapperExtensionEngineProcContext);
    ExitOnNull(pBootstrapperExtensionEngine, hr, E_OUTOFMEMORY, "Failed to allocate new BextBootstrapperExtensionEngine object.");

    hr = pBootstrapperExtensionEngine->QueryInterface(IID_PPV_ARGS(ppEngineForExtension));
    ExitOnFailure(hr, "Failed to QI for IBootstrapperExtensionEngine from BextBootstrapperExtensionEngine object.");

LExit:
    ReleaseObject(pBootstrapperExtensionEngine);
    return hr;
}
