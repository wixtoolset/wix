// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BuildTasks
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.BuildTasks;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using Xunit;

    public class MsbuildHeatFixture
    {
        private static readonly string WixTargetsPath = Path.Combine(Path.GetDirectoryName(new Uri(typeof(HeatTask).Assembly.CodeBase).AbsolutePath), "wix.targets");

        [Fact]
        public void CanBuildHeatFilePackage()
        {
            var projectPath = TestData.Get(@"TestData\HeatFilePackage\HeatFilePackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}"
                });
                result.AssertSuccess();

                var heatCommandLines = result.Output.Where(line => line.TrimStart().StartsWith("heat.exe file"));
                Assert.Single(heatCommandLines);

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, @"_ProductComponents_INSTALLFOLDER_HeatFilePackage.wixproj_file.wxs");
                Assert.True(File.Exists(generatedFilePath));

                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                Assert.Equal(@"<Wix>" +
                    "<Fragment>" +
                    "<DirectoryRef Id='INSTALLFOLDER'>" +
                    "<Component Id='HeatFilePackage.wixproj' Guid='*'>" +
                    "<File Id='HeatFilePackage.wixproj' KeyPath='yes' Source='SourceDir\\HeatFilePackage.wixproj' />" +
                    "</Component>" +
                    "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                    "<ComponentGroup Id='ProductComponents'>" +
                    "<ComponentRef Id='HeatFilePackage.wixproj' />" +
                    "</ComponentGroup>" +
                    "</Fragment>" +
                    "</Wix>", testXml);

                var pdbPath = Path.Combine(binFolder, "HeatFilePackage.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                Assert.Equal(@"SourceDir\HeatFilePackage.wixproj", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Fact]
        public void CanBuildHeatFileWithMultipleFilesPackage()
        {
            var projectPath = TestData.Get(@"TestData\HeatFileMultpleFilesSameFileName\HeatFileMultpleFilesSameFileName.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}"
                });
                result.AssertSuccess();

                var heatCommandLines = result.Output.Where(line => line.TrimStart().StartsWith("heat.exe file"));
                Assert.Equal(2, heatCommandLines.Count());

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, @"_TxtProductComponents_INSTALLFOLDER_MyProgram.txt_file.wxs");
                Assert.True(File.Exists(generatedFilePath));

                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                Assert.Equal("<Wix>" +
                    "<Fragment>" +
                    "<DirectoryRef Id='INSTALLFOLDER'>" +
                    "<Component Id='MyProgram.txt' Guid='*'>" +
                    @"<File Id='MyProgram.txt' KeyPath='yes' Source='SourceDir\MyProgram.txt' />" +
                    "</Component>" +
                    "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                    "<ComponentGroup Id='TxtProductComponents'>" +
                    "<ComponentRef Id='MyProgram.txt' />" +
                    "</ComponentGroup>" +
                    "</Fragment>" +
                    "</Wix>", testXml);

                generatedFilePath = Path.Combine(intermediateFolder, @"_JsonProductComponents_INSTALLFOLDER_MyProgram.json_file.wxs");
                Assert.True(File.Exists(generatedFilePath));

                generatedContents = File.ReadAllText(generatedFilePath);
                testXml = generatedContents.GetTestXml();
                Assert.Equal("<Wix>" +
                    "<Fragment>" +
                    "<DirectoryRef Id='INSTALLFOLDER'>" +
                    "<Component Id='MyProgram.json' Guid='*'>" +
                    @"<File Id='MyProgram.json' KeyPath='yes' Source='SourceDir\MyProgram.json' />" +
                    "</Component>" +
                    "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                    "<ComponentGroup Id='JsonProductComponents'>" +
                    "<ComponentRef Id='MyProgram.json' />" +
                    "</ComponentGroup>" +
                    "</Fragment>" +
                    "</Wix>", testXml);

                var pdbPath = Path.Combine(binFolder, "HeatFileMultpleFilesSameFileName.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileTuples = section.Tuples.OfType<FileTuple>().ToArray();
                Assert.Equal(@"SourceDir\MyProgram.txt", fileTuples[0][FileTupleFields.Source].PreviousValue.AsPath().Path);
                Assert.Equal(@"SourceDir\MyProgram.json", fileTuples[1][FileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }
    }
}
