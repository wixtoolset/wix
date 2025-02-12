// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

/********************************************************************
 * CreateSmb - CUSTOM ACTION ENTRY POINT for creating fileshares
 *
 * Input:  deferred CustomActionData -
 *    wzFsKey\twzShareDesc\twzFullPath\tfIntegratedAuth\twzUserName\tnPermissions\twzUserName\tnPermissions...
 *
 * ****************************************************************/
extern "C" UINT __stdcall CreateSmb(MSIHANDLE hInstall)
{
//AssertSz(0, "debug CreateSmb");
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzFsKey = NULL;
    LPWSTR pwzShareDesc = NULL;
    LPWSTR pwzDirectory = NULL;
    int iAccessMode = 0;
    DWORD nExPermissions = 0;
    BOOL fIntegratedAuth;
    LPWSTR pwzExUser = NULL;
    SCA_SMBP ssp = {0};
    DWORD dwExUserPerms = 0;
    DWORD dwCounter = 0;
    SCA_SMBP_USER_PERMS* pUserPermsList = NULL;

    hr = WcaInitialize(hInstall, "CreateSmb");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzFsKey); // share name
    ExitOnFailure(hr, "failed to read share name");
    hr = WcaReadStringFromCaData(&pwz, &pwzShareDesc); // share description
    ExitOnFailure(hr, "failed to read share name");
    hr = WcaReadStringFromCaData(&pwz, &pwzDirectory); // full path to share
    ExitOnFailure(hr, "failed to read share name");
    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&fIntegratedAuth));
    ExitOnFailure(hr, "failed to read integrated authentication");

    hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&dwExUserPerms));
    ExitOnFailure(hr, "failed to read count of permissions to set");
    if(dwExUserPerms > 0)
    {
        pUserPermsList = static_cast<SCA_SMBP_USER_PERMS*>(MemAlloc(sizeof(SCA_SMBP_USER_PERMS)*dwExUserPerms, TRUE));
        ExitOnNull(pUserPermsList, hr, E_OUTOFMEMORY, "failed to allocate memory for permissions structure");

        //Pull out all of the ExUserPerm strings
        for (dwCounter = 0; dwCounter < dwExUserPerms; ++dwCounter)
        {
            hr = WcaReadStringFromCaData(&pwz, &pwzExUser); // user account
            ExitOnFailure(hr, "failed to read user account");
            pUserPermsList[dwCounter].wzUser = pwzExUser;
            pwzExUser = NULL;

            hr = WcaReadIntegerFromCaData(&pwz, &iAccessMode);
            ExitOnFailure(hr, "failed to read access mode");
            pUserPermsList[dwCounter].accessMode = (ACCESS_MODE)iAccessMode;
            iAccessMode = 0;

            hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int *>(&nExPermissions));
            ExitOnFailure(hr, "failed to read count of permissions");
            pUserPermsList[dwCounter].nPermissions = nExPermissions;
            nExPermissions = 0;
        }
    }

    ssp.wzKey = pwzFsKey;
    ssp.wzDescription = pwzShareDesc;
    ssp.wzDirectory = pwzDirectory;
    ssp.fUseIntegratedAuth = fIntegratedAuth;
    ssp.dwUserPermissionCount = dwExUserPerms;
    ssp.pUserPerms = pUserPermsList;

    hr = ScaEnsureSmbExists(&ssp);
    MessageExitOnFailure(hr, msierrSMBFailedCreate, "failed to create share: '%ls'", pwzFsKey);

    hr = WcaProgressMessage(COST_SMB_CREATESMB, FALSE);

LExit:
    ReleaseStr(pwzFsKey);
    ReleaseStr(pwzShareDesc);
    ReleaseStr(pwzDirectory);
    ReleaseStr(pwzData);

    if (pUserPermsList)
    {
        MemFree(pUserPermsList);
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}



/********************************************************************
 DropSmb - CUSTOM ACTION ENTRY POINT for creating fileshares

 Input:  deferred CustomActionData - wzFsKey\twzShareDesc\twzFullPath\tnPermissions\tfIntegratedAuth\twzUserName\twzPassword

 * ****************************************************************/
extern "C" UINT __stdcall DropSmb(MSIHANDLE hInstall)
{
    //AssertSz(0, "debug DropSmb");
    UINT er = ERROR_SUCCESS;
    HRESULT hr = S_OK;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzFsKey = NULL;
    SCA_SMBP ssp = {0};

    hr = WcaInitialize(hInstall, "DropSmb");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzFsKey); // share name
    ExitOnFailure(hr, "failed to read share name");

    ssp.wzKey = pwzFsKey;

    hr = ScaDropSmb(&ssp);
    MessageExitOnFailure(hr, msierrSMBFailedDrop, "failed to delete share: '%ls'", pwzFsKey);

    hr = WcaProgressMessage(COST_SMB_DROPSMB, FALSE);

LExit:
    ReleaseStr(pwzFsKey);
    ReleaseStr(pwzData);

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }
    return WcaFinalize(er);
}


static HRESULT AddUserToGroup(
    __in LPWSTR wzUser,
    __in LPCWSTR wzUserDomain,
    __in LPCWSTR wzGroup,
    __in LPCWSTR wzGroupDomain
    )
{
    Assert(wzUser && *wzUser && wzUserDomain && wzGroup && *wzGroup && wzGroupDomain);

    HRESULT hr = S_OK;
    IADsGroup *pGroup = NULL;
    BSTR bstrUser = NULL;
    BSTR bstrGroup = NULL;
    LPCWSTR wz = NULL;
    LPWSTR pwzUser = NULL;
    LOCALGROUP_MEMBERS_INFO_3 lgmi;

    if (*wzGroupDomain)
    {
        wz = wzGroupDomain;
    }

    // Try adding it to the global group first
    UINT ui = ::NetGroupAddUser(wz, wzGroup, wzUser);
    if (NERR_GroupNotFound == ui)
    {
        // Try adding it to the local group
        if (wzUserDomain)
        {
            hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzUserDomain, wzUser);
            ExitOnFailure(hr, "failed to allocate user domain string");
        }

        lgmi.lgrmi3_domainandname = (NULL == pwzUser ? wzUser : pwzUser);
        ui = ::NetLocalGroupAddMembers(wz, wzGroup, 3 , reinterpret_cast<LPBYTE>(&lgmi), 1);
    }
    hr = HRESULT_FROM_WIN32(ui);
    if (HRESULT_FROM_WIN32(ERROR_MEMBER_IN_ALIAS) == hr) // if they're already a member of the group don't report an error
        hr = S_OK;

    //
    // If we failed, try active directory
    //
    if (FAILED(hr))
    {
        WcaLog(LOGMSG_VERBOSE, "Failed to add user: %ls, domain %ls to group: %ls, domain: %ls with error 0x%x.  Attempting to use Active Directory", wzUser, wzUserDomain, wzGroup, wzGroupDomain, hr);

        hr = UserCreateADsPath(wzUserDomain, wzUser, &bstrUser);
        ExitOnFailure(hr, "failed to create user ADsPath for user: %ls domain: %ls", wzUser, wzUserDomain);

        hr = UserCreateADsPath(wzGroupDomain, wzGroup, &bstrGroup);
        ExitOnFailure(hr, "failed to create group ADsPath for group: %ls domain: %ls", wzGroup, wzGroupDomain);

        hr = ::ADsGetObject(bstrGroup,IID_IADsGroup, reinterpret_cast<void**>(&pGroup));
        ExitOnFailure(hr, "Failed to get group '%ls'.", reinterpret_cast<WCHAR*>(bstrGroup) );

        hr = pGroup->Add(bstrUser);
        if ((HRESULT_FROM_WIN32(ERROR_OBJECT_ALREADY_EXISTS) == hr) || (HRESULT_FROM_WIN32(ERROR_MEMBER_IN_ALIAS) == hr))
            hr = S_OK;

        ExitOnFailure(hr, "Failed to add user %ls to group '%ls'.", reinterpret_cast<WCHAR*>(bstrUser), reinterpret_cast<WCHAR*>(bstrGroup) );
    }

