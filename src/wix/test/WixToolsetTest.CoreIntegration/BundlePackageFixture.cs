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
                var grandparentIntermediateFolder = Path.Combine(baseFolder, "obj", "Grandparent");
                var binFolder = Path.Combine(baseFolder, "bin");
                var chainBundlePath = Path.Combine(binFolder, "chain.exe");
                var chainPdbPath = Path.Combine(binFolder, "chain.wixpdb");
                var parentBundlePath = Path.Combine(binFolder, "parent.exe");
                var parentPdbPath = Path.Combine(binFolder, "parent.wixpdb");
                var grandparentBundlePath = Path.Combine(binFolder, "grandparent.exe");
                var grandparentPdbPath = Path.Combine(binFolder, "grandparent.wixpdb");
                var parentBaFolderPath = Path.Combine(baseFolder, "parentba");
                var grandparentBaFolderPath = Path.Combine(baseFolder, "grandparentba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                // chain.exe
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

                // parent.exe
                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundlePackage", "BundlePackage.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
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

                var extractResult = BundleExtractor.ExtractBAContainer(null, parentBundlePath, parentBaFolderPath, extractFolderPath);
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
                    $"<BundlePackage Id='chain.exe' Cache='keep' CacheId='{chainBundleId}v1.0.0.0' InstallSize='34' Size='*' PerMachine='yes' Permanent='yes' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_chain.exe' RollbackLogPathVariable='WixBundleRollbackLog_chain.exe' BundleId='{chainBundleId}' Version='1.0.0.0' InstallArguments='' UninstallArguments='' RepairArguments='' SupportsBurnProtocol='yes' Win64='no' HideARP='yes'>" +
                    "<Provides Key='MyProviderKey,v1.0' Version='1.0.0.0' DisplayName='BurnBundle' Imported='yes' />" +
                    "<RelatedBundle Id='{B94478B1-E1F3-4700-9CE8-6AA090854AEC}' Action='Upgrade' />" +
                    "<PayloadRef Id='chain.exe' />" +
                    "<PayloadRef Id='payP6wZpeHEAZbDUQPEKeCpQ_9bN.4' />" +
                    "</BundlePackage>",
                }, bundlePackages);

                var registrations = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Registration")
                                                 .Cast<XmlElement>()
                                                 .Select(e => e.GetTestXml())
                                                 .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    $"<Registration Id='{parentBundleId}' ExecutableName='parent.exe' PerMachine='yes' Tag='' Version='1.0.1.0' ProviderKey='{parentBundleId}'>" +
                    "<Arp DisplayName='BundlePackageBundle' DisplayVersion='1.0.1.0' Publisher='Example Corporation' />" +
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
                    "<WixPackageProperties Package='chain.exe' Vital='yes' DisplayName='BurnBundle' Description='BurnBundle' DownloadSize='*' PackageSize='*' InstalledSize='34' PackageType='Bundle' Permanent='yes' LogPathVariable='WixBundleLog_chain.exe' RollbackLogPathVariable='WixBundleRollbackLog_chain.exe' Compressed='no' Version='1.0.0.0' Cache='keep' />",
                }, packageElements);

                // grandparent.exe
                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundlePackage", "PermanentBundlePackage.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", grandparentIntermediateFolder,
                    "-o", grandparentBundlePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(grandparentBundlePath));

                string grandparentBundleId;
                using (var wixOutput = WixOutput.Read(grandparentPdbPath))
                {

                    var intermediate = Intermediate.Load(wixOutput);
                    var section = intermediate.Sections.Single();

                    var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
                    grandparentBundleId = bundleSymbol.BundleId;
                }

                var grandparentExtractResult = BundleExtractor.ExtractBAContainer(null, grandparentBundlePath, grandparentBaFolderPath, extractFolderPath);
                grandparentExtractResult.AssertSuccess();

                ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "BundlePackage", new List<string> { "Size" } },
                };
                bundlePackages = grandparentExtractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:BundlePackage")
                                                         .Cast<XmlElement>()
                                                         .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                                         .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    $"<BundlePackage Id='parent.exe' Cache='keep' CacheId='{parentBundleId}v1.0.1.0' InstallSize='34' Size='*' PerMachine='yes' Permanent='yes' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_parent.exe' RollbackLogPathVariable='WixBundleRollbackLog_parent.exe' BundleId='{parentBundleId}' Version='1.0.1.0' InstallArguments='' UninstallArguments='' RepairArguments='' SupportsBurnProtocol='yes' Win64='no'>" +
                    $"<Provides Key='{parentBundleId}' Version='1.0.1.0' DisplayName='BundlePackageBundle' Imported='yes' />" +
                    "<RelatedBundle Id='{4BE34BEE-CA23-488E-96A0-B15878E3654B}' Action='Upgrade' />" +
                    "<PayloadRef Id='parent.exe' />" +
                    "</BundlePackage>",
                }, bundlePackages);

                ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "Payload", new List<string> { "FileSize", "Hash" } },
                };
                var payloads = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Payload")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<Payload Id='payP6wZpeHEAZbDUQPEKeCpQ_9bN.4' FilePath='signed_cab1.cab' FileSize='*' Hash='*' Packaging='external' SourcePath='signed_cab1.cab' />",
                    "<Payload Id='chain.exe' FilePath='chain.exe' FileSize='*' Hash='*' Packaging='external' SourcePath='chain.exe' />",
                }, payloads);

                registrations = grandparentExtractResult.SelectManifestNodes("/burn:BurnManifest/burn:Registration")
                                                        .Cast<XmlElement>()
                                                        .Select(e => e.GetTestXml())
                                                        .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    $"<Registration Id='{grandparentBundleId}' ExecutableName='grandparent.exe' PerMachine='yes' Tag='' Version='1.0.2.0' ProviderKey='{grandparentBundleId}'>" +
                    "<Arp DisplayName='PermanentBundlePackageBundle' DisplayVersion='1.0.2.0' Publisher='Example Corporation' />" +
                    "</Registration>"
                }, registrations);

                ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "WixPackageProperties", new List<string> { "DownloadSize", "PackageSize" } },
                };
                packageElements = grandparentExtractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:WixPackageProperties")
                                                          .Cast<XmlElement>()
                                                          .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                                          .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixPackageProperties Package='parent.exe' Vital='yes' DisplayName='BundlePackageBundle' Description='BundlePackageBundle' DownloadSize='*' PackageSize='*' InstalledSize='34' PackageType='Bundle' Permanent='yes' LogPathVariable='WixBundleLog_parent.exe' RollbackLogPathVariable='WixBundleRollbackLog_parent.exe' Compressed='yes' Version='1.0.1.0' Cache='keep' />",
                }, packageElements);
            }
        }

        [Fact]
        public void CanBuildBundleWithRemoteBundlePackage()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var parentIntermediateFolder = Path.Combine(baseFolder, "obj", "Parent");
                var binFolder = Path.Combine(baseFolder, "bin");
                var parentBundlePath = Path.Combine(binFolder, "parent.exe");
                var parentPdbPath = Path.Combine(binFolder, "parent.wixpdb");
                var parentBaFolderPath = Path.Combine(baseFolder, "parentba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundlePackage", "RemoteBundlePackage.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", parentIntermediateFolder,
                    "-o", parentBundlePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(parentBundlePath));

                var chainBundleId = "{216BDA7F-74BD-45E8-957B-500552F05629}";
                string parentBundleId;
                using (var wixOutput = WixOutput.Read(parentPdbPath))
                {

                    var intermediate = Intermediate.Load(wixOutput);
                    var section = intermediate.Sections.Single();

                    var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
                    parentBundleId = bundleSymbol.BundleId;
                }

                var extractResult = BundleExtractor.ExtractBAContainer(null, parentBundlePath, parentBaFolderPath, extractFolderPath);
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
                    $"<BundlePackage Id='chain.exe' Cache='keep' CacheId='{chainBundleId}v1.0.0.0' InstallSize='34' Size='*' PerMachine='yes' Permanent='yes' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_chain.exe' RollbackLogPathVariable='WixBundleRollbackLog_chain.exe' BundleId='{chainBundleId}' Version='1.0.0.0' InstallArguments='' UninstallArguments='' RepairArguments='' SupportsBurnProtocol='yes' Win64='no'>" +
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
                    "<Arp DisplayName='RemoteBundlePackageBundle' DisplayVersion='1.0.1.0' Publisher='Example Corporation' />" +
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
                    "<WixPackageProperties Package='chain.exe' Vital='yes' DisplayName='BurnBundle' Description='BurnBundleDescription' DownloadSize='*' PackageSize='*' InstalledSize='34' PackageType='Bundle' Permanent='yes' LogPathVariable='WixBundleLog_chain.exe' RollbackLogPathVariable='WixBundleRollbackLog_chain.exe' Compressed='no' Version='1.0.0.0' Cache='keep' />",
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
                var dotDataPath = Path.Combine(folder, ".Data");
                var parentIntermediateFolder = Path.Combine(baseFolder, "obj", "Parent");
                var binFolder = Path.Combine(baseFolder, "bin");
                var parentBundlePath = Path.Combine(binFolder, "parent.exe");
                var parentPdbPath = Path.Combine(binFolder, "parent.wixpdb");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");
                string chainBundleId = "{215A70DB-AB35-48C7-BE51-D66EAAC87177}";

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "BundlePackage", "V3BundlePackage.wxs"),
                    "-bindpath", dotDataPath,
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", parentIntermediateFolder,
                    "-o", parentBundlePath,
                });

                result.AssertSuccess();

                var warningMessages = result.Messages.Where(m => m.Level == MessageLevel.Warning)
                                                     .Select(m => m.ToString().Replace(dotDataPath, "<dotDataPath>"))
                                                     .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "The BundlePackage 'v3bundle.exe' does not support hiding its ARP registration.",
                }, warningMessages);

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
                    $"<BundlePackage Id='v3bundle.exe' Cache='keep' CacheId='{chainBundleId}v1.0.0.0' InstallSize='1135' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_v3bundle.exe' RollbackLogPathVariable='WixBundleRollbackLog_v3bundle.exe' RepairCondition='0' BundleId='{chainBundleId}' Version='1.0.0.0' InstallArguments='' UninstallArguments='' RepairArguments='' SupportsBurnProtocol='yes' Win64='no'>" +
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
                    "<Arp DisplayName='V3BundlePackageBundle' DisplayVersion='1.1.1.1' Publisher='Example Corporation' />" +
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
                    "<WixPackageProperties Package='v3bundle.exe' Vital='yes' DisplayName='CustomV3Theme' Description='CustomV3Theme' DownloadSize='*' PackageSize='*' InstalledSize='1135' PackageType='Bundle' Permanent='no' LogPathVariable='WixBundleLog_v3bundle.exe' RollbackLogPathVariable='WixBundleRollbackLog_v3bundle.exe' Compressed='yes' Version='1.0.0.0' RepairCondition='0' Cache='keep' />",
                }, packageElements);
            }
        }
    }
}
