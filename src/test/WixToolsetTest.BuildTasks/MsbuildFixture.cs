// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BuildTasks
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.BuildTasks;
    using Xunit;

    public class MsbuildFixture
    {
        private static readonly string WixBinPath = Path.GetDirectoryName(new Uri(typeof(WixBuild).Assembly.CodeBase).AbsolutePath) + "\\";
        private static readonly string WixTargetsPath = Path.Combine(WixBinPath, "wix.targets");

        [Fact]
        public void CanBuildSimpleBundle()
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\SimpleBundle\SimpleBundle.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:WixBinDir={WixBinPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}"
                });
                result.AssertSuccess();

                var platformSwitches = result.Output.Where(line => line.TrimStart().StartsWith("wix.exe build -platform x86"));
                Assert.Single(platformSwitches);

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Equal(new[]
                {
                    @"bin\SimpleBundle.exe",
                    @"bin\SimpleBundle.wixpdb",
                }, paths);
            }
        }

        [Fact]
        public void CanBuildSimpleMsiPackage()
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage\MsiPackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:WixBinDir={WixBinPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}"
                });
                result.AssertSuccess();

                var platformSwitches = result.Output.Where(line => line.TrimStart().StartsWith("wix.exe build -platform x86"));
                Assert.Single(platformSwitches);

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Equal(4, warnings.Count());

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Equal(new[]
                {
                    @"bin\en-US\cab1.cab",
                    @"bin\en-US\MsiPackage.msi",
                    @"bin\en-US\MsiPackage.wixpdb",
                }, paths);
            }
        }

        [Fact]
        public void CanBuildWithDefaultAndExplicitlyFullWixpdbs()
        {
            var expectedOutputs = new[]
                {
                    @"bin\en-US\cab1.cab",
                    @"bin\en-US\MsiPackage.msi",
                    @"bin\en-US\MsiPackage.wixpdb",
                };

            this.AssertWixpdb(null, expectedOutputs);
            this.AssertWixpdb("Full", expectedOutputs);
        }

        [Fact]
        public void CanBuildWithNoWixpdb()
        {
            this.AssertWixpdb("NONE", new[]
                {
                    @"bin\en-US\cab1.cab",
                    @"bin\en-US\MsiPackage.msi",
                });
        }

        private void AssertWixpdb(string wixpdbType, string[] expectedOutputFiles)
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage\MsiPackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    wixpdbType == null ? String.Empty : $"-p:WixPdbType={wixpdbType}",
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:WixBinDir={WixBinPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}",
                });
                result.AssertSuccess();

                var paths = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Equal(expectedOutputFiles, paths);
            }
        }

        [Fact]
        public void CanBuild64BitMsiPackage()
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage\MsiPackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:WixBinDir={WixBinPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}",
                    $"-p:InstallerPlatform=x64",
                });
                result.AssertSuccess();

                var platformSwitches = result.Output.Where(line => line.TrimStart().StartsWith("wix.exe build -platform x64"));
                Assert.Single(platformSwitches);
            }
        }

        [Fact(Skip = "Currently fails")]
        public void CanBuildSimpleMsiPackageWithIceSuppressions()
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage\MsiPackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:WixBinDir={WixBinPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}",
                    "-p:SuppressIces=\"ICE45;ICE46\""
                });
                result.AssertSuccess();
            }
        }

        [Fact]
        public void CanBuildSimpleMsiPackageWithWarningSuppressions()
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage\MsiPackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:WixBinDir={WixBinPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}",
                    "-p:SuppressSpecificWarnings=\"1118;1102\""
                });
                result.AssertSuccess();

                var warnings = result.Output.Where(line => line.Contains(": warning"));
                Assert.Empty(warnings);
            }
        }

        [Fact]
        public void CanBuildSimpleMsiPackageAsWixipl()
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage\MsiPackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:WixBinDir={WixBinPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}",
                    "-p:OutputType=IntermediatePostLink"
                });
                result.AssertSuccess();

                var path = Directory.EnumerateFiles(binFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .Single();
                Assert.Equal(@"bin\MsiPackage.wixipl", path);
            }
        }

        [Fact]
        public void CanBuildAndCleanSimpleMsiPackage()
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage\MsiPackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                // Build
                var result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:WixBinDir={WixBinPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}",
                    "-v:diag"
                });
                result.AssertSuccess();

                var buildOutput = String.Join("\r\n", result.Output);

                var createdPaths = Directory.EnumerateFiles(baseFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.NotEmpty(createdPaths);

                // Clean
                result = MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:WixBinDir={WixBinPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}",
                    "-t:Clean",
                    "-v:diag"
                });
                result.AssertSuccess();

                var cleanOutput = String.Join("\r\n", result.Output);

                // Clean is only expected to delete the files listed in {Project}.FileListAbsolute.txt,
                // so this is not quite right but close enough.
                var remainingPaths = Directory.EnumerateFiles(baseFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .Where(s => s != "obj\\MsiPackage.wixproj.FileListAbsolute.txt")
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Empty(remainingPaths);
            }
        }
    }
}
