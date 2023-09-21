// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsFirewallExceptionQuery =
L"SELECT `Name`, `RemoteAddresses`, `Port`, `Protocol`, `Program`, `Attributes`, `Profile`, `Component_`, `Description`, `Direction`, `Action`, `EdgeTraversal`, `Enabled`, `Grouping`, `IcmpTypesAndCodes`, `Interfaces`, `InterfaceTypes`, `LocalAddresses`, `RemotePort`, `ServiceName`, `LocalAppPackageId`, `LocalUserAuthorizedList`, `LocalUserOwner`, `RemoteMachineAuthorizedList`, `RemoteUserAuthorizedList`, `SecureFlags` FROM `Wix5FirewallException`";
enum eFirewallExceptionQuery { feqName = 1, feqRemoteAddresses, feqPort, feqProtocol, feqProgram, feqAttributes, feqProfile, feqComponent, feqDescription, feqDirection, feqAction, feqEdgeTraversal, feqEnabled, feqGrouping, feqIcmpTypesAndCodes, feqInterfaces, feqInterfaceTypes, feqLocalAddresses, feqRemotePort, feqServiceName, feqLocalAppPackageId, feqLocalUserAuthorizedList, feqLocalUserOwner, feqRemoteMachineAuthorizedList, feqRemoteUserAuthorizedList, feqSecureFlags };
enum eFirewallExceptionAttributes { feaIgnoreFailures = 1, feaIgnoreUpdates = 2, feaEnableOnUpdate = 4, feaAddINetFwRule2 = 8, feaAddINetFwRule3 = 16 };

struct FIREWALL_EXCEPTION_ATTRIBUTES
{
    LPWSTR pwzName;
    int iAttributes;

    // INetFwRule
    int iAction;
    LPWSTR pwzApplicationName;
    LPWSTR pwzDescription;
    int iDirection;
    int iEnabled;
    LPWSTR pwzGrouping;
    LPWSTR pwzIcmpTypesAndCodes;
    LPWSTR pwzInterfaces;
    LPWSTR pwzInterfaceTypes;
    LPWSTR pwzLocalAddresses;
    LPWSTR pwzLocalPorts;
    int iProfile;
    int iProtocol;
    LPWSTR pwzRemoteAddresses;
    LPWSTR pwzRemotePorts;
    LPWSTR pwzServiceName;

    // INetFwRule2
    int iEdgeTraversal;

    // INetFwRule3
    LPWSTR pwzLocalAppPackageId;
    LPWSTR pwzLocalUserAuthorizedList;
    LPWSTR pwzLocalUserOwner;
    LPWSTR pwzRemoteMachineAuthorizedList;
    LPWSTR pwzRemoteUserAuthorizedList;
    int iSecureFlags;
};

