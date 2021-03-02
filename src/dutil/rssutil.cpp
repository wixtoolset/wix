// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


// Exit macros
#define RssExitOnLastError(x, s, ...) ExitOnLastErrorSource(DUTIL_SOURCE_RSSUTIL, x, s, __VA_ARGS__)
#define RssExitOnLastErrorDebugTrace(x, s, ...) ExitOnLastErrorDebugTraceSource(DUTIL_SOURCE_RSSUTIL, x, s, __VA_ARGS__)
#define RssExitWithLastError(x, s, ...) ExitWithLastErrorSource(DUTIL_SOURCE_RSSUTIL, x, s, __VA_ARGS__)
#define RssExitOnFailure(x, s, ...) ExitOnFailureSource(DUTIL_SOURCE_RSSUTIL, x, s, __VA_ARGS__)
#define RssExitOnRootFailure(x, s, ...) ExitOnRootFailureSource(DUTIL_SOURCE_RSSUTIL, x, s, __VA_ARGS__)
#define RssExitOnFailureDebugTrace(x, s, ...) ExitOnFailureDebugTraceSource(DUTIL_SOURCE_RSSUTIL, x, s, __VA_ARGS__)
#define RssExitOnNull(p, x, e, s, ...) ExitOnNullSource(DUTIL_SOURCE_RSSUTIL, p, x, e, s, __VA_ARGS__)
#define RssExitOnNullWithLastError(p, x, s, ...) ExitOnNullWithLastErrorSource(DUTIL_SOURCE_RSSUTIL, p, x, s, __VA_ARGS__)
#define RssExitOnNullDebugTrace(p, x, e, s, ...)  ExitOnNullDebugTraceSource(DUTIL_SOURCE_RSSUTIL, p, x, e, s, __VA_ARGS__)
#define RssExitOnInvalidHandleWithLastError(p, x, s, ...) ExitOnInvalidHandleWithLastErrorSource(DUTIL_SOURCE_RSSUTIL, p, x, s, __VA_ARGS__)
#define RssExitOnWin32Error(e, x, s, ...) ExitOnWin32ErrorSource(DUTIL_SOURCE_RSSUTIL, e, x, s, __VA_ARGS__)
#define RssExitOnGdipFailure(g, x, s, ...) ExitOnGdipFailureSource(DUTIL_SOURCE_RSSUTIL, g, x, s, __VA_ARGS__)

static HRESULT ParseRssDocument(
    __in IXMLDOMDocument *pixd,
    __out RSS_CHANNEL **ppChannel
    );
static HRESULT ParseRssChannel(
    __in IXMLDOMNode *pixnChannel,
    __out RSS_CHANNEL **ppChannel
    );
static HRESULT ParseRssItem(
    __in IXMLDOMNode *pixnItem,
    __in DWORD cItem,
    __in_xcount(pChannel->cItems) RSS_CHANNEL *pChannel
    );
static HRESULT ParseRssUnknownElement(
    __in IXMLDOMNode *pNode,
    __inout RSS_UNKNOWN_ELEMENT** ppUnknownElement
    );
static HRESULT ParseRssUnknownAttribute(
    __in IXMLDOMNode *pNode,
    __inout RSS_UNKNOWN_ATTRIBUTE** ppUnknownAttribute
    );
static void FreeRssUnknownElementList(
    __in_opt RSS_UNKNOWN_ELEMENT* pUnknownElement
    );
static void FreeRssUnknownAttributeList(
    __in_opt RSS_UNKNOWN_ATTRIBUTE* pUnknownAttribute
    );


/********************************************************************
 RssInitialize - Initialize RSS utilities.

*********************************************************************/
extern "C" HRESULT DAPI RssInitialize()
{
    return XmlInitialize();
}


/********************************************************************
 RssUninitialize - Uninitialize RSS utilities.

*********************************************************************/
extern "C" void DAPI RssUninitialize()
{
    XmlUninitialize();
}


