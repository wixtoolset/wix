#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.



// structs
struct SCA_GROUP
{
    WCHAR wzKey[MAX_DARWIN_KEY + 1];
    WCHAR wzComponent[MAX_DARWIN_KEY + 1];

    WCHAR wzDomain[MAX_DARWIN_COLUMN + 1];
    WCHAR wzName[MAX_DARWIN_COLUMN + 1];

    SCA_GROUP *psgNext;
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
    INT iAttributes;

    SCA_GROUP *psgGroups;

    SCA_USER *psuNext;
};


// prototypes
HRESULT __stdcall ScaGetUser(
    __in LPCWSTR wzUser, 
    __out SCA_USER* pscau
    );
