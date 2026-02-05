// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Example.Extension;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using Xunit;

    public class BundleManifestFixture
    {
        [Fact]
        public void PopulatesBAManifestWithBootstrapperApplicationBundleCustomData()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, "bin", "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleCustomTable", "BundleCustomTable.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var customElements = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:BundleCustomTableBA");
                WixAssert.CompareLineByLine(new[]
                {
                    "<BundleCustomTableBA Id='one' Column2='two' />",
                    "<BundleCustomTableBA Id='&gt;' Column2='&lt;' />",
                    "<BundleCustomTableBA Id='1' Column2='2' />",
                }, customElements);
            }
        }

        [Fact]
        public void PopulatesBAManifestWithPackageInformation()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "CustomPackageDescription", "CustomPackageDescription.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "WixPackageProperties", new List<string> { "DownloadSize", "PackageSize", "InstalledSize", "Version" } },
                };
                var packageElements = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixPackageProperties", ignoreAttributesByElementName);
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixPackageProperties Package='burn.exe' Vital='yes' DisplayName='Windows Installer XML Toolset' Description='WiX Toolset Bootstrapper' DownloadSize='*' PackageSize='*' InstalledSize='*' PackageType='Exe' Permanent='yes' LogPathVariable='WixBundleLog_burn.exe' RollbackLogPathVariable='WixBundleRollbackLog_burn.exe' Compressed='yes' Version='*' RepairCondition='RepairRedists' Cache='keep' />",
                    "<WixPackageProperties Package='RemotePayloadExe' Vital='yes' DisplayName='Override RemotePayload display name' Description='Override RemotePayload description' DownloadSize='*' PackageSize='*' InstalledSize='*' PackageType='Exe' Permanent='yes' LogPathVariable='WixBundleLog_RemotePayloadExe' RollbackLogPathVariable='WixBundleRollbackLog_RemotePayloadExe' Compressed='no' Version='*' Cache='keep' />",
                    "<WixPackageProperties Package='calc.exe' Vital='yes' DisplayName='Override harvested display name' Description='Override harvested description' DownloadSize='*' PackageSize='*' InstalledSize='*' PackageType='Exe' Permanent='yes' LogPathVariable='WixBundleLog_calc.exe' RollbackLogPathVariable='WixBundleRollbackLog_calc.exe' Compressed='yes' Version='*' Cache='keep' />",
                }, packageElements);
            }
        }

        [Fact]
        public void PopulatesBAManifestWithPayloadInformation()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "SharedPayloadsBetweenPackages", "SharedPayloadsBetweenPackages.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "WixPayloadProperties", new List<string> { "Size" } },
                };
                var payloadElements = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixPayloadProperties", ignoreAttributesByElementName).OrderBy(line => line).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixPayloadProperties Package='credwiz.exe' Payload='credwiz.exe' Container='WixAttachedContainer' Name='credwiz.exe' Size='*' />",
                    "<WixPayloadProperties Package='credwiz.exe' Payload='pldbF0sgj0VCScDauGEpgwmywekS84' Container='WixAttachedContainer' Name='SharedPayloadsBetweenPackages.wxs' Size='*' />",
                    "<WixPayloadProperties Package='cscript.exe' Payload='cscript.exe' Container='WixAttachedContainer' Name='cscript.exe' Size='*' />",
                    "<WixPayloadProperties Package='cscript.exe' Payload='pldbF0sgj0VCScDauGEpgwmywekS84' Container='WixAttachedContainer' Name='SharedPayloadsBetweenPackages.wxs' Size='*' />",
                }, payloadElements);
            }
        }

        [Fact]
        public void CanBuildBundleManifestWithNormalizedRelatedBundles()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
{
                    "build",
                    Path.Combine(folder, "BundleLocalized", "BundleWithLocalizedUpgradeCode.wxs"),
                    "-loc", Path.Combine(folder, "BundleLocalized", "BundleWithValidUpgradeCode.wxl"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var manifestRelatedBundlesElements = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:RelatedBundle");
                WixAssert.CompareLineByLine(new[]
                {
                    "<RelatedBundle Code='{6D4CE32B-FB91-45DA-A9B5-7E0D9929A3C3}' Action='Upgrade' />",
                }, manifestRelatedBundlesElements);

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "WixBundleProperties", new List<string> { "Code", "DisplayName" } },
                };
                var dataRelatedBundlesElements = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixBundleProperties", ignoreAttributesByElementName);
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixBundleProperties BundleId='WixToolsetTest.BundleWithInvalidUpgradeCode' Code='*' DisplayName='*' LogPathVariable='WixBundleLog' Compressed='no' UpgradeCode='{6D4CE32B-FB91-45DA-A9B5-7E0D9929A3C3}' Scope='perMachine' />",
                }, dataRelatedBundlesElements);
            }
        }

        [Fact]
        public void PopulatesBEManifestWithBootstrapperExtensionBundleCustomData()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleCustomTable", "BundleCustomTable.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var customElements = extractResult.GetBootstrapperExtensionTestXmlLines("/be:BootstrapperExtensionData/be:BootstrapperExtension[@Id='CustomTableExtension']/be:BundleCustomTableBE");
                WixAssert.CompareLineByLine(new[]
                {
                    "<BundleCustomTableBE Id='one' Column2='two' />",
                    "<BundleCustomTableBE Id='&gt;' Column2='&lt;' />",
                    "<BundleCustomTableBE Id='1' Column2='2' />",
                }, customElements);
            }
        }

        [Fact]
        public void PopulatesManifestWithBootstrapperExtension()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BootstrapperExtension", "BootstrapperExtension.wxs"),
                    Path.Combine(folder, "BootstrapperExtension", "SimpleBootstrapperExtension.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var bootstrapperExtensions = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:BootstrapperExtension");
                WixAssert.CompareLineByLine(new[]
                {
                    "<BootstrapperExtension Id='ExampleBext' EntryPayloadSourcePath='u1' />",
                }, bootstrapperExtensions);

                var bootstrapperExtensionPayloads = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:UX/burn:Payload[@Id='ExampleBext']");
                WixAssert.CompareLineByLine(new[]
                {
                    "<Payload Id='ExampleBext' FilePath='fakebext.dll' SourcePath='u1' />",
                }, bootstrapperExtensionPayloads);
            }
        }

        [Fact]
        public void PopulatesManifestWithBootstrapperExtensionSearches()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BootstrapperExtension", "BootstrapperExtensionSearches.wxs"),
                    Path.Combine(folder, "BootstrapperExtension", "BundleWithSearches.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-ext", ExtensionPaths.ExampleExtensionPath,
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var bootstrapperExtensions = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:BootstrapperExtension");
                WixAssert.CompareLineByLine(new[]
                {
                    "<BootstrapperExtension Id='ExampleBootstrapperExtension' EntryPayloadSourcePath='u1' />",
                }, bootstrapperExtensions);

                var extensionSearches = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:ExtensionSearch");
                WixAssert.CompareLineByLine(new[]
                {
                    "<ExtensionSearch Id='ExampleSearchBar' Variable='SearchBar' Condition='WixBundleInstalled' ExtensionId='ExampleBootstrapperExtension' />",
                    "<ExtensionSearch Id='ExampleSearchFoo' Variable='SearchFoo' ExtensionId='ExampleBootstrapperExtension' />",
                }, extensionSearches);

                var bootstrapperExtensionDatas = extractResult.GetBootstrapperExtensionTestXmlLines("/be:BootstrapperExtensionData/be:BootstrapperExtension[@Id='ExampleBootstrapperExtension']");
                WixAssert.CompareLineByLine(new[]
                {
                    "<BootstrapperExtension Id='ExampleBootstrapperExtension'>" +
                    "<ExampleSearch Id='ExampleSearchBar' SearchFor='Bar' />" +
                    "<ExampleSearch Id='ExampleSearchFoo' SearchFor='Foo' />" +
                    "</BootstrapperExtension>"
                }, bootstrapperExtensionDatas);

                var exampleSearches = extractResult.GetBootstrapperExtensionTestXmlLines("/be:BootstrapperExtensionData/be:BootstrapperExtension[@Id='ExampleBootstrapperExtension']/be:ExampleSearch");
                WixAssert.CompareLineByLine(new[]
                {
                    "<ExampleSearch Id='ExampleSearchBar' SearchFor='Bar' />",
                    "<ExampleSearch Id='ExampleSearchFoo' SearchFor='Foo' />",
                }, exampleSearches);
            }
        }

        [Fact]
        public void PopulatesManifestWithExePackages()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SharedPayloadsBetweenPackages", "SharedPayloadsBetweenPackages.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "ExePackage", new List<string> { "CacheId", "InstallSize", "Size" } },
                };
                var exePackageElements = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Chain/burn:ExePackage", ignoreAttributesByElementName);
                WixAssert.CompareLineByLine(new[]
                {
                    "<ExePackage Id='credwiz.exe' Cache='keep' CacheId='*' InstallSize='*' Size='*' Scope='perMachine' Permanent='yes' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' LogPathVariable='WixBundleLog_credwiz.exe' RollbackLogPathVariable='WixBundleRollbackLog_credwiz.exe' InstallArguments='' RepairArguments='' Repairable='no' DetectionType='condition' DetectCondition='none' UninstallArguments='-foo' Uninstallable='yes' Protocol='burn' Bundle='yes'><PayloadRef Id='credwiz.exe' /><PayloadRef Id='pldbF0sgj0VCScDauGEpgwmywekS84' /></ExePackage>",
                    "<ExePackage Id='cscript.exe' Cache='keep' CacheId='*' InstallSize='*' Size='*' Scope='perMachine' Permanent='yes' Vital='yes' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_cscript.exe' RollbackLogPathVariable='WixBundleRollbackLog_cscript.exe' InstallArguments='' RepairArguments='' Repairable='no' DetectionType='condition' DetectCondition='none' UninstallArguments='' Uninstallable='yes' Protocol='none' Bundle='yes'><PayloadRef Id='cscript.exe' /><PayloadRef Id='pldbF0sgj0VCScDauGEpgwmywekS84' /></ExePackage>",
                }, exePackageElements);
            }
        }

        [Fact]
        public void PopulatesManifestWithSetVariables()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SetVariable", "Simple.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var setVariables = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:SetVariable");
                WixAssert.CompareLineByLine(new[]
                {
                    "<SetVariable Id='SetCoercedNumber' Variable='CoercedNumber' Value='2' Type='numeric' />",
                    "<SetVariable Id='SetCoercedString' Variable='CoercedString' Value='Bar' Type='string' />",
                    "<SetVariable Id='SetCoercedVersion' Variable='CoercedVersion' Value='v2.0' Type='version' />",
                    "<SetVariable Id='SetNeedsFormatting' Variable='NeedsFormatting' Value='[One] [Two] [Three]' Type='string' />",
                    "<SetVariable Id='SetVersionString' Variable='VersionString' Value='v1.0' Type='string' />",
                    "<SetVariable Id='SetUnset' Variable='Unset' Condition='VersionString = v2.0' />",
                }, setVariables);
            }
        }
    }
}
