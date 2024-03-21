// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR BUNDLE_CLEAN_ROOM_WORKING_FOLDER_NAME = L".cr";
static const LPCWSTR BUNDLE_WORKING_FOLDER_NAME = L".be";
static const LPCWSTR UNVERIFIED_CACHE_FOLDER_NAME = L".unverified";
static const LPCWSTR PACKAGE_CACHE_FOLDER_NAME = L"Package Cache";
static const DWORD FILE_OPERATION_RETRY_COUNT = 3;
static const DWORD FILE_OPERATION_RETRY_WAIT = 2000;

static HRESULT CacheVerifyPayloadSignature(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile,
    __in BURN_CACHE_STEP cacheStep,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
static HRESULT CalculatePotentialBaseWorkingFolders(
    __in BURN_CACHE* pCache,
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in LPCWSTR wzAcquisitionFolder
    );
static HRESULT CalculateWorkingFolders(
    __in BURN_CACHE* pCache,
    __in BURN_ENGINE_COMMAND* pInternalCommand
    );
static HRESULT GetLastUsedSourceFolder(
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczLastSource
    );
static HRESULT SecurePerMachineCacheRoot(
    __in BURN_CACHE* pCache
    );
static HRESULT CreateCompletedPath(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in LPCWSTR wzCacheId,
    __in LPCWSTR wzFilePath,
    __out_z LPWSTR* psczCachePath
    );
static HRESULT CreateUnverifiedPath(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPayloadId,
    __out_z LPWSTR* psczUnverifiedPayloadPath
    );
static HRESULT GetRootPath(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in BOOL fAllowRedirect,
    __deref_out_z LPWSTR* psczRootPath
    );
static HRESULT VerifyThenTransferContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzCachedPath,
    __in_z LPCWSTR wzUnverifiedContainerPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
static HRESULT VerifyThenTransferPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzCachedPath,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
static HRESULT CacheTransferFileWithRetry(
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzDestinationPath,
    __in BOOL fMove,
    __in BURN_CACHE_STEP cacheStep,
    __in DWORD64 qwFileSize,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
static HRESULT VerifyFileAgainstContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzVerifyPath,
    __in BOOL fAlreadyCached,
    __in BURN_CACHE_STEP cacheStep,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
static HRESULT VerifyFileAgainstPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzVerifyPath,
    __in BOOL fAlreadyCached,
    __in BURN_CACHE_STEP cacheStep,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
static HRESULT ResetPathPermissions(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPath
    );
static HRESULT SecurePath(
    __in LPCWSTR wzPath
    );
static HRESULT CopyEngineToWorkingFolder(
    __in BOOL fElevated,
    __in BURN_CACHE* pCache,
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzWorkingFolderName,
    __in_z LPCWSTR wzExecutableName,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczEngineWorkingPath
    );
static HRESULT CopyEngineWithSignatureFixup(
    __in HANDLE hEngineFile,
    __in_z LPCWSTR wzEnginePath,
    __in_z LPCWSTR wzTargetPath,
    __in BURN_SECTION* pSection
    );
static HRESULT RemoveBundleOrPackage(
    __in BURN_CACHE* pCache,
    __in BOOL fBundle,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzBundleOrPackageId,
    __in_z LPCWSTR wzCacheId
    );
static HRESULT VerifyFileSize(
    __in HANDLE hFile,
    __in DWORD64 qwFileSize,
    __in_z LPCWSTR wzUnverifiedPayloadPath
    );
static HRESULT VerifyHash(
    __in BYTE* pbHash,
    __in DWORD cbHash,
    __in DWORD64 qwFileSize,
    __in BOOL fVerifyFileSize,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile,
    __in BURN_CACHE_STEP cacheStep,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    );
static HRESULT VerifyPayloadAgainstCertChain(
    __in BURN_PAYLOAD* pPayload,
    __in PCCERT_CHAIN_CONTEXT pChainContext
    );
static HRESULT SendCacheBeginMessage(
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPVOID pContext,
    __in BURN_CACHE_STEP cacheStep
    );
static HRESULT SendCacheSuccessMessage(
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPVOID pContext,
    __in DWORD64 qwFileSize
    );
static HRESULT SendCacheCompleteMessage(
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPVOID pContext,
    __in HRESULT hrStatus
    );
static HRESULT SendCacheFailureMessage(
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPVOID pContext,
    __in BURN_CACHE_STEP cacheStep
    );

extern "C" HRESULT CacheInitialize(
    __in BURN_CACHE* pCache,
    __in BURN_ENGINE_COMMAND* pInternalCommand
    )
{
    Assert(!pCache->fInitializedCache);

    HRESULT hr = S_OK;
    LPWSTR sczAppData = NULL;
    BOOL fPathEqual = FALSE;

    // Cache paths are initialized once so they cannot be changed while the engine is caching payloads.
    // Always construct the default machine package cache path so we can determine if we're redirected.
    hr = ShelGetFolder(&sczAppData, CSIDL_COMMON_APPDATA);
    ExitOnFailure(hr, "Failed to find local %hs appdata directory.", "per-machine");

    hr = PathConcat(sczAppData, PACKAGE_CACHE_FOLDER_NAME, &pCache->sczDefaultMachinePackageCache);
    ExitOnFailure(hr, "Failed to construct %hs package cache directory name.", "per-machine");

    hr = PathBackslashTerminate(&pCache->sczDefaultMachinePackageCache);
    ExitOnFailure(hr, "Failed to backslash terminate default %hs package cache directory name.", "per-machine");


    // The machine package cache can be redirected through policy.
    hr = PolcReadString(POLICY_BURN_REGISTRY_PATH, L"PackageCache", NULL, &pCache->sczCurrentMachinePackageCache);
    ExitOnFailure(hr, "Failed to read PackageCache policy directory.");

    if (pCache->sczCurrentMachinePackageCache && PathIsFullyQualified(pCache->sczCurrentMachinePackageCache))
    {
        hr = PathBackslashTerminate(&pCache->sczCurrentMachinePackageCache);
        ExitOnFailure(hr, "Failed to backslash terminate redirected per-machine package cache directory name.");
    }
    else
    {
        if (pCache->sczCurrentMachinePackageCache)
        {
            LogErrorId(E_INVALIDARG, MSG_INVALID_POLICY_MACHINE_PACKAGE_CACHE, pCache->sczCurrentMachinePackageCache, NULL, NULL);
        }

        hr = StrAllocString(&pCache->sczCurrentMachinePackageCache, pCache->sczDefaultMachinePackageCache, 0);
        ExitOnFailure(hr, "Failed to copy default package cache directory to current package cache directory.");
    }

    hr = PathCompareCanonicalized(pCache->sczDefaultMachinePackageCache, pCache->sczCurrentMachinePackageCache, &fPathEqual);
    ExitOnFailure(hr, "Failed to compare default and current package cache directories.");

    pCache->fCustomMachinePackageCache = !fPathEqual;


    hr = ShelGetFolder(&sczAppData, CSIDL_LOCAL_APPDATA);
    ExitOnFailure(hr, "Failed to find local %hs appdata directory.", "per-user");

    hr = PathConcat(sczAppData, PACKAGE_CACHE_FOLDER_NAME, &pCache->sczDefaultUserPackageCache);
    ExitOnFailure(hr, "Failed to construct %hs package cache directory name.", "per-user");

    hr = PathBackslashTerminate(&pCache->sczDefaultUserPackageCache);
    ExitOnFailure(hr, "Failed to backslash terminate default %hs package cache directory name.", "per-user");


    hr = CalculateWorkingFolders(pCache, pInternalCommand);

    pCache->fInitializedCache = TRUE;

LExit:
    ReleaseStr(sczAppData);

    return hr;
}


extern "C" HRESULT CacheInitializeSources(
    __in BURN_CACHE* pCache,
    __in BURN_REGISTRATION* pRegistration,
    __in BURN_VARIABLES* pVariables,
    __in BURN_ENGINE_COMMAND* pInternalCommand
    )
{
    Assert(!pCache->fInitializedCacheSources);

    HRESULT hr = S_OK;
    LPWSTR sczCurrentPath = NULL;
    LPWSTR sczCompletedFolder = NULL;
    LPWSTR sczCompletedPath = NULL;
    LPWSTR sczOriginalSource = NULL;
    LPWSTR sczOriginalSourceFolder = NULL;
    BOOL fPathEqual = FALSE;
    LPCWSTR wzSourceProcessPath = pInternalCommand->sczSourceProcessPath;

    hr = PathForCurrentProcess(&sczCurrentPath, NULL);
    ExitOnFailure(hr, "Failed to get current process path.");

    // Determine if we are running from the package cache or not.
    hr = CacheGetCompletedPath(pCache, pRegistration->fPerMachine, pRegistration->sczId, &sczCompletedFolder);
    ExitOnFailure(hr, "Failed to get completed path for bundle.");

    hr = PathConcatRelativeToFullyQualifiedBase(sczCompletedFolder, pRegistration->sczExecutableName, &sczCompletedPath);
    ExitOnFailure(hr, "Failed to combine working path with engine file name.");

    hr = PathCompareCanonicalized(sczCurrentPath, sczCompletedPath, &fPathEqual);
    ExitOnFailure(hr, "Failed to compare current path for bundle: %ls", sczCurrentPath);

    pCache->fRunningFromCache = fPathEqual;

    // If a source process path was not provided (e.g. we are not being
    // run in a clean room) then use the current process path as the
    // source process path.
    if (!wzSourceProcessPath)
    {
        wzSourceProcessPath = sczCurrentPath;
    }

    hr = PathGetDirectory(wzSourceProcessPath, &pCache->sczSourceProcessFolder);
    ExitOnFailure(hr, "Failed to initialize cache source folder.");

    // If we're not running from the cache, ensure the original source is set.
    if (!pCache->fRunningFromCache)
    {
        // If the original source has not been set already then set it where the bundle is
        // running from right now. This value will be persisted and we'll use it when launched
        // from the clean room or package cache since none of our packages will be relative to
        // those locations.
        hr = VariableGetString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE, &sczOriginalSource);
        if (E_NOTFOUND == hr)
        {
            hr = VariableSetString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE, wzSourceProcessPath, FALSE, FALSE);
            ExitOnFailure(hr, "Failed to set original source variable.");

            hr = StrAllocString(&sczOriginalSource, wzSourceProcessPath, 0);
            ExitOnFailure(hr, "Failed to copy current path to original source.");
        }

        hr = VariableGetString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER, &sczOriginalSourceFolder);
        if (E_NOTFOUND == hr)
        {
            hr = PathGetDirectory(sczOriginalSource, &sczOriginalSourceFolder);
            ExitOnFailure(hr, "Failed to get directory from original source path.");

            hr = VariableSetString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER, sczOriginalSourceFolder, FALSE, FALSE);
            ExitOnFailure(hr, "Failed to set original source directory variable.");
        }
    }

    pCache->fInitializedCacheSources = TRUE;

LExit:
    ReleaseStr(sczCurrentPath);
    ReleaseStr(sczCompletedFolder);
    ReleaseStr(sczCompletedPath);
    ReleaseStr(sczOriginalSource);
    ReleaseStr(sczOriginalSourceFolder);

    return hr;
}

extern "C" HRESULT CacheEnsureAcquisitionFolder(
    __in BURN_CACHE* pCache
    )
{
    Assert(pCache->fInitializedCache);

    HRESULT hr = S_OK;

    hr = DirEnsureExists(pCache->sczAcquisitionFolder, NULL);
    ExitOnFailure(hr, "Failed create acquisition folder.");

    // Best effort to ensure our acquisition folder is not encrypted.
    ::DecryptFileW(pCache->sczAcquisitionFolder, 0);

LExit:
    return hr;
}

