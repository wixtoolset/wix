// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

const UINT COST_FILEFORMATTING = 2000;


//
// WixSchedFormatFiles - immediate CA to schedule format files CAs
//
extern "C" UINT __stdcall WixSchedFormatFiles(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    PSCZ sczBinaryKey;
    PSCZ sczFileKey;
    PSCZ sczComponentKey;
    PSCZ sczFormattedFile;
    PSCZ sczFilePath;
    PMSIHANDLE hView;
    PMSIHANDLE hRec;
    PSCZ sczFileContent;
    PSCZ sczFormattedContent;
    PSCZ sczExecCustomActionData;
    PSCZ sczRollbackCustomActionData;

    LPCWSTR wzQuery =
        L"SELECT `WixFormatFiles`.`Binary_`, `WixFormatFiles`.`File_`, `File`.`Component_` "
        L"FROM `WixFormatFiles`, `File` "
        L"WHERE `WixFormatFiles`.`File_` = `File`.`File`";
    enum eQuery { eqBinaryKey = 1, eqFileKey, eqComponentKey };

    // initialize
    hr = WcaInitialize(hInstall, "WixSchedFormatFiles");
    ExitOnFailure(hr, "Failed to initialize for WixSchedFormatFiles.");

    // query and loop through all the files
    hr = WcaOpenExecuteView(wzQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on WixFormatFiles table");

    DWORD cFiles = 0;
    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        ++cFiles;

        hr = WcaGetRecordString(hRec, eqBinaryKey, &sczBinaryKey);
        ExitOnFailure(hr, "Failed to get Binary table key.");

        hr = WcaGetRecordString(hRec, eqFileKey, &sczFileKey);
        ExitOnFailure(hr, "Failed to get File table key.");

        hr = WcaGetRecordString(hRec, eqComponentKey, &sczComponentKey);
        ExitOnFailure(hr, "Failed to get Component table key.");

        // we need to know if the component's being installed, uninstalled, or reinstalled
        WCA_TODO todo = WcaGetComponentToDo(sczComponentKey);
        if (WCA_TODO_INSTALL == todo || WCA_TODO_REINSTALL == todo)
        {
            // turn the file key into the path to the target file
            hr = StrAllocFormatted(&sczFormattedFile, L"[#%ls]", sczFileKey);
            ExitOnFailure(hr, "Failed to format file string for file: %ls", sczFileKey);
            hr = WcaGetFormattedString(sczFormattedFile, &sczFilePath);
            ExitOnFailure(hr, "Failed to get path for file: %ls", sczFileKey);

            // extract binary to string
            WCA_ENCODING encoding = WCA_ENCODING_UNKNOWN;
            hr = WcaExtractBinaryToString(sczBinaryKey, &sczFileContent, &encoding);
            ExitOnFailure(hr, "Failed to extract binary: %ls", sczBinaryKey);

            // format string
            hr = WcaGetFormattedString(sczFileContent, &sczFormattedContent);
            ExitOnFailure(hr, "Failed to format file content: %ls", sczFileContent);

            // write to deferred custom action data
            hr = WcaWriteStringToCaData(sczFilePath, &sczExecCustomActionData);
            ExitOnFailure(hr, "Failed to write deferred custom action data for file: %ls", sczFilePath);

            hr = WcaWriteIntegerToCaData(encoding, &sczExecCustomActionData);
            ExitOnFailure(hr, "Failed to write deferred custom action data for encoding: %d", encoding);

            hr = WcaWriteStringToCaData(sczFormattedContent, &sczExecCustomActionData);
            ExitOnFailure(hr, "Failed to write deferred custom action data for file content: %ls", sczFilePath);

            // write to rollback custom action data
            hr = WcaWriteStringToCaData(sczFilePath, &sczRollbackCustomActionData);
            ExitOnFailure(hr, "Failed to write rollback custom action data for file: %ls", sczFilePath);

            hr = WcaWriteIntegerToCaData(encoding, &sczRollbackCustomActionData);
            ExitOnFailure(hr, "Failed to write deferred custom action data for encoding: %d", encoding);

            hr = WcaWriteStringToCaData(sczFileContent, &sczRollbackCustomActionData);
            ExitOnFailure(hr, "Failed to write rollback custom action data for file content: %ls", sczFilePath);
        }
    }

    // reaching the end of the list is actually a good thing, not an error
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "Failure occurred while processing WixFormatFiles table");

    // schedule deferred CAs if there's anything to do
    if (sczRollbackCustomActionData && *sczRollbackCustomActionData)
    {
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"WixRollbackFormatFiles"), sczRollbackCustomActionData, cFiles * COST_FILEFORMATTING);
        ExitOnFailure(hr, "Failed to schedule WixRollbackFormatFiles");
    }

    if (sczExecCustomActionData && *sczExecCustomActionData)
    {
        hr = WcaDoDeferredAction(PLATFORM_DECORATION(L"WixExecFormatFiles"), sczExecCustomActionData, cFiles * COST_FILEFORMATTING);
        ExitOnFailure(hr, "Failed to schedule WixExecFormatFiles");
    }

