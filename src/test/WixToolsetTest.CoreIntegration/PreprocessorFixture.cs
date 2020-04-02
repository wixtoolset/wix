// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Extensibility.Data;
    using Xunit;

    public class PreprocessorFixture
    {
        [Fact]
        public void PreprocessDirectly()
        {
            var folder = TestData.Get(@"TestData\IncludePath");
            var sourcePath = Path.Combine(folder, "Package.wxs");
            var includeFolder = Path.Combine(folder, "data");
            var includeFile = Path.Combine(includeFolder, "Package.wxi");

            var serviceProvider = new WixToolsetServiceProvider();

            var context = serviceProvider.GetService<IPreprocessContext>();
            context.SourcePath = sourcePath;
            context.IncludeSearchPaths = new[] { includeFolder };

            var preprocessor = serviceProvider.GetService<IPreprocessor>();
            var result = preprocessor.Preprocess(context);

            var includedFile = result.IncludedFiles.Single();
            Assert.NotNull(result.Document);
            Assert.Equal(includeFile, includedFile.Path);
            Assert.Equal(sourcePath, includedFile.SourceLineNumbers.FileName);
            Assert.Equal(2, includedFile.SourceLineNumbers.LineNumber.Value);
            Assert.Equal($"{sourcePath}*2", includedFile.SourceLineNumbers.QualifiedFileName);
            Assert.Null(includedFile.SourceLineNumbers.Parent);
        }

        [Fact]
        public void VariableRedefinitionIsAWarning()
        {
            var folder = TestData.Get(@"TestData\Variables");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();

                var warning = result.Messages.Where(message => message.Id == (int)WarningMessages.Ids.VariableDeclarationCollision);
                Assert.Single(warning);
            }
        }

        [Fact]
        public void ForEachLoopsWork()
        {
            var folder = TestData.Get(@"TestData\ForEach");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                result.AssertSuccess();
            }
        }

        [Fact]
        public void NonterminatedPreprocessorInstructionShowsSourceLineNumber()
        {
            var folder = TestData.Get(@"TestData\BadIf");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                Assert.Equal(147, result.ExitCode);
                Assert.StartsWith("Found a <?if?>", result.Messages.Single().ToString());
            }
        }
    }
}

