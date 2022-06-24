// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define ShelExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_SHELUTIL, x, s, __VA_ARGS__)
#define ShelExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_SHELUTIL, x, s, __VA_ARGS__)
#define ShelExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_SHELUTIL, x, s, __VA_ARGS__)
#define ShelExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_SHELUTIL, x, s, __VA_ARGS__)
#define ShelExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_SHELUTIL, x, s, __VA_ARGS__)
#define ShelExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_SHELUTIL, x, e, s, __VA_ARGS__)
#define ShelExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_SHELUTIL, x, s, __VA_ARGS__)
#define ShelExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_SHELUTIL, p, x, e, s, __VA_ARGS__)
#define ShelExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_SHELUTIL, p, x, s, __VA_ARGS__)
#define ShelExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_SHELUTIL, p, x, e, s, __VA_ARGS__)
#define ShelExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_SHELUTIL, p, x, s, __VA_ARGS__)
#define ShelExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_SHELUTIL, e, x, s, __VA_ARGS__)
#define ShelExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_SHELUTIL, g, x, s, __VA_ARGS__)

static PFN_SHELLEXECUTEEXW vpfnShellExecuteExW = ::ShellExecuteExW;

static HRESULT DAPI GetFolderFromCsidl(
    __out_z LPWSTR* psczFolderPath,
    __in int csidlFolder
    );
static HRESULT GetDesktopShellView(
    __in REFIID riid,
    __out void **ppv
    );
static HRESULT GetShellDispatchFromView(
    __in IShellView *psv,
    __in REFIID riid,
    __out void **ppv
    );

/********************************************************************
 ShelFunctionOverride - overrides the shell functions. Typically used
                       for unit testing.

*********************************************************************/
extern "C" void DAPI ShelFunctionOverride(
    __in_opt PFN_SHELLEXECUTEEXW pfnShellExecuteExW
    )
{
    vpfnShellExecuteExW = pfnShellExecuteExW ? pfnShellExecuteExW : ::ShellExecuteExW;
}


/********************************************************************
 ShelExec() - executes a target.

*******************************************************************/
extern "C" HRESULT DAPI ShelExec(
    __in_z LPCWSTR wzTargetPath,
    __in_z_opt LPCWSTR wzParameters,
    __in_z_opt LPCWSTR wzVerb,
    __in_z_opt LPCWSTR wzWorkingDirectory,
    __in int nShowCmd,
    __in_opt HWND hwndParent,
    __out_opt HANDLE* phProcess
    )
{
    HRESULT hr = S_OK;
    SHELLEXECUTEINFOW shExecInfo = { };
    size_t cchWorkingDirectory = 0;

    // CreateProcessW has undocumented MAX_PATH restriction for lpCurrentDirectory even when long path support is enabled.
    if (wzWorkingDirectory && FAILED(::StringCchLengthW(wzWorkingDirectory, MAX_PATH - 1, &cchWorkingDirectory)))
    {
        wzWorkingDirectory = NULL;
    }

    shExecInfo.cbSize = sizeof(SHELLEXECUTEINFO);
    shExecInfo.fMask = SEE_MASK_FLAG_DDEWAIT | SEE_MASK_FLAG_NO_UI | SEE_MASK_NOCLOSEPROCESS;
    shExecInfo.hwnd = hwndParent;
    shExecInfo.lpVerb = wzVerb;
    shExecInfo.lpFile = wzTargetPath;
    shExecInfo.lpParameters = wzParameters;
    shExecInfo.lpDirectory = wzWorkingDirectory;
    shExecInfo.nShow = nShowCmd;

    if (!vpfnShellExecuteExW(&shExecInfo))
    {
        ShelExitWithLastError(hr, "ShellExecEx failed with return code: %d", Dutil_er);
    }

    if (phProcess)
    {
        *phProcess = shExecInfo.hProcess;
        shExecInfo.hProcess = NULL;
    }

LExit:
    ReleaseHandle(shExecInfo.hProcess);

    return hr;
}