/******************************************************************
 SchedFirewallExceptions - immediate custom action worker to
   register and remove firewall exceptions.

********************************************************************/
static UINT SchedFirewallExceptions(
    __in MSIHANDLE hInstall,
    __in WCA_TODO todoSched
)
{
    HRESULT hr = S_OK;
    UINT er = ERROR_SUCCESS;
    int cFirewallExceptions = 0;

    PMSIHANDLE hView = NULL;
    PMSIHANDLE hRec = NULL;

    LPWSTR pwzCustomActionData = NULL;
    LPWSTR pwzComponent = NULL;

    FIREWALL_EXCEPTION_ATTRIBUTES attrs = { 0 };

    // initialize
    hr = WcaInitialize(hInstall, "SchedFirewallExceptions");
    ExitOnFailure(hr, "Failed to initialize");

    // anything to do?
    if (S_OK != WcaTableExists(L"Wix5FirewallException"))
    {
        WcaLog(LOGMSG_STANDARD, "Wix5FirewallException table doesn't exist, so there are no firewall exceptions to configure.");
        ExitFunction();
    }

    // query and loop through all the firewall exceptions
    hr = WcaOpenExecuteView(vcsFirewallExceptionQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on Wix5FirewallException table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordFormattedString(hRec, feqName, &attrs.pwzName);
        ExitOnFailure(hr, "Failed to get firewall exception name.");

        hr = WcaGetRecordFormattedString(hRec, feqRemoteAddresses, &attrs.pwzRemoteAddresses);
        ExitOnFailure(hr, "Failed to get firewall exception remote addresses.");

        hr = WcaGetRecordFormattedString(hRec, feqPort, &attrs.pwzLocalPorts);
        ExitOnFailure(hr, "Failed to get firewall exception port.");

        hr = WcaGetRecordFormattedInteger(hRec, feqProtocol, &attrs.iProtocol);
        ExitOnFailure(hr, "Failed to get firewall exception protocol.");

        hr = WcaGetRecordFormattedString(hRec, feqProgram, &attrs.pwzApplicationName);
        ExitOnFailure(hr, "Failed to get firewall exception program.");

        hr = WcaGetRecordInteger(hRec, feqAttributes, &attrs.iAttributes);
        ExitOnFailure(hr, "Failed to get firewall exception attributes.");

        hr = WcaGetRecordFormattedInteger(hRec, feqProfile, &attrs.iProfile);
        ExitOnFailure(hr, "Failed to get firewall exception profile.");

        hr = WcaGetRecordString(hRec, feqComponent, &pwzComponent);
        ExitOnFailure(hr, "Failed to get firewall exception component.");

        hr = WcaGetRecordFormattedString(hRec, feqDescription, &attrs.pwzDescription);
        ExitOnFailure(hr, "Failed to get firewall exception description.");

        hr = WcaGetRecordInteger(hRec, feqDirection, &attrs.iDirection);
        ExitOnFailure(hr, "Failed to get firewall exception direction.");

        hr = WcaGetRecordFormattedInteger(hRec, feqAction, &attrs.iAction);
        ExitOnFailure(hr, "Failed to get firewall exception action.");

        hr = WcaGetRecordFormattedInteger(hRec, feqEdgeTraversal, &attrs.iEdgeTraversal);
        ExitOnFailure(hr, "Failed to get firewall exception edge traversal.");

        hr = WcaGetRecordFormattedInteger(hRec, feqEnabled, &attrs.iEnabled);
        ExitOnFailure(hr, "Failed to get firewall exception enabled flag.");

        hr = WcaGetRecordFormattedString(hRec, feqGrouping, &attrs.pwzGrouping);
        ExitOnFailure(hr, "Failed to get firewall exception grouping.");

        hr = WcaGetRecordFormattedString(hRec, feqIcmpTypesAndCodes, &attrs.pwzIcmpTypesAndCodes);
        ExitOnFailure(hr, "Failed to get firewall exception ICMP types and codes.");

        hr = WcaGetRecordFormattedString(hRec, feqInterfaces, &attrs.pwzInterfaces);
        ExitOnFailure(hr, "Failed to get firewall exception interfaces.");

        hr = WcaGetRecordFormattedString(hRec, feqInterfaceTypes, &attrs.pwzInterfaceTypes);
        ExitOnFailure(hr, "Failed to get firewall exception interface types.");

        hr = WcaGetRecordFormattedString(hRec, feqLocalAddresses, &attrs.pwzLocalAddresses);
        ExitOnFailure(hr, "Failed to get firewall exception local addresses.");

        hr = WcaGetRecordFormattedString(hRec, feqRemotePort, &attrs.pwzRemotePorts);
        ExitOnFailure(hr, "Failed to get firewall exception remote port.");

        hr = WcaGetRecordFormattedString(hRec, feqServiceName, &attrs.pwzServiceName);
        ExitOnFailure(hr, "Failed to get firewall exception service name.");

        hr = WcaGetRecordFormattedString(hRec, feqLocalAppPackageId, &attrs.pwzLocalAppPackageId);
        ExitOnFailure(hr, "Failed to get firewall exception local app package id.");

        hr = WcaGetRecordFormattedString(hRec, feqLocalUserAuthorizedList, &attrs.pwzLocalUserAuthorizedList);
        ExitOnFailure(hr, "Failed to get firewall exception local user authorized list.");

        hr = WcaGetRecordFormattedString(hRec, feqLocalUserOwner, &attrs.pwzLocalUserOwner);
        ExitOnFailure(hr, "Failed to get firewall exception local user owner.");

        hr = WcaGetRecordFormattedString(hRec, feqRemoteMachineAuthorizedList, &attrs.pwzRemoteMachineAuthorizedList);
        ExitOnFailure(hr, "Failed to get firewall exception remote machine authorized list.");

        hr = WcaGetRecordFormattedString(hRec, feqRemoteUserAuthorizedList, &attrs.pwzRemoteUserAuthorizedList);
        ExitOnFailure(hr, "Failed to get firewall exception remote user authorized list.");

        hr = WcaGetRecordFormattedInteger(hRec, feqSecureFlags, &attrs.iSecureFlags);
        ExitOnFailure(hr, "Failed to get firewall exception secure flag.");

        // figure out what we're doing for this exception, treating reinstall the same as install
        WCA_TODO todoComponent = WcaGetComponentToDo(pwzComponent);
        if ((WCA_TODO_REINSTALL == todoComponent ? WCA_TODO_INSTALL : todoComponent) != todoSched)
        {
            WcaLog(LOGMSG_STANDARD, "Component '%ls' action state (%d) doesn't match request (%d)", pwzComponent, todoComponent, todoSched);
            continue;
        }

        ++cFirewallExceptions;
        hr = WcaWriteIntegerToCaData(todoComponent, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception action to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzName, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception name to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iProfile, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception profile to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzRemoteAddresses, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception remote addresses to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iAttributes, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception attributes to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzApplicationName, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write application path to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzLocalPorts, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write local ports to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iProtocol, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception protocol to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzDescription, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write firewall exception description to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iDirection, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write firewall exception direction to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iAction, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception action to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iEdgeTraversal, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception edge traversal to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iEnabled, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception enabled flag to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzGrouping, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write grouping to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzIcmpTypesAndCodes, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write icmp types and codes to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzInterfaces, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write interfaces to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzInterfaceTypes, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write interface types to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzLocalAddresses, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write local addresses to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzRemotePorts, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write remote ports to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzServiceName, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write service name to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzLocalAppPackageId, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write local app package id to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzLocalUserAuthorizedList, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write local user authorized list to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzLocalUserOwner, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write local user owner to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzRemoteMachineAuthorizedList, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write remote machine authorized list to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzRemoteUserAuthorizedList, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write remote user authorized list to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iSecureFlags, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception secure flags to custom action data");
    }

    // reaching the end of the list is actually a good thing, not an error
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    }
    ExitOnFailure(hr, "failure occured while processing Wix5FirewallException table");

    // schedule ExecFirewallExceptions if there's anything to do
    if (pwzCustomActionData && *pwzCustomActionData)
    {
        WcaLog(LOGMSG_STANDARD, "Scheduling firewall exception (%ls)", pwzCustomActionData);

        if (WCA_TODO_INSTALL == todoSched)
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION5(L"RollbackFirewallExceptionsInstall"), pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall install exceptions rollback");
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION5(L"ExecFirewallExceptionsInstall"), pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall install exceptions execution");
        }
        else
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION5(L"RollbackFirewallExceptionsUninstall"), pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall uninstall exceptions rollback");
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION5(L"ExecFirewallExceptionsUninstall"), pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall uninstall exceptions execution");
        }
    }
    else
    {
        WcaLog(LOGMSG_STANDARD, "No firewall exceptions scheduled");
    }

