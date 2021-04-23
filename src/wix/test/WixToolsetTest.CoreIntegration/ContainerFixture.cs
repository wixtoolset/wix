// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixBuildTools.TestSupport;
    using WixToolset.Core;
    using WixToolset.Core.Burn;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class ContainerFixture
    {
        [Fact(Skip = "Test demonstrates failure")]
        public void CanBuildWithCustomAttachedContainer()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                this.BuildMsis(folder, intermediateFolder, binFolder, buildToSubfolder: true);

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Container", "HarvestIntoAttachedContainer.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var payloads = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Payload");
                Assert.Equal(4, payloads.Count);
                var ignoreAttributes = new Dictionary<string, List<string>> { { "Payload", new List<string> { "FileSize", "Hash" } } };
                Assert.Equal(@"<Payload Id='FirstX64' FilePath='FirstX64\FirstX64.msi' FileSize='*' Hash='*' DownloadUrl='http://example.com//FirstX64/FirstX64/FirstX64.msi' Packaging='embedded' SourcePath='a0' Container='BundlePackages' />", payloads[0].GetTestXml(ignoreAttributes));
                Assert.Equal(@"<Payload Id='FirstX86.msi' FilePath='FirstX86\FirstX86.msi' FileSize='*' Hash='*' DownloadUrl='http://example.com//FirstX86.msi/FirstX86/FirstX86.msi' Packaging='embedded' SourcePath='a1' Container='BundlePackages' />", payloads[1].GetTestXml(ignoreAttributes));
                Assert.Equal(@"<Payload Id='fk1m38Cf9RZ2Bx_ipinRY6BftelU' FilePath='FirstX86\PFiles\MsiPackage\test.txt' FileSize='*' Hash='*' DownloadUrl='http://example.com/FirstX86/fk1m38Cf9RZ2Bx_ipinRY6BftelU/FirstX86/PFiles/MsiPackage/test.txt' Packaging='embedded' SourcePath='a2' Container='BundlePackages' />", payloads[2].GetTestXml(ignoreAttributes));
                Assert.Equal(@"<Payload Id='ff2L_N_DLQ.nSUi.l8LxG14gd2V4' FilePath='FirstX64\PFiles\MsiPackage\test.txt' FileSize='*' Hash='*' DownloadUrl='http://example.com/FirstX64/ff2L_N_DLQ.nSUi.l8LxG14gd2V4/FirstX64/PFiles/MsiPackage/test.txt' Packaging='embedded' SourcePath='a3' Container='BundlePackages' />", payloads[3].GetTestXml(ignoreAttributes));
            }
        }

        [Fact]
        public void HarvestedPayloadsArePutInCorrectContainer()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                this.BuildMsis(folder, intermediateFolder, binFolder);

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Container", "HarvestIntoDetachedContainer.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var payloads = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Payload");
                Assert.Equal(4, payloads.Count);
                var ignoreAttributes = new Dictionary<string, List<string>> { { "Payload", new List<string> { "FileSize", "Hash" } } };
                Assert.Equal(@"<Payload Id='FirstX86.msi' FilePath='FirstX86.msi' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a0' Container='WixAttachedContainer' />", payloads[0].GetTestXml(ignoreAttributes));
                Assert.Equal(@"<Payload Id='FirstX64.msi' FilePath='FirstX64.msi' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a1' Container='FirstX64' />", payloads[1].GetTestXml(ignoreAttributes));
                Assert.Equal(@"<Payload Id='fk1m38Cf9RZ2Bx_ipinRY6BftelU' FilePath='PFiles\MsiPackage\test.txt' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a2' Container='WixAttachedContainer' />", payloads[2].GetTestXml(ignoreAttributes));
                Assert.Equal(@"<Payload Id='fC0n41rZK8oW3JK8LzHu6AT3CjdQ' FilePath='PFiles\MsiPackage\test.txt' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a3' Container='FirstX64' />", payloads[3].GetTestXml(ignoreAttributes));
            }
        }

        [Fact]
        public void HarvestedPayloadsArePutInCorrectPackage()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                this.BuildMsis(folder, intermediateFolder, binFolder);

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Container", "HarvestIntoDetachedContainer.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributes = new Dictionary<string, List<string>>
                {
                    { "MsiPackage", new List<string> { "CacheId", "InstallSize", "Size", "ProductCode" } },
                    { "Provides", new List<string> { "Key" } },
                };
                var msiPackages = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Chain/burn:MsiPackage")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributes))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<MsiPackage Id='FirstX86.msi' Cache='keep' CacheId='*' InstallSize='*' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' LogPathVariable='WixBundleLog_FirstX86.msi' RollbackLogPathVariable='WixBundleRollbackLog_FirstX86.msi' ProductCode='*' Language='1033' Version='1.0.0.0' UpgradeCode='{12E4699F-E774-4D05-8A01-5BDD41BBA127}'>" +
                      "<MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />" +
                      "<Provides Key='*' Version='1.0.0.0' DisplayName='MsiPackage' />" +
                      "<RelatedPackage Id='{12E4699F-E774-4D05-8A01-5BDD41BBA127}' MaxVersion='1.0.0.0' MaxInclusive='no' OnlyDetect='no' LangInclusive='no'><Language Id='1033' /></RelatedPackage>" +
                      "<RelatedPackage Id='{12E4699F-E774-4D05-8A01-5BDD41BBA127}' MinVersion='1.0.0.0' MinInclusive='no' OnlyDetect='yes' LangInclusive='no'><Language Id='1033' /></RelatedPackage>" +
                      "<PayloadRef Id='FirstX86.msi' />" +
                      "<PayloadRef Id='fk1m38Cf9RZ2Bx_ipinRY6BftelU' />" +
                    "</MsiPackage>",
                    "<MsiPackage Id='FirstX64.msi' Cache='keep' CacheId='*' InstallSize='*' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_FirstX64.msi' RollbackLogPathVariable='WixBundleRollbackLog_FirstX64.msi' ProductCode='*' Language='1033' Version='1.0.0.0' UpgradeCode='{12E4699F-E774-4D05-8A01-5BDD41BBA127}'>" +
                      "<MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />" +
                      "<Provides Key='*' Version='1.0.0.0' DisplayName='MsiPackage' />" +
                      "<RelatedPackage Id='{12E4699F-E774-4D05-8A01-5BDD41BBA127}' MaxVersion='1.0.0.0' MaxInclusive='no' OnlyDetect='no' LangInclusive='no'><Language Id='1033' /></RelatedPackage>" +
                      "<RelatedPackage Id='{12E4699F-E774-4D05-8A01-5BDD41BBA127}' MinVersion='1.0.0.0' MinInclusive='no' OnlyDetect='yes' LangInclusive='no'><Language Id='1033' /></RelatedPackage>" +
                      "<PayloadRef Id='FirstX64.msi' />" +
                      "<PayloadRef Id='fC0n41rZK8oW3JK8LzHu6AT3CjdQ' />" +
                    "</MsiPackage>",
                }, msiPackages);
            }
        }

        [Fact]
        public void LayoutPayloadIsPutInContainer()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                this.BuildMsis(folder, intermediateFolder, binFolder);

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "Container", "LayoutPayloadInContainer.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                WixAssert.CompareLineByLine(new string[]
                {
                    "The layout-only Payload 'SharedPayload' is being added to Container 'FirstX64'. It will not be extracted during layout.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributes = new Dictionary<string, List<string>> { { "Payload", new List<string> { "FileSize", "Hash" } } };
                var payloads = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Payload[@Id='SharedPayload']")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributes))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<Payload Id='SharedPayload' FilePath='LayoutPayloadInContainer.wxs' FileSize='*' Hash='*' LayoutOnly='yes' Packaging='embedded' SourcePath='a1' Container='FirstX64' />",
                }, payloads);
            }
        }

        [Fact]
        public void MultipleAttachedContainersAreNotCurrentlySupported()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                this.BuildMsis(folder, intermediateFolder, binFolder);

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Container", "MultipleAttachedContainers.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                Assert.Equal((int)BurnBackendErrors.Ids.MultipleAttachedContainersUnsupported, result.ExitCode);
            }
        }

        [Fact]
        public void PayloadIsNotPutInMultipleContainers()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                this.BuildMsis(folder, intermediateFolder, binFolder);

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "Container", "PayloadInMultipleContainers.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                WixAssert.CompareLineByLine(new string[]
                {
                    "The Payload 'SharedPayload' can't be added to Container 'FirstX64' because it was already added to Container 'FirstX86'.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributes = new Dictionary<string, List<string>> { { "Payload", new List<string> { "FileSize", "Hash" } } };
                var payloads = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Payload[@Id='SharedPayload']")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributes))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<Payload Id='SharedPayload' FilePath='PayloadInMultipleContainers.wxs' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a2' Container='FirstX86' />",
                }, payloads);
            }
        }

        [Fact]
        public void PopulatesBAManifestWithLayoutOnlyPayloads()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                this.BuildMsis(folder, intermediateFolder, binFolder);

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "Container", "LayoutPayloadInContainer.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                WixAssert.CompareLineByLine(new string[]
                {
                    "The layout-only Payload 'SharedPayload' is being added to Container 'FirstX64'. It will not be extracted during layout.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributesByElementName = new Dictionary<string, List<string>>
                {
                    { "WixPayloadProperties", new List<string> { "Size" } },
                };
                var payloads = extractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:WixPayloadProperties")
                                            .Cast<XmlElement>()
                                            .Select(e => e.GetTestXml(ignoreAttributesByElementName))
                                            .ToArray();
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixPayloadProperties Package='FirstX64.msi' Payload='FirstX64.msi' Container='FirstX64' Name='FirstX64.msi' Size='*' />",
                    "<WixPayloadProperties Package='FirstX64.msi' Payload='SharedPayload' Container='FirstX64' Name='LayoutPayloadInContainer.wxs' Size='*' />",
                    "<WixPayloadProperties Package='FirstX64.msi' Payload='fC0n41rZK8oW3JK8LzHu6AT3CjdQ' Container='FirstX64' Name='PFiles\\MsiPackage\\test.txt' Size='*' />",
                    "<WixPayloadProperties Payload='SharedPayload' Container='FirstX64' Name='LayoutPayloadInContainer.wxs' Size='*' />",
                }, payloads);
            }
        }

        private void BuildMsis(string folder, string intermediateFolder, string binFolder, bool buildToSubfolder = false)
        {
            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "MsiTransaction", "FirstX86.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                "-intermediateFolder", intermediateFolder,
                "-o", Path.Combine(binFolder, buildToSubfolder ? "FirstX86" : ".", "FirstX86.msi"),
            });

            result.AssertSuccess();

            result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "MsiTransaction", "FirstX64.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                "-intermediateFolder", intermediateFolder,
                "-o", Path.Combine(binFolder, buildToSubfolder ? "FirstX64" : ".", "FirstX64.msi"),
            });

            result.AssertSuccess();
        }
    }
}
