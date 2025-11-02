// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// internal function declarations

static HRESULT IsRunDll32(
    __in BURN_VARIABLES* pVariables,
    __in LPCWSTR wzExecutablePath
    );

// function definitions

extern "C" HRESULT ApprovedExesParseFromXml(
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in IXMLDOMNode* pixnBundle
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR scz = NULL;

    // select approved exe nodes
    hr = XmlSelectNodes(pixnBundle, L"ApprovedExeForElevation", &pixnNodes);
    ExitOnFailure(hr, "Failed to select approved exe nodes.");

    // get approved exe node count
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get approved exe node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // allocate memory for approved exes
    pApprovedExes->rgApprovedExes = (BURN_APPROVED_EXE*)MemAlloc(sizeof(BURN_APPROVED_EXE) * cNodes, TRUE);
    ExitOnNull(pApprovedExes->rgApprovedExes, hr, E_OUTOFMEMORY, "Failed to allocate memory for approved exe structs.");

    pApprovedExes->cApprovedExes = cNodes;

    // parse approved exe elements
    for (DWORD i = 0; i < cNodes; ++i)
    {
        BURN_APPROVED_EXE* pApprovedExe = &pApprovedExes->rgApprovedExes[i];

        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &pApprovedExe->sczId);
        ExitOnFailure(hr, "Failed to get @Id.");

        // @Key
        hr = XmlGetAttributeEx(pixnNode, L"Key", &pApprovedExe->sczKey);
        ExitOnFailure(hr, "Failed to get @Key.");

        // @ValueName
        hr = XmlGetAttributeEx(pixnNode, L"ValueName", &pApprovedExe->sczValueName);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @ValueName.");
        }

        // @Win64
        hr = XmlGetYesNoAttribute(pixnNode, L"Win64", &pApprovedExe->fWin64);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get @Win64.");
        }

        // prepare next iteration
        ReleaseNullObject(pixnNode);
        ReleaseNullStr(scz);
    }

    hr = S_OK;

LExit:
    ReleaseObject(pixnNodes);
    ReleaseObject(pixnNode);
    ReleaseStr(scz);
    return hr;
}

extern "C" void ApprovedExesUninitialize(
    __in BURN_APPROVED_EXES* pApprovedExes
    )
{
    if (pApprovedExes->rgApprovedExes)
    {
        for (DWORD i = 0; i < pApprovedExes->cApprovedExes; ++i)
        {
            BURN_APPROVED_EXE* pApprovedExe = &pApprovedExes->rgApprovedExes[i];

            ReleaseStr(pApprovedExe->sczId);
            ReleaseStr(pApprovedExe->sczKey);
            ReleaseStr(pApprovedExe->sczValueName);
        }
        MemFree(pApprovedExes->rgApprovedExes);
    }
}

extern "C" void ApprovedExesUninitializeLaunch(
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe
    )
{
    if (pLaunchApprovedExe)
    {
        ReleaseStr(pLaunchApprovedExe->sczArguments);
        ReleaseStr(pLaunchApprovedExe->sczExecutablePath);
        ReleaseStr(pLaunchApprovedExe->sczId);
    }
}

