// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Heat
{
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixInternal.MSTestSupport;

    [TestClass]
    public class HeatFixture
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
                    "-o", outputPath
                };

                var result = HeatRunner.Execute(args);
                result.AssertSuccess();

                var wxs = File.ReadAllLines(outputPath).Select(s => s.Replace("\"", "'")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                    "    <Fragment>",
                    "        <StandardDirectory Id='TARGETDIR'>",
                    "            <Directory Id='dirwsJn0Cqs9KdlDSFdQsu9ygYvMF8' Name='SingleFile' />",
                    "        </StandardDirectory>",
                    "    </Fragment>",
                    "    <Fragment>",
                    "        <DirectoryRef Id='dirwsJn0Cqs9KdlDSFdQsu9ygYvMF8'>",
                    "            <Component Id='cmp0i3dThrp4nheCteEmXvHxBDa_VE' Guid='PUT-GUID-HERE'>",
                    "                <File Id='filziMcXYgrmcbVF8PuTUfIB9Vgqo0' KeyPath='yes' Source='SourceDir\\a.txt' />",
                    "            </Component>",
                    "        </DirectoryRef>",
                    "    </Fragment>",
                    "</Wix>",
                }, wxs);
            }
        }

        [TestMethod]
        public void CanHarvestSimpleDirectoryToComponentGroup()
        {
            var folder = TestData.Get("TestData", "SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var outputPath = Path.Combine(fs.GetFolder(), "out.wxs");

                var args = new[]
                {
                    "dir", folder,
                    "-cg", "CG1",
                    "-o", outputPath
                };

                var result = HeatRunner.Execute(args);
                result.AssertSuccess();

                var wxs = File.ReadAllLines(outputPath).Select(s => s.Replace("\"", "'")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                    "    <Fragment>",
                    "        <StandardDirectory Id='TARGETDIR'>",
                    "            <Directory Id='dirwsJn0Cqs9KdlDSFdQsu9ygYvMF8' Name='SingleFile' />",
                    "        </StandardDirectory>",
                    "    </Fragment>",
                    "    <Fragment>",
                    "        <ComponentGroup Id='CG1'>",
                    "            <Component Id='cmp0i3dThrp4nheCteEmXvHxBDa_VE' Directory='dirwsJn0Cqs9KdlDSFdQsu9ygYvMF8' Guid='PUT-GUID-HERE'>",
                    "                <File Id='filziMcXYgrmcbVF8PuTUfIB9Vgqo0' KeyPath='yes' Source='SourceDir\\a.txt' />",
                    "            </Component>",
                    "        </ComponentGroup>",
                    "    </Fragment>",
                    "</Wix>",
                }, wxs);
            }
        }

        [TestMethod]
        public void CanHarvestSimpleDirectoryToInstallFolder()
        {
            var folder = TestData.Get("TestData", "SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var outputPath = Path.Combine(fs.GetFolder(), "out.wxs");

                var args = new[]
                {
                    "dir", folder,
                    "-dr", "INSTALLFOLDER",
                    "-indent", "2",
                    "-o", outputPath
                };

                var result = HeatRunner.Execute(args);
                result.AssertSuccess();

                var wxs = File.ReadAllLines(outputPath).Select(s => s.Replace("\"", "'")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                    "  <Fragment>",
                    "    <DirectoryRef Id='INSTALLFOLDER'>",
                    "      <Directory Id='dirlooNEIrtEBL2w_RhFEIgiKcUlxE' Name='SingleFile' />",
                    "    </DirectoryRef>",
                    "  </Fragment>",
                    "  <Fragment>",
                    "    <DirectoryRef Id='dirlooNEIrtEBL2w_RhFEIgiKcUlxE'>",
                    "      <Component Id='cmpxHVF6oXohc0EWgRphmYZvw5.GGU' Guid='PUT-GUID-HERE'>",
                    "        <File Id='filk_7KUAfL4VfzxSRsGFf_XOBHln0' KeyPath='yes' Source='SourceDir\\a.txt' />",
                    "      </Component>",
                    "    </DirectoryRef>",
                    "  </Fragment>",
                    "</Wix>",
                }, wxs);
            }
        }

        [TestMethod]
        public void CanHarvestFile()
        {
            var folder = TestData.Get("TestData", "SingleFile");

            using (var fs = new DisposableFileSystem())
            {
                var outputPath = Path.Combine(fs.GetFolder(), "out.wxs");

                var args = new[]
                {
                    "file", Path.Combine(folder, "a.txt"),
                    "-cg", "GroupA",
                    "-dr", "ProgramFiles6432Folder",
                    "-o", outputPath

                };

                var result = HeatRunner.Execute(args);
                result.AssertSuccess();

                var wxs = File.ReadAllLines(outputPath).Select(s => s.Replace("\"", "'")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                    "    <Fragment>",
                    "        <StandardDirectory Id='ProgramFiles6432Folder'>",
                    "            <Directory Id='dirl6r_Yc0gvMUEUVe5rioOOIIasoU' Name='SingleFile' />",
                    "        </StandardDirectory>",
                    "    </Fragment>",
                    "    <Fragment>",
                    "        <ComponentGroup Id='GroupA'>",
                    "            <Component Id='cmpfBcW61_XzosRWK3EzUTBtJrcJD8' Directory='dirl6r_Yc0gvMUEUVe5rioOOIIasoU' Guid='PUT-GUID-HERE'>",
                    "                <File Id='filKrZgaIOSKpNZXFnezZc9X.LKGpw' KeyPath='yes' Source='SourceDir\\SingleFile\\a.txt' />",
                    "            </Component>",
                    "        </ComponentGroup>",
                    "    </Fragment>",
                    "</Wix>",
                }, wxs);
            }
        }

        [TestMethod]
        public void CanHarvestRegistry()
        {
            var folder = TestData.Get("TestData", "RegFile");

            using (var fs = new DisposableFileSystem())
            {
                var outputPath = Path.Combine(fs.GetFolder(), "out.wxs");

                var args = new[]
                {
                    "reg", Path.Combine(folder, "input.reg"),
                    "-o", outputPath
                };

                var result = HeatRunner.Execute(args);
                result.AssertSuccess();

                var actual = File.ReadAllLines(outputPath).Select(s => s.Replace("\"", "'")).ToArray();
                var expected = File.ReadAllLines(Path.Combine(folder, "Expected.wxs")).Select(s => s.Replace("\"", "'")).ToArray();
                WixAssert.CompareLineByLine(expected, actual);
            }
        }

        [TestMethod]
        public void CanHarvestRegistryIntoComponentGroup()
        {
            var folder = TestData.Get("TestData", "RegFile");

            using (var fs = new DisposableFileSystem())
            {
                var outputPath = Path.Combine(fs.GetFolder(), "out.wxs");

                var args = new[]
                {
                    "reg", Path.Combine(folder, "input.reg"),
                    "-cg", "CG1",
                    "-o", outputPath
                };

                var result = HeatRunner.Execute(args);
                result.AssertSuccess();

                var actual = File.ReadAllLines(outputPath).Select(s => s.Replace("\"", "'")).ToArray();
                var expected = File.ReadAllLines(Path.Combine(folder, "ExpectedWithComponentGroup.wxs")).Select(s => s.Replace("\"", "'")).ToArray();
                WixAssert.CompareLineByLine(expected, actual);
            }
        }
    }
}