/********************************************************************
 RssParseFromString - parses out an RSS channel from a string.

*********************************************************************/
extern "C" HRESULT DAPI RssParseFromString(
    __in_z LPCWSTR wzRssString,
    __out RSS_CHANNEL **ppChannel
    )
{
    Assert(wzRssString);
    Assert(ppChannel);

    HRESULT hr = S_OK;
    RSS_CHANNEL *pNewChannel = NULL;
    IXMLDOMDocument *pixdRss = NULL;

    hr = XmlLoadDocument(wzRssString, &pixdRss);
    RssExitOnFailure(hr, "Failed to load RSS string as XML document.");

    hr = ParseRssDocument(pixdRss, &pNewChannel);
    RssExitOnFailure(hr, "Failed to parse RSS document.");

    *ppChannel = pNewChannel;
    pNewChannel = NULL;

LExit:
    ReleaseObject(pixdRss);

    ReleaseRssChannel(pNewChannel);

    return hr;
}


/********************************************************************
 RssParseFromFile - parses out an RSS channel from a file path.

*********************************************************************/
extern "C" HRESULT DAPI RssParseFromFile(
    __in_z LPCWSTR wzRssFile,
    __out RSS_CHANNEL **ppChannel
    )
{
    Assert(wzRssFile);
    Assert(ppChannel);

    HRESULT hr = S_OK;
    RSS_CHANNEL *pNewChannel = NULL;
    IXMLDOMDocument *pixdRss = NULL;

    hr = XmlLoadDocumentFromFile(wzRssFile, &pixdRss);
    RssExitOnFailure(hr, "Failed to load RSS string as XML document.");

    hr = ParseRssDocument(pixdRss, &pNewChannel);
    RssExitOnFailure(hr, "Failed to parse RSS document.");

    *ppChannel = pNewChannel;
    pNewChannel = NULL;

LExit:
    ReleaseObject(pixdRss);

    ReleaseRssChannel(pNewChannel);

    return hr;
}


/********************************************************************
 RssFreeChannel - parses out an RSS channel from a string.

*********************************************************************/
extern "C" void DAPI RssFreeChannel(
    __in_xcount(pChannel->cItems) RSS_CHANNEL *pChannel
    )
{
    if (pChannel)
    {
        for (DWORD i = 0; i < pChannel->cItems; ++i)
        {
            ReleaseStr(pChannel->rgItems[i].wzTitle);
            ReleaseStr(pChannel->rgItems[i].wzLink);
            ReleaseStr(pChannel->rgItems[i].wzDescription);
            ReleaseStr(pChannel->rgItems[i].wzGuid);
            ReleaseStr(pChannel->rgItems[i].wzEnclosureUrl);
            ReleaseStr(pChannel->rgItems[i].wzEnclosureType);

            FreeRssUnknownElementList(pChannel->rgItems[i].pUnknownElements);
        }

        ReleaseStr(pChannel->wzTitle);
        ReleaseStr(pChannel->wzLink);
        ReleaseStr(pChannel->wzDescription);
        FreeRssUnknownElementList(pChannel->pUnknownElements);

        MemFree(pChannel);
    }
}


/********************************************************************
 ParseRssDocument - parses out an RSS channel from a loaded XML DOM document.

*********************************************************************/
static HRESULT ParseRssDocument(
    __in IXMLDOMDocument *pixd,
    __out RSS_CHANNEL **ppChannel
    )
{
    Assert(pixd);
    Assert(ppChannel);

    HRESULT hr = S_OK;
    IXMLDOMElement *pRssElement = NULL;
    IXMLDOMNodeList *pChannelNodes = NULL;
    IXMLDOMNode *pNode = NULL;
    BSTR bstrNodeName = NULL;

    RSS_CHANNEL *pNewChannel = NULL;

    //
    // Get the document element and start processing channels.
    //
    hr = pixd ->get_documentElement(&pRssElement);
    RssExitOnFailure(hr, "failed get_documentElement in ParseRssDocument");

    hr = pRssElement->get_childNodes(&pChannelNodes);
    RssExitOnFailure(hr, "Failed to get child nodes of Rss Document element.");

    while (S_OK == (hr = XmlNextElement(pChannelNodes, &pNode, &bstrNodeName)))
    {
        if (0 == lstrcmpW(bstrNodeName, L"channel"))
        {
            hr = ParseRssChannel(pNode, &pNewChannel);
            RssExitOnFailure(hr, "Failed to parse RSS channel.");
        }
        else if (0 == lstrcmpW(bstrNodeName, L"link"))
        {
        }

        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }

    *ppChannel = pNewChannel;
    pNewChannel = NULL;

LExit:
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pNode);
    ReleaseObject(pChannelNodes);
    ReleaseObject(pRssElement);

    ReleaseRssChannel(pNewChannel);

    return hr;
}


