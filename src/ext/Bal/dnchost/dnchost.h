#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


enum DNCHOSTTYPE
{
    DNCHOSTTYPE_UNKNOWN,
    DNCHOSTTYPE_FDD,
    DNCHOSTTYPE_SCD,
};

struct DNCSTATE
{
    BOOL fInitialized;
    BOOL fInitializedRuntime;
    HINSTANCE hInstance;
    LPWSTR sczModuleFullPath;
    LPWSTR sczAppBase;
    LPWSTR sczBaFactoryAssemblyName;
    LPWSTR sczBaFactoryAssemblyPath;
    LPWSTR sczBaFactoryDepsJsonPath;
    LPWSTR sczBaFactoryRuntimeConfigPath;
    DNCHOSTTYPE type;
    HOSTFXR_STATE hostfxrState;
    IBootstrapperApplicationFactory* pAppFactory;
    HMODULE hMbapreqModule;
    PREQBA_DATA prereqData;
};
