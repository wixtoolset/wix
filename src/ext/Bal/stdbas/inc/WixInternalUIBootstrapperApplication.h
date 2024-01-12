#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

EXTERN_C HRESULT CreateWixInternalUIBootstrapperApplication(
    __in HINSTANCE hInstance,
    __out IBootstrapperApplication** ppApplication
    );
