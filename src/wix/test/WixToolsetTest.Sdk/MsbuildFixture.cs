// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using Xunit;

    public class MsbuildFixture
    {
        public static readonly string WixMsbuildPath = Path.Combine(Path.GetDirectoryName(new Uri(typeof(MsbuildFixture).Assembly.CodeBase).AbsolutePath), "..", "..", "..", "publish", "WixToolset.Sdk");
        public static readonly string WixPropsPath = Path.Combine(WixMsbuildPath, "build", "WixToolset.Sdk.props");

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
                    @"TEST: SignBundleEngine: obj\x86\Release\SimpleBundle.exe",
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
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage");

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
                    @"TEST: SignBundleEngine: obj\x86\Release\UncompressedBundle.exe",
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
            var sourceFolder = TestData.Get(@"TestData\MergeModule\SimpleMergeModule");

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

                var warnings = result.Output.Where(line => line.Contains(": warning")).Select(ExtractWarningFromMessage).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"WIX1118: The variable 'Variable' with value 'DifferentValue' was previously declared with value 'Value'.",
                    @"WIX1102: The DefaultLanguage '1033' was used for file 'filcV1yrx0x8wJWj4qMzcH21jwkPko' which has no language or version. For unversioned files, specifying a value for DefaultLanguage is not neccessary and it will not be used when determining file versions. Remove the DefaultLanguage attribute to eliminate this warning.",
                    @"WIX1122: The installer database '<basefolder>\obj\x86\Release\en-US\MsiPackage.msi' has external cabs, but at least one of them is not signed. Please ensure that all external cabs are signed, if you mean to sign them. If you don't mean to sign them, there is no need to inscribe the MSI as part of your build.",
                    @"WIX1118: The variable 'Variable' with value 'DifferentValue' was previously declared with value 'Value'.",
                    @"WIX1102: The DefaultLanguage '1033' was used for file 'filcV1yrx0x8wJWj4qMzcH21jwkPko' which has no language or version. For unversioned files, specifying a value for DefaultLanguage is not neccessary and it will not be used when determining file versions. Remove the DefaultLanguage attribute to eliminate this warning.",
                    @"WIX1122: The installer database '<basefolder>\obj\x86\Release\en-US\MsiPackage.msi' has external cabs, but at least one of them is not signed. Please ensure that all external cabs are signed, if you mean to sign them. If you don't mean to sign them, there is no need to inscribe the MSI as part of your build."
                }, warnings);

                var testMessages = result.Output.Where(line => line.Contains("TEST:")).Select(ReplacePathsInMessage).ToArray();
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

            string ExtractWarningFromMessage(string message)
            {
                const string prefix = ": warning ";

                var start = message.IndexOf(prefix) + prefix.Length;
                var end = message.LastIndexOf("[");
                return ReplacePathsInMessage(message.Substring(start, end - start));
            }

            string ReplacePathsInMessage(string message)
            {
                return message.Replace(baseFolder, "<basefolder>").Trim();
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSimpleMsiPackageWithMergeModule(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\MergeModule");

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
                    $"-p:PdbOutputDir={pdbFolder}",
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

        private void AssertWixpdb(BuildSystem buildSystem, string debugType, string[] expectedOutputFiles)
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
            var sourceFolder = TestData.Get(@"TestData\MsiPackageWithIceError\MsiPackage");

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
                WixAssert.StringEqual(@"bin\x86\Release\SimpleWixlib.wixlib", path);
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
    }
}
