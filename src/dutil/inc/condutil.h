#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(__cplusplus)
extern "C" {
#endif

// function declarations

HRESULT DAPI CondEvaluate(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    );

#if defined(__cplusplus)
}
#endif