extern "C" HRESULT CacheEnsureBaseWorkingFolder(
    __in BOOL fElevated,
    __in BURN_CACHE* pCache,
    __deref_out_z_opt LPWSTR* psczBaseWorkingFolder
    )
{
    Assert(pCache->fInitializedCache);

    HRESULT hr = S_OK;
    LPWSTR sczPotential = NULL;
    PSECURITY_DESCRIPTOR psd = NULL;
    LPSECURITY_ATTRIBUTES pWorkingFolderAcl = NULL;

    if (!pCache->fInitializedBaseWorkingFolder)
    {
        // If elevated, allocate the pWorkingFolderAcl to protect the working folder to only SYSTEM and Admins.
        if (fElevated)
        {
            LPCWSTR wzSddl = L"D:PAI(A;;FA;;;BA)(A;OICIIO;GA;;;BA)(A;;FA;;;SY)(A;OICIIO;GA;;;SY)";
            if (!::ConvertStringSecurityDescriptorToSecurityDescriptorW(wzSddl, SDDL_REVISION_1, &psd, NULL))
            {
                ExitWithLastError(hr, "Failed to create the security descriptor for the working folder.");
            }

            pWorkingFolderAcl = reinterpret_cast<LPSECURITY_ATTRIBUTES>(MemAlloc(sizeof(SECURITY_ATTRIBUTES), TRUE));
            pWorkingFolderAcl->nLength = sizeof(SECURITY_ATTRIBUTES);
            pWorkingFolderAcl->lpSecurityDescriptor = psd;
            pWorkingFolderAcl->bInheritHandle = FALSE;
        }

        for (DWORD i = 0; i < pCache->cPotentialBaseWorkingFolders; ++i)
        {
            hr = PathConcatRelativeToFullyQualifiedBase(pCache->rgsczPotentialBaseWorkingFolders[i], pCache->wzGuid, &sczPotential);
            if (SUCCEEDED(hr))
            {
                hr = DirEnsureExists(sczPotential, pWorkingFolderAcl);
                if (SUCCEEDED(hr))
                {
                    pCache->sczBaseWorkingFolder = sczPotential;
                    sczPotential = NULL;
                    break;
                }
            }

            LogErrorId(hr, MSG_INVALID_BASE_WORKING_FOLDER, sczPotential, NULL, NULL);
        }

        ExitOnNull(pCache->sczBaseWorkingFolder, hr, E_INVALIDSTATE, "No usable base working folder found.");

        pCache->fInitializedBaseWorkingFolder = TRUE;
    }

    // Best effort to ensure our working folder is not encrypted.
    ::DecryptFileW(pCache->sczBaseWorkingFolder, 0);

    if (psczBaseWorkingFolder)
    {
        hr = StrAllocString(psczBaseWorkingFolder, pCache->sczBaseWorkingFolder, 0);
        ExitOnFailure(hr, "Failed to copy working folder.");
    }

LExit:
    ReleaseMem(pWorkingFolderAcl);
    if (psd)
    {
        ::LocalFree(psd);
    }
    ReleaseStr(sczPotential);

    return hr;
}

extern "C" HRESULT CacheCalculateBundleWorkingPath(
    __in BURN_CACHE* pCache,
    __in LPCWSTR wzExecutableName,
    __deref_out_z LPWSTR* psczWorkingPath
    )
{
    Assert(pCache->fInitializedCache);
    Assert(pCache->fInitializedBaseWorkingFolder);

    HRESULT hr = S_OK;

    // If the bundle is running out of the package cache then we use that as the
    // working folder since we feel safe in the package cache.
    if (CacheBundleRunningFromCache(pCache))
    {
        hr = PathForCurrentProcess(psczWorkingPath, NULL);
        ExitOnFailure(hr, "Failed to get current process path.");
    }
    else // Otherwise, use the real working folder.
    {
        hr = StrAllocFormatted(psczWorkingPath, L"%ls%ls\\%ls", pCache->sczBaseWorkingFolder, BUNDLE_WORKING_FOLDER_NAME, wzExecutableName);
        ExitOnFailure(hr, "Failed to calculate the bundle working path.");
    }

LExit:
    return hr;
}

extern "C" HRESULT CacheCalculateBundleLayoutWorkingPath(
    __in BURN_CACHE* pCache,
    __in_z LPCWSTR wzBundleId,
    __deref_out_z LPWSTR* psczWorkingPath
    )
{
    Assert(pCache->fInitializedCache);

    HRESULT hr = S_OK;

    hr = PathConcatRelativeToFullyQualifiedBase(pCache->sczAcquisitionFolder, wzBundleId, psczWorkingPath);
    ExitOnFailure(hr, "Failed to append bundle id for bundle layout working path.");

LExit:
    return hr;
}

extern "C" HRESULT CacheCalculatePayloadWorkingPath(
    __in BURN_CACHE* pCache,
    __in BURN_PAYLOAD* pPayload,
    __deref_out_z LPWSTR* psczWorkingPath
    )
{
    Assert(pCache->fInitializedCache);

    HRESULT hr = S_OK;

    hr = PathConcatRelativeToFullyQualifiedBase(pCache->sczAcquisitionFolder, pPayload->sczKey, psczWorkingPath);
    ExitOnFailure(hr, "Failed to append Id as payload unverified path.");

LExit:
    return hr;
}

extern "C" HRESULT CacheCalculateContainerWorkingPath(
    __in BURN_CACHE* pCache,
    __in BURN_CONTAINER* pContainer,
    __deref_out_z LPWSTR* psczWorkingPath
    )
{
    Assert(pCache->fInitializedCache);

    HRESULT hr = S_OK;

    hr = PathConcatRelativeToFullyQualifiedBase(pCache->sczAcquisitionFolder, pContainer->sczHash, psczWorkingPath);
    ExitOnFailure(hr, "Failed to append hash as container unverified path.");

LExit:
    return hr;
}

extern "C" HRESULT CacheGetPerMachineRootCompletedPath(
    __in BURN_CACHE* pCache,
    __out_z LPWSTR* psczCurrentRootCompletedPath,
    __out_z LPWSTR* psczDefaultRootCompletedPath
    )
{
    HRESULT hr = S_OK;

    *psczCurrentRootCompletedPath = NULL;
    *psczDefaultRootCompletedPath = NULL;

    hr = SecurePerMachineCacheRoot(pCache);
    ExitOnFailure(hr, "Failed to secure per-machine cache root.");

    hr = GetRootPath(pCache, TRUE, TRUE, psczCurrentRootCompletedPath);
    ExitOnFailure(hr, "Failed to get per-machine cache root.");

    if (S_FALSE == hr)
    {
        hr = GetRootPath(pCache, TRUE, FALSE, psczDefaultRootCompletedPath);
        ExitOnFailure(hr, "Failed to get default per-machine cache root.");

        hr = S_FALSE;
    }

LExit:
    return hr;
}

extern "C" HRESULT CacheGetCompletedPath(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzCacheId,
    __deref_out_z LPWSTR* psczCompletedPath
    )
{
    HRESULT hr = S_OK;
    BOOL fRedirected = FALSE;
    LPWSTR sczRootPath = NULL;
    LPWSTR sczCurrentCompletedPath = NULL;
    LPWSTR sczDefaultCompletedPath = NULL;

    hr = GetRootPath(pCache, fPerMachine, TRUE, &sczRootPath);
    ExitOnFailure(hr, "Failed to get %hs package cache root directory.", fPerMachine ? "per-machine" : "per-user");

    // GetRootPath returns S_FALSE if the package cache is redirected elsewhere.
    fRedirected = S_FALSE == hr;

    hr = PathConcatRelativeToFullyQualifiedBase(sczRootPath, wzCacheId, &sczCurrentCompletedPath);
    ExitOnFailure(hr, "Failed to construct cache path.");

    hr = PathBackslashTerminate(&sczCurrentCompletedPath);
    ExitOnFailure(hr, "Failed to ensure cache path was backslash terminated.");

    // Return the old package cache directory if the new directory does not exist but the old directory does.
    // If neither package cache directory exists return the (possibly) redirected package cache directory.
    if (fRedirected && !DirExists(sczCurrentCompletedPath, NULL))
    {
        hr = GetRootPath(pCache, fPerMachine, FALSE, &sczRootPath);
        ExitOnFailure(hr, "Failed to get old %hs package cache root directory.", fPerMachine ? "per-machine" : "per-user");

        hr = PathConcatRelativeToFullyQualifiedBase(sczRootPath, wzCacheId, &sczDefaultCompletedPath);
        ExitOnFailure(hr, "Failed to construct cache path.");

        hr = PathBackslashTerminate(&sczDefaultCompletedPath);
        ExitOnFailure(hr, "Failed to ensure cache path was backslash terminated.");

        if (DirExists(sczDefaultCompletedPath, NULL))
        {
            *psczCompletedPath = sczDefaultCompletedPath;
            sczDefaultCompletedPath = NULL;

            ExitFunction();
        }
    }

    *psczCompletedPath = sczCurrentCompletedPath;
    sczCurrentCompletedPath = NULL;

LExit:
    ReleaseNullStr(sczDefaultCompletedPath);
    ReleaseNullStr(sczCurrentCompletedPath);
    ReleaseNullStr(sczRootPath);

    return hr;
}

extern "C" HRESULT CacheGetResumePath(
    __in_z LPCWSTR wzPayloadWorkingPath,
    __deref_out_z LPWSTR* psczResumePath
    )
{
    HRESULT hr = S_OK;

    hr = StrAllocFormatted(psczResumePath, L"%ls.R", wzPayloadWorkingPath);
    ExitOnFailure(hr, "Failed to create resume path.");

LExit:
    return hr;
}

