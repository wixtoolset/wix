// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "scanet.h"

LPCWSTR vcsGroupQuery = L"SELECT `Group`, `Component_`, `Name`, `Domain` FROM `Wix4Group` WHERE `Group`=?";
enum eGroupQuery { vgqGroup = 1, vgqComponent, vgqName, vgqDomain };

LPCWSTR vcsGroupParentsQuery = L"SELECT `Parent_`,`Component_`,`Name`,`Domain`,`Child_` FROM `Wix6GroupGroup`,`Wix4Group` WHERE `Wix6GroupGroup`.`Parent_`=`Wix4Group`.`Group` AND `Wix6GroupGroup`.`Child_`=?";
enum eGroupParentsQuery { vgpqParent = 1, vgpqParentComponent, vgpqParentName, vgpqParentDomain, vgpqChild };

LPCWSTR vcsGroupChildrenQuery = L"SELECT `Parent_`,`Child_`,`Component_`,`Name`,`Domain` FROM `Wix6GroupGroup`,`Wix4Group` WHERE `Wix6GroupGroup`.`Child_`=`Wix4Group`.`Group` AND `Wix6GroupGroup`.`Parent_`=?";
enum eGroupChildrenQuery { vgcqParent = 1, vgcqChild, vgcqChildComponent, vgcqChildName, vgcqChildDomain };

LPCWSTR vActionableGroupQuery = L"SELECT `Group`,`Component_`,`Name`,`Domain`,`Comment`,`Attributes` FROM `Wix4Group`,`Wix6Group` WHERE `Component_` IS NOT NULL AND `Group`=`Group_`";
enum eActionableGroupQuery { vagqGroup = 1, vagqComponent, vagqName, vagqDomain, vagqComment, vagqAttributes };

static HRESULT AddGroupToList(
    __inout SCA_GROUP** ppsgList
    );

