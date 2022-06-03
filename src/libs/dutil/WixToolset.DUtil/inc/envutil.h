#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

/********************************************************************
 EnvExpandEnvironmentStrings - Wrapper for ::ExpandEnvironmentStrings.

 *******************************************************************/
HRESULT DAPI EnvExpandEnvironmentStrings(
    __in LPCWSTR wzSource,
    __out LPWSTR* psczExpanded,
    __out_opt SIZE_T* pcchExpanded
    );

#ifdef __cplusplus
}
#endif

