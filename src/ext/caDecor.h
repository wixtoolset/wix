#pragma once
// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.


#if defined(_M_ARM64)
#define CUSTOM_ACTION_DECORATION(f) L"Wix4" f L"_A64"
#elif defined(_M_AMD64)
#define CUSTOM_ACTION_DECORATION(f) L"Wix4" f L"_X64"
#elif defined(_M_ARM)
#define CUSTOM_ACTION_DECORATION(f) L"Wix4" f L"_ARM"
#else
#define CUSTOM_ACTION_DECORATION(f) L"Wix4" f L"_X86"
#endif

#if defined(_M_ARM64)
#define CUSTOM_ACTION_DECORATION5(f) L"Wix5" f L"_A64"
#elif defined(_M_AMD64)
#define CUSTOM_ACTION_DECORATION5(f) L"Wix5" f L"_X64"
#elif defined(_M_ARM)
#define CUSTOM_ACTION_DECORATION5(f) L"Wix5" f L"_ARM"
#else
#define CUSTOM_ACTION_DECORATION5(f) L"Wix5" f L"_X86"
#endif
