#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

enum BURN_LOGGING_STATE
{
    BURN_LOGGING_STATE_CLOSED,
    BURN_LOGGING_STATE_OPEN,
    BURN_LOGGING_STATE_DISABLED,
};

enum BURN_LOGGING_ATTRIBUTE
{
    BURN_LOGGING_ATTRIBUTE_APPEND = 0x1,
    BURN_LOGGING_ATTRIBUTE_VERBOSE = 0x2,
    BURN_LOGGING_ATTRIBUTE_EXTRADEBUG = 0x4,
    BURN_LOGGING_ATTRIBUTE_CONSOLE = 0x8,
};


// structs

typedef struct _BURN_LOGGING
{
    BURN_LOGGING_STATE state;
    LPWSTR sczPathVariable;

    DWORD dwAttributes;
    LPWSTR sczPath;
    LPWSTR sczPrefix;
    LPWSTR sczExtension;
} BURN_LOGGING;



// function declarations

HRESULT LoggingParseFromXml(
    __in BURN_LOGGING* pLog,
    __in IXMLDOMNode* pixnBundle
    );
HRESULT LoggingOpen(
    __in BURN_LOGGING* pLog,
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzBundleName
    );

void LoggingOpenFailed();

void LoggingIncrementPackageSequence();

HRESULT LoggingSetCompatiblePackageVariable(
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __out_opt LPWSTR* psczLogPath
    );

HRESULT LoggingSetPackageVariable(
    __in BURN_PACKAGE* pPackage,
    __in_z_opt LPCWSTR wzSuffix,
    __in BOOL fRollback,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __out_opt LPWSTR* psczLogPath
    );

HRESULT LoggingSetTransactionVariable(
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary,
    __in_z_opt LPCWSTR wzSuffix,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    );

LPCSTR LoggingBurnActionToString(
    __in BOOTSTRAPPER_ACTION action
    );

LPCSTR LoggingBurnMessageToString(
    __in UINT message
    );

LPCSTR LoggingActionStateToString(
    __in BOOTSTRAPPER_ACTION_STATE actionState
    );

LPCSTR LoggingCacheTypeToString(
    BOOTSTRAPPER_CACHE_TYPE cacheType
    );

LPCSTR LoggingCachePackageTypeToString(
    BURN_CACHE_PACKAGE_TYPE cachePackageType
    );

LPCSTR LoggingDependencyActionToString(
    BURN_DEPENDENCY_ACTION action
    );

LPCSTR LoggingBoolToString(
    __in BOOL f
    );

LPCSTR LoggingTrueFalseToString(
    __in BOOL f
    );

LPCSTR LoggingExitCodeTypeToString(
    __in BURN_EXE_EXIT_CODE_TYPE exitCodeType
    );

LPCSTR LoggingPackageStateToString(
    __in BOOTSTRAPPER_PACKAGE_STATE packageState
    );

LPCSTR LoggingPackageRegistrationStateToString(
    __in BOOL fCanAffectRegistration,
    __in BURN_PACKAGE_REGISTRATION_STATE registrationState
    );

LPCSTR LoggingMsiFileVersioningToString(
    __in BOOTSTRAPPER_MSI_FILE_VERSIONING fileVersioning
    );

LPCSTR LoggingMsiFeatureStateToString(
    __in BOOTSTRAPPER_FEATURE_STATE featureState
    );

LPCSTR LoggingMsiFeatureActionToString(
    __in BOOTSTRAPPER_FEATURE_ACTION featureAction
    );

LPCSTR LoggingMsiInstallContext(
    __in MSIINSTALLCONTEXT context
    );

LPCWSTR LoggingBurnMsiPropertyToString(
    __in BURN_MSI_PROPERTY burnMsiProperty
    );

LPCSTR LoggingMspTargetActionToString(
    __in BOOTSTRAPPER_ACTION_STATE action,
    __in BURN_PATCH_SKIP_STATE skipState
    );

LPCSTR LoggingPerMachineToString(
    __in BOOL fPerMachine
    );

LPCSTR LoggingPlannedCacheToString(
    __in const BURN_PACKAGE* pPackage
    );

LPCSTR LoggingRegistrationTypeToString(
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType
    );

LPCSTR LoggingRestartToString(
    __in BOOTSTRAPPER_APPLY_RESTART restart
    );

LPCSTR LoggingResumeModeToString(
    __in BURN_RESUME_MODE resumeMode
    );

LPCSTR LoggingPlanRelationTypeToString(
    __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE type
    );

LPCSTR LoggingRegistrationOptionsToString(
    __in DWORD dwRegistrationOptions
    );

LPCSTR LoggingRelationTypeToString(
    __in BOOTSTRAPPER_RELATION_TYPE type
    );

LPCSTR LoggingRelatedOperationToString(
    __in BOOTSTRAPPER_RELATED_OPERATION operation
    );

LPCSTR LoggingRequestStateToString(
    __in BOOTSTRAPPER_REQUEST_STATE requestState
    );

LPCSTR LoggingRollbackOrExecute(
    __in BOOL fRollback
    );

LPWSTR LoggingStringOrUnknownIfNull(
    __in LPCWSTR wz
    );


#if defined(__cplusplus)
}
#endif
