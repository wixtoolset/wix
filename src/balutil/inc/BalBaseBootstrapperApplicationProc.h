#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#include <windows.h>

#include "BootstrapperEngine.h"
#include "BootstrapperApplication.h"
#include "IBootstrapperEngine.h"
#include "IBootstrapperApplication.h"

static HRESULT BalBaseBAProcOnDetectBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTBEGIN_ARGS* pArgs,
    __inout BA_ONDETECTBEGIN_RESULTS* pResults
    )
{
    return pBA->OnDetectBegin(pArgs->fCached, pArgs->fInstalled, pArgs->cPackages, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTCOMPLETE_ARGS* pArgs,
    __inout BA_ONDETECTCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnDetectComplete(pArgs->hrStatus, pArgs->fEligibleForCleanup);
}

static HRESULT BalBaseBAProcOnPlanBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANBEGIN_ARGS* pArgs,
    __inout BA_ONPLANBEGIN_RESULTS* pResults
    )
{
    return pBA->OnPlanBegin(pArgs->cPackages, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANCOMPLETE_ARGS* pArgs,
    __inout BA_ONPLANCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnPlanComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnStartup(
    __in IBootstrapperApplication* pBA,
    __in BA_ONSTARTUP_ARGS* /*pArgs*/,
    __inout BA_ONSTARTUP_RESULTS* /*pResults*/
    )
{
    return pBA->OnStartup();
}

static HRESULT BalBaseBAProcOnShutdown(
    __in IBootstrapperApplication* pBA,
    __in BA_ONSHUTDOWN_ARGS* /*pArgs*/,
    __inout BA_ONSHUTDOWN_RESULTS* pResults
    )
{
    return pBA->OnShutdown(&pResults->action);
}

static HRESULT BalBaseBAProcOnSystemShutdown(
    __in IBootstrapperApplication* pBA,
    __in BA_ONSYSTEMSHUTDOWN_ARGS* pArgs,
    __inout BA_ONSYSTEMSHUTDOWN_RESULTS* pResults
    )
{
    return pBA->OnSystemShutdown(pArgs->dwEndSession, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectForwardCompatibleBundle(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS* pArgs,
    __inout BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS* pResults
    )
{
    return pBA->OnDetectForwardCompatibleBundle(pArgs->wzBundleId, pArgs->relationType, pArgs->wzBundleTag, pArgs->fPerMachine, pArgs->wzVersion, pArgs->fMissingFromCache, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectUpdateBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTUPDATEBEGIN_ARGS* pArgs,
    __inout BA_ONDETECTUPDATEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnDetectUpdateBegin(pArgs->wzUpdateLocation, &pResults->fCancel, &pResults->fSkip);
}

static HRESULT BalBaseBAProcOnDetectUpdate(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTUPDATE_ARGS* pArgs,
    __inout BA_ONDETECTUPDATE_RESULTS* pResults
    )
{
    return pBA->OnDetectUpdate(pArgs->wzUpdateLocation, pArgs->dw64Size, pArgs->wzVersion, pArgs->wzTitle, pArgs->wzSummary, pArgs->wzContentType, pArgs->wzContent, &pResults->fCancel, &pResults->fStopProcessingUpdates);
}

static HRESULT BalBaseBAProcOnDetectUpdateComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTUPDATECOMPLETE_ARGS* pArgs,
    __inout BA_ONDETECTUPDATECOMPLETE_RESULTS* pResults
    )
{
    return pBA->OnDetectUpdateComplete(pArgs->hrStatus, &pResults->fIgnoreError);
}

static HRESULT BalBaseBAProcOnDetectRelatedBundle(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTRELATEDBUNDLE_ARGS* pArgs,
    __inout BA_ONDETECTRELATEDBUNDLE_RESULTS* pResults
    )
{
    return pBA->OnDetectRelatedBundle(pArgs->wzBundleId, pArgs->relationType, pArgs->wzBundleTag, pArgs->fPerMachine, pArgs->wzVersion, pArgs->operation, pArgs->fMissingFromCache, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectPackageBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONDETECTPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnDetectPackageBegin(pArgs->wzPackageId, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectRelatedMsiPackage(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTRELATEDMSIPACKAGE_ARGS* pArgs,
    __inout BA_ONDETECTRELATEDMSIPACKAGE_RESULTS* pResults
    )
{
    return pBA->OnDetectRelatedMsiPackage(pArgs->wzPackageId, pArgs->wzUpgradeCode, pArgs->wzProductCode, pArgs->fPerMachine, pArgs->wzVersion, pArgs->operation, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectPatchTarget(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTPATCHTARGET_ARGS* pArgs,
    __inout BA_ONDETECTPATCHTARGET_RESULTS* pResults
    )
{
    return pBA->OnDetectPatchTarget(pArgs->wzPackageId, pArgs->wzProductCode, pArgs->patchState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectMsiFeature(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTMSIFEATURE_ARGS* pArgs,
    __inout BA_ONDETECTMSIFEATURE_RESULTS* pResults
    )
{
    return pBA->OnDetectMsiFeature(pArgs->wzPackageId, pArgs->wzFeatureId, pArgs->state, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnDetectPackageComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONDETECTPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONDETECTPACKAGECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnDetectPackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->state, pArgs->fCached);
}

static HRESULT BalBaseBAProcOnPlanRelatedBundle(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANRELATEDBUNDLE_ARGS* pArgs,
    __inout BA_ONPLANRELATEDBUNDLE_RESULTS* pResults
    )
{
    return pBA->OnPlanRelatedBundle(pArgs->wzBundleId, pArgs->recommendedState, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanPackageBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONPLANPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnPlanPackageBegin(pArgs->wzPackageId, pArgs->state, pArgs->fCached, pArgs->installCondition, pArgs->recommendedState, pArgs->recommendedCacheType, &pResults->requestedState, &pResults->requestedCacheType, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanPatchTarget(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANPATCHTARGET_ARGS* pArgs,
    __inout BA_ONPLANPATCHTARGET_RESULTS* pResults
    )
{
    return pBA->OnPlanPatchTarget(pArgs->wzPackageId, pArgs->wzProductCode, pArgs->recommendedState, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanMsiFeature(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANMSIFEATURE_ARGS* pArgs,
    __inout BA_ONPLANMSIFEATURE_RESULTS* pResults
    )
{
    return pBA->OnPlanMsiFeature(pArgs->wzPackageId, pArgs->wzFeatureId, pArgs->recommendedState, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnPlanPackageComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONPLANPACKAGECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnPlanPackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->requested);
}

static HRESULT BalBaseBAProcOnPlannedPackage(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANNEDPACKAGE_ARGS* pArgs,
    __inout BA_ONPLANNEDPACKAGE_RESULTS* /*pResults*/
    )
{
    return pBA->OnPlannedPackage(pArgs->wzPackageId, pArgs->execute, pArgs->rollback, pArgs->fPlannedCache, pArgs->fPlannedUncache);
}

static HRESULT BalBaseBAProcOnApplyBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONAPPLYBEGIN_ARGS* pArgs,
    __inout BA_ONAPPLYBEGIN_RESULTS* pResults
    )
{
    return pBA->OnApplyBegin(pArgs->dwPhaseCount, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnElevateBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONELEVATEBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONELEVATEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnElevateBegin(&pResults->fCancel);
}

static HRESULT BalBaseBAProcOnElevateComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONELEVATECOMPLETE_ARGS* pArgs,
    __inout BA_ONELEVATECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnElevateComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnProgress(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPROGRESS_ARGS* pArgs,
    __inout BA_ONPROGRESS_RESULTS* pResults
    )
{
    return pBA->OnProgress(pArgs->dwProgressPercentage, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnError(
    __in IBootstrapperApplication* pBA,
    __in BA_ONERROR_ARGS* pArgs,
    __inout BA_ONERROR_RESULTS* pResults
    )
{
    return pBA->OnError(pArgs->errorType, pArgs->wzPackageId, pArgs->dwCode, pArgs->wzError, pArgs->dwUIHint, pArgs->cData, pArgs->rgwzData, pArgs->nRecommendation, &pResults->nResult);
}

static HRESULT BalBaseBAProcOnRegisterBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONREGISTERBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONREGISTERBEGIN_RESULTS* pResults
    )
{
    return pBA->OnRegisterBegin(&pResults->fCancel);
}

static HRESULT BalBaseBAProcOnRegisterComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONREGISTERCOMPLETE_ARGS* pArgs,
    __inout BA_ONREGISTERCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnRegisterComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnCacheBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONCACHEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnCacheBegin(&pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCachePackageBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONCACHEPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnCachePackageBegin(pArgs->wzPackageId, pArgs->cCachePayloads, pArgs->dw64PackageCacheSize, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCacheAcquireBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEACQUIREBEGIN_ARGS* pArgs,
    __inout BA_ONCACHEACQUIREBEGIN_RESULTS* pResults
    )
{
    return pBA->OnCacheAcquireBegin(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->wzSource, pArgs->wzDownloadUrl, pArgs->wzPayloadContainerId, pArgs->recommendation, &pResults->action, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCacheAcquireProgress(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEACQUIREPROGRESS_ARGS* pArgs,
    __inout BA_ONCACHEACQUIREPROGRESS_RESULTS* pResults
    )
{
    return pBA->OnCacheAcquireProgress(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->dw64Progress, pArgs->dw64Total, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCacheAcquireResolving(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEACQUIRERESOLVING_ARGS* pArgs,
    __inout BA_ONCACHEACQUIRERESOLVING_RESULTS* pResults
    )
{
    return pBA->OnCacheAcquireResolving(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->rgSearchPaths, pArgs->cSearchPaths, pArgs->fFoundLocal, pArgs->dwRecommendedSearchPath, pArgs->wzDownloadUrl, pArgs->wzPayloadContainerId, pArgs->recommendation, &pResults->dwChosenSearchPath, &pResults->action, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCacheAcquireComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEACQUIRECOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHEACQUIRECOMPLETE_RESULTS* pResults
    )
{
    return pBA->OnCacheAcquireComplete(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->hrStatus, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAProcOnCacheVerifyBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEVERIFYBEGIN_ARGS* pArgs,
    __inout BA_ONCACHEVERIFYBEGIN_RESULTS* pResults
    )
{
    return pBA->OnCacheVerifyBegin(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCacheVerifyProgress(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEVERIFYPROGRESS_ARGS* pArgs,
    __inout BA_ONCACHEVERIFYPROGRESS_RESULTS* pResults
    )
{
    return pBA->OnCacheVerifyProgress(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->dw64Progress, pArgs->dw64Total, pArgs->dwOverallPercentage, pArgs->verifyStep, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCacheVerifyComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEVERIFYCOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHEVERIFYCOMPLETE_RESULTS* pResults
    )
{
    return pBA->OnCacheVerifyComplete(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->hrStatus, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAProcOnCachePackageComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHEPACKAGECOMPLETE_RESULTS* pResults
    )
{
    return pBA->OnCachePackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAProcOnCacheComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHECOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnCacheComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnExecuteBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONEXECUTEBEGIN_ARGS* pArgs,
    __inout BA_ONEXECUTEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnExecuteBegin(pArgs->cExecutingPackages, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnExecutePackageBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONEXECUTEPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONEXECUTEPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnExecutePackageBegin(pArgs->wzPackageId, pArgs->fExecute, pArgs->action, pArgs->uiLevel, pArgs->fDisableExternalUiHandler, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnExecutePatchTarget(
    __in IBootstrapperApplication* pBA,
    __in BA_ONEXECUTEPATCHTARGET_ARGS* pArgs,
    __inout BA_ONEXECUTEPATCHTARGET_RESULTS* pResults
    )
{
    return pBA->OnExecutePatchTarget(pArgs->wzPackageId, pArgs->wzTargetProductCode, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnExecuteProgress(
    __in IBootstrapperApplication* pBA,
    __in BA_ONEXECUTEPROGRESS_ARGS* pArgs,
    __inout BA_ONEXECUTEPROGRESS_RESULTS* pResults
    )
{
    return pBA->OnExecuteProgress(pArgs->wzPackageId, pArgs->dwProgressPercentage, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnExecuteMsiMessage(
    __in IBootstrapperApplication* pBA,
    __in BA_ONEXECUTEMSIMESSAGE_ARGS* pArgs,
    __inout BA_ONEXECUTEMSIMESSAGE_RESULTS* pResults
    )
{
    return pBA->OnExecuteMsiMessage(pArgs->wzPackageId, pArgs->messageType, pArgs->dwUIHint, pArgs->wzMessage, pArgs->cData, pArgs->rgwzData, pArgs->nRecommendation, &pResults->nResult);
}

static HRESULT BalBaseBAProcOnExecuteFilesInUse(
    __in IBootstrapperApplication* pBA,
    __in BA_ONEXECUTEFILESINUSE_ARGS* pArgs,
    __inout BA_ONEXECUTEFILESINUSE_RESULTS* pResults
    )
{
    return pBA->OnExecuteFilesInUse(pArgs->wzPackageId, pArgs->cFiles, pArgs->rgwzFiles, pArgs->nRecommendation, &pResults->nResult);
}

static HRESULT BalBaseBAProcOnExecutePackageComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONEXECUTEPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONEXECUTEPACKAGECOMPLETE_RESULTS* pResults
    )
{
    return pBA->OnExecutePackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->restart, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAProcOnExecuteComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONEXECUTECOMPLETE_ARGS* pArgs,
    __inout BA_ONEXECUTECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnExecuteComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnUnregisterBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONUNREGISTERBEGIN_ARGS* pArgs,
    __inout BA_ONUNREGISTERBEGIN_RESULTS* pResults
    )
{
    return pBA->OnUnregisterBegin(pArgs->fKeepRegistration, &pResults->fForceKeepRegistration);
}

static HRESULT BalBaseBAProcOnUnregisterComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONUNREGISTERCOMPLETE_ARGS* pArgs,
    __inout BA_ONUNREGISTERCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnUnregisterComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnApplyComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONAPPLYCOMPLETE_ARGS* pArgs,
    __inout BA_ONAPPLYCOMPLETE_RESULTS* pResults
    )
{
    return pBA->OnApplyComplete(pArgs->hrStatus, pArgs->restart, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAProcOnLaunchApprovedExeBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONLAUNCHAPPROVEDEXEBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONLAUNCHAPPROVEDEXEBEGIN_RESULTS* pResults
    )
{
    return pBA->OnLaunchApprovedExeBegin(&pResults->fCancel);
}

static HRESULT BalBaseBAProcOnLaunchApprovedExeComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONLAUNCHAPPROVEDEXECOMPLETE_ARGS* pArgs,
    __inout BA_ONLAUNCHAPPROVEDEXECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnLaunchApprovedExeComplete(pArgs->hrStatus, pArgs->dwProcessId);
}

static HRESULT BalBaseBAProcOnPlanMsiPackage(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANMSIPACKAGE_ARGS* pArgs,
    __inout BA_ONPLANMSIPACKAGE_RESULTS* pResults
    )
{
    return pBA->OnPlanMsiPackage(pArgs->wzPackageId, pArgs->fExecute, pArgs->action, &pResults->fCancel, &pResults->actionMsiProperty, &pResults->uiLevel, &pResults->fDisableExternalUiHandler);
}

static HRESULT BalBaseBAProcOnBeginMsiTransactionBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONBEGINMSITRANSACTIONBEGIN_ARGS* pArgs,
    __inout BA_ONBEGINMSITRANSACTIONBEGIN_RESULTS* pResults
    )
{
    return pBA->OnBeginMsiTransactionBegin(pArgs->wzTransactionId, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnBeginMsiTransactionComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONBEGINMSITRANSACTIONCOMPLETE_ARGS* pArgs,
    __inout BA_ONBEGINMSITRANSACTIONCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnBeginMsiTransactionComplete(pArgs->wzTransactionId, pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnCommitMsiTransactionBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCOMMITMSITRANSACTIONBEGIN_ARGS* pArgs,
    __inout BA_ONCOMMITMSITRANSACTIONBEGIN_RESULTS* pResults
    )
{
    return pBA->OnCommitMsiTransactionBegin(pArgs->wzTransactionId, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCommitMsiTransactionComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCOMMITMSITRANSACTIONCOMPLETE_ARGS* pArgs,
    __inout BA_ONCOMMITMSITRANSACTIONCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnCommitMsiTransactionComplete(pArgs->wzTransactionId, pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnRollbackMsiTransactionBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONROLLBACKMSITRANSACTIONBEGIN_ARGS* pArgs,
    __inout BA_ONROLLBACKMSITRANSACTIONBEGIN_RESULTS* /*pResults*/
    )
{
    return pBA->OnRollbackMsiTransactionBegin(pArgs->wzTransactionId);
}

static HRESULT BalBaseBAProcOnRollbackMsiTransactionComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONROLLBACKMSITRANSACTIONCOMPLETE_ARGS* pArgs,
    __inout BA_ONROLLBACKMSITRANSACTIONCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnRollbackMsiTransactionComplete(pArgs->wzTransactionId, pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnPauseAutomaticUpdatesBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPAUSEAUTOMATICUPDATESBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONPAUSEAUTOMATICUPDATESBEGIN_RESULTS* /*pResults*/
    )
{
    return pBA->OnPauseAutomaticUpdatesBegin();
}

static HRESULT BalBaseBAProcOnPauseAutomaticUpdatesComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_ARGS* pArgs,
    __inout BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnPauseAutomaticUpdatesComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnSystemRestorePointBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONSYSTEMRESTOREPOINTBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONSYSTEMRESTOREPOINTBEGIN_RESULTS* /*pResults*/
    )
{
    return pBA->OnSystemRestorePointBegin();
}

static HRESULT BalBaseBAProcOnSystemRestorePointComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONSYSTEMRESTOREPOINTCOMPLETE_ARGS* pArgs,
    __inout BA_ONSYSTEMRESTOREPOINTCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnSystemRestorePointComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnPlanForwardCompatibleBundle(
    __in IBootstrapperApplication* pBA,
    __in BA_ONPLANFORWARDCOMPATIBLEBUNDLE_ARGS* pArgs,
    __inout BA_ONPLANFORWARDCOMPATIBLEBUNDLE_RESULTS* pResults
    )
{
    return pBA->OnPlanForwardCompatibleBundle(pArgs->wzBundleId, pArgs->relationType, pArgs->wzBundleTag, pArgs->fPerMachine, pArgs->wzVersion, pArgs->fRecommendedIgnoreBundle, &pResults->fCancel, &pResults->fIgnoreBundle);
}

static HRESULT BalBaseBAProcOnCacheContainerOrPayloadVerifyBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_ARGS* pArgs,
    __inout BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_RESULTS* pResults
    )
{
    return pBA->OnCacheContainerOrPayloadVerifyBegin(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCacheContainerOrPayloadVerifyProgress(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_ARGS* pArgs,
    __inout BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_RESULTS* pResults
    )
{
    return pBA->OnCacheContainerOrPayloadVerifyProgress(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->dw64Progress, pArgs->dw64Total, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCacheContainerOrPayloadVerifyComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnCacheContainerOrPayloadVerifyComplete(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->hrStatus);
}

static HRESULT BalBaseBAProcOnCachePayloadExtractBegin(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEPAYLOADEXTRACTBEGIN_ARGS* pArgs,
    __inout BA_ONCACHEPAYLOADEXTRACTBEGIN_RESULTS* pResults
    )
{
    return pBA->OnCachePayloadExtractBegin(pArgs->wzContainerId, pArgs->wzPayloadId, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCachePayloadExtractProgress(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEPAYLOADEXTRACTPROGRESS_ARGS* pArgs,
    __inout BA_ONCACHEPAYLOADEXTRACTPROGRESS_RESULTS* pResults
    )
{
    return pBA->OnCachePayloadExtractProgress(pArgs->wzContainerId, pArgs->wzPayloadId, pArgs->dw64Progress, pArgs->dw64Total, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAProcOnCachePayloadExtractComplete(
    __in IBootstrapperApplication* pBA,
    __in BA_ONCACHEPAYLOADEXTRACTCOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHEPAYLOADEXTRACTCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBA->OnCachePayloadExtractComplete(pArgs->wzContainerId, pArgs->wzPayloadId, pArgs->hrStatus);
}

/*******************************************************************
BalBaseBootstrapperApplicationProc - requires pvContext to be of type IBootstrapperApplication.
                                     Provides a default mapping between the new message based BA interface and
                                     the old COM-based BA interface.

*******************************************************************/
static HRESULT WINAPI BalBaseBootstrapperApplicationProc(
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    )
{
    IBootstrapperApplication* pBA = reinterpret_cast<IBootstrapperApplication*>(pvContext);
    HRESULT hr = pBA->BAProc(message, pvArgs, pvResults, pvContext);
    
    if (E_NOTIMPL == hr)
    {
        switch (message)
        {
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTBEGIN:
            hr = BalBaseBAProcOnDetectBegin(pBA, reinterpret_cast<BA_ONDETECTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTCOMPLETE:
            hr = BalBaseBAProcOnDetectComplete(pBA, reinterpret_cast<BA_ONDETECTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANBEGIN:
            hr = BalBaseBAProcOnPlanBegin(pBA, reinterpret_cast<BA_ONPLANBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANCOMPLETE:
            hr = BalBaseBAProcOnPlanComplete(pBA, reinterpret_cast<BA_ONPLANCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSTARTUP:
            hr = BalBaseBAProcOnStartup(pBA, reinterpret_cast<BA_ONSTARTUP_ARGS*>(pvArgs), reinterpret_cast<BA_ONSTARTUP_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSHUTDOWN:
            hr = BalBaseBAProcOnShutdown(pBA, reinterpret_cast<BA_ONSHUTDOWN_ARGS*>(pvArgs), reinterpret_cast<BA_ONSHUTDOWN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMSHUTDOWN:
            hr = BalBaseBAProcOnSystemShutdown(pBA, reinterpret_cast<BA_ONSYSTEMSHUTDOWN_ARGS*>(pvArgs), reinterpret_cast<BA_ONSYSTEMSHUTDOWN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE:
            hr = BalBaseBAProcOnDetectForwardCompatibleBundle(pBA, reinterpret_cast<BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATEBEGIN:
            hr = BalBaseBAProcOnDetectUpdateBegin(pBA, reinterpret_cast<BA_ONDETECTUPDATEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATE:
            hr = BalBaseBAProcOnDetectUpdate(pBA, reinterpret_cast<BA_ONDETECTUPDATE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTUPDATECOMPLETE:
            hr = BalBaseBAProcOnDetectUpdateComplete(pBA, reinterpret_cast<BA_ONDETECTUPDATECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDBUNDLE:
            hr = BalBaseBAProcOnDetectRelatedBundle(pBA, reinterpret_cast<BA_ONDETECTRELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGEBEGIN:
            hr = BalBaseBAProcOnDetectPackageBegin(pBA, reinterpret_cast<BA_ONDETECTPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTRELATEDMSIPACKAGE:
            hr = BalBaseBAProcOnDetectRelatedMsiPackage(pBA, reinterpret_cast<BA_ONDETECTRELATEDMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPATCHTARGET:
            hr = BalBaseBAProcOnDetectPatchTarget(pBA, reinterpret_cast<BA_ONDETECTPATCHTARGET_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPATCHTARGET_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTMSIFEATURE:
            hr = BalBaseBAProcOnDetectMsiFeature(pBA, reinterpret_cast<BA_ONDETECTMSIFEATURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTMSIFEATURE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONDETECTPACKAGECOMPLETE:
            hr = BalBaseBAProcOnDetectPackageComplete(pBA, reinterpret_cast<BA_ONDETECTPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANRELATEDBUNDLE:
            hr = BalBaseBAProcOnPlanRelatedBundle(pBA, reinterpret_cast<BA_ONPLANRELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANRELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGEBEGIN:
            hr = BalBaseBAProcOnPlanPackageBegin(pBA, reinterpret_cast<BA_ONPLANPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPATCHTARGET:
            hr = BalBaseBAProcOnPlanPatchTarget(pBA, reinterpret_cast<BA_ONPLANPATCHTARGET_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPATCHTARGET_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIFEATURE:
            hr = BalBaseBAProcOnPlanMsiFeature(pBA, reinterpret_cast<BA_ONPLANMSIFEATURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANMSIFEATURE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANPACKAGECOMPLETE:
            hr = BalBaseBAProcOnPlanPackageComplete(pBA, reinterpret_cast<BA_ONPLANPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYBEGIN:
            hr = BalBaseBAProcOnApplyBegin(pBA, reinterpret_cast<BA_ONAPPLYBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONAPPLYBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATEBEGIN:
            hr = BalBaseBAProcOnElevateBegin(pBA, reinterpret_cast<BA_ONELEVATEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONELEVATEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONELEVATECOMPLETE:
            hr = BalBaseBAProcOnElevateComplete(pBA, reinterpret_cast<BA_ONELEVATECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONELEVATECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPROGRESS:
            hr = BalBaseBAProcOnProgress(pBA, reinterpret_cast<BA_ONPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONERROR:
            hr = BalBaseBAProcOnError(pBA, reinterpret_cast<BA_ONERROR_ARGS*>(pvArgs), reinterpret_cast<BA_ONERROR_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERBEGIN:
            hr = BalBaseBAProcOnRegisterBegin(pBA, reinterpret_cast<BA_ONREGISTERBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONREGISTERBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONREGISTERCOMPLETE:
            hr = BalBaseBAProcOnRegisterComplete(pBA, reinterpret_cast<BA_ONREGISTERCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONREGISTERCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEBEGIN:
            hr = BalBaseBAProcOnCacheBegin(pBA, reinterpret_cast<BA_ONCACHEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGEBEGIN:
            hr = BalBaseBAProcOnCachePackageBegin(pBA, reinterpret_cast<BA_ONCACHEPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREBEGIN:
            hr = BalBaseBAProcOnCacheAcquireBegin(pBA, reinterpret_cast<BA_ONCACHEACQUIREBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIREBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIREPROGRESS:
            hr = BalBaseBAProcOnCacheAcquireProgress(pBA, reinterpret_cast<BA_ONCACHEACQUIREPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIREPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRERESOLVING:
            hr = BalBaseBAProcOnCacheAcquireResolving(pBA, reinterpret_cast<BA_ONCACHEACQUIRERESOLVING_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIRERESOLVING_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEACQUIRECOMPLETE:
            hr = BalBaseBAProcOnCacheAcquireComplete(pBA, reinterpret_cast<BA_ONCACHEACQUIRECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIRECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYBEGIN:
            hr = BalBaseBAProcOnCacheVerifyBegin(pBA, reinterpret_cast<BA_ONCACHEVERIFYBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEVERIFYBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYPROGRESS:
            hr = BalBaseBAProcOnCacheVerifyProgress(pBA, reinterpret_cast<BA_ONCACHEVERIFYPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEVERIFYPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEVERIFYCOMPLETE:
            hr = BalBaseBAProcOnCacheVerifyComplete(pBA, reinterpret_cast<BA_ONCACHEVERIFYCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEVERIFYCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPACKAGECOMPLETE:
            hr = BalBaseBAProcOnCachePackageComplete(pBA, reinterpret_cast<BA_ONCACHEPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECOMPLETE:
            hr = BalBaseBAProcOnCacheComplete(pBA, reinterpret_cast<BA_ONCACHECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEBEGIN:
            hr = BalBaseBAProcOnExecuteBegin(pBA, reinterpret_cast<BA_ONEXECUTEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGEBEGIN:
            hr = BalBaseBAProcOnExecutePackageBegin(pBA, reinterpret_cast<BA_ONEXECUTEPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPATCHTARGET:
            hr = BalBaseBAProcOnExecutePatchTarget(pBA, reinterpret_cast<BA_ONEXECUTEPATCHTARGET_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPATCHTARGET_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPROGRESS:
            hr = BalBaseBAProcOnExecuteProgress(pBA, reinterpret_cast<BA_ONEXECUTEPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEMSIMESSAGE:
            hr = BalBaseBAProcOnExecuteMsiMessage(pBA, reinterpret_cast<BA_ONEXECUTEMSIMESSAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEMSIMESSAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEFILESINUSE:
            hr = BalBaseBAProcOnExecuteFilesInUse(pBA, reinterpret_cast<BA_ONEXECUTEFILESINUSE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEFILESINUSE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTEPACKAGECOMPLETE:
            hr = BalBaseBAProcOnExecutePackageComplete(pBA, reinterpret_cast<BA_ONEXECUTEPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONEXECUTECOMPLETE:
            hr = BalBaseBAProcOnExecuteComplete(pBA, reinterpret_cast<BA_ONEXECUTECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERBEGIN:
            hr = BalBaseBAProcOnUnregisterBegin(pBA, reinterpret_cast<BA_ONUNREGISTERBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONUNREGISTERBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONUNREGISTERCOMPLETE:
            hr = BalBaseBAProcOnUnregisterComplete(pBA, reinterpret_cast<BA_ONUNREGISTERCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONUNREGISTERCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONAPPLYCOMPLETE:
            hr = BalBaseBAProcOnApplyComplete(pBA, reinterpret_cast<BA_ONAPPLYCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONAPPLYCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXEBEGIN:
            hr = BalBaseBAProcOnLaunchApprovedExeBegin(pBA, reinterpret_cast<BA_ONLAUNCHAPPROVEDEXEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONLAUNCHAPPROVEDEXEBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONLAUNCHAPPROVEDEXECOMPLETE:
            hr = BalBaseBAProcOnLaunchApprovedExeComplete(pBA, reinterpret_cast<BA_ONLAUNCHAPPROVEDEXECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONLAUNCHAPPROVEDEXECOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANMSIPACKAGE:
            hr = BalBaseBAProcOnPlanMsiPackage(pBA, reinterpret_cast<BA_ONPLANMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONBEGIN:
            hr = BalBaseBAProcOnBeginMsiTransactionBegin(pBA, reinterpret_cast<BA_ONBEGINMSITRANSACTIONBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONBEGINMSITRANSACTIONBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONBEGINMSITRANSACTIONCOMPLETE:
            hr = BalBaseBAProcOnBeginMsiTransactionComplete(pBA, reinterpret_cast<BA_ONBEGINMSITRANSACTIONCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONBEGINMSITRANSACTIONCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONBEGIN:
            hr = BalBaseBAProcOnCommitMsiTransactionBegin(pBA, reinterpret_cast<BA_ONCOMMITMSITRANSACTIONBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCOMMITMSITRANSACTIONBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCOMMITMSITRANSACTIONCOMPLETE:
            hr = BalBaseBAProcOnCommitMsiTransactionComplete(pBA, reinterpret_cast<BA_ONCOMMITMSITRANSACTIONCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCOMMITMSITRANSACTIONCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONBEGIN:
            hr = BalBaseBAProcOnRollbackMsiTransactionBegin(pBA, reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONROLLBACKMSITRANSACTIONCOMPLETE:
            hr = BalBaseBAProcOnRollbackMsiTransactionComplete(pBA, reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONCOMPLETE_RESULTS*>(pvResults));
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESBEGIN:
            hr = BalBaseBAProcOnPauseAutomaticUpdatesBegin(pBA, reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPAUSEAUTOMATICUPDATESCOMPLETE:
            hr = BalBaseBAProcOnPauseAutomaticUpdatesComplete(pBA, reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTBEGIN:
            hr = BalBaseBAProcOnSystemRestorePointBegin(pBA, reinterpret_cast<BA_ONSYSTEMRESTOREPOINTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONSYSTEMRESTOREPOINTBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONSYSTEMRESTOREPOINTCOMPLETE:
            hr = BalBaseBAProcOnSystemRestorePointComplete(pBA, reinterpret_cast<BA_ONSYSTEMRESTOREPOINTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONSYSTEMRESTOREPOINTCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANNEDPACKAGE:
            hr = BalBaseBAProcOnPlannedPackage(pBA, reinterpret_cast<BA_ONPLANNEDPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANNEDPACKAGE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONPLANFORWARDCOMPATIBLEBUNDLE:
            hr = BalBaseBAProcOnPlanForwardCompatibleBundle(pBA, reinterpret_cast<BA_ONPLANFORWARDCOMPATIBLEBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANFORWARDCOMPATIBLEBUNDLE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYBEGIN:
            hr = BalBaseBAProcOnCacheContainerOrPayloadVerifyBegin(pBA, reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS:
            hr = BalBaseBAProcOnCacheContainerOrPayloadVerifyProgress(pBA, reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE:
            hr = BalBaseBAProcOnCacheContainerOrPayloadVerifyComplete(pBA, reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTBEGIN:
            hr = BalBaseBAProcOnCachePayloadExtractBegin(pBA, reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTBEGIN_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTPROGRESS:
            hr = BalBaseBAProcOnCachePayloadExtractProgress(pBA, reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTPROGRESS_RESULTS*>(pvResults));
            break;
        case BOOTSTRAPPER_APPLICATION_MESSAGE_ONCACHEPAYLOADEXTRACTCOMPLETE:
            hr = BalBaseBAProcOnCachePayloadExtractComplete(pBA, reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTCOMPLETE_RESULTS*>(pvResults));
            break;
        }
    }

    pBA->BAProcFallback(message, pvArgs, pvResults, &hr, pvContext);

    return hr;
}
