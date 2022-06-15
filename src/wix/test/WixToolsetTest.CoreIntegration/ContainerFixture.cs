// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.Burn.Bundles;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Data.WindowsInstaller.Rows;
    using Xunit;

    public class ContainerFixture
    {
        [Fact]
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

                var ignoreAttributes = new Dictionary<string, List<string>> { { "Payload", new List<string> { "FileSize", "Hash" } } };
                var payloads = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Payload", ignoreAttributes);
                WixAssert.CompareLineByLine(new[]
                {
                    @"<Payload Id='FirstX64' FilePath='FirstX64\FirstX64.msi' FileSize='*' Hash='*' DownloadUrl='http://example.com//FirstX64/FirstX64/FirstX64.msi' Packaging='embedded' SourcePath='a0' Container='BundlePackages' />",
                    @"<Payload Id='FirstX86.msi' FilePath='FirstX86\FirstX86.msi' FileSize='*' Hash='*' DownloadUrl='http://example.com//FirstX86.msi/FirstX86/FirstX86.msi' Packaging='embedded' SourcePath='a1' Container='BundlePackages' />",
                    @"<Payload Id='fk1m38Cf9RZ2Bx_ipinRY6BftelU' FilePath='FirstX86\PFiles\MsiPackage\test.txt' FileSize='*' Hash='*' DownloadUrl='http://example.com/FirstX86.msi/fk1m38Cf9RZ2Bx_ipinRY6BftelU/FirstX86/PFiles/MsiPackage/test.txt' Packaging='embedded' SourcePath='a2' Container='BundlePackages' />",
                    @"<Payload Id='ff2L_N_DLQ.nSUi.l8LxG14gd2V4' FilePath='FirstX64\PFiles\MsiPackage\test.txt' FileSize='*' Hash='*' DownloadUrl='http://example.com/FirstX64/ff2L_N_DLQ.nSUi.l8LxG14gd2V4/FirstX64/PFiles/MsiPackage/test.txt' Packaging='embedded' SourcePath='a3' Container='BundlePackages' />",
                }, payloads);
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

                var ignoreAttributes = new Dictionary<string, List<string>> { { "Payload", new List<string> { "FileSize", "Hash" } } };
                var payloads = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Payload", ignoreAttributes);
                WixAssert.CompareLineByLine(new[]
                {
                    @"<Payload Id='FirstX86.msi' FilePath='FirstX86.msi' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a0' Container='WixAttachedContainer' />",
                    @"<Payload Id='FirstX64.msi' FilePath='FirstX64.msi' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a1' Container='FirstX64' />",
                    @"<Payload Id='fk1m38Cf9RZ2Bx_ipinRY6BftelU' FilePath='PFiles\MsiPackage\test.txt' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a2' Container='WixAttachedContainer' />",
                    @"<Payload Id='fC0n41rZK8oW3JK8LzHu6AT3CjdQ' FilePath='PFiles\MsiPackage\test.txt' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a3' Container='FirstX64' />",
                }, payloads);
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

                var pdbPaths = this.BuildMsis(folder, intermediateFolder, binFolder);

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
                };
                var msiPackages = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Chain/burn:MsiPackage", ignoreAttributes);
                WixAssert.CompareLineByLine(new[]
                {
                    "<MsiPackage Id='FirstX86.msi' Cache='keep' CacheId='*' InstallSize='*' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryForward='WixDefaultBoundary' LogPathVariable='WixBundleLog_FirstX86.msi' RollbackLogPathVariable='WixBundleRollbackLog_FirstX86.msi' ProductCode='*' Language='1033' Version='1.0.0.0' UpgradeCode='{12E4699F-E774-4D05-8A01-5BDD41BBA127}'>" +
                      "<MsiProperty Id='MSIFASTINSTALL' Value='1' />" +
                      "<MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />" +
                     $"<Provides Key='{GetProductCodeFromMsiPdb(pdbPaths[0])}_v1.0.0.0' Version='1.0.0.0' DisplayName='MsiPackage' />" +
                      "<RelatedPackage Id='{12E4699F-E774-4D05-8A01-5BDD41BBA127}' MaxVersion='1.0.0.0' MaxInclusive='no' OnlyDetect='no' LangInclusive='yes'><Language Id='1033' /></RelatedPackage>" +
                      "<RelatedPackage Id='{12E4699F-E774-4D05-8A01-5BDD41BBA127}' MinVersion='1.0.0.0' MinInclusive='no' OnlyDetect='yes' LangInclusive='yes'><Language Id='1033' /></RelatedPackage>" +
                      "<PayloadRef Id='FirstX86.msi' />" +
                      "<PayloadRef Id='fk1m38Cf9RZ2Bx_ipinRY6BftelU' />" +
                    "</MsiPackage>",
                    "<MsiPackage Id='FirstX64.msi' Cache='keep' CacheId='*' InstallSize='*' Size='*' PerMachine='yes' Permanent='no' Vital='yes' RollbackBoundaryBackward='WixDefaultBoundary' LogPathVariable='WixBundleLog_FirstX64.msi' RollbackLogPathVariable='WixBundleRollbackLog_FirstX64.msi' ProductCode='*' Language='1033' Version='1.0.0.0' UpgradeCode='{12E4699F-E774-4D05-8A01-5BDD41BBA127}'>" +
                      "<MsiProperty Id='ARPSYSTEMCOMPONENT' Value='1' />" +
                      "<MsiProperty Id='MSIFASTINSTALL' Value='7' />" +
                     $"<Provides Key='{GetProductCodeFromMsiPdb(pdbPaths[1])}_v1.0.0.0' Version='1.0.0.0' DisplayName='MsiPackage' />" +
                      "<RelatedPackage Id='{12E4699F-E774-4D05-8A01-5BDD41BBA127}' MaxVersion='1.0.0.0' MaxInclusive='no' OnlyDetect='no' LangInclusive='yes'><Language Id='1033' /></RelatedPackage>" +
                      "<RelatedPackage Id='{12E4699F-E774-4D05-8A01-5BDD41BBA127}' MinVersion='1.0.0.0' MinInclusive='no' OnlyDetect='yes' LangInclusive='yes'><Language Id='1033' /></RelatedPackage>" +
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

                WixAssert.CompareLineByLine(new[]
                {
                    "The layout-only Payload 'SharedPayload' is being added to Container 'FirstX64'. It will not be extracted during layout.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributes = new Dictionary<string, List<string>> { { "Payload", new List<string> { "FileSize", "Hash" } } };
                var payloads = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Payload[@Id='SharedPayload']", ignoreAttributes);
                WixAssert.CompareLineByLine(new[]
                {
                    "<Payload Id='SharedPayload' FilePath='LayoutPayloadInContainer.wxs' FileSize='*' Hash='*' LayoutOnly='yes' Packaging='embedded' SourcePath='a1' Container='FirstX64' />",
                }, payloads);
            }
        }

        [Fact]
        public void MultipleAttachedContainers()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var bundlePath = Path.Combine(binFolder, "test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var attachedFolderPath = Path.Combine(baseFolder, "attached");
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

                result.AssertSuccess();
                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractAllContainers(null, bundlePath, baFolderPath, attachedFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(attachedFolderPath, "FirstX64", "FirstX64.msi")), "Expected extracted container to contain FirstX64.msi");
                Assert.True(File.Exists(Path.Combine(attachedFolderPath, "WixAttachedContainer", "FirstX86.msi")), "Expected extracted container to contain FirstX86.msi");

                var ignoreAttributes = new Dictionary<string, List<string>> { { "Payload", new List<string> { "FileSize", "Hash" } } };
                var payloads = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Payload", ignoreAttributes);
                WixAssert.CompareLineByLine(new[]
                {
                    "<Payload Id='FirstX86.msi' FilePath='FirstX86.msi' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a0' Container='WixAttachedContainer' />",
                    "<Payload Id='FirstX64.msi' FilePath='FirstX64.msi' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a1' Container='FirstX64' />",
                    "<Payload Id='fk1m38Cf9RZ2Bx_ipinRY6BftelU' FilePath='PFiles\\MsiPackage\\test.txt' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a2' Container='WixAttachedContainer' />",
                    "<Payload Id='fC0n41rZK8oW3JK8LzHu6AT3CjdQ' FilePath='PFiles\\MsiPackage\\test.txt' FileSize='*' Hash='*' Packaging='embedded' SourcePath='a3' Container='FirstX64' />",
                }, payloads);
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

                WixAssert.CompareLineByLine(new[]
                {
                    "The Payload 'SharedPayload' can't be added to Container 'FirstX64' because it was already added to Container 'FirstX86'.",
                }, result.Messages.Select(m => m.ToString()).ToArray());
                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var ignoreAttributes = new Dictionary<string, List<string>> { { "Payload", new List<string> { "FileSize", "Hash" } } };
                var payloads = extractResult.GetManifestTestXmlLines("/burn:BurnManifest/burn:Payload[@Id='SharedPayload']", ignoreAttributes);
                WixAssert.CompareLineByLine(new[]
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

                WixAssert.CompareLineByLine(new[]
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
                var payloads = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixPayloadProperties", ignoreAttributesByElementName);
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixPayloadProperties Package='FirstX64.msi' Payload='FirstX64.msi' Container='FirstX64' Name='FirstX64.msi' Size='*' />",
                    "<WixPayloadProperties Package='FirstX64.msi' Payload='SharedPayload' Container='FirstX64' Name='LayoutPayloadInContainer.wxs' Size='*' />",
                    "<WixPayloadProperties Package='FirstX64.msi' Payload='fC0n41rZK8oW3JK8LzHu6AT3CjdQ' Container='FirstX64' Name='PFiles\\MsiPackage\\test.txt' Size='*' />",
                    "<WixPayloadProperties Payload='SharedPayload' Container='FirstX64' Name='LayoutPayloadInContainer.wxs' Size='*' />",
                }, payloads);
            }
        }

        private string[] BuildMsis(string folder, string intermediateFolder, string binFolder, bool buildToSubfolder = false)
        {
            var x86OutputPath = Path.Combine(binFolder, buildToSubfolder ? "FirstX86" : ".", "FirstX86.msi");
            var result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "MsiTransaction", "FirstX86.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                "-intermediateFolder", intermediateFolder,
                "-o", x86OutputPath,
            });

            result.AssertSuccess();

            var x64OutputPath = Path.Combine(binFolder, buildToSubfolder ? "FirstX64" : ".", "FirstX64.msi");
            result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "MsiTransaction", "FirstX64.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                "-intermediateFolder", intermediateFolder,
                "-o", x64OutputPath,
            });

            result.AssertSuccess();

            return new[]
            {
                Path.ChangeExtension(x86OutputPath, ".wixpdb"),
                Path.ChangeExtension(x64OutputPath, ".wixpdb"),
            };
        }

        private static string GetProductCodeFromMsiPdb(string pdbPath)
        {
            var wiData = WindowsInstallerData.Load(pdbPath, suppressVersionCheck: false);
            return wiData.Tables["Property"].Rows.Cast<PropertyRow>().Single(r => r.Property == "ProductCode").Value;
        }
    }
}
