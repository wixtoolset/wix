// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// internal function declarations


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
    BOOL fChainPayload = pContainers && pLayoutPayloads; // These are required when parsing chain payloads.
    BOOL fValidFileSize = FALSE;
    size_t cByteOffset = fChainPayload ? offsetof(BURN_PAYLOAD, sczKey) : offsetof(BURN_PAYLOAD, sczSourcePath);
    BOOL fXmlFound = FALSE;

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

    // create dictionary for payloads
    hr = DictCreateWithEmbeddedKey(&pPayloads->sdhPayloads, pPayloads->cPayloads, reinterpret_cast<void**>(&pPayloads->rgPayloads), cByteOffset, DICT_FLAG_NONE);
    ExitOnFailure(hr, "Failed to create dictionary for payloads.");

    // parse payload elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_PAYLOAD* pPayload = &pPayloads->rgPayloads[i];
        fValidFileSize = FALSE;

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pPayload->sczKey);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Id.");

        // @FilePath
        hr = XmlGetAttributeEx(pixnNode, L"FilePath", &pPayload->sczFilePath);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @FilePath.");

        // @SourcePath
        hr = XmlGetAttributeEx(pixnNode, L"SourcePath", &pPayload->sczSourcePath);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @SourcePath.");

        if (!fChainPayload)
        {
            // All non-chain payloads are embedded in the UX container.
            pPayload->packaging = BURN_PAYLOAD_PACKAGING_EMBEDDED;
        }
        else
        {
            // @Packaging
            hr = XmlGetAttributeEx(pixnNode, L"Packaging", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Packaging.");

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
                ExitWithRootFailure(hr, E_INVALIDARG, "Invalid value for @Packaging: %ls", scz);
            }

            // @Container
            hr = XmlGetAttributeEx(pixnNode, L"Container", &scz);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @Container.");

            if (fXmlFound)
            {
                // find container
                hr = ContainerFindById(pContainers, scz, &pPayload->pContainer);
                ExitOnFailure(hr, "Failed to find container: %ls", scz);

                pPayload->pContainer->cParsedPayloads += 1;
            }
            else if (BURN_PAYLOAD_PACKAGING_EMBEDDED == pPayload->packaging)
            {
                ExitWithRootFailure(hr, E_NOTFOUND, "@Container is required for embedded payload.");
            }

            // @LayoutOnly
            hr = XmlGetYesNoAttribute(pixnNode, L"LayoutOnly", &pPayload->fLayoutOnly);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @LayoutOnly.");

            // @DownloadUrl
            hr = XmlGetAttributeEx(pixnNode, L"DownloadUrl", &pPayload->downloadSource.sczUrl);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @DownloadUrl.");

            // @FileSize
            hr = XmlGetAttributeEx(pixnNode, L"FileSize", &scz);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @FileSize.");

            if (fXmlFound)
            {
                hr = StrStringToUInt64(scz, 0, &pPayload->qwFileSize);
                ExitOnFailure(hr, "Failed to parse @FileSize.");

                fValidFileSize = TRUE;
            }

            // @CertificateAuthorityKeyIdentifier
            hr = XmlGetAttributeEx(pixnNode, L"CertificateRootPublicKeyIdentifier", &scz);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @CertificateRootPublicKeyIdentifier.");

            if (fXmlFound)
            {
                hr = StrAllocHexDecode(scz, &pPayload->pbCertificateRootPublicKeyIdentifier, &pPayload->cbCertificateRootPublicKeyIdentifier);
                ExitOnFailure(hr, "Failed to hex decode @CertificateRootPublicKeyIdentifier.");

                pPayload->verification = BURN_PAYLOAD_VERIFICATION_AUTHENTICODE;
            }

            // @CertificateThumbprint
            hr = XmlGetAttributeEx(pixnNode, L"CertificateRootThumbprint", &scz);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @CertificateRootThumbprint.");

            if (fXmlFound)
            {
                hr = StrAllocHexDecode(scz, &pPayload->pbCertificateRootThumbprint, &pPayload->cbCertificateRootThumbprint);
                ExitOnFailure(hr, "Failed to hex decode @CertificateRootThumbprint.");
            }

            // @Hash
            hr = XmlGetAttributeEx(pixnNode, L"Hash", &scz);
            ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @Hash.");

            if (fXmlFound)
            {
                hr = StrAllocHexDecode(scz, &pPayload->pbHash, &pPayload->cbHash);
                ExitOnFailure(hr, "Failed to hex decode the Payload/@Hash.");

                if (BURN_PAYLOAD_VERIFICATION_NONE == pPayload->verification)
                {
                    pPayload->verification = BURN_PAYLOAD_VERIFICATION_HASH;
                }
            }

            if (BURN_PAYLOAD_VERIFICATION_NONE == pPayload->verification)
            {
                ExitWithRootFailure(hr, E_INVALIDDATA, "There was no verification information for payload: %ls", pPayload->sczKey);
            }
            else if (BURN_PAYLOAD_VERIFICATION_HASH == pPayload->verification && !fValidFileSize)
            {
                ExitWithRootFailure(hr, E_INVALIDDATA, "File size is required when verifying by hash for payload: %ls", pPayload->sczKey);
            }

            if (pPayload->fLayoutOnly)
            {
                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pLayoutPayloads->rgItems), pLayoutPayloads->cItems + 1, sizeof(BURN_PAYLOAD_GROUP_ITEM), 5);
                ExitOnFailure(hr, "Failed to allocate memory for layout payloads.");

                pLayoutPayloads->rgItems[pLayoutPayloads->cItems].pPayload = pPayload;
                ++pLayoutPayloads->cItems;

                pLayoutPayloads->qwTotalSize += pPayload->qwFileSize;
            }
        }

        hr = DictAddValue(pPayloads->sdhPayloads, pPayload);
        ExitOnFailure(hr, "Failed to add payload to payloads dictionary.");

        // prepare next iteration
        ReleaseNullObject(pixnNode);
    }

    hr = S_OK;

    if (pContainers && pContainers->cContainers)
    {
        for (DWORD i = 0; i < pPayloads->cPayloads; ++i)
        {
            BURN_PAYLOAD* pPayload = &pPayloads->rgPayloads[i];
            BURN_CONTAINER* pContainer = pPayload->pContainer;

            if (!pContainer)
            {
                continue;
            }
            else if (!pContainer->sdhPayloads)
            {
                hr = DictCreateWithEmbeddedKey(&pContainer->sdhPayloads, pContainer->cParsedPayloads, NULL, offsetof(BURN_PAYLOAD, sczSourcePath), DICT_FLAG_NONE);
                ExitOnFailure(hr, "Failed to create dictionary for container payloads.");
            }

            hr = DictAddValue(pContainer->sdhPayloads, pPayload);
            ExitOnFailure(hr, "Failed to add payload to container dictionary.");
        }
    }

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
        ReleaseMem(pPayload->pbCertificateRootThumbprint);
        ReleaseMem(pPayload->pbCertificateRootPublicKeyIdentifier);
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

    ReleaseDict(pPayloads->sdhPayloads);

    // clear struct
    memset(pPayloads, 0, sizeof(BURN_PAYLOADS));
}

extern "C" HRESULT PayloadExtractUXContainer(
    __in BURN_PAYLOADS* pPayloads,
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
        hr = PayloadFindEmbeddedBySourcePath(pPayloads->sdhPayloads, sczStreamName, &pPayload);
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

        // if the payload has not been acquired
        if (BURN_PAYLOAD_STATE_ACQUIRED > pPayload->state)
        {
            ExitWithRootFailure(hr, E_INVALIDDATA, "Payload was not found in container: %ls", pPayload->sczKey);
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

    hr = DictGetValue(pPayloads->sdhPayloads, wzId, reinterpret_cast<void**>(ppPayload));

    return hr;
}

extern "C" HRESULT PayloadFindEmbeddedBySourcePath(
    __in STRINGDICT_HANDLE sdhPayloads,
    __in_z LPCWSTR wzStreamName,
    __out BURN_PAYLOAD** ppPayload
    )
{
    HRESULT hr = S_OK;

    hr = DictGetValue(sdhPayloads, wzStreamName, reinterpret_cast<void**>(ppPayload));

    return hr;
}


// internal function definitions
