#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


struct MBASTATE
{
    BOOL fInitialized;
    BOOL fInitializedRuntime;
    BOOL fStoppedRuntime;
    HINSTANCE hInstance;
    LPWSTR sczAppBase;
    LPWSTR sczConfigPath;
    mscorlib::_AppDomain* pAppDomain;
    ICorRuntimeHost* pCLRHost;
    HMODULE hMbapreqModule;
    PREQBA_DATA prereqData;
};
