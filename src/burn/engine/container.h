#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// typedefs

//typedef HRESULT (*PFN_EXTRACTOPEN)(
//    __in HANDLE hFile,
//    __in DWORD64 qwOffset,
//    __in DWORD64 qwSize,
//    __out void** ppCookie
//    );
//typedef HRESULT (*PFN_EXTRACTNEXTSTREAM)(
//    __in void* pCookie,
//    __inout_z LPWSTR* psczStreamName
//    );
//typedef HRESULT (*PFN_EXTRACTSTREAMTOFILE)(
//    __in void* pCookie,
//    __in_z LPCWSTR wzFileName
//    );
//typedef HRESULT (*PFN_EXTRACTSTREAMTOBUFFER)(
//    __in void* pCookie,
//    __out BYTE** ppbBuffer,
//    __out SIZE_T* pcbBuffer
//    );
//typedef HRESULT (*PFN_EXTRACTCLOSE)(
//    __in void* pCookie
//    );

// Forward declarations
typedef struct _BURN_EXTENSION BURN_EXTENSION;
typedef struct _BURN_EXTENSIONS BURN_EXTENSIONS;

// constants

enum BURN_CONTAINER_TYPE
{
    BURN_CONTAINER_TYPE_NONE,
    BURN_CONTAINER_TYPE_CABINET,
    BURN_CONTAINER_TYPE_SEVENZIP,
    BURN_CONTAINER_TYPE_EXTENSION,
};

enum BURN_CAB_OPERATION
{
    BURN_CAB_OPERATION_NONE,
    BURN_CAB_OPERATION_NEXT_STREAM,
    BURN_CAB_OPERATION_STREAM_TO_FILE,
    BURN_CAB_OPERATION_STREAM_TO_BUFFER,
    BURN_CAB_OPERATION_SKIP_STREAM,
    BURN_CAB_OPERATION_CLOSE,
};

enum BURN_CONTAINER_VERIFICATION
{
    BURN_CONTAINER_VERIFICATION_NONE,
    BURN_CONTAINER_VERIFICATION_HASH,
};


// structs

typedef struct _BURN_CONTAINER
{
    LPWSTR sczId;
    BURN_CONTAINER_TYPE type;
    BOOL fAttached;
    DWORD dwAttachedIndex;
    DWORD64 qwFileSize;
    LPWSTR sczHash;
    LPWSTR sczFilePath;         // relative path to container.
    DOWNLOAD_SOURCE downloadSource;

    DWORD cParsedPayloads;
    STRINGDICT_HANDLE sdhPayloads; // value is BURN_PAYLOAD*

    BYTE* pbHash;
    DWORD cbHash;
    BURN_CONTAINER_VERIFICATION verification;
    DWORD64 qwAttachedOffset;
    BOOL fActuallyAttached;     // indicates whether an attached container is attached or missing.

    BURN_EXTENSION* pExtension;

    // mutable members
    BOOL fPlanned;
    LPWSTR sczSourcePath;
    LPWSTR sczUnverifiedPath;
    DWORD64 qwExtractSizeTotal;
    DWORD64 qwCommittedCacheProgress;
    DWORD64 qwCommittedExtractProgress;
    BOOL fExtracted;
    BOOL fFailedVerificationFromAcquisition;
    LPWSTR sczFailedLocalAcquisitionPath;
} BURN_CONTAINER;

typedef struct _BURN_CONTAINERS
{
    BURN_CONTAINER* rgContainers;
    DWORD cContainers;
} BURN_CONTAINERS;

typedef struct _BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER
{
    HANDLE hFile;
    LARGE_INTEGER liPosition;
} BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER;

typedef struct _BURN_CONTAINER_CONTEXT_CABINET
{
    LPWSTR sczFile;

    HANDLE hThread;
    HANDLE hBeginOperationEvent;
    HANDLE hOperationCompleteEvent;

    BURN_CAB_OPERATION operation;
    HRESULT hrError;

    LPWSTR* psczStreamName;
    LPCWSTR wzTargetFile;
    HANDLE hTargetFile;
    BYTE* pbTargetBuffer;
    DWORD cbTargetBuffer;
    DWORD iTargetBuffer;

    BURN_CONTAINER_CONTEXT_CABINET_VIRTUAL_FILE_POINTER* rgVirtualFilePointers;
    DWORD cVirtualFilePointers;
} BURN_CONTAINER_CONTEXT_CABINET;

typedef struct _BURN_CONTAINER_CONTEXT_BEX
{
    BURN_EXTENSION* pExtension;
    LPWSTR szTempContainerPath;
    LPVOID pExtensionContext;
} BURN_CONTAINER_CONTEXT_BEX;

typedef struct _BURN_CONTAINER_CONTEXT
{
    HANDLE hFile;
    DWORD64 qwOffset;
    DWORD64 qwSize;

    //PFN_EXTRACTOPEN pfnExtractOpen;
    //PFN_EXTRACTNEXTSTREAM pfnExtractNextStream;
    //PFN_EXTRACTSTREAMTOFILE pfnExtractStreamToFile;
    //PFN_EXTRACTSTREAMTOBUFFER pfnExtractStreamToBuffer;
    //PFN_EXTRACTCLOSE pfnExtractClose;
    //void* pCookie;
    BURN_CONTAINER_TYPE type;
    union
    {
        BURN_CONTAINER_CONTEXT_CABINET Cabinet;
        BURN_CONTAINER_CONTEXT_BEX Bex;
    };

} BURN_CONTAINER_CONTEXT;


// functions

HRESULT ContainersParseFromXml(
    __in BURN_CONTAINERS* pContainers,
    __in IXMLDOMNode* pixnBundle,
    __in BURN_EXTENSIONS* pBurnExtensions
    );
HRESULT ContainersInitialize(
    __in BURN_CONTAINERS* pContainers,
    __in BURN_SECTION* pSection
    );
void ContainersUninitialize(
    __in BURN_CONTAINERS* pContainers
    );
HRESULT ContainerOpenUX(
    __in BURN_SECTION* pSection,
    __in BURN_CONTAINER_CONTEXT* pContext
    );
HRESULT ContainerOpen(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in BURN_CONTAINER* pContainer,
    __in HANDLE hContainerFile,
    __in_z LPCWSTR wzFilePath
    );
HRESULT ContainerNextStream(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout_z LPWSTR* psczStreamName
    );
HRESULT ContainerStreamToFile(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in_z LPCWSTR wzFileName
    );
HRESULT ContainerStreamToBuffer(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __out BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    );
HRESULT ContainerSkipStream(
    __in BURN_CONTAINER_CONTEXT* pContext
    );
HRESULT ContainerClose(
    __in BURN_CONTAINER_CONTEXT* pContext
    );
HRESULT ContainerFindById(
    __in BURN_CONTAINERS* pContainers,
    __in_z LPCWSTR wzId,
    __out BURN_CONTAINER** ppContainer
    );


#if defined(__cplusplus)
}
#endif
