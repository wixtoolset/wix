// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

typedef enum BALRETRY_TYPE
{
    BALRETRY_TYPE_CACHE_CONTAINER,
    BALRETRY_TYPE_CACHE_PAYLOAD,
    BALRETRY_TYPE_EXECUTE,
} BALRETRY_TYPE;

struct BALRETRY_INFO
{
    LPWSTR sczId;
    DWORD cRetries;
    DWORD dwLastError;
};

static DWORD vdwMaxRetries = 0;
static DWORD vdwTimeout = 0;
static BALRETRY_INFO vrgRetryInfo[3];

// prototypes
static BOOL IsActiveRetryEntry(
    __in BALRETRY_TYPE type,
    __in_z LPCWSTR sczId
    );

static HRESULT StartActiveRetryEntry(
    __in BALRETRY_TYPE type,
    __in_z LPCWSTR sczId
    );


DAPI_(void) BalRetryInitialize(
    __in DWORD dwMaxRetries,
    __in DWORD dwTimeout
    )
{
    BalRetryUninitialize(); // clean everything out.

    vdwMaxRetries = dwMaxRetries;
    vdwTimeout = dwTimeout;
}


DAPI_(void) BalRetryUninitialize()
{
    for (DWORD i = 0; i < countof(vrgRetryInfo); ++i)
    {
        ReleaseStr(vrgRetryInfo[i].sczId);
        memset(vrgRetryInfo + i, 0, sizeof(BALRETRY_INFO));
    }

    vdwMaxRetries = 0;
    vdwTimeout = 0;
}


DAPI_(void) BalRetryStartContainerOrPayload(
    __in_z_opt LPCWSTR wzContainerOrPackageId,
    __in_z_opt LPCWSTR wzPayloadId
    )
{
    if (!wzContainerOrPackageId && !wzPayloadId)
    {
        ReleaseNullStr(vrgRetryInfo[BALRETRY_TYPE_CACHE_CONTAINER].sczId);
        ReleaseNullStr(vrgRetryInfo[BALRETRY_TYPE_CACHE_PAYLOAD].sczId);
    }
    else if (wzPayloadId)
    {
        StartActiveRetryEntry(BALRETRY_TYPE_CACHE_PAYLOAD, wzPayloadId);
    }
    else
    {
        StartActiveRetryEntry(BALRETRY_TYPE_CACHE_CONTAINER, wzContainerOrPackageId);
    }
}


DAPI_(void) BalRetryStartPackage(
    __in_z LPCWSTR wzPackageId
    )
{
    StartActiveRetryEntry(BALRETRY_TYPE_EXECUTE, wzPackageId);
}


DAPI_(void) BalRetryErrorOccurred(
    __in_z LPCWSTR wzPackageId,
    __in DWORD dwError
    )
{
    if (IsActiveRetryEntry(BALRETRY_TYPE_EXECUTE, wzPackageId))
    {
        vrgRetryInfo[BALRETRY_TYPE_EXECUTE].dwLastError = dwError;
    }
}


DAPI_(HRESULT) BalRetryEndContainerOrPayload(
    __in_z_opt LPCWSTR wzContainerOrPackageId,
    __in_z_opt LPCWSTR wzPayloadId,
    __in HRESULT hrError,
    __inout BOOL* pfRetry
    )
{
    HRESULT hr = S_OK;
    BALRETRY_TYPE type = BALRETRY_TYPE_CACHE_PAYLOAD;
    LPCWSTR wzId = NULL;

    if (!wzContainerOrPackageId && !wzPayloadId)
    {
        ReleaseNullStr(vrgRetryInfo[BALRETRY_TYPE_CACHE_CONTAINER].sczId);
        ReleaseNullStr(vrgRetryInfo[BALRETRY_TYPE_CACHE_PAYLOAD].sczId);
        ExitFunction();
    }
    else if (wzPayloadId)
    {
        type = BALRETRY_TYPE_CACHE_PAYLOAD;
        wzId = wzPayloadId;
    }
    else
    {
        type = BALRETRY_TYPE_CACHE_CONTAINER;
        wzId = wzContainerOrPackageId;
    }

    if (FAILED(hrError) && vrgRetryInfo[type].cRetries < vdwMaxRetries && IsActiveRetryEntry(type, wzId))
    {
        // Retry on all errors except the following.
        if (HRESULT_FROM_WIN32(ERROR_INSTALL_USEREXIT) != hrError &&
            BG_E_NETWORK_DISCONNECTED != hrError &&
            HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) != hrError &&
            HRESULT_FROM_WIN32(ERROR_INTERNET_NAME_NOT_RESOLVED) != hrError)
        {
            *pfRetry = TRUE;
        }
    }

