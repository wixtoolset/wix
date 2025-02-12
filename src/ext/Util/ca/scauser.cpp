// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsUserQuery = L"SELECT `User`, `Component_`, `Name`, `Domain`, `Comment`, `Password` FROM `Wix4User` WHERE `User`=?";
enum eUserQuery { vuqUser = 1, vuqComponent, vuqName, vuqDomain, vuqComment, vuqPassword };

LPCWSTR vcsUserGroupQuery = L"SELECT `User_`, `Group_` FROM `Wix4UserGroup` WHERE `User_`=?";
enum eUserGroupQuery { vugqUser = 1, vugqGroup };

LPCWSTR vActionableQuery = L"SELECT `User`,`Component_`,`Name`,`Domain`,`Password`,`Comment`,`Attributes` FROM `Wix4User` WHERE `Component_` IS NOT NULL";
enum eActionableQuery { vaqUser = 1, vaqComponent, vaqName, vaqDomain, vaqPassword, vaqComment, vaqAttributes };


static HRESULT AddUserToList(
    __inout SCA_USER** ppsuList
    );

static HRESULT AddGroupToList(
    __inout SCA_GROUP** ppsgList
    );


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
    ExitOnFailure(hr, "Failed to open view on Wix4User table");
    hr = WcaExecuteView(hView, hRec);
    ExitOnFailure(hr, "Failed to execute view on Wix4User table");

    hr = WcaFetchSingleRecord(hView, &hRec);
    if (S_OK == hr)
    {
        hr = WcaGetRecordString(hRec, vuqUser, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.User");
        hr = ::StringCchCopyW(pscau->wzKey, countof(pscau->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to user object");

        hr = WcaGetRecordString(hRec, vuqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Component_");
        hr = ::StringCchCopyW(pscau->wzComponent, countof(pscau->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqName, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Name");
        hr = ::StringCchCopyW(pscau->wzName, countof(pscau->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy name string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Domain");
        hr = ::StringCchCopyW(pscau->wzDomain, countof(pscau->wzDomain), pwzData);
        ExitOnFailure(hr, "Failed to copy domain string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqComment, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Comment");
        hr = ::StringCchCopyW(pscau->wzComment, countof(pscau->wzComment), pwzData);
        ExitOnFailure(hr, "Failed to copy comment string to user object");

        hr = WcaGetRecordFormattedString(hRec, vuqPassword, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Password");
        hr = ::StringCchCopyW(pscau->wzPassword, countof(pscau->wzPassword), pwzData);
        ExitOnFailure(hr, "Failed to copy password string to user object");
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate Wix4User.User='%ls'", wzUser);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error or found multiple matching Wix4User rows");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}

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
            AssertSz(FALSE, "Found multiple matching Wix4User rows");
        }

        hr = WcaGetRecordString(hRec, vuqUser, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.User");
        hr = ::StringCchCopyW(pscau->wzKey, countof(pscau->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Component_");
        hr = ::StringCchCopyW(pscau->wzComponent, countof(pscau->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqName, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Name");
        hr = ::StringCchCopyW(pscau->wzName, countof(pscau->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy name string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Domain");
        hr = ::StringCchCopyW(pscau->wzDomain, countof(pscau->wzDomain), pwzData);
        ExitOnFailure(hr, "Failed to copy domain string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqComment, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Comment");
        hr = ::StringCchCopyW(pscau->wzComment, countof(pscau->wzComment), pwzData);
        ExitOnFailure(hr, "Failed to copy comment string to user object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vuqPassword, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4User.Password");
        hr = ::StringCchCopyW(pscau->wzPassword, countof(pscau->wzPassword), pwzData);
        ExitOnFailure(hr, "Failed to copy password string to user object (in deferred CA)");
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate Wix4User.User='%ls'", wzUser);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error fetching single Wix4User row");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}

void ScaUserFreeList(
    __in SCA_USER* psuList
    )
{
    SCA_USER* psuDelete = psuList;
    while (psuList)
    {
        psuDelete = psuList;
        psuList = psuList->psuNext;

        ScaGroupFreeList(psuDelete->psgGroups);
        MemFree(psuDelete);
    }
}


HRESULT ScaUserRead(
    __out SCA_USER** ppsuList
    )
{
    //Assert(FALSE);
    Assert(ppsuList);

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    PMSIHANDLE hView, hRec, hUserRec, hUserGroupView;

    LPWSTR pwzData = NULL;

    BOOL fUserGroupExists = FALSE;

    SCA_USER *psu = NULL;

    INSTALLSTATE isInstalled, isAction;

    if (S_OK != WcaTableExists(L"Wix4User"))
    {
        WcaLog(LOGMSG_VERBOSE, "Wix4User Table does not exist, exiting");
        ExitFunction1(hr = S_FALSE);
    }

    if (S_OK == WcaTableExists(L"Wix4UserGroup"))
    {
        fUserGroupExists = TRUE;
    }

    //
    // loop through all the users
    //
    hr = WcaOpenExecuteView(vActionableQuery, &hView);
    ExitOnFailure(hr, "failed to open view on Wix4User table");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, vaqComponent, &pwzData);
        ExitOnFailure(hr, "failed to get Wix4User.Component");

        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get Component state for Wix4User");

        // don't bother if we aren't installing or uninstalling this component
        if (WcaIsInstalling(isInstalled, isAction) || WcaIsUninstalling(isInstalled, isAction))
        {
            //
            // Add the user to the list and populate it's values
            //
            hr = AddUserToList(ppsuList);
            ExitOnFailure(hr, "failed to add user to list");

            psu = *ppsuList;

            psu->isInstalled = isInstalled;
            psu->isAction = isAction;
            hr = ::StringCchCopyW(psu->wzComponent, countof(psu->wzComponent), pwzData);
            ExitOnFailure(hr, "failed to copy component name: %ls", pwzData);

            hr = WcaGetRecordString(hRec, vaqUser, &pwzData);
            ExitOnFailure(hr, "failed to get Wix4User.User");
            hr = ::StringCchCopyW(psu->wzKey, countof(psu->wzKey), pwzData);
            ExitOnFailure(hr, "failed to copy user key: %ls", pwzData);

            hr = WcaGetRecordFormattedString(hRec, vaqName, &pwzData);
            ExitOnFailure(hr, "failed to get Wix4User.Name");
            hr = ::StringCchCopyW(psu->wzName, countof(psu->wzName), pwzData);
            ExitOnFailure(hr, "failed to copy user name: %ls", pwzData);

            hr = WcaGetRecordFormattedString(hRec, vaqDomain, &pwzData);
            ExitOnFailure(hr, "failed to get Wix4User.Domain");
            hr = ::StringCchCopyW(psu->wzDomain, countof(psu->wzDomain), pwzData);
            ExitOnFailure(hr, "failed to copy user domain: %ls", pwzData);
            hr = WcaGetRecordFormattedString(hRec, vaqComment, &pwzData);
            ExitOnFailure(hr, "failed to get Wix4User.Comment");
            hr = ::StringCchCopyW(psu->wzComment, countof(psu->wzComment), pwzData);
            ExitOnFailure(hr, "failed to copy user comment: %ls", pwzData);

            hr = WcaGetRecordFormattedString(hRec, vaqPassword, &pwzData);
            ExitOnFailure(hr, "failed to get Wix4User.Password");
            hr = ::StringCchCopyW(psu->wzPassword, countof(psu->wzPassword), pwzData);
            ExitOnFailure(hr, "failed to copy user password");

            hr = WcaGetRecordInteger(hRec, vaqAttributes, &psu->iAttributes);
            ExitOnFailure(hr, "failed to get Wix4User.Attributes");

            // Check if this user is to be added to any groups
            if (fUserGroupExists)
            {
                hUserRec = ::MsiCreateRecord(1);
                hr = WcaSetRecordString(hUserRec, 1, psu->wzKey);
                ExitOnFailure(hr, "Failed to create user record for querying Wix4UserGroup table");

                hr = WcaOpenView(vcsUserGroupQuery, &hUserGroupView);
                ExitOnFailure(hr, "Failed to open view on Wix4UserGroup table for user %ls", psu->wzKey);
                hr = WcaExecuteView(hUserGroupView, hUserRec);
                ExitOnFailure(hr, "Failed to execute view on Wix4UserGroup table for user: %ls", psu->wzKey);

                while (S_OK == (hr = WcaFetchRecord(hUserGroupView, &hRec)))
                {
                    hr = WcaGetRecordString(hRec, vugqGroup, &pwzData);
                    ExitOnFailure(hr, "failed to get Wix4UserGroup.Group");

                    hr = AddGroupToList(&(psu->psgGroups));
                    ExitOnFailure(hr, "failed to add group to list");

                    hr = ScaGetGroup(pwzData, psu->psgGroups);
                    ExitOnFailure(hr, "failed to get information for group: %ls", pwzData);
                }

                if (E_NOMOREITEMS == hr)
                {
                    hr = S_OK;
                }
                ExitOnFailure(hr, "failed to enumerate selected rows from Wix4UserGroup table");
            }
        }
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failed to enumerate selected rows from Wix4User table");

LExit:
    ReleaseStr(pwzData);

    return hr;
}


static HRESULT WriteGroupInfo(
    __in SCA_GROUP* psgList,
    __in LPWSTR *ppwzActionData
    )
{
    HRESULT hr = S_OK;

    for (SCA_GROUP* psg = psgList; psg; psg = psg->psgNext)
    {
        hr = WcaWriteStringToCaData(psg->wzName, ppwzActionData);
        ExitOnFailure(hr, "failed to add group name to custom action data: %ls", psg->wzName);

        hr = WcaWriteStringToCaData(psg->wzDomain, ppwzActionData);
        ExitOnFailure(hr, "failed to add group domain to custom action data: %ls", psg->wzDomain);
    }

LExit:
    return hr;
}


// Behaves like WriteGroupInfo, but it filters out groups the user is currently a member of,
// because we don't want to rollback those
static HRESULT WriteGroupRollbackInfo(
    __in LPCWSTR pwzName,
    __in LPCWSTR pwzDomain,
    __in SCA_GROUP* psgList,
    __in LPWSTR *ppwzActionData
    )
{
    HRESULT hr = S_OK;
    BOOL fIsMember = FALSE;

    for (SCA_GROUP* psg = psgList; psg; psg = psg->psgNext)
    {
        hr = UserCheckIsMember(pwzName, pwzDomain, psg->wzName, psg->wzDomain, &fIsMember);
        if (FAILED(hr))
        {
            WcaLog(LOGMSG_VERBOSE, "Failed to check if user: %ls (domain: %ls) is member of a group while collecting rollback information (error code 0x%x) - continuing", pwzName, pwzDomain, hr);
            hr = S_OK;
            continue;
        }

        // If the user is currently a member, we don't want to undo that on rollback, so skip adding
        // this group record to the list of groups to rollback
        if (fIsMember)
        {
            continue;
        }

        hr = WcaWriteStringToCaData(psg->wzName, ppwzActionData);
        ExitOnFailure(hr, "failed to add group name to custom action data: %ls", psg->wzName);

        hr = WcaWriteStringToCaData(psg->wzDomain, ppwzActionData);
        ExitOnFailure(hr, "failed to add group domain to custom action data: %ls", psg->wzDomain);
    }

LExit:
    return hr;
}


/* ****************************************************************
ScaUserExecute - Schedules user account creation or removal based on
component state.

******************************************************************/
HRESULT ScaUserExecute(
    __in SCA_USER *psuList
    )
{
    HRESULT hr = S_OK;
    DWORD er = 0;
    LPWSTR pwzDomainName = NULL;

    LPWSTR pwzBaseScriptKey = NULL;
    DWORD cScriptKey = 0;

    USER_INFO_0 *pUserInfo = NULL;
    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzActionData = NULL;
    LPWSTR pwzRollbackData = NULL;

    // Get the base script key for this CustomAction.
    hr = WcaCaScriptCreateKey(&pwzBaseScriptKey);
    ExitOnFailure(hr, "Failed to get encoding key.");

    // Loop through all the users to be configured.
    for (SCA_USER *psu = psuList; psu; psu = psu->psuNext)
    {
        USER_EXISTS ueUserExists = USER_EXISTS_INDETERMINATE;

        // Always put the User Name, Domain, and Comment on the front of the CustomAction data.
        // The attributes will be added when we have finished adjusting them. Sometimes we'll
        // add more data.
        Assert(psu->wzName);
        hr = WcaWriteStringToCaData(psu->wzName, &pwzActionData);
        ExitOnFailure(hr, "Failed to add user name to custom action data: %ls", psu->wzName);
        hr = WcaWriteStringToCaData(psu->wzDomain, &pwzActionData);
        ExitOnFailure(hr, "Failed to add user domain to custom action data: %ls", psu->wzDomain);
        hr = WcaWriteStringToCaData(psu->wzComment, &pwzActionData);
        ExitOnFailure(hr, "Failed to add user comment to custom action data: %ls", psu->wzComment);

        // Check to see if the user already exists since we have to be very careful when adding
        // and removing users.
        hr = GetDomainFromServerName(&pwzDomainName, psu->wzDomain, 0);
        ExitOnFailure(hr, "Failed to get domain from server name: %ls", psu->wzDomain);

        er = ::NetUserGetInfo(pwzDomainName, psu->wzName, 0, reinterpret_cast<LPBYTE*>(&pUserInfo));
        if (NERR_Success == er)
        {
            ueUserExists = USER_EXISTS_YES;
        }
        else if (NERR_UserNotFound == er)
        {
            ueUserExists = USER_EXISTS_NO;
        }
        else
        {
            ueUserExists = USER_EXISTS_INDETERMINATE;
            hr = HRESULT_FROM_WIN32(er);
            WcaLog(LOGMSG_VERBOSE, "Failed to check existence of domain: %ls, user: %ls (error code 0x%x) - continuing", pwzDomainName, psu->wzName, hr);
            hr = S_OK;
            er = ERROR_SUCCESS;
        }

        if (WcaIsInstalling(psu->isInstalled, psu->isAction))
        {
            // If the user exists, check to see if we are supposed to fail if the user exists before
            // the install.
            if (USER_EXISTS_YES == ueUserExists)
            {
                // Re-installs will always fail if we don't remove the check for "fail if exists".
                if (WcaIsReInstalling(psu->isInstalled, psu->isAction))
                {
                    psu->iAttributes &= ~SCAU_FAIL_IF_EXISTS;

                    // If install would create the user, re-install should be able to update the user.
                    if (!(psu->iAttributes & SCAU_DONT_CREATE_USER))
                    {
                        psu->iAttributes |= SCAU_UPDATE_IF_EXISTS;
                    }
                }

                if (SCAU_FAIL_IF_EXISTS & psu->iAttributes && !(SCAU_UPDATE_IF_EXISTS & psu->iAttributes))
                {
                    hr = HRESULT_FROM_WIN32(NERR_UserExists);
                    MessageExitOnFailure(hr, msierrUSRFailedUserCreateExists, "Failed to create user: %ls because user already exists.", psu->wzName);
                }
            }

            hr = WcaWriteIntegerToCaData(psu->iAttributes, &pwzActionData);
            ExitOnFailure(hr, "failed to add user attributes to custom action data for user: %ls", psu->wzKey);

            // Rollback only if the user already exists, we couldn't determine if the user exists, or we are going to create the user
            if ((USER_EXISTS_YES == ueUserExists) || (USER_EXISTS_INDETERMINATE == ueUserExists) || !(psu->iAttributes & SCAU_DONT_CREATE_USER))
            {
                ++cScriptKey;
                hr = StrAllocFormatted(&pwzScriptKey, L"%ls%u", pwzBaseScriptKey, cScriptKey);
                ExitOnFailure(hr, "Failed to create encoding key.");

                // Write the script key to CustomActionData for install and rollback so information can be passed to rollback.
                hr = WcaWriteStringToCaData(pwzScriptKey, &pwzActionData);
                ExitOnFailure(hr, "Failed to add encoding key to custom action data.");

                hr = WcaWriteStringToCaData(pwzScriptKey, &pwzRollbackData);
                ExitOnFailure(hr, "Failed to add encoding key to rollback custom action data.");

                INT iRollbackUserAttributes = psu->iAttributes;

                // If the user already exists, ensure this is accounted for in rollback
                if (USER_EXISTS_YES == ueUserExists)
                {
                    iRollbackUserAttributes |= SCAU_DONT_CREATE_USER;
                }
                else
                {
                    iRollbackUserAttributes &= ~SCAU_DONT_CREATE_USER;
                }

                // The deferred CA determines when to rollback User Rights Assignments so these should never be set.
                iRollbackUserAttributes &= ~SCAU_ALLOW_LOGON_AS_SERVICE;
                iRollbackUserAttributes &= ~SCAU_ALLOW_LOGON_AS_BATCH;

                hr = WcaWriteStringToCaData(psu->wzName, &pwzRollbackData);
                ExitOnFailure(hr, "Failed to add user name to rollback custom action data: %ls", psu->wzName);
                hr = WcaWriteStringToCaData(psu->wzDomain, &pwzRollbackData);
                ExitOnFailure(hr, "Failed to add user domain to rollback custom action data: %ls", psu->wzDomain);
                hr = WcaWriteIntegerToCaData(iRollbackUserAttributes, &pwzRollbackData);
                ExitOnFailure(hr, "failed to add user attributes to rollback custom action data for user: %ls", psu->wzKey);
                // If the user already exists, add relevant group information to rollback data
                if (USER_EXISTS_YES == ueUserExists || USER_EXISTS_INDETERMINATE == ueUserExists)
                {
                    hr = WriteGroupRollbackInfo(psu->wzName, psu->wzDomain, psu->psgGroups, &pwzRollbackData);
                    ExitOnFailure(hr, "failed to add group information to rollback custom action data");
                }

                hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"CreateUserRollback"), pwzRollbackData, COST_USER_DELETE);
                ExitOnFailure(hr, "failed to schedule CreateUserRollback");
            }
            else
            {
                // Write empty script key to CustomActionData since there is no rollback.
                hr = WcaWriteStringToCaData(L"", &pwzActionData);
                ExitOnFailure(hr, "Failed to add empty encoding key to custom action data.");
            }

            //
            // Schedule the creation now.
            //
            hr = WcaWriteStringToCaData(psu->wzPassword, &pwzActionData);
            ExitOnFailure(hr, "failed to add user password to custom action data for user: %ls", psu->wzKey);

            // Add user's group information to custom action data
            hr = WriteGroupInfo(psu->psgGroups, &pwzActionData);
            ExitOnFailure(hr, "failed to add group information to custom action data");

            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"CreateUser"), pwzActionData, COST_USER_ADD);
            ExitOnFailure(hr, "failed to schedule CreateUser");
        }
        else if (((USER_EXISTS_YES == ueUserExists) || (USER_EXISTS_INDETERMINATE == ueUserExists)) && WcaIsUninstalling(psu->isInstalled, psu->isAction) && !(psu->iAttributes & SCAU_DONT_REMOVE_ON_UNINSTALL))
        {
            hr = WcaWriteIntegerToCaData(psu->iAttributes, &pwzActionData);
            ExitOnFailure(hr, "failed to add user attributes to custom action data for user: %ls", psu->wzKey);

            // Add user's group information - this will ensure the user can be removed from any groups they were added to, if the user isn't be deleted
            hr = WriteGroupInfo(psu->psgGroups, &pwzActionData);
            ExitOnFailure(hr, "failed to add group information to custom action data");

            // Schedule the removal because the user exists and we don't have any flags set
            // that say, don't remove the user on uninstall.
            //
            // Note: We can't rollback the removal of a user which is why RemoveUser is a commit
            // CustomAction.
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"RemoveUser"), pwzActionData, COST_USER_DELETE);
            ExitOnFailure(hr, "failed to schedule RemoveUser");
        }

        ReleaseNullStr(pwzScriptKey);
        ReleaseNullStr(pwzActionData);
        ReleaseNullStr(pwzRollbackData);
        if (pUserInfo)
        {
            ::NetApiBufferFree(static_cast<LPVOID>(pUserInfo));
            pUserInfo = NULL;
        }
    }

LExit:
    ReleaseStr(pwzBaseScriptKey);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzActionData);
    ReleaseStr(pwzRollbackData);
    ReleaseStr(pwzDomainName);

    if (pUserInfo)
    {
        ::NetApiBufferFree(static_cast<LPVOID>(pUserInfo));
    }

    return hr;
}


static HRESULT AddUserToList(
    __inout SCA_USER** ppsuList
    )
{
    HRESULT hr = S_OK;
    SCA_USER* psu = static_cast<SCA_USER*>(MemAlloc(sizeof(SCA_USER), TRUE));
    ExitOnNull(psu, hr, E_OUTOFMEMORY, "failed to allocate memory for new user list element");

    psu->psuNext = *ppsuList;
    *ppsuList = psu;

LExit:
    return hr;
}


static HRESULT AddGroupToList(
    __inout SCA_GROUP** ppsgList
    )
{
    HRESULT hr = S_OK;
    SCA_GROUP* psg = static_cast<SCA_GROUP*>(MemAlloc(sizeof(SCA_GROUP), TRUE));
    ExitOnNull(psg, hr, E_OUTOFMEMORY, "failed to allocate memory for new group list element");

    psg->psgNext = *ppsgList;
    *ppsgList = psg;

LExit:
    return hr;
}
