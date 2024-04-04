// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "SfxUtil.h"

#define GUID_STRING_LENGTH 39

/// <summary>
/// Writes a formatted message to the MSI log.
/// Does out-of-proc MSI calls if necessary.
/// </summary>
void Log(MSIHANDLE hSession, const wchar_t* szMessage, ...)
{
        const int LOG_BUFSIZE = 4096;
        wchar_t szBuf[LOG_BUFSIZE];
        va_list args;
        va_start(args, szMessage);
        StringCchVPrintf(szBuf, LOG_BUFSIZE, szMessage, args);

        if (!g_fRunningOutOfProc || NULL == g_pRemote)
        {
                MSIHANDLE hRec = MsiCreateRecord(1);
                MsiRecordSetString(hRec, 0, L"SFXCA: [1]");
                MsiRecordSetString(hRec, 1, szBuf);
                MsiProcessMessage(hSession, INSTALLMESSAGE_INFO, hRec);
                MsiCloseHandle(hRec);
        }
        else
        {
                // Logging is the only remote-MSI operation done from unmanaged code.
                // It's not very convenient here because part of the infrastructure
                // for remote MSI APIs is on the managed side.

                RemoteMsiSession::RequestData req;
                RemoteMsiSession::RequestData* pResp = NULL;
                SecureZeroMemory(&req, sizeof(RemoteMsiSession::RequestData));

                req.fields[0].vt = VT_UI4;
                req.fields[0].uiValue = 1;
                g_pRemote->SendRequest(RemoteMsiSession::MsiCreateRecord, &req, &pResp);
                MSIHANDLE hRec = (MSIHANDLE) pResp->fields[0].iValue;

                req.fields[0].vt = VT_I4;
                req.fields[0].iValue = (int) hRec;
                req.fields[1].vt = VT_UI4;
                req.fields[1].uiValue = 0;
                req.fields[2].vt = VT_LPWSTR;
                req.fields[2].szValue = L"SFXCA: [1]";
                g_pRemote->SendRequest(RemoteMsiSession::MsiRecordSetString, &req, &pResp);

                req.fields[0].vt = VT_I4;
                req.fields[0].iValue = (int) hRec;
                req.fields[1].vt = VT_UI4;
                req.fields[1].uiValue = 1;
                req.fields[2].vt = VT_LPWSTR;
                req.fields[2].szValue = szBuf;
                g_pRemote->SendRequest(RemoteMsiSession::MsiRecordSetString, &req, &pResp);

                req.fields[0].vt = VT_I4;
                req.fields[0].iValue = (int) hSession;
                req.fields[1].vt = VT_I4;
                req.fields[1].iValue = (int) INSTALLMESSAGE_INFO;
                req.fields[2].vt = VT_I4;
                req.fields[2].iValue = (int) hRec;
                g_pRemote->SendRequest(RemoteMsiSession::MsiProcessMessage, &req, &pResp);

                req.fields[0].vt = VT_I4;
                req.fields[0].iValue = (int) hRec;
                req.fields[1].vt = VT_EMPTY;
                req.fields[2].vt = VT_EMPTY;
                g_pRemote->SendRequest(RemoteMsiSession::MsiCloseHandle, &req, &pResp);
        }
}

/// <summary>
/// Deletes a directory, including all files and subdirectories.
/// </summary>
/// <param name="szDir">Path to the directory to delete,
/// not including a trailing backslash.</param>
/// <returns>True if the directory was successfully deleted, or false
/// if the deletion failed (most likely because some files were locked).
/// </returns>
bool DeleteDirectory(const wchar_t* szDir)
{
        size_t cchDir = wcslen(szDir);
        size_t cchPathBuf = cchDir + 3 + MAX_PATH;
        wchar_t* szPath = (wchar_t*) _alloca(cchPathBuf * sizeof(wchar_t));
        if (szPath == NULL) return false;
        StringCchCopy(szPath, cchPathBuf, szDir);
        StringCchCat(szPath, cchPathBuf, L"\\*");
        WIN32_FIND_DATA fd;
        HANDLE hSearch = FindFirstFile(szPath, &fd);
        while (hSearch != INVALID_HANDLE_VALUE)
        {
                StringCchCopy(szPath + cchDir + 1, cchPathBuf - (cchDir + 1), fd.cFileName);
                if ((fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY) != 0)
                {
                        if (wcscmp(fd.cFileName, L".") != 0
                            && wcscmp(fd.cFileName, L"..") != 0
                            && ((fd.dwFileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) == 0))
                        {
                                DeleteDirectory(szPath);
                        }
                }
                else
                {
                        DeleteFile(szPath);
                }
                if (!FindNextFile(hSearch, &fd))
                {
                        FindClose(hSearch);
                        hSearch = INVALID_HANDLE_VALUE;
                }
        }

        for (int i = 0; i < 3; i++)
        {
            if (::RemoveDirectory(szDir))
            {
                return true;
            }

            ::Sleep(100);
        }

        return false;
}

