// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define ThmExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_THMUTIL, x, s, __VA_ARGS__)
#define ThmExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_THMUTIL, x, s, __VA_ARGS__)
#define ThmExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_THMUTIL, x, s, __VA_ARGS__)
#define ThmExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_THMUTIL, x, s, __VA_ARGS__)
#define ThmExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_THMUTIL, x, s, __VA_ARGS__)
#define ThmExitWithRootFailure(x, e, s, ...) ExitWithRootFailureSource(DUTIL_SOURCE_THMUTIL, x, e, s, __VA_ARGS__)
#define ThmExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_THMUTIL, x, s, __VA_ARGS__)
#define ThmExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_THMUTIL, p, x, e, s, __VA_ARGS__)
#define ThmExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_THMUTIL, p, x, s, __VA_ARGS__)
#define ThmExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_THMUTIL, p, x, e, s, __VA_ARGS__)
#define ThmExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_THMUTIL, p, x, s, __VA_ARGS__)
#define ThmExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_THMUTIL, e, x, s, __VA_ARGS__)
#define ThmExitOnOptionalXmlQueryFailure(x, b, s, ...) ExitOnOptionalXmlQueryFailureSource(DUTIL_SOURCE_THMUTIL, x, b, s, __VA_ARGS__)
#define ThmExitOnRequiredXmlQueryFailure(x, s, ...) ExitOnRequiredXmlQueryFailureSource(DUTIL_SOURCE_THMUTIL, x, s, __VA_ARGS__)
#define ThmExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_THMUTIL, g, x, s, __VA_ARGS__)

#define ThmExitOnUnexpectedAttribute(x, n, e, a) { x = ParseUnexpectedAttribute(n, e, a); if (FAILED(x)) { ExitFunction(); } }

// from CommCtrl.h
#ifndef BS_COMMANDLINK
#define BS_COMMANDLINK          0x0000000EL
#endif

#ifndef BCM_SETNOTE
#define BCM_SETNOTE         (BCM_FIRST + 0x0009)
#endif

#ifndef BCM_SETSHIELD
#define BCM_SETSHIELD       (BCM_FIRST + 0x000C)
#endif

#ifndef LWS_NOPREFIX
#define LWS_NOPREFIX        0x0004
#endif

const WORD THEME_FIRST_AUTO_ASSIGN_CONTROL_ID = 100;
const DWORD THEME_INVALID_ID = 0xFFFFFFFF;
const COLORREF THEME_INVISIBLE_COLORREF = 0xFFFFFFFF;
const DWORD GROW_FONT_INSTANCES = 3;
const DWORD GROW_IMAGE_INSTANCES = 5;

const LPCWSTR ALL_CONTROL_NAMES = L"Billboard|Button|Checkbox|Combobox|CommandLink|Editbox|Hyperlink|Hypertext|ImageControl|Label|ListView|Panel|Progressbar|Richedit|Static|Tabs|TreeView";
const LPCWSTR PANEL_CHILD_CONTROL_NAMES = L"Hyperlink|Hypertext|ImageControl|Label|Progressbar|Static";

static Gdiplus::GdiplusStartupInput vgsi;
static Gdiplus::GdiplusStartupOutput vgso = { };
static ULONG_PTR vgdiToken = 0;
static ULONG_PTR vgdiHookToken = 0;
static HMODULE vhHyperlinkRegisteredModule = NULL;
static HMODULE vhPanelRegisteredModule = NULL;
static HMODULE vhStaticOwnerDrawRegisteredModule = NULL;
static WNDPROC vpfnStaticOwnerDrawBaseWndProc = NULL;
static HMODULE vhModuleMsftEdit = NULL;
static HMODULE vhModuleRichEd = NULL;
static HCURSOR vhCursorHand = NULL;
static LPWSTR vsczHyperlinkClass = NULL;
static LPWSTR vsczPanelClass = NULL;
static LPWSTR vsczStaticOwnerDrawClass = NULL;

enum INTERNAL_CONTROL_STYLE
{
    INTERNAL_CONTROL_STYLE_HIDE_WHEN_DISABLED = 0x0001,
    INTERNAL_CONTROL_STYLE_FILESYSTEM_AUTOCOMPLETE = 0x0002,
    INTERNAL_CONTROL_STYLE_DISABLED = 0x0004,
    INTERNAL_CONTROL_STYLE_HIDDEN = 0x0008,
    INTERNAL_CONTROL_STYLE_OWNER_DRAW = 0x0010,
};


// prototypes
/********************************************************************
 ThemeHoverControl - mark a control as hover.

*******************************************************************/
static BOOL ThemeHoverControl(
    __in THEME* pTheme,
    __in HWND hwndParent,
    __in HWND hwndControl
    );

/********************************************************************
 ThemeSetControlColor - sets the color of text for a control.

*******************************************************************/
static BOOL ThemeSetControlColor(
    __in THEME* pTheme,
    __in HDC hdc,
    __in HWND hWnd,
    __out HBRUSH* phBackgroundBrush
    );

static HRESULT RegisterWindowClasses(
    __in_opt HMODULE hModule
    );
static HRESULT ParseTheme(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMDocument* pixd,
    __out THEME** ppTheme
    );
static HRESULT AddStandaloneImage(
    __in THEME* pTheme,
    __in Gdiplus::Bitmap** ppBitmap,
    __out DWORD* pdwIndex
    );
static HRESULT GetAttributeImageFileOrResource(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __out Gdiplus::Bitmap** ppBitmap
    );
static HRESULT ParseOwnerDrawImage(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in THEME* pTheme,
    __in IXMLDOMNode* pElement,
    __in_z LPCWSTR wzElementName,
    __in THEME_CONTROL* pControl,
    __in THEME_IMAGE_REFERENCE* pImageRef
    );
static HRESULT ParseButtonImages(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in THEME* pTheme,
    __in IXMLDOMNode* pElement,
    __in THEME_CONTROL* pControl
    );
static HRESULT ParseCommandLinkImage(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME_CONTROL* pControl
    );
static HRESULT ParseProgressBarImages(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in THEME* pTheme,
    __in IXMLDOMNode* pElement,
    __in THEME_CONTROL* pControl
    );
static HRESULT GetAttributeCoordinateOrDimension(
    __in IXMLDOMNode* pixn,
    __in LPCWSTR wzAttribute,
    __inout int* pnValue
    );
static HRESULT GetAttributeFontId(
    __in THEME* pTheme,
    __in IXMLDOMNode* pixn,
    __in LPCWSTR wzAttribute,
    __inout DWORD* pdwValue
    );
static HRESULT GetAttributeImageId(
    __in THEME* pTheme,
    __in IXMLDOMNode* pixn,
    __in LPCWSTR wzAttribute,
    __inout DWORD* pdwValue
    );
static HRESULT ParseSourceXY(
    __in IXMLDOMNode* pixn,
    __in THEME* pTheme,
    __in int nWidth,
    __in int nHeight,
    __inout THEME_IMAGE_REFERENCE* pReference
    );
static HRESULT ParseWindow(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    );
static HRESULT GetFontColor(
    __in IXMLDOMNode* pixn,
    __in_z LPCWSTR wzAttributeName,
    __out COLORREF* pColorRef,
    __out DWORD* pdwSystemColor
    );
static HRESULT ParseFonts(
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    );
static HRESULT ParseImages(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    );
static HRESULT ParsePages(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme
    );
static HRESULT ParseImageLists(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme
    );
static HRESULT ParseControls(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme,
    __in_opt THEME_CONTROL* pParentControl,
    __in_opt THEME_PAGE* pPage,
    __in_opt LPCWSTR wzControlNames
);
static HRESULT ParseControl(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pixn,
    __in_z LPCWSTR wzElementName,
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl,
    __in_opt THEME_PAGE* pPage
    );
static void InitializeThemeControl(
    THEME* pTheme,
    THEME_CONTROL* pControl
    );
static HRESULT ParseActions(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    );
static HRESULT ParseBillboardPanels(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme,
    __in THEME_CONTROL* pParentControl,
    __in_opt THEME_PAGE* pPage
    );
static HRESULT ParseColumns(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    );
static HRESULT ParseRadioButtons(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pixn,
    __in THEME* pTheme,
    __in_opt THEME_CONTROL* pParentControl,
    __in THEME_PAGE* pPage
    );
static HRESULT ParseTabs(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    );
static HRESULT ParseText(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl,
    __inout BOOL* pfAnyChildren
);
static HRESULT ParseTooltips(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl,
    __inout BOOL* pfAnyChildren
    );
static HRESULT ParseUnexpectedAttribute(
    __in IXMLDOMNode* pixn,
    __in_z LPCWSTR wzElementName,
    __in_z LPCWSTR wzAttribute
    );
static HRESULT ParseNotes(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl,
    __out BOOL* pfAnyChildren
    );
static HRESULT StopBillboard(
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl
    );
static HRESULT StartBillboard(
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl
    );
static HRESULT EnsureFontInstance(
    __in THEME* pTheme,
    __in THEME_FONT* pFont,
    __out THEME_FONT_INSTANCE** ppFontInstance
    );
static HRESULT FindImageList(
    __in THEME* pTheme,
    __in_z LPCWSTR wzImageListName,
    __out HIMAGELIST *phImageList
    );
static HRESULT LoadThemeControls(
    __in THEME* pTheme
    );
static void UnloadThemeControls(
    __in THEME* pTheme
    );
static HRESULT OnLoadingControl(
    __in THEME* pTheme,
    __in const THEME_CONTROL* pControl,
    __inout WORD* pwId,
    __inout DWORD* pdwAutomaticBehaviorType
    );
static HRESULT LoadControls(
    __in THEME* pTheme,
    __in_opt THEME_CONTROL* pParentControl
    );
static HRESULT ShowControl(
    __in THEME_CONTROL* pControl,
    __in int nCmdShow,
    __in BOOL fSaveEditboxes,
    __in THEME_SHOW_PAGE_REASON reason,
    __in DWORD dwPageId,
    __inout_opt HWND* phwndFocus
    );
static HRESULT ShowControls(
    __in THEME* pTheme,
    __in_opt const THEME_CONTROL* pParentControl,
    __in int nCmdShow,
    __in BOOL fSaveEditboxes,
    __in BOOL fSetFocus,
    __in THEME_SHOW_PAGE_REASON reason,
    __in DWORD dwPageId
    );
static HRESULT DrawButton(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    );
static void DrawControlText(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl,
    __in BOOL fCentered,
    __in BOOL fDrawFocusRect
    );
static HRESULT DrawHyperlink(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    );
static HRESULT DrawImage(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    );
static void GetImageInstance(
    __in THEME* pTheme,
    __in const THEME_IMAGE_REFERENCE* pReference,
    __out const THEME_IMAGE_INSTANCE** ppInstance
    );
static HRESULT DrawImageReference(
    __in THEME* pTheme,
    __in const THEME_IMAGE_REFERENCE* pReference,
    __in HDC hdc,
    __in int destX,
    __in int destY,
    __in int destWidth,
    __in int destHeight
    );
static HRESULT DrawGdipBitmap(
    __in HDC hdc,
    __in int destX,
    __in int destY,
    __in int destWidth,
    __in int destHeight,
    __in Gdiplus::Bitmap* pBitmap,
    __in int srcX,
    __in int srcY,
    __in int srcWidth,
    __in int srcHeight
    );
static HRESULT DrawProgressBar(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    );
static HRESULT DrawProgressBarImage(
    __in THEME* pTheme,
    __in const THEME_IMAGE_INSTANCE* pImageInstance,
    __in int srcX,
    __in int srcY,
    __in int srcWidth,
    __in int srcHeight,
    __in HDC hdc,
    __in int destX,
    __in int destY,
    __in int destWidth,
    __in int destHeight
    );
static BOOL DrawHoverControl(
    __in THEME* pTheme,
    __in BOOL fHover
    );
static void FreeFontInstance(
    __in THEME_FONT_INSTANCE* pFontInstance
    );
static void FreeFont(
    __in THEME_FONT* pFont
    );
static void FreeImage(
    __in THEME_IMAGE* pImage
    );
static void FreeImageInstance(
    __in THEME_IMAGE_INSTANCE* pImageInstance
    );
static void FreePage(
    __in THEME_PAGE* pPage
    );
static void FreeControl(
    __in THEME_CONTROL* pControl
    );
static void FreeConditionalText(
    __in THEME_CONDITIONAL_TEXT* pConditionalText
    );
static void FreeImageList(
    __in THEME_IMAGELIST* pImageList
    );
static void FreeAction(
    __in THEME_ACTION* pAction
    );
static void FreeColumn(
    __in THEME_COLUMN* pColumn
    );
static void FreeTab(
    __in THEME_TAB* pTab
    );
static void CALLBACK OnBillboardTimer(
    __in THEME* pTheme,
    __in HWND hwnd,
    __in UINT_PTR idEvent
    );
static void OnBrowseDirectory(
    __in THEME* pTheme,
    __in const THEME_ACTION* pAction
    );
static BOOL OnButtonClicked(
    __in THEME* pTheme,
    __in const THEME_CONTROL* pControl
    );
static BOOL OnDpiChanged(
    __in THEME* pTheme,
    __in WPARAM wParam,
    __in LPARAM lParam
    );
static BOOL OnHypertextClicked(
    __in THEME* pTheme,
    __in const THEME_CONTROL* pThemeControl,
    __in PNMLINK pnmlink
    );
static void OnNcCreate(
    __in THEME* pTheme,
    __in HWND hWnd,
    __in LPARAM lParam
    );
static BOOL OnNotifyEnLink(
    __in THEME* pTheme,
    __in const THEME_CONTROL* pThemeControl,
    __in ENLINK* link
    );
static BOOL OnNotifyEnMsgFilter(
    __in THEME* pTheme,
    __in const THEME_CONTROL* pThemeControl,
    __in MSGFILTER* msgFilter
    );
static BOOL OnPanelCreate(
    __in THEME_CONTROL* pControl,
    __in HWND hWnd
    );
static BOOL OnWmCommand(
    __in THEME* pTheme,
    __in WPARAM wParam,
    __in const THEME_CONTROL* pThemeControl,
    __inout LRESULT* plResult
    );
static BOOL OnWmNotify(
    __in THEME* pTheme,
    __in LPNMHDR lParam,
    __in const THEME_CONTROL* pThemeControl,
    __inout LRESULT* plResult
    );
static const THEME_CONTROL* FindControlFromId(
    __in const THEME* pTheme,
    __in WORD wId,
    __in_opt const THEME_CONTROL* pParentControl = NULL
    );
static const THEME_CONTROL* FindControlFromHWnd(
    __in const THEME* pTheme,
    __in HWND hWnd,
    __in_opt const THEME_CONTROL* pParentControl = NULL
    );
static void GetControlDimensions(
    __in const THEME_CONTROL* pControl,
    __in const RECT* prcParent,
    __out int* piWidth,
    __out int* piHeight,
    __out int* piX,
    __out int* piY
    );
// Using iWidth as total width of listview, base width of columns, and "Expands" flag on columns
// calculates final width of each column (storing result in each column's nWidth value)
static HRESULT SizeListViewColumns(
    __inout THEME_CONTROL* pControl
    );
static LRESULT CALLBACK ControlGroupDefWindowProc(
    __in_opt THEME* pTheme,
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam,
    __in BOOL fDialog
    );
static LRESULT CALLBACK PanelWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );
static LRESULT CALLBACK StaticOwnerDrawWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    );
static HRESULT LocalizeControls(
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls,
    __in const WIX_LOCALIZATION *pWixLoc
    );
static HRESULT LocalizeControl(
    __in THEME_CONTROL* pControl,
    __in const WIX_LOCALIZATION *pWixLoc
    );
static HRESULT LoadControlsString(
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls,
    __in HMODULE hResModule
    );
static HRESULT LoadControlString(
    __in THEME_CONTROL* pControl,
    __in HMODULE hResModule
    );
static void ResizeControls(
    __in THEME* pTheme,
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls,
    __in const RECT* prcParent
    );
static void ResizeControl(
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl,
    __in const RECT* prcParent
    );
static void ScaleThemeFromWindow(
    __in THEME* pTheme,
    __in UINT nDpi,
    __in int x,
    __in int y
    );
static void ScaleTheme(
    __in THEME* pTheme,
    __in UINT nDpi,
    __in int x,
    __in int y,
    __in DWORD dwStyle,
    __in BOOL fMenu,
    __in DWORD dwExStyle
    );
static void AdjustThemeWindowRect(
    __in THEME* pTheme,
    __in DWORD dwStyle,
    __in BOOL fMenu,
    __in DWORD dwExStyle
    );
static void ScaleControls(
    __in THEME* pTheme,
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls,
    __in UINT nDpi
    );
static void ScaleControl(
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl,
    __in UINT nDpi
    );
static void GetControls(
    __in THEME* pTheme,
    __in_opt THEME_CONTROL* pParentControl,
    __out DWORD** ppcControls,
    __out THEME_CONTROL*** pprgControls
    );
static void GetControls(
    __in const THEME* pTheme,
    __in_opt const THEME_CONTROL* pParentControl,
    __out DWORD& cControls,
    __out THEME_CONTROL*& rgControls
    );
static void ScaleImageReference(
    __in THEME* pTheme,
    __in THEME_IMAGE_REFERENCE* pImageRef,
    __in int nDestWidth,
    __in int nDestHeight
    );
static void UnloadControls(
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls
    );


// Public functions.

DAPI_(HRESULT) ThemeInitialize(
    __in_opt HMODULE hModule
    )
{
    HRESULT hr = S_OK;
    INITCOMMONCONTROLSEX icex = { };

    DpiuInitialize();

    hr = XmlInitialize();
    ThmExitOnFailure(hr, "Failed to initialize XML.");

    hr = StrAllocFormatted(&vsczHyperlinkClass, L"ThemeHyperLink_%p", hModule);
    ThmExitOnFailure(hr, "Failed to initialize hyperlink class name.");

    hr = StrAllocFormatted(&vsczPanelClass, L"ThemePanel_%p", hModule);
    ThmExitOnFailure(hr, "Failed to initialize panel class name.");

    hr = StrAllocFormatted(&vsczStaticOwnerDrawClass, L"ThemeStaticOwnerDraw_%p", hModule);
    ThmExitOnFailure(hr, "Failed to initialize static owner draw class name.");

    hr = RegisterWindowClasses(hModule);
    ThmExitOnFailure(hr, "Failed to register theme window classes.");

    // Initialize GDI+ and common controls.
    vgsi.SuppressBackgroundThread = TRUE;

    hr = GdipInitialize(&vgsi, &vgdiToken, &vgso);
    ThmExitOnFailure(hr, "Failed to initialize GDI+.");

    icex.dwSize = sizeof(INITCOMMONCONTROLSEX);
    icex.dwICC = ICC_STANDARD_CLASSES | ICC_PROGRESS_CLASS | ICC_LISTVIEW_CLASSES | ICC_TREEVIEW_CLASSES | ICC_TAB_CLASSES | ICC_LINK_CLASS;
    ::InitCommonControlsEx(&icex);

    (*vgso.NotificationHook)(&vgdiHookToken);

LExit:
    return hr;
}


DAPI_(void) ThemeUninitialize()
{
    if (vhModuleMsftEdit)
    {
        ::FreeLibrary(vhModuleMsftEdit);
        vhModuleMsftEdit = NULL;
    }

    if (vhModuleRichEd)
    {
        ::FreeLibrary(vhModuleRichEd);
        vhModuleRichEd = NULL;
    }

    if (vhHyperlinkRegisteredModule)
    {
        ::UnregisterClassW(vsczHyperlinkClass, vhHyperlinkRegisteredModule);
        vhHyperlinkRegisteredModule = NULL;
    }

    ReleaseStr(vsczHyperlinkClass);

    if (vhPanelRegisteredModule)
    {
        ::UnregisterClassW(vsczPanelClass, vhPanelRegisteredModule);
        vhPanelRegisteredModule = NULL;
    }

    ReleaseStr(vsczPanelClass);

    if (vhStaticOwnerDrawRegisteredModule)
    {
        ::UnregisterClassW(vsczStaticOwnerDrawClass, vhStaticOwnerDrawRegisteredModule);
        vhStaticOwnerDrawRegisteredModule = NULL;
        vpfnStaticOwnerDrawBaseWndProc = NULL;
    }

    ReleaseStr(vsczStaticOwnerDrawClass);

    if (vgdiToken)
    {
        GdipUninitialize(vgdiToken);
        vgdiToken = 0;
    }

    XmlUninitialize();
    DpiuUninitialize();
}


DAPI_(HRESULT) ThemeLoadFromFile(
    __in_z LPCWSTR wzThemeFile,
    __out THEME** ppTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMDocument* pixd = NULL;
    LPWSTR sczRelativePath = NULL;

    hr = XmlLoadDocumentFromFile(wzThemeFile, &pixd);
    ThmExitOnFailure(hr, "Failed to load theme resource as XML document.");

    hr = PathGetDirectory(wzThemeFile, &sczRelativePath);
    ThmExitOnFailure(hr, "Failed to get relative path from theme file.");

    hr = ParseTheme(NULL, sczRelativePath, pixd, ppTheme);
    ThmExitOnFailure(hr, "Failed to parse theme.");

LExit:
    ReleaseStr(sczRelativePath);
    ReleaseObject(pixd);

    return hr;
}


DAPI_(HRESULT) ThemeLoadFromResource(
    __in_opt HMODULE hModule,
    __in_z LPCSTR szResource,
    __out THEME** ppTheme
    )
{
    HRESULT hr = S_OK;
    LPVOID pvResource = NULL;
    DWORD cbResource = 0;
    LPWSTR sczXml = NULL;
    IXMLDOMDocument* pixd = NULL;

    hr = ResReadData(hModule, szResource, &pvResource, &cbResource);
    ThmExitOnFailure(hr, "Failed to read theme from resource.");

    hr = StrAllocStringAnsi(&sczXml, reinterpret_cast<LPCSTR>(pvResource), cbResource, CP_UTF8);
    ThmExitOnFailure(hr, "Failed to convert XML document data from UTF-8 to unicode string.");

    hr = XmlLoadDocument(sczXml, &pixd);
    ThmExitOnFailure(hr, "Failed to load theme resource as XML document.");

    hr = ParseTheme(hModule, NULL, pixd, ppTheme);
    ThmExitOnFailure(hr, "Failed to parse theme.");

LExit:
    ReleaseObject(pixd);
    ReleaseStr(sczXml);

    return hr;
}


DAPI_(void) ThemeFree(
    __in THEME* pTheme
    )
{
    if (pTheme)
    {
        for (DWORD i = 0; i < pTheme->cFonts; ++i)
        {
            FreeFont(pTheme->rgFonts + i);
        }

        for (DWORD i = 0; i < pTheme->cImages; ++i)
        {
            FreeImage(pTheme->rgImages + i);
        }

        for (DWORD i = 0; i < pTheme->cStandaloneImages; ++i)
        {
            FreeImageInstance(pTheme->rgStandaloneImages + i);
        }

        for (DWORD i = 0; i < pTheme->cPages; ++i)
        {
            FreePage(pTheme->rgPages + i);
        }

        for (DWORD i = 0; i < pTheme->cImageLists; ++i)
        {
            FreeImageList(pTheme->rgImageLists + i);
        }

        for (DWORD i = 0; i < pTheme->cControls; ++i)
        {
            FreeControl(pTheme->rgControls + i);
        }

        ReleaseMem(pTheme->rgControls);
        ReleaseMem(pTheme->rgPages);
        ReleaseMem(pTheme->rgStandaloneImages);
        ReleaseMem(pTheme->rgImages);
        ReleaseMem(pTheme->rgFonts);

        ReleaseStr(pTheme->sczCaption);
        ReleaseDict(pTheme->sdhFontDictionary);
        ReleaseDict(pTheme->sdhImageDictionary);
        ReleaseMem(pTheme);
    }
}

DAPI_(HRESULT) ThemeRegisterVariableCallbacks(
    __in THEME* pTheme,
    __in_opt PFNTHM_EVALUATE_VARIABLE_CONDITION pfnEvaluateCondition,
    __in_opt PFNTHM_FORMAT_VARIABLE_STRING pfnFormatString,
    __in_opt PFNTHM_GET_VARIABLE_NUMERIC pfnGetNumericVariable,
    __in_opt PFNTHM_SET_VARIABLE_NUMERIC pfnSetNumericVariable,
    __in_opt PFNTHM_GET_VARIABLE_STRING pfnGetStringVariable,
    __in_opt PFNTHM_SET_VARIABLE_STRING pfnSetStringVariable,
    __in_opt LPVOID pvContext
    )
{
    HRESULT hr = S_OK;
    ThmExitOnNull(pTheme, hr, S_FALSE, "Theme must be loaded first.");

    pTheme->pfnEvaluateCondition = pfnEvaluateCondition;
    pTheme->pfnFormatString = pfnFormatString;
    pTheme->pfnGetNumericVariable = pfnGetNumericVariable;
    pTheme->pfnSetNumericVariable = pfnSetNumericVariable;
    pTheme->pfnGetStringVariable = pfnGetStringVariable;
    pTheme->pfnSetStringVariable = pfnSetStringVariable;
    pTheme->pvVariableContext = pvContext;

LExit:
    return hr;
}


DAPI_(void) ThemeInitializeWindowClass(
    __in THEME* pTheme,
    __in WNDCLASSW* pWndClass,
    __in WNDPROC pfnWndProc,
    __in HINSTANCE hInstance,
    __in LPCWSTR wzClassName
    )
{
    pWndClass->style = CS_HREDRAW | CS_VREDRAW;
    pWndClass->cbWndExtra = DLGWINDOWEXTRA;
    pWndClass->hCursor = ::LoadCursorW(NULL, (LPCWSTR)IDC_ARROW);

    pWndClass->lpfnWndProc = pfnWndProc;
    pWndClass->hInstance = hInstance;
    pWndClass->lpszClassName = wzClassName;

    pWndClass->hIcon = reinterpret_cast<HICON>(pTheme->hIcon);
    pWndClass->hbrBackground = pTheme->rgFonts[pTheme->dwFontId].hBackground;
}


DAPI_(HRESULT) ThemeCreateParentWindow(
    __in THEME* pTheme,
    __in DWORD dwExStyle,
    __in LPCWSTR szClassName,
    __in LPCWSTR szWindowName,
    __in DWORD dwStyle,
    __in int x,
    __in int y,
    __in_opt HWND hwndParent,
    __in_opt HINSTANCE hInstance,
    __in_opt LPVOID lpParam,
    __in THEME_WINDOW_INITIAL_POSITION initialPosition,
    __out_opt HWND* phWnd
    )
{
    HRESULT hr = S_OK;
    DPIU_MONITOR_CONTEXT* pMonitorContext = NULL;
    POINT pt = { };
    RECT* pMonitorRect = NULL;
    HMENU hMenu = NULL;
    HWND hWnd = NULL;
    BOOL fScaledTheme = FALSE;

    if (pTheme->hwndParent)
    {
        ThmExitOnFailure(hr = E_INVALIDSTATE, "ThemeCreateParentWindow called after the theme was loaded.");
    }

    if (THEME_WINDOW_INITIAL_POSITION_CENTER_MONITOR_FROM_COORDINATES == initialPosition)
    {
        pt.x = x;
        pt.y = y;
        hr = DpiuGetMonitorContextFromPoint(&pt, &pMonitorContext);
        if (SUCCEEDED(hr))
        {
            pMonitorRect = &pMonitorContext->mi.rcWork;
            if (pMonitorContext->nDpi != pTheme->nDpi)
            {
                ScaleTheme(pTheme, pMonitorContext->nDpi, pMonitorRect->left, pMonitorRect->top, dwStyle, NULL != hMenu, dwExStyle);
                fScaledTheme = TRUE;
            }

            x = pMonitorRect->left + (pMonitorRect->right - pMonitorRect->left - pTheme->nWindowWidth) / 2;
            y = pMonitorRect->top + (pMonitorRect->bottom - pMonitorRect->top - pTheme->nWindowHeight) / 2;
        }
        else
        {
            hr = S_OK;
            x = CW_USEDEFAULT;
            y = CW_USEDEFAULT;
        }
    }

    // Make sure the client area matches the specified width and height.
    if (!fScaledTheme)
    {
        AdjustThemeWindowRect(pTheme, dwStyle, NULL != hMenu, dwExStyle);
    }

    hWnd = ::CreateWindowExW(dwExStyle, szClassName, szWindowName, dwStyle, x, y, pTheme->nWindowWidth, pTheme->nWindowHeight, hwndParent, hMenu, hInstance, lpParam);
    ThmExitOnNullWithLastError(hWnd, hr, "Failed to create theme parent window.");
    ThmExitOnNull(pTheme->hwndParent, hr, E_INVALIDSTATE, "Theme parent window is not set, make sure ThemeDefWindowProc is called for WM_NCCREATE.");
    AssertSz(hWnd == pTheme->hwndParent, "Theme parent window does not equal newly created window.");

    if (phWnd)
    {
        *phWnd = hWnd;
    }

LExit:
    ReleaseMem(pMonitorContext);

    return hr;
}

