// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// constants

const LPCWSTR REGISTRY_RUN_ONCE_KEY = L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce";
const LPCWSTR REGISTRY_BUNDLE_DISPLAY_ICON = L"DisplayIcon";
const LPCWSTR REGISTRY_BUNDLE_DISPLAY_VERSION = L"DisplayVersion";
const LPCWSTR REGISTRY_BUNDLE_ESTIMATED_SIZE = L"EstimatedSize";
const LPCWSTR REGISTRY_BUNDLE_INSTALL_DATE = L"InstallDate";
const LPCWSTR REGISTRY_BUNDLE_PUBLISHER = L"Publisher";
const LPCWSTR REGISTRY_BUNDLE_HELP_LINK = L"HelpLink";
const LPCWSTR REGISTRY_BUNDLE_HELP_TELEPHONE = L"HelpTelephone";
const LPCWSTR REGISTRY_BUNDLE_URL_INFO_ABOUT = L"URLInfoAbout";
const LPCWSTR REGISTRY_BUNDLE_URL_UPDATE_INFO = L"URLUpdateInfo";
const LPCWSTR REGISTRY_BUNDLE_PARENT_DISPLAY_NAME = L"ParentDisplayName";
const LPCWSTR REGISTRY_BUNDLE_PARENT_KEY_NAME = L"ParentKeyName";
const LPCWSTR REGISTRY_BUNDLE_COMMENTS = L"Comments";
const LPCWSTR REGISTRY_BUNDLE_CONTACT = L"Contact";
const LPCWSTR REGISTRY_BUNDLE_NO_MODIFY = L"NoModify";
const LPCWSTR REGISTRY_BUNDLE_MODIFY_PATH = L"ModifyPath";
const LPCWSTR REGISTRY_BUNDLE_NO_ELEVATE_ON_MODIFY = L"NoElevateOnModify";
const LPCWSTR REGISTRY_BUNDLE_NO_REMOVE = L"NoRemove";
const LPCWSTR REGISTRY_BUNDLE_SYSTEM_COMPONENT = L"SystemComponent";
const LPCWSTR REGISTRY_BUNDLE_QUIET_UNINSTALL_STRING = L"QuietUninstallString";
const LPCWSTR REGISTRY_BUNDLE_UNINSTALL_STRING = L"UninstallString";
const LPCWSTR REGISTRY_BUNDLE_RESUME_COMMAND_LINE = L"BundleResumeCommandLine";
const LPCWSTR REGISTRY_BUNDLE_VERSION_MAJOR = L"VersionMajor";
const LPCWSTR REGISTRY_BUNDLE_VERSION_MINOR = L"VersionMinor";
const LPCWSTR SWIDTAG_FOLDER = L"swidtag";
const LPCWSTR REGISTRY_BUNDLE_VARIABLE_KEY = L"variables";

// internal function declarations

static HRESULT ParseSoftwareTagsFromXml(
    __in IXMLDOMNode* pixnRegistrationNode,
    __out BURN_SOFTWARE_TAG** prgSoftwareTags,
    __out DWORD* pcSoftwareTags
    );
static HRESULT SetPaths(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_CACHE* pCache
    );
static HRESULT GetBundleManufacturer(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczBundleManufacturer
    );
static HRESULT GetBundleInProgressName(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczBundleName
    );
static HRESULT GetBundleName(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczBundleName
    );
static HRESULT EnsureRegistrationVariable(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z LPCWSTR wzDefaultValue,
    __out_z LPWSTR* psczValue
    );
static HRESULT UpdateResumeMode(
    __in BURN_REGISTRATION* pRegistration,
    __in HKEY hkRegistration,
    __in BURN_RESUME_MODE resumeMode,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType,
    __in BOOL fRestartInitiated
    );
static HRESULT FormatUpdateRegistrationKey(
    __in BURN_REGISTRATION* pRegistration,
    __out_z LPWSTR* psczKey
    );
static HRESULT WriteSoftwareTags(
    __in BURN_VARIABLES* pVariables,
    __in BURN_SOFTWARE_TAGS* pSoftwareTags
    );
static HRESULT RemoveSoftwareTags(
    __in BURN_VARIABLES* pVariables,
    __in BURN_SOFTWARE_TAGS* pSoftwareTags
    );
static HRESULT WriteUpdateRegistration(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    );
static HRESULT RemoveUpdateRegistration(
    __in BURN_REGISTRATION* pRegistration
    );
static HRESULT RegWriteStringVariable(
    __in HKEY hkKey,
    __in BURN_VARIABLES* pVariables,
    __in LPCWSTR wzVariable,
    __in LPCWSTR wzName
    );
static HRESULT UpdateBundleNameRegistration(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in HKEY hkRegistration,
    __in BOOL fInProgressRegistration
    );
static HRESULT UpdateEstimatedSize(
    __in HKEY hkRegistration,
    __in DWORD64 qwEstimatedSize
    );
static BOOL IsWuRebootPending();
static BOOL IsRegistryRebootPending();

// function definitions

