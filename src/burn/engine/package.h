#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

struct _BURN_RELATED_BUNDLES;
typedef _BURN_RELATED_BUNDLES BURN_RELATED_BUNDLES;

struct _BURN_PACKAGE;
typedef _BURN_PACKAGE BURN_PACKAGE;

// constants

const DWORD BURN_PACKAGE_INVALID_PATCH_INDEX = 0x80000000;

enum BURN_CACHE_PACKAGE_TYPE
{
    BURN_CACHE_PACKAGE_TYPE_NONE,
    BURN_CACHE_PACKAGE_TYPE_OPTIONAL,
    BURN_CACHE_PACKAGE_TYPE_REQUIRED,
};

enum BURN_EXE_DETECTION_TYPE
{
    BURN_EXE_DETECTION_TYPE_NONE,
    BURN_EXE_DETECTION_TYPE_CONDITION,
    BURN_EXE_DETECTION_TYPE_ARP,
};

enum BURN_EXE_EXIT_CODE_TYPE
{
    BURN_EXE_EXIT_CODE_TYPE_NONE,
    BURN_EXE_EXIT_CODE_TYPE_SUCCESS,
    BURN_EXE_EXIT_CODE_TYPE_ERROR,
    BURN_EXE_EXIT_CODE_TYPE_SCHEDULE_REBOOT,
    BURN_EXE_EXIT_CODE_TYPE_FORCE_REBOOT,
    BURN_EXE_EXIT_CODE_TYPE_ERROR_SCHEDULE_REBOOT,
    BURN_EXE_EXIT_CODE_TYPE_ERROR_FORCE_REBOOT,
};

enum BURN_EXE_PROTOCOL_TYPE
{
    BURN_EXE_PROTOCOL_TYPE_NONE,
    BURN_EXE_PROTOCOL_TYPE_BURN,
    BURN_EXE_PROTOCOL_TYPE_NETFX4,
};

enum BURN_PACKAGE_TYPE
{
    BURN_PACKAGE_TYPE_NONE,
    BURN_PACKAGE_TYPE_BUNDLE,
    BURN_PACKAGE_TYPE_EXE,
    BURN_PACKAGE_TYPE_MSI,
    BURN_PACKAGE_TYPE_MSP,
    BURN_PACKAGE_TYPE_MSU,
};

enum BURN_DEPENDENCY_ACTION
{
    BURN_DEPENDENCY_ACTION_NONE,
    BURN_DEPENDENCY_ACTION_UNREGISTER,
    BURN_DEPENDENCY_ACTION_REGISTER,
};

enum BURN_PATCH_TARGETCODE_TYPE
{
    BURN_PATCH_TARGETCODE_TYPE_UNKNOWN,
    BURN_PATCH_TARGETCODE_TYPE_PRODUCT,
    BURN_PATCH_TARGETCODE_TYPE_UPGRADE,
};

enum BOOTSTRAPPER_FEATURE_ACTION
{
    BOOTSTRAPPER_FEATURE_ACTION_NONE,
    BOOTSTRAPPER_FEATURE_ACTION_ADDLOCAL,
    BOOTSTRAPPER_FEATURE_ACTION_ADDSOURCE,
    BOOTSTRAPPER_FEATURE_ACTION_ADDDEFAULT,
    BOOTSTRAPPER_FEATURE_ACTION_REINSTALL,
    BOOTSTRAPPER_FEATURE_ACTION_ADVERTISE,
    BOOTSTRAPPER_FEATURE_ACTION_REMOVE,
};

enum BURN_PACKAGE_REGISTRATION_STATE
{
    BURN_PACKAGE_REGISTRATION_STATE_UNKNOWN,
    BURN_PACKAGE_REGISTRATION_STATE_ABSENT,
    BURN_PACKAGE_REGISTRATION_STATE_IGNORED,
    BURN_PACKAGE_REGISTRATION_STATE_PRESENT,
};

enum BURN_PATCH_SKIP_STATE
{
    BURN_PATCH_SKIP_STATE_NONE,
    BURN_PATCH_SKIP_STATE_TARGET_UNINSTALL,
    BURN_PATCH_SKIP_STATE_SLIPSTREAM,
};

