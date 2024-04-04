// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

class CBalBootstrapperEngine : public IBootstrapperEngine
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
            return m_pFreeThreadedMarshaler->QueryInterface(riid, ppvObject);
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
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;

        ExitOnNull(pcPackages, hr, E_INVALIDARG, "pcPackages is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetPackageCount args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetPackageCount results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_GETPACKAGECOUNT, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA GetPackageCount failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read value length from GetPackageCount results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, (DWORD*)&results.cPackages);
        ExitOnFailure(hr, "Failed to read value length from GetPackageCount results.");

        *pcPackages = results.cPackages;

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

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
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;
        LPWSTR sczValue = NULL;

        ExitOnNull(pllValue, hr, E_INVALIDARG, "pllValue is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzVariable = wzVariable;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetVariableNumeric args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVariable);
        ExitOnFailure(hr, "Failed to write variable name of GetVariableNumeric args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetVariableNumeric results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLENUMERIC, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA GetVariableNumeric failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read value length from GetVariableNumeric results.");

        hr = BuffReadNumber64(rpc.pbData, rpc.cbData, &iBuffer, (DWORD64*)&results.llValue);
        ExitOnFailure(hr, "Failed to read value length from GetVariableNumeric results.");

        *pllValue = results.llValue;

    LExit:
        ReleaseStr(sczValue);
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP GetVariableString(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout SIZE_T* pcchValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETVARIABLESTRING_ARGS args = { };
        BAENGINE_GETVARIABLESTRING_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;
        LPWSTR sczValue = NULL;

        ExitOnNull(pcchValue, hr, E_INVALIDARG, "pcchValue is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzVariable = wzVariable;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        results.cchValue = static_cast<DWORD>(*pcchValue);

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetVariableString args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVariable);
        ExitOnFailure(hr, "Failed to write variable name of GetVariableString args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetVariableString results.");

        hr = BuffWriteNumberToBuffer(&bufferResults, results.cchValue);
        ExitOnFailure(hr, "Failed to write API version of GetVariableString results value.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLESTRING, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA GetVariableString failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read value length from GetVariableString results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.cchValue);
        ExitOnFailure(hr, "Failed to read value length from GetVariableString results.");

        hr = BuffReadString(rpc.pbData, rpc.cbData, &iBuffer, &sczValue);
        ExitOnFailure(hr, "Failed to read value from GetVariableString results.");

        results.wzValue = sczValue;

        if (wzValue)
        {
            hr = ::StringCchCopyW(wzValue, *pcchValue, results.wzValue);
            if (E_INSUFFICIENT_BUFFER == hr)
            {
                hr = E_MOREDATA;
            }
        }
        else if (results.cchValue)
        {
            hr = E_MOREDATA;
        }

        *pcchValue = results.cchValue;
        ExitOnFailure(hr, "Failed to copy value from GetVariableString results.");

    LExit:
        ReleaseStr(sczValue);
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP GetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __out_ecount_opt(*pcchValue) LPWSTR wzValue,
        __inout SIZE_T* pcchValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_GETVARIABLEVERSION_ARGS args = { };
        BAENGINE_GETVARIABLEVERSION_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;
        LPWSTR sczValue = NULL;

        ExitOnNull(pcchValue, hr, E_INVALIDARG, "pcchValue is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzVariable = wzVariable;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        hr = DutilSizetToDword(*pcchValue, &results.cchValue);
        ExitOnFailure(hr, "Failed to convert pcchValue to DWORD.");

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetVariableVersion args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVariable);
        ExitOnFailure(hr, "Failed to write variable name of GetVariableVersion args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetVariableVersion results.");

        hr = BuffWriteNumberToBuffer(&bufferResults, results.cchValue);
        ExitOnFailure(hr, "Failed to write API version of GetVariableVersion results value.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_GETVARIABLEVERSION, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA GetVariableVersion failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read value length from GetVariableVersion results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.cchValue);
        ExitOnFailure(hr, "Failed to read value length from GetVariableVersion results.");

        hr = BuffReadString(rpc.pbData, rpc.cbData, &iBuffer, &sczValue);
        ExitOnFailure(hr, "Failed to read value from GetVariableVersion results.");

        results.wzValue = sczValue;

        if (wzValue)
        {
            hr = ::StringCchCopyW(wzValue, *pcchValue, results.wzValue);
            if (E_INSUFFICIENT_BUFFER == hr)
            {
                hr = E_MOREDATA;
            }
        }
        else if (results.cchValue)
        {
            hr = E_MOREDATA;
        }

        *pcchValue = results.cchValue;
        ExitOnFailure(hr, "Failed to copy value from GetVariableVersion results.");

    LExit:
        ReleaseStr(sczValue);
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

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
        BAENGINE_GETRELATEDBUNDLEVARIABLE_ARGS args = { };
        BAENGINE_GETRELATEDBUNDLEVARIABLE_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;
        LPWSTR sczValue = NULL;

        ExitOnNull(pcchValue, hr, E_INVALIDARG, "pcchValue is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzBundleId = wzBundleId;
        args.wzVariable = wzVariable;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        hr = DutilSizetToDword(*pcchValue, &results.cchValue);
        ExitOnFailure(hr, "Failed to convert pcchValue to DWORD.");

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetRelatedBundleVariable args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzBundleId);
        ExitOnFailure(hr, "Failed to write bundle id of GetRelatedBundleVariable args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVariable);
        ExitOnFailure(hr, "Failed to write variable name of GetRelatedBundleVariable args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of GetRelatedBundleVariable results.");

        hr = BuffWriteNumberToBuffer(&bufferResults, results.cchValue);
        ExitOnFailure(hr, "Failed to write API version of GetRelatedBundleVariable results value.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_GETRELATEDBUNDLEVARIABLE, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA GetRelatedBundleVariable failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read value length from GetRelatedBundleVariable results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.cchValue);
        ExitOnFailure(hr, "Failed to read value length from GetRelatedBundleVariable results.");

        hr = BuffReadString(rpc.pbData, rpc.cbData, &iBuffer, &sczValue);
        ExitOnFailure(hr, "Failed to read value from GetRelatedBundleVariable results.");

        results.wzValue = sczValue;

        if (wzValue)
        {
            hr = ::StringCchCopyW(wzValue, *pcchValue, results.wzValue);
            if (E_INSUFFICIENT_BUFFER == hr)
            {
                hr = E_MOREDATA;
            }
        }
        else if (results.cchValue)
        {
            hr = E_MOREDATA;
        }

        *pcchValue = results.cchValue;
        ExitOnFailure(hr, "Failed to copy value from GetRelatedBundleVariable results.");

    LExit:
        ReleaseStr(sczValue);
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP FormatString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout SIZE_T* pcchOut
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_FORMATSTRING_ARGS args = { };
        BAENGINE_FORMATSTRING_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;
        LPWSTR sczOut = NULL;

        ExitOnNull(pcchOut, hr, E_INVALIDARG, "pcchOut is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzIn = wzIn;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        hr = DutilSizetToDword(*pcchOut, &results.cchOut);
        ExitOnFailure(hr, "Failed to convert pcchOut to DWORD.");

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of FormatString args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzIn);
        ExitOnFailure(hr, "Failed to write string to format of FormatString args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of FormatString results.");

        hr = BuffWriteNumberToBuffer(&bufferResults, results.cchOut);
        ExitOnFailure(hr, "Failed to write format string maximum size of FormatString results value.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_FORMATSTRING, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA FormatString failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read size from FormatString results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.cchOut);
        ExitOnFailure(hr, "Failed to read formatted string length from FormatString results.");

        hr = BuffReadString(rpc.pbData, rpc.cbData, &iBuffer, &sczOut);
        ExitOnFailure(hr, "Failed to read formatted string from FormatString results.");

        results.wzOut = sczOut;

        if (wzOut)
        {
            hr = ::StringCchCopyW(wzOut, *pcchOut, results.wzOut);
            if (E_INSUFFICIENT_BUFFER == hr)
            {
                hr = E_MOREDATA;
            }
        }
        else if (results.cchOut)
        {
            hr = E_MOREDATA;
        }

        *pcchOut = results.cchOut;
        ExitOnFailure(hr, "Failed to copy formatted string from FormatString results.");

    LExit:
        ReleaseStr(sczOut);
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP EscapeString(
        __in_z LPCWSTR wzIn,
        __out_ecount_opt(*pcchOut) LPWSTR wzOut,
        __inout SIZE_T* pcchOut
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_ESCAPESTRING_ARGS args = { };
        BAENGINE_ESCAPESTRING_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;
        LPWSTR sczOut = NULL;

        ExitOnNull(pcchOut, hr, E_INVALIDARG, "pcchOut is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzIn = wzIn;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        hr = DutilSizetToDword(*pcchOut, &results.cchOut);
        ExitOnFailure(hr, "Failed to convert pcchOut to DWORD.");

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of EscapeString args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzIn);
        ExitOnFailure(hr, "Failed to write string to escape of EscapeString args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of EscapeString results.");

        hr = BuffWriteNumberToBuffer(&bufferResults, results.cchOut);
        ExitOnFailure(hr, "Failed to write escape string maximum size of EscapeString results value.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_ESCAPESTRING, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA EscapeString failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read size from EscapeString results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.cchOut);
        ExitOnFailure(hr, "Failed to read escaped string length from EscapeString results.");

        hr = BuffReadString(rpc.pbData, rpc.cbData, &iBuffer, &sczOut);
        ExitOnFailure(hr, "Failed to read escaped string from EscapeString results.");

        results.wzOut = sczOut;

        if (wzOut)
        {
            hr = ::StringCchCopyW(wzOut, *pcchOut, results.wzOut);
            if (E_INSUFFICIENT_BUFFER == hr)
            {
                hr = E_MOREDATA;
            }
        }
        else if (results.cchOut)
        {
            hr = E_MOREDATA;
        }

        *pcchOut = results.cchOut;
        ExitOnFailure(hr, "Failed to copy escaped string from EscapeString results.");

    LExit:
        ReleaseStr(sczOut);
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

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
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;

        ExitOnNull(wzCondition, hr, E_INVALIDARG, "wzCondition is required");
        ExitOnNull(pf, hr, E_INVALIDARG, "pf is required");

        // Empty condition evaluates to true.
        if (!*wzCondition)
        {
            *pf = TRUE;
            ExitFunction();
        }

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzCondition = wzCondition;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of EvaluateCondition args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzCondition);
        ExitOnFailure(hr, "Failed to write condition of EvaluateCondition args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of EvaluateCondition results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_EVALUATECONDITION, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA EvaluateCondition failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read size from EvaluateCondition results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.f));
        ExitOnFailure(hr, "Failed to read result from EvaluateCondition results.");

        *pf = results.f;

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP Log(
        __in BOOTSTRAPPER_LOG_LEVEL level,
        __in_z LPCWSTR wzMessage
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_LOG_ARGS args = { };
        BAENGINE_LOG_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.level = level;
        args.wzMessage = wzMessage;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Log args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, args.level);
        ExitOnFailure(hr, "Failed to write level of Log args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzMessage);
        ExitOnFailure(hr, "Failed to write message of Log args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Log results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_LOG, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA Log failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
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
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;

        ExitOnNull(pnResult, hr, E_INVALIDARG, "pnResult is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.dwErrorCode = dwErrorCode;
        args.wzMessage = wzMessage;
        args.dwUIHint = dwUIHint;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SendEmbeddedError args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwErrorCode);
        ExitOnFailure(hr, "Failed to write error code of SendEmbeddedError args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzMessage);
        ExitOnFailure(hr, "Failed to write message of SendEmbeddedError args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwUIHint);
        ExitOnFailure(hr, "Failed to write UI hint of SendEmbeddedError args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SendEmbeddedError results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDERROR, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA SendEmbeddedError failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read size from SendEmbeddedError results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.nResult));
        ExitOnFailure(hr, "Failed to read result from SendEmbeddedError results.");

        *pnResult = results.nResult;

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

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
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;

        ExitOnNull(pnResult, hr, E_INVALIDARG, "pnResult is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.dwProgressPercentage = dwProgressPercentage;
        args.dwOverallProgressPercentage = dwOverallProgressPercentage;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SendEmbeddedProgress args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwProgressPercentage);
        ExitOnFailure(hr, "Failed to write progress of SendEmbeddedProgress args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwOverallProgressPercentage);
        ExitOnFailure(hr, "Failed to write overall progress of SendEmbeddedProgress args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SendEmbeddedProgress results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_SENDEMBEDDEDPROGRESS, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA SendEmbeddedProgress failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read size from SendEmbeddedProgress results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.nResult));
        ExitOnFailure(hr, "Failed to read result from SendEmbeddedProgress results.");

        *pnResult = results.nResult;

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP SetUpdate(
        __in_z_opt LPCWSTR wzLocalSource,
        __in_z_opt LPCWSTR wzDownloadSource,
        __in DWORD64 qwSize,
        __in BOOTSTRAPPER_UPDATE_HASH_TYPE hashType,
        __in_z_opt LPCWSTR wzHash,
        __in_z_opt LPCWSTR wzUpdatePackageId
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_SETUPDATE_ARGS args = { };
        BAENGINE_SETUPDATE_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzLocalSource = wzLocalSource;
        args.wzDownloadSource = wzDownloadSource;
        args.qwSize = qwSize;
        args.hashType = hashType;
        args.wzHash = wzHash;
        args.wzUpdatePackageId = wzUpdatePackageId;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetUpdate args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzLocalSource);
        ExitOnFailure(hr, "Failed to write local source of SetUpdate args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzDownloadSource);
        ExitOnFailure(hr, "Failed to write download source of SetUpdate args.");

        hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.qwSize);
        ExitOnFailure(hr, "Failed to write udpate size of SetUpdate args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, static_cast<DWORD>(args.hashType));
        ExitOnFailure(hr, "Failed to write hash type of SetUpdate args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzHash);
        ExitOnFailure(hr, "Failed to write hash of SetUpdate args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzHash);
        ExitOnFailure(hr, "Failed to write hash of SetUpdate args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzUpdatePackageId);
        ExitOnFailure(hr, "Failed to write update package id to SetUpdate args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetUpdate results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_SETUPDATE, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA SetUpdate failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP SetLocalSource(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzPath
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_SETLOCALSOURCE_ARGS args = { };
        BAENGINE_SETLOCALSOURCE_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzPackageOrContainerId = wzPackageOrContainerId;
        args.wzPayloadId = wzPayloadId;
        args.wzPath = wzPath;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetLocalSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
        ExitOnFailure(hr, "Failed to write package or container id of SetLocalSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
        ExitOnFailure(hr, "Failed to write payload id of SetLocalSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPath);
        ExitOnFailure(hr, "Failed to write path of SetLocalSource args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetLocalSource results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_SETLOCALSOURCE, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA SetLocalSource failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP SetDownloadSource(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR wzUrl,
        __in_z_opt LPCWSTR wzUser,
        __in_z_opt LPCWSTR wzPassword,
        __in_z_opt LPCWSTR wzAuthorizationHeader
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_SETDOWNLOADSOURCE_ARGS args = { };
        BAENGINE_SETDOWNLOADSOURCE_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzPackageOrContainerId = wzPackageOrContainerId;
        args.wzPayloadId = wzPayloadId;
        args.wzUrl = wzUrl;
        args.wzUser = wzUser;
        args.wzPassword = wzPassword;
        args.wzAuthorizationHeader = wzAuthorizationHeader;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetDownloadSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPackageOrContainerId);
        ExitOnFailure(hr, "Failed to write package or container id of SetDownloadSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPayloadId);
        ExitOnFailure(hr, "Failed to write payload id of SetDownloadSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzUrl);
        ExitOnFailure(hr, "Failed to write url of SetDownloadSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzUser);
        ExitOnFailure(hr, "Failed to write user of SetDownloadSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzPassword);
        ExitOnFailure(hr, "Failed to write password of SetDownloadSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzAuthorizationHeader);
        ExitOnFailure(hr, "Failed to write authorization header of SetDownloadSource args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetDownloadSource results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_SETDOWNLOADSOURCE, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA SetDownloadSource failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP SetVariableNumeric(
        __in_z LPCWSTR wzVariable,
        __in LONGLONG llValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_SETVARIABLENUMERIC_ARGS args = { };
        BAENGINE_SETVARIABLENUMERIC_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzVariable = wzVariable;
        args.llValue = llValue;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetVariableNumeric args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVariable);
        ExitOnFailure(hr, "Failed to write variable of SetVariableNumeric args.");

        hr = BuffWriteNumber64ToBuffer(&bufferArgs, static_cast<DWORD64>(args.llValue));
        ExitOnFailure(hr, "Failed to write value of SetVariableNumeric args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetVariableNumeric results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLENUMERIC, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA SetVariableNumeric failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP SetVariableString(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue,
        __in BOOL fFormatted
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_SETVARIABLESTRING_ARGS args = { };
        BAENGINE_SETVARIABLESTRING_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzVariable = wzVariable;
        args.wzValue = wzValue;
        args.fFormatted = fFormatted;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetVariableString args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVariable);
        ExitOnFailure(hr, "Failed to write variable of SetVariableString args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzValue);
        ExitOnFailure(hr, "Failed to write value of SetVariableString args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, args.fFormatted);
        ExitOnFailure(hr, "Failed to write formatted flag of SetVariableString args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetVariableString results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLESTRING, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA SetVariableString failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP SetVariableVersion(
        __in_z LPCWSTR wzVariable,
        __in_z_opt LPCWSTR wzValue
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_SETVARIABLEVERSION_ARGS args = { };
        BAENGINE_SETVARIABLEVERSION_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzVariable = wzVariable;
        args.wzValue = wzValue;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetVariableVersion args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVariable);
        ExitOnFailure(hr, "Failed to write variable of SetVariableVersion args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzValue);
        ExitOnFailure(hr, "Failed to write value of SetVariableVersion args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetVariableVersion results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_SETVARIABLEVERSION, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA SetVariableVersion failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP CloseSplashScreen()
    {
        HRESULT hr = S_OK;
        BAENGINE_CLOSESPLASHSCREEN_ARGS args = { };
        BAENGINE_CLOSESPLASHSCREEN_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of CloseSplashScreen args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of CloseSplashScreen results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_CLOSESPLASHSCREEN, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA CloseSplashScreen failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP Detect(
        __in_opt HWND hwndParent
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_DETECT_ARGS args = { };
        BAENGINE_DETECT_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.hwndParent = reinterpret_cast<DWORD64>(hwndParent);

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Detect args.");

        hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.hwndParent);
        ExitOnFailure(hr, "Failed to write parent window of Detect args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Detect results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_DETECT, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA Detect failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP Plan(
        __in BOOTSTRAPPER_ACTION action
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_PLAN_ARGS args = { };
        BAENGINE_PLAN_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.action = action;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Plan args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, static_cast<DWORD>(args.action));
        ExitOnFailure(hr, "Failed to write parent window of Plan args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Plan results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_PLAN, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA Plan failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP Elevate(
        __in_opt HWND hwndParent
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_ELEVATE_ARGS args = { };
        BAENGINE_ELEVATE_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.hwndParent = reinterpret_cast<DWORD64>(hwndParent);

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Elevate args.");

        hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.hwndParent);
        ExitOnFailure(hr, "Failed to write parent window of Elevate args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Elevate results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_ELEVATE, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA Elevate failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP Apply(
        __in HWND hwndParent
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_APPLY_ARGS args = { };
        BAENGINE_APPLY_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.hwndParent = reinterpret_cast<DWORD64>(hwndParent);

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Apply args.");

        hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.hwndParent);
        ExitOnFailure(hr, "Failed to write parent window of Apply args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Apply results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_APPLY, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA Apply failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP Quit(
        __in DWORD dwExitCode
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_QUIT_ARGS args = { };
        BAENGINE_QUIT_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.dwExitCode = dwExitCode;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Quit args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwExitCode);
        ExitOnFailure(hr, "Failed to write exit code of Quit args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of Quit results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_QUIT, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA Quit failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP LaunchApprovedExe(
        __in_opt HWND hwndParent,
        __in_z LPCWSTR wzApprovedExeForElevationId,
        __in_z_opt LPCWSTR wzArguments,
        __in DWORD dwWaitForInputIdleTimeout
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_LAUNCHAPPROVEDEXE_ARGS args = { };
        BAENGINE_LAUNCHAPPROVEDEXE_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        ExitOnNull(wzApprovedExeForElevationId, hr, E_INVALIDARG, "wzApprovedExeForElevationId is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.hwndParent = reinterpret_cast<DWORD64>(hwndParent);
        args.wzApprovedExeForElevationId = wzApprovedExeForElevationId;
        args.wzArguments = wzArguments;
        args.dwWaitForInputIdleTimeout = dwWaitForInputIdleTimeout;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of LaunchApprovedExe args.");

        hr = BuffWriteNumber64ToBuffer(&bufferArgs, args.hwndParent);
        ExitOnFailure(hr, "Failed to write parent window of LaunchApprovedExe args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzApprovedExeForElevationId);
        ExitOnFailure(hr, "Failed to write approved exe elevation id of LaunchApprovedExe args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzArguments);
        ExitOnFailure(hr, "Failed to write arguments of LaunchApprovedExe args.");

        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwWaitForInputIdleTimeout);
        ExitOnFailure(hr, "Failed to write wait for idle input timeout of LaunchApprovedExe args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of LaunchApprovedExe results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_LAUNCHAPPROVEDEXE, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA LaunchApprovedExe failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

    virtual STDMETHODIMP SetUpdateSource(
        __in_z LPCWSTR wzUrl,
        __in_z_opt LPCWSTR wzAuthorizationHeader
        )
    {
        HRESULT hr = S_OK;
        BAENGINE_SETUPDATESOURCE_ARGS args = { };
        BAENGINE_SETUPDATESOURCE_RESULTS results = { };
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };

        ExitOnNull(wzUrl, hr, E_INVALIDARG, "wzUrl is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzUrl = wzUrl;
        args.wzAuthorizationHeader = wzAuthorizationHeader;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetUpdateSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzUrl);
        ExitOnFailure(hr, "Failed to write url of SetUpdateSource args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzAuthorizationHeader);
        ExitOnFailure(hr, "Failed to write authorization header of SetUpdateSource args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of SetUpdateSource results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_SETUPDATESOURCE, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA SetUpdateSource failed.");

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
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
        BUFF_BUFFER bufferArgs = { };
        BUFF_BUFFER bufferResults = { };
        PIPE_RPC_RESULT rpc = { };
        SIZE_T iBuffer = 0;

        ExitOnNull(pnResult, hr, E_INVALIDARG, "pnResult is required");

        // Init send structs.
        args.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;
        args.wzVersion1 = wzVersion1;
        args.wzVersion2 = wzVersion2;

        results.dwApiVersion = WIX_5_BOOTSTRAPPER_APPLICATION_API_VERSION;

        // Send args.
        hr = BuffWriteNumberToBuffer(&bufferArgs, args.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of CompareVersions args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVersion1);
        ExitOnFailure(hr, "Failed to write first input of CompareVersions args.");

        hr = BuffWriteStringToBuffer(&bufferArgs, args.wzVersion2);
        ExitOnFailure(hr, "Failed to write second input of CompareVersions args.");

        // Send results.
        hr = BuffWriteNumberToBuffer(&bufferResults, results.dwApiVersion);
        ExitOnFailure(hr, "Failed to write API version of CompareVersions results.");

        // Get results.
        hr = SendRequest(BOOTSTRAPPER_ENGINE_MESSAGE_COMPAREVERSIONS, &bufferArgs, &bufferResults, &rpc);
        ExitOnFailure(hr, "BA CompareVersions failed.");

        // Read results.
        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, &results.dwApiVersion);
        ExitOnFailure(hr, "Failed to read size from CompareVersions results.");

        hr = BuffReadNumber(rpc.pbData, rpc.cbData, &iBuffer, reinterpret_cast<DWORD*>(&results.nResult));
        ExitOnFailure(hr, "Failed to read result from CompareVersions results.");

        *pnResult = results.nResult;

    LExit:
        PipeFreeRpcResult(&rpc);
        ReleaseBuffer(bufferResults);
        ReleaseBuffer(bufferArgs);

        return hr;
    }

private:
    HRESULT SendRequest(
        __in DWORD dwMessageType,
        __in BUFF_BUFFER* pBufferArgs,
        __in BUFF_BUFFER* pBufferResults,
        __in PIPE_RPC_RESULT* pRpc
        )
    {
        HRESULT hr = S_OK;
        BUFF_BUFFER buffer = { };

        hr = CombineArgsAndResults(pBufferArgs, pBufferResults, &buffer);
        if (SUCCEEDED(hr))
        {
            hr = PipeRpcRequest(&m_hRpcPipe, dwMessageType, buffer.pbData, buffer.cbData, pRpc);
        }

        ReleaseBuffer(buffer);
        return hr;
    }

    HRESULT CombineArgsAndResults(
        __in BUFF_BUFFER* pBufferArgs,
        __in BUFF_BUFFER* pBufferResults,
        __in BUFF_BUFFER* pBufferCombined
        )
    {
        HRESULT hr = S_OK;

        // Write args to buffer.
        hr = BuffWriteStreamToBuffer(pBufferCombined, pBufferArgs->pbData, pBufferArgs->cbData);
        ExitOnFailure(hr, "Failed to write args buffer.");

        // Write results to buffer.
        hr = BuffWriteStreamToBuffer(pBufferCombined, pBufferResults->pbData, pBufferResults->cbData);
        ExitOnFailure(hr, "Failed to write results buffer.");

    LExit:
        return hr;
    }

public:
    CBalBootstrapperEngine(
        __in HANDLE hPipe,
        __out HRESULT* phr
        )
    {
        m_cReferences = 1;

        PipeRpcInitialize(&m_hRpcPipe, hPipe, FALSE);

        *phr = ::CoCreateFreeThreadedMarshaler(this, &m_pFreeThreadedMarshaler);
    }

    ~CBalBootstrapperEngine()
    {
        PipeRpcUninitiailize(&m_hRpcPipe);
        ReleaseObject(m_pFreeThreadedMarshaler);
    }

private:
    long m_cReferences;
    PIPE_RPC_HANDLE m_hRpcPipe;
    IUnknown* m_pFreeThreadedMarshaler;
};


HRESULT BalBootstrapperEngineCreate(
    __in HANDLE hPipe,
    __out IBootstrapperEngine** ppBootstrapperEngine
    )
{
    HRESULT hr = S_OK;
    CBalBootstrapperEngine* pBootstrapperEngine = NULL;

    pBootstrapperEngine = new CBalBootstrapperEngine(hPipe, &hr);
    ExitOnNull(pBootstrapperEngine, hr, E_OUTOFMEMORY, "Failed to allocate new BalBootstrapperEngine object.");
    ExitOnFailure(hr, "Failed to initialize BalBootstrapperEngine.");

    hr = pBootstrapperEngine->QueryInterface(IID_PPV_ARGS(ppBootstrapperEngine));
    ExitOnFailure(hr, "Failed to QI for IBootstrapperEngine from BalBootstrapperEngine object.");

LExit:
    ReleaseObject(pBootstrapperEngine);
    return hr;
}
