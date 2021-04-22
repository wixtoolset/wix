#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#ifdef __cplusplus
extern "C" {
#endif

/*******************************************************************
 UncConvertFromMountedDrive - Converts the string in-place from a
                mounted drive path to a UNC path
*******************************************************************/
DAPI_(HRESULT) UncConvertFromMountedDrive(
    __inout LPWSTR *psczUNCPath,
    __in LPCWSTR sczMountedDrivePath
    );

#ifdef __cplusplus
}
#endif
