#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

// TODO: remove when definition is provided in hostfxr.h.
typedef int32_t(HOSTFXR_CALLTYPE* hostfxr_get_dotnet_environment_info_fn)(
    const char_t* dotnet_root,
    void* reserved,
    hostfxr_get_dotnet_environment_info_result_fn result,
    void* result_context
    );
