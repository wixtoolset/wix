// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


class CBalBootstrapperEngine : public IBootstrapperEngine, public IMarshal
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

        if (::IsEqualIID(__uuidof(IBootstrapperEngine), riid))
        {
            *ppvObject = static_cast<IBootstrapperEngine*>(this);
        }
        else if (::IsEqualIID(IID_IMarshal, riid))
        {
            *ppvObject = static_cast<IMarshal*>(this);
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

public: // IBootstrapperEngine
    virtual STDMETHODIMP GetPackageCount(
        __out DWORD* pcPackages
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETPACKAGECOUNT_ARGS args = { };
        BAENGINE_GETPACKAGECOUNT_RESULTS results = { };

        ExitOnNull(pcPackages, hr, E_INVALIDARG, "pcPackages is required");

        args.cbSize = sizeof(args);

        results.cbSize = sizeof(results);

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_GETPACKAGECOUNT, &args, &results, m_pvBAEngineProcContext);

        *pcPackages = results.cPackages;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP GetVariableNumeric(
        __in_z LPCWSTR wzVariable,
        __out LONGLONG* pllValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETVARIABLENUMERIC_ARGS args = { };
        BAENGINE_GETVARIABLENUMERIC_RESULTS results = { };

        ExitOnNull(pllValue, hr, E_INVALIDARG, "pllValue is required");

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLENUMERIC, &args, &results, m_pvBAEngineProcContext);

        *pllValue = results.llValue;

    LExit:
        SecureZeroMemory(&results, sizeof(results));
        return hr;
    }

    virtual STDMETHODIMP GetVariableString(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout DWORD* pcchValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETVARIABLESTRING_ARGS args = { };
        BAENGINE_GETVARIABLESTRING_RESULTS results = { };

        ExitOnNull(pcchValue, hr, E_INVALIDARG, "pcchValue is required");

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);
        results.wzValue = wzValue;
        results.cchValue = *pcchValue;

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLESTRING, &args, &results, m_pvBAEngineProcContext);

        *pcchValue = results.cchValue;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP GetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout DWORD* pcchValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETVARIABLEVERSION_ARGS args = { };
        BAENGINE_GETVARIABLEVERSION_RESULTS results = { };

        ExitOnNull(pcchValue, hr, E_INVALIDARG, "pcchValue is required");

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;

        results.cbSize = sizeof(results);
        results.wzValue = wzValue;
        results.cchValue = *pcchValue;

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLEVERSION, &args, &results, m_pvBAEngineProcContext);

        *pcchValue = results.cchValue;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP FormatString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout DWORD* pcchOut
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_FORMATSTRING_ARGS args = { };
        BAENGINE_FORMATSTRING_RESULTS results = { };

        ExitOnNull(pcchOut, hr, E_INVALIDARG, "pcchOut is required");

        args.cbSize = sizeof(args);
        args.wzIn = wzIn;

        results.cbSize = sizeof(results);
        results.wzOut = wzOut;
        results.cchOut = *pcchOut;

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_FORMATSTRING, &args, &results, m_pvBAEngineProcContext);

        *pcchOut = results.cchOut;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP EscapeString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout DWORD* pcchOut
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_ESCAPESTRING_ARGS args = { };
        BAENGINE_ESCAPESTRING_RESULTS results = { };

        ExitOnNull(pcchOut, hr, E_INVALIDARG, "pcchOut is required");

        args.cbSize = sizeof(args);
        args.wzIn = wzIn;

        results.cbSize = sizeof(results);
        results.wzOut = wzOut;
        results.cchOut = *pcchOut;

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_ESCAPESTRING, &args, &results, m_pvBAEngineProcContext);

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
        BAENGINE_EVALUATECONDITION_ARGS args = { };
        BAENGINE_EVALUATECONDITION_RESULTS results = { };

        ExitOnNull(pf, hr, E_INVALIDARG, "pf is required");

        args.cbSize = sizeof(args);
        args.wzCondition = wzCondition;

        results.cbSize = sizeof(results);

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_EVALUATECONDITION, &args, &results, m_pvBAEngineProcContext);

        *pf = results.f;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP Log(
        __in BOOTSTRAPPER_LOG_LEVEL level,
        __in_z LPCWSTR wzMessage
        )
    {
        BAENGINE_LOG_ARGS args = { };
        BAENGINE_LOG_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.level = level;
        args.wzMessage = wzMessage;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_LOG, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP SendEmbeddedError(
        __in DWORD dwErrorCode,
        __in_z_opt LPCWSTR wzMessage,
        __in DWORD dwUIHint,
        __out int* pnResult
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_SENDEMBEDDEDERROR_ARGS args = { };
        BAENGINE_SENDEMBEDDEDERROR_RESULTS results = { };

        ExitOnNull(pnResult, hr, E_INVALIDARG, "pnResult is required");

        args.cbSize = sizeof(args);
        args.dwErrorCode = dwErrorCode;
        args.wzMessage = wzMessage;
        args.dwUIHint = dwUIHint;

        results.cbSize = sizeof(results);

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDERROR, &args, &results, m_pvBAEngineProcContext);

        *pnResult = results.nResult;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP SendEmbeddedProgress(
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallProgressPercentage,
        __out int* pnResult
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_SENDEMBEDDEDPROGRESS_ARGS args = { };
        BAENGINE_SENDEMBEDDEDPROGRESS_RESULTS results = { };

        ExitOnNull(pnResult, hr, E_INVALIDARG, "pnResult is required");

        args.cbSize = sizeof(args);
        args.dwProgressPercentage = dwProgressPercentage;
        args.dwOverallProgressPercentage = dwOverallProgressPercentage;

        results.cbSize = sizeof(results);

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDPROGRESS, &args, &results, m_pvBAEngineProcContext);

        *pnResult = results.nResult;

    LExit:
        return hr;
    }

    virtual STDMETHODIMP SetUpdate(
        __in_z_opt LPCWSTR wzLocalSource,
        __in_z_opt LPCWSTR wzDownloadSource,
        __in DWORD64 qwSize,
        __in BOOTSTRAPPER_UPDATE_HASH_TYPE hashType,
        __in_bcount_opt(cbHash) BYTE* rgbHash,
        __in DWORD cbHash
        )
    {
        BAENGINE_SETUPDATE_ARGS args = { };
        BAENGINE_SETUPDATE_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.wzLocalSource = wzLocalSource;
        args.wzDownloadSource = wzDownloadSource;
        args.qwSize = qwSize;
        args.hashType = hashType;
        args.rgbHash = rgbHash;
        args.cbHash = cbHash;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_SETUPDATE, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP SetLocalSource(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzPath
        )
    {
        BAENGINE_SETLOCALSOURCE_ARGS args = { };
        BAENGINE_SETLOCALSOURCE_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.wzPackageOrContainerId = wzPackageOrContainerId;
        args.wzPayloadId = wzPayloadId;
        args.wzPath = wzPath;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_SETLOCALSOURCE, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP SetDownloadSource(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzUrl,
        __in_z_opt LPCWSTR wzUser,
        __in_z_opt LPCWSTR wzPassword
        )
    {
        BAENGINE_SETDOWNLOADSOURCE_ARGS args = { };
        BAENGINE_SETDOWNLOADSOURCE_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.wzPackageOrContainerId = wzPackageOrContainerId;
        args.wzPayloadId = wzPayloadId;
        args.wzUrl = wzUrl;
        args.wzUser = wzUser;
        args.wzPassword = wzPassword;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_SETDOWNLOADSOURCE, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP SetVariableNumeric(
        __in_z LPCWSTR wzVariable,
        __in LONGLONG llValue
        )
    {
        BAENGINE_SETVARIABLENUMERIC_ARGS args = { };
        BAENGINE_SETVARIABLENUMERIC_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;
        args.llValue = llValue;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLENUMERIC, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP SetVariableString(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue,
        __in BOOL fFormatted
        )
    {
        BAENGINE_SETVARIABLESTRING_ARGS args = { };
        BAENGINE_SETVARIABLESTRING_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;
        args.wzValue = wzValue;
        args.fFormatted = fFormatted;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLESTRING, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP SetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue
        )
    {
        BAENGINE_SETVARIABLEVERSION_ARGS args = { };
        BAENGINE_SETVARIABLEVERSION_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.wzVariable = wzVariable;
        args.wzValue = wzValue;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLEVERSION, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP CloseSplashScreen()
    {
        BAENGINE_CLOSESPLASHSCREEN_ARGS args = { };
        BAENGINE_CLOSESPLASHSCREEN_RESULTS results = { };

        args.cbSize = sizeof(args);

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_CLOSESPLASHSCREEN, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP Detect(
        __in_opt HWND hwndParent
        )
    {
        BAENGINE_DETECT_ARGS args = { };
        BAENGINE_DETECT_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.hwndParent = hwndParent;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_DETECT, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP Plan(
        __in BOOTSTRAPPER_ACTION action
        )
    {
        BAENGINE_PLAN_ARGS args = { };
        BAENGINE_PLAN_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.action = action;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_PLAN, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP Elevate(
        __in_opt HWND hwndParent
        )
    {
        BAENGINE_ELEVATE_ARGS args = { };
        BAENGINE_ELEVATE_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.hwndParent = hwndParent;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_ELEVATE, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP Apply(
        __in_opt HWND hwndParent
        )
    {
        BAENGINE_APPLY_ARGS args = { };
        BAENGINE_APPLY_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.hwndParent = hwndParent;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_APPLY, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP Quit(
        __in DWORD dwExitCode
        )
    {
        BAENGINE_QUIT_ARGS args = { };
        BAENGINE_QUIT_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.dwExitCode = dwExitCode;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_QUIT, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP LaunchApprovedExe(
        __in_opt HWND hwndParent,
        __in_z LPCWSTR wzApprovedExeForElevationId,
        __in_z_opt LPCWSTR wzArguments,
        __in DWORD dwWaitForInputIdleTimeout
        )
    {
        BAENGINE_LAUNCHAPPROVEDEXE_ARGS args = { };
        BAENGINE_LAUNCHAPPROVEDEXE_RESULTS results = { };

        args.cbSize = sizeof(args);
        args.hwndParent = hwndParent;
        args.wzApprovedExeForElevationId = wzApprovedExeForElevationId;
        args.wzArguments = wzArguments;
        args.dwWaitForInputIdleTimeout = dwWaitForInputIdleTimeout;

        results.cbSize = sizeof(results);

        return m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_LAUNCHAPPROVEDEXE, &args, &results, m_pvBAEngineProcContext);
    }

    virtual STDMETHODIMP CompareVersions(
        __in_z LPCWSTR wzVersion1,
        __in_z LPCWSTR wzVersion2,
        __out int* pnResult
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_COMPAREVERSIONS_ARGS args = { };
        BAENGINE_COMPAREVERSIONS_RESULTS results = { };

        ExitOnNull(pnResult, hr, E_INVALIDARG, "pnResult is required");

        args.cbSize = sizeof(args);
        args.wzVersion1 = wzVersion1;
        args.wzVersion2 = wzVersion2;

        results.cbSize = sizeof(results);

        hr = m_pfnBAEngineProc(BOOTSTRAPPER_ENGINE_MESSAGE_COMPAREVERSIONS, &args, &results, m_pvBAEngineProcContext);

        *pnResult = results.nResult;

    LExit:
        return hr;
    }

public: // IMarshal
    virtual STDMETHODIMP GetUnmarshalClass(
        __in REFIID /*riid*/,
        __in_opt LPVOID /*pv*/,
        __in DWORD /*dwDestContext*/,
        __reserved LPVOID /*pvDestContext*/,
        __in DWORD /*mshlflags*/,
        __out LPCLSID /*pCid*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP GetMarshalSizeMax(
        __in REFIID riid,
        __in_opt LPVOID /*pv*/,
        __in DWORD dwDestContext,
        __reserved LPVOID /*pvDestContext*/,
        __in DWORD /*mshlflags*/,
        __out DWORD *pSize
        )
    {
        HRESULT hr = S_OK;

        // We only support marshaling the IBootstrapperEngine interface in-proc.
        if (__uuidof(IBootstrapperEngine) != riid)
        {
            // Skip logging the following message since it appears way too often in the log.
            // "Unexpected IID requested to be marshalled. BootstrapperEngineForApplication can only marshal the IBootstrapperEngine interface."
            ExitFunction1(hr = E_NOINTERFACE);
        }
        else if (0 == (MSHCTX_INPROC & dwDestContext))
        {
            hr = E_FAIL;
            ExitOnRootFailure(hr, "Cannot marshal IBootstrapperEngine interface out of proc.");
        }

        // E_FAIL is used because E_INVALIDARG is not a supported return value.
        ExitOnNull(pSize, hr, E_FAIL, "Invalid size output parameter is NULL.");

        // Specify enough size to marshal just the interface pointer across threads.
        *pSize = sizeof(LPVOID);

    LExit:
        return hr;
    }

    virtual STDMETHODIMP MarshalInterface(
        __in IStream* pStm,
        __in REFIID riid,
        __in_opt LPVOID pv,
        __in DWORD dwDestContext,
        __reserved LPVOID /*pvDestContext*/,
        __in DWORD /*mshlflags*/
        )
    {
        HRESULT hr = S_OK;
        IBootstrapperEngine *pThis = NULL;
        ULONG ulWritten = 0;

        // We only support marshaling the IBootstrapperEngine interface in-proc.
        if (__uuidof(IBootstrapperEngine) != riid)
        {
            // Skip logging the following message since it appears way too often in the log.
            // "Unexpected IID requested to be marshalled. BootstrapperEngineForApplication can only marshal the IBootstrapperEngine interface."
            ExitFunction1(hr = E_NOINTERFACE);
        }
        else if (0 == (MSHCTX_INPROC & dwDestContext))
        {
            hr = E_FAIL;
            ExitOnRootFailure(hr, "Cannot marshal IBootstrapperEngine interface out of proc.");
        }

        // "pv" may not be set, so we should us "this" otherwise.
        if (pv)
        {
            pThis = reinterpret_cast<IBootstrapperEngine*>(pv);
        }
        else
        {
            pThis = static_cast<IBootstrapperEngine*>(this);
        }

        // E_INVALIDARG is not a supported return value.
        ExitOnNull(pStm, hr, E_FAIL, "The marshaling stream parameter is NULL.");

        // Marshal the interface pointer in-proc as is.
        hr = pStm->Write(pThis, sizeof(pThis), &ulWritten);
        if (STG_E_MEDIUMFULL == hr)
        {
            ExitOnFailure(hr, "Failed to write the stream because the stream is full.");
        }
        else if (FAILED(hr))
        {
            // All other STG error must be converted into E_FAIL based on IMarshal documentation.
            hr = E_FAIL;
            ExitOnFailure(hr, "Failed to write the IBootstrapperEngine interface pointer to the marshaling stream.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP UnmarshalInterface(
        __in IStream* pStm,
        __in REFIID riid,
        __deref_out LPVOID* ppv
        )
    {
        HRESULT hr = S_OK;
        ULONG ulRead = 0;

        // We only support marshaling the engine in-proc.
        if (__uuidof(IBootstrapperEngine) != riid)
        {
            // Skip logging the following message since it appears way too often in the log.
            // "Unexpected IID requested to be marshalled. BootstrapperEngineForApplication can only marshal the IBootstrapperEngine interface."
            ExitFunction1(hr = E_NOINTERFACE);
        }

        // E_FAIL is used because E_INVALIDARG is not a supported return value.
        ExitOnNull(pStm, hr, E_FAIL, "The marshaling stream parameter is NULL.");
        ExitOnNull(ppv, hr, E_FAIL, "The interface output parameter is NULL.");

        // Unmarshal the interface pointer in-proc as is.
        hr = pStm->Read(*ppv, sizeof(LPVOID), &ulRead);
        if (FAILED(hr))
        {
            // All STG errors must be converted into E_FAIL based on IMarshal documentation.
            hr = E_FAIL;
            ExitOnFailure(hr, "Failed to read the IBootstrapperEngine interface pointer from the marshaling stream.");
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP ReleaseMarshalData(
        __in IStream* /*pStm*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP DisconnectObject(
        __in DWORD /*dwReserved*/
        )
    {
        return E_NOTIMPL;
    }

public:
    CBalBootstrapperEngine(
        __in PFN_BOOTSTRAPPER_ENGINE_PROC pfnBAEngineProc,
        __in_opt LPVOID pvBAEngineProcContext
        )
    {
        m_cReferences = 1;
        m_pfnBAEngineProc = pfnBAEngineProc;
        m_pvBAEngineProcContext = pvBAEngineProcContext;
    }

private:
    long m_cReferences;
    PFN_BOOTSTRAPPER_ENGINE_PROC m_pfnBAEngineProc;
    LPVOID m_pvBAEngineProcContext;
};

HRESULT BalBootstrapperEngineCreate(
    __in PFN_BOOTSTRAPPER_ENGINE_PROC pfnBAEngineProc,
    __in_opt LPVOID pvBAEngineProcContext,
    __out IBootstrapperEngine** ppBootstrapperEngine
    )
{
    HRESULT hr = S_OK;
    CBalBootstrapperEngine* pBootstrapperEngine = NULL;

    pBootstrapperEngine = new CBalBootstrapperEngine(pfnBAEngineProc, pvBAEngineProcContext);
    ExitOnNull(pBootstrapperEngine, hr, E_OUTOFMEMORY, "Failed to allocate new BalBootstrapperEngine object.");

    hr = pBootstrapperEngine->QueryInterface(IID_PPV_ARGS(ppBootstrapperEngine));
    ExitOnFailure(hr, "Failed to QI for IBootstrapperEngine from BalBootstrapperEngine object.");

LExit:
    ReleaseObject(pBootstrapperEngine);
    return hr;
}
