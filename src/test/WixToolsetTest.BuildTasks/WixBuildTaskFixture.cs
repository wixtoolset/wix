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
                };

                var result = task.Execute();
                Assert.True(result, $"MSBuild task failed unexpectedly. Output:\r\n{engine.Output}");

                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\cab1.cab")));

                var intermediate = Intermediate.Load(Path.Combine(intermediateFolder, @"test.wir"));
                var section = intermediate.Sections.Single();

                var wixFile = section.Tuples.OfType<WixFileTuple>().Single();
                Assert.Equal(Path.Combine(folder, @"data\test.txt"), wixFile[WixFileTupleFields.Source].AsPath().Path);
                Assert.Equal(@"test.txt", wixFile[WixFileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }
    }
}
