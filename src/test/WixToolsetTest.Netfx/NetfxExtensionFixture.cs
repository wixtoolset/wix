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

            var results = build.BuildAndQuery(Build, "Wix4NetFxNativeImage");
            Assert.Equal(new[]
            {
                "Wix4NetFxNativeImage:ExampleNgen\tfil6349_KNDJhqShNzVdHX3ihhvA6Y\t3\t8\t\t",
            }, results.OrderBy(s => s).ToArray());
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