HRESULT __stdcall ScaGetGroup(
    __in LPCWSTR wzGroup,
    __out SCA_GROUP* pscag
    )
{
    if (!wzGroup || *wzGroup==0 || !pscag)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    PMSIHANDLE hView, hRec;

    LPWSTR pwzData = NULL;

    hRec = ::MsiCreateRecord(1);
    hr = WcaSetRecordString(hRec, 1, wzGroup);
    ExitOnFailure(hr, "Failed to look up Group");

    hr = WcaOpenView(vcsGroupQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on Wix4Group table");
    hr = WcaExecuteView(hView, hRec);
    ExitOnFailure(hr, "Failed to execute view on Wix4Group table");

    hr = WcaFetchSingleRecord(hView, &hRec);
    if (S_OK == hr)
    {
        hr = WcaGetRecordString(hRec, vgqGroup, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4Group.Group");
        hr = ::StringCchCopyW(pscag->wzKey, countof(pscag->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to group object");

        hr = WcaGetRecordString(hRec, vgqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4Group.Component_");
        hr = ::StringCchCopyW(pscag->wzComponent, countof(pscag->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to group object");

        hr = WcaGetRecordFormattedString(hRec, vgqName, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4Group.Name");
        hr = ::StringCchCopyW(pscag->wzName, countof(pscag->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy name string to group object");

        hr = WcaGetRecordFormattedString(hRec, vgqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4Group.Domain");
        hr = ::StringCchCopyW(pscag->wzDomain, countof(pscag->wzDomain), pwzData);
        ExitOnFailure(hr, "Failed to copy domain string to group object");
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate Wix4Group.Group='%ls'", wzGroup);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error or found multiple matching Wix4Group rows");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}

HRESULT __stdcall ScaGetGroupDeferred(
    __in LPCWSTR wzGroup,
    __in WCA_WRAPQUERY_HANDLE hGroupQuery,
    __out SCA_USER* pscag
    )
{
    if (!wzGroup || !pscag)
    {
        return E_INVALIDARG;
    }

    HRESULT hr = S_OK;
    MSIHANDLE hRec, hRecTest;

    LPWSTR pwzData = NULL;

    // clear struct and bail right away if no group key was passed to search for
    ::ZeroMemory(pscag, sizeof(*pscag));
    if (!*wzGroup)
    {
        ExitFunction1(hr = S_OK);
    }

    // Reset back to the first record
    WcaFetchWrappedReset(hGroupQuery);

    hr = WcaFetchWrappedRecordWhereString(hGroupQuery, vgqGroup, wzGroup, &hRec);
    if (S_OK == hr)
    {
        hr = WcaFetchWrappedRecordWhereString(hGroupQuery, vgqGroup, wzGroup, &hRecTest);
        if (S_OK == hr)
        {
            AssertSz(FALSE, "Found multiple matching Wix4Group rows");
        }

        hr = WcaGetRecordString(hRec, vgqGroup, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4Group.Group");
        hr = ::StringCchCopyW(pscag->wzKey, countof(pscag->wzKey), pwzData);
        ExitOnFailure(hr, "Failed to copy key string to group object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vgqComponent, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4Group.Component_");
        hr = ::StringCchCopyW(pscag->wzComponent, countof(pscag->wzComponent), pwzData);
        ExitOnFailure(hr, "Failed to copy component string to group object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vgqName, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4Group.Name");
        hr = ::StringCchCopyW(pscag->wzName, countof(pscag->wzName), pwzData);
        ExitOnFailure(hr, "Failed to copy name string to group object (in deferred CA)");

        hr = WcaGetRecordString(hRec, vgqDomain, &pwzData);
        ExitOnFailure(hr, "Failed to get Wix4Group.Domain");
        hr = ::StringCchCopyW(pscag->wzDomain, countof(pscag->wzDomain), pwzData);
        ExitOnFailure(hr, "Failed to copy domain string to group object (in deferred CA)");
    }
    else if (E_NOMOREITEMS == hr)
    {
        WcaLog(LOGMSG_STANDARD, "Error: Cannot locate Wix4Group.Group='%ls'", wzGroup);
        hr = E_FAIL;
    }
    else
    {
        ExitOnFailure(hr, "Error fetching single Wix4Group row");
    }

LExit:
    ReleaseStr(pwzData);

    return hr;
}

void ScaGroupFreeList(
    __in SCA_GROUP* psgList
    )
{
    SCA_GROUP* psgDelete = psgList;
    while (psgList)
    {
        psgDelete = psgList;
        psgList = psgList->psgNext;
        ScaGroupFreeList(psgDelete->psgParents);
        ScaGroupFreeList(psgDelete->psgChildren);

        MemFree(psgDelete);
    }
}

HRESULT ScaGroupGetParents(
    __inout SCA_GROUP* psg
)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    SCA_GROUP* psgParent = NULL;
    PMSIHANDLE hView, hParamRec, hRec;
    LPWSTR pwzTempStr = NULL;

    if (S_OK != WcaTableExists(L"Wix6GroupGroup"))
    {
        WcaLog(LOGMSG_VERBOSE, "Wix6GroupGroup Table does not exist, exiting");
        ExitFunction1(hr = S_FALSE);
    }

    // setup the query parameter record
    hParamRec = ::MsiCreateRecord(1);
    hr = WcaSetRecordString(hParamRec, 1, psg->wzKey);

    //
    // loop through all the groups
    //
    hr = WcaOpenView(vcsGroupParentsQuery, &hView);
    ExitOnFailure(hr, "failed to open view on Wix6GroupGroup,Wix4Group table(s)");
    hr = WcaExecuteView(hView, hParamRec);
    ExitOnFailure(hr, "failed to open view on Wix4Group,Wix6Group table(s)");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = AddGroupToList(&psg->psgParents);
        ExitOnFailure(hr, "failed to add group to list");

        psgParent = psg->psgParents;

        if (::MsiRecordIsNull(hRec, vgcqChildComponent))
        {
            psgParent->isInstalled = INSTALLSTATE_NOTUSED;
            psgParent->isAction = INSTALLSTATE_NOTUSED;
        }
        else
        {
            hr = WcaGetRecordString(hRec, vgpqParentComponent, &pwzTempStr);
            ExitOnFailure(hr, "failed to get Wix4Group.Component");
            wcsncpy_s(psgParent->wzComponent, pwzTempStr, MAX_DARWIN_KEY);
            ReleaseNullStr(pwzTempStr);

            er = ::MsiGetComponentStateW(WcaGetInstallHandle(), psgParent->wzComponent, &psgParent->isInstalled, &psgParent->isAction);
            hr = HRESULT_FROM_WIN32(er);
            ExitOnFailure(hr, "failed to get Component state for Wix4Group");
        }

        hr = WcaGetRecordString(hRec, vgpqParentName, &pwzTempStr);
        ExitOnFailure(hr, "failed to get Wix4Group.Name");
        wcsncpy_s(psgParent->wzName, pwzTempStr, MAX_DARWIN_COLUMN);
        ReleaseNullStr(pwzTempStr);


        hr = WcaGetRecordString(hRec, vgpqParentDomain, &pwzTempStr);
        ExitOnFailure(hr, "failed to get Wix4Group.Domain");
        wcsncpy_s(psgParent->wzDomain, pwzTempStr, MAX_DARWIN_COLUMN);
        ReleaseNullStr(pwzTempStr);
    }

LExit:
    ReleaseNullStr(pwzTempStr);
    return hr;
}

HRESULT ScaGroupGetChildren(
    __inout SCA_GROUP* psg
)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    SCA_GROUP* psgChild = NULL;
    PMSIHANDLE hView, hParamRec, hRec;
    LPWSTR pwzTempStr = NULL;

    if (S_OK != WcaTableExists(L"Wix6GroupGroup"))
    {
        WcaLog(LOGMSG_VERBOSE, "Wix6GroupGroup Table does not exist, exiting");
        ExitFunction1(hr = S_FALSE);
    }

    // setup the query parameter record
    hParamRec = ::MsiCreateRecord(1);
    hr = WcaSetRecordString(hParamRec, 1, psg->wzKey);

    //
    // loop through all the groups
    //
    hr = WcaOpenView(vcsGroupChildrenQuery, &hView);
    ExitOnFailure(hr, "failed to open view on Wix6GroupGroup,Wix4Group table(s)");
    hr = WcaExecuteView(hView, hParamRec);
    ExitOnFailure(hr, "failed to open view on Wix4Group,Wix6Group table(s)");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = AddGroupToList(&psg->psgChildren);
        ExitOnFailure(hr, "failed to add group to list");

        psgChild = psg->psgChildren;

        if (::MsiRecordIsNull(hRec, vgcqChildComponent))
        {
            psgChild->isInstalled = INSTALLSTATE_NOTUSED;
            psgChild->isAction = INSTALLSTATE_NOTUSED;
        }
        else
        {
            hr = WcaGetRecordString(hRec, vgcqChildComponent, &pwzTempStr);
            ExitOnFailure(hr, "failed to get Wix4Group.Component");
            wcsncpy_s(psgChild->wzComponent, pwzTempStr, MAX_DARWIN_KEY);
            ReleaseNullStr(pwzTempStr);

            er = ::MsiGetComponentStateW(WcaGetInstallHandle(), psgChild->wzComponent, &psgChild->isInstalled, &psgChild->isAction);
            hr = HRESULT_FROM_WIN32(er);
            ExitOnFailure(hr, "failed to get Component state for Wix4Group");
        }

        hr = WcaGetRecordString(hRec, vgcqChildName, &pwzTempStr);
        ExitOnFailure(hr, "failed to get Wix4Group.Name");
        wcsncpy_s(psgChild->wzName, pwzTempStr, MAX_DARWIN_COLUMN);
        ReleaseNullStr(pwzTempStr);


        hr = WcaGetRecordString(hRec, vgcqChildDomain, &pwzTempStr);
        ExitOnFailure(hr, "failed to get Wix4Group.Domain");
        wcsncpy_s(psgChild->wzDomain, pwzTempStr, MAX_DARWIN_COLUMN);
        ReleaseNullStr(pwzTempStr);
    }

LExit:
    ReleaseNullStr(pwzTempStr);
    return hr;
}


HRESULT ScaGroupRead(
    __out SCA_GROUP** ppsgList
    )
{
    //Assert(FALSE);
    Assert(ppsgList);

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    PMSIHANDLE hView, hRec, hGroupRec, hGroupGroupView;

    LPWSTR pwzData = NULL;

    //BOOL fGroupGroupExists = FALSE;

    SCA_GROUP *psg = NULL;

    INSTALLSTATE isInstalled, isAction;

    if (S_OK != WcaTableExists(L"Wix4Group"))
    {
        WcaLog(LOGMSG_VERBOSE, "Wix4Group Table does not exist, exiting");
        ExitFunction1(hr = S_FALSE);
    }
    if (S_OK != WcaTableExists(L"Wix6Group"))
    {
        WcaLog(LOGMSG_VERBOSE, "Wix6Group Table does not exist, exiting");
        ExitFunction1(hr = S_FALSE);
    }

    //
    // loop through all the groups
    //
    hr = WcaOpenExecuteView(vActionableGroupQuery, &hView);
    ExitOnFailure(hr, "failed to open view on Wix4Group,Wix6Group table(s)");
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordString(hRec, vagqComponent, &pwzData);
        ExitOnFailure(hr, "failed to get Wix4Group.Component");

        er = ::MsiGetComponentStateW(WcaGetInstallHandle(), pwzData, &isInstalled, &isAction);
        hr = HRESULT_FROM_WIN32(er);
        ExitOnFailure(hr, "failed to get Component state for Wix4Group");

        // don't bother if we aren't installing or uninstalling this component
        if (WcaIsInstalling(isInstalled, isAction) || WcaIsUninstalling(isInstalled, isAction))
        {
            //
            // Add the group to the list and populate it's values
            //
            hr = AddGroupToList(ppsgList);
            ExitOnFailure(hr, "failed to add group to list");

            psg = *ppsgList;

            psg->isInstalled = isInstalled;
            psg->isAction = isAction;
            hr = ::StringCchCopyW(psg->wzComponent, countof(psg->wzComponent), pwzData);
            ExitOnFailure(hr, "failed to copy component name: %ls", pwzData);
            ReleaseNullStr(pwzData);

            hr = WcaGetRecordString(hRec, vagqGroup, &pwzData);
            ExitOnFailure(hr, "failed to get Wix4Group.Group");
            hr = ::StringCchCopyW(psg->wzKey, countof(psg->wzKey), pwzData);
            ExitOnFailure(hr, "failed to copy group key: %ls", pwzData);
            ReleaseNullStr(pwzData);

            hr = WcaGetRecordFormattedString(hRec, vagqName, &pwzData);
            ExitOnFailure(hr, "failed to get Wix4Group.Name");
            hr = ::StringCchCopyW(psg->wzName, countof(psg->wzName), pwzData);
            ExitOnFailure(hr, "failed to copy group name: %ls", pwzData);
            ReleaseNullStr(pwzData);

            hr = WcaGetRecordFormattedString(hRec, vagqDomain, &pwzData);
            ExitOnFailure(hr, "failed to get Wix4Group.Domain");
            hr = ::StringCchCopyW(psg->wzDomain, countof(psg->wzDomain), pwzData);
            ExitOnFailure(hr, "failed to copy group domain: %ls", pwzData);
            ReleaseNullStr(pwzData);

            hr = WcaGetRecordFormattedString(hRec, vagqComment, &pwzData);
            ExitOnFailure(hr, "failed to get Wix6Group.Comment");
            hr = ::StringCchCopyW(psg->wzComment, countof(psg->wzComment), pwzData);
            ExitOnFailure(hr, "failed to copy group comment: %ls", pwzData);

            hr = WcaGetRecordInteger(hRec, vagqAttributes, &psg->iAttributes);
            ExitOnFailure(hr, "failed to get Wix6Group.Attributes");

            ScaGroupGetParents(psg);

            ScaGroupGetChildren(psg);
        }
    }

    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failed to enumerate selected rows from Wix4Group table");

LExit:
    ReleaseStr(pwzData);

    return hr;
}

/* ****************************************************************
ScaGroupExecute - Schedules group account creation or removal based on
component state.
******************************************************************/
HRESULT ScaGroupExecute(
    __in SCA_GROUP *psgList
    )
{
    HRESULT hr = S_OK;
    DWORD er = 0;

    LPWSTR pwzBaseScriptKey = NULL;
    DWORD cScriptKey = 0;

    LOCALGROUP_INFO_0 *pGroupInfo = NULL;
    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzActionData = NULL;
    LPWSTR pwzRollbackData = NULL;
    LPWSTR pwzServerName = NULL;

    // Get the base script key for this CustomAction.
    hr = WcaCaScriptCreateKey(&pwzBaseScriptKey);
    ExitOnFailure(hr, "Failed to get encoding key.");

    // Loop through all the users to be configured.
    for (SCA_GROUP *psg = psgList; psg; psg = psg->psgNext)
    {
        GROUP_EXISTS geGroupExists = GROUP_EXISTS_INDETERMINATE;

        // Always put the Group Name, Domain, and Comment on the front of the CustomAction data.
        // The attributes will be added when we have finished adjusting them. Sometimes we'll
        // add more data.
        Assert(psg->wzName);
        hr = WcaWriteStringToCaData(psg->wzName, &pwzActionData);
        ExitOnFailure(hr, "Failed to add group name to custom action data: %ls", psg->wzName);
        hr = WcaWriteStringToCaData(psg->wzDomain, &pwzActionData);
        ExitOnFailure(hr, "Failed to add group domain to custom action data: %ls", psg->wzDomain);
        hr = WcaWriteStringToCaData(psg->wzComment, &pwzActionData);
        ExitOnFailure(hr, "Failed to add group comment to custom action data: %ls", psg->wzComment);

        // Check to see if the group already exists since we have to be very careful when adding
        // and removing groups.  Note: MSDN says that it is safe to call these APIs from any
        // user, so we should be safe calling it during immediate mode.

        LPCWSTR wzDomain = psg->wzDomain;
        hr = GetDomainServerName(wzDomain, &pwzServerName);

        er = ::NetLocalGroupGetInfo(pwzServerName, psg->wzName, 0, reinterpret_cast<LPBYTE*>(&pGroupInfo));
        if (NERR_Success == er)
        {
            geGroupExists = GROUP_EXISTS_YES;
        }
        else if (NERR_GroupNotFound == er)
        {
            geGroupExists = GROUP_EXISTS_NO;
        }
        else
        {
            geGroupExists = GROUP_EXISTS_INDETERMINATE;
            hr = HRESULT_FROM_WIN32(er);
            WcaLog(LOGMSG_VERBOSE, "Failed to check existence of domain: %ls, group: %ls (error code 0x%x) - continuing", wzDomain, psg->wzName, hr);
            hr = S_OK;
            er = ERROR_SUCCESS;
        }

        if (WcaIsInstalling(psg->isInstalled, psg->isAction))
        {
            // If the group exists, check to see if we are supposed to fail if the group exists before
            // the install.
            if (GROUP_EXISTS_YES == geGroupExists)
            {
                // Re-installs will always fail if we don't remove the check for "fail if exists".
                if (WcaIsReInstalling(psg->isInstalled, psg->isAction))
                {
                    psg->iAttributes &= ~SCAG_FAIL_IF_EXISTS;

                    // If install would create the group, re-install should be able to update the group.
                    if (!(psg->iAttributes & SCAG_DONT_CREATE_GROUP))
                    {
                        psg->iAttributes |= SCAG_UPDATE_IF_EXISTS;
                    }
                }

                if (SCAG_FAIL_IF_EXISTS & psg->iAttributes
                    && !(SCAG_UPDATE_IF_EXISTS & psg->iAttributes))
                {
                    hr = HRESULT_FROM_WIN32(NERR_GroupExists);
                    MessageExitOnFailure(hr, msierrGRPFailedGroupCreateExists, "Failed to create group: %ls because group already exists.", psg->wzName);
                }
            }

            hr = WcaWriteIntegerToCaData(psg->iAttributes, &pwzActionData);
            ExitOnFailure(hr, "failed to add group attributes to custom action data for group: %ls", psg->wzKey);

            // Rollback only if the group already exists, we couldn't determine if the group exists, or we are going to create the group
            if ((GROUP_EXISTS_YES == geGroupExists)
                || (GROUP_EXISTS_INDETERMINATE == geGroupExists)
                || !(psg->iAttributes & SCAG_DONT_CREATE_GROUP))
            {
                ++cScriptKey;
                hr = StrAllocFormatted(&pwzScriptKey, L"%ls%u", pwzBaseScriptKey, cScriptKey);
                ExitOnFailure(hr, "Failed to create encoding key.");

                // Write the script key to CustomActionData for install and rollback so information can be passed to rollback.
                hr = WcaWriteStringToCaData(pwzScriptKey, &pwzActionData);
                ExitOnFailure(hr, "Failed to add encoding key to custom action data.");

                hr = WcaWriteStringToCaData(pwzScriptKey, &pwzRollbackData);
                ExitOnFailure(hr, "Failed to add encoding key to rollback custom action data.");

                INT iRollbackUserAttributes = psg->iAttributes;

                // If the user already exists, ensure this is accounted for in rollback
                if (GROUP_EXISTS_YES == geGroupExists)
                {
                    iRollbackUserAttributes |= SCAG_DONT_CREATE_GROUP;
                }
                else
                {
                    iRollbackUserAttributes &= ~SCAG_DONT_CREATE_GROUP;
                }

                hr = WcaWriteStringToCaData(psg->wzName, &pwzRollbackData);
                ExitOnFailure(hr, "Failed to add group name to rollback custom action data: %ls", psg->wzName);
                hr = WcaWriteStringToCaData(psg->wzDomain, &pwzRollbackData);
                ExitOnFailure(hr, "Failed to add group domain to rollback custom action data: %ls", psg->wzDomain);
                hr = WcaWriteIntegerToCaData(iRollbackUserAttributes, &pwzRollbackData);
                ExitOnFailure(hr, "failed to add group attributes to rollback custom action data for group: %ls", psg->wzKey);

                hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"CreateGroupRollback"), pwzRollbackData, COST_GROUP_DELETE);
                ExitOnFailure(hr, "failed to schedule CreateGroupRollback");
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
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"CreateGroup"), pwzActionData, COST_GROUP_ADD);
            ExitOnFailure(hr, "failed to schedule CreateGroup");
        }
        else if (((GROUP_EXISTS_YES == geGroupExists)
            || (GROUP_EXISTS_INDETERMINATE == geGroupExists))
            && WcaIsUninstalling(psg->isInstalled, psg->isAction)
            && !(psg->iAttributes & SCAG_DONT_REMOVE_ON_UNINSTALL))
        {
            hr = WcaWriteIntegerToCaData(psg->iAttributes, &pwzActionData);
            ExitOnFailure(hr, "failed to add group attributes to custom action data for group: %ls", psg->wzKey);

            // Schedule the removal because the group exists and we don't have any flags set
            // that say not to remove the group on uninstall.
            //
            // Note: We can't rollback the removal of a group which is why RemoveGroup is a commit
            // CustomAction.
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"RemoveGroup"), pwzActionData, COST_GROUP_DELETE);
            ExitOnFailure(hr, "failed to schedule RemoveGroup");
        }

        ReleaseNullStr(pwzScriptKey);
        ReleaseNullStr(pwzActionData);
        ReleaseNullStr(pwzRollbackData);
        ReleaseNullStr(pwzServerName);
        if (pGroupInfo)
        {
            ::NetApiBufferFree(static_cast<LPVOID>(pGroupInfo));
            pGroupInfo = NULL;
        }
    }

LExit:
    ReleaseStr(pwzBaseScriptKey);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzActionData);
    ReleaseStr(pwzRollbackData);
    ReleaseStr(pwzServerName);
    if (pGroupInfo)
    {
        ::NetApiBufferFree(static_cast<LPVOID>(pGroupInfo));
        pGroupInfo = NULL;
    }

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

/* ****************************************************************
ScaGroupMembershipRemoveParentsExecute - Schedules group membership removal
based on parent/child component state
******************************************************************/
HRESULT ScaGroupMembershipRemoveParentsExecute(
    __in SCA_GROUP* psg
)
{
    HRESULT hr = S_OK;
    LPWSTR pwzActionData = NULL;

    for (SCA_GROUP* psgp = psg->psgParents; psgp; psgp = psgp->psgNext)
    {
        Assert(psgp->wzName);
        if (WcaIsUninstalling(psg->isInstalled, psg->isAction)
            || WcaIsUninstalling(psgp->isInstalled, psgp->isAction))
        {
            hr = WcaWriteStringToCaData(psgp->wzName, &pwzActionData);
            ExitOnFailure(hr, "Failed to add parent group name to custom action data: %ls", psgp->wzName);
            hr = WcaWriteStringToCaData(psgp->wzDomain, &pwzActionData);
            ExitOnFailure(hr, "Failed to add parent group domain to custom action data: %ls", psgp->wzDomain);
            hr = WcaWriteStringToCaData(psg->wzName, &pwzActionData);
            ExitOnFailure(hr, "Failed to add child group name to custom action data: %ls", psg->wzName);
            hr = WcaWriteStringToCaData(psg->wzDomain, &pwzActionData);
            ExitOnFailure(hr, "Failed to add child group domain to custom action data: %ls", psg->wzDomain);
            hr = WcaWriteIntegerToCaData(psg->iAttributes, &pwzActionData);
            ExitOnFailure(hr, "Failed to add group attributes to custom action data: %i", psg->iAttributes);
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"RemoveGroupMembership"), pwzActionData, COST_GROUPMEMBERSHIP_DELETE);

        LExit:
            ReleaseNullStr(pwzActionData);
            if (hr != S_OK && !(psg->iAttributes & SCAG_NON_VITAL))
            {
                return hr;
            }
        }
    }
    return S_OK;
}

