// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// internal function declarations

static HRESULT FindEmbeddedBySourcePath(
    __in BURN_PAYLOADS* pPayloads,
    __in_opt BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzStreamName,
    __out BURN_PAYLOAD** ppPayload
    );


// function definitions

extern "C" HRESULT PayloadsParseFromXml(
    __in BURN_PAYLOADS* pPayloads,
    __in_opt BURN_CONTAINERS* pContainers,
    __in_opt BURN_PAYLOAD_GROUP* pLayoutPayloads,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // select payload nodes
    hr = XmlSelectNodes(pixnBundle, L"Payload", &pixnNodes);
    ExitOnFailure(hr, "Failed to select payload nodes.");

    // get payload node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get payload node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // allocate memory for payloads
    pPayloads->rgPayloads = (BURN_PAYLOAD*)MemAlloc(sizeof(BURN_PAYLOAD) * cNodes, TRUE);
    ExitOnNull(pPayloads->rgPayloads, hr, E_OUTOFMEMORY, "Failed to allocate memory for payload structs.");

    pPayloads->cPayloads = cNodes;

    // parse search elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_PAYLOAD* pPayload = &pPayloads->rgPayloads[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pPayload->sczKey);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @FilePath
        hr = XmlGetAttributeEx(pixnNode, L"FilePath", &pPayload->sczFilePath);
        ExitOnFailure(hr, "Failed to get @FilePath.");

        // @Packaging
        hr = XmlGetAttributeEx(pixnNode, L"Packaging", &scz);
        ExitOnFailure(hr, "Failed to get @Packaging.");

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"embedded", -1))
        {
            pPayload->packaging = BURN_PAYLOAD_PACKAGING_EMBEDDED;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"external", -1))
        {
            pPayload->packaging = BURN_PAYLOAD_PACKAGING_EXTERNAL;
        }
        else
        {
            hr = E_INVALIDARG;
            ExitOnFailure(hr, "Invalid value for @Packaging: %ls", scz);
        }

        // @Container
        if (pContainers)
        {
            hr = XmlGetAttributeEx(pixnNode, L"Container", &scz);
            if (E_NOTFOUND != hr || BURN_PAYLOAD_PACKAGING_EMBEDDED == pPayload->packaging)
            {
                ExitOnFailure(hr, "Failed to get @Container.");

                // find container
                hr = ContainerFindById(pContainers, scz, &pPayload->pContainer);
                ExitOnFailure(hr, "Failed to to find container: %ls", scz);
            }
        }

        // @LayoutOnly
        hr = XmlGetYesNoAttribute(pixnNode, L"LayoutOnly", &pPayload->fLayoutOnly);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @LayoutOnly.");
        }

        // @SourcePath
        hr = XmlGetAttributeEx(pixnNode, L"SourcePath", &pPayload->sczSourcePath);
        ExitOnFailure(hr, "Failed to get @SourcePath.");

        // @DownloadUrl
        hr = XmlGetAttributeEx(pixnNode, L"DownloadUrl", &pPayload->downloadSource.sczUrl);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @DownloadUrl.");
        }

        // @FileSize
        hr = XmlGetAttributeEx(pixnNode, L"FileSize", &scz);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @FileSize.");

            hr = StrStringToUInt64(scz, 0, &pPayload->qwFileSize);
            ExitOnFailure(hr, "Failed to parse @FileSize.");
        }

        // @Hash
        hr = XmlGetAttributeEx(pixnNode, L"Hash", &scz);
        ExitOnFailure(hr, "Failed to get @Hash.");

        hr = StrAllocHexDecode(scz, &pPayload->pbHash, &pPayload->cbHash);
        ExitOnFailure(hr, "Failed to hex decode the Payload/@Hash.");

        if (pPayload->fLayoutOnly && pLayoutPayloads)
        {
            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pLayoutPayloads->rgItems), pLayoutPayloads->cItems + 1, sizeof(BURN_PAYLOAD_GROUP_ITEM), 5);
            ExitOnFailure(hr, "Failed to allocate memory for layout payloads.");

            pLayoutPayloads->rgItems[pLayoutPayloads->cItems].pPayload = pPayload;
            ++pLayoutPayloads->cItems;

            pLayoutPayloads->qwTotalSize += pPayload->qwFileSize;
        }

        // prepare next iteration
        ReleaseNullObject(pixnNode);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);

    return hr;
}

extern "C" void PayloadUninitialize(
    __in BURN_PAYLOAD* pPayload
    )
{
    if (pPayload)
    {
        ReleaseStr(pPayload->sczKey);
        ReleaseStr(pPayload->sczFilePath);
        ReleaseMem(pPayload->pbHash);
        ReleaseStr(pPayload->sczSourcePath);
        ReleaseStr(pPayload->sczLocalFilePath);
        ReleaseStr(pPayload->downloadSource.sczUrl);
        ReleaseStr(pPayload->downloadSource.sczUser);
        ReleaseStr(pPayload->downloadSource.sczPassword);
        ReleaseStr(pPayload->sczUnverifiedPath);
    }
}

