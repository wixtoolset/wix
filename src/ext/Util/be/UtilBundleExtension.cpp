// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BextBaseBundleExtension.h"

class CWixUtilBundleExtension : public CBextBaseBundleExtension
{
public: // IBundleExtension
    virtual STDMETHODIMP Search(
        __in LPCWSTR wzId,
        __in LPCWSTR wzVariable
        )
    {
        HRESULT hr = S_OK;

        hr = UtilSearchExecute(&m_searches, wzId, wzVariable, m_pEngine);

        return hr;
    }

public: //CBextBaseBundleExtension
    virtual STDMETHODIMP Initialize(
        __in const BUNDLE_EXTENSION_CREATE_ARGS* pCreateArgs
        )
    {
        HRESULT hr = S_OK;
        IXMLDOMDocument* pixdManifest = NULL;
        IXMLDOMNode* pixnBundleExtension = NULL;

        hr = CBextBaseBundleExtension::Initialize(pCreateArgs);
        ExitOnFailure(hr, "CBextBaseBundleExtension initialization failed.");

        hr = XmlLoadDocumentFromFile(m_sczBundleExtensionDataPath, &pixdManifest);
        ExitOnFailure(hr, "Failed to load bundle extension manifest from path: %ls", m_sczBundleExtensionDataPath);

        hr = BextGetBundleExtensionDataNode(pixdManifest, UTIL_BUNDLE_EXTENSION_ID, &pixnBundleExtension);
        ExitOnFailure(hr, "Failed to get BundleExtension '%ls'", UTIL_BUNDLE_EXTENSION_ID);

        hr = UtilSearchParseFromXml(&m_searches, pixnBundleExtension);
        ExitOnFailure(hr, "Failed to parse searches from bundle extension manifest.");

    LExit:
        ReleaseObject(pixnBundleExtension);
        ReleaseObject(pixdManifest);

        return hr;
    }

public:
    CWixUtilBundleExtension(
        __in IBundleExtensionEngine* pEngine
        ) : CBextBaseBundleExtension(pEngine)
    {
        m_searches = { };
    }

    ~CWixUtilBundleExtension()
    {
        UtilSearchUninitialize(&m_searches);
    }

private:
    UTIL_SEARCHES m_searches;
};

HRESULT UtilBundleExtensionCreate(
    __in IBundleExtensionEngine* pEngine,
    __in const BUNDLE_EXTENSION_CREATE_ARGS* pArgs,
    __out IBundleExtension** ppBundleExtension
    )
{
    HRESULT hr = S_OK;
    CWixUtilBundleExtension* pExtension = NULL;

    pExtension = new CWixUtilBundleExtension(pEngine);
    ExitOnNull(pExtension, hr, E_OUTOFMEMORY, "Failed to create new CWixUtilBundleExtension.");

    hr = pExtension->Initialize(pArgs);
    ExitOnFailure(hr, "CWixUtilBundleExtension initialization failed");

    *ppBundleExtension = pExtension;
    pExtension = NULL;

LExit:
    ReleaseObject(pExtension);
    return hr;
}