DAPI_(HRESULT) ThemeLocalize(
    __in THEME *pTheme,
    __in const WIX_LOCALIZATION *pWixLoc
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczCaption = NULL;

    hr = LocLocalizeString(pWixLoc, &pTheme->sczCaption);
    ThmExitOnFailure(hr, "Failed to localize theme caption.");

    if (pTheme->pfnFormatString)
    {
        hr = pTheme->pfnFormatString(pTheme->sczCaption, &sczCaption, pTheme->pvVariableContext);
        if (SUCCEEDED(hr))
        {
            hr = ThemeUpdateCaption(pTheme, sczCaption);
        }
    }

    hr = LocalizeControls(pTheme->cControls, pTheme->rgControls, pWixLoc);

LExit:
    ReleaseStr(sczCaption);
    
    return hr;
}

/********************************************************************
 ThemeLoadStrings - Loads string resources.
 Must be called after loading a theme and before calling
 ThemeLoadControls.
*******************************************************************/
DAPI_(HRESULT) ThemeLoadStrings(
    __in THEME* pTheme,
    __in HMODULE hResModule
    )
{
    HRESULT hr = S_OK;
    ThmExitOnNull(pTheme, hr, S_FALSE, "Theme must be loaded first.");

    if (UINT_MAX != pTheme->uStringId)
    {
        hr = ResReadString(hResModule, pTheme->uStringId, &pTheme->sczCaption);
        ThmExitOnFailure(hr, "Failed to load theme caption.");
    }

    hr = LoadControlsString(pTheme->cControls, pTheme->rgControls, hResModule);

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeLoadRichEditFromFile(
    __in const THEME_CONTROL* pThemeControl,
    __in_z LPCWSTR wzFileName,
    __in HMODULE hModule
    )
{
    HRESULT hr = E_INVALIDARG;

    if (pThemeControl)
    {
        AssertSz(THEME_CONTROL_TYPE_RICHEDIT == pThemeControl->type, "ThemeLoadRichEditFromFile called for non-RichEdit control.");

        hr = WnduLoadRichEditFromFile(pThemeControl->hWnd, wzFileName, hModule);
    }

    return hr;
}


DAPI_(HRESULT) ThemeLoadRichEditFromResource(
    __in const THEME_CONTROL* pThemeControl,
    __in_z LPCSTR szResourceName,
    __in HMODULE hModule
    )
{
    HRESULT hr = E_INVALIDARG;

    if (pThemeControl)
    {
        AssertSz(THEME_CONTROL_TYPE_RICHEDIT == pThemeControl->type, "ThemeLoadRichEditFromResource called for non-RichEdit control.");

        hr = WnduLoadRichEditFromResource(pThemeControl->hWnd, szResourceName, hModule);
    }

    return hr;
}


DAPI_(BOOL) ThemeHandleKeyboardMessage(
    __in_opt THEME* pTheme,
    __in HWND /*hWnd*/,
    __in MSG* pMsg
    )
{
    return pTheme ? ::IsDialogMessageW(pTheme->hwndParent, pMsg) : FALSE;
}


extern "C" LRESULT CALLBACK ThemeDefWindowProc(
    __in_opt THEME* pTheme,
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    RECT rcParent = { };
    RECT *pRect = NULL;

    if (pTheme)
    {
        switch (uMsg)
        {
        case WM_NCCREATE:
            if (pTheme->hwndParent)
            {
                AssertSz(FALSE, "WM_NCCREATE called multiple times");
            }
            else
            {
                OnNcCreate(pTheme, hWnd, lParam);
            }
            break;

        case WM_CREATE:
            if (FAILED(LoadThemeControls(pTheme)))
            {
                return -1;
            }
            break;

        case WM_DESTROY:
            UnloadThemeControls(pTheme);
            break;

        case WM_NCHITTEST:
            if (pTheme->dwStyle & WS_POPUP)
            {
                return HTCAPTION; // allow pop-up windows to be moved by grabbing any non-control.
            }
            break;

        case WM_DPICHANGED:
            if (OnDpiChanged(pTheme, wParam, lParam))
            {
                return 0;
            }
            break;

        case WM_SIZING:
            if (pTheme->fAutoResize)
            {
                pRect = reinterpret_cast<RECT *>(lParam);
                if (pRect->right - pRect->left < pTheme->nMinimumWidth)
                {
                    if (wParam == WMSZ_BOTTOMLEFT || wParam == WMSZ_LEFT || wParam == WMSZ_TOPLEFT)
                    {
                        pRect->left = pRect->right - pTheme->nMinimumWidth;
                    }
                    else
                    {
                        pRect->right = pRect->left + pTheme->nMinimumWidth;
                    }
                }
                if (pRect->bottom - pRect->top < pTheme->nMinimumHeight)
                {
                    if (wParam == WMSZ_BOTTOM || wParam == WMSZ_BOTTOMLEFT || wParam == WMSZ_BOTTOMRIGHT)
                    {
                        pRect->bottom = pRect->top + pTheme->nMinimumHeight;
                    }
                    else
                    {
                        pRect->top = pRect->bottom - pTheme->nMinimumHeight;
                    }
                }

                return TRUE;
            }
            break;

        case WM_SIZE:
            if (pTheme->fAutoResize || pTheme->fForceResize)
            {
                pTheme->fForceResize = FALSE;
                ::GetClientRect(pTheme->hwndParent, &rcParent);
                ScaleImageReference(pTheme, &pTheme->windowImageRef, rcParent.right - rcParent.left, rcParent.bottom - rcParent.top);
                ResizeControls(pTheme, pTheme->cControls, pTheme->rgControls, &rcParent);
                return 0;
            }
            break;
        }
    }

    return ControlGroupDefWindowProc(pTheme, hWnd, uMsg, wParam, lParam, TRUE);
}


DAPI_(void) ThemeGetPageIds(
    __in const THEME* pTheme,
    __in_ecount(cGetPages) LPCWSTR* rgwzFindNames,
    __inout_ecount(cGetPages) DWORD* rgdwPageIds,
    __in DWORD cGetPages
    )
{
    for (DWORD i = 0; i < cGetPages; ++i)
    {
        LPCWSTR wzFindName = rgwzFindNames[i];
        for (DWORD j = 0; j < pTheme->cPages; ++j)
        {
            LPCWSTR wzPageName = pTheme->rgPages[j].sczName;
            if (wzPageName && CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, wzPageName, -1, wzFindName, -1))
            {
                rgdwPageIds[i] = j + 1; // add one to make the page ids 1-based (so zero is invalid).
                break;
            }
        }
    }
}


DAPI_(THEME_PAGE*) ThemeGetPage(
    __in const THEME* pTheme,
    __in DWORD dwPage
    )
{
    DWORD iPage = dwPage - 1;
    THEME_PAGE* pPage = NULL;

    if (iPage < pTheme->cPages)
    {
        pPage = pTheme->rgPages + iPage;
    }

    return pPage;
}


DAPI_(HRESULT) ThemeShowPage(
    __in THEME* pTheme,
    __in DWORD dwPage,
    __in int nCmdShow
    )
{
    return ThemeShowPageEx(pTheme, dwPage, nCmdShow, THEME_SHOW_PAGE_REASON_DEFAULT);
}


DAPI_(HRESULT) ThemeShowPageEx(
    __in THEME* pTheme,
    __in DWORD dwPage,
    __in int nCmdShow,
    __in THEME_SHOW_PAGE_REASON reason
    )
{
    HRESULT hr = S_OK;
    BOOL fHide = SW_HIDE == nCmdShow;
    BOOL fSaveEditboxes = FALSE;
    THEME_SAVEDVARIABLE* pSavedVariable = NULL;
    SIZE_T cb = 0;
    BOOL fSetFocus = dwPage != pTheme->dwCurrentPageId;
    THEME_PAGE* pPage = ThemeGetPage(pTheme, dwPage);

    if (pPage)
    {
        if (fHide)
        {
            switch (reason)
            {
            case THEME_SHOW_PAGE_REASON_DEFAULT:
                // Set the variables in the loop below.
                fSaveEditboxes = TRUE;
                break;
            case THEME_SHOW_PAGE_REASON_CANCEL:
                if (pPage->cSavedVariables && pTheme->pfnSetStringVariable)
                {
                    // Best effort to cancel any changes to the variables.
                    for (DWORD v = 0; v < pPage->cSavedVariables; ++v)
                    {
                        pSavedVariable = pPage->rgSavedVariables + v;
                        if (pSavedVariable->wzName)
                        {
                            pTheme->pfnSetStringVariable(pSavedVariable->wzName, pSavedVariable->sczValue, FALSE, pTheme->pvVariableContext);
                        }
                    }
                }
                break;
            }

            if (THEME_SHOW_PAGE_REASON_REFRESH != reason)
            {
                pPage->cSavedVariables = 0;
                if (pPage->rgSavedVariables && SUCCEEDED(MemSizeChecked(pPage->rgSavedVariables, &cb)))
                {
                    SecureZeroMemory(pPage->rgSavedVariables, cb);
                }
            }

            pTheme->dwCurrentPageId = 0;
        }
        else
        {
            if (THEME_SHOW_PAGE_REASON_REFRESH == reason)
            {
                fSaveEditboxes = TRUE;
            }
            else
            {
                hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pPage->rgSavedVariables), pPage->cControlIndices, sizeof(THEME_SAVEDVARIABLE), pPage->cControlIndices);
                ThmExitOnFailure(hr, "Failed to allocate memory for saved variables.");

                if (SUCCEEDED(MemSizeChecked(pPage->rgSavedVariables, &cb)))
                {
                    SecureZeroMemory(pPage->rgSavedVariables, cb);
                }

                pPage->cSavedVariables = pPage->cControlIndices;

                // Save the variables in the loop below.
            }

            pTheme->dwCurrentPageId = dwPage;
        }
    }

    hr = ShowControls(pTheme, NULL, nCmdShow, fSaveEditboxes, fSetFocus, reason, dwPage);
    ThmExitOnFailure(hr, "Failed to show page controls.");

LExit:
    return hr;
}


DAPI_(BOOL) ThemeControlExistsByHWnd(
    __in const THEME* pTheme,
    __in HWND hWnd,
    __out_opt const THEME_CONTROL** ppThemeControl
    )
{
    const THEME_CONTROL* pControl = FindControlFromHWnd(pTheme, hWnd);

    if (ppThemeControl)
    {
        *ppThemeControl = pControl;
    }

    return NULL != pControl;
}


DAPI_(BOOL) ThemeControlExistsById(
    __in const THEME* pTheme,
    __in WORD wId,
    __out_opt const THEME_CONTROL** ppThemeControl
    )
{
    const THEME_CONTROL* pControl = FindControlFromId(pTheme, wId);

    if (ppThemeControl)
    {
        *ppThemeControl = pControl;
    }

    return NULL != pControl;
}


DAPI_(void) ThemeControlEnable(
    __in const THEME_CONTROL* pThemeControl,
    __in BOOL fEnable
    )
{
    if (pThemeControl)
    {
        THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(pThemeControl);
        pControl->dwInternalStyle = fEnable ? (pControl->dwInternalStyle & ~INTERNAL_CONTROL_STYLE_DISABLED) : (pControl->dwInternalStyle | INTERNAL_CONTROL_STYLE_DISABLED);
        ::EnableWindow(pControl->hWnd, fEnable);

        if (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_HIDE_WHEN_DISABLED)
        {
            ::ShowWindow(pControl->hWnd, fEnable ? SW_SHOW : SW_HIDE);
        }
    }
}


DAPI_(BOOL) ThemeControlEnabled(
    __in const THEME_CONTROL* pThemeControl
    )
{
    BOOL fEnabled = FALSE;

    if (pThemeControl)
    {
        fEnabled = !(pThemeControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_DISABLED);
    }

    return fEnabled;
}


DAPI_(void) ThemeControlElevates(
    __in const THEME_CONTROL* pThemeControl,
    __in BOOL fElevates
    )
{
    if (pThemeControl)
    {
        ::SendMessageW(pThemeControl->hWnd, BCM_SETSHIELD, 0, fElevates);
    }
}


DAPI_(void) ThemeShowControl(
    __in const THEME_CONTROL* pThemeControl,
    __in int nCmdShow
    )
{
    if (pThemeControl)
    {
        THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(pThemeControl);
        ::ShowWindow(pControl->hWnd, nCmdShow);

        // Save the control's visible state.
        pControl->dwInternalStyle = (SW_HIDE == nCmdShow) ? (pControl->dwInternalStyle | INTERNAL_CONTROL_STYLE_HIDDEN) : (pControl->dwInternalStyle & ~INTERNAL_CONTROL_STYLE_HIDDEN);
    }
}


DAPI_(void) ThemeShowControlEx(
    __in const THEME_CONTROL* pThemeControl,
    __in int nCmdShow
    )
{
    if (pThemeControl)
    {
        THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(pThemeControl);
        ShowControl(pControl, nCmdShow, THEME_CONTROL_TYPE_EDITBOX == pControl->type, THEME_SHOW_PAGE_REASON_REFRESH, 0, NULL);
    }
}


DAPI_(BOOL) ThemeControlVisible(
    __in const THEME_CONTROL* pThemeControl
    )
{
    BOOL fVisible = FALSE;

    if (pThemeControl)
    {
        fVisible = ::IsWindowVisible(pThemeControl->hWnd);
    }

    return fVisible;
}


DAPI_(HRESULT) ThemeDrawBackground(
    __in THEME* pTheme,
    __in PAINTSTRUCT* pps
    )
{
    HRESULT hr = S_FALSE;

    if (pps->fErase && THEME_IMAGE_REFERENCE_TYPE_NONE != pTheme->windowImageRef.type)
    {
        hr = DrawImageReference(pTheme, &pTheme->windowImageRef, pps->hdc, 0, 0, pTheme->nWidth, pTheme->nHeight);
    }

    return hr;
}


DAPI_(HRESULT) ThemeDrawControl(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis
    )
{
    HRESULT hr = S_OK;
    const THEME_CONTROL* pControl = NULL;
    BOOL fExists = ThemeControlExistsByHWnd(pTheme, pdis->hwndItem, &pControl);

    AssertSz(fExists, "Expected control window from owner draw window.");
    if (!fExists)
    {
        ExitFunction1(hr = E_INVALIDARG);
    }

    AssertSz(pControl->hWnd == pdis->hwndItem, "Expected control window to match owner draw window.");
    AssertSz(pControl->nWidth < 1 || pControl->nWidth == pdis->rcItem.right - pdis->rcItem.left, "Expected control window width to match owner draw window width.");
    AssertSz(pControl->nHeight < 1 || pControl->nHeight == pdis->rcItem.bottom - pdis->rcItem.top, "Expected control window height to match owner draw window height.");

    switch (pControl->type)
    {
    case THEME_CONTROL_TYPE_BUTTON:
        hr = DrawButton(pTheme, pdis, pControl);
        ThmExitOnFailure(hr, "Failed to draw button.");
        break;

    case THEME_CONTROL_TYPE_HYPERLINK:
        hr = DrawHyperlink(pTheme, pdis, pControl);
        ThmExitOnFailure(hr, "Failed to draw hyperlink.");
        break;

    case THEME_CONTROL_TYPE_IMAGE:
        hr = DrawImage(pTheme, pdis, pControl);
        ThmExitOnFailure(hr, "Failed to draw image.");
        break;

    case THEME_CONTROL_TYPE_PROGRESSBAR:
        hr = DrawProgressBar(pTheme, pdis, pControl);
        ThmExitOnFailure(hr, "Failed to draw progress bar.");
        break;

    default:
        hr = E_UNEXPECTED;
        ThmExitOnRootFailure(hr, "Did not specify an owner draw control to draw.");
    }

LExit:
    return hr;
}


static BOOL ThemeHoverControl(
    __in THEME* pTheme,
    __in HWND hwndParent,
    __in HWND hwndControl
    )
{
    BOOL fHovered = FALSE;
    if (hwndControl != pTheme->hwndHover)
    {
        if (pTheme->hwndHover && pTheme->hwndHover != hwndParent)
        {
            DrawHoverControl(pTheme, FALSE);
        }

        pTheme->hwndHover = hwndControl;

        if (pTheme->hwndHover && pTheme->hwndHover != hwndParent)
        {
            fHovered = DrawHoverControl(pTheme, TRUE);
        }
    }

    return fHovered;
}


DAPI_(BOOL) ThemeIsControlChecked(
    __in const THEME_CONTROL* pThemeControl
    )
{
    BOOL fChecked = FALSE;

    if (pThemeControl)
    {
        fChecked = BST_CHECKED == ::SendMessageW(pThemeControl->hWnd, BM_GETCHECK, 0, 0);
    }

    return fChecked;
}


static BOOL ThemeSetControlColor(
    __in THEME* pTheme,
    __in HDC hdc,
    __in HWND hWnd,
    __out HBRUSH* phBackgroundBrush
    )
{
    THEME_FONT* pFont = NULL;
    BOOL fHasBackground = FALSE;
    const THEME_CONTROL* pControl = NULL;

    *phBackgroundBrush = NULL;

    if (hWnd == pTheme->hwndParent)
    {
        pFont = (THEME_INVALID_ID == pTheme->dwFontId) ? NULL : pTheme->rgFonts + pTheme->dwFontId;
    }
    else if (ThemeControlExistsByHWnd(pTheme, hWnd, &pControl))
    {
        pFont = THEME_INVALID_ID == pControl->dwFontId ? NULL : pTheme->rgFonts + pControl->dwFontId;
    }

    if (pFont)
    {
        if (pFont->hForeground)
        {
            ::SetTextColor(hdc, pFont->crForeground);
        }

        if (pFont->hBackground)
        {
            ::SetBkColor(hdc, pFont->crBackground);

            *phBackgroundBrush = pFont->hBackground;
            fHasBackground = TRUE;
        }
        else
        {
            ::SetBkMode(hdc, TRANSPARENT);
            *phBackgroundBrush = static_cast<HBRUSH>(::GetStockObject(NULL_BRUSH));
            fHasBackground = TRUE;
        }
    }

    return fHasBackground;
}


DAPI_(HRESULT) ThemeSetProgressControl(
    __in const THEME_CONTROL* pThemeControl,
    __in DWORD dwProgressPercentage
    )
{
    HRESULT hr = E_INVALIDARG;

    if (pThemeControl && THEME_CONTROL_TYPE_PROGRESSBAR == pThemeControl->type)
    {
        THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(pThemeControl);

        DWORD dwCurrentProgress = LOWORD(pControl->dwData);

        if (dwCurrentProgress != dwProgressPercentage)
        {
            DWORD dwColor = HIWORD(pControl->dwData);
            pControl->dwData = MAKEDWORD(dwProgressPercentage, dwColor);

            if (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_OWNER_DRAW)
            {
                if (!::InvalidateRect(pControl->hWnd, NULL, FALSE))
                {
                    ThmExitWithLastError(hr, "Failed to invalidate progress bar window.");
                }
            }
            else
            {
                ::SendMessageW(pControl->hWnd, PBM_SETPOS, dwProgressPercentage, 0);
            }

            hr = S_OK;
        }
        else
        {
            hr = S_FALSE;
        }
    }

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeSetProgressControlColor(
    __in const THEME_CONTROL* pThemeControl,
    __in DWORD dwColorIndex
    )
{
    HRESULT hr = E_INVALIDARG;

    // Only set color on owner draw progress bars.
    if (pThemeControl && THEME_CONTROL_TYPE_PROGRESSBAR == pThemeControl->type && (pThemeControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_OWNER_DRAW))
    {
        THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(pThemeControl);

        if (pControl->ProgressBar.cImageRef <= dwColorIndex)
        {
            ThmExitWithRootFailure(hr, E_INVALIDARG, "Invalid progress bar color index: %u", dwColorIndex);
        }

        if (HIWORD(pControl->dwData) != dwColorIndex)
        {
            DWORD dwCurrentProgress =  LOWORD(pControl->dwData);
            pControl->dwData = MAKEDWORD(dwCurrentProgress, dwColorIndex);

            if (!::InvalidateRect(pControl->hWnd, NULL, FALSE))
            {
                ThmExitWithLastError(hr, "Failed to invalidate progress bar window.");
            }

            hr = S_OK;
        }
        else
        {
            hr = S_FALSE;
        }
    }

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeSetTextControl(
    __in const THEME_CONTROL* pThemeControl,
    __in_z_opt LPCWSTR wzText
    )
{
    return ThemeSetTextControlEx(pThemeControl, FALSE, wzText);
}


DAPI_(HRESULT) ThemeSetTextControlEx(
    __in const THEME_CONTROL* pThemeControl,
    __in BOOL fUpdate,
    __in_z_opt LPCWSTR wzText
    )
{
    HRESULT hr = E_INVALIDARG;

    if (pThemeControl)
    {
        if (fUpdate)
        {
            ::ShowWindow(pThemeControl->hWnd, SW_HIDE);
        }

        if (!::SetWindowTextW(pThemeControl->hWnd, wzText))
        {
            ThmExitWithLastError(hr, "Failed to set control text.");
        }

        if (fUpdate)
        {
            ::ShowWindow(pThemeControl->hWnd, SW_SHOW);
        }

        hr = S_OK;
    }

LExit:
    return hr;
}


DAPI_(HRESULT) ThemeGetTextControl(
    __in const THEME_CONTROL* pThemeControl,
    __inout_z LPWSTR* psczText
    )
{
    HRESULT hr = E_INVALIDARG;

    if (pThemeControl)
    {
        hr = WnduGetControlText(pThemeControl->hWnd, psczText);
    }

    return hr;
}


DAPI_(HRESULT) ThemeUpdateCaption(
    __in THEME* pTheme,
    __in_z LPCWSTR wzCaption
    )
{
    HRESULT hr = S_OK;

    hr = StrAllocString(&pTheme->sczCaption, wzCaption, 0);
    ThmExitOnFailure(hr, "Failed to update theme caption.");

LExit:
    return hr;
}


DAPI_(void) ThemeSetFocus(
    __in const THEME_CONTROL* pThemeControl
    )
{
    if (pThemeControl)
    {
        HWND hwndFocus = pThemeControl->hWnd;
        if (hwndFocus && !ThemeControlEnabled(pThemeControl))
        {
            hwndFocus = ::GetNextDlgTabItem(pThemeControl->pTheme->hwndParent, hwndFocus, FALSE);
        }

        if (hwndFocus)
        {
            ::SendMessage(pThemeControl->pTheme->hwndParent, WM_NEXTDLGCTL, (WPARAM)hwndFocus, TRUE);
        }
    }
}


DAPI_(void) ThemeShowChild(
    __in THEME_CONTROL* pParentControl,
    __in DWORD dwIndex
    )
{
    // show one child, hide the rest
    for (DWORD i = 0; i < pParentControl->cControls; ++i)
    {
        THEME_CONTROL* pControl = pParentControl->rgControls + i;
        ShowControl(pControl, dwIndex == i ? SW_SHOW : SW_HIDE, FALSE, THEME_SHOW_PAGE_REASON_DEFAULT, 0, NULL);
    }
}


// Internal functions.

static HRESULT RegisterWindowClasses(
    __in_opt HMODULE hModule
    )
{
    HRESULT hr = S_OK;
    WNDCLASSW wcHyperlink = { };
    WNDCLASSW wcPanel = { };
    WNDCLASSW wcStaticOwnerDraw = { };
    WNDPROC pfnStaticOwnerDrawBaseWndProc = NULL;

    vhCursorHand = ::LoadCursorA(NULL, IDC_HAND);

    // Base the theme hyperlink class on a button but give it the "hand" icon.
    if (!::GetClassInfoW(NULL, WC_BUTTONW, &wcHyperlink))
    {
        ThmExitWithLastError(hr, "Failed to get button window class.");
    }

    wcHyperlink.lpszClassName = vsczHyperlinkClass;
#pragma prefast(push)
#pragma prefast(disable:25068)
    wcHyperlink.hCursor = vhCursorHand;
#pragma prefast(pop)

    if (!::RegisterClassW(&wcHyperlink))
    {
        ThmExitWithLastError(hr, "Failed to register hyperlink window class.");
    }
    vhHyperlinkRegisteredModule = hModule;

    // Panel is its own do-nothing class.
    wcPanel.lpfnWndProc = PanelWndProc;
    wcPanel.hInstance = hModule;
    wcPanel.hCursor = ::LoadCursorW(NULL, (LPCWSTR) IDC_ARROW);
    wcPanel.lpszClassName = vsczPanelClass;
    if (!::RegisterClassW(&wcPanel))
    {
        ThmExitWithLastError(hr, "Failed to register panel window class.");
    }
    vhPanelRegisteredModule = hModule;

    if (!::GetClassInfoW(NULL, WC_STATICW, &wcStaticOwnerDraw))
    {
        ThmExitWithLastError(hr, "Failed to get static window class.");
    }

    pfnStaticOwnerDrawBaseWndProc = wcStaticOwnerDraw.lpfnWndProc;
    wcStaticOwnerDraw.lpfnWndProc = StaticOwnerDrawWndProc;
    wcStaticOwnerDraw.hInstance = hModule;
    wcStaticOwnerDraw.lpszClassName = vsczStaticOwnerDrawClass;
    if (!::RegisterClassW(&wcStaticOwnerDraw))
    {
        ThmExitWithLastError(hr, "Failed to register OwnerDraw window class.");
    }
    vhStaticOwnerDrawRegisteredModule = hModule;
    vpfnStaticOwnerDrawBaseWndProc = pfnStaticOwnerDrawBaseWndProc;


LExit:
    return hr;
}

static HRESULT ParseTheme(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMDocument* pixd,
    __out THEME** ppTheme
    )
{
    HRESULT hr = S_OK;
    THEME* pTheme = NULL;
    IXMLDOMElement *pThemeElement = NULL;
    Gdiplus::Bitmap* pBitmap = NULL;
    BOOL fXmlFound = FALSE;

    hr = pixd->get_documentElement(&pThemeElement);
    ThmExitOnFailure(hr, "Failed to get theme element.");

    pTheme = static_cast<THEME*>(MemAlloc(sizeof(THEME), TRUE));
    ThmExitOnNull(pTheme, hr, E_OUTOFMEMORY, "Failed to allocate memory for theme.");

    pTheme->nDpi = USER_DEFAULT_SCREEN_DPI;
    pTheme->wNextControlId = THEME_FIRST_AUTO_ASSIGN_CONTROL_ID;

    // Parse the optional background resource image.
    hr = GetAttributeImageFileOrResource(hModule, wzRelativePath, pThemeElement, &pBitmap);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed while parsing theme image.");

    if (fXmlFound)
    {
        hr = AddStandaloneImage(pTheme, &pBitmap, &pTheme->dwSourceImageInstanceIndex);
        ThmExitOnFailure(hr, "Failed to store theme image.");
    }
    else
    {
        pTheme->dwSourceImageInstanceIndex = THEME_INVALID_ID;
    }

    // Parse the fonts.
    hr = ParseFonts(pThemeElement, pTheme);
    ThmExitOnFailure(hr, "Failed to parse theme fonts.");

    // Parse the images.
    hr = ParseImages(hModule, wzRelativePath, pThemeElement, pTheme);
    ThmExitOnFailure(hr, "Failed to parse theme images.");

    // Parse any image lists.
    hr = ParseImageLists(hModule, wzRelativePath, pThemeElement, pTheme);
    ThmExitOnFailure(hr, "Failed to parse image lists.");

    // Parse the window element.
    hr = ParseWindow(hModule, wzRelativePath, pThemeElement, pTheme);
    ThmExitOnFailure(hr, "Failed to parse theme window element.");

    *ppTheme = pTheme;
    pTheme = NULL;

LExit:
    ReleaseObject(pThemeElement);

    if (pBitmap)
    {
        delete pBitmap;
    }

    if (pTheme)
    {
        ThemeFree(pTheme);
    }

    return hr;
}

static HRESULT AddStandaloneImage(
    __in THEME* pTheme,
    __in Gdiplus::Bitmap** ppBitmap,
    __out DWORD* pdwIndex
    )
{
    HRESULT hr = S_OK;
    THEME_IMAGE_INSTANCE* pInstance = NULL;

    hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(&pTheme->rgStandaloneImages), pTheme->cStandaloneImages, 1, sizeof(THEME_IMAGE_INSTANCE), GROW_IMAGE_INSTANCES);
    ThmExitOnFailure(hr, "Failed to allocate memory for image instances.");

    *pdwIndex = pTheme->cStandaloneImages;
    ++pTheme->cStandaloneImages;

    pInstance = pTheme->rgStandaloneImages + *pdwIndex;

    pInstance->pBitmap = *ppBitmap;
    *ppBitmap = NULL;

LExit:
    return hr;
}

static HRESULT GetAttributeImageFileOrResource(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __out Gdiplus::Bitmap** ppBitmap
    )
{
    HRESULT hr = S_OK;
    BSTR bstr = NULL;
    LPWSTR sczImageFile = NULL;
    WORD wResourceId = 0;
    BOOL fFound = FALSE;
    Gdiplus::Bitmap* pBitmap = NULL;
    *ppBitmap = NULL;

    hr = XmlGetAttributeUInt16(pElement, L"ImageResource", &wResourceId);
    ThmExitOnOptionalXmlQueryFailure(hr, fFound, "Failed to get image resource attribute.");

    if (fFound)
    {
        hr = GdipBitmapFromResource(hModule, MAKEINTRESOURCE(wResourceId), &pBitmap);
        ThmExitOnFailure(hr, "Failed to load image from resource: %hu", wResourceId);
    }

    hr = XmlGetAttribute(pElement, L"ImageFile", &bstr);
    ThmExitOnOptionalXmlQueryFailure(hr, fFound, "Failed to get image file attribute.");

    if (fFound)
    {
        if (pBitmap)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "ImageFile attribute can't be specified with ImageResource attribute.");
        }

        // Parse the optional background image from a given file.
        if (wzRelativePath)
        {
            hr = PathConcat(wzRelativePath, bstr, &sczImageFile);
            ThmExitOnFailure(hr, "Failed to combine image file path.");
        }
        else
        {
            hr = PathRelativeToModule(&sczImageFile, bstr, hModule);
            ThmExitOnFailure(hr, "Failed to get image filename.");
        }

        hr = GdipBitmapFromFile(sczImageFile, &pBitmap);
        ThmExitOnFailure(hr, "Failed to load image from file: %ls", sczImageFile);
    }

    if (pBitmap)
    {
        *ppBitmap = pBitmap;
        pBitmap = NULL;
    }
    else
    {
        hr = E_NOTFOUND;
    }

