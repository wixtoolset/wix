// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class ValidationFixture
    {
        [Fact]
        public void CanValidateMsiWithIceIssues()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin", "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Validation", "PackageWithIceIssues.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                var validationResult = WixRunner.Execute(new[]
                {
                    "msi", "validate",
                    "-intermediateFolder", intermediateFolder,
                    msiPath
                });

                Assert.Equal(1, validationResult.ExitCode);

                var messages = validationResult.Messages.Select(m => m.ToString()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"ICE12: CustomAction: CausesICE12Error is of type: 35. Therefore it must come after CostFinalize @ 1000 in Seq Table: InstallExecuteSequence. CA Seq#: 49",
                    @"ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only."
                }, messages);
            }
        }

        [Fact]
        public void CanValidateMsiSuppressIceError()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin", "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Validation", "PackageWithIceIssues.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                var validationResult = WixRunner.Execute(warningsAsErrors: false, new[]
                {
                    "msi", "validate",
                    "-intermediateFolder", intermediateFolder,
                    "-sice", "ICE12",
                    msiPath
                });

                validationResult.AssertSuccess();

                var messages = validationResult.Messages.Select(m => m.ToString()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only."
                }, messages);
            }
        }

        [Fact]
        public void CanValidateMsiWithWarningsAsErrors()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin", "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Validation", "PackageWithIceIssues.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                var validationResult = WixRunner.Execute(warningsAsErrors: true, new[]
                {
                    "msi", "validate",
                    "-intermediateFolder", intermediateFolder,
                    "-sice", "ICE12",
                    msiPath
                });

                Assert.Equal(1, validationResult.ExitCode);

                var messages = validationResult.Messages.Select(m => m.ToString()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only."
                }, messages);
            }
        }
    }
}
