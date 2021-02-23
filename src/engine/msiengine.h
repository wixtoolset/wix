#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

// constants
#define BURNMSIINSTALL_PROPERTY_NAME L"BURNMSIINSTALL"
#define BURNMSIMODIFY_PROPERTY_NAME L"BURNMSIMODIFY"
#define BURNMSIREPAIR_PROPERTY_NAME L"BURNMSIREPAIR"
#define BURNMSIUNINSTALL_PROPERTY_NAME L"BURNMSIUNINSTALL"


#if defined(__cplusplus)
extern "C" {
#endif


// function declarations

HRESULT MsiEngineParsePackageFromXml(
    __in IXMLDOMNode* pixnBundle,
    __in BURN_PACKAGE* pPackage
    );
HRESULT MsiEngineParsePropertiesFromXml(
    __in IXMLDOMNode* pixnPackage,
    __out BURN_MSIPROPERTY** prgProperties,
    __out DWORD* pcProperties
    );
void MsiEnginePackageUninitialize(
    __in BURN_PACKAGE* pPackage
    );
HRESULT MsiEngineDetectInitialize(
    __in BURN_PACKAGES* pPackages
    );
HRESULT MsiEngineDetectPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT MsiEnginePlanInitializePackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_VARIABLES* pVariables,
    __in BURN_USER_EXPERIENCE* pUserExperience
    );
HRESULT MsiEnginePlanCalculatePackage(
    __in BURN_PACKAGE* pPackage,
    __in BOOL fInsideMsiTransaction
    );
HRESULT MsiEnginePlanAddPackage(
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in_opt HANDLE hCacheEvent,
    __in BOOL fPlanPackageCacheRollback
    );
HRESULT MsiEngineBeginTransaction(
    __in LPCWSTR wzName
    );
HRESULT MsiEngineCommitTransaction(
    __in LPCWSTR wzName
    );
HRESULT MsiEngineRollbackTransaction(
    __in LPCWSTR wzName
    );
HRESULT MsiEngineExecutePackage(
    __in_opt HWND hwndParent,
    __in BURN_EXECUTE_ACTION* pExecuteAction,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __in PFN_MSIEXECUTEMESSAGEHANDLER pfnMessageHandler,
    __in LPVOID pvContext,
    __out BOOTSTRAPPER_APPLY_RESTART* pRestart
    );
HRESULT MsiEngineConcatActionProperty(
    __in BURN_MSI_PROPERTY actionMsiProperty,
    __deref_out_z LPWSTR* psczProperties
    );
HRESULT MsiEngineConcatProperties(
    __in_ecount(cProperties) BURN_MSIPROPERTY* rgProperties,
    __in DWORD cProperties,
    __in BURN_VARIABLES* pVariables,
    __in BOOL fRollback,
    __deref_out_z LPWSTR* psczProperties,
    __in BOOL fObfuscateHiddenVariables
    );
HRESULT MsiEngineCalculateInstallUiLevel(
    __in BOOTSTRAPPER_DISPLAY display,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in LPCWSTR wzPackageId,
    __in BOOL fExecute,
    __in BOOTSTRAPPER_ACTION_STATE actionState,
    __out BURN_MSI_PROPERTY* pActionMsiProperty,
    __out INSTALLUILEVEL* pUiLevel,
    __out BOOL* pfDisableExternalUiHandler
    );
void MsiEngineUpdateInstallRegistrationState(
    __in BURN_EXECUTE_ACTION* pAction,
    __in BOOL fRollback,
    __in HRESULT hrExecute,
    __in BOOL fInsideMsiTransaction
    );

#if defined(__cplusplus)
}
#endif
