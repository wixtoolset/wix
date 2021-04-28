#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

typedef enum BAL_INFO_PACKAGE_TYPE
{
    BAL_INFO_PACKAGE_TYPE_UNKNOWN,
    BAL_INFO_PACKAGE_TYPE_EXE,
    BAL_INFO_PACKAGE_TYPE_MSI,
    BAL_INFO_PACKAGE_TYPE_MSP,
    BAL_INFO_PACKAGE_TYPE_MSU,
    BAL_INFO_PACKAGE_TYPE_BUNDLE_UPGRADE,
    BAL_INFO_PACKAGE_TYPE_BUNDLE_ADDON,
    BAL_INFO_PACKAGE_TYPE_BUNDLE_PATCH,
} BAL_INFO_PACKAGE_TYPE;


typedef struct _BAL_INFO_PACKAGE
{
    LPWSTR sczId;
    LPWSTR sczDisplayName;
    LPWSTR sczDescription;
    BAL_INFO_PACKAGE_TYPE type;
    BOOL fPermanent;
    BOOL fVital;
    LPWSTR sczDisplayInternalUICondition;
    LPWSTR sczProductCode;
    LPWSTR sczUpgradeCode;
    LPWSTR sczVersion;
    LPWSTR sczInstallCondition;
    BOOTSTRAPPER_CACHE_TYPE cacheType;
    BOOL fPrereqPackage;
    LPWSTR sczPrereqLicenseFile;
    LPWSTR sczPrereqLicenseUrl;
    LPVOID pvCustomData;
} BAL_INFO_PACKAGE;


typedef struct _BAL_INFO_PACKAGES
{
    BAL_INFO_PACKAGE* rgPackages;
    DWORD cPackages;
} BAL_INFO_PACKAGES;


typedef struct _BAL_INFO_BUNDLE
{
    BOOL fPerMachine;
    LPWSTR sczName;
    LPWSTR sczLogVariable;
    BAL_INFO_PACKAGES packages;
} BAL_INFO_BUNDLE;


/*******************************************************************
 BalInfoParseFromXml - loads the bundle and package info from the UX
                       manifest.

********************************************************************/
DAPI_(HRESULT) BalInfoParseFromXml(
    __in BAL_INFO_BUNDLE* pBundle,
    __in IXMLDOMDocument* pixdManifest
    );


/*******************************************************************
 BalInfoAddRelatedBundleAsPackage - adds a related bundle as a package.

 ********************************************************************/
DAPI_(HRESULT) BalInfoAddRelatedBundleAsPackage(
    __in BAL_INFO_PACKAGES* pPackages,
    __in LPCWSTR wzId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOL fPerMachine,
    __out_opt BAL_INFO_PACKAGE** ppPackage
    );


/*******************************************************************
 BalInfoFindPackageById - finds a package by its id.

 ********************************************************************/
DAPI_(HRESULT) BalInfoFindPackageById(
    __in BAL_INFO_PACKAGES* pPackages,
    __in LPCWSTR wzId,
    __out BAL_INFO_PACKAGE** ppPackage
    );


/*******************************************************************
 BalInfoUninitialize - uninitializes any info previously loaded.

********************************************************************/
DAPI_(void) BalInfoUninitialize(
    __in BAL_INFO_BUNDLE* pBundle
    );


#ifdef __cplusplus
}
#endif
