#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


DECLARE_INTERFACE_IID_(IBundleExtension, IUnknown, "93123C9D-796B-4FCD-A507-6EDEF9A925FD")
{
    STDMETHOD(Search)(
        __in LPCWSTR wzId,
        __in LPCWSTR wzVariable
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
