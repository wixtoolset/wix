// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Sdk
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using Xunit;

    public class MsbuildHeatFixture
    {
        public static readonly string HeatTargetsPath = Path.Combine(Path.GetDirectoryName(new Uri(typeof(MsbuildHeatFixture).Assembly.CodeBase).AbsolutePath), "..", "..", "..", "publish", "WixToolset.Heat", "build", "WixToolset.Heat.targets");

        public MsbuildHeatFixture()
        {
            EnsureWixSdkCached();
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildHeatFilePackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get("TestData", "HeatFilePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(sourceFolder, "HeatFilePackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    "-Restore",
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "HeatTargetsPath", MsbuildHeatFixture.HeatTargetsPath),
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "BaseIntermediateOutputPath", intermediateFolder),
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "OutputPath", binFolder),
                });
                result.AssertSuccess();

                var heatCommandLines = MsbuildUtilities.GetToolCommandLines(result, "heat", "file", buildSystem, true);
                Assert.Single(heatCommandLines);

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "Release", "_ProductComponents_INSTALLFOLDER_HeatFilePackage.wixproj_file.wxs");
                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                WixAssert.StringEqual(@"<Wix>" +
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
                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                WixAssert.StringEqual(@"SourceDir\HeatFilePackage.wixproj", fileSymbol[FileSymbolFields.Source].PreviousValue.AsPath()?.Path);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildHeatFileWithMultipleFilesPackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "HeatFileMultipleFilesSameFileName");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(sourceFolder, "HeatFileMultipleFilesSameFileName.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    "-Restore",
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "HeatTargetsPath", MsbuildHeatFixture.HeatTargetsPath),
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "BaseIntermediateOutputPath", intermediateFolder),
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "OutputPath", binFolder),
                });
                result.AssertSuccess();

                var heatCommandLines = MsbuildUtilities.GetToolCommandLines(result, "heat", "file", buildSystem, true);
                Assert.Equal(2, heatCommandLines.Count());

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "Release", "_TxtProductComponents_INSTALLFOLDER_MyProgram.txt_file.wxs");
                Assert.True(File.Exists(generatedFilePath));

                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                WixAssert.StringEqual("<Wix>" +
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

                generatedFilePath = Path.Combine(intermediateFolder, "Release", "_JsonProductComponents_INSTALLFOLDER_MyProgram.json_file.wxs");
                Assert.True(File.Exists(generatedFilePath));

                generatedContents = File.ReadAllText(generatedFilePath);
                testXml = generatedContents.GetTestXml();
                WixAssert.StringEqual("<Wix>" +
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

                var pdbPath = Path.Combine(binFolder, "HeatFileMultipleFilesSameFileName.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbols = section.Symbols.OfType<FileSymbol>().ToArray();
                WixAssert.StringEqual(@"SourceDir\MyProgram.txt", fileSymbols[0][FileSymbolFields.Source].PreviousValue.AsPath()?.Path);
                WixAssert.StringEqual(@"SourceDir\MyProgram.json", fileSymbols[1][FileSymbolFields.Source].PreviousValue.AsPath()?.Path);
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
            var sourceFolder = TestData.Get(@"TestData", "HeatProject");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                File.Copy("global.json", Path.Combine(fs.BaseFolder, "global.json"));

                var baseFolder = Path.Combine(fs.BaseFolder, "HeatProjectPreSdkStyle");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(baseFolder, "HeatProjectPreSdkStyle.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "HeatTargetsPath", MsbuildHeatFixture.HeatTargetsPath),
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

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "Release", "_ToolsVersion4Cs.wxs");
                Assert.True(File.Exists(generatedFilePath));

                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                WixAssert.StringEqual(@"<Wix>" +
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

                var pdbPath = Path.Combine(binFolder, "Release", "HeatProjectPreSdkStyle.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                WixAssert.StringEqual(Path.Combine(fs.BaseFolder, "ToolsVersion4Cs", "bin", "Release\\\\ToolsVersion4Cs.dll"), fileSymbol[FileSymbolFields.Source].AsPath()?.Path);
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
                File.Copy("global.json", Path.Combine(fs.BaseFolder, "global.json"));

                var baseFolder = Path.Combine(fs.BaseFolder, "HeatProjectSdkStyle");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");
                var projectPath = Path.Combine(fs.BaseFolder, "HeatProjectSdkStyle", "HeatProjectSdkStyle.wixproj");
                var referencedProjectPath = Path.Combine(fs.BaseFolder, "SdkStyleCs", "SdkStyleCs.csproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, referencedProjectPath, new[]
                {
                     "-t:restore",
                     MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "HeatTargetsPath", MsbuildHeatFixture.HeatTargetsPath),
                 });
                result.AssertSuccess();

                result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                     MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "HeatTargetsPath", MsbuildHeatFixture.HeatTargetsPath),
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

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var generatedFilePath = Path.Combine(intermediateFolder, "Release", "_SdkStyleCs.wxs");
                Assert.True(File.Exists(generatedFilePath));

                var generatedContents = File.ReadAllText(generatedFilePath);
                var testXml = generatedContents.GetTestXml();
                WixAssert.StringEqual(@"<Wix>" +
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

                var pdbPath = Path.Combine(binFolder, "Release", "HeatProjectSdkStyle.wixpdb");
                Assert.True(File.Exists(pdbPath));

                var intermediate = Intermediate.Load(pdbPath);
                var section = intermediate.Sections.Single();

                var fileSymbol = section.Symbols.OfType<FileSymbol>().Single();
                WixAssert.StringEqual(Path.Combine(fs.BaseFolder, "SdkStyleCs", "bin", "Release", "netstandard2.0\\\\SdkStyleCs.dll"), fileSymbol[FileSymbolFields.Source].AsPath()?.Path);
            }
        }

        /// <summary>
        /// This method exists to get the WixToolset.Sdk.nupkg into the NuGet package cache using the global.json
        /// and nuget.config in the root of the repository. By pre-caching the WiX SDK, the rest of the tests will
        /// pull the binaries out of the cache instead of needing to find the original .nupkg in the build artifacts
        /// folder (which requires use of nuget.config found in the root of the repo)
        /// </summary>
        private static void EnsureWixSdkCached()
        {
            // This EnsureWixSdkCached project exists only to pre-cache the WixToolset.Sdk for use by later projects.
            var sourceFolder = TestData.Get("TestData", "EnsureWixSdkCached");

            var result = MsbuildUtilities.BuildProject(BuildSystem.DotNetCoreSdk, Path.Combine(sourceFolder, "EnsureWixSdkCached.wixproj"), new[]
            {
                "-t:restore",
            });
            result.AssertSuccess();
        }
    }
}
