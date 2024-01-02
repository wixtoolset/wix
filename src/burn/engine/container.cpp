// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// function definitions

extern "C" HRESULT ContainersParseFromXml(
    __in BURN_CONTAINERS* pContainers,
    __in IXMLDOMNode* pixnBundle,
    __in BURN_EXTENSIONS* pBurnExtensions
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;
    BOOL fXmlFound = FALSE;

    // select container nodes
    hr = XmlSelectNodes(pixnBundle, L"Container", &pixnNodes);
    ExitOnFailure(hr, "Failed to select container nodes.");

    // get container node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get container node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // allocate memory for searches
    pContainers->rgContainers = (BURN_CONTAINER*)MemAlloc(sizeof(BURN_CONTAINER) * cNodes, TRUE);
    ExitOnNull(pContainers->rgContainers, hr, E_OUTOFMEMORY, "Failed to allocate memory for container structs.");

    pContainers->cContainers = cNodes;

    // parse container elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_CONTAINER* pContainer = &pContainers->rgContainers[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pContainer->sczId);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Id.");

        // @Type
        pContainer->type = BURN_CONTAINER_TYPE_CABINET; // Default
        hr = XmlGetAttributeEx(pixnNode, L"Type", &scz);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @Type.");
        if (fXmlFound && scz && *scz)
        {
            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"Extension", -1))
            {
                pContainer->type = BURN_CONTAINER_TYPE_EXTENSION;
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, scz, -1, L"Cabinet", -1))
            {
                pContainer->type = BURN_CONTAINER_TYPE_CABINET;
            }
            else
            {
                hr = E_INVALIDDATA;
                ExitOnFailure(hr, "Unsupported container type '%ls'.", scz);
            }
        }

        if (BURN_CONTAINER_TYPE_EXTENSION == pContainer->type)
        {
            // @ExtensionId
            hr = XmlGetAttributeEx(pixnNode, L"ExtensionId", &scz);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @ExtensionId.");

            hr = BurnExtensionFindById(pBurnExtensions, scz, &pContainer->pExtension);
            ExitOnRootFailure(hr, "Failed to find bundle extension '%ls' for container '%ls'", scz, pContainer->sczId);
        }

        // @Attached
        hr = XmlGetYesNoAttribute(pixnNode, L"Attached", &pContainer->fAttached);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @Attached.");

        // Attached containers are always found attached to the current process, so use the current proccess's
        // name instead of what may be in the manifest.
        if (pContainer->fAttached)
        {
            // @AttachedIndex
            hr = XmlGetAttributeNumber(pixnNode, L"AttachedIndex", &pContainer->dwAttachedIndex);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @AttachedIndex.");

            hr = PathForCurrentProcess(&scz, NULL);
            ExitOnFailure(hr, "Failed to get path to current process for attached container.");

            LPCWSTR wzFileName = PathFile(scz);

            hr = StrAllocString(&pContainer->sczFilePath, wzFileName, 0);
            ExitOnFailure(hr, "Failed to set attached container file path.");
        }
        else
        {
            // @FilePath
            hr = XmlGetAttributeEx(pixnNode, L"FilePath", &pContainer->sczFilePath);
            ExitOnRequiredXmlQueryFailure(hr, "Failed to get @FilePath.");
        }

        // The source path starts as the file path.
        hr = StrAllocString(&pContainer->sczSourcePath, pContainer->sczFilePath, 0);
        ExitOnFailure(hr, "Failed to copy @FilePath");

        // @DownloadUrl
        hr = XmlGetAttributeEx(pixnNode, L"DownloadUrl", &pContainer->downloadSource.sczUrl);
        ExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get @DownloadUrl.");

        // @Hash
        hr = XmlGetAttributeEx(pixnNode, L"Hash", &pContainer->sczHash);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @Hash.");

        hr = StrAllocHexDecode(pContainer->sczHash, &pContainer->pbHash, &pContainer->cbHash);
        ExitOnFailure(hr, "Failed to hex decode the Container/@Hash.");

        // @FileSize
        hr = XmlGetAttributeEx(pixnNode, L"FileSize", &scz);
        ExitOnRequiredXmlQueryFailure(hr, "Failed to get @FileSize.");

        hr = StrStringToUInt64(scz, 0, &pContainer->qwFileSize);
        ExitOnFailure(hr, "Failed to parse @FileSize.");

        if (!pContainer->qwFileSize)
        {
            ExitWithRootFailure(hr, E_INVALIDDATA, "File size is required when verifying by hash for container: %ls", pContainer->sczId);
        }

        pContainer->verification = BURN_CONTAINER_VERIFICATION_HASH;

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

