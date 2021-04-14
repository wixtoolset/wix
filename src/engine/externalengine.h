#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#define ValidateMessageParameter(x, pv, type) { x = ExternalEngineValidateMessageParameter(pv, offsetof(type, cbSize), sizeof(type)); if (FAILED(x)) { goto LExit; }}
#define ValidateMessageArgs(x, pv, type, identifier) ValidateMessageParameter(x, pv, type); const type* identifier = reinterpret_cast<type*>(pv); UNREFERENCED_PARAMETER(identifier)
#define ValidateMessageResults(x, pv, type, identifier) ValidateMessageParameter(x, pv, type); type* identifier = reinterpret_cast<type*>(pv); UNREFERENCED_PARAMETER(identifier)


#if defined(__cplusplus)
extern "C" {
#endif

void ExternalEngineGetPackageCount(
    __in BURN_ENGINE_STATE* pEngineState,
    __out DWORD* pcPackages
    );

HRESULT ExternalEngineGetVariableNumeric(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __out LONGLONG* pllValue
    );

HRESULT ExternalEngineGetVariableString(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout DWORD* pcchValue
    );

HRESULT ExternalEngineGetVariableVersion(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __out_ecount_opt(*pcchValue) LPWSTR wzValue,
    __inout DWORD* pcchValue
    );

HRESULT ExternalEngineFormatString(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzIn,
    __out_ecount_opt(*pcchOut) LPWSTR wzOut,
    __inout DWORD* pcchOut
    );

HRESULT ExternalEngineEscapeString(
    __in_z LPCWSTR wzIn,
    __out_ecount_opt(*pcchOut) LPWSTR wzOut,
    __inout DWORD* pcchOut
    );

HRESULT ExternalEngineEvaluateCondition(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    );

HRESULT ExternalEngineLog(
    __in REPORT_LEVEL rl,
    __in_z LPCWSTR wzMessage
    );

HRESULT ExternalEngineSendEmbeddedError(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const DWORD dwErrorCode,
    __in_z LPCWSTR wzMessage,
    __in const DWORD dwUIHint,
    __out int* pnResult
    );

HRESULT ExternalEngineSendEmbeddedProgress(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const DWORD dwProgressPercentage,
    __in const DWORD dwOverallProgressPercentage,
    __out int* pnResult
    );

HRESULT ExternalEngineSetUpdate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z_opt LPCWSTR wzLocalSource,
    __in_z_opt LPCWSTR wzDownloadSource,
    __in const DWORD64 qwSize,
    __in const BOOTSTRAPPER_UPDATE_HASH_TYPE hashType,
    __in_opt const BYTE* rgbHash,
    __in const DWORD cbHash
    );

HRESULT ExternalEngineSetLocalSource(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z LPCWSTR wzPath
    );

HRESULT ExternalEngineSetDownloadSource(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z_opt LPCWSTR wzPackageOrContainerId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in_z_opt LPCWSTR wzUrl,
    __in_z_opt LPCWSTR wzUser,
    __in_z_opt LPCWSTR wzPassword
    );

HRESULT ExternalEngineSetVariableNumeric(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __in const LONGLONG llValue
    );

HRESULT ExternalEngineSetVariableString(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue,
    __in const BOOL fFormatted
    );

HRESULT ExternalEngineSetVariableVersion(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzVariable,
    __in_z_opt LPCWSTR wzValue
    );

void ExternalEngineCloseSplashScreen(
    __in BURN_ENGINE_STATE* pEngineState
    );

HRESULT ExternalEngineCompareVersions(
    __in_z LPCWSTR wzVersion1,
    __in_z LPCWSTR wzVersion2,
    __out int* pnResult
    );

HRESULT ExternalEngineDetect(
    __in const DWORD dwThreadId,
    __in_opt const HWND hwndParent
    );

HRESULT ExternalEnginePlan(
    __in const DWORD dwThreadId,
    __in const BOOTSTRAPPER_ACTION action
    );

HRESULT ExternalEngineElevate(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const DWORD dwThreadId,
    __in_opt const HWND hwndParent
    );

HRESULT ExternalEngineApply(
    __in const DWORD dwThreadId,
    __in_opt const HWND hwndParent
    );

HRESULT ExternalEngineQuit(
    __in const DWORD dwThreadId,
    __in const DWORD dwExitCode
    );

HRESULT ExternalEngineLaunchApprovedExe(
    __in BURN_ENGINE_STATE* pEngineState,
    __in const DWORD dwThreadId,
    __in_opt const HWND hwndParent,
    __in_z LPCWSTR wzApprovedExeForElevationId,
    __in_z_opt LPCWSTR wzArguments,
    __in const DWORD dwWaitForInputIdleTimeout
    );

HRESULT ExternalEngineSetUpdateSource(
    __in BURN_ENGINE_STATE* pEngineState,
    __in_z LPCWSTR wzUrl
    );

HRESULT WINAPI ExternalEngineValidateMessageParameter(
    __in_opt const LPVOID pv,
    __in SIZE_T cbSizeOffset,
    __in DWORD dwMinimumSize
    );

#if defined(__cplusplus)
}
#endif
