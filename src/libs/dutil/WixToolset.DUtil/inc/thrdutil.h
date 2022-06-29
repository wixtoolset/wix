#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

/********************************************************************
 ThrdWaitForCompletion - waits for thread to complete and gets return code.

 *******************************************************************/
HRESULT DAPI ThrdWaitForCompletion(
    __in HANDLE hThread,
    __in DWORD dwTimeout,
    __out_opt DWORD* pdwReturnCode
    );

#ifdef __cplusplus
}
#endif

