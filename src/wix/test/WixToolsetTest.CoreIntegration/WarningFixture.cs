// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class WarningFixture
    {
        [Fact]
        public void SuppressedWarningsWithWarningAsErrorsAreNotErrors()
        {
            var folder = TestData.Get(@"TestData\Payload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(warningsAsErrors: true, new[]
                {
                    "build",
                    "-sw1152",
                    Path.Combine(folder, "CanonicalizeName.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                result.AssertSuccess();
            }
        }

        [Fact]
        public void WarningsAsErrorsTreatsWarningsAsErrors()
        {
            var folder = TestData.Get(@"TestData\Payload");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(warningsAsErrors: true, new[]
                {
                    "build",
                    Path.Combine(folder, "CanonicalizeName.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                Assert.Equal((int)WarningMessages.Ids.PathCanonicalized, result.ExitCode);

                var message = Assert.Single(result.Messages);
                Assert.Equal(MessageLevel.Warning, message.Level); // TODO: is this right?
            }
        }
    }
}
