// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// prototypes
static HRESULT ParsePackagesFromXml(
    __in BAL_INFO_PACKAGES* pPackages,
    __in IXMLDOMDocument* pixdManifest
    );
static HRESULT ParseBalPackageInfoFromXml(
    __in BAL_INFO_PACKAGES* pPackages,
    __in IXMLDOMDocument* pixdManifest
    );
static HRESULT ParseOverridableVariablesFromXml(
    __in BAL_INFO_OVERRIDABLE_VARIABLES* pOverridableVariables,
    __in IXMLDOMDocument* pixdManifest
    );


DAPI_(HRESULT) BalInfoParseCommandLine(
    __in BAL_INFO_COMMAND* pCommand,
    __in const BOOTSTRAPPER_COMMAND* pBootstrapperCommand
    )
{
    HRESULT hr = S_OK;
    int argc = 0;
    LPWSTR* argv = NULL;
    BOOL fUnknownArg = FALSE;

    BalInfoUninitializeCommandLine(pCommand);

    if (!pBootstrapperCommand->wzCommandLine || !*pBootstrapperCommand->wzCommandLine)
    {
        ExitFunction();
    }

    hr = AppParseCommandLine(pBootstrapperCommand->wzCommandLine, &argc, &argv);
    BalExitOnFailure(hr, "Failed to parse command line.");

    for (int i = 0; i < argc; ++i)
    {
        fUnknownArg = FALSE;

        if (argv[i][0] == L'-' || argv[i][0] == L'/')
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"norestart", -1))
            {
                if (BAL_INFO_RESTART_UNKNOWN == pCommand->restart)
                {
                    pCommand->restart = BAL_INFO_RESTART_NEVER;
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, NORM_IGNORECASE, &argv[i][1], -1, L"forcerestart", -1))
            {
                if (BAL_INFO_RESTART_UNKNOWN == pCommand->restart)
                {
                    pCommand->restart = BAL_INFO_RESTART_ALWAYS;
                }
            }
            else
            {
                fUnknownArg = TRUE;
            }
        }
        else
        {
            const wchar_t* pwc = wcschr(argv[i], L'=');
            if (!pwc)
            {
                fUnknownArg = TRUE;
            }
            else
            {
                hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(&pCommand->rgVariableNames), pCommand->cVariables, 1, sizeof(LPWSTR), 5);
                BalExitOnFailure(hr, "Failed to ensure size for variable names.");

                hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(&pCommand->rgVariableValues), pCommand->cVariables, 1, sizeof(LPWSTR), 5);
                BalExitOnFailure(hr, "Failed to ensure size for variable values.");

                LPWSTR* psczVariableName = pCommand->rgVariableNames + pCommand->cVariables;
                LPWSTR* psczVariableValue = pCommand->rgVariableValues + pCommand->cVariables;
                pCommand->cVariables += 1;

                hr = StrAllocString(psczVariableName, argv[i], pwc - argv[i]);
                BalExitOnFailure(hr, "Failed to copy variable name.");

                hr = StrAllocString(psczVariableValue, ++pwc, 0);
                BalExitOnFailure(hr, "Failed to copy variable value.");
            }
        }

        if (fUnknownArg)
        {
            hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(&pCommand->rgUnknownArgs), pCommand->cUnknownArgs, 1, sizeof(LPWSTR), 5);
            BalExitOnFailure(hr, "Failed to ensure size for unknown args.");

            LPWSTR* psczArg = pCommand->rgUnknownArgs + pCommand->cUnknownArgs;
            pCommand->cUnknownArgs += 1;

            StrAllocString(psczArg, argv[i], 0);
            BalExitOnFailure(hr, "Failed to copy unknown arg.");
        }
    }

LExit:
    if (BAL_INFO_RESTART_UNKNOWN == pCommand->restart)
    {
        pCommand->restart = BOOTSTRAPPER_DISPLAY_FULL > pBootstrapperCommand->display ? BAL_INFO_RESTART_AUTOMATIC : BAL_INFO_RESTART_PROMPT;
    }

    if (argv)
    {
        AppFreeCommandLineArgs(argv);
    }

    return hr;
}

