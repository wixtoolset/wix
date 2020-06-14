// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Netfx
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Netfx;
    using Xunit;

    public class NetfxExtensionFixture
    {
        [Fact]
        public void CanBuildUsingDotNetCorePackages()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\UsingDotNetCorePackages");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-ext", TestData.Get(@"WixToolset.Netfx.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));
            }
        }

        [Fact]
        public void CanBuildUsingNativeImage()
        {
            var folder = TestData.Get(@"TestData\UsingNativeImage");
            var build = new Builder(folder, typeof(NetfxExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "Binary", "CustomAction", "Wix4NetFxNativeImage");
            Assert.Equal(new[]
            {
                "Binary:Wix4NetFxCA_X86\t[Binary data]",
                "CustomAction:Wix4NetFxExecuteNativeImageCommitInstall_X86\t3649\tWix4NetFxCA_X86\tExecNetFx\t",
                "CustomAction:Wix4NetFxExecuteNativeImageCommitUninstall_X86\t3649\tWix4NetFxCA_X86\tExecNetFx\t",
                "CustomAction:Wix4NetFxExecuteNativeImageInstall_X86\t3137\tWix4NetFxCA_X86\tExecNetFx\t",
                "CustomAction:Wix4NetFxExecuteNativeImageUninstall_X86\t3137\tWix4NetFxCA_X86\tExecNetFx\t",
                "CustomAction:Wix4NetFxScheduleNativeImage_X86\t1\tWix4NetFxCA_X86\tSchedNetFx\t",
                "Wix4NetFxNativeImage:ExampleNgen\tfil6349_KNDJhqShNzVdHX3ihhvA6Y\t3\t8\t\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildUsingNativeImageX64()
        {
            var folder = TestData.Get(@"TestData\UsingNativeImage");
            var build = new Builder(folder, typeof(NetfxExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "Wix4NetFxNativeImage");
            Assert.Equal(new[]
            {
                "Binary:Wix4NetFxCA_X64\t[Binary data]",
                "CustomAction:Wix4NetFxExecuteNativeImageCommitInstall_X64\t3649\tWix4NetFxCA_X64\tExecNetFx\t",
                "CustomAction:Wix4NetFxExecuteNativeImageCommitUninstall_X64\t3649\tWix4NetFxCA_X64\tExecNetFx\t",
                "CustomAction:Wix4NetFxExecuteNativeImageInstall_X64\t3137\tWix4NetFxCA_X64\tExecNetFx\t",
                "CustomAction:Wix4NetFxExecuteNativeImageUninstall_X64\t3137\tWix4NetFxCA_X64\tExecNetFx\t",
                "CustomAction:Wix4NetFxScheduleNativeImage_X64\t1\tWix4NetFxCA_X64\tSchedNetFx\t",
                "Wix4NetFxNativeImage:ExampleNgen\tfil6349_KNDJhqShNzVdHX3ihhvA6Y\t3\t8\t\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildUsingNativeImageARM64()
        {
            var folder = TestData.Get(@"TestData\UsingNativeImage");
            var build = new Builder(folder, typeof(NetfxExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Binary", "CustomAction", "Wix4NetFxNativeImage");
            Assert.Equal(new[]
            {
                "Binary:Wix4NetFxCA_A64\t[Binary data]",
                "CustomAction:Wix4NetFxExecuteNativeImageCommitInstall_A64\t3649\tWix4NetFxCA_A64\tExecNetFx\t",
                "CustomAction:Wix4NetFxExecuteNativeImageCommitUninstall_A64\t3649\tWix4NetFxCA_A64\tExecNetFx\t",
                "CustomAction:Wix4NetFxExecuteNativeImageInstall_A64\t3137\tWix4NetFxCA_A64\tExecNetFx\t",
                "CustomAction:Wix4NetFxExecuteNativeImageUninstall_A64\t3137\tWix4NetFxCA_A64\tExecNetFx\t",
                "CustomAction:Wix4NetFxScheduleNativeImage_A64\t1\tWix4NetFxCA_A64\tSchedNetFx\t",
                "Wix4NetFxNativeImage:ExampleNgen\tfil6349_KNDJhqShNzVdHX3ihhvA6Y\t3\t8\t\t",
            }, results.OrderBy(s => s).ToArray());
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }

        private static void BuildX64(string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Add("-platform");
            newArgs.Add("x64");

            var result = WixRunner.Execute(newArgs.ToArray());
            result.AssertSuccess();
        }

        private static void BuildARM64(string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Add("-platform");
            newArgs.Add("arm64");

            var result = WixRunner.Execute(newArgs.ToArray());
            result.AssertSuccess();
        }
    }
}
