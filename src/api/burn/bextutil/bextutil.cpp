// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static IBundleExtensionEngine* vpEngine = NULL;

// prototypes

DAPI_(void) BextInitialize(
    __in IBundleExtensionEngine* pEngine
    )
{
    pEngine->AddRef();

    ReleaseObject(vpEngine);
    vpEngine = pEngine;
}

DAPI_(HRESULT) BextInitializeFromCreateArgs(
    __in const BUNDLE_EXTENSION_CREATE_ARGS* pArgs,
    __out_opt IBundleExtensionEngine** ppEngine
    )
{
    HRESULT hr = S_OK;
    IBundleExtensionEngine* pEngine = NULL;

    hr = BextBundleExtensionEngineCreate(pArgs->pfnBundleExtensionEngineProc, pArgs->pvBundleExtensionEngineProcContext, &pEngine);
    ExitOnFailure(hr, "Failed to create BextBundleExtensionEngine.");

    BextInitialize(pEngine);

    if (ppEngine)
    {
        *ppEngine = pEngine;
    }
    pEngine = NULL;

LExit:
    ReleaseObject(pEngine);

    return hr;
}


DAPI_(void) BextUninitialize()
{
    ReleaseNullObject(vpEngine);
}

DAPI_(HRESULT) BextGetBundleExtensionDataNode(
    __in IXMLDOMDocument* pixdManifest,
    __in LPCWSTR wzExtensionId,
    __out IXMLDOMNode** ppixnBundleExtension
    )
{
    HRESULT hr = S_OK;
    IXMLDOMElement* pixeBundleExtensionData = NULL;
    IXMLDOMNodeList* pixnNodes = NULL;
    IXMLDOMNode* pixnNode = NULL;
    DWORD cNodes = 0;
    LPWSTR sczId = NULL;

    // Get BundleExtensionData element.
    hr = pixdManifest->get_documentElement(&pixeBundleExtensionData);
    ExitOnFailure(hr, "Failed to get BundleExtensionData element.");

    // Select BundleExtension nodes.
    hr = XmlSelectNodes(pixeBundleExtensionData, L"BundleExtension", &pixnNodes);
    ExitOnFailure(hr, "Failed to select BundleExtension nodes.");

    // Get BundleExtension node count.
    hr = pixnNodes->get_length((long*)&cNodes);
    ExitOnFailure(hr, "Failed to get BundleExtension node count.");

    if (!cNodes)
    {
        ExitFunction();
    }

    // Find requested extension.
    for (DWORD i = 0; i < cNodes; ++i)
    {
        hr = XmlNextElement(pixnNodes, &pixnNode, NULL);
        ExitOnFailure(hr, "Failed to get next node.");

        // @Id
        hr = XmlGetAttributeEx(pixnNode, L"Id", &sczId);
        ExitOnFailure(hr, "Failed to get @Id.");

        if (CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, sczId, -1, wzExtensionId, -1))
        {
            *ppixnBundleExtension = pixnNode;
            pixnNode = NULL;

            ExitFunction1(hr = S_OK);
        }

        // Prepare next iteration.
        ReleaseNullObject(pixnNode);
    }

    hr = E_NOTFOUND;

LExit:
    ReleaseStr(sczId);
    ReleaseObject(pixnNode);
    ReleaseObject(pixnNodes);
    ReleaseObject(pixeBundleExtensionData);

    return hr;
}


DAPIV_(HRESULT) BextLog(
    __in BUNDLE_EXTENSION_LOG_LEVEL level,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BextInitialize() must be called first.");
    }

    va_start(args, szFormat);
    hr = BextLogArgs(level, szFormat, args);
    va_end(args);

LExit:
    return hr;
}


DAPI_(HRESULT) BextLogArgs(
    __in BUNDLE_EXTENSION_LOG_LEVEL level,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    HRESULT hr = S_OK;
    LPSTR sczFormattedAnsi = NULL;
    LPWSTR sczMessage = NULL;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BextInitialize() must be called first.");
    }

    hr = StrAnsiAllocFormattedArgs(&sczFormattedAnsi, szFormat, args);
    ExitOnFailure(hr, "Failed to format log string.");

    hr = StrAllocStringAnsi(&sczMessage, sczFormattedAnsi, 0, CP_UTF8);
    ExitOnFailure(hr, "Failed to convert log string to Unicode.");

    hr = vpEngine->Log(level, sczMessage);

LExit:
    ReleaseStr(sczMessage);
    ReleaseStr(sczFormattedAnsi);
    return hr;
}


DAPIV_(HRESULT) BextLogError(
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    ...
    )
{
    HRESULT hr = S_OK;
    va_list args;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BextInitialize() must be called first.");
    }

    va_start(args, szFormat);
    hr = BextLogErrorArgs(hrError, szFormat, args);
    va_end(args);

LExit:
    return hr;
}


DAPI_(HRESULT) BextLogErrorArgs(
    __in HRESULT hrError,
    __in_z __format_string LPCSTR szFormat,
    __in va_list args
    )
{
    HRESULT hr = S_OK;
    LPSTR sczFormattedAnsi = NULL;
    LPWSTR sczMessage = NULL;

    if (!vpEngine)
    {
        hr = E_POINTER;
        ExitOnRootFailure(hr, "BextInitialize() must be called first.");
    }

    hr = StrAnsiAllocFormattedArgs(&sczFormattedAnsi, szFormat, args);
    ExitOnFailure(hr, "Failed to format error log string.");

    hr = StrAllocFormatted(&sczMessage, L"Error 0x%08x: %S", hrError, sczFormattedAnsi);
    ExitOnFailure(hr, "Failed to prepend error number to error log string.");

    hr = vpEngine->Log(BUNDLE_EXTENSION_LOG_LEVEL_ERROR, sczMessage);

LExit:
    ReleaseStr(sczMessage);
    ReleaseStr(sczFormattedAnsi);
    return hr;
}
