// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class MsiTransactionFixture
    {
        [Fact]
        public void CantBuildX64AfterX86Bundle()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var exePath = Path.Combine(binFolder, "test.exe");

                BuildMsiPackages(folder, intermediateFolder, binFolder);

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MsiTransaction", "X64AfterX86Bundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                Assert.Equal(390, result.ExitCode);
            }
        }

        [Fact]
        public void CanBuildX86AfterX64Bundle()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var binFolder = Path.Combine(baseFolder, "bin");
                var exePath = Path.Combine(binFolder, "test.exe");

                BuildMsiPackages(folder, intermediateFolder, binFolder);

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MsiTransaction", "X86AfterX64Bundle.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", binFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(exePath));
            }
        }

        private static void BuildMsiPackages(string folder, string intermediateFolder, string binFolder)
        {
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
                Path.Combine(folder, "MsiTransaction", "SecondX86.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                "-intermediateFolder", intermediateFolder,
                "-o", Path.Combine(binFolder, "SecondX86.msi"),
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
                "-arch", "x64",
                "-o", Path.Combine(binFolder, "FirstX64.msi"),
            });

            result.AssertSuccess();

            result = WixRunner.Execute(new[]
            {
                "build",
                Path.Combine(folder, "MsiTransaction", "SecondX64.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                "-intermediateFolder", intermediateFolder,
                "-arch", "x64",
                "-o", Path.Combine(binFolder, "SecondX64.msi"),
            });

            result.AssertSuccess();
        }
    }
}
