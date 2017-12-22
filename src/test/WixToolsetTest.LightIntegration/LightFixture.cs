// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.LightIntegration
{
    using System.IO;
    using System.Linq;
    using WixToolset.Core;
    using WixToolset.Tools;
    using WixToolsetTest.LightIntegration.Utility;
    using Xunit;

    public class LightFixture
    {
        [Fact]
        public void CanBuildFromWixout()
        {
            var folder = TestData.Get(@"TestData\Wixout");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var program = new Light();
                var result = program.Run(new WixToolsetServiceProvider(), null, new[]
                {
                    Path.Combine(folder, "test.wixout"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-b", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.msi")
                });

                Assert.Equal(0, result);

                var binFolder = Path.Combine(baseFolder, @"bin\");
                var builtFiles = Directory.GetFiles(binFolder, "*", SearchOption.AllDirectories);

                Assert.Equal(new[]{
                    "MsiPackage\\test.txt",
                    "test.msi",
                    "test.wir",
                    "test.wixpdb",
                }, builtFiles.Select(f => f.Substring(binFolder.Length)).OrderBy(s => s).ToArray());
            }
        }
    }
}
