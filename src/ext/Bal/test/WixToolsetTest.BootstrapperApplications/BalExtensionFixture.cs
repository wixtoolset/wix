// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BootstrapperApplications
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixToolset.BootstrapperApplications;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using Xunit;

    public class BalExtensionFixture
    {
        [Fact]
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
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", Path.Combine(bundleSourceFolder, "data"),
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var balPackageInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixBalPackageInfo");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixBalPackageInfo PackageId='test.msi' DisplayInternalUICondition='1' />",
                }, balPackageInfos);

                Assert.True(File.Exists(Path.Combine(baFolderPath, "thm.wxl")));
            }
        }

        [Fact]
        public void CanBuildUsingDisplayFilesInUseDialogCondition()
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
                    Path.Combine(bundleSourceFolder, "DisplayFilesInUseDialogConditionBundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", Path.Combine(bundleSourceFolder, "data"),
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var balPackageInfos = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixBalPackageInfo");
                WixAssert.CompareLineByLine(new string[]
                {
                    "<WixBalPackageInfo PackageId='test.msi' DisplayFilesInUseDialogCondition='1' />",
                }, balPackageInfos);

                Assert.True(File.Exists(Path.Combine(baFolderPath, "thm.wxl")));
            }
        }

        [Fact]
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
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var balCommandLines = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixStdbaCommandLine");
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixStdbaCommandLine VariableType='caseInsensitive' />",
                }, balCommandLines);

                var balOverridableVariables = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixStdbaOverridableVariable");
                WixAssert.CompareLineByLine(new[]
                {
                    "<WixStdbaOverridableVariable Name='TEST1' />",
                }, balOverridableVariables);
            }
        }

        [Fact]
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
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundleFile,
                });
                compileResult.AssertSuccess();

                Assert.True(File.Exists(bundleFile));
            }
        }

        //[Fact]
        //public void CanBuildUsingMBAWithAlwaysInstallPrereqs()
        //{
        //    using (var fs = new DisposableFileSystem())
        //    {
        //        var baseFolder = fs.GetFolder();
        //        var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
        //        var bundleSourceFolder = TestData.Get("TestData", "MBA");
        //        var intermediateFolder = Path.Combine(baseFolder, "obj");
        //        var baFolderPath = Path.Combine(baseFolder, "ba");
        //        var extractFolderPath = Path.Combine(baseFolder, "extract");

        //        var compileResult = WixRunner.Execute(new[]
        //        {
        //            "build",
        //            Path.Combine(bundleSourceFolder, "AlwaysInstallPrereqsBundle.wxs"),
        //            "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
        //            "-intermediateFolder", intermediateFolder,
        //            "-o", bundleFile,
        //        });

        //        compileResult.AssertSuccess();

        //        Assert.True(File.Exists(bundleFile));

        //        var extractResult = BundleExtractor.ExtractBAContainer(null, bundleFile, baFolderPath, extractFolderPath);
        //        extractResult.AssertSuccess();

        //        var wixPrereqOptionsElements = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixPrereqOptions");
        //        WixAssert.CompareLineByLine(new[]
        //        {
        //            "<WixPrereqOptions AlwaysInstallPrereqs='1' />",
        //        }, wixPrereqOptionsElements);

        //        var wixPrereqInformationElements = extractResult.GetBADataTestXmlLines("/ba:BootstrapperApplicationData/ba:WixPrereqInformation");
        //        WixAssert.CompareLineByLine(new[]
        //        {
        //            "<WixPrereqInformation PackageId='wixnative.exe' />",
        //        }, wixPrereqInformationElements);
        //    }
        //}

        [Fact]
        public void CannotBuildUsingMBAWithNoPrereqs()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData", "MBA");
                var dataFolder = TestData.Get(@"TestData", ".Data");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-bindpath", dataFolder,
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The WixManagedBootstrapperApplicationHost element has been deprecated.",
                }, compileResult.Messages.Select(m => m.ToString()).ToArray());
                Assert.Equal(1130, compileResult.ExitCode);

                Assert.False(File.Exists(bundleFile));
                Assert.False(File.Exists(Path.Combine(intermediateFolder, "test.exe")));
            }
        }

        [Fact]
        public void CannotBuildUsingDncbaMissingBAFactoryPayload()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData\Dncba");
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var compileResult = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Bundle.wxs"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundleFile,
                });

                WixAssert.CompareLineByLine(new[]
                {
                    "The WixDotNetCoreBootstrapperApplicationHost element has been deprecated.",
                    "The BootstrapperApplication element's Name or SourceFile attribute was not found; one of these is required."
                }, compileResult.Messages.Select(x => x.ToString()).ToArray());
                Assert.Equal(44, compileResult.ExitCode);

                Assert.False(File.Exists(bundleFile));
                Assert.False(File.Exists(Path.Combine(intermediateFolder, "test.exe")));
            }
        }

        [Fact]
        public void CannotBuildUsingOverridableWrongCase()
        {
            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var bundleFile = Path.Combine(baseFolder, "bin", "test.exe");
                var bundleSourceFolder = TestData.Get(@"TestData");
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(bundleSourceFolder, "Overridable", "WrongCaseBundle.wxs"),
                    "-loc", Path.Combine(bundleSourceFolder, "Overridable", "WrongCaseBundle.wxl"),
                    "-bindpath", Path.Combine(bundleSourceFolder, "WixStdBa", "Data"),
                    "-ext", TestData.Get(@"WixToolset.BootstrapperApplications.wixext.dll"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundleFile,
                });

                Assert.InRange(result.ExitCode, 2, Int32.MaxValue);

                var messages = result.Messages.Select(m => m.ToString()).ToList();
                messages.Sort();

                WixAssert.CompareLineByLine(new[]
                {
                    "bal:Condition/@Condition contains the built-in Variable 'WixBundleAction', which is not available when it is evaluated. (Unavailable Variables are: 'WixBundleAction'.). Rewrite the condition to avoid Variables that are never valid during its evaluation.",
                    "Overridable variable 'TEST1' collides with 'Test1' with Bundle/@CommandLineVariables value 'caseInsensitive'.",
                    "The *Package/@bal:DisplayFilesInUseDialogCondition attribute's value '=' is not a valid bundle condition.",
                    "The *Package/@bal:DisplayInternalUICondition attribute's value '=' is not a valid bundle condition.",
                    "The location of the Variable related to the previous error.",
                }, messages.ToArray());
            }
        }
    }
}
