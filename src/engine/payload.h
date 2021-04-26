#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif


// constants

enum BURN_PAYLOAD_PACKAGING
{
    BURN_PAYLOAD_PACKAGING_NONE,
    BURN_PAYLOAD_PACKAGING_EMBEDDED,
    BURN_PAYLOAD_PACKAGING_EXTERNAL,
};

enum BURN_PAYLOAD_STATE
{
    BURN_PAYLOAD_STATE_NONE,
    BURN_PAYLOAD_STATE_ACQUIRED,
    BURN_PAYLOAD_STATE_CACHED,
};


// structs

typedef struct _BURN_PAYLOAD
{
    LPWSTR sczKey;
    BURN_PAYLOAD_PACKAGING packaging;
    BOOL fLayoutOnly;
    DWORD64 qwFileSize;
    LPWSTR sczFilePath; // file path relative to the execute location

    BYTE* pbHash;
    DWORD cbHash;

    LPWSTR sczSourcePath;
    BURN_CONTAINER* pContainer;
    DOWNLOAD_SOURCE downloadSource;

    // mutable members
    BURN_PAYLOAD_STATE state;
    LPWSTR sczLocalFilePath; // location of extracted or downloaded copy

    LPWSTR sczUnverifiedPath;
    DWORD cRemainingInstances;
} BURN_PAYLOAD;

typedef struct _BURN_PAYLOADS
{
    BURN_PAYLOAD* rgPayloads;
    DWORD cPayloads;
} BURN_PAYLOADS;

typedef struct _BURN_PAYLOAD_GROUP_ITEM
{
    BURN_PAYLOAD* pPayload;

    // mutable members
    BOOL fCached;
    DWORD64 qwCommittedCacheProgress;
} BURN_PAYLOAD_GROUP_ITEM;

typedef struct _BURN_PAYLOAD_GROUP
{
    BURN_PAYLOAD_GROUP_ITEM* rgItems;
    DWORD cItems;
    DWORD64 qwTotalSize;
} BURN_PAYLOAD_GROUP;

// functions

HRESULT PayloadsParseFromXml(
    __in BURN_PAYLOADS* pPayloads,
    __in_opt BURN_CONTAINERS* pContainers,
    __in_opt BURN_PAYLOAD_GROUP* pLayoutPayloads,
    __in IXMLDOMNode* pixnBundle
    );
void PayloadUninitialize(
    __in BURN_PAYLOAD* pPayload
    );
void PayloadsUninitialize(
    __in BURN_PAYLOADS* pPayloads
    );
HRESULT PayloadExtractFromContainer(
    __in BURN_PAYLOADS* pPayloads,
    __in_opt BURN_CONTAINER* pContainer,
    __in BURN_CONTAINER_CONTEXT* pContainerContext,
    __in_z LPCWSTR wzTargetDir
    );
HRESULT PayloadFindById(
    __in BURN_PAYLOADS* pPayloads,
    __in_z LPCWSTR wzId,
    __out BURN_PAYLOAD** ppPayload
    );
HRESULT PayloadFindEmbeddedBySourcePath(
    __in BURN_PAYLOADS* pPayloads,
    __in_z LPCWSTR wzStreamName,
    __out BURN_PAYLOAD** ppPayload
    );


#if defined(__cplusplus)
}
#endif