LExit:
    if (pBitmap)
    {
        delete pBitmap;
    }

    ReleaseStr(sczImageFile);
    ReleaseBSTR(bstr);

    return hr;
}


static HRESULT ParseOwnerDrawImage(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in THEME* pTheme,
    __in IXMLDOMNode* pElement,
    __in_z LPCWSTR wzElementName,
    __in THEME_CONTROL* pControl,
    __in THEME_IMAGE_REFERENCE* pImageRef
    )
{
    HRESULT hr = S_OK;
    DWORD dwValue = 0;
    BOOL fXmlFound = FALSE;
    BOOL fFoundImage = FALSE;
    Gdiplus::Bitmap* pBitmap = NULL;

    hr = GetAttributeImageId(pTheme, pElement, L"ImageId", &dwValue);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to parse ImageId attribute.");

    if (fXmlFound)
    {
        pImageRef->type = THEME_IMAGE_REFERENCE_TYPE_COMPLETE;
        pImageRef->dwImageIndex = dwValue;
        pImageRef->dwImageInstanceIndex = 0;
        fFoundImage = TRUE;
    }
    else
    {
        pImageRef->dwImageIndex = THEME_INVALID_ID;
    }

    // Parse the optional background resource image.
    hr = GetAttributeImageFileOrResource(hModule, wzRelativePath, pElement, &pBitmap);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed while parsing control image.");

    if (fXmlFound)
    {
        if (fFoundImage)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Unexpected image attribute with ImageId attribute.");
        }

        hr = AddStandaloneImage(pTheme, &pBitmap, &pImageRef->dwImageInstanceIndex);
        ThmExitOnFailure(hr, "Failed to store owner draw image.");

        pImageRef->type = THEME_IMAGE_REFERENCE_TYPE_COMPLETE;

        fFoundImage = TRUE;
    }

    hr = ParseSourceXY(pElement, pTheme, pControl->nWidth, pControl->nHeight, pImageRef);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get control SourceX and SourceY attributes.");

    if (fXmlFound)
    {
        if (fFoundImage)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Unexpected SourceX attribute with image attribute.");
        }
        else if (1 > pControl->nWidth || 1 > pControl->nHeight)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Control Width and Height must be positive when using SourceX and SourceY.");
        }

        fFoundImage = TRUE;
    }

    if (!fFoundImage)
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "%ls didn't specify an image.", wzElementName);
    }

LExit:
    if (pBitmap)
    {
        delete pBitmap;
    }

    return hr;
}


static HRESULT ParseButtonImages(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in THEME* pTheme,
    __in IXMLDOMNode* pElement,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrType = NULL;
    THEME_IMAGE_REFERENCE* pImageRef = NULL;
    THEME_IMAGE_REFERENCE* pDefaultImageRef = NULL;
    THEME_IMAGE_REFERENCE* pFocusImageRef = NULL;
    THEME_IMAGE_REFERENCE* pHoverImageRef = NULL;
    THEME_IMAGE_REFERENCE* pSelectedImageRef = NULL;

    hr = XmlSelectNodes(pElement, L"ButtonImage|ButtonFocusImage|ButtonHoverImage|ButtonSelectedImage", &pixnl);
    ThmExitOnFailure(hr, "Failed to select child ButtonImage nodes.");

    i = 0;
    while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, &bstrType)))
    {
        if (!bstrType)
        {
            hr = E_UNEXPECTED;
            ThmExitOnFailure(hr, "Null element encountered!");
        }

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"ButtonFocusImage", -1))
        {
            if (pFocusImageRef)
            {
                ThmExitWithRootFailure(hr, E_INVALIDDATA, "Duplicate ButtonFocusImage element.");
            }

            pImageRef = pFocusImageRef = pControl->Button.rgImageRef + 3;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"ButtonHoverImage", -1))
        {
            if (pHoverImageRef)
            {
                ThmExitWithRootFailure(hr, E_INVALIDDATA, "Duplicate ButtonHoverImage element.");
            }

            pImageRef = pHoverImageRef = pControl->Button.rgImageRef + 1;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"ButtonSelectedImage", -1))
        {
            if (pSelectedImageRef)
            {
                ThmExitWithRootFailure(hr, E_INVALIDDATA, "Duplicate ButtonSelectedImage element.");
            }

            pImageRef = pSelectedImageRef = pControl->Button.rgImageRef + 2;
        }
        else
        {
            if (pDefaultImageRef)
            {
                ThmExitWithRootFailure(hr, E_INVALIDDATA, "Duplicate ButtonImage element.");
            }

            pImageRef = pDefaultImageRef = pControl->Button.rgImageRef;
        }

        hr = ParseOwnerDrawImage(hModule, wzRelativePath, pTheme, pixnChild, bstrType, pControl, pImageRef);
        ThmExitOnFailure(hr, "Failed when parsing %ls", bstrType);

        ReleaseBSTR(bstrType);
        ReleaseNullObject(pixnChild);
        ++i;
    }

    if (!pDefaultImageRef && (pFocusImageRef || pHoverImageRef || pSelectedImageRef) ||
        pDefaultImageRef && (!pHoverImageRef || !pSelectedImageRef))
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "Graphic buttons require ButtonImage, ButtonHoverImage, and ButtonSelectedImage.");
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrType);

    return hr;
}


static HRESULT ParseCommandLinkImage(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    BSTR bstr = NULL;
    BOOL fImageFound = FALSE;
    BOOL fXmlFound = FALSE;
    LPWSTR sczIconFile = NULL;
    WORD wResourceId = 0;
    Gdiplus::Bitmap* pBitmap = NULL;

    hr = GetAttributeImageFileOrResource(hModule, wzRelativePath, pElement, &pBitmap);
    ThmExitOnOptionalXmlQueryFailure(hr, fImageFound, "Failed to parse image attributes for CommandLink.");

    if (pBitmap)
    {
        hr = GdipBitmapToGdiBitmap(pBitmap, &pControl->CommandLink.hImage);
        ThmExitOnFailure(hr, "Failed to convert bitmap for CommandLink.");
    }

    hr = XmlGetAttributeUInt16(pElement, L"IconResource", &wResourceId);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get icon resource attribute.");

    if (fXmlFound)
    {
        if (fImageFound)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Unexpected IconResource attribute with image attribute.");
        }

        pControl->CommandLink.hIcon = reinterpret_cast<HICON>(::LoadImageW(hModule, MAKEINTRESOURCEW(wResourceId), IMAGE_ICON, 0, 0, LR_DEFAULTSIZE));
        ThmExitOnNullWithLastError(pControl->CommandLink.hIcon, hr, "Failed to load icon.");
    }

    hr = XmlGetAttribute(pElement, L"IconFile", &bstr);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get icon file attribute.");

    if (fXmlFound)
    {
        if (fImageFound)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Unexpected IconFile attribute with image attribute.");
        }
        else if (pControl->CommandLink.hIcon)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "IconFile attribute can't be specified with IconResource attribute.");
        }

        if (wzRelativePath)
        {
            hr = PathConcat(wzRelativePath, bstr, &sczIconFile);
            ThmExitOnFailure(hr, "Failed to combine image file path.");
        }
        else
        {
            hr = PathRelativeToModule(&sczIconFile, bstr, hModule);
            ThmExitOnFailure(hr, "Failed to get image filename.");
        }

        pControl->CommandLink.hIcon = reinterpret_cast<HICON>(::LoadImageW(NULL, sczIconFile, IMAGE_ICON, 0, 0, LR_DEFAULTSIZE | LR_LOADFROMFILE));
        ThmExitOnNullWithLastError(pControl->CommandLink.hIcon, hr, "Failed to load icon: %ls.", sczIconFile);
    }

    ThmExitOnUnexpectedAttribute(hr, pElement, L"CommandLink", L"SourceX");
    ThmExitOnUnexpectedAttribute(hr, pElement, L"CommandLink", L"SourceY");

LExit:
    if (pBitmap)
    {
        delete pBitmap;
    }

    ReleaseStr(sczIconFile);
    ReleaseBSTR(bstr);

    return hr;
}


static HRESULT ParseProgressBarImages(
    __in_opt HMODULE hModule,
    __in_z_opt LPCWSTR wzRelativePath,
    __in THEME* pTheme,
    __in IXMLDOMNode* pElement,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;

    hr = XmlSelectNodes(pElement, L"ProgressbarImage", &pixnl);
    ThmExitOnFailure(hr, "Failed to select child ProgressbarImage nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pControl->ProgressBar.cImageRef));
    ThmExitOnFailure(hr, "Failed to count the number of ProgressbarImage nodes.");

    if (!pControl->ProgressBar.cImageRef)
    {
        ExitFunction();
    }

    MemAllocArray(reinterpret_cast<LPVOID*>(&pControl->ProgressBar.rgImageRef), sizeof(THEME_IMAGE_REFERENCE), pControl->ProgressBar.cImageRef);
    ThmExitOnNull(pControl->ProgressBar.rgImageRef, hr, E_OUTOFMEMORY, "Failed to allocate progress bar images.");

    i = 0;
    while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
    {
        THEME_IMAGE_REFERENCE* pImageRef = pControl->ProgressBar.rgImageRef + i;

        hr = ParseOwnerDrawImage(hModule, wzRelativePath, pTheme, pixnChild, L"ProgressbarImage", pControl, pImageRef);
        ThmExitOnFailure(hr, "Failed when parsing ProgressbarImage image: %u.", i);

        ReleaseNullObject(pixnChild);
        ++i;
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);

    return hr;
}


static HRESULT GetAttributeCoordinateOrDimension(
    __in IXMLDOMNode* pixn,
    __in LPCWSTR wzAttribute,
    __inout int* pnValue
    )
{
    HRESULT hr = S_OK;
    int nValue = 0;
    BOOL fXmlFound = FALSE;

    hr = XmlGetAttributeInt32(pixn, wzAttribute, &nValue);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get coordinate or dimension attribute.");

    if (!fXmlFound)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }
    else if (abs(nValue) > SHORT_MAX)
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "Invalid coordinate or dimension attribute value: %i", nValue);
    }

    *pnValue = nValue;

LExit:
    return hr;
}

static HRESULT GetAttributeFontId(
    __in THEME* pTheme,
    __in IXMLDOMNode* pixn,
    __in LPCWSTR wzAttribute,
    __inout DWORD* pdwValue
    )
{
    HRESULT hr = S_OK;
    BSTR bstrId = NULL;
    THEME_FONT* pFont = NULL;
    BOOL fXmlFound = FALSE;

    hr = XmlGetAttribute(pixn, wzAttribute, &bstrId);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get font id attribute.");

    if (!fXmlFound)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = DictGetValue(pTheme->sdhFontDictionary, bstrId, reinterpret_cast<void**>(&pFont));
    if (E_NOTFOUND == hr)
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "Unknown font id: %ls", bstrId);
    }
    ThmExitOnFailure(hr, "Failed to find font with id: %ls", bstrId);

    *pdwValue = pFont->dwIndex;

LExit:
    ReleaseBSTR(bstrId);

    return hr;
}

static HRESULT GetAttributeImageId(
    __in THEME* pTheme,
    __in IXMLDOMNode* pixn,
    __in LPCWSTR wzAttribute,
    __inout DWORD* pdwValue
    )
{
    HRESULT hr = S_OK;
    BSTR bstrId = NULL;
    THEME_IMAGE* pImage = NULL;
    BOOL fXmlFound = FALSE;

    hr = XmlGetAttribute(pixn, wzAttribute, &bstrId);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get image id attribute.");

    if (!fXmlFound)
    {
        ExitFunction1(hr = E_NOTFOUND);
    }

    hr = DictGetValue(pTheme->sdhImageDictionary, bstrId, reinterpret_cast<void**>(&pImage));
    if (E_NOTFOUND == hr)
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "Unknown image id: %ls", bstrId);
    }
    ThmExitOnFailure(hr, "Failed to find image with id: %ls", bstrId);

    *pdwValue = pImage->dwIndex;

LExit:
    ReleaseBSTR(bstrId);

    return hr;
}

static HRESULT ParseSourceXY(
    __in IXMLDOMNode* pixn,
    __in THEME* pTheme,
    __in int nWidth,
    __in int nHeight,
    __inout THEME_IMAGE_REFERENCE* pReference
    )
{
    HRESULT hr = S_OK;
    BOOL fXFound = FALSE;
    BOOL fYFound = FALSE;
    int nX = 0;
    int nY = 0;
    DWORD dwImageInstanceIndex = pTheme->dwSourceImageInstanceIndex;
    THEME_IMAGE_INSTANCE* pInstance = THEME_INVALID_ID != dwImageInstanceIndex ? pTheme->rgStandaloneImages + dwImageInstanceIndex : NULL;
    int nSourceWidth = pInstance ? pInstance->pBitmap->GetWidth() : 0;
    int nSourceHeight = pInstance ? pInstance->pBitmap->GetHeight() : 0;

    hr = GetAttributeCoordinateOrDimension(pixn, L"SourceX", &nX);
    ThmExitOnOptionalXmlQueryFailure(hr, fXFound, "Failed to get SourceX attribute.");

    if (!fXFound)
    {
        nX = -1;
    }
    else
    {
        if (!pInstance)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceX cannot be specified without an image specified on Theme.");
        }
        else if (0 > nX)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceX must be non-negative.");
        }
        else if (nSourceWidth <= nX)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceX (%i) must be less than the image width: %i.", nX, nSourceWidth);
        }
        else if (nSourceWidth <= (nX + nWidth))
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceX (%i) with width %i must be less than the image width: %i.", nX, nWidth, nSourceWidth);
        }
    }

    hr = GetAttributeCoordinateOrDimension(pixn, L"SourceY", &nY);
    ThmExitOnOptionalXmlQueryFailure(hr, fYFound, "Failed to get SourceY attribute.");

    if (!fYFound)
    {
        if (fXFound)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceY must be specified with SourceX.");
        }

        ExitFunction1(hr = E_NOTFOUND);
    }
    else
    {
        if (!pInstance)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceY cannot be specified without an image specified on Theme.");
        }
        else if (!fXFound)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceY must be specified with SourceX.");
        }
        else if (0 > nY)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceY must be non-negative.");
        }
        else if (nSourceHeight <= nY)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceY (%i) must be less than the image height: %i.", nY, nSourceHeight);
        }
        else if (nSourceHeight <= (nY + nHeight))
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "SourceY (%i) with height %i must be less than the image height: %i.", nY, nHeight, nSourceHeight);
        }
    }

    pReference->type = THEME_IMAGE_REFERENCE_TYPE_PARTIAL;
    pReference->dwImageIndex = THEME_INVALID_ID;
    pReference->dwImageInstanceIndex = dwImageInstanceIndex;
    pReference->nX = nX;
    pReference->nY = nY;
    pReference->nWidth = nWidth;
    pReference->nHeight = nHeight;

LExit:
    return hr;
}

static HRESULT ParseWindow(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixn = NULL;
    BOOL fXmlFound = FALSE;
    int nValue = 0;
    BSTR bstr = NULL;
    LPWSTR sczIconFile = NULL;

    hr = XmlSelectSingleNode(pElement, L"Window", &pixn);
    ThmExitOnRequiredXmlQueryFailure(hr, "Failed to find window element.");

    hr = XmlGetYesNoAttribute(pixn, L"AutoResize", &pTheme->fAutoResize);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get window AutoResize attribute.");

    hr = GetAttributeCoordinateOrDimension(pixn, L"Width", &nValue);
    ThmExitOnRequiredXmlQueryFailure(hr, "Failed to get window Width attribute.");

    if (1 > nValue)
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "Window/@Width must be positive: %i", nValue);
    }

    pTheme->nWidth = pTheme->nDefaultDpiWidth = pTheme->nWindowWidth = nValue;

    hr = GetAttributeCoordinateOrDimension(pixn, L"Height", &nValue);
    ThmExitOnRequiredXmlQueryFailure(hr, "Failed to get window Height attribute.");

    if (1 > nValue)
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "Window/@Height must be positive: %i", nValue);
    }

    pTheme->nHeight = pTheme->nDefaultDpiHeight = pTheme->nWindowHeight = nValue;

    hr = GetAttributeCoordinateOrDimension(pixn, L"MinimumWidth", &nValue);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get window MinimumWidth attribute.");

    if (fXmlFound)
    {
        if (!pTheme->fAutoResize)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Window/@MinimumWidth can't be specified unless AutoResize is enabled.");
        }
        else if (1 > nValue || pTheme->nWidth < nValue)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Window/@MinimumWidth must be positive and not greater than Window/@Width: %i", nValue);
        }

        pTheme->nMinimumWidth = pTheme->nDefaultDpiMinimumWidth = nValue;
    }

    hr = GetAttributeCoordinateOrDimension(pixn, L"MinimumHeight", &nValue);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get window MinimumHeight attribute.");

    if (fXmlFound)
    {
        if (!pTheme->fAutoResize)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Window/@MinimumHeight can't be specified unless AutoResize is enabled.");
        }
        else if (1 > nValue || pTheme->nHeight < nValue)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Window/@MinimumHeight must be positive and not greater than Window/@Height: %i", nValue);
        }

        pTheme->nMinimumHeight = pTheme->nDefaultDpiMinimumHeight = nValue;
    }

    hr = GetAttributeFontId(pTheme, pixn, L"FontId", &pTheme->dwFontId);
    ThmExitOnRequiredXmlQueryFailure(hr, "Failed to get window FontId attribute.");

    // Get the optional window icon from a resource.
    hr = XmlGetAttribute(pixn, L"IconResource", &bstr);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get window IconResource attribute.");

    if (fXmlFound)
    {
        pTheme->hIcon = ::LoadIconW(hModule, bstr);
        ThmExitOnNullWithLastError(pTheme->hIcon, hr, "Failed to load window icon from IconResource.");

        ReleaseNullBSTR(bstr);
    }

    // Get the optional window icon from a file.
    hr = XmlGetAttribute(pixn, L"IconFile", &bstr);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get window IconFile attribute.");

    if (fXmlFound)
    {
        if (pTheme->hIcon)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Window/@IconFile can't be specified with IconResource.");
        }

        if (wzRelativePath)
        {
            hr = PathConcat(wzRelativePath, bstr, &sczIconFile);
            ThmExitOnFailure(hr, "Failed to combine icon file path.");
        }
        else
        {
            hr = PathRelativeToModule(&sczIconFile, bstr, hModule);
            ThmExitOnFailure(hr, "Failed to get icon filename.");
        }

        pTheme->hIcon = ::LoadImageW(NULL, sczIconFile, IMAGE_ICON, 0, 0, LR_DEFAULTSIZE | LR_LOADFROMFILE);
        ThmExitOnNullWithLastError(pTheme->hIcon, hr, "Failed to load window icon from IconFile: %ls.", bstr);

        ReleaseNullBSTR(bstr);
    }

    hr = ParseSourceXY(pixn, pTheme, pTheme->nDefaultDpiWidth, pTheme->nDefaultDpiHeight, &pTheme->windowImageRef);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get window SourceX and SourceY attributes.");

    // Parse the optional window style.
    hr = XmlGetAttributeNumberBase(pixn, L"HexStyle", 16, &pTheme->dwStyle);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get theme window style (Window@HexStyle) attribute.");

    if (!fXmlFound)
    {
        pTheme->dwStyle = WS_VISIBLE | WS_MINIMIZEBOX | WS_SYSMENU | WS_CAPTION;
        pTheme->dwStyle |= (THEME_IMAGE_REFERENCE_TYPE_NONE != pTheme->windowImageRef.type) ? WS_POPUP : WS_OVERLAPPED;
    }

    hr = XmlGetAttributeUInt32(pixn, L"StringId", reinterpret_cast<DWORD*>(&pTheme->uStringId));
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get window StringId attribute.");

    if (!fXmlFound)
    {
        pTheme->uStringId = UINT_MAX;
    }
    else if (UINT_MAX == pTheme->uStringId)
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "Invalid StringId: %u", pTheme->uStringId);
    }

    hr = XmlGetAttributeEx(pixn, L"Caption", &pTheme->sczCaption);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get window Caption attribute.");

    if (fXmlFound && UINT_MAX != pTheme->uStringId || !fXmlFound && UINT_MAX == pTheme->uStringId)
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "Window elements must contain either the Caption or StringId attribute.");
    }

    // Parse the pages.
    hr = ParsePages(hModule, wzRelativePath, pixn, pTheme);
    ThmExitOnFailure(hr, "Failed to parse theme pages.");

    // Parse the non-paged controls.
    hr = ParseControls(hModule, wzRelativePath, pixn, pTheme, NULL, NULL, NULL);
    ThmExitOnFailure(hr, "Failed to parse theme controls.");

LExit:
    ReleaseStr(sczIconFile);
    ReleaseBSTR(bstr);
    ReleaseObject(pixn);

    return hr;
}


static HRESULT ParseFonts(
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixn = NULL;
    LPWSTR sczFontId = NULL;
    BSTR bstrName = NULL;
    DWORD dwId = 0;
    BOOL fXmlFound = FALSE;
    COLORREF crForeground = THEME_INVISIBLE_COLORREF;
    COLORREF crBackground = THEME_INVISIBLE_COLORREF;
    DWORD dwSystemForegroundColor = FALSE;
    DWORD dwSystemBackgroundColor = FALSE;

    hr = XmlSelectNodes(pElement, L"Font", &pixnl);
    ThmExitOnFailure(hr, "Failed to find font elements.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pTheme->cFonts));
    ThmExitOnFailure(hr, "Failed to count the number of theme fonts.");

    if (!pTheme->cFonts)
    {
        ThmExitOnRootFailure(hr = E_INVALIDDATA, "No font elements found.");
    }

    pTheme->rgFonts = static_cast<THEME_FONT*>(MemAlloc(sizeof(THEME_FONT) * pTheme->cFonts, TRUE));
    ThmExitOnNull(pTheme->rgFonts, hr, E_OUTOFMEMORY, "Failed to allocate theme fonts.");

    hr = DictCreateWithEmbeddedKey(&pTheme->sdhFontDictionary, pTheme->cFonts, reinterpret_cast<void**>(&pTheme->rgFonts), offsetof(THEME_FONT, sczId), DICT_FLAG_NONE);
    ThmExitOnFailure(hr, "Failed to create font dictionary.");

    while (S_OK == (hr = XmlNextElement(pixnl, &pixn, NULL)))
    {
        hr = XmlGetAttributeEx(pixn, L"Id", &sczFontId);
        ThmExitOnRequiredXmlQueryFailure(hr, "Failed to find font id.");

        hr = DictKeyExists(pTheme->sdhFontDictionary, sczFontId);
        if (E_NOTFOUND != hr)
        {
            ThmExitOnFailure(hr, "Failed to check for duplicate font id.");
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Theme font id duplicated: %ls", sczFontId);
        }

        THEME_FONT* pFont = pTheme->rgFonts + dwId;
        pFont->sczId = sczFontId;
        sczFontId = NULL;
        pFont->dwIndex = dwId;
        ++dwId;

        pFont->lfQuality = CLEARTYPE_QUALITY;

        hr = XmlGetText(pixn, &bstrName);
        ThmExitOnRequiredXmlQueryFailure(hr, "Failed to get font name.");

        hr = StrAllocString(&pFont->sczFaceName, bstrName, 0);
        ThmExitOnFailure(hr, "Failed to copy font name.");

        hr = XmlGetAttributeInt32(pixn, L"Height", reinterpret_cast<int*>(&pFont->lfHeight));
        ThmExitOnRequiredXmlQueryFailure(hr, "Failed to find font height attribute.");

        hr = XmlGetAttributeInt32(pixn, L"Weight", reinterpret_cast<int*>(&pFont->lfWeight));
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to find font weight attribute.");

        if (!fXmlFound)
        {
            pFont->lfWeight = FW_DONTCARE;
        }

        hr = XmlGetYesNoAttribute(pixn, L"Underline", reinterpret_cast<BOOL*>(&pFont->lfUnderline));
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to find font underline attribute.");

        if (!fXmlFound)
        {
            pFont->lfUnderline = FALSE;
        }

        hr = GetFontColor(pixn, L"Foreground", &crForeground, &dwSystemForegroundColor);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to find font foreground color.");

        hr = GetFontColor(pixn, L"Background", &crBackground, &dwSystemBackgroundColor);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to find font background color.");

        pFont->crForeground = crForeground;
        if (THEME_INVISIBLE_COLORREF != pFont->crForeground)
        {
            pFont->hForeground = dwSystemForegroundColor ? ::GetSysColorBrush(dwSystemForegroundColor) : ::CreateSolidBrush(pFont->crForeground);
            ThmExitOnNull(pFont->hForeground, hr, E_OUTOFMEMORY, "Failed to create text foreground brush.");
        }

        pFont->crBackground = crBackground;
        if (THEME_INVISIBLE_COLORREF != pFont->crBackground)
        {
            pFont->hBackground = dwSystemBackgroundColor ? ::GetSysColorBrush(dwSystemBackgroundColor) : ::CreateSolidBrush(pFont->crBackground);
            ThmExitOnNull(pFont->hBackground, hr, E_OUTOFMEMORY, "Failed to create text background brush.");
        }

        hr = DictAddValue(pTheme->sdhFontDictionary, pFont);
        ThmExitOnFailure(hr, "Failed to add font to dictionary.");

        ReleaseNullBSTR(bstrName);
        ReleaseNullObject(pixn);
    }
    ThmExitOnFailure(hr, "Failed to enumerate all fonts.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseBSTR(bstrName);
    ReleaseStr(sczFontId);
    ReleaseObject(pixn);
    ReleaseObject(pixnl);

    return hr;
}


static HRESULT GetFontColor(
    __in IXMLDOMNode* pixn,
    __in_z LPCWSTR wzAttributeName,
    __out COLORREF* pColorRef,
    __out DWORD* pdwSystemColor
    )
{
    HRESULT hr = S_OK;
    BSTR bstr = NULL;
    BOOL fXmlFound = FALSE;

    *pdwSystemColor = 0;

    hr = XmlGetAttribute(pixn, wzAttributeName, &bstr);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to find font %ls color.", wzAttributeName);

    if (!fXmlFound)
    {
        *pColorRef = THEME_INVISIBLE_COLORREF;
        ExitFunction1(hr = E_NOTFOUND);
    }

    if (pdwSystemColor)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstr, -1, L"btnface", -1))
        {
            *pdwSystemColor = COLOR_BTNFACE;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstr, -1, L"btntext", -1))
        {
            *pdwSystemColor = COLOR_BTNTEXT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstr, -1, L"graytext", -1))
        {
            *pdwSystemColor = COLOR_GRAYTEXT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstr, -1, L"highlight", -1))
        {
            *pdwSystemColor = COLOR_HIGHLIGHT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstr, -1, L"highlighttext", -1))
        {
            *pdwSystemColor = COLOR_HIGHLIGHTTEXT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstr, -1, L"hotlight", -1))
        {
            *pdwSystemColor = COLOR_HOTLIGHT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstr, -1, L"window", -1))
        {
            *pdwSystemColor = COLOR_WINDOW;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstr, -1, L"windowtext", -1))
        {
            *pdwSystemColor = COLOR_WINDOWTEXT;
        }
        else
        {
            *pColorRef = ::wcstoul(bstr, NULL, 16);

            if (THEME_INVISIBLE_COLORREF == *pColorRef)
            {
                ThmExitWithRootFailure(hr, E_INVALIDDATA, "Invalid %ls value: %ls.", wzAttributeName, bstr);
            }
        }

        if (*pdwSystemColor)
        {
            *pColorRef = ::GetSysColor(*pdwSystemColor);
        }
    }

