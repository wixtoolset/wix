#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

// IDD_BURN_SPLASH_SCREEN_CONFIGURATION, BURN_SPLASH_SCREEN_TYPE, and BURN_SPLASH_SCREEN_CONFIGURATION must stay in sync with src\wix\WixToolset.Core.Burn\Bundles\CreateBundleExeCommand.cs

#define IDD_BURN_SPLASH_SCREEN_CONFIGURATION 1

// constants

enum BURN_SPLASH_SCREEN_TYPE
{
    BURN_SPLASH_SCREEN_TYPE_NONE,
    BURN_SPLASH_SCREEN_TYPE_BITMAP_RESOURCE,
};

// structs

typedef struct _BURN_SPLASH_SCREEN_CONFIGURATION
{
    BURN_SPLASH_SCREEN_TYPE type;
    WORD wResourceId;
} BURN_SPLASH_SCREEN_CONFIGURATION;


// functions

void SplashScreenCreate(
    __in HINSTANCE hInstance,
    __in_z_opt LPCWSTR wzCaption,
    __out HWND* pHwnd
    );
HRESULT SplashScreenDisplayError(
    __in BOOTSTRAPPER_DISPLAY display,
    __in_z LPCWSTR wzBundleName,
    __in HRESULT hrError
    );

#if defined(__cplusplus)
}
#endif
