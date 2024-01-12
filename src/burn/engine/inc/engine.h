#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


// constants

// If these defaults ever change, be sure to update constants in wix\WixToolset.Core.Burn\Bundles\BurnCommon.cs as well.
#define BURN_SECTION_NAME ".wixburn"
#define BURN_SECTION_MAGIC 0x00f14300
#define BURN_SECTION_VERSION 0x00000002

// This needs to be incremented whenever a breaking change is made to the Burn protocol.
#define BURN_PROTOCOL_VERSION 1

#if defined(__cplusplus)
extern "C" {
#endif


// function declarations

HRESULT EngineRun(
    __in HINSTANCE hInstance,
    __in HANDLE hEngineFile,
    __in_z_opt LPCWSTR wzCommandLine,
    __in int nCmdShow,
    __out DWORD* pdwExitCode
    );


#if defined(__cplusplus)
}
#endif
