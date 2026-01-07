// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static const LPCWSTR WIX_BUNDLE_ICON_FILENAME = L"WixBundle.ico";


//
// LoadBundleIcon - loads the icon that was (optionally) authored in the bundle otherwise use the one embedded in the bootstrapper application.
//
HRESULT LoadBundleIcon(
    __in HMODULE hModule,
    __out HICON* phIcon,
    __out HICON* phSmallIcon
)
{
    HRESULT hr = S_OK;
    LPWSTR sczIconPath = NULL;
    int nIconWidth = ::GetSystemMetrics(SM_CXICON);
    int nIconHeight = ::GetSystemMetrics(SM_CYICON);
    int nSmallIconWidth = ::GetSystemMetrics(SM_CXSMICON);
    int nSmallIconHeight = ::GetSystemMetrics(SM_CYSMICON);
    HICON hIcon = NULL;
    HICON hSmallIcon = NULL;

    // First look for the optional authored bundle icon.
    hr = PathRelativeToModule(&sczIconPath, WIX_BUNDLE_ICON_FILENAME, hModule);
    ExitOnFailure(hr, "Failed to get path to bundle icon: %ls", WIX_BUNDLE_ICON_FILENAME);

    if (FileExistsEx(sczIconPath, NULL))
    {
        hIcon = reinterpret_cast<HICON>(::LoadImageW(NULL, sczIconPath, IMAGE_ICON, nIconWidth, nIconHeight, LR_LOADFROMFILE));

        hSmallIcon = reinterpret_cast<HICON>(::LoadImageW(NULL, sczIconPath, IMAGE_ICON, nSmallIconWidth, nSmallIconHeight, LR_LOADFROMFILE));
    }
    else // fallback to the first icon resource in the bootstrapper application.
    {
        hIcon = reinterpret_cast<HICON>(::LoadImageW(hModule, MAKEINTRESOURCEW(1), IMAGE_ICON, nIconWidth, nIconHeight, LR_DEFAULTCOLOR));

        hSmallIcon = reinterpret_cast<HICON>(::LoadImageW(hModule, MAKEINTRESOURCEW(1), IMAGE_ICON, nSmallIconWidth, nSmallIconHeight, LR_DEFAULTCOLOR));
    }

    *phIcon = hIcon;
    *phSmallIcon = hSmallIcon;

LExit:
    ReleaseStr(sczIconPath);

    return hr;
}
