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
    using namespace Xunit;
    using namespace WixInternal::TestSupport;

    public ref class RelatedBundleTest : BurnUnitTest, IClassFixture<TestRegistryFixture^>
    {
    private:
        TestRegistryFixture^ testRegistry;
    public:
        RelatedBundleTest(BurnTestFixture^ fixture, TestRegistryFixture^ registryFixture) : BurnUnitTest(fixture)
        {
            this->testRegistry = registryFixture;
        }

        [Fact]
        void RelatedBundleDetectPerMachineTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_REGISTRATION registration = { };
            BURN_RELATED_BUNDLES relatedBundles = { };
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };

            try
            {
                this->testRegistry->SetUp();
                this->RegisterFakeBundles();

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.exe' FilePath='ux.exe' Packaging='embedded' SourcePath='ux.exe' />"
                    L"    </UX>"
                    L"    <RelatedBundle Id='{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}' Action='Upgrade' />"
                    L"    <Registration Id='{D54F896D-1952-43E6-9C67-B5652240618C}' Tag='foo' ProviderKey='foo' Version='1.0.0.0' ExecutableName='setup.exe' PerMachine='yes'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='RegisterBasicTest' DisplayVersion='1.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = CacheInitialize(&cache, &internalCommand);
                TestThrowOnFailure(hr, L"Failed initialize cache.");

                hr = RegistrationParseFromXml(&registration, &cache, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                RelatedBundlesInitializeForScope(registration.fPerMachine, &registration, &relatedBundles);

                Assert::Equal(1lu, relatedBundles.cRelatedBundles);

                BURN_RELATED_BUNDLE* pRelatedBundle = relatedBundles.rgRelatedBundles + 0;
                NativeAssert::StringEqual(L"{AD75BE46-B5D7-4208-BC8B-918553C72D83}", pRelatedBundle->package.sczId);
                //{E2355133-384C-4332-9B62-1FA950D707B7} should be missing because it causes an error while processing it. It's important that this doesn't cause initialization to fail.
            }
            finally
            {
                ReleaseObject(pixeBundle);
                RegistrationUninitialize(&registration);

                this->testRegistry->TearDown();
            }
        }

        [Fact]
        void RelatedBundleDetectPerUserTest()
        {
            HRESULT hr = S_OK;
            IXMLDOMElement* pixeBundle = NULL;
            BURN_REGISTRATION registration = { };
            BURN_RELATED_BUNDLES relatedBundles = { };
            BURN_CACHE cache = { };
            BURN_ENGINE_COMMAND internalCommand = { };

            try
            {
                this->testRegistry->SetUp();
                this->RegisterFakeBundles();

                LPCWSTR wzDocument =
                    L"<Bundle>"
                    L"    <UX>"
                    L"        <Payload Id='ux.exe' FilePath='ux.exe' Packaging='embedded' SourcePath='ux.exe' />"
                    L"    </UX>"
                    L"    <RelatedBundle Id='{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}' Action='Upgrade' />"
                    L"    <Registration Id='{3DB49D3D-1FB8-4147-A465-BBE8BFD0DAD0}' Tag='foo' ProviderKey='foo' Version='4.0.0.0' ExecutableName='setup.exe' PerMachine='no'>"
                    L"        <Arp Register='yes' Publisher='WiX Toolset' DisplayName='RegisterBasicTest' DisplayVersion='4.0.0.0' />"
                    L"    </Registration>"
                    L"</Bundle>";

                // load XML document
                LoadBundleXmlHelper(wzDocument, &pixeBundle);

                hr = CacheInitialize(&cache, &internalCommand);
                TestThrowOnFailure(hr, L"Failed initialize cache.");

                hr = RegistrationParseFromXml(&registration, &cache, pixeBundle);
                TestThrowOnFailure(hr, L"Failed to parse registration from XML.");

                RelatedBundlesInitializeForScope(registration.fPerMachine, &registration, &relatedBundles);

                Assert::Equal(1lu, relatedBundles.cRelatedBundles);

                BURN_RELATED_BUNDLE* pRelatedBundle = relatedBundles.rgRelatedBundles + 0;
                NativeAssert::StringEqual(L"{6DB5D48C-CD7D-40D2-BCBC-AF630E136761}", pRelatedBundle->package.sczId);
                //{42D16EBE-8B6B-4A9A-9AE9-5300F30011AA} should be missing because it causes an error while processing it. It's important that this doesn't cause initialization to fail.
            }
            finally
            {
                ReleaseObject(pixeBundle);
                RegistrationUninitialize(&registration);

                this->testRegistry->TearDown();
            }
        }

        void RegisterFakeBundles()
        {
            this->RegisterFakeBundle(L"{D54F896D-1952-43E6-9C67-B5652240618C}", L"{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}", NULL, L"1.0.0.0", TRUE);
            this->RegisterFakeBundle(L"{E2355133-384C-4332-9B62-1FA950D707B7}", L"{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}", L"", L"1.1.0.0", TRUE);
            this->RegisterFakeBundle(L"{AD75BE46-B5D7-4208-BC8B-918553C72D83}", L"{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}", NULL, L"2.0.0.0", TRUE);
            this->RegisterFakeBundle(L"{6DB5D48C-CD7D-40D2-BCBC-AF630E136761}", L"{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}", NULL, L"3.0.0.0", FALSE);
            this->RegisterFakeBundle(L"{42D16EBE-8B6B-4A9A-9AE9-5300F30011AA}", L"{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}", L"", L"3.1.0.0", FALSE);
            this->RegisterFakeBundle(L"{3DB49D3D-1FB8-4147-A465-BBE8BFD0DAD0}", L"{89FDAE1F-8CC1-48B9-B930-3945E0D3E7F0}", NULL, L"4.0.0.0", FALSE);
        }

        void RegisterFakeBundle(LPCWSTR wzBundleId, LPCWSTR wzUpgradeCodes, LPCWSTR wzCachePath, LPCWSTR wzVersion, BOOL fPerMachine)
        {
            HRESULT hr = S_OK;
            LPWSTR* rgsczUpgradeCodes = NULL;
            DWORD cUpgradeCodes = 0;
            LPWSTR sczRegistrationKey = NULL;
            LPWSTR sczCachePath = NULL;
            HKEY hkRegistration = NULL;
            HKEY hkRoot = fPerMachine ? HKEY_LOCAL_MACHINE : HKEY_CURRENT_USER;

            try
            {
                hr = StrSplitAllocArray(&rgsczUpgradeCodes, reinterpret_cast<UINT*>(&cUpgradeCodes), wzUpgradeCodes, L";");
                NativeAssert::Succeeded(hr, "Failed to split upgrade codes.");

                hr = StrAllocFormatted(&sczRegistrationKey, L"%s\\%s", BURN_REGISTRATION_REGISTRY_UNINSTALL_KEY, wzBundleId);
                NativeAssert::Succeeded(hr, "Failed to build uninstall registry key path.");

                if (!wzCachePath)
                {
                    hr = StrAllocFormatted(&sczCachePath, L"%ls.exe", wzBundleId);
                    NativeAssert::Succeeded(hr, "Failed to build cache path.");

                    wzCachePath = sczCachePath;
                }

                hr = RegCreate(hkRoot, sczRegistrationKey, KEY_WRITE, &hkRegistration);
                NativeAssert::Succeeded(hr, "Failed to create registration key.");

                hr = RegWriteStringArray(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE, rgsczUpgradeCodes, cUpgradeCodes);
                NativeAssert::Succeeded(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_UPGRADE_CODE);

                if (wzCachePath && *wzCachePath)
                {
                    hr = RegWriteString(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH, wzCachePath);
                    NativeAssert::Succeeded(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_CACHE_PATH);
                }

                hr = RegWriteString(hkRegistration, BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION, wzVersion);
                NativeAssert::Succeeded(hr, "Failed to write %ls value.", BURN_REGISTRATION_REGISTRY_BUNDLE_VERSION);
            }
            finally
            {
                ReleaseStrArray(rgsczUpgradeCodes, cUpgradeCodes);
                ReleaseStr(sczRegistrationKey);
                ReleaseStr(sczCachePath);
                ReleaseRegKey(hkRegistration);
            }
        }
    };
}
}
}
}
}
