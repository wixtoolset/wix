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
    BURN_CACHE_MESSAGE_FAILURE,
};

enum BURN_CACHE_STEP
{
    BURN_CACHE_STEP_HASH_TO_SKIP_ACQUIRE,
    BURN_CACHE_STEP_HASH_TO_SKIP_VERIFY,
    BURN_CACHE_STEP_STAGE,
    BURN_CACHE_STEP_HASH,
    BURN_CACHE_STEP_FINALIZE,
};

typedef struct _BURN_CACHE
{
    BOOL fInitializedCache;
    BOOL fPerMachineCacheRootVerified;
    BOOL fOriginalPerMachineCacheRootVerified;
    BOOL fUnverifiedCacheFolderCreated;
    BOOL fCustomMachinePackageCache;
    LPWSTR sczDefaultUserPackageCache;
    LPWSTR sczDefaultMachinePackageCache;
    LPWSTR sczCurrentMachinePackageCache;

    WCHAR wzGuid[GUID_STRING_LENGTH + 1];
    LPWSTR* rgsczPotentialBaseWorkingFolders;
    DWORD cPotentialBaseWorkingFolders;

    // Only valid after CacheInitializeSources
    BOOL fInitializedCacheSources;
    BOOL fRunningFromCache;
    LPWSTR sczSourceProcessFolder;
    LPWSTR sczAcquisitionFolder;

    // Only valid after CacheEnsureBaseWorkingFolder
    BOOL fInitializedBaseWorkingFolder;
    LPWSTR sczBaseWorkingFolder;
} BURN_CACHE;

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
        struct
        {
            BURN_CACHE_STEP cacheStep;
        } failure;
    };
} BURN_CACHE_MESSAGE;

typedef HRESULT(CALLBACK* PFN_BURNCACHEMESSAGEHANDLER)(
    __in BURN_CACHE_MESSAGE* pMessage,
    __in LPVOID pvContext
    );

// functions

HRESULT CacheInitialize(
    __in BURN_CACHE* pCache,
    __in BURN_ENGINE_COMMAND* pInternalCommand
    );
HRESULT CacheInitializeSources(
    __in BURN_CACHE* pCache,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BURN_ENGINE_COMMAND* pInternalCommand
    );
HRESULT CacheEnsureAcquisitionFolder(
    __in BURN_CACHE* pCache
    );
HRESULT CacheEnsureBaseWorkingFolder(
    __in BOOL fElevated,
    __in BURN_CACHE* pCache,
    __deref_out_z_opt LPWSTR* psczBaseWorkingFolder
    );
HRESULT CacheCalculateBundleWorkingPath(
    __in BURN_CACHE* pCache,
    __in LPCWSTR wzExecutableName,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheCalculateBundleLayoutWorkingPath(
    __in BURN_CACHE* pCache,
    __in_z LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheCalculatePayloadWorkingPath(
    __in BURN_CACHE* pCache,
    __in BURN_PAYLOAD* pPayload,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheCalculateContainerWorkingPath(
    __in BURN_CACHE* pCache,
    __in BURN_CONTAINER* pContainer,
    __deref_out_z LPWSTR* psczWorkingPath
    );
HRESULT CacheGetPerMachineRootCompletedPath(
    __in BURN_CACHE* pCache,
    __out_z LPWSTR* psczCurrentRootCompletedPath,
    __out_z LPWSTR* psczDefaultRootCompletedPath
    );
HRESULT CacheGetCompletedPath(
    __in BURN_CACHE* pCache,
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
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __inout LPWSTR** prgSearchPaths,
    __out DWORD* pcSearchPaths,
    __out DWORD* pdwLikelySearchPath,
    __out DWORD* pdwDestinationSearchPath
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
BOOL CacheBundleRunningFromCache(
    __in BURN_CACHE* pCache
    );
HRESULT CachePreparePackage(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGE* pPackage
    );
HRESULT CacheBundleToCleanRoom(
    __in BOOL fElevated,
    __in BURN_CACHE* pCache,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczCleanRoomBundlePath
    );
HRESULT CacheBundleToWorkingDirectory(
    __in BOOL fElvated,
    __in BURN_CACHE* pCache,
    __in_z LPCWSTR wzExecutableName,
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
    __in BURN_CACHE* pCache,
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
    __in BURN_CACHE* pCache,
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
HRESULT CacheRemoveBaseWorkingFolder(
    __in BURN_CACHE* pCache
    );
HRESULT CacheRemoveBundle(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPackageId
    );
HRESULT CacheRemovePackage(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCacheId
    );
void CacheCleanup(
    __in BOOL fPerMachine,
    __in BURN_CACHE* pCache
    );
void CacheUninitialize(
    __in BURN_CACHE* pCache
    );

#ifdef __cplusplus
}
#endif
