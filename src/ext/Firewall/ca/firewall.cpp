// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

LPCWSTR vcsFirewallExceptionQuery =
    L"SELECT `Name`, `RemoteAddresses`, `Port`, `Protocol`, `Program`, `Attributes`, `Profile`, `Component_`, `Description`, `Direction` FROM `Wix4FirewallException`";
enum eFirewallExceptionQuery { feqName = 1, feqRemoteAddresses, feqPort, feqProtocol, feqProgram, feqAttributes, feqProfile, feqComponent, feqDescription, feqDirection };
enum eFirewallExceptionTarget { fetPort = 1, fetApplication, fetUnknown };
enum eFirewallExceptionAttributes { feaIgnoreFailures = 1 };

struct FIREWALL_EXCEPTION_ATTRIBUTES
{
    LPWSTR pwzName;

    LPWSTR pwzRemoteAddresses;
    LPWSTR pwzPort;
    int iProtocol;
    LPWSTR pwzProgram;
    int iAttributes;
    int iProfile;
    LPWSTR pwzDescription;
    int iDirection;
};

/******************************************************************
 SchedFirewallExceptions - immediate custom action worker to 
   register and remove firewall exceptions.

********************************************************************/
static UINT SchedFirewallExceptions(
    __in MSIHANDLE hInstall,
    WCA_TODO todoSched
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
    if (S_OK != WcaTableExists(L"Wix4FirewallException"))
    {
        WcaLog(LOGMSG_STANDARD, "Wix4FirewallException table doesn't exist, so there are no firewall exceptions to configure.");
        ExitFunction();
    }

    // query and loop through all the firewall exceptions
    hr = WcaOpenExecuteView(vcsFirewallExceptionQuery, &hView);
    ExitOnFailure(hr, "Failed to open view on Wix4FirewallException table");

    while (S_OK == (hr = WcaFetchRecord(hView, &hRec)))
    {
        hr = WcaGetRecordFormattedString(hRec, feqName, &attrs.pwzName);
        ExitOnFailure(hr, "Failed to get firewall exception name.");

        hr = WcaGetRecordFormattedString(hRec, feqRemoteAddresses, &attrs.pwzRemoteAddresses);
        ExitOnFailure(hr, "Failed to get firewall exception remote addresses.");

        hr = WcaGetRecordFormattedString(hRec, feqPort, &attrs.pwzPort);
        ExitOnFailure(hr, "Failed to get firewall exception port.");

        hr = WcaGetRecordInteger(hRec, feqProtocol, &attrs.iProtocol);
        ExitOnFailure(hr, "Failed to get firewall exception protocol.");

        hr = WcaGetRecordFormattedString(hRec, feqProgram, &attrs.pwzProgram);
        ExitOnFailure(hr, "Failed to get firewall exception program.");

        hr = WcaGetRecordInteger(hRec, feqAttributes, &attrs.iAttributes);
        ExitOnFailure(hr, "Failed to get firewall exception attributes.");
        
        hr = WcaGetRecordInteger(hRec, feqProfile, &attrs.iProfile);
        ExitOnFailure(hr, "Failed to get firewall exception profile.");

        hr = WcaGetRecordString(hRec, feqComponent, &pwzComponent);
        ExitOnFailure(hr, "Failed to get firewall exception component.");

        hr = WcaGetRecordFormattedString(hRec, feqDescription, &attrs.pwzDescription);
        ExitOnFailure(hr, "Failed to get firewall exception description.");

        hr = WcaGetRecordInteger(hRec, feqDirection, &attrs.iDirection);
        ExitOnFailure(hr, "Failed to get firewall exception direction.");

        // figure out what we're doing for this exception, treating reinstall the same as install
        WCA_TODO todoComponent = WcaGetComponentToDo(pwzComponent);
        if ((WCA_TODO_REINSTALL == todoComponent ? WCA_TODO_INSTALL : todoComponent) != todoSched)
        {
            WcaLog(LOGMSG_STANDARD, "Component '%ls' action state (%d) doesn't match request (%d)", pwzComponent, todoComponent, todoSched);
            continue;
        }

        // action :: name :: profile :: remoteaddresses :: attributes :: target :: {port::protocol | path}
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

        if (*attrs.pwzProgram)
        {
            // If program is defined, we have an application exception.
            hr = WcaWriteIntegerToCaData(fetApplication, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write exception target (application) to custom action data");

            hr = WcaWriteStringToCaData(attrs.pwzProgram, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write application path to custom action data");
        }
        else
        {
            // we have a port-only exception
            hr = WcaWriteIntegerToCaData(fetPort, &pwzCustomActionData);
            ExitOnFailure(hr, "failed to write exception target (port) to custom action data");
        }

        hr = WcaWriteStringToCaData(attrs.pwzPort, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write application path to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iProtocol, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write exception protocol to custom action data");

        hr = WcaWriteStringToCaData(attrs.pwzDescription, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write firewall rule description to custom action data");

        hr = WcaWriteIntegerToCaData(attrs.iDirection, &pwzCustomActionData);
        ExitOnFailure(hr, "failed to write firewall rule direction to custom action data");
    }

    // reaching the end of the list is actually a good thing, not an error
    if (E_NOMOREITEMS == hr)
    {
        hr = S_OK;
    } 
    ExitOnFailure(hr, "failure occured while processing Wix4FirewallException table");

    // schedule ExecFirewallExceptions if there's anything to do
    if (pwzCustomActionData && *pwzCustomActionData)
    {
        WcaLog(LOGMSG_STANDARD, "Scheduling firewall exception (%ls)", pwzCustomActionData);

        if (WCA_TODO_INSTALL == todoSched)
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"RollbackFirewallExceptionsInstall"), pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall install exceptions rollback");            
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"ExecFirewallExceptionsInstall"), pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall install exceptions execution");
        }
        else
        {
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"RollbackFirewallExceptionsUninstall"), pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
            ExitOnFailure(hr, "failed to schedule firewall uninstall exceptions rollback");    
            hr = WcaDoDeferredAction(CUSTOM_ACTION_DECORATION(L"ExecFirewallExceptionsUninstall"), pwzCustomActionData, cFirewallExceptions * COST_FIREWALL_EXCEPTION);
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
    ReleaseStr(attrs.pwzPort);
    ReleaseStr(attrs.pwzProgram);
    ReleaseStr(attrs.pwzDescription);
    ReleaseStr(pwzComponent);
    ReleaseStr(pwzCustomActionData);

    return WcaFinalize(er = FAILED(hr) ? ERROR_INSTALL_FAILURE : er);
}

/******************************************************************
 SchedFirewallExceptionsInstall - immediate custom action entry
   point to register firewall exceptions.

********************************************************************/
extern "C" UINT __stdcall SchedFirewallExceptionsInstall(
    __in MSIHANDLE hInstall
    )
{
    return SchedFirewallExceptions(hInstall, WCA_TODO_INSTALL);
}

/******************************************************************
 SchedFirewallExceptionsUninstall - immediate custom action entry
   point to remove firewall exceptions.

********************************************************************/
extern "C" UINT __stdcall SchedFirewallExceptionsUninstall(
    __in MSIHANDLE hInstall
    )
{
    return SchedFirewallExceptions(hInstall, WCA_TODO_UNINSTALL);
}

/******************************************************************
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

/******************************************************************
 CreateFwRuleObject - CoCreate a firewall rule, and set the common set of properties which are shared
 between port and application firewall rules

********************************************************************/
static HRESULT CreateFwRuleObject(
    __in BSTR bstrName,
    __in FIREWALL_EXCEPTION_ATTRIBUTES const& attrs,
    __out INetFwRule** ppNetFwRule
    )
{
    HRESULT hr = S_OK;
    BSTR bstrRemoteAddresses = NULL;
    BSTR bstrPort = NULL;
    BSTR bstrDescription = NULL;
    INetFwRule* pNetFwRule = NULL;
    *ppNetFwRule = NULL;

    // convert to BSTRs to make COM happy
    bstrRemoteAddresses = ::SysAllocString(attrs.pwzRemoteAddresses);
    ExitOnNull(bstrRemoteAddresses, hr, E_OUTOFMEMORY, "failed SysAllocString for remote addresses");
    bstrPort = ::SysAllocString(attrs.pwzPort);
    ExitOnNull(bstrPort, hr, E_OUTOFMEMORY, "failed SysAllocString for port");
    bstrDescription = ::SysAllocString(attrs.pwzDescription);
    ExitOnNull(bstrDescription, hr, E_OUTOFMEMORY, "failed SysAllocString for description");

    hr = ::CoCreateInstance(__uuidof(NetFwRule), NULL, CLSCTX_ALL, __uuidof(INetFwRule), (void**)&pNetFwRule);
    ExitOnFailure(hr, "failed to create NetFwRule object");

    hr = pNetFwRule->put_Name(bstrName);
    ExitOnFailure(hr, "failed to set exception name");

    hr = pNetFwRule->put_Profiles(static_cast<NET_FW_PROFILE_TYPE2>(attrs.iProfile));
    ExitOnFailure(hr, "failed to set exception profile");

    if (MSI_NULL_INTEGER != attrs.iProtocol)
    {
        hr = pNetFwRule->put_Protocol(static_cast<NET_FW_IP_PROTOCOL>(attrs.iProtocol));
        ExitOnFailure(hr, "failed to set exception protocol");
    }

    if (bstrPort && *bstrPort)
    {
        hr = pNetFwRule->put_LocalPorts(bstrPort);
        ExitOnFailure(hr, "failed to set exception port");
    }

    if (bstrRemoteAddresses && *bstrRemoteAddresses)
    {
        hr = pNetFwRule->put_RemoteAddresses(bstrRemoteAddresses);
        ExitOnFailure(hr, "failed to set exception remote addresses '%ls'", bstrRemoteAddresses);
    }

    if (bstrDescription && *bstrDescription)
    {
        hr = pNetFwRule->put_Description(bstrDescription);
        ExitOnFailure(hr, "failed to set exception description '%ls'", bstrDescription);
    }

    if (MSI_NULL_INTEGER != attrs.iDirection)
    {
        hr = pNetFwRule->put_Direction(static_cast<NET_FW_RULE_DIRECTION> (attrs.iDirection));
        ExitOnFailure(hr, "failed to set exception direction");
    }

    *ppNetFwRule = pNetFwRule;
    pNetFwRule = NULL;

LExit:
    ReleaseBSTR(bstrRemoteAddresses);
    ReleaseBSTR(bstrPort);
    ReleaseBSTR(bstrDescription);
    ReleaseObject(pNetFwRule);

    return hr;
}

/******************************************************************
 AddApplicationException

********************************************************************/
static HRESULT AddApplicationException(
    __in FIREWALL_EXCEPTION_ATTRIBUTES const& attrs,
    __in BOOL fIgnoreFailures
    )
{
    HRESULT hr = S_OK;
    BSTR bstrFile = NULL;
    BSTR bstrName = NULL;
    INetFwRules* pNetFwRules = NULL;
    INetFwRule* pNetFwRule = NULL;

    // convert to BSTRs to make COM happy
    bstrFile = ::SysAllocString(attrs.pwzProgram);
    ExitOnNull(bstrFile, hr, E_OUTOFMEMORY, "failed SysAllocString for path");
    bstrName = ::SysAllocString(attrs.pwzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString for name");

    // get the collection of firewall rules
    hr = GetFirewallRules(fIgnoreFailures, &pNetFwRules);
    ExitOnFailure(hr, "failed to get firewall rules object");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    // try to find it (i.e., support reinstall)
    hr = pNetFwRules->Item(bstrName, &pNetFwRule);
    if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
    {
        hr = CreateFwRuleObject(bstrName, attrs, &pNetFwRule);
        ExitOnFailure(hr, "failed to create FwRule object");

        // set edge traversal to true
        hr = pNetFwRule->put_EdgeTraversal(VARIANT_TRUE);
        ExitOnFailure(hr, "failed to set application exception edgetraversal property");
        
        // set path
        hr = pNetFwRule->put_ApplicationName(bstrFile);
        ExitOnFailure(hr, "failed to set application name");
        
        // enable it
        hr = pNetFwRule->put_Enabled(VARIANT_TRUE);
        ExitOnFailure(hr, "failed to to enable application exception");
     
        // add it to the list of authorized apps
        hr = pNetFwRules->Add(pNetFwRule);
        ExitOnFailure(hr, "failed to add app to the authorized apps list");
    }
    else
    {
        // we found an existing app exception (if we succeeded, that is)
        ExitOnFailure(hr, "failed trying to find existing app");
        
        // enable it (just in case it was disabled)
        pNetFwRule->put_Enabled(VARIANT_TRUE);
    }

LExit:
    ReleaseBSTR(bstrName);
    ReleaseBSTR(bstrFile);
    ReleaseObject(pNetFwRules);
    ReleaseObject(pNetFwRule);

    return fIgnoreFailures ? S_OK : hr;
}

/******************************************************************
 AddPortException

********************************************************************/
static HRESULT AddPortException(
    __in FIREWALL_EXCEPTION_ATTRIBUTES const& attrs,
    __in BOOL fIgnoreFailures
    )
{
    HRESULT hr = S_OK;
    BSTR bstrName = NULL;
    INetFwRules* pNetFwRules = NULL;
    INetFwRule* pNetFwRule = NULL;

    // convert to BSTRs to make COM happy
    bstrName = ::SysAllocString(attrs.pwzName);
    ExitOnNull(bstrName, hr, E_OUTOFMEMORY, "failed SysAllocString for name");

    // get the collection of firewall rules
    hr = GetFirewallRules(fIgnoreFailures, &pNetFwRules);
    ExitOnFailure(hr, "failed to get firewall rules object");
    if (S_FALSE == hr) // user or package author chose to ignore missing firewall
    {
        ExitFunction();
    }

    // try to find it (i.e., support reinstall)
    hr = pNetFwRules->Item(bstrName, &pNetFwRule);
    if (HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) == hr)
    {
        hr = CreateFwRuleObject(bstrName, attrs, &pNetFwRule);
        ExitOnFailure(hr, "failed to create FwRule object");

        // enable it
        hr = pNetFwRule->put_Enabled(VARIANT_TRUE);
        ExitOnFailure(hr, "failed to to enable port exception");

        // add it to the list of authorized ports
        hr = pNetFwRules->Add(pNetFwRule);
        ExitOnFailure(hr, "failed to add app to the authorized ports list");
    }
    else
    {
        // we found an existing port exception (if we succeeded, that is)
        ExitOnFailure(hr, "failed trying to find existing port rule");

        // enable it (just in case it was disabled)
        pNetFwRule->put_Enabled(VARIANT_TRUE);
    }

LExit:
    ReleaseBSTR(bstrName);
    ReleaseObject(pNetFwRules);
    ReleaseObject(pNetFwRule);

    return fIgnoreFailures ? S_OK : hr;
}

/******************************************************************
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
    ExitOnFailure(hr, "failed to remove firewall rule");

LExit:
    ReleaseBSTR(bstrName);
    ReleaseObject(pNetFwRules);

    return fIgnoreFailures ? S_OK : hr;
}

/******************************************************************
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
    int iTarget = fetUnknown;

    FIREWALL_EXCEPTION_ATTRIBUTES attrs = { 0 };

    // initialize
    hr = WcaInitialize(hInstall, "ExecFirewallExceptions");
    ExitOnFailure(hr, "failed to initialize");

    hr = WcaGetProperty( L"CustomActionData", &pwzCustomActionData);
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

        hr = WcaReadIntegerFromCaData(&pwz, &iTarget);
        ExitOnFailure(hr, "failed to read target from custom action data");

        if (iTarget == fetApplication)
        {
            hr = WcaReadStringFromCaData(&pwz, &attrs.pwzProgram);
            ExitOnFailure(hr, "failed to read file path from custom action data");
        }

        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzPort);
        ExitOnFailure(hr, "failed to read port from custom action data");
        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iProtocol);
        ExitOnFailure(hr, "failed to read protocol from custom action data");
        hr = WcaReadStringFromCaData(&pwz, &attrs.pwzDescription);
        ExitOnFailure(hr, "failed to read protocol from custom action data");
        hr = WcaReadIntegerFromCaData(&pwz, &attrs.iDirection);
        ExitOnFailure(hr, "failed to read direction from custom action data");

        switch (iTarget)
        {
        case fetPort:
            switch (iTodo)
            {
            case WCA_TODO_INSTALL:
            case WCA_TODO_REINSTALL:
                WcaLog(LOGMSG_STANDARD, "Installing firewall exception %ls on port %ls, protocol %d", attrs.pwzName, attrs.pwzPort, attrs.iProtocol);
                hr = AddPortException(attrs, fIgnoreFailures);
                ExitOnFailure(hr, "failed to add/update port exception for name '%ls' on port %ls, protocol %d", attrs.pwzName, attrs.pwzPort, attrs.iProtocol);
                break;

            case WCA_TODO_UNINSTALL:
                WcaLog(LOGMSG_STANDARD, "Uninstalling firewall exception %ls on port %ls, protocol %d", attrs.pwzName, attrs.pwzPort, attrs.iProtocol);
                hr = RemoveException(attrs.pwzName, fIgnoreFailures);
                ExitOnFailure(hr, "failed to remove port exception for name '%ls' on port %ls, protocol %d", attrs.pwzName, attrs.pwzPort, attrs.iProtocol);
                break;
            }
            break;

        case fetApplication:
            switch (iTodo)
            {
            case WCA_TODO_INSTALL:
            case WCA_TODO_REINSTALL:
                WcaLog(LOGMSG_STANDARD, "Installing firewall exception %ls (%ls)", attrs.pwzName, attrs.pwzProgram);
                hr = AddApplicationException(attrs, fIgnoreFailures);
                ExitOnFailure(hr, "failed to add/update application exception for name '%ls', file '%ls'", attrs.pwzName, attrs.pwzProgram);
                break;

            case WCA_TODO_UNINSTALL:
                WcaLog(LOGMSG_STANDARD, "Uninstalling firewall exception %ls (%ls)", attrs.pwzName, attrs.pwzProgram);
                hr = RemoveException(attrs.pwzName, fIgnoreFailures);
                ExitOnFailure(hr, "failed to remove application exception for name '%ls', file '%ls'", attrs.pwzName, attrs.pwzProgram);
                break;
            }
            break;
        }
    }

LExit:
    ReleaseStr(pwzCustomActionData);
    ReleaseStr(attrs.pwzName);
    ReleaseStr(attrs.pwzRemoteAddresses);
    ReleaseStr(attrs.pwzProgram);
    ReleaseStr(attrs.pwzPort);
    ReleaseStr(attrs.pwzDescription);
    ::CoUninitialize();

    return WcaFinalize(FAILED(hr) ? ERROR_INSTALL_FAILURE : ERROR_SUCCESS);
}