LExit:
    ReleaseStr(attrs.pwzName);
    ReleaseStr(attrs.pwzRemoteAddresses);
    ReleaseStr(attrs.pwzLocalPorts);
    ReleaseStr(attrs.pwzApplicationName);
    ReleaseStr(attrs.pwzDescription);
    ReleaseStr(attrs.pwzGrouping);
    ReleaseStr(attrs.pwzIcmpTypesAndCodes);
    ReleaseStr(attrs.pwzInterfaces);
    ReleaseStr(attrs.pwzInterfaceTypes);
    ReleaseStr(attrs.pwzLocalAddresses);
    ReleaseStr(attrs.pwzRemotePorts);
    ReleaseStr(attrs.pwzServiceName);
    ReleaseStr(attrs.pwzLocalAppPackageId);
    ReleaseStr(attrs.pwzLocalUserAuthorizedList);
    ReleaseStr(attrs.pwzLocalUserOwner);
    ReleaseStr(attrs.pwzRemoteMachineAuthorizedList);
    ReleaseStr(attrs.pwzRemoteUserAuthorizedList);
    ReleaseStr(pwzComponent);
    ReleaseStr(pwzCustomActionData);

    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}


/*******************************************************************
 SchedFirewallExceptionsInstall - immediate custom action entry
   point to register firewall exceptions.

********************************************************************/
extern "C" UINT __stdcall SchedFirewallExceptionsInstall(
    __in MSIHANDLE hInstall
)
{
    return SchedFirewallExceptions(hInstall, WCA_TODO_INSTALL);
}


/*******************************************************************
 SchedFirewallExceptionsUninstall - immediate custom action entry
   point to remove firewall exceptions.

********************************************************************/
extern "C" UINT __stdcall SchedFirewallExceptionsUninstall(
    __in MSIHANDLE hInstall
)
{
    return SchedFirewallExceptions(hInstall, WCA_TODO_UNINSTALL);
}


/*******************************************************************
 GetFirewallRules - Get the collection of firewall rules.

********************************************************************/
static HRESULT GetFirewallRules(
    __in BOOL fIgnoreFailures,
    __out INetFwRules** ppNetFwRules
)
{
    HRESULT hr = S_OK;
    INetFwPolicy2* pNetFwPolicy2 = NULL;
    INetFwRules* pNetFwRules = NULL;
    *ppNetFwRules = NULL;

    do
    {
        ReleaseNullObject(pNetFwPolicy2);
        ReleaseNullObject(pNetFwRules);

        if (SUCCEEDED(hr = ::CoCreateInstance(__uuidof(NetFwPolicy2), NULL, CLSCTX_ALL, __uuidof(INetFwPolicy2), (void**)&pNetFwPolicy2)) &&
            SUCCEEDED(hr = pNetFwPolicy2->get_Rules(&pNetFwRules)))
        {
            break;
        }
        else if (fIgnoreFailures)
        {
            ExitFunction1(hr = S_FALSE);
        }
        else
        {
            WcaLog(LOGMSG_STANDARD, "Failed to connect to Windows Firewall");
            UINT er = WcaErrorMessage(msierrFirewallCannotConnect, hr, INSTALLMESSAGE_ERROR | MB_ABORTRETRYIGNORE, 0);
            switch (er)
            {
            case IDABORT: // exit with the current HRESULT
                ExitFunction();
            case IDRETRY: // clean up and retry the loop
                hr = S_FALSE;
                break;
            case IDIGNORE: // pass S_FALSE back to the caller, who knows how to ignore the failure
                ExitFunction1(hr = S_FALSE);
            default: // No UI, so default is to fail.
                ExitFunction();
            }
        }
    } while (S_FALSE == hr);

    *ppNetFwRules = pNetFwRules;
    pNetFwRules = NULL;

LExit:
    ReleaseObject(pNetFwPolicy2);
    ReleaseObject(pNetFwRules);

    return hr;
}


/*******************************************************************
 CreateFwRuleObject - CoCreate a firewall rule, and set the name

********************************************************************/
static HRESULT CreateFwRuleObject(
    __in BSTR bstrName,
    __out INetFwRule** ppNetFwRule
)
{
    HRESULT hr = S_OK;
    INetFwRule* pNetFwRule = NULL;
    *ppNetFwRule = NULL;

    hr = ::CoCreateInstance(__uuidof(NetFwRule), NULL, CLSCTX_ALL, __uuidof(INetFwRule), (LPVOID*)&pNetFwRule);
    ExitOnFailure(hr, "failed to create NetFwRule object");

    hr = pNetFwRule->put_Name(bstrName);
    ExitOnFailure(hr, "failed to set firewall exception name");

    *ppNetFwRule = pNetFwRule;

LExit:
    return hr;
}


/*********************************************************************
 GetFwRuleInterfaces - pack firewall rule interfaces into a VARIANT.
 The populated VARIANT needs to be cleaned up by the calling function.

**********************************************************************/
static HRESULT GetFwRuleInterfaces(
    __in FIREWALL_EXCEPTION_ATTRIBUTES const& attrs,
    __out VARIANT& vInterfaces
)
{
    HRESULT hr = S_OK;
    BSTR bstrInterfaces = NULL;
    const WCHAR FORBIDDEN_FIREWALL_CHAR = L'|';
    LONG iInterfacesCount = 0;
    UINT iLength = 0;
    LONG iIndex = 0;

    ::VariantInit(&vInterfaces);
    ExitOnNull(attrs.pwzInterfaces, hr, S_OK, "No interfaces to pack");

    bstrInterfaces = ::SysAllocString(attrs.pwzInterfaces);
    ExitOnNull(bstrInterfaces, hr, E_OUTOFMEMORY, "failed SysAllocString for interfaces");

    iLength = ::SysStringLen(bstrInterfaces);

    LPWSTR pwzT = bstrInterfaces;
    while (*pwzT)
    {
        if (FORBIDDEN_FIREWALL_CHAR == *pwzT)
        {
            *pwzT = L'\0';
            pwzT++;

            // skip empty values inside the interfaces eg. |||
            if (*pwzT && FORBIDDEN_FIREWALL_CHAR != *pwzT)
            {
                iInterfacesCount++;
            }
        }
        else
        {
            if (pwzT == bstrInterfaces)
            {
                iInterfacesCount++;
            }

            pwzT++;
        }
    }

    ExitOnNull(iInterfacesCount, hr, S_OK, "All interfaces are empty values");

    vInterfaces.vt = VT_ARRAY | VT_VARIANT;
    // this will be cleaned up by ReleaseVariant call of the calling function
    vInterfaces.parray = SafeArrayCreateVector(VT_VARIANT, 0, iInterfacesCount);

    for (LPCWSTR pwzElement = bstrInterfaces; pwzElement < (bstrInterfaces + iLength); ++pwzElement)
    {
        if (*pwzElement)
        {
            VARIANT vElement;
            ::VariantInit(&vElement);

            vElement.vt = VT_BSTR;
            // this will be cleaned up by ReleaseVariant call of the calling function
            vElement.bstrVal = ::SysAllocString(pwzElement);
            ExitOnNull(vElement.bstrVal, hr, E_OUTOFMEMORY, "failed SysAllocString for interface element");

            hr = SafeArrayPutElement(vInterfaces.parray, &iIndex, &vElement);
            ExitOnFailure(hr, "failed to put interface '%ls' into safe array", pwzElement);

            pwzElement += ::SysStringLen(vElement.bstrVal);
            iIndex++;
        }
    }

LExit:
    ReleaseBSTR(bstrInterfaces);

    return hr;
}