LExit:
    ReleaseObject(pGroup);
    ReleaseBSTR(bstrUser);
    ReleaseBSTR(bstrGroup);

    return hr;
}

static HRESULT RemoveUserFromGroup(
    __in LPWSTR wzUser,
    __in LPCWSTR wzUserDomain,
    __in LPCWSTR wzGroup,
    __in LPCWSTR wzGroupDomain
    )
{
    Assert(wzUser && *wzUser && wzUserDomain && wzGroup && *wzGroup && wzGroupDomain);

    HRESULT hr = S_OK;
    IADsGroup *pGroup = NULL;
    BSTR bstrUser = NULL;
    BSTR bstrGroup = NULL;
    LPCWSTR wz = NULL;
    LPWSTR pwzUser = NULL;
    LOCALGROUP_MEMBERS_INFO_3 lgmi;

    if (*wzGroupDomain)
    {
        wz = wzGroupDomain;
    }

    // Try removing it from the global group first
    UINT ui = ::NetGroupDelUser(wz, wzGroup, wzUser);
    if (NERR_GroupNotFound == ui)
    {
        // Try removing it from the local group
        if (wzUserDomain)
        {
            hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzUserDomain, wzUser);
            ExitOnFailure(hr, "failed to allocate user domain string");
        }

        lgmi.lgrmi3_domainandname = (NULL == pwzUser ? wzUser : pwzUser);
        ui = ::NetLocalGroupDelMembers(wz, wzGroup, 3 , reinterpret_cast<LPBYTE>(&lgmi), 1);
    }
    hr = HRESULT_FROM_WIN32(ui);

    //
    // If we failed, try active directory
    //
    if (FAILED(hr))
    {
        WcaLog(LOGMSG_VERBOSE, "Failed to remove user: %ls, domain %ls from group: %ls, domain: %ls with error 0x%x.  Attempting to use Active Directory", wzUser, wzUserDomain, wzGroup, wzGroupDomain, hr);

        hr = UserCreateADsPath(wzUserDomain, wzUser, &bstrUser);
        ExitOnFailure(hr, "failed to create user ADsPath in order to remove user: %ls domain: %ls from a group", wzUser, wzUserDomain);

        hr = UserCreateADsPath(wzGroupDomain, wzGroup, &bstrGroup);
        ExitOnFailure(hr, "failed to create group ADsPath in order to remove user from group: %ls domain: %ls", wzGroup, wzGroupDomain);

        hr = ::ADsGetObject(bstrGroup,IID_IADsGroup, reinterpret_cast<void**>(&pGroup));
        ExitOnFailure(hr, "Failed to get group '%ls'.", reinterpret_cast<WCHAR*>(bstrGroup) );

        hr = pGroup->Remove(bstrUser);
        ExitOnFailure(hr, "Failed to remove user %ls from group '%ls'.", reinterpret_cast<WCHAR*>(bstrUser), reinterpret_cast<WCHAR*>(bstrGroup) );
    }

LExit:
    ReleaseObject(pGroup);
    ReleaseBSTR(bstrUser);
    ReleaseBSTR(bstrGroup);

    return hr;
}

static HRESULT GetUserHasRight(
    __in LSA_HANDLE hPolicy,
    __in PSID pUserSid,
    __in LPWSTR wzRight,
    __out BOOL* fHasRight
)
{
    HRESULT hr = S_OK;
    NTSTATUS nt = 0;
    LSA_UNICODE_STRING lucPrivilege = { 0 };
    PLSA_ENUMERATION_INFORMATION rgSids = NULL;
    ULONG cSids = 0;
    *fHasRight = FALSE;

    lucPrivilege.Buffer = wzRight;
    lucPrivilege.Length = static_cast<USHORT>(lstrlenW(lucPrivilege.Buffer) * sizeof(WCHAR));
    lucPrivilege.MaximumLength = (lucPrivilege.Length + 1) * sizeof(WCHAR);

    nt = ::LsaEnumerateAccountsWithUserRight(hPolicy, &lucPrivilege, reinterpret_cast<PVOID*>(&rgSids), &cSids);
    hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
    ExitOnFailure(hr, "Failed to enumerate users for right: %ls", lucPrivilege.Buffer);

    for (DWORD i = 0; i < cSids; ++i)
    {
        PLSA_ENUMERATION_INFORMATION pInfo = rgSids + i;
        if (::EqualSid(pUserSid, pInfo->Sid))
        {
            *fHasRight = TRUE;
            break;
        }
    }

LExit:
    if (rgSids)
    {
        ::LsaFreeMemory(rgSids);
    }

    return hr;
}


static HRESULT GetExistingUserRightsAssignments(
    __in_opt LPCWSTR wzDomain,
    __in LPCWSTR wzName,
    __inout int* iAttributes
)
{
    HRESULT hr = S_OK;
    NTSTATUS nt = 0;
    BOOL fHasRight = FALSE;

    LSA_HANDLE hPolicy = NULL;
    LSA_OBJECT_ATTRIBUTES objectAttributes = { 0 };

    LPWSTR pwzUser = NULL;
    PSID psid = NULL;

    if (wzDomain && *wzDomain)
    {
        hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzDomain, wzName);
        ExitOnFailure(hr, "Failed to allocate user with domain string");
    }
    else
    {
        hr = StrAllocString(&pwzUser, wzName, 0);
        ExitOnFailure(hr, "Failed to allocate string from user name.");
    }

    hr = AclGetAccountSid(NULL, pwzUser, &psid);
    ExitOnFailure(hr, "Failed to get SID for user: %ls", pwzUser);

    nt = ::LsaOpenPolicy(NULL, &objectAttributes, POLICY_LOOKUP_NAMES | POLICY_VIEW_LOCAL_INFORMATION, &hPolicy);
    hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
    ExitOnFailure(hr, "Failed to open LSA policy store");

    hr = GetUserHasRight(hPolicy, psid, L"SeServiceLogonRight", &fHasRight);
    ExitOnFailure(hr, "Failed to check LogonAsService right");

    if (fHasRight)
    {
        *iAttributes |= SCAU_ALLOW_LOGON_AS_SERVICE;
    }

    hr = GetUserHasRight(hPolicy, psid, L"SeBatchLogonRight", &fHasRight);
    ExitOnFailure(hr, "Failed to check LogonAsBatchJob right");

    if (fHasRight)
    {
        *iAttributes |= SCAU_ALLOW_LOGON_AS_BATCH;
    }

