// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class RegistryFixture
    {
        [Fact]
        public void PopulatesRegistryTableFromRegistryValue()
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
                    Path.Combine(folder, "Registry", "RegistryValue.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Registry" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Registry:reg04OIwIchl.9ZTjisTT6NzGSsQSM\t2\tPath\\To\\AnotherKey\tSecret\t#x\tMiscComponent",
                    "Registry:regEblTuusqFNSUQNy88zaP_UA5kIY\t2\tPath\\To\\Key\t\t1.0.1234.123\tMiscComponent",
                }, results);
            }
        }

        [Fact]
        public void PopulatesRegistryTableFromRegistryValueMultiString()
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
                    Path.Combine(folder, "Registry", "RegistryValueMultiString.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var results = Query.QueryDatabase(msiPath, new[] { "Registry" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Registry:regitq_Wx9LfvJuNSc2un6gIHAzr4A\t2\tPath\\To\\AnotherKey\tSecret\t#x\tMultiStringComponent",
                    "Registry:regmeTJMpOD41igfxhTcUVZ7kNG1Mo\t2\tPath\\To\\Key\t\ta[~]b[~][~]c[~]\tMultiStringComponent",
                }, results);
            }
        }

        [Fact]
        public void DuplicateRegistryValueIdsAreDetectedSmoothly()
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
                    Path.Combine(folder, "Registry", "DuplicateRegistryValueIds.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                }, out var messages);

                Assert.Equal(2, messages.Where(m => m.Id == (int)ErrorMessages.Ids.DuplicateSymbol).Count());
                Assert.Equal(2, messages.Where(m => m.Id == (int)ErrorMessages.Ids.DuplicateSymbol2).Count());
            }
        }

        [Fact]
        public void PopulatesRegistryTableFromRemoveRegistryKey()
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
                    Path.Combine(folder, "Registry", "RemoveRegistryKey.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Registry" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Registry:RemoveAKeyName\t2\tAKeyName\t-\t\tRemoveRegistryKeyComp",
                }, results);
            }
        }

        [Fact]
        public void PopulatesRegistryTableWithoutExtraBackslash()
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
                    Path.Combine(folder, "Registry", "RegistryKeyEndingWithBackslash.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Registry" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Registry:reg1\t2\tSoftware\\WBM\\WB\t*\t\tMiscComponent",
                    "Registry:reg2\t2\tSoftware\\WBM\\WB\tInstallationPath\t[INSTALLFOLDER]\tMiscComponent",
                }, results);
            }
        }
    }
}
