// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"

static HRESULT WINAPI PlanTestBAProc(
    __in BOOTSTRAPPER_APPLICATION_MESSAGE message,
    __in const LPVOID pvArgs,
    __inout LPVOID pvResults,
    __in_opt LPVOID pvContext
    );

static LPCWSTR wzMsiTransactionManifest =
    L"<Bundle>"
    L"    <Log PathVariable='WixBundleLog' Prefix='~manual_BundleA' Extension='log' />"
    L"    <RelatedBundle Id='{90ED10D5-B187-4470-B498-05D80DAB729A}' Action='Upgrade' />"
    L"    <Variable Id='TestName' Value='MsiTransaction' Type='string' Hidden='no' Persisted='no' />"
    L"    <Variable Id='WixBundleName' Hidden='no' Persisted='yes' />"
    L"    <Variable Id='WixBundleOriginalSource' Hidden='no' Persisted='yes' />"
    L"    <Variable Id='WixBundleOriginalSourceFolder' Hidden='no' Persisted='yes' />"
    L"    <Variable Id='WixBundleLastUsedSource' Hidden='no' Persisted='yes' />"
    L"    <UX>"
    L"        <Payload Id='WixStandardBootstrapperApplication.RtfLicense' FilePath='wixstdba.dll' FileSize='1312768' Hash='B331BC5C819F019981D2524C48CC370DE2BFE9F8' Packaging='embedded' SourcePath='u0' />"
    L"        <Payload Id='payF3wjO.aj6M8ftq9wrfYB1N4aw4g' FilePath='thm.xml' FileSize='8048' Hash='A712A9E1427A4EE1D0B68343A54019A3FC9967CB' Packaging='embedded' SourcePath='u1' />"
    L"        <Payload Id='payViPRdlZD7MPIBgwmX2Hv5C6oKt4' FilePath='thm.wxl' FileSize='3926' Hash='B88FAB9B6C2B52FBE017F91B0915781C6B76058E' Packaging='embedded' SourcePath='u2' />"
    L"        <Payload Id='payjqSD44latbvJnf4vAQuVMUST73A' FilePath='logo.png' FileSize='852' Hash='239F10674BF6022854C1F1BF7C91955BDE34D3E4' Packaging='embedded' SourcePath='u3' />"
    L"        <Payload Id='pay60yHh1x6HODo4M_38Pud7jhl2Ig' FilePath='license.rtf' FileSize='4908' Hash='383034848F8CC4F3C8E795CC0F4D716A285E9465' Packaging='embedded' SourcePath='u4' />"
    L"        <Payload Id='uxTxMXPVMXwQrPTMIGa5WGt93w0Ns' FilePath='BootstrapperApplicationData.xml' FileSize='6870' Hash='5302818DB5BD565463715D3C7099FE5123474476' Packaging='embedded' SourcePath='u5' />"
    L"    </UX>"
    L"    <Container Id='WixAttachedContainer' FileSize='9198' Hash='D932DEBC15B7EC41B3EB64DD075A1C7148C2BD6D' FilePath='BundleA.exe' AttachedIndex='1' Attached='yes' Primary='yes' />"
    L"    <Payload Id='PackageA' FilePath='~manual_PackageA.msi' FileSize='32768' Hash='4011C700186B4C162B2A50D981C895108AD67EBB' Packaging='embedded' SourcePath='a0' Container='WixAttachedContainer' />"
    L"    <Payload Id='PackageB' FilePath='~manual_PackageB.msi' FileSize='32768' Hash='0DA2766C6B5D2253A37675D89E6F15A70EDD18EB' Packaging='embedded' SourcePath='a1' Container='WixAttachedContainer' />"
    L"    <Payload Id='PackageC' FilePath='~manual_PackageC.msi' FileSize='32768' Hash='B5E1B37DCF08B7D88F2EB45D063513BDE052E5E4' Packaging='embedded' SourcePath='a2' Container='WixAttachedContainer' />"
    L"    <Payload Id='cab1QmlL013Hqv_44W64R0cvnHn_2c' FilePath='cab1.cab' FileSize='973' Hash='7A51FCEDBCD0A697A94F0C47A89BDD5EFCC0CB4B' Packaging='embedded' SourcePath='a3' Container='WixAttachedContainer' />"
    L"    <Payload Id='cabQH1Sgh7w2K8tLIftUaaWVhMWt0s' FilePath='cab1.cab' FileSize='985' Hash='32EFE9983CB1FF0905A3725B901D0BBD5334E616' Packaging='embedded' SourcePath='a4' Container='WixAttachedContainer' />"
    L"    <Payload Id='cabRT8kdm93olnEAQB2GSO3u0400VI' FilePath='cab1.cab' FileSize='971' Hash='1D20203378E2AEC4AD728F7849A5CC7F6E7D094D' Packaging='embedded' SourcePath='a5' Container='WixAttachedContainer' />"
    L"    <RollbackBoundary Id='WixDefaultBoundary' Vital='yes' Transaction='no' />"
    L"    <RollbackBoundary Id='rbaOCA08D8ky7uBOK71_6FWz1K3TuQ' Vital='yes' Transaction='yes' />"
    L"    <Registration Id='{c096190a-8bf3-4342-a1d2-94ea9cb853d6}' ExecutableName='BundleA.exe' PerMachine='yes' Tag='' Version='1.0.0.0' ProviderKey='{c096190a-8bf3-4342-a1d2-94ea9cb853d6}'>"
    L"        <Arp Register='yes' DisplayName='~manual - Bundle A' DisplayVersion='1.0.0.0' />"
    L"    </Registration>"
    L"    <Chain>"
    L"        <MsiPackage Id='PackageA' Cache='yes' CacheId='{196E43EA-EF92-4FF8-B9AC-A0FD0D225BB4}v1.0.0.0' InstallSize='1634' Size='33741' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_PackageA' RollbackLogPathVariable='WixBundleRollbackLog_PackageA' ProductCode='{196E43EA-EF92-4FF8-B9AC-A0FD0D225BB4}' Language='1033' Version='1.0.0.0' UpgradeCode='{5B6AB1CF-5DD5-4BB1-851A-9C7E789BCDC7}'>"
    L"            <MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />"
    L"            <MsiProperty Id='MSIFASTINSTALL' Value='7' />"
    L"            <Provides Key='{196E43EA-EF92-4FF8-B9AC-A0FD0D225BB4}' DisplayName='~manual - A' />"
    L"            <RelatedPackage Id='{5B6AB1CF-5DD5-4BB1-851A-9C7E789BCDC7}' MaxVersion='1.0.0.0' MaxInclusive='no' OnlyDetect='no' LangInclusive='yes'>"
    L"                <Language Id='1033' />"
    L"            </RelatedPackage>"
    L"            <RelatedPackage Id='{5B6AB1CF-5DD5-4BB1-851A-9C7E789BCDC7}' MinVersion='1.0.0.0' MinInclusive='no' OnlyDetect='yes' LangInclusive='yes'>"
    L"                <Language Id='1033' />"
    L"            </RelatedPackage>"
    L"            <PayloadRef Id='PackageA' />"
    L"            <PayloadRef Id='cab1QmlL013Hqv_44W64R0cvnHn_2c' />"
    L"        </MsiPackage>"
    L"        <MsiPackage Id='PackageB' Cache='yes' CacheId='{388E4963-13AD-4EE7-B907-AA8888F50E54}v1.0.0.0' InstallSize='1665' Size='33753' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='rbaOCA08D8ky7uBOK71_6FWz1K3TuQ' LogPathVariable='WixBundleLog_PackageB' RollbackLogPathVariable='WixBundleRollbackLog_PackageB' ProductCode='{388E4963-13AD-4EE7-B907-AA8888F50E54}' Language='1033' Version='1.0.0.0' UpgradeCode='{04ABCDBB-2C66-4338-9B1D-DE2AC9B0D1C2}'>"
    L"            <MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />"
    L"            <MsiProperty Id='MSIFASTINSTALL' Value='7' />"
    L"            <Provides Key='{388E4963-13AD-4EE7-B907-AA8888F50E54}' DisplayName='~manual - B' />"
    L"            <RelatedPackage Id='{04ABCDBB-2C66-4338-9B1D-DE2AC9B0D1C2}' MaxVersion='1.0.0.0' MaxInclusive='yes' OnlyDetect='no' LangInclusive='yes'>"
    L"                <Language Id='1033' />"
    L"            </RelatedPackage>"
    L"            <RelatedPackage Id='{04ABCDBB-2C66-4338-9B1D-DE2AC9B0D1C2}' MinVersion='1.0.0.0' MinInclusive='no' OnlyDetect='yes' LangInclusive='yes'>"
    L"                <Language Id='1033' />"
    L"            </RelatedPackage>"
    L"            <PayloadRef Id='PackageB' />"
    L"            <PayloadRef Id='cabQH1Sgh7w2K8tLIftUaaWVhMWt0s' />"
    L"        </MsiPackage>"
    L"        <MsiPackage Id='PackageC' Cache='yes' CacheId='{BE27CF2B-9E5F-4500-BAE3-5E0E522FB962}v1.0.0.0' InstallSize='1634' Size='33739' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryBackward='rbaOCA08D8ky7uBOK71_6FWz1K3TuQ' LogPathVariable='WixBundleLog_PackageC' RollbackLogPathVariable='WixBundleRollbackLog_PackageC' ProductCode='{BE27CF2B-9E5F-4500-BAE3-5E0E522FB962}' Language='1033' Version='1.0.0.0' UpgradeCode='{3F8C1522-741D-499E-9137-7E192405E01A}'>"
    L"            <MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />"
    L"            <MsiProperty Id='MSIFASTINSTALL' Value='7' />"
    L"            <Provides Key='{BE27CF2B-9E5F-4500-BAE3-5E0E522FB962}' DisplayName='~manual - C' />"
    L"            <RelatedPackage Id='{3F8C1522-741D-499E-9137-7E192405E01A}' MaxVersion='1.0.0.0' MaxInclusive='no' OnlyDetect='no' LangInclusive='yes'>"
    L"                <Language Id='1033' />"
    L"            </RelatedPackage>"
    L"            <RelatedPackage Id='{3F8C1522-741D-499E-9137-7E192405E01A}' MinVersion='1.0.0.0' MinInclusive='no' OnlyDetect='yes' LangInclusive='yes'>"
    L"                <Language Id='1033' />"
    L"            </RelatedPackage>"
    L"            <PayloadRef Id='PackageC' />"
    L"            <PayloadRef Id='cabRT8kdm93olnEAQB2GSO3u0400VI' />"
    L"        </MsiPackage>"
    L"    </Chain>"
    L"</Bundle>";