LExit:
    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}


//
// WixExecFormatFiles - deferred and rollback CAs to write formatted files
//
extern "C" UINT __stdcall WixExecFormatFiles(
    __in MSIHANDLE hInstall
    )
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    PSCZ sczCustomActionData;
    LPWSTR pwz = NULL;
    PSCZ sczFilePath;
    PSCZ sczFileContent;
    LPSTR psz = NULL;

    // initialize
    hr = WcaInitialize(hInstall, "WixExecFormatFiles");
    ExitOnFailure(hr, "Failed to initialize for WixExecFormatFiles.");

    hr = WcaGetProperty(L"CustomActionData", &sczCustomActionData);
    ExitOnFailure(hr, "Failed to get CustomActionData.");
#ifdef _DEBUG
    WcaLog(LOGMSG_STANDARD, "CustomActionData: %ls", sczCustomActionData);
#endif

    // loop through all the passed in data
    pwz = sczCustomActionData;
    while (pwz && *pwz)
    {
        // extract the custom action data
        hr = WcaReadStringFromCaData(&pwz, &sczFilePath);
        ExitOnFailure(hr, "Failed to read file path from custom action data");

        WCA_ENCODING encoding = WCA_ENCODING_UNKNOWN;
        hr = WcaReadIntegerFromCaData(&pwz, reinterpret_cast<int*>(&encoding));
        ExitOnFailure(hr, "Failed to read encoding from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &sczFileContent);
        ExitOnFailure(hr, "Failed to read file content from custom action data");

        // re-encode content
        LPCBYTE pbData = NULL;
        size_t cbData = 0;
        switch (encoding)
        {
        case WCA_ENCODING_UTF_16:
            pbData = reinterpret_cast<LPCBYTE>(LPCWSTR(sczFileContent));
            cbData = lstrlenW(sczFileContent) * sizeof(WCHAR);
            break;

        case WCA_ENCODING_UTF_8:
            hr = StrAnsiAllocString(&psz, sczFileContent, 0, CP_UTF8);
            ExitOnFailure(hr, "Failed to convert Unicode to UTF-8.");
            pbData = reinterpret_cast<LPCBYTE>(psz);

            hr = ::StringCbLengthA(psz, STRSAFE_MAX_CCH, &cbData);
            ExitOnFailure(hr, "Failed to count UTF-8 bytes.");
            break;

        case WCA_ENCODING_ANSI:
            hr = StrAnsiAllocString(&psz, sczFileContent, 0, CP_ACP);
            ExitOnFailure(hr, "Failed to convert Unicode to ANSI.");
            pbData = reinterpret_cast<LPCBYTE>(psz);

            hr = ::StringCbLengthA(psz, STRSAFE_MAX_CCH, &cbData);
            ExitOnFailure(hr, "Failed to count UTF-8 bytes.");
            break;

        default:
            break;
        }

#ifdef _DEBUG
        WcaLog(LOGMSG_STANDARD, "File: %ls", sczCustomActionData);
        WcaLog(LOGMSG_STANDARD, "Content: %ls", sczFileContent);
#endif

        // write file and preserve modified time
        FILETIME filetime;

        hr = FileGetTime(sczFilePath, NULL, NULL, &filetime);
        ExitOnFailure(hr, "Failed to get modified time of file : %ls", sczFilePath);

        hr = FileWrite(sczFilePath, FILE_ATTRIBUTE_NORMAL, pbData, static_cast<DWORD>(cbData), NULL);
        ExitOnFailure(hr, "Failed to write file content: %ls", sczFilePath);

        hr = FileSetTime(sczFilePath, NULL, NULL, &filetime);
        ExitOnFailure(hr, "Failed to set modified time of file : %ls", sczFilePath);

        // Tick the progress bar
        hr = WcaProgressMessage(COST_FILEFORMATTING, FALSE);
        ExitOnFailure(hr, "Failed to tick progress bar for file: %ls", sczFilePath);
    }

LExit:
    ReleaseStr(psz);

    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}
