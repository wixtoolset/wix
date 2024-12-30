// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT BalBaseBAFunctionsProcOnDestroy(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDESTROY_ARGS* pArgs,
    __inout BA_ONDESTROY_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnDestroy(pArgs->fReload);
}

static HRESULT BalBaseBAFunctionsProcOnDetectBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTBEGIN_ARGS* pArgs,
    __inout BA_ONDETECTBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectBegin(pArgs->fCached, pArgs->registrationType, pArgs->cPackages, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnDetectComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTCOMPLETE_ARGS* pArgs,
    __inout BA_ONDETECTCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnDetectComplete(pArgs->hrStatus, pArgs->fEligibleForCleanup);
}

static HRESULT BalBaseBAFunctionsProcOnPlanBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANBEGIN_ARGS* pArgs,
    __inout BA_ONPLANBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanBegin(pArgs->cPackages, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnPlanComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANCOMPLETE_ARGS* pArgs,
    __inout BA_ONPLANCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnPlanComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnStartup(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONSTARTUP_ARGS* /*pArgs*/,
    __inout BA_ONSTARTUP_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnStartup();
}

static HRESULT BalBaseBAFunctionsProcOnShutdown(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONSHUTDOWN_ARGS* /*pArgs*/,
    __inout BA_ONSHUTDOWN_RESULTS* pResults
    )
{
    return pBAFunctions->OnShutdown(&pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnDetectForwardCompatibleBundle(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS* pArgs,
    __inout BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectForwardCompatibleBundle(pArgs->wzBundleCode, pArgs->relationType, pArgs->wzBundleTag, pArgs->fPerMachine, pArgs->wzVersion, pArgs->fMissingFromCache, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnDetectUpdateBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTUPDATEBEGIN_ARGS* pArgs,
    __inout BA_ONDETECTUPDATEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectUpdateBegin(pArgs->wzUpdateLocation, &pResults->fCancel, &pResults->fSkip);
}

static HRESULT BalBaseBAFunctionsProcOnDetectUpdate(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTUPDATE_ARGS* pArgs,
    __inout BA_ONDETECTUPDATE_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectUpdate(pArgs->wzUpdateLocation, pArgs->dw64Size, pArgs->wzHash, pArgs->hashAlgorithm, pArgs->wzVersion, pArgs->wzTitle, pArgs->wzSummary, pArgs->wzContentType, pArgs->wzContent, &pResults->fCancel, &pResults->fStopProcessingUpdates);
}

static HRESULT BalBaseBAFunctionsProcOnDetectUpdateComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTUPDATECOMPLETE_ARGS* pArgs,
    __inout BA_ONDETECTUPDATECOMPLETE_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectUpdateComplete(pArgs->hrStatus, &pResults->fIgnoreError);
}

static HRESULT BalBaseBAFunctionsProcOnDetectRelatedBundle(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTRELATEDBUNDLE_ARGS* pArgs,
    __inout BA_ONDETECTRELATEDBUNDLE_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectRelatedBundle(pArgs->wzBundleCode, pArgs->relationType, pArgs->wzBundleTag, pArgs->fPerMachine, pArgs->wzVersion, pArgs->fMissingFromCache, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnDetectPackageBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONDETECTPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectPackageBegin(pArgs->wzPackageId, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnDetectCompatiblePackage(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTCOMPATIBLEMSIPACKAGE_ARGS* pArgs,
    __inout BA_ONDETECTCOMPATIBLEMSIPACKAGE_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectCompatibleMsiPackage(pArgs->wzPackageId, pArgs->wzCompatiblePackageId, pArgs->wzCompatiblePackageVersion, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnDetectRelatedMsiPackage(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTRELATEDMSIPACKAGE_ARGS* pArgs,
    __inout BA_ONDETECTRELATEDMSIPACKAGE_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectRelatedMsiPackage(pArgs->wzPackageId, pArgs->wzUpgradeCode, pArgs->wzProductCode, pArgs->fPerMachine, pArgs->wzVersion, pArgs->operation, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnDetectPatchTarget(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTPATCHTARGET_ARGS* pArgs,
    __inout BA_ONDETECTPATCHTARGET_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectPatchTarget(pArgs->wzPackageId, pArgs->wzProductCode, pArgs->patchState, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnDetectMsiFeature(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTMSIFEATURE_ARGS* pArgs,
    __inout BA_ONDETECTMSIFEATURE_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectMsiFeature(pArgs->wzPackageId, pArgs->wzFeatureId, pArgs->state, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnDetectPackageComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONDETECTPACKAGECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnDetectPackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->state, pArgs->fCached);
}

static HRESULT BalBaseBAFunctionsProcOnPlanRelatedBundle(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANRELATEDBUNDLE_ARGS* pArgs,
    __inout BA_ONPLANRELATEDBUNDLE_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanRelatedBundle(pArgs->wzBundleCode, pArgs->recommendedState, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnPlanRollbackBoundary(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANROLLBACKBOUNDARY_ARGS* pArgs,
    __inout BA_ONPLANROLLBACKBOUNDARY_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanRollbackBoundary(pArgs->wzRollbackBoundaryId, pArgs->fRecommendedTransaction, &pResults->fTransaction, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnPlanPackageBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONPLANPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanPackageBegin(pArgs->wzPackageId, pArgs->state, pArgs->fCached, pArgs->installCondition, pArgs->repairCondition, pArgs->recommendedState, pArgs->recommendedCacheType, &pResults->requestedState, &pResults->requestedCacheType, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnPlanCompatibleMsiPackageBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanCompatibleMsiPackageBegin(pArgs->wzPackageId, pArgs->wzCompatiblePackageId, pArgs->wzCompatiblePackageVersion, pArgs->fRecommendedRemove, &pResults->fRequestRemove, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnPlanCompatibleMsiPackageComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnPlanCompatibleMsiPackageComplete(pArgs->wzPackageId, pArgs->wzCompatiblePackageId, pArgs->hrStatus, pArgs->fRequestedRemove);
}

static HRESULT BalBaseBAFunctionsProcOnPlanPatchTarget(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANPATCHTARGET_ARGS* pArgs,
    __inout BA_ONPLANPATCHTARGET_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanPatchTarget(pArgs->wzPackageId, pArgs->wzProductCode, pArgs->recommendedState, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnPlanMsiFeature(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANMSIFEATURE_ARGS* pArgs,
    __inout BA_ONPLANMSIFEATURE_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanMsiFeature(pArgs->wzPackageId, pArgs->wzFeatureId, pArgs->recommendedState, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnPlanPackageComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONPLANPACKAGECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnPlanPackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->requested);
}

static HRESULT BalBaseBAFunctionsProcOnPlannedCompatiblePackage(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANNEDCOMPATIBLEPACKAGE_ARGS* pArgs,
    __inout BA_ONPLANNEDCOMPATIBLEPACKAGE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnPlannedCompatiblePackage(pArgs->wzPackageId, pArgs->wzCompatiblePackageId, pArgs->fRemove);
}

static HRESULT BalBaseBAFunctionsProcOnPlannedPackage(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANNEDPACKAGE_ARGS* pArgs,
    __inout BA_ONPLANNEDPACKAGE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnPlannedPackage(pArgs->wzPackageId, pArgs->execute, pArgs->rollback, pArgs->fPlannedCache, pArgs->fPlannedUncache);
}

static HRESULT BalBaseBAFunctionsProcOnApplyBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONAPPLYBEGIN_ARGS* pArgs,
    __inout BA_ONAPPLYBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnApplyBegin(pArgs->dwPhaseCount, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnElevateBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONELEVATEBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONELEVATEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnElevateBegin(&pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnElevateComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONELEVATECOMPLETE_ARGS* pArgs,
    __inout BA_ONELEVATECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnElevateComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnProgress(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPROGRESS_ARGS* pArgs,
    __inout BA_ONPROGRESS_RESULTS* pResults
    )
{
    return pBAFunctions->OnProgress(pArgs->dwProgressPercentage, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnError(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONERROR_ARGS* pArgs,
    __inout BA_ONERROR_RESULTS* pResults
    )
{
    return pBAFunctions->OnError(pArgs->errorType, pArgs->wzPackageId, pArgs->dwCode, pArgs->wzError, pArgs->dwUIHint, pArgs->cData, pArgs->rgwzData, pArgs->nRecommendation, &pResults->nResult);
}

static HRESULT BalBaseBAFunctionsProcOnRegisterBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONREGISTERBEGIN_ARGS* pArgs,
    __inout BA_ONREGISTERBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnRegisterBegin(pArgs->recommendedRegistrationType, &pResults->fCancel, &pResults->registrationType);
}

static HRESULT BalBaseBAFunctionsProcOnRegisterComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONREGISTERCOMPLETE_ARGS* pArgs,
    __inout BA_ONREGISTERCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnRegisterComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnCacheBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONCACHEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheBegin(&pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCachePackageBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONCACHEPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnCachePackageBegin(pArgs->wzPackageId, pArgs->cCachePayloads, pArgs->dw64PackageCacheSize, pArgs->fVital, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCacheAcquireBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEACQUIREBEGIN_ARGS* pArgs,
    __inout BA_ONCACHEACQUIREBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheAcquireBegin(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->wzSource, pArgs->wzDownloadUrl, pArgs->wzPayloadContainerId, pArgs->recommendation, &pResults->action, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCacheAcquireProgress(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEACQUIREPROGRESS_ARGS* pArgs,
    __inout BA_ONCACHEACQUIREPROGRESS_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheAcquireProgress(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->dw64Progress, pArgs->dw64Total, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCacheAcquireResolving(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEACQUIRERESOLVING_ARGS* pArgs,
    __inout BA_ONCACHEACQUIRERESOLVING_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheAcquireResolving(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->rgSearchPaths, pArgs->cSearchPaths, pArgs->fFoundLocal, pArgs->dwRecommendedSearchPath, pArgs->wzDownloadUrl, pArgs->wzPayloadContainerId, pArgs->recommendation, &pResults->dwChosenSearchPath, &pResults->action, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCacheAcquireComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEACQUIRECOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHEACQUIRECOMPLETE_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheAcquireComplete(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->hrStatus, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnCacheVerifyBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEVERIFYBEGIN_ARGS* pArgs,
    __inout BA_ONCACHEVERIFYBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheVerifyBegin(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCacheVerifyProgress(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEVERIFYPROGRESS_ARGS* pArgs,
    __inout BA_ONCACHEVERIFYPROGRESS_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheVerifyProgress(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->dw64Progress, pArgs->dw64Total, pArgs->dwOverallPercentage, pArgs->verifyStep, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCacheVerifyComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEVERIFYCOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHEVERIFYCOMPLETE_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheVerifyComplete(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->hrStatus, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnCachePackageComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHEPACKAGECOMPLETE_RESULTS* pResults
    )
{
    return pBAFunctions->OnCachePackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnCacheComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHECOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnCacheComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnExecuteBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONEXECUTEBEGIN_ARGS* pArgs,
    __inout BA_ONEXECUTEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnExecuteBegin(pArgs->cExecutingPackages, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnExecutePackageBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONEXECUTEPACKAGEBEGIN_ARGS* pArgs,
    __inout BA_ONEXECUTEPACKAGEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnExecutePackageBegin(pArgs->wzPackageId, pArgs->fExecute, pArgs->action, pArgs->uiLevel, pArgs->fDisableExternalUiHandler, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnExecutePatchTarget(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONEXECUTEPATCHTARGET_ARGS* pArgs,
    __inout BA_ONEXECUTEPATCHTARGET_RESULTS* pResults
    )
{
    return pBAFunctions->OnExecutePatchTarget(pArgs->wzPackageId, pArgs->wzTargetProductCode, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnExecuteProgress(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONEXECUTEPROGRESS_ARGS* pArgs,
    __inout BA_ONEXECUTEPROGRESS_RESULTS* pResults
    )
{
    return pBAFunctions->OnExecuteProgress(pArgs->wzPackageId, pArgs->dwProgressPercentage, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnExecuteMsiMessage(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONEXECUTEMSIMESSAGE_ARGS* pArgs,
    __inout BA_ONEXECUTEMSIMESSAGE_RESULTS* pResults
    )
{
    return pBAFunctions->OnExecuteMsiMessage(pArgs->wzPackageId, pArgs->messageType, pArgs->dwUIHint, pArgs->wzMessage, pArgs->cData, pArgs->rgwzData, pArgs->nRecommendation, &pResults->nResult);
}

static HRESULT BalBaseBAFunctionsProcOnExecuteFilesInUse(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONEXECUTEFILESINUSE_ARGS* pArgs,
    __inout BA_ONEXECUTEFILESINUSE_RESULTS* pResults
    )
{
    return pBAFunctions->OnExecuteFilesInUse(pArgs->wzPackageId, pArgs->cFiles, pArgs->rgwzFiles, pArgs->nRecommendation, pArgs->source, &pResults->nResult);
}

static HRESULT BalBaseBAFunctionsProcOnExecutePackageComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONEXECUTEPACKAGECOMPLETE_ARGS* pArgs,
    __inout BA_ONEXECUTEPACKAGECOMPLETE_RESULTS* pResults
    )
{
    return pBAFunctions->OnExecutePackageComplete(pArgs->wzPackageId, pArgs->hrStatus, pArgs->restart, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnExecuteProcessCancel(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONEXECUTEPROCESSCANCEL_ARGS* pArgs,
    __inout BA_ONEXECUTEPROCESSCANCEL_RESULTS* pResults
    )
{
    return pBAFunctions->OnExecuteProcessCancel(pArgs->wzPackageId, pArgs->dwProcessId, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnExecuteComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONEXECUTECOMPLETE_ARGS* pArgs,
    __inout BA_ONEXECUTECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnExecuteComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnUnregisterBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONUNREGISTERBEGIN_ARGS* pArgs,
    __inout BA_ONUNREGISTERBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnUnregisterBegin(pArgs->recommendedRegistrationType, &pResults->registrationType);
}

static HRESULT BalBaseBAFunctionsProcOnUnregisterComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONUNREGISTERCOMPLETE_ARGS* pArgs,
    __inout BA_ONUNREGISTERCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnUnregisterComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnApplyComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONAPPLYCOMPLETE_ARGS* pArgs,
    __inout BA_ONAPPLYCOMPLETE_RESULTS* pResults
    )
{
    return pBAFunctions->OnApplyComplete(pArgs->hrStatus, pArgs->restart, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnLaunchApprovedExeBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONLAUNCHAPPROVEDEXEBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONLAUNCHAPPROVEDEXEBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnLaunchApprovedExeBegin(&pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnLaunchApprovedExeComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONLAUNCHAPPROVEDEXECOMPLETE_ARGS* pArgs,
    __inout BA_ONLAUNCHAPPROVEDEXECOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnLaunchApprovedExeComplete(pArgs->hrStatus, pArgs->dwProcessId);
}

static HRESULT BalBaseBAFunctionsProcOnPlanMsiPackage(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANMSIPACKAGE_ARGS* pArgs,
    __inout BA_ONPLANMSIPACKAGE_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanMsiPackage(pArgs->wzPackageId, pArgs->fExecute, pArgs->action, pArgs->recommendedFileVersioning, &pResults->fCancel, &pResults->actionMsiProperty, &pResults->uiLevel, &pResults->fDisableExternalUiHandler, &pResults->fileVersioning);
}

static HRESULT BalBaseBAFunctionsProcOnBeginMsiTransactionBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONBEGINMSITRANSACTIONBEGIN_ARGS* pArgs,
    __inout BA_ONBEGINMSITRANSACTIONBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnBeginMsiTransactionBegin(pArgs->wzTransactionId, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnBeginMsiTransactionComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONBEGINMSITRANSACTIONCOMPLETE_ARGS* pArgs,
    __inout BA_ONBEGINMSITRANSACTIONCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnBeginMsiTransactionComplete(pArgs->wzTransactionId, pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnCommitMsiTransactionBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCOMMITMSITRANSACTIONBEGIN_ARGS* pArgs,
    __inout BA_ONCOMMITMSITRANSACTIONBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnCommitMsiTransactionBegin(pArgs->wzTransactionId, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCommitMsiTransactionComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCOMMITMSITRANSACTIONCOMPLETE_ARGS* pArgs,
    __inout BA_ONCOMMITMSITRANSACTIONCOMPLETE_RESULTS* pResults
    )
{
    return pBAFunctions->OnCommitMsiTransactionComplete(pArgs->wzTransactionId, pArgs->hrStatus, pArgs->restart, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnRollbackMsiTransactionBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONROLLBACKMSITRANSACTIONBEGIN_ARGS* pArgs,
    __inout BA_ONROLLBACKMSITRANSACTIONBEGIN_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnRollbackMsiTransactionBegin(pArgs->wzTransactionId);
}

static HRESULT BalBaseBAFunctionsProcOnRollbackMsiTransactionComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONROLLBACKMSITRANSACTIONCOMPLETE_ARGS* pArgs,
    __inout BA_ONROLLBACKMSITRANSACTIONCOMPLETE_RESULTS* pResults
    )
{
    return pBAFunctions->OnRollbackMsiTransactionComplete(pArgs->wzTransactionId, pArgs->hrStatus, pArgs->restart, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnPauseAutomaticUpdatesBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPAUSEAUTOMATICUPDATESBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONPAUSEAUTOMATICUPDATESBEGIN_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnPauseAutomaticUpdatesBegin();
}

static HRESULT BalBaseBAFunctionsProcOnPauseAutomaticUpdatesComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_ARGS* pArgs,
    __inout BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnPauseAutomaticUpdatesComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnSystemRestorePointBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONSYSTEMRESTOREPOINTBEGIN_ARGS* /*pArgs*/,
    __inout BA_ONSYSTEMRESTOREPOINTBEGIN_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnSystemRestorePointBegin();
}

static HRESULT BalBaseBAFunctionsProcOnSystemRestorePointComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONSYSTEMRESTOREPOINTCOMPLETE_ARGS* pArgs,
    __inout BA_ONSYSTEMRESTOREPOINTCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnSystemRestorePointComplete(pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnPlanForwardCompatibleBundle(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANFORWARDCOMPATIBLEBUNDLE_ARGS* pArgs,
    __inout BA_ONPLANFORWARDCOMPATIBLEBUNDLE_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanForwardCompatibleBundle(pArgs->wzBundleCode, pArgs->relationType, pArgs->wzBundleTag, pArgs->fPerMachine, pArgs->wzVersion, pArgs->fRecommendedIgnoreBundle, &pResults->fCancel, &pResults->fIgnoreBundle);
}

static HRESULT BalBaseBAFunctionsProcOnCacheContainerOrPayloadVerifyBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_ARGS* pArgs,
    __inout BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheContainerOrPayloadVerifyBegin(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCacheContainerOrPayloadVerifyProgress(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_ARGS* pArgs,
    __inout BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_RESULTS* pResults
    )
{
    return pBAFunctions->OnCacheContainerOrPayloadVerifyProgress(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->dw64Progress, pArgs->dw64Total, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCacheContainerOrPayloadVerifyComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnCacheContainerOrPayloadVerifyComplete(pArgs->wzPackageOrContainerId, pArgs->wzPayloadId, pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnCachePayloadExtractBegin(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEPAYLOADEXTRACTBEGIN_ARGS* pArgs,
    __inout BA_ONCACHEPAYLOADEXTRACTBEGIN_RESULTS* pResults
    )
{
    return pBAFunctions->OnCachePayloadExtractBegin(pArgs->wzContainerId, pArgs->wzPayloadId, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCachePayloadExtractProgress(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEPAYLOADEXTRACTPROGRESS_ARGS* pArgs,
    __inout BA_ONCACHEPAYLOADEXTRACTPROGRESS_RESULTS* pResults
    )
{
    return pBAFunctions->OnCachePayloadExtractProgress(pArgs->wzContainerId, pArgs->wzPayloadId, pArgs->dw64Progress, pArgs->dw64Total, pArgs->dwOverallPercentage, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCachePayloadExtractComplete(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEPAYLOADEXTRACTCOMPLETE_ARGS* pArgs,
    __inout BA_ONCACHEPAYLOADEXTRACTCOMPLETE_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnCachePayloadExtractComplete(pArgs->wzContainerId, pArgs->wzPayloadId, pArgs->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnPlanRestoreRelatedBundle(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANRESTORERELATEDBUNDLE_ARGS* pArgs,
    __inout BA_ONPLANRESTORERELATEDBUNDLE_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanRestoreRelatedBundle(pArgs->wzBundleCode, pArgs->recommendedState, &pResults->requestedState, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnPlanRelatedBundleType(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONPLANRELATEDBUNDLETYPE_ARGS* pArgs,
    __inout BA_ONPLANRELATEDBUNDLETYPE_RESULTS* pResults
    )
{
    return pBAFunctions->OnPlanRelatedBundleType(pArgs->wzBundleCode, pArgs->recommendedType, &pResults->requestedType, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnApplyDowngrade(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONAPPLYDOWNGRADE_ARGS* pArgs,
    __inout BA_ONAPPLYDOWNGRADE_RESULTS* pResults
    )
{
    return pBAFunctions->OnApplyDowngrade(pArgs->hrRecommended, &pResults->hrStatus);
}

static HRESULT BalBaseBAFunctionsProcOnDetectRelatedBundlePackage(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONDETECTRELATEDBUNDLEPACKAGE_ARGS* pArgs,
    __inout BA_ONDETECTRELATEDBUNDLEPACKAGE_RESULTS* pResults
    )
{
    return pBAFunctions->OnDetectRelatedBundlePackage(pArgs->wzPackageId, pArgs->wzBundleCode, pArgs->relationType, pArgs->fPerMachine, pArgs->wzVersion, &pResults->fCancel);
}

static HRESULT BalBaseBAFunctionsProcOnCachePackageNonVitalValidationFailure(
    __in IBAFunctions* pBAFunctions,
    __in BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_ARGS* pArgs,
    __inout BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_RESULTS* pResults
    )
{
    return pBAFunctions->OnCachePackageNonVitalValidationFailure(pArgs->wzPackageId, pArgs->hrStatus, pArgs->recommendation, &pResults->action);
}

static HRESULT BalBaseBAFunctionsProcOnThemeLoaded(
    __in IBAFunctions* pBAFunctions,
    __in BA_FUNCTIONS_ONTHEMELOADED_ARGS* pArgs,
    __inout BA_FUNCTIONS_ONTHEMELOADED_RESULTS* /*pResults*/
    )
{
    return pBAFunctions->OnThemeLoaded(pArgs->hWnd);
}

static HRESULT BalBaseBAFunctionsProcWndProc(
    __in IBAFunctions* pBAFunctions,
    __in BA_FUNCTIONS_WNDPROC_ARGS* pArgs,
    __inout BA_FUNCTIONS_WNDPROC_RESULTS* pResults
    )
{
    return pBAFunctions->WndProc(pArgs->hWnd, pArgs->uMsg, pArgs->wParam, pArgs->lParam, &pResults->fProcessed, &pResults->lResult);
}

static HRESULT BalBaseBAFunctionsProcOnThemeControlLoading(
    __in IBAFunctions* pBAFunctions,
    __in BA_FUNCTIONS_ONTHEMECONTROLLOADING_ARGS* pArgs,
    __inout BA_FUNCTIONS_ONTHEMECONTROLLOADING_RESULTS* pResults
    )
{
    return pBAFunctions->OnThemeControlLoading(pArgs->wzName, &pResults->fProcessed, &pResults->wId, &pResults->dwAutomaticBehaviorType);
}

static HRESULT BalBaseBAFunctionsProcOnThemeControlWmCommand(
    __in IBAFunctions* pBAFunctions,
    __in BA_FUNCTIONS_ONTHEMECONTROLWMCOMMAND_ARGS* pArgs,
    __inout BA_FUNCTIONS_ONTHEMECONTROLWMCOMMAND_RESULTS* pResults
    )
{
    return pBAFunctions->OnThemeControlWmCommand(pArgs->wParam, pArgs->wzName, pArgs->wId, pArgs->hWnd, &pResults->fProcessed, &pResults->lResult);
}

static HRESULT BalBaseBAFunctionsProcOnThemeControlWmNotify(
    __in IBAFunctions* pBAFunctions,
    __in BA_FUNCTIONS_ONTHEMECONTROLWMNOTIFY_ARGS* pArgs,
    __inout BA_FUNCTIONS_ONTHEMECONTROLWMNOTIFY_RESULTS* pResults
    )
{
    return pBAFunctions->OnThemeControlWmNotify(pArgs->lParam, pArgs->wzName, pArgs->wId, pArgs->hWnd, &pResults->fProcessed, &pResults->lResult);
}

static HRESULT BalBaseBAFunctionsProcOnThemeControlLoaded(
    __in IBAFunctions* pBAFunctions,
    __in BA_FUNCTIONS_ONTHEMECONTROLLOADED_ARGS* pArgs,
    __inout BA_FUNCTIONS_ONTHEMECONTROLLOADED_RESULTS* pResults
    )
{
    return pBAFunctions->OnThemeControlLoaded(pArgs->wzName, pArgs->wId, pArgs->hWnd, &pResults->fProcessed);
}

/*******************************************************************
BalBaseBAFunctionsProc - requires pvContext to be of type IBAFunctions.
Provides a default mapping between the message based BAFunctions interface and
the COM-based BAFunctions interface.

*******************************************************************/
HRESULT WINAPI BalBaseBAFunctionsProc(
    __in BA_FUNCTIONS_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    )
{
    IBAFunctions* pBAFunctions = reinterpret_cast<IBAFunctions*>(pvContext);
    HRESULT hr = pBAFunctions->BAFunctionsProc(message, pvArgs, pvResults, pvContext);

    if (E_NOTIMPL == hr)
    {
        switch (message)
        {
        case BA_FUNCTIONS_MESSAGE_ONCREATE:
            // ONCREATE is handled when the function is created, not via callback.
            break;
        case BA_FUNCTIONS_MESSAGE_ONDESTROY:
            hr = BalBaseBAFunctionsProcOnDestroy(pBAFunctions, reinterpret_cast<BA_ONDESTROY_ARGS*>(pvArgs), reinterpret_cast<BA_ONDESTROY_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONSTARTUP:
            hr = BalBaseBAFunctionsProcOnStartup(pBAFunctions, reinterpret_cast<BA_ONSTARTUP_ARGS*>(pvArgs), reinterpret_cast<BA_ONSTARTUP_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONSHUTDOWN:
            hr = BalBaseBAFunctionsProcOnShutdown(pBAFunctions, reinterpret_cast<BA_ONSHUTDOWN_ARGS*>(pvArgs), reinterpret_cast<BA_ONSHUTDOWN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTBEGIN:
            hr = BalBaseBAFunctionsProcOnDetectBegin(pBAFunctions, reinterpret_cast<BA_ONDETECTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTCOMPLETE:
            hr = BalBaseBAFunctionsProcOnDetectComplete(pBAFunctions, reinterpret_cast<BA_ONDETECTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANBEGIN:
            hr = BalBaseBAFunctionsProcOnPlanBegin(pBAFunctions, reinterpret_cast<BA_ONPLANBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANCOMPLETE:
            hr = BalBaseBAFunctionsProcOnPlanComplete(pBAFunctions, reinterpret_cast<BA_ONPLANCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTFORWARDCOMPATIBLEBUNDLE:
            hr = BalBaseBAFunctionsProcOnDetectForwardCompatibleBundle(pBAFunctions, reinterpret_cast<BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTFORWARDCOMPATIBLEBUNDLE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTUPDATEBEGIN:
            hr = BalBaseBAFunctionsProcOnDetectUpdateBegin(pBAFunctions, reinterpret_cast<BA_ONDETECTUPDATEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTUPDATE:
            hr = BalBaseBAFunctionsProcOnDetectUpdate(pBAFunctions, reinterpret_cast<BA_ONDETECTUPDATE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTUPDATECOMPLETE:
            hr = BalBaseBAFunctionsProcOnDetectUpdateComplete(pBAFunctions, reinterpret_cast<BA_ONDETECTUPDATECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTUPDATECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTRELATEDBUNDLE:
            hr = BalBaseBAFunctionsProcOnDetectRelatedBundle(pBAFunctions, reinterpret_cast<BA_ONDETECTRELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTPACKAGEBEGIN:
            hr = BalBaseBAFunctionsProcOnDetectPackageBegin(pBAFunctions, reinterpret_cast<BA_ONDETECTPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTRELATEDMSIPACKAGE:
            hr = BalBaseBAFunctionsProcOnDetectRelatedMsiPackage(pBAFunctions, reinterpret_cast<BA_ONDETECTRELATEDMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTPATCHTARGET:
            hr = BalBaseBAFunctionsProcOnDetectPatchTarget(pBAFunctions, reinterpret_cast<BA_ONDETECTPATCHTARGET_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPATCHTARGET_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTMSIFEATURE:
            hr = BalBaseBAFunctionsProcOnDetectMsiFeature(pBAFunctions, reinterpret_cast<BA_ONDETECTMSIFEATURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTMSIFEATURE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTPACKAGECOMPLETE:
            hr = BalBaseBAFunctionsProcOnDetectPackageComplete(pBAFunctions, reinterpret_cast<BA_ONDETECTPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANRELATEDBUNDLE:
            hr = BalBaseBAFunctionsProcOnPlanRelatedBundle(pBAFunctions, reinterpret_cast<BA_ONPLANRELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANRELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANPACKAGEBEGIN:
            hr = BalBaseBAFunctionsProcOnPlanPackageBegin(pBAFunctions, reinterpret_cast<BA_ONPLANPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANPATCHTARGET:
            hr = BalBaseBAFunctionsProcOnPlanPatchTarget(pBAFunctions, reinterpret_cast<BA_ONPLANPATCHTARGET_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPATCHTARGET_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANMSIFEATURE:
            hr = BalBaseBAFunctionsProcOnPlanMsiFeature(pBAFunctions, reinterpret_cast<BA_ONPLANMSIFEATURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANMSIFEATURE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANPACKAGECOMPLETE:
            hr = BalBaseBAFunctionsProcOnPlanPackageComplete(pBAFunctions, reinterpret_cast<BA_ONPLANPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONAPPLYBEGIN:
            hr = BalBaseBAFunctionsProcOnApplyBegin(pBAFunctions, reinterpret_cast<BA_ONAPPLYBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONAPPLYBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONELEVATEBEGIN:
            hr = BalBaseBAFunctionsProcOnElevateBegin(pBAFunctions, reinterpret_cast<BA_ONELEVATEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONELEVATEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONELEVATECOMPLETE:
            hr = BalBaseBAFunctionsProcOnElevateComplete(pBAFunctions, reinterpret_cast<BA_ONELEVATECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONELEVATECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPROGRESS:
            hr = BalBaseBAFunctionsProcOnProgress(pBAFunctions, reinterpret_cast<BA_ONPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONPROGRESS_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONERROR:
            hr = BalBaseBAFunctionsProcOnError(pBAFunctions, reinterpret_cast<BA_ONERROR_ARGS*>(pvArgs), reinterpret_cast<BA_ONERROR_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONREGISTERBEGIN:
            hr = BalBaseBAFunctionsProcOnRegisterBegin(pBAFunctions, reinterpret_cast<BA_ONREGISTERBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONREGISTERBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONREGISTERCOMPLETE:
            hr = BalBaseBAFunctionsProcOnRegisterComplete(pBAFunctions, reinterpret_cast<BA_ONREGISTERCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONREGISTERCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEBEGIN:
            hr = BalBaseBAFunctionsProcOnCacheBegin(pBAFunctions, reinterpret_cast<BA_ONCACHEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEPACKAGEBEGIN:
            hr = BalBaseBAFunctionsProcOnCachePackageBegin(pBAFunctions, reinterpret_cast<BA_ONCACHEPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEACQUIREBEGIN:
            hr = BalBaseBAFunctionsProcOnCacheAcquireBegin(pBAFunctions, reinterpret_cast<BA_ONCACHEACQUIREBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIREBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEACQUIREPROGRESS:
            hr = BalBaseBAFunctionsProcOnCacheAcquireProgress(pBAFunctions, reinterpret_cast<BA_ONCACHEACQUIREPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIREPROGRESS_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEACQUIRERESOLVING:
            hr = BalBaseBAFunctionsProcOnCacheAcquireResolving(pBAFunctions, reinterpret_cast<BA_ONCACHEACQUIRERESOLVING_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIRERESOLVING_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEACQUIRECOMPLETE:
            hr = BalBaseBAFunctionsProcOnCacheAcquireComplete(pBAFunctions, reinterpret_cast<BA_ONCACHEACQUIRECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEACQUIRECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEVERIFYBEGIN:
            hr = BalBaseBAFunctionsProcOnCacheVerifyBegin(pBAFunctions, reinterpret_cast<BA_ONCACHEVERIFYBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEVERIFYBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEVERIFYPROGRESS:
            hr = BalBaseBAFunctionsProcOnCacheVerifyProgress(pBAFunctions, reinterpret_cast<BA_ONCACHEVERIFYPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEVERIFYPROGRESS_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEVERIFYCOMPLETE:
            hr = BalBaseBAFunctionsProcOnCacheVerifyComplete(pBAFunctions, reinterpret_cast<BA_ONCACHEVERIFYCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEVERIFYCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEPACKAGECOMPLETE:
            hr = BalBaseBAFunctionsProcOnCachePackageComplete(pBAFunctions, reinterpret_cast<BA_ONCACHEPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHECOMPLETE:
            hr = BalBaseBAFunctionsProcOnCacheComplete(pBAFunctions, reinterpret_cast<BA_ONCACHECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONEXECUTEBEGIN:
            hr = BalBaseBAFunctionsProcOnExecuteBegin(pBAFunctions, reinterpret_cast<BA_ONEXECUTEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONEXECUTEPACKAGEBEGIN:
            hr = BalBaseBAFunctionsProcOnExecutePackageBegin(pBAFunctions, reinterpret_cast<BA_ONEXECUTEPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONEXECUTEPATCHTARGET:
            hr = BalBaseBAFunctionsProcOnExecutePatchTarget(pBAFunctions, reinterpret_cast<BA_ONEXECUTEPATCHTARGET_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPATCHTARGET_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONEXECUTEPROGRESS:
            hr = BalBaseBAFunctionsProcOnExecuteProgress(pBAFunctions, reinterpret_cast<BA_ONEXECUTEPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPROGRESS_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONEXECUTEMSIMESSAGE:
            hr = BalBaseBAFunctionsProcOnExecuteMsiMessage(pBAFunctions, reinterpret_cast<BA_ONEXECUTEMSIMESSAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEMSIMESSAGE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONEXECUTEFILESINUSE:
            hr = BalBaseBAFunctionsProcOnExecuteFilesInUse(pBAFunctions, reinterpret_cast<BA_ONEXECUTEFILESINUSE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEFILESINUSE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONEXECUTEPACKAGECOMPLETE:
            hr = BalBaseBAFunctionsProcOnExecutePackageComplete(pBAFunctions, reinterpret_cast<BA_ONEXECUTEPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONEXECUTECOMPLETE:
            hr = BalBaseBAFunctionsProcOnExecuteComplete(pBAFunctions, reinterpret_cast<BA_ONEXECUTECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONUNREGISTERBEGIN:
            hr = BalBaseBAFunctionsProcOnUnregisterBegin(pBAFunctions, reinterpret_cast<BA_ONUNREGISTERBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONUNREGISTERBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONUNREGISTERCOMPLETE:
            hr = BalBaseBAFunctionsProcOnUnregisterComplete(pBAFunctions, reinterpret_cast<BA_ONUNREGISTERCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONUNREGISTERCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONAPPLYCOMPLETE:
            hr = BalBaseBAFunctionsProcOnApplyComplete(pBAFunctions, reinterpret_cast<BA_ONAPPLYCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONAPPLYCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONLAUNCHAPPROVEDEXEBEGIN:
            hr = BalBaseBAFunctionsProcOnLaunchApprovedExeBegin(pBAFunctions, reinterpret_cast<BA_ONLAUNCHAPPROVEDEXEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONLAUNCHAPPROVEDEXEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONLAUNCHAPPROVEDEXECOMPLETE:
            hr = BalBaseBAFunctionsProcOnLaunchApprovedExeComplete(pBAFunctions, reinterpret_cast<BA_ONLAUNCHAPPROVEDEXECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONLAUNCHAPPROVEDEXECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANMSIPACKAGE:
            hr = BalBaseBAFunctionsProcOnPlanMsiPackage(pBAFunctions, reinterpret_cast<BA_ONPLANMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONBEGINMSITRANSACTIONBEGIN:
            hr = BalBaseBAFunctionsProcOnBeginMsiTransactionBegin(pBAFunctions, reinterpret_cast<BA_ONBEGINMSITRANSACTIONBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONBEGINMSITRANSACTIONBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONBEGINMSITRANSACTIONCOMPLETE:
            hr = BalBaseBAFunctionsProcOnBeginMsiTransactionComplete(pBAFunctions, reinterpret_cast<BA_ONBEGINMSITRANSACTIONCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONBEGINMSITRANSACTIONCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCOMMITMSITRANSACTIONBEGIN:
            hr = BalBaseBAFunctionsProcOnCommitMsiTransactionBegin(pBAFunctions, reinterpret_cast<BA_ONCOMMITMSITRANSACTIONBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCOMMITMSITRANSACTIONBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCOMMITMSITRANSACTIONCOMPLETE:
            hr = BalBaseBAFunctionsProcOnCommitMsiTransactionComplete(pBAFunctions, reinterpret_cast<BA_ONCOMMITMSITRANSACTIONCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCOMMITMSITRANSACTIONCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONROLLBACKMSITRANSACTIONBEGIN:
            hr = BalBaseBAFunctionsProcOnRollbackMsiTransactionBegin(pBAFunctions, reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONROLLBACKMSITRANSACTIONCOMPLETE:
            hr = BalBaseBAFunctionsProcOnRollbackMsiTransactionComplete(pBAFunctions, reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONROLLBACKMSITRANSACTIONCOMPLETE_RESULTS*>(pvResults));
        case BA_FUNCTIONS_MESSAGE_ONPAUSEAUTOMATICUPDATESBEGIN:
            hr = BalBaseBAFunctionsProcOnPauseAutomaticUpdatesBegin(pBAFunctions, reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPAUSEAUTOMATICUPDATESCOMPLETE:
            hr = BalBaseBAFunctionsProcOnPauseAutomaticUpdatesComplete(pBAFunctions, reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPAUSEAUTOMATICUPDATESCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONSYSTEMRESTOREPOINTBEGIN:
            hr = BalBaseBAFunctionsProcOnSystemRestorePointBegin(pBAFunctions, reinterpret_cast<BA_ONSYSTEMRESTOREPOINTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONSYSTEMRESTOREPOINTBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONSYSTEMRESTOREPOINTCOMPLETE:
            hr = BalBaseBAFunctionsProcOnSystemRestorePointComplete(pBAFunctions, reinterpret_cast<BA_ONSYSTEMRESTOREPOINTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONSYSTEMRESTOREPOINTCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANNEDPACKAGE:
            hr = BalBaseBAFunctionsProcOnPlannedPackage(pBAFunctions, reinterpret_cast<BA_ONPLANNEDPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANNEDPACKAGE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANFORWARDCOMPATIBLEBUNDLE:
            hr = BalBaseBAFunctionsProcOnPlanForwardCompatibleBundle(pBAFunctions, reinterpret_cast<BA_ONPLANFORWARDCOMPATIBLEBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANFORWARDCOMPATIBLEBUNDLE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYBEGIN:
            hr = BalBaseBAFunctionsProcOnCacheContainerOrPayloadVerifyBegin(pBAFunctions, reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS:
            hr = BalBaseBAFunctionsProcOnCacheContainerOrPayloadVerifyProgress(pBAFunctions, reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYPROGRESS_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE:
            hr = BalBaseBAFunctionsProcOnCacheContainerOrPayloadVerifyComplete(pBAFunctions, reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHECONTAINERORPAYLOADVERIFYCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEPAYLOADEXTRACTBEGIN:
            hr = BalBaseBAFunctionsProcOnCachePayloadExtractBegin(pBAFunctions, reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEPAYLOADEXTRACTPROGRESS:
            hr = BalBaseBAFunctionsProcOnCachePayloadExtractProgress(pBAFunctions, reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTPROGRESS_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTPROGRESS_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEPAYLOADEXTRACTCOMPLETE:
            hr = BalBaseBAFunctionsProcOnCachePayloadExtractComplete(pBAFunctions, reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTCOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPAYLOADEXTRACTCOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANROLLBACKBOUNDARY:
            hr = BalBaseBAFunctionsProcOnPlanRollbackBoundary(pBAFunctions, reinterpret_cast<BA_ONPLANROLLBACKBOUNDARY_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANROLLBACKBOUNDARY_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTCOMPATIBLEMSIPACKAGE:
            hr = BalBaseBAFunctionsProcOnDetectCompatiblePackage(pBAFunctions, reinterpret_cast<BA_ONDETECTCOMPATIBLEMSIPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTCOMPATIBLEMSIPACKAGE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGEBEGIN:
            hr = BalBaseBAFunctionsProcOnPlanCompatibleMsiPackageBegin(pBAFunctions, reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGEBEGIN_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE:
            hr = BalBaseBAFunctionsProcOnPlanCompatibleMsiPackageComplete(pBAFunctions, reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANCOMPATIBLEMSIPACKAGECOMPLETE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANNEDCOMPATIBLEPACKAGE:
            hr = BalBaseBAFunctionsProcOnPlannedCompatiblePackage(pBAFunctions, reinterpret_cast<BA_ONPLANNEDCOMPATIBLEPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANNEDCOMPATIBLEPACKAGE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANRESTORERELATEDBUNDLE:
            hr = BalBaseBAFunctionsProcOnPlanRestoreRelatedBundle(pBAFunctions, reinterpret_cast<BA_ONPLANRESTORERELATEDBUNDLE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANRESTORERELATEDBUNDLE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONPLANRELATEDBUNDLETYPE:
            hr = BalBaseBAFunctionsProcOnPlanRelatedBundleType(pBAFunctions, reinterpret_cast<BA_ONPLANRELATEDBUNDLETYPE_ARGS*>(pvArgs), reinterpret_cast<BA_ONPLANRELATEDBUNDLETYPE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONAPPLYDOWNGRADE:
            hr = BalBaseBAFunctionsProcOnApplyDowngrade(pBAFunctions, reinterpret_cast<BA_ONAPPLYDOWNGRADE_ARGS*>(pvArgs), reinterpret_cast<BA_ONAPPLYDOWNGRADE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONEXECUTEPROCESSCANCEL:
            hr = BalBaseBAFunctionsProcOnExecuteProcessCancel(pBAFunctions, reinterpret_cast<BA_ONEXECUTEPROCESSCANCEL_ARGS*>(pvArgs), reinterpret_cast<BA_ONEXECUTEPROCESSCANCEL_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONDETECTRELATEDBUNDLEPACKAGE:
            hr = BalBaseBAFunctionsProcOnDetectRelatedBundlePackage(pBAFunctions, reinterpret_cast<BA_ONDETECTRELATEDBUNDLEPACKAGE_ARGS*>(pvArgs), reinterpret_cast<BA_ONDETECTRELATEDBUNDLEPACKAGE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONCACHEPACKAGENONVITALVALIDATIONFAILURE:
            hr = BalBaseBAFunctionsProcOnCachePackageNonVitalValidationFailure(pBAFunctions, reinterpret_cast<BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_ARGS*>(pvArgs), reinterpret_cast<BA_ONCACHEPACKAGENONVITALVALIDATIONFAILURE_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONTHEMELOADED:
            hr = BalBaseBAFunctionsProcOnThemeLoaded(pBAFunctions, reinterpret_cast<BA_FUNCTIONS_ONTHEMELOADED_ARGS*>(pvArgs), reinterpret_cast<BA_FUNCTIONS_ONTHEMELOADED_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_WNDPROC:
            hr = BalBaseBAFunctionsProcWndProc(pBAFunctions, reinterpret_cast<BA_FUNCTIONS_WNDPROC_ARGS*>(pvArgs), reinterpret_cast<BA_FUNCTIONS_WNDPROC_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONTHEMECONTROLLOADING:
            hr = BalBaseBAFunctionsProcOnThemeControlLoading(pBAFunctions, reinterpret_cast<BA_FUNCTIONS_ONTHEMECONTROLLOADING_ARGS*>(pvArgs), reinterpret_cast<BA_FUNCTIONS_ONTHEMECONTROLLOADING_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONTHEMECONTROLWMCOMMAND:
            hr = BalBaseBAFunctionsProcOnThemeControlWmCommand(pBAFunctions, reinterpret_cast<BA_FUNCTIONS_ONTHEMECONTROLWMCOMMAND_ARGS*>(pvArgs), reinterpret_cast<BA_FUNCTIONS_ONTHEMECONTROLWMCOMMAND_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONTHEMECONTROLWMNOTIFY:
            hr = BalBaseBAFunctionsProcOnThemeControlWmNotify(pBAFunctions, reinterpret_cast<BA_FUNCTIONS_ONTHEMECONTROLWMNOTIFY_ARGS*>(pvArgs), reinterpret_cast<BA_FUNCTIONS_ONTHEMECONTROLWMNOTIFY_RESULTS*>(pvResults));
            break;
        case BA_FUNCTIONS_MESSAGE_ONTHEMECONTROLLOADED:
            hr = BalBaseBAFunctionsProcOnThemeControlLoaded(pBAFunctions, reinterpret_cast<BA_FUNCTIONS_ONTHEMECONTROLLOADED_ARGS*>(pvArgs), reinterpret_cast<BA_FUNCTIONS_ONTHEMECONTROLLOADED_RESULTS*>(pvResults));
            break;
        }
    }

    return hr;
}
