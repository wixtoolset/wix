// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class FeatureFixture
    {
        [Fact]
        public void CanDetectMissingFeatureComponentMapping()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Feature", "PackageMissingFeatureComponentMapping.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                Assert.Equal(267, result.ExitCode);

                var errors = result.Messages.Where(m => m.Level == MessageLevel.Error);
                Assert.Equal(new[]
                {
                    267
                }, errors.Select(e => e.Id).ToArray());
            }
        }

        [Fact]
        public void CannotBuildMsiWithTooLargeFeatureDepth()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Feature", "PackageWithExcessiveFeatureDepth.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                Assert.Equal(7503, result.ExitCode);

                var errors = result.Messages.Where(m => m.Level == MessageLevel.Error);
                Assert.Equal(new[]
                {
                    7503
                }, errors.Select(e => e.Id).ToArray());
                Assert.Equal("Maximum depth of the Feature tree allowed in an MSI was exceeded. An MSI does not support a Feature tree with depth greater than 16. The Feature 'Depth17' is at depth 17.", errors.Single().ToString());
            }
        }
    }
}
