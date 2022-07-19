#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


HRESULT CreateBootstrapperApplication(
    __in HMODULE hModule,
    __in_opt PREQBA_DATA* pPrereqData,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults,
    __out IBootstrapperApplication** ppApplication
    );

void DestroyBootstrapperApplication(
    __in IBootstrapperApplication* pApplication,
    __in const BOOTSTRAPPER_DESTROY_ARGS* pArgs,
    __inout BOOTSTRAPPER_DESTROY_RESULTS* pResults
    );