/******************************************************************************
 UpdateFwRule2Object - update properties for a firewall INetFwRule2 interface.
 Requires Windows 7 / 2008 R2

 ******************************************************************************/
static HRESULT UpdateFwRule2Object(
    __in INetFwRule* pNetFwRule,
    __in BOOL fUpdateRule,
    __in FIREWALL_EXCEPTION_ATTRIBUTES const& attrs
)
{
    HRESULT hr = S_OK;
    INetFwRule2* pNetFwRule2 = NULL;

    hr = pNetFwRule->QueryInterface(__uuidof(INetFwRule2), (LPVOID*)&pNetFwRule2);
    ExitOnFailure(hr, "failed to query INetFwRule2 interface");

    if (MSI_NULL_INTEGER != attrs.iEdgeTraversal)
    {
        hr = pNetFwRule2->put_EdgeTraversalOptions(attrs.iEdgeTraversal);
        ExitOnFailure(hr, "failed to set exception edge traversal option");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule2->put_EdgeTraversalOptions(NET_FW_EDGE_TRAVERSAL_TYPE_DENY);
        ExitOnFailure(hr, "failed to remove exception edge traversal option");
    }

LExit:
    ReleaseObject(pNetFwRule2);

    return hr;
}


/******************************************************************************
 UpdateFwRule3Object - update properties for a firewall INetFwRule3 interface.
 Requires Windows 8 / 2012

 ******************************************************************************/
static HRESULT UpdateFwRule3Object(
    __in INetFwRule* pNetFwRule,
    __in BOOL fUpdateRule,
    __in FIREWALL_EXCEPTION_ATTRIBUTES const& attrs
)
{
    HRESULT hr = S_OK;

    BSTR bstrLocalAppPackageId = NULL;
    BSTR bstrLocalUserAuthorizedList = NULL;
    BSTR bstrLocalUserOwner = NULL;
    BSTR bstrRemoteMachineAuthorizedList = NULL;
    BSTR bstrRemoteUserAuthorizedList = NULL;
    INetFwRule3* pNetFwRule3 = NULL;

    bstrLocalAppPackageId = ::SysAllocString(attrs.pwzLocalAppPackageId);
    ExitOnNull(bstrLocalAppPackageId, hr, E_OUTOFMEMORY, "failed SysAllocString for local app package id");
    bstrLocalUserAuthorizedList = ::SysAllocString(attrs.pwzLocalUserAuthorizedList);
    ExitOnNull(bstrLocalUserAuthorizedList, hr, E_OUTOFMEMORY, "failed SysAllocString for local user authorized list");
    bstrLocalUserOwner = ::SysAllocString(attrs.pwzLocalUserOwner);
    ExitOnNull(bstrLocalUserOwner, hr, E_OUTOFMEMORY, "failed SysAllocString for local user owner");
    bstrRemoteMachineAuthorizedList = ::SysAllocString(attrs.pwzRemoteMachineAuthorizedList);
    ExitOnNull(bstrRemoteMachineAuthorizedList, hr, E_OUTOFMEMORY, "failed SysAllocString for remote machine authorized list");
    bstrRemoteUserAuthorizedList = ::SysAllocString(attrs.pwzRemoteUserAuthorizedList);
    ExitOnNull(bstrRemoteUserAuthorizedList, hr, E_OUTOFMEMORY, "failed SysAllocString for remote user authorized list");

    hr = pNetFwRule->QueryInterface(__uuidof(INetFwRule3), (LPVOID*)&pNetFwRule3);
    ExitOnFailure(hr, "failed to query INetFwRule3 interface");

    if (bstrLocalAppPackageId && *bstrLocalAppPackageId)
    {
        hr = pNetFwRule3->put_LocalAppPackageId(bstrLocalAppPackageId);
        ExitOnFailure(hr, "failed to set exception local app package id");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule3->put_LocalAppPackageId(NULL);
        ExitOnFailure(hr, "failed to remove exception local app package id");
    }

    if (bstrLocalUserAuthorizedList && *bstrLocalUserAuthorizedList)
    {
        hr = pNetFwRule3->put_LocalUserAuthorizedList(bstrLocalUserAuthorizedList);
        ExitOnFailure(hr, "failed to set exception local user authorized list");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule3->put_LocalUserAuthorizedList(NULL);
        ExitOnFailure(hr, "failed to remove exception local user authorized list");
    }

    if (bstrLocalUserOwner && *bstrLocalUserOwner)
    {
        hr = pNetFwRule3->put_LocalUserOwner(bstrLocalUserOwner);
        ExitOnFailure(hr, "failed to set exception local user owner");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule3->put_LocalUserOwner(NULL);
        ExitOnFailure(hr, "failed to remove exception local user owner");
    }

    if (bstrRemoteMachineAuthorizedList && *bstrRemoteMachineAuthorizedList)
    {
        hr = pNetFwRule3->put_RemoteMachineAuthorizedList(bstrRemoteMachineAuthorizedList);
        ExitOnFailure(hr, "failed to set exception remote machine authorized list");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule3->put_RemoteMachineAuthorizedList(NULL);
        ExitOnFailure(hr, "failed to remove exception remote machine authorized list");
    }

    if (bstrRemoteUserAuthorizedList && *bstrRemoteUserAuthorizedList)
    {
        hr = pNetFwRule3->put_RemoteUserAuthorizedList(bstrRemoteUserAuthorizedList);
        ExitOnFailure(hr, "failed to set exception remote user authorized list");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule3->put_RemoteUserAuthorizedList(NULL);
        ExitOnFailure(hr, "failed to remove exception remote user authorized list");
    }

    if (MSI_NULL_INTEGER != attrs.iSecureFlags)
    {
        hr = pNetFwRule3->put_SecureFlags(attrs.iSecureFlags);
        ExitOnFailure(hr, "failed to set exception IPsec secure flags");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule3->put_SecureFlags(NET_FW_AUTHENTICATE_NONE);
        ExitOnFailure(hr, "failed to reset exception IPsec secure flags");
    }

LExit:
    ReleaseBSTR(bstrLocalAppPackageId);
    ReleaseBSTR(bstrLocalUserAuthorizedList);
    ReleaseBSTR(bstrLocalUserOwner);
    ReleaseBSTR(bstrRemoteMachineAuthorizedList);
    ReleaseBSTR(bstrRemoteUserAuthorizedList);
    ReleaseObject(pNetFwRule3);

    return hr;
}