/*******************************************************************
 RegistrationParseFromXml - Parses registration information from manifest.

*******************************************************************/
extern "C" HRESULT RegistrationParseFromXml(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_CACHE* pCache,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixnRegistrationNode = NULL;
    IXMLDOMNode* pixnArpNode = NULL;
    IXMLDOMNode* pixnUpdateNode = NULL;
    LPWSTR scz = NULL;
    BOOL fFoundXml = FALSE;

    // select registration node
    hr = XmlSelectSingleNode(pixnBundle, L"Registration", &pixnRegistrationNode);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to select registration node.");

    // @Id
    hr = XmlGetAttributeEx(pixnRegistrationNode, L"Id", &pRegistration->sczId);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Id.");

    // @Tag
    hr = XmlGetAttributeEx(pixnRegistrationNode, L"Tag", &pRegistration->sczTag);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Tag.");

    hr = BundlePackageEngineParseRelatedCodes(pixnBundle, &pRegistration->rgsczDetectCodes, &pRegistration->cDetectCodes, &pRegistration->rgsczUpgradeCodes, &pRegistration->cUpgradeCodes, &pRegistration->rgsczAddonCodes, &pRegistration->cAddonCodes, &pRegistration->rgsczPatchCodes, &pRegistration->cPatchCodes);
    ExitOnFailure(hr, "Failed to parse related bundles");

    // @Version
    hr = XmlGetAttributeEx(pixnRegistrationNode, L"Version", &scz);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Version.");

    hr = VerParseVersion(scz, 0, FALSE, &pRegistration->pVersion);
    ExitOnFailure(hr, "Failed to parse @Version: %ls", scz);

    if (pRegistration->pVersion->fInvalid)
    {
        LogId(REPORT_WARNING, MSG_MANIFEST_INVALID_VERSION, scz);
    }

    // @ProviderKey
    hr = XmlGetAttributeEx(pixnRegistrationNode, L"ProviderKey", &pRegistration->sczProviderKey);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get @ProviderKey.");

    // @ExecutableName
    hr = XmlGetAttributeEx(pixnRegistrationNode, L"ExecutableName", &pRegistration->sczExecutableName);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get @ExecutableName.");

    // @PerMachine
    hr = XmlGetYesNoAttribute(pixnRegistrationNode, L"PerMachine", &pRegistration->fPerMachine);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get @PerMachine.");

    // select ARP node
    hr = XmlSelectSingleNode(pixnRegistrationNode, L"Arp", &pixnArpNode);
    ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to select ARP node.");

    if (fFoundXml)
    {
        // @DisplayName
        hr = XmlGetAttributeEx(pixnArpNode, L"DisplayName", &pRegistration->sczDisplayName);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @DisplayName.");

        // @InProgressDisplayName
        hr = XmlGetAttributeEx(pixnArpNode, L"InProgressDisplayName", &pRegistration->sczInProgressDisplayName);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @InProgressDisplayName.");

        // @DisplayVersion
        hr = XmlGetAttributeEx(pixnArpNode, L"DisplayVersion", &pRegistration->sczDisplayVersion);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @DisplayVersion.");

        // @Publisher
        hr = XmlGetAttributeEx(pixnArpNode, L"Publisher", &pRegistration->sczPublisher);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @Publisher.");

        // @HelpLink
        hr = XmlGetAttributeEx(pixnArpNode, L"HelpLink", &pRegistration->sczHelpLink);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @HelpLink.");

        // @HelpTelephone
        hr = XmlGetAttributeEx(pixnArpNode, L"HelpTelephone", &pRegistration->sczHelpTelephone);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @HelpTelephone.");

        // @AboutUrl
        hr = XmlGetAttributeEx(pixnArpNode, L"AboutUrl", &pRegistration->sczAboutUrl);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @AboutUrl.");

        // @UpdateUrl
        hr = XmlGetAttributeEx(pixnArpNode, L"UpdateUrl", &pRegistration->sczUpdateUrl);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @UpdateUrl.");

        // @ParentDisplayName
        hr = XmlGetAttributeEx(pixnArpNode, L"ParentDisplayName", &pRegistration->sczParentDisplayName);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @ParentDisplayName.");

        // @Comments
        hr = XmlGetAttributeEx(pixnArpNode, L"Comments", &pRegistration->sczComments);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @Comments.");

        // @Contact
        hr = XmlGetAttributeEx(pixnArpNode, L"Contact", &pRegistration->sczContact);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @Contact.");

        // @DisableModify
        hr = XmlGetAttributeEx(pixnArpNode, L"DisableModify", &scz);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @DisableModify.");

        if (fFoundXml)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"button", -1))
            {
                pRegistration->modify = BURN_REGISTRATION_MODIFY_DISABLE_BUTTON;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"yes", -1))
            {
                pRegistration->modify = BURN_REGISTRATION_MODIFY_DISABLE;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"no", -1))
            {
                pRegistration->modify = BURN_REGISTRATION_MODIFY_ENABLED;
            }
            else
            {
                ExitWithRootFailure(hr, E_UNEXPECTED, "Invalid modify disabled type: %ls", scz);
            }
        }
        else
        {
            pRegistration->modify = BURN_REGISTRATION_MODIFY_ENABLED;
        }

        // @DisableRemove
        hr = XmlGetYesNoAttribute(pixnArpNode, L"DisableRemove", &pRegistration->fNoRemove);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @DisableRemove.");
    }

    if (pRegistration->fNoRemove && BURN_REGISTRATION_MODIFY_ENABLED != pRegistration->modify)
    {
        pRegistration->fForceSystemComponent = TRUE;
    }

    hr = ParseSoftwareTagsFromXml(pixnRegistrationNode, &pRegistration->softwareTags.rgSoftwareTags, &pRegistration->softwareTags.cSoftwareTags);
    ExitOnFailure(hr, "Failed to parse software tag.");

    // select Update node
    hr = XmlSelectSingleNode(pixnRegistrationNode, L"Update", &pixnUpdateNode);
    ExitOnOptionalXmlQueryFailure(hr, pRegistration->update.fRegisterUpdate, "Failed to select Update node.");

    if (pRegistration->update.fRegisterUpdate)
    {
        // @Manufacturer
        hr = XmlGetAttributeEx(pixnUpdateNode, L"Manufacturer", &pRegistration->update.sczManufacturer);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Manufacturer.");

        // @Department
        hr = XmlGetAttributeEx(pixnUpdateNode, L"Department", &pRegistration->update.sczDepartment);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @Department.");

        // @ProductFamily
        hr = XmlGetAttributeEx(pixnUpdateNode, L"ProductFamily", &pRegistration->update.sczProductFamily);
        ExitOnOptionalXmlQueryFailure(hr, fFoundXml, "Failed to get @ProductFamily.");

        // @Name
        hr = XmlGetAttributeEx(pixnUpdateNode, L"Name", &pRegistration->update.sczName);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Name.");

        // @Classification
        hr = XmlGetAttributeEx(pixnUpdateNode, L"Classification", &pRegistration->update.sczClassification);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Classification.");
    }

    hr = SetPaths(pRegistration, pCache);
    ExitOnFailure(hr, "Failed to set registration paths.");

LExit:
    ReleaseObject(pixnRegistrationNode);
    ReleaseObject(pixnArpNode);
    ReleaseObject(pixnUpdateNode);
    ReleaseStr(scz);

    return hr;
}

/*******************************************************************
 RegistrationUninitialize -

*******************************************************************/
extern "C" void RegistrationUninitialize(
    __in BURN_REGISTRATION* pRegistration
    )
{
    ReleaseStr(pRegistration->sczId);
    ReleaseStr(pRegistration->sczTag);

    for (DWORD i = 0; i < pRegistration->cDetectCodes; ++i)
    {
        ReleaseStr(pRegistration->rgsczDetectCodes[i]);
    }
    ReleaseMem(pRegistration->rgsczDetectCodes);

    for (DWORD i = 0; i < pRegistration->cUpgradeCodes; ++i)
    {
        ReleaseStr(pRegistration->rgsczUpgradeCodes[i]);
    }
    ReleaseMem(pRegistration->rgsczUpgradeCodes);

    for (DWORD i = 0; i < pRegistration->cAddonCodes; ++i)
    {
        ReleaseStr(pRegistration->rgsczAddonCodes[i]);
    }
    ReleaseMem(pRegistration->rgsczAddonCodes);

    for (DWORD i = 0; i < pRegistration->cPatchCodes; ++i)
    {
        ReleaseStr(pRegistration->rgsczPatchCodes[i]);
    }
    ReleaseMem(pRegistration->rgsczPatchCodes);

    ReleaseStr(pRegistration->sczProviderKey);
    ReleaseStr(pRegistration->sczExecutableName);

    ReleaseStr(pRegistration->sczRegistrationKey);
    ReleaseStr(pRegistration->sczCacheExecutablePath);
    ReleaseStr(pRegistration->sczResumeCommandLine);
    ReleaseStr(pRegistration->sczStateFile);

    ReleaseStr(pRegistration->sczDisplayName);
    ReleaseStr(pRegistration->sczInProgressDisplayName);
    ReleaseStr(pRegistration->sczDisplayVersion);
    ReleaseStr(pRegistration->sczPublisher);
    ReleaseStr(pRegistration->sczHelpLink);
    ReleaseStr(pRegistration->sczHelpTelephone);
    ReleaseStr(pRegistration->sczAboutUrl);
    ReleaseStr(pRegistration->sczUpdateUrl);
    ReleaseStr(pRegistration->sczParentDisplayName);
    ReleaseStr(pRegistration->sczComments);
    ReleaseStr(pRegistration->sczContact);

    ReleaseStr(pRegistration->update.sczManufacturer);
    ReleaseStr(pRegistration->update.sczDepartment);
    ReleaseStr(pRegistration->update.sczProductFamily);
    ReleaseStr(pRegistration->update.sczName);
    ReleaseStr(pRegistration->update.sczClassification);

    if (pRegistration->softwareTags.rgSoftwareTags)
    {
        for (DWORD i = 0; i < pRegistration->softwareTags.cSoftwareTags; ++i)
        {
            ReleaseStr(pRegistration->softwareTags.rgSoftwareTags[i].sczFilename);
            ReleaseStr(pRegistration->softwareTags.rgSoftwareTags[i].sczRegid);
            ReleaseStr(pRegistration->softwareTags.rgSoftwareTags[i].sczPath);
            ReleaseStr(pRegistration->softwareTags.rgSoftwareTags[i].sczTag);
        }

        MemFree(pRegistration->softwareTags.rgSoftwareTags);
    }

    ReleaseStr(pRegistration->sczDetectedProviderKeyBundleId);
    ReleaseStr(pRegistration->sczBundlePackageAncestors);
    RelatedBundlesUninitialize(&pRegistration->relatedBundles);

    if (pRegistration->rgDependents)
    {
        ReleaseDependencyArray(pRegistration->rgDependents, pRegistration->cDependents);
    }
    // clear struct
    memset(pRegistration, 0, sizeof(BURN_REGISTRATION));
}

