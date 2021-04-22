#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

/*******************************************************************
 BalRetryInitialize - initialize the retry count and timeout between
                      retries (in milliseconds).
********************************************************************/
DAPI_(void) BalRetryInitialize(
    __in DWORD dwMaxRetries,
    __in DWORD dwTimeout
    );

/*******************************************************************
 BalRetryUninitialize - call to cleanup any memory allocated during
                        use of the retry utility.
********************************************************************/
DAPI_(void) BalRetryUninitialize();

/*******************************************************************
 BalRetryStartPackage - call when a package begins to be modified. If
                        the package is being retried, the function will
                        wait the specified timeout.
********************************************************************/
DAPI_(void) BalRetryStartPackage(
    __in_z LPCWSTR wzPackageId
    );

/*******************************************************************
 BalRetryErrorOccured - call when an error occurs for the retry utility
                        to consider.
********************************************************************/
DAPI_(void) BalRetryErrorOccurred(
    __in_z LPCWSTR wzPackageId,
    __in DWORD dwError
    );

/*******************************************************************
 BalRetryEndPackage - returns TRUE if a retry is recommended.
********************************************************************/
DAPI_(HRESULT) BalRetryEndPackage(
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrError,
    __inout BOOL* pfRetry
    );

/*******************************************************************
 BalRetryStartContainerOrPayload - call when a container or payload
        begins to be acquired. If the target is being retried,
        the function will wait the specified timeout.
********************************************************************/
DAPI_(void) BalRetryStartContainerOrPayload(
    __in_z_opt LPCWSTR wzContainerOrPackageId,
    __in_z_opt LPCWSTR wzPayloadId
    );

/*******************************************************************
 BalRetryEndContainerOrPayload - returns TRUE if a retry is recommended.
********************************************************************/
DAPI_(HRESULT) BalRetryEndContainerOrPayload(
    __in_z_opt LPCWSTR wzContainerOrPackageId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrError,
    __inout BOOL* pfRetry
    );


#ifdef __cplusplus
}
#endif
