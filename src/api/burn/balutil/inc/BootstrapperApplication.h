#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "IBootstrapperApplication.h"

#if defined(__cplusplus)
extern "C" {
#endif

/*******************************************************************
 BootstrapperApplicationRun - runs the IBootstrapperApplication until
                              the application quits.

********************************************************************/
HRESULT __stdcall BootstrapperApplicationRun(
    __in IBootstrapperApplication* pApplication
    );

#if defined(__cplusplus)
}
#endif
