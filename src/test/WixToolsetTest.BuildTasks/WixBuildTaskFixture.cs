// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BuildTasks
{
    using System.IO;
    using System.Linq;
    using Microsoft.Build.Utilities;
    using WixBuildTools.TestSupport;
    using WixToolset.BuildTasks;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using Xunit;

    public class WixBuildTaskFixture
    {
        [Fact]
        public void CanBuildSimpleMsiPackage()
        {
            var folder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var pdbPath = Path.Combine(baseFolder, @"bin\testpackage.wixpdb");
                var engine = new FakeBuildEngine();

                var task = new DoIt
                {
                    BuildEngine = engine,
                    SourceFiles = new[]
                    {
                        new TaskItem(Path.Combine(folder, "Package.wxs")),
                        new TaskItem(Path.Combine(folder, "PackageComponents.wxs")),
                    },
                    LocalizationFiles = new[]
                    {
                        new TaskItem(Path.Combine(folder, "Package.en-us.wxl")),
                    },
                    BindInputPaths = new[]
                    {
                        new TaskItem(Path.Combine(folder, "data")),
                    },
                    IntermediateDirectory = new TaskItem(intermediateFolder),
                    OutputFile = new TaskItem(Path.Combine(baseFolder, @"bin\test.msi")),
                    PdbType = "Full",
                    PdbFile = new TaskItem(pdbPath),
                };

                var result = task.Execute();
                Assert.True(result, $"MSBuild task failed unexpectedly. Output:\r\n{engine.Output}");

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.msi")));
                Assert.True(File.Exists(pdbPath));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\cab1.cab")));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), fileTuple[FileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact(Skip = "Requires deleting wixnative.exe from output folder after build but before running the test.")]
        public void ReportsInnerExceptionForUnexpectedExceptions()
        {
            var folder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var pdbPath = Path.Combine(baseFolder, @"bin\testpackage.wixpdb");
                var engine = new FakeBuildEngine();

                var task = new DoIt
                {
                    BuildEngine = engine,
                    SourceFiles = new[]
                    {
                        new TaskItem(Path.Combine(folder, "Package.wxs")),
                        new TaskItem(Path.Combine(folder, "PackageComponents.wxs")),
                    },
                    LocalizationFiles = new[]
                    {
                        new TaskItem(Path.Combine(folder, "Package.en-us.wxl")),
                    },
                    BindInputPaths = new[]
                    {
                        new TaskItem(Path.Combine(folder, "data")),
                    },
                    IntermediateDirectory = new TaskItem(intermediateFolder),
                    OutputFile = new TaskItem(Path.Combine(baseFolder, @"bin\test.msi")),
                    PdbType = "Full",
                    PdbFile = new TaskItem(pdbPath),
                };

                var result = task.Execute();
                Assert.False(result, $"MSBuild task succeeded unexpectedly. Output:\r\n{engine.Output}");

                Assert.Contains(
                    "System.PlatformNotSupportedException: Could not find platform specific 'wixnative.exe' ---> System.IO.FileNotFoundException: Could not find internal piece of WiX Toolset from",
                    engine.Output);
            }
        }
    }
}
