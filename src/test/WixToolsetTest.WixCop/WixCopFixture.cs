// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.WixCop
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using Xunit;

    public class WixCopFixture
    {
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

                var actualLines = File.ReadAllLines(targetFile);
                var expectedLines = File.ReadAllLines(Path.Combine(folder, afterFileName));

                for (var i = 0; i < actualLines.Length && i < expectedLines.Length; ++i)
                {
                    Assert.Equal(expectedLines[i], actualLines[i]);
                }
                Assert.Equal(expectedLines.Length, actualLines.Length);

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

                var actualLines = File.ReadAllLines(targetFile);
                var expectedLines = File.ReadAllLines(Path.Combine(folder, afterFileName));
                Assert.Equal(expectedLines, actualLines);

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
    }
}
