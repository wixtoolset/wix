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

    public class ValidationFixture
    {
        [Fact]
        public void CanBuildAndValidateSimpleModule()
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
                WixAssert.CompareLineByLine(new[]
                {
                    "File1.243FB739_4D05_472F_9CFB_EF6B1017B6DE",
                    "File2.243FB739_4D05_472F_9CFB_EF6B1017B6DE",
                }, fileRows.Select(r => r.FieldAsString(0)).ToArray());

                var cabPath = Path.Combine(intermediateFolder, "msm-test.cab");
                Query.ExtractStream(msmPath, "MergeModule.CABinet", cabPath);
                var files = Query.GetCabinetFiles(cabPath);
                WixAssert.CompareLineByLine(new[]
                {
                    "File1.243FB739_4D05_472F_9CFB_EF6B1017B6DE",
                    "File2.243FB739_4D05_472F_9CFB_EF6B1017B6DE",
                }, files.Select(f => Path.Combine(f.Path, f.Name)).ToArray());

                var rows = Query.QueryDatabase(msmPath, new[] { "_SummaryInformation" });
                WixAssert.CompareLineByLine(new[]
                {
                    "_SummaryInformation:Title\tMerge Module",
                    "_SummaryInformation:Subject\tMergeModule1",
                    "_SummaryInformation:Author\tExample Company",
                    "_SummaryInformation:Keywords\tMergeModule, MSI, database",
                    "_SummaryInformation:Comments\tThis merge module contains the logic and data required to install MergeModule1.",
                    "_SummaryInformation:Template\tIntel;1033",
                    "_SummaryInformation:CodePage\t1252",
                    "_SummaryInformation:PageCount\t200",
                    "_SummaryInformation:WordCount\t0",
                    "_SummaryInformation:CharacterCount\t0",
                    "_SummaryInformation:Security\t2",
                }, rows);

                var validationResult = WixRunner.Execute(new[]
                {
                    "msi", "validate",
                    "-intermediateFolder", intermediateFolder,
                    msmPath
                });
                validationResult.AssertSuccess();
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

                var validationResult = WixRunner.Execute(new[]
                {
                    "msi", "validate",
                    "-intermediateFolder", intermediateFolder,
                    msiPath
                });
                validationResult.AssertSuccess();

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

        [Fact]
        public void CanValidateMsiWithIceIssues()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin", "CanValidateMsiWithIceIssues.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Validation", "PackageWithIceIssues.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                var validationResult = WixRunner.Execute(new[]
                {
                    "msi", "validate",
                    "-intermediateFolder", intermediateFolder,
                    msiPath
                });

                Assert.Equal(1, validationResult.ExitCode);

                var messages = validationResult.Messages.Select(m => m.ToString()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"ICE12: CustomAction: CausesICE12Error is of type: 35. Therefore it must come after CostFinalize @ 1000 in Seq Table: InstallExecuteSequence. CA Seq#: 49",
                    @"ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only."
                }, messages);
            }
        }

        [Fact]
        public void CanValidateMsiSuppressIceError()
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
                    Path.Combine(folder, "Validation", "PackageWithIceIssues.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                var validationResult = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "msi", "validate",
                    "-intermediateFolder", intermediateFolder,
                    "-sice", "ICE12",
                    msiPath
                });

                validationResult.AssertSuccess();

                var messages = validationResult.Messages.Select(m => m.ToString()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only."
                }, messages);
            }
        }

        [Fact]
        public void CanValidateMsiWithWarningsAsErrors()
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
                    Path.Combine(folder, "Validation", "PackageWithIceIssues.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                var validationResult = WixRunner.Execute(warningsAsErrors: true, new[]
                {
                    "msi", "validate",
                    "-intermediateFolder", intermediateFolder,
                    "-sice", "ICE12",
                    msiPath
                });

                Assert.Equal(1, validationResult.ExitCode);

                var messages = validationResult.Messages.Select(m => m.ToString()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only."
                }, messages);
            }
        }
    }
}
