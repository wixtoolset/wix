#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


struct PREQBA_DATA
{
    HRESULT hrHostInitialization;
    BOOL fCompleted;
};

extern "C" typedef HRESULT(WINAPI* PFN_PREQ_BOOTSTRAPPER_APPLICATION_CREATE)(
    __in PREQBA_DATA* pPreqData,
    __in IBootstrapperEngine* pEngine,
    __in const BOOTSTRAPPER_CREATE_ARGS* pArgs,
    __inout BOOTSTRAPPER_CREATE_RESULTS* pResults
    );
