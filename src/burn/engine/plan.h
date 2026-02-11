#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

const DWORD BURN_PLAN_INVALID_ACTION_INDEX = 0x80000000;

enum BURN_REGISTRATION_ACTION_OPERATIONS
{
    BURN_REGISTRATION_ACTION_OPERATIONS_NONE = 0x0,
    BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE = 0x1,
    BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY = 0x2,
    BURN_REGISTRATION_ACTION_OPERATIONS_ARP_SYSTEM_COMPONENT = 0x4,
};

enum BURN_DEPENDENT_REGISTRATION_ACTION_TYPE
{
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_NONE,
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_REGISTER,
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE_UNREGISTER,
};

enum BURN_CACHE_ACTION_TYPE
{
    BURN_CACHE_ACTION_TYPE_NONE,
    BURN_CACHE_ACTION_TYPE_CHECKPOINT,
    BURN_CACHE_ACTION_TYPE_LAYOUT_BUNDLE,
    BURN_CACHE_ACTION_TYPE_PACKAGE,
    BURN_CACHE_ACTION_TYPE_ROLLBACK_PACKAGE,
    BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT,
    BURN_CACHE_ACTION_TYPE_CONTAINER,
};

enum BURN_EXECUTE_ACTION_TYPE
{
    BURN_EXECUTE_ACTION_TYPE_NONE,
    BURN_EXECUTE_ACTION_TYPE_CHECKPOINT,
    BURN_EXECUTE_ACTION_TYPE_WAIT_CACHE_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_UNCACHE_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_RELATED_BUNDLE,
    BURN_EXECUTE_ACTION_TYPE_BUNDLE_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_MSP_TARGET,
    BURN_EXECUTE_ACTION_TYPE_MSU_PACKAGE,
    BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER,
    BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY,
    BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_START,
    BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY_END,
    BURN_EXECUTE_ACTION_TYPE_BEGIN_MSI_TRANSACTION,
    BURN_EXECUTE_ACTION_TYPE_COMMIT_MSI_TRANSACTION,
    BURN_EXECUTE_ACTION_TYPE_UNINSTALL_MSI_COMPATIBLE_PACKAGE,
};

enum BURN_CLEAN_ACTION_TYPE
{
    BURN_CLEAN_ACTION_TYPE_NONE,
    BURN_CLEAN_ACTION_TYPE_COMPATIBLE_PACKAGE,
    BURN_CLEAN_ACTION_TYPE_PACKAGE,
};


// structs

typedef struct _BURN_DEPENDENT_REGISTRATION_ACTION
{
    BURN_DEPENDENT_REGISTRATION_ACTION_TYPE type;
    LPWSTR sczBundleCode;
    LPWSTR sczDependentProviderKey;
} BURN_DEPENDENT_REGISTRATION_ACTION;

typedef struct _BURN_CACHE_CONTAINER_PROGRESS
{
    LPWSTR wzId;
    DWORD iIndex;
    BOOL fCachedDuringApply;
    BURN_CONTAINER* pContainer;
} BURN_CACHE_CONTAINER_PROGRESS;

typedef struct _BURN_CACHE_PAYLOAD_PROGRESS
{
    LPWSTR wzId;
    DWORD iIndex;
    BOOL fCachedDuringApply;
    BURN_PAYLOAD* pPayload;
} BURN_CACHE_PAYLOAD_PROGRESS;

typedef struct _BURN_CACHE_ACTION
{
    BURN_CACHE_ACTION_TYPE type;
    union
    {
        struct
        {
            DWORD dwId;
        } checkpoint;
        struct
        {
            LPWSTR sczExecutableName;
            LPWSTR sczUnverifiedPath;
            DWORD64 qwBundleSize;
            BURN_PAYLOAD_GROUP* pPayloadGroup;
        } bundleLayout;
        struct
        {
            BURN_PACKAGE* pPackage;
        } package;
        struct
        {
            BURN_PACKAGE* pPackage;
        } rollbackPackage;
        struct
        {
            BURN_PACKAGE* pPackage;
        } syncpoint;
        struct
        {
            BURN_CONTAINER* pContainer;
        } container;
    };
} BURN_CACHE_ACTION;

