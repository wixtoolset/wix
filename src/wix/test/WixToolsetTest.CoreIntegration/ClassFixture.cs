// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using Xunit;

    public class ClassFixture
    {
        [Fact]
        public void ClassWithoutContextDoesNotCrash()
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
                    Path.Combine(folder, "Class", "ClassWithoutContext.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var results = Query.QueryDatabase(msiPath, new[] { "Class", "Registry" });
                Assert.Empty(results);
            }
        }

        [Fact]
        public void PopulatesClassTablesWhenIconIndexIsZero()
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
                    Path.Combine(folder, "Class", "IconIndex0.wxs"),
                    Path.Combine(folder, "Icon", "SampleIcon.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Class" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Class:{3FAED4CC-C473-4B8A-BE8B-303871377A4A}\tLocalServer32\tClassComp\t\tFakeClass3FAE\t\t\tSampleIcon\t0\t\t\tProductFeature\t",
                }, results);
            }
        }

        [Fact]
        public void PopulatesClassTablesWhenProgIdIsNestedUnderAdvertisedClass()
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
                    Path.Combine(folder, "ProgId", "NestedUnderClass.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Class", "ProgId", "Registry" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Class:{F12A6F69-117F-471F-AE73-F8E74218F498}\tLocalServer32\tProgIdComp\t73E7DF7E-EFAC-4E11-90E2-6EBAEB8DE58D\tFakeClassF12A\t\t\t\t\t\t\tProductFeature\t",
                    "ProgId:73E7DF7E-EFAC-4E11-90E2-6EBAEB8DE58D\t\t{F12A6F69-117F-471F-AE73-F8E74218F498}\tFakeClassF12A\t\t",
                    "Registry:regUIIK326nDZpkWHuexeF58EikQvA\t0\t73E7DF7E-EFAC-4E11-90E2-6EBAEB8DE58D\tNoOpen\tNoOpen73E7\tProgIdComp",
                    "Registry:regvrhMurMp98anbQJkpgA8yJCefdM\t0\tCLSID\\{F12A6F69-117F-471F-AE73-F8E74218F498}\\Version\t\t0.0.0.1\tProgIdComp",
                    "Registry:regY1F4E2lvu_Up6gV6c3jeN5ukn8s\t0\tCLSID\\{F12A6F69-117F-471F-AE73-F8E74218F498}\\LocalServer32\tThreadingModel\tApartment\tProgIdComp",
                }, results);
            }
        }

        [Fact]
        public void PopulatesModuleClassTablesWhenProgIdIsNestedUnderAdvertisedClass()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin", "test.msm");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ProgId", "NestedUnderClass.wxs"),
                    Path.Combine(folder, "ProgId", "Module.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Class", "ProgId", "Registry" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Class:{F12A6F69-117F-471F-AE73-F8E74218F498}\tLocalServer32\tProgIdComp.047730A5_30FE_4A62_A520_DA9381B8226A\t73E7DF7E-EFAC-4E11-90E2-6EBAEB8DE58D\tFakeClassF12A\t\t\t\t\t\t\t{00000000-0000-0000-0000-000000000000}\t",
                    "ProgId:73E7DF7E-EFAC-4E11-90E2-6EBAEB8DE58D\t\t{F12A6F69-117F-471F-AE73-F8E74218F498}\tFakeClassF12A\t\t",
                    "Registry:regUIIK326nDZpkWHuexeF58EikQvA.047730A5_30FE_4A62_A520_DA9381B8226A\t0\t73E7DF7E-EFAC-4E11-90E2-6EBAEB8DE58D\tNoOpen\tNoOpen73E7\tProgIdComp.047730A5_30FE_4A62_A520_DA9381B8226A",
                    "Registry:regvrhMurMp98anbQJkpgA8yJCefdM.047730A5_30FE_4A62_A520_DA9381B8226A\t0\tCLSID\\{F12A6F69-117F-471F-AE73-F8E74218F498}\\Version\t\t0.0.0.1\tProgIdComp.047730A5_30FE_4A62_A520_DA9381B8226A",
                    "Registry:regY1F4E2lvu_Up6gV6c3jeN5ukn8s.047730A5_30FE_4A62_A520_DA9381B8226A\t0\tCLSID\\{F12A6F69-117F-471F-AE73-F8E74218F498}\\LocalServer32\tThreadingModel\tApartment\tProgIdComp.047730A5_30FE_4A62_A520_DA9381B8226A",
                }, results);
            }
        }

        [Fact]
        public void CanBuildProductWithProgIdExtensionVerb()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin", "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ProgId", "ProgIdExtensionVerb.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Extension", "ProgId", "Verb" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Extension:foo\tfilTki4JQ2gSapF7wK4K1vd.4mDSFQ\tExample.Foo\t\tProductFeature",
                    "ProgId:Example.Foo\t\t\t\t\t0",
                    "Verb:foo\tOpenVerb\t\topen\t\"%1\""
                }, results);
            }
        }

        [Fact]
        public void CanBuildModuleWithProgIdExtensionVerb()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msmPath = Path.Combine(baseFolder, @"bin", "test.msm");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "ProgId", "ProgIdExtensionVerb.wxs"),
                    Path.Combine(folder, "ProgId", "Module.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msmPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msmPath));
                var results = Query.QueryDatabase(msmPath, new[] { "Extension", "ProgId", "Verb" });
                WixAssert.CompareLineByLine(new[]
                {
                    "Extension:foo\tfilTki4JQ2gSapF7wK4K1vd.4mDSFQ.047730A5_30FE_4A62_A520_DA9381B8226A\tExample.Foo\t\t{00000000-0000-0000-0000-000000000000}",
                    "ProgId:Example.Foo\t\t\t\t\t0",
                    "Verb:foo\tOpenVerb\t\topen\t\"%1\""
                }, results);
            }
        }
    }
}
