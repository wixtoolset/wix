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
    BAL_INFO_PACKAGE_TYPE_BUNDLE_UPDATE,
    BAL_INFO_PACKAGE_TYPE_BUNDLE_CHAIN,
} BAL_INFO_PACKAGE_TYPE;

typedef enum _BAL_INFO_PRIMARY_PACKAGE_TYPE
{
    BAL_INFO_PRIMARY_PACKAGE_TYPE_NONE,
    BAL_INFO_PRIMARY_PACKAGE_TYPE_DEFAULT,
    BAL_INFO_PRIMARY_PACKAGE_TYPE_X86,
    BAL_INFO_PRIMARY_PACKAGE_TYPE_X64,
    BAL_INFO_PRIMARY_PACKAGE_TYPE_ARM64,
} BAL_INFO_PRIMARY_PACKAGE_TYPE;

typedef enum _BAL_INFO_RESTART
{
    BAL_INFO_RESTART_UNKNOWN,
    BAL_INFO_RESTART_NEVER,
    BAL_INFO_RESTART_PROMPT,
    BAL_INFO_RESTART_AUTOMATIC,
    BAL_INFO_RESTART_ALWAYS,
} BAL_INFO_RESTART;

typedef enum _BAL_INFO_VARIABLE_COMMAND_LINE_TYPE
{
    BAL_INFO_VARIABLE_COMMAND_LINE_TYPE_CASE_SENSITIVE,
    BAL_INFO_VARIABLE_COMMAND_LINE_TYPE_CASE_INSENSITIVE,
} BAL_INFO_VARIABLE_COMMAND_LINE_TYPE;


typedef struct _BAL_INFO_PACKAGE
{
    LPWSTR sczId;
    LPWSTR sczDisplayName;
    LPWSTR sczDescription;
    BAL_INFO_PACKAGE_TYPE type;
    BOOL fPermanent;
    BOOL fVital;
    LPWSTR sczDisplayInternalUICondition;
    LPWSTR sczDisplayFilesInUseDialogCondition;
    LPWSTR sczProductCode;
    LPWSTR sczUpgradeCode;
    LPWSTR sczVersion;
    LPWSTR sczInstallCondition;
    LPWSTR sczRepairCondition;
    BOOTSTRAPPER_CACHE_TYPE cacheType;
    BOOL fPrereqPackage;
    LPWSTR sczPrereqLicenseFile;
    LPWSTR sczPrereqLicenseUrl;
    BAL_INFO_PRIMARY_PACKAGE_TYPE primaryPackageType;
    LPVOID pvCustomData;
} BAL_INFO_PACKAGE;


typedef struct _BAL_INFO_PACKAGES
{
    BAL_INFO_PACKAGE* rgPackages;
    DWORD cPackages;
} BAL_INFO_PACKAGES;


typedef struct _BAL_INFO_OVERRIDABLE_VARIABLE
{
    LPWSTR sczName;
} BAL_INFO_OVERRIDABLE_VARIABLE;


typedef struct _BAL_INFO_OVERRIDABLE_VARIABLES
{
    BAL_INFO_OVERRIDABLE_VARIABLE* rgVariables;
    DWORD cVariables;
    STRINGDICT_HANDLE sdVariables;
    BAL_INFO_VARIABLE_COMMAND_LINE_TYPE commandLineType;
} BAL_INFO_OVERRIDABLE_VARIABLES;


typedef struct _BAL_INFO_BUNDLE
{
    BOOL fPerMachine;
    LPWSTR sczName;
    LPWSTR sczLogVariable;
    BAL_INFO_PACKAGES packages;
    BAL_INFO_OVERRIDABLE_VARIABLES overridableVariables;
} BAL_INFO_BUNDLE;


typedef struct _BAL_INFO_COMMAND
{
    DWORD cUnknownArgs;
    LPWSTR* rgUnknownArgs;
    DWORD cVariables;
    LPWSTR* rgVariableNames;
    LPWSTR* rgVariableValues;
    BAL_INFO_RESTART restart;
} BAL_INFO_COMMAND;


/*******************************************************************
 BalInfoParseCommandLine - parses wzCommandLine from BOOTSTRAPPER_COMMAND.

********************************************************************/
DAPI_(HRESULT) BalInfoParseCommandLine(
    __in BAL_INFO_COMMAND* pCommand,
    __in const BOOTSTRAPPER_COMMAND* pBootstrapperCommand
    );


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
    __in_z LPCWSTR wzId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOL fPerMachine,
    __out_opt BAL_INFO_PACKAGE** ppPackage
    );


/*******************************************************************
 BalInfoAddUpdateBundleAsPackage - adds an update bundle as a package.

 ********************************************************************/
DAPI_(HRESULT) BalInfoAddUpdateBundleAsPackage(
    __in BAL_INFO_PACKAGES* pPackages,
    __in_z LPCWSTR wzId,
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


/*******************************************************************
 BalInfoUninitializeCommandLine - uninitializes BAL_INFO_COMMAND.

********************************************************************/
DAPI_(void) BalInfoUninitializeCommandLine(
    __in BAL_INFO_COMMAND* pCommand
);


/*******************************************************************
 BalInfoSetOverridableVariablesFromEngine - sets overridable variables from command line.

 ********************************************************************/
DAPI_(HRESULT) BalSetOverridableVariablesFromEngine(
    __in BAL_INFO_OVERRIDABLE_VARIABLES* pOverridableVariables,
    __in BAL_INFO_COMMAND* pCommand,
    __in IBootstrapperEngine* pEngine
    );


#ifdef __cplusplus
}
#endif