// structs

typedef struct _BURN_EXE_EXIT_CODE
{
    BURN_EXE_EXIT_CODE_TYPE type;
    DWORD dwCode;
    BOOL fWildcard;
} BURN_EXE_EXIT_CODE;

typedef struct _BURN_EXE_COMMAND_LINE_ARGUMENT
{
    LPWSTR sczInstallArgument;
    LPWSTR sczUninstallArgument;
    LPWSTR sczRepairArgument;
    LPWSTR sczCondition;
} BURN_EXE_COMMAND_LINE_ARGUMENT;

typedef struct _BURN_MSPTARGETPRODUCT
{
    MSIINSTALLCONTEXT context;
    DWORD dwOrder;
    WCHAR wzTargetProductCode[39];
    BURN_PACKAGE* pChainedTargetPackage;
    BOOL fInstalled;
    BOOL fSlipstream;
    BOOL fSlipstreamRequired; // this means the target product is not present on the machine, but is available in the chain as a slipstream target.

    BOOTSTRAPPER_PACKAGE_STATE patchPackageState; // only valid after Detect.
    BOOTSTRAPPER_REQUEST_STATE defaultRequested;  // only valid during Plan.
    BOOTSTRAPPER_REQUEST_STATE requested;         // only valid during Plan.
    BOOTSTRAPPER_ACTION_STATE execute;            // only valid during Plan.
    BOOTSTRAPPER_ACTION_STATE rollback;           // only valid during Plan.
    BURN_PATCH_SKIP_STATE executeSkip;            // only valid during Plan.
    BURN_PATCH_SKIP_STATE rollbackSkip;           // only valid during Plan.

    BURN_PACKAGE_REGISTRATION_STATE registrationState;           // initialized during Detect, updated during Apply.
    BURN_PACKAGE_REGISTRATION_STATE transactionRegistrationState;// only valid during Apply inside an MSI transaction.
} BURN_MSPTARGETPRODUCT;

typedef struct _BURN_MSIPROPERTY
{
    LPWSTR sczId;
    LPWSTR sczValue; // used during forward execution
    LPWSTR sczRollbackValue;  // used during rollback
    LPWSTR sczCondition;
} BURN_MSIPROPERTY;

typedef struct _BURN_MSIFEATURE
{
    LPWSTR sczId;
    LPWSTR sczAddLocalCondition;
    LPWSTR sczAddSourceCondition;
    LPWSTR sczAdvertiseCondition;
    LPWSTR sczRollbackAddLocalCondition;
    LPWSTR sczRollbackAddSourceCondition;
    LPWSTR sczRollbackAdvertiseCondition;

    BOOTSTRAPPER_FEATURE_STATE currentState;       // only valid after Detect.
    BOOTSTRAPPER_FEATURE_STATE expectedState;      // only valid during Plan.
    BOOTSTRAPPER_FEATURE_STATE defaultRequested;   // only valid during Plan.
    BOOTSTRAPPER_FEATURE_STATE requested;          // only valid during Plan.
    BOOTSTRAPPER_FEATURE_ACTION execute;           // only valid during Plan.
    BOOTSTRAPPER_FEATURE_ACTION rollback;          // only valid during Plan.
} BURN_MSIFEATURE;

typedef struct _BURN_COMPATIBLE_PROVIDER_ENTRY
{
    LPWSTR sczProviderKey;
    LPWSTR sczId;
    LPWSTR sczName;
    LPWSTR sczVersion;
} BURN_COMPATIBLE_PROVIDER_ENTRY;

typedef struct _BURN_RELATED_MSI
{
    LPWSTR sczUpgradeCode;
    VERUTIL_VERSION* pMinVersion;
    VERUTIL_VERSION* pMaxVersion;
    BOOL fMinProvided;
    BOOL fMaxProvided;
    BOOL fMinInclusive;
    BOOL fMaxInclusive;
    BOOL fOnlyDetect;
    BOOL fLangInclusive;

    DWORD* rgdwLanguages;
    DWORD cLanguages;
} BURN_RELATED_MSI;

