#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include <windows.h>
#include <msiquery.h>

#include <IBAFunctions.h>

class CBalBaseBAFunctions : public IBAFunctions
{
public: // IUnknown
    virtual STDMETHODIMP QueryInterface(
        __in REFIID riid,
        __out LPVOID *ppvObject
        )
    {
        if (!ppvObject)
        {
            return E_INVALIDARG;
        }

        *ppvObject = NULL;

        if (::IsEqualIID(__uuidof(IBAFunctions), riid))
        {
            *ppvObject = static_cast<IBAFunctions*>(this);
        }
        else if (::IsEqualIID(__uuidof(IBootstrapperApplication), riid))
        {
            *ppvObject = static_cast<IBootstrapperApplication*>(this);
        }
        else if (::IsEqualIID(IID_IUnknown, riid))
        {
            *ppvObject = static_cast<IUnknown*>(this);
        }
        else // no interface for requested iid
        {
            return E_NOINTERFACE;
        }

        AddRef();
        return S_OK;
    }

    virtual STDMETHODIMP_(ULONG) AddRef()
    {
        return ::InterlockedIncrement(&this->m_cReferences);
    }

    virtual STDMETHODIMP_(ULONG) Release()
    {
        long l = ::InterlockedDecrement(&this->m_cReferences);
        if (0 < l)
        {
            return l;
        }

        delete this;
        return 0;
    }

public: // IBootstrapperApplication
    virtual STDMETHODIMP_(HRESULT) BAProc(
        __in BOOTSTRAPPER_APPLICATION_MESSAGE /*message*/,
        __in const LPVOID /*pvArgs*/,
        __inout LPVOID /*pvResults*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP_(void) BAProcFallback(
        __in BOOTSTRAPPER_APPLICATION_MESSAGE /*message*/,
        __in const LPVOID /*pvArgs*/,
        __inout LPVOID /*pvResults*/,
        __inout HRESULT* /*phr*/
        )
    {
    }

    virtual STDMETHODIMP OnCreate(
        __in IBootstrapperEngine* pEngine,
        __in BOOTSTRAPPER_COMMAND* /*pCommand*/
        )
    {
        HRESULT hr = S_OK;

        pEngine->AddRef();
        m_pEngine = pEngine;

        return hr;
    }