DAPI_(HRESULT) BalInfoParseFromXml(
    __in BAL_INFO_BUNDLE* pBundle,
    __in IXMLDOMDocument* pixdManifest
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pNode = NULL;
    BOOL fXmlFound = FALSE;

    hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixBundleProperties", &pNode);
    BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to select bundle information.");

    if (fXmlFound)
    {
        hr = XmlGetYesNoAttribute(pNode, L"PerMachine", &pBundle->fPerMachine);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to read bundle information per-machine.");

        hr = XmlGetAttributeEx(pNode, L"DisplayName", &pBundle->sczName);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to read bundle information display name.");

        hr = XmlGetAttributeEx(pNode, L"LogPathVariable", &pBundle->sczLogVariable);
        BalExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to read bundle information log path variable.");
    }

    hr = ParseOverridableVariablesFromXml(&pBundle->overridableVariables, pixdManifest);
    BalExitOnFailure(hr, "Failed to parse overridable variables from bootstrapper application data.");

    hr = ParsePackagesFromXml(&pBundle->packages, pixdManifest);
    BalExitOnFailure(hr, "Failed to parse package information from bootstrapper application data.");

    hr = ParseBalPackageInfoFromXml(&pBundle->packages, pixdManifest);
    BalExitOnFailure(hr, "Failed to parse bal package information from bootstrapper application data.");

LExit:
    ReleaseObject(pNode);

    return hr;
}


DAPI_(HRESULT) BalInfoAddRelatedBundleAsPackage(
    __in BAL_INFO_PACKAGES* pPackages,
    __in_z LPCWSTR wzId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOL /*fPerMachine*/,
    __out_opt BAL_INFO_PACKAGE** ppPackage
    )
{
    HRESULT hr = S_OK;
    BAL_INFO_PACKAGE_TYPE type = BAL_INFO_PACKAGE_TYPE_UNKNOWN;
    BAL_INFO_PACKAGE* pPackage = NULL;

    // Ensure we have a supported relation type.
    switch (relationType)
    {
    case BOOTSTRAPPER_RELATION_ADDON:
        type = BAL_INFO_PACKAGE_TYPE_BUNDLE_ADDON;
        break;

    case BOOTSTRAPPER_RELATION_PATCH:
        type = BAL_INFO_PACKAGE_TYPE_BUNDLE_PATCH;
        break;

    case BOOTSTRAPPER_RELATION_UPGRADE:
        type = BAL_INFO_PACKAGE_TYPE_BUNDLE_UPGRADE;
        break;

    default:
        ExitWithRootFailure(hr, E_INVALIDARG, "Unknown related bundle type: %u", relationType);
    }

    // Check to see if the bundle is already in the list of packages.
    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzId, -1, pPackages->rgPackages[i].sczId, -1))
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS));
        }
    }

    // Add the related bundle as a package.
    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPackages->rgPackages), pPackages->cPackages + 1, sizeof(BAL_INFO_PACKAGE), 2);
    ExitOnFailure(hr, "Failed to allocate memory for related bundle package information.");

    pPackage = pPackages->rgPackages + pPackages->cPackages;
    ++pPackages->cPackages;

    hr = StrAllocString(&pPackage->sczId, wzId, 0);
    ExitOnFailure(hr, "Failed to copy related bundle package id.");

    pPackage->type = type;

    // TODO: try to look up the DisplayName and Description in Add/Remove Programs with the wzId.

    if (ppPackage)
    {
        *ppPackage = pPackage;
    }

LExit:
    return hr;
}


DAPI_(HRESULT) BalInfoAddUpdateBundleAsPackage(
    __in BAL_INFO_PACKAGES* pPackages,
    __in_z LPCWSTR wzId,
    __out_opt BAL_INFO_PACKAGE** ppPackage
    )
{
    HRESULT hr = S_OK;
    BAL_INFO_PACKAGE* pPackage = NULL;

    // Check to see if the bundle is already in the list of packages.
    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzId, -1, pPackages->rgPackages[i].sczId, -1))
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_ALREADY_EXISTS));
        }
    }

    // Add the update bundle as a package.
    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPackages->rgPackages), pPackages->cPackages + 1, sizeof(BAL_INFO_PACKAGE), 2);
    ExitOnFailure(hr, "Failed to allocate memory for update bundle package information.");

    pPackage = pPackages->rgPackages + pPackages->cPackages;
    ++pPackages->cPackages;

    hr = StrAllocString(&pPackage->sczId, wzId, 0);
    ExitOnFailure(hr, "Failed to copy update bundle package id.");

    pPackage->type = BAL_INFO_PACKAGE_TYPE_BUNDLE_UPDATE;

    if (ppPackage)
    {
        *ppPackage = pPackage;
    }