/********************************************************************
 ShelExecUnelevated() - executes a target unelevated.

*******************************************************************/
extern "C" HRESULT DAPI ShelExecUnelevated(
    __in_z LPCWSTR wzTargetPath,
    __in_z_opt LPCWSTR wzParameters,
    __in_z_opt LPCWSTR wzVerb,
    __in_z_opt LPCWSTR wzWorkingDirectory,
    __in int nShowCmd
    )
{
    HRESULT hr = S_OK;
    BSTR bstrTargetPath = NULL;
    VARIANT vtParameters = { };
    VARIANT vtVerb = { };
    VARIANT vtWorkingDirectory = { };
    VARIANT vtShow = { };
    IShellView* psv = NULL;
    IShellDispatch2* psd = NULL;

    bstrTargetPath = ::SysAllocString(wzTargetPath);
    ShelExitOnNull(bstrTargetPath, hr, E_OUTOFMEMORY, "Failed to allocate target path BSTR.");

    if (wzParameters && *wzParameters)
    {
        vtParameters.vt = VT_BSTR;
        vtParameters.bstrVal = ::SysAllocString(wzParameters);
        ShelExitOnNull(bstrTargetPath, hr, E_OUTOFMEMORY, "Failed to allocate parameters BSTR.");
    }

    if (wzVerb && *wzVerb)
    {
        vtVerb.vt = VT_BSTR;
        vtVerb.bstrVal = ::SysAllocString(wzVerb);
        ShelExitOnNull(bstrTargetPath, hr, E_OUTOFMEMORY, "Failed to allocate verb BSTR.");
    }

    if (wzWorkingDirectory && *wzWorkingDirectory)
    {
        vtWorkingDirectory.vt = VT_BSTR;
        vtWorkingDirectory.bstrVal = ::SysAllocString(wzWorkingDirectory);
        ShelExitOnNull(bstrTargetPath, hr, E_OUTOFMEMORY, "Failed to allocate working directory BSTR.");
    }

    vtShow.vt = VT_INT;
    vtShow.intVal = nShowCmd;

    hr = GetDesktopShellView(IID_PPV_ARGS(&psv));
    ShelExitOnFailure(hr, "Failed to get desktop shell view.");

    hr = GetShellDispatchFromView(psv, IID_PPV_ARGS(&psd));
    ShelExitOnFailure(hr, "Failed to get shell dispatch from view.");

    hr = psd->ShellExecute(bstrTargetPath, vtParameters, vtWorkingDirectory, vtVerb, vtShow);
    if (S_FALSE == hr)
    {
        hr = HRESULT_FROM_WIN32(ERROR_CANCELLED);
    }
    ShelExitOnRootFailure(hr, "Failed to launch unelevate executable: %ls", bstrTargetPath);

LExit:
    ReleaseObject(psd);
    ReleaseObject(psv);
    ReleaseBSTR(vtWorkingDirectory.bstrVal);
    ReleaseBSTR(vtVerb.bstrVal);
    ReleaseBSTR(vtParameters.bstrVal);
    ReleaseBSTR(bstrTargetPath);

    return hr;
}


static HRESULT DAPI GetFolderFromCsidl(
    __out_z LPWSTR* psczFolderPath,
    __in int csidlFolder
    )
{
    HRESULT hr = S_OK;
    WCHAR wzPath[MAX_PATH];

    hr = ::SHGetFolderPathW(NULL, csidlFolder | CSIDL_FLAG_CREATE, NULL, SHGFP_TYPE_CURRENT, wzPath);
    ShelExitOnFailure(hr, "Failed to get folder path for CSIDL: %d", csidlFolder);

    hr = StrAllocString(psczFolderPath, wzPath, 0);
    ShelExitOnFailure(hr, "Failed to copy shell folder path: %ls", wzPath);

    hr = PathBackslashTerminate(psczFolderPath);
    ShelExitOnFailure(hr, "Failed to backslash terminate shell folder path: %ls", *psczFolderPath);

LExit:
    return hr;
}


