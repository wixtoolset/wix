#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

const LPCWSTR BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH = L"BundleCachePath";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE = L"BundleAddonCode";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE = L"BundleDetectCode";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE = L"BundlePatchCode";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE = L"BundleUpgradeCode";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_DISPLAY_NAME = L"DisplayName";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION = L"BundleVersion";
const LPCWSTR BURN_REGISTRATION_REGISTRY_ENGINE_VERSION = L"EngineVersion";
const LPCWSTR BURN_REGISTRATION_REGISTRY_ENGINE_PROTOCOL_VERSION = L"EngineProtocolVersion";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY = L"BundleProviderKey";
const LPCWSTR BURN_REGISTRATION_REGISTRY_BUNDLE_TAG = L"BundleTag";

const LPCWSTR REGISTRY_BUNDLE_INSTALLED = L"Installed";

enum BURN_RESUME_MODE
{
    BURN_RESUME_MODE_NONE,
    BURN_RESUME_MODE_ACTIVE,
    BURN_RESUME_MODE_SUSPEND,
    BURN_RESUME_MODE_ARP,
    BURN_RESUME_MODE_REBOOT_PENDING,
};

enum BURN_REGISTRATION_MODIFY_TYPE
{
    BURN_REGISTRATION_MODIFY_ENABLED,
    BURN_REGISTRATION_MODIFY_DISABLE,
    BURN_REGISTRATION_MODIFY_DISABLE_BUTTON,
};


// structs

typedef struct _BURN_UPDATE_REGISTRATION
{
    BOOL fRegisterUpdate;
    LPWSTR sczManufacturer;
    LPWSTR sczDepartment;
    LPWSTR sczProductFamily;
    LPWSTR sczName;
    LPWSTR sczClassification;
} BURN_UPDATE_REGISTRATION;

typedef struct _BURN_RELATED_BUNDLE
{
    BOOTSTRAPPER_RELATION_TYPE detectRelationType;
    BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE defaultPlanRelationType;
    BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE planRelationType;
    BOOL fForwardCompatible;

    VERUTIL_VERSION* pVersion;
    LPWSTR sczTag;
    BOOL fPlannable;

    BURN_PACKAGE package;

    BOOTSTRAPPER_REQUEST_STATE defaultRequestedRestore;
    BOOTSTRAPPER_REQUEST_STATE requestedRestore;
    BOOTSTRAPPER_ACTION_STATE restore;
} BURN_RELATED_BUNDLE;

typedef struct _BURN_RELATED_BUNDLES
{
    BURN_RELATED_BUNDLE* rgRelatedBundles;
    DWORD cRelatedBundles;
    BURN_RELATED_BUNDLE** rgpPlanSortedRelatedBundles;
} BURN_RELATED_BUNDLES;

typedef struct _BURN_SOFTWARE_TAG
{
    LPWSTR sczFilename;
    LPWSTR sczRegid;
    LPWSTR sczPath;
    LPSTR sczTag;
} BURN_SOFTWARE_TAG;

typedef struct _BURN_SOFTWARE_TAGS
{
    BURN_SOFTWARE_TAG* rgSoftwareTags;
    DWORD cSoftwareTags;
} BURN_SOFTWARE_TAGS;