/*******************************************************************
 RegistrationSetVariables - Initializes bundle variables that map to
                            registration entities.

*******************************************************************/
extern "C" HRESULT RegistrationSetVariables(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR scz = NULL;

    // Ensure the registration bundle name is updated.
    hr = GetBundleInProgressName(pRegistration, pVariables, &scz);
    ExitOnFailure(hr, "Failed to initialize bundle name.");

    hr = GetBundleName(pRegistration, pVariables, &scz);
    ExitOnFailure(hr, "Failed to initialize bundle name.");

    hr = GetBundleManufacturer(pRegistration, pVariables, &scz);
    ExitOnFailure(hr, "Failed to initialize bundle manufacturer.");

    hr = VariableSetString(pVariables, BURN_BUNDLE_PROVIDER_KEY, pRegistration->sczProviderKey, TRUE, FALSE);
    ExitOnFailure(hr, "Failed to overwrite the bundle provider key built-in variable.");

    hr = VariableSetString(pVariables, BURN_BUNDLE_TAG, pRegistration->sczTag, TRUE, FALSE);
    ExitOnFailure(hr, "Failed to overwrite the bundle tag built-in variable.");

    hr = VariableSetVersion(pVariables, BURN_BUNDLE_VERSION, pRegistration->pVersion, TRUE);
    ExitOnFailure(hr, "Failed to overwrite the bundle version built-in variable.");

LExit:
    ReleaseStr(scz);

    return hr;
}

/*******************************************************************
 RegistrationSetDynamicVariables - Initializes bundle variables that
     map to registration entities that can change during execution.

*******************************************************************/
extern "C" HRESULT RegistrationSetDynamicVariables(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LONGLONG llInstalled = 0;

    // Detect if bundle is already installed.
    hr = RegistrationDetectInstalled(pRegistration);
    ExitOnFailure(hr, "Failed to detect bundle install state.");

    llInstalled = BOOTSTRAPPER_REGISTRATION_TYPE_FULL == pRegistration->detectedRegistrationType ? 1 : 0;

    hr = VariableSetNumeric(pVariables, BURN_BUNDLE_INSTALLED, llInstalled, TRUE);
    ExitOnFailure(hr, "Failed to set the bundle installed built-in variable.");

    hr = VariableSetNumeric(pVariables, VARIABLE_REBOOTPENDING, IsWuRebootPending() || IsRegistryRebootPending(), TRUE);
    ExitOnFailure(hr, "Failed to overwrite the bundle reboot-pending built-in variable.");

LExit:
    return hr;
}

extern "C" HRESULT RegistrationDetectInstalled(
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;
    HKEY hkRegistration = NULL;
    DWORD dwInstalled = 0;

    pRegistration->fCached = FileExistsEx(pRegistration->sczCacheExecutablePath, NULL);
    pRegistration->detectedRegistrationType = BOOTSTRAPPER_REGISTRATION_TYPE_NONE;

    // open registration key
    hr = RegOpen(pRegistration->hkRoot, pRegistration->sczRegistrationKey, KEY_QUERY_VALUE, &hkRegistration);
    if (SUCCEEDED(hr))
    {
        hr = RegReadNumber(hkRegistration, REGISTRY_BUNDLE_INSTALLED, &dwInstalled);

        pRegistration->detectedRegistrationType = (1 == dwInstalled) ? BOOTSTRAPPER_REGISTRATION_TYPE_FULL : BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS;
    }

    // Not finding the key or value is okay.
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        hr = S_OK;
    }

    ReleaseRegKey(hkRegistration);
    return hr;
}

/*******************************************************************
 RegistrationDetectResumeMode - Detects registration information on the system
                                to determine if a resume is taking place.

*******************************************************************/
extern "C" HRESULT RegistrationDetectResumeType(
    __in BURN_REGISTRATION* pRegistration,
    __out BOOTSTRAPPER_RESUME_TYPE* pResumeType
    )
{
    HRESULT hr = S_OK;
    HKEY hkRegistration = NULL;
    BOOL fExists = FALSE;
    DWORD dwResume = 0;

    // open registration key
    hr = RegOpen(pRegistration->hkRoot, pRegistration->sczRegistrationKey, KEY_QUERY_VALUE, &hkRegistration);
    ExitOnPathFailure(hr, fExists, "Failed to open registration key.");

    if (!fExists)
    {
        *pResumeType = BOOTSTRAPPER_RESUME_TYPE_NONE;
        ExitFunction();
    }

    // read Resume value
    hr = RegReadNumber(hkRegistration, L"Resume", &dwResume);
    ExitOnPathFailure(hr, fExists, "Failed to read Resume value.");

    if (!fExists)
    {
        *pResumeType = BOOTSTRAPPER_RESUME_TYPE_INVALID;
        ExitFunction();
    }

    switch (dwResume)
    {
    case BURN_RESUME_MODE_ACTIVE:
        // a previous run was interrupted
        *pResumeType = BOOTSTRAPPER_RESUME_TYPE_INTERRUPTED;
        break;

    case BURN_RESUME_MODE_SUSPEND:
        *pResumeType = BOOTSTRAPPER_RESUME_TYPE_SUSPEND;
        break;

    case BURN_RESUME_MODE_ARP:
        *pResumeType = BOOTSTRAPPER_RESUME_TYPE_ARP;
        break;

    case BURN_RESUME_MODE_REBOOT_PENDING:
        // The volatile pending registry doesn't exist (checked above) which means
        // the system was successfully restarted.
        *pResumeType = BOOTSTRAPPER_RESUME_TYPE_REBOOT;
        break;

    default:
        // the value stored in the registry is not valid
        *pResumeType = BOOTSTRAPPER_RESUME_TYPE_INVALID;
        break;
    }

LExit:
    ReleaseRegKey(hkRegistration);

    return hr;
}

/*******************************************************************
 RegistrationDetectRelatedBundles - finds the bundles with same
  upgrade/detect/addon/patch codes.

*******************************************************************/
extern "C" HRESULT RegistrationDetectRelatedBundles(
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;

    hr = RelatedBundlesInitializeForScope(TRUE, pRegistration, &pRegistration->relatedBundles);
    ExitOnFailure(hr, "Failed to initialize per-machine related bundles.");

    hr = RelatedBundlesInitializeForScope(FALSE, pRegistration, &pRegistration->relatedBundles);
    ExitOnFailure(hr, "Failed to initialize per-user related bundles.");

    RelatedBundlesSortDetect(&pRegistration->relatedBundles);

LExit:
    return hr;
}

extern "C" HRESULT RegistrationPlanInitialize(
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;

    if (pRegistration->relatedBundles.cRelatedBundles && !pRegistration->relatedBundles.rgpPlanSortedRelatedBundles)
    {
        hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pRegistration->relatedBundles.rgpPlanSortedRelatedBundles), pRegistration->relatedBundles.cRelatedBundles, sizeof(BURN_RELATED_BUNDLE*), 5);
        ExitOnFailure(hr, "Failed to initialize plan related bundles array.");

        for (DWORD i = 0; i < pRegistration->relatedBundles.cRelatedBundles; ++i)
        {
            pRegistration->relatedBundles.rgpPlanSortedRelatedBundles[i] = pRegistration->relatedBundles.rgRelatedBundles + i;
        }
    }

LExit:
    return hr;
}

