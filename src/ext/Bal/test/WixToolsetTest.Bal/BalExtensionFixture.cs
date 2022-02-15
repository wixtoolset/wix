// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Bal
{
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixBuildTools.TestSupport;
    using WixToolset.Bal;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class BalExtensionFixture
    {
        [Fact(Skip = "xxxxx CodeBase Issue: We can't determine the file path from which we were loaded. xxxxx")]
        public void CanBuildUsingDisplayInternalUICondition()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixStdBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "DisplayInternalUIConditionBundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", Path.Combine(bundleSourceFolder, "data"),
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var balPackageInfos = extractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:WixBalPackageInfo");
                var balPackageInfo = (XmlNode)Assert.Single(balPackageInfos);
                Assert.Equal("<WixBalPackageInfo PackageId='test.msi' DisplayInternalUICondition='1' />", balPackageInfo.GetTestXml());

                Assert.True(File.Exists(Path.Combine(baFolderPath, "thm.wxl")));
            }
        }

        [Fact(Skip = "xxxxx CodeBase Issue: We can't determine the file path from which we were loaded. xxxxx")]
        public void CanBuildUsingOverridable()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\Overridable");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var balOverridableVariables = extractResult.SelectBADataNodes("/ba:BootstrapperApplicationData/ba:WixStdbaOverridableVariable");
                var balOverridableVariable = (XmlNode)Assert.Single(balOverridableVariables);
                Assert.Equal("<WixStdbaOverridableVariable Name='TEST1' />", balOverridableVariable.GetTestXml());
            }
        }

        [Fact(Skip = "xxxxx CodeBase Issue: We can't determine the file path from which we were loaded. xxxxx")]
        public void CanBuildUsingWixStdBa()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\WixStdBa");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));
            }
        }

        [Fact]
        public void CantBuildUsingMBAWithNoPrereqs()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\MBA");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundleFile,
                });
                Assert.Equal(6802, compileResult.ExitCode);
                Assert.Equal("There must be at least one PrereqPackage when using the ManagedBootstrapperApplicationHost.\nThis is typically done by using the WixNetFxExtension and referencing one of the NetFxAsPrereq package groups.", compileResult.Messages[0].ToString());

                Assert.False(File.Exists(bundleFile));
                Assert.False(File.Exists(Path.Combine(intermediateFolder, "test.exe")));
            }
        }

        [Fact]
        public void CantBuildUsingOverridableWrongCase()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\Overridable");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "WrongCaseBundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.Bal.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundleFile,
                });
                Assert.Equal((int)BalErrors.Ids.NonUpperCaseOverridableVariable, compileResult.ExitCode);
            }
        }
    }
}
