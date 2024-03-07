// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BextBaseBootstrapperExtension.h"

class CWixNetfxBootstrapperExtension : public CBextBaseBootstrapperExtension
{
public: // IBootstrapperExtension
    virtual STDMETHODIMP Search(
        __in LPCWSTR wzId,
        __in LPCWSTR wzVariable
        )
    {
        HRESULT hr = S_OK;

        hr = NetfxSearchExecute(&m_searches, wzId, wzVariable, m_pEngine, m_sczBaseDirectory);

        return hr;
    }

public: //CBextBaseBootstrapperExtension
    virtual STDMETHODIMP Initialize(
        __in const BOOTSTRAPPER_EXTENSION_CREATE_ARGS* pCreateArgs
        )
    {
        HRESULT hr = S_OK;
        LPWSTR sczModulePath = NULL;
        IXMLDOMDocument* pixdManifest = NULL;
        IXMLDOMNode* pixnBootstrapperExtension = NULL;

        hr = __super::Initialize(pCreateArgs);
        BextExitOnFailure(hr, "CBextBaseBootstrapperExtension initialization failed.");

        hr = PathForCurrentProcess(&sczModulePath, m_hInstance);
        BextExitOnFailure(hr, "Failed to get bundle extension path.");

        hr = PathGetDirectory(sczModulePath, &m_sczBaseDirectory);
        BextExitOnFailure(hr, "Failed to get bundle extension base directory.");

        hr = XmlLoadDocumentFromFile(m_sczBootstrapperExtensionDataPath, &pixdManifest);
        BextExitOnFailure(hr, "Failed to load bundle extension manifest from path: %ls", m_sczBootstrapperExtensionDataPath);

        hr = BextGetBootstrapperExtensionDataNode(pixdManifest, NETFX_BOOTSTRAPPER_EXTENSION_ID, &pixnBootstrapperExtension);
        BextExitOnFailure(hr, "Failed to get BootstrapperExtension '%ls'", NETFX_BOOTSTRAPPER_EXTENSION_ID);

        hr = NetfxSearchParseFromXml(&m_searches, pixnBootstrapperExtension);
        BextExitOnFailure(hr, "Failed to parse searches from bundle extension manifest.");

    LExit:
        ReleaseObject(pixnBootstrapperExtension);
        ReleaseObject(pixdManifest);
        ReleaseStr(sczModulePath);

        return hr;
    }

public:
    CWixNetfxBootstrapperExtension(
        __in HINSTANCE hInstance,
        __in IBootstrapperExtensionEngine* pEngine
        ) : CBextBaseBootstrapperExtension(pEngine)
    {
        m_searches = { };
        m_hInstance = hInstance;
        m_sczBaseDirectory = NULL;
    }

    ~CWixNetfxBootstrapperExtension()
    {
        NetfxSearchUninitialize(&m_searches);
        ReleaseStr(m_sczBaseDirectory);
    }

private:
    NETFX_SEARCHES m_searches;
    HINSTANCE m_hInstance;
    LPWSTR m_sczBaseDirectory;
};

HRESULT NetfxBootstrapperExtensionCreate(
    __in HINSTANCE hInstance,
    __in IBootstrapperExtensionEngine* pEngine,
    __in const BOOTSTRAPPER_EXTENSION_CREATE_ARGS* pArgs,
    __out IBootstrapperExtension** ppBootstrapperExtension
    )
{
    HRESULT hr = S_OK;
    CWixNetfxBootstrapperExtension* pExtension = NULL;

    pExtension = new CWixNetfxBootstrapperExtension(hInstance, pEngine);
    BextExitOnNull(pExtension, hr, E_OUTOFMEMORY, "Failed to create new CWixNetfxBootstrapperExtension.");

    hr = pExtension->Initialize(pArgs);
    BextExitOnFailure(hr, "CWixNetfxBootstrapperExtension initialization failed.");

    *ppBootstrapperExtension = pExtension;
    pExtension = NULL;

LExit:
    ReleaseObject(pExtension);
    return hr;
}