/* ****************************************************************
ScaGroupMembershipRemoveChildrenExecute - 
******************************************************************/
HRESULT ScaGroupMembershipRemoveChildrenExecute(
    __in SCA_GROUP* psg
)
{
    HRESULT hr = S_OK;
    LPWSTR pwzActionData = NULL;

    for (SCA_GROUP* psgc = psg->psgChildren; psgc; psgc = psgc->psgNext)
    {
        Assert(psgc->wzName);
        if (WcaIsUninstalling(psg->isInstalled, psg->isAction)
            || WcaIsUninstalling(psgc->isInstalled, psgc->isAction))
        {
            hr = WcaWriteStringToCaData(psg->wzName, &pwzActionData);
            ExitOnFailure(hr, "Failed to add parent group name to custom action data: %ls", psg->wzName);
            hr = WcaWriteStringToCaData(psg->wzDomain, &pwzActionData);
            ExitOnFailure(hr, "Failed to add parent group domain to custom action data: %ls", psg->wzDomain);
            hr = WcaWriteStringToCaData(psgc->wzName, &pwzActionData);
            ExitOnFailure(hr, "Failed to add child group name to custom action data: %ls", psgc->wzName);
            hr = WcaWriteStringToCaData(psgc->wzDomain, &pwzActionData);
            ExitOnFailure(hr, "Failed to add child group domain to custom action data: %ls", psgc->wzDomain);
            hr = WcaWriteIntegerToCaData(psg->iAttributes, &pwzActionData);
            ExitOnFailure(hr, "Failed to add group attributes to custom action data: %i", psg->iAttributes);
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"RemoveGroupMembership"), pwzActionData, COST_GROUPMEMBERSHIP_DELETE);

        LExit:
            ReleaseNullStr(pwzActionData);

            if (hr != S_OK && !(psg->iAttributes & SCAG_NON_VITAL))
            {
                return hr;
            }
        }
    }
    return S_OK;
}

