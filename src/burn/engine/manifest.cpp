// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


static HRESULT ParseFromXml(
    __in IXMLDOMDocument* pixdDocument,
    __in BURN_ENGINE_STATE* pEngineState
    );
#if DEBUG
static void ValidateHarvestingAttributes(
    __in IXMLDOMDocument* pixdDocument
    );
#endif

// function definitions

extern "C" HRESULT ManifestLoadXmlFromFile(
    __in LPCWSTR wzPath,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    IXMLDOMDocument* pixdDocument = NULL;

    // load xml document
    hr = XmlLoadDocumentFromFile(wzPath, &pixdDocument);
    ExitOnFailure(hr, "Failed to load manifest as XML document.");

    hr = ParseFromXml(pixdDocument, pEngineState);

LExit:
    ReleaseObject(pixdDocument);

    return hr;
}

extern "C" HRESULT ManifestLoadXmlFromBuffer(
    __in_bcount(cbBuffer) BYTE* pbBuffer,
    __in SIZE_T cbBuffer,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    IXMLDOMDocument* pixdDocument = NULL;

    // load xml document
    hr = XmlLoadDocumentFromBuffer(pbBuffer, cbBuffer, &pixdDocument);
    ExitOnFailure(hr, "Failed to load manifest as XML document.");

#if DEBUG
    ValidateHarvestingAttributes(pixdDocument);
#endif

    hr = ParseFromXml(pixdDocument, pEngineState);

LExit:
    ReleaseObject(pixdDocument);

    return hr;
}

static HRESULT ParseFromXml(
    __in IXMLDOMDocument* pixdDocument,
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    IXMLDOMElement* pixeBundle = NULL;
    IXMLDOMNode* pixnChain = NULL;

    // get bundle element
    hr = pixdDocument->get_documentElement(&pixeBundle);
    ExitOnFailure(hr, "Failed to get bundle element.");

    hr = LoggingParseFromXml(&pEngineState->log, pixeBundle);
    ExitOnFailure(hr, "Failed to parse logging.");

    // get the chain element
    hr = XmlSelectSingleNode(pixeBundle, L"Chain", &pixnChain);
    ExitOnFailure(hr, "Failed to get chain element.");

    if (S_OK == hr)
    {
        // parse disable rollback
        hr = XmlGetYesNoAttribute(pixnChain, L"DisableRollback", &pEngineState->fDisableRollback);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get Chain/@DisableRollback");
        }

        // parse disable system restore
        hr = XmlGetYesNoAttribute(pixnChain, L"DisableSystemRestore", &pEngineState->internalCommand.fDisableSystemRestore);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get Chain/@DisableSystemRestore");
        }

        // parse parallel cache
        hr = XmlGetYesNoAttribute(pixnChain, L"ParallelCache", &pEngineState->fParallelCacheAndExecute);
        if (E_NOTFOUND != hr)
        {
            ExitOnFailure(hr, "Failed to get Chain/@ParallelCache");
        }
    }

    // parse built-in condition
    hr = ConditionGlobalParseFromXml(&pEngineState->condition, pixeBundle);
    ExitOnFailure(hr, "Failed to parse global condition.");

    // parse variables
    hr = VariablesParseFromXml(&pEngineState->variables, pixeBundle);
    ExitOnFailure(hr, "Failed to parse variables.");

    // parse user experience
    hr = BootstrapperApplicationParseFromXml(&pEngineState->userExperience, pixeBundle);
    ExitOnFailure(hr, "Failed to parse bootstrapper application.");

    // parse extensions
    hr = BurnExtensionParseFromXml(&pEngineState->extensions, &pEngineState->userExperience.payloads, pixeBundle);
    ExitOnFailure(hr, "Failed to parse extensions.");

    // parse searches
    hr = SearchesParseFromXml(&pEngineState->searches, &pEngineState->extensions, pixeBundle);
    ExitOnFailure(hr, "Failed to parse searches.");

    // parse registration
    hr = RegistrationParseFromXml(&pEngineState->registration, &pEngineState->cache, pixeBundle);
    ExitOnFailure(hr, "Failed to parse registration.");

    // parse update
    hr = UpdateParseFromXml(&pEngineState->update, pixeBundle);
    ExitOnFailure(hr, "Failed to parse update.");

    // parse containers
    hr = ContainersParseFromXml(&pEngineState->containers, pixeBundle, &pEngineState->extensions);
    ExitOnFailure(hr, "Failed to parse containers.");

    // parse payloads
    hr = PayloadsParseFromXml(&pEngineState->payloads, &pEngineState->containers, &pEngineState->layoutPayloads, pixeBundle);
    ExitOnFailure(hr, "Failed to parse payloads.");

    // parse packages
    hr = PackagesParseFromXml(&pEngineState->packages, &pEngineState->payloads, pixeBundle);
    ExitOnFailure(hr, "Failed to parse packages.");

    // parse approved exes for elevation
    hr = ApprovedExesParseFromXml(&pEngineState->approvedExes, pixeBundle);
    ExitOnFailure(hr, "Failed to parse approved exes.");

LExit:
    ReleaseObject(pixnChain);
    ReleaseObject(pixeBundle);
    return hr;
}

#if DEBUG
static void ValidateHarvestingAttributes(
    __in IXMLDOMDocument* pixdDocument
    )
{
    HRESULT hr = S_OK;
    IXMLDOMElement* pixeBundle = NULL;
    LPWSTR sczEngineVersion = NULL;
    DWORD dwProtocolVersion = 0;
    BOOL fWin64 = FALSE;

    hr = pixdDocument->get_documentElement(&pixeBundle);
    ExitOnFailure(hr, "Failed to get document element.");

    hr = XmlGetAttributeEx(pixeBundle, L"EngineVersion", &sczEngineVersion);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get BurnManifest/@EngineVersion attribute.");

    hr = XmlGetAttributeUInt32(pixeBundle, L"ProtocolVersion", &dwProtocolVersion);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get BurnManifest/@ProtocolVersion attribute.");

    hr = XmlGetYesNoAttribute(pixeBundle, L"Win64", &fWin64);
    ExitOnRequiredXmlQueryFailure(hr, "Failed to get BurnManifest/@Win64 attribute.");

    Assert(CSTR_EQUAL == ::CompareStringW(LOCALE_INVARIANT, 0, sczEngineVersion, -1, wzVerMajorMinorBuild, -1));

    Assert(BURN_PROTOCOL_VERSION == dwProtocolVersion);

#if !defined(_WIN64)
    Assert(!fWin64);
#else
    Assert(fWin64);
#endif

LExit:
    AssertSz(SUCCEEDED(hr), "Failed to get harvesting attributes.");

    ReleaseStr(sczEngineVersion);
    ReleaseObject(pixeBundle);
}
#endif
