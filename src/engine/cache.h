#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#define BURN_CACHE_MAX_SEARCH_PATHS 7

#ifdef __cplusplus
extern "C" {
#endif


enum BURN_CACHE_MESSAGE_TYPE
{
    BURN_CACHE_MESSAGE_BEGIN,
    BURN_CACHE_MESSAGE_SUCCESS,
    BURN_CACHE_MESSAGE_COMPLETE,
};

enum BURN_CACHE_STEP
{
    BURN_CACHE_STEP_HASH_TO_SKIP_ACQUIRE,
    BURN_CACHE_STEP_HASH_TO_SKIP_VERIFY,
    BURN_CACHE_STEP_STAGE,
    BURN_CACHE_STEP_HASH,
    BURN_CACHE_STEP_FINALIZE,
};

typedef struct _BURN_CACHE_MESSAGE
{
    BURN_CACHE_MESSAGE_TYPE type;

    union
    {
        struct
        {
            BURN_CACHE_STEP cacheStep;
        } begin;
        struct
        {
            DWORD64 qwFileSize;
        } success;
        struct
        {
            HRESULT hrStatus;
        } complete;
    };
} BURN_CACHE_MESSAGE;

typedef HRESULT(CALLBACK* PFN_BURNCACHEMESSAGEHANDLER)(
    __in BURN_CACHE_MESSAGE* pMessage,
    __in LPVOID pvContext
    );

// functions

HRESULT CacheInitialize(
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in_z_opt LPCWSTR wzSourceProcessPath
    );
HRESULT CacheEnsureWorkingFolder(
    __in_z_opt LPCWSTR wzBundleId,
    __deref_out_z_opt LPWSTR* psczWorkingFolder
    );
HRESULT CacheCalculateBundleWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __in LPCWSTR wzExecutableName,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheCalculateBundleLayoutWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheCalculatePayloadWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __in BURN_PAYLOAD* pPayload,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheCalculateContainerWorkingPath(
    __in_z LPCWSTR wzBundleId,
    __in BURN_CONTAINER* pContainer,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheGetRootCompletedPath(
    __in BOOL fPerMachine,
    __in BOOL fForceInitialize,
    __deref_out_z LPWSTR* psczRootCompletedPath
    );
HRESULT CacheGetCompletedPath(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzCacheId,
    __deref_out_z LPWSTR* psczCompletedPath
    );
HRESULT CacheGetResumePath(
    __in_z LPCWSTR wzPayloadWorkingPath,
    __deref_out_z LPWSTR* psczResumePath
    );
HRESULT CacheGetLocalSourcePaths(
    __in_z LPCWSTR wzRelativePath,
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzDestinationPath,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __in BURN_VARIABLES* pVariables,
    __inout LPWSTR** prgSearchPaths,
    __out DWORD* pcSearchPaths,
    __out DWORD* pdwLikelySearchPath
    );
HRESULT CacheSetLastUsedSource(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzRelativePath
    );
HRESULT CacheSendProgressCallback(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in HANDLE hDestinationFile
    );
void CacheSendErrorCallback(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in HRESULT hrError,
    __in_z_opt LPCWSTR wzError,
    __out_opt BOOL* pfRetry
    );
BOOL CacheBundleRunningFromCache();
HRESULT CacheBundleToCleanRoom(
    __in BURN_PAYLOADS* pUxPayloads,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczCleanRoomBundlePath
    );
HRESULT CacheBundleToWorkingDirectory(
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzExecutableName,
    __in BURN_PAYLOADS* pUxPayloads,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczEngineWorkingPath
    );
HRESULT CacheLayoutBundle(
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzSourceBundlePath,
    __in DWORD64 qwBundleSize,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
HRESULT CacheCompleteBundle(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzSourceBundlePath
#ifdef DEBUG
    , __in_z LPCWSTR wzExecutablePath
#endif
    );
HRESULT CacheLayoutContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzUnverifiedContainerPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
HRESULT CacheLayoutPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
HRESULT CacheCompletePayload(
    __in BOOL fPerMachine,
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzCacheId,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
HRESULT CacheVerifyContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzCachedDirectory,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
HRESULT CacheVerifyPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzCachedDirectory,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
HRESULT CacheRemoveWorkingFolder(
    __in_z_opt LPCWSTR wzBundleId
    );
HRESULT CacheRemoveBundle(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPackageId
    );
HRESULT CacheRemovePackage(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCacheId
    );
void CacheCleanup(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzBundleId
    );
void CacheUninitialize();

#ifdef __cplusplus
}
#endif
