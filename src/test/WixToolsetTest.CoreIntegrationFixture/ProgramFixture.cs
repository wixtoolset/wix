// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegrationFixture
{
    using System.IO;
    using WixToolset.Core;
    using WixToolsetTest.CoreIntegrationFixture.Utility;
    using Xunit;

    public class ProgramFixture
    {
        [Fact]
        public void CanBuildSingleFile()
        {
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            using (var pushd = new Pushd(folder))
            {
                var intermediateFolder = fs.GetFolder();

                var program = new Program();
                var result = program.Run(new[] { "build", "Package.wxs", "PackageComponents.wxs", "-loc", "Package.en-us.wxl", "-bindpath", "data", "-intermediateFolder", intermediateFolder, "-o", $@"{intermediateFolder}\bin\test.msi" });

                Assert.Equal(0, result);
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.msi")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\test.wixpdb")));
                Assert.True(File.Exists(Path.Combine(intermediateFolder, @"bin\MsiPackage\test.txt")));
            }
        }
    }
}