typedef struct _BURN_ORDERED_PATCHES
{
    BURN_PACKAGE* pPackage;

    BURN_MSPTARGETPRODUCT* pTargetProduct; // only valid in the unelevated engine.
} BURN_ORDERED_PATCHES;

typedef struct _BURN_EXECUTE_ACTION_CHECKPOINT
{
    DWORD dwId;
    BURN_ROLLBACK_BOUNDARY* pActiveRollbackBoundary;
} BURN_EXECUTE_ACTION_CHECKPOINT;

typedef struct _BURN_EXECUTE_ACTION
{
    BURN_EXECUTE_ACTION_TYPE type;
    BOOL fDeleted; // used to skip an action after it was planned since deleting actions out of the plan is too hard.
    union
    {
        BURN_EXECUTE_ACTION_CHECKPOINT checkpoint;
        struct
        {
            BURN_PACKAGE* pPackage;
        } waitCachePackage;
        struct
        {
            BURN_PACKAGE* pPackage;
        } uncachePackage;
        struct
        {
            BURN_RELATED_BUNDLE* pRelatedBundle;
            BOOTSTRAPPER_ACTION_STATE action;
            LPWSTR sczIgnoreDependencies;
            LPWSTR sczAncestors;
            LPWSTR sczEngineWorkingDirectory;
        } relatedBundle;
        struct
        {
            BURN_PACKAGE* pPackage;
            BOOTSTRAPPER_ACTION_STATE action;
            LPWSTR sczParent;
            LPWSTR sczIgnoreDependencies;
            LPWSTR sczAncestors;
            LPWSTR sczEngineWorkingDirectory;
        } bundlePackage;
        struct
        {
            BURN_PACKAGE* pPackage;
            BOOTSTRAPPER_ACTION_STATE action;
            LPWSTR sczAncestors;
            LPWSTR sczEngineWorkingDirectory;
        } exePackage;
        struct
        {
            BURN_PACKAGE* pPackage;
            LPWSTR sczLogPath;
            DWORD dwLoggingAttributes;
            BURN_MSI_PROPERTY actionMsiProperty;
            INSTALLUILEVEL uiLevel;
            BOOL fDisableExternalUiHandler;
            BOOTSTRAPPER_ACTION_STATE action;
            BOOTSTRAPPER_MSI_FILE_VERSIONING fileVersioning;

            BOOTSTRAPPER_FEATURE_ACTION* rgFeatures;
        } msiPackage;
        struct
        {
            BURN_PACKAGE* pPackage;
            LPWSTR sczTargetProductCode;
            BURN_PACKAGE* pChainedTargetPackage;
            BOOL fSlipstream;
            BOOL fPerMachineTarget;
            LPWSTR sczLogPath;
            BURN_MSI_PROPERTY actionMsiProperty;
            INSTALLUILEVEL uiLevel;
            BOOL fDisableExternalUiHandler;
            BOOTSTRAPPER_ACTION_STATE action;
            BOOTSTRAPPER_MSI_FILE_VERSIONING fileVersioning;

            BURN_ORDERED_PATCHES* rgOrderedPatches;
            DWORD cOrderedPatches;
        } mspTarget;
        struct
        {
            BURN_PACKAGE* pPackage;
            LPWSTR sczLogPath;
            BOOTSTRAPPER_ACTION_STATE action;
        } msuPackage;
        struct
        {
            BURN_ROLLBACK_BOUNDARY* pRollbackBoundary;
        } rollbackBoundary;
        struct
        {
            BURN_PACKAGE* pPackage;
        } packageProvider;
        struct
        {
            BURN_PACKAGE* pPackage;
            LPWSTR sczBundleProviderKey;
        } packageDependency;
        struct
        {
            BURN_ROLLBACK_BOUNDARY* pRollbackBoundary;
        } msiTransaction;
        struct
        {
            BURN_PACKAGE* pParentPackage;
            LPWSTR sczLogPath;
            DWORD dwLoggingAttributes;
        } uninstallMsiCompatiblePackage;
    };
} BURN_EXECUTE_ACTION;

