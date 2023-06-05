// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


extern "C" HRESULT PseudoBundleInitializeRelated(
    __in BURN_PACKAGE* pPackage,
    __in BOOL fSupportsBurnProtocol,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzId,
#ifdef DEBUG
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
#endif
    __in BOOL fCached,
    __in_z LPCWSTR wzFilePath,
    __in DWORD64 qwSize,
    __in_opt BURN_DEPENDENCY_PROVIDER* pDependencyProvider
    )
{
    HRESULT hr = S_OK;
    BURN_PAYLOAD* pPayload = NULL;

    AssertSz(BOOTSTRAPPER_RELATION_UPDATE != relationType, "Update pseudo bundles must use PseudoBundleInitializeUpdateBundle instead.");

    // Initialize the single payload, and fill out all the necessary fields
    pPackage->payloads.rgItems = (BURN_PAYLOAD_GROUP_ITEM*)MemAlloc(sizeof(BURN_PAYLOAD_GROUP_ITEM), TRUE);
    ExitOnNull(pPackage->payloads.rgItems, hr, E_OUTOFMEMORY, "Failed to allocate space for burn payload group inside of related bundle struct");
    pPackage->payloads.cItems = 1;

    pPayload = (BURN_PAYLOAD*)MemAlloc(sizeof(BURN_PAYLOAD), TRUE);
    ExitOnNull(pPayload, hr, E_OUTOFMEMORY, "Failed to allocate space for burn payload inside of related bundle struct");
    pPackage->payloads.rgItems[0].pPayload = pPayload;
    pPayload->packaging = BURN_PAYLOAD_PACKAGING_EXTERNAL;
    pPayload->qwFileSize = qwSize;

    hr = StrAllocString(&pPayload->sczKey, wzId, 0);
    ExitOnFailure(hr, "Failed to copy key for pseudo bundle payload.");

    hr = StrAllocString(&pPayload->sczFilePath, wzFilePath, 0);
    ExitOnFailure(hr, "Failed to copy filename for pseudo bundle.");

    hr = StrAllocString(&pPayload->sczSourcePath, wzFilePath, 0);
    ExitOnFailure(hr, "Failed to copy local source path for pseudo bundle.");

    pPackage->type = BURN_PACKAGE_TYPE_BUNDLE;
    pPackage->fPerMachine = fPerMachine;
    pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_PRESENT;
    pPackage->fCached = fCached;
    pPackage->qwInstallSize = qwSize;
    pPackage->qwSize = qwSize;
    pPackage->fVital = FALSE;

    pPackage->fPermanent = FALSE;
    pPackage->Bundle.fSupportsBurnProtocol = fSupportsBurnProtocol;

    hr = StrAllocString(&pPackage->sczId, wzId, 0);
    ExitOnFailure(hr, "Failed to copy key for pseudo bundle.");

    hr = StrAllocString(&pPackage->sczCacheId, wzId, 0);
    ExitOnFailure(hr, "Failed to copy cache id for pseudo bundle.");

    // Log variables - best effort
    StrAllocFormatted(&pPackage->sczLogPathVariable, L"WixBundleLog_%ls", pPackage->sczId);
    StrAllocFormatted(&pPackage->sczRollbackLogPathVariable, L"WixBundleRollbackLog_%ls", pPackage->sczId);

    if (pDependencyProvider)
    {
        pPackage->rgDependencyProviders = (BURN_DEPENDENCY_PROVIDER*)MemAlloc(sizeof(BURN_DEPENDENCY_PROVIDER), TRUE);
        ExitOnNull(pPackage->rgDependencyProviders, hr, E_OUTOFMEMORY, "Failed to allocate memory for dependency providers.");
        pPackage->cDependencyProviders = 1;

        pPackage->rgDependencyProviders[0].fImported = pDependencyProvider->fImported;

        hr = StrAllocString(&pPackage->rgDependencyProviders[0].sczKey, pDependencyProvider->sczKey, 0);
        ExitOnFailure(hr, "Failed to copy key for pseudo bundle.");

        hr = StrAllocString(&pPackage->rgDependencyProviders[0].sczVersion, pDependencyProvider->sczVersion, 0);
        ExitOnFailure(hr, "Failed to copy version for pseudo bundle.");

        hr = StrAllocString(&pPackage->rgDependencyProviders[0].sczDisplayName, pDependencyProvider->sczDisplayName, 0);
        ExitOnFailure(hr, "Failed to copy display name for pseudo bundle.");
    }

LExit:
    return hr;
}