LExit:
    return hr;
}


DAPI_(HRESULT) BalInfoFindPackageById(
    __in BAL_INFO_PACKAGES* pPackages,
    __in LPCWSTR wzId,
    __out BAL_INFO_PACKAGE** ppPackage
    )
{
    *ppPackage = NULL;

    for (DWORD i = 0; i < pPackages->cPackages; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzId, -1, pPackages->rgPackages[i].sczId, -1))
        {
            *ppPackage = pPackages->rgPackages + i;
            break;
        }
    }

    return *ppPackage ? S_OK : E_NOTFOUND;
}


DAPI_(void) BalInfoUninitialize(
    __in BAL_INFO_BUNDLE* pBundle
    )
{
    for (DWORD i = 0; i < pBundle->packages.cPackages; ++i)
    {
        ReleaseStr(pBundle->packages.rgPackages[i].sczDisplayName);
        ReleaseStr(pBundle->packages.rgPackages[i].sczDescription);
        ReleaseStr(pBundle->packages.rgPackages[i].sczId);
        ReleaseStr(pBundle->packages.rgPackages[i].sczDisplayInternalUICondition);
        ReleaseStr(pBundle->packages.rgPackages[i].sczDisplayFilesInUseDialogCondition);
        ReleaseStr(pBundle->packages.rgPackages[i].sczProductCode);
        ReleaseStr(pBundle->packages.rgPackages[i].sczUpgradeCode);
        ReleaseStr(pBundle->packages.rgPackages[i].sczVersion);
        ReleaseStr(pBundle->packages.rgPackages[i].sczInstallCondition);
        ReleaseStr(pBundle->packages.rgPackages[i].sczRepairCondition);
        ReleaseStr(pBundle->packages.rgPackages[i].sczPrereqLicenseFile);
        ReleaseStr(pBundle->packages.rgPackages[i].sczPrereqLicenseUrl);
    }

    ReleaseMem(pBundle->packages.rgPackages);

    for (DWORD i = 0; i < pBundle->overridableVariables.cVariables; ++i)
    {
        ReleaseStr(pBundle->overridableVariables.rgVariables[i].sczName);
    }

    ReleaseMem(pBundle->overridableVariables.rgVariables);
    ReleaseDict(pBundle->overridableVariables.sdVariables);

    ReleaseStr(pBundle->sczName);
    ReleaseStr(pBundle->sczLogVariable);
    memset(pBundle, 0, sizeof(BAL_INFO_BUNDLE));
}


DAPI_(void) BalInfoUninitializeCommandLine(
    __in BAL_INFO_COMMAND* pCommand
    )
{
    for (DWORD i = 0; i < pCommand->cUnknownArgs; ++i)
    {
        ReleaseNullStrSecure(pCommand->rgUnknownArgs[i]);
    }

    ReleaseMem(pCommand->rgUnknownArgs);

    for (DWORD i = 0; i < pCommand->cVariables; ++i)
    {
        ReleaseNullStrSecure(pCommand->rgVariableNames[i]);
        ReleaseNullStrSecure(pCommand->rgVariableValues[i]);
    }

    ReleaseMem(pCommand->rgVariableNames);
    ReleaseMem(pCommand->rgVariableValues);

    memset(pCommand, 0, sizeof(BAL_INFO_COMMAND));
}