extern "C" HRESULT ContainersInitialize(
    __in BURN_CONTAINERS* pContainers,
    __in BURN_SECTION* pSection
    )
{
    HRESULT hr = S_OK;
    DWORD64 qwSize = 0;

    if (pContainers->rgContainers)
    {
        for (DWORD i = 0; i < pContainers->cContainers; ++i)
        {
            BURN_CONTAINER* pContainer = &pContainers->rgContainers[i];

            // If the container is attached, make sure the information in the section matches what the
            // manifest contained and get the offset to the container.
            if (pContainer->fAttached)
            {
                hr = SectionGetAttachedContainerInfo(pSection, pContainer->dwAttachedIndex, &pContainer->qwAttachedOffset, &qwSize, &pContainer->fActuallyAttached);
                ExitOnFailure(hr, "Failed to get attached container information.");

                if (qwSize != pContainer->qwFileSize)
                {
                    ExitOnFailure(hr, "Attached container '%ls' size '%llu' didn't match size from manifest: '%llu'", pContainer->sczId, qwSize, pContainer->qwFileSize);
                }
            }
        }
    }

LExit:
    return hr;
}

extern "C" void ContainersUninitialize(
    __in BURN_CONTAINERS* pContainers
    )
{
    if (pContainers->rgContainers)
    {
        for (DWORD i = 0; i < pContainers->cContainers; ++i)
        {
            BURN_CONTAINER* pContainer = &pContainers->rgContainers[i];

            ReleaseStr(pContainer->sczId);
            ReleaseStr(pContainer->sczHash);
            ReleaseStr(pContainer->sczSourcePath);
            ReleaseStr(pContainer->sczFilePath);
            ReleaseMem(pContainer->pbHash);
            ReleaseStr(pContainer->downloadSource.sczUrl);
            ReleaseStr(pContainer->downloadSource.sczUser);
            ReleaseStr(pContainer->downloadSource.sczPassword);
            ReleaseStr(pContainer->sczUnverifiedPath);
            ReleaseStr(pContainer->sczFailedLocalAcquisitionPath);
            ReleaseDict(pContainer->sdhPayloads);
        }
        MemFree(pContainers->rgContainers);
    }

    // clear struct
    memset(pContainers, 0, sizeof(BURN_CONTAINERS));
}

extern "C" HRESULT ContainerOpenUX(
    __in BURN_SECTION* pSection,
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER container = { };
    LPWSTR sczExecutablePath = NULL;

    // open attached container
    container.type = BURN_CONTAINER_TYPE_CABINET;
    container.fAttached = TRUE;
    container.dwAttachedIndex = 0;

    hr = SectionGetAttachedContainerInfo(pSection, container.dwAttachedIndex, &container.qwAttachedOffset, &container.qwFileSize, &container.fActuallyAttached);
    ExitOnFailure(hr, "Failed to get container information for UX container.");

    if (BURN_CONTAINER_TYPE_CABINET != pSection->dwFormat)
    {
        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
        ExitOnRootFailure(hr, "Unexpected UX container format %u. UX container is expected to always be a cabinet file", pSection->dwFormat);
    }

    AssertSz(container.fActuallyAttached, "The BA container must always be found attached.");

    hr = PathForCurrentProcess(&sczExecutablePath, NULL);
    ExitOnFailure(hr, "Failed to get path for executing module.");

    hr = ContainerOpen(pContext, &container, pSection->hEngineFile, sczExecutablePath);
    ExitOnFailure(hr, "Failed to open attached container.");

LExit:
    ReleaseStr(sczExecutablePath);

    return hr;
}

extern "C" HRESULT ContainerOpen(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in BURN_CONTAINER* pContainer,
    __in HANDLE hContainerFile,
    __in_z LPCWSTR wzFilePath
    )
{
    HRESULT hr = S_OK;
    LARGE_INTEGER li = { };
    LPWSTR szTempFile = NULL;

    // initialize context
    pContext->type = pContainer->type;
    pContext->qwSize = pContainer->qwFileSize;
    pContext->qwOffset = pContainer->qwAttachedOffset;

    // If the handle to the container is not open already, open container file
    if (INVALID_HANDLE_VALUE == hContainerFile)
    {
        pContext->hFile = ::CreateFileW(wzFilePath, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
        ExitOnInvalidHandleWithLastError(pContext->hFile, hr, "Failed to open file: %ls", wzFilePath);
    }
    else // use the container file handle.
    {
        if (!::DuplicateHandle(::GetCurrentProcess(), hContainerFile, ::GetCurrentProcess(), &pContext->hFile, 0, FALSE, DUPLICATE_SAME_ACCESS))
        {
            ExitWithLastError(hr, "Failed to duplicate handle to container: %ls", wzFilePath);
        }
    }

    // If it is a container attached to an executable, seek to the container offset.
    if (pContainer->fAttached)
    {
        li.QuadPart = (LONGLONG)pContext->qwOffset;
    }

    if (!::SetFilePointerEx(pContext->hFile, li, NULL, FILE_BEGIN))
    {
        ExitWithLastError(hr, "Failed to move file pointer to container offset.");
    }

    // open the archive
    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractOpen(pContext, wzFilePath);
        break;
    case BURN_CONTAINER_TYPE_EXTENSION:

        pContext->Bex.pExtension = pContainer->pExtension;

        if (pContainer->fAttached)
        {
            hr = BurnExtensionContainerOpenAttached(pContainer->pExtension, pContainer->sczId, pContext->hFile, pContext->qwOffset, pContext->qwSize, pContext);
            if (FAILED(hr))
            {
                LogId(REPORT_STANDARD, MSG_EXT_ATTACHED_CONTAINER_FAILED, pContainer->sczId);

                hr = FileCreateTemp(L"CNTNR", L"dat", &szTempFile, NULL);
                ExitOnFailure(hr, "Failed to create temporary container file");

                hr = FileCopyPartial(pContext->hFile, pContext->qwOffset, pContext->qwSize, szTempFile);
                ExitOnFailure(hr, "Failed to write to temporary container file");

                pContext->Bex.szTempContainerPath = szTempFile;
                szTempFile = NULL;

                hr = BurnExtensionContainerOpen(pContainer->pExtension, pContainer->sczId, pContext->Bex.szTempContainerPath, pContext);
            }
        }
        else
        {
            hr = BurnExtensionContainerOpen(pContainer->pExtension, pContainer->sczId, wzFilePath, pContext);
        }
        break;
    }
    ExitOnFailure(hr, "Failed to open container.");

LExit:
    ReleaseStr(szTempFile);

    return hr;
}

