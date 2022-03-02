// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class ExePackageFixture
    {
        [Fact]
        public void ErrorWhenMissingDetectCondition()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "MissingDetectCondition.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The ExePackage element's UninstallArguments attribute was not found; it is required without attribute Permanent present.",
                    "The ExePackage/@DetectCondition attribute is recommended so the package is only installed when absent."
                }, result.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(1153, result.ExitCode);
            }
        }

        [Fact]
        public void ErrorWhenRequireDetectCondition()
        {
            var folder = TestData.Get(@"TestData", "ExePackage");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "RequireDetectCondition.wxs"),
                    "-o", Path.Combine(baseFolder, "test.wixlib")
                });

                Assert.Equal(401, result.ExitCode);
            }
        }
    }
}
