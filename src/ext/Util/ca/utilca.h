#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

HRESULT GetDomainFromServerName(
    __deref_out_z LPWSTR* ppwzDomainName,
    __in_z LPCWSTR wzServerName,
    __in DWORD dwFlags
    );
