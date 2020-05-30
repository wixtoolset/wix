// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MSBuild
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Tuples;
    using Xunit;

    public class MsbuildHeatFixture
    {
        [Theory]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildHeatFilePackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\HeatFilePackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(baseFolder, "HeatFilePackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath);
                result.AssertSuccess();

                var heatCommandLines = result.Output.Where(line => line.TrimStart().StartsWith("heat.exe file"));
                Assert.Single(heatCommandLines);

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "x86", "Debug", "_ProductComponents_INSTALLFOLDER_HeatFilePackage.wixproj_file.wxs");
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

                var pdbPath = Path.Combine(binFolder, "x86", "Debug", "HeatFilePackage.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileTuple = section.Tuples.OfType<FileTuple>().Single();
                Assert.Equal(@"SourceDir\HeatFilePackage.wixproj", fileTuple[FileTupleFields.Source].PreviousValue.AsPath().Path);
            }
        }

        [Theory]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildHeatFileWithMultipleFilesPackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\HeatFileMultipleFilesSameFileName");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(baseFolder, "HeatFileMultipleFilesSameFileName.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath);
                result.AssertSuccess();

                var heatCommandLines = result.Output.Where(line => line.TrimStart().StartsWith("heat.exe file"));
                Assert.Equal(2, heatCommandLines.Count());

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "x86", "Debug", "_TxtProductComponents_INSTALLFOLDER_MyProgram.txt_file.wxs");
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

                generatedFilePath = Path.Combine(intermediateFolder, "x86", "Debug", "_JsonProductComponents_INSTALLFOLDER_MyProgram.json_file.wxs");
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

                var pdbPath = Path.Combine(binFolder, "x86", "Debug", "HeatFileMultipleFilesSameFileName.wixpdb");
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
