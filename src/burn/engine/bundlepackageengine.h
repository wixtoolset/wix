#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// function declarations

HRESULT BundlePackageEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnBundlePackage,
    __in BURN_PACKAGE* pPackage
    );
HRESULT BundlePackageEngineParseRelatedCodes(
    __in IXMLDOMNode* pixnBundle,
    __in LPWSTR** prgsczDetectCodes,
    __in DWORD* pcDetectCodes,
    __in LPWSTR** prgsczUpgradeCodes,
    __in DWORD* pcUpgradeCodes,
    __in LPWSTR** prgsczAddonCodes,
    __in DWORD* pcAddonCodes,
    __in LPWSTR** prgsczPatchCodes,
    __in DWORD* pcPatchCodes
    );
void BundlePackageEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    );
HRESULT BundlePackageEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT BundlePackageEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage
    );
HRESULT BundlePackageEnginePlanAddPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    );
HRESULT BundlePackageEnginePlanAddRelatedBundle(
    __in_opt DWORD *pdwInsertSequence,
    __in BURN_RELATED_BUNDLE* pRelatedBundle,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    );
HRESULT BundlePackageEngineExecutePackage(
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
HRESULT BundlePackageEngineExecuteRelatedBundle(
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_GENERICMESSAGEHANDLER pfnGenericExecuteProgress,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
void BundlePackageEngineUpdateInstallRegistrationState(
    __in BURN_EXECUTE_ACTION* pAction,
    __in HRESULT hrExecute
    );


#if defined(__cplusplus)
}
#endif