LExit:
    if (hPolicy)
    {
        ::LsaClose(hPolicy);
    }

    ReleaseSid(psid);
    ReleaseStr(pwzUser);
    return hr;
}


static HRESULT ModifyUserLocalServiceRight(
    __in_opt LPCWSTR wzDomain,
    __in LPCWSTR wzName,
    __in BOOL fAdd
    )
{
    HRESULT hr = S_OK;
    NTSTATUS nt = 0;

    LPWSTR pwzUser = NULL;
    PSID psid = NULL;
    LSA_HANDLE hPolicy = NULL;
    LSA_OBJECT_ATTRIBUTES ObjectAttributes = { 0 };
    LSA_UNICODE_STRING lucPrivilege = { 0 };

    if (wzDomain && *wzDomain)
    {
        hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzDomain, wzName);
        ExitOnFailure(hr, "Failed to allocate user with domain string");
    }
    else
    {
        hr = StrAllocString(&pwzUser, wzName, 0);
        ExitOnFailure(hr, "Failed to allocate string from user name.");
    }

    hr = AclGetAccountSid(NULL, pwzUser, &psid);
    ExitOnFailure(hr, "Failed to get SID for user: %ls", pwzUser);

    nt = ::LsaOpenPolicy(NULL, &ObjectAttributes, POLICY_ALL_ACCESS, &hPolicy);
    hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
    ExitOnFailure(hr, "Failed to open LSA policy store.");

    lucPrivilege.Buffer = L"SeServiceLogonRight";
    lucPrivilege.Length = static_cast<USHORT>(lstrlenW(lucPrivilege.Buffer) * sizeof(WCHAR));
    lucPrivilege.MaximumLength = (lucPrivilege.Length + 1) * sizeof(WCHAR);

    if (fAdd)
    {
        nt = ::LsaAddAccountRights(hPolicy, psid, &lucPrivilege, 1);
        hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
        ExitOnFailure(hr, "Failed to add 'logon as service' bit to user: %ls", pwzUser);
    }
    else
    {
        nt = ::LsaRemoveAccountRights(hPolicy, psid, FALSE, &lucPrivilege, 1);
        hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
        ExitOnFailure(hr, "Failed to remove 'logon as service' bit from user: %ls", pwzUser);
    }

LExit:
    if (hPolicy)
    {
        ::LsaClose(hPolicy);
    }

    ReleaseSid(psid);
    ReleaseStr(pwzUser);
    return hr;
}


static HRESULT ModifyUserLocalBatchRight(
  __in_opt LPCWSTR wzDomain,
  __in LPCWSTR wzName,
  __in BOOL fAdd
  )
{
    HRESULT hr = S_OK;
    NTSTATUS nt = 0;

    LPWSTR pwzUser = NULL;
    PSID psid = NULL;
    LSA_HANDLE hPolicy = NULL;
    LSA_OBJECT_ATTRIBUTES ObjectAttributes = { 0 };
    LSA_UNICODE_STRING lucPrivilege = { 0 };

    if (wzDomain && *wzDomain)
    {
        hr = StrAllocFormatted(&pwzUser, L"%s\\%s", wzDomain, wzName);
        ExitOnFailure(hr, "Failed to allocate user with domain string");
    }
    else
    {
        hr = StrAllocString(&pwzUser, wzName, 0);
        ExitOnFailure(hr, "Failed to allocate string from user name.");
    }

    hr = AclGetAccountSid(NULL, pwzUser, &psid);
    ExitOnFailure(hr, "Failed to get SID for user: %ls", pwzUser);

    nt = ::LsaOpenPolicy(NULL, &ObjectAttributes, POLICY_ALL_ACCESS, &hPolicy);
    hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
    ExitOnFailure(hr, "Failed to open LSA policy store.");

    lucPrivilege.Buffer = L"SeBatchLogonRight";
    lucPrivilege.Length = static_cast<USHORT>(lstrlenW(lucPrivilege.Buffer) * sizeof(WCHAR));
    lucPrivilege.MaximumLength = (lucPrivilege.Length + 1) * sizeof(WCHAR);

    if (fAdd)
    {
        nt = ::LsaAddAccountRights(hPolicy, psid, &lucPrivilege, 1);
        hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
        ExitOnFailure(hr, "Failed to add 'logon as batch job' bit to user: %ls", pwzUser);
    }
    else
    {
        nt = ::LsaRemoveAccountRights(hPolicy, psid, FALSE, &lucPrivilege, 1);
        hr = HRESULT_FROM_WIN32(::LsaNtStatusToWinError(nt));
        ExitOnFailure(hr, "Failed to remove 'logon as batch job' bit from user: %ls", pwzUser);
    }

  LExit:
    if (hPolicy)
    {
        ::LsaClose(hPolicy);
    }

    ReleaseSid(psid);
    ReleaseStr(pwzUser);
    return hr;
}

static void ApplyAttributes(int iAttributes, DWORD* pFlags)
{
    if (SCAU_DONT_EXPIRE_PASSWRD & iAttributes)
    {
        *pFlags |= UF_DONT_EXPIRE_PASSWD;
    }
    else
    {
        *pFlags &= ~UF_DONT_EXPIRE_PASSWD;
    }

    if (SCAU_PASSWD_CANT_CHANGE & iAttributes)
    {
        *pFlags |= UF_PASSWD_CANT_CHANGE;
    }
    else
    {
        *pFlags &= ~UF_PASSWD_CANT_CHANGE;
    }

    if (SCAU_DISABLE_ACCOUNT & iAttributes)
    {
        *pFlags |= UF_ACCOUNTDISABLE;
    }
    else
    {
        *pFlags &= ~UF_ACCOUNTDISABLE;
    }

    if (SCAU_PASSWD_CHANGE_REQD_ON_LOGIN & iAttributes) // TODO: for some reason this doesn't work
    {
        *pFlags |= UF_PASSWORD_EXPIRED;
    }
    else
    {
        *pFlags &= ~UF_PASSWORD_EXPIRED;
    }
}

static void ApplyComment(int iAttributes, LPWSTR pwzComment, LPWSTR* ppComment)
{
    if (SCAU_REMOVE_COMMENT & iAttributes)
    {
        *ppComment = L"";
    }
    else if (pwzComment && *pwzComment)
    {
        *ppComment = pwzComment;
    }
}

static NET_API_STATUS SetUserPassword(__in LPWSTR pwzServerName, __in LPWSTR pwzName, __in LPWSTR pwzPassword)
{
    NET_API_STATUS er = NERR_Success;
    _USER_INFO_1003 userInfo1003 = { };

    userInfo1003.usri1003_password = pwzPassword;
    er = ::NetUserSetInfo(pwzServerName, pwzName, 1003, reinterpret_cast<LPBYTE>(&userInfo1003), NULL);
    return HRESULT_FROM_WIN32(er);
}

static HRESULT SetUserComment(__in LPWSTR pwzServerName, __in LPWSTR pwzName, __in LPWSTR pwzComment)
{
    NET_API_STATUS er = NERR_Success;
    _USER_INFO_1007 userInfo1007 = { };

    userInfo1007.usri1007_comment = pwzComment;
    er = ::NetUserSetInfo(pwzServerName, pwzName, 1007, reinterpret_cast<LPBYTE>(&userInfo1007), NULL);
    return HRESULT_FROM_WIN32(er);
}

