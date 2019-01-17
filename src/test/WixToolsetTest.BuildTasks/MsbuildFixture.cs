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
        private static readonly string WixTargetsPath = Path.Combine(Path.GetDirectoryName(new Uri(typeof(DoIt).Assembly.CodeBase).AbsolutePath), "wix.targets");

        public MsbuildFixture()
        {
            this.MsbuildRunner = new MsbuildRunner();
        }

        private MsbuildRunner MsbuildRunner { get; }

        [Fact]
        public void CanBuildSimpleMsiPackage()
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage\MsiPackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = this.MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
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
        public void CanBuild64BitMsiPackage()
        {
            var projectPath = TestData.Get(@"TestData\SimpleMsiPackage\MsiPackage\MsiPackage.wixproj");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var binFolder = Path.Combine(baseFolder, @"bin\");
                var intermediateFolder = Path.Combine(baseFolder, @"obj\");

                var result = this.MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
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

                var result = this.MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
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

                var result = this.MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
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

                var result = this.MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
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
                var result = this.MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
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
                result = this.MsbuildRunner.Execute(projectPath, new[]
                {
                    $"-p:WixTargetsPath={WixTargetsPath}",
                    $"-p:IntermediateOutputPath={intermediateFolder}",
                    $"-p:OutputPath={binFolder}",
                    "-t:Clean",
                    "-v:diag"
                });
                result.AssertSuccess();

                var cleanOutput = String.Join("\r\n", result.Output);

                var remainingPaths = Directory.EnumerateFiles(baseFolder, @"*.*", SearchOption.AllDirectories)
                    .Select(s => s.Substring(baseFolder.Length + 1))
                    .OrderBy(s => s)
                    .ToArray();
                Assert.Empty(remainingPaths);
            }
        }
    }
}