EXTERN_C typedef HRESULT (STDAPICALLTYPE *PFN_SHGetKnownFolderPath)(
    REFKNOWNFOLDERID rfid,
    DWORD dwFlags,
    HANDLE hToken,
    PWSTR *ppszPath
    );

extern "C" HRESULT DAPI ShelGetKnownFolder(
    __out_z LPWSTR* psczFolderPath,
    __in REFKNOWNFOLDERID rfidFolder
    )
{
    HRESULT hr = S_OK;
    HMODULE hShell32Dll = NULL;
    PFN_SHGetKnownFolderPath pfn = NULL;
    LPWSTR pwzPath = NULL;

    hr = LoadSystemLibrary(L"shell32.dll", &hShell32Dll);
    if (E_MODNOTFOUND == hr)
    {
        TraceError(hr, "Failed to load shell32.dll");
        ExitFunction1(hr = E_NOTIMPL);
    }
    ShelExitOnFailure(hr, "Failed to load shell32.dll.");

    pfn = reinterpret_cast<PFN_SHGetKnownFolderPath>(::GetProcAddress(hShell32Dll, "SHGetKnownFolderPath"));
    ShelExitOnNull(pfn, hr, E_NOTIMPL, "Failed to find SHGetKnownFolderPath entry point.");

    hr = pfn(rfidFolder, KF_FLAG_CREATE, NULL, &pwzPath);
    ShelExitOnFailure(hr, "Failed to get known folder path.");

    hr = StrAllocString(psczFolderPath, pwzPath, 0);
    ShelExitOnFailure(hr, "Failed to copy shell folder path: %ls", pwzPath);

    hr = PathBackslashTerminate(psczFolderPath);
    ShelExitOnFailure(hr, "Failed to backslash terminate shell folder path: %ls", *psczFolderPath);

LExit:
    if (pwzPath)
    {
        ::CoTaskMemFree(pwzPath);
    }

    if (hShell32Dll)
    {
        ::FreeLibrary(hShell32Dll);
    }

    return hr;
}