    virtual STDMETHODIMP OnDestroy(
        __in BOOL /*fReload*/
    )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnStartup()
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnShutdown(
        __inout BOOTSTRAPPER_SHUTDOWN_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectBegin(
        __in BOOL /*fCached*/,
        __in BOOTSTRAPPER_REGISTRATION_TYPE /*registrationType*/,
        __in DWORD /*cPackages*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectForwardCompatibleBundle(
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_RELATION_TYPE /*relationType*/,
        __in_z LPCWSTR /*wzBundleTag*/,
        __in BOOL /*fPerMachine*/,
        __in LPCWSTR /*wzVersion*/,
        __in BOOL /*fMissingFromCache*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectUpdateBegin(
        __in_z LPCWSTR /*wzUpdateLocation*/,
        __inout BOOL* /*pfCancel*/,
        __inout BOOL* /*pfSkip*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectUpdate(
        __in_z LPCWSTR /*wzUpdateLocation*/,
        __in DWORD64 /*dw64Size*/,
        __in_z_opt LPCWSTR /*wzHash*/,
        __in BOOTSTRAPPER_UPDATE_HASH_TYPE /*hashAlgorithm*/,
        __in LPCWSTR /*wzVersion*/,
        __in_z LPCWSTR /*wzTitle*/,
        __in_z LPCWSTR /*wzSummary*/,
        __in_z LPCWSTR /*wzContentType*/,
        __in_z LPCWSTR /*wzContent*/,
        __inout BOOL* /*pfCancel*/,
        __inout BOOL* /*pfStopProcessingUpdates*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectUpdateComplete(
        __in HRESULT /*hrStatus*/,
        __inout BOOL* /*pfIgnoreError*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectRelatedBundle(
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_RELATION_TYPE /*relationType*/,
        __in_z LPCWSTR /*wzBundleTag*/,
        __in BOOL /*fPerMachine*/,
        __in LPCWSTR /*wzVersion*/,
        __in BOOL /*fMissingFromCache*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectPackageBegin(
        __in_z LPCWSTR /*wzPackageId*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectCompatibleMsiPackage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzCompatiblePackageId*/,
        __in LPCWSTR /*wzCompatiblePackageVersion*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectRelatedMsiPackage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzUpgradeCode*/,
        __in_z LPCWSTR /*wzProductCode*/,
        __in BOOL /*fPerMachine*/,
        __in LPCWSTR /*wzVersion*/,
        __in BOOTSTRAPPER_RELATED_OPERATION /*operation*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectPatchTarget(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzProductCode*/,
        __in BOOTSTRAPPER_PACKAGE_STATE /*patchState*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectMsiFeature(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzFeatureId*/,
        __in BOOTSTRAPPER_FEATURE_STATE /*state*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectPackageComplete(
        __in_z LPCWSTR /*wzPackageId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_PACKAGE_STATE /*state*/,
        __in BOOL /*fCached*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectComplete(
        __in HRESULT /*hrStatus*/,
        __in BOOL /*fEligibleForCleanup*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanBegin(
        __in DWORD /*cPackages*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanRelatedBundle(
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_REQUEST_STATE /*recommendedState*/,
        __inout BOOTSTRAPPER_REQUEST_STATE* /*pRequestedState*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanRollbackBoundary(
        __in_z LPCWSTR /*wzRollbackBoundaryId*/,
        __in BOOL /*fRecommendedTransaction*/,
        __inout BOOL* /*pfTransaction*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanPackageBegin(
        __in_z LPCWSTR /*wzPackageId*/,
        __in BOOTSTRAPPER_PACKAGE_STATE /*state*/,
        __in BOOL /*fCached*/,
        __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT /*installCondition*/,
        __in BOOTSTRAPPER_PACKAGE_CONDITION_RESULT /*repairCondition*/,
        __in BOOTSTRAPPER_REQUEST_STATE /*recommendedState*/,
        __in BOOTSTRAPPER_CACHE_TYPE /*recommendedCacheType*/,
        __inout BOOTSTRAPPER_REQUEST_STATE* /*pRequestState*/,
        __inout BOOTSTRAPPER_CACHE_TYPE* /*pRequestedCacheType*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanCompatibleMsiPackageBegin(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzCompatiblePackageId*/,
        __in LPCWSTR /*wzCompatiblePackageVersion*/,
        __in BOOL /*fRecommendedRemove*/,
        __inout BOOL* /*pfRequestRemove*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanCompatibleMsiPackageComplete(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzCompatiblePackageId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOL /*fRequestedRemove*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanPatchTarget(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzProductCode*/,
        __in BOOTSTRAPPER_REQUEST_STATE /*recommendedState*/,
        __inout BOOTSTRAPPER_REQUEST_STATE* /*pRequestedState*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanMsiFeature(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzFeatureId*/,
        __in BOOTSTRAPPER_FEATURE_STATE /*recommendedState*/,
        __inout BOOTSTRAPPER_FEATURE_STATE* /*pRequestedState*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanMsiPackage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in BOOL /*fExecute*/,
        __in BOOTSTRAPPER_ACTION_STATE /*action*/,
        __in BOOTSTRAPPER_MSI_FILE_VERSIONING /*recommendedFileVersioning*/,
        __inout BOOL* /*pfCancel*/,
        __inout BURN_MSI_PROPERTY* /*pActionMsiProperty*/,
        __inout INSTALLUILEVEL* /*pUiLevel*/,
        __inout BOOL* /*pfDisableExternalUiHandler*/,
        __inout BOOTSTRAPPER_MSI_FILE_VERSIONING* /*pFileVersioning*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanPackageComplete(
        __in_z LPCWSTR /*wzPackageId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_REQUEST_STATE /*requested*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlannedCompatiblePackage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzCompatiblePackageId*/,
        __in BOOL /*fRemove*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlannedPackage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in BOOTSTRAPPER_ACTION_STATE /*execute*/,
        __in BOOTSTRAPPER_ACTION_STATE /*rollback*/,
        __in BOOL /*fPlannedCache*/,
        __in BOOL /*fPlannedUncache*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnApplyBegin(
        __in DWORD /*dwPhaseCount*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnElevateBegin(
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnElevateComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnProgress(
        __in DWORD /*dwProgressPercentage*/,
        __in DWORD /*dwOverallProgressPercentage*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return IDNOACTION;
    }

    virtual STDMETHODIMP OnError(
        __in BOOTSTRAPPER_ERROR_TYPE /*errorType*/,
        __in_z LPCWSTR /*wzPackageId*/,
        __in DWORD /*dwCode*/,
        __in_z LPCWSTR /*wzError*/,
        __in DWORD /*dwUIHint*/,
        __in DWORD /*cData*/,
        __in_ecount_z_opt(cData) LPCWSTR* /*rgwzData*/,
        __in int /*nRecommendation*/,
        __inout int* /*pResult*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnRegisterBegin(
        __in BOOTSTRAPPER_REGISTRATION_TYPE /*recommendedRegistrationType*/,
        __inout BOOL* /*pfCancel*/,
        __inout BOOTSTRAPPER_REGISTRATION_TYPE* /*pRegistrationType*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnRegisterComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheBegin(
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCachePackageBegin(
        __in_z LPCWSTR /*wzPackageId*/,
        __in DWORD /*cCachePayloads*/,
        __in DWORD64 /*dw64PackageCacheSize*/,
        __in BOOL /*fVital*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheAcquireBegin(
        __in_z LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in_z LPCWSTR /*wzSource*/,
        __in_z_opt LPCWSTR /*wzDownloadUrl*/,
        __in_z_opt LPCWSTR /*wzPayloadContainerId*/,
        __in BOOTSTRAPPER_CACHE_OPERATION /*recommendation*/,
        __inout BOOTSTRAPPER_CACHE_OPERATION* /*pAction*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheAcquireProgress(
        __in_z LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in DWORD64 /*dw64Progress*/,
        __in DWORD64 /*dw64Total*/,
        __in DWORD /*dwOverallPercentage*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheAcquireResolving(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in_z LPCWSTR* /*rgSearchPaths*/,
        __in DWORD /*cSearchPaths*/,
        __in BOOL /*fFoundLocal*/,
        __in DWORD /*dwRecommendedSearchPath*/,
        __in_z_opt LPCWSTR /*wzDownloadUrl*/,
        __in_z_opt LPCWSTR /*wzPayloadContainerId*/,
        __in BOOTSTRAPPER_CACHE_RESOLVE_OPERATION /*recommendation*/,
        __inout DWORD* /*pdwChosenSearchPath*/,
        __inout BOOTSTRAPPER_CACHE_RESOLVE_OPERATION* /*pAction*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheAcquireComplete(
        __in_z LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheVerifyBegin(
        __in_z LPCWSTR /*wzPackageOrContainerId*/,
        __in_z LPCWSTR /*wzPayloadId*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheVerifyProgress(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in DWORD64 /*dw64Progress*/,
        __in DWORD64 /*dw64Total*/,
        __in DWORD /*dwOverallPercentage*/,
        __in BOOTSTRAPPER_CACHE_VERIFY_STEP /*verifyStep*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheVerifyComplete(
        __in_z LPCWSTR /*wzPackageOrContainerId*/,
        __in_z LPCWSTR /*wzPayloadId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCachePackageComplete(
        __in_z LPCWSTR /*wzPackageId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnExecuteBegin(
        __in DWORD /*cExecutingPackages*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnExecutePackageBegin(
        __in_z LPCWSTR /*wzPackageId*/,
        __in BOOL /*fExecute*/,
        __in BOOTSTRAPPER_ACTION_STATE /*action*/,
        __in INSTALLUILEVEL /*uiLevel*/,
        __in BOOL /*fDisableExternalUiHandler*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnExecutePatchTarget(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzTargetProductCode*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnExecuteProgress(
        __in_z LPCWSTR /*wzPackageId*/,
        __in DWORD /*dwProgressPercentage*/,
        __in DWORD /*dwOverallProgressPercentage*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnExecuteMsiMessage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in INSTALLMESSAGE /*messageType*/,
        __in DWORD /*dwUIHint*/,
        __in_z LPCWSTR /*wzMessage*/,
        __in DWORD /*cData*/,
        __in_ecount_z_opt(cData) LPCWSTR* /*rgwzData*/,
        __in int /*nRecommendation*/,
        __inout int* /*pResult*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnExecuteFilesInUse(
        __in_z LPCWSTR /*wzPackageId*/,
        __in DWORD /*cFiles*/,
        __in_ecount_z(cFiles) LPCWSTR* /*rgwzFiles*/,
        __in int /*nRecommendation*/,
        __in BOOTSTRAPPER_FILES_IN_USE_TYPE /*source*/,
        __inout int* /*pResult*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnExecutePackageComplete(
        __in_z LPCWSTR /*wzPackageId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_APPLY_RESTART /*restart*/,
        __in BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnExecuteComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnUnregisterBegin(
        __in BOOTSTRAPPER_REGISTRATION_TYPE /*recommendedRegistrationType*/,
        __inout BOOTSTRAPPER_REGISTRATION_TYPE* /*pRegistrationType*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnUnregisterComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnApplyComplete(
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_APPLY_RESTART /*restart*/,
        __in BOOTSTRAPPER_APPLYCOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnLaunchApprovedExeBegin(
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnLaunchApprovedExeComplete(
        __in HRESULT /*hrStatus*/,
        __in DWORD /*dwProcessId*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnBeginMsiTransactionBegin(
        __in_z LPCWSTR /*wzTransactionId*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnBeginMsiTransactionComplete(
        __in_z LPCWSTR /*wzTransactionId*/,
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCommitMsiTransactionBegin(
        __in_z LPCWSTR /*wzTransactionId*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCommitMsiTransactionComplete(
        __in_z LPCWSTR /*wzTransactionId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_APPLY_RESTART /*restart*/,
        __in BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnRollbackMsiTransactionBegin(
        __in_z LPCWSTR /*wzTransactionId*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnRollbackMsiTransactionComplete(
        __in_z LPCWSTR /*wzTransactionId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_APPLY_RESTART /*restart*/,
        __in BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_EXECUTEMSITRANSACTIONCOMPLETE_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPauseAutomaticUpdatesBegin(
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPauseAutomaticUpdatesComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnSystemRestorePointBegin(
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnSystemRestorePointComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanForwardCompatibleBundle(
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_RELATION_TYPE /*relationType*/,
        __in_z LPCWSTR /*wzBundleTag*/,
        __in BOOL /*fPerMachine*/,
        __in LPCWSTR /*wzVersion*/,
        __in BOOL /*fRecommendedIgnoreBundle*/,
        __inout BOOL* /*pfCancel*/,
        __inout BOOL* /*pfIgnoreBundle*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheContainerOrPayloadVerifyBegin(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheContainerOrPayloadVerifyProgress(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in DWORD64 /*dw64Progress*/,
        __in DWORD64 /*dw64Total*/,
        __in DWORD /*dwOverallPercentage*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheContainerOrPayloadVerifyComplete(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCachePayloadExtractBegin(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCachePayloadExtractProgress(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in DWORD64 /*dw64Progress*/,
        __in DWORD64 /*dw64Total*/,
        __in DWORD /*dwOverallPercentage*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCachePayloadExtractComplete(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanRestoreRelatedBundle(
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_REQUEST_STATE /*recommendedState*/,
        __inout BOOTSTRAPPER_REQUEST_STATE* /*pRequestedState*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanRelatedBundleType(
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE /*recommendedType*/,
        __inout BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE* /*pRequestedType*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnApplyDowngrade(
        __in HRESULT /*hrRecommended*/,
        __in HRESULT* /*phrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnExecuteProcessCancel(
        __in_z LPCWSTR /*wzPackageId*/,
        __in DWORD /*dwProcessId*/,
        __in BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_EXECUTEPROCESSCANCEL_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectRelatedBundlePackage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_RELATION_TYPE /*relationType*/,
        __in BOOL /*fPerMachine*/,
        __in LPCWSTR /*wzVersion*/,
        __inout BOOL* /*pfCancel*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCachePackageNonVitalValidationFailure(
        __in_z LPCWSTR /*wzPackageId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_CACHEPACKAGENONVITALVALIDATIONFAILURE_ACTION* /*pAction*/
        )
    {
        return S_OK;
    }

public: // IBAFunctions
    virtual STDMETHODIMP OnPlan(
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnThemeLoaded(
        __in HWND hWnd
        )
    {
        HRESULT hr = S_OK;

        m_hwndParent = hWnd;

        return hr;
    }

    virtual STDMETHODIMP WndProc(
        __in HWND /*hWnd*/,
        __in UINT /*uMsg*/,
        __in WPARAM /*wParam*/,
        __in LPARAM /*lParam*/,
        __inout BOOL* /*pfProcessed*/,
        __inout LRESULT* /*plResult*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP BAFunctionsProc(
        __in BA_FUNCTIONS_MESSAGE /*message*/,
        __in const LPVOID /*pvArgs*/,
        __inout LPVOID /*pvResults*/,
        __in_opt LPVOID /*pvContext*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP OnThemeControlLoading(
        __in LPCWSTR /*wzName*/,
        __inout BOOL* /*pfProcessed*/,
        __inout WORD* /*pwId*/,
        __inout DWORD* /*pdwAutomaticBehaviorType*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnThemeControlWmCommand(
        __in WPARAM /*wParam*/,
        __in LPCWSTR /*wzName*/,
        __in WORD /*wId*/,
        __in HWND /*hWnd*/,
        __inout BOOL* /*pfProcessed*/,
        __inout LRESULT* /*plResult*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnThemeControlWmNotify(
        __in LPNMHDR /*lParam*/,
        __in LPCWSTR /*wzName*/,
        __in WORD /*wId*/,
        __in HWND /*hWnd*/,
        __inout BOOL* /*pfProcessed*/,
        __inout LRESULT* /*plResult*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnThemeControlLoaded(
        __in LPCWSTR /*wzName*/,
        __in WORD /*wId*/,
        __in HWND /*hWnd*/,
        __inout BOOL* /*pfProcessed*/
        )
    {
        return S_OK;
    }

protected:
    CBalBaseBAFunctions(HMODULE hModule)
    {
        m_cReferences = 1;
        m_hModule = hModule;

        m_hwndParent = NULL;
        m_pEngine = NULL;
    }

    virtual ~CBalBaseBAFunctions()
    {
        ReleaseNullObject(m_pEngine);
    }

private:
    long m_cReferences;

protected:
    IBootstrapperEngine* m_pEngine;
    HMODULE m_hModule;
    HWND m_hwndParent;
};
