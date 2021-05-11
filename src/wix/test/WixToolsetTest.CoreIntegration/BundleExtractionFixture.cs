// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;
    using Xunit;

    public class BundleExtractionFixture
    {
        [Fact]
        public void CanExtractBundleWithDetachedContainer()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");
                var pdbPath = Path.Combine(baseFolder, @"bin\test.wixpdb");
                var extractFolderPath = Path.Combine(baseFolder, "extract");
                var baFolderPath = Path.Combine(extractFolderPath, "UX");
                var attachedContainerFolderPath = Path.Combine(extractFolderPath, "AttachedContainer");

                // TODO: use WixRunner.Execute(string[]) to always go through the command line.
                var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider();
                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithDetachedContainer", "Bundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "MinimalPackageGroup.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                }, serviceProvider, out var messages).Result;

                WixRunnerResult.AssertSuccess(result, messages);
                Assert.Empty(messages.Where(m => m.Level == MessageLevel.Warning));

                Assert.True(File.Exists(exePath));

                var unbinder = serviceProvider.GetService<IUnbinder>();
                unbinder.Unbind(exePath, OutputType.Bundle, extractFolderPath);

                Assert.True(File.Exists(Path.Combine(baFolderPath, "manifest.xml")));
                Assert.False(Directory.Exists(attachedContainerFolderPath));
            }
        }
    }
}
