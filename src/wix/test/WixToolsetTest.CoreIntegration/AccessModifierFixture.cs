// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class AccessModifierFixture
    {
        [Fact]
        public void CanCompileVirtualSymbol()
        {
            var dirSymbols = BuildToGetDirectorySymbols("TestData", "AccessModifier", "HasVirtualSymbol.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "virtual:ProgramFilesFolder:TARGETDIR:PFiles",
                "virtual:TARGETDIR::SourceDir",
                "virtual:TestFolder:ProgramFilesFolder:Test Folder",
            }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Access.AsString() + ":" + d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
        }

        [Fact]
        public void CanCompileOverrideVirtualSymbol()
        {
            var dirSymbols = BuildToGetDirectorySymbols("TestData", "AccessModifier", "OverrideVirtualSymbol.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "virtual:ProgramFilesFolder:TARGETDIR:PFiles",
                "virtual:TARGETDIR::SourceDir",
                "override:TestFolder:ProgramFilesFolder:Override Test Folder",
            }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Access.AsString() + ":" + d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
        }

        [Fact]
        public void CanCompileVirtualSymbolOverridden()
        {
            var dirSymbols = BuildToGetDirectorySymbols("TestData", "AccessModifier", "VirtualSymbolOverridden.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "virtual:ProgramFilesFolder:TARGETDIR:PFiles",
                "virtual:TARGETDIR::SourceDir",
                "override:TestFolder:ProgramFilesFolder:Test Folder Overrode Virtual",
            }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Access.AsString() + ":" + d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
        }

        [Fact]
        public void CanCompileVirtualSymbolWithFragments()
        {
            var dirSymbols = BuildToGetDirectorySymbols("TestData", "AccessModifier", "OverrideVirtualSymbolWithFragments.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "global:AlsoIncluded:ProgramFilesFolder:Also Included",
                "virtual:ProgramFilesFolder:TARGETDIR:PFiles",
                "virtual:TARGETDIR::SourceDir",
                "override:TestFolder:ProgramFilesFolder:Override Test Folder Includes Another",
            }, dirSymbols.OrderBy(d => d.Id.Id).Select(d => d.Id.Access.AsString() + ":" + d.Id.Id + ":" + d.ParentDirectoryRef + ":" + d.Name).ToArray());
        }

        [Fact]
        public void CannotCompileInvalidCrossFragmentReference()
        {
            var errors = BuildForFailure("TestData", "AccessModifier", "InvalidCrossFragmentReference.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "ln 4: The identifier 'Directory:Foo' is inaccessible due to its protection level.",
            }, errors);
        }

        [Fact]
        public void CannotCompileDuplicateCrossFragmentReference()
        {
            var errors = BuildForFailure("TestData", "AccessModifier", "DuplicateCrossFragmentReference.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "ln 12: Duplicate Directory with identifier 'TestFolder' referenced by <sourceFolder>\\DuplicateCrossFragmentReference.wxs(4). Ensure all your identifiers of a given type (Directory, File, etc.) are unique or use an access modifier to scope the identfier.",
                "ln 8: Location of symbol related to previous error."
            }, errors);
        }

        [Fact]
        public void CannotCompileOverrideWithoutVirtualSymbol()
        {
            var errors = BuildForFailure("TestData", "AccessModifier", "OverrideWithoutVirtualSymbol.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "ln 5: Could not find a virtual symbol to override with the Directory symbol 'TestFolder'. Remove the override access modifier or include the code with the virtual symbol.",
            }, errors);
        }

        [Fact]
        public void CannotCompileDuplicatedOverride()
        {
            var errors = BuildForFailure("TestData", "AccessModifier", "DuplicatedOverrideVirtualSymbol.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "ln 14: Duplicate Directory with identifier 'TestFolder' found. Access modifiers (global, library, file, section) cannot prevent these conflicts. Ensure all your identifiers of a given type (Directory, File, etc.) are unique.",
                "ln 6: Location of symbol related to previous error."
            }, errors);
        }

        [Fact]
        public void CannotCompileDuplicatedVirtual()
        {
            var errors = BuildForFailure("TestData", "AccessModifier", "DuplicatedVirtualSymbol.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "ln 6: The virtual Directory with identifier 'TestFolder' is duplicated. Ensure identifiers of a given type (Directory, File, etc.) are unique or did you mean to make one an override for the virtual symbol?",
                "ln 10: Location of symbol related to previous error."
            }, errors);
        }

        [Fact]
        public void CannotCompilePublicWithVirtualSymbol()
        {
            var errors = BuildForFailure("TestData", "AccessModifier", "VirtualSymbolWithoutOverride.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "ln 9: The Directory symbol 'TestFolder' conflicts with a virtual symbol. Use the 'override' access modifier to override the virtual symbol or use a different Id to avoid the conflict.",
                "ln 5: Location of symbol related to previous error."
            }, errors);
        }

        [Fact]
        public void CannotCompilePublicAndOverrideWithVirtualSymbol()
        {
            var errors = BuildForFailure("TestData", "AccessModifier", "DuplicatePublicOverrideVirtualSymbol.wxs");
            WixAssert.CompareLineByLine(new[]
            {
                "ln 14: Duplicate Directory with identifier 'TestFolder' found. Access modifiers (global, library, file, section) cannot prevent these conflicts. Ensure all your identifiers of a given type (Directory, File, etc.) are unique.",
                "ln 6: Location of symbol related to previous error."
            }, errors);
        }

        private static string[] BuildForFailure(params string[] testSourceFilePaths)
        {
            var sourceFile = TestData.Get(testSourceFilePaths);
            var sourceFolder = Path.GetDirectoryName(sourceFile);

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, "bin", "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    sourceFile,
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                return result.Messages.Where(m => m.Level == MessageLevel.Error)
                                      .Select(m => $"ln {m.SourceLineNumbers.LineNumber}: {m}".Replace(sourceFolder, "<sourceFolder>").Replace(baseFolder, "<baseFolder>"))
                                      .ToArray();
            }
        }

        private static List<DirectorySymbol> BuildToGetDirectorySymbols(params string[] testSourceFilePaths)
        {
            var sourceFile = TestData.Get(testSourceFilePaths);

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, "bin", "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    sourceFile,
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var intermediate = Intermediate.Load(Path.Combine(baseFolder, "bin", "test.wixpdb"));
                var section = intermediate.Sections.Single();

                return section.Symbols.OfType<WixToolset.Data.Symbols.DirectorySymbol>().ToList();
            }
        }
    }
}
