// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Bootstrapper
{
    using namespace System;
    using namespace System::IO;
    using namespace System::Threading;
    using namespace Xunit;

struct EXIT_CODE_ITEM
{
    DWORD dwExitCode;
    HRESULT hrExpectedResult;
    BOOTSTRAPPER_APPLY_RESTART expectedRestart;
    LPCWSTR wzPackageId;
    HRESULT hrResultPerUser;
    BOOTSTRAPPER_APPLY_RESTART restartPerUser;
    HRESULT hrResultPerMachine;
    BOOTSTRAPPER_APPLY_RESTART restartPerMachine;
};

static BOOL STDAPICALLTYPE ExitCodeTest_ShellExecuteExW(
    __inout LPSHELLEXECUTEINFOW lpExecInfo
    );
static DWORD CALLBACK ExitCodeTest_ElevationThreadProc(
    __in LPVOID lpThreadParameter
    );
static BOOL STDAPICALLTYPE ExitCodeTest_CreateProcessW(
    __in_opt LPCWSTR lpApplicationName,
    __inout_opt LPWSTR lpCommandLine,
    __in_opt LPSECURITY_ATTRIBUTES lpProcessAttributes,
    __in_opt LPSECURITY_ATTRIBUTES lpThreadAttributes,
    __in BOOL bInheritHandles,
    __in DWORD dwCreationFlags,
    __in_opt LPVOID lpEnvironment,
    __in_opt LPCWSTR lpCurrentDirectory,
    __in LPSTARTUPINFOW lpStartupInfo,
    __out LPPROCESS_INFORMATION lpProcessInformation
    );
static DWORD CALLBACK ExitCodeTest_PackageThreadProc(
    __in LPVOID lpThreadParameter
    );
static int ExitCodeTest_GenericMessageHandler(
    __in GENERIC_EXECUTE_MESSAGE* pMessage,
    __in LPVOID pvContext
    );
static void LoadEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    );

    public ref class ExitCodeTest : BurnUnitTest
    {
    public:
        ExitCodeTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact]
        void ExitCodeHandlingTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_PIPE_CONNECTION* pConnection = &engineState.companionConnection;
            EXIT_CODE_ITEM rgExitCodeItems[] =
            {
                { 0, S_OK, BOOTSTRAPPER_APPLY_RESTART_NONE, L"Standard" },
                { 1, HRESULT_FROM_WIN32(1), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Standard" },
                { ERROR_SUCCESS_REBOOT_REQUIRED, S_OK, BOOTSTRAPPER_APPLY_RESTART_REQUIRED, L"Standard" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED), S_OK, BOOTSTRAPPER_APPLY_RESTART_REQUIRED, L"Standard" },
                { ERROR_SUCCESS_RESTART_REQUIRED, S_OK, BOOTSTRAPPER_APPLY_RESTART_REQUIRED, L"Standard" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_SUCCESS_RESTART_REQUIRED), S_OK, BOOTSTRAPPER_APPLY_RESTART_REQUIRED, L"Standard" },
                { ERROR_SUCCESS_REBOOT_INITIATED, S_OK, BOOTSTRAPPER_APPLY_RESTART_INITIATED, L"Standard" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_INITIATED), S_OK, BOOTSTRAPPER_APPLY_RESTART_INITIATED, L"Standard" },
                { ERROR_FAIL_REBOOT_REQUIRED, HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_REQUIRED), BOOTSTRAPPER_APPLY_RESTART_REQUIRED, L"Standard" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_REQUIRED), HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_REQUIRED), BOOTSTRAPPER_APPLY_RESTART_REQUIRED, L"Standard" },
                { ERROR_FAIL_REBOOT_INITIATED, HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_INITIATED), BOOTSTRAPPER_APPLY_RESTART_INITIATED, L"Standard" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_INITIATED), HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_INITIATED), BOOTSTRAPPER_APPLY_RESTART_INITIATED, L"Standard" },
                { 0, E_FAIL, BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { 1, S_OK, BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { 3, S_OK, BOOTSTRAPPER_APPLY_RESTART_REQUIRED, L"Custom" },
                { 4, S_OK, BOOTSTRAPPER_APPLY_RESTART_INITIATED, L"Custom" },
                { 5, HRESULT_FROM_WIN32(5), BOOTSTRAPPER_APPLY_RESTART_REQUIRED, L"Custom" },
                { (DWORD)HRESULT_FROM_WIN32(5), HRESULT_FROM_WIN32(5), BOOTSTRAPPER_APPLY_RESTART_REQUIRED, L"Custom" },
                { 6, HRESULT_FROM_WIN32(6), BOOTSTRAPPER_APPLY_RESTART_INITIATED, L"Custom" },
                { (DWORD)HRESULT_FROM_WIN32(6), HRESULT_FROM_WIN32(6), BOOTSTRAPPER_APPLY_RESTART_INITIATED, L"Custom" },
                { ERROR_SUCCESS_REBOOT_REQUIRED, HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED), HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_REQUIRED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { ERROR_SUCCESS_RESTART_REQUIRED, HRESULT_FROM_WIN32(ERROR_SUCCESS_RESTART_REQUIRED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_SUCCESS_RESTART_REQUIRED), HRESULT_FROM_WIN32(ERROR_SUCCESS_RESTART_REQUIRED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { ERROR_SUCCESS_REBOOT_INITIATED, HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_INITIATED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_INITIATED), HRESULT_FROM_WIN32(ERROR_SUCCESS_REBOOT_INITIATED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { ERROR_FAIL_REBOOT_REQUIRED, HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_REQUIRED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_REQUIRED), HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_REQUIRED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { ERROR_FAIL_REBOOT_INITIATED, HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_INITIATED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
                { (DWORD)HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_INITIATED), HRESULT_FROM_WIN32(ERROR_FAIL_REBOOT_INITIATED), BOOTSTRAPPER_APPLY_RESTART_NONE, L"Custom" },
            };

            engineState.sczBundleEngineWorkingPath = L"tests\\ignore\\this\\path\\to\\burn.exe";

            try
            {
                ShelFunctionOverride(ExitCodeTest_ShellExecuteExW);
                CoreFunctionOverride(ExitCodeTest_CreateProcessW, ThrdWaitForCompletion);

                //
                // per-user side setup
                //
                LoadEngineState(&engineState);

                hr = ElevationElevate(&engineState, WM_BURN_ELEVATE, NULL);
                TestThrowOnFailure(hr, L"Failed to elevate.");

                for (DWORD i = 0; i < countof(rgExitCodeItems); ++i)
                {
                    // "run" the package both per-user and per-machine
                    ExecuteExePackage(&engineState, rgExitCodeItems + i);
                }

                //
                // initiate termination
                //
                hr = BurnPipeTerminateChildProcess(pConnection, 0, FALSE);
                TestThrowOnFailure(hr, L"Failed to terminate elevated process.");

                // check results
                for (DWORD i = 0; i < countof(rgExitCodeItems); ++i)
                {
                    EXIT_CODE_ITEM* pExitCode = rgExitCodeItems + i;
                    String^ packageId = gcnew String(pExitCode->wzPackageId);
                    String^ exitCodeString = ((UInt32)pExitCode->dwExitCode).ToString();

                    NativeAssert::SpecificReturnCode(pExitCode->hrExpectedResult, pExitCode->hrResultPerMachine, L"Per-machine package: {0}, exit code: {1}", packageId, exitCodeString);
                    Assert::True(pExitCode->expectedRestart == pExitCode->restartPerMachine, String::Format("Per-machine package: {0}, exit code: {1}, expected restart type '{2}' but got '{3}'", packageId, exitCodeString, gcnew String(LoggingRestartToString(pExitCode->expectedRestart)), gcnew String(LoggingRestartToString(pExitCode->restartPerMachine))));

                    NativeAssert::SpecificReturnCode(pExitCode->hrExpectedResult, pExitCode->hrResultPerUser, L"Per-user package: {0}, exit code: {1}", packageId, exitCodeString);
                    Assert::True(pExitCode->expectedRestart == pExitCode->restartPerUser, String::Format("Per-user package: {0}, exit code: {1}, expected restart type '{2}' but got '{3}'", packageId, exitCodeString, gcnew String(LoggingRestartToString(pExitCode->expectedRestart)), gcnew String(LoggingRestartToString(pExitCode->restartPerUser))));
                }
            }
            finally
            {
                VariablesUninitialize(&engineState.variables);
                BurnPipeConnectionUninitialize(pConnection);
            }
        }

    private:
        void ExecuteExePackage(
            __in BURN_ENGINE_STATE* pEngineState,
            __in EXIT_CODE_ITEM* pExitCode
            )
        {
            HRESULT hr = S_OK;
            LPWSTR sczExitCode = NULL;
            BURN_PACKAGE* pPackage = NULL;
            BURN_EXECUTE_ACTION executeAction = { };
            BURN_PIPE_CONNECTION* pConnection = &pEngineState->companionConnection;
            BOOL fRollback = FALSE;

            hr = PackageFindById(&pEngineState->packages, pExitCode->wzPackageId, &pPackage);
            TestThrowOnFailure(hr, L"Failed to find package.");

            executeAction.type = BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE;
            executeAction.exePackage.action = BOOTSTRAPPER_ACTION_STATE_INSTALL;
            executeAction.exePackage.pPackage = pPackage;

            try
            {
                hr = StrAllocFormatted(&sczExitCode, L"%u", pExitCode->dwExitCode);
                TestThrowOnFailure(hr, L"Failed to convert exit code to string.");

                hr = VariableSetString(&pEngineState->variables, L"ExeExitCode", sczExitCode, FALSE, FALSE);
                TestThrowOnFailure(hr, L"Failed to set variable.");

                pExitCode->hrResultPerMachine = ElevationExecuteExePackage(pConnection->hPipe, &executeAction, &pEngineState->variables, fRollback, ExitCodeTest_GenericMessageHandler, NULL, &pExitCode->restartPerMachine);

                pExitCode->hrResultPerUser = ExeEngineExecutePackage(&executeAction, &pEngineState->cache, &pEngineState->variables, fRollback, ExitCodeTest_GenericMessageHandler, NULL, &pExitCode->restartPerUser);
            }
            finally
            {
                ReleaseStr(sczExitCode);
            }
        }
    };


static BOOL STDAPICALLTYPE ExitCodeTest_ShellExecuteExW(
    __inout LPSHELLEXECUTEINFOW lpExecInfo
    )
{
    HRESULT hr = S_OK;
    LPWSTR scz = NULL;

    hr = StrAllocString(&scz, lpExecInfo->lpParameters, 0);
    ExitOnFailure(hr, "Failed to copy arguments.");

    // Pretend this thread is the elevated process.
    lpExecInfo->hProcess = ::CreateThread(NULL, 0, ExitCodeTest_ElevationThreadProc, scz, 0, NULL);
    ExitOnNullWithLastError(lpExecInfo->hProcess, hr, "Failed to create thread.");
    scz = NULL;

LExit:
    ReleaseStr(scz);

    return SUCCEEDED(hr);
}

static DWORD CALLBACK ExitCodeTest_ElevationThreadProc(
    __in LPVOID lpThreadParameter
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczArguments = (LPWSTR)lpThreadParameter;
    BURN_ENGINE_STATE engineState = { };
    BURN_PIPE_CONNECTION* pConnection = &engineState.companionConnection;
    HANDLE hLock = NULL;
    DWORD dwChildExitCode = 0;
    BOOL fRestart = FALSE;
    BOOL fApplying = FALSE;

    LoadEngineState(&engineState);

    StrAlloc(&pConnection->sczName, MAX_PATH);
    StrAlloc(&pConnection->sczSecret, MAX_PATH);

    // parse command line arguments
    if (3 != swscanf_s(sczArguments, L"-q -burn.elevated %s %s %u", pConnection->sczName, MAX_PATH, pConnection->sczSecret, MAX_PATH, &pConnection->dwProcessId))
    {
        hr = E_INVALIDARG;
        ExitOnFailure(hr, "Failed to parse argument string.");
    }

    // set up connection with per-user process
    hr = BurnPipeChildConnect(pConnection, TRUE);
    ExitOnFailure(hr, "Failed to connect to per-user process.");

    hr = ElevationChildPumpMessages(pConnection->hPipe, pConnection->hCachePipe, &engineState.approvedExes, &engineState.cache, &engineState.containers, &engineState.packages, &engineState.payloads, &engineState.variables, &engineState.registration, &engineState.userExperience, &hLock, &dwChildExitCode, &fRestart, &fApplying);
    ExitOnFailure(hr, "Failed while pumping messages in child 'process'.");

LExit:
    BurnPipeConnectionUninitialize(pConnection);
    VariablesUninitialize(&engineState.variables);
    ReleaseStr(sczArguments);

    return FAILED(hr) ? (DWORD)hr : dwChildExitCode;
}

static BOOL STDAPICALLTYPE ExitCodeTest_CreateProcessW(
    __in_opt LPCWSTR /*lpApplicationName*/,
    __inout_opt LPWSTR lpCommandLine,
    __in_opt LPSECURITY_ATTRIBUTES /*lpProcessAttributes*/,
    __in_opt LPSECURITY_ATTRIBUTES /*lpThreadAttributes*/,
    __in BOOL /*bInheritHandles*/,
    __in DWORD /*dwCreationFlags*/,
    __in_opt LPVOID /*lpEnvironment*/,
    __in_opt LPCWSTR /*lpCurrentDirectory*/,
    __in LPSTARTUPINFOW /*lpStartupInfo*/,
    __out LPPROCESS_INFORMATION lpProcessInformation
    )
{
    HRESULT hr = S_OK;
    LPWSTR scz = NULL;
    LPCWSTR wzArgs = lpCommandLine;

    hr = StrAllocString(&scz, wzArgs, 0);
    ExitOnFailure(hr, "Failed to copy arguments.");

    // Pretend this thread is the package process.
    lpProcessInformation->hProcess = ::CreateThread(NULL, 0, ExitCodeTest_PackageThreadProc, scz, 0, NULL);
    ExitOnNullWithLastError(lpProcessInformation->hProcess, hr, "Failed to create thread.");

    scz = NULL;

LExit:
    ReleaseStr(scz);

    return SUCCEEDED(hr);
}

static DWORD CALLBACK ExitCodeTest_PackageThreadProc(
    __in LPVOID lpThreadParameter
    )
{
    HRESULT hr = S_OK;
    LPWSTR sczArguments = (LPWSTR)lpThreadParameter;
    int argc = 0;
    LPWSTR* argv = NULL;
    DWORD dwResult = 0;

    hr = AppParseCommandLine(sczArguments, &argc, &argv);
    ExitOnFailure(hr, "Failed to parse command line: %ls", sczArguments);

    hr = StrStringToUInt32(argv[1], 0, reinterpret_cast<UINT*>(&dwResult));
    ExitOnFailure(hr, "Failed to convert %ls to DWORD.", argv[1]);

LExit:
    AppFreeCommandLineArgs(argv);
    ReleaseStr(sczArguments);

    return FAILED(hr) ? (DWORD)hr : dwResult;
}

static int ExitCodeTest_GenericMessageHandler(
    __in GENERIC_EXECUTE_MESSAGE* /*pMessage*/,
    __in LPVOID /*pvContext*/
    )
{
    return IDNOACTION;
}

static void LoadEngineState(
    __in BURN_ENGINE_STATE* pEngineState
    )
{
    HRESULT hr = S_OK;
    IXMLDOMElement* pixeBundle = NULL;

    LPCWSTR wzDocument =
        L"<BurnManifest>"
        L"    <Payload Id='test.exe' FilePath='test.exe' Packaging='external' SourcePath='test.exe' Hash='000000000000' FileSize='1' />"
        L"    <Chain>"
        L"        <ExePackage Id='Custom' Cache='remove' CacheId='test.exe' InstallSize='1' Size='1' PerMachine='no' Permanent='yes' Vital='yes' DetectCondition='' InstallArguments='[ExeExitCode]' UninstallArguments='' Uninstallable='no' RepairArguments='' Repairable='no' Protocol='none' DetectionType='condition'>"
        L"            <ExitCode Code='0' Type='2' />"
        L"            <ExitCode Code='3' Type='3' />"
        L"            <ExitCode Code='4' Type='4' />"
        L"            <ExitCode Code='5' Type='5' />"
        L"            <ExitCode Code='-2147024891' Type='5' />"
        L"            <ExitCode Code='6' Type='6' />"
        L"            <ExitCode Code='-2147024890' Type='6' />"
        L"            <ExitCode Code='3010' Type='2' />"
        L"            <ExitCode Code='-2147021886' Type='2' />"
        L"            <ExitCode Code='3011' Type='2' />"
        L"            <ExitCode Code='-2147021885' Type='2' />"
        L"            <ExitCode Code='1641' Type='2' />"
        L"            <ExitCode Code='-2147023255' Type='2' />"
        L"            <ExitCode Code='3017' Type='2' />"
        L"            <ExitCode Code='-2147021879' Type='2' />"
        L"            <ExitCode Code='3018' Type='2' />"
        L"            <ExitCode Code='-2147021878' Type='2' />"
        L"            <ExitCode Code='*' Type='1' />"
        L"            <PayloadRef Id='test.exe' />"
        L"        </ExePackage>"
        L"        <ExePackage Id='Standard' Cache='remove' CacheId='test.exe' InstallSize='1' Size='1' PerMachine='no' Permanent='yes' Vital='yes' DetectCondition='' InstallArguments='[ExeExitCode]' UninstallArguments='' Uninstallable='no' RepairArguments='' Repairable='no' Protocol='none' DetectionType='condition'>"
        L"            <PayloadRef Id='test.exe' />"
        L"        </ExePackage>"
        L"    </Chain>"
        L"</BurnManifest>";

    VariableInitialize(&pEngineState->variables);

    BurnPipeConnectionInitialize(&pEngineState->companionConnection);

    hr = CacheInitialize(&pEngineState->cache, &pEngineState->internalCommand);
    TestThrowOnFailure(hr, "CacheInitialize failed.");

    // load XML document
    LoadBundleXmlHelper(wzDocument, &pixeBundle);

    hr = PayloadsParseFromXml(&pEngineState->payloads, &pEngineState->containers, &pEngineState->layoutPayloads, pixeBundle);
    TestThrowOnFailure(hr, "Failed to parse payloads from manifest.");

    hr = PackagesParseFromXml(&pEngineState->packages, &pEngineState->payloads, pixeBundle);
    TestThrowOnFailure(hr, "Failed to parse packages from manifest.");

    ReleaseObject(pixeBundle);
}
}
}
}
}
}