static HRESULT SetGroupComment(__in LPWSTR pwzServerName, __in LPWSTR pwzName, __in LPWSTR pwzComment)
{
    _LOCALGROUP_INFO_1002  groupInfo1002 {};

    groupInfo1002.lgrpi1002_comment = pwzComment;
    NET_API_STATUS er = ::NetLocalGroupSetInfo(pwzServerName, pwzName, 1002, reinterpret_cast<LPBYTE>(&groupInfo1002), NULL);
    return HRESULT_FROM_WIN32(er);
}

static HRESULT SetUserFlags(__in LPWSTR pwzServerName, __in LPWSTR pwzName, __in DWORD flags)
{
    NET_API_STATUS er = NERR_Success;
    _USER_INFO_1008 userInfo1008 = { };

    userInfo1008.usri1008_flags = flags;
    er = ::NetUserSetInfo(pwzServerName, pwzName, 1008, reinterpret_cast<LPBYTE>(&userInfo1008), NULL);
    return HRESULT_FROM_WIN32(er);
}

static HRESULT RemoveUserInternal(
    LPWSTR wzGroupCaData,
    LPWSTR wzDomain,
    LPWSTR wzName,
    int iAttributes
)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwz = NULL;
    LPWSTR pwzGroup = NULL;
    LPWSTR pwzGroupDomain = NULL;
    LPWSTR pwzDomainName = NULL;

    //
    // Remove the logon as service privilege.
    //
    if (SCAU_ALLOW_LOGON_AS_SERVICE & iAttributes)
    {
        hr = ModifyUserLocalServiceRight(wzDomain, wzName, FALSE);
        if (FAILED(hr))
        {
            WcaLogError(hr, "Failed to remove logon as service right from user, continuing...");
            hr = S_OK;
        }
    }

    if (SCAU_ALLOW_LOGON_AS_BATCH & iAttributes)
    {
        hr = ModifyUserLocalBatchRight(wzDomain, wzName, FALSE);
        if (FAILED(hr))
        {
            WcaLogError(hr, "Failed to remove logon as batch job right from user, continuing...");
            hr = S_OK;
        }
    }

    //
    // Remove the User Account if the user was created by us.
    //
    if (!(SCAU_DONT_CREATE_USER & iAttributes))
    {
        hr = GetDomainFromServerName(&pwzDomainName, wzDomain, 0);
        ExitOnFailure(hr, "Failed to get domain to remove user from server name: %ls", wzDomain);

        er = ::NetUserDel(pwzDomainName, wzName);
        if (NERR_UserNotFound == er)
        {
            er = NERR_Success;
        }
        ExitOnFailure(hr = HRESULT_FROM_WIN32(er), "failed to delete user account: %ls", wzName);
    }
    else
    {
        //
        // Remove the user from the groups
        //
        pwz = wzGroupCaData;
        while (S_OK == (hr = WcaReadStringFromCaData(&pwz, &pwzGroup)))
        {
            hr = WcaReadStringFromCaData(&pwz, &pwzGroupDomain);

            if (FAILED(hr))
            {
                WcaLogError(hr, "failed to get domain for group: %ls, continuing anyway.", pwzGroup);
            }
            else
            {
                hr = RemoveUserFromGroup(wzName, wzDomain, pwzGroup, pwzGroupDomain);
                if (FAILED(hr))
                {
                    WcaLogError(hr, "failed to remove user: %ls from group %ls, continuing anyway.", wzName, pwzGroup);
                }
            }
        }

        if (E_NOMOREITEMS == hr) // if there are no more items, all is well
        {
            hr = S_OK;
        }

        ExitOnFailure(hr, "failed to get next group from which to remove user:%ls", wzName);
    }

LExit:
    ReleaseStr(pwzDomainName);
    ReleaseStr(pwzGroupDomain);
    ReleaseStr(pwzGroup);

    return hr;
}

static HRESULT RemoveGroupInternal(
    LPWSTR wzDomain,
    LPWSTR wzName,
    int iAttributes
)
{
    HRESULT hr = S_OK;

    LPWSTR pwzServerName = NULL;

    //
    // Remove the Group if the group was created by us.
    //
    if (!(SCAG_DONT_CREATE_GROUP & iAttributes))
    {
        GetDomainServerName(wzDomain, &pwzServerName, DS_WRITABLE_REQUIRED);

        NET_API_STATUS er = ::NetLocalGroupDel(pwzServerName, wzName);
        hr = HRESULT_FROM_WIN32(er);
        if (HRESULT_FROM_WIN32(ERROR_NO_SUCH_ALIAS) == hr
            || HRESULT_FROM_WIN32(NERR_GroupNotFound) == hr) // we wanted to delete it.. and the group doesn't exist.. solved.
        {
            hr = S_OK;
        }
        ExitOnFailure(hr, "failed to delete group: %ls", wzName);
    }

LExit:
    ReleaseStr(pwzServerName);

    return hr;
}


/********************************************************************
 CreateUser - CUSTOM ACTION ENTRY POINT for creating users

  Input:  deferred CustomActionData - UserName\tDomain\tPassword\tAttributes\tGroupName\tDomain\tGroupName\tDomain...
 * *****************************************************************/