extern "C" HRESULT DAPI ShelGetFolder(
    __out_z LPWSTR* psczFolderPath,
    __in int csidlFolder
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;
    KNOWNFOLDERID rfid = { };

    csidlFolder &= ~CSIDL_FLAG_MASK;

    switch (csidlFolder)
    {
    case CSIDL_ADMINTOOLS:
        rfid = FOLDERID_AdminTools;
        break;
    case CSIDL_APPDATA:
        rfid = FOLDERID_RoamingAppData;
        break;
    case CSIDL_CDBURN_AREA:
        rfid = FOLDERID_CDBurning;
        break;
    case CSIDL_COMMON_ADMINTOOLS:
        rfid = FOLDERID_CommonAdminTools;
        break;
    case CSIDL_COMMON_APPDATA:
        rfid = FOLDERID_ProgramData;
        break;
    case CSIDL_COMMON_DESKTOPDIRECTORY:
        rfid = FOLDERID_PublicDesktop;
        break;
    case CSIDL_COMMON_DOCUMENTS:
        rfid = FOLDERID_PublicDocuments;
        break;
    case CSIDL_COMMON_MUSIC:
        rfid = FOLDERID_PublicMusic;
        break;
    case CSIDL_COMMON_OEM_LINKS:
        rfid = FOLDERID_CommonOEMLinks;
        break;
    case CSIDL_COMMON_PICTURES:
        rfid = FOLDERID_PublicPictures;
        break;
    case CSIDL_COMMON_PROGRAMS:
        rfid = FOLDERID_CommonPrograms;
        break;
    case CSIDL_COMMON_STARTMENU:
        rfid = FOLDERID_CommonStartMenu;
        break;
    case CSIDL_COMMON_STARTUP: __fallthrough;
    case CSIDL_COMMON_ALTSTARTUP:
        rfid = FOLDERID_CommonStartup;
        break;
    case CSIDL_COMMON_TEMPLATES:
        rfid = FOLDERID_CommonTemplates;
        break;
    case CSIDL_COMMON_VIDEO:
        rfid = FOLDERID_PublicVideos;
        break;
    case CSIDL_COOKIES:
        rfid = FOLDERID_Cookies;
        break;
    case CSIDL_DESKTOP:
    case CSIDL_DESKTOPDIRECTORY:
        rfid = FOLDERID_Desktop;
        break;
    case CSIDL_FAVORITES: __fallthrough;
    case CSIDL_COMMON_FAVORITES:
        rfid = FOLDERID_Favorites;
        break;
    case CSIDL_FONTS:
        rfid = FOLDERID_Fonts;
        break;
    case CSIDL_HISTORY:
        rfid = FOLDERID_History;
        break;
    case CSIDL_INTERNET_CACHE:
        rfid = FOLDERID_InternetCache;
        break;
    case CSIDL_LOCAL_APPDATA:
        rfid = FOLDERID_LocalAppData;
        break;
    case CSIDL_MYMUSIC:
        rfid = FOLDERID_Music;
        break;
    case CSIDL_MYPICTURES:
        rfid = FOLDERID_Pictures;
        break;
    case CSIDL_MYVIDEO:
        rfid = FOLDERID_Videos;
        break;
    case CSIDL_NETHOOD:
        rfid = FOLDERID_NetHood;
        break;
    case CSIDL_PERSONAL:
        rfid = FOLDERID_Documents;
        break;
    case CSIDL_PRINTHOOD:
        rfid = FOLDERID_PrintHood;
        break;
    case CSIDL_PROFILE:
        rfid = FOLDERID_Profile;
        break;
    case CSIDL_PROGRAM_FILES:
        rfid = FOLDERID_ProgramFiles;
        break;
    case CSIDL_PROGRAM_FILESX86:
        rfid = FOLDERID_ProgramFilesX86;
        break;
    case CSIDL_PROGRAM_FILES_COMMON:
        rfid = FOLDERID_ProgramFilesCommon;
        break;
    case CSIDL_PROGRAM_FILES_COMMONX86:
        rfid = FOLDERID_ProgramFilesCommonX86;
        break;
    case CSIDL_PROGRAMS:
        rfid = FOLDERID_Programs;
        break;
    case CSIDL_RECENT:
        rfid = FOLDERID_Recent;
        break;
    case CSIDL_RESOURCES:
        rfid = FOLDERID_ResourceDir;
        break;
    case CSIDL_RESOURCES_LOCALIZED:
        rfid = FOLDERID_LocalizedResourcesDir;
        break;
    case CSIDL_SENDTO:
        rfid = FOLDERID_SendTo;
        break;
    case CSIDL_STARTMENU:
        rfid = FOLDERID_StartMenu;
        break;
    case CSIDL_STARTUP:
    case CSIDL_ALTSTARTUP:
        rfid = FOLDERID_Startup;
        break;
    case CSIDL_SYSTEM:
        rfid = FOLDERID_System;
        break;
    case CSIDL_SYSTEMX86:
        rfid = FOLDERID_SystemX86;
        break;
    case CSIDL_TEMPLATES:
        rfid = FOLDERID_Templates;
        break;
    case CSIDL_WINDOWS:
        rfid = FOLDERID_Windows;
        break;
    default:
        ShelExitWithRootFailure(hr, E_INVALIDARG, "Unknown csidl: %d", csidlFolder);
    }

    hr = ShelGetKnownFolder(&sczPath, rfid);
    if (E_NOTIMPL == hr)
    {
        hr = S_FALSE;
    }
    ShelExitOnFailure(hr, "Failed to get known folder.");

    if (S_FALSE == hr)
    {
        hr = GetFolderFromCsidl(&sczPath, csidlFolder);
        ShelExitOnFailure(hr, "Failed to get csidl folder.");
    }

    *psczFolderPath = sczPath;
    sczPath = NULL;

LExit:
    ReleaseStr(sczPath);

    return hr;
}


