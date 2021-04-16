// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


extern "C" HRESULT PseudoBundleInitialize(
    __in DWORD64 qwEngineVersion,
    __in BURN_PACKAGE* pPackage,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BOOL fCached,
    __in_z LPCWSTR wzFilePath,
    __in_z LPCWSTR wzLocalSource,
    __in_z_opt LPCWSTR wzDownloadSource,
    __in DWORD64 qwSize,
    __in BOOL fVital,
    __in_z_opt LPCWSTR wzInstallArguments,
    __in_z_opt LPCWSTR wzRepairArguments,
    __in_z_opt LPCWSTR wzUninstallArguments,
    __in_opt BURN_DEPENDENCY_PROVIDER* pDependencyProvider,
    __in_opt const BYTE* pbHash,
    __in const DWORD cbHash
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczRelationTypeCommandLineSwitch = NULL;
    BURN_PAYLOAD* pPayload = NULL;

    LPCWSTR wzRelationTypeCommandLine = CoreRelationTypeToCommandLineString(relationType);
    if (wzRelationTypeCommandLine)
    {
        hr = StrAllocFormatted(&sczRelationTypeCommandLineSwitch, L" -%ls", wzRelationTypeCommandLine);
    }

    // Initialize the single payload, and fill out all the necessary fields
    pPackage->payloads.rgpPayloads = (BURN_PAYLOAD**)MemAlloc(sizeof(BURN_PAYLOAD*), TRUE);
    ExitOnNull(pPackage->payloads.rgpPayloads, hr, E_OUTOFMEMORY, "Failed to allocate space for burn payload group inside of related bundle struct");
    pPackage->payloads.cPayloads = 1;

    pPayload = (BURN_PAYLOAD*)MemAlloc(sizeof(BURN_PAYLOAD), TRUE); 
    ExitOnNull(pPayload, hr, E_OUTOFMEMORY, "Failed to allocate space for burn payload inside of related bundle struct");
    pPackage->payloads.rgpPayloads[0] = pPayload;
    pPayload->packaging = BURN_PAYLOAD_PACKAGING_EXTERNAL;
    pPayload->qwFileSize = qwSize;

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

    if (pbHash)
    {
        pPayload->pbHash = static_cast<BYTE*>(MemAlloc(cbHash, FALSE));
        ExitOnNull(pPayload->pbHash, hr, E_OUTOFMEMORY, "Failed to allocate memory for pseudo bundle payload hash.");

        pPayload->cbHash = cbHash;
        memcpy_s(pPayload->pbHash, pPayload->cbHash, pbHash, cbHash);
    }

    pPackage->Exe.fPseudoBundle = TRUE;

    pPackage->type = BURN_PACKAGE_TYPE_EXE;
    pPackage->fPerMachine = fPerMachine;
    pPackage->currentState = state;
    pPackage->fCached = fCached;
    pPackage->qwInstallSize = qwSize;
    pPackage->qwSize = qwSize;
    pPackage->fVital = fVital;

    hr = StrAllocString(&pPackage->sczId, wzId, 0);
    ExitOnFailure(hr, "Failed to copy key for pseudo bundle.");

    hr = StrAllocString(&pPackage->sczCacheId, wzId, 0);
    ExitOnFailure(hr, "Failed to copy cache id for pseudo bundle.");

    // If we are a self updating bundle, we don't have to have Install arguments.
    if (wzInstallArguments)
    {
        hr = StrAllocString(&pPackage->Exe.sczInstallArguments, wzInstallArguments, 0);
        ExitOnFailure(hr, "Failed to copy install arguments for related bundle package");
    }

    if (sczRelationTypeCommandLineSwitch)
    {
        hr = StrAllocConcat(&pPackage->Exe.sczInstallArguments, sczRelationTypeCommandLineSwitch, 0);
        ExitOnFailure(hr, "Failed to append relation type to install arguments for related bundle package");
    }

    if (wzRepairArguments)
    {
        hr = StrAllocString(&pPackage->Exe.sczRepairArguments, wzRepairArguments, 0);
        ExitOnFailure(hr, "Failed to copy repair arguments for related bundle package");

        if (sczRelationTypeCommandLineSwitch)
        {
            hr = StrAllocConcat(&pPackage->Exe.sczRepairArguments, sczRelationTypeCommandLineSwitch, 0);
            ExitOnFailure(hr, "Failed to append relation type to repair arguments for related bundle package");
        }

        pPackage->Exe.fRepairable = TRUE;
    }

    if (wzUninstallArguments)
    {
        hr = StrAllocString(&pPackage->Exe.sczUninstallArguments, wzUninstallArguments, 0);
        ExitOnFailure(hr, "Failed to copy uninstall arguments for related bundle package");

        if (sczRelationTypeCommandLineSwitch)
        {
            hr = StrAllocConcat(&pPackage->Exe.sczUninstallArguments, sczRelationTypeCommandLineSwitch, 0);
            ExitOnFailure(hr, "Failed to append relation type to uninstall arguments for related bundle package");
        }

        pPackage->fUninstallable = TRUE;
    }

    // Only support progress from engines that are compatible (aka: version greater than or equal to last protocol breaking change *and* versions that are older or the same as this engine).
    pPackage->Exe.protocol = (FILEMAKEVERSION(3, 6, 2221, 0) <= qwEngineVersion && qwEngineVersion <= FILEMAKEVERSION(rmj, rmm, rup, rpr)) ? BURN_EXE_PROTOCOL_TYPE_BURN : BURN_EXE_PROTOCOL_TYPE_NONE;
    
    // All versions of Burn past v3.9 RTM support suppressing ancestors.
    pPackage->Exe.fSupportsAncestors = FILEMAKEVERSION(3, 9, 1006, 0) <= qwEngineVersion;

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
    ReleaseStr(sczRelationTypeCommandLineSwitch);

    return hr;
}

