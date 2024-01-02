// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Data;
    using Xunit;

    public class BadInputFixture
    {
        [Fact]
        public void HandleInvalidIds()
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
                    Path.Combine(folder, "InvalidIds.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                Assert.Equal(330, result.ExitCode);
            }
        }

        [Fact]
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
        public void BundleExePackageWithNetfxProtocolIsRejected()
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
                    Path.Combine(folder, "BundleExePackageWithNetfxProtocol.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                Assert.Equal(193, result.ExitCode);
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

        [Fact]
        public void CannotBuildBundleWithLocVariableNames()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithInvalid", "BundleWithInvalidLocVariableNames.wxs"),
                    "-loc", Path.Combine(folder, "BundleWithInvalid", "BundleWithInvalidLocValues.wxl"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                var messages = result.Messages.Select(m => m.ToString()).ToList();
                messages.Sort();

                WixAssert.CompareLineByLine(new[]
                {
                    "The Variable/@Name attribute was not found; it is required.",
                    "The Variable/@Name attribute's value, '!(loc.BuiltinBurnVariableName)', is not a legal identifier. Identifiers may contain ASCII characters A-Z, a-z, digits, underscores (_), or periods (.). Every identifier must begin with either a letter or an underscore.",
                }, messages.ToArray());

                Assert.Equal(10, result.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildBundleWithReservedVariableNames()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var wixlibPath = Path.Combine(intermediateFolder, @"test.wixlib");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithInvalid", "BundleWithReservedVariableNames.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", wixlibPath,
                });

                var messages = result.Messages.Select(m => m.ToString()).ToList();
                messages.Sort();

                WixAssert.CompareLineByLine(new[]
                {
                    "The SetVariable/@Variable attribute's value, 'WixBundleInstalled', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFiles64Folder', 'CommonFiles6432Folder', 'CommonFilesFolder', 'CompatibilityMode', 'ComputerName', 'Date', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'InstallerName', 'InstallerVersion', 'LocalAppDataFolder', 'LogonUser', 'MyPicturesFolder', 'NativeMachine', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProcessorArchitecture', 'ProgramFiles64Folder', 'ProgramFiles6432Folder', 'ProgramFilesFolder', 'ProgramMenuFolder', 'RebootPending', 'SendToFolder', 'ServicePackLevel', 'StartMenuFolder', 'StartupFolder', 'System64Folder', 'SystemFolder', 'SystemLanguageID', 'TempFolder', 'TemplateFolder', 'TerminalServer', 'UserLanguageID', 'UserUILanguageID', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsBuildNumber', 'WindowsFolder', 'WindowsVolume', 'WixBundleAction', 'WixBundleActiveParent', 'WixBundleCommandLineAction', 'WixBundleElevated', 'WixBundleExecutePackageAction', 'WixBundleExecutePackageCacheFolder', 'WixBundleForcedRestartPackage', 'WixBundleInstalled', 'WixBundleProviderKey', 'WixBundleSourceProcessFolder', 'WixBundleSourceProcessPath', 'WixBundleTag', 'WixBundleUILevel', or 'WixBundleVersion'.",
                    "The Variable/@Name attribute's value, 'AppDataFolder', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFiles64Folder', 'CommonFiles6432Folder', 'CommonFilesFolder', 'CompatibilityMode', 'ComputerName', 'Date', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'InstallerName', 'InstallerVersion', 'LocalAppDataFolder', 'LogonUser', 'MyPicturesFolder', 'NativeMachine', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProcessorArchitecture', 'ProgramFiles64Folder', 'ProgramFiles6432Folder', 'ProgramFilesFolder', 'ProgramMenuFolder', 'RebootPending', 'SendToFolder', 'ServicePackLevel', 'StartMenuFolder', 'StartupFolder', 'System64Folder', 'SystemFolder', 'SystemLanguageID', 'TempFolder', 'TemplateFolder', 'TerminalServer', 'UserLanguageID', 'UserUILanguageID', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsBuildNumber', 'WindowsFolder', 'WindowsVolume', 'WixBundleAction', 'WixBundleActiveParent', 'WixBundleCommandLineAction', 'WixBundleElevated', 'WixBundleExecutePackageAction', 'WixBundleExecutePackageCacheFolder', 'WixBundleForcedRestartPackage', 'WixBundleInstalled', 'WixBundleProviderKey', 'WixBundleSourceProcessFolder', 'WixBundleSourceProcessPath', 'WixBundleTag', 'WixBundleUILevel', or 'WixBundleVersion'.",
                }, messages.ToArray());

                Assert.Equal(348, result.ExitCode);
            }
        }

        [Fact]
        public void CannotBuildWithUnknownOutputType()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var outputPath = Path.Combine(intermediateFolder, @"test.pkg");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "SimplePackage", "SimplePackage.wxs"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-outputType", "invalid",
                    "-o", outputPath,
                });

                var messages = result.Messages.Select(m => m.ToString()).ToList();
                messages.Sort();

                WixAssert.CompareLineByLine(new[]
                {
                    @"Unable to find a backend to process output type: invalid for output file: <folder>\test.pkg. Specify a different output type or output file extension.",
                }, messages.Select(s => s.Replace(intermediateFolder, "<folder>")).ToArray());

                Assert.Equal(7014, result.ExitCode);
            }
        }

        [Fact]
        public void GuardsAgainstVariousBundleValuesFromLoc()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(false, new[]
                {
                    "build",
                    Path.Combine(folder, "BundleWithInvalid", "BundleWithInvalidLocValues.wxs"),
                    "-loc", Path.Combine(folder, "BundleWithInvalid", "BundleWithInvalidLocValues.wxl"),
                    "-bindpath", Path.Combine(folder, ".Data"),
                    "-bindpath", Path.Combine(folder, "DecompileSingleFileCompressed"),
                    "-bindpath", Path.Combine(folder, "SimpleBundle", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(baseFolder, @"bin\test.exe")
                });

                var warningMessages = result.Messages.Where(m => m.Level == MessageLevel.Warning).Select(m => m.ToString()).OrderBy(s => s, StringComparer.Ordinal).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    "*Search/@Condition contains the built-in Variable 'WixBundleAction', which is not available when it is evaluated. (Unavailable Variables are: 'WixBundleAction'.). Rewrite the condition to avoid Variables that are never valid during its evaluation.",
                    "Bundle/@Condition contains the built-in Variable 'WixBundleInstalled', which is not available when it is evaluated. (Unavailable Variables are: 'RebootPending', 'WixBundleAction', or 'WixBundleInstalled'.). Rewrite the condition to avoid Variables that are never valid during its evaluation.",
                    "ExePackage/@DetectCondition contains the built-in Variable 'WixBundleAction', which is not available when it is evaluated. (Unavailable Variables are: 'WixBundleAction'.). Rewrite the condition to avoid Variables that are never valid during its evaluation.",
                    "The *Search/@Variable attribute's value references the well-known log Variable 'WixBundleLog' to change its value. This variable is set by the engine and is intended to be read-only. Change your attribute's value to reference a custom variable.",
                }, warningMessages);

                var errorMessages = result.Messages.Where(m => m.Level == MessageLevel.Error).Select(m => m.ToString()).OrderBy(s => s, StringComparer.Ordinal).ToArray();

                WixAssert.CompareLineByLine(new[]
                {
                    "The 'REINSTALLMODE' MsiProperty is controlled by the bootstrapper and cannot be authored. (Illegal properties are: 'ACTION', 'ADDLOCAL', 'ADDSOURCE', 'ADDDEFAULT', 'ADVERTISE', 'ALLUSERS', 'REBOOT', 'REINSTALL', 'REINSTALLMODE', or 'REMOVE'.) Remove the MsiProperty element.",
                    "The CommandLine/@Condition attribute's value '=' is not a valid bundle condition.",
                    "The MsiPackage/@InstallCondition attribute's value '=' is not a valid bundle condition.",
                    "The MsiProperty/@Condition attribute's value '=' is not a valid bundle condition.",
                }, errorMessages);

                Assert.Equal(409, result.ExitCode);
            }
        }
    }
}
