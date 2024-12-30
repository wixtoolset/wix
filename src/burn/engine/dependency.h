#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

// constants

const LPCWSTR DEPENDENCY_IGNOREDEPENDENCIES = L"IGNOREDEPENDENCIES";


typedef struct _BURN_DEPENDENCIES
{
    DEPENDENCY* rgIgnoredDependencies;
    UINT cIgnoredDependencies;
    LPCWSTR wzActiveParent;
    LPCWSTR wzSelfDependent;
    BOOL fIgnoreAllDependents;
    BOOL fSelfDependent;
    BOOL fActiveParent;
} BURN_DEPENDENCIES;


// function declarations

/********************************************************************
 DependencyUninitializeProvider - Frees and zeros memory allocated in
  the dependency provider.

*********************************************************************/
void DependencyUninitializeProvider(
    __in BURN_DEPENDENCY_PROVIDER* pProvider
    );

/********************************************************************
 DependencyParseProvidersFromXml - Parses dependency information
  from the manifest for the specified package.

*********************************************************************/
HRESULT DependencyParseProvidersFromXml(
    __in BURN_PACKAGE* pPackage,
    __in IXMLDOMNode* pixnPackage
    );

HRESULT DependencyInitialize(
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in BURN_DEPENDENCIES* pDependencies,
    __in BURN_REGISTRATION* pRegistration
    );

void DependencyUninitialize(
    __in BURN_DEPENDENCIES* pDependencies
    );

/********************************************************************
 DependencyDetectProviderKeyBundleCode - Detect if the provider key is
  registered and if so what bundle is registered.

 Note: Returns E_NOTFOUND if the provider key is not registered.
*********************************************************************/
HRESULT DependencyDetectProviderKeyBundleCode(
    __in BURN_REGISTRATION* pRegistration
    );

/********************************************************************
 DependencyDetect - Detects dependency information.

*********************************************************************/
HRESULT DependencyDetectBundle(
    __in BURN_DEPENDENCIES* pDependencies,
    __in BURN_REGISTRATION* pRegistration
    );

HRESULT DependencyDetectChainPackage(
    __in BURN_PACKAGE* pPackage,
    __in BURN_REGISTRATION* pRegistration
    );

HRESULT DependencyDetectRelatedBundle(
    __in BURN_RELATED_BUNDLE* pRelatedBundle,
    __in BURN_REGISTRATION* pRegistration
    );

/********************************************************************
 DependencyPlanInitialize - Initializes the plan.

*********************************************************************/
HRESULT DependencyPlanInitialize(
    __in BURN_DEPENDENCIES* pDependencies,
    __in BURN_PLAN* pPlan
    );

/********************************************************************
 DependencyAllocIgnoreDependencies - Allocates the dependencies to
  ignore as a semicolon-delimited string.

*********************************************************************/
HRESULT DependencyAllocIgnoreDependencies(
    __in const BURN_PLAN *pPlan,
    __out_z LPWSTR* psczIgnoreDependencies
    );

/********************************************************************
 DependencyAddIgnoreDependencies - Populates the ignore dependency
  names.

*********************************************************************/
HRESULT DependencyAddIgnoreDependencies(
    __in STRINGDICT_HANDLE sdIgnoreDependencies,
    __in_z LPCWSTR wzAddIgnoreDependencies
    );

/********************************************************************
 DependencyPlanPackageBegin - Updates the dependency registration
  action depending on the calculated state for the package.

*********************************************************************/
HRESULT DependencyPlanPackageBegin(
    __in BOOL fPerMachine,
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan
    );

/********************************************************************
 DependencyPlanPackage - adds dependency related actions to the plan
  for this package.

*********************************************************************/
HRESULT DependencyPlanPackage(
    __in_opt DWORD *pdwInsertSequence,
    __in const BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan
    );

/********************************************************************
 DependencyPlanPackageComplete - Updates the dependency registration
  action depending on the planned action for the package.

*********************************************************************/
HRESULT DependencyPlanPackageComplete(
    __in BURN_PACKAGE* pPackage,
    __in BURN_PLAN* pPlan
    );

/********************************************************************
 DependencyExecutePackageProviderAction - Registers or unregisters
  provider information for the package contained within the action.

*********************************************************************/
HRESULT DependencyExecutePackageProviderAction(
    __in const BURN_EXECUTE_ACTION* pAction,
    __in BOOL fRollback
    );

/********************************************************************
 DependencyExecutePackageDependencyAction - Registers or unregisters
  dependency information for the package contained within the action.

*********************************************************************/
HRESULT DependencyExecutePackageDependencyAction(
    __in BOOL fPerMachine,
    __in const BURN_EXECUTE_ACTION* pAction,
    __in BOOL fRollback
    );

/********************************************************************
 DependencyRegisterBundle - Registers the bundle dependency provider.

*********************************************************************/
HRESULT DependencyRegisterBundle(
    __in const BURN_REGISTRATION* pRegistration
    );

/********************************************************************
 DependencyProcessDependentRegistration - Registers or unregisters dependents
  on the bundle based on the action.

*********************************************************************/
HRESULT DependencyProcessDependentRegistration(
    __in const BURN_REGISTRATION* pRegistration,
    __in const BURN_DEPENDENT_REGISTRATION_ACTION* pAction
    );

/********************************************************************
 DependencyUnregisterBundle - Removes the bundle dependency provider.

 Note: Does not check for existing dependents before removing the key.
*********************************************************************/
void DependencyUnregisterBundle(
    __in const BURN_REGISTRATION* pRegistration,
    __in const BURN_PACKAGES* pPackages
    );

HRESULT DependencyDetectCompatibleEntry(
    __in BURN_PACKAGE* pPackage,
    __in BURN_REGISTRATION* pRegistration
    );

#if defined(__cplusplus)
}
#endif
