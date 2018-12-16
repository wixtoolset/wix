// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsUserQuery = L"SELECT `User`, `Component_`, `Name`, `Domain`, `Password` FROM `User` WHERE `User`=?";
enum eUserQuery { vuqUser = 1, vuqComponent, vuqName, vuqDomain, vuqPassword };


HRESULT __stdcall ScaGetUser(
    __in LPCWSTR wzUser,
    __out SCA_USER* pscau
    )
{
    if (!wzUser || !pscau)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    PMSIHANDLE hView, hRec;

    LPWSTR pwzData = NULL;

    // clear struct and bail right away if no user key was passed to search for
    ::ZeroMemory(pscau, sizeof(*pscau));
    if (!*wzUser)
    {
        ExitFunction1(hr = S_OK);
    }

    hRec = ::MsiCreateRecord(1);
    hr = WcaSetRecordString(hRec, 1, wzUser);
    ExitOnFailure(hr, "Failed to look up User");

    hr = WcaOpenView(vcsUserQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on User table");
    hr = WcaExecuteView(hView, hRec);
    ExitOnFailure(hr, "Failed to execute view on User table");

    hr = WcaFetchSingleRecord(hView, &hRec);
    if (S_OK == hr)
    {
        hr = WcaGetRecordString(hRec, vuqUser, &pwzData);
        ExitOnFailure(hr, "Failed to get User.User");
        hr = ::StringCchCopyW(pscau->wzKey, countof(pscau->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to user object");

        hr = WcaGetRecordString(hRec, vuqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Component_");
        hr = ::StringCchCopyW(pscau->wzComponent, countof(pscau->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqName, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Name");
        hr = ::StringCchCopyW(pscau->wzName, countof(pscau->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy name string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Domain");
        hr = ::StringCchCopyW(pscau->wzDomain, countof(pscau->wzDomain), pwzData);
        ExitOnFailure(hr, "Failed to copy domain string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqPassword, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Password");
        hr = ::StringCchCopyW(pscau->wzPassword, countof(pscau->wzPassword), pwzData);
        ExitOnFailure(hr, "Failed to copy password string to user object");
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate User.User='%ls'", wzUser);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error or found multiple matching User rows");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}
