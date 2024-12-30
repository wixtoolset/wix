// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include <windows.h>
#include <msiquery.h>

#include "IBootstrapperApplication.h"

#include "balutil.h"
#include "balinfo.h"
#include "balretry.h"

#define CBalBaseBootstrapperApplication CBootstrapperApplicationBase

class CBootstrapperApplicationBase : public IBootstrapperApplication
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

        if (::IsEqualIID(__uuidof(IBootstrapperApplication), riid))
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
        __in BOOTSTRAPPER_COMMAND* pCommand
        )
    {
        HRESULT hr = S_OK;

        m_commandDisplay = pCommand->display;

        hr = BalInfoParseCommandLine(&m_BalInfoCommand, pCommand);
        BalExitOnFailure(hr, "Failed to parse command line with balutil.");

        pEngine->AddRef();
        m_pEngine = pEngine;

    LExit:
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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectForwardCompatibleBundle(
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_RELATION_TYPE /*relationType*/,
        __in_z LPCWSTR /*wzBundleTag*/,
        __in BOOL /*fPerMachine*/,
        __in_z LPCWSTR /*wzVersion*/,
        __in BOOL /*fMissingFromCache*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectUpdateBegin(
        __in_z LPCWSTR /*wzUpdateLocation*/,
        __inout BOOL* pfCancel,
        __inout BOOL* /*pfSkip*/
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectUpdate(
        __in_z LPCWSTR /*wzUpdateLocation*/,
        __in DWORD64 /*dw64Size*/,
        __in_z_opt LPCWSTR /*wzHash*/,
        __in BOOTSTRAPPER_UPDATE_HASH_TYPE /*hashAlgorithm*/,
        __in_z LPCWSTR /*wzVersion*/,
        __in_z LPCWSTR /*wzTitle*/,
        __in_z LPCWSTR /*wzSummary*/,
        __in_z LPCWSTR /*wzContentType*/,
        __in_z LPCWSTR /*wzContent*/,
        __inout BOOL* pfCancel,
        __inout BOOL* /*pfStopProcessingUpdates*/
        )
    {
        *pfCancel |= CheckCanceled();
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
        __in_z LPCWSTR /*wzVersion*/,
        __in BOOL /*fMissingFromCache*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectPackageBegin(
        __in_z LPCWSTR /*wzPackageId*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectCompatibleMsiPackage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzCompatiblePackageId*/,
        __in_z LPCWSTR /*wzCompatiblePackageVersion*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectRelatedMsiPackage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzUpgradeCode*/,
        __in_z LPCWSTR /*wzProductCode*/,
        __in BOOL /*fPerMachine*/,
        __in_z LPCWSTR /*wzVersion*/,
        __in BOOTSTRAPPER_RELATED_OPERATION /*operation*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectPatchTarget(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzProductCode*/,
        __in BOOTSTRAPPER_PACKAGE_STATE /*patchState*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnDetectMsiFeature(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzFeatureId*/,
        __in BOOTSTRAPPER_FEATURE_STATE /*state*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanRelatedBundle(
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_REQUEST_STATE /*recommendedState*/,
        __inout BOOTSTRAPPER_REQUEST_STATE* /*pRequestedState*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanRollbackBoundary(
        __in_z LPCWSTR /*wzRollbackBoundaryId*/,
        __in BOOL /*fRecommendedTransaction*/,
        __inout BOOL* /*pfTransaction*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanCompatibleMsiPackageBegin(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzCompatiblePackageId*/,
        __in_z LPCWSTR /*wzCompatiblePackageVersion*/,
        __in BOOL /*fRecommendedRemove*/,
        __inout BOOL* /*pfRequestRemove*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanMsiFeature(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzFeatureId*/,
        __in BOOTSTRAPPER_FEATURE_STATE /*recommendedState*/,
        __inout BOOTSTRAPPER_FEATURE_STATE* /*pRequestedState*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanMsiPackage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in BOOL /*fExecute*/,
        __in BOOTSTRAPPER_ACTION_STATE /*action*/,
        __in BOOTSTRAPPER_MSI_FILE_VERSIONING /*recommendedFileVersioning*/,
        __inout BOOL* pfCancel,
        __inout BURN_MSI_PROPERTY* /*pActionMsiProperty*/,
        __inout INSTALLUILEVEL* /*pUiLevel*/,
        __inout BOOL* /*pfDisableExternalUiHandler*/,
        __inout BOOTSTRAPPER_MSI_FILE_VERSIONING* /*pFileVersioning*/
        )
    {
        *pfCancel |= CheckCanceled();
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
        __inout BOOL* pfCancel
        )
    {
        m_dwProgressPercentage = 0;
        m_dwOverallProgressPercentage = 0;

        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnElevateBegin(
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnElevateComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnProgress(
        __in DWORD dwProgressPercentage,
        __in DWORD dwOverallProgressPercentage,
        __inout BOOL* pfCancel
        )
    {
        HRESULT hr = S_OK;
        int nResult = IDNOACTION;

        m_dwProgressPercentage = dwProgressPercentage;
        m_dwOverallProgressPercentage = dwOverallProgressPercentage;

        if (BOOTSTRAPPER_DISPLAY_EMBEDDED == m_commandDisplay)
        {
            hr = m_pEngine->SendEmbeddedProgress(m_dwProgressPercentage, m_dwOverallProgressPercentage, &nResult);
            BalExitOnFailure(hr, "Failed to send embedded overall progress.");

            if (IDERROR == nResult)
            {
                hr = E_FAIL;
            }
            else if (IDCANCEL == nResult)
            {
                *pfCancel = TRUE;
            }
        }

    LExit:
        *pfCancel |= CheckCanceled();
        return hr;
    }

    virtual STDMETHODIMP OnError(
        __in BOOTSTRAPPER_ERROR_TYPE errorType,
        __in_z LPCWSTR wzPackageId,
        __in DWORD dwCode,
        __in_z LPCWSTR wzError,
        __in DWORD dwUIHint,
        __in DWORD /*cData*/,
        __in_ecount_z_opt(cData) LPCWSTR* /*rgwzData*/,
        __in int /*nRecommendation*/,
        __inout int* pResult
        )
    {
        BalRetryErrorOccurred(wzPackageId, dwCode);

        if (BOOTSTRAPPER_DISPLAY_EMBEDDED == m_commandDisplay)
        {
            HRESULT hr = m_pEngine->SendEmbeddedError(dwCode, wzError, dwUIHint, pResult);
            if (FAILED(hr))
            {
                *pResult = IDERROR;
            }
        }
        else if (CheckCanceled())
        {
            *pResult = IDCANCEL;
        }
        else if (BOOTSTRAPPER_DISPLAY_FULL == m_commandDisplay)
        {
            if (BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_SERVER == errorType || BOOTSTRAPPER_ERROR_TYPE_HTTP_AUTH_PROXY == errorType)
            {
                *pResult = IDTRYAGAIN;
            }
        }

        return S_OK;
    }

    virtual STDMETHODIMP OnRegisterBegin(
        __in BOOTSTRAPPER_REGISTRATION_TYPE /*recommendedRegistrationType*/,
        __inout BOOL* pfCancel,
        __inout BOOTSTRAPPER_REGISTRATION_TYPE* /*pRegistrationType*/
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnRegisterComplete(
        __in HRESULT /*hrStatus*/
        )
    {
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheBegin(
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnCachePackageBegin(
        __in_z LPCWSTR /*wzPackageId*/,
        __in DWORD /*cCachePayloads*/,
        __in DWORD64 /*dw64PackageCacheSize*/,
        __in BOOL /*fVital*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheAcquireBegin(
        __in_z_opt LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in_z LPCWSTR /*wzSource*/,
        __in_z_opt LPCWSTR /*wzDownloadUrl*/,
        __in_z_opt LPCWSTR /*wzPayloadContainerId*/,
        __in BOOTSTRAPPER_CACHE_OPERATION /*recommendation*/,
        __inout BOOTSTRAPPER_CACHE_OPERATION* /*pAction*/,
        __inout BOOL* pfCancel
        )
    {
        BalRetryStartContainerOrPayload(wzPackageOrContainerId, wzPayloadId);
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheAcquireProgress(
        __in_z LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in DWORD64 /*dw64Progress*/,
        __in DWORD64 /*dw64Total*/,
        __in DWORD /*dwOverallPercentage*/,
        __inout BOOL* pfCancel
        )
    {
        HRESULT hr = S_OK;
        int nResult = IDNOACTION;

        // Send progress even though we don't update the numbers to at least give the caller an opportunity
        // to cancel.
        if (BOOTSTRAPPER_DISPLAY_EMBEDDED == m_commandDisplay)
        {
            hr = m_pEngine->SendEmbeddedProgress(m_dwProgressPercentage, m_dwOverallProgressPercentage, &nResult);
            BalExitOnFailure(hr, "Failed to send embedded cache progress.");

            if (IDERROR == nResult)
            {
                hr = E_FAIL;
            }
            else if (IDCANCEL == nResult)
            {
                *pfCancel = TRUE;
            }
        }

    LExit:
        *pfCancel |= CheckCanceled();
        return hr;
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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheAcquireComplete(
        __in_z LPCWSTR wzPackageOrContainerId,
        __in_z_opt LPCWSTR wzPayloadId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION* pAction
        )
    {
        HRESULT hr = S_OK;
        BOOL fRetry = FALSE;

        if (CheckCanceled())
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT));
        }

        hr = BalRetryEndContainerOrPayload(wzPackageOrContainerId, wzPayloadId, hrStatus, &fRetry);
        ExitOnFailure(hr, "BalRetryEndPackage for cache failed");

        if (fRetry)
        {
            *pAction = BOOTSTRAPPER_CACHEACQUIRECOMPLETE_ACTION_RETRY;
        }

    LExit:
        return hr;
    }

    virtual STDMETHODIMP OnCacheVerifyBegin(
        __in_z LPCWSTR /*wzPackageOrContainerId*/,
        __in_z LPCWSTR /*wzPayloadId*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheVerifyProgress(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in DWORD64 /*dw64Progress*/,
        __in DWORD64 /*dw64Total*/,
        __in DWORD /*dwOverallPercentage*/,
        __in BOOTSTRAPPER_CACHE_VERIFY_STEP /*verifyStep*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheVerifyComplete(
        __in_z LPCWSTR /*wzPackageOrContainerId*/,
        __in_z LPCWSTR /*wzPayloadId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION* pAction
        )
    {
        if (CheckCanceled())
        {
            *pAction = BOOTSTRAPPER_CACHEVERIFYCOMPLETE_ACTION_NONE;
        }

        return S_OK;
    }

    virtual STDMETHODIMP OnCachePackageComplete(
        __in_z LPCWSTR /*wzPackageId*/,
        __in HRESULT /*hrStatus*/,
        __in BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION* pAction
        )
    {
        if (CheckCanceled())
        {
            *pAction = BOOTSTRAPPER_CACHEPACKAGECOMPLETE_ACTION_NONE;
        }

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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnExecutePackageBegin(
        __in_z LPCWSTR wzPackageId,
        __in BOOL fExecute,
        __in BOOTSTRAPPER_ACTION_STATE /*action*/,
        __in INSTALLUILEVEL /*uiLevel*/,
        __in BOOL /*fDisableExternalUiHandler*/,
        __inout BOOL* pfCancel
        )
    {
        // Only track retry on execution (not rollback).
        if (fExecute)
        {
            BalRetryStartPackage(wzPackageId);
        }

        m_fRollingBack = !fExecute;
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnExecutePatchTarget(
        __in_z LPCWSTR /*wzPackageId*/,
        __in_z LPCWSTR /*wzTargetProductCode*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnExecuteProgress(
        __in_z LPCWSTR /*wzPackageId*/,
        __in DWORD /*dwProgressPercentage*/,
        __in DWORD /*dwOverallProgressPercentage*/,
        __inout BOOL* pfCancel
        )
    {
        HRESULT hr = S_OK;
        int nResult = IDNOACTION;

        // Send progress even though we don't update the numbers to at least give the caller an opportunity
        // to cancel.
        if (BOOTSTRAPPER_DISPLAY_EMBEDDED == m_commandDisplay)
        {
            hr = m_pEngine->SendEmbeddedProgress(m_dwProgressPercentage, m_dwOverallProgressPercentage, &nResult);
            BalExitOnFailure(hr, "Failed to send embedded execute progress.");

            if (IDERROR == nResult)
            {
                hr = E_FAIL;
            }
            else if (IDCANCEL == nResult)
            {
                *pfCancel = TRUE;
            }
        }

    LExit:
        *pfCancel |= CheckCanceled();
        return hr;
    }

    virtual STDMETHODIMP OnExecuteMsiMessage(
        __in_z LPCWSTR /*wzPackageId*/,
        __in INSTALLMESSAGE /*messageType*/,
        __in DWORD /*dwUIHint*/,
        __in_z LPCWSTR /*wzMessage*/,
        __in DWORD /*cData*/,
        __in_ecount_z_opt(cData) LPCWSTR* /*rgwzData*/,
        __in int /*nRecommendation*/,
        __inout int* pResult
        )
    {
        if (CheckCanceled())
        {
            *pResult = IDCANCEL;
        }

        return S_OK;
    }

    virtual STDMETHODIMP OnExecuteFilesInUse(
        __in_z LPCWSTR /*wzPackageId*/,
        __in DWORD /*cFiles*/,
        __in_ecount_z(cFiles) LPCWSTR* /*rgwzFiles*/,
        __in int /*nRecommendation*/,
        __in BOOTSTRAPPER_FILES_IN_USE_TYPE /*source*/,
        __inout int* pResult
        )
    {
        if (CheckCanceled())
        {
            *pResult = IDCANCEL;
        }

        return S_OK;
    }

    virtual STDMETHODIMP OnExecutePackageComplete(
        __in_z LPCWSTR wzPackageId,
        __in HRESULT hrStatus,
        __in BOOTSTRAPPER_APPLY_RESTART /*restart*/,
        __in BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION* pAction
        )
    {
        HRESULT hr = S_OK;
        BOOL fRetry = FALSE;

        if (CheckCanceled())
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT));
        }

        hr = BalRetryEndPackage(wzPackageId, hrStatus, &fRetry);
        ExitOnFailure(hr, "BalRetryEndPackage for execute failed");

        if (fRetry)
        {
            *pAction = BOOTSTRAPPER_EXECUTEPACKAGECOMPLETE_ACTION_RETRY;
        }

    LExit:
        return hr;
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
        __in BOOTSTRAPPER_APPLY_RESTART restart,
        __in BOOTSTRAPPER_APPLYCOMPLETE_ACTION /*recommendation*/,
        __inout BOOTSTRAPPER_APPLYCOMPLETE_ACTION* pAction
        )
    {
        HRESULT hr = S_OK;
        BOOL fRestartRequired = BOOTSTRAPPER_APPLY_RESTART_REQUIRED == restart;
        BOOL fShouldBlockRestart = BOOTSTRAPPER_DISPLAY_FULL <= m_commandDisplay && BAL_INFO_RESTART_PROMPT >= m_BalInfoCommand.restart;

        if (fRestartRequired && !fShouldBlockRestart)
        {
            *pAction = BOOTSTRAPPER_APPLYCOMPLETE_ACTION_RESTART;
        }

        return hr;
    }

    virtual STDMETHODIMP OnLaunchApprovedExeBegin(
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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
        HRESULT hr = S_OK;

        if (CheckCanceled())
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT));
        }

    LExit:
        return hr;
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
        HRESULT hr = S_OK;

        if (CheckCanceled())
        {
            ExitFunction1(hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT));
        }

    LExit:
        return hr;
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
        __in_z LPCWSTR /*wzVersion*/,
        __in BOOL /*fRecommendedIgnoreBundle*/,
        __inout BOOL* pfCancel,
        __inout BOOL* /*pfIgnoreBundle*/
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheContainerOrPayloadVerifyBegin(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnCacheContainerOrPayloadVerifyProgress(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in DWORD64 /*dw64Progress*/,
        __in DWORD64 /*dw64Total*/,
        __in DWORD /*dwOverallPercentage*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnCachePayloadExtractProgress(
        __in_z_opt LPCWSTR /*wzPackageOrContainerId*/,
        __in_z_opt LPCWSTR /*wzPayloadId*/,
        __in DWORD64 /*dw64Progress*/,
        __in DWORD64 /*dw64Total*/,
        __in DWORD /*dwOverallPercentage*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
        return S_OK;
    }

    virtual STDMETHODIMP OnPlanRelatedBundleType(
        __in_z LPCWSTR /*wzBundleCode*/,
        __in BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE /*recommendedType*/,
        __inout BOOTSTRAPPER_RELATED_BUNDLE_PLAN_TYPE* /*pRequestedType*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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
        __in_z LPCWSTR /*wzVersion*/,
        __inout BOOL* pfCancel
        )
    {
        *pfCancel |= CheckCanceled();
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

protected:
    //
    // PromptCancel - prompts the user to close (if not forced).
    //
    virtual BOOL PromptCancel(
        __in HWND hWnd,
        __in BOOL fForceCancel,
        __in_z_opt LPCWSTR wzMessage,
        __in_z_opt LPCWSTR wzCaption
        )
    {
        ::EnterCriticalSection(&m_csCanceled);

        // Only prompt the user to close if we have not canceled already.
        if (!m_fCanceled)
        {
            if (fForceCancel)
            {
                m_fCanceled = TRUE;
            }
            else
            {
                m_fCanceled = (IDYES == ::MessageBoxW(hWnd, wzMessage, wzCaption, MB_YESNO | MB_ICONEXCLAMATION));
            }
        }

        ::LeaveCriticalSection(&m_csCanceled);

        return m_fCanceled;
    }

    //
    // CheckCanceled - waits if the cancel dialog is up and checks to see if the user canceled the operation.
    //
    BOOL CheckCanceled()
    {
        ::EnterCriticalSection(&m_csCanceled);
        ::LeaveCriticalSection(&m_csCanceled);
        return m_fRollingBack ? FALSE : m_fCanceled;
    }

    BOOL IsRollingBack()
    {
        return m_fRollingBack;
    }

    BOOL IsCanceled()
    {
        return m_fCanceled;
    }

    CBootstrapperApplicationBase(
        __in DWORD dwRetryCount = 0,
        __in DWORD dwRetryTimeout = 1000
        )
    {
        m_cReferences = 1;
        m_commandDisplay = BOOTSTRAPPER_DISPLAY_UNKNOWN;

        m_pEngine = NULL;

        ::InitializeCriticalSection(&m_csCanceled);
        m_fCanceled = FALSE;
        m_BalInfoCommand = { };
        m_fRollingBack = FALSE;

        m_dwProgressPercentage = 0;
        m_dwOverallProgressPercentage = 0;

        BalRetryInitialize(dwRetryCount, dwRetryTimeout);
    }

    virtual ~CBootstrapperApplicationBase()
    {
        BalInfoUninitializeCommandLine(&m_BalInfoCommand);
        BalRetryUninitialize();
        ::DeleteCriticalSection(&m_csCanceled);

        ReleaseNullObject(m_pEngine);
    }

protected:
    CRITICAL_SECTION m_csCanceled;
    BOOL m_fCanceled;

    IBootstrapperEngine* m_pEngine;
    BAL_INFO_COMMAND m_BalInfoCommand;

private:
    long m_cReferences;
    BOOTSTRAPPER_DISPLAY m_commandDisplay;

    BOOL m_fRollingBack;

    DWORD m_dwProgressPercentage;
    DWORD m_dwOverallProgressPercentage;
};
