#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

// user creation attributes definitions
enum SCAU_ATTRIBUTES
{
    SCAU_DONT_EXPIRE_PASSWRD = 0x00000001,
    SCAU_PASSWD_CANT_CHANGE = 0x00000002,
    SCAU_PASSWD_CHANGE_REQD_ON_LOGIN = 0x00000004,
    SCAU_DISABLE_ACCOUNT = 0x00000008,
    SCAU_FAIL_IF_EXISTS = 0x00000010,
    SCAU_UPDATE_IF_EXISTS = 0x00000020,
    SCAU_ALLOW_LOGON_AS_SERVICE = 0x00000040,
    SCAU_ALLOW_LOGON_AS_BATCH = 0x00000080,

    SCAU_DONT_REMOVE_ON_UNINSTALL = 0x00000100,
    SCAU_DONT_CREATE_USER = 0x00000200,
    SCAU_NON_VITAL = 0x00000400,
};