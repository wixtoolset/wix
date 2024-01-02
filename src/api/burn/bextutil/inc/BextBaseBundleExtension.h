// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include <windows.h>

#include "BundleExtensionEngine.h"
#include "BundleExtension.h"
#include "IBundleExtensionEngine.h"
#include "IBundleExtension.h"

#include "bextutil.h"

class CBextBaseBundleExtension : public IBundleExtension
{
public: // IUnknown
    virtual STDMETHODIMP QueryInterface(
        __in REFIID riid,
        __out LPVOID *ppvObject
        )
    {
        if (!ppvObject)
        {
            return E_INVALIDARG;
        }

        *ppvObject = NULL;

        if (::IsEqualIID(__uuidof(IBundleExtension), riid))
        {
            *ppvObject = static_cast<IBundleExtension*>(this);
        }
        else if (::IsEqualIID(IID_IUnknown, riid))
        {
            *ppvObject = static_cast<IUnknown*>(this);
        }
        else // no interface for requested iid
        {
            return E_NOINTERFACE;
        }

        AddRef();
        return S_OK;
    }

    virtual STDMETHODIMP_(ULONG) AddRef()
    {
        return ::InterlockedIncrement(&this->m_cReferences);
    }

    virtual STDMETHODIMP_(ULONG) Release()
    {
        long l = ::InterlockedDecrement(&this->m_cReferences);
        if (0 < l)
        {
            return l;
        }

        delete this;
        return 0;
    }

public: // IBundleExtension
    virtual STDMETHODIMP Search(
        __in LPCWSTR /*wzId*/,
        __in LPCWSTR /*wzVariable*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP ContainerOpen(
        __in LPCWSTR /*wzContainerId*/,
        __in LPCWSTR /*wzFilePath*/,
        __out LPVOID* /*pContext*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP ContainerOpenAttached(
            __in LPCWSTR /*wzContainerId*/,
            __in HANDLE /*hBundle*/,
            __in DWORD64 /*qwContainerStartPos*/,
            __in DWORD64 /*qwContainerSize*/,
            __out LPVOID* /*ppContext*/
        )
    {
        return E_NOTIMPL;
    }

    // Implementor should keep the stream name in the contex, to release it when done
    virtual STDMETHODIMP ContainerNextStream(
        __in LPVOID /*pContext*/,
        __inout_z LPWSTR* /*psczStreamName*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP ContainerStreamToFile(
        __in LPVOID /*pContext*/,
        __in_z LPCWSTR /*wzFileName*/
        )
    {
        return E_NOTIMPL;
    }

    // Not really needed because it is only used to read the manifest by the engine, and that is always a cab.
    virtual STDMETHODIMP ContainerStreamToBuffer(
        __in LPVOID /*pContext*/,
        __out BYTE** /*ppbBuffer*/,
        __out SIZE_T* /*pcbBuffer*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP ContainerSkipStream(
        __in LPVOID /*pContext*/
        )
    {
        return E_NOTIMPL;
    }

    // Don't forget to release everything in the context
    virtual STDMETHODIMP ContainerClose(
        __in LPVOID /*pContext*/
        )
    {
        return E_NOTIMPL;
    }

    virtual STDMETHODIMP BundleExtensionProc(
        __in BUNDLE_EXTENSION_MESSAGE /*message*/,
        __in const LPVOID /*pvArgs*/,
        __inout LPVOID /*pvResults*/,
        __in_opt LPVOID /*pvContext*/
        )
    {
        return E_NOTIMPL;
    }

public: //CBextBaseBundleExtension
    virtual STDMETHODIMP Initialize(
        __in const BUNDLE_EXTENSION_CREATE_ARGS* pCreateArgs
        )
    {
        HRESULT hr = S_OK;

        hr = StrAllocString(&m_sczBundleExtensionDataPath, pCreateArgs->wzBundleExtensionDataPath, 0);
        ExitOnFailure(hr, "Failed to copy BundleExtensionDataPath.");

    LExit:
        return hr;
    }

protected:

    CBextBaseBundleExtension(
        __in IBundleExtensionEngine* pEngine
        )
    {
        m_cReferences = 1;

        pEngine->AddRef();
        m_pEngine = pEngine;

        m_sczBundleExtensionDataPath = NULL;
    }

    virtual ~CBextBaseBundleExtension()
    {
        ReleaseNullObject(m_pEngine);
        ReleaseStr(m_sczBundleExtensionDataPath);
    }

protected:
    IBundleExtensionEngine* m_pEngine;
    LPWSTR m_sczBundleExtensionDataPath;

private:
    long m_cReferences;
};