extern "C" HRESULT CacheGetLocalSourcePaths(
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
    )
{
    AssertSz(pCache->fInitializedCacheSources, "Cache sources weren't initialized");

    HRESULT hr = S_OK;
    LPWSTR sczCurrentPath = NULL;
    LPWSTR sczLastSourceFolder = NULL;
    LPWSTR* psczPath = NULL;
    BOOL fPreferSourcePathLocation = FALSE;
    BOOL fTryLastFolder = FALSE;
    BOOL fTryRelativePath = FALSE;
    BOOL fSourceIsAbsolute = FALSE;
    DWORD cSearchPaths = 0;
    DWORD dwLikelySearchPath = 0;
    DWORD dwDestinationSearchPath = 0;

    hr = GetLastUsedSourceFolder(pVariables, &sczLastSourceFolder);
    fPreferSourcePathLocation = !pCache->fRunningFromCache || FAILED(hr);
    fTryLastFolder = SUCCEEDED(hr) && sczLastSourceFolder && *sczLastSourceFolder && CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, pCache->sczSourceProcessFolder, -1, sczLastSourceFolder, -1);
    fTryRelativePath = CSTR_EQUAL != ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, wzSourcePath, -1, wzRelativePath, -1);
    fSourceIsAbsolute = PathIsRooted(wzSourcePath);

    // If the source path provided is a full path, try that first.
    if (fSourceIsAbsolute)
    {
        hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgSearchPaths), cSearchPaths + 1, sizeof(LPWSTR), BURN_CACHE_MAX_SEARCH_PATHS);
        ExitOnFailure(hr, "Failed to ensure size for search paths array.");

        psczPath = *prgSearchPaths + cSearchPaths;
        ++cSearchPaths;

        hr = StrAllocString(psczPath, wzSourcePath, 0);
        ExitOnFailure(hr, "Failed to copy absolute source path.");
    }
    else
    {
        // If none of the paths exist, then most BAs will want to prompt the user with a possible path.
        // The destination path is a temporary location and so not really a possible path.
        dwLikelySearchPath = 1;
    }

    // Try the destination path next.
    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgSearchPaths), cSearchPaths + 1, sizeof(LPWSTR), BURN_CACHE_MAX_SEARCH_PATHS);
    ExitOnFailure(hr, "Failed to ensure size for search paths array.");

    dwDestinationSearchPath = cSearchPaths;
    psczPath = *prgSearchPaths + cSearchPaths;
    ++cSearchPaths;

    hr = StrAllocString(psczPath, wzDestinationPath, 0);
    ExitOnFailure(hr, "Failed to copy absolute source path.");

    if (!fSourceIsAbsolute)
    {
        // Calculate the source path location.
        // In the case where we are in the bundle's package cache and
        // couldn't find a last used source that will be the package cache path
        // which isn't likely to have what we are looking for.
        hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgSearchPaths), cSearchPaths + 1, sizeof(LPWSTR), BURN_CACHE_MAX_SEARCH_PATHS);
        ExitOnFailure(hr, "Failed to ensure size for search paths array.");

        hr = PathConcat(pCache->sczSourceProcessFolder, wzSourcePath, &sczCurrentPath);
        ExitOnFailure(hr, "Failed to combine source process folder with source.");

        // If we're not running from cache or we couldn't get the last source,
        // try the source path location next.
        if (fPreferSourcePathLocation)
        {
            (*prgSearchPaths)[cSearchPaths] = sczCurrentPath;
            ++cSearchPaths;
            sczCurrentPath = NULL;
        }

        // If we have a last used source and it is not the source path location,
        // add the last used source to the search path next.
        if (fTryLastFolder)
        {
            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgSearchPaths), cSearchPaths + 1, sizeof(LPWSTR), BURN_CACHE_MAX_SEARCH_PATHS);
            ExitOnFailure(hr, "Failed to ensure size for search paths array.");

            psczPath = *prgSearchPaths + cSearchPaths;
            ++cSearchPaths;

            hr = PathConcat(sczLastSourceFolder, wzSourcePath, psczPath);
            ExitOnFailure(hr, "Failed to combine last source with source.");
        }

        if (!fPreferSourcePathLocation)
        {
            (*prgSearchPaths)[cSearchPaths] = sczCurrentPath;
            ++cSearchPaths;
            sczCurrentPath = NULL;
        }

        // Also consider the layout directory if doing Layout.
        if (wzLayoutDirectory)
        {
            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgSearchPaths), cSearchPaths + 1, sizeof(LPWSTR), BURN_CACHE_MAX_SEARCH_PATHS);
            ExitOnFailure(hr, "Failed to ensure size for search paths array.");

            psczPath = *prgSearchPaths + cSearchPaths;
            ++cSearchPaths;

            hr = PathConcat(wzLayoutDirectory, wzSourcePath, psczPath);
            ExitOnFailure(hr, "Failed to combine layout source with source.");
        }
    }

    if (fTryRelativePath)
    {
        hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgSearchPaths), cSearchPaths + 1, sizeof(LPWSTR), BURN_CACHE_MAX_SEARCH_PATHS);
        ExitOnFailure(hr, "Failed to ensure size for search paths array.");

        hr = PathConcat(pCache->sczSourceProcessFolder, wzRelativePath, &sczCurrentPath);
        ExitOnFailure(hr, "Failed to combine source process folder with relative.");

        if (fPreferSourcePathLocation)
        {
            (*prgSearchPaths)[cSearchPaths] = sczCurrentPath;
            ++cSearchPaths;
            sczCurrentPath = NULL;
        }

        if (fTryLastFolder)
        {
            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgSearchPaths), cSearchPaths + 1, sizeof(LPWSTR), BURN_CACHE_MAX_SEARCH_PATHS);
            ExitOnFailure(hr, "Failed to ensure size for search paths array.");

            psczPath = *prgSearchPaths + cSearchPaths;
            ++cSearchPaths;

            hr = PathConcat(sczLastSourceFolder, wzRelativePath, psczPath);
            ExitOnFailure(hr, "Failed to combine last source with relative.");
        }

        if (!fPreferSourcePathLocation)
        {
            (*prgSearchPaths)[cSearchPaths] = sczCurrentPath;
            ++cSearchPaths;
            sczCurrentPath = NULL;
        }

        if (wzLayoutDirectory)
        {
            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(prgSearchPaths), cSearchPaths + 1, sizeof(LPWSTR), BURN_CACHE_MAX_SEARCH_PATHS);
            ExitOnFailure(hr, "Failed to ensure size for search paths array.");

            psczPath = *prgSearchPaths + cSearchPaths;
            ++cSearchPaths;

            hr = PathConcat(wzLayoutDirectory, wzSourcePath, psczPath);
            ExitOnFailure(hr, "Failed to combine layout source with relative.");
        }
    }

LExit:
    ReleaseStr(sczCurrentPath);
    ReleaseStr(sczLastSourceFolder);

    AssertSz(cSearchPaths <= BURN_CACHE_MAX_SEARCH_PATHS, "Got more than BURN_CACHE_MAX_SEARCH_PATHS search paths");
    *pcSearchPaths = cSearchPaths;
    *pdwLikelySearchPath = dwLikelySearchPath;
    *pdwDestinationSearchPath = dwDestinationSearchPath;

    return hr;
}

extern "C" HRESULT CacheSetLastUsedSource(
    __in BURN_VARIABLES* pVariables,
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzRelativePath
    )
{
    HRESULT hr = S_OK;
    size_t cchSourcePath = 0;
    size_t cchRelativePath = 0;
    size_t iSourceRelativePath = 0;
    LPWSTR sczSourceFolder = NULL;
    LPWSTR sczLastSourceFolder = NULL;
    int nCompare = 0;

    hr = ::StringCchLengthW(wzSourcePath, STRSAFE_MAX_CCH, &cchSourcePath);
    ExitOnFailure(hr, "Failed to determine length of source path.");

    hr = ::StringCchLengthW(wzRelativePath, STRSAFE_MAX_CCH, &cchRelativePath);
    ExitOnFailure(hr, "Failed to determine length of relative path.");

    // If the source path is smaller than the relative path (plus space for "X:\") then we know they
    // are not relative to each other.
    if (cchSourcePath < cchRelativePath + 3)
    {
        ExitFunction();
    }

    // If the source path ends with the relative path then this source could be a new path.
    iSourceRelativePath = cchSourcePath - cchRelativePath;
    if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, wzSourcePath + iSourceRelativePath, -1, wzRelativePath, -1))
    {
        hr = StrAllocString(&sczSourceFolder, wzSourcePath, iSourceRelativePath);
        ExitOnFailure(hr, "Failed to trim source folder.");

        hr = VariableGetString(pVariables, BURN_BUNDLE_LAST_USED_SOURCE, &sczLastSourceFolder);
        if (SUCCEEDED(hr))
        {
            nCompare = ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, sczSourceFolder, -1, sczLastSourceFolder, -1);
        }
        else if (E_NOTFOUND == hr)
        {
            nCompare = CSTR_GREATER_THAN;
            hr = S_OK;
        }

        if (CSTR_EQUAL != nCompare)
        {
            hr = VariableSetString(pVariables, BURN_BUNDLE_LAST_USED_SOURCE, sczSourceFolder, FALSE, FALSE);
            ExitOnFailure(hr, "Failed to set last source.");
        }
    }

LExit:
    ReleaseStr(sczLastSourceFolder);
    ReleaseStr(sczSourceFolder);

    return hr;
}

extern "C" HRESULT CacheSendProgressCallback(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in DWORD64 dw64Progress,
    __in DWORD64 dw64Total,
    __in HANDLE hDestinationFile
    )
{
    static LARGE_INTEGER LARGE_INTEGER_ZERO = { };

    HRESULT hr = S_OK;
    DWORD dwResult = PROGRESS_CONTINUE;
    LARGE_INTEGER liTotalSize = { };
    LARGE_INTEGER liTotalTransferred = { };

    if (pCallback->pfnProgress)
    {
        liTotalSize.QuadPart = dw64Total;
        liTotalTransferred.QuadPart = dw64Progress;

        dwResult = (*pCallback->pfnProgress)(liTotalSize, liTotalTransferred, LARGE_INTEGER_ZERO, LARGE_INTEGER_ZERO, 1, CALLBACK_CHUNK_FINISHED, INVALID_HANDLE_VALUE, hDestinationFile, pCallback->pv);
        switch (dwResult)
        {
        case PROGRESS_CONTINUE:
            hr = S_OK;
            break;

        case PROGRESS_CANCEL: __fallthrough; // TODO: should cancel and stop be treated differently?
        case PROGRESS_STOP:
            hr = HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT);
            ExitOnRootFailure(hr, "UX aborted on download progress.");

        case PROGRESS_QUIET: // Not actually an error, just an indication to the caller to stop requesting progress.
            pCallback->pfnProgress = NULL;
            hr = S_OK;
            break;

        default:
            hr = E_UNEXPECTED;
            ExitOnRootFailure(hr, "Invalid return code from progress routine.");
        }
    }

LExit:
    return hr;
}

extern "C" void CacheSendErrorCallback(
    __in DOWNLOAD_CACHE_CALLBACK* pCallback,
    __in HRESULT hrError,
    __in_z_opt LPCWSTR wzError,
    __out_opt BOOL* pfRetry
    )
{
    if (pfRetry)
    {
        *pfRetry = FALSE;
    }

    if (pCallback->pfnCancel)
    {
        int nResult = (*pCallback->pfnCancel)(hrError, wzError, pfRetry != NULL, pCallback->pv);
        if (pfRetry && IDRETRY == nResult)
        {
            *pfRetry = TRUE;
        }
    }
}

extern "C" BOOL CacheBundleRunningFromCache(
    __in BURN_CACHE* pCache
    )
{
    AssertSz(pCache->fInitializedCacheSources, "Cache sources weren't initialized");

    return pCache->fRunningFromCache;
}

extern "C" HRESULT CachePreparePackage(
    __in BURN_CACHE* pCache,
    __in BURN_PACKAGE* pPackage
    )
{
    HRESULT hr = S_OK;

    if (!pPackage->sczCacheFolder)
    {
        hr = CreateCompletedPath(pCache, pPackage->fPerMachine, pPackage->sczCacheId, NULL, &pPackage->sczCacheFolder);
    }

    return hr;
}

extern "C" HRESULT CacheBundleToCleanRoom(
    __in BOOL fElevated,
    __in BURN_CACHE* pCache,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczCleanRoomBundlePath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczSourcePath = NULL;
    LPWSTR wzExecutableName = NULL;

    hr = PathForCurrentProcess(&sczSourcePath, NULL);
    ExitOnFailure(hr, "Failed to get current path for process to cache to clean room.");

    wzExecutableName = PathFile(sczSourcePath);

    hr = CopyEngineToWorkingFolder(fElevated, pCache, sczSourcePath, BUNDLE_CLEAN_ROOM_WORKING_FOLDER_NAME, wzExecutableName, pSection, psczCleanRoomBundlePath);
    ExitOnFailure(hr, "Failed to cache bundle to clean room.");

LExit:
    ReleaseStr(sczSourcePath);

    return hr;
}

extern "C" HRESULT CacheBundleToWorkingDirectory(
    __in BOOL fElevated,
    __in BURN_CACHE* pCache,
    __in_z LPCWSTR wzExecutableName,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczEngineWorkingPath
    )
{
    Assert(pCache->fInitializedCache);

    HRESULT hr = S_OK;
    LPWSTR sczSourcePath = NULL;

    // Initialize the source.
    hr = PathForCurrentProcess(&sczSourcePath, NULL);
    ExitOnFailure(hr, "Failed to get current process path.");

    // If the bundle is running out of the package cache then we don't need to copy it to
    // the working folder since we feel safe in the package cache and will run from there.
    if (CacheBundleRunningFromCache(pCache))
    {
        hr = StrAllocString(psczEngineWorkingPath, sczSourcePath, 0);
        ExitOnFailure(hr, "Failed to use current process path as target path.");
    }
    else // otherwise, carry on putting the bundle in the working folder.
    {
        hr = CopyEngineToWorkingFolder(fElevated, pCache, sczSourcePath, BUNDLE_WORKING_FOLDER_NAME, wzExecutableName, pSection, psczEngineWorkingPath);
        ExitOnFailure(hr, "Failed to copy engine to working folder.");
    }

LExit:
    ReleaseStr(sczSourcePath);

    return hr;
}