static HRESULT CreateGuid(
    _Out_z_cap_c_(GUID_STRING_LENGTH) wchar_t* wzGuid)
{
        HRESULT hr = S_OK;
        RPC_STATUS rs = RPC_S_OK;
        UUID guid = {};

        rs = ::UuidCreate(&guid);
        if (rs != RPC_S_OK)
        {
                hr = (HRESULT)(rs | FACILITY_RPC);
        }
        else if (!::StringFromGUID2(guid, wzGuid, GUID_STRING_LENGTH))
        {
                hr = E_OUTOFMEMORY;
        }
        else // make the temp directory more recognizable for easy deletion.
        {
                // Copy the first four hex chars of the GUID over the dashes in the GUID and trim the, so
                // '{1234ABCD-ABCD-ABCD-ABCD-ABCDABCDABCD}' turns into '{1234ABCD1ABCD2ABCD3ABCD4ABCDABCDABCD}'
                wzGuid[9] = wzGuid[1];
                wzGuid[14] = wzGuid[2];
                wzGuid[19] = wzGuid[3];
                wzGuid[24] = wzGuid[4];

                // Now '{1234ABCD1ABCD2ABCD3ABCD4ABCDABCDABCD}' turns into 'SFXCAABCD1ABCD2ABCD3ABCD4ABCDABCDABCD'
                wzGuid[0] = L'S';
                wzGuid[1] = L'F';
                wzGuid[2] = L'X';
                wzGuid[3] = L'C';
                wzGuid[4] = L'A';
                wzGuid[GUID_STRING_LENGTH - 2] = L'\0';
        }

        return hr;
}

static HRESULT ProcessElevated()
{
        HRESULT hr = S_OK;
        HANDLE hToken = NULL;
        TOKEN_ELEVATION tokenElevated = {};
        DWORD cbToken = 0;

        if (::OpenProcessToken(::GetCurrentProcess(), TOKEN_QUERY, &hToken) &&
            ::GetTokenInformation(hToken, TokenElevation, &tokenElevated, sizeof(TOKEN_ELEVATION), &cbToken))
        {
                hr = (0 != tokenElevated.TokenIsElevated) ? S_OK : S_FALSE;
        }
        else
        {
                hr = HRESULT_FROM_WIN32(::GetLastError());
        }

        return hr;
}

/// <summary>
/// Extracts a cabinet that is concatenated to a module
/// to a new temporary directory.
/// </summary>
/// <param name="hSession">Handle to the installer session,
/// used just for logging.</param>
/// <param name="hModule">Module that has the concatenated cabinet.</param>
/// <param name="szTempDir">Buffer for returning the path of the
/// created temp directory.</param>
/// <param name="cchTempDirBuf">Size in characters of the buffer.
/// <returns>True if the files were extracted, or false if the
/// buffer was too small or the directory could not be created
/// or the extraction failed for some other reason.</returns>
__success(return != false)
bool ExtractToTempDirectory(__in MSIHANDLE hSession, __in HMODULE hModule,
        __out_ecount_z(cchTempDirBuf) wchar_t* szTempDir, DWORD cchTempDirBuf)
{
        HRESULT hr = S_OK;
        wchar_t szModule[MAX_PATH] = {};
        wchar_t szGuid[GUID_STRING_LENGTH] = {};

        DWORD cchCopied = ::GetModuleFileName(hModule, szModule, MAX_PATH - 1);
        if (cchCopied == 0 || cchCopied == MAX_PATH - 1)
        {
                hr = HRESULT_FROM_WIN32(::GetLastError());
                if (SUCCEEDED(hr))
                {
                        hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                }

                Log(hSession, L"Failed to get module path. Error code 0x%x.", hr);
                goto LExit;
        }

        hr = CreateGuid(szGuid);
        if (FAILED(hr))
        {
                Log(hSession, L"Failed to create a GUID. Error code 0x%x", hr);
                goto LExit;
        }

        // Unelevated we use the user's temp directory.
        hr = ProcessElevated();
        if (S_FALSE == hr)
        {
                // Temp path is documented to be returned with a trailing backslash.
                cchCopied = ::GetTempPath(cchTempDirBuf, szTempDir);
                if (cchCopied == 0 || cchCopied >= cchTempDirBuf)
                {
                        hr = HRESULT_FROM_WIN32(::GetLastError());
                        if (SUCCEEDED(hr))
                        {
                                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                        }

                        Log(hSession, L"Failed to get user temp directory. Error code 0x%x", hr);
                        goto LExit;
                }
        }
        else // elevated or we couldn't check (in the latter case, assume we're elevated since it's safer to use)
        {
                // Windows directory will not contain a trailing backslash, so we add it next.
                cchCopied = ::GetWindowsDirectoryW(szTempDir, cchTempDirBuf);
                if (cchCopied == 0 || cchCopied >= cchTempDirBuf)
                {
                        hr = HRESULT_FROM_WIN32(::GetLastError());
                        if (SUCCEEDED(hr))
                        {
                                hr = HRESULT_FROM_WIN32(ERROR_INSUFFICIENT_BUFFER);
                        }

                        Log(hSession, L"Failed to get Windows directory. Error code 0x%x", hr);
                        goto LExit;
                }

                hr = ::StringCchCat(szTempDir, cchTempDirBuf, L"\\Installer\\");
                if (FAILED(hr))
                {
                        Log(hSession, L"Failed append 'Installer' to Windows directory. Error code 0x%x", hr);
                        goto LExit;
                }
        }

        hr = ::StringCchCat(szTempDir, cchTempDirBuf, szGuid);
        if (FAILED(hr))
        {
                Log(hSession, L"Failed append GUID to temp path. Error code 0x%x", hr);
                goto LExit;
        }

        if (!::CreateDirectory(szTempDir, NULL))
        {
                hr = HRESULT_FROM_WIN32(::GetLastError());
                Log(hSession, L"Failed to create temp directory. Error code 0x%x", hr);
                goto LExit;
        }

        Log(hSession, L"Extracting custom action to temporary directory: %s\\", szTempDir);
        int err = ExtractCabinet(szModule, szTempDir);
        if (err != 0)
        {
                hr = E_FAIL;
                Log(hSession, L"Failed to extract to temporary directory. Cabinet error code %d.", err);
                DeleteDirectory(szTempDir);
        }

LExit:
        return SUCCEEDED(hr);
}
