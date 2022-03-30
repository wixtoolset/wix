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
                    $"<BundlePackage Id='chain.exe' Cache='keep' CacheId='{chainBundleId}v1.0.0.0' InstallSize='34' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_chain.exe' RollbackLogPathVariable='WixBundleRollbackLog_chain.exe' BundleId='{chainBundleId}' InstallArguments='' UninstallArguments='' RepairArguments='' SupportsBurnProtocol='yes' Win64='no'><Provides Key='MyProviderKey,v1.0' Version='1.0.0.0' DisplayName='BurnBundle' Imported='yes' /><PayloadRef Id='chain.exe' /></BundlePackage>",
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
            }
        }
    }
}
