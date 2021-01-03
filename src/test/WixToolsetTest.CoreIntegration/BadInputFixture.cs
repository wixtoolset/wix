// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class BadInputFixture
    {
        [Fact]
        public void SwitchIsNotConsideredAnArgument()
        {
            var result = WixRunner.Execute(new[]
            {
                "build",
                "-bindpath", "-thisisaswitchnotanarg",
            });

            Assert.Single(result.Messages, m => m.Id == (int)ErrorMessages.Ids.ExpectedArgument);
            // TODO: when CantBuildSingleExeBundleWithInvalidArgument is fixed, uncomment:
            //Assert.Equal((int)ErrorMessages.Ids.ExpectedArgument, result.ExitCode);
        }

        [Fact(Skip = "Test demonstrates failure")]
        public void CantBuildSingleExeBundleWithInvalidArgument()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var exePath = Path.Combine(baseFolder, @"bin\test.exe");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SingleExeBundle", "SingleExePackageGroup.wxs"),
                    Path.Combine(folder, "BundleWithPackageGroupRef", "Bundle.wxs"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", exePath,
                    "-nonexistentswitch", "param",
                });

                Assert.NotEqual(0, result.ExitCode);

                Assert.False(File.Exists(exePath));
            }
        }

        [Fact]
        public void RegistryKeyWithoutAttributesDoesntCrash()
        {
            var folder = TestData.Get(@"TestData\BadInput");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "RegistryKey.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                Assert.InRange(result.ExitCode, 2, Int32.MaxValue);
            }
        }

        [Fact]
        public void BundleVariableWithBadTypeIsRejected()
        {
            var folder = TestData.Get(@"TestData\BadInput");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleVariable.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                Assert.Equal(21, result.ExitCode);
            }
        }

        [Fact]
        public void BundleVariableWithHiddenPersistedIsRejected()
        {
            var folder = TestData.Get(@"TestData\BadInput");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "HiddenPersistedBundleVariable.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                Assert.Equal(193, result.ExitCode);
            }
        }
    }
}
