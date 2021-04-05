// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class DirectoryFixture
    {
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
                Assert.Equal(new[]
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
                Assert.Equal(new[]
                {
                    "INSTALLFOLDER:ProgramFiles6432Folder:MsiPackage",
                    "ProgramFiles6432Folder:ProgramFiles64Folder:.",
                    "ProgramFiles64Folder:TARGETDIR:PFiles64",
                    "TARGETDIR::SourceDir"
                }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
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
                Assert.Equal(new[]
                {
                    "dZsSsu81KcG46xXTwc4mTSZO5Zx4:INSTALLFOLDER:dupe",
                    "INSTALLFOLDER:ProgramFiles6432Folder:MsiPackage",
                    "ProgramFiles6432Folder:ProgramFiles64Folder:.",
                    "ProgramFiles64Folder:TARGETDIR:PFiles64",
                    "TARGETDIR::SourceDir"
                }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
            }
        }
            }
        }
    }
}
