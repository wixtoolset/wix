#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

// Generic action enum.
enum SCA_ACTION
{
    SCA_ACTION_NONE,
    SCA_ACTION_INSTALL,
    SCA_ACTION_UNINSTALL
};

// sql database attributes definitions
enum SCADB_ATTRIBUTES
{
    SCADB_CREATE_ON_INSTALL = 0x00000001,
    SCADB_DROP_ON_UNINSTALL = 0x00000002,
    SCADB_CONTINUE_ON_ERROR = 0x00000004,
    SCADB_DROP_ON_INSTALL = 0x00000008,
    SCADB_CREATE_ON_UNINSTALL = 0x00000010,
    SCADB_CONFIRM_OVERWRITE = 0x00000020,
    SCADB_CREATE_ON_REINSTALL = 0x00000040,
    SCADB_DROP_ON_REINSTALL = 0x00000080,
};

// sql string/script attributes definitions
enum SCASQL_ATTRIBUTES
{
    SCASQL_EXECUTE_ON_INSTALL = 0x00000001,
    SCASQL_EXECUTE_ON_UNINSTALL = 0x00000002,
    SCASQL_CONTINUE_ON_ERROR = 0x00000004,
    SCASQL_ROLLBACK = 0x00000008,
    SCASQL_EXECUTE_ON_REINSTALL = 0x00000010,
};
