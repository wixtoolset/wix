// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Converters;
    using WixToolset.Core;
    using WixInternal.Core.TestPackage;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class ConverterIntegrationFixture
    {
        [Fact]
        public void CanConvertPermissionExFile()
        {
            const string beforeFileName = "v3.wxs";
            const string afterFileName = "v4_expected.wxs";
            var folder = TestData.Get(@"TestData\PermissionEx");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder(true);
                var targetFile = Path.Combine(baseFolder, beforeFileName);
                File.Copy(Path.Combine(folder, beforeFileName), Path.Combine(baseFolder, beforeFileName));

                var messaging = new MockMessaging();
                var converter = new WixConverter(messaging, 4);
                var errors = converter.ConvertFile(targetFile, true);

                Assert.Equal(8, errors);

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                WixAssert.StringEqual(expected, actual);

                EnsureFixed(targetFile);
            }
        }

        [Fact]
        public void CanConvertSingleFile()
        {
            const string beforeFileName = "SingleFile.wxs";
            const string afterFileName = "ConvertedSingleFile.wxs";
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder(true);
                var targetFile = Path.Combine(baseFolder, beforeFileName);
                File.Copy(Path.Combine(folder, beforeFileName), Path.Combine(baseFolder, beforeFileName));

                var messaging = new MockMessaging();
                var converter = new WixConverter(messaging, 4);
                var errors = converter.ConvertFile(targetFile, true);

                Assert.Equal(9, errors);

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                WixAssert.StringEqual(expected, actual);

                EnsureFixed(targetFile);
            }
        }

        [Fact]
        public void CanDetectReadOnlyOutputFile()
        {
            const string beforeFileName = "SingleFile.wxs";
            var folder = TestData.Get(@"TestData\SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder(true);
                var targetFile = Path.Combine(baseFolder, beforeFileName);
                File.Copy(Path.Combine(folder, beforeFileName), Path.Combine(baseFolder, beforeFileName));

                var info = new FileInfo(targetFile);
                info.IsReadOnly = true;

                var messaging = new MockMessaging();
                var converter = new WixConverter(messaging, 4);
                var convertedCount = converter.ConvertFile(targetFile, true);

                var errors = messaging.Messages.Where(m => m.Level == WixToolset.Data.MessageLevel.Error).Select(m => m.ToString()).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "Could not write to file. (UnauthorizedAccessException)"
                }, errors);
                Assert.Equal(10, convertedCount);
            }
        }

        [Fact]
        public void RetainsPreprocessorInstructions()
        {
            const string beforeFileName = "Preprocessor.wxs";
            const string afterFileName = "ConvertedPreprocessor.wxs";
            var folder = TestData.Get(@"TestData\Preprocessor");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder(true);
                var targetFile = Path.Combine(baseFolder, beforeFileName);
                File.Copy(Path.Combine(folder, beforeFileName), Path.Combine(baseFolder, beforeFileName));

                var settingsFile = Path.Combine(folder, "wixcop.settings.xml");

                var result = RunConversion(targetFile, settingsFile: settingsFile);
                Assert.Equal(9, result.ExitCode);

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                WixAssert.StringEqual(expected, actual);

                EnsureFixed(targetFile);
            }
        }

        [Fact]
        public void CanConvertQtExec()
        {
            const string beforeFileName = "v3.wxs";
            const string afterFileName = "v4_expected.wxs";
            var folder = TestData.Get(@"TestData\QtExec");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder(true);
                var targetFile = Path.Combine(baseFolder, beforeFileName);
                File.Copy(Path.Combine(folder, beforeFileName), Path.Combine(baseFolder, beforeFileName));

                var result = RunConversion(targetFile);
                Assert.Equal(13, result.ExitCode);

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                WixAssert.StringEqual(expected, actual);

                EnsureFixed(targetFile);
            }
        }

        [Fact]
        public void DetectUnconvertableQtExecCmdTimeout()
        {
            const string beforeFileName = "v3.wxs";
            const string afterFileName = "v4_expected.wxs";
            var folder = TestData.Get(@"TestData\QtExec.bad");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder(true);
                var targetFile = Path.Combine(baseFolder, beforeFileName);
                File.Copy(Path.Combine(folder, beforeFileName), Path.Combine(baseFolder, beforeFileName));

                var result = RunConversion(targetFile);

                Assert.Equal(13, result.ExitCode);
                Assert.Single(result.Messages, message => message.ToString().EndsWith("(QtExecCmdTimeoutAmbiguous)"));

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                WixAssert.StringEqual(expected, actual);

                // still fails because QtExecCmdTimeoutAmbiguous is unfixable
                var result2 = RunConversion(targetFile);
                Assert.Equal(1, result2.ExitCode);
            }
        }

        private static WixRunnerResult RunConversion(string targetFile, bool fixErrors = true, string settingsFile = null)
        {
            var serviceProvider = WixToolsetServiceProviderFactory.CreateServiceProvider().AddConverter();

            var exitCode = WixRunner.Execute(new[]
            {
                    "convert",
                    fixErrors ? null : "--dry-run",
                    String.IsNullOrEmpty(settingsFile) ? null : "-set1" + settingsFile,
                    targetFile
                }, serviceProvider, out var messages);

            return new WixRunnerResult { ExitCode = exitCode.Result, Messages = messages.ToArray() };
        }

        private static void EnsureFixed(string targetFile)
        {
            var messaging2 = new MockMessaging();
            var converter2 = new WixConverter(messaging2, 4);
            var errors2 = converter2.ConvertFile(targetFile, true);
            Assert.Equal(0, errors2);
        }
    }
}
