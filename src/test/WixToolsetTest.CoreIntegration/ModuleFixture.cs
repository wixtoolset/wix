// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class ModuleFixture
    {
        [Fact]
        public void CanBuildSimpleModule()
        {
            var folder = TestData.Get(@"TestData\SimpleModule");

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
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msm")
                });

                result.AssertSuccess();

                var msmPath = Path.Combine(intermediateFolder, @"bin\test.msm");
                Assert.True(File.Exists(msmPath));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var dirSymbols = section.Symbols.OfType<DirectorySymbol>().OrderBy(d => d.Id.Id).ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    "MergeRedirectFolder\tTARGETDIR\t.",
                    "NotTheMergeRedirectFolder\tTARGETDIR\t.",
                    "TARGETDIR\t\tSourceDir"
                }, dirSymbols.Select(d => String.Join("\t", d.Id.Id, d.ParentDirectoryRef, d.Name)).ToArray());

                var fileSymbols = section.Symbols.OfType<FileSymbol>().OrderBy(d => d.Id.Id).ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    $"File1\t{Path.Combine(folder, @"data\test.txt")}\ttest.txt",
                    $"File2\t{Path.Combine(folder, @"data\test.txt")}\ttest.txt",
                }, fileSymbols.Select(fileSymbol => String.Join("\t", fileSymbol.Id.Id, fileSymbol[FileSymbolFields.Source].AsPath().Path, fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path)).ToArray());

                var data = WindowsInstallerData.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var fileRows = data.Tables["File"].Rows;
                Assert.Equal(new[]
                {
                    "File1.243FB739_4D05_472F_9CFB_EF6B1017B6DE",
                    "File2.243FB739_4D05_472F_9CFB_EF6B1017B6DE",
                }, fileRows.Select(r => r.FieldAsString(0)).ToArray());

                var cabPath = Path.Combine(intermediateFolder, "msm-test.cab");
                Query.ExtractStream(msmPath, "MergeModule.CABinet", cabPath);
                var files = Query.GetCabinetFiles(cabPath);
                Assert.Equal(new[]
                {
                    "File1.243FB739_4D05_472F_9CFB_EF6B1017B6DE",
                    "File2.243FB739_4D05_472F_9CFB_EF6B1017B6DE",
                }, files.Select(f => Path.Combine(f.Path, f.Name)).ToArray());
            }
        }

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
    }
}
