// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Collections.Generic;
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class BundleManifestFixture
    {
        [Fact]
        public void PopulatesManifestWithBundleExtension()
        {
            var burnStubPath = TestData.Get(@"TestData\.Data\burn.exe");
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
                    "-burnStub", burnStubPath,
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
    }
}
