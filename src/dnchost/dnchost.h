#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


struct DNCSTATE
{
    BOOL fInitialized;
    BOOL fInitializedRuntime;
    HINSTANCE hInstance;
    LPWSTR sczModuleFullPath;
    LPWSTR sczAppBase;
    LPWSTR sczManagedHostPath;
    LPWSTR sczBaFactoryAssemblyName;
    LPWSTR sczBaFactoryAssemblyPath;
    LPWSTR sczBaFactoryDepsJsonPath;
    LPWSTR sczBaFactoryRuntimeConfigPath;
    HOSTFXR_STATE hostfxrState;
    IBootstrapperApplicationFactory* pAppFactory;
};