typedef struct _BURN_REGISTRATION
{
    // For configurable-scope bundles, fPerMachine is only valid after
    // planning when scope is known. For fixed per-machine or per-user
    // bundles, valid immediately.
    BOOL fPerMachine;
    BOOL fForceSystemComponent;
    BOOL fDisableResume;
    BOOL fCached;
    BOOTSTRAPPER_REGISTRATION_TYPE detectedRegistrationType;
    BOOTSTRAPPER_SCOPE detectedScope;
    BOOTSTRAPPER_PACKAGE_SCOPE scope;
    LPWSTR sczCode;
    LPWSTR sczTag;

    LPWSTR *rgsczDetectCodes;
    DWORD cDetectCodes;

    LPWSTR *rgsczUpgradeCodes;
    DWORD cUpgradeCodes;

    LPWSTR *rgsczAddonCodes;
    DWORD cAddonCodes;

    LPWSTR *rgsczPatchCodes;
    DWORD cPatchCodes;

    VERUTIL_VERSION* pVersion;
    LPWSTR sczProviderKey;
    LPWSTR sczExecutableName;

    // paths
    HKEY hkRoot;
    LPWSTR sczRegistrationKey;
    LPWSTR sczCacheExecutablePath;
    LPWSTR sczResumeCommandLine;
    LPWSTR sczStateFile;

    // ARP registration
    LPWSTR sczDisplayName;
    LPWSTR sczInProgressDisplayName;
    LPWSTR sczDisplayVersion;
    LPWSTR sczPublisher;
    LPWSTR sczHelpLink;
    LPWSTR sczHelpTelephone;
    LPWSTR sczAboutUrl;
    LPWSTR sczUpdateUrl;
    LPWSTR sczParentDisplayName;
    LPWSTR sczComments;
    //LPWSTR sczReadme; // TODO: this would be a file path
    LPWSTR sczContact;
    //DWORD64 qwEstimatedSize; // TODO: size should come from disk cost calculation
    BURN_REGISTRATION_MODIFY_TYPE modify;
    BOOL fNoRemove;

    BURN_SOFTWARE_TAGS softwareTags;

    // Update registration
    BURN_UPDATE_REGISTRATION update;

    BURN_RELATED_BUNDLES relatedBundles; // Only valid after detect.
    DEPENDENCY* rgDependents;            // Only valid after detect.
    UINT cDependents;                    // Only valid after detect.
    BOOL fSelfRegisteredAsDependent;     // Only valid after detect.
    BOOL fParentRegisteredAsDependent;   // Only valid after detect.
    BOOL fForwardCompatibleBundleExists; // Only valid after detect.
    BOOL fEligibleForCleanup;            // Only valid after detect.

    BOOL fDetectedForeignProviderKeyBundleCode;
    LPWSTR sczDetectedProviderKeyBundleCode;
    LPWSTR sczBundlePackageAncestors;
} BURN_REGISTRATION;


// functions

HRESULT RegistrationParseFromXml(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_CACHE* pCache,
    __in IXMLDOMNode* pixnBundle
);
void RegistrationUninitialize(
    __in BURN_REGISTRATION* pRegistration
    );
HRESULT RegistrationSetVariables(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    );
HRESULT RegistrationSetDynamicVariables(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    );
HRESULT RegistrationDetectInstalled(
    __in BURN_REGISTRATION* pRegistration
    );
HRESULT RegistrationDetectResumeType(
    __in BURN_REGISTRATION* pRegistration,
    __out BOOTSTRAPPER_RESUME_TYPE* pResumeType
    );
HRESULT RegistrationDetectRelatedBundles(
    __in BURN_REGISTRATION* pRegistration
    );
HRESULT RegistrationPlanInitialize(
    __in BURN_REGISTRATION* pRegistration
);
HRESULT RegistrationSessionBegin(
    __in_z LPCWSTR wzEngineWorkingPath,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in DWORD dwRegistrationOptions,
    __in DWORD64 qwEstimatedSize,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType
    );
HRESULT RegistrationSessionEnd(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PACKAGES* pPackages,
    __in BURN_RESUME_MODE resumeMode,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __in DWORD64 qwEstimatedSize,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType
    );
HRESULT RegistrationSaveState(
    __in BURN_REGISTRATION* pRegistration,
    __in_bcount_opt(cbBuffer) BYTE* pbBuffer,
    __in_opt SIZE_T cbBuffer
    );
HRESULT RegistrationLoadState(
    __in BURN_REGISTRATION* pRegistration,
    __out_bcount(*pcbBuffer) BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    );
HRESULT RegistrationGetResumeCommandLine(
    __in const BURN_REGISTRATION* pRegistration,
    __deref_out_z LPWSTR* psczResumeCommandLine
    );
HRESULT RegistrationSetPaths(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_CACHE* pCache
    );


#if defined(__cplusplus)
}
#endif
