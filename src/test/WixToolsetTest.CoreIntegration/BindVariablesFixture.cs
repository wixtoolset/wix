// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class BindVariablesFixture
    {
        [Fact]
        public void CanBuildWithDefaultValue()
        {
            var folder = TestData.Get(@"TestData", "BindVariables");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "DefaultedVariable.wxs"),
                    "-bf",
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", folder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();
            }
        }

        [Fact]
        public void CannotBuildWixlibWithBinariesFromMissingNamedBindPaths()
        {
            var folder = TestData.Get(@"TestData", "WixlibWithBinaries");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-bf",
                    "-bindpath", Path.Combine(folder, "data"),
                    // Use names that aren't excluded in default .gitignores.
                    "-bindpath", $"AlphaBits={Path.Combine(folder, "data", "alpha")}",
                    "-bindpath", $"PowerBits={Path.Combine(folder, "data", "powerpc")}",
                    "-bindpath", $"{Path.Combine(folder, "data", "alpha")}",
                    "-bindpath", $"{Path.Combine(folder, "data", "powerpc")}",
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                Assert.Equal(103, result.ExitCode);
            }
        }
    }
}
