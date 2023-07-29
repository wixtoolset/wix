// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class DirectoryFixture
    {
        [Fact]
        public void CanGetDefaultInstallFolder()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var dirSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.DirectorySymbol>().ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    "INSTALLFOLDER:ProgramFiles6432Folder:Example Corporation MsiPackage",
                    "ProgramFiles6432Folder:ProgramFilesFolder:.",
                    "ProgramFilesFolder:TARGETDIR:PFiles",
                    "TARGETDIR::SourceDir"
                }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
            }
        }

        [Fact]
        public void CanGet32bitProgramFiles6432Folder()
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
                    Path.Combine(folder, "Directory", "Empty.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var dirSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.DirectorySymbol>().ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    "INSTALLFOLDER:ProgramFiles6432Folder:MsiPackage",
                    "ProgramFiles6432Folder:ProgramFilesFolder:.",
                    "ProgramFilesFolder:TARGETDIR:PFiles",
                    "TARGETDIR::SourceDir"
                }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
            }
        }

        [Fact]
        public void CanGet64bitProgramFiles6432Folder()
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
                    "-arch", "x64",
                    Path.Combine(folder, "Directory", "Empty.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var dirSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.DirectorySymbol>().ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    "INSTALLFOLDER:ProgramFiles6432Folder:MsiPackage",
                    "ProgramFiles6432Folder:ProgramFiles64Folder:.",
                    "ProgramFiles64Folder:TARGETDIR:PFiles64",
                    "TARGETDIR::SourceDir"
                }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
            }
        }

        [Fact]
        public void CanGetDefaultName()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, "bin", "test.msi");
                var wixpdbPath = Path.Combine(baseFolder, "bin", "test.wixpdb");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Directory", "DefaultName.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(wixpdbPath);
                var section = intermediate.Sections.Single();

                var dirSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.DirectorySymbol>().ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    "BinFolder\tCompanyFolder\t.",
                    "CompanyFolder\tProgramFilesFolder\tExample Corporation",
                    "DesktopFolder\tTARGETDIR\tDesktop",
                    "ProgramFilesFolder\tTARGETDIR\tPFiles",
                    "ProgramMenuFolder\tTARGETDIR\tPMenu",
                    "TARGETDIR\t\tSourceDir"
                }, dirSymbols.Select(d => String.Join('\t', d.Id.Id, d.ParentDirectoryRef, d.Name)).OrderBy(s => s).ToArray());

                var data = WindowsInstallerData.Load(wixpdbPath);
                var directoryRows = data.Tables["Directory"].Rows;
                WixAssert.CompareLineByLine(new[]
                {
                    "BinFolder\tCompanyFolder\t.",
                    "CompanyFolder\tProgramFilesFolder\tu7-b4gch|Example Corporation",
                    "DesktopFolder\tTARGETDIR\tDesktop",
                    "ProgramFilesFolder\tTARGETDIR\tPFiles",
                    "ProgramMenuFolder\tTARGETDIR\tPMenu",
                    "TARGETDIR\t\tSourceDir"
                }, directoryRows.Select(r => String.Join('\t', r.FieldAsString(0), r.FieldAsString(1), r.FieldAsString(2))).OrderBy(s => s).ToArray());
            }
        }

        [Fact]
        public void CanGetDuplicateDir()
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
                    "-arch", "x64",
                    Path.Combine(folder, "DuplicateDir", "DuplicateDir.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var dirSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.DirectorySymbol>().ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    @"d6axmdFGwwNJUBTBpSSKcI7uWXo8:INSTALLFOLDER:path\to\path1",
                    @"dQ9mCRk.rZXStHc.ILz66dIhE0FI:INSTALLFOLDER:path\to\path2",
                    "INSTALLFOLDER:ProgramFiles6432Folder:MsiPackage",
                    "ProgramFiles6432Folder:ProgramFiles64Folder:.",
                    "ProgramFiles64Folder:TARGETDIR:PFiles64",
                    "TARGETDIR::SourceDir"
                }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
            }
        }

        [Fact]
        public void CanGetWithMultiNestedSubdirectory()
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
                    "-arch", "x64",
                    Path.Combine(folder, "Directory", "Nested.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var dirSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.DirectorySymbol>().ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    "BinFolder:ProgramFilesFolder:Example Corporation\\Test Product\\bin",
                    "ProgramFilesFolder:TARGETDIR:PFiles",
                    "TARGETDIR::SourceDir"
                }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());

                var data = WindowsInstallerData.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var directoryRows = data.Tables["Directory"].Rows;
                WixAssert.CompareLineByLine(new[]
                {
                    "d4EceYatXTyy8HXPt5B6DT9Rj.wE:ProgramFilesFolder:u7-b4gch|Example Corporation",
                    "dSJ1pgiASlW7kJTu0wqsGBklJsS0:d4EceYatXTyy8HXPt5B6DT9Rj.wE:vjj-gxay|Test Product",
                    "BinFolder:dSJ1pgiASlW7kJTu0wqsGBklJsS0:bin",
                    "ProgramFilesFolder:TARGETDIR:PFiles",
                    "TARGETDIR::SourceDir"
                }, directoryRows.Select(r => r.FieldAsString(0) + ":" + r.FieldAsString(1) + ":" + r.FieldAsString(2)).ToArray());
            }
        }

        [Fact]
        public void CanGetDuplicateTargetSourceName()
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
                    "-arch", "x64",
                    Path.Combine(folder, "Directory", "DuplicateTargetSourceName.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var section = intermediate.Sections.Single();

                var dirSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.DirectorySymbol>().ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    "BinFolder\tProgramFilesFolder\tbin",
                    "ProgramFilesFolder\tTARGETDIR\tPFiles",
                    "TARGETDIR\t\tSourceDir"
                }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => String.Join('\t', d.Id.Id, d.ParentDirectoryRef, d.Name)).ToArray());

                var data = WindowsInstallerData.Load(Path.Combine(baseFolder, @"bin\test.wixpdb"));
                var directoryRows = data.Tables["Directory"].Rows;
                WixAssert.CompareLineByLine(new[]
                {
                    "BinFolder\tProgramFilesFolder\tbin",
                    "ProgramFilesFolder\tTARGETDIR\tPFiles",
                    "TARGETDIR\t\tSourceDir"
                }, directoryRows.Select(r => String.Join('\t', r.FieldAsString(0), r.FieldAsString(1), r.FieldAsString(2))).ToArray());
            }
        }

        [Fact]
        public void CanFindRedundantSubdirectoryInSecondSection()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, "bin", "test.msi");
                var wixpdbPath = Path.ChangeExtension(msiPath, ".wixpdb");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Directory", "RedundantSubdirectoryInSecondSection.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(wixpdbPath);
                var section = intermediate.Sections.Single();

                var dirSymbols = section.Symbols.OfType<WixToolset.Data.Symbols.DirectorySymbol>().ToList();
                WixAssert.CompareLineByLine(new[]
                {
                    @"dKO7wPCF.XLmq6KnqybHHgcBBqtU:ProgramFilesFolder:a\b\c",
                    @"ProgramFilesFolder:TARGETDIR:PFiles",
                    @"TARGETDIR::SourceDir"
                }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).OrderBy(s => s).ToArray());

                var data = WindowsInstallerData.Load(wixpdbPath);
                var directoryRows = data.Tables["Directory"].Rows;
                WixAssert.CompareLineByLine(new[]
                {
                    @"d1nVb5_zcCwRCz7i2YXNAofGRmfc:ProgramFilesFolder:a",
                    @"dijlG.bNicFgvj1_DujiGg9EBGrQ:d1nVb5_zcCwRCz7i2YXNAofGRmfc:b",
                    @"dKO7wPCF.XLmq6KnqybHHgcBBqtU:dijlG.bNicFgvj1_DujiGg9EBGrQ:c",
                    "ProgramFilesFolder:TARGETDIR:PFiles",
                    "TARGETDIR::SourceDir"
                }, directoryRows.Select(r => r.FieldAsString(0) + ":" + r.FieldAsString(1) + ":" + r.FieldAsString(2)).OrderBy(s => s).ToArray());
            }
        }
    }
}