extern "C" HRESULT CacheLayoutBundle(
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzSourceBundlePath,
    __in DWORD64 qwBundleSize,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczTargetPath = NULL;

    hr = PathConcatRelativeToFullyQualifiedBase(wzLayoutDirectory, wzExecutableName, &sczTargetPath);
    ExitOnFailure(hr, "Failed to combine completed path with engine file name for layout.");

    LogStringLine(REPORT_STANDARD, "Layout bundle from: '%ls' to: '%ls'", wzSourceBundlePath, sczTargetPath);

    hr = CacheTransferFileWithRetry(wzSourceBundlePath, sczTargetPath, TRUE, BURN_CACHE_STEP_FINALIZE, qwBundleSize, pfnCacheMessageHandler, pfnProgress, pContext);
    ExitOnFailure(hr, "Failed to layout bundle from: '%ls' to '%ls'", wzSourceBundlePath, sczTargetPath);

LExit:
    ReleaseStr(sczTargetPath);

    return hr;
}

extern "C" HRESULT CacheCompleteBundle(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzExecutableName,
    __in_z LPCWSTR wzBundleId,
    __in_z LPCWSTR wzSourceBundlePath
#ifdef DEBUG
    , __in_z LPCWSTR wzExecutablePath
#endif
    )
{
    HRESULT hr = S_OK;
    BOOL fPathEqual = FALSE;
    LPWSTR sczTargetDirectory = NULL;
    LPWSTR sczTargetPath = NULL;

    hr = CreateCompletedPath(pCache, fPerMachine, wzBundleId, NULL, &sczTargetDirectory);
    ExitOnFailure(hr, "Failed to create completed cache path for bundle.");

    hr = PathConcatRelativeToFullyQualifiedBase(sczTargetDirectory, wzExecutableName, &sczTargetPath);
    ExitOnFailure(hr, "Failed to combine completed path with engine file name.");

    // We can't just use wzExecutablePath because we needed to call CreateCompletedPath to ensure that the destination was secured.
    Assert(CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, NORM_IGNORECASE, wzExecutablePath, -1, sczTargetPath, -1));

    // If the bundle is running out of the package cache then we don't need to copy it there
    // (and don't want to since it'll be in use) so bail.
    hr = PathCompareCanonicalized(wzSourceBundlePath, sczTargetPath, &fPathEqual);
    ExitOnFailure(hr, "Failed to compare completed cache path for bundle: %ls", wzSourceBundlePath);

    if (fPathEqual)
    {
        ExitFunction();
    }

    // Otherwise, carry on putting the bundle in the cache.
    LogStringLine(REPORT_STANDARD, "Caching bundle from: '%ls' to: '%ls'", wzSourceBundlePath, sczTargetPath);

    FileRemoveFromPendingRename(sczTargetPath); // best effort to ensure bundle is not deleted from cache post restart.

    hr = FileEnsureCopyWithRetry(wzSourceBundlePath, sczTargetPath, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
    if (FAILED(hr) && FileExistsEx(sczTargetPath, NULL))
    {
        LogId(REPORT_WARNING, MSG_IGNORING_CACHE_BUNDLE_FAILURE, hr);
        ExitFunction1(hr = S_OK);
    }
    ExitOnFailure(hr, "Failed to cache bundle from: '%ls' to '%ls'", wzSourceBundlePath, sczTargetPath);

    // Reset the path permissions in the cache.
    hr = ResetPathPermissions(fPerMachine, sczTargetPath);
    ExitOnFailure(hr, "Failed to reset permissions on cached bundle: '%ls'", sczTargetPath);

LExit:
    ReleaseStr(sczTargetPath);
    ReleaseStr(sczTargetDirectory);

    return hr;
}

extern "C" HRESULT CacheLayoutContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzUnverifiedContainerPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachedPath = NULL;

    hr = PathConcatRelativeToFullyQualifiedBase(wzLayoutDirectory, pContainer->sczFilePath, &sczCachedPath);
    ExitOnFailure(hr, "Failed to concat complete cached path.");

    hr = VerifyThenTransferContainer(pContainer, sczCachedPath, wzUnverifiedContainerPath, fMove, pfnCacheMessageHandler, pfnProgress, pContext);
    ExitOnFailure(hr, "Failed to layout container from cached path: %ls", sczCachedPath);

LExit:
    ReleaseStr(sczCachedPath);

    return hr;
}

extern "C" HRESULT CacheLayoutPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z_opt LPCWSTR wzLayoutDirectory,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachedPath = NULL;

    hr = PathConcatRelativeToFullyQualifiedBase(wzLayoutDirectory, pPayload->sczFilePath, &sczCachedPath);
    ExitOnFailure(hr, "Failed to concat complete cached path.");

    hr = VerifyThenTransferPayload(pPayload, sczCachedPath, wzUnverifiedPayloadPath, fMove, pfnCacheMessageHandler, pfnProgress, pContext);
    ExitOnFailure(hr, "Failed to layout payload from cached payload: %ls", sczCachedPath);

LExit:
    ReleaseStr(sczCachedPath);

    return hr;
}

extern "C" HRESULT CacheCompletePayload(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzCacheId,
    __in_z LPCWSTR wzWorkingPayloadPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachedPath = NULL;
    LPWSTR sczUnverifiedPayloadPath = NULL;

    hr = CreateCompletedPath(pCache, fPerMachine, wzCacheId, pPayload->sczFilePath, &sczCachedPath);
    ExitOnFailure(hr, "Failed to get cached path for package with cache id: %ls", wzCacheId);

    // If the cached file matches what we expected, we're good.
    hr = VerifyFileAgainstPayload(pPayload, sczCachedPath, TRUE, BURN_CACHE_STEP_HASH_TO_SKIP_VERIFY, pfnCacheMessageHandler, pfnProgress, pContext);
    if (SUCCEEDED(hr))
    {
        ExitFunction();
    }

    hr = CreateUnverifiedPath(pCache, fPerMachine, pPayload->sczKey, &sczUnverifiedPayloadPath);
    ExitOnFailure(hr, "Failed to create unverified path.");

    // If the working path exists, let's get it into the unverified path so we can reset the ACLs and verify the file.
    if (FileExistsEx(wzWorkingPayloadPath, NULL))
    {
        hr = CacheTransferFileWithRetry(wzWorkingPayloadPath, sczUnverifiedPayloadPath, fMove, BURN_CACHE_STEP_STAGE, pPayload->qwFileSize, pfnCacheMessageHandler, pfnProgress, pContext);
        ExitOnFailure(hr, "Failed to transfer working path to unverified path for payload: %ls.", pPayload->sczKey);
    }
    else if (FileExistsEx(sczUnverifiedPayloadPath, NULL))
    {
        // Make sure the staging progress is sent even though there was nothing to do.
        hr = SendCacheBeginMessage(pfnCacheMessageHandler, pContext, BURN_CACHE_STEP_STAGE);
        if (SUCCEEDED(hr))
        {
            hr = SendCacheSuccessMessage(pfnCacheMessageHandler, pContext, pPayload->qwFileSize);
        }
        SendCacheCompleteMessage(pfnCacheMessageHandler, pContext, hr);
        ExitOnFailure(hr, "Aborted transferring working path to unverified path for payload: %ls.", pPayload->sczKey);
    }
    else // if the working path and unverified path do not exist, nothing we can do.
    {
        ExitWithRootFailure(hr, E_FILENOTFOUND, "Failed to find payload: %ls in working path: %ls and unverified path: %ls", pPayload->sczKey, wzWorkingPayloadPath, sczUnverifiedPayloadPath);
    }

    hr = ResetPathPermissions(fPerMachine, sczUnverifiedPayloadPath);
    ExitOnFailure(hr, "Failed to reset permissions on unverified cached payload: %ls", pPayload->sczKey);

    hr = VerifyFileAgainstPayload(pPayload, sczUnverifiedPayloadPath, FALSE, BURN_CACHE_STEP_HASH, pfnCacheMessageHandler, pfnProgress, pContext);
    LogExitOnFailure(hr, MSG_FAILED_VERIFY_PAYLOAD, "Failed to verify payload: %ls at path: %ls", pPayload->sczKey, sczUnverifiedPayloadPath, NULL);

    LogId(REPORT_STANDARD, MSG_VERIFIED_ACQUIRED_PAYLOAD, pPayload->sczKey, sczUnverifiedPayloadPath, fMove ? "moving" : "copying", sczCachedPath);

    hr = CacheTransferFileWithRetry(sczUnverifiedPayloadPath, sczCachedPath, TRUE, BURN_CACHE_STEP_FINALIZE, pPayload->qwFileSize, pfnCacheMessageHandler, pfnProgress, pContext);
    ExitOnFailure(hr, "Failed to move verified file to complete payload path: %ls", sczCachedPath);

    ::DecryptFileW(sczCachedPath, 0);  // Let's try to make sure it's not encrypted.

LExit:
    ReleaseStr(sczUnverifiedPayloadPath);
    ReleaseStr(sczCachedPath);

    return hr;
}

extern "C" HRESULT CacheVerifyContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzCachedDirectory,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachedPath = NULL;

    hr = PathConcatRelativeToFullyQualifiedBase(wzCachedDirectory, pContainer->sczFilePath, &sczCachedPath);
    ExitOnFailure(hr, "Failed to concat complete cached path.");

    hr = VerifyFileAgainstContainer(pContainer, sczCachedPath, TRUE, BURN_CACHE_STEP_HASH_TO_SKIP_ACQUIRE, pfnCacheMessageHandler, pfnProgress, pContext);

LExit:
    ReleaseStr(sczCachedPath);

    return hr;
}

extern "C" HRESULT CacheVerifyPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzCachedDirectory,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCachedPath = NULL;

    hr = PathConcatRelativeToFullyQualifiedBase(wzCachedDirectory, pPayload->sczFilePath, &sczCachedPath);
    ExitOnFailure(hr, "Failed to concat complete cached path.");

    hr = VerifyFileAgainstPayload(pPayload, sczCachedPath, TRUE, BURN_CACHE_STEP_HASH_TO_SKIP_ACQUIRE, pfnCacheMessageHandler, pfnProgress, pContext);

LExit:
    ReleaseStr(sczCachedPath);

    return hr;
}

