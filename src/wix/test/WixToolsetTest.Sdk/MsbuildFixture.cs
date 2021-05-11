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
        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CanBuildSimpleBundle(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\SimpleMsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "SimpleBundle");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "SimpleBundle.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath);
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

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

        [Theory(Skip = "Test demonstrates failure")]
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

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath);
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"bin\x86\Release\SimpleBundle.exe",
                    @"bin\x86\Release\SimpleBundle.wixpdb",
                    @"bin\x86\Release\test.txt",
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

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath);
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

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

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath);
                result.AssertSuccess();

                var platformSwitches = result.Output.Where(line => line.Contains("-platform x86"));
                Assert.Single(platformSwitches);

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Equal(4, warnings.Count());

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
            var sourceFolder = TestData.Get(@"TestData\MergeModule");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = Path.Combine(fs.BaseFolder, "MergeMsiPackage");
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var projectPath = Path.Combine(baseFolder, "MergeMsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath);
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

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

        private void AssertWixpdb(BuildSystem buildSystem, string wixpdbType, string[] expectedOutputFiles)
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
                    wixpdbType == null ? String.Empty : $"-p:WixPdbType={wixpdbType}",
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
        public void CanBuildSimpleMsiPackageWithIceSuppressions(BuildSystem buildSystem)
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
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "SuppressIces", "ICE45;ICE46"),
                });
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
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "SuppressSpecificWarnings", "1118;1102"),
                });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk, null)]
        [InlineData(BuildSystem.DotNetCoreSdk, true)]
        [InlineData(BuildSystem.MSBuild, null)]
        [InlineData(BuildSystem.MSBuild, true)]
        [InlineData(BuildSystem.MSBuild64, null)]
        [InlineData(BuildSystem.MSBuild64, true)]
        public void CanBuildSimpleMsiPackageAsWixipl(BuildSystem buildSystem, bool? outOfProc)
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
                    "-p:OutputType=IntermediatePostLink",
                }, outOfProc: outOfProc);
                result.AssertSuccess();

                var wixBuildCommands = MsbuildUtilities.GetToolCommandLines(result, "wix", "build", buildSystem, outOfProc);
                Assert.Single(wixBuildCommands);

                var path = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .Single();
                Assert.Equal(@"bin\x86\Release\MsiPackage.wixipl", path);
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
                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, verbosityLevel: "diag");
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
                    "-t:Clean",
                }, verbosityLevel: "diag");
                result.AssertSuccess();

                var cleanOutput = String.Join("\r\n", result.Output);

                // Clean is only expected to delete the files listed in {Project}.FileListAbsolute.txt,
                // so this is not quite right but close enough.
                var allowedFiles = new HashSet<string>
                {
                    "MsiPackage.wixproj",
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
                Assert.Empty(remainingPaths);
            }
        }

        [Theory]
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
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "WixToolDir", Path.Combine(MsbuildUtilities.WixMsbuildPath, "broken", "net461")),
                }, outOfProc: true);
                Assert.Equal(1, result.ExitCode);

                var expectedMessage = "System.PlatformNotSupportedException: Could not find platform specific 'wixnative.exe' ---> System.IO.FileNotFoundException: Could not find internal piece of WiX Toolset from";
                Assert.Contains(result.Output, m => m.Contains(expectedMessage));
            }
        }
    }
}