/********************************************************************
 ParseRssChannel - parses out an RSS channel from a loaded XML DOM element.

*********************************************************************/
static HRESULT ParseRssChannel(
    __in IXMLDOMNode *pixnChannel,
    __out RSS_CHANNEL **ppChannel
    )
{
    Assert(pixnChannel);
    Assert(ppChannel);

    HRESULT hr = S_OK;
    IXMLDOMNodeList *pNodeList = NULL;

    RSS_CHANNEL *pNewChannel = NULL;
    long cItems = 0;

    IXMLDOMNode *pNode = NULL;
    BSTR bstrNodeName = NULL;
    BSTR bstrNodeValue = NULL;

    //
    // First, calculate how many RSS items we're going to have and allocate
    // the RSS_CHANNEL structure
    //
    hr = XmlSelectNodes(pixnChannel, L"item", &pNodeList);
    RssExitOnFailure(hr, "Failed to select all RSS items in an RSS channel.");

    hr = pNodeList->get_length(&cItems);
    RssExitOnFailure(hr, "Failed to count the number of RSS items in RSS channel.");

    pNewChannel = static_cast<RSS_CHANNEL*>(MemAlloc(sizeof(RSS_CHANNEL) + sizeof(RSS_ITEM) * cItems, TRUE));
    RssExitOnNull(pNewChannel, hr, E_OUTOFMEMORY, "Failed to allocate RSS channel structure.");

    pNewChannel->cItems = cItems;

    //
    // Process the elements under a channel now.
    //
    hr = pixnChannel->get_childNodes(&pNodeList);
    RssExitOnFailure(hr, "Failed to get child nodes of RSS channel element.");

    cItems = 0; // reset the counter and use this to walk through the channel items
    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, &bstrNodeName)))
    {
        if (0 == lstrcmpW(bstrNodeName, L"title"))
        {
            hr = XmlGetText(pNode, &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS channel title.");

            hr = StrAllocString(&pNewChannel->wzTitle, bstrNodeValue, 0);
            RssExitOnFailure(hr, "Failed to allocate RSS channel title.");
        }
        else if (0 == lstrcmpW(bstrNodeName, L"link"))
        {
            hr = XmlGetText(pNode, &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS channel link.");

            hr = StrAllocString(&pNewChannel->wzLink, bstrNodeValue, 0);
            RssExitOnFailure(hr, "Failed to allocate RSS channel link.");
        }
        else if (0 == lstrcmpW(bstrNodeName, L"description"))
        {
            hr = XmlGetText(pNode, &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS channel description.");

            hr = StrAllocString(&pNewChannel->wzDescription, bstrNodeValue, 0);
            RssExitOnFailure(hr, "Failed to allocate RSS channel description.");
        }
        else if (0 == lstrcmpW(bstrNodeName, L"ttl"))
        {
            hr = XmlGetText(pNode, &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS channel description.");

            pNewChannel->dwTimeToLive = (DWORD)wcstoul(bstrNodeValue, NULL, 10);
        }
        else if (0 == lstrcmpW(bstrNodeName, L"item"))
        {
            hr = ParseRssItem(pNode, cItems, pNewChannel);
            RssExitOnFailure(hr, "Failed to parse RSS item.");

            ++cItems;
        }
        else
        {
            hr = ParseRssUnknownElement(pNode, &pNewChannel->pUnknownElements);
            RssExitOnFailure(hr, "Failed to parse unknown RSS channel element: %ls", bstrNodeName);
        }

        ReleaseNullBSTR(bstrNodeValue);
        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }

    *ppChannel = pNewChannel;
    pNewChannel = NULL;

LExit:
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);

    ReleaseRssChannel(pNewChannel);

    return hr;
}


/********************************************************************
 ParseRssItem - parses out an RSS item from a loaded XML DOM node.

*********************************************************************/
static HRESULT ParseRssItem(
    __in IXMLDOMNode *pixnItem,
    __in DWORD cItem,
    __in_xcount(pChannel->cItems) RSS_CHANNEL *pChannel
    )
{
    HRESULT hr = S_OK;

    RSS_ITEM *pItem = NULL;
    IXMLDOMNodeList *pNodeList = NULL;

    IXMLDOMNode *pNode = NULL;
    BSTR bstrNodeName = NULL;
    BSTR bstrNodeValue = NULL;

    //
    // First make sure we're dealing with a valid item.
    //
    if (pChannel->cItems <= cItem)
    {
        hr = E_UNEXPECTED;
        RssExitOnFailure(hr, "Unexpected number of items parsed.");
    }

    pItem = pChannel->rgItems + cItem;

    //
    // Process the elements under an item now.
    //
    hr = pixnItem->get_childNodes(&pNodeList);
    RssExitOnFailure(hr, "Failed to get child nodes of RSS item element.");
    while (S_OK == (hr = XmlNextElement(pNodeList, &pNode, &bstrNodeName)))
    {
        if (0 == lstrcmpW(bstrNodeName, L"title"))
        {
            hr = XmlGetText(pNode, &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS channel title.");

            hr = StrAllocString(&pItem->wzTitle, bstrNodeValue, 0);
            RssExitOnFailure(hr, "Failed to allocate RSS item title.");
        }
        else if (0 == lstrcmpW(bstrNodeName, L"link"))
        {
            hr = XmlGetText(pNode, &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS channel link.");

            hr = StrAllocString(&pItem->wzLink, bstrNodeValue, 0);
            RssExitOnFailure(hr, "Failed to allocate RSS item link.");
        }
        else if (0 == lstrcmpW(bstrNodeName, L"description"))
        {
            hr = XmlGetText(pNode, &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS item description.");

            hr = StrAllocString(&pItem->wzDescription, bstrNodeValue, 0);
            RssExitOnFailure(hr, "Failed to allocate RSS item description.");
        }
        else if (0 == lstrcmpW(bstrNodeName, L"guid"))
        {
            hr = XmlGetText(pNode, &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS item guid.");

            hr = StrAllocString(&pItem->wzGuid, bstrNodeValue, 0);
            RssExitOnFailure(hr, "Failed to allocate RSS item guid.");
        }
        else if (0 == lstrcmpW(bstrNodeName, L"pubDate"))
        {
            hr = XmlGetText(pNode, &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS item guid.");

            hr = TimeFromString(bstrNodeValue, &pItem->ftPublished);
            RssExitOnFailure(hr, "Failed to convert RSS item time.");
        }
        else if (0 == lstrcmpW(bstrNodeName, L"enclosure"))
        {
            hr = XmlGetAttribute(pNode, L"url", &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS item enclosure url.");

            hr = StrAllocString(&pItem->wzEnclosureUrl, bstrNodeValue, 0);
            RssExitOnFailure(hr, "Failed to allocate RSS item enclosure url.");
            ReleaseNullBSTR(bstrNodeValue);

            hr = XmlGetAttributeNumber(pNode, L"length", &pItem->dwEnclosureSize);
            RssExitOnFailure(hr, "Failed to get RSS item enclosure length.");

            hr = XmlGetAttribute(pNode, L"type", &bstrNodeValue);
            RssExitOnFailure(hr, "Failed to get RSS item enclosure type.");

            hr = StrAllocString(&pItem->wzEnclosureType, bstrNodeValue, 0);
            RssExitOnFailure(hr, "Failed to allocate RSS item enclosure type.");
        }
        else
        {
            hr = ParseRssUnknownElement(pNode, &pItem->pUnknownElements);
            RssExitOnFailure(hr, "Failed to parse unknown RSS item element: %ls", bstrNodeName);
        }

        ReleaseNullBSTR(bstrNodeValue);
        ReleaseNullBSTR(bstrNodeName);
        ReleaseNullObject(pNode);
    }

LExit:
    ReleaseBSTR(bstrNodeValue);
    ReleaseBSTR(bstrNodeName);
    ReleaseObject(pNode);
    ReleaseObject(pNodeList);

    return hr;
}


/********************************************************************
 ParseRssUnknownElement - parses out an unknown item from the RSS feed from a loaded XML DOM node.

*********************************************************************/
static HRESULT ParseRssUnknownElement(
    __in IXMLDOMNode *pNode,
    __inout RSS_UNKNOWN_ELEMENT** ppUnknownElement
    )
{
    Assert(ppUnknownElement);

    HRESULT hr = S_OK;
    BSTR bstrNodeNamespace = NULL;
    BSTR bstrNodeName = NULL;
    BSTR bstrNodeValue = NULL;
    IXMLDOMNamedNodeMap* pixnnmAttributes = NULL;
    IXMLDOMNode* pixnAttribute = NULL;
    RSS_UNKNOWN_ELEMENT* pNewUnknownElement;

    pNewUnknownElement = static_cast<RSS_UNKNOWN_ELEMENT*>(MemAlloc(sizeof(RSS_UNKNOWN_ELEMENT), TRUE));
    RssExitOnNull(pNewUnknownElement, hr, E_OUTOFMEMORY, "Failed to allocate unknown element.");

    hr = pNode->get_namespaceURI(&bstrNodeNamespace);
    if (S_OK == hr)
    {
        hr = StrAllocString(&pNewUnknownElement->wzNamespace, bstrNodeNamespace, 0);
        RssExitOnFailure(hr, "Failed to allocate RSS unknown element namespace.");
    }
    else if (S_FALSE == hr)
    {
        hr = S_OK;
    }
    RssExitOnFailure(hr, "Failed to get unknown element namespace.");

    hr = pNode->get_baseName(&bstrNodeName);
    RssExitOnFailure(hr, "Failed to get unknown element name.");

    hr = StrAllocString(&pNewUnknownElement->wzElement, bstrNodeName, 0);
    RssExitOnFailure(hr, "Failed to allocate RSS unknown element name.");

    hr = XmlGetText(pNode, &bstrNodeValue);
    RssExitOnFailure(hr, "Failed to get unknown element value.");

    hr = StrAllocString(&pNewUnknownElement->wzValue, bstrNodeValue, 0);
    RssExitOnFailure(hr, "Failed to allocate RSS unknown element value.");

    hr = pNode->get_attributes(&pixnnmAttributes);
    RssExitOnFailure(hr, "Failed get attributes on RSS unknown element.");

    while (S_OK == (hr = pixnnmAttributes->nextNode(&pixnAttribute)))
    {
        hr = ParseRssUnknownAttribute(pixnAttribute, &pNewUnknownElement->pAttributes);
        RssExitOnFailure(hr, "Failed to parse attribute on RSS unknown element.");

        ReleaseNullObject(pixnAttribute);
    }

    if (S_FALSE == hr)
    {
        hr = S_OK;
    }
    RssExitOnFailure(hr, "Failed to enumerate all attributes on RSS unknown element.");

    RSS_UNKNOWN_ELEMENT** ppTail = ppUnknownElement;
    while (*ppTail)
    {
        ppTail = &(*ppTail)->pNext;
    }

    *ppTail = pNewUnknownElement;
    pNewUnknownElement = NULL;

LExit:
    FreeRssUnknownElementList(pNewUnknownElement);

    ReleaseBSTR(bstrNodeNamespace);
    ReleaseBSTR(bstrNodeName);
    ReleaseBSTR(bstrNodeValue);
    ReleaseObject(pixnnmAttributes);
    ReleaseObject(pixnAttribute);

    return hr;
}


/********************************************************************
 ParseRssUnknownAttribute - parses out attribute from an unknown element

*********************************************************************/
static HRESULT ParseRssUnknownAttribute(
    __in IXMLDOMNode *pNode,
    __inout RSS_UNKNOWN_ATTRIBUTE** ppUnknownAttribute
    )
{
    Assert(ppUnknownAttribute);

    HRESULT hr = S_OK;
    BSTR bstrNodeNamespace = NULL;
    BSTR bstrNodeName = NULL;
    BSTR bstrNodeValue = NULL;
    RSS_UNKNOWN_ATTRIBUTE* pNewUnknownAttribute;

    pNewUnknownAttribute = static_cast<RSS_UNKNOWN_ATTRIBUTE*>(MemAlloc(sizeof(RSS_UNKNOWN_ATTRIBUTE), TRUE));
    RssExitOnNull(pNewUnknownAttribute, hr, E_OUTOFMEMORY, "Failed to allocate unknown attribute.");

    hr = pNode->get_namespaceURI(&bstrNodeNamespace);
    if (S_OK == hr)
    {
        hr = StrAllocString(&pNewUnknownAttribute->wzNamespace, bstrNodeNamespace, 0);
        RssExitOnFailure(hr, "Failed to allocate RSS unknown attribute namespace.");
    }
    else if (S_FALSE == hr)
    {
        hr = S_OK;
    }
    RssExitOnFailure(hr, "Failed to get unknown attribute namespace.");

    hr = pNode->get_baseName(&bstrNodeName);
    RssExitOnFailure(hr, "Failed to get unknown attribute name.");

    hr = StrAllocString(&pNewUnknownAttribute->wzAttribute, bstrNodeName, 0);
    RssExitOnFailure(hr, "Failed to allocate RSS unknown attribute name.");

    hr = XmlGetText(pNode, &bstrNodeValue);
    RssExitOnFailure(hr, "Failed to get unknown attribute value.");

    hr = StrAllocString(&pNewUnknownAttribute->wzValue, bstrNodeValue, 0);
    RssExitOnFailure(hr, "Failed to allocate RSS unknown attribute value.");

    RSS_UNKNOWN_ATTRIBUTE** ppTail = ppUnknownAttribute;
    while (*ppTail)
    {
        ppTail = &(*ppTail)->pNext;
    }

    *ppTail = pNewUnknownAttribute;
    pNewUnknownAttribute = NULL;

LExit:
    FreeRssUnknownAttributeList(pNewUnknownAttribute);

    ReleaseBSTR(bstrNodeNamespace);
    ReleaseBSTR(bstrNodeName);
    ReleaseBSTR(bstrNodeValue);

    return hr;
}


/********************************************************************
 FreeRssUnknownElement - releases all of the memory used by a list of unknown elements

*********************************************************************/
static void FreeRssUnknownElementList(
    __in_opt RSS_UNKNOWN_ELEMENT* pUnknownElement
    )
{
    while (pUnknownElement)
    {
        RSS_UNKNOWN_ELEMENT* pFree = pUnknownElement;
        pUnknownElement = pUnknownElement->pNext;

        FreeRssUnknownAttributeList(pFree->pAttributes);
        ReleaseStr(pFree->wzNamespace);
        ReleaseStr(pFree->wzElement);
        ReleaseStr(pFree->wzValue);
        MemFree(pFree);
    }
}


/********************************************************************
 FreeRssUnknownAttribute - releases all of the memory used by a list of unknown attributes

*********************************************************************/
static void FreeRssUnknownAttributeList(
    __in_opt RSS_UNKNOWN_ATTRIBUTE* pUnknownAttribute
    )
{
    while (pUnknownAttribute)
    {
        RSS_UNKNOWN_ATTRIBUTE* pFree = pUnknownAttribute;
        pUnknownAttribute = pUnknownAttribute->pNext;

        ReleaseStr(pFree->wzNamespace);
        ReleaseStr(pFree->wzAttribute);
        ReleaseStr(pFree->wzValue);
        MemFree(pFree);
    }
}
