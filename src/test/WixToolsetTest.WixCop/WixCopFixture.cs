// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.WixCop
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using Xunit;

    public class WixCopFixture
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

                var runner = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result = runner.Execute();

                Assert.Equal(2, result.ExitCode);

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                Assert.Equal(expected, actual);

                var runner2 = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result2 = runner2.Execute();

                Assert.Equal(0, result2.ExitCode);
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

                var runner = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result = runner.Execute();

                Assert.Equal(2, result.ExitCode);

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                Assert.Equal(expected, actual);

                var runner2 = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result2 = runner2.Execute();

                Assert.Equal(0, result2.ExitCode);
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

                var runner = new WixCopRunner
                {
                    FixErrors = true,
                    SettingFile1 = Path.Combine(folder, "wixcop.settings.xml"),
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result = runner.Execute();

                Assert.Equal(2, result.ExitCode);

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                Assert.Equal(expected, actual);

                var runner2 = new WixCopRunner
                {
                    FixErrors = true,
                    SettingFile1 = Path.Combine(folder, "wixcop.settings.xml"),
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result2 = runner2.Execute();

                Assert.Equal(0, result2.ExitCode);
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

                var runner = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result = runner.Execute();

                Assert.Equal(2, result.ExitCode);

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                Assert.Equal(expected, actual);

                var runner2 = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result2 = runner2.Execute();

                Assert.Equal(0, result2.ExitCode);
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

                var runner = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result = runner.Execute();

                Assert.Equal(2, result.ExitCode);

                Assert.Single(result.Messages.Where(message => message.ToString().EndsWith("(QtExecCmdTimeoutAmbiguous)")));

                var expected = File.ReadAllText(Path.Combine(folder, afterFileName)).Replace("\r\n", "\n");
                var actual = File.ReadAllText(targetFile).Replace("\r\n", "\n");
                Assert.Equal(expected, actual);

                // still fails because QtExecCmdTimeoutAmbiguous is unfixable
                var runner2 = new WixCopRunner
                {
                    FixErrors = true,
                    SearchPatterns =
                    {
                        targetFile,
                    },
                };

                var result2 = runner2.Execute();

                Assert.Equal(2, result2.ExitCode);
            }
        }
    }
}