/*******************************************************************
 RegistrationSessionBegin - Registers a run session on the system.

*******************************************************************/
extern "C" HRESULT RegistrationSessionBegin(
    __in_z LPCWSTR wzEngineWorkingPath,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in DWORD dwRegistrationOptions,
    __in DWORD64 qwEstimatedSize,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType
    )
{
    HRESULT hr = S_OK;
    HKEY hkRegistration = NULL;
    BOOL fCreated = FALSE;
    LPWSTR sczPublisher = NULL;
    SYSTEMTIME systime = { };
    DWORD er = ERROR_SUCCESS;

    AssertSz(BOOTSTRAPPER_REGISTRATION_TYPE_NONE != registrationType, "Registration type can't be NONE");

    LogId(REPORT_VERBOSE, MSG_SESSION_BEGIN, pRegistration->sczRegistrationKey, dwRegistrationOptions, LoggingBoolToString(pRegistration->fDisableResume));

    // Cache bundle executable.
    if (dwRegistrationOptions & BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE)
    {
        hr = CacheCompleteBundle(pCache, pRegistration->fPerMachine, pRegistration->sczExecutableName, pRegistration->sczId, wzEngineWorkingPath
#ifdef DEBUG
                        , pRegistration->sczCacheExecutablePath
#endif
                        );
        ExitOnFailure(hr, "Failed to cache bundle from path: %ls", wzEngineWorkingPath);
    }

    // create registration key
    hr = RegCreateEx(pRegistration->hkRoot, pRegistration->sczRegistrationKey, KEY_WRITE, REG_KEY_DEFAULT, FALSE, NULL, &hkRegistration, &fCreated);
    ExitOnFailure(hr, "Failed to create registration key.");

    // Write any ARP values and software tags.
    hr = RegWriteString(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH, pRegistration->sczCacheExecutablePath);
    ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH);

    hr = RegWriteStringArray(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, pRegistration->rgsczUpgradeCodes, pRegistration->cUpgradeCodes);
    ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE);

    hr = RegWriteStringArray(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE, pRegistration->rgsczAddonCodes, pRegistration->cAddonCodes);
    ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_ADDON_CODE);

    hr = RegWriteStringArray(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE, pRegistration->rgsczDetectCodes, pRegistration->cDetectCodes);
    ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_DETECT_CODE);

    hr = RegWriteStringArray(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE, pRegistration->rgsczPatchCodes, pRegistration->cPatchCodes);
    ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_PATCH_CODE);

    hr = RegWriteString(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION, pRegistration->pVersion->sczVersion);
    ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION);

    hr = RegWriteNumber(hkRegistration, REGISTRY_BUNDLE_VERSION_MAJOR, pRegistration->pVersion->dwMajor);
    ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_VERSION_MAJOR);

    hr = RegWriteNumber(hkRegistration, REGISTRY_BUNDLE_VERSION_MINOR, pRegistration->pVersion->dwMinor);
    ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_VERSION_MINOR);

    if (pRegistration->sczProviderKey)
    {
        hr = RegWriteString(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY, pRegistration->sczProviderKey);
        ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_PROVIDER_KEY);
    }

    if (pRegistration->sczTag)
    {
        hr = RegWriteString(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_TAG, pRegistration->sczTag);
        ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_TAG);
    }

    hr = RegWriteStringFormatted(hkRegistration, BURN_REGISTRATION_REGISTRY_ENGINE_VERSION, L"%hs", szVerMajorMinorBuild);
    ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_ENGINE_VERSION);

    hr = RegWriteNumber(hkRegistration, BURN_REGISTRATION_REGISTRY_ENGINE_PROTOCOL_VERSION, BURN_PROTOCOL_VERSION);
    ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_ENGINE_PROTOCOL_VERSION);

    // DisplayIcon: [path to exe] and ",0" to refer to the first icon in the executable.
    hr = RegWriteStringFormatted(hkRegistration, REGISTRY_BUNDLE_DISPLAY_ICON, L"%s,0", pRegistration->sczCacheExecutablePath);
    ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_DISPLAY_ICON);

    // update display name
    hr = UpdateBundleNameRegistration(pRegistration, pVariables, hkRegistration, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS == registrationType);
    ExitOnFailure(hr, "Failed to update name and publisher.");

    // DisplayVersion: provided by UI
    if (pRegistration->sczDisplayVersion)
    {
        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_DISPLAY_VERSION, pRegistration->sczDisplayVersion);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_DISPLAY_VERSION);
    }

    // Publisher: provided by UI
    hr = GetBundleManufacturer(pRegistration, pVariables, &sczPublisher);
    hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_PUBLISHER, SUCCEEDED(hr) ? sczPublisher : pRegistration->sczPublisher);
    ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_PUBLISHER);

    // HelpLink: provided by UI
    if (pRegistration->sczHelpLink)
    {
        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_HELP_LINK, pRegistration->sczHelpLink);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_HELP_LINK);
    }

    // HelpTelephone: provided by UI
    if (pRegistration->sczHelpTelephone)
    {
        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_HELP_TELEPHONE, pRegistration->sczHelpTelephone);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_HELP_TELEPHONE);
    }

    // URLInfoAbout, provided by UI
    if (pRegistration->sczAboutUrl)
    {
        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_URL_INFO_ABOUT, pRegistration->sczAboutUrl);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_URL_INFO_ABOUT);
    }

    // URLUpdateInfo, provided by UI
    if (pRegistration->sczUpdateUrl)
    {
        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_URL_UPDATE_INFO, pRegistration->sczUpdateUrl);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_URL_UPDATE_INFO);
    }

    // ParentDisplayName
    if (pRegistration->sczParentDisplayName)
    {
        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_PARENT_DISPLAY_NAME, pRegistration->sczParentDisplayName);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_PARENT_DISPLAY_NAME);

        // Need to write the ParentKeyName but can be set to anything.
        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_PARENT_KEY_NAME, pRegistration->sczParentDisplayName);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_PARENT_KEY_NAME);
    }

    // Comments, provided by UI
    if (pRegistration->sczComments)
    {
        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_COMMENTS, pRegistration->sczComments);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_COMMENTS);
    }

    // Contact, provided by UI
    if (pRegistration->sczContact)
    {
        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_CONTACT, pRegistration->sczContact);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_CONTACT);
    }

    // InstallLocation: provided by UI
    // TODO: need to figure out what "InstallLocation" means in a chainer. <smile/>

    // NoModify
    if (BURN_REGISTRATION_MODIFY_DISABLE == pRegistration->modify)
    {
        hr = RegWriteNumber(hkRegistration, REGISTRY_BUNDLE_NO_MODIFY, 1);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_NO_MODIFY);
    }
    else if (BURN_REGISTRATION_MODIFY_DISABLE_BUTTON != pRegistration->modify) // if support modify (aka: did not disable anything)
    {
        // ModifyPath: [path to exe] /modify
        hr = RegWriteStringFormatted(hkRegistration, REGISTRY_BUNDLE_MODIFY_PATH, L"\"%ls\" /modify", pRegistration->sczCacheExecutablePath);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_MODIFY_PATH);

        // NoElevateOnModify: 1
        hr = RegWriteNumber(hkRegistration, REGISTRY_BUNDLE_NO_ELEVATE_ON_MODIFY, 1);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_NO_ELEVATE_ON_MODIFY);
    }

    // NoRemove: should this be allowed?
    if (pRegistration->fNoRemove)
    {
        hr = RegWriteNumber(hkRegistration, REGISTRY_BUNDLE_NO_REMOVE, 1);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_NO_REMOVE);
    }

    // Conditionally hide the ARP entry.
    if (pRegistration->fForceSystemComponent || (dwRegistrationOptions & BURN_REGISTRATION_ACTION_OPERATIONS_ARP_SYSTEM_COMPONENT))
    {
        hr = RegWriteNumber(hkRegistration, REGISTRY_BUNDLE_SYSTEM_COMPONENT, 1);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_SYSTEM_COMPONENT);
    }
    else
    {
        er = ::RegDeleteValueW(hkRegistration, REGISTRY_BUNDLE_SYSTEM_COMPONENT);
        if (ERROR_FILE_NOT_FOUND == er || ERROR_PATH_NOT_FOUND == er)
        {
            er = ERROR_SUCCESS;
        }
        ExitOnWin32Error(er, hr, "Failed to delete %ls value.", REGISTRY_BUNDLE_SYSTEM_COMPONENT);
    }

    // QuietUninstallString: [path to exe] /uninstall /quiet
    hr = RegWriteStringFormatted(hkRegistration, REGISTRY_BUNDLE_QUIET_UNINSTALL_STRING, L"\"%ls\" /uninstall /quiet", pRegistration->sczCacheExecutablePath);
    ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_QUIET_UNINSTALL_STRING);

    // UninstallString, [path to exe]
    // If the modify button is to be disabled, we'll add "/modify" to the uninstall string because the button is "Uninstall/Change". Otherwise,
    // it's just the "Uninstall" button so we add "/uninstall" to make the program just go away.
    LPCWSTR wzUninstallParameters = (BURN_REGISTRATION_MODIFY_DISABLE_BUTTON == pRegistration->modify) ? L"/modify" : L" /uninstall";
    hr = RegWriteStringFormatted(hkRegistration, REGISTRY_BUNDLE_UNINSTALL_STRING, L"\"%ls\" %ls", pRegistration->sczCacheExecutablePath, wzUninstallParameters);
    ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_UNINSTALL_STRING);

    if (pRegistration->softwareTags.cSoftwareTags)
    {
        hr = WriteSoftwareTags(pVariables, &pRegistration->softwareTags);
        ExitOnFailure(hr, "Failed to write software tags.");
    }

    // Update registration.
    if (pRegistration->update.fRegisterUpdate)
    {
        hr = WriteUpdateRegistration(pRegistration, pVariables);
        ExitOnFailure(hr, "Failed to write update registration.");
    }

    // Only set install date and initial estimated size here for the first time.
    // Estimated size will always get updated at the end of the session.
    if (fCreated)
    {
        // Write the install date.
        ::GetLocalTime(&systime);

        hr = RegWriteStringFormatted(hkRegistration, REGISTRY_BUNDLE_INSTALL_DATE, L"%04u%02u%02u", systime.wYear, systime.wMonth, systime.wDay);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_INSTALL_DATE);

        // Write the initial estimated size.
        hr = UpdateEstimatedSize(hkRegistration, qwEstimatedSize);
        ExitOnFailure(hr, "Failed to update estimated size.");
    }

    // Register the bundle dependency key.
    if (dwRegistrationOptions & BURN_REGISTRATION_ACTION_OPERATIONS_WRITE_PROVIDER_KEY)
    {
        hr = DependencyRegisterBundle(pRegistration);
        ExitOnFailure(hr, "Failed to register the bundle dependency key.");
    }

    // update resume mode
    hr = UpdateResumeMode(pRegistration, hkRegistration, BURN_RESUME_MODE_ACTIVE, registrationType, FALSE);
    ExitOnFailure(hr, "Failed to update resume mode.");

