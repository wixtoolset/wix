// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

// function definitions

/********************************************************************
CondEvaluate - evaluates the condition using the given variables.
********************************************************************/
extern "C" HRESULT DAPI CondEvaluate(
    __in VARIABLES_HANDLE pVariables,
    __in_z LPCWSTR wzCondition,
    __out BOOL* pf
    )
{
    UNREFERENCED_PARAMETER(pVariables);
    UNREFERENCED_PARAMETER(wzCondition);
    UNREFERENCED_PARAMETER(pf);
    return E_NOTIMPL;
}
