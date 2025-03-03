// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.UI
{
    using System.IO;
    using System.Linq;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixInternal.Core.MSTestPackage;
    using WixInternal.MSTestSupport;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.UI;

    [TestClass]
    public class UIExtensionFixture
    {
        [TestMethod]
        public void CanBuildUsingWixUIAdvanced()
        {
            var folder = TestData.Get(@"TestData", "WixUI_Advanced");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Binary", "Dialog", "CustomAction", "ControlEvent", "InstallUISequence");
            WixAssert.Single(results, result => result.StartsWith("Dialog:AdvancedWelcomeEulaDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixSetDefaultPerMachineFolder\t51\tWixPerMachineFolder\t[ProgramFilesFolder][ApplicationFolderName]\t",
                "CustomAction:WixSetDefaultPerUserFolder\t51\tWixPerUserFolder\t[LocalAppDataFolder]Apps\\[ApplicationFolderName]\t",
                "CustomAction:WixSetPerMachineFolder\t51\tAPPLICATIONFOLDER\t[WixPerMachineFolder]\t",
                "CustomAction:WixSetPerUserFolder\t51\tAPPLICATIONFOLDER\t[WixPerUserFolder]\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
            WixAssert.Empty(results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("DoAction")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "InstallUISequence:AdvancedWelcomeEulaDlg\tNOT Installed\t1297",
                "InstallUISequence:WelcomeDlg\tInstalled AND PATCH\t1296",
            }, results.Where(r => r.StartsWith("InstallUISequence:AdvancedWelcome") || r.StartsWith("InstallUISequence:Welcome")).ToArray());
        }

        [TestMethod]
        public void CanBuildUsingWixUIAdvancedX64()
        {
            var folder = TestData.Get(@"TestData", "WixUI_Advanced");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "Dialog", "CustomAction", "ControlEvent", "InstallUISequence");
            WixAssert.Single(results, result => result.StartsWith("Dialog:AdvancedWelcomeEulaDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixSetDefaultPerMachineFolder\t51\tWixPerMachineFolder\t[ProgramFilesFolder][ApplicationFolderName]\t",
                "CustomAction:WixSetDefaultPerUserFolder\t51\tWixPerUserFolder\t[LocalAppDataFolder]Apps\\[ApplicationFolderName]\t",
                "CustomAction:WixSetPerMachineFolder\t51\tAPPLICATIONFOLDER\t[WixPerMachineFolder]\t",
                "CustomAction:WixSetPerUserFolder\t51\tAPPLICATIONFOLDER\t[WixPerUserFolder]\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
            WixAssert.Empty(results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("DoAction")).ToArray());
        }

        [TestMethod]
        public void CanBuildUsingWixUIAdvancedARM64()
        {
            var folder = TestData.Get(@"TestData", "WixUI_Advanced");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildARM64, "Binary", "Dialog", "CustomAction", "ControlEvent", "InstallUISequence");
            WixAssert.Single(results, result => result.StartsWith("Dialog:AdvancedWelcomeEulaDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:WixSetDefaultPerMachineFolder\t51\tWixPerMachineFolder\t[ProgramFilesFolder][ApplicationFolderName]\t",
                "CustomAction:WixSetDefaultPerUserFolder\t51\tWixPerUserFolder\t[LocalAppDataFolder]Apps\\[ApplicationFolderName]\t",
                "CustomAction:WixSetPerMachineFolder\t51\tAPPLICATIONFOLDER\t[WixPerMachineFolder]\t",
                "CustomAction:WixSetPerUserFolder\t51\tAPPLICATIONFOLDER\t[WixPerUserFolder]\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
            WixAssert.Empty(results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("DoAction")).ToArray());
        }

        [TestMethod]
        public void CanBuildUsingWixUIFeatureTree()
        {
            var folder = TestData.Get(@"TestData", "WixUI_FeatureTree");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "Dialog", "CustomAction", "ControlEvent", "InstallUISequence");
            WixAssert.Single(results, result => result.StartsWith("Dialog:WelcomeDlg\t"));
            WixAssert.Single(results, result => result.StartsWith("Dialog:CustomizeDlg\t"));
            WixAssert.Empty(results.Where(result => result.StartsWith("Dialog:SetupTypeDlg\t")));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:SetWIXUI_EXITDIALOGOPTIONALTEXT\t51\tWIXUI_EXITDIALOGOPTIONALTEXT\tThank you for installing [ProductName].\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
            WixAssert.Empty(results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("DoAction")));
            WixAssert.CompareLineByLine(new[]
            {
                "InstallUISequence:WelcomeDlg\tNOT Installed OR PATCH\t1297",
            }, results.Where(r => r.StartsWith("InstallUISequence:AdvancedWelcome") || r.StartsWith("InstallUISequence:Welcome")).ToArray());
        }

        [TestMethod]
        public void CanBuildWithWixUIInstallDirWithCustomizedEula()
        {
            var folder = TestData.Get(@"TestData", "WixUI_InstallDir");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildEula, "Binary", "Dialog", "CustomAction", "Property", "ControlEvent", "InstallUISequence");
            WixAssert.Single(results, result => result.StartsWith("Dialog:InstallDirDlg\t"));
            WixAssert.Single(results, result => result.StartsWith("Dialog:WelcomeDlg\t"));
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
                "CustomAction:WixUIValidatePath_X86\t65\tWixUiCa_X86\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "Property:WIXUI_INSTALLDIR\tINSTALLFOLDER",
            }, results.Where(r => r.StartsWith("Property:WIXUI")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "ControlEvent:BrowseDlg\tOK\tDoAction\tWixUIValidatePath_X86\t1\t1",
                "ControlEvent:InstallDirDlg\tNext\tDoAction\tWixUIValidatePath_X86\t1\t1",
            }, results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("DoAction")).OrderBy(s => s).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "InstallUISequence:WelcomeDlg\tNOT Installed OR PATCH\t1297",
            }, results.Where(r => r.StartsWith("InstallUISequence:AdvancedWelcome") || r.StartsWith("InstallUISequence:Welcome")).ToArray());
        }

        [TestMethod]
        public void CanBuildUsingWixUIMinimal()
        {
            var folder = TestData.Get(@"TestData", "WixUI_Minimal");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Binary", "Dialog", "CustomAction", "ControlEvent", "InstallUISequence");
            WixAssert.Single(results, result => result.StartsWith("Dialog:WelcomeEulaDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.Empty(results.Where(r => r.StartsWith("CustomAction:")));
            WixAssert.Empty(results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("DoAction")));
            WixAssert.CompareLineByLine(new[]
            {
                "InstallUISequence:WelcomeDlg\tInstalled AND PATCH\t1296",
                "InstallUISequence:WelcomeEulaDlg\tNOT Installed\t1297",
            }, results.Where(r => r.StartsWith("InstallUISequence:AdvancedWelcome") || r.StartsWith("InstallUISequence:Welcome")).ToArray());
        }

        [TestMethod]
        public void CanBuildUsingWixUIMinimalInKazakh()
        {
            var folder = TestData.Get(@"TestData", "WixUI_Minimal");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildInKazakh, "Dialog");
            var welcomeDlg = results.Where(r => r.StartsWith("Dialog:WelcomeDlg\t")).Select(r => r.Split('\t')).Single();
            Assert.AreEqual("[ProductName] бағдарламасын орнату", welcomeDlg[6]);
        }

        [TestMethod]
        public void CanBuildUsingWixUIMinimalAndReadPdb()
        {
            var folder = TestData.Get(@"TestData", "WixUI_Minimal");
            var bindFolder = TestData.Get(@"TestData", "data");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                Build(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-ext", Path.GetFullPath(typeof(UIExtensionFactory).Assembly.Location),
                    "-bindpath", bindFolder,
                    "-intermediateFolder", intermediateFolder,
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msi")
                });

                var wid = WindowsInstallerData.Load(Path.Combine(intermediateFolder, @"bin\test.wixpdb"));
                var dialogTable = wid.Tables["Dialog"];
                var dialogRow = dialogTable.Rows.Single(r => r.GetPrimaryKey() == "WelcomeEulaDlg");
            }
        }

        [TestMethod]
        public void CanBuildUsingWixUIMondo()
        {
            var folder = TestData.Get(@"TestData", "WixUI_Mondo");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Binary", "Dialog", "CustomAction", "ControlEvent", "InstallUISequence");
            WixAssert.Single(results, result => result.StartsWith("Dialog:WelcomeDlg\t"));
            WixAssert.Single(results, result => result.StartsWith("Dialog:CustomizeDlg\t"));
            WixAssert.Single(results, result => result.StartsWith("Dialog:SetupTypeDlg\t"));
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
                "CustomAction:WixUIValidatePath_X86\t65\tWixUiCa_X86\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "ControlEvent:BrowseDlg\tOK\tDoAction\tWixUIValidatePath_X86\t1\t1",
            }, results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("DoAction")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "InstallUISequence:WelcomeDlg\tNOT Installed OR PATCH\t1297",
            }, results.Where(r => r.StartsWith("InstallUISequence:AdvancedWelcome") || r.StartsWith("InstallUISequence:Welcome")).ToArray());
        }

        [TestMethod]
        public void CanBuildUsingWixUIMondoLocalized()
        {
            var folder = TestData.Get(@"TestData", "WixUI_Mondo");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildInGerman, "Control");
            WixAssert.CompareLineByLine(new[]
            {
                "&Ja",
            }, results.Where(s => s.StartsWith("Control:ErrorDlg\tY")).Select(s => s.Split('\t')[9]).ToArray());
        }

        [TestMethod]
        public void CanBuildWithInstallDirAndRemovedDialog()
        {
            var folder = TestData.Get(@"TestData", "InstallDir_NoLicense");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(Build, "Binary", "Dialog", "CustomAction", "Property", "ControlEvent", "InstallUISequence");
            WixAssert.Single(results, result => result.StartsWith("Dialog:InstallDirDlg\t"));
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
                "CustomAction:WixUIValidatePath_X86\t65\tWixUiCa_X86\tValidatePath\t",
            }, results.Where(r => r.StartsWith("CustomAction:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "Property:WIXUI_INSTALLDIR\tINSTALLFOLDER",
            }, results.Where(r => r.StartsWith("Property:WIXUI")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "ControlEvent:BrowseDlg\tOK\tDoAction\tWixUIValidatePath_X86\t1\t3",
                "ControlEvent:InstallDirDlg\tNext\tDoAction\tWixUIValidatePath_X86\t1\t2",
            }, results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("DoAction")).OrderBy(s => s).ToArray());

            WixAssert.Empty(results.Where(result => result.Contains("LicenseAgreementDlg")).ToArray());

            WixAssert.CompareLineByLine(new[]
            {
                "InstallUISequence:WelcomeDlg\tNOT Installed OR PATCH\t1297",
            }, results.Where(r => r.StartsWith("InstallUISequence:AdvancedWelcome") || r.StartsWith("InstallUISequence:Welcome")).ToArray());
        }

        [TestMethod]
        public void CanBuildWithInstallDirAndAddedDialog()
        {
            var folder = TestData.Get(@"TestData", "InstallDir_SpecialDlg");
            var bindFolder = TestData.Get(@"TestData", "data");
            var build = new Builder(folder, typeof(UIExtensionFactory), new[] { bindFolder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "Control", "Dialog", "CustomAction", "Property", "ControlEvent", "InstallUISequence");
            WixAssert.Single(results, result => result.StartsWith("Dialog:InstallDirDlg\t"));
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:WixUI_Bmp_Banner\t[Binary data]",
                "Binary:WixUI_Bmp_Dialog\t[Binary data]",
                "Binary:WixUI_Bmp_New\t[Binary data]",
                "Binary:WixUI_Bmp_Up\t[Binary data]",
                "Binary:WixUI_Ico_Exclam\t[Binary data]",
                "Binary:WixUI_Ico_Info\t[Binary data]",
            }, results.Where(r => r.StartsWith("Binary:")).ToArray());
            WixAssert.Empty(results.Where(r => r.StartsWith("CustomAction:")).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "Property:WIXUI_INSTALLDIR\tINSTALLFOLDER",
            }, results.Where(r => r.StartsWith("Property:WIXUI")).ToArray());
            WixAssert.Empty(results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("DoAction")).OrderBy(s => s).ToArray());
            WixAssert.CompareLineByLine(new[]
            {
                "InstallUISequence:WelcomeDlg\tNOT Installed OR PATCH\t1297",
            }, results.Where(r => r.StartsWith("InstallUISequence:AdvancedWelcome") || r.StartsWith("InstallUISequence:Welcome")).ToArray());

            Assert.AreEqual(10, results.Where(result => result.StartsWith("Control:") && result.Contains("SpecialDlg")).Count());
            Assert.AreEqual(5, results.Where(result => result.StartsWith("ControlEvent:") && result.Contains("SpecialDlg")).Count());
            WixAssert.Single(results.Where(result => result.StartsWith("Dialog:") && result.Contains("SpecialDlg")));
        }

        [TestMethod]
        public void CannotBuildWithV3LikeUIRef()
        {
            var folder = TestData.Get(@"TestData", "InvalidUIRef");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var outputPath = Path.Combine(intermediateFolder, "bin", "test.msi");

                var args = new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    "-ext", typeof(UIExtensionFactory).Assembly.Location,
                    "-intermediateFolder", intermediateFolder,
                    "-o", outputPath,
                };

                var results = WixRunner.Execute(args);
                var message = results.Messages.Single();
                Assert.AreEqual("The identifier 'WixUI:WixUI_Mondo' is inaccessible due to its protection level.", message.ToString());
            }
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
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

        private static void BuildEula(string[] args)
        {
            var result = WixRunner.Execute(args.Concat(new[] { "-bv", "WixUILicenseRtf=bpl.rtf" }).ToArray())
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
