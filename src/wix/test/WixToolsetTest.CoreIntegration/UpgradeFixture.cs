// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class UpgradeFixture
    {

        [Fact]
        public void PopulatesInstallExecuteSequenceTable()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Upgrade", "UpgradeInvalidMinVersion.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                var errorMessages = result.Messages.Where(m => m.Level == MessageLevel.Error)
                                                   .Select(m => m.ToString())
                                                   .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "Invalid product version '1.256.0'. Product version must have a major version less than 256, a minor version less than 256, and a build version less than 65536.",
                }, errorMessages);
                Assert.Equal(242, result.ExitCode);
            }
        }
    }
}
