// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"
#include "BextBaseBootstrapperExtension.h"

class CWixUtilBootstrapperExtension : public CBextBaseBootstrapperExtension
{
public: // IBootstrapperExtension
    virtual STDMETHODIMP Search(
        __in LPCWSTR wzId,
        __in LPCWSTR wzVariable
        )
    {
        HRESULT hr = S_OK;

        hr = UtilSearchExecute(&m_searches, wzId, wzVariable, m_pEngine);

        return hr;
    }

public: //CBextBaseBootstrapperExtension
    virtual STDMETHODIMP Initialize(
        __in const BOOTSTRAPPER_EXTENSION_CREATE_ARGS* pCreateArgs
        )
    {
        HRESULT hr = S_OK;
        IXMLDOMDocument* pixdManifest = NULL;
        IXMLDOMNode* pixnBootstrapperExtension = NULL;

        hr = __super::Initialize(pCreateArgs);
        BextExitOnFailure(hr, "CBextBaseBootstrapperExtension initialization failed.");

        hr = XmlLoadDocumentFromFile(m_sczBootstrapperExtensionDataPath, &pixdManifest);
        BextExitOnFailure(hr, "Failed to load bundle extension manifest from path: %ls", m_sczBootstrapperExtensionDataPath);

        hr = BextGetBootstrapperExtensionDataNode(pixdManifest, UTIL_BOOTSTRAPPER_EXTENSION_ID, &pixnBootstrapperExtension);
        BextExitOnFailure(hr, "Failed to get BootstrapperExtension '%ls'", UTIL_BOOTSTRAPPER_EXTENSION_ID);

        hr = UtilSearchParseFromXml(&m_searches, pixnBootstrapperExtension);
        BextExitOnFailure(hr, "Failed to parse searches from bundle extension manifest.");

    LExit:
        ReleaseObject(pixnBootstrapperExtension);
        ReleaseObject(pixdManifest);

        return hr;
    }

public:
    CWixUtilBootstrapperExtension(
        __in IBootstrapperExtensionEngine* pEngine
        ) : CBextBaseBootstrapperExtension(pEngine)
    {
        m_searches = { };
    }

    ~CWixUtilBootstrapperExtension()
    {
        UtilSearchUninitialize(&m_searches);
    }

private:
    UTIL_SEARCHES m_searches;
};

HRESULT UtilBootstrapperExtensionCreate(
    __in IBootstrapperExtensionEngine* pEngine,
    __in const BOOTSTRAPPER_EXTENSION_CREATE_ARGS* pArgs,
    __out IBootstrapperExtension** ppBootstrapperExtension
    )
{
    HRESULT hr = S_OK;
    CWixUtilBootstrapperExtension* pExtension = NULL;

    pExtension = new CWixUtilBootstrapperExtension(pEngine);
    BextExitOnNull(pExtension, hr, E_OUTOFMEMORY, "Failed to create new CWixUtilBootstrapperExtension.");

    hr = pExtension->Initialize(pArgs);
    BextExitOnFailure(hr, "CWixUtilBootstrapperExtension initialization failed.");

    *ppBootstrapperExtension = pExtension;
    pExtension = NULL;

LExit:
    ReleaseObject(pExtension);
    return hr;
}