typedef struct _BURN_CHAINED_PATCH
{
    BURN_PACKAGE* pMspPackage;
    DWORD dwMspTargetProductIndex; // index into the Msp.rgTargetProducts
} BURN_CHAINED_PATCH;

typedef struct _BURN_SLIPSTREAM_MSP
{
    BURN_PACKAGE* pMspPackage;
    DWORD dwMsiChainedPatchIndex; // index into the Msi.rgChainedPatches

    BOOTSTRAPPER_ACTION_STATE execute;    // only valid during Plan.
    BOOTSTRAPPER_ACTION_STATE rollback;   // only valid during Plan.
} BURN_SLIPSTREAM_MSP;

typedef struct _BURN_DEPENDENCY_PROVIDER
{
    LPWSTR sczKey;
    LPWSTR sczVersion;
    LPWSTR sczDisplayName;
    BOOL fImported;

    BOOL fExists;                              // only valid after Detect.
    BOOL fBundleRegisteredAsDependent;         // only valid after Detect.
    DEPENDENCY* rgDependents;                  // only valid after Detect.
    UINT cDependents;                          // only valid after Detect.

    BURN_DEPENDENCY_ACTION dependentExecute;   // only valid during Plan.
    BURN_DEPENDENCY_ACTION dependentRollback;  // only valid during Plan.
    BURN_DEPENDENCY_ACTION providerExecute;    // only valid during Plan.
    BURN_DEPENDENCY_ACTION providerRollback;   // only valid during Plan.
} BURN_DEPENDENCY_PROVIDER;

typedef struct _BURN_ROLLBACK_BOUNDARY
{
    LPWSTR sczId;
    BOOL fVital;
    BOOL fTransactionAuthored;
    BOOL fTransaction;
    BOOL fActiveTransaction; // only valid during Apply.
    LPWSTR sczLogPathVariable;
    LPWSTR sczLogPath;
} BURN_ROLLBACK_BOUNDARY;

typedef struct _BURN_PATCH_TARGETCODE
{
    LPWSTR sczTargetCode;
    BURN_PATCH_TARGETCODE_TYPE type;
} BURN_PATCH_TARGETCODE;

typedef struct _BURN_COMPATIBLE_PACKAGE
{
    BOOL fDetected;
    BOOL fPlannable;
    BOOL fDefaultRequested;
    BOOL fRequested;
    BOOL fRemove;
    LPWSTR sczCacheId;
    BURN_COMPATIBLE_PROVIDER_ENTRY compatibleEntry;

    BURN_PACKAGE_TYPE type;
    union
    {
        struct
        {
            LPWSTR sczVersion;
            VERUTIL_VERSION* pVersion;
        } Msi;
    };
} BURN_COMPATIBLE_PACKAGE;

