#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


struct INTERNAL_UI_BA_STATE
{
    BOOL fInitialized;
    HINSTANCE hInstance;
    LPWSTR sczAppBase;
    HMODULE hPrereqModule;
    PREQBA_DATA prereqData;
    IBootstrapperApplication* pApplication;
};
