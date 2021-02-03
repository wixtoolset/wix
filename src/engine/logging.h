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

HRESULT LoggingOpen(
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BOOTSTRAPPER_DISPLAY display,
    __in_z LPCWSTR wzBundleName
    );

void LoggingOpenFailed();

void LoggingIncrementPackageSequence();

HRESULT LoggingSetPackageVariable(
    __in BURN_PACKAGE* pPackage,
    __in_z_opt LPCWSTR wzSuffix,
    __in BOOL fRollback,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __out_opt LPWSTR* psczLogPath
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

LPCSTR LoggingDependencyActionToString(
    BURN_DEPENDENCY_ACTION action
    );

LPCSTR LoggingBoolToString(
    __in BOOL f
    );

LPCSTR LoggingTrueFalseToString(
    __in BOOL f
    );

LPCSTR LoggingPackageStateToString(
    __in BOOTSTRAPPER_PACKAGE_STATE packageState
    );

LPCSTR LoggingPackageRegistrationStateToString(
    __in BOOL fCanAffectRegistration,
    __in BURN_PACKAGE_REGISTRATION_STATE registrationState
    );

LPCSTR LoggingCacheStateToString(
    __in BURN_CACHE_STATE cacheState
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

LPCSTR LoggingPerMachineToString(
    __in BOOL fPerMachine
    );

LPCSTR LoggingRestartToString(
    __in BOOTSTRAPPER_APPLY_RESTART restart
    );

LPCSTR LoggingResumeModeToString(
    __in BURN_RESUME_MODE resumeMode
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