typedef struct _BURN_CLEAN_ACTION
{
    BURN_CLEAN_ACTION_TYPE type;
    BURN_PACKAGE* pPackage;
} BURN_CLEAN_ACTION;

typedef struct _BURN_PLAN
{
    BOOTSTRAPPER_ACTION action;
    BOOTSTRAPPER_SCOPE plannedScope;
    BURN_CACHE* pCache;
    BOOTSTRAPPER_COMMAND* pCommand;
    BURN_ENGINE_COMMAND* pInternalCommand;
    BURN_PAYLOADS* pPayloads;
    LPWSTR wzBundleCode;          // points directly into parent the ENGINE_STATE.
    LPWSTR wzBundleProviderKey; // points directly into parent the ENGINE_STATE.
    BOOL fPerMachine;
    BOOL fCanAffectMachineState;
    DWORD dwRegistrationOperations;
    BOOL fDisallowRemoval;
    BOOL fDisableRollback;
    BOOL fAffectedMachineState;
    LPWSTR sczLayoutDirectory;
    BOOL fPlanPackageCacheRollback;
    BOOL fDowngrade;
    BOOL fApplying;

    DWORD64 qwCacheSizeTotal;

    DWORD cExecutePackagesTotal;
    DWORD cOverallProgressTicksTotal;

    BOOL fEnabledForwardCompatibleBundle;
    BURN_PACKAGE forwardCompatibleBundle;

    BURN_DEPENDENT_REGISTRATION_ACTION* rgRegistrationActions;
    DWORD cRegistrationActions;

    BURN_DEPENDENT_REGISTRATION_ACTION* rgRollbackRegistrationActions;
    DWORD cRollbackRegistrationActions;

    BURN_CACHE_ACTION* rgCacheActions;
    DWORD cCacheActions;

    BURN_CACHE_ACTION* rgRollbackCacheActions;
    DWORD cRollbackCacheActions;

    BURN_EXECUTE_ACTION* rgExecuteActions;
    DWORD cExecuteActions;

    BURN_EXECUTE_ACTION* rgRollbackActions;
    DWORD cRollbackActions;

    BURN_EXECUTE_ACTION* rgRestoreRelatedBundleActions;
    DWORD cRestoreRelatedBundleActions;

    BURN_CLEAN_ACTION* rgCleanActions;
    DWORD cCleanActions;

    DEPENDENCY* rgPlannedProviders;
    UINT cPlannedProviders;

    BURN_CACHE_CONTAINER_PROGRESS* rgContainerProgress;
    DWORD cContainerProgress;
    STRINGDICT_HANDLE shContainerProgress;

    BURN_CACHE_PAYLOAD_PROGRESS* rgPayloadProgress;
    DWORD cPayloadProgress;
    STRINGDICT_HANDLE shPayloadProgress;

    DWORD dwNextCheckpointId; // for plan internal use
    BURN_ROLLBACK_BOUNDARY* pActiveRollbackBoundary; // for plan internal use
} BURN_PLAN;


// functions

void PlanReset(
    __in BURN_PLAN* pPlan,
    __in BURN_VARIABLES* pVariables,
    __in BURN_CONTAINERS* pContainers,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOAD_GROUP* pLayoutPayloads
    );
void PlanUninitializeExecuteAction(
    __in BURN_EXECUTE_ACTION* pExecuteAction
    );
HRESULT PlanSetVariables(
    __in BOOTSTRAPPER_ACTION action,
    __in BOOTSTRAPPER_PACKAGE_SCOPE authoredScope,
    __in BOOTSTRAPPER_SCOPE plannedScope,
    __in BURN_VARIABLES* pVariables
    );
HRESULT PlanDefaultRelatedBundlePlanType(
    __in BOOTSTRAPPER_RELATION_TYPE relatedBundleRelationType,
    __in VERUTIL_VERSION* pRegistrationVersion,
    __in VERUTIL_VERSION* pRelatedBundleVersion,
    __inout BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE* pPlanRelationType
    );
