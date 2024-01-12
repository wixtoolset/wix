// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static void CALLBACK WixstdbaTraceError(
    __in_z LPCSTR szFile,
    __in int iLine,
    __in REPORT_LEVEL rl,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    );

EXTERN_C int WINAPI wWinMain(
    __in HINSTANCE hInstance,
    __in_opt HINSTANCE /* hPrevInstance */,
    __in_z_opt LPWSTR /*lpCmdLine*/,
    __in int /*nCmdShow*/
    )
{
    HRESULT hr = S_OK;
    IBootstrapperApplication* pApplication = NULL;

    DutilInitialize(&WixstdbaTraceError);

    hr = CreateWixStandardBootstrapperApplication(hInstance, &pApplication);
    ExitOnFailure(hr, "Failed to create WiX standard bootstrapper application.");

    hr = BootstrapperApplicationRun(pApplication);
    ExitOnFailure(hr, "Failed to run WiX standard bootstrapper application.");

LExit:
    ReleaseObject(pApplication);

    return 0;
}

static void CALLBACK WixstdbaTraceError(
    __in_z LPCSTR /*szFile*/,
    __in int /*iLine*/,
    __in REPORT_LEVEL /*rl*/,
    __in UINT source,
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    // BalLogError currently uses the Exit... macros,
    // so if expanding the scope need to ensure this doesn't get called recursively.
    if (DUTIL_SOURCE_THMUTIL == source)
    {
        BalLogErrorArgs(hrError, szFormat, args);
    }
}
