// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class MsbuildHeatFixture
    {
        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
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

                var heatCommandLines = MsbuildUtilities.GetToolCommandLines(result, "heat", "file", buildSystem, true);
                Assert.Single(heatCommandLines);

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "x86", "Release", "_ProductComponents_INSTALLFOLDER_HeatFilePackage.wixproj_file.wxs");
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

                var pdbPath = Path.Combine(binFolder, "x86", "Release", "HeatFilePackage.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                Assert.Equal(@"SourceDir\HeatFilePackage.wixproj", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath()?.Path);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
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

                var heatCommandLines = MsbuildUtilities.GetToolCommandLines(result, "heat", "file", buildSystem, true);
                Assert.Equal(2, heatCommandLines.Count());

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "x86", "Release", "_TxtProductComponents_INSTALLFOLDER_MyProgram.txt_file.wxs");
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

                generatedFilePath = Path.Combine(intermediateFolder, "x86", "Release", "_JsonProductComponents_INSTALLFOLDER_MyProgram.json_file.wxs");
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

                var pdbPath = Path.Combine(binFolder, "x86", "Release", "HeatFileMultipleFilesSameFileName.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbols = section.Symbols.OfType<FileSymbol>().ToArray();
                Assert.Equal(@"SourceDir\MyProgram.txt", fileSymbols[0][FileSymbolFields.Source].PreviousValue.AsPath()?.Path);
                Assert.Equal(@"SourceDir\MyProgram.json", fileSymbols[1][FileSymbolFields.Source].PreviousValue.AsPath()?.Path);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk, true)]
        [InlineData(BuildSystem.DotNetCoreSdk, false)]
        [InlineData(BuildSystem.MSBuild, true)]
        [InlineData(BuildSystem.MSBuild, false)]
        [InlineData(BuildSystem.MSBuild64, true)]
        [InlineData(BuildSystem.MSBuild64, false)]
        public void CanBuildHeatProjectPreSdkStyle(BuildSystem buildSystem, bool useToolsVersion)
        {
            var sourceFolder = TestData.Get(@"TestData\HeatProject");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "HeatProjectPreSdkStyle");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(baseFolder, "HeatProjectPreSdkStyle.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    useToolsVersion ? $"-p:HarvestProjectsUseToolsVersion=true" : String.Empty,
                });
                result.AssertSuccess();

                var heatCommandLines = MsbuildUtilities.GetToolCommandLines(result, "heat", "project", buildSystem, true);
                var heatCommandLine = Assert.Single(heatCommandLines);

                if (useToolsVersion && buildSystem != BuildSystem.DotNetCoreSdk)
                {
                    Assert.Contains("-usetoolsversion", heatCommandLine);
                }
                else
                {
                    Assert.DoesNotContain("-usetoolsversion", heatCommandLine);
                }

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "x86", "Release", "_ToolsVersion4Cs.wxs");
                Assert.True(File.Exists(generatedFilePath));

                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                Assert.Equal(@"<Wix>" +
                    "<Fragment>" +
                        "<DirectoryRef Id='ToolsVersion4Cs.Binaries'>" +
                            "<Component Id='ToolsVersion4Cs.Binaries.ToolsVersion4Cs.dll' Guid='*'>" +
                                "<File Id='ToolsVersion4Cs.Binaries.ToolsVersion4Cs.dll' Source='$(var.ToolsVersion4Cs.TargetDir)\\ToolsVersion4Cs.dll' />" +
                            "</Component>" +
                        "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='ToolsVersion4Cs.Binaries'>" +
                            "<ComponentRef Id='ToolsVersion4Cs.Binaries.ToolsVersion4Cs.dll' />" +
                        "</ComponentGroup>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<DirectoryRef Id='ToolsVersion4Cs.Symbols'>" +
                            "<Component Id='ToolsVersion4Cs.Symbols.ToolsVersion4Cs.pdb' Guid='*'>" +
                                "<File Id='ToolsVersion4Cs.Symbols.ToolsVersion4Cs.pdb' Source='$(var.ToolsVersion4Cs.TargetDir)\\ToolsVersion4Cs.pdb' />" +
                            "</Component>" +
                        "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='ToolsVersion4Cs.Symbols'>" +
                            "<ComponentRef Id='ToolsVersion4Cs.Symbols.ToolsVersion4Cs.pdb' />" +
                        "</ComponentGroup>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<DirectoryRef Id='ToolsVersion4Cs.Sources'>" +
                            "<Component Id='ToolsVersion4Cs.Sources.ToolsVersion4Cs.csproj' Guid='*'>" +
                                "<File Id='ToolsVersion4Cs.Sources.ToolsVersion4Cs.csproj' Source='$(var.ToolsVersion4Cs.ProjectDir)\\ToolsVersion4Cs.csproj' />" +
                            "</Component>" +
                            "<Directory Id='ToolsVersion4Cs.Sources.Properties' Name='Properties'>" +
                                "<Component Id='ToolsVersion4Cs.Sources.AssemblyInfo.cs' Guid='*'>" +
                                    "<File Id='ToolsVersion4Cs.Sources.AssemblyInfo.cs' Source='$(var.ToolsVersion4Cs.ProjectDir)\\Properties\\AssemblyInfo.cs' />" +
                                "</Component>" +
                            "</Directory>" +
                        "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='ToolsVersion4Cs.Sources'>" +
                            "<ComponentRef Id='ToolsVersion4Cs.Sources.ToolsVersion4Cs.csproj' />" +
                            "<ComponentRef Id='ToolsVersion4Cs.Sources.AssemblyInfo.cs' />" +
                        "</ComponentGroup>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='ToolsVersion4Cs.Content' />" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='ToolsVersion4Cs.Satellites' />" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='ToolsVersion4Cs.Documents' />" +
                    "</Fragment>" +
                    "</Wix>", testXml);

                var pdbPath = Path.Combine(binFolder, "x86", "Release", "HeatProjectPreSdkStyle.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                Assert.Equal(Path.Combine(fs.BaseFolder, "ToolsVersion4Cs", "bin", "Release\\\\ToolsVersion4Cs.dll"), fileSymbol[FileSymbolFields.Source].AsPath()?.Path);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk, true)]
        [InlineData(BuildSystem.DotNetCoreSdk, false)]
        [InlineData(BuildSystem.MSBuild, true)]
        [InlineData(BuildSystem.MSBuild, false)]
        [InlineData(BuildSystem.MSBuild64, true)]
        [InlineData(BuildSystem.MSBuild64, false)]
        public void CanBuildHeatProjectSdkStyle(BuildSystem buildSystem, bool useToolsVersion)
        {
            var sourceFolder = TestData.Get(@"TestData\HeatProject");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "HeatProjectSdkStyle");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(baseFolder, "HeatProjectSdkStyle.wixproj");
                var referencedProjectPath = Path.Combine(fs.BaseFolder, "SdkStyleCs", "SdkStyleCs.csproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, referencedProjectPath, new[]
                {
                    "-t:restore",
                });
                result.AssertSuccess();

                result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    useToolsVersion ? $"-p:HarvestProjectsUseToolsVersion=true" : String.Empty,
                });
                result.AssertSuccess();

                var heatCommandLines = MsbuildUtilities.GetToolCommandLines(result, "heat", "project", buildSystem, true);
                var heatCommandLine = Assert.Single(heatCommandLines);

                if (useToolsVersion && buildSystem != BuildSystem.DotNetCoreSdk)
                {
                    Assert.Contains("-usetoolsversion", heatCommandLine);
                }
                else
                {
                    Assert.DoesNotContain("-usetoolsversion", heatCommandLine);
                }

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "x86", "Release", "_SdkStyleCs.wxs");
                Assert.True(File.Exists(generatedFilePath));

                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                Assert.Equal(@"<Wix>" +
                    "<Fragment>" +
                        "<DirectoryRef Id='SdkStyleCs.Binaries'>" +
                            "<Component Id='SdkStyleCs.Binaries.SdkStyleCs.dll' Guid='*'>" +
                                "<File Id='SdkStyleCs.Binaries.SdkStyleCs.dll' Source='$(var.SdkStyleCs.TargetDir)\\SdkStyleCs.dll' />" +
                            "</Component>" +
                        "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='SdkStyleCs.Binaries'>" +
                            "<ComponentRef Id='SdkStyleCs.Binaries.SdkStyleCs.dll' />" +
                        "</ComponentGroup>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<DirectoryRef Id='SdkStyleCs.Symbols'>" +
                            "<Component Id='SdkStyleCs.Symbols.SdkStyleCs.pdb' Guid='*'>" +
                                "<File Id='SdkStyleCs.Symbols.SdkStyleCs.pdb' Source='$(var.SdkStyleCs.TargetDir)\\SdkStyleCs.pdb' />" +
                            "</Component>" +
                        "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='SdkStyleCs.Symbols'>" +
                            "<ComponentRef Id='SdkStyleCs.Symbols.SdkStyleCs.pdb' />" +
                        "</ComponentGroup>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<DirectoryRef Id='SdkStyleCs.Sources'>" +
                            "<Component Id='SdkStyleCs.Sources.SdkStyleCs.cs' Guid='*'>" +
                                "<File Id='SdkStyleCs.Sources.SdkStyleCs.cs' Source='$(var.SdkStyleCs.ProjectDir)\\SdkStyleCs.cs' />" +
                            "</Component>" +
                            "<Component Id='SdkStyleCs.Sources.SdkStyleCs.csproj' Guid='*'>" +
                                "<File Id='SdkStyleCs.Sources.SdkStyleCs.csproj' Source='$(var.SdkStyleCs.ProjectDir)\\SdkStyleCs.csproj' />" +
                            "</Component>" +
                        "</DirectoryRef>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='SdkStyleCs.Sources'>" +
                            "<ComponentRef Id='SdkStyleCs.Sources.SdkStyleCs.cs' />" +
                            "<ComponentRef Id='SdkStyleCs.Sources.SdkStyleCs.csproj' />" +
                        "</ComponentGroup>" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='SdkStyleCs.Content' />" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='SdkStyleCs.Satellites' />" +
                    "</Fragment>" +
                    "<Fragment>" +
                        "<ComponentGroup Id='SdkStyleCs.Documents' />" +
                    "</Fragment>" +
                    "</Wix>", testXml);

                var pdbPath = Path.Combine(binFolder, "x86", "Release", "HeatProjectSdkStyle.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                Assert.Equal(Path.Combine(fs.BaseFolder, "SdkStyleCs", "bin", "Release", "netstandard2.0\\\\SdkStyleCs.dll"), fileSymbol[FileSymbolFields.Source].AsPath()?.Path);
            }
        }
    }
}
