// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT CompressFiles(HANDLE hCab);
static void __stdcall CabNamesCallback(LPWSTR wzFirstCabName, LPWSTR wzNewCabName, LPWSTR wzFileToken);


HRESULT SmartCabCommand(
    __in int argc,
    __in LPWSTR argv[]
)
{
    HRESULT hr = E_INVALIDARG;
    LPCWSTR wzCabPath = NULL;
    LPCWSTR wzCabName = NULL;
    LPWSTR sczCabDir = NULL;
    UINT uiFileCount = 0;
    UINT uiMaxSize = 0;
    UINT uiMaxThresh = 0;
    COMPRESSION_TYPE ct = COMPRESSION_TYPE_NONE;
    HANDLE hCab = NULL;

    if (argc < 1)
    {
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Must specify: outCabPath [compressionType] [fileCount] [maxSizePerCabInMB [maxThreshold]]");
    }
    else
    {
        wzCabPath = argv[0];
        wzCabName = PathFile(wzCabPath);

        hr = PathGetDirectory(wzCabPath, &sczCabDir);
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Could not parse directory from path: %ls", wzCabPath);

        if (argc > 1)
        {
            UINT uiCompressionType;
            hr = StrStringToUInt32(argv[1], 0, &uiCompressionType);
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Could not parse compression type as number: %ls", argv[1]);

            ct = (uiCompressionType > 4) ? COMPRESSION_TYPE_HIGH : static_cast<COMPRESSION_TYPE>(uiCompressionType);
        }

        if (argc > 2)
        {
            hr = StrStringToUInt32(argv[2], 0, &uiFileCount);
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Could not parse file count as number: %ls", argv[2]);
        }

        if (argc > 3)
        {
            hr = StrStringToUInt32(argv[3], 0, &uiMaxSize);
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Could not parse max size as number: %ls", argv[3]);
        }

        if (argc > 4)
        {
            hr = StrStringToUInt32(argv[4], 0, &uiMaxThresh);
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "Could not parse max threshold as number: %ls", argv[4]);
        }
    }

    hr = CabCBegin(wzCabName, sczCabDir, uiFileCount, uiMaxSize, uiMaxThresh, ct, &hCab);
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to initialize cabinet: %ls", wzCabPath);

    if (uiFileCount > 0)
    {
        hr = CompressFiles(hCab);
        ExitOnFailure(hr, "failed to compress files into cabinet: %ls", wzCabPath);
    }

    hr = CabCFinish(hCab, CabNamesCallback);
    hCab = NULL; // once finish is called, the handle is invalid.
    ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to compress cabinet: %ls", wzCabPath);


LExit:
    if (hCab)
    {
        CabCCancel(hCab);
    }
    ReleaseStr(sczCabDir);

    return hr;
}


static HRESULT CompressFiles(
    __in HANDLE hCab
)
{
    HRESULT hr = S_OK;
    LPWSTR sczLine = NULL;
    LPWSTR* rgsczSplit = NULL;
    UINT cSplit = 0;
    MSIFILEHASHINFO hashInfo = { sizeof(MSIFILEHASHINFO) };

    for (;;)
    {
        hr = ConsoleReadW(&sczLine);
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to read smartcab line from stdin");

        if (!*sczLine)
        {
            break;
        }

        hr = StrSplitAllocArray(&rgsczSplit, &cSplit, sczLine, L"\t");
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to split smartcab line from stdin: %ls", sczLine);

        if (cSplit != 2 && cSplit != 6)
        {
            hr = E_INVALIDARG;
            ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to split smartcab line into hash x 4, token, source file: %ls", sczLine);
        }

        LPCWSTR wzFilePath = rgsczSplit[0];
        LPCWSTR wzToken = rgsczSplit[1];
        PMSIFILEHASHINFO pHashInfo = NULL;

        if (cSplit == 6)
        {
            for (int i = 0; i < 4; ++i)
            {
                LPCWSTR wzHash = rgsczSplit[i + 2];

                hr = StrStringToInt32(wzHash, 0, reinterpret_cast<INT*>(hashInfo.dwData + i));
                ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to parse hash: %ls for file: %ls", wzHash, wzFilePath);
            }

            pHashInfo = &hashInfo;
        }

        hr = CabCAddFile(wzFilePath, wzToken, pHashInfo, hCab);
        ConsoleExitOnFailure(hr, CONSOLE_COLOR_RED, "failed to add file: %ls", wzFilePath);

        ReleaseNullStrArray(rgsczSplit, cSplit);
    }

LExit:
    ReleaseNullStrArray(rgsczSplit, cSplit);
    ReleaseStr(sczLine);

    return hr;
}


// Callback from PFNFCIGETNEXTCABINET CabCGetNextCabinet method
// First argument is the name of splitting cabinet without extension e.g. "cab1"
// Second argument is name of the new cabinet that would be formed by splitting e.g. "cab1b.cab"
// Third argument is the file token of the first file present in the splitting cabinet
static void __stdcall CabNamesCallback(
    __in LPWSTR wzFirstCabName,
    __in LPWSTR wzNewCabName,
    __in LPWSTR wzFileToken
)
{
    ConsoleWriteLine(CONSOLE_COLOR_NORMAL, "%ls\t%ls\t%ls", wzFirstCabName, wzNewCabName, wzFileToken);
}