extern "C" UINT __stdcall CreateUser(
    __in MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug CreateUser");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzDomain = NULL;
    LPWSTR pwzComment = NULL;
    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzPassword = NULL;
    LPWSTR pwzGroup = NULL;
    LPWSTR pwzGroupDomain = NULL;
    LPWSTR pwzDomainName = NULL;
    int iAttributes = 0;
    BOOL fInitializedCom = FALSE;

    WCA_CASCRIPT_HANDLE hRollbackScript = NULL;
    int iOriginalAttributes = 0;
    int iRollbackAttributes = 0;

    USER_INFO_1 userInfo1;
    USER_INFO_1* pUserInfo1 = NULL;
    DWORD dw;

    hr = WcaInitialize(hInstall, "CreateUser");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    //
    // Read in the CustomActionData
    //
    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzName);
    ExitOnFailure(hr, "failed to read user name from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
    ExitOnFailure(hr, "failed to read domain from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzComment);
    ExitOnFailure(hr, "failed to read user comment from custom action data");

    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzScriptKey);
    ExitOnFailure(hr, "failed to read encoding key from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzPassword);
    ExitOnFailure(hr, "failed to read password from custom action data");

    if (!(SCAU_DONT_CREATE_USER & iAttributes))
    {
        pUserInfo1 = &userInfo1;
        ::ZeroMemory(pUserInfo1, sizeof(USER_INFO_1));
        pUserInfo1->usri1_name = pwzName;
        pUserInfo1->usri1_priv = USER_PRIV_USER;
        pUserInfo1->usri1_flags = UF_SCRIPT;
        pUserInfo1->usri1_home_dir = NULL;
        pUserInfo1->usri1_comment = NULL;
        pUserInfo1->usri1_script_path = NULL;

        // Set the user's password
        pUserInfo1->usri1_password = pwzPassword;

        // Set the user's comment
        ApplyComment(iAttributes, pwzComment, &pUserInfo1->usri1_comment);

        // Set the user's flags
        ApplyAttributes(iAttributes, &pUserInfo1->usri1_flags);

        //
        // Create the User
        //
        hr = GetDomainFromServerName(&pwzDomainName, pwzDomain, 0);
        ExitOnFailure(hr, "Failed to get domain from server name: %ls", pwzDomain);

        er = ::NetUserAdd(pwzDomainName, 1, reinterpret_cast<LPBYTE>(pUserInfo1), &dw);
        if (NERR_UserExists == er)
        {
            if (SCAU_FAIL_IF_EXISTS & iAttributes)
            {
                hr = HRESULT_FROM_WIN32(er);
                ExitOnFailure(hr, "User was not supposed to exist, but does.");
            }

            er = ERROR_SUCCESS; // Make sure that we don't report this situation as an error
                                // if we fall through the tests that follow.

            if (SCAU_UPDATE_IF_EXISTS & iAttributes)
            {
                pUserInfo1 = NULL;
                er = ::NetUserGetInfo(pwzDomainName, pwzName, 1, reinterpret_cast<LPBYTE*>(&pUserInfo1));
                if (ERROR_SUCCESS == er)
                {
                    // There is no rollback scheduled if the key is empty.
                    // Best effort to get original configuration and save it in the script so rollback can restore it.

                    if (*pwzScriptKey)
                    {
                        // Try to open the rollback script
                        hr = WcaCaScriptOpen(WCA_ACTION_INSTALL, WCA_CASCRIPT_ROLLBACK, FALSE, pwzScriptKey, &hRollbackScript);

                        if (INVALID_HANDLE_VALUE != hRollbackScript)
                        {
                            WcaCaScriptClose(hRollbackScript, WCA_CASCRIPT_CLOSE_PRESERVE);
                        }
                        else
                        {
                            hRollbackScript = NULL;
                            hr = WcaCaScriptCreate(WCA_ACTION_INSTALL, WCA_CASCRIPT_ROLLBACK, FALSE, pwzScriptKey, FALSE, &hRollbackScript);
                            ExitOnFailure(hr, "Failed to open rollback CustomAction script.");

                            iRollbackAttributes = 0;
                            hr = GetExistingUserRightsAssignments(pwzDomain, pwzName, &iOriginalAttributes);
                            if (FAILED(hr))
                            {
                                WcaLogError(hr, "failed to get existing user rights: %ls, continuing anyway.", pwzName);
                                hr = S_OK;
                            }
                            else
                            {
                                if (!(SCAU_ALLOW_LOGON_AS_SERVICE & iOriginalAttributes) && (SCAU_ALLOW_LOGON_AS_SERVICE & iAttributes))
                                {
                                    iRollbackAttributes |= SCAU_ALLOW_LOGON_AS_SERVICE;
                                }

                                if (!(SCAU_ALLOW_LOGON_AS_BATCH & iOriginalAttributes) && (SCAU_ALLOW_LOGON_AS_BATCH & iAttributes))
                                {
                                    iRollbackAttributes |= SCAU_ALLOW_LOGON_AS_BATCH;
                                }
                            }

                            hr = WcaCaScriptWriteString(hRollbackScript, pUserInfo1->usri1_comment);
                            ExitOnFailure(hr, "Failed to add rollback comment to rollback script.");

                            if (!pUserInfo1->usri1_comment || !*pUserInfo1->usri1_comment)
                            {
                                iRollbackAttributes |= SCAU_REMOVE_COMMENT;
                            }

                            hr = WcaCaScriptWriteNumber(hRollbackScript, iRollbackAttributes);
                            ExitOnFailure(hr, "Failed to add rollback attributes to rollback script.");

                            // Nudge the system to get all our rollback data written to disk.
                            WcaCaScriptFlush(hRollbackScript);
                        }
                    }
                }

                if (ERROR_SUCCESS == er)
                {
                    hr = SetUserPassword(pwzDomainName, pwzName, pwzPassword);
                    if (FAILED(hr))
                    {
                        WcaLogError(hr, "failed to set user password for user %ls\\%ls, continuing anyway.", pwzDomainName, pwzName);
                        hr = S_OK;
                    }

                    if (SCAU_REMOVE_COMMENT & iAttributes)
                    {
                        hr = SetUserComment(pwzDomainName, pwzName, L"");
                        if (FAILED(hr))
                        {
                            WcaLogError(hr, "failed to clear user comment for user %ls\\%ls, continuing anyway.", pwzDomainName, pwzName);
                            hr = S_OK;
                        }
                    }
                    else if (pwzComment && *pwzComment)
                    {
                        hr = SetUserComment(pwzDomainName, pwzName, pwzComment);
                        if (FAILED(hr))
                        {
                            WcaLogError(hr, "failed to set user comment to %ls for user %ls\\%ls, continuing anyway.", pwzComment, pwzDomainName, pwzName);
                            hr = S_OK;
                        }
                    }

                    DWORD flags = pUserInfo1->usri1_flags;

                    ApplyAttributes(iAttributes, &flags);

                    hr = SetUserFlags(pwzDomainName, pwzName, flags);
                    if (FAILED(hr))
                    {
                        WcaLogError(hr, "failed to set user flags for user %ls\\%ls, continuing anyway.", pwzDomainName, pwzName);
                        hr = S_OK;
                    }
                }
            }
        }
        else if (NERR_PasswordTooShort == er || NERR_PasswordTooLong == er)
        {
            MessageExitOnFailure(hr = HRESULT_FROM_WIN32(er), msierrUSRFailedUserCreatePswd, "failed to create user: %ls due to invalid password.", pwzName);
        }

        if (ERROR_SUCCESS != er)
        {
            MessageExitOnFailure(hr = HRESULT_FROM_WIN32(er), msierrUSRFailedUserCreate, "failed to create user: %ls", pwzName);
        }
    }

    if (SCAU_ALLOW_LOGON_AS_SERVICE & iAttributes)
    {
        hr = ModifyUserLocalServiceRight(pwzDomain, pwzName, TRUE);
        MessageExitOnFailure(hr, msierrUSRFailedGrantLogonAsService, "Failed to grant logon as service rights to user: %ls", pwzName);
    }

    if (SCAU_ALLOW_LOGON_AS_BATCH & iAttributes)
    {
        hr = ModifyUserLocalBatchRight(pwzDomain, pwzName, TRUE);
        MessageExitOnFailure(hr, msierrUSRFailedGrantLogonAsService, "Failed to grant logon as batch job rights to user: %ls", pwzName);
    }

    //
    // Add the users to groups
    //
    while (S_OK == (hr = WcaReadStringFromCaData(&pwz, &pwzGroup)))
    {
        hr = WcaReadStringFromCaData(&pwz, &pwzGroupDomain);
        ExitOnFailure(hr, "failed to get domain for group: %ls", pwzGroup);

        WcaLog(LOGMSG_STANDARD, "Adding user %ls\\%ls to group %ls\\%ls", pwzDomain, pwzName, pwzGroupDomain, pwzGroup);
        hr = AddUserToGroup(pwzName, pwzDomain, pwzGroup, pwzGroupDomain);
        MessageExitOnFailure(hr, msierrUSRFailedUserGroupAdd, "failed to add user: %ls to group %ls", pwzName, pwzGroup);
    }
    if (E_NOMOREITEMS == hr) // if there are no more items, all is well
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failed to get next group in which to include user: %ls", pwzName);

LExit:
    WcaCaScriptClose(hRollbackScript, WCA_CASCRIPT_CLOSE_PRESERVE);

    if (pUserInfo1 && pUserInfo1 != &userInfo1)
    {
        ::NetApiBufferFree((LPVOID)pUserInfo1);
    }

    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzComment);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzPassword);
    ReleaseStr(pwzGroup);
    ReleaseStr(pwzGroupDomain);
    ReleaseStr(pwzDomainName)

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (SCAU_NON_VITAL & iAttributes)
    {
        er = ERROR_SUCCESS;
    }
    else if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}


