// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class ModuleFixture
    {
        [Fact]
        public void CanSuppressModularization()
        {
            var folder = TestData.Get(@"TestData\SuppressModularization");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Module.wxs"),
                    "-loc", Path.Combine(folder, "Module.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-sw1079",
                    "-sw1086",
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msm")
                });

                result.AssertSuccess();

                var msmPath = Path.Combine(intermediateFolder, @"bin\test.msm");

                var rows = Query.QueryDatabase(msmPath, new[] { "CustomAction", "Property" });
                WixAssert.CompareLineByLine(new[]
                {
                    "CustomAction:Test\t11265\tFakeCA.243FB739_4D05_472F_9CFB_EF6B1017B6DE\tTestEntry\t",
                    "Property:MsiHiddenProperties\tTest"
                }, rows);
            }
        }

        [Fact]
        public void CanMergeModuleAndValidate()
        {
            var msmFolder = TestData.Get(@"TestData\SimpleModule");
            var folder = TestData.Get(@"TestData\SimpleMerge");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var msiPath = Path.Combine(intermediateFolder, @"bin\test.msi");
                var cabPath = Path.Combine(intermediateFolder, @"bin\cab1.cab");

                var msmResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(msmFolder, "Module.wxs"),
                    "-loc", Path.Combine(msmFolder, "Module.en-us.wxl"),
                    "-bindpath", Path.Combine(msmFolder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, "bin", "test", "test.msm")
                });

                msmResult.AssertSuccess();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(intermediateFolder, "bin", "test"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();
                Assert.Empty(section.Symbols.OfType<FileSymbol>());

                var data = WindowsInstallerData.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                Assert.Empty(data.Tables["File"].Rows);

                var results = Query.QueryDatabase(msiPath, new[] { "File" });
                WixAssert.CompareLineByLine(new[]
                {
                    "File:File1.243FB739_4D05_472F_9CFB_EF6B1017B6DE\tModuleComponent1.243FB739_4D05_472F_9CFB_EF6B1017B6DE\tfile1.txt\t17\t\t\t512\t1",
                    "File:File2.243FB739_4D05_472F_9CFB_EF6B1017B6DE\tModuleComponent2.243FB739_4D05_472F_9CFB_EF6B1017B6DE\tfile2.txt\t17\t\t\t512\t2",
                }, results);

                var files = Query.GetCabinetFiles(cabPath);
                WixAssert.CompareLineByLine(new[]
                {
                    "File1.243FB739_4D05_472F_9CFB_EF6B1017B6DE",
                    "File2.243FB739_4D05_472F_9CFB_EF6B1017B6DE"
                }, files.Select(f => f.Name).ToArray());
            }
        }
    }
}
