// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using Xunit;

    public class BundleBackwardsCompatibleFixture
    {
        [Fact]
        public void CanBuildBundleWithBootstrapperApplicationDll()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin", "test.exe");

                var result = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "build",
                    Path.Combine(folder, "BundleBackwardsCompatible", "BundleWithBootstrapperApplicationDll.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                result.AssertSuccess();

                var messages = result.Messages.Select(WixMessageFormatter.FormatMessage).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "Warning 1130: The BootstrapperApplicationDll element has been deprecated.",
                }, messages);

                Assert.True(File.Exists(exePath));
            }
        }

        [Fact]
        public void CannotBuildBundleWithBootstrapperApplicationSourceAndBootstrapperApplicationDll()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin", "test.exe");

                var result = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "build",
                    Path.Combine(folder, "BundleBackwardsCompatible", "BundleWithBootstrapperApplicationSourceAndBootstrapperApplicationDll.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                });

                var messages = result.Messages.Select(m => WixMessageFormatter.FormatMessage(m, folder, "<testdata>")).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    "Warning 1130: The BootstrapperApplicationDll element has been deprecated.",
                    "Error 6604: More than one BootstrapperApplication source file was specified. Only one is allowed. Another BootstrapperApplication source file was defined via the BootstrapperApplication element at <testdata>\\BundleBackwardsCompatible\\BundleWithBootstrapperApplicationSourceAndBootstrapperApplicationDll.wxs(3)."
                }, messages);
            }
        }
    }
}
