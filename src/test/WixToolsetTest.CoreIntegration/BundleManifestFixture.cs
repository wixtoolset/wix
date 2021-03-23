// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Example.Extension;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
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

                var customElements = extractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:BundleCustomTableBA");
                Assert.Equal(3, customElements.Count);
                Assert.Equal("<BundleCustomTableBA Id='one' Column2='two' />", customElements[0].GetTestXml());
                Assert.Equal("<BundleCustomTableBA Id='&gt;' Column2='&lt;' />", customElements[1].GetTestXml());
                Assert.Equal("<BundleCustomTableBA Id='1' Column2='2' />", customElements[2].GetTestXml());
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

                var packageElements = extractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:WixPackageProperties");
                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "WixPackageProperties", new List<string> { "DownloadSize", "PackageSize", "InstalledSize", "Version" } },
                };
                Assert.Equal(3, packageElements.Count);
                Assert.Equal("<WixPackageProperties Package='burn.exe' Vital='yes' DisplayName='Windows Installer XML Toolset' Description='WiX Toolset Bootstrapper' DownloadSize='*' PackageSize='*' InstalledSize='*' PackageType='Exe' Permanent='yes' LogPathVariable='WixBundleLog_burn.exe' RollbackLogPathVariable='WixBundleRollbackLog_burn.exe' Compressed='yes' Version='*' Cache='yes' />", packageElements[0].GetTestXml(ignoreAttributesByElementName));
                Assert.Equal("<WixPackageProperties Package='RemotePayloadExe' Vital='yes' DisplayName='Override RemotePayload display name' Description='Override RemotePayload description' DownloadSize='1' PackageSize='1' InstalledSize='1' PackageType='Exe' Permanent='yes' LogPathVariable='WixBundleLog_RemotePayloadExe' RollbackLogPathVariable='WixBundleRollbackLog_RemotePayloadExe' Compressed='no' Version='1.0.0.0' Cache='yes' />", packageElements[1].GetTestXml());
                Assert.Equal("<WixPackageProperties Package='calc.exe' Vital='yes' DisplayName='Override harvested display name' Description='Override harvested description' DownloadSize='*' PackageSize='*' InstalledSize='*' PackageType='Exe' Permanent='yes' LogPathVariable='WixBundleLog_calc.exe' RollbackLogPathVariable='WixBundleRollbackLog_calc.exe' Compressed='yes' Version='*' Cache='yes' />", packageElements[2].GetTestXml(ignoreAttributesByElementName));
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

                var payloadElements = extractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:WixPayloadProperties");
                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "WixPayloadProperties", new List<string> { "Size" } },
                };
                Assert.Equal(4, payloadElements.Count);
                Assert.Equal("<WixPayloadProperties Package='credwiz.exe' Payload='SourceFilePayload' Container='WixAttachedContainer' Name='SharedPayloadsBetweenPackages.wxs' Size='*' LayoutOnly='no' />", payloadElements[0].GetTestXml(ignoreAttributesByElementName));
                Assert.Equal("<WixPayloadProperties Package='credwiz.exe' Payload='credwiz.exe' Container='WixAttachedContainer' Name='credwiz.exe' Size='*' LayoutOnly='no' />", payloadElements[1].GetTestXml(ignoreAttributesByElementName));
                Assert.Equal("<WixPayloadProperties Package='cscript.exe' Payload='SourceFilePayload' Container='WixAttachedContainer' Name='SharedPayloadsBetweenPackages.wxs' Size='*' LayoutOnly='no' />", payloadElements[2].GetTestXml(ignoreAttributesByElementName));
                Assert.Equal("<WixPayloadProperties Package='cscript.exe' Payload='cscript.exe' Container='WixAttachedContainer' Name='cscript.exe' Size='*' LayoutOnly='no' />", payloadElements[3].GetTestXml(ignoreAttributesByElementName));
            }
        }

        [Fact]
        public void PopulatesBEManifestWithBundleExtensionBundleCustomData()
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

                var customElements = extractResult.SelectBundleExtensionDataNodes("/be:BundleExtensionData/be:BundleExtension[@Id='CustomTableExtension']/be:BundleCustomTableBE");
                Assert.Equal(3, customElements.Count);
                Assert.Equal("<BundleCustomTableBE Id='one' Column2='two' />", customElements[0].GetTestXml());
                Assert.Equal("<BundleCustomTableBE Id='&gt;' Column2='&lt;' />", customElements[1].GetTestXml());
                Assert.Equal("<BundleCustomTableBE Id='1' Column2='2' />", customElements[2].GetTestXml());
            }
        }

        [Fact]
        public void PopulatesManifestWithBundleExtension()
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
                    Path.Combine(folder, "BundleExtension", "BundleExtension.wxs"),
                    Path.Combine(folder, "BundleExtension", "SimpleBundleExtension.wxs"),
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

                var bundleExtensions = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:BundleExtension");
                Assert.Equal(1, bundleExtensions.Count);
                Assert.Equal("<BundleExtension Id='ExampleBext' EntryPayloadId='ExampleBext' />", bundleExtensions[0].GetTestXml());

                var bundleExtensionPayloads = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:UX/burn:Payload[@Id='ExampleBext']");
                Assert.Equal(1, bundleExtensionPayloads.Count);
                var ignored = new Dictionary<string, List<string>>
                {
                    { "Payload", new List<string> { "FileSize", "Hash", "SourcePath" } },
                };
                Assert.Equal("<Payload Id='ExampleBext' FilePath='fakebext.dll' FileSize='*' Hash='*' Packaging='embedded' SourcePath='*' />", bundleExtensionPayloads[0].GetTestXml(ignored));
            }
        }

        [Fact]
        public void PopulatesManifestWithBundleExtensionSearches()
        {
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);
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
                    Path.Combine(folder, "BundleExtension", "BundleExtensionSearches.wxs"),
                    Path.Combine(folder, "BundleExtension", "BundleWithSearches.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-ext", extensionPath,
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var bundleExtensions = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:BundleExtension");
                Assert.Equal(1, bundleExtensions.Count);
                Assert.Equal("<BundleExtension Id='ExampleBundleExtension' EntryPayloadId='ExampleBundleExtension' />", bundleExtensions[0].GetTestXml());

                var extensionSearches = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:ExtensionSearch");
                Assert.Equal(2, extensionSearches.Count);
                Assert.Equal("<ExtensionSearch Id='ExampleSearchBar' Variable='SearchBar' Condition='WixBundleInstalled' ExtensionId='ExampleBundleExtension' />", extensionSearches[0].GetTestXml());
                Assert.Equal("<ExtensionSearch Id='ExampleSearchFoo' Variable='SearchFoo' ExtensionId='ExampleBundleExtension' />", extensionSearches[1].GetTestXml());

                var bundleExtensionDatas = extractResult.SelectBundleExtensionDataNodes("/be:BundleExtensionData/be:BundleExtension[@Id='ExampleBundleExtension']");
                Assert.Equal(1, bundleExtensionDatas.Count);
                Assert.Equal("<BundleExtension Id='ExampleBundleExtension'>" +
                    "<ExampleSearch Id='ExampleSearchBar' SearchFor='Bar' />" +
                    "<ExampleSearch Id='ExampleSearchFoo' SearchFor='Foo' />" +
                    "</BundleExtension>", bundleExtensionDatas[0].GetTestXml());

                var exampleSearches = extractResult.SelectBundleExtensionDataNodes("/be:BundleExtensionData/be:BundleExtension[@Id='ExampleBundleExtension']/be:ExampleSearch");
                Assert.Equal(2, exampleSearches.Count);
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

                var exePackageElements = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:ExePackage");
                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "ExePackage", new List<string> { "CacheId", "InstallSize", "Size" } },
                };
                Assert.Equal(2, exePackageElements.Count);
                Assert.Equal("<ExePackage Id='credwiz.exe' Cache='yes' CacheId='*' InstallSize='*' Size='*' PerMachine='yes' Permanent='yes' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' LogPathVariable='WixBundleLog_credwiz.exe' RollbackLogPathVariable='WixBundleRollbackLog_credwiz.exe' DetectCondition='' InstallArguments='' UninstallArguments='' RepairArguments='' Repairable='no'><PayloadRef Id='credwiz.exe' /><PayloadRef Id='SourceFilePayload' /></ExePackage>", exePackageElements[0].GetTestXml(ignoreAttributesByElementName));
                Assert.Equal("<ExePackage Id='cscript.exe' Cache='yes' CacheId='*' InstallSize='*' Size='*' PerMachine='yes' Permanent='yes' Vital='yes' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_cscript.exe' RollbackLogPathVariable='WixBundleRollbackLog_cscript.exe' DetectCondition='' InstallArguments='' UninstallArguments='' RepairArguments='' Repairable='no'><PayloadRef Id='cscript.exe' /><PayloadRef Id='SourceFilePayload' /></ExePackage>", exePackageElements[1].GetTestXml(ignoreAttributesByElementName));
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

                var setVariables = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:SetVariable");
                Assert.Equal(6, setVariables.Count);
                Assert.Equal("<SetVariable Id='SetCoercedNumber' Variable='CoercedNumber' Value='2' Type='numeric' />", setVariables[0].GetTestXml());
                Assert.Equal("<SetVariable Id='SetCoercedString' Variable='CoercedString' Value='Bar' Type='string' />", setVariables[1].GetTestXml());
                Assert.Equal("<SetVariable Id='SetCoercedVersion' Variable='CoercedVersion' Value='v2.0' Type='version' />", setVariables[2].GetTestXml());
                Assert.Equal("<SetVariable Id='SetNeedsFormatting' Variable='NeedsFormatting' Value='[One] [Two] [Three]' Type='string' />", setVariables[3].GetTestXml());
                Assert.Equal("<SetVariable Id='SetVersionString' Variable='VersionString' Value='v1.0' Type='string' />", setVariables[4].GetTestXml());
                Assert.Equal("<SetVariable Id='SetUnset' Variable='Unset' Condition='VersionString = v2.0' />", setVariables[5].GetTestXml());
            }
        }
    }
}
