// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const int ERROR_STRING_BUFFER = 1024;

static char szMsg[ERROR_STRING_BUFFER];
static WCHAR wzMsg[ERROR_STRING_BUFFER];

void CALLBACK DutilTestTraceError(
    __in_z LPCSTR /*szFile*/,
    __in int /*iLine*/,
    __in REPORT_LEVEL /*rl*/,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    if (DUTIL_SOURCE_EXTERNAL == source)
    {
        ::StringCchPrintfA(szMsg, countof(szMsg), szFormat, args);
        MultiByteToWideChar(CP_ACP, 0, szMsg, -1, wzMsg, countof(wzMsg));
        throw gcnew System::Exception(System::String::Format("hr = 0x{0:X8}, message = {1}", hrError, gcnew System::String(wzMsg)));
    }
}
