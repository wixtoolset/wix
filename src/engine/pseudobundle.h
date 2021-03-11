#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

HRESULT PseudoBundleInitialize(
    __in DWORD64 qwEngineVersion,
    __in BURN_PACKAGE* pPackage,
    __in BOOL fPerMachine,
    __in_z LPCWSTR wzId,
    __in BOOTSTRAPPER_RELATION_TYPE relationType,
    __in BOOTSTRAPPER_PACKAGE_STATE state,
    __in BURN_CACHE_STATE cacheState,
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
    );
HRESULT PseudoBundleInitializePassthrough(
    __in BURN_PACKAGE* pPassthroughPackage,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in_z_opt LPCWSTR wzAppendLogPath,
    __in_z_opt LPCWSTR wzActiveParent,
    __in_z_opt LPCWSTR wzAncestors,
    __in BURN_PACKAGE* pPackage
    );

#if defined(__cplusplus)
}
#endif
