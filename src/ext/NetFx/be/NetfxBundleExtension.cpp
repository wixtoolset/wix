// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BextBaseBundleExtension.h"

class CWixNetfxBundleExtension : public CBextBaseBundleExtension
{
public: // IBundleExtension
    virtual STDMETHODIMP Search(
        __in LPCWSTR wzId,
        __in LPCWSTR wzVariable
        )
    {
        HRESULT hr = S_OK;

        hr = NetfxSearchExecute(&m_searches, wzId, wzVariable, m_pEngine, m_sczBaseDirectory);

        return hr;
    }

public: //CBextBaseBundleExtension
    virtual STDMETHODIMP Initialize(
        __in const BUNDLE_EXTENSION_CREATE_ARGS* pCreateArgs
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczModulePath = NULL;
        IXMLDOMDocument* pixdManifest = NULL;
        IXMLDOMNode* pixnBundleExtension = NULL;

        hr = __super::Initialize(pCreateArgs);
        BextExitOnFailure(hr, "CBextBaseBundleExtension initialization failed.");

        hr = PathForCurrentProcess(&sczModulePath, m_hInstance);
        BextExitOnFailure(hr, "Failed to get bundle extension path.");

        hr = PathGetDirectory(sczModulePath, &m_sczBaseDirectory);
        BextExitOnFailure(hr, "Failed to get bundle extension base directory.");

        hr = XmlLoadDocumentFromFile(m_sczBundleExtensionDataPath, &pixdManifest);
        BextExitOnFailure(hr, "Failed to load bundle extension manifest from path: %ls", m_sczBundleExtensionDataPath);

        hr = BextGetBundleExtensionDataNode(pixdManifest, NETFX_BUNDLE_EXTENSION_ID, &pixnBundleExtension);
        BextExitOnFailure(hr, "Failed to get BundleExtension '%ls'", NETFX_BUNDLE_EXTENSION_ID);

        hr = NetfxSearchParseFromXml(&m_searches, pixnBundleExtension);
        BextExitOnFailure(hr, "Failed to parse searches from bundle extension manifest.");

    LExit:
        ReleaseObject(pixnBundleExtension);
        ReleaseObject(pixdManifest);
        ReleaseStr(sczModulePath);

        return hr;
    }

public:
    CWixNetfxBundleExtension(
        __in HINSTANCE hInstance,
        __in IBundleExtensionEngine* pEngine
        ) : CBextBaseBundleExtension(pEngine)
    {
        m_searches = { };
        m_hInstance = hInstance;
        m_sczBaseDirectory = NULL;
    }

    ~CWixNetfxBundleExtension()
    {
        NetfxSearchUninitialize(&m_searches);
        ReleaseStr(m_sczBaseDirectory);
    }

private:
    NETFX_SEARCHES m_searches;
    HINSTANCE m_hInstance;
    LPWSTR m_sczBaseDirectory;
};

HRESULT NetfxBundleExtensionCreate(
    __in HINSTANCE hInstance,
    __in IBundleExtensionEngine* pEngine,
    __in const BUNDLE_EXTENSION_CREATE_ARGS* pArgs,
    __out IBundleExtension** ppBundleExtension
    )
{
    HRESULT hr = S_OK;
    CWixNetfxBundleExtension* pExtension = NULL;

    pExtension = new CWixNetfxBundleExtension(hInstance, pEngine);
    BextExitOnNull(pExtension, hr, E_OUTOFMEMORY, "Failed to create new CWixNetfxBundleExtension.");

    hr = pExtension->Initialize(pArgs);
    BextExitOnFailure(hr, "CWixNetfxBundleExtension initialization failed.");

    *ppBundleExtension = pExtension;
    pExtension = NULL;

LExit:
    ReleaseObject(pExtension);
    return hr;
}
