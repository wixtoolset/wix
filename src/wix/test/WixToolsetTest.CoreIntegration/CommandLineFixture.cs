// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class CommandLineFixture
    {
        [Fact]
        public void SwitchIsNotConsideredAnArgument()
        {
            var result = WixRunner.Execute(new[]
            {
                "build",
                "-bindpath", "-thisisaswitchnotanarg",
            });

            WixAssert.CompareLineByLine(new[]
            {
                "-bindpath is expected to be followed by a value. See -? for additional detail.",
                "Additional argument '-bindpath' was unexpected.  Remove the argument and add the '-?' switch for more information.",
                "No source files specified."
            }, result.Messages.Select(m => m.ToString()).ToArray());
            Assert.Equal(391, result.ExitCode);
        }

        [Fact]
        public void CanBuildWithNoOutputSpecified()
        {
            var folder = TestData.Get(@"TestData", "SimplePackage");
            var bindFolder = TestData.Get(@"TestData", "SingleFile", "data");

            using (var fs = new DisposableFileSystem())
            {
                var testFolder = fs.GetFolder(create: true);
                var srcFile = Path.Combine(testFolder, "SimplePackage.wxs");
                var intermediateFolder = Path.Combine(testFolder, "obj");
                var expectedPath = Path.Combine(testFolder, "SimplePackage.msi");

                // Copy the source folder into the test working folder so the output can be written to the same folder.
                File.Copy(Path.Combine(folder, "SimplePackage.wxs"), srcFile);

                var result = WixRunner.Execute(new[]
                {
                    "build", srcFile,
                    "-bindpath", bindFolder,
                    "-intermediateFolder", intermediateFolder
                });

                result.AssertSuccess();
                Assert.True(File.Exists(expectedPath), $"Expected to build MSI to: {expectedPath}");
            }
        }
    }
}
