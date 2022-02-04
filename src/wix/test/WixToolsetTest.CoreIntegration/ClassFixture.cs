// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
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
    }
}