DAPI_(HRESULT) BalSetOverridableVariablesFromEngine(
    __in BAL_INFO_OVERRIDABLE_VARIABLES* pOverridableVariables,
    __in BAL_INFO_COMMAND* pCommand,
    __in IBootstrapperEngine* pEngine
    )
{
    HRESULT hr = S_OK;
    BAL_INFO_OVERRIDABLE_VARIABLE* pOverridableVariable = NULL;

    for (DWORD i = 0; i < pCommand->cVariables; ++i)
    {
        LPCWSTR wzVariableName = pCommand->rgVariableNames[i];
        LPCWSTR wzVariableValue = pCommand->rgVariableValues[i];

        hr = DictGetValue(pOverridableVariables->sdVariables, wzVariableName, reinterpret_cast<void**>(&pOverridableVariable));
        if (E_NOTFOUND == hr || E_INVALIDARG == hr)
        {
            BalLog(BOOTSTRAPPER_LOG_LEVEL_ERROR, "Ignoring attempt to set non-overridable variable: '%ls'.", wzVariableName);
            hr = S_OK;
            continue;
        }
        BalExitOnFailure(hr, "Failed to check the dictionary of overridable variables.");

        hr = pEngine->SetVariableString(pOverridableVariable->sczName, wzVariableValue, FALSE);
        BalExitOnFailure(hr, "Failed to set variable: '%ls'.", pOverridableVariable->sczName);
    }

LExit:
    return hr;
}


static HRESULT ParsePackagesFromXml(
    __in BAL_INFO_PACKAGES* pPackages,
    __in IXMLDOMDocument* pixdManifest
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pNodeList = NULL;
    IXMLDOMNode* pNode = NULL;
    BAL_INFO_PACKAGE* prgPackages = NULL;
    DWORD cPackages = 0;
    LPWSTR scz = NULL;
    BOOL fXmlFound = FALSE;

    hr = XmlSelectNodes(pixdManifest, L"/BootstrapperApplicationData/WixPackageProperties", &pNodeList);
    ExitOnFailure(hr, "Failed to select all packages.");

    hr = pNodeList->get_length(reinterpret_cast<long*>(&cPackages));
    ExitOnFailure(hr, "Failed to get the package count.");

    prgPackages = static_cast<BAL_INFO_PACKAGE*>(MemAlloc(sizeof(BAL_INFO_PACKAGE) * cPackages, TRUE));
    ExitOnNull(prgPackages, hr, E_OUTOFMEMORY, "Failed to allocate memory for packages.");

    DWORD iPackage = 0;
    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, NULL)))
    {
        hr = XmlGetAttributeEx(pNode, L"Package", &prgPackages[iPackage].sczId);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get package identifier for package.");

        hr = XmlGetAttributeEx(pNode, L"DisplayName", &prgPackages[iPackage].sczDisplayName);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get display name for package.");

        hr = XmlGetAttributeEx(pNode, L"Description", &prgPackages[iPackage].sczDescription);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get description for package.");

        hr = XmlGetAttributeEx(pNode, L"PackageType", &scz);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get package type for package.");

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Bundle", -1, scz, -1))
        {
            prgPackages[iPackage].type = BAL_INFO_PACKAGE_TYPE_BUNDLE_CHAIN;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Exe", -1, scz, -1))
        {
            prgPackages[iPackage].type = BAL_INFO_PACKAGE_TYPE_EXE;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Msi", -1, scz, -1))
        {
            prgPackages[iPackage].type = BAL_INFO_PACKAGE_TYPE_MSI;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Msp", -1, scz, -1))
        {
            prgPackages[iPackage].type = BAL_INFO_PACKAGE_TYPE_MSP;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, L"Msu", -1, scz, -1))
        {
            prgPackages[iPackage].type = BAL_INFO_PACKAGE_TYPE_MSU;
        }

        hr = XmlGetYesNoAttribute(pNode, L"Permanent", &prgPackages[iPackage].fPermanent);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get permanent setting for package.");

        hr = XmlGetYesNoAttribute(pNode, L"Vital", &prgPackages[iPackage].fVital);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get vital setting for package.");

        hr = XmlGetAttributeEx(pNode, L"ProductCode", &prgPackages[iPackage].sczProductCode);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get product code for package.");

        hr = XmlGetAttributeEx(pNode, L"UpgradeCode", &prgPackages[iPackage].sczUpgradeCode);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get upgrade code for package.");

        hr = XmlGetAttributeEx(pNode, L"Version", &prgPackages[iPackage].sczVersion);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get version for package.");

        hr = XmlGetAttributeEx(pNode, L"InstallCondition", &prgPackages[iPackage].sczInstallCondition);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get install condition for package.");

        hr = XmlGetAttributeEx(pNode, L"RepairCondition", &prgPackages[iPackage].sczRepairCondition);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get repair condition for package.");

        hr = XmlGetAttributeEx(pNode, L"Cache", &scz);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get cache type for package.");

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, scz, -1, L"remove", -1))
        {
            prgPackages[iPackage].cacheType = BOOTSTRAPPER_CACHE_TYPE_REMOVE;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, scz, -1, L"keep", -1))
        {
            prgPackages[iPackage].cacheType = BOOTSTRAPPER_CACHE_TYPE_KEEP;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, scz, -1, L"force", -1))
        {
            prgPackages[iPackage].cacheType = BOOTSTRAPPER_CACHE_TYPE_FORCE;
        }

        ++iPackage;
        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to parse all package property elements.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

    pPackages->cPackages = cPackages;
    pPackages->rgPackages = prgPackages;
    prgPackages = NULL;

