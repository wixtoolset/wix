#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


/**
 * Locates a domain controller (server name) for a given input domain.
 * Flags can be provided where required (as per those for DsGetDcName) for a specific server to be returned.
 * NOTE: Where the domain provided is identical to the local machine, this function will return NULL, such that the
 * result can be provided directly to NetUserAdd or similar functions.
 *
 * @param pwzDomain Pointer to the domain name to be queried
 * @param ppwzServerName Pointer to the server name to be returned
 * @param flags Flags to be used in the DsGetDcName call(s)
 * @return HRESULT to indicate if an error was encountered
 */
HRESULT GetDomainServerName(LPCWSTR pwzDomain, LPWSTR* ppwzServerName, ULONG flags);