extern "C" HRESULT ContainerNextStream(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __inout_z LPWSTR* psczStreamName
    )
{
    HRESULT hr = S_OK;

    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractNextStream(pContext, psczStreamName);
        break;
    case BURN_CONTAINER_TYPE_EXTENSION:
        hr = BurnExtensionContainerNextStream(pContext->Bex.pExtension, pContext, psczStreamName);
        break;
    }

//LExit:
    return hr;
}

extern "C" HRESULT ContainerStreamToFile(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __in_z LPCWSTR wzFileName
    )
{
    HRESULT hr = S_OK;

    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractStreamToFile(pContext, wzFileName);
        break;
    case BURN_CONTAINER_TYPE_EXTENSION:
        hr = BurnExtensionContainerStreamToFile(pContext->Bex.pExtension, pContext, wzFileName);
        break;
    }

//LExit:
    return hr;
}

extern "C" HRESULT ContainerStreamToBuffer(
    __in BURN_CONTAINER_CONTEXT* pContext,
    __out BYTE** ppbBuffer,
    __out SIZE_T* pcbBuffer
    )
{
    HRESULT hr = S_OK;

    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractStreamToBuffer(pContext, ppbBuffer, pcbBuffer);
        break;
    case BURN_CONTAINER_TYPE_EXTENSION:
        hr = BurnExtensionContainerStreamToBuffer(pContext->Bex.pExtension, pContext, ppbBuffer, pcbBuffer);
        break;

    default:
        *ppbBuffer = NULL;
        *pcbBuffer = 0;
    }

//LExit:
    return hr;
}

extern "C" HRESULT ContainerSkipStream(
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;

    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractSkipStream(pContext);
        break;
    case BURN_CONTAINER_TYPE_EXTENSION:
        hr = BurnExtensionContainerSkipStream(pContext->Bex.pExtension, pContext);
        break;
    }

//LExit:
    return hr;
}

extern "C" HRESULT ContainerClose(
    __in BURN_CONTAINER_CONTEXT* pContext
    )
{
    HRESULT hr = S_OK;

    // close container
    switch (pContext->type)
    {
    case BURN_CONTAINER_TYPE_CABINET:
        hr = CabExtractClose(pContext);
        ExitOnFailure(hr, "Failed to close cabinet.");
        break;
    case BURN_CONTAINER_TYPE_EXTENSION:
        hr = BurnExtensionContainerClose(pContext->Bex.pExtension, pContext);
        if (pContext->Bex.szTempContainerPath && *pContext->Bex.szTempContainerPath)
        {
            FileEnsureDelete(pContext->Bex.szTempContainerPath);
            ReleaseNullStr(pContext->Bex.szTempContainerPath);
        }
        ExitOnFailure(hr, "Failed to close cabinet.");
        break;
    }

LExit:
    ReleaseFile(pContext->hFile);

    if (SUCCEEDED(hr))
    {
        memset(pContext, 0, sizeof(BURN_CONTAINER_CONTEXT));
    }

    return hr;
}

extern "C" HRESULT ContainerFindById(
    __in BURN_CONTAINERS* pContainers,
    __in_z LPCWSTR wzId,
    __out BURN_CONTAINER** ppContainer
    )
{
    HRESULT hr = S_OK;
    BURN_CONTAINER* pContainer = NULL;

    for (DWORD i = 0; i < pContainers->cContainers; ++i)
    {
        pContainer = &pContainers->rgContainers[i];

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pContainer->sczId, -1, wzId, -1))
        {
            *ppContainer = pContainer;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}
