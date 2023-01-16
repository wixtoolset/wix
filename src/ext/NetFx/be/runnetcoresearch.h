#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

HRESULT RunNetCoreSearch(
    __in NETFX_NET_CORE_PLATFORM platform,
    __in LPCWSTR wzBaseDirectory,
    __in LPCWSTR wzArguments,
    __inout LPWSTR* psczLatestVersion
    );
