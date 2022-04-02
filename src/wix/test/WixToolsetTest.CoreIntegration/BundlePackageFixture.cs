// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class BundlePackageFixture
    {
        [Fact]
        public void CanBuildBundleWithBundlePackage()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var chainIntermediateFolder = Path.Combine(baseFolder, "obj", "Chain");
                var parentIntermediateFolder = Path.Combine(baseFolder, "obj", "Parent");
                var binFolder = Path.Combine(baseFolder, "bin");
                var chainBundlePath = Path.Combine(binFolder, "chain.exe");
                var chainPdbPath = Path.Combine(binFolder, "chain.wixpdb");
                var parentBundlePath = Path.Combine(binFolder, "parent.exe");
                var parentPdbPath = Path.Combine(binFolder, "parent.wixpdb");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Dependency", "CustomProviderKeyBundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", chainIntermediateFolder,
                    "-o", chainBundlePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(chainBundlePath));

                string chainBundleId;
                using (var wixOutput = WixOutput.Read(chainPdbPath))
                {

                    var intermediate = Intermediate.Load(wixOutput);
                    var section = intermediate.Sections.Single();

                    var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
                    chainBundleId = bundleSymbol.BundleId;
                }

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundlePackage", "BundlePackage.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", parentIntermediateFolder,
                    "-o", parentBundlePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(parentBundlePath));

                string parentBundleId;
                using (var wixOutput = WixOutput.Read(parentPdbPath))
                {

                    var intermediate = Intermediate.Load(wixOutput);
                    var section = intermediate.Sections.Single();

                    var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
                    parentBundleId = bundleSymbol.BundleId;
                }

                var extractResult = BundleExtractor.ExtractBAContainer(null, parentBundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "BundlePackage", new List<string> { "Size" } },
                };
                var bundlePackages = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:BundlePackage")
                                                  .Cast<XmlElement>()
                                                  .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                                  .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    $"<BundlePackage Id='chain.exe' Cache='keep' CacheId='{chainBundleId}v1.0.0.0' InstallSize='34' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_chain.exe' RollbackLogPathVariable='WixBundleRollbackLog_chain.exe' BundleId='{chainBundleId}' Version='1.0.0.0' InstallArguments='' UninstallArguments='' RepairArguments='' SupportsBurnProtocol='yes' Win64='no'>" +
                    "<Provides Key='MyProviderKey,v1.0' Version='1.0.0.0' DisplayName='BurnBundle' Imported='yes' />" +
                    "<RelatedBundle Id='{B94478B1-E1F3-4700-9CE8-6AA090854AEC}' Action='Upgrade' />" +
                    "<PayloadRef Id='chain.exe' />" +
                    "</BundlePackage>",
                }, bundlePackages);

                var registrations = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Registration")
                                                 .Cast<XmlElement>()
                                                 .Select(e => e.GetTestXml())
                                                 .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    $"<Registration Id='{parentBundleId}' ExecutableName='parent.exe' PerMachine='yes' Tag='' Version='1.0.1.0' ProviderKey='{parentBundleId}'>" +
                    "<Arp Register='yes' DisplayName='BundlePackageBundle' DisplayVersion='1.0.1.0' Publisher='Example Corporation' />" +
                    "</Registration>"
                }, registrations);

                ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "WixPackageProperties", new List<string> { "DownloadSize", "PackageSize" } },
                };
                var packageElements = extractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:WixPackageProperties")
                                                   .Cast<XmlElement>()
                                                   .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                                   .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixPackageProperties Package='chain.exe' Vital='yes' DisplayName='BurnBundle' Description='BurnBundle' DownloadSize='*' PackageSize='*' InstalledSize='34' PackageType='Bundle' Permanent='no' LogPathVariable='WixBundleLog_chain.exe' RollbackLogPathVariable='WixBundleRollbackLog_chain.exe' Compressed='yes' Version='1.0.0.0' Cache='keep' />",
                }, packageElements);
            }
        }

        [Fact]
        public void CanBuildBundleWithV3BundlePackage()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var parentIntermediateFolder = Path.Combine(baseFolder, "obj", "Parent");
                var binFolder = Path.Combine(baseFolder, "bin");
                var parentBundlePath = Path.Combine(binFolder, "parent.exe");
                var parentPdbPath = Path.Combine(binFolder, "parent.wixpdb");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");
                string chainBundleId = "{215A70DB-AB35-48C7-BE51-D66EAAC87177}";

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundlePackage", "V3BundlePackage.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", parentIntermediateFolder,
                    "-o", parentBundlePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(parentBundlePath));

                string parentBundleId;
                using (var wixOutput = WixOutput.Read(parentPdbPath))
                {

                    var intermediate = Intermediate.Load(wixOutput);
                    var section = intermediate.Sections.Single();

                    var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
                    parentBundleId = bundleSymbol.BundleId;
                }

                var extractResult = BundleExtractor.ExtractBAContainer(null, parentBundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "BundlePackage", new List<string> { "Size" } },
                };
                var bundlePackages = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:BundlePackage")
                                                  .Cast<XmlElement>()
                                                  .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                                  .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    $"<BundlePackage Id='v3bundle.exe' Cache='keep' CacheId='{chainBundleId}v1.0.0.0' InstallSize='1135' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_v3bundle.exe' RollbackLogPathVariable='WixBundleRollbackLog_v3bundle.exe' BundleId='{chainBundleId}' Version='1.0.0.0' InstallArguments='' UninstallArguments='' RepairArguments='' SupportsBurnProtocol='yes' Win64='no'>" +
                    "<Provides Key='{215a70db-ab35-48c7-be51-d66eaac87177}' Version='1.0.0.0' DisplayName='CustomV3Theme' Imported='yes' />" +
                    "<RelatedBundle Id='{2BF4C01F-C132-4E70-97AB-2BC68C7CCD10}' Action='Upgrade' />" +
                    "<PayloadRef Id='v3bundle.exe' />" +
                    "</BundlePackage>",
                }, bundlePackages);

                var registrations = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Registration")
                                                 .Cast<XmlElement>()
                                                 .Select(e => e.GetTestXml())
                                                 .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    $"<Registration Id='{parentBundleId}' ExecutableName='parent.exe' PerMachine='yes' Tag='' Version='1.1.1.1' ProviderKey='{parentBundleId}'>" +
                    "<Arp Register='yes' DisplayName='V3BundlePackageBundle' DisplayVersion='1.1.1.1' Publisher='Example Corporation' />" +
                    "</Registration>"
                }, registrations);

                ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "WixPackageProperties", new List<string> { "DownloadSize", "PackageSize" } },
                };
                var packageElements = extractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:WixPackageProperties")
                                                   .Cast<XmlElement>()
                                                   .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                                   .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixPackageProperties Package='v3bundle.exe' Vital='yes' DisplayName='CustomV3Theme' Description='CustomV3Theme' DownloadSize='*' PackageSize='*' InstalledSize='1135' PackageType='Bundle' Permanent='no' LogPathVariable='WixBundleLog_v3bundle.exe' RollbackLogPathVariable='WixBundleRollbackLog_v3bundle.exe' Compressed='yes' Version='1.0.0.0' Cache='keep' />",
                }, packageElements);
            }
        }
    }
}
