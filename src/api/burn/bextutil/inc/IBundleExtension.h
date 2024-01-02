#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


DECLARE_INTERFACE_IID_(IBundleExtension, IUnknown, "93123C9D-796B-4FCD-A507-6EDEF9A925FD")
{
    STDMETHOD(Search)(
        __in LPCWSTR wzId,
        __in LPCWSTR wzVariable
        ) = 0;

    /* ContainerOpen
    Open a container file
    */
    STDMETHOD(ContainerOpen)(
        __in LPCWSTR wzContainerId,
        __in LPCWSTR wzFilePath,
        __out LPVOID *ppContext
        ) = 0;

    /* ContainerOpenAttached
    Open an attached container
    If not implemented, return E_NOTIMPL. In that case, burn will extract the container to a temporary file and call ContainerOpen(). Note that, this may come with substantial performance penalty
    */
    STDMETHOD(ContainerOpenAttached)(
            __in LPCWSTR wzContainerId,
            __in HANDLE hBundle,
            __in DWORD64 qwContainerStartPos,
            __in DWORD64 qwContainerSize,
            __out LPVOID *ppContext
        ) = 0;

    /* ContainerNextStream
    Return the file name of the subsequent stream
    */
    STDMETHOD(ContainerNextStream)(
        __in LPVOID pContext,
        __inout_z LPWSTR* psczStreamName
        ) = 0;

    /* ContainerStreamToFile
    Extract the current stream to a file. May be implemented a-synchronically. All extractions must be completed before the call to ContainerClose returns.
    */
    STDMETHOD(ContainerStreamToFile)(
        __in LPVOID pContext,
        __in_z LPCWSTR wzFileName
        ) = 0;

    /* ContainerStreamToBuffer
    Extract the current stream to a buffer. May not be implemented a-synchronically
    */
    STDMETHOD(ContainerStreamToBuffer)(
        __in LPVOID pContext,
        __out BYTE** ppbBuffer,
        __out SIZE_T* pcbBuffer
        ) = 0;

    /* ContainerSkipStream
    Notifies that the current stream is not needed
    */
    STDMETHOD(ContainerSkipStream)(
        __in LPVOID pContext
        ) = 0;

    /* ContainerClose
    Complete all pending file extractions and release the container.
    */
    STDMETHOD(ContainerClose)(
        __in LPVOID pContext
        ) = 0;

    // BundleExtensionProc - The PFN_BUNDLE_EXTENSION_PROC can call this method to give the BundleExtension raw access to the callback from the engine.
    //                       This might be used to help the BundleExtension support more than one version of the engine.
    STDMETHOD(BundleExtensionProc)(
        __in BUNDLE_EXTENSION_MESSAGE message,
        __in const LPVOID pvArgs,
        __inout LPVOID pvResults,
        __in_opt LPVOID pvContext
        ) = 0;
};