typedef struct _BURN_PACKAGE
{
    LPWSTR sczId;

    LPWSTR sczLogPathVariable;          // name of the variable that will be set to the log path.
    LPWSTR sczRollbackLogPathVariable;  // name of the variable that will be set to the rollback path.
    LPWSTR sczCompatibleLogPathVariable;

    LPWSTR sczInstallCondition;
    LPWSTR sczRepairCondition;
    BOOTSTRAPPER_PACKAGE_SCOPE scope;
    BOOL fPerMachine;                   // only valid after Plan (for PUOM/PMOU packages).
    BOOL fPermanent;
    BOOL fVital;
    BOOL fCanAffectRegistration;

    BOOTSTRAPPER_CACHE_TYPE authoredCacheType;
    LPWSTR sczCacheId;

    DWORD64 qwInstallSize;
    DWORD64 qwSize;

    BURN_ROLLBACK_BOUNDARY* pRollbackBoundaryForward;  // used during install and repair.
    BURN_ROLLBACK_BOUNDARY* pRollbackBoundaryBackward; // used during uninstall.

    BOOL fDetectedPerMachine;                   // only valid after Detect.
    BOOTSTRAPPER_PACKAGE_STATE currentState;    // only valid after Detect.
    BOOL fCached;                               // only valid after Detect.
    BOOTSTRAPPER_CACHE_TYPE cacheType;          // only valid during Plan.
    BOOTSTRAPPER_REQUEST_STATE defaultRequested;// only valid during Plan.
    BOOTSTRAPPER_REQUEST_STATE requested;       // only valid during Plan.
    BOOL fCacheVital;                           // only valid during Plan.
    BOOL fPlannedUncache;                       // only valid during Plan.
    BOOTSTRAPPER_ACTION_STATE execute;          // only valid during Plan.
    BOOTSTRAPPER_ACTION_STATE rollback;         // only valid during Plan.
    BOOL fProviderExecute;                      // only valid during Plan.
    BOOL fProviderRollback;                     // only valid during Plan.
    BURN_DEPENDENCY_ACTION dependencyExecute;   // only valid during Plan.
    BURN_DEPENDENCY_ACTION dependencyRollback;  // only valid during Plan.
    BOOL fDependencyManagerWasHere;             // only valid during Plan.
    BURN_CACHE_PACKAGE_TYPE executeCacheType;   // only valid during Plan.
    BURN_CACHE_PACKAGE_TYPE rollbackCacheType;  // only valid during Plan.
    HANDLE hCacheEvent;                         // only valid during Plan.
    LPWSTR sczCacheFolder;                      // only valid during Apply.
    HRESULT hrCacheResult;                      // only valid during Apply.
    BOOL fAcquireOptionalSource;                // only valid during Apply.
    BOOL fReachedExecution;                     // only valid during Apply.
    BOOL fAbandonedProcess;                     // only valid during Apply.

    BURN_PACKAGE_REGISTRATION_STATE cacheRegistrationState;          // initialized during Detect, updated during Apply.
    BURN_PACKAGE_REGISTRATION_STATE installRegistrationState;        // initialized during Detect, updated during Apply.
    BURN_PACKAGE_REGISTRATION_STATE expectedCacheRegistrationState;  // only valid after Plan.
    BURN_PACKAGE_REGISTRATION_STATE expectedInstallRegistrationState;// only valid after Plan.
    BURN_PACKAGE_REGISTRATION_STATE transactionRegistrationState;    // only valid during Apply inside an MSI transaction.

    BURN_PAYLOAD_GROUP payloads;

    BURN_DEPENDENCY_PROVIDER* rgDependencyProviders;
    DWORD cDependencyProviders;

    BURN_COMPATIBLE_PACKAGE compatiblePackage;

    BURN_PACKAGE_TYPE type;
    union
    {
        struct
        {
            LPWSTR sczBundleCode;
            LPWSTR sczArpKeyPath;
            VERUTIL_VERSION* pVersion;
            LPWSTR sczRegistrationKey;
            LPWSTR sczInstallArguments;
            LPWSTR sczRepairArguments;
            LPWSTR sczUninstallArguments;

            LPWSTR* rgsczDetectCodes;
            DWORD cDetectCodes;

            LPWSTR* rgsczUpgradeCodes;
            DWORD cUpgradeCodes;

            LPWSTR* rgsczAddonCodes;
            DWORD cAddonCodes;

            LPWSTR* rgsczPatchCodes;
            DWORD cPatchCodes;

            BOOL fHideARP;
            BOOL fWin64;
            BOOL fSupportsBurnProtocol;

            BURN_EXE_EXIT_CODE* rgExitCodes;
            DWORD cExitCodes;

            BURN_EXE_COMMAND_LINE_ARGUMENT* rgCommandLineArguments;
            DWORD cCommandLineArguments;

            LPWSTR sczIgnoreDependencies;
            LPCWSTR wzAncestors; // points directly into engine state.
            LPCWSTR wzEngineWorkingDirectory; // points directly into engine state.
        } Bundle;
        struct
        {
            BURN_EXE_DETECTION_TYPE detectionType;

            BOOL fArpWin64;
            BOOL fArpUseUninstallString;
            LPWSTR sczArpKeyPath;
            VERUTIL_VERSION* pArpDisplayVersion;

            LPWSTR sczDetectCondition;
            LPWSTR sczInstallArguments;
            LPWSTR sczRepairArguments;
            LPWSTR sczUninstallArguments;
            LPCWSTR wzAncestors; // points directly into engine state.
            LPCWSTR wzEngineWorkingDirectory; // points directly into engine state.

            BOOL fBundle;
            BOOL fPseudoPackage;
            BOOL fFireAndForget;
            BOOL fRepairable;
            BOOL fUninstallable;
            BURN_EXE_PROTOCOL_TYPE protocol;

            BURN_EXE_EXIT_CODE* rgExitCodes;
            DWORD cExitCodes;

            BURN_EXE_COMMAND_LINE_ARGUMENT* rgCommandLineArguments;
            DWORD cCommandLineArguments;
        } Exe;
        struct
        {
            LPWSTR sczProductCode;
            DWORD dwLanguage;
            VERUTIL_VERSION* pVersion;
            LPWSTR sczUpgradeCode;

            BOOTSTRAPPER_RELATED_OPERATION operation;

            BURN_MSIPROPERTY* rgProperties;
            DWORD cProperties;

            BURN_MSIFEATURE* rgFeatures;
            DWORD cFeatures;

            BURN_RELATED_MSI* rgRelatedMsis;
            DWORD cRelatedMsis;

            BURN_SLIPSTREAM_MSP* rgSlipstreamMsps;
            LPWSTR* rgsczSlipstreamMspPackageIds;
            DWORD cSlipstreamMspPackages;

            BURN_CHAINED_PATCH* rgChainedPatches;
            DWORD cChainedPatches;
        } Msi;
        struct
        {
            LPWSTR sczPatchCode;
            LPWSTR sczApplicabilityXml;

            BURN_MSIPROPERTY* rgProperties;
            DWORD cProperties;

            BURN_MSPTARGETPRODUCT* rgTargetProducts;
            DWORD cTargetProductCodes;
        } Msp;
        struct
        {
            LPWSTR sczDetectCondition;
        } Msu;
    };
} BURN_PACKAGE;