/********************************************************************
 CreateUserRollback - CUSTOM ACTION ENTRY POINT for CreateUser rollback

 * *****************************************************************/
extern "C" UINT __stdcall CreateUserRollback(
    MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug CreateUserRollback");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzDomain = NULL;
    LPWSTR pwzComment = NULL;
    int iAttributes = 0;
    BOOL fInitializedCom = FALSE;

    WCA_CASCRIPT_HANDLE hRollbackScript = NULL;
    LPWSTR pwzRollbackData = NULL;
    int iOriginalAttributes = 0;
    LPWSTR pwzOriginalComment = NULL;

    hr = WcaInitialize(hInstall, "CreateUserRollback");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    //
    // Read in the CustomActionData
    //
    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzScriptKey);
    ExitOnFailure(hr, "failed to read encoding key from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzName);
    ExitOnFailure(hr, "failed to read name from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
    ExitOnFailure(hr, "failed to read domain from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzComment);
    ExitOnFailure(hr, "failed to read comment from custom action data");

    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data");

    // Best effort to read original configuration from CreateUser.
    hr = WcaCaScriptOpen(WCA_ACTION_INSTALL, WCA_CASCRIPT_ROLLBACK, FALSE, pwzScriptKey, &hRollbackScript);
    if (FAILED(hr))
    {
        WcaLogError(hr, "Failed to open rollback CustomAction script, continuing anyway.");
    }
    else
    {
        hr = WcaCaScriptReadAsCustomActionData(hRollbackScript, &pwzRollbackData);
        if (FAILED(hr))
        {
            WcaLogError(hr, "Failed to read rollback script into CustomAction data, continuing anyway.");
        }
        else
        {
            WcaLog(LOGMSG_TRACEONLY, "Rollback Data: %ls", pwzRollbackData);

            pwz = pwzRollbackData;
            hr = WcaReadStringFromCaData(&pwz, &pwzOriginalComment);
            if (FAILED(hr))
            {
                WcaLogError(hr, "failed to read comment from rollback data, continuing anyway");
            }
            else
            {
                pwzComment = pwzOriginalComment;
            }
            hr = WcaReadIntegerFromCaData(&pwz, &iOriginalAttributes);
            if (FAILED(hr))
            {
                WcaLogError(hr, "failed to read attributes from rollback data, continuing anyway");
            }
            else
            {
                iAttributes |= iOriginalAttributes;
            }
        }
    }

    hr = RemoveUserInternal(pwz, pwzDomain, pwzName, iAttributes);

LExit:
    WcaCaScriptClose(hRollbackScript, WCA_CASCRIPT_CLOSE_DELETE);

    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzComment);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzRollbackData);
    ReleaseStr(pwzOriginalComment);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}


/********************************************************************
 RemoveUser - CUSTOM ACTION ENTRY POINT for removing users

  Input:  deferred CustomActionData - Name\tDomain
 * *****************************************************************/
extern "C" UINT __stdcall RemoveUser(
    MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug RemoveUser");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzDomain = NULL;
    LPWSTR pwzComment = NULL;
    int iAttributes = 0;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "RemoveUser");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    //
    // Read in the CustomActionData
    //
    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzName);
    ExitOnFailure(hr, "failed to read name from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
    ExitOnFailure(hr, "failed to read domain from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzComment);
    ExitOnFailure(hr, "failed to read comment from custom action data");

    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data");

    hr = RemoveUserInternal(pwz, pwzDomain, pwzName, iAttributes);

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzComment);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}


/********************************************************************
 CreateGroup - CUSTOM ACTION ENTRY POINT for creating groups

  Input:  deferred CustomActionData - GroupName\tDomain\tComment\tAttributes
 * *****************************************************************/
extern "C" UINT __stdcall CreateGroup(
    __in MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug CreateGroup");

    HRESULT hr = S_OK;
    NET_API_STATUS er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzDomain = NULL;
    LPWSTR pwzComment = NULL;
    LPWSTR pwzScriptKey = NULL;
    int iAttributes = 0;
    BOOL fInitializedCom = FALSE;

    LOCALGROUP_INFO_1* pGroupInfo1 = NULL;

    WCA_CASCRIPT_HANDLE hRollbackScript = NULL;
    int iRollbackAttributes = 0;

    DWORD dw;
    LPWSTR pwzServerName = NULL;

    hr = WcaInitialize(hInstall, "CreateGroup");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    //
    // Read in the CustomActionData
    //
    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzName);
    ExitOnFailure(hr, "failed to read group name from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
    ExitOnFailure(hr, "failed to read domain from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzComment);
    ExitOnFailure(hr, "failed to read group comment from custom action data");

    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzScriptKey);
    ExitOnFailure(hr, "failed to read encoding key from custom action data");

    if (!(SCAG_DONT_CREATE_GROUP & iAttributes))
    {
        hr = GetDomainServerName(pwzDomain, &pwzServerName, DS_WRITABLE_REQUIRED);
        ExitOnFailure(hr, "failed to find Domain %ls.", pwzDomain);

        // Set the group's comment
        if (SCAG_REMOVE_COMMENT & iAttributes)
        {
            StrAllocString(&pwzComment, L"", 0);
        }

        //
        // Create the Group
        //
        LOCALGROUP_INFO_1 groupInfo1;
        groupInfo1.lgrpi1_name = pwzName;
        groupInfo1.lgrpi1_comment = pwzComment;
        er = ::NetLocalGroupAdd(pwzServerName, 1, reinterpret_cast<LPBYTE>(&groupInfo1), &dw);
        hr = HRESULT_FROM_WIN32(er);

        if (HRESULT_FROM_WIN32(ERROR_ALIAS_EXISTS) == hr
            || HRESULT_FROM_WIN32(NERR_GroupExists) == hr)
        {
            if (SCAG_FAIL_IF_EXISTS & iAttributes)
            {
                MessageExitOnFailure(hr, msierrGRPFailedGroupCreateExists, "Group (%ls) was not supposed to exist, but does", pwzName);
            }

            hr = S_OK; // Make sure that we don't report this situation as an error
            // if we fall through the tests that follow.

            if (SCAG_UPDATE_IF_EXISTS & iAttributes)
            {
                er = ::NetLocalGroupGetInfo(pwzServerName, pwzName, 1, reinterpret_cast<LPBYTE*>(&pGroupInfo1));
                hr = HRESULT_FROM_WIN32(er);
                if (S_OK == hr)
                {
                    // There is no rollback scheduled if the key is empty.
                    // Best effort to get original configuration and save it in the script so rollback can restore it.
                    if (*pwzScriptKey)
                    {
                        // Try to open the rollback script
                        hr = WcaCaScriptOpen(WCA_ACTION_INSTALL, WCA_CASCRIPT_ROLLBACK, FALSE, pwzScriptKey, &hRollbackScript);

                        if (hRollbackScript && INVALID_HANDLE_VALUE != hRollbackScript->hScriptFile)
                        {
                            WcaCaScriptClose(hRollbackScript, WCA_CASCRIPT_CLOSE_PRESERVE);
                        }
                        else
                        {
                            hRollbackScript = NULL;
                            hr = WcaCaScriptCreate(WCA_ACTION_INSTALL, WCA_CASCRIPT_ROLLBACK, FALSE, pwzScriptKey, FALSE, &hRollbackScript);
                            ExitOnFailure(hr, "Failed to open rollback CustomAction script.");

                            iRollbackAttributes = 0;

                            hr = WcaCaScriptWriteString(hRollbackScript, pGroupInfo1->lgrpi1_comment);
                            ExitOnFailure(hr, "Failed to add rollback comment to rollback script.");

                            if (!pGroupInfo1->lgrpi1_comment || !*pGroupInfo1->lgrpi1_comment)
                            {
                                iRollbackAttributes |= SCAG_REMOVE_COMMENT;
                            }

                            hr = WcaCaScriptWriteNumber(hRollbackScript, iRollbackAttributes);
                            ExitOnFailure(hr, "Failed to add rollback attributes to rollback script.");

                            // Nudge the system to get all our rollback data written to disk.
                            WcaCaScriptFlush(hRollbackScript);
                        }
                    }
                }

                if (S_OK == hr)
                {
                    if (SCAG_REMOVE_COMMENT & iAttributes)
                    {
                        hr = SetGroupComment(pwzServerName, pwzName, L"");
                        if (FAILED(hr))
                        {
                            WcaLogError(hr, "failed to clear comment for group %ls\\%ls, continuing anyway.", pwzServerName, pwzName);
                            hr = S_OK;
                        }
                    }
                    else if (pwzComment && *pwzComment)
                    {
                        hr = SetGroupComment(pwzServerName, pwzName, pwzComment);
                        if (FAILED(hr))
                        {
                            WcaLogError(hr, "failed to set comment to %ls for group %ls\\%ls, continuing anyway.", pwzComment, pwzServerName, pwzName);
                            hr = S_OK;
                        }
                    }
                }
            }
        }
        MessageExitOnFailure(hr, msierrGRPFailedGroupCreate, "failed to create group: %ls", pwzName);
    }

LExit:
    WcaCaScriptClose(hRollbackScript, WCA_CASCRIPT_CLOSE_PRESERVE);

    if (pGroupInfo1)
    {
        ::NetApiBufferFree((LPVOID)pGroupInfo1);
    }

    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzComment);
    ReleaseStr(pwzScriptKey);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (SCAG_NON_VITAL & iAttributes)
    {
        er = ERROR_SUCCESS;
    }
    else if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}