LExit:
    ReleaseStr(scz);
    ReleaseMem(prgPackages);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);

    return hr;
}


static HRESULT ParseBalPackageInfoFromXml(
    __in BAL_INFO_PACKAGES* pPackages,
    __in IXMLDOMDocument* pixdManifest
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pNodeList = NULL;
    IXMLDOMNode* pNode = NULL;
    LPWSTR scz = NULL;
    BAL_INFO_PACKAGE* pPackage = NULL;
    BOOL fXmlFound = FALSE;

    hr = XmlSelectNodes(pixdManifest, L"/BootstrapperApplicationData/WixBalPackageInfo", &pNodeList);
    ExitOnFailure(hr, "Failed to select all packages.");

    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, NULL)))
    {
        hr = XmlGetAttributeEx(pNode, L"PackageId", &scz);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get package identifier for WixBalPackageInfo.");

        hr = BalInfoFindPackageById(pPackages, scz, &pPackage);
        ExitOnFailure(hr, "Failed to find package specified in WixBalPackageInfo: %ls", scz);

        hr = XmlGetAttributeEx(pNode, L"DisplayInternalUICondition", &pPackage->sczDisplayInternalUICondition);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get DisplayInternalUICondition setting for package.");

        hr = XmlGetAttributeEx(pNode, L"DisplayFilesInUseDialogCondition", &pPackage->sczDisplayFilesInUseDialogCondition);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get DisplayFilesInUseDialogCondition setting for package.");

        hr = XmlGetAttributeEx(pNode, L"PrimaryPackageType", &scz);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get PrimaryPackageType setting for package.");

        if (fXmlFound)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"default", -1))
            {
                pPackage->primaryPackageType = BAL_INFO_PRIMARY_PACKAGE_TYPE_DEFAULT;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"x86", -1))
            {
                pPackage->primaryPackageType = BAL_INFO_PRIMARY_PACKAGE_TYPE_X86;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"x64", -1))
            {
                pPackage->primaryPackageType = BAL_INFO_PRIMARY_PACKAGE_TYPE_X64;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"arm64", -1))
            {
                pPackage->primaryPackageType = BAL_INFO_PRIMARY_PACKAGE_TYPE_ARM64;
            }
            else
            {
                ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for WixBalPackageInfo/@PrimaryPackageType: %ls", scz);
            }
        }

        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to parse all WixBalPackageInfo elements.");

    hr = XmlSelectNodes(pixdManifest, L"/BootstrapperApplicationData/WixPrereqInformation", &pNodeList);
    ExitOnFailure(hr, "Failed to select all packages.");

    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, NULL)))
    {
        hr = XmlGetAttributeEx(pNode, L"PackageId", &scz);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get package identifier for WixPrereqInformation.");

        hr = BalInfoFindPackageById(pPackages, scz, &pPackage);
        ExitOnFailure(hr, "Failed to find package specified in WixPrereqInformation: %ls", scz);

        pPackage->fPrereqPackage = TRUE;

        hr = XmlGetAttributeEx(pNode, L"LicenseFile", &pPackage->sczPrereqLicenseFile);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get LicenseFile setting for prereq package.");

        hr = XmlGetAttributeEx(pNode, L"LicenseUrl", &pPackage->sczPrereqLicenseUrl);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get LicenseUrl setting for prereq package.");

        ReleaseNullObject(pNode);
    }
    ExitOnFailure(hr, "Failed to parse all WixPrereqInformation elements.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseStr(scz);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);

    return hr;
}


