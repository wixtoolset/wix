// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using Example.Extension;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Burn;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class BundleFixture
    {
        [Fact]
        public void CanBuildMultiFileBundle()
        {
            var folder = TestData.Get(@"TestData\SimpleBundle");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MultiFileBootstrapperApplication.wxs"),
                    Path.Combine(folder, "MultiFileBundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildSimpleBundle()
        {
            var folder = TestData.Get(@"TestData\SimpleBundle");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var pdbPath = Path.Combine(baseFolder, @"bin\test.wixpdb");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();
                Assert.Empty(result.Messages.Where(m => m.Level == MessageLevel.Warning));

                Assert.True(File.Exists(exePath));
                Assert.True(File.Exists(pdbPath));

                using (var wixOutput = WixOutput.Read(pdbPath))
                {

                    var intermediate = Intermediate.Load(wixOutput);
                    var section = intermediate.Sections.Single();

                    var bundleSymbol = section.Symbols.OfType<WixBundleSymbol>().Single();
                    Assert.Equal("1.0.0.0", bundleSymbol.Version);

                    var previousVersion = bundleSymbol.Fields[(int)WixBundleSymbolFields.Version].PreviousValue;
                    Assert.Equal("!(bind.packageVersion.test.msi)", previousVersion.AsString());

                    var msiSymbol = section.Symbols.OfType<WixBundlePackageSymbol>().Single();
                    Assert.Equal("test.msi", msiSymbol.Id.Id);

                    var extractResult = BundleExtractor.ExtractBAContainer(null, exePath, baFolderPath, extractFolderPath);
                    extractResult.AssertSuccess();

                    var burnManifestData = wixOutput.GetData(BurnConstants.BurnManifestWixOutputStreamName);
                    var extractedBurnManifestData = File.ReadAllText(Path.Combine(baFolderPath, "manifest.xml"), Encoding.UTF8);
                    Assert.Equal(extractedBurnManifestData, burnManifestData);

                    var baManifestData = wixOutput.GetData(BurnConstants.BootstrapperApplicationDataWixOutputStreamName);
                    var extractedBaManifestData = File.ReadAllText(Path.Combine(baFolderPath, "BootstrapperApplicationData.xml"), Encoding.UTF8);
                    Assert.Equal(extractedBaManifestData, baManifestData);

                    var bextManifestData = wixOutput.GetData(BurnConstants.BundleExtensionDataWixOutputStreamName);
                    var extractedBextManifestData = File.ReadAllText(Path.Combine(baFolderPath, "BundleExtensionData.xml"), Encoding.UTF8);
                    Assert.Equal(extractedBextManifestData, bextManifestData);

                    var logElements = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Log");
                    var logElement = (XmlNode)Assert.Single(logElements);
                    Assert.Equal("<Log PathVariable='WixBundleLog' Prefix='~TestBundle' Extension='log' />", logElement.GetTestXml());

                    var registrationElements = extractResult.SelectManifestNodes("/burn:BurnManifest/burn:Registration");
                    var registrationElement = (XmlNode)Assert.Single(registrationElements);
                    Assert.Equal($"<Registration Id='{bundleSymbol.BundleId}' ExecutableName='test.exe' PerMachine='yes' Tag='' Version='1.0.0.0' ProviderKey='{bundleSymbol.BundleId}'>" +
                        "<Arp Register='yes' DisplayName='~TestBundle' DisplayVersion='1.0.0.0' Publisher='Example Corporation' />" +
                        "</Registration>", registrationElement.GetTestXml());
                }
            }
        }

        [Fact]
        public void CanBuildSimpleBundleUsingExtensionBA()
        {
            var extensionPath = Path.GetFullPath(new Uri(typeof(ExampleExtensionFactory).Assembly.CodeBase).LocalPath);
            var folder = TestData.Get(@"TestData\SimpleBundle");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MultiFileBundle.wxs"),
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-ext", extensionPath,
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                result.AssertSuccess();

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.exe")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
            }
        }

        [Fact]
        public void CanBuildSingleExeBundle()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SingleExeBundle", "SingleExePackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
            }
        }

        [Fact(Skip = "Test demonstrates failure")]
        public void CantBuildSingleExeBundleWithInvalidArgument()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SingleExeBundle", "SingleExePackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                    "-nonexistentswitch", "param",
                });

                Assert.NotEqual(0, result.ExitCode);

                Assert.False(File.Exists(exePath));
            }
        }

        [Fact]
        public void CanBuildSingleExeRemotePayloadBundle()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SingleExeBundle", "SingleExeRemotePayload.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
            }
        }
    }
}
