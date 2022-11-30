#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

HRESULT NetfxPerformDetectNetCoreSdk(
    __in LPCWSTR wzVariable,
    __in NETFX_SEARCH* pSearch,
    __in IBundleExtensionEngine* pEngine,
    __in LPCWSTR wzBaseDirectory
    );