extern "C" void PayloadsUninitialize(
    __in BURN_PAYLOADS* pPayloads
    )
{
    if (pPayloads->rgPayloads)
    {
        for (DWORD i = 0; i < pPayloads->cPayloads; ++i)
        {
            PayloadUninitialize(pPayloads->rgPayloads + i);
        }
        MemFree(pPayloads->rgPayloads);
    }

    // clear struct
    memset(pPayloads, 0, sizeof(BURN_PAYLOADS));
}

extern "C" HRESULT PayloadExtractFromContainer(
    __in BURN_PAYLOADS* pPayloads,
    __in_opt BURN_CONTAINER* pContainer,
    __in BURN_CONTAINER_CONTEXT* pContainerContext,
    __in_z LPCWSTR wzTargetDir
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczStreamName = NULL;
    LPWSTR sczDirectory = NULL;
    BURN_PAYLOAD* pPayload = NULL;

    // extract all payloads
    for (;;)
    {
        // get next stream
        hr = ContainerNextStream(pContainerContext, &sczStreamName);
        if (E_NOMOREITEMS == hr)
        {
            hr = S_OK;
            break;
        }
        ExitOnFailure(hr, "Failed to get next stream.");

        // find payload by stream name
        hr = FindEmbeddedBySourcePath(pPayloads, pContainer, sczStreamName, &pPayload);
        ExitOnFailure(hr, "Failed to find embedded payload: %ls", sczStreamName);

        // make file path
        hr = PathConcat(wzTargetDir, pPayload->sczFilePath, &pPayload->sczLocalFilePath);
        ExitOnFailure(hr, "Failed to concat file paths.");

        // extract file
        hr = PathGetDirectory(pPayload->sczLocalFilePath, &sczDirectory);
        ExitOnFailure(hr, "Failed to get directory portion of local file path");

        hr = DirEnsureExists(sczDirectory, NULL);
        ExitOnFailure(hr, "Failed to ensure directory exists");

        hr = ContainerStreamToFile(pContainerContext, pPayload->sczLocalFilePath);
        ExitOnFailure(hr, "Failed to extract file.");

        // flag that the payload has been acquired
        pPayload->state = BURN_PAYLOAD_STATE_ACQUIRED;
    }

    // locate any payloads that were not extracted
    for (DWORD i = 0; i < pPayloads->cPayloads; ++i)
    {
        pPayload = &pPayloads->rgPayloads[i];

        // if the payload is part of the container
        if (!pContainer || pPayload->pContainer == pContainer)
        {
            // if the payload has not been acquired
            if (BURN_PAYLOAD_STATE_ACQUIRED > pPayload->state)
            {
                hr = E_INVALIDDATA;
                ExitOnRootFailure(hr, "Payload was not found in container: %ls", pPayload->sczKey);
            }
        }
    }

LExit:
    ReleaseStr(sczStreamName);
    ReleaseStr(sczDirectory);

    return hr;
}

extern "C" HRESULT PayloadFindById(
    __in BURN_PAYLOADS* pPayloads,
    __in_z LPCWSTR wzId,
    __out BURN_PAYLOAD** ppPayload
    )
{
    HRESULT hr = S_OK;
    BURN_PAYLOAD* pPayload = NULL;

    for (DWORD i = 0; i < pPayloads->cPayloads; ++i)
    {
        pPayload = &pPayloads->rgPayloads[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pPayload->sczKey, -1, wzId, -1))
        {
            *ppPayload = pPayload;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

extern "C" HRESULT PayloadFindEmbeddedBySourcePath(
    __in BURN_PAYLOADS* pPayloads,
    __in_z LPCWSTR wzStreamName,
    __out BURN_PAYLOAD** ppPayload
    )
{
    HRESULT hr = S_OK;
    BURN_PAYLOAD* pPayload = NULL;

    for (DWORD i = 0; i < pPayloads->cPayloads; ++i)
    {
        pPayload = &pPayloads->rgPayloads[i];

        if (BURN_PAYLOAD_PACKAGING_EMBEDDED == pPayload->packaging)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pPayload->sczSourcePath, -1, wzStreamName, -1))
            {
                *ppPayload = pPayload;
                ExitFunction1(hr = S_OK);
            }
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}


// internal function definitions

static HRESULT FindEmbeddedBySourcePath(
    __in BURN_PAYLOADS* pPayloads,
    __in_opt BURN_CONTAINER* pContainer,
    __in_z LPCWSTR wzStreamName,
    __out BURN_PAYLOAD** ppPayload
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pPayloads->cPayloads; ++i)
    {
        BURN_PAYLOAD* pPayload = &pPayloads->rgPayloads[i];

        if (BURN_PAYLOAD_PACKAGING_EMBEDDED == pPayload->packaging && (!pContainer || pPayload->pContainer == pContainer))
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pPayload->sczSourcePath, -1, wzStreamName, -1))
            {
                *ppPayload = pPayload;
                ExitFunction1(hr = S_OK);
            }
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}