extern "C" HRESULT PseudoBundleInitializePassthrough(
    __in BURN_PACKAGE* pPassthroughPackage,
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_PACKAGE* pPackage
    )
{
    Assert(BURN_PACKAGE_TYPE_BUNDLE == pPackage->type);

    HRESULT hr = S_OK;
    LPWSTR sczArguments = NULL;

    // Initialize the payloads, and copy the necessary fields.
    pPassthroughPackage->payloads.rgItems = (BURN_PAYLOAD_GROUP_ITEM*)MemAlloc(sizeof(BURN_PAYLOAD_GROUP_ITEM) * pPackage->payloads.cItems, TRUE);
    ExitOnNull(pPassthroughPackage->payloads.rgItems, hr, E_OUTOFMEMORY, "Failed to allocate space for burn package payload inside of passthrough bundle.");
    pPassthroughPackage->payloads.cItems = pPackage->payloads.cItems;

    for (DWORD iPayload = 0; iPayload < pPackage->payloads.cItems; ++iPayload)
    {
        pPassthroughPackage->payloads.rgItems[iPayload].pPayload = pPackage->payloads.rgItems[iPayload].pPayload;
    }

    pPassthroughPackage->fPerMachine = FALSE; // passthrough bundles are always launched per-user.
    pPassthroughPackage->type = BURN_PACKAGE_TYPE_EXE;
    pPassthroughPackage->currentState = pPackage->currentState;
    pPassthroughPackage->fCached = pPackage->fCached;
    pPassthroughPackage->qwInstallSize = pPackage->qwInstallSize;
    pPassthroughPackage->qwSize = pPackage->qwSize;
    pPassthroughPackage->fVital = pPackage->fVital;
    pPassthroughPackage->fPermanent = TRUE;

    pPassthroughPackage->Exe.fPseudoPackage = TRUE;
    pPassthroughPackage->Exe.fUninstallable = FALSE;
    pPassthroughPackage->Exe.protocol = pPackage->Bundle.fSupportsBurnProtocol ? BURN_EXE_PROTOCOL_TYPE_BURN : BURN_EXE_PROTOCOL_TYPE_NONE;

    hr = StrAllocString(&pPassthroughPackage->sczId, pPackage->sczId, 0);
    ExitOnFailure(hr, "Failed to copy key for passthrough pseudo bundle.");

    hr = StrAllocString(&pPassthroughPackage->sczCacheId, pPackage->sczCacheId, 0);
    ExitOnFailure(hr, "Failed to copy cache id for passthrough pseudo bundle.");

    // Log variables - best effort
    StrAllocFormatted(&pPackage->sczLogPathVariable, L"WixBundleLog_%ls", pPackage->sczId);
    StrAllocFormatted(&pPackage->sczRollbackLogPathVariable, L"WixBundleRollbackLog_%ls", pPackage->sczId);

    hr = CoreCreatePassthroughBundleCommandLine(&sczArguments, pInternalCommand, pCommand);
    ExitOnFailure(hr, "Failed to create command-line arguments.");

    hr = StrAllocString(&pPassthroughPackage->Exe.sczInstallArguments, sczArguments, 0);
    ExitOnFailure(hr, "Failed to copy install arguments for passthrough bundle package");

LExit:
    ReleaseStr(sczArguments);
    return hr;
}

