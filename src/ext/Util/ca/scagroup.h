#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

enum GROUP_EXISTS
{
    GROUP_EXISTS_YES,
    GROUP_EXISTS_NO,
    GROUP_EXISTS_INDETERMINATE
};

// structs
struct SCA_GROUP
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzComponent[MAX_DARWIN_KEY + 1];
    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;

    WCHAR wzDomain[MAX_DARWIN_COLUMN + 1];
    WCHAR wzName[MAX_DARWIN_COLUMN + 1];
    WCHAR wzComment[MAX_DARWIN_COLUMN + 1];
    INT iAttributes;

    SCA_GROUP* psgParents;
    SCA_GROUP* psgChildren;

    SCA_GROUP *psgNext;
};


// prototypes
HRESULT __stdcall ScaGetGroup(
    __in LPCWSTR wzGroup,
    __out SCA_GROUP* pscag
    );
HRESULT __stdcall ScaGetGroupDeferred(
    __in LPCWSTR wzGroup,
    __in WCA_WRAPQUERY_HANDLE hGroupQuery,
    __out SCA_GROUP* pscag
    );
void ScaGroupFreeList(
    __in SCA_GROUP* psgList
    );
HRESULT ScaGroupRead(
    __inout SCA_GROUP** ppsgList
    );
HRESULT ScaGroupMembershipRemoveExecute(
        __in SCA_GROUP* psgList
    );
HRESULT ScaGroupMembershipAddExecute(
    __in SCA_GROUP* psgList
);
HRESULT ScaGroupExecute(
    __in SCA_GROUP*psgList
    );
