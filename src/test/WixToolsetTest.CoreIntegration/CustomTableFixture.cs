// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class CustomTableFixture
    {
        [Fact]
        public void PopulatesCustomTable1()
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
                    Path.Combine(folder, "CustomTable", "CustomTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CustomTable1" });
                Assert.Equal(new[]
                {
                    "CustomTable1:Row1\ttest.txt",
                    "CustomTable1:Row2\ttest.txt",
                }, results);
            }
        }

        [Fact]
        public void PopulatesCustomTableWithLocalization()
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
                    Path.Combine(folder, "CustomTable", "LocalizedCustomTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-loc", Path.Combine(folder, "CustomTable", "LocalizedCustomTable.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CustomTableLocalized" });
                Assert.Equal(new[]
                {
                    "CustomTableLocalized:Row1\tThis is row one",
                    "CustomTableLocalized:Row2\tThis is row two",
                }, results);
            }
        }

        [Fact]
        public void PopulatesCustomTableWithFilePath()
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
                    Path.Combine(folder, "CustomTable", "CustomTableWithFile.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "CustomTable", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CustomTableWithFile" });
                Assert.Equal(new[]
                {
                    "CustomTableWithFile:Row1\t[Binary data]",
                    "CustomTableWithFile:Row2\t[Binary data]",
                }, results);
            }
        }

        [Fact]
        public void PopulatesCustomTableWithFilePathSerialized()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(baseFolder, @"bin\test.wixlib");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "CustomTable", "CustomTableWithFile.wxs"),
                    "-bindpath", Path.Combine(folder, "CustomTable", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath
                });

                result.AssertSuccess();

                result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-lib", wixlibPath,
                    "-bindpath", Path.Combine(folder, "CustomTable", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "CustomTableWithFile" });
                Assert.Equal(new[]
                {
                    "CustomTableWithFile:Row1\t[Binary data]",
                    "CustomTableWithFile:Row2\t[Binary data]",
                }, results);
            }
        }

        [Fact]
        public void CanCompileAndDecompile()
        {
            var folder = TestData.Get(@"TestData");
            var expectedFile = Path.Combine(folder, "CustomTable", "CustomTable-Expected.wxs");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");
                var decompiledWxsPath = Path.Combine(baseFolder, @"decompiled.wxs");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "CustomTable", "CustomTable.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();
                Assert.True(File.Exists(msiPath));

                result = WixRunner.Execute(new[]
                {
                    "decompile", msiPath,
                    "-intermediateFolder", intermediateFolder,
                    "-o", decompiledWxsPath
                });

                result.AssertSuccess();

                CompareLineByLine(expectedFile, decompiledWxsPath);
            }
        }

        private static void CompareLineByLine(string expectedFile, string actualFile)
        {
            var expectedLines = File.ReadAllLines(expectedFile);
            var actualLines = File.ReadAllLines(actualFile);
            for (var i = 0; i < expectedLines.Length; ++i)
            {
                Assert.True(actualLines.Length > i, $"{i}: Expected file longer than actual file");
                Assert.Equal($"{i}: {expectedLines[i]}", $"{i}: {actualLines[i]}");
            }
            Assert.True(expectedLines.Length == actualLines.Length, "Actual file longer than expected file");
        }
    }
}
