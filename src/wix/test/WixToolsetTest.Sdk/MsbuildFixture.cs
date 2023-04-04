// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using Xunit;

    public class MsbuildFixture
    {
        public static readonly string WixMsbuildPath = Path.Combine(Path.GetDirectoryName(new Uri(typeof(MsbuildFixture).Assembly.CodeBase).LocalPath), "..", "..", "..", "publish", "WixToolset.Sdk");
        public static readonly string WixPropsPath = Path.Combine(WixMsbuildPath, "Sdk", "Sdk.props");

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSimpleBundle(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "SimpleMsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "SimpleBundle");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "SimpleBundle.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    "-p:SignOutput=true",
                    });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var testMessages = result.Output.Where(line => line.Contains("TEST:")).Select(line => line.Trim()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"TEST: SignBundleEngine: obj\x86\Release\SimpleBundle-engine.exe",
                    @"TEST: SignBundle: obj\x86\Release\SimpleBundle.exe",
                }, testMessages);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\x86\Release\SimpleBundle.exe",
                    @"bin\x86\Release\SimpleBundle.wixpdb",
                }, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildUncompressedBundle(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get("TestData", "SimpleMsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "UncompressedBundle");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "UncompressedBundle.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    "-p:SignOutput=true"
                });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var testMessages = result.Output.Where(line => line.Contains("TEST:")).Select(line => line.Trim()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"TEST: SignBundleEngine: obj\x86\Release\UncompressedBundle-engine.exe",
                }, testMessages);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\x86\Release\test.txt",
                    @"bin\x86\Release\UncompressedBundle.exe",
                    @"bin\x86\Release\UncompressedBundle.wixpdb",
                }, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSimpleMergeModule(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get("TestData", "MergeModule", "SimpleMergeModule");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "SimpleMergeModule.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath)
                });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\x86\Release\SimpleMergeModule.msm",
                    @"bin\x86\Release\SimpleMergeModule.wixpdb",
                }, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSimpleMsiPackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            var baseFolder = String.Empty;

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                baseFolder = fs.BaseFolder;

                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    "-p:SignOutput=true"
                });
                result.AssertSuccess();

                var platformSwitches = result.Output.Where(line => line.Contains("-platform x86"));
                Assert.Single(platformSwitches);

                var warnings = result.Output.Where(line => line.Contains(": warning")).Select(line => ExtractWarningFromMessage(line, baseFolder)).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"WIX1118: The variable 'Variable' with value 'DifferentValue' was previously declared with value 'Value'.",
                    @"WIX1102: The DefaultLanguage '1033' was used for file 'filcV1yrx0x8wJWj4qMzcH21jwkPko' which has no language or version. For unversioned files, specifying a value for DefaultLanguage is not neccessary and it will not be used when determining file versions. Remove the DefaultLanguage attribute to eliminate this warning.",
                    @"WIX1122: The installer database '<basefolder>\obj\x86\Release\en-US\MsiPackage.msi' has external cabs, but at least one of them is not signed. Please ensure that all external cabs are signed, if you mean to sign them. If you don't mean to sign them, there is no need to inscribe the MSI as part of your build.",
                    @"WIX1118: The variable 'Variable' with value 'DifferentValue' was previously declared with value 'Value'.",
                    @"WIX1102: The DefaultLanguage '1033' was used for file 'filcV1yrx0x8wJWj4qMzcH21jwkPko' which has no language or version. For unversioned files, specifying a value for DefaultLanguage is not neccessary and it will not be used when determining file versions. Remove the DefaultLanguage attribute to eliminate this warning.",
                    @"WIX1122: The installer database '<basefolder>\obj\x86\Release\en-US\MsiPackage.msi' has external cabs, but at least one of them is not signed. Please ensure that all external cabs are signed, if you mean to sign them. If you don't mean to sign them, there is no need to inscribe the MSI as part of your build."
                }, warnings);

                var testMessages = result.Output.Where(line => line.Contains("TEST:")).Select(line => ReplacePathsInMessage(line, baseFolder)).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"TEST: SignCabs: <basefolder>\obj\x86\Release\en-US\cab1.cab",
                    @"TEST: SignMsi: obj\x86\Release\en-US\MsiPackage.msi",
                }, testMessages);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\x86\Release\en-US\cab1.cab",
                    @"bin\x86\Release\en-US\MsiPackage.msi",
                    @"bin\x86\Release\en-US\MsiPackage.wixpdb",
                }, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSimpleMsiPackageWithMergeModule(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "MergeModule");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "MergeMsiPackage");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MergeMsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath)
                });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\x86\Release\cab1.cab",
                    @"bin\x86\Release\MergeMsiPackage.msi",
                    @"bin\x86\Release\MergeMsiPackage.wixpdb",
                }, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildMsiPackageWithBindVariables(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get("TestData", "MsiPackageWithBindVariables");

            var baseFolder = String.Empty;

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                baseFolder = fs.BaseFolder;

                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackageWithBindVariables.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath)
                });
                result.AssertSuccess();

                var trackingContents = File.ReadAllLines(Path.Combine(baseFolder, "obj", "Release", "MsiPackageWithBindVariables.wixproj.BindTracking-neutral.txt"));
                var lines = trackingContents.Select(l => l.Replace(baseFolder, "<basefolder>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "BuiltContentOutput\t<basefolder>\\obj\\Release\\cab1.cab",
                    "BuiltPdbOutput\t<basefolder>\\obj\\Release\\MsiPackageWithBindVariables.wixpdb",
                    "BuiltTargetOutput\t<basefolder>\\obj\\Release\\MsiPackageWithBindVariables.msi",
                    "Input\tdata\\test.txt",
                    "Intermediate\t<basefolder>\\obj\\Release\\cab1.cab"
                }, lines);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\Release\cab1.cab",
                    @"bin\Release\MsiPackageWithBindVariables.msi",
                    @"bin\Release\MsiPackageWithBindVariables.wixpdb",
                }, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildWithDefaultAndExplicitlyFullWixpdbs(BuildSystem buildSystem)
        {
            var expectedOutputs = new[]
                {
                    @"bin\x86\Release\en-US\cab1.cab",
                    @"bin\x86\Release\en-US\MsiPackage.msi",
                    @"bin\x86\Release\en-US\MsiPackage.wixpdb",
                };

            this.AssertWixpdb(buildSystem, null, expectedOutputs);
            this.AssertWixpdb(buildSystem, "Full", expectedOutputs);
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildWithNoWixpdb(BuildSystem buildSystem)
        {
            this.AssertWixpdb(buildSystem, "NONE", new[]
                {
                    @"bin\x86\Release\en-US\cab1.cab",
                    @"bin\x86\Release\en-US\MsiPackage.msi",
                });
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildWithWixpdbToDifferentFolder(BuildSystem buildSystem)
        {
            var expectedOutputFiles = new[]
            {
                @"bin\x86\Release\en-US\cab1.cab",
                @"bin\x86\Release\en-US\MsiPackage.msi",
                @"pdb\en-US\MsiPackage.wixpdb",
            };

            var sourceFolder = TestData.Get(@"TestData", "SimpleMsiPackage", "MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var pdbFolder = Path.Combine(baseFolder, @"pdb\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "PdbOutputDir", pdbFolder),
                    "-p:SuppressValidation=true"
                });
                result.AssertSuccess();

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Concat(Directory.EnumerateFiles(pdbFolder, @"*.*", SearchOption.AllDirectories))
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(expectedOutputFiles, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuild64BitMsiPackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    $"-p:Platform=x64",
                });
                result.AssertSuccess();

                var platformSwitches = result.Output.Where(line => line.Contains("-platform x64"));
                Assert.Single(platformSwitches);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\x64\Release\en-US\cab1.cab",
                    @"bin\x64\Release\en-US\MsiPackage.msi",
                    @"bin\x64\Release\en-US\MsiPackage.wixpdb",
                }, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildMsiPackageWithIceSuppressions(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get("TestData", "MsiPackageWithIceError", "MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "SuppressIces", "ICE12"),
                }, suppressValidation: false);
                result.AssertSuccess();
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSimpleMsiPackageWithWarningSuppressions(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get("TestData", "SimpleMsiPackage", "MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "SuppressSpecificWarnings", "1118;1102"),
                });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSingleCultureWithFallbackMsiPackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "SingleCultureWithFallbackMsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "SingleCultureWithFallbackMsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath)
                });
                result.AssertSuccess();

                var trackingContents = File.ReadAllLines(Path.Combine(baseFolder, "obj", "Release", "SingleCultureWithFallbackMsiPackage.wixproj.BindTracking-de-DE.txt"));
                var lines = trackingContents.Select(l => l.Replace(baseFolder, "<basefolder>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "BuiltContentOutput\t<basefolder>\\obj\\Release\\de-DE\\cab1.cab",
                    "BuiltPdbOutput\t<basefolder>\\obj\\Release\\de-DE\\SingleCultureWithFallbackMsiPackage.wixpdb",
                    "BuiltTargetOutput\t<basefolder>\\obj\\Release\\de-DE\\SingleCultureWithFallbackMsiPackage.msi",
                    "Input\t<basefolder>\\data\\test.txt",
                    "Intermediate\t<basefolder>\\obj\\Release\\de-DE\\cab1.cab"
                }, lines);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildMultiCulturalMsiPackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "MultiCulturalMsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var slnPath = Path.Combine(baseFolder, "MultiCulturalMsiPackage.sln");
                var projectFolder = Path.Combine(baseFolder, "MsiPackage");

                var result = MsbuildUtilities.BuildProject(buildSystem, slnPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath)
                });
                result.AssertSuccess();

                var trackingEnuContents = File.ReadAllLines(Path.Combine(projectFolder, "obj", "x64", "Release", "MsiPackage.wixproj.BindTracking-en-US.txt"));
                var enuLines = trackingEnuContents.Select(l => l.Replace(projectFolder, "<basefolder>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "BuiltContentOutput\t<basefolder>\\obj\\x64\\Release\\en-US\\cab1.cab",
                    "BuiltPdbOutput\t<basefolder>\\obj\\x64\\Release\\en-US\\MsiPackage.wixpdb",
                    "BuiltTargetOutput\t<basefolder>\\obj\\x64\\Release\\en-US\\MsiPackage.msi",
                    "Input\t<basefolder>\\data\\test.txt",
                    "Intermediate\t<basefolder>\\obj\\x64\\Release\\en-US\\cab1.cab"
                }, enuLines);

                var trackingDeuContents = File.ReadAllLines(Path.Combine(projectFolder, "obj", "x64", "Release", "MsiPackage.wixproj.BindTracking-de-DE.txt"));
                var deuLines = trackingDeuContents.Select(l => l.Replace(projectFolder, "<basefolder>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "BuiltContentOutput\t<basefolder>\\obj\\x64\\Release\\de-DE\\cab1.cab",
                    "BuiltPdbOutput\t<basefolder>\\obj\\x64\\Release\\de-DE\\MsiPackage.wixpdb",
                    "BuiltTargetOutput\t<basefolder>\\obj\\x64\\Release\\de-DE\\MsiPackage.msi",
                    "Input\t<basefolder>\\data\\test.txt",
                    "Intermediate\t<basefolder>\\obj\\x64\\Release\\de-DE\\cab1.cab"
                }, deuLines);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSimpleMsiPackageAsWixipl(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    "-p:OutputType=IntermediatePostLink",
                });
                result.AssertSuccess();

                var wixBuildCommands = MsbuildUtilities.GetToolCommandLines(result, "wix", "build", buildSystem);
                Assert.Single(wixBuildCommands);

                var path = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .Single();
                WixAssert.StringEqual(@"bin\x86\Release\MsiPackage.wixipl", path);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSimpleWixlib(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "Wixlib", "SimpleWixlib");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "SimpleWixlib.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                });
                result.AssertSuccess();

                var wixBuildCommands = MsbuildUtilities.GetToolCommandLines(result, "wix", "build", buildSystem);
                Assert.Single(wixBuildCommands);

                var path = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .Single();
                WixAssert.StringEqual(@"bin\Release\SimpleWixlib.wixlib", path);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildPackageIncludingSimpleWixlib(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "Wixlib");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, "PackageIncludesWixlib", @"bin\");
                var projectPath = Path.Combine(baseFolder, "PackageIncludesWixlib", "PackageIncludesWixlib.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                });
                result.AssertSuccess();

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"PackageIncludesWixlib\bin\x86\Release\cab1.cab",
                    @"PackageIncludesWixlib\bin\x86\Release\PackageIncludesWixlib.msi",
                    @"PackageIncludesWixlib\bin\x86\Release\PackageIncludesWixlib.wixpdb",
                }, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildAndCleanSimpleMsiPackage(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                // Build
                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath)
                }, verbosityLevel: "diag");
                result.AssertSuccess();

                var buildOutput = String.Join("\r\n", result.Output);

                var createdPaths = Directory.EnumerateFiles(baseFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.NotEmpty(createdPaths);

                // Clean
                result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    "-t:Clean",
                }, verbosityLevel: "diag");
                result.AssertSuccess();

                var cleanOutput = String.Join("\r\n", result.Output);

                // Clean is only expected to delete the files listed in {Project}.FileListAbsolute.txt,
                // so this is not quite right but close enough.
                var allowedFiles = new HashSet<string>
                {
                    "MsiPackage.wixproj",
                    "MsiPackage.binlog",
                    "Package.en-us.wxl",
                    "Package.wxs",
                    "PackageComponents.wxs",
                    @"data\test.txt",
                    @"obj\x86\Release\MsiPackage.wixproj.FileListAbsolute.txt",
                };

                var remainingPaths = Directory.EnumerateFiles(baseFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .Where(s => !allowedFiles.Contains(s))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.StringCollectionEmpty(remainingPaths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildMultiTargetingWixlibUsingRids(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "MultiTargetingWixlib");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "PackageUsingRids");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var filesFolder = Path.Combine(binFolder, "Release", @"PFiles\");
                var projectPath = Path.Combine(baseFolder, "PackageUsingRids.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    "-Restore",
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath)
                    });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var releaseFiles = Directory.EnumerateFiles(filesFolder, "*", SearchOption.AllDirectories);
                var releaseFileSizes = releaseFiles.Select(p => PathAndSize(p, filesFolder)).OrderBy(s => s).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    @"net472_x64\e_sqlite3.dll - 1601536",
                    @"net472_x86\e_sqlite3.dll - 1207296",
                    @"net6_x64\e_sqlite3.dll - 1601536",
                    @"net6_x86\e_sqlite3.dll - 1207296",
                }, releaseFileSizes);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildMultiTargetingWixlibUsingRidsWithReleaseAndDebug(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "MultiTargetingWixlib");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "PackageReleaseAndDebug");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var filesFolder = Path.Combine(binFolder, "Release", @"PFiles\");
                var projectPath = Path.Combine(baseFolder, "PackageReleaseAndDebug.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    "-Restore",
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath)
                    });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning")).ToArray();
                WixAssert.StringCollectionEmpty(warnings);

                var releaseFiles = Directory.EnumerateFiles(filesFolder, "*", SearchOption.AllDirectories);
                var releaseFileSizes = releaseFiles.Select(p => PathAndSize(p, filesFolder)).OrderBy(s => s).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    @"debug_net472_x64\e_sqlite3.dll - 1601536",
                    @"debug_net472_x86\e_sqlite3.dll - 1207296",
                    @"debug_net6_x64\e_sqlite3.dll - 1601536",
                    @"debug_net6_x86\e_sqlite3.dll - 1207296",
                    @"release_net472_x64\e_sqlite3.dll - 1601536",
                    @"release_net472_x86\e_sqlite3.dll - 1207296",
                    @"release_net6_x64\e_sqlite3.dll - 1601536",
                    @"release_net6_x86\e_sqlite3.dll - 1207296",
                }, releaseFileSizes);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CannotBuildMultiTargetingWixlibUsingExplicitSubsetOfTfmAndRid(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "MultiTargetingWixlib");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "PackageUsingExplicitTfmAndRids");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var filesFolder = Path.Combine(binFolder, "Release", @"PFiles\");
                var projectPath = Path.Combine(baseFolder, "PackageUsingExplicitTfmAndRids.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    "-Restore",
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath)
                    });

                var errors = GetDistinctErrorMessages(result.Output, baseFolder);
                WixAssert.CompareLineByLine(new[]
                {
                    @"<basefolder>\Package.wxs(22): error WIX0103: Cannot find the File file '!(bindpath.TestExe.net472.win_x86)\e_sqlite3.dll'. The following paths were checked: !(bindpath.TestExe.net472.win_x86)\e_sqlite3.dll [<basefolder>\PackageUsingExplicitTfmAndRids.wixproj]",
                }, errors);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildWithWarningWhenExtensionIsMissing(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "WixlibMissingExtension");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "WixlibMissingExtension.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[] {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    "-p:SignOutput=true",
                    });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning")).Select(line => ExtractWarningFromMessage(line, baseFolder)).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "WXE0001: Unable to find extension DoesNotExist.wixext.dll.",
                    "WXE0001: Unable to find extension DoesNotExist.wixext.dll.",
                }, warnings);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\Release\WixlibMissingExtension.wixlib",
                }, paths);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildPackageWithComma(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData", "PackageWith,Comma");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "PackageWith,Comma.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                });
                result.AssertSuccess();

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\Release\cab1.cab",
                    @"bin\Release\PackageWith,Comma.msi",
                    @"bin\Release\PackageWith,Comma.wixpdb",
                }, paths);
            }
        }

        [Theory(Skip = "Depends on creating broken publish which is not supported at this time")]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void ReportsInnerExceptionForUnexpectedExceptions(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixToolDir", Path.Combine(MsbuildFixture.WixMsbuildPath, "broken", "net461")),
                });
                Assert.Equal(1, result.ExitCode);

                var expectedMessage = "System.PlatformNotSupportedException: Could not find platform specific 'wixnative.exe' ---> System.IO.FileNotFoundException: Could not find internal piece of WiX Toolset from";
                Assert.Contains(result.Output, m => m.Contains(expectedMessage));
            }
        }

        private void AssertWixpdb(BuildSystem buildSystem, string debugType, string[] expectedOutputFiles)
        {
            var sourceFolder = TestData.Get("TestData", "SimpleMsiPackage", "MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixMSBuildProps", MsbuildFixture.WixPropsPath),
                    debugType == null ? String.Empty : $"-p:DebugType={debugType}",
                    "-p:SuppressValidation=true"
                });
                result.AssertSuccess();

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(expectedOutputFiles, paths);
            }
        }

        private static string ExtractWarningFromMessage(string message, string baseFolder)
        {
            const string prefix = ": warning ";

            var start = message.IndexOf(prefix) + prefix.Length;
            var end = message.LastIndexOf("[");

            return ReplacePathsInMessage(message.Substring(start, end - start), baseFolder);
        }

        private static string[] GetDistinctErrorMessages(string[] output, string baseFolder)
        {
            return output.Where(l => l.Contains(": error ")).Select(line =>
            {
                var trimmed = ReplacePathsInMessage(line, baseFolder);

                // If the message starts with a multi-proc build marker (like: "1>" or "2>") trim it.
                if (trimmed[1] == '>')
                {
                    trimmed = trimmed.Substring(2);
                }

                return trimmed;
            }).Distinct().ToArray();
        }

        private static string ReplacePathsInMessage(string message, string baseFolder)
        {
            return message.Trim().Replace(baseFolder, "<basefolder>");
        }

        private static string PathAndSize(string path, string replace)
        {
            var fi = new FileInfo(path);
            return $"{fi.FullName.Replace(replace, String.Empty)} - {fi.Length}";
        }
    }
}