extern "C" HRESULT PseudoBundleInitializeUpdateBundle(
    __in BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzCacheId,
    __in_z LPCWSTR wzFilePath,
    __in_z LPCWSTR wzLocalSource,
    __in_z_opt LPCWSTR wzDownloadSource,
    __in DWORD64 qwSize,
    __in_z LPCWSTR wzInstallArguments,
    __in_opt LPCWSTR wzHash
)
{
    HRESULT hr = S_OK;
    BURN_PAYLOAD* pPayload = NULL;

    // Initialize the single payload, and fill out all the necessary fields
    pPackage->payloads.rgItems = (BURN_PAYLOAD_GROUP_ITEM*)MemAlloc(sizeof(BURN_PAYLOAD_GROUP_ITEM), TRUE);
    ExitOnNull(pPackage->payloads.rgItems, hr, E_OUTOFMEMORY, "Failed to allocate space for burn payload group inside of update bundle struct");
    pPackage->payloads.cItems = 1;

    pPayload = (BURN_PAYLOAD*)MemAlloc(sizeof(BURN_PAYLOAD), TRUE);
    ExitOnNull(pPayload, hr, E_OUTOFMEMORY, "Failed to allocate space for burn payload inside of update bundle struct");
    pPackage->payloads.rgItems[0].pPayload = pPayload;
    pPayload->packaging = BURN_PAYLOAD_PACKAGING_EXTERNAL;
    pPayload->qwFileSize = qwSize;
    pPayload->verification = BURN_PAYLOAD_VERIFICATION_UPDATE_BUNDLE;

    hr = StrAllocString(&pPayload->sczKey, wzId, 0);
    ExitOnFailure(hr, "Failed to copy key for pseudo bundle payload.");

    hr = StrAllocString(&pPayload->sczFilePath, wzFilePath, 0);
    ExitOnFailure(hr, "Failed to copy filename for pseudo bundle.");

    hr = StrAllocString(&pPayload->sczSourcePath, wzLocalSource, 0);
    ExitOnFailure(hr, "Failed to copy local source path for pseudo bundle.");

    if (wzDownloadSource && *wzDownloadSource)
    {
        hr = StrAllocString(&pPayload->downloadSource.sczUrl, wzDownloadSource, 0);
        ExitOnFailure(hr, "Failed to copy download source for pseudo bundle.");
    }

    if (wzHash && *wzHash)
    {
        BYTE* rgbHash = NULL;
        DWORD cbHash = 0;

        hr = StrAllocHexDecode(wzHash, &rgbHash, &cbHash);
        ExitOnFailure(hr, "Failed to decode hash string: %ls.", wzHash);

        pPayload->pbHash = static_cast<BYTE*>(MemAlloc(cbHash, FALSE));
        ExitOnNull(pPayload->pbHash, hr, E_OUTOFMEMORY, "Failed to allocate memory for update bundle payload hash.");

        pPayload->cbHash = cbHash;

        memcpy_s(pPayload->pbHash, pPayload->cbHash, rgbHash, cbHash);
    }

    pPackage->type = BURN_PACKAGE_TYPE_EXE;
    pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT;
    pPackage->qwInstallSize = qwSize;
    pPackage->qwSize = qwSize;
    pPackage->fVital = TRUE;

    // Trust the BA to only use UPDATE_REPLACE_EMBEDDED when appropriate.
    pPackage->Exe.protocol = BURN_EXE_PROTOCOL_TYPE_BURN;
    pPackage->Exe.fPseudoPackage = TRUE;

    hr = StrAllocString(&pPackage->sczId, wzId, 0);
    ExitOnFailure(hr, "Failed to copy id for update bundle.");

    hr = StrAllocString(&pPackage->sczCacheId, wzCacheId, 0);
    ExitOnFailure(hr, "Failed to copy cache id for update bundle.");

    // Log variables - best effort
    StrAllocFormatted(&pPackage->sczLogPathVariable, L"WixBundleLog_%ls", pPackage->sczId);
    StrAllocFormatted(&pPackage->sczRollbackLogPathVariable, L"WixBundleRollbackLog_%ls", pPackage->sczId);

    hr = StrAllocString(&pPackage->Exe.sczInstallArguments, wzInstallArguments, 0);
    ExitOnFailure(hr, "Failed to copy install arguments for update bundle package");

LExit:
    return hr;
}