/********************************************************************
 CreateGroupRollback - CUSTOM ACTION ENTRY POINT for CreateGroup rollback

 * *****************************************************************/
extern "C" UINT __stdcall CreateGroupRollback(
    MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug CreateGroupRollback");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzScriptKey = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzDomain = NULL;
    LPWSTR pwzComment = NULL;
    int iAttributes = 0;
    BOOL fInitializedCom = FALSE;

    WCA_CASCRIPT_HANDLE hRollbackScript = NULL;
    LPWSTR pwzRollbackData = NULL;
    int iOriginalAttributes = 0;
    LPWSTR pwzOriginalComment = NULL;

    hr = WcaInitialize(hInstall, "CreateGroupRollback");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    //
    // Read in the CustomActionData
    //
    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzScriptKey);
    ExitOnFailure(hr, "failed to read encoding key from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzName);
    ExitOnFailure(hr, "failed to read name from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
    ExitOnFailure(hr, "failed to read domain from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzComment);
    ExitOnFailure(hr, "failed to read comment from custom action data");

    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data");

    // Best effort to read original configuration from CreateUser.
    hr = WcaCaScriptOpen(WCA_ACTION_INSTALL, WCA_CASCRIPT_ROLLBACK, FALSE, pwzScriptKey, &hRollbackScript);
    if (FAILED(hr))
    {
        WcaLogError(hr, "Failed to open rollback CustomAction script, continuing anyway.");
    }
    else
    {
        hr = WcaCaScriptReadAsCustomActionData(hRollbackScript, &pwzRollbackData);
        if (FAILED(hr))
        {
            WcaLogError(hr, "Failed to read rollback script into CustomAction data, continuing anyway.");
        }
        else
        {
            WcaLog(LOGMSG_TRACEONLY, "Rollback Data: %ls", pwzRollbackData);

            pwz = pwzRollbackData;
            hr = WcaReadStringFromCaData(&pwz, &pwzOriginalComment);
            if (FAILED(hr))
            {
                WcaLogError(hr, "failed to read comment from rollback data, continuing anyway");
            }
            else
            {
                pwzComment = pwzOriginalComment;
            }
            hr = WcaReadIntegerFromCaData(&pwz, &iOriginalAttributes);
            if (FAILED(hr))
            {
                WcaLogError(hr, "failed to read attributes from rollback data, continuing anyway");
            }
            else
            {
                iAttributes |= iOriginalAttributes;
            }
        }
    }

    hr = RemoveGroupInternal(pwzDomain, pwzName, iAttributes);

LExit:
    WcaCaScriptClose(hRollbackScript, WCA_CASCRIPT_CLOSE_DELETE);

    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzComment);
    ReleaseStr(pwzScriptKey);
    ReleaseStr(pwzRollbackData);
    ReleaseStr(pwzOriginalComment);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}


/********************************************************************
 RemoveGroup - CUSTOM ACTION ENTRY POINT for removing groups

  Input:  deferred CustomActionData - Name\tDomain
 * *****************************************************************/
extern "C" UINT __stdcall RemoveGroup(
    MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug RemoveGroup");

    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzName = NULL;
    LPWSTR pwzDomain = NULL;
    LPWSTR pwzComment = NULL;
    int iAttributes = 0;
    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "RemoveGroup");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = WcaGetProperty(L"CustomActionData", &pwzData);
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    //
    // Read in the CustomActionData
    //
    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzName);
    ExitOnFailure(hr, "failed to read name from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzDomain);
    ExitOnFailure(hr, "failed to read domain from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzComment);
    ExitOnFailure(hr, "failed to read comment from custom action data");

    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data");

    hr = RemoveGroupInternal(pwzDomain, pwzName, iAttributes);

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzName);
    ReleaseStr(pwzDomain);
    ReleaseStr(pwzComment);

    if (fInitializedCom)
    {
        ::CoUninitialize();
    }

    if (FAILED(hr))
    {
        er = ERROR_INSTALL_FAILURE;
    }

    return WcaFinalize(er);
}

