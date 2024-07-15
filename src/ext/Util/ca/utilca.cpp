// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

HRESULT GetDomainFromServerName(
    __deref_out_z LPWSTR* ppwzDomainName,
    __in_z LPCWSTR wzServerName,
    __in DWORD dwFlags
    )
{
    HRESULT hr = S_OK;
    DWORD er = ERROR_SUCCESS;
    PDOMAIN_CONTROLLER_INFOW pDomainControllerInfo = NULL;
    LPCWSTR wz = wzServerName ? wzServerName : L""; // initialize the domain to the provided server name (or empty string).

    // If the server name was not empty, try to get the domain name out of it.
    if (*wz)
    {
        er = ::DsGetDcNameW(NULL, wz, NULL, NULL, dwFlags, &pDomainControllerInfo);
        if (RPC_S_SERVER_UNAVAILABLE == er)
        {
            // MSDN says, if we get the above error code, try again with the "DS_FORCE_REDISCOVERY" flag.
            er = ::DsGetDcNameW(NULL, wz, NULL, NULL, dwFlags | DS_FORCE_REDISCOVERY, &pDomainControllerInfo);
        }
        ExitOnWin32Error(er, hr, "Could not get domain name from server name: %ls", wz);

        if (pDomainControllerInfo->DomainControllerName)
        {
            // Skip the \\ prefix if present.
            if ('\\' == *pDomainControllerInfo->DomainControllerName && '\\' == *(pDomainControllerInfo->DomainControllerName + 1))
            {
                wz = pDomainControllerInfo->DomainControllerName + 2;
            }
            else
            {
                wz = pDomainControllerInfo->DomainControllerName;
            }
        }
    }

LExit:
    // Note: we overwrite the error code here as failure to contact domain controller above is not a fatal error.
    if (wz && *wz)
    {
        hr = StrAllocString(ppwzDomainName, wz, 0);
    }
    else // return NULL the server name ended up empty.
    {
        ReleaseNullStr(*ppwzDomainName);
        hr = S_OK;
    }

    if (pDomainControllerInfo)
    {
        ::NetApiBufferFree((LPVOID)pDomainControllerInfo);
    }

    return hr;
}
