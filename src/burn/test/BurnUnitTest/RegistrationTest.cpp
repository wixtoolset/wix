// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

#include "precomp.h"


#define REGISTRY_UNINSTALL_KEY L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
#define REGISTRY_RUN_KEY L"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\RunOnce"
#define TEST_BUNDLE_ID L"{D54F896D-1952-43E6-9C67-B5652240618C}"
#define TEST_BUNDLE_UPGRADE_CODE L"{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}"


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
    using namespace Microsoft::Win32;
    using namespace System;
    using namespace System::IO;
    using namespace Xunit;
    using namespace WixBuildTools::TestSupport;

    public ref class RegistrationTest : BurnUnitTest, IClassFixture<TestRegistryFixture^>
    {
    private:
        TestRegistryFixture^ testRegistry;
        String^ testRunKeyPath;
        String^ testUninstallKeyPath;
        String^ testVariableKeyPath;
    public:
        RegistrationTest(BurnTestFixture^ fixture, TestRegistryFixture^ registryFixture) : BurnUnitTest(fixture)
        {
            this->testRegistry = registryFixture;

            this->testRunKeyPath = this->testRegistry->GetDirectHkcuPath(REG_KEY_DEFAULT, gcnew String(REGISTRY_RUN_KEY));
            this->testUninstallKeyPath = this->testRegistry->GetDirectHkcuPath(REG_KEY_DEFAULT, gcnew String(REGISTRY_UNINSTALL_KEY), gcnew String(TEST_BUNDLE_ID));
            this->testVariableKeyPath = Path::Combine(this->testUninstallKeyPath, gcnew String(L"variables"));
        }

        [Fact]
        void RegisterBasicTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            BURN_PLAN plan = { };
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(TEST_BUNDLE_ID));
            String^ cacheExePath = Path::Combine(cacheDirectory, gcnew String(L"setup.exe"));
            DWORD dwRegistrationOptions = BURN_REGISTRATION_ACTION_OPERATIONS_CACHE_BUNDLE;
            DWORD64 qwEstimatedSize = 1024;

            try
            {
                this->testRegistry->SetUp();

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43E6-9C67-B5652240618C}' UpgradeCode='{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='RegisterBasicTest' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = CacheInitialize(&cache, &internalCommand);
                TestThrowOnFailure(hr, L"Failed initialize cache.");

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, &cache, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                plan.action = BOOTSTRAPPER_ACTION_INSTALL;
                plan.pCommand = &command;
                plan.pInternalCommand = &internalCommand;

                hr = PlanSetResumeCommand(&plan, &registration, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &cache, &variables, dwRegistrationOptions, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                Assert::True(Directory::Exists(cacheDirectory), "Cache directory didn't exist.");
                Assert::True(File::Exists(Path::Combine(cacheDirectory, gcnew String(L"setup.exe"))), "Bundle exe wasn't cached.");

                this->ValidateUninstallKeyResume(Int32(BURN_RESUME_MODE_ACTIVE));
                this->ValidateRunOnceKeyEntry(cacheExePath);

                // end session
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                Assert::False(Directory::Exists(cacheDirectory), "Cache directory wasn't removed.");

                this->ValidateUninstallKeyNull(L"Resume");
                this->ValidateRunOnceKeyString(TEST_BUNDLE_ID, nullptr);
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                this->testRegistry->TearDown();
            }
        }

        [Fact]
        void RegisterArpMinimumTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            BURN_PLAN plan = { };
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(TEST_BUNDLE_ID));
            String^ cacheExePath = Path::Combine(cacheDirectory, gcnew String(L"setup.exe"));
            DWORD dwRegistrationOptions = 0;
            DWORD64 qwEstimatedSize = 1024;
            try
            {
                this->testRegistry->SetUp();

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43E6-9C67-B5652240618C}' UpgradeCode='{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='Product1' InProgressDisplayName='Product1 Installation' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = CacheInitialize(&cache, &internalCommand);
                TestThrowOnFailure(hr, L"Failed initialize cache.");

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, &cache, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                plan.action = BOOTSTRAPPER_ACTION_INSTALL;
                plan.pCommand = &command;
                plan.pInternalCommand = &internalCommand;

                hr = PlanSetResumeCommand(&plan, &registration, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                //
                // install
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &cache, &variables, dwRegistrationOptions, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                this->ValidateUninstallKeyDisplayName(L"Product1 Installation");
                this->ValidateUninstallKeyResume(Int32(BURN_RESUME_MODE_ACTIVE));
                this->ValidateRunOnceKeyEntry(cacheExePath);

                // complete registration
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_ARP, BOOTSTRAPPER_APPLY_RESTART_NONE, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was updated
                this->ValidateUninstallKeyResume(Int32(BURN_RESUME_MODE_ARP));
                this->ValidateUninstallKeyInstalled(0);
                this->ValidateRunOnceKeyString(TEST_BUNDLE_ID, nullptr);

                //
                // uninstall
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &cache, &variables, dwRegistrationOptions, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was updated
                this->ValidateUninstallKeyResume(Int32(BURN_RESUME_MODE_ACTIVE));
                this->ValidateUninstallKeyInstalled(0);
                this->ValidateRunOnceKeyEntry(cacheExePath);

                // delete registration
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                this->ValidateUninstallKeyNull(L"Resume");
                this->ValidateUninstallKeyNull(L"Installed");
                this->ValidateRunOnceKeyString(TEST_BUNDLE_ID, nullptr);
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                this->testRegistry->TearDown();
            }
        }

        [Fact]
        void RegisterVariablesTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            BURN_PLAN plan = { };
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(TEST_BUNDLE_ID));
            String^ cacheExePath = Path::Combine(cacheDirectory, gcnew String(L"setup.exe"));
            DWORD dwRegistrationOptions = 0;
            DWORD64 qwEstimatedSize = 1024;
            try
            {
                this->testRegistry->SetUp();

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43E6-9C67-B5652240618C}' UpgradeCode='{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}' Tag='foo' ProviderKey='bar' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='Product1' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = CacheInitialize(&cache, &internalCommand);
                TestThrowOnFailure(hr, L"Failed initialize cache.");

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, &cache, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                plan.action = BOOTSTRAPPER_ACTION_INSTALL;
                plan.pCommand = &command;
                plan.pInternalCommand = &internalCommand;

                hr = RegistrationSetVariables(&registration, &variables);
                TestThrowOnFailure(hr, L"Failed to set registration variables.");

                hr = PlanSetResumeCommand(&plan, &registration, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                //
                // install
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &cache, &variables, dwRegistrationOptions, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                this->ValidateUninstallKeyResume(Int32(BURN_RESUME_MODE_ACTIVE));
                this->ValidateRunOnceKeyEntry(cacheExePath);

                // complete registration
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_ARP, BOOTSTRAPPER_APPLY_RESTART_REQUIRED, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_FULL);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration variables were updated
                this->ValidateUninstallKeyDisplayName(L"Product1");
                registration.detectedRegistrationType = BOOTSTRAPPER_REGISTRATION_TYPE_FULL;

                hr = RegistrationSetDynamicVariables(&registration, &variables);
                TestThrowOnFailure(hr, L"Failed to set dynamic registration variables.");

                Assert::Equal(1ll, VariableGetNumericHelper(&variables, BURN_BUNDLE_INSTALLED));
                Assert::Equal<String^>(gcnew String(L"foo"), VariableGetStringHelper(&variables, BURN_BUNDLE_TAG));
                Assert::Equal<String^>(gcnew String(L"bar"), VariableGetStringHelper(&variables, BURN_BUNDLE_PROVIDER_KEY));
                Assert::Equal<String^>(gcnew String(L"1.0.0.0"), VariableGetVersionHelper(&variables, BURN_BUNDLE_VERSION));

                //
                // uninstall
                //

                // delete registration
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                this->ValidateUninstallKeyNull(L"Resume");
                this->ValidateUninstallKeyNull(L"Installed");
                this->ValidateRunOnceKeyString(TEST_BUNDLE_ID, nullptr);
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                this->testRegistry->TearDown();
            }
        }

        [Fact]
        void RegisterArpFullTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            BURN_PLAN plan = { };
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(TEST_BUNDLE_ID));
            String^ cacheExePath = Path::Combine(cacheDirectory, gcnew String(L"setup.exe"));
            DWORD dwRegistrationOptions = 0;
            DWORD64 qwEstimatedSize = 1024;
            try
            {
                this->testRegistry->SetUp();

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX UxDllPayloadId='ux.dll'>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43E6-9C67-B5652240618C}' UpgradeCode='{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' DisplayName='DisplayName1' DisplayVersion='1.2.3.4' Publisher='Publisher1' HelpLink='http://www.microsoft.com/help'"
                    L"             HelpTelephone='555-555-5555' AboutUrl='http://www.microsoft.com/about' UpdateUrl='http://www.microsoft.com/update'"
                    L"             Comments='Comments1' Contact='Contact1' DisableModify='yes' DisableRemove='yes' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = CacheInitialize(&cache, &internalCommand);
                TestThrowOnFailure(hr, L"Failed initialize cache.");

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, &cache, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                plan.action = BOOTSTRAPPER_ACTION_INSTALL;
                plan.pCommand = &command;
                plan.pInternalCommand = &internalCommand;

                hr = PlanSetResumeCommand(&plan, &registration, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                //
                // install
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &cache, &variables, dwRegistrationOptions, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was created
                this->ValidateUninstallKeyResume(Int32(BURN_RESUME_MODE_ACTIVE));
                this->ValidateRunOnceKeyEntry(cacheExePath);

                // finish registration
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_ARP, BOOTSTRAPPER_APPLY_RESTART_NONE, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_FULL);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was updated
                this->ValidateUninstallKeyResume(Int32(BURN_RESUME_MODE_ARP));
                this->ValidateUninstallKeyInstalled(1);
                this->ValidateRunOnceKeyString(TEST_BUNDLE_ID, nullptr);

                this->ValidateUninstallKeyDisplayName(L"DisplayName1");
                this->ValidateUninstallKeyString(L"DisplayVersion", L"1.2.3.4");
                this->ValidateUninstallKeyString(L"Publisher", L"Publisher1");
                this->ValidateUninstallKeyString(L"HelpLink", L"http://www.microsoft.com/help");
                this->ValidateUninstallKeyString(L"HelpTelephone", L"555-555-5555");
                this->ValidateUninstallKeyString(L"URLInfoAbout", L"http://www.microsoft.com/about");
                this->ValidateUninstallKeyString(L"URLUpdateInfo", L"http://www.microsoft.com/update");
                this->ValidateUninstallKeyString(L"Comments", L"Comments1");
                this->ValidateUninstallKeyString(L"Contact", L"Contact1");
                this->ValidateUninstallKeyNumber(L"NoModify", 1);
                this->ValidateUninstallKeyNumber(L"NoRemove", 1);

                //
                // uninstall
                //

                // write registration
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &cache, &variables, dwRegistrationOptions, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                // verify that registration was updated
                this->ValidateUninstallKeyResume(Int32(BURN_RESUME_MODE_ACTIVE));
                this->ValidateRunOnceKeyEntry(cacheExePath);

                // delete registration
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // verify that registration was removed
                this->ValidateUninstallKeyNull(L"Resume");
                this->ValidateUninstallKeyNull(L"Installed");
                this->ValidateRunOnceKeyString(TEST_BUNDLE_ID, nullptr);
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                this->testRegistry->TearDown();
            }
        }

        [Fact]
        void DUtilButilTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            LPWSTR sczValue = NULL;
            LPWSTR sczRelatedBundleId = NULL;
            DWORD dwRelatedBundleIndex = 0;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            BURN_PLAN plan = { };
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            BYTE* pbBuffer = NULL;
            SIZE_T cbBuffer = 0;
            DWORD dwRegistrationOptions = 0;
            DWORD64 qwEstimatedSize = 1024;
            
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(TEST_BUNDLE_ID));
            try
            {
                this->testRegistry->SetUp();

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <RelatedBundle Id='" TEST_BUNDLE_UPGRADE_CODE L"' Action='Upgrade' />"
                    L"    <Registration Id='" TEST_BUNDLE_ID L"' Tag='foo' ProviderKey='" TEST_BUNDLE_ID L"' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='RegisterBasicTest' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"    <Variable Id='MyBurnVariable1' Type='numeric' Value='0' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable2' Type='string' Value='foo' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable3' Type='version' Value='v1.1-alpha' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable4' Type='string' Value='foo' Hidden='no' Persisted='no' />"
                    L"    <Variable Id='MyBurnVariable5' Type='version' Hidden='no' Persisted='yes' />"
                    L"    <CommandLine Variables='upperCase' />"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = CacheInitialize(&cache, &internalCommand);
                TestThrowOnFailure(hr, L"Failed initialize cache.");

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = VariablesParseFromXml(&variables, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse variables from XML.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, &cache, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                plan.action = BOOTSTRAPPER_ACTION_INSTALL;
                plan.pCommand = &command;
                plan.pInternalCommand = &internalCommand;

                hr = PlanSetResumeCommand(&plan, &registration, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                // begin session
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &cache, &variables, dwRegistrationOptions, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                VariableSetNumericHelper(&variables, L"MyBurnVariable1", 42);
                VariableSetStringHelper(&variables, L"MyBurnVariable2", L"bar", FALSE);
                VariableSetVersionHelper(&variables, L"MyBurnVariable3", L"v1.0-beta");
                VariableSetVersionHelper(&variables, L"MyBurnVariable5", L"vvv");

                hr = VariableSerialize(&variables, TRUE, &pbBuffer, &cbBuffer);
                TestThrowOnFailure(hr, "Failed to serialize variables.");

                if (!Directory::Exists(cacheDirectory))
                {
                    Directory::CreateDirectory(cacheDirectory);
                }

                hr = RegistrationSaveState(&registration, pbBuffer, cbBuffer);
                TestThrowOnFailure(hr, L"Failed to save state.");

                ReleaseNullBuffer(pbBuffer);
                cbBuffer = 0;

                // Verify the variables exist
                this->ValidateVariableKey(L"MyBurnVariable1", gcnew String(L"42"));
                this->ValidateVariableKey(L"MyBurnVariable2", gcnew String(L"bar"));
                this->ValidateVariableKey(L"MyBurnVariable3", gcnew String(L"1.0-beta"));
                this->ValidateVariableKey(L"MyBurnVariable5", gcnew String(L"vvv"));
                this->ValidateVariableKeyEmpty(L"WixBundleForcedRestartPackage");

                hr = StrAlloc(&sczRelatedBundleId, MAX_GUID_CHARS + 1);
                NativeAssert::Succeeded(hr, "Failed to allocate buffer for related bundle id.");

                // Verify we can find ourself via the UpgradeCode
                hr = BundleEnumRelatedBundleFixed(TEST_BUNDLE_UPGRADE_CODE, BUNDLE_INSTALL_CONTEXT_USER, REG_KEY_DEFAULT, &dwRelatedBundleIndex, sczRelatedBundleId);
                TestThrowOnFailure(hr, L"Failed to enumerate related bundle.");

                NativeAssert::StringEqual(TEST_BUNDLE_ID, sczRelatedBundleId);

                // Verify we can read the bundle variables via the API
                hr = BundleGetBundleVariable(TEST_BUNDLE_ID, L"MyBurnVariable1", &sczValue);
                TestThrowOnFailure(hr, L"Failed to read MyBurnVariable1.");

                NativeAssert::StringEqual(L"42", sczValue);

                // end session
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");
            }
            finally
            {
                ReleaseStr(sczRelatedBundleId);
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                this->testRegistry->TearDown();
            }
        }

        [Fact]
        void ResumeTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            LPWSTR sczCurrentProcess = NULL;
            LPWSTR sczValue = NULL;
            BURN_VARIABLES variables = { };
            BURN_USER_EXPERIENCE userExperience = { };
            BOOTSTRAPPER_COMMAND command = { };
            BURN_REGISTRATION registration = { };
            BURN_LOGGING logging = { };
            BURN_PACKAGES packages = { };
            BURN_PLAN plan = { };
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };
            BOOTSTRAPPER_RESUME_TYPE resumeType = BOOTSTRAPPER_RESUME_TYPE_NONE;
            BYTE* pbBuffer = NULL;
            SIZE_T cbBuffer = 0;
            SIZE_T piBuffer = 0;
            String^ cacheDirectory = Path::Combine(Path::Combine(Environment::GetFolderPath(Environment::SpecialFolder::LocalApplicationData), gcnew String(L"Package Cache")), gcnew String(TEST_BUNDLE_ID));
            String^ cacheExePath = Path::Combine(cacheDirectory, gcnew String(L"setup.exe"));
            DWORD dwRegistrationOptions = 0;
            DWORD64 qwEstimatedSize = 1024;
            try
            {
                this->testRegistry->SetUp();

                logging.sczPath = L"BurnUnitTest.txt";

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.dll' FilePath='ux.dll' Packaging='embedded' SourcePath='ux.dll' Hash='000000000000' />"
                    L"    </UX>"
                    L"    <Registration Id='{D54F896D-1952-43E6-9C67-B5652240618C}' UpgradeCode='{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='RegisterBasicTest' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"    <Variable Id='MyBurnVariable1' Type='numeric' Value='0' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable2' Type='string' Value='foo' Hidden='no' Persisted='yes' />"
                    L"    <Variable Id='MyBurnVariable3' Type='version' Value='v1.1-alpha' Hidden='no' Persisted='yes' />"
                    L"    <CommandLine Variables='upperCase' />"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = CacheInitialize(&cache, &internalCommand);
                TestThrowOnFailure(hr, L"Failed initialize cache.");

                hr = VariableInitialize(&variables);
                TestThrowOnFailure(hr, L"Failed to initialize variables.");

                hr = VariablesParseFromXml(&variables, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse variables from XML.");

                hr = UserExperienceParseFromXml(&userExperience, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse UX from XML.");

                hr = RegistrationParseFromXml(&registration, &cache, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                plan.action = BOOTSTRAPPER_ACTION_INSTALL;
                plan.pCommand = &command;
                plan.pInternalCommand = &internalCommand;

                hr = PlanSetResumeCommand(&plan, &registration, &logging);
                TestThrowOnFailure(hr, L"Failed to set registration resume command.");

                hr = PathForCurrentProcess(&sczCurrentProcess, NULL);
                TestThrowOnFailure(hr, L"Failed to get current process path.");

                // read resume type before session
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read resume type.");

                Assert::Equal((int)BOOTSTRAPPER_RESUME_TYPE_NONE, (int)resumeType);

                // begin session
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &cache, &variables, dwRegistrationOptions, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to register bundle.");

                VariableSetNumericHelper(&variables, L"MyBurnVariable1", 42);
                VariableSetStringHelper(&variables, L"MyBurnVariable2", L"bar", FALSE);
                VariableSetVersionHelper(&variables, L"MyBurnVariable3", L"v1.0-beta");

                hr = VariableSerialize(&variables, TRUE, &pbBuffer, &cbBuffer);
                TestThrowOnFailure(hr, "Failed to serialize variables.");

                if (!Directory::Exists(cacheDirectory))
                {
                    Directory::CreateDirectory(cacheDirectory);
                }

                hr = RegistrationSaveState(&registration, pbBuffer, cbBuffer);
                TestThrowOnFailure(hr, L"Failed to save state.");

                ReleaseNullBuffer(pbBuffer);
                cbBuffer = 0;

                // Verify the variables exist
                this->ValidateVariableKey(L"MyBurnVariable1", gcnew String(L"42"));
                this->ValidateVariableKey(L"MyBurnVariable2", gcnew String(L"bar"));
                this->ValidateVariableKey(L"MyBurnVariable3", gcnew String(L"1.0-beta"));
                this->ValidateVariableKeyEmpty(L"WixBundleForcedRestartPackage");

                hr = BundleGetBundleVariable(TEST_BUNDLE_ID, L"MyBurnVariable1", &sczValue);
                TestThrowOnFailure(hr, L"Failed to read MyBurnVariable1.");

                NativeAssert::StringEqual(L"42", sczValue);

                // read interrupted resume type
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read interrupted resume type.");

                Assert::Equal((int)BOOTSTRAPPER_RESUME_TYPE_INTERRUPTED, (int)resumeType);

                // suspend session
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_SUSPEND, BOOTSTRAPPER_APPLY_RESTART_NONE, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to suspend session.");

                // verify that run key was removed
                this->ValidateRunOnceKeyString(TEST_BUNDLE_ID, nullptr);

                // read suspend resume type
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read suspend resume type.");

                Assert::Equal((int)BOOTSTRAPPER_RESUME_TYPE_SUSPEND, (int)resumeType);

                // read state back
                hr = RegistrationLoadState(&registration, &pbBuffer, &cbBuffer);
                TestThrowOnFailure(hr, L"Failed to load state.");

                hr = VariableDeserialize(&variables, TRUE, pbBuffer, cbBuffer, &piBuffer);
                TestThrowOnFailure(hr, L"Failed to deserialize variables.");

                // write active resume mode
                hr = RegistrationSessionBegin(sczCurrentProcess, &registration, &cache, &variables, dwRegistrationOptions, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_INPROGRESS);
                TestThrowOnFailure(hr, L"Failed to write active resume mode.");

                // verify that run key was put back
                this->ValidateRunOnceKeyEntry(cacheExePath);

                // end session
                hr = RegistrationSessionEnd(&registration, &cache, &variables, &packages, BURN_RESUME_MODE_NONE, BOOTSTRAPPER_APPLY_RESTART_NONE, qwEstimatedSize, BOOTSTRAPPER_REGISTRATION_TYPE_NONE);
                TestThrowOnFailure(hr, L"Failed to unregister bundle.");

                // read resume type after session
                hr = RegistrationDetectResumeType(&registration, &resumeType);
                TestThrowOnFailure(hr, L"Failed to read resume type.");

                Assert::Equal((int)BOOTSTRAPPER_RESUME_TYPE_NONE, (int)resumeType);
            }
            finally
            {
                ReleaseStr(sczCurrentProcess);
                ReleaseObject(pixeBundle);
                UserExperienceUninitialize(&userExperience);
                RegistrationUninitialize(&registration);
                VariablesUninitialize(&variables);

                if (Directory::Exists(cacheDirectory))
                {
                    Directory::Delete(cacheDirectory, true);
                }

                this->testRegistry->TearDown();
            }
        }

        void ValidateRunOnceKeyString(LPCWSTR valueName, String^ expected)
        {
            WixAssert::StringEqual(expected, (String^)Registry::GetValue(this->testRunKeyPath, gcnew String(valueName), nullptr), false);
        }

        void ValidateRunOnceKeyEntry(String^ exePath)
        {
            this->ValidateRunOnceKeyString(TEST_BUNDLE_ID, String::Concat(L"\"", exePath, L"\" /burn.clean.room /burn.runonce"));
        }

        void ValidateUninstallKeyNull(LPCWSTR valueName)
        {
            Assert::Null(Registry::GetValue(this->testUninstallKeyPath, gcnew String(valueName), nullptr));
        }

        void ValidateUninstallKeyNumber(LPCWSTR valueName, Int32 expected)
        {
            Assert::Equal(expected, (Int32)Registry::GetValue(this->testUninstallKeyPath, gcnew String(valueName), nullptr));
        }

        void ValidateUninstallKeyString(LPCWSTR valueName, String^ expected)
        {
            WixAssert::StringEqual(expected, (String^)Registry::GetValue(this->testUninstallKeyPath, gcnew String(valueName), nullptr), false);
        }

        void ValidateUninstallKeyDisplayName(String^ expected)
        {
            this->ValidateUninstallKeyString(L"DisplayName", expected);
        }

        void ValidateUninstallKeyInstalled(Int32 expected)
        {
            this->ValidateUninstallKeyNumber(L"Installed", expected);
        }

        void ValidateUninstallKeyResume(Int32 expected)
        {
            this->ValidateUninstallKeyNumber(L"Resume", expected);
        }

        void ValidateVariableKey(LPCWSTR valueName, String^ expected)
        {
            WixAssert::StringEqual(expected, (String^)Registry::GetValue(this->testVariableKeyPath, gcnew String(valueName), nullptr), false);
        }

        void ValidateVariableKeyEmpty(LPCWSTR valueName)
        {
            Assert::Empty((System::Collections::IEnumerable^)Registry::GetValue(this->testVariableKeyPath, gcnew String(valueName), nullptr));
        }
    };
}
}
}
}
}