/**********************************************************************
 UpdateFwRuleObject - update all properties for a basic firewall rule.
 Requires Windows Vista / 2008

 **********************************************************************/
static HRESULT UpdateFwRuleObject(
    __in INetFwRule* pNetFwRule,
    __in BOOL fUpdateRule,
    __in FIREWALL_EXCEPTION_ATTRIBUTES const& attrs
)
{
    HRESULT hr = S_OK;
    BSTR bstrEmpty = NULL;
    BSTR bstrRemoteAddresses = NULL;
    BSTR bstrFile = NULL;
    BSTR bstrPort = NULL;
    BSTR bstrDescription = NULL;
    BSTR bstrGrouping = NULL;
    BSTR bstrIcmpTypesAndCodes = NULL;
    BSTR bstrInterfaceTypes = NULL;
    BSTR bstrLocalAddresses = NULL;
    BSTR bstrRemotePort = NULL;
    BSTR bstrServiceName = NULL;
    VARIANT vInterfaces;
    ::VariantInit(&vInterfaces);
    LONG iProtocol = 0;

    INetFwRule2* pNetFwRule2 = NULL;

    // convert to BSTRs to make COM happy
    bstrEmpty = ::SysAllocString(L"");
    ExitOnNull(bstrEmpty, hr, E_OUTOFMEMORY, "failed SysAllocString for empty placeholder");

    bstrRemoteAddresses = ::SysAllocString(attrs.pwzRemoteAddresses);
    ExitOnNull(bstrRemoteAddresses, hr, E_OUTOFMEMORY, "failed SysAllocString for remote addresses");
    bstrFile = ::SysAllocString(attrs.pwzApplicationName);
    ExitOnNull(bstrFile, hr, E_OUTOFMEMORY, "failed SysAllocString for application name");
    bstrPort = ::SysAllocString(attrs.pwzLocalPorts);
    ExitOnNull(bstrPort, hr, E_OUTOFMEMORY, "failed SysAllocString for port");
    bstrDescription = ::SysAllocString(attrs.pwzDescription);
    ExitOnNull(bstrDescription, hr, E_OUTOFMEMORY, "failed SysAllocString for description");
    bstrGrouping = ::SysAllocString(attrs.pwzGrouping);
    ExitOnNull(bstrGrouping, hr, E_OUTOFMEMORY, "failed SysAllocString for grouping");
    bstrIcmpTypesAndCodes = ::SysAllocString(attrs.pwzIcmpTypesAndCodes);
    ExitOnNull(bstrIcmpTypesAndCodes, hr, E_OUTOFMEMORY, "failed SysAllocString for icmp types and codes");
    bstrInterfaceTypes = ::SysAllocString(attrs.pwzInterfaceTypes);
    ExitOnNull(bstrInterfaceTypes, hr, E_OUTOFMEMORY, "failed SysAllocString for interface types");
    bstrLocalAddresses = ::SysAllocString(attrs.pwzLocalAddresses);
    ExitOnNull(bstrLocalAddresses, hr, E_OUTOFMEMORY, "failed SysAllocString for local addresses");
    bstrRemotePort = ::SysAllocString(attrs.pwzRemotePorts);
    ExitOnNull(bstrRemotePort, hr, E_OUTOFMEMORY, "failed SysAllocString for remote port");
    bstrServiceName = ::SysAllocString(attrs.pwzServiceName);
    ExitOnNull(bstrServiceName, hr, E_OUTOFMEMORY, "failed SysAllocString for service name");

    if (fUpdateRule)
    {
        hr = pNetFwRule->get_Protocol(&iProtocol);
        ExitOnFailure(hr, "failed to get exception protocol");

        // If you are editing a TCP port rule and converting it into an ICMP rule,
        // first delete the ports, change protocol from TCP to ICMP, and then add the ports.

        switch (iProtocol)
        {
        case NET_FW_IP_PROTOCOL_ANY:
            break;

        case 1: // ICMP
            hr = pNetFwRule->put_IcmpTypesAndCodes(NULL);
            ExitOnFailure(hr, "failed to remove exception icmp types and codes");
            // fall through and reset ports too

        default:
            hr = pNetFwRule->put_LocalPorts(NULL);
            ExitOnFailure(hr, "failed to update exception local ports to NULL");

            hr = pNetFwRule->put_RemotePorts(NULL);
            ExitOnFailure(hr, "failed to update exception remote ports to NULL");
            break;
        }
    }

    if (MSI_NULL_INTEGER != attrs.iProfile)
    {
        hr = pNetFwRule->put_Profiles(static_cast<NET_FW_PROFILE_TYPE2> (attrs.iProfile));
        ExitOnFailure(hr, "failed to set exception profile");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_Profiles(NET_FW_PROFILE2_ALL);
        ExitOnFailure(hr, "failed to reset exception profile to all");
    }

    // The Protocol property must be set before the LocalPorts/RemotePorts properties or an error will be returned.
    if (MSI_NULL_INTEGER != attrs.iProtocol)
    {
        hr = pNetFwRule->put_Protocol(static_cast<NET_FW_IP_PROTOCOL> (attrs.iProtocol));
        ExitOnFailure(hr, "failed to set exception protocol");
    }
    else if (fUpdateRule)
    {
        if ((bstrPort && *bstrPort) || (bstrRemotePort && *bstrRemotePort))
        {
            // default protocol is "TCP" in the WiX firewall compiler if a port is specified
            hr = pNetFwRule->put_Protocol(NET_FW_IP_PROTOCOL_TCP);
            ExitOnFailure(hr, "failed to reset exception protocol to TCP");
        }
        else
        {
            hr = pNetFwRule->put_Protocol(NET_FW_IP_PROTOCOL_ANY);
            ExitOnFailure(hr, "failed to reset exception protocol to ANY");
        }
    }

    if (bstrPort && *bstrPort)
    {
        hr = pNetFwRule->put_LocalPorts(bstrPort);
        ExitOnFailure(hr, "failed to set exception local ports '%ls'", bstrPort);
    }

    if (bstrRemoteAddresses && *bstrRemoteAddresses)
    {
        hr = pNetFwRule->put_RemoteAddresses(bstrRemoteAddresses);
        ExitOnFailure(hr, "failed to set exception remote addresses '%ls'", bstrRemoteAddresses);
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_RemoteAddresses(bstrEmpty);
        ExitOnFailure(hr, "failed to remove exception remote addresses");
    }

    if (bstrDescription && *bstrDescription)
    {
        hr = pNetFwRule->put_Description(bstrDescription);
        ExitOnFailure(hr, "failed to set exception description '%ls'", bstrDescription);
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_Description(bstrEmpty);
        ExitOnFailure(hr, "failed to remove exception description");
    }

    if (MSI_NULL_INTEGER != attrs.iDirection)
    {
        hr = pNetFwRule->put_Direction(static_cast<NET_FW_RULE_DIRECTION> (attrs.iDirection));
        ExitOnFailure(hr, "failed to set exception direction");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_Direction(NET_FW_RULE_DIR_IN);
        ExitOnFailure(hr, "failed to reset exception direction to in");
    }

    if (MSI_NULL_INTEGER != attrs.iAction)
    {
        hr = pNetFwRule->put_Action(static_cast<NET_FW_ACTION> (attrs.iAction));
        ExitOnFailure(hr, "failed to set exception action");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_Action(NET_FW_ACTION_ALLOW);
        ExitOnFailure(hr, "failed to reset exception action to allow");
    }

    if (bstrFile && *bstrFile)
    {
        hr = pNetFwRule->put_ApplicationName(bstrFile);
        ExitOnFailure(hr, "failed to set exception application name");
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_ApplicationName(NULL);
        ExitOnFailure(hr, "failed to remove exception application name");
    }

    if (MSI_NULL_INTEGER != attrs.iEdgeTraversal)
    {
        switch (attrs.iEdgeTraversal)
        {
        default:
            hr = pNetFwRule->put_EdgeTraversal(NET_FW_EDGE_TRAVERSAL_TYPE_DENY != attrs.iEdgeTraversal ? VARIANT_TRUE : VARIANT_FALSE);
            ExitOnFailure(hr, "failed to set exception edge traversal");
            break;

            // handled by put_EdgeTraversalOptions
        case NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_APP:
        case NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER:
            break;
        }
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_EdgeTraversal(VARIANT_FALSE);
        ExitOnFailure(hr, "failed to remove exception edge traversal");
    }

    // enable even when iEnabled == MSI_NULL_INTEGER
    hr = pNetFwRule->put_Enabled(attrs.iEnabled ? VARIANT_TRUE : VARIANT_FALSE);
    ExitOnFailure(hr, "failed to set exception enabled flag");

    if (bstrGrouping && *bstrGrouping)
    {
        hr = pNetFwRule->put_Grouping(bstrGrouping);
        ExitOnFailure(hr, "failed to set exception grouping '%ls'", bstrGrouping);
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_Grouping(bstrEmpty);
        ExitOnFailure(hr, "failed to remove exception grouping");
    }

    if (bstrIcmpTypesAndCodes && *bstrIcmpTypesAndCodes)
    {
        hr = pNetFwRule->put_IcmpTypesAndCodes(bstrIcmpTypesAndCodes);
        ExitOnFailure(hr, "failed to set exception icmp types and codes '%ls'", bstrIcmpTypesAndCodes);
    }

    hr = GetFwRuleInterfaces(attrs, vInterfaces);
    ExitOnFailure(hr, "failed to prepare exception interfaces '%ls'", attrs.pwzInterfaces);

    if (attrs.pwzInterfaces && *attrs.pwzInterfaces)
    {
        hr = pNetFwRule->put_Interfaces(vInterfaces);
        ExitOnFailure(hr, "failed to set exception interfaces '%ls'", attrs.pwzInterfaces);
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_Interfaces(vInterfaces);
        ExitOnFailure(hr, "failed to remove exception interfaces");
    }

    if (bstrInterfaceTypes && *bstrInterfaceTypes)
    {
        hr = pNetFwRule->put_InterfaceTypes(bstrInterfaceTypes);
        ExitOnFailure(hr, "failed to set exception interface types '%ls'", bstrInterfaceTypes);
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_InterfaceTypes(bstrEmpty);
        ExitOnFailure(hr, "failed to remove exception interface types");
    }

    if (bstrLocalAddresses && *bstrLocalAddresses)
    {
        hr = pNetFwRule->put_LocalAddresses(bstrLocalAddresses);
        ExitOnFailure(hr, "failed to set exception local addresses '%ls'", bstrLocalAddresses);
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_LocalAddresses(bstrEmpty);
        ExitOnFailure(hr, "failed to remove exception local addresses");
    }

    if (bstrRemotePort && *bstrRemotePort)
    {
        hr = pNetFwRule->put_RemotePorts(bstrRemotePort);
        ExitOnFailure(hr, "failed to set exception remote ports '%ls'", bstrRemotePort);
    }

    if (bstrServiceName && *bstrServiceName)
    {
        hr = pNetFwRule->put_ServiceName(bstrServiceName);
        ExitOnFailure(hr, "failed to set exception service name '%ls'", bstrServiceName);
    }
    else if (fUpdateRule)
    {
        hr = pNetFwRule->put_ServiceName(NULL);
        ExitOnFailure(hr, "failed to remove exception service name");
    }

LExit:
    ReleaseBSTR(bstrRemoteAddresses);
    ReleaseBSTR(bstrFile);
    ReleaseBSTR(bstrPort);
    ReleaseBSTR(bstrDescription);
    ReleaseBSTR(bstrGrouping);
    ReleaseBSTR(bstrIcmpTypesAndCodes);
    ReleaseBSTR(bstrInterfaceTypes);
    ReleaseBSTR(bstrLocalAddresses);
    ReleaseBSTR(bstrRemotePort);
    ReleaseBSTR(bstrServiceName);
    ReleaseVariant(vInterfaces);
    ReleaseObject(pNetFwRule2);

    return hr;
}