LExit:
    ReleaseStr(sczPublisher);
    ReleaseRegKey(hkRegistration);

    return hr;
}


/*******************************************************************
 RegistrationSessionEnd - Unregisters a run session from the system.

 *******************************************************************/
extern "C" HRESULT RegistrationSessionEnd(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in BURN_PACKAGES* pPackages,
    __in BURN_RESUME_MODE resumeMode,
    __in BOOTSTRAPPER_APPLY_RESTART restart,
    __in DWORD64 qwEstimatedSize,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType
    )
{
    HRESULT hr = S_OK;
    HKEY hkRegistration = NULL;
    BOOL fDeleted = FALSE;

    // If no resume mode, then remove the bundle registration.
    if (BURN_RESUME_MODE_NONE == resumeMode)
    {
        AssertSz(BOOTSTRAPPER_REGISTRATION_TYPE_NONE == registrationType, "Registration type must be NONE if resume mode is NONE");

        // Remove the bundle dependencies.
        DependencyUnregisterBundle(pRegistration, pPackages);

        // Delete update registration key.
        if (pRegistration->update.fRegisterUpdate)
        {
            RemoveUpdateRegistration(pRegistration);
        }

        RemoveSoftwareTags(pVariables, &pRegistration->softwareTags);

        // Delete registration key.
        hr = RegDelete(pRegistration->hkRoot, pRegistration->sczRegistrationKey, REG_KEY_DEFAULT, TRUE);
        ExitOnPathFailure(hr, fDeleted, "Failed to delete registration key: %ls", pRegistration->sczRegistrationKey);

        CacheRemoveBundle(pCache, pRegistration->fPerMachine, pRegistration->sczId);
    }
    else // the mode needs to be updated so open the registration key.
    {
        AssertSz(BOOTSTRAPPER_REGISTRATION_TYPE_NONE != registrationType, "Registration type must not be NONE if resume mode is not NONE");

        // Open registration key.
        hr = RegOpen(pRegistration->hkRoot, pRegistration->sczRegistrationKey, KEY_WRITE, &hkRegistration);
        ExitOnFailure(hr, "Failed to open registration key.");

        // update display name
        hr = UpdateBundleNameRegistration(pRegistration, pVariables, hkRegistration, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS == registrationType);
        ExitOnFailure(hr, "Failed to update name and publisher.");

        hr = UpdateEstimatedSize(hkRegistration, qwEstimatedSize);
        ExitOnFailure(hr, "Failed to update estimated size.");
    }

    // Update resume mode.
    hr = UpdateResumeMode(pRegistration, hkRegistration, resumeMode, registrationType, BOOTSTRAPPER_APPLY_RESTART_INITIATED == restart);
    ExitOnFailure(hr, "Failed to update resume mode.");

LExit:
    ReleaseRegKey(hkRegistration);

    return hr;
}