LExit:
    ReleaseBSTR(bstr);

    return hr;
}


static HRESULT ParseImages(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMElement* pElement,
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixn = NULL;
    LPWSTR sczImageId = NULL;
    DWORD dwImageIndex = 0;
    Gdiplus::Bitmap* pDefaultBitmap = NULL;
    IXMLDOMNodeList* pixnlAlternates = NULL;
    IXMLDOMNode* pixnAlternate = NULL;
    DWORD dwInstances = 0;
    THEME_IMAGE_INSTANCE* pInstance = NULL;
    BOOL fXmlFound = FALSE;

    hr = XmlSelectNodes(pElement, L"Image", &pixnl);
    ThmExitOnFailure(hr, "Failed to find font elements.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pTheme->cImages));
    ThmExitOnFailure(hr, "Failed to count the number of theme images.");

    if (!pTheme->cImages)
    {
        ExitFunction1(hr = S_OK);
    }

    pTheme->rgImages = static_cast<THEME_IMAGE*>(MemAlloc(sizeof(THEME_IMAGE) * pTheme->cImages, TRUE));
    ThmExitOnNull(pTheme->rgImages, hr, E_OUTOFMEMORY, "Failed to allocate theme images.");

    hr = DictCreateWithEmbeddedKey(&pTheme->sdhImageDictionary, pTheme->cImages, reinterpret_cast<void**>(&pTheme->rgImages), offsetof(THEME_IMAGE, sczId), DICT_FLAG_NONE);
    ThmExitOnFailure(hr, "Failed to create image dictionary.");

    while (S_OK == (hr = XmlNextElement(pixnl, &pixn, NULL)))
    {
        hr = XmlGetAttributeEx(pixn, L"Id", &sczImageId);
        ThmExitOnRequiredXmlQueryFailure(hr, "Failed to find image id.");

        hr = DictKeyExists(pTheme->sdhImageDictionary, sczImageId);
        if (E_NOTFOUND != hr)
        {
            ThmExitOnFailure(hr, "Failed to check for duplicate image id.");
            ThmExitOnRootFailure(hr = E_INVALIDDATA, "Theme image id duplicated: %ls", sczImageId);
        }

        THEME_IMAGE* pImage = pTheme->rgImages + dwImageIndex;
        pImage->sczId = sczImageId;
        sczImageId = NULL;
        pImage->dwIndex = dwImageIndex;
        ++dwImageIndex;

        hr = GetAttributeImageFileOrResource(hModule, wzRelativePath, pixn, &pDefaultBitmap);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to parse Image: %ls", pImage->sczId);

        if (!fXmlFound)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Image didn't specify an image: %ls.", pImage->sczId);
        }

        hr = DictAddValue(pTheme->sdhImageDictionary, pImage);
        ThmExitOnFailure(hr, "Failed to add image to dictionary.");

        // Parse alternates, if any.
        hr = XmlSelectNodes(pixn, L"AlternateResolution", &pixnlAlternates);
        ThmExitOnFailure(hr, "Failed to select child AlternateResolution nodes.");

        hr = pixnlAlternates->get_length(reinterpret_cast<long*>(&dwInstances));
        ThmExitOnFailure(hr, "Failed to count the number of alternates.");

        dwInstances += 1;

        pImage->rgImageInstances = static_cast<THEME_IMAGE_INSTANCE*>(MemAlloc(sizeof(THEME_IMAGE_INSTANCE) * dwInstances, TRUE));
        ThmExitOnNull(pImage->rgImageInstances, hr, E_OUTOFMEMORY, "Failed to allocate image instances.");

        pInstance = pImage->rgImageInstances;
        pInstance->pBitmap = pDefaultBitmap;
        pDefaultBitmap = NULL;
        pImage->cImageInstances += 1;

        while (S_OK == (hr = XmlNextElement(pixnlAlternates, &pixnAlternate, NULL)))
        {
            pInstance = pImage->rgImageInstances + pImage->cImageInstances;

            hr = GetAttributeImageFileOrResource(hModule, wzRelativePath, pixnAlternate, &pInstance->pBitmap);
            ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to parse Image: '%ls', alternate resolution: %u", pImage->sczId, pImage->cImageInstances);

            if (!fXmlFound)
            {
                ThmExitWithRootFailure(hr, E_INVALIDDATA, "Image: '%ls', alternate resolution: %u, didn't specify an image.", pImage->sczId, pImage->cImageInstances);
            }

            ReleaseNullObject(pixnAlternate);

            pImage->cImageInstances += 1;
        }

        ReleaseNullObject(pixnlAlternates);
        ReleaseNullObject(pixn);
    }
    ThmExitOnFailure(hr, "Failed to enumerate all images.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    if (pDefaultBitmap)
    {
        delete pDefaultBitmap;
    }

    ReleaseObject(pixnAlternate);
    ReleaseObject(pixnlAlternates);
    ReleaseStr(sczImageId);
    ReleaseObject(pixn);
    ReleaseObject(pixnl);

    return hr;
}

static HRESULT ParsePages(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixn = NULL;
    BSTR bstrType = NULL;
    THEME_PAGE* pPage = NULL;
    DWORD iPage = 0;
    BOOL fXmlFound = FALSE;

    hr = XmlSelectNodes(pElement, L"Page", &pixnl);
    ThmExitOnFailure(hr, "Failed to find page elements.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pTheme->cPages));
    ThmExitOnFailure(hr, "Failed to count the number of theme pages.");

    if (!pTheme->cPages)
    {
        ExitFunction1(hr = S_OK);
    }

    pTheme->rgPages = static_cast<THEME_PAGE*>(MemAlloc(sizeof(THEME_PAGE) * pTheme->cPages, TRUE));
    ThmExitOnNull(pTheme->rgPages, hr, E_OUTOFMEMORY, "Failed to allocate theme pages.");

    while (S_OK == (hr = XmlNextElement(pixnl, &pixn, &bstrType)))
    {
        pPage = pTheme->rgPages + iPage;

        pPage->wId = static_cast<WORD>(iPage + 1);

        hr = XmlGetAttributeEx(pixn, L"Name", &pPage->sczName);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying page Name.");

        hr = ParseControls(hModule, wzRelativePath, pixn, pTheme, NULL, pPage, NULL);
        ThmExitOnFailure(hr, "Failed to parse page controls.");

        ++iPage;

        ReleaseNullBSTR(bstrType);
        ReleaseNullObject(pixn);
    }
    ThmExitOnFailure(hr, "Failed to enumerate all pages.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

LExit:
    ReleaseBSTR(bstrType);
    ReleaseObject(pixn);
    ReleaseObject(pixnl);

    return hr;
}


static HRESULT ParseImageLists(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnlImageLists = NULL;
    IXMLDOMNode* pixnImageList = NULL;
    IXMLDOMNodeList* pixnlImages = NULL;
    IXMLDOMNode* pixnImage = NULL;
    DWORD dwImageListIndex = 0;
    DWORD dwImageCount = 0;
    THEME_IMAGELIST* pThemeImageList = NULL;
    BOOL fXmlFound = FALSE;
    Gdiplus::Bitmap* pBitmap = NULL;
    HBITMAP hImage = NULL;
    DWORD i = 0;
    int iRetVal = 0;

    hr = XmlSelectNodes(pElement, L"ImageList", &pixnlImageLists);
    ThmExitOnFailure(hr, "Failed to find ImageList elements.");

    hr = pixnlImageLists->get_length(reinterpret_cast<long*>(&pTheme->cImageLists));
    ThmExitOnFailure(hr, "Failed to count the number of image lists.");

    if (!pTheme->cImageLists)
    {
        ExitFunction1(hr = S_OK);
    }

    pTheme->rgImageLists = static_cast<THEME_IMAGELIST*>(MemAlloc(sizeof(THEME_IMAGELIST) * pTheme->cImageLists, TRUE));
    ThmExitOnNull(pTheme->rgImageLists, hr, E_OUTOFMEMORY, "Failed to allocate theme image lists.");

    while (S_OK == (hr = XmlNextElement(pixnlImageLists, &pixnImageList, NULL)))
    {
        pThemeImageList = pTheme->rgImageLists + dwImageListIndex;
        ++dwImageListIndex;
        i = 0;

        hr = XmlGetAttributeEx(pixnImageList, L"Name", &pThemeImageList->sczName);
        ThmExitOnRequiredXmlQueryFailure(hr, "Failed to find ImageList/@Name attribute.");

        hr = XmlSelectNodes(pixnImageList, L"ImageListItem", &pixnlImages);
        ThmExitOnFailure(hr, "Failed to select child ImageListItem nodes.");

        hr = pixnlImages->get_length(reinterpret_cast<long*>(&dwImageCount));
        ThmExitOnFailure(hr, "Failed to count the number of images in list.");

        if (!dwImageCount)
        {
            ThmExitOnRootFailure(hr = E_INVALIDDATA, "ImageList '%ls' has no images.", pThemeImageList->sczName);
        }

        while (S_OK == (hr = XmlNextElement(pixnlImages, &pixnImage, NULL)))
        {
            if (pBitmap)
            {
                delete pBitmap;
                pBitmap = NULL;
            }
            if (hImage)
            {
                ::DeleteObject(hImage);
                hImage = NULL;
            }

            hr = GetAttributeImageFileOrResource(hModule, wzRelativePath, pixnImage, &pBitmap);
            ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to parse image list: '%ls', item: %u", pThemeImageList->sczName, i);

            if (!fXmlFound)
            {
                ThmExitWithRootFailure(hr, E_INVALIDDATA, "Image list: '%ls', item %u didn't specify an image.", pThemeImageList->sczName, i);
            }

            if (0 == i)
            {
                pThemeImageList->hImageList = ImageList_Create(pBitmap->GetWidth(), pBitmap->GetHeight(), ILC_COLOR24, dwImageCount, 0);
                ThmExitOnNullWithLastError(pThemeImageList->hImageList, hr, "Failed to create image list.");
            }

            hr = GdipBitmapToGdiBitmap(pBitmap, &hImage);
            ThmExitOnFailure(hr, "Failed to convert bitmap for CommandLink.");

            iRetVal = ImageList_Add(pThemeImageList->hImageList, hImage, NULL);
            if (-1 == iRetVal)
            {
                ThmExitWithLastError(hr, "Failed to add image %u to image list.", i);
            }

            ++i;
            ReleaseNullObject(pixnImage);
        }

        ReleaseNullObject(pixnlImages);
        ReleaseNullObject(pixnImageList);
    }

LExit:
    if (hImage)
    {
        ::DeleteObject(hImage);
    }
    if (pBitmap)
    {
        delete pBitmap;
    }
    ReleaseObject(pixnlImageLists);
    ReleaseObject(pixnImageList);
    ReleaseObject(pixnlImages);
    ReleaseObject(pixnImage);

    return hr;
}

static void GetControls(
    __in THEME* pTheme,
    __in_opt THEME_CONTROL* pParentControl,
    __out DWORD** ppcControls,
    __out THEME_CONTROL*** pprgControls
    )
{
    if (pParentControl)
    {
        *ppcControls = &pParentControl->cControls;
        *pprgControls = &pParentControl->rgControls;
    }
    else
    {
        *ppcControls = &pTheme->cControls;
        *pprgControls = &pTheme->rgControls;
    }
}

static void GetControls(
    __in const THEME* pTheme,
    __in_opt const THEME_CONTROL* pParentControl,
    __out DWORD& cControls,
    __out THEME_CONTROL*& rgControls
    )
{
    if (pParentControl)
    {
        cControls = pParentControl->cControls;
        rgControls = pParentControl->rgControls;
    }
    else
    {
        cControls = pTheme->cControls;
        rgControls = pTheme->rgControls;
    }
}

static HRESULT ParseControls(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme,
    __in_opt THEME_CONTROL* pParentControl,
    __in_opt THEME_PAGE* pPage,
    __in_opt LPCWSTR wzControlNames
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixn = NULL;
    BSTR bstrType = NULL;
    DWORD cNewControls = 0;
    DWORD iControl = 0;
    DWORD* pcControls = NULL;
    THEME_CONTROL** prgControls = NULL;

    GetControls(pTheme, pParentControl, &pcControls, &prgControls);

    hr = ParseRadioButtons(hModule, wzRelativePath, pElement, pTheme, pParentControl, pPage);
    ThmExitOnFailure(hr, "Failed to parse radio buttons.");

    hr = XmlSelectNodes(pElement, wzControlNames ? wzControlNames : ALL_CONTROL_NAMES, &pixnl);
    ThmExitOnFailure(hr, "Failed to find control elements.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&cNewControls));
    ThmExitOnFailure(hr, "Failed to count the number of theme controls.");

    if (!cNewControls)
    {
        ExitFunction1(hr = S_OK);
    }

    hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(prgControls), *pcControls, cNewControls, sizeof(THEME_CONTROL), 0);
    ThmExitOnFailure(hr, "Failed to reallocate theme controls.");

    cNewControls += *pcControls;

    if (pPage)
    {
        pPage->cControlIndices += cNewControls;
    }

    iControl = *pcControls;
    *pcControls = cNewControls;

    while (S_OK == (hr = XmlNextElement(pixnl, &pixn, &bstrType)))
    {
        THEME_CONTROL_TYPE type = THEME_CONTROL_TYPE_UNKNOWN;

        if (!bstrType)
        {
            hr = E_UNEXPECTED;
            ThmExitOnFailure(hr, "Null element encountered!");
        }

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Billboard", -1))
        {
            type = THEME_CONTROL_TYPE_BILLBOARD;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Button", -1))
        {
            type = THEME_CONTROL_TYPE_BUTTON;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Checkbox", -1))
        {
            type = THEME_CONTROL_TYPE_CHECKBOX;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Combobox", -1))
        {
            type = THEME_CONTROL_TYPE_COMBOBOX;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"CommandLink", -1))
        {
            type = THEME_CONTROL_TYPE_COMMANDLINK;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Editbox", -1))
        {
            type = THEME_CONTROL_TYPE_EDITBOX;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Hyperlink", -1))
        {
            type = THEME_CONTROL_TYPE_HYPERLINK;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Hypertext", -1))
        {
            type = THEME_CONTROL_TYPE_HYPERTEXT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"ImageControl", -1))
        {
            type = THEME_CONTROL_TYPE_IMAGE;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Label", -1))
        {
            type = THEME_CONTROL_TYPE_LABEL;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"ListView", -1))
        {
            type = THEME_CONTROL_TYPE_LISTVIEW;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Panel", -1))
        {
            type = THEME_CONTROL_TYPE_PANEL;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Progressbar", -1))
        {
            type = THEME_CONTROL_TYPE_PROGRESSBAR;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Richedit", -1))
        {
            type = THEME_CONTROL_TYPE_RICHEDIT;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Static", -1))
        {
            type = THEME_CONTROL_TYPE_STATIC;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"Tabs", -1))
        {
            type = THEME_CONTROL_TYPE_TAB;
        }
        else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"TreeView", -1))
        {
            type = THEME_CONTROL_TYPE_TREEVIEW;
        }

        if (THEME_CONTROL_TYPE_UNKNOWN != type)
        {
            THEME_CONTROL* pControl = *prgControls + iControl;
            pControl->type = type;

            hr = ParseControl(hModule, wzRelativePath, pixn, bstrType, pTheme, pControl, pPage);
            ThmExitOnFailure(hr, "Failed to parse control.");

            if (pPage)
            {
                pControl->wPageId = pPage->wId;
            }

            ++iControl;
        }

        ReleaseNullBSTR(bstrType);
        ReleaseNullObject(pixn);
    }
    ThmExitOnFailure(hr, "Failed to enumerate all controls.");

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

    AssertSz(iControl == cNewControls, "The number of parsed controls didn't match the number of expected controls.");

LExit:
    ReleaseBSTR(bstrType);
    ReleaseObject(pixn);
    ReleaseObject(pixnl);

    return hr;
}


static HRESULT ParseControl(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pixn,
    __in_z LPCWSTR wzElementName,
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl,
    __in_opt THEME_PAGE* pPage
    )
{
    HRESULT hr = S_OK;
    BOOL fXmlFound = FALSE;
    DWORD dwValue = 0;
    int nValue = 0;
    BOOL fValue = FALSE;
    BSTR bstrText = NULL;
    BOOL fAnyTextChildren = FALSE;
    BOOL fAnyNoteChildren = FALSE;

    InitializeThemeControl(pTheme, pControl);

    hr = XmlGetAttributeEx(pixn, L"Name", &pControl->sczName);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control Name attribute.");

    hr = XmlGetAttributeEx(pixn, L"EnableCondition", &pControl->sczEnableCondition);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' EnableCondition attribute.", pControl->sczName);

    hr = XmlGetAttributeEx(pixn, L"VisibleCondition", &pControl->sczVisibleCondition);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' VisibleCondition attribute.", pControl->sczName);

    hr = GetAttributeCoordinateOrDimension(pixn, L"X", &nValue);
    ThmExitOnRequiredXmlQueryFailure(hr, "Failed to find control '%ls' X attribute.", pControl->sczName);

    pControl->nX = pControl->nDefaultDpiX = nValue;

    hr = GetAttributeCoordinateOrDimension(pixn, L"Y", &nValue);
    ThmExitOnRequiredXmlQueryFailure(hr, "Failed to find control '%ls' Y attribute.", pControl->sczName);

    pControl->nY = pControl->nDefaultDpiY = nValue;

    hr = GetAttributeCoordinateOrDimension(pixn, L"Height", &nValue);
    ThmExitOnRequiredXmlQueryFailure(hr, "Failed to find control '%ls' Height attribute.", pControl->sczName);

    pControl->nHeight = pControl->nDefaultDpiHeight = nValue;

    hr = GetAttributeCoordinateOrDimension(pixn, L"Width", &nValue);
    ThmExitOnRequiredXmlQueryFailure(hr, "Failed to find control '%ls' Width attribute.", pControl->sczName);

    pControl->nWidth = pControl->nDefaultDpiWidth = nValue;

    switch (pControl->type)
    {
    case THEME_CONTROL_TYPE_COMMANDLINK:
        hr = ParseCommandLinkImage(hModule, wzRelativePath, pixn, pControl);
        ThmExitOnFailure(hr, "Failed while parsing CommandLink '%ls' image.", pControl->sczName);
        break;
    case THEME_CONTROL_TYPE_BUTTON:
        hr = ParseButtonImages(hModule, wzRelativePath, pTheme, pixn, pControl);
        ThmExitOnFailure(hr, "Failed while parsing Button '%ls' images.", pControl->sczName);
        break;
    case THEME_CONTROL_TYPE_IMAGE:
        hr = ParseOwnerDrawImage(hModule, wzRelativePath, pTheme, pixn, wzElementName, pControl, &pControl->Image.imageRef);
        ThmExitOnFailure(hr, "Failed while parsing ImageControl '%ls' image.", pControl->sczName);
        break;
    case THEME_CONTROL_TYPE_PROGRESSBAR:
        hr = ParseProgressBarImages(hModule, wzRelativePath, pTheme, pixn, pControl);
        ThmExitOnFailure(hr, "Failed while parsing Progressbar '%ls' images.", pControl->sczName);
        break;
    default:
        ThmExitOnUnexpectedAttribute(hr, pixn, wzElementName, L"ImageId");
        ThmExitOnUnexpectedAttribute(hr, pixn, wzElementName, L"ImageFile");
        ThmExitOnUnexpectedAttribute(hr, pixn, wzElementName, L"ImageResource");
        ThmExitOnUnexpectedAttribute(hr, pixn, wzElementName, L"SourceX");
        ThmExitOnUnexpectedAttribute(hr, pixn, wzElementName, L"SourceY");
        break;
    }


    hr = GetAttributeFontId(pTheme, pixn, L"FontId", &pControl->dwFontId);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' FontId attribute.", pControl->sczName);

    // Parse the optional window style.
    hr = XmlGetAttributeNumberBase(pixn, L"HexStyle", 16, &pControl->dwStyle);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' HexStyle attribute.", pControl->sczName);

    // Parse the tabstop bit "shortcut nomenclature", this could have been set with the style above.
    hr = XmlGetYesNoAttribute(pixn, L"TabStop", &fValue);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' TabStop attribute.", pControl->sczName);

    if (fXmlFound && fValue)
    {
        pControl->dwStyle |= WS_TABSTOP;
    }

    hr = XmlGetYesNoAttribute(pixn, L"Visible", &fValue);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' Visible attribute.", pControl->sczName);

    if (fXmlFound && fValue)
    {
        pControl->dwStyle |= WS_VISIBLE;
    }

    hr = XmlGetYesNoAttribute(pixn, L"HideWhenDisabled", &fValue);
    ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' HideWhenDisabled attribute.", pControl->sczName);

    if (fXmlFound && fValue)
    {
        pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_HIDE_WHEN_DISABLED;
    }

    hr = ParseActions(pixn, pControl);
    ThmExitOnFailure(hr, "Failed to parse action nodes of the control '%ls'.", pControl->sczName);

    hr = ParseText(pixn, pControl, &fAnyTextChildren);
    ThmExitOnFailure(hr, "Failed to parse text nodes of the control '%ls'.", pControl->sczName);

    hr = ParseTooltips(pixn, pControl, &fAnyTextChildren);
    ThmExitOnFailure(hr, "Failed to parse control '%ls' Tooltip.", pControl->sczName);

    if (THEME_CONTROL_TYPE_COMMANDLINK == pControl->type)
    {
        hr = ParseNotes(pixn, pControl, &fAnyNoteChildren);
        ThmExitOnFailure(hr, "Failed to parse note text nodes of the control '%ls'.", pControl->sczName);
    }

    if (!fAnyTextChildren && !fAnyNoteChildren)
    {
        hr = XmlGetAttributeUInt32(pixn, L"StringId", &dwValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' StringId attribute.", pControl->sczName);

        if (fXmlFound)
        {
            pControl->uStringId = dwValue;
        }
        else
        {
            // Billboards and panels have child elements and we don't want to pick up child element text in the parents.
            if (THEME_CONTROL_TYPE_BILLBOARD != pControl->type && THEME_CONTROL_TYPE_PANEL != pControl->type)
            {
                hr = XmlGetText(pixn, &bstrText);
                ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get control '%ls' inner text.", pControl->sczName);

                if (fXmlFound)
                {
                    hr = StrAllocString(&pControl->sczText, bstrText, 0);
                    ThmExitOnFailure(hr, "Failed to copy control text.");

                    ReleaseNullBSTR(bstrText);
                }
            }
        }
    }

    if (THEME_CONTROL_TYPE_BILLBOARD == pControl->type)
    {
        hr = XmlGetYesNoAttribute(pixn, L"Loop", &pControl->fBillboardLoops);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' Billboard/@Loop attribute.", pControl->sczName);

        hr = XmlGetAttributeUInt16(pixn, L"Interval", &pControl->wBillboardInterval);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' Billboard/@Interval attribute.", pControl->sczName);

        if (!pControl->wBillboardInterval)
        {
            pControl->wBillboardInterval = 5000;
        }

        hr = ParseBillboardPanels(hModule, wzRelativePath, pixn, pTheme, pControl, pPage);
        ThmExitOnFailure(hr, "Failed to parse billboard '%ls' children.", pControl->sczName);
    }
    else if (THEME_CONTROL_TYPE_EDITBOX == pControl->type)
    {
        hr = XmlGetYesNoAttribute(pixn, L"FileSystemAutoComplete", &fValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' Editbox/@FileSystemAutoComplete attribute.", pControl->sczName);

        if (fXmlFound && fValue)
        {
            pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_FILESYSTEM_AUTOCOMPLETE;
        }
    }
    else if (THEME_CONTROL_TYPE_HYPERLINK == pControl->type || THEME_CONTROL_TYPE_BUTTON == pControl->type)
    {
        hr = GetAttributeFontId(pTheme, pixn, L"HoverFontId", &pControl->dwFontHoverId);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' HoverFontId attribute.", pControl->sczName);

        hr = GetAttributeFontId(pTheme, pixn, L"SelectedFontId", &pControl->dwFontSelectedId);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying control '%ls' SelectedFontId attribute.", pControl->sczName);
    }
    else if (THEME_CONTROL_TYPE_LABEL == pControl->type)
    {
        hr = XmlGetYesNoAttribute(pixn, L"Center", &fValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' Label/@Center attribute.", pControl->sczName);

        if (fXmlFound && fValue)
        {
            pControl->dwStyle |= SS_CENTER;
        }

        hr = XmlGetYesNoAttribute(pixn, L"DisablePrefix", &fValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' Label/@DisablePrefix attribute.", pControl->sczName);

        if (fXmlFound && fValue)
        {
            pControl->dwStyle |= SS_NOPREFIX;
        }
    }
    else if (THEME_CONTROL_TYPE_LISTVIEW == pControl->type)
    {
        // Parse the optional extended window style.
        hr = XmlGetAttributeNumberBase(pixn, L"HexExtendedStyle", 16, &pControl->dwExtendedStyle);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' ListView/@HexExtendedStyle attribute.", pControl->sczName);

        hr = XmlGetAttribute(pixn, L"ImageList", &bstrText);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' ListView/@ImageList attribute.", pControl->sczName);

        if (fXmlFound)
        {
            hr = FindImageList(pTheme, bstrText, &pControl->ListView.rghImageList[0]);
            ThmExitOnFailure(hr, "Failed to find image list %ls while setting ImageList for ListView.", bstrText);
        }

        hr = XmlGetAttribute(pixn, L"ImageListSmall", &bstrText);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' ListView/@ImageListSmall attribute.", pControl->sczName);

        if (fXmlFound)
        {
            hr = FindImageList(pTheme, bstrText, &pControl->ListView.rghImageList[1]);
            ThmExitOnFailure(hr, "Failed to find image list %ls while setting ImageListSmall for ListView.", bstrText);
        }

        hr = XmlGetAttribute(pixn, L"ImageListState", &bstrText);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' ListView/@ImageListState attribute.", pControl->sczName);

        if (fXmlFound)
        {
            hr = FindImageList(pTheme, bstrText, &pControl->ListView.rghImageList[2]);
            ThmExitOnFailure(hr, "Failed to find image list %ls while setting ImageListState for ListView.", bstrText);
        }

        hr = XmlGetAttribute(pixn, L"ImageListGroupHeader", &bstrText);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' ListView/@ImageListGroupHeader attribute.", pControl->sczName);

        if (fXmlFound)
        {
            hr = FindImageList(pTheme, bstrText, &pControl->ListView.rghImageList[3]);
            ThmExitOnFailure(hr, "Failed to find image list %ls while setting ImageListGroupHeader for ListView.", bstrText);
        }

        hr = ParseColumns(pixn, pControl);
        ThmExitOnFailure(hr, "Failed to parse columns.");
    }
    else if (THEME_CONTROL_TYPE_PANEL == pControl->type)
    {
        hr = ParseControls(hModule, wzRelativePath, pixn, pTheme, pControl, pPage, PANEL_CHILD_CONTROL_NAMES);
        ThmExitOnFailure(hr, "Failed to parse panel children.");
    }
    else if (THEME_CONTROL_TYPE_RADIOBUTTON == pControl->type)
    {
        hr = XmlGetAttributeEx(pixn, L"Value", &pControl->sczValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' RadioButton/@Value attribute.", pControl->sczName);
    }
    else if (THEME_CONTROL_TYPE_TAB == pControl->type)
    {
        hr = ParseTabs(pixn, pControl);
        ThmExitOnFailure(hr, "Failed to parse tabs");
    }
    else if (THEME_CONTROL_TYPE_TREEVIEW == pControl->type)
    {
        pControl->dwStyle |= TVS_DISABLEDRAGDROP;

        hr = XmlGetYesNoAttribute(pixn, L"EnableDragDrop", &fValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' TreeView/@EnableDragDrop attribute.", pControl->sczName);

        if (fXmlFound && fValue)
        {
            pControl->dwStyle &= ~TVS_DISABLEDRAGDROP;
        }

        hr = XmlGetYesNoAttribute(pixn, L"FullRowSelect", &fValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' TreeView/@FullRowSelect attribute.", pControl->sczName);

        if (fXmlFound && fValue)
        {
            pControl->dwStyle |= TVS_FULLROWSELECT;
        }

        hr = XmlGetYesNoAttribute(pixn, L"HasButtons", &fValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' TreeView/@HasButtons attribute.", pControl->sczName);

        if (fXmlFound && fValue)
        {
            pControl->dwStyle |= TVS_HASBUTTONS;
        }

        hr = XmlGetYesNoAttribute(pixn, L"AlwaysShowSelect", &fValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' TreeView/@AlwaysShowSelect attribute.", pControl->sczName);

        if (fXmlFound && fValue)
        {
            pControl->dwStyle |= TVS_SHOWSELALWAYS;
        }

        hr = XmlGetYesNoAttribute(pixn, L"LinesAtRoot", &fValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' TreeView/@LinesAtRoot attribute.", pControl->sczName);

        if (fXmlFound && fValue)
        {
            pControl->dwStyle |= TVS_LINESATROOT;
        }

        hr = XmlGetYesNoAttribute(pixn, L"HasLines", &fValue);
        ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed when querying '%ls' TreeView/@HasLines attribute.", pControl->sczName);

        if (fXmlFound && fValue)
        {
            pControl->dwStyle |= TVS_HASLINES;
        }
    }

LExit:
    ReleaseBSTR(bstrText);

    return hr;
}

static void InitializeThemeControl(
    THEME* pTheme,
    THEME_CONTROL* pControl
    )
{
    pControl->dwFontHoverId = THEME_INVALID_ID;
    pControl->dwFontId = THEME_INVALID_ID;
    pControl->dwFontSelectedId = THEME_INVALID_ID;
    pControl->uStringId = UINT_MAX;
    pControl->pTheme = pTheme;
}


static HRESULT ParseActions(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrType = NULL;

    hr = XmlSelectNodes(pixn, L"BrowseDirectoryAction|ChangePageAction|CloseWindowAction", &pixnl);
    ThmExitOnFailure(hr, "Failed to select child action nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pControl->cActions));
    ThmExitOnFailure(hr, "Failed to count the number of action nodes.");

    if (0 < pControl->cActions)
    {
        MemAllocArray(reinterpret_cast<LPVOID*>(&pControl->rgActions), sizeof(THEME_ACTION), pControl->cActions);
        ThmExitOnNull(pControl->rgActions, hr, E_OUTOFMEMORY, "Failed to allocate THEME_ACTION structs.");

        i = 0;
        while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, &bstrType)))
        {
            if (!bstrType)
            {
                hr = E_UNEXPECTED;
                ThmExitOnFailure(hr, "Null element encountered!");
            }

            THEME_ACTION* pAction = pControl->rgActions + i;

            if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"BrowseDirectoryAction", -1))
            {
                pAction->type = THEME_ACTION_TYPE_BROWSE_DIRECTORY;

                hr = XmlGetAttributeEx(pixnChild, L"VariableName", &pAction->BrowseDirectory.sczVariableName);
                ThmExitOnFailure(hr, "Failed when querying BrowseDirectoryAction/@VariableName attribute.");
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"ChangePageAction", -1))
            {
                pAction->type = THEME_ACTION_TYPE_CHANGE_PAGE;

                hr = XmlGetAttributeEx(pixnChild, L"Page", &pAction->ChangePage.sczPageName);
                ThmExitOnFailure(hr, "Failed when querying ChangePageAction/@Page attribute.");

                hr = XmlGetYesNoAttribute(pixnChild, L"Cancel", &pAction->ChangePage.fCancel);
                if (E_NOTFOUND != hr)
                {
                    ThmExitOnFailure(hr, "Failed when querying ChangePageAction/@Cancel attribute.");
                }
            }
            else if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, bstrType, -1, L"CloseWindowAction", -1))
            {
                pAction->type = THEME_ACTION_TYPE_CLOSE_WINDOW;
            }
            else
            {
                hr = E_UNEXPECTED;
                ThmExitOnFailure(hr, "Unexpected element encountered: %ls", bstrType);
            }

            hr = XmlGetAttributeEx(pixnChild, L"Condition", &pAction->sczCondition);
            if (E_NOTFOUND != hr)
            {
                ThmExitOnFailure(hr, "Failed when querying %ls/@Condition attribute.", bstrType);
            }

            if (!pAction->sczCondition)
            {
                if (pControl->pDefaultAction)
                {
                    hr = E_INVALIDDATA;
                    ThmExitOnFailure(hr, "Control '%ls' has multiple actions without a condition.", pControl->sczName);
                }

                pControl->pDefaultAction = pAction;
            }

            ++i;
            ReleaseNullBSTR(bstrType);
            ReleaseNullObject(pixnChild);
        }
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrType);

    return hr;
}


static HRESULT ParseBillboardPanels(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pElement,
    __in THEME* pTheme,
    __in THEME_CONTROL* pParentControl,
    __in_opt THEME_PAGE* pPage
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    DWORD dwValue = 0;
    THEME_CONTROL* pControl = NULL;

    hr = XmlSelectNodes(pElement, L"Panel", &pixnl);
    ThmExitOnFailure(hr, "Failed to select billboard child nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&dwValue));
    ThmExitOnFailure(hr, "Failed to count the number of billboard panel nodes.");

    if (!dwValue)
    {
        ThmExitWithRootFailure(hr, E_INVALIDDATA, "Billboard must have at least one Panel.");
    }

    hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(&pParentControl->rgControls), pParentControl->cControls, dwValue, sizeof(THEME_CONTROL), 0);
    ThmExitOnFailure(hr, "Failed to ensure theme control array size for BillboardPanels.");

    while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
    {
        hr = pixnChild->get_attributes(&pixnnmAttributes);
        ThmExitOnFailure(hr, "Failed to get attributes for billboard panel.");

        hr = pixnnmAttributes->get_length(reinterpret_cast<long*>(&dwValue));
        ThmExitOnFailure(hr, "Failed to count attributes for billboard panel.");

        if (dwValue)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "Billboard panels cannot contain attributes.");
        }

        pControl = pParentControl->rgControls + pParentControl->cControls;
        pParentControl->cControls += 1;
        pControl->type = THEME_CONTROL_TYPE_PANEL;
        InitializeThemeControl(pTheme, pControl);

        if (pPage)
        {
            pControl->wPageId = pPage->wId;
        }

        hr = ParseControls(hModule, wzRelativePath, pixnChild, pTheme, pControl, pPage, PANEL_CHILD_CONTROL_NAMES);
        ThmExitOnFailure(hr, "Failed to parse control.");

        ReleaseNullObject(pixnnmAttributes);
        ReleaseNullObject(pixnChild);
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnnmAttributes);
    ReleaseObject(pixnChild);

    return hr;
}


static HRESULT ParseColumns(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrText = NULL;
    int nValue = 0;
    BOOL fXmlFound = FALSE;

    hr = XmlSelectNodes(pixn, L"Column", &pixnl);
    ThmExitOnFailure(hr, "Failed to select child column nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pControl->ListView.cColumns));
    ThmExitOnFailure(hr, "Failed to count the number of control columns.");

    if (0 < pControl->ListView.cColumns)
    {
        hr = MemAllocArray(reinterpret_cast<LPVOID*>(&pControl->ListView.ptcColumns), sizeof(THEME_COLUMN), pControl->ListView.cColumns);
        ThmExitOnFailure(hr, "Failed to allocate column structs.");

        i = 0;
        while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
        {
            THEME_COLUMN* pColumn = pControl->ListView.ptcColumns + i;

            hr = XmlGetText(pixnChild, &bstrText);
            ThmExitOnFailure(hr, "Failed to get inner text of column element.");

            hr = GetAttributeCoordinateOrDimension(pixnChild, L"Width", &nValue);
            ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get column width attribute.");

            if (!fXmlFound)
            {
                nValue = 100;
            }

            pColumn->nBaseWidth = pColumn->nDefaultDpiBaseWidth = nValue;

            hr = XmlGetYesNoAttribute(pixnChild, L"Expands", &pColumn->fExpands);
            ThmExitOnOptionalXmlQueryFailure(hr, fXmlFound, "Failed to get expands attribute.");

            hr = StrAllocString(&pColumn->pszName, bstrText, 0);
            ThmExitOnFailure(hr, "Failed to copy column name.");

            ++i;
            ReleaseNullBSTR(bstrText);
            ReleaseNullObject(pixnChild);
        }
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrText);

    return hr;
}


static HRESULT ParseRadioButtons(
    __in_opt HMODULE hModule,
    __in_opt LPCWSTR wzRelativePath,
    __in IXMLDOMNode* pixn,
    __in THEME* pTheme,
    __in_opt THEME_CONTROL* pParentControl,
    __in THEME_PAGE* pPage
    )
{
    HRESULT hr = S_OK;
    DWORD cRadioButtons = 0;
    IXMLDOMNodeList* pixnlRadioButtons = NULL;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnRadioButtons = NULL;
    IXMLDOMNode* pixnChild = NULL;
    LPWSTR sczName = NULL;
    THEME_CONTROL* pControl = NULL;
    BOOL fFirst = FALSE;
    DWORD* pcControls = NULL;
    THEME_CONTROL** prgControls = NULL;

    GetControls(pTheme, pParentControl, &pcControls, &prgControls);

    hr = XmlSelectNodes(pixn, L"RadioButtons", &pixnlRadioButtons);
    ThmExitOnFailure(hr, "Failed to select RadioButtons nodes.");

    while (S_OK == (hr = XmlNextElement(pixnlRadioButtons, &pixnRadioButtons, NULL)))
    {
        hr = XmlGetAttributeEx(pixnRadioButtons, L"Name", &sczName);
        if (E_NOTFOUND == hr)
        {
            hr = S_OK;
        }
        ThmExitOnFailure(hr, "Failed when querying RadioButtons Name.");

        hr = XmlSelectNodes(pixnRadioButtons, L"RadioButton", &pixnl);
        ThmExitOnFailure(hr, "Failed to select RadioButton nodes.");

        hr = pixnl->get_length(reinterpret_cast<long*>(&cRadioButtons));
        ThmExitOnFailure(hr, "Failed to count the number of RadioButton nodes.");

        if (!cRadioButtons)
        {
            ThmExitWithRootFailure(hr, E_INVALIDDATA, "RadioButtons must have at least one RadioButton.");
        }

        if (pPage)
        {
            pPage->cControlIndices += cRadioButtons;
        }

        hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(prgControls), *pcControls, cRadioButtons, sizeof(THEME_CONTROL), 0);
        ThmExitOnFailure(hr, "Failed to ensure theme control array size for RadioButtons.");

        fFirst = TRUE;

        while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
        {
            pControl = *prgControls + *pcControls;
            pControl->type = THEME_CONTROL_TYPE_RADIOBUTTON;
            *pcControls += 1;

            hr = ParseControl(hModule, wzRelativePath, pixnChild, L"RadioButton", pTheme, pControl, pPage);
            ThmExitOnFailure(hr, "Failed to parse control.");

            if (fFirst)
            {
                pControl->dwStyle |= WS_GROUP;
                fFirst = FALSE;
            }

            hr = StrAllocString(&pControl->sczVariable, sczName, 0);
            ThmExitOnFailure(hr, "Failed to copy radio button variable.");

            if (pPage)
            {
                pControl->wPageId = pPage->wId;
            }

            ReleaseNullObject(pixnChild);
        }

        if (!fFirst)
        {
            pControl->fLastRadioButton = TRUE;
        }

        ReleaseNullObject(pixnl);
        ReleaseNullObject(pixnRadioButtons);
    }

LExit:
    ReleaseStr(sczName);
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseObject(pixnlRadioButtons);
    ReleaseObject(pixnRadioButtons);

    return hr;
}


static HRESULT ParseTabs(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrText = NULL;

    hr = XmlSelectNodes(pixn, L"Tab", &pixnl);
    ThmExitOnFailure(hr, "Failed to select child tab nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pControl->cTabs));
    ThmExitOnFailure(hr, "Failed to count the number of tabs.");

    if (0 < pControl->cTabs)
    {
        hr = MemAllocArray(reinterpret_cast<LPVOID*>(&pControl->pttTabs), sizeof(THEME_TAB), pControl->cTabs);
        ThmExitOnFailure(hr, "Failed to allocate tab structs.");

        i = 0;
        while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
        {
            hr = XmlGetText(pixnChild, &bstrText);
            ThmExitOnFailure(hr, "Failed to get inner text of tab element.");

            hr = StrAllocString(&(pControl->pttTabs[i].pszName), bstrText, 0);
            ThmExitOnFailure(hr, "Failed to copy tab name.");

            ++i;
            ReleaseNullBSTR(bstrText);
        }
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrText);

    return hr;
}


static HRESULT ParseText(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl,
    __inout BOOL* pfAnyChildren
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrText = NULL;

    hr = XmlSelectNodes(pixn, L"Text", &pixnl);
    ThmExitOnFailure(hr, "Failed to select child Text nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pControl->cConditionalText));
    ThmExitOnFailure(hr, "Failed to count the number of Text nodes.");

    *pfAnyChildren |= 0 < pControl->cConditionalText;

    if (0 < pControl->cConditionalText)
    {
        MemAllocArray(reinterpret_cast<LPVOID*>(&pControl->rgConditionalText), sizeof(THEME_CONDITIONAL_TEXT), pControl->cConditionalText);
        ThmExitOnNull(pControl->rgConditionalText, hr, E_OUTOFMEMORY, "Failed to allocate THEME_CONDITIONAL_TEXT structs.");

        i = 0;
        while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
        {
            THEME_CONDITIONAL_TEXT* pConditionalText = pControl->rgConditionalText + i;

            hr = XmlGetAttributeEx(pixnChild, L"Condition", &pConditionalText->sczCondition);
            if (E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ThmExitOnFailure(hr, "Failed when querying Text/@Condition attribute.");

            hr = XmlGetText(pixnChild, &bstrText);
            ThmExitOnFailure(hr, "Failed to get inner text of Text element.");

            if (S_OK == hr)
            {
                if (pConditionalText->sczCondition)
                {
                    hr = StrAllocString(&pConditionalText->sczText, bstrText, 0);
                    ThmExitOnFailure(hr, "Failed to copy text to conditional text.");

                    ++i;
                }
                else
                {
                    if (pControl->sczText)
                    {
                        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
                        ThmExitOnFailure(hr, "Unconditional text for the '%ls' control is specified multiple times.", pControl->sczName);
                    }

                    hr = StrAllocString(&pControl->sczText, bstrText, 0);
                    ThmExitOnFailure(hr, "Failed to copy text to control.");

                    // Unconditional text entries aren't stored in the conditional text list.
                    --pControl->cConditionalText;
                }
            }

            ReleaseNullBSTR(bstrText);
        }
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrText);

    return hr;
}


static HRESULT ParseTooltips(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl,
    __inout BOOL* pfAnyChildren
    )
{
    HRESULT hr = S_OK;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrText = NULL;

    hr = XmlSelectSingleNode(pixn, L"Tooltip", &pixnChild);
    ThmExitOnFailure(hr, "Failed to select child Tooltip node.");

    if (S_OK == hr)
    {
        *pfAnyChildren |= TRUE;

        hr = XmlGetText(pixnChild, &bstrText);
        ThmExitOnFailure(hr, "Failed to get inner text of Tooltip element.");

        if (S_OK == hr)
        {
            hr = StrAllocString(&pControl->sczTooltip, bstrText, 0);
            ThmExitOnFailure(hr, "Failed to copy tooltip text to control.");
        }
    }

LExit:
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrText);

    return hr;
}


static HRESULT ParseUnexpectedAttribute(
    __in IXMLDOMNode* pixn,
    __in_z LPCWSTR wzElementName,
    __in_z LPCWSTR wzAttribute
    )
{
    HRESULT hr = S_OK;
    BSTR bstr = NULL;

    hr = XmlGetAttribute(pixn, wzAttribute, &bstr);
    ThmExitOnFailure(hr, "Failed to get attribute %ls/@%ls", wzElementName, wzAttribute);

    if (S_OK == hr)
    {
        ThmExitOnRootFailure(hr = E_INVALIDDATA, "Element '%ls' has unexpected attribute '%ls', value: %ls.", wzElementName, wzAttribute, bstr);
    }

    hr = S_OK;

LExit:
    ReleaseBSTR(bstr);

    return hr;
}


static HRESULT ParseNotes(
    __in IXMLDOMNode* pixn,
    __in THEME_CONTROL* pControl,
    __out BOOL* pfAnyChildren
    )
{
    HRESULT hr = S_OK;
    DWORD i = 0;
    IXMLDOMNodeList* pixnl = NULL;
    IXMLDOMNode* pixnChild = NULL;
    BSTR bstrText = NULL;

    hr = XmlSelectNodes(pixn, L"Note", &pixnl);
    ThmExitOnFailure(hr, "Failed to select child Note nodes.");

    hr = pixnl->get_length(reinterpret_cast<long*>(&pControl->CommandLink.cConditionalNotes));
    ThmExitOnFailure(hr, "Failed to count the number of Note nodes.");

    if (pfAnyChildren)
    {
        *pfAnyChildren = 0 < pControl->CommandLink.cConditionalNotes;
    }

    if (0 < pControl->CommandLink.cConditionalNotes)
    {
        MemAllocArray(reinterpret_cast<LPVOID*>(&pControl->CommandLink.rgConditionalNotes), sizeof(THEME_CONDITIONAL_TEXT), pControl->CommandLink.cConditionalNotes);
        ThmExitOnNull(pControl->CommandLink.rgConditionalNotes, hr, E_OUTOFMEMORY, "Failed to allocate note THEME_CONDITIONAL_TEXT structs.");

        i = 0;
        while (S_OK == (hr = XmlNextElement(pixnl, &pixnChild, NULL)))
        {
            THEME_CONDITIONAL_TEXT* pConditionalNote = pControl->CommandLink.rgConditionalNotes + i;

            hr = XmlGetAttributeEx(pixnChild, L"Condition", &pConditionalNote->sczCondition);
            if (E_NOTFOUND == hr)
            {
                hr = S_OK;
            }
            ThmExitOnFailure(hr, "Failed when querying Note/@Condition attribute.");

            hr = XmlGetText(pixnChild, &bstrText);
            ThmExitOnFailure(hr, "Failed to get inner text of Note element.");

            if (S_OK == hr)
            {
                if (pConditionalNote->sczCondition)
                {
                    hr = StrAllocString(&pConditionalNote->sczText, bstrText, 0);
                    ThmExitOnFailure(hr, "Failed to copy text to conditional note text.");

                    ++i;
                }
                else
                {
                    if (pControl->sczNote)
                    {
                        hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
                        ThmExitOnFailure(hr, "Unconditional note text for the '%ls' control is specified multiple times.", pControl->sczName);
                    }

                    hr = StrAllocString(&pControl->sczNote, bstrText, 0);
                    ThmExitOnFailure(hr, "Failed to copy text to command link control.");

                    // Unconditional note entries aren't stored in the conditional notes list.
                    --pControl->CommandLink.cConditionalNotes;
                }
            }

            ReleaseNullBSTR(bstrText);
        }
    }

LExit:
    ReleaseObject(pixnl);
    ReleaseObject(pixnChild);
    ReleaseBSTR(bstrText);

    return hr;
}


static HRESULT StartBillboard(
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = E_NOTFOUND;
    UINT_PTR idEvent = reinterpret_cast<UINT_PTR>(pControl);

    if (THEME_CONTROL_TYPE_BILLBOARD == pControl->type)
    {
        // kick off
        pControl->dwData = 0;
        OnBillboardTimer(pTheme, pTheme->hwndParent, idEvent);

        if (!::SetTimer(pTheme->hwndParent, idEvent, pControl->wBillboardInterval, NULL))
        {
            ThmExitWithLastError(hr, "Failed to start billboard.");
        }

        hr = S_OK;
    }

LExit:
    return hr;
}


static HRESULT StopBillboard(
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl
    )
{
    HRESULT hr = E_NOTFOUND;
    UINT_PTR idEvent = reinterpret_cast<UINT_PTR>(pControl);

    if (THEME_CONTROL_TYPE_BILLBOARD == pControl->type)
    {
        if (::KillTimer(pTheme->hwndParent, idEvent))
        {
            hr = S_OK;
        }
    }

    return hr;
}

static HRESULT EnsureFontInstance(
    __in THEME* pTheme,
    __in THEME_FONT* pFont,
    __out THEME_FONT_INSTANCE** ppFontInstance
    )
{
    HRESULT hr = S_OK;
    THEME_FONT_INSTANCE* pFontInstance = NULL;
    LOGFONTW lf = { };

    for (DWORD i = 0; i < pFont->cFontInstances; ++i)
    {
        pFontInstance = pFont->rgFontInstances + i;
        if (pTheme->nDpi == pFontInstance->nDpi)
        {
            *ppFontInstance = pFontInstance;
            ExitFunction();
        }
    }

    hr = MemEnsureArraySizeForNewItems(reinterpret_cast<LPVOID*>(&pFont->rgFontInstances), pFont->cFontInstances, 1, sizeof(THEME_FONT_INSTANCE), GROW_FONT_INSTANCES);
    ThmExitOnFailure(hr, "Failed to allocate memory for font instances.");

    pFontInstance = pFont->rgFontInstances + pFont->cFontInstances;
    pFontInstance->nDpi = pTheme->nDpi;

    lf.lfHeight = DpiuScaleValue(pFont->lfHeight, pFontInstance->nDpi);
    lf.lfWeight = pFont->lfWeight;
    lf.lfUnderline = pFont->lfUnderline;
    lf.lfQuality = pFont->lfQuality;

    hr = ::StringCchCopyW(lf.lfFaceName, countof(lf.lfFaceName), pFont->sczFaceName);
    ThmExitOnFailure(hr, "Failed to copy font name to create font.");

    pFontInstance->hFont = ::CreateFontIndirectW(&lf);
    ThmExitOnNull(pFontInstance->hFont, hr, E_OUTOFMEMORY, "Failed to create DPI specific font.");

    ++pFont->cFontInstances;
    *ppFontInstance = pFontInstance;

LExit:
    return hr;
}


static HRESULT FindImageList(
    __in THEME* pTheme,
    __in_z LPCWSTR wzImageListName,
    __out HIMAGELIST *phImageList
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < pTheme->cImageLists; ++i)
    {
        if (CSTR_EQUAL == ::CompareStringW(LOCALE_NEUTRAL, 0, pTheme->rgImageLists[i].sczName, -1, wzImageListName, -1))
        {
            *phImageList = pTheme->rgImageLists[i].hImageList;
            ExitFunction1(hr = S_OK);
        }
    }

    hr = E_NOTFOUND;

LExit:
    return hr;
}


static HRESULT DrawButton(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    const THEME_IMAGE_REFERENCE* pImageRef = NULL;
    BOOL fDrawFocusRect = FALSE;
    int nHeight = pdis->rcItem.bottom - pdis->rcItem.top;
    int nWidth = pdis->rcItem.right - pdis->rcItem.left;

    // "clicked" gets priority
    if (ODS_SELECTED & pdis->itemState)
    {
        pImageRef = pControl->Button.rgImageRef + 2;
    }
    // then hover
    else if (pControl->dwData & THEME_CONTROL_DATA_HOVER)
    {
        pImageRef = pControl->Button.rgImageRef + 1;
    }
    // then focused
    else if ((WS_TABSTOP & ::GetWindowLongPtrW(pdis->hwndItem, GWL_STYLE)) && (ODS_FOCUS & pdis->itemState))
    {
        if (THEME_IMAGE_REFERENCE_TYPE_NONE != pControl->Button.rgImageRef[3].type)
        {
            pImageRef = pControl->Button.rgImageRef + 3;
        }
        else
        {
            fDrawFocusRect = TRUE;
            pImageRef = pControl->Button.rgImageRef;
        }
    }
    else
    {
        pImageRef = pControl->Button.rgImageRef;
    }

    hr = DrawImageReference(pTheme, pImageRef, pdis->hDC, 0, 0, nWidth, nHeight);

    DrawControlText(pTheme, pdis, pControl, TRUE, fDrawFocusRect);

    return hr;
}


static HRESULT DrawHyperlink(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    )
{
    DrawControlText(pTheme, pdis, pControl, FALSE, TRUE);
    return S_OK;
}


static void DrawControlText(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl,
    __in BOOL fCentered,
    __in BOOL fDrawFocusRect
    )
{
    HRESULT hr = S_OK;
    WCHAR wzText[256] = { };
    THEME_FONT* pFont = NULL;
    THEME_FONT_INSTANCE* pFontInstance = NULL;
    HFONT hfPrev = NULL;
    DWORD cchText = ::GetWindowTextW(pdis->hwndItem, wzText, countof(wzText));

    if (cchText)
    {
        if (ODS_SELECTED & pdis->itemState)
        {
            pFont = pTheme->rgFonts + (THEME_INVALID_ID != pControl->dwFontSelectedId ? pControl->dwFontSelectedId : pControl->dwFontId);
        }
        else if (pControl->dwData & THEME_CONTROL_DATA_HOVER)
        {
            pFont = pTheme->rgFonts + (THEME_INVALID_ID != pControl->dwFontHoverId ? pControl->dwFontHoverId : pControl->dwFontId);
        }
        else
        {
            pFont = pTheme->rgFonts + pControl->dwFontId;
        }

        hr = EnsureFontInstance(pTheme, pFont, &pFontInstance);
        if (SUCCEEDED(hr))
        {
            hfPrev = SelectFont(pdis->hDC, pFontInstance->hFont);
        }

        ::DrawTextExW(pdis->hDC, wzText, cchText, &pdis->rcItem, DT_SINGLELINE | (fCentered ? (DT_CENTER | DT_VCENTER) : 0), NULL);

        if (hfPrev)
        {
            SelectFont(pdis->hDC, hfPrev);
        }
    }

    if (fDrawFocusRect && (WS_TABSTOP & ::GetWindowLongPtrW(pdis->hwndItem, GWL_STYLE)) && (ODS_FOCUS & pdis->itemState))
    {
        ::DrawFocusRect(pdis->hDC, &pdis->rcItem);
    }
}


static HRESULT DrawImage(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    int nHeight = pdis->rcItem.bottom - pdis->rcItem.top;
    int nWidth = pdis->rcItem.right - pdis->rcItem.left;

    hr = DrawImageReference(pTheme, &pControl->Image.imageRef, pdis->hDC, 0, 0, nWidth, nHeight);

    return hr;
}

static void GetImageInstance(
    __in THEME* pTheme,
    __in const THEME_IMAGE_REFERENCE* pReference,
    __out const THEME_IMAGE_INSTANCE** ppInstance
    )
{
    switch (pReference->type)
    {
    case THEME_IMAGE_REFERENCE_TYPE_PARTIAL:
    case THEME_IMAGE_REFERENCE_TYPE_COMPLETE:
        if (THEME_INVALID_ID == pReference->dwImageIndex)
        {
            *ppInstance = pTheme->rgStandaloneImages + pReference->dwImageInstanceIndex;
        }
        else
        {
            THEME_IMAGE* pImage = pTheme->rgImages + pReference->dwImageIndex;
            *ppInstance = pImage->rgImageInstances + pReference->dwImageInstanceIndex;
        }
        break;
    default:
        *ppInstance = NULL;
        break;
    }
}

static HRESULT DrawImageReference(
    __in THEME* pTheme,
    __in const THEME_IMAGE_REFERENCE* pReference,
    __in HDC hdc,
    __in int destX,
    __in int destY,
    __in int destWidth,
    __in int destHeight
    )
{
    HRESULT hr = S_OK;
    const THEME_IMAGE_INSTANCE* pImageInstance = NULL;
    int nX = 0;
    int nY = 0;
    int nWidth = 0;
    int nHeight = 0;

    GetImageInstance(pTheme, pReference, &pImageInstance);
    ExitOnNull(pImageInstance, hr, E_INVALIDARG, "Invalid image reference for drawing.");

    switch (pReference->type)
    {
    case THEME_IMAGE_REFERENCE_TYPE_PARTIAL:
        nX = pReference->nX;
        nY = pReference->nY;
        nWidth = pReference->nWidth;
        nHeight = pReference->nHeight;
        break;
    case THEME_IMAGE_REFERENCE_TYPE_COMPLETE:
        nX = 0;
        nY = 0;
        nWidth = pImageInstance->pBitmap->GetWidth();
        nHeight = pImageInstance->pBitmap->GetHeight();
        break;
    default:
        ExitFunction1(hr = E_INVALIDARG); // This should be unreachable because GetImageInstance should have returned null.
    }

    hr = DrawGdipBitmap(hdc, destX, destY, destWidth, destHeight, pImageInstance->pBitmap, nX, nY, nWidth, nHeight);

LExit:
    return hr;
}

static HRESULT DrawGdipBitmap(
    __in HDC hdc,
    __in int destX,
    __in int destY,
    __in int destWidth,
    __in int destHeight,
    __in Gdiplus::Bitmap* pBitmap,
    __in int srcX,
    __in int srcY,
    __in int srcWidth,
    __in int srcHeight
    )
{
    // Note that this only indicates that GDI+ supports transparency from the source image type.
    // Bitmaps with alpha information will return FALSE, while fully opaque PNGs will return TRUE.
    BOOL fTransparency = (pBitmap->GetFlags() & Gdiplus::ImageFlagsHasAlpha) == Gdiplus::ImageFlagsHasAlpha;
    Gdiplus::ImageAttributes attrs;
    Gdiplus::Rect destRect(destX, destY, destWidth, destHeight);
    Gdiplus::Graphics graphics(hdc);
    Gdiplus::Status gs = Gdiplus::Status::Ok;

    // This fixes GDI+ behavior where it badly handles the edges of an image when scaling.
    // This is easily seen when using an image Progressbar that's 4x1 - the progress bar fades to transparent in both directions.
    attrs.SetWrapMode(Gdiplus::WrapMode::WrapModeTileFlipXY);

    // graphics.SmoothingMode is not set because it has no impact on DrawImage.

    // This is the best interpolation mode, and the only one that is decent at downscaling.
    graphics.SetInterpolationMode(Gdiplus::InterpolationMode::InterpolationModeHighQualityBicubic);

    // There's a significant quality improvement when scaling to use the HighQuality pixel offset mode with no measurable difference in performance.
    graphics.SetPixelOffsetMode(Gdiplus::PixelOffsetMode::PixelOffsetModeHighQuality);

    // If there's transparency, make sure that the blending is done with high quality.
    // If not, try to skip blending.
    if (fTransparency)
    {
        graphics.SetCompositingMode(Gdiplus::CompositingMode::CompositingModeSourceOver);
        graphics.SetCompositingQuality(Gdiplus::CompositingQuality::CompositingQualityHighQuality);
    }
    else
    {
        graphics.SetCompositingMode(Gdiplus::CompositingMode::CompositingModeSourceCopy);
        graphics.SetCompositingQuality(Gdiplus::CompositingQuality::CompositingQualityHighSpeed);
    }

    gs = graphics.DrawImage(pBitmap, destRect, srcX, srcY, srcWidth, srcHeight, Gdiplus::Unit::UnitPixel, &attrs);

    return GdipHresultFromStatus(gs);
}


static HRESULT DrawProgressBar(
    __in THEME* pTheme,
    __in DRAWITEMSTRUCT* pdis,
    __in const THEME_CONTROL* pControl
    )
{
    const int nSideWidth = 1;
    HRESULT hr = S_OK;
    WORD wProgressColorIndex = HIWORD(pControl->dwData);
    WORD wProgressPercentage = LOWORD(pControl->dwData);
    const THEME_IMAGE_REFERENCE* pImageRef = pControl->ProgressBar.rgImageRef + wProgressColorIndex;
    const THEME_IMAGE_INSTANCE* pInstance = NULL;
    int nHeight = pdis->rcItem.bottom - pdis->rcItem.top;
    int nSourceHeight = 0;
    int nSourceX = 0;
    int nSourceY = 0;
    int nFillableWidth = pdis->rcItem.right - 2 * nSideWidth;
    int nCenter = nFillableWidth > 0 ? nFillableWidth * wProgressPercentage / 100 : 0;

    if (0 > nFillableWidth)
    {
        ExitFunction1(hr = S_FALSE);
    }

    GetImageInstance(pTheme, pImageRef, &pInstance);
    ExitOnNull(pInstance, hr, E_INVALIDARG, "Invalid image reference for drawing.");

    switch (pImageRef->type)
    {
    case THEME_IMAGE_REFERENCE_TYPE_PARTIAL:
        nSourceHeight = pImageRef->nHeight;
        nSourceX = pImageRef->nX;
        nSourceY = pImageRef->nY;
        break;
    case THEME_IMAGE_REFERENCE_TYPE_COMPLETE:
        nSourceHeight = pInstance->pBitmap->GetHeight();
        nSourceX = 0;
        nSourceY = 0;
        break;
    default:
        ExitFunction1(hr = E_INVALIDARG); // This should be unreachable because GetImageInstance should have returned null.
    }

    // Draw the left side of the progress bar.
    hr = DrawProgressBarImage(pTheme, pInstance, nSourceX, nSourceY, 1, nSourceHeight, pdis->hDC, 0, 0, nSideWidth, nHeight);

    // Draw the filled side of the progress bar, if there is any.
    if (0 < nCenter)
    {
        hr = DrawProgressBarImage(pTheme, pInstance, nSourceX + 1, nSourceY, 1, nSourceHeight, pdis->hDC, nSideWidth, 0, nCenter, nHeight);
    }

    // Draw the unfilled side of the progress bar, if there is any.
    if (nCenter < nFillableWidth)
    {
        hr = DrawProgressBarImage(pTheme, pInstance, nSourceX + 2, nSourceY, 1, nSourceHeight, pdis->hDC, nSideWidth + nCenter, 0, pdis->rcItem.right - nCenter - nSideWidth, nHeight);
    }

    // Draw the right side of the progress bar.
    hr = DrawProgressBarImage(pTheme, pInstance, nSourceX + 3, nSourceY, 1, nSourceHeight, pdis->hDC, pdis->rcItem.right - nSideWidth, 0, nSideWidth, nHeight);

LExit:
    return hr;
}

static HRESULT DrawProgressBarImage(
    __in THEME* /*pTheme*/,
    __in const THEME_IMAGE_INSTANCE* pImageInstance,
    __in int srcX,
    __in int srcY,
    __in int srcWidth,
    __in int srcHeight,
    __in HDC hdc,
    __in int destX,
    __in int destY,
    __in int destWidth,
    __in int destHeight
    )
{
    HRESULT hr = S_OK;
    Gdiplus::Rect dest(0, 0, srcWidth, srcHeight);
    Gdiplus::Bitmap isolated(dest.Width, dest.Height);
    Gdiplus::Graphics graphics(&isolated);
    graphics.SetCompositingMode(Gdiplus::CompositingMode::CompositingModeSourceCopy);

    // Isolate the source rectangle into a temporary bitmap because otherwise GDI+ would use pixels outside of that rectangle when stretching.
    Gdiplus::Status gs = graphics.DrawImage(pImageInstance->pBitmap, dest, srcX, srcY, srcWidth, srcHeight, Gdiplus::Unit::UnitPixel);
    hr = GdipHresultFromStatus(gs);
    if (SUCCEEDED(hr))
    {
        hr = DrawGdipBitmap(hdc, destX, destY, destWidth, destHeight, &isolated, 0, 0, isolated.GetWidth(), isolated.GetHeight());
    }

    return hr;
}


static BOOL DrawHoverControl(
    __in THEME* pTheme,
    __in BOOL fHover
    )
{
    BOOL fChangedHover = FALSE;
    THEME_CONTROL* pControl = const_cast<THEME_CONTROL*>(FindControlFromHWnd(pTheme, pTheme->hwndHover));

    // Only hyperlinks and owner-drawn buttons have hover states.
    if (pControl && (THEME_CONTROL_TYPE_HYPERLINK == pControl->type ||
        (THEME_CONTROL_TYPE_BUTTON == pControl->type && (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_OWNER_DRAW))))
    {
        if (fHover)
        {
            pControl->dwData |= THEME_CONTROL_DATA_HOVER;
        }
        else
        {
            pControl->dwData &= ~THEME_CONTROL_DATA_HOVER;
        }

        ::InvalidateRect(pControl->hWnd, NULL, FALSE);
        fChangedHover = TRUE;
    }

    return fChangedHover;
}


static void FreePage(
    __in THEME_PAGE* pPage
    )
{
    if (pPage)
    {
        ReleaseStr(pPage->sczName);

        if (pPage->cSavedVariables)
        {
            for (DWORD i = 0; i < pPage->cSavedVariables; ++i)
            {
                ReleaseStr(pPage->rgSavedVariables[i].sczValue);
            }
        }

        ReleaseMem(pPage->rgSavedVariables);
    }
}


static void FreeImageList(
    __in THEME_IMAGELIST* pImageList
    )
{
    if (pImageList)
    {
        ReleaseStr(pImageList->sczName);
        ImageList_Destroy(pImageList->hImageList);
    }
}

static void FreeControl(
    __in THEME_CONTROL* pControl
    )
{
    if (pControl)
    {
        if (::IsWindow(pControl->hWnd))
        {
            ::CloseWindow(pControl->hWnd);
            pControl->hWnd = NULL;
        }

        ReleaseStr(pControl->sczName);
        ReleaseStr(pControl->sczText);
        ReleaseStr(pControl->sczTooltip);
        ReleaseStr(pControl->sczNote);
        ReleaseStr(pControl->sczEnableCondition);
        ReleaseStr(pControl->sczVisibleCondition);
        ReleaseStr(pControl->sczValue);
        ReleaseStr(pControl->sczVariable);

        switch (pControl->type)
        {
        case THEME_CONTROL_TYPE_COMMANDLINK:
            if (pControl->CommandLink.hImage)
            {
                ::DeleteBitmap(pControl->CommandLink.hImage);
            }

            if (pControl->CommandLink.hIcon)
            {
                ::DestroyIcon(pControl->CommandLink.hIcon);
            }

            for (DWORD i = 0; i < pControl->CommandLink.cConditionalNotes; ++i)
            {
                FreeConditionalText(pControl->CommandLink.rgConditionalNotes + i);
            }

            ReleaseMem(pControl->CommandLink.rgConditionalNotes);
            break;
        case THEME_CONTROL_TYPE_LISTVIEW:
            for (DWORD i = 0; i < pControl->ListView.cColumns; ++i)
            {
                FreeColumn(pControl->ListView.ptcColumns + i);
            }

            ReleaseMem(pControl->ListView.ptcColumns);
            break;
        }

        for (DWORD i = 0; i < pControl->cControls; ++i)
        {
            FreeControl(pControl->rgControls + i);
        }

        for (DWORD i = 0; i < pControl->cActions; ++i)
        {
            FreeAction(&(pControl->rgActions[i]));
        }

        for (DWORD i = 0; i < pControl->cConditionalText; ++i)
        {
            FreeConditionalText(&(pControl->rgConditionalText[i]));
        }

        for (DWORD i = 0; i < pControl->cTabs; ++i)
        {
            FreeTab(&(pControl->pttTabs[i]));
        }

        ReleaseMem(pControl->rgActions)
        ReleaseMem(pControl->rgConditionalText);
        ReleaseMem(pControl->pttTabs);
    }
}


static void FreeAction(
    __in THEME_ACTION* pAction
    )
{
    switch (pAction->type)
    {
    case THEME_ACTION_TYPE_BROWSE_DIRECTORY:
        ReleaseStr(pAction->BrowseDirectory.sczVariableName);
        break;
    case THEME_ACTION_TYPE_CHANGE_PAGE:
        ReleaseStr(pAction->ChangePage.sczPageName);
        break;
    }

    ReleaseStr(pAction->sczCondition);
}


static void FreeColumn(
    __in THEME_COLUMN* pColumn
    )
{
    ReleaseStr(pColumn->pszName);
}


static void FreeConditionalText(
    __in THEME_CONDITIONAL_TEXT* pConditionalText
    )
{
    ReleaseStr(pConditionalText->sczCondition);
    ReleaseStr(pConditionalText->sczText);
}


static void FreeTab(
    __in THEME_TAB* pTab
    )
{
    ReleaseStr(pTab->pszName);
}


static void FreeFontInstance(
    __in THEME_FONT_INSTANCE* pFontInstance
    )
{
    if (pFontInstance->hFont)
    {
        ::DeleteObject(pFontInstance->hFont);
        pFontInstance->hFont = NULL;
    }
}


static void FreeFont(
    __in THEME_FONT* pFont
    )
{
    if (pFont)
    {
        if (pFont->hBackground)
        {
            ::DeleteObject(pFont->hBackground);
            pFont->hBackground = NULL;
        }

        if (pFont->hForeground)
        {
            ::DeleteObject(pFont->hForeground);
            pFont->hForeground = NULL;
        }

        for (DWORD i = 0; i < pFont->cFontInstances; ++i)
        {
            FreeFontInstance(&(pFont->rgFontInstances[i]));
        }

        ReleaseMem(pFont->rgFontInstances);
        ReleaseStr(pFont->sczFaceName);
        ReleaseStr(pFont->sczId);
    }
}


static void FreeImage(
    __in THEME_IMAGE* pImage
    )
{
    if (pImage)
    {
        for (DWORD i = 0; i < pImage->cImageInstances; ++i)
        {
            FreeImageInstance(pImage->rgImageInstances + i);
        }

        ReleaseStr(pImage->sczId);
    }
}

static void FreeImageInstance(
    __in THEME_IMAGE_INSTANCE* pImageInstance
    )
{
    if (pImageInstance->pBitmap)
    {
        delete pImageInstance->pBitmap;
    }
}


static void CALLBACK OnBillboardTimer(
    __in THEME* /*pTheme*/,
    __in HWND hwnd,
    __in UINT_PTR idEvent
    )
{
    THEME_CONTROL* pControl = reinterpret_cast<THEME_CONTROL*>(idEvent);

    if (pControl)
    {
        AssertSz(THEME_CONTROL_TYPE_BILLBOARD == pControl->type, "Only billboard controls should get billboard timer messages.");

        if (pControl->dwData < pControl->cControls)
        {
            ThemeShowChild(pControl, pControl->dwData);
        }
        else if (pControl->fBillboardLoops)
        {
            pControl->dwData = 0;
            ThemeShowChild(pControl, pControl->dwData);
        }
        else // no more looping
        {
            ::KillTimer(hwnd, idEvent);
        }

        ++pControl->dwData;
    }
}

static void OnBrowseDirectory(
    __in THEME* pTheme,
    __in const THEME_ACTION* pAction
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczPath = NULL;
    THEME_CONTROL* pTargetControl = NULL;
    BOOL fSetVariable = NULL != pTheme->pfnSetStringVariable;

    hr = WnduShowOpenFolderDialog(pTheme->hwndParent, TRUE, pTheme->sczCaption, &sczPath);
    if (HRESULT_FROM_WIN32(ERROR_CANCELLED) == hr)
    {
        ExitFunction();
    }
    ThmExitOnFailure(hr, "Failed to prompt user for directory.");

    for (DWORD i = 0; i < pTheme->cControls; ++i)
    {
        THEME_CONTROL* pControl = pTheme->rgControls + i;

        if ((!pControl->wPageId || pControl->wPageId == pTheme->dwCurrentPageId) &&
            CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, pControl->sczName, -1, pAction->BrowseDirectory.sczVariableName, -1))
        {
            pTargetControl = pControl;
            break;
        }
    }

    // Since editbox changes aren't immediately saved off, we have to treat them differently.
    if (pTargetControl && pTargetControl->fAutomaticValue && THEME_CONTROL_TYPE_EDITBOX == pTargetControl->type)
    {
        fSetVariable = FALSE;
        hr = ThemeSetTextControl(pTargetControl, sczPath);
        ThmExitOnFailure(hr, "Failed to set text on control: %ls", pTargetControl->sczName);
    }

    if (fSetVariable)
    {
        hr = pTheme->pfnSetStringVariable(pAction->BrowseDirectory.sczVariableName, sczPath, FALSE, pTheme->pvVariableContext);
        ThmExitOnFailure(hr, "Failed to set variable: %ls", pAction->BrowseDirectory.sczVariableName);
    }

    ThemeShowPageEx(pTheme, pTheme->dwCurrentPageId, SW_SHOW, THEME_SHOW_PAGE_REASON_REFRESH);

LExit:
    ReleaseStr(sczPath);
}

static BOOL OnButtonClicked(
    __in THEME* pTheme,
    __in const THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    BOOL fHandled = FALSE;

    if (THEME_CONTROL_TYPE_BUTTON == pControl->type || THEME_CONTROL_TYPE_COMMANDLINK == pControl->type)
    {
        if (pControl->fAutomaticAction && pControl->cActions)
        {
            fHandled = TRUE;
            THEME_ACTION* pChosenAction = pControl->pDefaultAction;

            if (pTheme->pfnEvaluateCondition)
            {
                // As documented in the xsd, if there are multiple conditions that are true at the same time then the behavior is undefined.
                // This is the current implementation and can change at any time.
                for (DWORD j = 0; j < pControl->cActions; ++j)
                {
                    THEME_ACTION* pAction = pControl->rgActions + j;

                    if (pAction->sczCondition)
                    {
                        BOOL fCondition = FALSE;

                        hr = pTheme->pfnEvaluateCondition(pAction->sczCondition, &fCondition, pTheme->pvVariableContext);
                        ThmExitOnFailure(hr, "Failed to evaluate condition: %ls", pAction->sczCondition);

                        if (fCondition)
                        {
                            pChosenAction = pAction;
                            break;
                        }
                    }
                }
            }

            if (pChosenAction)
            {
                switch (pChosenAction->type)
                {
                case THEME_ACTION_TYPE_BROWSE_DIRECTORY:
                    OnBrowseDirectory(pTheme, pChosenAction);
                    break;

                case THEME_ACTION_TYPE_CLOSE_WINDOW:
                    ::SendMessageW(pTheme->hwndParent, WM_CLOSE, 0, 0);
                    break;

                case THEME_ACTION_TYPE_CHANGE_PAGE:
                    DWORD dwPageId = 0;
                    LPCWSTR pPageNames = pChosenAction->ChangePage.sczPageName;
                    ThemeGetPageIds(pTheme, &pPageNames, &dwPageId, 1);

                    if (!dwPageId)
                    {
                        ThmExitOnFailure(E_INVALIDDATA, "Unknown page: %ls", pChosenAction->ChangePage.sczPageName);
                    }

                    ThemeShowPageEx(pTheme, pTheme->dwCurrentPageId, SW_HIDE, pChosenAction->ChangePage.fCancel ? THEME_SHOW_PAGE_REASON_CANCEL : THEME_SHOW_PAGE_REASON_DEFAULT);
                    ThemeShowPage(pTheme, dwPageId, SW_SHOW);
                    break;
                }
            }
        }
    }
    else if (pControl->fAutomaticValue && (pTheme->pfnSetNumericVariable || pTheme->pfnSetStringVariable))
    {
        BOOL fRefresh = FALSE;

        switch (pControl->type)
        {
        case THEME_CONTROL_TYPE_CHECKBOX:
            if (pTheme->pfnSetNumericVariable && pControl->sczName && *pControl->sczName)
            {
                BOOL fChecked = ThemeIsControlChecked(pControl);
                pTheme->pfnSetNumericVariable(pControl->sczName, fChecked ? 1 : 0, pTheme->pvVariableContext);
                fRefresh = TRUE;
            }
            break;
        case THEME_CONTROL_TYPE_RADIOBUTTON:
            if (pTheme->pfnSetStringVariable && pControl->sczVariable && *pControl->sczVariable && ThemeIsControlChecked(pControl))
            {
                pTheme->pfnSetStringVariable(pControl->sczVariable, pControl->sczValue, FALSE, pTheme->pvVariableContext);
                fRefresh = TRUE;
            }
            break;
        }

        if (fRefresh)
        {
            ThemeShowPageEx(pTheme, pTheme->dwCurrentPageId, SW_SHOW, THEME_SHOW_PAGE_REASON_REFRESH);
            fHandled = TRUE;
        }
    }

LExit:
    return fHandled;
}

static BOOL OnDpiChanged(
    __in THEME* pTheme,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    UINT nDpi = HIWORD(wParam);
    RECT* pRect = reinterpret_cast<RECT*>(lParam);
    BOOL fIgnored = pTheme->nDpi == nDpi;

    if (fIgnored)
    {
        ExitFunction();
    }


    pTheme->fForceResize = !pTheme->fAutoResize;
    ScaleThemeFromWindow(pTheme, nDpi, pRect->left, pRect->top);

LExit:
    return !fIgnored;
}

static BOOL OnHypertextClicked(
    __in THEME* pTheme,
    __in const THEME_CONTROL* /*pThemeControl*/,
    __in PNMLINK pnmlink
    )
{
    BOOL fProcessed = FALSE;
    HRESULT hr = S_OK;
    LITEM litem = pnmlink->item;

    hr = ShelExec(litem.szUrl, NULL, L"open", NULL, SW_SHOWDEFAULT, pTheme->hwndParent, NULL);
    ThmExitOnFailure(hr, "Failed to launch hypertext link: %ls", litem.szUrl);

    fProcessed = TRUE;

LExit:
    return fProcessed;
}

static void OnNcCreate(
    __in THEME* pTheme,
    __in HWND hWnd,
    __in LPARAM lParam
    )
{
    DPIU_WINDOW_CONTEXT windowContext = { };
    CREATESTRUCTW* pCreateStruct = reinterpret_cast<CREATESTRUCTW*>(lParam);

    pTheme->hwndParent = hWnd;

    DpiuGetWindowContext(pTheme->hwndParent, &windowContext);

    if (windowContext.nDpi != pTheme->nDpi)
    {
        ScaleTheme(pTheme, windowContext.nDpi, pCreateStruct->x, pCreateStruct->y, pCreateStruct->style, NULL != pCreateStruct->hMenu, pCreateStruct->dwExStyle);
    }
}

static BOOL OnNotifyEnLink(
    __in THEME* pTheme,
    __in const THEME_CONTROL* pThemeControl,
    __in ENLINK* link
    )
{
    BOOL fProcessed = FALSE;
    HRESULT hr = S_OK;
    LPWSTR sczLink = NULL;
    TEXTRANGEW tr = { };

    // Hyperlink clicks from rich-edit control.
    if (THEME_CONTROL_TYPE_RICHEDIT == pThemeControl->type)
    {
        switch (link->msg)
        {
        case WM_LBUTTONDOWN:
            hr = StrAlloc(&sczLink, (SIZE_T)2 + link->chrg.cpMax - link->chrg.cpMin);
            ThmExitOnFailure(hr, "Failed to allocate string for link.");

            tr.chrg.cpMin = link->chrg.cpMin;
            tr.chrg.cpMax = link->chrg.cpMax;
            tr.lpstrText = sczLink;

            if (0 < ::SendMessageW(pThemeControl->hWnd, EM_GETTEXTRANGE, 0, reinterpret_cast<LPARAM>(&tr)))
            {
                hr = ShelExec(sczLink, NULL, L"open", NULL, SW_SHOWDEFAULT, pTheme->hwndParent, NULL);
                ThmExitOnFailure(hr, "Failed to launch link: %ls", sczLink);

                fProcessed = TRUE;
            }

            break;

        case WM_SETCURSOR:
            ::SetCursor(vhCursorHand);
            fProcessed = TRUE;

            break;
        }
    }

LExit:
    ReleaseStr(sczLink);

    return fProcessed;
}

static const THEME_CONTROL* FindControlFromId(
    __in const THEME* pTheme,
    __in WORD wId,
    __in_opt const THEME_CONTROL* pParentControl
    )
{
    DWORD cControls = 0;
    THEME_CONTROL* rgControls = NULL;
    const THEME_CONTROL* pChildControl = NULL;

    GetControls(pTheme, pParentControl, cControls, rgControls);

    // Breadth first search since control ids are technically only valid for direct child windows of a specific parent window.
    for (DWORD i = 0; i < cControls; ++i)
    {
        if (wId == rgControls[i].wId)
        {
            return rgControls + i;
        }
    }

    for (DWORD i = 0; i < cControls; ++i)
    {
        if (0 < rgControls[i].cControls)
        {
            pChildControl = FindControlFromId(pTheme, wId, rgControls + i);
            if (pChildControl)
            {
                return pChildControl;
            }
        }
    }

    return NULL;
}

static BOOL OnNotifyEnMsgFilter(
    __in THEME* pTheme,
    __in const THEME_CONTROL* pThemeControl,
    __in MSGFILTER* msgFilter
    )
{
    BOOL fProcessed = FALSE;

    // Tab/Shift+Tab support for rich-edit control.
    if (THEME_CONTROL_TYPE_RICHEDIT == pThemeControl->type)
    {
        switch (msgFilter->msg)
        {
        case WM_KEYDOWN:
            if (VK_TAB == msgFilter->wParam)
            {
                BOOL fShift = 0x8000 & ::GetKeyState(VK_SHIFT);
                HWND hwndFocus = ::GetNextDlgTabItem(pTheme->hwndParent, pThemeControl->hWnd, fShift);
                ::SendMessage(pTheme->hwndParent, WM_NEXTDLGCTL, (WPARAM)hwndFocus, TRUE);

                fProcessed = TRUE;
            }
            break;
        }
    }

    return fProcessed;
}

static BOOL OnPanelCreate(
    __in THEME_CONTROL* pControl,
    __in HWND hWnd
    )
{
    HRESULT hr = S_OK;

    ThmExitOnNull(pControl, hr, E_INVALIDSTATE, "Null control for OnPanelCreate");

    pControl->hWnd = hWnd;

    hr = LoadControls(pControl->pTheme, pControl);
    ThmExitOnFailure(hr, "Failed to load panel controls.");

LExit:
    return SUCCEEDED(hr);
}

static BOOL OnWmCommand(
    __in THEME* pTheme,
    __in WPARAM wParam,
    __in const THEME_CONTROL* pThemeControl,
    __inout LRESULT* plResult
    )
{
    BOOL fProcessed = FALSE;
    THEME_CONTROLWMCOMMAND_ARGS args = { };
    THEME_CONTROLWMCOMMAND_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.wParam = wParam;
    args.pThemeControl = pThemeControl;

    results.cbSize = sizeof(results);
    results.lResult = *plResult;

    if (::SendMessageW(pTheme->hwndParent, WM_THMUTIL_CONTROL_WM_COMMAND, reinterpret_cast<WPARAM>(&args), reinterpret_cast<LPARAM>(&results)))
    {
        fProcessed = TRUE;
        *plResult = results.lResult;
        ExitFunction();
    }

    switch (HIWORD(wParam))
    {
    case BN_CLICKED:
        if (OnButtonClicked(pTheme, pThemeControl))
        {
            fProcessed = TRUE;
            *plResult = 0;
            ExitFunction();
        }
        break;
    }

LExit:
    return fProcessed;
}

static BOOL OnWmNotify(
    __in THEME* pTheme,
    __in LPNMHDR lParam,
    __in const THEME_CONTROL* pThemeControl,
    __inout LRESULT* plResult
    )
{
    BOOL fProcessed = FALSE;
    THEME_CONTROLWMNOTIFY_ARGS args = { };
    THEME_CONTROLWMNOTIFY_RESULTS results = { };

    args.cbSize = sizeof(args);
    args.lParam = lParam;
    args.pThemeControl = pThemeControl;

    results.cbSize = sizeof(results);
    results.lResult = *plResult;

    if (::SendMessageW(pTheme->hwndParent, WM_THMUTIL_CONTROL_WM_NOTIFY, reinterpret_cast<WPARAM>(&args), reinterpret_cast<LPARAM>(&results)))
    {
        fProcessed = TRUE;
        *plResult = results.lResult;
        ExitFunction();
    }

    switch (lParam->code)
    {
    case EN_MSGFILTER:
        if (OnNotifyEnMsgFilter(pTheme, pThemeControl, reinterpret_cast<MSGFILTER*>(lParam)))
        {
            fProcessed = TRUE;
            *plResult = 1;
            ExitFunction();
        }
        break;

    case EN_LINK:
        if (OnNotifyEnLink(pTheme, pThemeControl, reinterpret_cast<ENLINK*>(lParam)))
        {
            fProcessed = TRUE;
            *plResult = 1;
            ExitFunction();
        }

    case NM_CLICK: __fallthrough;
    case NM_RETURN:
        switch (pThemeControl->type)
        {
        case THEME_CONTROL_TYPE_HYPERTEXT:
            // Clicks on a hypertext/syslink control.
            if (OnHypertextClicked(pTheme, pThemeControl, reinterpret_cast<PNMLINK>(lParam)))
            {
                fProcessed = TRUE;
                *plResult = 1;
                ExitFunction();
            }
        }
    }

LExit:
    return fProcessed;
}

static const THEME_CONTROL* FindControlFromHWnd(
    __in const THEME* pTheme,
    __in HWND hWnd,
    __in_opt const THEME_CONTROL* pParentControl
    )
{
    DWORD cControls = 0;
    THEME_CONTROL* rgControls = NULL;

    GetControls(pTheme, pParentControl, cControls, rgControls);

    // As we can't use GWLP_USERDATA (SysLink controls on Windows XP uses it too)...
    for (DWORD i = 0; i < cControls; ++i)
    {
        if (hWnd == rgControls[i].hWnd)
        {
            return rgControls + i;
        }
        else if (0 < rgControls[i].cControls)
        {
            const THEME_CONTROL* pChildControl = FindControlFromHWnd(pTheme, hWnd, rgControls + i);
            if (pChildControl)
            {
                return pChildControl;
            }
        }
    }

    return NULL;
}

static void GetControlDimensions(
    __in const THEME_CONTROL* pControl,
    __in const RECT* prcParent,
    __out int* piWidth,
    __out int* piHeight,
    __out int* piX,
    __out int* piY
    )
{
    *piWidth  = pControl->nWidth  + (0 < pControl->nWidth  ? 0 : prcParent->right  - max(0, pControl->nX));
    *piHeight = pControl->nHeight + (0 < pControl->nHeight ? 0 : prcParent->bottom - max(0, pControl->nY));
    *piX = pControl->nX + (-1 < pControl->nX ? 0 : prcParent->right - *piWidth);
    *piY = pControl->nY + (-1 < pControl->nY ? 0 : prcParent->bottom - *piHeight);
}

static HRESULT SizeListViewColumns(
    __inout THEME_CONTROL* pControl
    )
{
    HRESULT hr = S_OK;
    RECT rcParent = { };
    int cNumExpandingColumns = 0;
    int iExtraAvailableSize;
    THEME_COLUMN* pColumn = NULL;

    if (!pControl->hWnd)
    {
        ExitFunction();
    }

    if (!::GetWindowRect(pControl->hWnd, &rcParent))
    {
        ThmExitWithLastError(hr, "Failed to get window rect of listview control.");
    }

    iExtraAvailableSize = rcParent.right - rcParent.left;

    for (DWORD i = 0; i < pControl->ListView.cColumns; ++i)
    {
        pColumn = pControl->ListView.ptcColumns + i;

        if (pColumn->fExpands)
        {
            ++cNumExpandingColumns;
        }

        iExtraAvailableSize -= pColumn->nBaseWidth;
    }

    // Leave room for a vertical scroll bar just in case.
    iExtraAvailableSize -= ::GetSystemMetrics(SM_CXVSCROLL);

    for (DWORD i = 0; i < pControl->ListView.cColumns; ++i)
    {
        pColumn = pControl->ListView.ptcColumns + i;

        if (pColumn->fExpands)
        {
            pColumn->nWidth = pColumn->nBaseWidth + (iExtraAvailableSize / cNumExpandingColumns);
            // In case there is any remainder, use it up the first chance we get.
            pColumn->nWidth += iExtraAvailableSize % cNumExpandingColumns;
            iExtraAvailableSize -= iExtraAvailableSize % cNumExpandingColumns;
        }
        else
        {
            pColumn->nWidth = pColumn->nBaseWidth;
        }
    }

LExit:
    return hr;
}


static HRESULT ShowControl(
    __in THEME_CONTROL* pControl,
    __in int nCmdShow,
    __in BOOL fSaveEditboxes,
    __in THEME_SHOW_PAGE_REASON reason,
    __in DWORD dwPageId,
    __inout_opt HWND* phwndFocus
    )
{
    HRESULT hr = S_OK;
    DWORD iPageControl = 0;
    HWND hwndFocus = phwndFocus ? *phwndFocus : NULL;
    LPWSTR sczFormatString = NULL;
    LPWSTR sczText = NULL;
    THEME_SAVEDVARIABLE* pSavedVariable = NULL;
    BOOL fHide = SW_HIDE == nCmdShow;
    THEME* pTheme = pControl->pTheme;
    THEME_PAGE* pPage = ThemeGetPage(pTheme, dwPageId);

    // Save the editbox value if necessary (other control types save their values immediately).
    if (pTheme->pfnSetStringVariable && pControl->fAutomaticValue &&
        fSaveEditboxes && THEME_CONTROL_TYPE_EDITBOX == pControl->type && pControl->sczName && *pControl->sczName)
    {
        hr = ThemeGetTextControl(pControl, &sczText);
        ThmExitOnFailure(hr, "Failed to get the text for control: %ls", pControl->sczName);

        hr = pTheme->pfnSetStringVariable(pControl->sczName, sczText, FALSE, pTheme->pvVariableContext);
        ThmExitOnFailure(hr, "Failed to set the variable '%ls' to '%ls'", pControl->sczName, sczText);
    }

    HWND hWnd = pControl->hWnd;

    if (fHide && pControl->wPageId)
    {
        ::ShowWindow(hWnd, SW_HIDE);

        if (THEME_CONTROL_TYPE_BILLBOARD == pControl->type)
        {
            StopBillboard(pTheme, pControl);
        }

        ExitFunction();
    }

    BOOL fEnabled = !(pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_DISABLED);
    BOOL fVisible = !(pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_HIDDEN);

    if (pTheme->pfnEvaluateCondition)
    {
        // If the control has a VisibleCondition, check if it's true.
        if (pControl->sczVisibleCondition && pControl->fAutomaticVisible)
        {
            hr = pTheme->pfnEvaluateCondition(pControl->sczVisibleCondition, &fVisible, pTheme->pvVariableContext);
            ThmExitOnFailure(hr, "Failed to evaluate VisibleCondition: %ls", pControl->sczVisibleCondition);
        }

        // If the control has an EnableCondition, check if it's true.
        if (pControl->sczEnableCondition && pControl->fAutomaticEnabled)
        {
            hr = pTheme->pfnEvaluateCondition(pControl->sczEnableCondition, &fEnabled, pTheme->pvVariableContext);
            ThmExitOnFailure(hr, "Failed to evaluate EnableCondition: %ls", pControl->sczEnableCondition);
        }
    }

    // Try to format each control's text based on context, except for editboxes since their text comes from the user.
    if (pTheme->pfnFormatString && pControl->fAutomaticText && ((pControl->sczText && *pControl->sczText) || pControl->cConditionalText) && THEME_CONTROL_TYPE_EDITBOX != pControl->type)
    {
        LPCWSTR wzText = pControl->sczText;
        LPCWSTR wzNote = pControl->sczNote;

        if (pTheme->pfnEvaluateCondition)
        {
            // As documented in the xsd, if there are multiple conditions that are true at the same time then the behavior is undefined.
            // This is the current implementation and can change at any time.
            for (DWORD j = 0; j < pControl->cConditionalText; ++j)
            {
                THEME_CONDITIONAL_TEXT* pConditionalText = pControl->rgConditionalText + j;

                if (pConditionalText->sczCondition)
                {
                    BOOL fCondition = FALSE;

                    hr = pTheme->pfnEvaluateCondition(pConditionalText->sczCondition, &fCondition, pTheme->pvVariableContext);
                    ThmExitOnFailure(hr, "Failed to evaluate condition: %ls", pConditionalText->sczCondition);

                    if (fCondition)
                    {
                        wzText = pConditionalText->sczText;
                        break;
                    }
                }
            }

            if (THEME_CONTROL_TYPE_COMMANDLINK == pControl->type)
            {
                for (DWORD j = 0; j < pControl->CommandLink.cConditionalNotes; ++j)
                {
                    THEME_CONDITIONAL_TEXT* pConditionalNote = pControl->CommandLink.rgConditionalNotes + j;

                    if (pConditionalNote->sczCondition)
                    {
                        BOOL fCondition = FALSE;

                        hr = pTheme->pfnEvaluateCondition(pConditionalNote->sczCondition, &fCondition, pTheme->pvVariableContext);
                        ThmExitOnFailure(hr, "Failed to evaluate note condition: %ls", pConditionalNote->sczCondition);

                        if (fCondition)
                        {
                            wzNote = pConditionalNote->sczText;
                            break;
                        }
                    }
                }
            }
        }

        if (wzText && *wzText)
        {
            hr = pTheme->pfnFormatString(wzText, &sczText, pTheme->pvVariableContext);
            ThmExitOnFailure(hr, "Failed to format string: %ls", wzText);
        }
        else
        {
            ReleaseNullStr(sczText);
        }

        ThemeSetTextControl(pControl, sczText);

        if (wzNote && *wzNote)
        {
            hr = pTheme->pfnFormatString(wzNote, &sczText, pTheme->pvVariableContext);
            ThmExitOnFailure(hr, "Failed to format note: %ls", wzNote);
        }
        else
        {
            ReleaseNullStr(sczText);
        }

        ::SendMessageW(pControl->hWnd, BCM_SETNOTE, 0, reinterpret_cast<WPARAM>(sczText));
    }

    if (pControl->fAutomaticValue)
    {
        // If this is a named control, do variable magic.
        if (pControl->sczName && *pControl->sczName)
        {
            // If this is a checkbox control,
            // try to set its default state to the state of a matching named variable.
            if (pTheme->pfnGetNumericVariable && THEME_CONTROL_TYPE_CHECKBOX == pControl->type)
            {
                LONGLONG llValue = 0;
                hr = pTheme->pfnGetNumericVariable(pControl->sczName, &llValue, pTheme->pvVariableContext);
                if (E_NOTFOUND == hr)
                {
                    hr = S_OK;
                }
                ThmExitOnFailure(hr, "Failed to get numeric variable: %ls", pControl->sczName);

                if (THEME_SHOW_PAGE_REASON_REFRESH != reason && pPage && pControl->wPageId)
                {
                    pSavedVariable = pPage->rgSavedVariables + iPageControl;
                    pSavedVariable->wzName = pControl->sczName;

                    if (SUCCEEDED(hr))
                    {
                        hr = StrAllocFormattedSecure(&pSavedVariable->sczValue, L"%lld", llValue);
                        ThmExitOnFailure(hr, "Failed to save variable: %ls", pControl->sczName);
                    }

                    ++iPageControl;
                }

                ::SendMessageW(pControl->hWnd, BM_SETCHECK, SUCCEEDED(hr) && llValue ? BST_CHECKED : BST_UNCHECKED, 0);
            }

            // If this is an editbox control,
            // try to set its default state to the state of a matching named variable.
            if (pTheme->pfnFormatString && THEME_CONTROL_TYPE_EDITBOX == pControl->type)
            {
                hr = StrAllocFormatted(&sczFormatString, L"[%ls]", pControl->sczName);
                ThmExitOnFailure(hr, "Failed to create format string: '%ls'", pControl->sczName);

                hr = pTheme->pfnFormatString(sczFormatString, &sczText, pTheme->pvVariableContext);
                ThmExitOnFailure(hr, "Failed to format string: '%ls'", sczFormatString);

                if (THEME_SHOW_PAGE_REASON_REFRESH != reason && pPage && pControl->wPageId)
                {
                    pSavedVariable = pPage->rgSavedVariables + iPageControl;
                    pSavedVariable->wzName = pControl->sczName;

                    if (SUCCEEDED(hr))
                    {
                        hr = StrAllocStringSecure(&pSavedVariable->sczValue, sczText, 0);
                        ThmExitOnFailure(hr, "Failed to save variable: %ls", pControl->sczName);
                    }

                    ++iPageControl;
                }

                ThemeSetTextControl(pControl, sczText);
            }
        }

        // If this is a radio button associated with a variable,
        // try to set its default state to the state of the variable.
        if (pTheme->pfnGetStringVariable && THEME_CONTROL_TYPE_RADIOBUTTON == pControl->type && pControl->sczVariable && *pControl->sczVariable)
        {
            hr = pTheme->pfnGetStringVariable(pControl->sczVariable, &sczText, pTheme->pvVariableContext);
            if (E_NOTFOUND == hr)
            {
                ReleaseNullStr(sczText);
            }
            else
            {
                ThmExitOnFailure(hr, "Failed to get string variable: %ls", pControl->sczVariable);
            }

            if (THEME_SHOW_PAGE_REASON_REFRESH != reason && pPage && pControl->wPageId && pControl->fLastRadioButton)
            {
                pSavedVariable = pPage->rgSavedVariables + iPageControl;
                pSavedVariable->wzName = pControl->sczVariable;

                if (SUCCEEDED(hr))
                {
                    hr = StrAllocStringSecure(&pSavedVariable->sczValue, sczText, 0);
                    ThmExitOnFailure(hr, "Failed to save variable: %ls", pControl->sczVariable);
                }

                ++iPageControl;
            }

            hr = S_OK;

            Button_SetCheck(hWnd, (!sczText && !pControl->sczValue) || (sczText && CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, sczText, -1, pControl->sczValue, -1)));
        }
    }

    if (!fVisible || (!fEnabled && (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_HIDE_WHEN_DISABLED)))
    {
        ::ShowWindow(hWnd, SW_HIDE);
    }
    else
    {
        ::EnableWindow(hWnd, !fHide && fEnabled);

        if (!hwndFocus && pControl->wPageId && (pControl->dwStyle & WS_TABSTOP))
        {
            hwndFocus = hWnd;
        }

        ::ShowWindow(hWnd, nCmdShow);
    }

    if (0 < pControl->cControls)
    {
        ShowControls(pTheme, pControl, nCmdShow, fSaveEditboxes, FALSE/*fSetFocus*/, reason, dwPageId);
    }

    if (THEME_CONTROL_TYPE_BILLBOARD == pControl->type && pControl->wPageId)
    {
        if (fEnabled)
        {
            StartBillboard(pTheme, pControl);
        }
        else
        {
            StopBillboard(pTheme, pControl);
        }
    }

    if (phwndFocus)
    {
        *phwndFocus = hwndFocus;
    }

LExit:
    ReleaseStr(sczFormatString);
    ReleaseStr(sczText);

    return hr;
}

static HRESULT ShowControls(
    __in THEME* pTheme,
    __in_opt const THEME_CONTROL* pParentControl,
    __in int nCmdShow,
    __in BOOL fSaveEditboxes,
    __in BOOL fSetFocus,
    __in THEME_SHOW_PAGE_REASON reason,
    __in DWORD dwPageId
    )
{
    HRESULT hr = S_OK;
    HWND hwndFocus = NULL;
    DWORD cControls = 0;
    THEME_CONTROL* rgControls = NULL;

    GetControls(pTheme, pParentControl, cControls, rgControls);

    for (DWORD i = 0; i < cControls; ++i)
    {
        THEME_CONTROL* pControl = rgControls + i;

        // Only look at non-page controls and the specified page's controls.
        if (!pControl->wPageId || pControl->wPageId == dwPageId)
        {
            hr = ShowControl(pControl, nCmdShow, fSaveEditboxes, reason, dwPageId, &hwndFocus);
            ThmExitOnFailure(hr, "Failed to show control '%ls' at index %d.", pControl->sczName, i);
        }
    }

    if (fSetFocus && hwndFocus)
    {
        ::SendMessage(pTheme->hwndParent, WM_NEXTDLGCTL, (WPARAM)hwndFocus, TRUE);
    }

LExit:
    return hr;
}


static LRESULT CALLBACK ControlGroupDefWindowProc(
    __in_opt THEME* pTheme,
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam,
    __in BOOL fDialog
    )
{
    LRESULT lres = 0;
    const THEME_CONTROL* pThemeControl = NULL;

    if (pTheme)
    {
        switch (uMsg)
        {
        case WM_DRAWITEM:
            ThemeDrawControl(pTheme, reinterpret_cast<LPDRAWITEMSTRUCT>(lParam));
            return TRUE;

        case WM_CTLCOLORBTN: __fallthrough;
        case WM_CTLCOLOREDIT: __fallthrough;
        case WM_CTLCOLORLISTBOX: __fallthrough;
        case WM_CTLCOLORSCROLLBAR: __fallthrough;
        case WM_CTLCOLORSTATIC:
        {
            HBRUSH hBrush = NULL;
            if (ThemeSetControlColor(pTheme, reinterpret_cast<HDC>(wParam), reinterpret_cast<HWND>(lParam), &hBrush))
            {
                return reinterpret_cast<LRESULT>(hBrush);
            }
        }
        break;

        case WM_SETCURSOR:
            if (ThemeHoverControl(pTheme, hWnd, reinterpret_cast<HWND>(wParam)))
            {
                return TRUE;
            }
            break;

        case WM_PAINT:
            if (::GetUpdateRect(hWnd, NULL, FALSE))
            {
                PAINTSTRUCT ps;
                ::BeginPaint(hWnd, &ps);
                if (hWnd == pTheme->hwndParent)
                {
                    ThemeDrawBackground(pTheme, &ps);
                }
                ::EndPaint(hWnd, &ps);
            }
            return 0;

        case WM_TIMER:
            OnBillboardTimer(pTheme, hWnd, wParam);
            break;

        case WM_NOTIFY:
            if (lParam)
            {
                LPNMHDR pnmhdr = reinterpret_cast<LPNMHDR>(lParam);
                pThemeControl = FindControlFromHWnd(pTheme, pnmhdr->hwndFrom);
                if (pThemeControl && OnWmNotify(pTheme, pnmhdr, pThemeControl, &lres))
                {
                    return lres;
                }
            }
            break;

        case WM_COMMAND:
            if (lParam)
            {
                pThemeControl = FindControlFromHWnd(pTheme, (HWND)lParam);
                if (pThemeControl && OnWmCommand(pTheme, wParam, pThemeControl, &lres))
                {
                    return lres;
                }
            }
            break;

        case WM_CLOSE: __fallthrough;
        case WM_ERASEBKGND:
            fDialog = FALSE;
            break;
        }
    }

    return fDialog ? ::DefDlgProcW(hWnd, uMsg, wParam, lParam) : ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
}


static LRESULT CALLBACK PanelWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    LRESULT lres = 0;
    THEME_CONTROL* pControl = reinterpret_cast<THEME_CONTROL*>(::GetWindowLongPtrW(hWnd, GWLP_USERDATA));

    switch (uMsg)
    {
    case WM_NCCREATE:
    {
        LPCREATESTRUCTW lpcs = reinterpret_cast<LPCREATESTRUCTW>(lParam);
        pControl = reinterpret_cast<THEME_CONTROL*>(lpcs->lpCreateParams);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(pControl));
        break;
    }

    case WM_CREATE:
        if (!OnPanelCreate(pControl, hWnd))
        {
            return -1;
        }
        break;

    case WM_NCDESTROY:
        lres = ::DefWindowProcW(hWnd, uMsg, wParam, lParam);
        ::SetWindowLongPtrW(hWnd, GWLP_USERDATA, 0);
        return lres;

    case WM_NCHITTEST:
        return HTCLIENT;
        break;
    }

    return ControlGroupDefWindowProc(pControl ? pControl->pTheme : NULL, hWnd, uMsg, wParam, lParam, FALSE);
}

static LRESULT CALLBACK StaticOwnerDrawWndProc(
    __in HWND hWnd,
    __in UINT uMsg,
    __in WPARAM wParam,
    __in LPARAM lParam
    )
{
    switch (uMsg)
    {
    case WM_UPDATEUISTATE:
        return ::DefWindowProc(hWnd, uMsg, wParam, lParam);
    default:
        return (*vpfnStaticOwnerDrawBaseWndProc)(hWnd, uMsg, wParam, lParam);
    }
}

static HRESULT OnLoadingControl(
    __in THEME* pTheme,
    __in const THEME_CONTROL* pControl,
    __inout WORD* pwId,
    __inout DWORD* pdwAutomaticBehaviorType
    )
{
    HRESULT hr = S_OK;
    THEME_LOADINGCONTROL_ARGS loadingControlArgs = { };
    THEME_LOADINGCONTROL_RESULTS loadingControlResults = { };

    loadingControlArgs.cbSize = sizeof(loadingControlArgs);
    loadingControlArgs.pThemeControl = pControl;

    loadingControlResults.cbSize = sizeof(loadingControlResults);
    loadingControlResults.hr = E_NOTIMPL;
    loadingControlResults.wId = *pwId;

    if (::SendMessageW(pTheme->hwndParent, WM_THMUTIL_LOADING_CONTROL, reinterpret_cast<WPARAM>(&loadingControlArgs), reinterpret_cast<LPARAM>(&loadingControlResults)))
    {
        hr = loadingControlResults.hr;
        if (SUCCEEDED(hr))
        {
            *pwId = loadingControlResults.wId;
            *pdwAutomaticBehaviorType = loadingControlResults.dwAutomaticBehaviorType;
        }
    }

    return hr;
}

static HRESULT LoadThemeControls(
    __in THEME* pTheme
    )
{
    HRESULT hr = S_OK;

    ThmExitOnNull(pTheme->hwndParent, hr, E_INVALIDSTATE, "LoadThemeControls called before theme parent window created.");

    hr = LoadControls(pTheme, NULL);

LExit:
    return hr;
}

static void UnloadThemeControls(
    __in THEME* pTheme
    )
{
    UnloadControls(pTheme->cControls, pTheme->rgControls);

    pTheme->hwndHover = NULL;
    pTheme->hwndParent = NULL;
}

static HRESULT LoadControls(
    __in THEME* pTheme,
    __in_opt THEME_CONTROL* pParentControl
    )
{
    HRESULT hr = S_OK;
    RECT rcParent = { };
    LPWSTR sczText = NULL;
    BOOL fStartNewGroup = FALSE;
    DWORD cControls = 0;
    THEME_CONTROL* rgControls = NULL;
    HWND hwndParent = pParentControl ? pParentControl->hWnd : pTheme->hwndParent;
    int w = 0;
    int h = 0;
    int x = 0;
    int y = 0;
    THEME_LOADEDCONTROL_ARGS loadedControlArgs = { };
    THEME_LOADEDCONTROL_RESULTS loadedControlResults = { };

    GetControls(pTheme, pParentControl, cControls, rgControls);
    ::GetClientRect(hwndParent, &rcParent);

    loadedControlArgs.cbSize = sizeof(loadedControlArgs);
    loadedControlResults.cbSize = sizeof(loadedControlResults);

    for (DWORD i = 0; i < cControls; ++i)
    {
        THEME_CONTROL* pControl = rgControls + i;
        THEME_FONT* pControlFont = (pTheme->cFonts > pControl->dwFontId) ? pTheme->rgFonts + pControl->dwFontId : NULL;
        THEME_FONT_INSTANCE* pControlFontInstance = NULL;
        LPCWSTR wzWindowClass = NULL;
        DWORD dwWindowBits = WS_CHILD;
        DWORD dwWindowExBits = 0;

        if (fStartNewGroup)
        {
            dwWindowBits |= WS_GROUP;
            fStartNewGroup = FALSE;
        }

        switch (pControl->type)
        {
        case THEME_CONTROL_TYPE_BILLBOARD: __fallthrough;
        case THEME_CONTROL_TYPE_PANEL:
            wzWindowClass = vsczPanelClass;
            dwWindowExBits |= WS_EX_CONTROLPARENT;
#ifdef DEBUG
            StrAllocFormatted(&pControl->sczText, L"Panel '%ls', id: %d", pControl->sczName, pControl->wId);
#endif
            break;

        case THEME_CONTROL_TYPE_CHECKBOX:
            dwWindowBits |= BS_AUTOCHECKBOX | BS_MULTILINE; // checkboxes are basically buttons with an extra bit tossed in.
            __fallthrough;
        case THEME_CONTROL_TYPE_BUTTON:
            wzWindowClass = WC_BUTTONW;
            if (THEME_IMAGE_REFERENCE_TYPE_NONE != pControl->Button.rgImageRef[0].type)
            {
                dwWindowBits |= BS_OWNERDRAW;
                pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_OWNER_DRAW;
            }
            break;

        case THEME_CONTROL_TYPE_COMBOBOX:
            wzWindowClass = WC_COMBOBOXW;
            dwWindowBits |= CBS_DROPDOWNLIST | CBS_HASSTRINGS;
            break;

        case THEME_CONTROL_TYPE_COMMANDLINK:
            wzWindowClass = WC_BUTTONW;
            dwWindowBits |= BS_COMMANDLINK;
            break;

        case THEME_CONTROL_TYPE_EDITBOX:
            wzWindowClass = WC_EDITW;
            dwWindowBits |= ES_LEFT | ES_AUTOHSCROLL;
            dwWindowExBits = WS_EX_CLIENTEDGE;
            break;

        case THEME_CONTROL_TYPE_HYPERLINK: // hyperlinks are basically just owner drawn buttons.
            wzWindowClass = vsczHyperlinkClass;
            dwWindowBits |= BS_OWNERDRAW | BTNS_NOPREFIX;
            break;

        case THEME_CONTROL_TYPE_HYPERTEXT:
            wzWindowClass = WC_LINK;
            dwWindowBits |= LWS_NOPREFIX;
            break;

        case THEME_CONTROL_TYPE_IMAGE: // images are basically just owner drawn static controls (so we can draw .jpgs and .pngs instead of just bitmaps).
            if (THEME_IMAGE_REFERENCE_TYPE_NONE != pControl->Image.imageRef.type)
            {
                wzWindowClass = vsczStaticOwnerDrawClass;
                dwWindowBits |= SS_OWNERDRAW;
                pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_OWNER_DRAW;
            }
            else
            {
                hr = HRESULT_FROM_WIN32(ERROR_INVALID_DATA);
                ThmExitOnRootFailure(hr, "Invalid image or image list coordinates.");
            }
            break;

        case THEME_CONTROL_TYPE_LABEL:
            wzWindowClass = WC_STATICW;
            break;

        case THEME_CONTROL_TYPE_LISTVIEW:
            // If thmutil is handling the image list for this listview, tell Windows not to free it when the control is destroyed.
            if (pControl->ListView.rghImageList[0] || pControl->ListView.rghImageList[1] || pControl->ListView.rghImageList[2] || pControl->ListView.rghImageList[3])
            {
                pControl->dwStyle |= LVS_SHAREIMAGELISTS;
            }
            wzWindowClass = WC_LISTVIEWW;
            break;

        case THEME_CONTROL_TYPE_PROGRESSBAR:
            if (pControl->ProgressBar.cImageRef)
            {
                wzWindowClass = vsczStaticOwnerDrawClass; // no such thing as an owner drawn progress bar so we'll make our own out of a static control.
                dwWindowBits |= SS_OWNERDRAW;
                pControl->dwInternalStyle |= INTERNAL_CONTROL_STYLE_OWNER_DRAW;
            }
            else
            {
                wzWindowClass = PROGRESS_CLASSW;
            }
            break;

        case THEME_CONTROL_TYPE_RADIOBUTTON:
            dwWindowBits |= BS_AUTORADIOBUTTON | BS_MULTILINE;
            wzWindowClass = WC_BUTTONW;

            if (pControl->fLastRadioButton)
            {
                fStartNewGroup = TRUE;
            }
            break;

        case THEME_CONTROL_TYPE_RICHEDIT:
            if (!vhModuleMsftEdit && !vhModuleRichEd)
            {
                hr = LoadSystemLibrary(L"Msftedit.dll", &vhModuleMsftEdit);
                if (FAILED(hr))
                {
                    hr = LoadSystemLibrary(L"Riched20.dll", &vhModuleRichEd);
                    ThmExitOnFailure(hr, "Failed to load Rich Edit control library.");
                }
            }

            wzWindowClass = vhModuleMsftEdit ? MSFTEDIT_CLASS : RICHEDIT_CLASSW;
            dwWindowBits |= ES_SAVESEL | ES_MULTILINE | WS_VSCROLL | ES_READONLY;
            break;

        case THEME_CONTROL_TYPE_STATIC:
            wzWindowClass = WC_STATICW;
            dwWindowBits |= SS_ETCHEDHORZ;
            break;

        case THEME_CONTROL_TYPE_TAB:
            wzWindowClass = WC_TABCONTROLW;
            break;

        case THEME_CONTROL_TYPE_TREEVIEW:
            wzWindowClass = WC_TREEVIEWW;
            break;
        }
        ThmExitOnNull(wzWindowClass, hr, E_INVALIDDATA, "Failed to configure control %u because of unknown type: %u", i, pControl->type);

        // Default control ids to the next id, unless there is a specific id to assign to a control.
        WORD wControlId = THEME_FIRST_AUTO_ASSIGN_CONTROL_ID;
        DWORD dwAutomaticBehaviorType = THEME_CONTROL_AUTOMATIC_BEHAVIOR_ALL;
        hr = OnLoadingControl(pTheme, pControl, &wControlId, &dwAutomaticBehaviorType);
        ThmExitOnFailure(hr, "ThmLoadingControl failed.");

        pControl->fAutomaticEnabled = THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_ENABLED != (THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_ENABLED & dwAutomaticBehaviorType);
        pControl->fAutomaticVisible = THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_VISIBLE != (THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_VISIBLE & dwAutomaticBehaviorType);
        pControl->fAutomaticAction = THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_ACTION != (THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_ACTION & dwAutomaticBehaviorType);
        pControl->fAutomaticText = THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_TEXT != (THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_TEXT & dwAutomaticBehaviorType);
        pControl->fAutomaticValue = THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_VALUE != (THEME_CONTROL_AUTOMATIC_BEHAVIOR_EXCLUDE_VALUE & dwAutomaticBehaviorType);

        // This range is reserved for thmutil. The process will run out of available window handles before reaching the end of the range.
        if (THEME_FIRST_AUTO_ASSIGN_CONTROL_ID <= wControlId && THEME_FIRST_ASSIGN_CONTROL_ID > wControlId)
        {
            wControlId = pTheme->wNextControlId;
            pTheme->wNextControlId += 1;
        }

        pControl->wId = wControlId;

        GetControlDimensions(pControl, &rcParent, &w, &h, &x, &y);

        BOOL fVisible = pControl->dwStyle & WS_VISIBLE;
        BOOL fDisabled = pControl->dwStyle & WS_DISABLED;

        // If the control is supposed to be initially visible and it has a VisibleCondition, check if it's true.
        if (fVisible && pControl->sczVisibleCondition && pTheme->pfnEvaluateCondition && pControl->fAutomaticVisible)
        {
            hr = pTheme->pfnEvaluateCondition(pControl->sczVisibleCondition, &fVisible, pTheme->pvVariableContext);
            ThmExitOnFailure(hr, "Failed to evaluate VisibleCondition: %ls", pControl->sczVisibleCondition);

            if (!fVisible)
            {
                pControl->dwStyle &= ~WS_VISIBLE;
            }
        }

        // Disable controls that aren't visible so their shortcut keys don't trigger.
        if (!fVisible)
        {
            dwWindowBits |= WS_DISABLED;
            fDisabled = TRUE;
        }

        // If the control is supposed to be initially enabled and it has an EnableCondition, check if it's true.
        if (!fDisabled && pControl->sczEnableCondition && pTheme->pfnEvaluateCondition && pControl->fAutomaticEnabled)
        {
            BOOL fEnable = TRUE;

            hr = pTheme->pfnEvaluateCondition(pControl->sczEnableCondition, &fEnable, pTheme->pvVariableContext);
            ThmExitOnFailure(hr, "Failed to evaluate EnableCondition: %ls", pControl->sczEnableCondition);

            fDisabled = !fEnable;
            dwWindowBits |= fDisabled ? WS_DISABLED : 0;
        }

        // Honor the HideWhenDisabled option.
        if ((pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_HIDE_WHEN_DISABLED) && fVisible && fDisabled)
        {
            fVisible = FALSE;
            pControl->dwStyle &= ~WS_VISIBLE;
        }

        pControl->hWnd = ::CreateWindowExW(dwWindowExBits, wzWindowClass, pControl->sczText, pControl->dwStyle | dwWindowBits, x, y, w, h, hwndParent, reinterpret_cast<HMENU>(wControlId), NULL, pControl);
        ThmExitOnNullWithLastError(pControl->hWnd, hr, "Failed to create window.");

        if (pControl->sczTooltip)
        {
            if (!pTheme->hwndTooltip)
            {
                pTheme->hwndTooltip = ::CreateWindowExW(WS_EX_TOOLWINDOW, TOOLTIPS_CLASSW, NULL, WS_POPUP | TTS_ALWAYSTIP | TTS_BALLOON | TTS_NOPREFIX, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, CW_USEDEFAULT, hwndParent, NULL, NULL, NULL);
            }

            if (pTheme->hwndTooltip)
            {
                TOOLINFOW toolinfo = {};
                toolinfo.cbSize = sizeof(toolinfo);
                toolinfo.hwnd = hwndParent;
                toolinfo.uFlags = TTF_IDISHWND | TTF_SUBCLASS;
                toolinfo.uId = reinterpret_cast<UINT_PTR>(pControl->hWnd);
                toolinfo.lpszText = pControl->sczTooltip;
                ::SendMessageW(pTheme->hwndTooltip, TTM_ADDTOOLW, 0, reinterpret_cast<LPARAM>(&toolinfo));
            }
        }

        if (THEME_CONTROL_TYPE_COMMANDLINK == pControl->type)
        {
            if (pControl->sczNote)
            {
                ::SendMessageW(pControl->hWnd, BCM_SETNOTE, 0, reinterpret_cast<WPARAM>(pControl->sczNote));
            }

            if (pControl->CommandLink.hImage)
            {
                ::SendMessageW(pControl->hWnd, BM_SETIMAGE, IMAGE_BITMAP, reinterpret_cast<LPARAM>(pControl->CommandLink.hImage));
            }
            else if (pControl->CommandLink.hIcon)
            {
                ::SendMessageW(pControl->hWnd, BM_SETIMAGE, IMAGE_ICON, reinterpret_cast<LPARAM>(pControl->CommandLink.hIcon));
            }
        }
        else if (THEME_CONTROL_TYPE_EDITBOX == pControl->type)
        {
            if (pControl->dwInternalStyle & INTERNAL_CONTROL_STYLE_FILESYSTEM_AUTOCOMPLETE)
            {
                hr = ::SHAutoComplete(pControl->hWnd, SHACF_FILESYS_ONLY);
            }
        }
        else if (THEME_CONTROL_TYPE_LISTVIEW == pControl->type)
        {
            ::SendMessageW(pControl->hWnd, LVM_SETEXTENDEDLISTVIEWSTYLE, 0, pControl->dwExtendedStyle);

            hr = SizeListViewColumns(pControl);
            ThmExitOnFailure(hr, "Failed to get size of list view columns.");

            for (DWORD j = 0; j < pControl->ListView.cColumns; ++j)
            {
                LVCOLUMNW lvc = { };
                lvc.mask = LVCF_FMT | LVCF_WIDTH | LVCF_TEXT | LVCF_SUBITEM;
                lvc.cx = pControl->ListView.ptcColumns[j].nWidth;
                lvc.iSubItem = j;
                lvc.pszText = pControl->ListView.ptcColumns[j].pszName;
                lvc.fmt = LVCFMT_LEFT;
                lvc.cchTextMax = 4;

                if (-1 == ::SendMessageW(pControl->hWnd, LVM_INSERTCOLUMNW, (WPARAM) (int) (j), (LPARAM) (const LV_COLUMNW *) (&lvc)))
                {
                    ThmExitWithLastError(hr, "Failed to insert listview column %u into tab control.", j);
                }

                // Return value tells us the old image list, we don't care.
                if (pControl->ListView.rghImageList[0])
                {
                    ::SendMessageW(pControl->hWnd, LVM_SETIMAGELIST, static_cast<WPARAM>(LVSIL_NORMAL), reinterpret_cast<LPARAM>(pControl->ListView.rghImageList[0]));
                }
                if (pControl->ListView.rghImageList[1])
                {
                    ::SendMessageW(pControl->hWnd, LVM_SETIMAGELIST, static_cast<WPARAM>(LVSIL_SMALL), reinterpret_cast<LPARAM>(pControl->ListView.rghImageList[1]));
                }
                if (pControl->ListView.rghImageList[2])
                {
                    ::SendMessageW(pControl->hWnd, LVM_SETIMAGELIST, static_cast<WPARAM>(LVSIL_STATE), reinterpret_cast<LPARAM>(pControl->ListView.rghImageList[2]));
                }
                if (pControl->ListView.rghImageList[3])
                {
                    ::SendMessageW(pControl->hWnd, LVM_SETIMAGELIST, static_cast<WPARAM>(LVSIL_GROUPHEADER), reinterpret_cast<LPARAM>(pControl->ListView.rghImageList[3]));
                }
            }
        }
        else if (THEME_CONTROL_TYPE_RICHEDIT == pControl->type)
        {
            ::SendMessageW(pControl->hWnd, EM_AUTOURLDETECT, static_cast<WPARAM>(TRUE), 0);
            ::SendMessageW(pControl->hWnd, EM_SETEVENTMASK, 0, ENM_KEYEVENTS | ENM_LINK);
        }
        else if (THEME_CONTROL_TYPE_TAB == pControl->type)
        {
            ULONG_PTR hbrBackground = 0;
            if (THEME_INVALID_ID != pControl->dwFontId)
            {
                hbrBackground = reinterpret_cast<ULONG_PTR>(pTheme->rgFonts[pControl->dwFontId].hBackground);
            }
            else
            {
                hbrBackground = ::GetClassLongPtr(pTheme->hwndParent, GCLP_HBRBACKGROUND);
            }
            ::SetClassLongPtr(pControl->hWnd, GCLP_HBRBACKGROUND, hbrBackground);

            for (DWORD j = 0; j < pControl->cTabs; ++j)
            {
                TCITEMW tci = { };
                tci.mask = TCIF_TEXT | TCIF_IMAGE;
                tci.iImage = -1;
                tci.pszText = pControl->pttTabs[j].pszName;

                if (-1 == ::SendMessageW(pControl->hWnd, TCM_INSERTITEMW, (WPARAM) (int) (j), (LPARAM) (const TC_ITEMW *) (&tci)))
                {
                    ThmExitWithLastError(hr, "Failed to insert tab %u into tab control.", j);
                }
            }
        }

        if (pControlFont)
        {
            hr = EnsureFontInstance(pTheme, pControlFont, &pControlFontInstance);
            ThmExitOnFailure(hr, "Failed to get DPI specific font.");

            ::SendMessageW(pControl->hWnd, WM_SETFONT, (WPARAM) pControlFontInstance->hFont, FALSE);
        }

        // Initialize the text on all "application" (non-page) controls, best effort only.
        if (pTheme->pfnFormatString && !pControl->wPageId && pControl->sczText && *pControl->sczText)
        {
            HRESULT hrFormat = pTheme->pfnFormatString(pControl->sczText, &sczText, pTheme->pvVariableContext);
            if (SUCCEEDED(hrFormat))
            {
                ThemeSetTextControl(pControl, sczText);
            }
        }

        loadedControlArgs.pThemeControl = pControl;
        loadedControlResults.hr = E_NOTIMPL;
        if (::SendMessageW(pTheme->hwndParent, WM_THMUTIL_LOADED_CONTROL, reinterpret_cast<WPARAM>(&loadedControlArgs), reinterpret_cast<LPARAM>(&loadedControlResults)))
        {
            if (E_NOTIMPL != loadedControlResults.hr)
            {
                hr = loadedControlResults.hr;
                ThmExitOnFailure(hr, "ThmLoadedControl failed");
            }
        }
    }

LExit:
    ReleaseStr(sczText);

    return hr;
}

static HRESULT LocalizeControls(
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls,
    __in const WIX_LOCALIZATION *pWixLoc
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < cControls; ++i)
    {
        THEME_CONTROL* pControl = rgControls + i;
        hr = LocalizeControl(pControl, pWixLoc);
        ThmExitOnFailure(hr, "Failed to localize control: %ls", pControl->sczName);
    }

LExit:
    return hr;
}

static HRESULT LocalizeControl(
    __in THEME_CONTROL* pControl,
    __in const WIX_LOCALIZATION *pWixLoc
    )
{
    HRESULT hr = S_OK;
    LOC_CONTROL* pLocControl = NULL;
    LPWSTR sczLocStringId = NULL;

    if (pControl->sczText && *pControl->sczText)
    {
        hr = LocLocalizeString(pWixLoc, &pControl->sczText);
        ThmExitOnFailure(hr, "Failed to localize control text.");
    }
    else if (pControl->sczName)
    {
        LOC_STRING* plocString = NULL;

        hr = StrAllocFormatted(&sczLocStringId, L"#(loc.%ls)", pControl->sczName);
        ThmExitOnFailure(hr, "Failed to format loc string id: %ls", pControl->sczName);

        hr = LocGetString(pWixLoc, sczLocStringId, &plocString);
        if (E_NOTFOUND != hr)
        {
            ThmExitOnFailure(hr, "Failed to get loc string: %ls", pControl->sczName);

            hr = StrAllocString(&pControl->sczText, plocString->wzText, 0);
            ThmExitOnFailure(hr, "Failed to copy loc string to control: %ls", plocString->wzText);
        }
    }

    if (pControl->sczTooltip && *pControl->sczTooltip)
    {
        hr = LocLocalizeString(pWixLoc, &pControl->sczTooltip);
        ThmExitOnFailure(hr, "Failed to localize control tooltip text.");
    }

    if (pControl->sczNote && *pControl->sczNote)
    {
        hr = LocLocalizeString(pWixLoc, &pControl->sczNote);
        ThmExitOnFailure(hr, "Failed to localize control note text.");
    }

    for (DWORD j = 0; j < pControl->cConditionalText; ++j)
    {
        hr = LocLocalizeString(pWixLoc, &pControl->rgConditionalText[j].sczText);
        ThmExitOnFailure(hr, "Failed to localize conditional text.");
    }

    switch (pControl->type)
    {
    case THEME_CONTROL_TYPE_COMMANDLINK:
        for (DWORD j = 0; j < pControl->CommandLink.cConditionalNotes; ++j)
        {
            hr = LocLocalizeString(pWixLoc, &pControl->CommandLink.rgConditionalNotes[j].sczText);
            ThmExitOnFailure(hr, "Failed to localize conditional note.");
        }

        break;
    case THEME_CONTROL_TYPE_LISTVIEW:
        for (DWORD j = 0; j < pControl->ListView.cColumns; ++j)
        {
            hr = LocLocalizeString(pWixLoc, &pControl->ListView.ptcColumns[j].pszName);
            ThmExitOnFailure(hr, "Failed to localize column text.");
        }

        break;
    }

    for (DWORD j = 0; j < pControl->cTabs; ++j)
    {
        hr = LocLocalizeString(pWixLoc, &pControl->pttTabs[j].pszName);
        ThmExitOnFailure(hr, "Failed to localize tab text.");
    }

    // Localize control's size, location, and text.
    if (pControl->sczName)
    {
        hr = LocGetControl(pWixLoc, pControl->sczName, &pLocControl);
        if (E_NOTFOUND == hr)
        {
            ExitFunction1(hr = S_OK);
        }
        ThmExitOnFailure(hr, "Failed to localize control.");

        if (LOC_CONTROL_NOT_SET != pLocControl->nX)
        {
            pControl->nDefaultDpiX = pLocControl->nX;
        }

        if (LOC_CONTROL_NOT_SET != pLocControl->nY)
        {
            pControl->nDefaultDpiY = pLocControl->nY;
        }

        if (LOC_CONTROL_NOT_SET != pLocControl->nWidth)
        {
            pControl->nDefaultDpiWidth = pLocControl->nWidth;
        }

        if (LOC_CONTROL_NOT_SET != pLocControl->nHeight)
        {
            pControl->nDefaultDpiHeight = pLocControl->nHeight;
        }

        if (pLocControl->wzText && *pLocControl->wzText)
        {
            hr = StrAllocString(&pControl->sczText, pLocControl->wzText, 0);
            ThmExitOnFailure(hr, "Failed to localize control text.");
        }
    }

    hr = LocalizeControls(pControl->cControls, pControl->rgControls, pWixLoc);

LExit:
    ReleaseStr(sczLocStringId);

    return hr;
}

static HRESULT LoadControlsString(
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls,
    __in HMODULE hResModule
    )
{
    HRESULT hr = S_OK;

    for (DWORD i = 0; i < cControls; ++i)
    {
        THEME_CONTROL* pControl = rgControls + i;
        hr = LoadControlString(pControl, hResModule);
        ThmExitOnFailure(hr, "Failed to load string for control: %ls", pControl->sczName);
    }

LExit:
    return hr;
}

static HRESULT LoadControlString(
    __in THEME_CONTROL* pControl,
    __in HMODULE hResModule
    )
{
    HRESULT hr = S_OK;
    if (UINT_MAX != pControl->uStringId)
    {
        hr = ResReadString(hResModule, pControl->uStringId, &pControl->sczText);
        ThmExitOnFailure(hr, "Failed to load control text.");

        switch (pControl->type)
        {
        case THEME_CONTROL_TYPE_LISTVIEW:
            for (DWORD j = 0; j < pControl->ListView.cColumns; ++j)
            {
                THEME_COLUMN* pColumn = pControl->ListView.ptcColumns + j;
                if (UINT_MAX != pColumn->uStringId)
                {
                    hr = ResReadString(hResModule, pColumn->uStringId, &pColumn->pszName);
                    ThmExitOnFailure(hr, "Failed to load column text.");
                }
            }

            break;
        }

        for (DWORD j = 0; j < pControl->cTabs; ++j)
        {
            if (UINT_MAX != pControl->pttTabs[j].uStringId)
            {
                hr = ResReadString(hResModule, pControl->pttTabs[j].uStringId, &pControl->pttTabs[j].pszName);
                ThmExitOnFailure(hr, "Failed to load tab text.");
            }
        }
    }

    hr = LoadControlsString(pControl->cControls, pControl->rgControls, hResModule);

LExit:
    return hr;
}

static void ResizeControls(
    __in THEME* pTheme,
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls,
    __in const RECT* prcParent
    )
{
    for (DWORD i = 0; i < cControls; ++i)
    {
        THEME_CONTROL* pControl = rgControls + i;
        ResizeControl(pTheme, pControl, prcParent);
    }
}

static void ResizeControl(
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl,
    __in const RECT* prcParent
    )
{
    int w = 0;
    int h = 0;
    int x = 0;
    int y = 0;
    RECT rcControl = { };

    GetControlDimensions(pControl, prcParent, &w, &h, &x, &y);
    ::SetWindowPos(pControl->hWnd, NULL, x, y, w, h, SWP_NOACTIVATE | SWP_NOZORDER);

#ifdef DEBUG
    if (THEME_CONTROL_TYPE_BUTTON == pControl->type)
    {
        Trace(REPORT_STANDARD, "Resizing button (%ls/%ls) to (%d,%d)+(%d,%d) for parent (%d,%d)-(%d,%d)",
            pControl->sczName, pControl->sczText, x, y, w, h, prcParent->left, prcParent->top, prcParent->right, prcParent->bottom);
    }
#endif


    switch (pControl->type)
    {
    case THEME_CONTROL_TYPE_BUTTON:
        for (DWORD i = 0; i < (sizeof(pControl->Button.rgImageRef) / sizeof(pControl->Button.rgImageRef[0])); ++i)
        {
            ScaleImageReference(pTheme, pControl->Button.rgImageRef + i, w, h);
        }

        break;
    case THEME_CONTROL_TYPE_IMAGE:
        ScaleImageReference(pTheme, &pControl->Image.imageRef, w, h);
        break;
    case THEME_CONTROL_TYPE_LISTVIEW:
        SizeListViewColumns(pControl);

        for (DWORD j = 0; j < pControl->ListView.cColumns; ++j)
        {
            if (-1 == ::SendMessageW(pControl->hWnd, LVM_SETCOLUMNWIDTH, (WPARAM) (int) (j), (LPARAM) (pControl->ListView.ptcColumns[j].nWidth)))
            {
                Trace(REPORT_DEBUG, "Failed to resize listview column %u with error %u", j, ::GetLastError());
                return;
            }
        }

        break;
    case THEME_CONTROL_TYPE_PROGRESSBAR:
        for (DWORD i = 0; i < pControl->ProgressBar.cImageRef; ++i)
        {
            ScaleImageReference(pTheme, pControl->ProgressBar.rgImageRef + i, 4, h);
        }

        break;
    }

    if (pControl->cControls)
    {
        ::GetClientRect(pControl->hWnd, &rcControl);
        ResizeControls(pTheme, pControl->cControls, pControl->rgControls, &rcControl);
    }
}

static void ScaleThemeFromWindow(
    __in THEME* pTheme,
    __in UINT nDpi,
    __in int x,
    __in int y
    )
{
    DWORD dwStyle = GetWindowStyle(pTheme->hwndParent);
    BOOL fMenu = NULL != ::GetMenu(pTheme->hwndParent);
    DWORD dwExStyle = GetWindowExStyle(pTheme->hwndParent);

    ScaleTheme(pTheme, nDpi, x, y, dwStyle, fMenu, dwExStyle);
}

static void ScaleTheme(
    __in THEME* pTheme,
    __in UINT nDpi,
    __in int x,
    __in int y,
    __in DWORD dwStyle,
    __in BOOL fMenu,
    __in DWORD dwExStyle
    )
{
    pTheme->nDpi = nDpi;

    pTheme->nHeight = DpiuScaleValue(pTheme->nDefaultDpiHeight, pTheme->nDpi);
    pTheme->nWidth = DpiuScaleValue(pTheme->nDefaultDpiWidth, pTheme->nDpi);
    pTheme->nMinimumHeight = DpiuScaleValue(pTheme->nDefaultDpiMinimumHeight, pTheme->nDpi);
    pTheme->nMinimumWidth = DpiuScaleValue(pTheme->nDefaultDpiMinimumWidth, pTheme->nDpi);

    AdjustThemeWindowRect(pTheme, dwStyle, fMenu, dwExStyle);

    ScaleControls(pTheme, pTheme->cControls, pTheme->rgControls, pTheme->nDpi);

    if (pTheme->hwndParent)
    {
        ::SetWindowPos(pTheme->hwndParent, NULL, x, y, pTheme->nWindowWidth, pTheme->nWindowHeight, SWP_NOACTIVATE | SWP_NOZORDER);
    }
}

static void AdjustThemeWindowRect(
    __in THEME* pTheme,
    __in DWORD dwStyle,
    __in BOOL fMenu,
    __in DWORD dwExStyle
    )
{
    RECT rect = { };

    rect.left = 0;
    rect.top = 0;
    rect.right = pTheme->nWidth;
    rect.bottom = pTheme->nHeight;
    DpiuAdjustWindowRect(&rect, dwStyle, fMenu, dwExStyle, pTheme->nDpi);
    pTheme->nWindowWidth = rect.right - rect.left;
    pTheme->nWindowHeight = rect.bottom - rect.top;
}

static void ScaleControls(
    __in THEME* pTheme,
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls,
    __in UINT nDpi
    )
{
    for (DWORD i = 0; i < cControls; ++i)
    {
        THEME_CONTROL* pControl = rgControls + i;
        ScaleControl(pTheme, pControl, nDpi);
    }
}

static void ScaleControl(
    __in THEME* pTheme,
    __in THEME_CONTROL* pControl,
    __in UINT nDpi
    )
{
    HRESULT hr = S_OK;
    THEME_FONT* pControlFont = (pTheme->cFonts > pControl->dwFontId) ? pTheme->rgFonts + pControl->dwFontId : NULL;
    THEME_FONT_INSTANCE* pControlFontInstance = NULL;

    if (pControlFont)
    {
        hr = EnsureFontInstance(pTheme, pControlFont, &pControlFontInstance);
        if (SUCCEEDED(hr) && pControl->hWnd)
        {
            ::SendMessageW(pControl->hWnd, WM_SETFONT, (WPARAM)pControlFontInstance->hFont, FALSE);
        }
    }

    if (THEME_CONTROL_TYPE_LISTVIEW == pControl->type)
    {
        for (DWORD i = 0; i < pControl->ListView.cColumns; ++i)
        {
            THEME_COLUMN* pColumn = pControl->ListView.ptcColumns + i;

            pColumn->nBaseWidth = DpiuScaleValue(pColumn->nDefaultDpiBaseWidth, nDpi);
        }
    }

    pControl->nWidth = DpiuScaleValue(pControl->nDefaultDpiWidth, nDpi);
    pControl->nHeight = DpiuScaleValue(pControl->nDefaultDpiHeight, nDpi);
    pControl->nX = DpiuScaleValue(pControl->nDefaultDpiX, nDpi);
    pControl->nY = DpiuScaleValue(pControl->nDefaultDpiY, nDpi);

    if (pControl->cControls)
    {
        ScaleControls(pTheme, pControl->cControls, pControl->rgControls, nDpi);
    }
}

static void ScaleImageReference(
    __in THEME* pTheme,
    __in THEME_IMAGE_REFERENCE* pImageRef,
    __in int nDestWidth,
    __in int nDestHeight
    )
{
    THEME_IMAGE* pImage = NULL;
    THEME_IMAGE_INSTANCE* pDownscaleInstance = NULL;
    THEME_IMAGE_INSTANCE* pUpscaleInstance = NULL;
    THEME_IMAGE_INSTANCE* pInstance = NULL;
    DWORD dwIndex = THEME_INVALID_ID;
    DWORD64 qwPixels = 0;
    DWORD64 qwBestMatchPixels = 0;

    if (THEME_IMAGE_REFERENCE_TYPE_COMPLETE == pImageRef->type && THEME_INVALID_ID != pImageRef->dwImageIndex)
    {
        pImage = pTheme->rgImages + pImageRef->dwImageIndex;

        //The dimensions of the destination rectangle are compared to all of the available sources:
        //    1. If there is an exact match for width and height then that source will be used (no scaling required).
        //    2. If there is not an exact match then the smallest source whose width and height are larger or equal to the destination will be used and downscaled.
        //    3. If there is still no match then the largest source will be used and upscaled.
        for (DWORD i = 0; i < pImage->cImageInstances; ++i)
        {
            pInstance = pImage->rgImageInstances + i;

            if (nDestWidth == (int)pInstance->pBitmap->GetWidth() && nDestHeight == (int)pInstance->pBitmap->GetHeight())
            {
                dwIndex = i;
                break;
            }
            else if (nDestWidth <= (int)pInstance->pBitmap->GetWidth() && nDestHeight <= (int)pInstance->pBitmap->GetHeight())
            {
                qwPixels = (DWORD64)pInstance->pBitmap->GetWidth() * pInstance->pBitmap->GetHeight();
                if (!pDownscaleInstance || qwPixels < qwBestMatchPixels)
                {
                    qwBestMatchPixels = qwPixels;
                    pDownscaleInstance = pInstance;
                    dwIndex = i;
                }
            }
            else if (!pDownscaleInstance)
            {
                qwPixels = (DWORD64)pInstance->pBitmap->GetWidth() * pInstance->pBitmap->GetHeight();
                if (!pUpscaleInstance || qwPixels > qwBestMatchPixels)
                {
                    qwBestMatchPixels = qwPixels;
                    pUpscaleInstance = pInstance;
                    dwIndex = i;
                }
            }
        }

        if (THEME_INVALID_ID != dwIndex)
        {
            pImageRef->dwImageInstanceIndex = dwIndex;
        }
    }
}

static void UnloadControls(
    __in DWORD cControls,
    __in THEME_CONTROL* rgControls
    )
{
    for (DWORD i = 0; i < cControls; ++i)
    {
        THEME_CONTROL* pControl = rgControls + i;
        pControl->hWnd = NULL;

        UnloadControls(pControl->cControls, pControl->rgControls);
    }
}