/* ****************************************************************
ScaGroupMembershipRemoveExecute - Schedules group membership removal
based on parent/child component state
******************************************************************/
HRESULT ScaGroupMembershipRemoveExecute(
    __in SCA_GROUP* psgList
)
{
    HRESULT hr = S_OK;

    // Loop through all the users to be configured.
    for (SCA_GROUP* psg = psgList; psg; psg = psg->psgNext)
    {
        Assert(psg->wzName);
        // first we loop through the Parents
        hr = ScaGroupMembershipRemoveParentsExecute(psg);
        ExitOnFailure(hr, "Failed to remove parent membership for vital group: %ls", psg->wzKey);

        // then through the Children
        hr = ScaGroupMembershipRemoveChildrenExecute(psg);
        ExitOnFailure(hr, "Failed to remove child membership for vital group: %ls", psg->wzKey);
    }

LExit:
    return hr;
}

/* ****************************************************************
ScaGroupMembershipAddParentsExecute - Schedules group membership removal
based on parent/child component state
******************************************************************/
HRESULT ScaGroupMembershipAddParentsExecute(
    __in SCA_GROUP* psg
)
{
    HRESULT hr = S_OK;
    LPWSTR pwzActionData = NULL;

    for (SCA_GROUP* psgp = psg->psgParents; psgp; psgp = psgp->psgNext)
    {
        Assert(psgp->wzName);
        if (WcaIsInstalling(psg->isInstalled, psg->isAction)
            || WcaIsInstalling(psgp->isInstalled, psgp->isAction))
        {
            hr = WcaWriteStringToCaData(psgp->wzName, &pwzActionData);
            ExitOnFailure(hr, "Failed to add parent group domain to custom action data: %ls", psgp->wzName);
            hr = WcaWriteStringToCaData(psgp->wzDomain, &pwzActionData);
            ExitOnFailure(hr, "Failed to add parent group domain to custom action data: %ls", psgp->wzDomain);
            hr = WcaWriteStringToCaData(psg->wzName, &pwzActionData);
            ExitOnFailure(hr, "Failed to add child group name to custom action data: %ls", psg->wzName);
            hr = WcaWriteStringToCaData(psg->wzDomain, &pwzActionData);
            ExitOnFailure(hr, "Failed to add child group domain to custom action data: %ls", psg->wzDomain);
            hr = WcaWriteIntegerToCaData(psg->iAttributes, &pwzActionData);
            ExitOnFailure(hr, "Failed to add group attributes to custom action data: %i", psg->iAttributes);
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"AddGroupMembership"), pwzActionData, COST_GROUPMEMBERSHIP_ADD);

        LExit:
            ReleaseNullStr(pwzActionData);

            if (hr != S_OK && !(psg->iAttributes & SCAG_NON_VITAL))
            {
                return hr;
            }
        }
    }
    return S_OK;
}