/*******************************************************************
 RegistrationSaveState - Saves an engine state BLOB for retreval after a resume.

*******************************************************************/
extern "C" HRESULT RegistrationSaveState(
    __in BURN_REGISTRATION* pRegistration,
    __in_bcount(cbBuffer) BYTE* pbBuffer,
    __in SIZE_T cbBuffer
    )
{
    HRESULT hr = S_OK;
    BURN_VARIABLES variables = { };
    SIZE_T iBuffer_Unused = 0;
    HKEY hkRegistration = NULL;
    LPWSTR sczVariableKey = NULL;
    LPWSTR sczVariableValue = NULL;
    LPWSTR sczValueName = NULL;
    DWORD dwType = 0;
    DWORD dwNumberOfExistingValues = 0;
    DWORD er = ERROR_SUCCESS;


    // write data to file
    hr = FileWrite(pRegistration->sczStateFile, FILE_ATTRIBUTE_NORMAL, pbBuffer, cbBuffer, NULL);
    if (E_PATHNOTFOUND == hr)
    {
        // TODO: should we log that the bundle's cache folder was not present so the state file wasn't created either?
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failed to write state to file: %ls", pRegistration->sczStateFile);

    ::InitializeCriticalSection(&variables.csAccess);

    hr = VariableDeserialize(&variables, TRUE, pbBuffer, cbBuffer, &iBuffer_Unused);
    ExitOnFailure(hr, "Failed to read variables.");

    // build variable registry key path
    hr = StrAllocFormatted(&sczVariableKey, L"%s\\%s", pRegistration->sczRegistrationKey, REGISTRY_BUNDLE_VARIABLE_KEY);
    ExitOnFailure(hr, "Failed to build variable registry key path.");

    // open registration variable key
    hr = RegCreate(pRegistration->hkRoot, sczVariableKey, KEY_WRITE | KEY_QUERY_VALUE, &hkRegistration);
    ExitOnFailure(hr, "Failed to create registration variable key.");

    hr = RegQueryInfoKey(hkRegistration, 0, 0, 0, 0, 0, 0, &dwNumberOfExistingValues, 0, 0, 0, 0);
    ExitOnFailure(hr, "Failed to query registration variable count.");

    for (DWORD i = dwNumberOfExistingValues; i >= 0; --i)
    {
        hr = RegValueEnum(hkRegistration, i, &sczValueName, &dwType);

        if (E_NOMOREITEMS == hr)
        {
            hr = S_OK;
            break;
        }

        ExitOnFailure(hr, "Failed to enumerate value %u", i);

        er = ::RegDeleteValueW(hkRegistration, sczValueName);
        if (ERROR_FILE_NOT_FOUND != er)
        {
            ExitOnWin32Error(er, hr, "Failed to delete registration variable value.");
        }
    }

    // Write variables.
    for (DWORD i = 0; i < variables.cVariables; ++i)
    {
        BURN_VARIABLE* pVariable = &variables.rgVariables[i];

        // Write variable value.
        switch (pVariable->Value.Type)
        {
        case BURN_VARIANT_TYPE_NONE:
            hr = RegWriteNone(hkRegistration, pVariable->sczName);
            ExitOnFailure(hr, "Failed to set variable value.");
            break;
        case BURN_VARIANT_TYPE_NUMERIC: __fallthrough;
        case BURN_VARIANT_TYPE_VERSION: __fallthrough;
        case BURN_VARIANT_TYPE_FORMATTED: __fallthrough;
        case BURN_VARIANT_TYPE_STRING:
            hr = BVariantGetString(&pVariable->Value, &sczVariableValue);
            ExitOnFailure(hr, "Failed to get variable value.");

            hr = RegWriteString(hkRegistration, pVariable->sczName, sczVariableValue);
            ExitOnFailure(hr, "Failed to set variable value.");

            ReleaseNullStrSecure(sczVariableValue);

            break;
        default:
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Unsupported variable type.");
        }

    }
LExit:
    VariablesUninitialize(&variables);
    ReleaseStr(sczValueName);
    ReleaseStr(sczVariableValue);
    ReleaseStr(sczVariableKey);
    ReleaseRegKey(hkRegistration);

    return hr;
}

/*******************************************************************
 RegistrationLoadState - Loads a previously stored engine state BLOB.

*******************************************************************/
extern "C" HRESULT RegistrationLoadState(
    __in BURN_REGISTRATION* pRegistration,
    __out_bcount(*pcbBuffer) BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    )
{
    // read data from file
    HRESULT hr = FileRead(ppbBuffer, pcbBuffer, pRegistration->sczStateFile);
    return hr;
}

/*******************************************************************
RegistrationGetResumeCommandLine - Gets the resume command line from the registry

*******************************************************************/
extern "C" HRESULT RegistrationGetResumeCommandLine(
    __in const BURN_REGISTRATION* pRegistration,
    __deref_out_z LPWSTR* psczResumeCommandLine
    )
{
    HRESULT hr = S_OK;
    HKEY hkRegistration = NULL;

    // open registration key
    hr = RegOpen(pRegistration->hkRoot, pRegistration->sczRegistrationKey, KEY_QUERY_VALUE, &hkRegistration);
    if (SUCCEEDED(hr))
    {
        hr = RegReadString(hkRegistration, REGISTRY_BUNDLE_RESUME_COMMAND_LINE, psczResumeCommandLine);
    }

    // Not finding the key or value is okay.
    if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
    {
        hr = S_OK;
    }

    ReleaseRegKey(hkRegistration);

    return hr;
}


// internal helper functions

static HRESULT ParseSoftwareTagsFromXml(
    __in IXMLDOMNode* pixnRegistrationNode,
    __out BURN_SOFTWARE_TAG** prgSoftwareTags,
    __out DWORD* pcSoftwareTags
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;

    BURN_SOFTWARE_TAG* pSoftwareTags = NULL;
    BSTR bstrTagXml = NULL;

    // select tag nodes
    hr = XmlSelectNodes(pixnRegistrationNode, L"SoftwareTag", &pixnNodes);
    ExitOnFailure(hr, "Failed to select software tag nodes.");

    // get tag node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get software tag count.");

    if (cNodes)
    {
        pSoftwareTags = (BURN_SOFTWARE_TAG*)MemAlloc(sizeof(BURN_SOFTWARE_TAG) * cNodes, TRUE);
        ExitOnNull(pSoftwareTags, hr, E_OUTOFMEMORY, "Failed to allocate memory for software tag structs.");

        for (DWORD i = 0; i < cNodes; ++i)
        {
            BURN_SOFTWARE_TAG* pSoftwareTag = &pSoftwareTags[i];

            hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get next node.");

            hr = XmlGetAttributeEx(pixnNode, L"Filename", &pSoftwareTag->sczFilename);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Filename.");

            hr = XmlGetAttributeEx(pixnNode, L"Regid", &pSoftwareTag->sczRegid);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Regid.");

            hr = XmlGetAttributeEx(pixnNode, L"Path", &pSoftwareTag->sczPath);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Path.");

            hr = XmlGetText(pixnNode, &bstrTagXml);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get SoftwareTag text.");

            hr = StrAnsiAllocString(&pSoftwareTag->sczTag, bstrTagXml, 0, CP_UTF8);
            ExitOnFailure(hr, "Failed to convert SoftwareTag text to UTF-8");

            // prepare next iteration
            ReleaseNullBSTR(bstrTagXml);
            ReleaseNullObject(pixnNode);
        }
    }

    *pcSoftwareTags = cNodes;
    *prgSoftwareTags = pSoftwareTags;
    pSoftwareTags = NULL;

    hr = S_OK;

LExit:
    ReleaseBSTR(bstrTagXml);
    ReleaseObject(pixnNode);
    ReleaseObject(pixnNodes);
    ReleaseMem(pSoftwareTags);

    return hr;
}

static HRESULT SetPaths(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_CACHE* pCache
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCacheDirectory = NULL;

    // save registration key root
    pRegistration->hkRoot = pRegistration->fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;

    // build uninstall registry key path
    hr = StrAllocFormatted(&pRegistration->sczRegistrationKey, L"%ls\\%ls", BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY, pRegistration->sczId);
    ExitOnFailure(hr, "Failed to build uninstall registry key path.");

    // build cache directory
    hr = CacheGetCompletedPath(pCache, pRegistration->fPerMachine, pRegistration->sczId, &sczCacheDirectory);
    ExitOnFailure(hr, "Failed to build cache directory.");

    // build cached executable path
    hr = PathConcatRelativeToFullyQualifiedBase(sczCacheDirectory, pRegistration->sczExecutableName, &pRegistration->sczCacheExecutablePath);
    ExitOnFailure(hr, "Failed to build cached executable path.");

    // build state file path
    hr = StrAllocFormatted(&pRegistration->sczStateFile, L"%ls\\state.rsm", sczCacheDirectory);
    ExitOnFailure(hr, "Failed to build state file path.");

LExit:
    ReleaseStr(sczCacheDirectory);
    return hr;
}

static HRESULT GetBundleManufacturer(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczBundleManufacturer
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzPublisher = pRegistration->sczPublisher ? pRegistration->sczPublisher : L"";

    hr = EnsureRegistrationVariable(pVariables, BURN_BUNDLE_MANUFACTURER, wzPublisher, psczBundleManufacturer);
    ExitOnFailure(hr, "Failed to get bundle manufacturer.");

LExit:
    return hr;
}

static HRESULT GetBundleInProgressName(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczInProgressBundleName
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzInProgressDisplayName = pRegistration->sczInProgressDisplayName ? pRegistration->sczInProgressDisplayName : L"";

    hr = EnsureRegistrationVariable(pVariables, BURN_BUNDLE_INPROGRESS_NAME, wzInProgressDisplayName, psczInProgressBundleName);
    ExitOnFailure(hr, "Failed to ensure in-progress bundle name.");

LExit:
    return hr;
}

static HRESULT GetBundleName(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczBundleName
    )
{
    HRESULT hr = S_OK;
    LPCWSTR wzDisplayName = pRegistration->sczDisplayName ? pRegistration->sczDisplayName : L"";

    hr = EnsureRegistrationVariable(pVariables, BURN_BUNDLE_NAME, wzDisplayName, psczBundleName);
    ExitOnFailure(hr, "Failed to ensure bundle name.");

LExit:
    return hr;
}

static HRESULT EnsureRegistrationVariable(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzVariable,
    __in_z LPCWSTR wzDefaultValue,
    __out_z LPWSTR* psczValue
    )
{
    HRESULT hr = S_OK;

    hr = VariableGetString(pVariables, wzVariable, psczValue);
    if (E_NOTFOUND == hr)
    {
        hr = VariableSetString(pVariables, wzVariable, wzDefaultValue, FALSE, FALSE);
        ExitOnFailure(hr, "Failed to set registration variable.");

        hr = StrAllocString(psczValue, wzDefaultValue, 0);
    }
    ExitOnFailure(hr, "Failed to get registration variable.");

LExit:
    return hr;
}

static HRESULT UpdateResumeMode(
    __in BURN_REGISTRATION* pRegistration,
    __in HKEY hkRegistration,
    __in BURN_RESUME_MODE resumeMode,
    __in BOOTSTRAPPER_REGISTRATION_TYPE registrationType,
    __in BOOL fRestartInitiated
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    HKEY hkRun = NULL;
    LPWSTR sczRunOnceCommandLine = NULL;
    LPCWSTR sczResumeKey = REGISTRY_RUN_ONCE_KEY;

    LogId(REPORT_STANDARD, MSG_SESSION_UPDATE, pRegistration->sczRegistrationKey, LoggingResumeModeToString(resumeMode), LoggingBoolToString(fRestartInitiated), LoggingBoolToString(pRegistration->fDisableResume));

    // write resume information
    if (hkRegistration)
    {
        // write Resume value
        hr = RegWriteNumber(hkRegistration, L"Resume", (DWORD)resumeMode);
        ExitOnFailure(hr, "Failed to write Resume value.");

        // Write Installed value.
        hr = RegWriteNumber(hkRegistration, REGISTRY_BUNDLE_INSTALLED, BOOTSTRAPPER_REGISTRATION_TYPE_FULL == registrationType ? 1 : 0);
        ExitOnFailure(hr, "Failed to write Installed value.");
    }

    // If the engine is active write the run key so we resume if there is an unexpected
    // power loss. Also, if a restart was initiated in the middle of the chain then
    // ensure the run key exists (it should since going active would have written it).
    // Do not write the run key when embedded since the containing bundle
    // is expected to detect for and restart the embedded bundle.
    if ((BURN_RESUME_MODE_ACTIVE == resumeMode || fRestartInitiated) && !pRegistration->fDisableResume)
    {
        // append RunOnce switch
        hr = StrAllocFormatted(&sczRunOnceCommandLine, L"\"%ls\" /%ls", pRegistration->sczCacheExecutablePath, BURN_COMMANDLINE_SWITCH_RUNONCE);
        ExitOnFailure(hr, "Failed to format resume command line for RunOnce.");

        // write run key
        hr = RegCreate(pRegistration->hkRoot, sczResumeKey, KEY_WRITE, &hkRun);
        ExitOnFailure(hr, "Failed to create run key.");

        hr = RegWriteString(hkRun, pRegistration->sczId, sczRunOnceCommandLine);
        ExitOnFailure(hr, "Failed to write run key value.");

        hr = RegWriteString(hkRegistration, REGISTRY_BUNDLE_RESUME_COMMAND_LINE, pRegistration->sczResumeCommandLine);
        ExitOnFailure(hr, "Failed to write resume command line value.");
    }
    else // delete run key value
    {
        hr = RegOpen(pRegistration->hkRoot, sczResumeKey, KEY_WRITE, &hkRun);
        if (E_FILENOTFOUND == hr || E_PATHNOTFOUND == hr)
        {
            hr = S_OK;
        }
        else
        {
            ExitOnFailure(hr, "Failed to open run key.");

            er = ::RegDeleteValueW(hkRun, pRegistration->sczId);
            if (ERROR_FILE_NOT_FOUND == er)
            {
                er = ERROR_SUCCESS;
            }
            ExitOnWin32Error(er, hr, "Failed to delete run key value.");
        }

        if (hkRegistration)
        {
            er = ::RegDeleteValueW(hkRegistration, REGISTRY_BUNDLE_RESUME_COMMAND_LINE);
            if (ERROR_FILE_NOT_FOUND == er)
            {
                er = ERROR_SUCCESS;
            }
            ExitOnWin32Error(er, hr, "Failed to delete resume command line value.");
        }
    }

LExit:
    ReleaseStr(sczRunOnceCommandLine);
    ReleaseRegKey(hkRun);

    return hr;
}

static HRESULT FormatUpdateRegistrationKey(
    __in BURN_REGISTRATION* pRegistration,
    __out_z LPWSTR* psczKey
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;

    hr = StrAllocFormatted(&sczKey, L"SOFTWARE\\%ls\\Updates\\", pRegistration->update.sczManufacturer);
    ExitOnFailure(hr, "Failed to format the key path for update registration.");

    if (pRegistration->update.sczProductFamily)
    {
        hr = StrAllocFormatted(&sczKey, L"%ls%ls\\", sczKey, pRegistration->update.sczProductFamily);
        ExitOnFailure(hr, "Failed to format the key path for update registration.");
    }

    hr = StrAllocConcat(&sczKey, pRegistration->update.sczName, 0);
    ExitOnFailure(hr, "Failed to format the key path for update registration.");

    *psczKey = sczKey;
    sczKey = NULL;

LExit:
    ReleaseStr(sczKey);

    return hr;
}

static HRESULT WriteSoftwareTags(
    __in BURN_VARIABLES* pVariables,
    __in BURN_SOFTWARE_TAGS* pSoftwareTags
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczRootFolder = NULL;
    LPWSTR sczTagFolder = NULL;
    LPWSTR sczPath = NULL;

    for (DWORD iTag = 0; iTag < pSoftwareTags->cSoftwareTags; ++iTag)
    {
        BURN_SOFTWARE_TAG* pSoftwareTag = pSoftwareTags->rgSoftwareTags + iTag;

        hr = VariableFormatString(pVariables, pSoftwareTag->sczPath, &sczRootFolder, NULL);
        ExitOnFailure(hr, "Failed to format tag folder path.");

        hr = PathConcat(sczRootFolder, SWIDTAG_FOLDER, &sczTagFolder);
        ExitOnFailure(hr, "Failed to allocate regid folder path.");

        hr = PathConcatRelativeToFullyQualifiedBase(sczTagFolder, pSoftwareTag->sczFilename, &sczPath);
        ExitOnFailure(hr, "Failed to allocate regid file path.");

        hr = DirEnsureExists(sczTagFolder, NULL);
        ExitOnFailure(hr, "Failed to create regid folder: %ls", sczTagFolder);

        hr = FileWrite(sczPath, FILE_ATTRIBUTE_NORMAL, reinterpret_cast<LPBYTE>(pSoftwareTag->sczTag), lstrlenA(pSoftwareTag->sczTag), NULL);
        ExitOnFailure(hr, "Failed to write tag xml to file: %ls", sczPath);
    }

LExit:
    ReleaseStr(sczPath);
    ReleaseStr(sczTagFolder);
    ReleaseStr(sczRootFolder);

    return hr;
}

static HRESULT RemoveSoftwareTags(
    __in BURN_VARIABLES* pVariables,
    __in BURN_SOFTWARE_TAGS* pSoftwareTags
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczRootFolder = NULL;
    LPWSTR sczTagFolder = NULL;
    LPWSTR sczPath = NULL;

    for (DWORD iTag = 0; iTag < pSoftwareTags->cSoftwareTags; ++iTag)
    {
        BURN_SOFTWARE_TAG* pSoftwareTag = pSoftwareTags->rgSoftwareTags + iTag;

        hr = VariableFormatString(pVariables, pSoftwareTag->sczPath, &sczRootFolder, NULL);
        ExitOnFailure(hr, "Failed to format tag folder path.");

        hr = PathConcat(sczRootFolder, SWIDTAG_FOLDER, &sczTagFolder);
        ExitOnFailure(hr, "Failed to allocate regid folder path.");

        hr = PathConcatRelativeToFullyQualifiedBase(sczTagFolder, pSoftwareTag->sczFilename, &sczPath);
        ExitOnFailure(hr, "Failed to allocate regid file path.");

        // Best effort to delete the software tag file and the regid folder.
        FileEnsureDelete(sczPath);

        DirDeleteEmptyDirectoriesToRoot(sczTagFolder, 0);
    }

LExit:
    ReleaseStr(sczPath);
    ReleaseStr(sczTagFolder);
    ReleaseStr(sczRootFolder);

    return hr;
}

static HRESULT WriteUpdateRegistration(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;
    HKEY hkKey = NULL;

    hr = FormatUpdateRegistrationKey(pRegistration, &sczKey);
    ExitOnFailure(hr, "Failed to get the formatted key path for update registration.");

    hr = RegCreate(pRegistration->hkRoot, sczKey, KEY_WRITE, &hkKey);
    ExitOnFailure(hr, "Failed to create the key for update registration.");

    hr = RegWriteString(hkKey, L"ThisVersionInstalled", L"Y");
    ExitOnFailure(hr, "Failed to write %ls value.", L"ThisVersionInstalled");

    hr = RegWriteString(hkKey, L"PackageName", pRegistration->sczDisplayName);
    ExitOnFailure(hr, "Failed to write %ls value.", L"PackageName");

    hr = RegWriteString(hkKey, L"PackageVersion", pRegistration->sczDisplayVersion);
    ExitOnFailure(hr, "Failed to write %ls value.", L"PackageVersion");

    hr = RegWriteString(hkKey, L"Publisher", pRegistration->sczPublisher);
    ExitOnFailure(hr, "Failed to write %ls value.", L"Publisher");

    if (pRegistration->update.sczDepartment)
    {
        hr = RegWriteString(hkKey, L"PublishingGroup", pRegistration->update.sczDepartment);
        ExitOnFailure(hr, "Failed to write %ls value.", L"PublishingGroup");
    }

    hr = RegWriteString(hkKey, L"ReleaseType", pRegistration->update.sczClassification);
    ExitOnFailure(hr, "Failed to write %ls value.", L"ReleaseType");

    hr = RegWriteStringVariable(hkKey, pVariables, VARIABLE_LOGONUSER, L"InstalledBy");
    ExitOnFailure(hr, "Failed to write %ls value.", L"InstalledBy");

    hr = RegWriteStringVariable(hkKey, pVariables, VARIABLE_DATE, L"InstalledDate");
    ExitOnFailure(hr, "Failed to write %ls value.", L"InstalledDate");

    hr = RegWriteStringVariable(hkKey, pVariables, VARIABLE_INSTALLERNAME, L"InstallerName");
    ExitOnFailure(hr, "Failed to write %ls value.", L"InstallerName");

    hr = RegWriteStringVariable(hkKey, pVariables, VARIABLE_INSTALLERVERSION, L"InstallerVersion");
    ExitOnFailure(hr, "Failed to write %ls value.", L"InstallerVersion");

LExit:
    ReleaseRegKey(hkKey);
    ReleaseStr(sczKey);

    return hr;
}

static HRESULT RemoveUpdateRegistration(
    __in BURN_REGISTRATION* pRegistration
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczKey = NULL;
    LPWSTR sczPackageVersion = NULL;
    HKEY hkKey = NULL;
    BOOL fDeleteRegKey = TRUE;
    BOOL fDeleted = FALSE;

    hr = FormatUpdateRegistrationKey(pRegistration, &sczKey);
    ExitOnFailure(hr, "Failed to format key for update registration.");

    // Only delete if the uninstalling bundle's PackageVersion is the same as the
    // PackageVersion in the update registration key.
    // This is to support build to build upgrades
    hr = RegOpen(pRegistration->hkRoot, sczKey, KEY_QUERY_VALUE, &hkKey);
    if (SUCCEEDED(hr))
    {
        hr = RegReadString(hkKey, L"PackageVersion", &sczPackageVersion);
        if (SUCCEEDED(hr))
        {
            if (CSTR_EQUAL != ::CompareStringW(LOCALE_INVARIANT, 0, sczPackageVersion, -1, pRegistration->sczDisplayVersion, -1))
            {
                fDeleteRegKey = FALSE;
            }
        }
        ReleaseRegKey(hkKey);
    }

    // Unable to open the key or read the value is okay.
    hr = S_OK;

    if (fDeleteRegKey)
    {
        hr = RegDelete(pRegistration->hkRoot, sczKey, REG_KEY_DEFAULT, FALSE);
        ExitOnPathFailure(hr, fDeleted, "Failed to remove update registration key: %ls", sczKey);
    }

LExit:
    ReleaseStr(sczPackageVersion);
    ReleaseStr(sczKey);

    return hr;
}

static HRESULT RegWriteStringVariable(
    __in HKEY hk,
    __in BURN_VARIABLES* pVariables,
    __in LPCWSTR wzVariable,
    __in LPCWSTR wzName
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczValue = NULL;

    hr = VariableGetString(pVariables, wzVariable, &sczValue);
    ExitOnFailure(hr, "Failed to get the %ls variable.", wzVariable);

    hr = RegWriteString(hk, wzName, sczValue);
    ExitOnFailure(hr, "Failed to write %ls value.", wzName);

LExit:
    StrSecureZeroFreeString(sczValue);

    return hr;
}

static HRESULT UpdateBundleNameRegistration(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in HKEY hkRegistration,
    __in BOOL fInProgressRegistration
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczDisplayName = NULL;

    if (fInProgressRegistration)
    {
        hr = GetBundleInProgressName(pRegistration, pVariables, &sczDisplayName);
        ExitOnFailure(hr, "Failed to get bundle in-progress name.");
    }

    if (!sczDisplayName || !*sczDisplayName)
    {
        hr = GetBundleName(pRegistration, pVariables, &sczDisplayName);
        ExitOnFailure(hr, "Failed to get bundle name.");
    }

    hr = RegWriteString(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_DISPLAY_NAME, sczDisplayName);
    ExitOnFailure(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_DISPLAY_NAME);

LExit:
    ReleaseStr(sczDisplayName);

    return hr;
}

static HRESULT UpdateEstimatedSize(
    __in HKEY hkRegistration,
    __in DWORD64 qwEstimatedSize
    )
{
    HRESULT hr = S_OK;
    DWORD dwSize = 0;

    qwEstimatedSize /= 1024; // Convert bytes to KB
    if (0 < qwEstimatedSize)
    {
        if (DWORD_MAX < qwEstimatedSize)
        {
            // ARP doesn't support QWORDs here
            dwSize = DWORD_MAX;
        }
        else
        {
            dwSize = static_cast<DWORD>(qwEstimatedSize);
        }

        hr = RegWriteNumber(hkRegistration, REGISTRY_BUNDLE_ESTIMATED_SIZE, dwSize);
        ExitOnFailure(hr, "Failed to write %ls value.", REGISTRY_BUNDLE_ESTIMATED_SIZE);
    }

LExit:
    return hr;
}

static BOOL IsWuRebootPending()
{
    HRESULT hr = S_OK;
    BOOL fUninitializeCom = FALSE;
    BOOL fRebootPending = FALSE;

    // Do a best effort to ask WU if a reboot is required. If anything goes
    // wrong then let's pretend a reboot is not required.
    hr = ::CoInitialize(NULL);
    fUninitializeCom = SUCCEEDED(hr);
    if (fUninitializeCom || RPC_E_CHANGED_MODE == hr)
    {
        hr = WuaRestartRequired(&fRebootPending);
        if (FAILED(hr))
        {
            fRebootPending = FALSE;
        }

        if (fUninitializeCom)
        {
            ::CoUninitialize();
        }
    }

    return fRebootPending;
}

static BOOL IsRegistryRebootPending()
{
    HRESULT hr = S_OK;
    DWORD dwValue;
    HKEY hk = NULL;
    BOOL fRebootPending = FALSE;

    hr = RegKeyReadNumber(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\ServerManager", L"CurrentRebootAttempts", REG_KEY_DEFAULT, &dwValue);
    fRebootPending = SUCCEEDED(hr) && 0 < dwValue;

    if (!fRebootPending)
    {
        hr = RegKeyReadNumber(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Updates", L"UpdateExeVolatile", REG_KEY_DEFAULT, &dwValue);
        fRebootPending = SUCCEEDED(hr) && 0 < dwValue;

        if (!fRebootPending)
        {
            fRebootPending = RegValueExists(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootPending", NULL, REG_KEY_DEFAULT);

            if (!fRebootPending)
            {
                fRebootPending = RegValueExists(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Component Based Servicing\\RebootInProgress", NULL, REG_KEY_DEFAULT);

                if (!fRebootPending)
                {
                    hr = RegKeyReadNumber(HKEY_LOCAL_MACHINE, L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\WindowsUpdate\\Auto Update", L"AUState", REG_KEY_DEFAULT, &dwValue);
                    fRebootPending = SUCCEEDED(hr) && 8 == dwValue;

                    if (!fRebootPending)
                    {
                        fRebootPending = RegValueExists(HKEY_LOCAL_MACHINE, L"SYSTEM\\CurrentControlSet\\Control\\Session Manager", L"PendingFileRenameOperations", REG_KEY_DEFAULT);

                        if (!fRebootPending)
                        {
                            fRebootPending = RegValueExists(HKEY_LOCAL_MACHINE, L"SYSTEM\\CurrentControlSet\\Control\\Session Manager", L"PendingFileRenameOperations2", REG_KEY_DEFAULT);

                            if (!fRebootPending)
                            {
                                hr = RegOpenEx(HKEY_LOCAL_MACHINE, L"SYSTEM\\CurrentControlSet\\Control\\Session Manager\\FileRenameOperations", KEY_READ, REG_KEY_DEFAULT, &hk);
                                if (SUCCEEDED(hr))
                                {
                                    DWORD cSubKeys = 0;
                                    DWORD cValues = 0;
                                    hr = RegQueryKey(hk, &cSubKeys, &cValues);
                                    fRebootPending = SUCCEEDED(hr) && (0 < cSubKeys || 0 < cValues);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    ReleaseRegKey(hk);

    return fRebootPending;
}
