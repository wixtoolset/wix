// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class ContainerFixture
    {
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

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MsiTransaction", "FirstX86.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(binFolder, "FirstX86.msi"),
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
                    "-o", Path.Combine(binFolder, "FirstX64.msi"),
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
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

        [Fact(Skip = "https://github.com/wixtoolset/issues/issues/6144")]
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

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MsiTransaction", "FirstX86.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(binFolder, "FirstX86.msi"),
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
                    "-o", Path.Combine(binFolder, "FirstX64.msi"),
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Container", "MultipleAttachedContainers.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                Assert.InRange(result.ExitCode, 2, Int32.MaxValue);
            }
        }
    }
}