static HRESULT ParseOverridableVariablesFromXml(
    __in BAL_INFO_OVERRIDABLE_VARIABLES* pOverridableVariables,
    __in IXMLDOMDocument* pixdManifest
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pCommandLineNode = NULL;
    BOOL fXmlFound = FALSE;
    LPWSTR scz = NULL;
    IXMLDOMNode* pNode = NULL;
    IXMLDOMNodeList* pNodes = NULL;
    BAL_INFO_OVERRIDABLE_VARIABLE* pOverridableVariable = NULL;

    hr = XmlSelectSingleNode(pixdManifest, L"/BootstrapperApplicationData/WixStdbaCommandLine", &pCommandLineNode);
    ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to select command line information.");

    if (!fXmlFound)
    {
        pOverridableVariables->commandLineType = BAL_INFO_VARIABLE_COMMAND_LINE_TYPE_CASE_SENSITIVE;
    }
    else
    {
        // @Variables
        hr = XmlGetAttributeEx(pCommandLineNode, L"VariableType", &scz);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get command line variable type.");

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"caseInsensitive", -1))
        {
            pOverridableVariables->commandLineType = BAL_INFO_VARIABLE_COMMAND_LINE_TYPE_CASE_INSENSITIVE;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"caseSensitive", -1))
        {
            pOverridableVariables->commandLineType = BAL_INFO_VARIABLE_COMMAND_LINE_TYPE_CASE_SENSITIVE;
        }
        else
        {
            ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for CommandLine/@Variables: %ls", scz);
        }
    }

    // Get the list of variables users can override on the command line.
    hr = XmlSelectNodes(pixdManifest, L"/BootstrapperApplicationData/WixStdbaOverridableVariable", &pNodes);
    ExitOnFailure(hr, "Failed to select overridable variable nodes.");

    hr = pNodes->get_length(reinterpret_cast<long*>(&pOverridableVariables->cVariables));
    ExitOnFailure(hr, "Failed to get overridable variable node count.");

    if (pOverridableVariables->cVariables)
    {
        DICT_FLAG dfFlags = BAL_INFO_VARIABLE_COMMAND_LINE_TYPE_CASE_INSENSITIVE == pOverridableVariables->commandLineType ? DICT_FLAG_CASEINSENSITIVE : DICT_FLAG_NONE;

        hr = DictCreateWithEmbeddedKey(&pOverridableVariables->sdVariables, pOverridableVariables->cVariables, reinterpret_cast<void**>(&pOverridableVariables->rgVariables), offsetof(BAL_INFO_OVERRIDABLE_VARIABLE, sczName), dfFlags);
        ExitOnFailure(hr, "Failed to create the overridable variables string dictionary.");

        hr = MemAllocArray(reinterpret_cast<LPVOID*>(&pOverridableVariables->rgVariables), sizeof(pOverridableVariable), pOverridableVariables->cVariables);
        ExitOnFailure(hr, "Failed to create the overridable variables array.");

        for (DWORD i = 0; i < pOverridableVariables->cVariables; ++i)
        {
            pOverridableVariable = pOverridableVariables->rgVariables + i;

            hr = XmlNextElement(pNodes, &pNode, NULL);
            ExitOnFailure(hr, "Failed to get next node.");

            // @Name
            hr = XmlGetAttributeEx(pNode, L"Name", &pOverridableVariable->sczName);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get name for overridable variable.");

            hr = DictAddValue(pOverridableVariables->sdVariables, pOverridableVariable);
            ExitOnFailure(hr, "Failed to add \"%ls\" to the string dictionary.", pOverridableVariable->sczName);

            // prepare next iteration
            ReleaseNullObject(pNode);
        }
    }

LExit:
    ReleaseStr(scz);
    ReleaseObject(pCommandLineNode);
    ReleaseObject(pNode);
    ReleaseObject(pNodes);
    return hr;
}