/* ****************************************************************
ScaGroupMembershipAddChildrenExecute - Schedules group membership removal
based on parent/child component state
******************************************************************/
HRESULT ScaGroupMembershipAddChildrenExecute(
    __in SCA_GROUP* psg
)
{
    HRESULT hr = S_OK;
    LPWSTR pwzActionData = NULL;

    // then through the Children
    for (SCA_GROUP* psgc = psg->psgChildren; psgc; psgc = psgc->psgNext)
    {
        Assert(psgc->wzName);
        if (WcaIsInstalling(psg->isInstalled, psg->isAction)
            || WcaIsInstalling(psgc->isInstalled, psgc->isAction))
        {
            hr = WcaWriteStringToCaData(psg->wzName, &pwzActionData);
            ExitOnFailure(hr, "Failed to add child group name to custom action data: %ls", psg->wzName);
            hr = WcaWriteStringToCaData(psg->wzDomain, &pwzActionData);
            ExitOnFailure(hr, "Failed to add child group domain to custom action data: %ls", psg->wzDomain);
            hr = WcaWriteStringToCaData(psgc->wzName, &pwzActionData);
            ExitOnFailure(hr, "Failed to add parent group domain to custom action data: %ls", psgc->wzName);
            hr = WcaWriteStringToCaData(psgc->wzDomain, &pwzActionData);
            ExitOnFailure(hr, "Failed to add parent group domain to custom action data: %ls", psgc->wzDomain);
            hr = WcaWriteIntegerToCaData(psg->iAttributes, &pwzActionData);
            ExitOnFailure(hr, "Failed to add group attributes to custom action data: %i", psg->iAttributes);
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION6(L"AddGroupMembership"), pwzActionData, COST_GROUPMEMBERSHIP_ADD);

        LExit:
            ReleaseNullStr(pwzActionData);
            if (hr != S_OK && !(psg->iAttributes & SCAG_NON_VITAL))
            {
                return hr;
            }
        }
    }
    return S_OK;
}

/* ****************************************************************
ScaGroupMembershipAddExecute - Schedules group membership addition
based on parent/child component state
******************************************************************/
HRESULT ScaGroupMembershipAddExecute(
    __in SCA_GROUP* psgList
)
{
    HRESULT hr = S_OK;

    // Loop through all the users to be configured.
    for (SCA_GROUP* psg = psgList; psg; psg = psg->psgNext)
    {
        Assert(psg->wzName);
        // first we loop through the Parents
        hr = ScaGroupMembershipAddParentsExecute(psg);
        ExitOnFailure(hr, "Failed to add parent membership for vital group: %ls", psg->wzKey);

        // then through the Children
        hr = ScaGroupMembershipAddChildrenExecute(psg);
        ExitOnFailure(hr, "Failed to add child membership for vital group: %ls", psg->wzKey);
    }

LExit:
    return hr;
}
