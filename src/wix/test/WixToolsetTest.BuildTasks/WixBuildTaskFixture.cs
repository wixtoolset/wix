// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BuildTasks
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Microsoft.Build.Utilities;
    using WixInternal.TestSupport;
    using WixToolset.BuildTasks;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class WixBuildTaskFixture
    {
        public static readonly string PublishedWixSdkToolsFolder = Path.Combine(Path.GetDirectoryName(new Uri(typeof(WixBuildTaskFixture).Assembly.CodeBase).LocalPath), "..", "..", "..", "publish", "WixToolset.Sdk", "tools");

        // This line replicates what happens in WixBuild task when hosted in the PublishedWixSdkToolsFolder. However, WixBuild task is hosted inproc to this test assembly so the
        // root folder is relative to the test assembly's folder which does not have wix.exe local. So, we have to find wix.exe relative to PublishedWixSdkToolsFolder.
        public static readonly string PublishedWixExeFolder = Path.Combine(PublishedWixSdkToolsFolder, "net472", RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant());

        [Fact]
        public void CanBuildSimpleMsiPackage()
        {
            var folder = TestData.Get("TestData", "SimpleMsiPackage", "MsiPackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var pdbPath = Path.Combine(baseFolder, "bin", "testpackage.wixpdb");
                var engine = new FakeBuildEngine();

                var task = new WixBuild
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
                    BindPaths = new[]
                    {
                        new TaskItem(Path.Combine(folder, "data")),
                    },
                    IntermediateDirectory = new TaskItem(intermediateFolder),
                    OutputFile = new TaskItem(Path.Combine(baseFolder, "bin", "test.msi")),
                    PdbType = "Full",
                    PdbFile = new TaskItem(pdbPath),
                    DefaultCompressionLevel = "nOnE",
                    AcceptEula = "wix" + SomeVerInfo.Major,
                    ToolPath = PublishedWixExeFolder
                };

                var result = task.Execute();
                Assert.True(result, $"MSBuild task failed unexpectedly. Output:\r\n{engine.Output}");

                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "test.msi")));
                Assert.True(File.Exists(pdbPath));
                Assert.True(File.Exists(Path.Combine(baseFolder, "bin", "cab1.cab")));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                WixAssert.StringEqual(Path.Combine(folder, "data", "test.txt"), fileSymbol[FileSymbolFields.Source].AsPath().Path);
                WixAssert.StringEqual(@"test.txt", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath().Path);
            }
        }
    }
}