// Internal functions.

static HRESULT GetDesktopShellView(
    __in REFIID riid,
    __out void **ppv
    )
{
    HRESULT hr = S_OK;
    IShellWindows* psw = NULL;
    HWND hwnd = NULL;
    IDispatch* pdisp = NULL;
    VARIANT vEmpty = {}; // VT_EMPTY
    IShellBrowser* psb = NULL;
    IShellFolder* psf = NULL;
    IShellView* psv = NULL;

    // use the shell view for the desktop using the shell windows automation to find the 
    // desktop web browser and then grabs its view
    // returns IShellView, IFolderView and related interfaces
    hr = ::CoCreateInstance(CLSID_ShellWindows, NULL, CLSCTX_LOCAL_SERVER, IID_PPV_ARGS(&psw));
    ShelExitOnFailure(hr, "Failed to get shell view.");

    hr = psw->FindWindowSW(&vEmpty, &vEmpty, SWC_DESKTOP, (long*)&hwnd, SWFO_NEEDDISPATCH, &pdisp);
    if (S_OK == hr)
    {
        hr = IUnknown_QueryService(pdisp, SID_STopLevelBrowser, IID_PPV_ARGS(&psb));
        ShelExitOnFailure(hr, "Failed to get desktop window.");

        hr = psb->QueryActiveShellView(&psv);
        ShelExitOnFailure(hr, "Failed to get active shell view.");

        hr = psv->QueryInterface(riid, ppv);
        ShelExitOnFailure(hr, "Failed to query for the desktop shell view.");
    }
    else if (S_FALSE == hr)
    {
        //Windows XP
        hr = ::SHGetDesktopFolder(&psf);
        ShelExitOnFailure(hr, "Failed to get desktop folder.");

        hr = psf->CreateViewObject(NULL, IID_IShellView, ppv);
        ShelExitOnFailure(hr, "Failed to query for the desktop shell view.");
    }
    else
    {
        ShelExitOnFailure(hr, "Failed to get desktop window.");
    }

LExit:
    ReleaseObject(psv);
    ReleaseObject(psb);
    ReleaseObject(psf);
    ReleaseObject(pdisp);
    ReleaseObject(psw);

    return hr;
}

static HRESULT GetShellDispatchFromView(
    __in IShellView *psv,
    __in REFIID riid,
    __out void **ppv
    )
{
    HRESULT hr = S_OK;
    IDispatch *pdispBackground = NULL;
    IShellFolderViewDual *psfvd = NULL;
    IDispatch *pdisp = NULL;

    // From a shell view object, gets its automation interface and from that get the shell
    // application object that implements IShellDispatch2 and related interfaces.
    hr = psv->GetItemObject(SVGIO_BACKGROUND, IID_PPV_ARGS(&pdispBackground));
    ShelExitOnFailure(hr, "Failed to get the automation interface for shell.");

    hr = pdispBackground->QueryInterface(IID_PPV_ARGS(&psfvd));
    ShelExitOnFailure(hr, "Failed to get shell folder view dual.");

    hr = psfvd->get_Application(&pdisp);
    ShelExitOnFailure(hr, "Failed to application object.");

    hr = pdisp->QueryInterface(riid, ppv);
    ShelExitOnFailure(hr, "Failed to get IShellDispatch2.");

LExit:
    ReleaseObject(pdisp);
    ReleaseObject(psfvd);
    ReleaseObject(pdispBackground);

    return hr;
}
