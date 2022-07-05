// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.UI
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.UI;
    using Xunit;

    public class UIExtensionFixture
    {
        [Fact]
        public void CanBuildUsingWixUIAdvanced()
        {
            var folder = TestData.Get(@"TestData\WixUI_Advanced");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Binary", "Dialog", "CustomAction");
            Assert.Single(results, result => result.StartsWith("Dialog:AdvancedWelcomeEulaDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
                "Binary:WixUiCa_X86\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixSetDefaultPerMachineFolder\t51\tWixPerMachineFolder\t[ProgramFilesFolder][ApplicationFolderName]\t",
                "CustomAction:WixSetDefaultPerUserFolder\t51\tWixPerUserFolder\t[LocalAppDataFolder]Apps\\[ApplicationFolderName]\t",
                "CustomAction:WixSetPerMachineFolder\t51\tAPPLICATIONFOLDER\t[WixPerMachineFolder]\t",
                "CustomAction:WixSetPerUserFolder\t51\tAPPLICATIONFOLDER\t[WixPerUserFolder]\t",
                "CustomAction:WixUIPrintEula\t65\tWixUiCa_X86\tPrintEula\t",
                "CustomAction:WixUIValidatePath\t65\tWixUiCa_X86\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIAdvancedX64()
        {
            var folder = TestData.Get(@"TestData\WixUI_Advanced");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "Dialog", "CustomAction");
            Assert.Single(results, result => result.StartsWith("Dialog:AdvancedWelcomeEulaDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
                "Binary:WixUiCa_X64\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixSetDefaultPerMachineFolder\t51\tWixPerMachineFolder\t[ProgramFilesFolder][ApplicationFolderName]\t",
                "CustomAction:WixSetDefaultPerUserFolder\t51\tWixPerUserFolder\t[LocalAppDataFolder]Apps\\[ApplicationFolderName]\t",
                "CustomAction:WixSetPerMachineFolder\t51\tAPPLICATIONFOLDER\t[WixPerMachineFolder]\t",
                "CustomAction:WixSetPerUserFolder\t51\tAPPLICATIONFOLDER\t[WixPerUserFolder]\t",
                "CustomAction:WixUIPrintEula\t65\tWixUiCa_X64\tPrintEula\t",
                "CustomAction:WixUIValidatePath\t65\tWixUiCa_X64\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIAdvancedARM64()
        {
            var folder = TestData.Get(@"TestData\WixUI_Advanced");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildARM64, "Binary", "Dialog", "CustomAction");
            Assert.Single(results, result => result.StartsWith("Dialog:AdvancedWelcomeEulaDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
                "Binary:WixUiCa_A64\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixSetDefaultPerMachineFolder\t51\tWixPerMachineFolder\t[ProgramFilesFolder][ApplicationFolderName]\t",
                "CustomAction:WixSetDefaultPerUserFolder\t51\tWixPerUserFolder\t[LocalAppDataFolder]Apps\\[ApplicationFolderName]\t",
                "CustomAction:WixSetPerMachineFolder\t51\tAPPLICATIONFOLDER\t[WixPerMachineFolder]\t",
                "CustomAction:WixSetPerUserFolder\t51\tAPPLICATIONFOLDER\t[WixPerUserFolder]\t",
                "CustomAction:WixUIPrintEula\t65\tWixUiCa_A64\tPrintEula\t",
                "CustomAction:WixUIValidatePath\t65\tWixUiCa_A64\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIFeatureTree()
        {
            var folder = TestData.Get(@"TestData\WixUI_FeatureTree");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Binary", "Dialog", "CustomAction");
            Assert.Single(results, result => result.StartsWith("Dialog:WelcomeDlg\t"));
            Assert.Single(results, result => result.StartsWith("Dialog:CustomizeDlg\t"));
            Assert.Empty(results.Where(result => result.StartsWith("Dialog:SetupTypeDlg\t")));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
                "Binary:WixUiCa_X86\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixUIPrintEula\t65\tWixUiCa_X86\tPrintEula\t",
                "CustomAction:WixUIValidatePath\t65\tWixUiCa_X86\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIInstallDir()
        {
            var folder = TestData.Get(@"TestData\WixUI_InstallDir");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Binary", "Dialog", "CustomAction");
            Assert.Single(results, result => result.StartsWith("Dialog:InstallDirDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
                "Binary:WixUiCa_X86\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixUIPrintEula\t65\tWixUiCa_X86\tPrintEula\t",
                "CustomAction:WixUIValidatePath\t65\tWixUiCa_X86\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIMinimal()
        {
            var folder = TestData.Get(@"TestData\WixUI_Minimal");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Binary", "Dialog", "CustomAction");
            Assert.Single(results, result => result.StartsWith("Dialog:WelcomeEulaDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
                "Binary:WixUiCa_X86\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixUIPrintEula\t65\tWixUiCa_X86\tPrintEula\t",
                "CustomAction:WixUIValidatePath\t65\tWixUiCa_X86\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIMinimalInKazakh()
        {
            var folder = TestData.Get(@"TestData\WixUI_Minimal");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildInKazakh, "Dialog");
            var welcomeDlg = results.Where(r => r.StartsWith("Dialog:WelcomeDlg\t")).Select(r => r.Split('\t')).Single();
            Assert.Equal("[ProductName] бағдарламасын орнату", welcomeDlg[6]);
        }

        [Fact]
        public void CanBuildUsingWixUIMinimalAndReadPdb()
        {
            var folder = TestData.Get(@"TestData\WixUI_Minimal");
            var bindFolder = TestData.Get(@"TestData\data");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                Build(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-ext", Path.GetFullPath(new Uri(typeof(UIExtensionFactory).Assembly.CodeBase).LocalPath),
                    "-bindpath", bindFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                var wid = WindowsInstallerData.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var dialogTable = wid.Tables["Dialog"];
                var dialogRow = dialogTable.Rows.Single(r => r.GetPrimaryKey() == "WelcomeEulaDlg");
            }
        }

        [Fact]
        public void CanBuildUsingWixUIMondo()
        {
            var folder = TestData.Get(@"TestData\WixUI_Mondo");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Binary", "Dialog", "CustomAction");
            Assert.Single(results, result => result.StartsWith("Dialog:WelcomeDlg\t"));
            Assert.Single(results, result => result.StartsWith("Dialog:CustomizeDlg\t"));
            Assert.Single(results, result => result.StartsWith("Dialog:SetupTypeDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
                "Binary:WixUiCa_X86\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixUIPrintEula\t65\tWixUiCa_X86\tPrintEula\t",
                "CustomAction:WixUIValidatePath\t65\tWixUiCa_X86\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
        }

        [Fact]
        public void CanBuildUsingWixUIMondoLocalized()
        {
            var folder = TestData.Get(@"TestData\WixUI_Mondo");
            var bindFolder = TestData.Get(@"TestData\data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildInGerman, "Control");
            WixAssert.CompareLineByLine(new[]
            {
                "&Ja",
            }, results.Where(s => s.StartsWith("Control:ErrorDlg\tY")).Select(s => s.Split('\t')[9]).ToArray());
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }

        private static void BuildX64(string[] args)
        {
            var result = WixRunner.Execute(args.Concat(new[] { "-arch", "x64" }).ToArray())
                                  .AssertSuccess();
        }

        private static void BuildARM64(string[] args)
        {
            var result = WixRunner.Execute(args.Concat(new[] { "-arch", "arm64" }).ToArray())
                                  .AssertSuccess();
        }

        private static void BuildInGerman(string[] args)
        {
            var localizedArgs = args.Append("-culture").Append("de-DE").ToArray();

            var result = WixRunner.Execute(localizedArgs)
                                  .AssertSuccess();
        }

        private static void BuildInKazakh(string[] args)
        {
            var localizedArgs = args.Append("-culture").Append("kk-KZ").ToArray();

            var result = WixRunner.Execute(localizedArgs)
                                  .AssertSuccess();
        }
    }
}