HRESULT PlanDefaultPackageRequestState(
    __in BURN_PACKAGE_TYPE packageType,
    __in BOOTSTRAPPER_PACKAGE_STATE currentState,
    __in BOOTSTRAPPER_ACTION action,
    __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT installCondition,
    __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT repairCondition,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __out BOOTSTRAPPER_REQUEST_STATE* pRequestState
    );
HRESULT PlanLayoutBundle(
    __in BURN_PLAN* pPlan,
    __in_z LPCWSTR wzExecutableName,
    __in DWORD64 qwBundleSize,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PAYLOAD_GROUP* pLayoutPayloads
    );
HRESULT PlanForwardCompatibleBundles(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PLAN* pPlan,
    __in BURN_REGISTRATION* pRegistration
    );
HRESULT PlanPackages(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGES* pPackages,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    );
HRESULT PlanRegistration(
    __in BURN_PLAN* pPlan,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_DEPENDENCIES* pDependencies,
    __in BOOTSTRAPPER_RESUME_TYPE resumeType,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __inout BOOL* pfContinuePlanning
    );
HRESULT PlanPassThroughBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    );
HRESULT PlanUpdateBundle(
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    );
HRESULT PlanLayoutContainer(
    __in BURN_PLAN* pPlan,
    __in BURN_CONTAINER* pContainer
    );
HRESULT PlanLayoutPackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BOOL fVital
    );
HRESULT PlanExecutePackage(
    __in BOOL fPerMachine,
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables
    );
HRESULT PlanDefaultRelatedBundleRequestState(
    __in BOOTSTRAPPER_RELATION_TYPE commandRelationType,
    __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE relatedBundleRelationType,
    __in BOOTSTRAPPER_ACTION action,
    __inout BOOTSTRAPPER_REQUEST_STATE* pRequestState
    );
HRESULT PlanRelatedBundlesInitialize(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_PLAN* pPlan
    );
HRESULT PlanRelatedBundlesBegin(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_REGISTRATION* pRegistration,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BURN_PLAN* pPlan
    );
HRESULT PlanRelatedBundlesComplete(
    __in BURN_USER_EXPERIENCE* pUserExperience,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_PLAN* pPlan,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in DWORD dwExecuteActionEarlyIndex
    );
HRESULT PlanFinalizeActions(
    __in BURN_PLAN* pPlan
    );
HRESULT PlanCleanPackage(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );
HRESULT PlanExecuteCacheSyncAndRollback(
    __in BURN_PLAN* pPlan,
    __in BURN_PACKAGE* pPackage
    );
HRESULT PlanExecuteCheckpoint(
    __in BURN_PLAN* pPlan
    );
HRESULT PlanInsertExecuteAction(
    __in DWORD dwIndex,
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    );
HRESULT PlanInsertRollbackAction(
    __in DWORD dwIndex,
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppRollbackAction
    );
HRESULT PlanAppendExecuteAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    );
HRESULT PlanAppendRollbackAction(
    __in BURN_PLAN* pPlan,
    __out BURN_EXECUTE_ACTION** ppExecuteAction
    );
HRESULT PlanRollbackBoundaryBegin(
    __in BURN_PLAN* pPlan,
    __in BURN_USER_EXPERIENCE* pUX,
    __in BURN_LOGGING* pLog,
    __in BURN_VARIABLES* pVariables,
    __in BURN_ROLLBACK_BOUNDARY* pRollbackBoundary
    );
HRESULT PlanRollbackBoundaryComplete(
    __in BURN_PLAN* pPlan
    );
HRESULT PlanSetResumeCommand(
    __in BURN_PLAN* pPlan,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_LOGGING* pLog
    );
void PlanDump(
    __in BURN_PLAN* pPlan
    );
HRESULT PlanPackagesAndBundleScope(
    __in BURN_PACKAGE* rgPackages,
    __in DWORD cPackages,
    __in BOOTSTRAPPER_SCOPE scope,
    __in BOOTSTRAPPER_PACKAGE_SCOPE authoredScope,
    __in BOOTSTRAPPER_SCOPE commandLineScope,
    __in BOOTSTRAPPER_SCOPE detectedScope,
    __out BOOTSTRAPPER_SCOPE* pResultingScope,
    __out BOOL* pfPerMachine
);

#if defined(__cplusplus)
}
#endif
