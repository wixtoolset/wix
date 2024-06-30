#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.
#include "scagroup.h"

enum USER_EXISTS
{
    USER_EXISTS_YES,
    USER_EXISTS_NO,
    USER_EXISTS_INDETERMINATE
};


struct SCA_USER
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzComponent[MAX_DARWIN_KEY + 1];
    INSTALLSTATE isInstalled;
    INSTALLSTATE isAction;

    WCHAR wzDomain[MAX_DARWIN_COLUMN + 1];
    WCHAR wzName[MAX_DARWIN_COLUMN + 1];
    WCHAR wzPassword[MAX_DARWIN_COLUMN + 1];
    WCHAR wzComment[MAX_DARWIN_COLUMN + 1];
    INT iAttributes;

    SCA_GROUP *psgGroups;

    SCA_USER *psuNext;
};


// prototypes
HRESULT __stdcall ScaGetUser(
    __in LPCWSTR wzUser,
    __out SCA_USER* pscau
    );
HRESULT __stdcall ScaGetUserDeferred(
    __in LPCWSTR wzUser,
    __in WCA_WRAPQUERY_HANDLE hUserQuery,
    __out SCA_USER* pscau
    );
void ScaUserFreeList(
    __in SCA_USER* psuList
    );
HRESULT ScaUserRead(
    __inout SCA_USER** ppsuList
    );
HRESULT ScaUserExecute(
    __in SCA_USER *psuList
    );