LExit:
    return hr;
}


DAPI_(HRESULT) BalRetryEndPackage(
    __in_z LPCWSTR wzPackageId,
    __in HRESULT hrError,
    __inout BOOL* pfRetry
    )
{
    HRESULT hr = S_OK;
    BALRETRY_TYPE type = BALRETRY_TYPE_EXECUTE;

    if (!wzPackageId || !*wzPackageId)
    {
        ReleaseNullStr(vrgRetryInfo[type].sczId);
    }
    else if (FAILED(hrError) && vrgRetryInfo[type].cRetries < vdwMaxRetries && IsActiveRetryEntry(type, wzPackageId))
    {
        // If the service is out of whack, just try again.
        if (HRESULT_FROM_WIN32(ERROR_INSTALL_SERVICE_FAILURE) == hrError)
        {
            *pfRetry = TRUE;
        }
        else if (HRESULT_FROM_WIN32(ERROR_INSTALL_FAILURE) == hrError)
        {
            DWORD dwError = vrgRetryInfo[type].dwLastError;

            // If we failed with one of these specific error codes, then retry since
            // we've seen these have a high success of succeeding on retry.
            if (1303 == dwError ||
                1304 == dwError ||
                1306 == dwError ||
                1307 == dwError ||
                1309 == dwError ||
                1310 == dwError ||
                1311 == dwError ||
                1312 == dwError ||
                1316 == dwError ||
                1317 == dwError ||
                1321 == dwError ||
                1335 == dwError ||
                1402 == dwError ||
                1406 == dwError ||
                1606 == dwError ||
                1706 == dwError ||
                1719 == dwError ||
                1723 == dwError ||
                1923 == dwError ||
                1931 == dwError)
            {
                *pfRetry = TRUE;
            }
        }
        else if (HRESULT_FROM_WIN32(ERROR_INSTALL_ALREADY_RUNNING) == hrError)
        {
            *pfRetry = TRUE;
        }
    }

    return hr;
}


// Internal functions.

static BOOL IsActiveRetryEntry(
    __in BALRETRY_TYPE type,
    __in_z LPCWSTR sczId
    )
{
    BOOL fActive = FALSE;

    fActive = vrgRetryInfo[type].sczId && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, sczId, -1, vrgRetryInfo[type].sczId, -1);

    return fActive;
}

static HRESULT StartActiveRetryEntry(
    __in BALRETRY_TYPE type,
    __in_z LPCWSTR sczId
    )
{
    HRESULT hr = S_OK;

    if (!sczId || !*sczId)
    {
        ReleaseNullStr(vrgRetryInfo[type].sczId);
    }
    else if (IsActiveRetryEntry(type, sczId))
    {
        ++vrgRetryInfo[type].cRetries;
        ::Sleep(vdwTimeout);
    }
    else
    {
        hr = StrAllocString(&vrgRetryInfo[type].sczId, sczId, 0);

        vrgRetryInfo[type].cRetries = 0;
    }

    vrgRetryInfo[type].dwLastError = ERROR_SUCCESS;

    return hr;
}
