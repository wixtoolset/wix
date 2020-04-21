// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Bal
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class BalExtensionFixture
    {
        [Fact]
        public void CanBuildUsingWixStdBa()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixStdBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-burnStub", TestData.Get(@"runtimes\win-x86\native\burn.x86.exe"),
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));
            }
        }
    }
}