extern "C" HRESULT CacheRemoveBaseWorkingFolder(
    __in BURN_CACHE* pCache
    )
{
    HRESULT hr = S_OK;

    if (pCache->fInitializedBaseWorkingFolder)
    {
        // Try to clean out everything in the working folder.
        hr = DirEnsureDeleteEx(pCache->sczBaseWorkingFolder, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
        TraceError(hr, "Could not delete bundle engine working folder.");
    }

    return hr;
}

extern "C" HRESULT CacheRemoveBundle(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzBundleId
    )
{
    HRESULT hr = S_OK;

    hr = RemoveBundleOrPackage(pCache, TRUE, fPerMachine, wzBundleId, wzBundleId);
    ExitOnFailure(hr, "Failed to remove bundle id: %ls.", wzBundleId);

LExit:
    return hr;
}

extern "C" HRESULT CacheRemovePackage(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPackageId,
    __in_z LPCWSTR wzCacheId
    )
{
    HRESULT hr = S_OK;

    hr = RemoveBundleOrPackage(pCache, FALSE, fPerMachine, wzPackageId, wzCacheId);
    ExitOnFailure(hr, "Failed to remove package id: %ls.", wzPackageId);

LExit:
    return hr;
}

static HRESULT CacheVerifyPayloadSignature(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile,
    __in BURN_CACHE_STEP cacheStep,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE /*pfnProgress*/,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    LONG er = ERROR_SUCCESS;
    BOOL fFailedVerification = FALSE;

    GUID guidAuthenticode = WINTRUST_ACTION_GENERIC_VERIFY_V2;
    WINTRUST_FILE_INFO wfi = { };
    WINTRUST_DATA wtd = { };
    CRYPT_PROVIDER_DATA* pProviderData = NULL;
    CRYPT_PROVIDER_SGNR* pSigner = NULL;

    hr = SendCacheBeginMessage(pfnCacheMessageHandler, pContext, cacheStep);
    ExitOnFailure(hr, "Aborted cache verify payload signature begin.");

    fFailedVerification = TRUE;

    // Verify the payload assuming online.
    wfi.cbStruct = sizeof(wfi);
    wfi.pcwszFilePath = wzUnverifiedPayloadPath;
    wfi.hFile = hFile;

    wtd.cbStruct = sizeof(wtd);
    wtd.dwUnionChoice = WTD_CHOICE_FILE;
    wtd.pFile = &wfi;
    wtd.dwStateAction = WTD_STATEACTION_VERIFY;
    wtd.dwProvFlags = WTD_REVOCATION_CHECK_CHAIN_EXCLUDE_ROOT;
    wtd.dwUIChoice = WTD_UI_NONE;

    er = ::WinVerifyTrust(static_cast<HWND>(INVALID_HANDLE_VALUE), &guidAuthenticode, &wtd);
    if (er)
    {
        // Verify the payload assuming offline.
        wtd.dwProvFlags |= WTD_CACHE_ONLY_URL_RETRIEVAL;

        er = ::WinVerifyTrust(static_cast<HWND>(INVALID_HANDLE_VALUE), &guidAuthenticode, &wtd);
        ExitOnWin32Error(er, hr, "Failed authenticode verification of payload: %ls", wzUnverifiedPayloadPath);
    }

    pProviderData = ::WTHelperProvDataFromStateData(wtd.hWVTStateData);
    ExitOnNullWithLastError(pProviderData, hr, "Failed to get provider state from authenticode certificate.");

    pSigner = ::WTHelperGetProvSignerFromChain(pProviderData, 0, FALSE, 0);
    ExitOnNullWithLastError(pSigner, hr, "Failed to get signer chain from authenticode certificate.");

    hr = VerifyPayloadAgainstCertChain(pPayload, pSigner->pChainContext);
    ExitOnFailure(hr, "Failed to verify expected payload against actual certificate chain.");

    fFailedVerification = FALSE;

    hr = SendCacheSuccessMessage(pfnCacheMessageHandler, pContext, pPayload->qwFileSize);

LExit:
    if (fFailedVerification)
    {
        // Make sure the BA process marks this payload as having failed verification.
        SendCacheFailureMessage(pfnCacheMessageHandler, pContext, cacheStep);
    }

    SendCacheCompleteMessage(pfnCacheMessageHandler, pContext, hr);

    return hr;
}

extern "C" void CacheCleanup(
    __in BOOL fPerMachine,
    __in BURN_CACHE* pCache
    )
{
    Assert(pCache->fInitializedCache);

    HRESULT hr = S_OK;
    LPWSTR sczFolder = NULL;
    LPWSTR sczFiles = NULL;
    LPWSTR sczDelete = NULL;
    HANDLE hFind = INVALID_HANDLE_VALUE;
    WIN32_FIND_DATAW wfd = { };
    size_t cchFileName = 0;

    hr = CacheGetCompletedPath(pCache, fPerMachine, UNVERIFIED_CACHE_FOLDER_NAME, &sczFolder);
    if (SUCCEEDED(hr))
    {
        hr = DirEnsureDeleteEx(sczFolder, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
    }

    if (!fPerMachine)
    {
        if (pCache->sczAcquisitionFolder)
        {
            hr = PathConcat(pCache->sczAcquisitionFolder, L"*.*", &sczFiles);
            if (SUCCEEDED(hr))
            {
                hFind = ::FindFirstFileW(sczFiles, &wfd);
                if (INVALID_HANDLE_VALUE != hFind)
                {
                    do
                    {
                        // Skip directories.
                        if (wfd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
                        {
                            continue;
                        }

                        // Skip resume files (they end with ".R").
                        hr = ::StringCchLengthW(wfd.cFileName, MAX_PATH, &cchFileName);
                        if (FAILED(hr) ||
                            2 < cchFileName && L'.' == wfd.cFileName[cchFileName - 2] && (L'R' == wfd.cFileName[cchFileName - 1] || L'r' == wfd.cFileName[cchFileName - 1]))
                        {
                            continue;
                        }

                        hr = PathConcatCch(pCache->sczAcquisitionFolder, 0, wfd.cFileName, cchFileName, &sczDelete);
                        if (SUCCEEDED(hr))
                        {
                            hr = FileEnsureDelete(sczDelete);
                        }
                    } while (::FindNextFileW(hFind, &wfd));
                }
            }
        }
    }

    if (INVALID_HANDLE_VALUE != hFind)
    {
        ::FindClose(hFind);
    }

    ReleaseStr(sczDelete);
    ReleaseStr(sczFiles);
    ReleaseStr(sczFolder);
}

extern "C" void CacheUninitialize(
    __in BURN_CACHE* pCache
    )
{
    ReleaseStrArray(pCache->rgsczPotentialBaseWorkingFolders, pCache->cPotentialBaseWorkingFolders);
    ReleaseStr(pCache->sczCurrentMachinePackageCache);
    ReleaseStr(pCache->sczDefaultMachinePackageCache);
    ReleaseStr(pCache->sczDefaultUserPackageCache);
    ReleaseStr(pCache->sczBaseWorkingFolder);
    ReleaseStr(pCache->sczAcquisitionFolder);
    ReleaseStr(pCache->sczSourceProcessFolder);

    memset(pCache, 0, sizeof(BURN_CACHE));
}

// Internal functions.

static HRESULT CalculatePotentialBaseWorkingFolders(
    __in BURN_CACHE* pCache,
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in LPCWSTR wzAcquisitionFolder
    )
{
    Assert(!pCache->rgsczPotentialBaseWorkingFolders && !pCache->cPotentialBaseWorkingFolders);
    HRESULT hr = S_OK;
    LPWSTR sczTemp = NULL;
    LPWSTR sczPolicy = NULL;
    BOOL fNeedsExpansion = FALSE;

    hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pCache->rgsczPotentialBaseWorkingFolders), 6, sizeof(LPWSTR), 6);
    ExitOnFailure(hr, "Failed to initialize array.");

    // The value from the command line takes precedence.
    if (pInternalCommand->sczEngineWorkingDirectory)
    {
        hr = PathExpand(&sczTemp, pInternalCommand->sczEngineWorkingDirectory, PATH_EXPAND_FULLPATH);
        ExitOnFailure(hr, "Failed to expand engine working directory from command-line: '%ls'", pInternalCommand->sczEngineWorkingDirectory);

        pCache->rgsczPotentialBaseWorkingFolders[pCache->cPotentialBaseWorkingFolders] = sczTemp;
        sczTemp = NULL;
        ++pCache->cPotentialBaseWorkingFolders;
    }

    // The base working folder can be specified through policy,
    // but only use it if elevated because it should be secured against non-admin users.
    if (pInternalCommand->fInitiallyElevated)
    {
        hr = PolcReadUnexpandedString(POLICY_BURN_REGISTRY_PATH, L"EngineWorkingDirectory", NULL, &fNeedsExpansion, &sczPolicy);
        ExitOnFailure(hr, "Failed to read EngineWorkingDirectory policy directory.");

        if (S_FALSE != hr)
        {
            if (fNeedsExpansion)
            {
                hr = EnvExpandEnvironmentStringsForUser(NULL, sczPolicy, &sczTemp, NULL);
                ExitOnFailure(hr, "Failed to expand EngineWorkingDirectory policy directory.");
            }
            else
            {
                sczTemp = sczPolicy;
                sczPolicy = NULL;
            }

            pCache->rgsczPotentialBaseWorkingFolders[pCache->cPotentialBaseWorkingFolders] = sczTemp;
            sczTemp = NULL;
            ++pCache->cPotentialBaseWorkingFolders;
        }
    }

    // Default to the acquisition folder, but need to use system temp path for security reasons if running elevated.
    if (pInternalCommand->fInitiallyElevated)
    {
        hr = PathGetSystemTempPaths(&pCache->rgsczPotentialBaseWorkingFolders, &pCache->cPotentialBaseWorkingFolders);
        ExitOnFailure(hr, "Failed to get system temp folder paths for base working folder.");
    }
    else
    {
        hr = StrAllocString(&sczTemp, wzAcquisitionFolder, 0);
        ExitOnFailure(hr, "Failed to copy acquisition folder path for base working folder.");

        pCache->rgsczPotentialBaseWorkingFolders[pCache->cPotentialBaseWorkingFolders] = sczTemp;
        sczTemp = NULL;
        ++pCache->cPotentialBaseWorkingFolders;
    }

LExit:
    ReleaseStr(sczTemp);
    ReleaseStr(sczPolicy);

    return hr;
}

static HRESULT CalculateWorkingFolders(
    __in BURN_CACHE* pCache,
    __in BURN_ENGINE_COMMAND* pInternalCommand
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczBaseAcquisitionPath = NULL;

    hr = PathGetTempPath(&sczBaseAcquisitionPath, NULL);
    ExitOnFailure(hr, "Failed to get temp folder path for acquisition folder base.");

    hr = PathBackslashTerminate(&sczBaseAcquisitionPath);
    ExitOnFailure(hr, "Failed to backslashify base engine working directory.");

    hr = CalculatePotentialBaseWorkingFolders(pCache, pInternalCommand, sczBaseAcquisitionPath);
    ExitOnFailure(hr, "Failed to get potential base engine working directories.");

    hr = GuidFixedCreate(pCache->wzGuid);
    ExitOnFailure(hr, "Failed to create working folder guid.");

    pCache->wzGuid[GUID_STRING_LENGTH - 1] = L'\\';
    pCache->wzGuid[GUID_STRING_LENGTH] = L'\0';

    hr = PathConcatRelativeToFullyQualifiedBase(sczBaseAcquisitionPath, pCache->wzGuid, &pCache->sczAcquisitionFolder);
    ExitOnFailure(hr, "Failed to append random guid on to temp path for acquisition folder.");

LExit:
    ReleaseStr(sczBaseAcquisitionPath);

    return hr;
}

static HRESULT GetRootPath(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in BOOL fAllowRedirect,
    __deref_out_z LPWSTR* psczRootPath
    )
{
    Assert(pCache->fInitializedCache);

    HRESULT hr = S_OK;

    if (fPerMachine)
    {
        BOOL fRedirect = fAllowRedirect && pCache->fCustomMachinePackageCache;

        hr = StrAllocString(psczRootPath, fRedirect ? pCache->sczCurrentMachinePackageCache : pCache->sczDefaultMachinePackageCache, 0);
        ExitOnFailure(hr, "Failed to copy %hs package cache root directory.", "per-machine");

        // Return S_FALSE if the current location is not the default location (redirected).
        hr = fRedirect ? S_FALSE : S_OK;
    }
    else
    {
        hr = StrAllocString(psczRootPath, pCache->sczDefaultUserPackageCache, 0);
        ExitOnFailure(hr, "Failed to copy %hs package cache root directory.", "per-user");
    }

LExit:
    return hr;
}

static HRESULT GetLastUsedSourceFolder(
    __in BURN_VARIABLES* pVariables,
    __out_z LPWSTR* psczLastSource
    )
{
    HRESULT hr = S_OK;

    hr = VariableGetString(pVariables, BURN_BUNDLE_LAST_USED_SOURCE, psczLastSource);
    if (E_NOTFOUND == hr)
    {
        // Try the original source folder.
        hr = VariableGetString(pVariables, BURN_BUNDLE_ORIGINAL_SOURCE_FOLDER, psczLastSource);
    }

    return hr;
}

static HRESULT SecurePerMachineCacheRoot(
    __in BURN_CACHE* pCache
    )
{
    HRESULT hr = S_OK;
    BOOL fRedirected = FALSE;
    LPWSTR sczCacheDirectory = NULL;

    if (!pCache->fPerMachineCacheRootVerified)
    {
        // If we are doing a permachine install but have not yet verified that the root cache folder
        // was created with the correct ACLs yet, do that now.
        hr = GetRootPath(pCache, TRUE, TRUE, &sczCacheDirectory);
        ExitOnFailure(hr, "Failed to get cache directory.");

        fRedirected = S_FALSE == hr;

        hr = DirEnsureExists(sczCacheDirectory, NULL);
        ExitOnFailure(hr, "Failed to create cache directory: %ls", sczCacheDirectory);

        hr = SecurePath(sczCacheDirectory);
        ExitOnFailure(hr, "Failed to secure cache directory: %ls", sczCacheDirectory);

        pCache->fPerMachineCacheRootVerified = TRUE;

        if (!fRedirected)
        {
            pCache->fOriginalPerMachineCacheRootVerified = TRUE;
        }
    }

    if (!pCache->fOriginalPerMachineCacheRootVerified)
    {
        // If we are doing a permachine install but have not yet verified that the original root cache folder
        // was created with the correct ACLs yet, do that now.
        hr = GetRootPath(pCache, TRUE, FALSE, &sczCacheDirectory);
        ExitOnFailure(hr, "Failed to get original cache directory.");

        hr = DirEnsureExists(sczCacheDirectory, NULL);
        ExitOnFailure(hr, "Failed to create original cache directory: %ls", sczCacheDirectory);

        hr = SecurePath(sczCacheDirectory);
        ExitOnFailure(hr, "Failed to secure original cache directory: %ls", sczCacheDirectory);

        pCache->fOriginalPerMachineCacheRootVerified = TRUE;
    }

LExit:
    ReleaseStr(sczCacheDirectory);

    return hr;
}

static HRESULT CreateCompletedPath(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in LPCWSTR wzId,
    __in LPCWSTR wzFilePath,
    __out_z LPWSTR* psczCachePath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCacheDirectory = NULL;
    LPWSTR sczCacheFile = NULL;

    if (fPerMachine)
    {
        hr = SecurePerMachineCacheRoot(pCache);
        ExitOnFailure(hr, "Failed to secure per-machine cache root.");
    }

    // Get the cache completed path.
    hr = CacheGetCompletedPath(pCache, fPerMachine, wzId, &sczCacheDirectory);
    ExitOnFailure(hr, "Failed to get cache directory.");

    // Ensure it exists.
    hr = DirEnsureExists(sczCacheDirectory, NULL);
    ExitOnFailure(hr, "Failed to create cache directory: %ls", sczCacheDirectory);

    if (!wzFilePath)
    {
        // Reset any permissions people might have tried to set on the directory
        // so we inherit the (correct!) security permissions from the parent directory.
        ResetPathPermissions(fPerMachine, sczCacheDirectory);

        *psczCachePath = sczCacheDirectory;
        sczCacheDirectory = NULL;
    }
    else
    {
        // Get the cache completed file path.
        hr = PathConcatRelativeToFullyQualifiedBase(sczCacheDirectory, wzFilePath, &sczCacheFile);
        ExitOnFailure(hr, "Failed to construct cache file.");

        // Don't reset permissions here. The payload's package must reset its cache folder when it starts caching.

        *psczCachePath = sczCacheFile;
        sczCacheFile = NULL;
    }

LExit:
    ReleaseStr(sczCacheDirectory);
    ReleaseStr(sczCacheFile);
    return hr;
}

static HRESULT CreateUnverifiedPath(
    __in BURN_CACHE* pCache,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPayloadId,
    __out_z LPWSTR* psczUnverifiedPayloadPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczUnverifiedCacheFolder = NULL;

    hr = CacheGetCompletedPath(pCache, fPerMachine, UNVERIFIED_CACHE_FOLDER_NAME, &sczUnverifiedCacheFolder);
    ExitOnFailure(hr, "Failed to get cache directory.");

    if (!pCache->fUnverifiedCacheFolderCreated)
    {
        hr = DirEnsureExists(sczUnverifiedCacheFolder, NULL);
        ExitOnFailure(hr, "Failed to create unverified cache directory: %ls", sczUnverifiedCacheFolder);

        ResetPathPermissions(fPerMachine, sczUnverifiedCacheFolder);

        pCache->fUnverifiedCacheFolderCreated = TRUE;
    }

    hr = PathConcatRelativeToFullyQualifiedBase(sczUnverifiedCacheFolder, wzPayloadId, psczUnverifiedPayloadPath);
    ExitOnFailure(hr, "Failed to concat payload id to unverified folder path.");

LExit:
    ReleaseStr(sczUnverifiedCacheFolder);

    return hr;
}

static HRESULT VerifyThenTransferContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzCachedPath,
    __in_z LPCWSTR wzUnverifiedContainerPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // Get the container on disk actual hash.
    hFile = ::CreateFileW(wzUnverifiedContainerPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError(hr, "Failed to open container in working path: %ls", wzUnverifiedContainerPath);
    }


    switch (pContainer->verification)
    {
    case BURN_CONTAINER_VERIFICATION_HASH:
        hr = VerifyHash(pContainer->pbHash, pContainer->cbHash, pContainer->qwFileSize, TRUE, wzUnverifiedContainerPath, hFile, BURN_CACHE_STEP_HASH, pfnCacheMessageHandler, pfnProgress, pContext);
        ExitOnFailure(hr, "Failed to verify container hash: %ls", wzCachedPath);
        break;
    default:
        ExitOnRootFailure(hr = E_INVALIDARG, "Container has no verification information: %ls", pContainer->sczId);
        break;
    }

    LogStringLine(REPORT_STANDARD, "%ls container from working path '%ls' to path '%ls'", fMove ? L"Moving" : L"Copying", wzUnverifiedContainerPath, wzCachedPath);

    hr = CacheTransferFileWithRetry(wzUnverifiedContainerPath, wzCachedPath, fMove, BURN_CACHE_STEP_FINALIZE, pContainer->qwFileSize, pfnCacheMessageHandler, pfnProgress, pContext);

LExit:
    ReleaseFileHandle(hFile);

    return hr;
}

