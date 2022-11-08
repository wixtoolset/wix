// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Sdk
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using Xunit;

    // When these tests are run repeatedly, they will expose an issue
    // in the Windows Installer where ICE validations will occasionally
    // fail to send all error/warning messages. The inconsistency
    // obviously wreaks havoc on the tests.
    //
    // These tests are still interesting (and complex) enough to keep
    // around for manual testing. Uncomment or define the following
    // line to do so.
#if DISABLE_VALIDATION_TESTS_DUE_TO_WINDOWS_INSTALLER_INCONSISTENCIES
    public class MsbuildValidationFixture
    {
        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CannotBuildMsiPackageWithIceIssues(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\MsiPackageWithIceError\MsiPackage");

            var testLogsFolder = TestData.GetUnitTestLogsFolder();
            File.Delete(Path.Combine(testLogsFolder, buildSystem + ".binlog"));
            File.Delete(Path.Combine(testLogsFolder, buildSystem + ".msi"));

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, suppressValidation: false);

                File.Copy(Path.ChangeExtension(projectPath, ".binlog"), Path.Combine(testLogsFolder, buildSystem + ".binlog"));
                File.Copy(Path.Combine(baseFolder, "obj", "x86", "Release", "en-US", "MsiPackage.msi"), Path.Combine(testLogsFolder, buildSystem + ".msi"));

                var iceIssues = result.Output.Where(line => line.Contains(": error") || line.Contains(": warning"))
                                             .Select(line => line.Replace(baseFolder, "<baseFolder>")
                                                                 .Replace("1>", String.Empty)  // remove any multi-proc markers
                                                                 .Replace("WIX204", "WIX0204") // normalize error number reporting because MSBuild is not consistent on zero padding
                                                                 .Trim())
                                             .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                                             .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"<baseFolder>\Package.wxs(17): error WIX0204: ICE12: CustomAction: CausesICE12Error is of type: 35. Therefore it must come after CostFinalize @ 1000 in Seq Table: InstallExecuteSequence. CA Seq#: 49 [<baseFolder>\MsiPackage.wixproj]",
                    @"<baseFolder>\Package.wxs(17): error WIX0204: ICE12: CustomAction: CausesICE12Error is of type: 35. Therefore it must come after CostFinalize @ 1000 in Seq Table: InstallExecuteSequence. CA Seq#: 49 [<baseFolder>\MsiPackage.wixproj]",
                    @"<baseFolder>\Package.wxs(8): warning WIX1076: ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only. [<baseFolder>\MsiPackage.wixproj]",
                    @"<baseFolder>\Package.wxs(8): warning WIX1076: ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only. [<baseFolder>\MsiPackage.wixproj]",
                }, iceIssues);
            }
        }

        [Theory]
        [InlineData(BuildSystem.DotNetCoreSdk)]
        [InlineData(BuildSystem.MSBuild)]
        [InlineData(BuildSystem.MSBuild64)]
        public void CannotBuildMsiPackageWithIceWarningsAsErrors(BuildSystem buildSystem)
        {
            var sourceFolder = TestData.Get(@"TestData\MsiPackageWithIceError\MsiPackage");

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(sourceFolder);
                var baseFolder = fs.BaseFolder;
                var projectPath = Path.Combine(baseFolder, "MsiPackage.wixproj");

                var result = MsbuildUtilities.BuildProject(buildSystem, projectPath, new[]
                {
                    $"-p:TreatWarningsAsErrors=true",
                    MsbuildUtilities.GetQuotedPropertySwitch(buildSystem, "SuppressIces", "ICE12"),
                }, suppressValidation: false);
                Assert.Equal(1, result.ExitCode);

                var iceIssues = result.Output.Where(line => (line.Contains(": error") || line.Contains(": warning")))
                                             .Select(line => line.Replace(baseFolder, "<baseFolder>")
                                                                 .Replace("1>", String.Empty)  // remove any multi-proc markers
                                                                 .Replace("WIX204", "WIX0204") // normalize error number reporting because MSBuild is not consistent on zero padding
                                                                 .Trim())
                                             .OrderBy(s => s, StringComparer.OrdinalIgnoreCase)
                                             .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"<baseFolder>\Package.wxs(8): error WIX1076: ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only. [<baseFolder>\MsiPackage.wixproj]",
                    @"<baseFolder>\Package.wxs(8): error WIX1076: ICE46: Property 'Myproperty' referenced in column 'LaunchCondition'.'Condition' of row 'Myproperty' differs from a defined property by case only. [<baseFolder>\MsiPackage.wixproj]",
                }, iceIssues);
            }
        }
    }
#endif
}
