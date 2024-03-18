#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "BootstrapperExtensionTypes.h"

DECLARE_INTERFACE_IID_(IBootstrapperExtension, IUnknown, "93123C9D-796B-4FCD-A507-6EDEF9A925FD")
{
    STDMETHOD(Search)(
        __in LPCWSTR wzId,
        __in LPCWSTR wzVariable
        ) = 0;

    /* ContainerOpen
    Open a container file.
    */
    STDMETHOD(ContainerOpen)(
            __in LPCWSTR wzContainerId,
            __in LPCWSTR wzFilePath,
            __out LPVOID *ppContext
        ) = 0;

    /* ContainerOpenAttached
    Open an attached container.
    If not implemented, return E_NOTIMPL. In that case, burn will extract the container to a temporary file and call ContainerOpen(). Note that, this may come with substantial performance penalty
    */
    STDMETHOD(ContainerOpenAttached)(
            __in LPCWSTR wzContainerId,
            __in HANDLE hBundle,
            __in DWORD64 qwContainerStartPos,
            __in DWORD64 qwContainerSize,
            __out LPVOID *ppContext
        ) = 0;

    /* ContainerExtractFiles
    Extract files.
    */
    STDMETHOD(ContainerExtractFiles)(
            __in LPVOID pContext,
            __in DWORD cFiles,
            __in LPCWSTR *psczEmbeddedIds,
            __in LPCWSTR *psczTargetPaths
        ) = 0;

    /* ContainerClose
    Release the container.
    */
    STDMETHOD(ContainerClose)(
            __in LPVOID pContext
        ) = 0;

    // BootstrapperExtensionProc - The PFN_BOOTSTRAPPER_EXTENSION_PROC can call this method to give the BootstrapperExtension raw access to the callback from the engine.
    //                       This might be used to help the BootstrapperExtension support more than one version of the engine.
    STDMETHOD(BootstrapperExtensionProc)(
        __in BOOTSTRAPPER_EXTENSION_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __in_opt LPVOID pvContext
        ) = 0;
};