HRESULT AlterGroupMembership(bool remove, bool isRollback = false)
{
    HRESULT hr = S_OK;
    NET_API_STATUS er = ERROR_SUCCESS;

    LPWSTR pwzData = NULL;
    LPWSTR pwz = NULL;
    LPWSTR pwzParentName = NULL;
    LPWSTR pwzParentDomain = NULL;
    LPWSTR pwzChildName = NULL;
    LPWSTR pwzChildDomain = NULL;
    int iAttributes = 0;
    LPWSTR pwzChildFullName = NULL;
    LPWSTR pwzServerName = NULL;
    LOCALGROUP_MEMBERS_INFO_3 memberInfo3 = {};
    WCA_CASCRIPT_HANDLE phRollbackScript = NULL;

    if (isRollback)
    {
        // Get a CaScript key
        hr = WcaCaScriptOpen(WCA_ACTION_NONE, WCA_CASCRIPT_ROLLBACK, FALSE, remove ? L"AddGroupMembershipRollback" : L"RemoveGroupMembershipRollback", &phRollbackScript);
        hr = WcaCaScriptReadAsCustomActionData(phRollbackScript, &pwzData);
    }
    else
    {
        hr = WcaCaScriptCreate(WCA_ACTION_NONE, WCA_CASCRIPT_ROLLBACK, FALSE, remove ? L"RemoveGroupMembershipRollback" : L"AddGroupMembershipRollback", TRUE, &phRollbackScript);
        hr = WcaGetProperty(L"CustomActionData", &pwzData);
    }
    ExitOnFailure(hr, "failed to get CustomActionData");

    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzData);

    //
    // Read in the CustomActionData
    //
    pwz = pwzData;
    hr = WcaReadStringFromCaData(&pwz, &pwzParentName);
    ExitOnFailure(hr, "failed to read group name from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzParentDomain);
    ExitOnFailure(hr, "failed to read domain from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzChildName);
    ExitOnFailure(hr, "failed to read group comment from custom action data");

    hr = WcaReadStringFromCaData(&pwz, &pwzChildDomain);
    ExitOnFailure(hr, "failed to read group comment from custom action data");

    hr = WcaReadIntegerFromCaData(&pwz, &iAttributes);
    ExitOnFailure(hr, "failed to read attributes from custom action data");

    hr = GetDomainServerName(pwzParentDomain, &pwzServerName, DS_WRITABLE_REQUIRED);
    ExitOnFailure(hr, "failed to contact domain server %ls", pwzParentDomain);

    if (*pwzChildDomain)
    {
        StrAllocFormatted(&pwzChildFullName, L"%ls\\%ls", pwzChildDomain, pwzChildName);
    }
    else
    {
        StrAllocFormatted(&pwzChildFullName, L"%ls", pwzChildName);
    }
    memberInfo3.lgrmi3_domainandname = pwzChildFullName;

    if (remove)
    {
        er = ::NetLocalGroupDelMembers(pwzServerName, pwzParentName, 3, (LPBYTE)&memberInfo3, 1);
    }
    else
    {
        er = ::NetLocalGroupAddMembers(pwzServerName, pwzParentName, 3, (LPBYTE)&memberInfo3, 1);
    }
    hr = HRESULT_FROM_WIN32(er);

    if (S_OK == hr && !isRollback)
    {
        // we need to log rollback data, we can just use exactly the same data we used to do the initial action though
        WcaCaScriptWriteString(phRollbackScript, pwzParentName);
        WcaCaScriptWriteString(phRollbackScript, pwzParentDomain);
        WcaCaScriptWriteString(phRollbackScript, pwzChildName);
        WcaCaScriptWriteString(phRollbackScript, pwzChildDomain);
        WcaCaScriptWriteNumber(phRollbackScript, iAttributes);
        WcaCaScriptFlush(phRollbackScript);
        WcaCaScriptClose(phRollbackScript, WCA_CASCRIPT_CLOSE_PRESERVE);
    }

    if (remove)
    {
        if (HRESULT_FROM_WIN32(NERR_GroupNotFound) == hr
            || HRESULT_FROM_WIN32(ERROR_NO_SUCH_MEMBER) == hr
            || HRESULT_FROM_WIN32(ERROR_MEMBER_NOT_IN_ALIAS) == hr)
        {
            hr = S_OK;
        }
    }
    else
    {
        if (HRESULT_FROM_WIN32(ERROR_MEMBER_IN_ALIAS) == hr)
        {
            hr = S_OK;
        }
    }

LExit:
    ReleaseStr(pwzData);
    ReleaseStr(pwzParentName);
    ReleaseStr(pwzParentDomain);
    ReleaseStr(pwzChildName);
    ReleaseStr(pwzChildDomain);
    ReleaseStr(pwzChildFullName);
    ReleaseStr(pwzServerName);

    if (SCAG_NON_VITAL & iAttributes)
    {
        return S_OK;
    }
    return hr;
}

/********************************************************************
 AddGroupmembership - CUSTOM ACTION ENTRY POINT for creating groups

  Input:  deferred CustomActionData -
  ParentGroupName\tParentGroupDomain\tChildGroupName\tChildGroupDomain\tAttributes
 * *****************************************************************/
extern "C" UINT __stdcall AddGroupMembership(
    __in MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug AddGroupMembership");

    HRESULT hr = S_OK;

    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "AddGroupMembership");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = AlterGroupMembership(false, false);

LExit:
    if (fInitializedCom)
    {
        ::CoUninitialize();
    }
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILED : ERROR_SUCCESS);
}

/********************************************************************
 AddGroupmembership - CUSTOM ACTION ENTRY POINT for creating groups

  Input:  deferred CustomActionData -
  ParentGroupName\tParentGroupDomain\tChildGroupName\tChildGroupDomain\tAttributes
 * *****************************************************************/
extern "C" UINT __stdcall AddGroupMembershipRollback(
    __in MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug AddGroupMembershipRollback");

    HRESULT hr = S_OK;

    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "AddGroupMembershipRollback");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = AlterGroupMembership(true, true);

LExit:
    if (fInitializedCom)
    {
        ::CoUninitialize();
    }
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILED : ERROR_SUCCESS);
}

/********************************************************************
 RemoveGroupMembership - CUSTOM ACTION ENTRY POINT for creating groups

  Input:  deferred CustomActionData -
  ParentGroupName\tParentGroupDomain\tChildGroupName\tChildGroupDomain\tAttributes
 * *****************************************************************/
extern "C" UINT __stdcall RemoveGroupMembership(
    __in MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug RemoveGroupMembership");

    HRESULT hr = S_OK;

    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "RemoveGroupMembership");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = AlterGroupMembership(true, false);

LExit:
    if (fInitializedCom)
    {
        ::CoUninitialize();
    }
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILED : ERROR_SUCCESS);
}

/********************************************************************
 RemoveGroupMembershipRollback - CUSTOM ACTION ENTRY POINT for creating groups

  Input:  deferred CustomActionData -
  ParentGroupName\tParentGroupDomain\tChildGroupName\tChildGroupDomain\tAttributes
 * *****************************************************************/
extern "C" UINT __stdcall RemoveGroupMembershipRollback(
    __in MSIHANDLE hInstall
)
{
    //AssertSz(0, "Debug RemoveGroupMembershipRollback");

    HRESULT hr = S_OK;

    BOOL fInitializedCom = FALSE;

    hr = WcaInitialize(hInstall, "RemoveGroupMembershipRollback");
    ExitOnFailure(hr, "failed to initialize");

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");
    fInitializedCom = TRUE;

    hr = AlterGroupMembership(false, true);

LExit:
    if (fInitializedCom)
    {
        ::CoUninitialize();
    }
    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILED : ERROR_SUCCESS);
}
