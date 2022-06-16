#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

HRESULT CreateBAFunctions(
    __in HMODULE hModule,
    __in IBootstrapperEngine* pEngine,
    __in const BA_FUNCTIONS_CREATE_ARGS* pArgs,
    __in BA_FUNCTIONS_CREATE_RESULTS* pResults,
    __out IBAFunctions** ppApplication
    );