static HRESULT VerifyThenTransferPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzCachedPath,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in BOOL fMove,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // Get the payload on disk actual hash.
    hFile = ::CreateFileW(wzUnverifiedPayloadPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        ExitWithLastError(hr, "Failed to open payload in working path: %ls", wzUnverifiedPayloadPath);
    }

    switch (pPayload->verification)
    {
    case BURN_PAYLOAD_VERIFICATION_AUTHENTICODE:
        hr = CacheVerifyPayloadSignature(pPayload, wzUnverifiedPayloadPath, hFile, BURN_CACHE_STEP_HASH, pfnCacheMessageHandler, pfnProgress, pContext);
        ExitOnFailure(hr, "Failed to verify payload signature: %ls", wzCachedPath);
        break;
    case BURN_PAYLOAD_VERIFICATION_HASH:
        hr = VerifyHash(pPayload->pbHash, pPayload->cbHash, pPayload->qwFileSize, TRUE, wzUnverifiedPayloadPath, hFile, BURN_CACHE_STEP_HASH, pfnCacheMessageHandler, pfnProgress, pContext);
        ExitOnFailure(hr, "Failed to verify payload hash: %ls", wzCachedPath);
        break;
    case BURN_PAYLOAD_VERIFICATION_UPDATE_BUNDLE: __fallthrough;
    default:
        ExitOnRootFailure(hr = E_INVALIDARG, "Payload has no verification information: %ls", pPayload->sczKey);
        break;
    }

    LogStringLine(REPORT_STANDARD, "%ls payload from working path '%ls' to path '%ls'", fMove ? L"Moving" : L"Copying", wzUnverifiedPayloadPath, wzCachedPath);

    hr = CacheTransferFileWithRetry(wzUnverifiedPayloadPath, wzCachedPath, fMove, BURN_CACHE_STEP_FINALIZE, pPayload->qwFileSize, pfnCacheMessageHandler, pfnProgress, pContext);

LExit:
    ReleaseFileHandle(hFile);

    return hr;
}