extern "C" HRESULT ApprovedExesFindById(
    __in BURN_APPROVED_EXES* pApprovedExes,
    __in_z LPCWSTR wzId,
    __out BURN_APPROVED_EXE** ppApprovedExe
    )
{
    HRESULT hr = S_OK;
    BURN_APPROVED_EXE* pApprovedExe = NULL;

    for (DWORD i = 0; i < pApprovedExes->cApprovedExes; ++i)
    {
        pApprovedExe = &pApprovedExes->rgApprovedExes[i];

        if (CSTR_EQUAL == ::CompareStringOrdinal(pApprovedExe->sczId, -1, wzId, -1, FALSE))
        {
            *ppApprovedExe = pApprovedExe;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}

extern "C" HRESULT ApprovedExesLaunch(
    __in BURN_VARIABLES* pVariables,
    __in BURN_LAUNCH_APPROVED_EXE* pLaunchApprovedExe,
    __out DWORD* pdwProcessId
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczArgumentsFormatted = NULL;
    LPWSTR sczArgumentsObfuscated = NULL;
    LPWSTR sczCommand = NULL;
    LPWSTR sczCommandObfuscated = NULL;
    LPWSTR sczExecutableDirectory = NULL;
    size_t cchExecutableDirectory = 0;
    STARTUPINFOW si = { };
    PROCESS_INFORMATION pi = { };

    // build command
    if (pLaunchApprovedExe->sczArguments && *pLaunchApprovedExe->sczArguments)
    {
        hr = VariableFormatString(pVariables, pLaunchApprovedExe->sczArguments, &sczArgumentsFormatted, NULL);
        ExitOnFailure(hr, "Failed to format argument string.");

        hr = StrAllocFormattedSecure(&sczCommand, L"\"%ls\" %s", pLaunchApprovedExe->sczExecutablePath, sczArgumentsFormatted);
        ExitOnFailure(hr, "Failed to create executable command.");

        hr = VariableFormatStringObfuscated(pVariables, pLaunchApprovedExe->sczArguments, &sczArgumentsObfuscated, NULL);
        ExitOnFailure(hr, "Failed to format obfuscated argument string.");

        hr = StrAllocFormatted(&sczCommandObfuscated, L"\"%ls\" %s", pLaunchApprovedExe->sczExecutablePath, sczArgumentsObfuscated);
    }
    else
    {
        hr = StrAllocFormatted(&sczCommand, L"\"%ls\"", pLaunchApprovedExe->sczExecutablePath);
        ExitOnFailure(hr, "Failed to create executable command.");

        hr = StrAllocFormatted(&sczCommandObfuscated, L"\"%ls\"", pLaunchApprovedExe->sczExecutablePath);
    }
    ExitOnFailure(hr, "Failed to create obfuscated executable command.");

    // Try to get the directory of the executable so we can set the current directory of the process to help those executables
    // that expect stuff to be relative to them.  Best effort only.
    hr = PathGetDirectory(pLaunchApprovedExe->sczExecutablePath, &sczExecutableDirectory);
    if (SUCCEEDED(hr))
    {
        // CreateProcessW has undocumented MAX_PATH restriction for lpCurrentDirectory even when long path support is enabled.
        hr = ::StringCchLengthW(sczExecutableDirectory, MAX_PATH - 1, &cchExecutableDirectory);
    }

    if (FAILED(hr))
    {
        ReleaseNullStr(sczExecutableDirectory);

        hr = S_OK;
    }

    LogId(REPORT_STANDARD, MSG_LAUNCHING_APPROVED_EXE, pLaunchApprovedExe->sczExecutablePath, sczCommandObfuscated);

    si.cb = sizeof(si);
    if (!::CreateProcessW(pLaunchApprovedExe->sczExecutablePath, sczCommand, NULL, NULL, FALSE, CREATE_NEW_PROCESS_GROUP, NULL, sczExecutableDirectory, &si, &pi))
    {
        ExitWithLastError(hr, "Failed to CreateProcess on path: %ls", pLaunchApprovedExe->sczExecutablePath);
    }

    *pdwProcessId = pi.dwProcessId;

    if (pLaunchApprovedExe->dwWaitForInputIdleTimeout)
    {
        ::WaitForInputIdle(pi.hProcess, pLaunchApprovedExe->dwWaitForInputIdleTimeout);
    }

LExit:
    StrSecureZeroFreeString(sczArgumentsFormatted);
    ReleaseStr(sczArgumentsObfuscated);
    StrSecureZeroFreeString(sczCommand);
    ReleaseStr(sczCommandObfuscated);
    ReleaseStr(sczExecutableDirectory);

    ReleaseHandle(pi.hThread);
    ReleaseHandle(pi.hProcess);

    return hr;
}

extern "C" HRESULT ApprovedExesVerifySecureLocation(
    __in BURN_CACHE* pCache,
    __in BURN_VARIABLES* pVariables,
    __in LPCWSTR wzExecutablePath,
    __in int argc,
    __in LPCWSTR* argv
    )
{
    HRESULT hr = S_OK;
    LPWSTR scz = NULL;
    LPWSTR sczSecondary = NULL;
    LPWSTR sczRunDll32Param = NULL;

    const LPCWSTR vrgSecureFolderVariables[] = {
        L"ProgramFiles64Folder",
        L"ProgramFilesFolder",
    };

    for (DWORD i = 0; i < countof(vrgSecureFolderVariables); ++i)
    {
        LPCWSTR wzSecureFolderVariable = vrgSecureFolderVariables[i];

        hr = VariableGetString(pVariables, wzSecureFolderVariable, &scz);
        if (SUCCEEDED(hr))
        {
            hr = PathDirectoryContainsPath(scz, wzExecutablePath);
            if (S_OK == hr)
            {
                ExitFunction();
            }
        }
        else if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get the variable: %ls", wzSecureFolderVariable);
        }
    }

    // The problem with using a Variable for the root package cache folder is that it might not have been secured yet.
    // Getting it through CacheGetPerMachineRootCompletedPath makes sure it has been secured.
    hr = CacheGetPerMachineRootCompletedPath(pCache, &scz, &sczSecondary);
    ExitOnFailure(hr, "Failed to get the root package cache folder.");

    // If the package cache is redirected, hr is S_FALSE.
    if (S_FALSE == hr)
    {
        hr = PathDirectoryContainsPath(sczSecondary, wzExecutablePath);
        if (S_OK == hr)
        {
            ExitFunction();
        }
    }

    hr = PathDirectoryContainsPath(scz, wzExecutablePath);
    if (S_OK == hr)
    {
        ExitFunction();
    }

    // Test if executable is rundll32.exe, and it's target is in a secure location
    // Example for CUDA UninstallString: "C:\WINDOWS\SysWOW64\RunDll32.EXE" "C:\Program Files\NVIDIA Corporation\Installer2\InstallerCore\NVI2.DLL",UninstallPackage CUDAToolkit_12.8
    if (argc && argv && argv[0] && *argv[0])
    {
        hr = IsRunDll32(pVariables, wzExecutablePath);
        ExitOnFailure(hr, "Failed to test whether executable is rundll32");

        if (hr == S_OK)
        {
            LPCWSTR szComma = wcschr(argv[0], L',');
            if (szComma && *szComma)
            {
                hr = StrAllocString(&sczRunDll32Param, argv[0], szComma - argv[0]);
                ExitOnFailure(hr, "Failed to allocate string");
            }
            else
            {
                hr = StrAllocString(&sczRunDll32Param, argv[0], 0);
                ExitOnFailure(hr, "Failed to allocate string");
            }

            hr = ApprovedExesVerifySecureLocation(pCache, pVariables, sczRunDll32Param, 0, NULL);
            ExitOnFailure(hr, "Failed to test whether rundll32's parameter, '%ls', is in a secure location", sczRunDll32Param);
            if (hr == S_OK)
            {
                ExitFunction();
            }
        }
    }

    hr = S_FALSE;

LExit:
    ReleaseStr(scz);
    ReleaseStr(sczSecondary);
    ReleaseStr(sczRunDll32Param);

    return hr;
}

static HRESULT IsRunDll32(
    __in BURN_VARIABLES* pVariables,
    __in LPCWSTR wzExecutablePath
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczFolder = NULL;
    LPWSTR sczFullPath = NULL;
    BOOL fEqual = FALSE;

    hr = VariableGetString(pVariables, L"SystemFolder", &sczFolder);
    ExitOnFailure(hr, "Failed to get the variable: SystemFolder");

    hr = PathConcat(sczFolder, L"rundll32.exe", &sczFullPath);
    ExitOnFailure(hr, "Failed to combine paths");

    hr = PathCompareCanonicalized(wzExecutablePath, sczFullPath, &fEqual);
    ExitOnFailure(hr, "Failed to compare paths");
    if (fEqual)
    {
        hr = S_OK;
        ExitFunction();
    }

    hr = VariableGetString(pVariables, L"System64Folder", &sczFolder);
    ExitOnFailure(hr, "Failed to get the variable: System64Folder");

    hr = PathConcat(sczFolder, L"rundll32.exe", &sczFullPath);
    ExitOnFailure(hr, "Failed to combine paths");

    hr = PathCompareCanonicalized(wzExecutablePath, sczFullPath, &fEqual);
    ExitOnFailure(hr, "Failed to compare paths");
    if (fEqual)
    {
        hr = S_OK;
        ExitFunction();
    }

    // Sysnative
    hr = PathSystemWindowsSubdirectory(L"SysNative\\", &sczFolder);
    ExitOnFailure(hr, "Failed to append SysNative directory.");

    hr = PathConcat(sczFolder, L"rundll32.exe", &sczFullPath);
    ExitOnFailure(hr, "Failed to combine paths");

    hr = PathCompareCanonicalized(wzExecutablePath, sczFullPath, &fEqual);
    ExitOnFailure(hr, "Failed to compare paths");
    if (fEqual)
    {
        hr = S_OK;
        ExitFunction();
    }

    hr = S_FALSE;

LExit:
    ReleaseStr(sczFolder);
    ReleaseStr(sczFullPath);

    return hr;
}