static LPCWSTR wzSingleMsiManifest =
    L"<Bundle>"
    L"    <Log PathVariable='WixBundleLog' Prefix='~manual_BundleB' Extension='log' />"
    L"    <RelatedBundle Id='{CAAD4202-2097-4065-82BB-83F9F3FF61CE}' Action='Upgrade' />"
    L"    <Variable Id='TestName' Value='SingleMsi' Type='string' Hidden='no' Persisted='no' />"
    L"    <Variable Id='WixBundleName' Hidden='no' Persisted='yes' />"
    L"    <Variable Id='WixBundleOriginalSource' Hidden='no' Persisted='yes' />"
    L"    <Variable Id='WixBundleOriginalSourceFolder' Hidden='no' Persisted='yes' />"
    L"    <Variable Id='WixBundleLastUsedSource' Hidden='no' Persisted='yes' />"
    L"    <UX>"
    L"        <Payload Id='WixStandardBootstrapperApplication.RtfLicense' FilePath='wixstdba.dll' FileSize='1312768' Hash='B331BC5C819F019981D2524C48CC370DE2BFE9F8' Packaging='embedded' SourcePath='u0' />"
    L"        <Payload Id='payF3wjO.aj6M8ftq9wrfYB1N4aw4g' FilePath='thm.xml' FileSize='8048' Hash='A712A9E1427A4EE1D0B68343A54019A3FC9967CB' Packaging='embedded' SourcePath='u1' />"
    L"        <Payload Id='payViPRdlZD7MPIBgwmX2Hv5C6oKt4' FilePath='thm.wxl' FileSize='3926' Hash='B88FAB9B6C2B52FBE017F91B0915781C6B76058E' Packaging='embedded' SourcePath='u2' />"
    L"        <Payload Id='payjqSD44latbvJnf4vAQuVMUST73A' FilePath='logo.png' FileSize='852' Hash='239F10674BF6022854C1F1BF7C91955BDE34D3E4' Packaging='embedded' SourcePath='u3' />"
    L"        <Payload Id='pay60yHh1x6HODo4M_38Pud7jhl2Ig' FilePath='license.rtf' FileSize='4908' Hash='383034848F8CC4F3C8E795CC0F4D716A285E9465' Packaging='embedded' SourcePath='u4' />"
    L"        <Payload Id='uxTxMXPVMXwQrPTMIGa5WGt93w0Ns' FilePath='BootstrapperApplicationData.xml' FileSize='3854' Hash='2807D2AB42585125D18B7DCE49DB6454A1AFC367' Packaging='embedded' SourcePath='u5' />"
    L"    </UX>"
    L"    <Container Id='WixAttachedContainer' FileSize='6486' Hash='944E8702BD8DCDB1E41C47033115B690CED42033' FilePath='BundleC.exe' AttachedIndex='1' Attached='yes' Primary='yes' />"
    L"    <Payload Id='PackageE' FilePath='~manual_PackageE.msi' FileSize='32768' Hash='EB5B931CFCD724391A014A93A9B41037AEE57EC5' Packaging='embedded' SourcePath='a0' Container='WixAttachedContainer' />"
    L"    <Payload Id='cabkAPka1fWa1PyiVdoVPuoB6Qvs3k' FilePath='cab1.cab' FileSize='973' Hash='A0D42DE329CFCF0AF60D5FFA902C7E53DD5F3B4F' Packaging='embedded' SourcePath='a1' Container='WixAttachedContainer' />"
    L"    <RollbackBoundary Id='WixDefaultBoundary' Vital='yes' Transaction='no' />"
    L"    <Registration Id='{4a04385a-0081-44ba-acd1-9e4e95cfc97f}' ExecutableName='BundleC.exe' PerMachine='yes' Tag='' Version='1.0.0.0' ProviderKey='{4a04385a-0081-44ba-acd1-9e4e95cfc97f}'>"
    L"        <Arp Register='yes' DisplayName='~manual - Bundle B' DisplayVersion='1.0.0.0' />"
    L"    </Registration>"
    L"    <Chain>"
    L"        <MsiPackage Id='PackageE' Cache='yes' CacheId='{284F56B6-B6C7-404A-B9B5-78F63BF79494}v1.0.0.0' InstallSize='1640' Size='33741' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_PackageE' RollbackLogPathVariable='WixBundleRollbackLog_PackageE' ProductCode='{284F56B6-B6C7-404A-B9B5-78F63BF79494}' Language='1033' Version='1.0.0.0' UpgradeCode='{04ABCDBB-2C66-4338-9B1D-DE2AC9B0D1C2}'>"
    L"            <MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />"
    L"            <MsiProperty Id='MSIFASTINSTALL' Value='7' />"
    L"            <Provides Key='{284F56B6-B6C7-404A-B9B5-78F63BF79494}' DisplayName='~manual - E' />"
    L"            <RelatedPackage Id='{04ABCDBB-2C66-4338-9B1D-DE2AC9B0D1C2}' MaxVersion='1.0.0.0' MaxInclusive='no' OnlyDetect='no' LangInclusive='yes'>"
    L"                <Language Id='1033' />"
    L"            </RelatedPackage>"
    L"            <RelatedPackage Id='{04ABCDBB-2C66-4338-9B1D-DE2AC9B0D1C2}' MinVersion='1.0.0.0' MinInclusive='no' OnlyDetect='yes' LangInclusive='yes'>"
    L"                <Language Id='1033' />"
    L"            </RelatedPackage>"
    L"            <PayloadRef Id='PackageE' />"
    L"            <PayloadRef Id='cabkAPka1fWa1PyiVdoVPuoB6Qvs3k' />"
    L"        </MsiPackage>"
    L"    </Chain>"
    L"</Bundle>";

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
    using namespace Xunit;

    public ref class PlanTest : BurnUnitTest
    {
    public:
        PlanTest(BurnTestFixture^ fixture) : BurnUnitTest(fixture)
        {
        }

        [Fact]
        void MsiTransactionInstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzMsiTransactionManifest, pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectUpgradeBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"1.0.0.0");

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fKeepRegistrationDefault);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            DWORD dwPackageStart = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            dwPackageStart = ValidateCachePackageStart(pPlan, fRollback, dwIndex++, L"PackageA", 6, 2, 33741, FALSE);
            ValidateCacheAcquireContainer(pPlan, fRollback, dwIndex++, L"WixAttachedContainer", FALSE);
            ValidateCacheExtractContainer(pPlan, fRollback, dwIndex++, L"WixAttachedContainer", FALSE, dwPackageStart, 6);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageA", L"PackageA", TRUE, FALSE, dwPackageStart);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageA", L"cab1QmlL013Hqv_44W64R0cvnHn_2c", TRUE, FALSE, dwPackageStart);
            ValidateCachePackageStop(pPlan, fRollback, dwIndex++, L"PackageA", FALSE);
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++, FALSE);
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 9);
            dwPackageStart = ValidateCachePackageStart(pPlan, fRollback, dwIndex++, L"PackageB", 14, 2, 33753, FALSE);
            ValidateCacheAcquireContainer(pPlan, fRollback, dwIndex++, L"WixAttachedContainer", TRUE);
            ValidateCacheExtractContainer(pPlan, fRollback, dwIndex++, L"WixAttachedContainer", FALSE, dwPackageStart, 2);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageB", L"PackageB", TRUE, FALSE, dwPackageStart);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageB", L"cabQH1Sgh7w2K8tLIftUaaWVhMWt0s", TRUE, FALSE, dwPackageStart);
            ValidateCachePackageStop(pPlan, fRollback, dwIndex++, L"PackageB", FALSE);
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++, FALSE);
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 14);
            dwPackageStart = ValidateCachePackageStart(pPlan, fRollback, dwIndex++, L"PackageC", 22, 2, 33739, FALSE);
            ValidateCacheAcquireContainer(pPlan, fRollback, dwIndex++, L"WixAttachedContainer", TRUE);
            ValidateCacheExtractContainer(pPlan, fRollback, dwIndex++, L"WixAttachedContainer", FALSE, dwPackageStart, 2);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageC", L"PackageC", TRUE, FALSE, dwPackageStart);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageC", L"cabRT8kdm93olnEAQB2GSO3u0400VI", TRUE, FALSE, dwPackageStart);
            ValidateCachePackageStop(pPlan, fRollback, dwIndex++, L"PackageC", FALSE);
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++, FALSE);
            Assert::Equal(24ul, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageA", FALSE);
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(106166ull, pPlan->qwEstimatedSize);
            Assert::Equal(101233ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteWaitSyncpoint(pPlan, fRollback, dwIndex++, pPlan->rgCacheActions[7].syncpoint.hEvent);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRegistration(pPlan, fRollback, dwIndex++, TRUE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ", TRUE, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteBeginMsiTransaction(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ");
            ValidateExecuteWaitSyncpoint(pPlan, fRollback, dwIndex++, pPlan->rgCacheActions[15].syncpoint.hEvent);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageB", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageB", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitSyncpoint(pPlan, fRollback, dwIndex++, pPlan->rgCacheActions[23].syncpoint.hEvent);
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageC", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageC", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageC", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCommitMsiTransaction(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitSyncpoint(pPlan, fRollback, dwIndex++, pPlan->rgCacheActions[23].syncpoint.hEvent);
            ValidateExecuteExePackage(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteRegistration(pPlan, fRollback, dwIndex++, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageA");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ", TRUE, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageB");
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageB", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageC");
            dwExecuteCheckpointId += 1; // cache checkpoints
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageC", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageC", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteExePackage(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(4ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(7ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);
        }

        [Fact]
        void MsiTransactionUninstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzMsiTransactionManifest, pEngineState);
            DetectPackagesAsPresentAndCached(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(TRUE, pPlan->fKeepRegistrationDefault);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ", TRUE, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteBeginMsiTransaction(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageC", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageC", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageC", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageB", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageB", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCommitMsiTransaction(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteRegistration(pPlan, fRollback, dwIndex++, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"rbaOCA08D8ky7uBOK71_6FWz1K3TuQ", TRUE, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageC", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageC", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageB", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageB", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageA", L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageA", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageA", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRegistration(pPlan, fRollback, dwIndex++, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(3ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(3ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PackageC");
            ValidateCleanAction(pPlan, dwIndex++, L"PackageB");
            ValidateCleanAction(pPlan, dwIndex++, L"PackageA");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{c096190a-8bf3-4342-a1d2-94ea9cb853d6}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{BE27CF2B-9E5F-4500-BAE3-5E0E522FB962}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{388E4963-13AD-4EE7-B907-AA8888F50E54}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{196E43EA-EF92-4FF8-B9AC-A0FD0D225BB4}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);
        }

        [Fact]
        void SingleMsiCacheTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifest, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectUpgradeBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"0.9.0.0");

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_CACHE);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_CACHE, pPlan->action);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fKeepRegistrationDefault);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            DWORD dwPackageStart = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            dwPackageStart = ValidateCachePackageStart(pPlan, fRollback, dwIndex++, L"PackageE", 5, 2, 33741, FALSE);
            ValidateCacheExtractContainer(pPlan, fRollback, dwIndex++, L"WixAttachedContainer", FALSE, BURN_PLAN_INVALID_ACTION_INDEX, 2);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageE", L"PackageE", TRUE, FALSE, dwPackageStart);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageE", L"cabkAPka1fWa1PyiVdoVPuoB6Qvs3k", TRUE, FALSE, dwPackageStart);
            ValidateCachePackageStop(pPlan, fRollback, dwIndex++, L"PackageE", FALSE);
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++, FALSE);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(33741ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteWaitSyncpoint(pPlan, fRollback, dwIndex++, pPlan->rgCacheActions[6].syncpoint.hEvent);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageE");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageE", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(0ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(1ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{4a04385a-0081-44ba-acd1-9e4e95cfc97f}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);
        }

        [Fact]
        void SingleMsiInstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifest, pEngineState);
            DetectAttachedContainerAsAttached(pEngineState);
            DetectPackagesAsAbsent(pEngineState);
            DetectUpgradeBundle(pEngineState, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", L"0.9.0.0");

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_INSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_INSTALL, pPlan->action);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(FALSE, pPlan->fKeepRegistrationDefault);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            DWORD dwPackageStart = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            dwPackageStart = ValidateCachePackageStart(pPlan, fRollback, dwIndex++, L"PackageE", 5, 2, 33741, FALSE);
            ValidateCacheExtractContainer(pPlan, fRollback, dwIndex++, L"WixAttachedContainer", FALSE, BURN_PLAN_INVALID_ACTION_INDEX, 2);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageE", L"PackageE", TRUE, FALSE, dwPackageStart);
            ValidateCacheCachePayload(pPlan, fRollback, dwIndex++, L"PackageE", L"cabkAPka1fWa1PyiVdoVPuoB6Qvs3k", TRUE, FALSE, dwPackageStart);
            ValidateCachePackageStop(pPlan, fRollback, dwIndex++, L"PackageE", FALSE);
            ValidateCacheSignalSyncpoint(pPlan, fRollback, dwIndex++, FALSE);
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            ValidateCacheCheckpoint(pPlan, fRollback, dwIndex++, 1);
            ValidateCacheRollbackPackage(pPlan, fRollback, dwIndex++, L"PackageE", FALSE);
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(35381ull, pPlan->qwEstimatedSize);
            Assert::Equal(33741ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteWaitSyncpoint(pPlan, fRollback, dwIndex++, pPlan->rgCacheActions[6].syncpoint.hEvent);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageE", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageE", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRegistration(pPlan, fRollback, dwIndex++, TRUE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageE", L"{4a04385a-0081-44ba-acd1-9e4e95cfc97f}", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteWaitSyncpoint(pPlan, fRollback, dwIndex++, pPlan->rgCacheActions[6].syncpoint.hEvent);
            ValidateExecuteExePackage(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 2;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteRegistration(pPlan, fRollback, dwIndex++, FALSE);
            ValidateExecuteUncachePackage(pPlan, fRollback, dwIndex++, L"PackageE");
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageE", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageE", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageE", L"{4a04385a-0081-44ba-acd1-9e4e95cfc97f}", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteExePackage(pPlan, fRollback, dwIndex++, L"{FD9920AD-DBCA-4C6C-8CD5-B47431CE8D21}", BOOTSTRAPPER_ACTION_STATE_INSTALL, NULL);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(2ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(3ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{4a04385a-0081-44ba-acd1-9e4e95cfc97f}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);
        }

        [Fact]
        void SingleMsiUninstallTest()
        {
            HRESULT hr = S_OK;
            BURN_ENGINE_STATE engineState = { };
            BURN_ENGINE_STATE* pEngineState = &engineState;
            BURN_PLAN* pPlan = &engineState.plan;

            InitializeEngineStateForCorePlan(wzSingleMsiManifest, pEngineState);
            DetectPackagesAsPresentAndCached(pEngineState);

            hr = CorePlan(pEngineState, BOOTSTRAPPER_ACTION_UNINSTALL);
            NativeAssert::Succeeded(hr, "CorePlan failed");

            Assert::Equal<DWORD>(BOOTSTRAPPER_ACTION_UNINSTALL, pPlan->action);
            Assert::Equal<BOOL>(TRUE, pPlan->fPerMachine);
            Assert::Equal<BOOL>(FALSE, pPlan->fDisableRollback);
            Assert::Equal<BOOL>(TRUE, pPlan->fKeepRegistrationDefault);

            BOOL fRollback = FALSE;
            DWORD dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cCacheActions);

            fRollback = TRUE;
            dwIndex = 0;
            Assert::Equal(dwIndex, pPlan->cRollbackCacheActions);

            Assert::Equal(0ull, pPlan->qwEstimatedSize);
            Assert::Equal(0ull, pPlan->qwCacheSizeTotal);

            fRollback = FALSE;
            dwIndex = 0;
            DWORD dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageE", L"{4a04385a-0081-44ba-acd1-9e4e95cfc97f}", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageE", BURN_DEPENDENCY_ACTION_UNREGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageE", BOOTSTRAPPER_ACTION_STATE_UNINSTALL, BURN_MSI_PROPERTY_UNINSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteRegistration(pPlan, fRollback, dwIndex++, FALSE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            Assert::Equal(dwIndex, pPlan->cExecuteActions);

            fRollback = TRUE;
            dwIndex = 0;
            dwExecuteCheckpointId = 1;
            ValidateExecuteRollbackBoundary(pPlan, fRollback, dwIndex++, L"WixDefaultBoundary", TRUE, FALSE);
            ValidateExecutePackageDependency(pPlan, fRollback, dwIndex++, L"PackageE", L"{4a04385a-0081-44ba-acd1-9e4e95cfc97f}", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecutePackageProvider(pPlan, fRollback, dwIndex++, L"PackageE", BURN_DEPENDENCY_ACTION_REGISTER);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteMsiPackage(pPlan, fRollback, dwIndex++, L"PackageE", BOOTSTRAPPER_ACTION_STATE_INSTALL, BURN_MSI_PROPERTY_INSTALL, INSTALLUILEVEL_NONE, FALSE, 0);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteRegistration(pPlan, fRollback, dwIndex++, TRUE);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            ValidateExecuteCheckpoint(pPlan, fRollback, dwIndex++, dwExecuteCheckpointId++);
            Assert::Equal(dwIndex, pPlan->cRollbackActions);

            Assert::Equal(1ul, pPlan->cExecutePackagesTotal);
            Assert::Equal(1ul, pPlan->cOverallProgressTicksTotal);

            dwIndex = 0;
            ValidateCleanAction(pPlan, dwIndex++, L"PackageE");
            Assert::Equal(dwIndex, pPlan->cCleanActions);

            UINT uIndex = 0;
            ValidatePlannedProvider(pPlan, uIndex++, L"{4a04385a-0081-44ba-acd1-9e4e95cfc97f}", NULL);
            ValidatePlannedProvider(pPlan, uIndex++, L"{284F56B6-B6C7-404A-B9B5-78F63BF79494}", NULL);
            Assert::Equal(uIndex, pPlan->cPlannedProviders);
        }

    private:
        // This doesn't initialize everything, just enough for CorePlan to work.
        void InitializeEngineStateForCorePlan(LPCWSTR wzManifest, BURN_ENGINE_STATE* pEngineState)
        {
            HRESULT hr = S_OK;

            ::InitializeCriticalSection(&pEngineState->csActive);
            ::InitializeCriticalSection(&pEngineState->userExperience.csEngineActive);

            hr = VariableInitialize(&pEngineState->variables);
            NativeAssert::Succeeded(hr, "Failed to initialize variables.");

            hr = ManifestLoadXml(wzManifest, pEngineState);
            NativeAssert::Succeeded(hr, "Failed to load manifest.");

            pEngineState->userExperience.pfnBAProc = PlanTestBAProc;
        }

        void DetectAttachedContainerAsAttached(BURN_ENGINE_STATE* pEngineState)
        {
            for (DWORD i = 0; i < pEngineState->containers.cContainers; ++i)
            {
                BURN_CONTAINER* pContainer = pEngineState->containers.rgContainers + i;
                if (pContainer->fAttached)
                {
                    pContainer->fActuallyAttached = TRUE;
                }
            }
        }

        void DetectPackagesAsAbsent(BURN_ENGINE_STATE* pEngineState)
        {
            DetectReset(&pEngineState->registration, &pEngineState->packages);
            PlanReset(&pEngineState->plan, &pEngineState->packages);

            for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
            {
                BURN_PACKAGE* pPackage = pEngineState->packages.rgPackages + i;
                pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_ABSENT;
            }
        }

        void DetectPackagesAsPresentAndCached(BURN_ENGINE_STATE* pEngineState)
        {
            DetectReset(&pEngineState->registration, &pEngineState->packages);
            PlanReset(&pEngineState->plan, &pEngineState->packages);

            pEngineState->registration.fInstalled = TRUE;

            for (DWORD i = 0; i < pEngineState->packages.cPackages; ++i)
            {
                BURN_PACKAGE* pPackage = pEngineState->packages.rgPackages + i;
                pPackage->currentState = BOOTSTRAPPER_PACKAGE_STATE_PRESENT;
                pPackage->cache = BURN_CACHE_STATE_COMPLETE;

                for (DWORD j = 0; j < pPackage->cPayloads; ++j)
                {
                    pPackage->rgPayloads[j].fCached = TRUE;
                }
            }
        }

        HRESULT DetectUpgradeBundle(
            __in BURN_ENGINE_STATE* pEngineState,
            __in LPCWSTR wzId,
            __in LPCWSTR wzVersion
            )
        {
            HRESULT hr = S_OK;
            BURN_RELATED_BUNDLES* pRelatedBundles = &pEngineState->registration.relatedBundles;
            BURN_DEPENDENCY_PROVIDER dependencyProvider = { };

            hr = StrAllocString(&dependencyProvider.sczKey, wzId, 0);
            ExitOnFailure(hr, "Failed to copy provider key");

            dependencyProvider.fImported = TRUE;

            hr = StrAllocString(&dependencyProvider.sczVersion, wzVersion, 0);
            ExitOnFailure(hr, "Failed to copy version");

            hr = MemEnsureArraySize(reinterpret_cast<LPVOID*>(&pRelatedBundles->rgRelatedBundles), pRelatedBundles->cRelatedBundles + 1, sizeof(BURN_RELATED_BUNDLE), 5);
            ExitOnFailure(hr, "Failed to ensure there is space for related bundles.");

            BURN_RELATED_BUNDLE* pRelatedBundle = pRelatedBundles->rgRelatedBundles + pRelatedBundles->cRelatedBundles;

            hr = VerParseVersion(wzVersion, 0, FALSE, &pRelatedBundle->pVersion);
            ExitOnFailure(hr, "Failed to parse pseudo bundle version: %ls", wzVersion);

            pRelatedBundle->relationType = BOOTSTRAPPER_RELATION_UPGRADE;

            hr = PseudoBundleInitialize(0, &pRelatedBundle->package, TRUE, wzId, pRelatedBundle->relationType, BOOTSTRAPPER_PACKAGE_STATE_PRESENT, NULL, NULL, NULL, 0, FALSE, L"-quiet", L"-repair -quiet", L"-uninstall -quiet", &dependencyProvider, NULL, 0);
            ExitOnFailure(hr, "Failed to initialize related bundle to represent bundle: %ls", wzId);

            ++pRelatedBundles->cRelatedBundles;

        LExit:
            return hr;
        }

        void ValidateCacheAcquireContainer(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzContainerId,
            __in BOOL fSkipUntilRetried
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_ACQUIRE_CONTAINER, pAction->type);
            NativeAssert::StringEqual(wzContainerId, pAction->extractContainer.pContainer->sczId);
            Assert::Equal<BOOL>(fSkipUntilRetried, pAction->fSkipUntilRetried);
        }

        BURN_CACHE_ACTION* ValidateCacheActionExists(BURN_PLAN* pPlan, BOOL fRollback, DWORD dwIndex)
        {
            Assert::InRange(dwIndex + 1ul, 1ul, (fRollback ? pPlan->cRollbackCacheActions : pPlan->cCacheActions));
            return (fRollback ? pPlan->rgRollbackCacheActions : pPlan->rgCacheActions) + dwIndex;
        }

        void ValidateCacheCachePayload(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in LPCWSTR wzPayloadId,
            __in BOOL fMove,
            __in BOOL fSkipUntilRetried,
            __in DWORD iTryAgainAction
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_CACHE_PAYLOAD, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->cachePayload.pPackage->sczId);
            NativeAssert::StringEqual(wzPayloadId, pAction->cachePayload.pPayload->sczKey);
            Assert::Equal<BOOL>(fMove, pAction->cachePayload.fMove);
            Assert::Equal<BOOL>(fSkipUntilRetried, pAction->fSkipUntilRetried);
            Assert::Equal(iTryAgainAction, pAction->cachePayload.iTryAgainAction);
        }

        void ValidateCacheCheckpoint(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in DWORD dwId
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_CHECKPOINT, pAction->type);
            Assert::Equal(dwId, pAction->checkpoint.dwId);
        }

        void ValidateCacheExtractContainer(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzContainerId,
            __in BOOL fSkipUntilRetried,
            __in DWORD iSkipUntilAcquiredByAction,
            __in DWORD cPayloads
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_EXTRACT_CONTAINER, pAction->type);
            NativeAssert::StringEqual(wzContainerId, pAction->extractContainer.pContainer->sczId);
            Assert::Equal<BOOL>(fSkipUntilRetried, pAction->fSkipUntilRetried);
            Assert::Equal(iSkipUntilAcquiredByAction, pAction->extractContainer.iSkipUntilAcquiredByAction);
            Assert::Equal(cPayloads, pAction->extractContainer.cPayloads);
        }

        DWORD ValidateCachePackageStart(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in DWORD iPackageCompleteAction,
            __in DWORD cCachePayloads,
            __in DWORD64 qwCachePayloadSizeTotal,
            __in BOOL fSkipUntilRetried
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_PACKAGE_START, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->packageStart.pPackage->sczId);
            Assert::Equal(iPackageCompleteAction, pAction->packageStart.iPackageCompleteAction);
            Assert::Equal(cCachePayloads, pAction->packageStart.cCachePayloads);
            Assert::Equal(qwCachePayloadSizeTotal, pAction->packageStart.qwCachePayloadSizeTotal);
            Assert::Equal<BOOL>(fSkipUntilRetried, pAction->fSkipUntilRetried);
            return dwIndex + 1;
        }

        void ValidateCachePackageStop(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BOOL fSkipUntilRetried
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_PACKAGE_STOP, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->packageStop.pPackage->sczId);
            Assert::Equal<BOOL>(fSkipUntilRetried, pAction->fSkipUntilRetried);
        }

        void ValidateCacheRollbackPackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BOOL fSkipUntilRetried
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_ROLLBACK_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->rollbackPackage.pPackage->sczId);
            Assert::Equal<BOOL>(fSkipUntilRetried, pAction->fSkipUntilRetried);
        }

        void ValidateCacheSignalSyncpoint(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in BOOL fSkipUntilRetried
            )
        {
            BURN_CACHE_ACTION* pAction = ValidateCacheActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_CACHE_ACTION_TYPE_SIGNAL_SYNCPOINT, pAction->type);
            Assert::NotEqual((DWORD_PTR)NULL, (DWORD_PTR)pAction->syncpoint.hEvent);
            Assert::Equal<BOOL>(fSkipUntilRetried, pAction->fSkipUntilRetried);
        }

        void ValidateCleanAction(
            __in BURN_PLAN* pPlan,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId
            )
        {
            Assert::InRange(dwIndex + 1ul, 1ul, pPlan->cCleanActions);

            BURN_CLEAN_ACTION* pCleanAction = pPlan->rgCleanActions + dwIndex;
            Assert::NotEqual((DWORD_PTR)0, (DWORD_PTR)pCleanAction->pPackage);
            NativeAssert::StringEqual(wzPackageId, pCleanAction->pPackage->sczId);
        }

        BURN_EXECUTE_ACTION* ValidateExecuteActionExists(BURN_PLAN* pPlan, BOOL fRollback, DWORD dwIndex)
        {
            Assert::InRange(dwIndex + 1ul, 1ul, (fRollback ? pPlan->cRollbackActions : pPlan->cExecuteActions));
            return (fRollback ? pPlan->rgRollbackActions : pPlan->rgExecuteActions) + dwIndex;
        }

        void ValidateExecuteBeginMsiTransaction(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzRollbackBoundaryId
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_BEGIN_MSI_TRANSACTION, pAction->type);
            NativeAssert::StringEqual(wzRollbackBoundaryId, pAction->msiTransaction.pRollbackBoundary->sczId);
        }

        void ValidateExecuteCheckpoint(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in DWORD dwId
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_CHECKPOINT, pAction->type);
            Assert::Equal(dwId, pAction->checkpoint.dwId);
        }

        void ValidateExecuteCommitMsiTransaction(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzRollbackBoundaryId
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_COMMIT_MSI_TRANSACTION, pAction->type);
            NativeAssert::StringEqual(wzRollbackBoundaryId, pAction->msiTransaction.pRollbackBoundary->sczId);
        }

        void ValidateExecuteExePackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BOOTSTRAPPER_ACTION_STATE action,
            __in LPCWSTR wzIgnoreDependencies
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_EXE_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->exePackage.pPackage->sczId);
            Assert::Equal<DWORD>(action, pAction->exePackage.action);
            NativeAssert::StringEqual(wzIgnoreDependencies, pAction->exePackage.sczIgnoreDependencies);
        }

        void ValidateExecuteMsiPackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BOOTSTRAPPER_ACTION_STATE action,
            __in BURN_MSI_PROPERTY actionMsiProperty,
            __in DWORD uiLevel,
            __in BOOL fDisableExternalUiHandler,
            __in DWORD dwLoggingAttributes
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_MSI_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->msiPackage.pPackage->sczId);
            Assert::Equal<DWORD>(action, pAction->msiPackage.action);
            Assert::Equal<DWORD>(actionMsiProperty, pAction->msiPackage.actionMsiProperty);
            Assert::Equal<DWORD>(uiLevel, pAction->msiPackage.uiLevel);
            Assert::Equal<BOOL>(fDisableExternalUiHandler, pAction->msiPackage.fDisableExternalUiHandler);
            NativeAssert::NotNull(pAction->msiPackage.sczLogPath);
            Assert::Equal<DWORD>(dwLoggingAttributes, pAction->msiPackage.dwLoggingAttributes);
        }

        void ValidateExecutePackageDependency(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in LPCWSTR wzBundleProviderKey,
            __in BURN_DEPENDENCY_ACTION action
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_PACKAGE_DEPENDENCY, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->packageDependency.pPackage->sczId);
            NativeAssert::StringEqual(wzBundleProviderKey, pAction->packageDependency.sczBundleProviderKey);
            Assert::Equal<DWORD>(action, pAction->packageDependency.action);
        }

        void ValidateExecutePackageProvider(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId,
            __in BURN_DEPENDENCY_ACTION action
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_PACKAGE_PROVIDER, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->packageProvider.pPackage->sczId);
            Assert::Equal<DWORD>(action, pAction->packageProvider.action);
        }

        void ValidateExecuteRegistration(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in BOOL fKeep
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_REGISTRATION, pAction->type);
            Assert::Equal<BOOL>(fKeep, pAction->registration.fKeep);
        }

        void ValidateExecuteRollbackBoundary(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzId,
            __in BOOL fVital,
            __in BOOL fTransaction
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_ROLLBACK_BOUNDARY, pAction->type);
            NativeAssert::StringEqual(wzId, pAction->rollbackBoundary.pRollbackBoundary->sczId);
            Assert::Equal<BOOL>(fVital, pAction->rollbackBoundary.pRollbackBoundary->fVital);
            Assert::Equal<BOOL>(fTransaction, pAction->rollbackBoundary.pRollbackBoundary->fTransaction);
        }

        void ValidateExecuteUncachePackage(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in LPCWSTR wzPackageId
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_UNCACHE_PACKAGE, pAction->type);
            NativeAssert::StringEqual(wzPackageId, pAction->uncachePackage.pPackage->sczId);
        }

        void ValidateExecuteWaitSyncpoint(
            __in BURN_PLAN* pPlan,
            __in BOOL fRollback,
            __in DWORD dwIndex,
            __in HANDLE hEvent
            )
        {
            BURN_EXECUTE_ACTION* pAction = ValidateExecuteActionExists(pPlan, fRollback, dwIndex);
            Assert::Equal<DWORD>(BURN_EXECUTE_ACTION_TYPE_WAIT_SYNCPOINT, pAction->type);
            Assert::Equal((DWORD_PTR)hEvent, (DWORD_PTR)pAction->syncpoint.hEvent);
        }

        void ValidatePlannedProvider(
            __in BURN_PLAN* pPlan,
            __in UINT uIndex,
            __in LPCWSTR wzKey,
            __in LPCWSTR wzName
            )
        {
            Assert::InRange(uIndex + 1u, 1u, pPlan->cPlannedProviders);

            DEPENDENCY* pProvider = pPlan->rgPlannedProviders + uIndex;
            NativeAssert::StringEqual(wzKey, pProvider->sczKey);
            NativeAssert::StringEqual(wzName, pProvider->sczName);
        }
    };
}
}
}
}
}

static HRESULT WINAPI PlanTestBAProc(
    __in BOOTSTRAPPER_APPLICATION_MESSAGE /*message*/,
    __in const LPVOID /*pvArgs*/,
    __inout LPVOID /*pvResults*/,
    __in_opt LPVOID /*pvContext*/
    )
{
    return S_OK;
}