/*******************************************************************
 AddFirewallException

********************************************************************/
static HRESULT AddFirewallException(
    __in FIREWALL_EXCEPTION_ATTRIBUTES const& attrs,
    __in BOOL fIgnoreFailures
)
{
    HRESULT hr = S_OK;
    BSTR bstrName = NULL;
    INetFwRules* pNetFwRules = NULL;
    INetFwRule* pNetFwRule = NULL;

    BOOL fIgnoreUpdates = feaIgnoreUpdates == (attrs.iAttributes & feaIgnoreUpdates);
    BOOL fEnableOnUpdate = feaEnableOnUpdate == (attrs.iAttributes & feaEnableOnUpdate);
    BOOL fAddINetFwRule2 = feaAddINetFwRule2 == (attrs.iAttributes & feaAddINetFwRule2);
    BOOL fAddINetFwRule3 = feaAddINetFwRule3 == (attrs.iAttributes & feaAddINetFwRule3);

    // convert to BSTRs to make COM happy
    bstrName = ::SysAllocString(attrs.pwzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString for name");

    // get the collection of firewall rules
    hr = GetFirewallRules(fIgnoreFailures, &pNetFwRules);
    ExitOnFailure(hr, "failed to get firewall exception object");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    // try to find it (i.e., support reinstall)
    hr = pNetFwRules->Item(bstrName, &pNetFwRule);
    if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
    {
        hr = CreateFwRuleObject(bstrName, &pNetFwRule);
        ExitOnFailure(hr, "failed to create FwRule object '%ls'", attrs.pwzName);

        // set attributes of the new firewall rule
        hr = UpdateFwRuleObject(pNetFwRule, FALSE, attrs);
        ExitOnFailure(hr, "failed to create INetFwRule firewall exception '%ls'", attrs.pwzName);

        if (fAddINetFwRule2)
        {
            hr = UpdateFwRule2Object(pNetFwRule, FALSE, attrs);
            ExitOnFailure(hr, "failed to create INetFwRule2 firewall exception '%ls'", attrs.pwzName);
        }

        if (fAddINetFwRule3)
        {
            hr = UpdateFwRule3Object(pNetFwRule, FALSE, attrs);
            ExitOnFailure(hr, "failed to create INetFwRule3 firewall exception '%ls'", attrs.pwzName);
        }

        hr = pNetFwRules->Add(pNetFwRule);
        ExitOnFailure(hr, "failed to add firewall exception '%ls' to the list", attrs.pwzName);
    }
    else
    {
        // we found an existing firewall rule (if we succeeded, that is)
        ExitOnFailure(hr, "failed trying to find existing firewall exception '%ls'", attrs.pwzName);

        if (fEnableOnUpdate)
        {
            hr = pNetFwRule->put_Enabled(VARIANT_TRUE);
            ExitOnFailure(hr, "failed to enable existing firewall exception '%ls'", attrs.pwzName);
        }
        else if (!fIgnoreUpdates)
        {
            // overwrite attributes of the existing firewall rule
            hr = UpdateFwRuleObject(pNetFwRule, TRUE, attrs);
            ExitOnFailure(hr, "failed to update INetFwRule firewall exception '%ls'", attrs.pwzName);

            if (fAddINetFwRule2)
            {
                hr = UpdateFwRule2Object(pNetFwRule, TRUE, attrs);
                ExitOnFailure(hr, "failed to update INetFwRule2 firewall exception '%ls'", attrs.pwzName);
            }

            if (fAddINetFwRule3)
            {
                hr = UpdateFwRule3Object(pNetFwRule, TRUE, attrs);
                ExitOnFailure(hr, "failed to update INetFwRule3 firewall exception '%ls'", attrs.pwzName);
            }
        }
    }

LExit:
    ReleaseBSTR(bstrName);
    ReleaseObject(pNetFwRules);
    ReleaseObject(pNetFwRule);

    return fIgnoreFailures ? S_OK : hr;
}


/*******************************************************************
 RemoveException - Removes all exception rules with the given name.

********************************************************************/
static HRESULT RemoveException(
    __in LPCWSTR wzName,
    __in BOOL fIgnoreFailures
)
{
    HRESULT hr = S_OK;;
    INetFwRules* pNetFwRules = NULL;

    // convert to BSTRs to make COM happy
    BSTR bstrName = ::SysAllocString(wzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString for path");

    // get the collection of firewall rules
    hr = GetFirewallRules(fIgnoreFailures, &pNetFwRules);
    ExitOnFailure(hr, "failed to get firewall rules object");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    hr = pNetFwRules->Remove(bstrName);
    ExitOnFailure(hr, "failed to remove firewall exception for name %ls", wzName);

LExit:
    ReleaseBSTR(bstrName);
    ReleaseObject(pNetFwRules);

    return fIgnoreFailures ? S_OK : hr;
}


/*******************************************************************
 ExecFirewallExceptions - deferred custom action entry point to
   register and remove firewall exceptions.

********************************************************************/
extern "C" UINT __stdcall ExecFirewallExceptions(
    __in MSIHANDLE hInstall
)
{
    HRESULT hr = S_OK;
    LPWSTR pwz = NULL;
    LPWSTR pwzCustomActionData = NULL;
    int iTodo = WCA_TODO_UNKNOWN;

    FIREWALL_EXCEPTION_ATTRIBUTES attrs = { 0 };

    // initialize
    hr = WcaInitialize(hInstall, "ExecFirewallExceptions");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty(L"CustomActionData", &pwzCustomActionData);
    ExitOnFailure(hr, "failed to get CustomActionData");
    WcaLog(LOGMSG_TRACEONLY, "CustomActionData: %ls", pwzCustomActionData);

    hr = ::CoInitialize(NULL);
    ExitOnFailure(hr, "failed to initialize COM");

    // loop through all the passed in data
    pwz = pwzCustomActionData;
    while (pwz && *pwz)
    {
        // extract the custom action data and if rolling back, swap INSTALL and UNINSTALL
        hr = WcaReadIntegerFromCaData(&pwz, &iTodo);
        ExitOnFailure(hr, "failed to read todo from custom action data");
        if (::MsiGetMode(hInstall, MSIRUNMODE_ROLLBACK))
        {
            if (WCA_TODO_INSTALL == iTodo)
            {
                iTodo = WCA_TODO_UNINSTALL;
            }
            else if (WCA_TODO_UNINSTALL == iTodo)
            {
                iTodo = WCA_TODO_INSTALL;
            }
        }

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzName);
        ExitOnFailure(hr, "failed to read name from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iProfile);
        ExitOnFailure(hr, "failed to read profile from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzRemoteAddresses);
        ExitOnFailure(hr, "failed to read remote addresses from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iAttributes);
        ExitOnFailure(hr, "failed to read attributes from custom action data");
        BOOL fIgnoreFailures = feaIgnoreFailures == (attrs.iAttributes & feaIgnoreFailures);

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzApplicationName);
        ExitOnFailure(hr, "failed to read file path from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzLocalPorts);
        ExitOnFailure(hr, "failed to read port from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iProtocol);
        ExitOnFailure(hr, "failed to read protocol from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzDescription);
        ExitOnFailure(hr, "failed to read protocol from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iDirection);
        ExitOnFailure(hr, "failed to read direction from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iAction);
        ExitOnFailure(hr, "failed to read action from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iEdgeTraversal);
        ExitOnFailure(hr, "failed to read edge traversal from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iEnabled);
        ExitOnFailure(hr, "failed to read enabled flag from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzGrouping);
        ExitOnFailure(hr, "failed to read grouping from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzIcmpTypesAndCodes);
        ExitOnFailure(hr, "failed to read icmp types and codes from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzInterfaces);
        ExitOnFailure(hr, "failed to read interfaces from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzInterfaceTypes);
        ExitOnFailure(hr, "failed to read interface types from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzLocalAddresses);
        ExitOnFailure(hr, "failed to read local addresses from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzRemotePorts);
        ExitOnFailure(hr, "failed to read remote port from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzServiceName);
        ExitOnFailure(hr, "failed to read service name from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzLocalAppPackageId);
        ExitOnFailure(hr, "failed to read local app package id from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzLocalUserAuthorizedList);
        ExitOnFailure(hr, "failed to read local user authorized list from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzLocalUserOwner);
        ExitOnFailure(hr, "failed to read local user owner from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzRemoteMachineAuthorizedList);
        ExitOnFailure(hr, "failed to read remote machine authorized list from custom action data");

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzRemoteUserAuthorizedList);
        ExitOnFailure(hr, "failed to read remote user authorized list from custom action data");

        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iSecureFlags);
        ExitOnFailure(hr, "failed to read exception secure flags from custom action data");

        switch (iTodo)
        {
        case WCA_TODO_INSTALL:
        case WCA_TODO_REINSTALL:
            WcaLog(LOGMSG_STANDARD, "Installing firewall exception %ls", attrs.pwzName);
            hr = AddFirewallException(attrs, fIgnoreFailures);
            ExitOnFailure(hr, "failed to add/update firewall exception for name '%ls'", attrs.pwzName);
            break;

        case WCA_TODO_UNINSTALL:
            WcaLog(LOGMSG_STANDARD, "Uninstalling firewall exception %ls", attrs.pwzName);
            hr = RemoveException(attrs.pwzName, fIgnoreFailures);
            ExitOnFailure(hr, "failed to remove firewall exception");
            break;
        }
    }

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(attrs.pwzName);
    ReleaseStr(attrs.pwzRemoteAddresses);
    ReleaseStr(attrs.pwzApplicationName);
    ReleaseStr(attrs.pwzLocalPorts);
    ReleaseStr(attrs.pwzDescription);
    ReleaseStr(attrs.pwzGrouping);
    ReleaseStr(attrs.pwzIcmpTypesAndCodes);
    ReleaseStr(attrs.pwzInterfaces);
    ReleaseStr(attrs.pwzInterfaceTypes);
    ReleaseStr(attrs.pwzLocalAddresses);
    ReleaseStr(attrs.pwzRemotePorts);
    ReleaseStr(attrs.pwzServiceName);
    ReleaseStr(attrs.pwzLocalAppPackageId);
    ReleaseStr(attrs.pwzLocalUserAuthorizedList);
    ReleaseStr(attrs.pwzLocalUserOwner);
    ReleaseStr(attrs.pwzRemoteMachineAuthorizedList);
    ReleaseStr(attrs.pwzRemoteUserAuthorizedList);
    ::CoUninitialize();

    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}