typedef struct _BURN_PACKAGES
{
    BURN_ROLLBACK_BOUNDARY* rgRollbackBoundaries;
    DWORD cRollbackBoundaries;

    BURN_PACKAGE* rgPackages;
    DWORD cPackages;

    BURN_PATCH_TARGETCODE* rgPatchTargetCodes;
    DWORD cPatchTargetCodes;

    MSIPATCHSEQUENCEINFOW* rgPatchInfo;
    BURN_PACKAGE** rgPatchInfoToPackage; // direct lookup from patch information to the (MSP) package it describes.
                                         // Thus this array is the exact same size as rgPatchInfo.
    DWORD cPatchInfo;
} BURN_PACKAGES;


// function declarations

HRESULT PackagesParseFromXml(
    __in BURN_PACKAGES* pPackages,
    __in BURN_PAYLOADS* pPayloads,
    __in IXMLDOMNode* pixnBundle
    );
void PackageUninitialize(
    __in BURN_PACKAGE* pPackage
    );
void PackageUninitializeCompatible(
    __in BURN_COMPATIBLE_PACKAGE* pCompatiblePackage
    );
void PackagesUninitialize(
    __in BURN_PACKAGES* pPackages
    );
HRESULT PackageFindById(
    __in BURN_PACKAGES* pPackages,
    __in_z LPCWSTR wzId,
    __out BURN_PACKAGE** ppPackage
    );
HRESULT PackageFindRelatedById(
    __in BURN_RELATED_BUNDLES* pRelatedBundles,
    __in_z LPCWSTR wzId,
    __out BURN_PACKAGE** ppPackage
    );
HRESULT PackageGetProperty(
    __in const BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzProperty,
    __out_z_opt LPWSTR* psczValue
    );
HRESULT PackageFindRollbackBoundaryById(
    __in BURN_PACKAGES* pPackages,
    __in_z LPCWSTR wzId,
    __out BURN_ROLLBACK_BOUNDARY** ppRollbackBoundary
    );
HRESULT PackageParseScopeFromXml(
    __in IXMLDOMNode* pixn,
    __in BOOTSTRAPPER_PACKAGE_SCOPE* pScope
    );


#if defined(__cplusplus)
}
#endif
