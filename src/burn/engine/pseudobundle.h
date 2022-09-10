#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

HRESULT PseudoBundleInitializeRelated(
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
    );
HRESULT PseudoBundleInitializePassthrough(
    __in BURN_PACKAGE* pPassthroughPackage,
    __in BURN_ENGINE_COMMAND* pInternalCommand,
    __in BOOTSTRAPPER_COMMAND* pCommand,
    __in BURN_PACKAGE* pPackage
    );
HRESULT PseudoBundleInitializeUpdateBundle(
    __in BURN_PACKAGE* pPackage,
    __in_z LPCWSTR wzId,
    __in_z LPCWSTR wzCacheId,
    __in_z LPCWSTR wzFilePath,
    __in_z LPCWSTR wzLocalSource,
    __in_z_opt LPCWSTR wzDownloadSource,
    __in DWORD64 qwSize,
    __in_z LPCWSTR wzInstallArguments,
    __in_opt LPCWSTR wzHash
);

#if defined(__cplusplus)
}
#endif