static HRESULT CacheTransferFileWithRetry(
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzDestinationPath,
    __in BOOL fMove,
    __in BURN_CACHE_STEP cacheStep,
    __in DWORD64 qwFileSize,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE /*pfnProgress*/,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;

    hr = SendCacheBeginMessage(pfnCacheMessageHandler, pContext, cacheStep);
    ExitOnFailure(hr, "Aborted cache file transfer begin.");

    // TODO: send progress during the file transfer.
    if (fMove)
    {
        hr = FileEnsureMoveWithRetry(wzSourcePath, wzDestinationPath, TRUE, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
        ExitOnFailure(hr, "Failed to move %ls to %ls", wzSourcePath, wzDestinationPath);
    }
    else
    {
        hr = FileEnsureCopyWithRetry(wzSourcePath, wzDestinationPath, TRUE, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
        ExitOnFailure(hr, "Failed to copy %ls to %ls", wzSourcePath, wzDestinationPath);
    }

    hr = SendCacheSuccessMessage(pfnCacheMessageHandler, pContext, qwFileSize);

LExit:
    SendCacheCompleteMessage(pfnCacheMessageHandler, pContext, hr);

    return hr;
}

static HRESULT VerifyFileAgainstContainer(
    __in BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzVerifyPath,
    __in BOOL fAlreadyCached,
    __in BURN_CACHE_STEP cacheStep,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;

    // Get the container on disk actual hash.
    hFile = ::CreateFileW(wzVerifyPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        hr = HRESULT_FROM_WIN32(::GetLastError());
        if (E_PATHNOTFOUND == hr || E_FILENOTFOUND == hr)
        {
            ExitFunction(); // do not log error when the file was not found.
        }
        ExitOnRootFailure(hr, "Failed to open container at path: %ls", wzVerifyPath);
    }

    switch (pContainer->verification)
    {
    case BURN_CONTAINER_VERIFICATION_HASH:
        hr = VerifyHash(pContainer->pbHash, pContainer->cbHash, pContainer->qwFileSize, TRUE, wzVerifyPath, hFile, cacheStep, pfnCacheMessageHandler, pfnProgress, pContext);
        ExitOnFailure(hr, "Failed to verify hash of container: %ls", pContainer->sczId);
        break;
    default:
        ExitOnRootFailure(hr = E_INVALIDARG, "Container has no verification information: %ls", pContainer->sczId);
        break;
    }

    if (fAlreadyCached)
    {
        LogId(REPORT_STANDARD, MSG_VERIFIED_EXISTING_CONTAINER, pContainer->sczId, wzVerifyPath);
        ::DecryptFileW(wzVerifyPath, 0);  // Let's try to make sure it's not encrypted.
    }

LExit:
    ReleaseFileHandle(hFile);

    if (FAILED(hr) && E_PATHNOTFOUND != hr && E_FILENOTFOUND != hr)
    {
        if (fAlreadyCached)
        {
            LogErrorId(hr, MSG_FAILED_VERIFY_CONTAINER, pContainer->sczId, wzVerifyPath, NULL);
        }

        FileEnsureDelete(wzVerifyPath); // if the file existed but did not verify correctly, make it go away.
    }

    return hr;
}

static HRESULT VerifyFileAgainstPayload(
    __in BURN_PAYLOAD* pPayload,
    __in_z LPCWSTR wzVerifyPath,
    __in BOOL fAlreadyCached,
    __in BURN_CACHE_STEP cacheStep,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE pfnProgress,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    HANDLE hFile = INVALID_HANDLE_VALUE;
    BOOL fVerifyFileSize = FALSE;

    // Get the payload on disk actual hash.
    hFile = ::CreateFileW(wzVerifyPath, GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hFile)
    {
        hr = HRESULT_FROM_WIN32(::GetLastError());
        if (E_PATHNOTFOUND == hr || E_FILENOTFOUND == hr)
        {
            ExitFunction(); // do not log error when the file was not found.
        }
        ExitOnRootFailure(hr, "Failed to open payload at path: %ls", wzVerifyPath);
    }

    switch (pPayload->verification)
    {
    case BURN_PAYLOAD_VERIFICATION_AUTHENTICODE:
        hr = CacheVerifyPayloadSignature(pPayload, wzVerifyPath, hFile, cacheStep, pfnCacheMessageHandler, pfnProgress, pContext);
        ExitOnFailure(hr, "Failed to verify signature of payload: %ls", pPayload->sczKey);
        break;
    case BURN_PAYLOAD_VERIFICATION_HASH:
        fVerifyFileSize = TRUE;

        hr = VerifyHash(pPayload->pbHash, pPayload->cbHash, pPayload->qwFileSize, fVerifyFileSize, wzVerifyPath, hFile, cacheStep, pfnCacheMessageHandler, pfnProgress, pContext);
        ExitOnFailure(hr, "Failed to verify hash of payload: %ls", pPayload->sczKey);

        break;
    case BURN_PAYLOAD_VERIFICATION_UPDATE_BUNDLE:
        fVerifyFileSize = 0 != pPayload->qwFileSize;

        if (pPayload->pbHash)
        {
            hr = VerifyHash(pPayload->pbHash, pPayload->cbHash, pPayload->qwFileSize, fVerifyFileSize, wzVerifyPath, hFile, cacheStep, pfnCacheMessageHandler, pfnProgress, pContext);
            ExitOnFailure(hr, "Failed to verify hash of payload: %ls", pPayload->sczKey);
        }
        else if (fVerifyFileSize)
        {
            hr = VerifyFileSize(hFile, pPayload->qwFileSize, wzVerifyPath);
            ExitOnFailure(hr, "Failed to verify file size for path: %ls", wzVerifyPath);
        }

        break;
    default:
        ExitOnRootFailure(hr = E_INVALIDARG, "Payload has no verification information: %ls", pPayload->sczKey);
        break;
    }

    if (fAlreadyCached)
    {
        LogId(REPORT_STANDARD, MSG_VERIFIED_EXISTING_PAYLOAD, pPayload->sczKey, wzVerifyPath);
        ::DecryptFileW(wzVerifyPath, 0);  // Let's try to make sure it's not encrypted.
    }

LExit:
    ReleaseFileHandle(hFile);

    if (FAILED(hr) && E_PATHNOTFOUND != hr && E_FILENOTFOUND != hr)
    {
        if (fAlreadyCached)
        {
            LogErrorId(hr, MSG_FAILED_VERIFY_PAYLOAD, pPayload->sczKey, wzVerifyPath, NULL);
        }

        FileEnsureDelete(wzVerifyPath); // if the file existed but did not verify correctly, make it go away.
    }

    return hr;
}

static HRESULT AllocateSid(
    __in WELL_KNOWN_SID_TYPE type,
    __out PSID* ppSid
    )
{
    HRESULT hr = S_OK;
    PSID pAllocSid = NULL;
    DWORD cbSid = SECURITY_MAX_SID_SIZE;

    pAllocSid = static_cast<PSID>(MemAlloc(cbSid, TRUE));
    ExitOnNull(pAllocSid, hr, E_OUTOFMEMORY, "Failed to allocate memory for well known SID.");

    if (!::CreateWellKnownSid(type, NULL, pAllocSid, &cbSid))
    {
        ExitWithLastError(hr, "Failed to create well known SID.");
    }

    *ppSid = pAllocSid;
    pAllocSid = NULL;

LExit:
    ReleaseMem(pAllocSid);
    return hr;
}


static HRESULT ResetPathPermissions(
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    DWORD dwSetSecurity = DACL_SECURITY_INFORMATION | UNPROTECTED_DACL_SECURITY_INFORMATION;
    ACL acl = { };
    PSID pSid = NULL;

    if (fPerMachine)
    {
        hr = AllocateSid(WinBuiltinAdministratorsSid, &pSid);
        ExitOnFailure(hr, "Failed to allocate administrator SID.");

        // Create an empty (not NULL!) ACL to reset the permissions on the file to purely inherit from parent.
        if (!::InitializeAcl(&acl, sizeof(acl), ACL_REVISION))
        {
            ExitWithLastError(hr, "Failed to initialize ACL.");
        }

        dwSetSecurity |= OWNER_SECURITY_INFORMATION;
    }

    hr = AclSetSecurityWithRetry(wzPath, SE_FILE_OBJECT, dwSetSecurity, pSid, NULL, &acl, NULL, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
    ExitOnWin32Error(er, hr, "Failed to reset the ACL on cached file: %ls", wzPath);

    ::SetFileAttributesW(wzPath, FILE_ATTRIBUTE_NORMAL); // Let's try to reset any possible read-only/system bits.

LExit:
    ReleaseMem(pSid);
    return hr;
}


static HRESULT GrantAccessAndAllocateSid(
    __in WELL_KNOWN_SID_TYPE type,
    __in DWORD dwGrantAccess,
    __in EXPLICIT_ACCESS* pAccess
    )
{
    HRESULT hr = S_OK;

    hr = AllocateSid(type, reinterpret_cast<PSID*>(&pAccess->Trustee.ptstrName));
    ExitOnFailure(hr, "Failed to allocate SID to grate access.");

    pAccess->grfAccessMode = GRANT_ACCESS;
    pAccess->grfAccessPermissions = dwGrantAccess;
    pAccess->grfInheritance = SUB_CONTAINERS_AND_OBJECTS_INHERIT;
    pAccess->Trustee.TrusteeForm = TRUSTEE_IS_SID;
    pAccess->Trustee.TrusteeType = TRUSTEE_IS_GROUP;

LExit:
    return hr;
}


static HRESULT SecurePath(
    __in LPCWSTR wzPath
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    EXPLICIT_ACCESSW access[4] = { };
    PACL pAcl = NULL;

    // Administrators must be the first one in the array so we can reuse the allocated SID below.
    hr = GrantAccessAndAllocateSid(WinBuiltinAdministratorsSid, FILE_ALL_ACCESS, &access[0]);
    ExitOnFailure(hr, "Failed to allocate access for Administrators group to path: %ls", wzPath);

    hr = GrantAccessAndAllocateSid(WinLocalSystemSid, FILE_ALL_ACCESS, &access[1]);
    ExitOnFailure(hr, "Failed to allocate access for SYSTEM group to path: %ls", wzPath);

    hr = GrantAccessAndAllocateSid(WinWorldSid, GENERIC_READ | GENERIC_EXECUTE, &access[2]);
    ExitOnFailure(hr, "Failed to allocate access for Everyone group to path: %ls", wzPath);

    hr = GrantAccessAndAllocateSid(WinBuiltinUsersSid, GENERIC_READ | GENERIC_EXECUTE, &access[3]);
    ExitOnFailure(hr, "Failed to allocate access for Users group to path: %ls", wzPath);

    er = ::SetEntriesInAclW(countof(access), access, NULL, &pAcl);
    ExitOnWin32Error(er, hr, "Failed to create ACL to secure cache path: %ls", wzPath);

    // Set the ACL and ensure the Administrators group ends up the owner
    hr = AclSetSecurityWithRetry(wzPath, SE_FILE_OBJECT, OWNER_SECURITY_INFORMATION | DACL_SECURITY_INFORMATION | PROTECTED_DACL_SECURITY_INFORMATION,
                                 reinterpret_cast<PSID>(access[0].Trustee.ptstrName), NULL, pAcl, NULL, FILE_OPERATION_RETRY_COUNT, FILE_OPERATION_RETRY_WAIT);
    ExitOnFailure(hr, "Failed to secure cache path: %ls", wzPath);

LExit:
    if (pAcl)
    {
        ::LocalFree(pAcl);
    }

    for (DWORD i = 0; i < countof(access); ++i)
    {
        ReleaseMem(access[i].Trustee.ptstrName);
    }

    return hr;
}


static HRESULT CopyEngineToWorkingFolder(
    __in BOOL fElevated,
    __in BURN_CACHE* pCache,
    __in_z LPCWSTR wzSourcePath,
    __in_z LPCWSTR wzWorkingFolderName,
    __in_z LPCWSTR wzExecutableName,
    __in BURN_SECTION* pSection,
    __deref_out_z_opt LPWSTR* psczEngineWorkingPath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczWorkingFolder = NULL;
    LPWSTR sczTargetDirectory = NULL;
    LPWSTR sczTargetPath = NULL;
    LPWSTR sczSourceDirectory = NULL;
    LPWSTR sczPayloadSourcePath = NULL;
    LPWSTR sczPayloadTargetPath = NULL;

    hr = CacheEnsureBaseWorkingFolder(fElevated, pCache, &sczWorkingFolder);
    ExitOnFailure(hr, "Failed to create working path to copy engine.");

    hr = PathConcatRelativeToFullyQualifiedBase(sczWorkingFolder, wzWorkingFolderName, &sczTargetDirectory);
    ExitOnFailure(hr, "Failed to calculate the bundle working folder target name.");

    hr = DirEnsureExists(sczTargetDirectory, NULL);
    ExitOnFailure(hr, "Failed create bundle working folder.");

    hr = PathConcatRelativeToFullyQualifiedBase(sczTargetDirectory, wzExecutableName, &sczTargetPath);
    ExitOnFailure(hr, "Failed to combine working path with engine file name.");

    // Copy the engine without any attached containers to the working path.
    hr = CopyEngineWithSignatureFixup(pSection->hEngineFile, wzSourcePath, sczTargetPath, pSection);
    ExitOnFailure(hr, "Failed to copy engine: '%ls' to working path: %ls", wzSourcePath, sczTargetPath);

    if (psczEngineWorkingPath)
    {
        hr = StrAllocString(psczEngineWorkingPath, sczTargetPath, 0);
        ExitOnFailure(hr, "Failed to copy target path for engine working path.");
    }

LExit:
    ReleaseStr(sczPayloadTargetPath);
    ReleaseStr(sczPayloadSourcePath);
    ReleaseStr(sczSourceDirectory);
    ReleaseStr(sczTargetPath);
    ReleaseStr(sczTargetDirectory);
    ReleaseStr(sczWorkingFolder);

    return hr;
}


static HRESULT CopyEngineWithSignatureFixup(
    __in HANDLE hEngineFile,
    __in_z LPCWSTR wzEnginePath,
    __in_z LPCWSTR wzTargetPath,
    __in BURN_SECTION* pSection
    )
{
    HRESULT hr = S_OK;
    HANDLE hTarget = INVALID_HANDLE_VALUE;
    LARGE_INTEGER li = { };
    DWORD dwZeroOriginals[3] = { };

    hTarget = ::CreateFileW(wzTargetPath, GENERIC_WRITE, FILE_SHARE_READ | FILE_SHARE_DELETE, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
    if (INVALID_HANDLE_VALUE == hTarget)
    {
        ExitWithLastError(hr, "Failed to create engine file at path: %ls", wzTargetPath);
    }

    hr = FileSetPointer(hEngineFile, 0, NULL, FILE_BEGIN);
    ExitOnFailure(hr, "Failed to seek to beginning of engine file: %ls", wzEnginePath);

    hr = FileCopyUsingHandles(hEngineFile, hTarget, pSection->cbEngineSize, NULL);
    ExitOnFailure(hr, "Failed to copy engine from: %ls to: %ls", wzEnginePath, wzTargetPath);

    // If the original executable was signed, let's put back the checksum and signature.
    if (pSection->dwOriginalSignatureOffset)
    {
        // Fix up the checksum.
        li.QuadPart = pSection->dwChecksumOffset;
        if (!::SetFilePointerEx(hTarget, li, NULL, FILE_BEGIN))
        {
            ExitWithLastError(hr, "Failed to seek to checksum in exe header.");
        }

        hr = FileWriteHandle(hTarget, reinterpret_cast<LPBYTE>(&pSection->dwOriginalChecksum), sizeof(pSection->dwOriginalChecksum));
        ExitOnFailure(hr, "Failed to update signature offset.");

        // Fix up the signature information.
        li.QuadPart = pSection->dwCertificateTableOffset;
        if (!::SetFilePointerEx(hTarget, li, NULL, FILE_BEGIN))
        {
            ExitWithLastError(hr, "Failed to seek to signature table in exe header.");
        }

        hr = FileWriteHandle(hTarget, reinterpret_cast<LPBYTE>(&pSection->dwOriginalSignatureOffset), sizeof(pSection->dwOriginalSignatureOffset));
        ExitOnFailure(hr, "Failed to update signature offset.");

        hr = FileWriteHandle(hTarget, reinterpret_cast<LPBYTE>(&pSection->dwOriginalSignatureSize), sizeof(pSection->dwOriginalSignatureSize));
        ExitOnFailure(hr, "Failed to update signature offset.");

        // Zero out the original information since that is how it was when the file was originally signed.
        li.QuadPart = pSection->dwOriginalChecksumAndSignatureOffset;
        if (!::SetFilePointerEx(hTarget, li, NULL, FILE_BEGIN))
        {
            ExitWithLastError(hr, "Failed to seek to original data in exe burn section header.");
        }

        hr = FileWriteHandle(hTarget, reinterpret_cast<LPBYTE>(&dwZeroOriginals), sizeof(dwZeroOriginals));
        ExitOnFailure(hr, "Failed to zero out original data offset.");
    }

LExit:
    ReleaseFileHandle(hTarget);

    return hr;
}


static HRESULT RemoveBundleOrPackage(
    __in BURN_CACHE* pCache,
    __in BOOL fBundle,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzBundleOrPackageId,
    __in_z LPCWSTR wzCacheId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczRootCacheDirectory = NULL;
    LPWSTR sczDirectory = NULL;

    hr = CacheGetCompletedPath(pCache, fPerMachine, wzCacheId, &sczDirectory);
    ExitOnFailure(hr, "Failed to calculate cache path.");

    LogId(REPORT_STANDARD, fBundle ? MSG_UNCACHE_BUNDLE : MSG_UNCACHE_PACKAGE, wzBundleOrPackageId, sczDirectory);

    // Try really hard to remove the cache directory.
    hr = E_FAIL;
    for (DWORD iRetry = 0; FAILED(hr) && iRetry < FILE_OPERATION_RETRY_COUNT; ++iRetry)
    {
        if (0 < iRetry)
        {
            ::Sleep(FILE_OPERATION_RETRY_WAIT);
        }

        hr = DirEnsureDeleteEx(sczDirectory, DIR_DELETE_FILES | DIR_DELETE_RECURSE | DIR_DELETE_SCHEDULE);
        if (E_PATHNOTFOUND == hr)
        {
            break;
        }
    }

    if (E_PATHNOTFOUND != hr && FAILED(hr))
    {
        LogId(REPORT_STANDARD, fBundle ? MSG_UNABLE_UNCACHE_BUNDLE : MSG_UNABLE_UNCACHE_PACKAGE, wzBundleOrPackageId, sczDirectory, hr);
        hr = S_OK;
    }
    else
    {
        // Try to remove root package cache in the off chance it is now empty.
        hr = GetRootPath(pCache, fPerMachine, TRUE, &sczRootCacheDirectory);
        ExitOnFailure(hr, "Failed to get %hs package cache root directory.", fPerMachine ? "per-machine" : "per-user");
        DirEnsureDeleteEx(sczRootCacheDirectory, DIR_DELETE_SCHEDULE);

        // GetRootPath returns S_FALSE if the package cache is redirected elsewhere.
        if (S_FALSE == hr)
        {
            hr = GetRootPath(pCache, fPerMachine, FALSE, &sczRootCacheDirectory);
            ExitOnFailure(hr, "Failed to get old %hs package cache root directory.", fPerMachine ? "per-machine" : "per-user");
            DirEnsureDeleteEx(sczRootCacheDirectory, DIR_DELETE_SCHEDULE);
        }
    }

LExit:
    ReleaseStr(sczDirectory);
    ReleaseStr(sczRootCacheDirectory);

    return hr;
}

static HRESULT VerifyFileSize(
    __in HANDLE hFile,
    __in DWORD64 qwFileSize,
    __in_z LPCWSTR wzUnverifiedPayloadPath
    )
{
    HRESULT hr = S_OK;
    LONGLONG llSize = 0;

    hr = FileSizeByHandle(hFile, &llSize);
    ExitOnFailure(hr, "Failed to get file size for path: %ls", wzUnverifiedPayloadPath);

    if (static_cast<DWORD64>(llSize) != qwFileSize)
    {
        ExitOnRootFailure(hr = ERROR_FILE_CORRUPT, "File size mismatch for path: %ls, expected: %llu, actual: %lld", wzUnverifiedPayloadPath, qwFileSize, llSize);
    }

LExit:
    return hr;
}

static HRESULT VerifyHash(
    __in BYTE* pbHash,
    __in DWORD cbHash,
    __in DWORD64 qwFileSize,
    __in BOOL fVerifyFileSize,
    __in_z LPCWSTR wzUnverifiedPayloadPath,
    __in HANDLE hFile,
    __in BURN_CACHE_STEP cacheStep,
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPPROGRESS_ROUTINE /*pfnProgress*/,
    __in LPVOID pContext
    )
{
    HRESULT hr = S_OK;
    BYTE rgbActualHash[SHA512_HASH_LEN] = { };
    DWORD64 qwHashedBytes = 0;
    LPWSTR pszExpected = NULL;
    LPWSTR pszActual = NULL;
    BOOL fFailedVerification = FALSE;

    hr = SendCacheBeginMessage(pfnCacheMessageHandler, pContext, cacheStep);
    ExitOnFailure(hr, "Aborted cache verify hash begin.");

    fFailedVerification = TRUE;

    if (fVerifyFileSize)
    {
        hr = VerifyFileSize(hFile, qwFileSize, wzUnverifiedPayloadPath);
        ExitOnFailure(hr, "Failed to verify file size for path: %ls", wzUnverifiedPayloadPath);
    }

    // TODO: create a cryp hash file that sends progress.
    hr = CrypHashFileHandle(hFile, PROV_RSA_AES, CALG_SHA_512, rgbActualHash, sizeof(rgbActualHash), &qwHashedBytes);
    ExitOnFailure(hr, "Failed to calculate hash for path: %ls", wzUnverifiedPayloadPath);

    // Compare hashes.
    if (cbHash != sizeof(rgbActualHash) || 0 != memcmp(pbHash, rgbActualHash, sizeof(rgbActualHash)))
    {
        hr = CRYPT_E_HASH_VALUE;

        // Best effort to log the expected and actual hash value strings.
        if (SUCCEEDED(StrAllocHexEncode(pbHash, cbHash, &pszExpected)) &&
            SUCCEEDED(StrAllocHexEncode(rgbActualHash, sizeof(rgbActualHash), &pszActual)))
        {
            ExitOnFailure(hr, "Hash mismatch for path: %ls, expected: %ls, actual: %ls", wzUnverifiedPayloadPath, pszExpected, pszActual);
        }
        else
        {
            ExitOnFailure(hr, "Hash mismatch for path: %ls", wzUnverifiedPayloadPath);
        }
    }

    fFailedVerification = FALSE;

    hr = SendCacheSuccessMessage(pfnCacheMessageHandler, pContext, qwFileSize);

LExit:
    if (fFailedVerification)
    {
        // Make sure the BA process marks this container or payload as having failed verification.
        SendCacheFailureMessage(pfnCacheMessageHandler, pContext, cacheStep);
    }

    SendCacheCompleteMessage(pfnCacheMessageHandler, pContext, hr);

    ReleaseStr(pszActual);
    ReleaseStr(pszExpected);

    return hr;
}

static HRESULT VerifyPayloadAgainstCertChain(
    __in BURN_PAYLOAD* pPayload,
    __in PCCERT_CHAIN_CONTEXT pChainContext
    )
{
    HRESULT hr = S_OK;
    PCCERT_CONTEXT pChainElementCertContext = NULL;

    BYTE rgbPublicKeyIdentifier[SHA1_HASH_LEN] = { };
    DWORD cbPublicKeyIdentifier = sizeof(rgbPublicKeyIdentifier);
    BYTE* pbThumbprint = NULL;
    DWORD cbThumbprint = 0;

    // Walk up the chain looking for a certificate in the chain that matches our expected public key identifier
    // and thumbprint (if a thumbprint was provided).
    HRESULT hrChainVerification = E_NOTFOUND; // assume we won't find a match.
    for (DWORD i = 0; i < pChainContext->rgpChain[0]->cElement; ++i)
    {
        pChainElementCertContext = pChainContext->rgpChain[0]->rgpElement[i]->pCertContext;

        // Get the certificate's public key identifier.
        if (!::CryptHashPublicKeyInfo(NULL, CALG_SHA1, 0, X509_ASN_ENCODING, &pChainElementCertContext->pCertInfo->SubjectPublicKeyInfo, rgbPublicKeyIdentifier, &cbPublicKeyIdentifier))
        {
            ExitWithLastError(hr, "Failed to get certificate public key identifier.");
        }

        // Compare the certificate's public key identifier with the payload's public key identifier. If they
        // match, we're one step closer to the a positive result.
        if (pPayload->cbCertificateRootPublicKeyIdentifier == cbPublicKeyIdentifier &&
            0 == memcmp(pPayload->pbCertificateRootPublicKeyIdentifier, rgbPublicKeyIdentifier, cbPublicKeyIdentifier))
        {
            // If the payload specified a thumbprint for the certificate, verify it.
            if (pPayload->pbCertificateRootThumbprint)
            {
                hr = CertReadProperty(pChainElementCertContext, CERT_SHA1_HASH_PROP_ID, &pbThumbprint, &cbThumbprint);
                ExitOnFailure(hr, "Failed to read certificate thumbprint.");

                if (pPayload->cbCertificateRootThumbprint == cbThumbprint &&
                    0 == memcmp(pPayload->pbCertificateRootThumbprint, pbThumbprint, cbThumbprint))
                {
                    // If we got here, we found that our payload public key identifier and thumbprint
                    // matched an element in the certficate chain.
                    hrChainVerification = S_OK;
                    break;
                }

                ReleaseNullMem(pbThumbprint);
            }
            else // no thumbprint match necessary so we're good to go.
            {
                hrChainVerification = S_OK;
                break;
            }
        }
    }
    hr = hrChainVerification;
    ExitOnFailure(hr, "Failed to find expected public key in certificate chain.");

LExit:
    ReleaseMem(pbThumbprint);

    return hr;
}

static HRESULT SendCacheBeginMessage(
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPVOID pContext,
    __in BURN_CACHE_STEP cacheStep
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_MESSAGE message = { };

    message.type = BURN_CACHE_MESSAGE_BEGIN;
    message.begin.cacheStep = cacheStep;

    hr = pfnCacheMessageHandler(&message, pContext);

    return hr;
}

static HRESULT SendCacheSuccessMessage(
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPVOID pContext,
    __in DWORD64 qwFileSize
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_MESSAGE message = { };

    message.type = BURN_CACHE_MESSAGE_SUCCESS;
    message.success.qwFileSize = qwFileSize;

    hr = pfnCacheMessageHandler(&message, pContext);

    return hr;
}

static HRESULT SendCacheCompleteMessage(
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPVOID pContext,
    __in HRESULT hrStatus
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_MESSAGE message = { };

    message.type = BURN_CACHE_MESSAGE_COMPLETE;
    message.complete.hrStatus = hrStatus;

    hr = pfnCacheMessageHandler(&message, pContext);

    return hr;
}

static HRESULT SendCacheFailureMessage(
    __in PFN_BURNCACHEMESSAGEHANDLER pfnCacheMessageHandler,
    __in LPVOID pContext,
    __in BURN_CACHE_STEP cacheStep
    )
{
    HRESULT hr = S_OK;
    BURN_CACHE_MESSAGE message = { };

    message.type = BURN_CACHE_MESSAGE_FAILURE;
    message.failure.cacheStep = cacheStep;

    hr = pfnCacheMessageHandler(&message, pContext);

    return hr;
}