extern "C" HRESULT PseudoBundleInitializePassthrough(
    __in BURN_PACKAGE* pPassthroughPackage,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in_z_opt LPCWSTR wzAppendLogPath,
    __in_z_opt LPCWSTR wzActiveParent,
    __in_z_opt LPCWSTR wzAncestors,
    __in BURN_PACKAGE* pPackage
    )
{
    Assert(BURN_PACKAGE_TYPE_EXE == pPackage->type);

    HRESULT hr = S_OK;
    LPWSTR sczArguments = NULL;

    // Initialize the payloads, and copy the necessary fields.
    pPassthroughPackage->payloads.rgpPayloads = (BURN_PAYLOAD**)MemAlloc(sizeof(BURN_PAYLOAD*) * pPackage->payloads.cPayloads, TRUE);
    ExitOnNull(pPassthroughPackage->payloads.rgpPayloads, hr, E_OUTOFMEMORY, "Failed to allocate space for burn package payload inside of passthrough bundle.");
    pPassthroughPackage->payloads.cPayloads = pPackage->payloads.cPayloads;

    for (DWORD iPayload = 0; iPayload < pPackage->payloads.cPayloads; ++iPayload)
    {
        pPassthroughPackage->payloads.rgpPayloads[iPayload] = pPackage->payloads.rgpPayloads[iPayload];
    }

    pPassthroughPackage->Exe.fPseudoBundle = TRUE;

    pPassthroughPackage->fPerMachine = FALSE; // passthrough bundles are always launched per-user.
    pPassthroughPackage->type = pPackage->type;
    pPassthroughPackage->currentState = pPackage->currentState;
    pPassthroughPackage->fCached = pPackage->fCached;
    pPassthroughPackage->qwInstallSize = pPackage->qwInstallSize;
    pPassthroughPackage->qwSize = pPackage->qwSize;
    pPassthroughPackage->fVital = pPackage->fVital;

    hr = StrAllocString(&pPassthroughPackage->sczId, pPackage->sczId, 0);
    ExitOnFailure(hr, "Failed to copy key for passthrough pseudo bundle.");

    hr = StrAllocString(&pPassthroughPackage->sczCacheId, pPackage->sczCacheId, 0);
    ExitOnFailure(hr, "Failed to copy cache id for passthrough pseudo bundle.");

    pPassthroughPackage->Exe.protocol = pPackage->Exe.protocol;

    // No matter the operation, we're passing the same command-line. That's what makes
    // this a passthrough bundle.
    hr = CoreRecreateCommandLine(&sczArguments, pCommand->action, pCommand->display, pCommand->restart, pCommand->relationType, TRUE, wzActiveParent, wzAncestors, wzAppendLogPath, pCommand->wzCommandLine);
    ExitOnFailure(hr, "Failed to recreate command-line arguments.");

    hr = StrAllocString(&pPassthroughPackage->Exe.sczInstallArguments, sczArguments, 0);
    ExitOnFailure(hr, "Failed to copy install arguments for passthrough bundle package");

    hr = StrAllocString(&pPassthroughPackage->Exe.sczRepairArguments, sczArguments, 0);
    ExitOnFailure(hr, "Failed to copy related arguments for passthrough bundle package");

    pPassthroughPackage->Exe.fRepairable = TRUE;

    hr = StrAllocString(&pPassthroughPackage->Exe.sczUninstallArguments, sczArguments, 0);
    ExitOnFailure(hr, "Failed to copy uninstall arguments for passthrough bundle package");

    pPassthroughPackage->fUninstallable = TRUE;

    // TODO: consider bringing this back in the near future.
    //if (pDependencyProvider)
    //{
    //    pPassthroughPackage->rgDependencyProviders = (BURN_DEPENDENCY_PROVIDER*)MemAlloc(sizeof(BURN_DEPENDENCY_PROVIDER), TRUE);
    //    ExitOnNull(pPassthroughPackage->rgDependencyProviders, hr, E_OUTOFMEMORY, "Failed to allocate memory for dependency providers.");
    //    pPassthroughPackage->cDependencyProviders = 1;

    //    pPassthroughPackage->rgDependencyProviders[0].fImported = pDependencyProvider->fImported;

    //    hr = StrAllocString(&pPassthroughPackage->rgDependencyProviders[0].sczKey, pDependencyProvider->sczKey, 0);
    //    ExitOnFailure(hr, "Failed to copy key for pseudo bundle.");

    //    hr = StrAllocString(&pPassthroughPackage->rgDependencyProviders[0].sczVersion, pDependencyProvider->sczVersion, 0);
    //    ExitOnFailure(hr, "Failed to copy version for pseudo bundle.");

    //    hr = StrAllocString(&pPassthroughPackage->rgDependencyProviders[0].sczDisplayName, pDependencyProvider->sczDisplayName, 0);
    //    ExitOnFailure(hr, "Failed to copy display name for pseudo bundle.");
    //}

LExit:
    ReleaseStr(sczArguments);
    return hr;
}
