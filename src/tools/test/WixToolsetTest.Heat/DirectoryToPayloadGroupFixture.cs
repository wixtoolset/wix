// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Heat
{
    using System;
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixInternal.MSTestSupport;

    [TestClass]
    public class DirectoryToPayloadGroupFixture
    {
        [TestMethod]
        public void CanHarvestSimpleDirectory()
        {
            var folder = TestData.Get("TestData", "SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var outputPath = Path.Combine(fs.GetFolder(), "out.wxs");

                var args = new[]
                {
                    "dir", folder,
                    "-generate", "payloadgroup",
                    "-o", outputPath
                };

                var result = HeatRunner.Execute(args);
                result.AssertSuccess();

                var wxs = File.ReadAllLines(outputPath).Select(s => s.Replace("\"", "'")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                    "    <Fragment>",
                    "        <PayloadGroup Id='TARGETDIR'>",
                    "            <Payload SourceFile='SourceDir\\a.txt' />",
                    "        </PayloadGroup>",
                    "    </Fragment>",
                    "</Wix>",
                }, wxs);
            }
        }

        [TestMethod]
        public void CanHarvestSimpleDirectoryWithSourceDirSubstitution()
        {
            var folder = TestData.Get("TestData", "SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var outputPath = Path.Combine(fs.GetFolder(), "out.wxs");

                var args = new[]
                {
                    "dir", folder,
                    "-generate", "payloadgroup",
                    "-var", "var.RootDir",
                    "-o", outputPath
                };

                var result = HeatRunner.Execute(args);
                result.AssertSuccess();

                var wxs = File.ReadAllLines(outputPath).Select(s => s.Replace("\"", "'")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                    "    <Fragment>",
                    "        <PayloadGroup Id='TARGETDIR'>",
                    "            <Payload SourceFile='$(var.RootDir)\\a.txt' />",
                    "        </PayloadGroup>",
                    "    </Fragment>",
                    "</Wix>",
                }, wxs);
            }
        }

        [TestMethod]
        public void CanHarvestNestedFiles()
        {
            var folder = TestData.Get("TestData", "NestedFiles");

            using (var fs = new DisposableFileSystem())
            {
                var outputPath = Path.Combine(fs.GetFolder(), "out.wxs");

                var args = new[]
                {
                    "dir", folder,
                    "-generate", "payloadgroup",
                    "-o", outputPath
                };

                var result = HeatRunner.Execute(args);
                result.AssertSuccess();

                var wxs = File.ReadAllLines(outputPath).Select(s => s.Replace("\"", "'")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                    "    <Fragment>",
                    "        <PayloadGroup Id='TARGETDIR'>",
                    "            <Payload SourceFile='SourceDir\\Nested\\c.txt' Name='Nested\\c.txt' />",
                    "            <Payload SourceFile='SourceDir\\b.txt' />",
                    "        </PayloadGroup>",
                    "    </Fragment>",
                    "</Wix>",
                }, wxs);
            }
        }
    }
}
