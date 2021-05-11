// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsUserQuery = L"SELECT `User`, `Component_`, `Name`, `Domain`, `Password` FROM `User` WHERE `User`=?";
enum eUserQuery { vuqUser = 1, vuqComponent, vuqName, vuqDomain, vuqPassword };

LPCWSTR vcsGroupQuery = L"SELECT `Group`, `Component_`, `Name`, `Domain` FROM `Group` WHERE `Group`=?";
enum eGroupQuery { vgqGroup = 1, vgqComponent, vgqName, vgqDomain };

LPCWSTR vcsUserGroupQuery = L"SELECT `User_`, `Group_` FROM `UserGroup` WHERE `User_`=?";
enum eUserGroupQuery { vugqUser = 1, vugqGroup };

LPCWSTR vActionableQuery = L"SELECT `User`,`Component_`,`Name`,`Domain`,`Password`,`Attributes` FROM `User` WHERE `Component_` IS NOT NULL";
enum eActionableQuery { vaqUser = 1, vaqComponent, vaqName, vaqDomain, vaqPassword, vaqAttributes };

HRESULT __stdcall ScaGetUserDeferred(
    __in LPCWSTR wzUser,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __out SCA_USER* pscau
    )
{
    if (!wzUser || !pscau)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    MSIHANDLE hRec, hRecTest;

    LPWSTR pwzData = NULL;

    // clear struct and bail right away if no user key was passed to search for
    ::ZeroMemory(pscau, sizeof(*pscau));
    if (!*wzUser)
    {
        ExitFunction1(hr = S_OK);
    }

    // Reset back to the first record
    WcaFetchWrappedReset(hUserQuery);

    hr = WcaFetchWrappedRecordWhereString(hUserQuery, vuqUser, wzUser, &hRec);
    if (S_OK == hr)
    {
        hr = WcaFetchWrappedRecordWhereString(hUserQuery, vuqUser, wzUser, &hRecTest);
        if (S_OK == hr)
        {
            AssertSz(FALSE, "Found multiple matching User rows");
        }

        hr = WcaGetRecordString(hRec, vuqUser, &pwzData);
        ExitOnFailure(hr, "Failed to get User.User");
        hr = ::StringCchCopyW(pscau->wzKey, countof(pscau->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Component_");
        hr = ::StringCchCopyW(pscau->wzComponent, countof(pscau->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqName, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Name");
        hr = ::StringCchCopyW(pscau->wzName, countof(pscau->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy name string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Domain");
        hr = ::StringCchCopyW(pscau->wzDomain, countof(pscau->wzDomain), pwzData);
        ExitOnFailure(hr, "Failed to copy domain string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqPassword, &pwzData);
        ExitOnFailure(hr, "Failed to get User.Password");
        hr = ::StringCchCopyW(pscau->wzPassword, countof(pscau->wzPassword), pwzData);
        ExitOnFailure(hr, "Failed to copy password string to user object (in deferred CA)");
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate User.User='%ls'", wzUser);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error fetching single User row");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}
