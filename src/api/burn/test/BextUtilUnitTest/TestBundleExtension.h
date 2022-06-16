#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

HRESULT TestBundleExtensionCreate(
    __in IBundleExtensionEngine* pEngine,
    __in const BUNDLE_EXTENSION_CREATE_ARGS* pArgs,
    __inout BUNDLE_EXTENSION_CREATE_RESULTS* pResults,
    __out IBundleExtension** ppBundleExtension
    );
