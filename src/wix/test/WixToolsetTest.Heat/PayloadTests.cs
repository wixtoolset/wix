// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Harvesters
{
    using System;
    using System.IO;
    using WixBuildTools.TestSupport;
    using Xunit;

    public class PayloadTests
    {
        [Fact]
        public void CanHarvestExePackagePayload()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var outputFilePath = Path.Combine(baseFolder, "test.wxs");

                var result = HeatRunner.Execute(new[]
                {
                    "exepackagepayload",
                    Path.Combine(folder, ".Data", "burn.exe"),
                    "-o", outputFilePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(outputFilePath));

                var expected = File.ReadAllText(Path.Combine(folder, "Payload", "HarvestedExePackagePayload.wxs")).Replace("\r\n", "\n");
                var actual = File.ReadAllText(outputFilePath).Replace("\r\n", "\n");
                Assert.Equal(expected, actual);
            }
        }

        [Fact]
        public void CanHarvestMsuPackagePayload()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var outputFilePath = Path.Combine(baseFolder, "test.wxs");

                var result = HeatRunner.Execute(new[]
                {
                    "msupackagepayload",
                    Path.Combine(folder, ".Data", "Windows8.1-KB2937592-x86.msu"),
                    "-o", outputFilePath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(outputFilePath));

                var expected = File.ReadAllText(Path.Combine(folder, "Payload", "HarvestedMsuPackagePayload.wxs")).Replace("\r\n", "\n");
                var actual = File.ReadAllText(outputFilePath).Replace("\r\n", "\n");
                Assert.Equal(expected, actual);
            }
        }
    }
}
