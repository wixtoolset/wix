// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Util
{
    using System.IO;
    using System.Linq;
    using System.Xml;
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.Util;
    using Xunit;
    using System.Xml.Linq;
    using System;

    public class UtilExtensionFixture
    {
        [Fact]
        public void CanBuildUsingFileShare()
        {
            var folder = TestData.Get(@"TestData\UsingFileShare");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "Binary", "CustomAction", "Wix4FileShare", "Wix4FileSharePermissions");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X86\t[Binary data]",
                "CustomAction:Wix4ConfigureSmbInstall_X86\t1\tWix4UtilCA_X86\tConfigureSmbInstall\t",
                "CustomAction:Wix4ConfigureSmbUninstall_X86\t1\tWix4UtilCA_X86\tConfigureSmbUninstall\t",
                "CustomAction:Wix4CreateSmb_X86\t11265\tWix4UtilCA_X86\tCreateSmb\t",
                "CustomAction:Wix4CreateSmbRollback_X86\t11585\tWix4UtilCA_X86\tDropSmb\t",
                "CustomAction:Wix4DropSmb_X86\t11265\tWix4UtilCA_X86\tDropSmb\t",
                "CustomAction:Wix4DropSmbRollback_X86\t11585\tWix4UtilCA_X86\tCreateSmb\t",
                "Wix4FileShare:ExampleFileShare\texample\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tAn example file share\tINSTALLFOLDER",
                "Wix4FileSharePermissions:ExampleFileShare\tEveryone\t1",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildUsingFileShareX64()
        {
            var folder = TestData.Get(@"TestData\UsingFileShare");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "Wix4FileShare", "Wix4FileSharePermissions");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64\t[Binary data]",
                "CustomAction:Wix4ConfigureSmbInstall_X64\t1\tWix4UtilCA_X64\tConfigureSmbInstall\t",
                "CustomAction:Wix4ConfigureSmbUninstall_X64\t1\tWix4UtilCA_X64\tConfigureSmbUninstall\t",
                "CustomAction:Wix4CreateSmb_X64\t11265\tWix4UtilCA_X64\tCreateSmb\t",
                "CustomAction:Wix4CreateSmbRollback_X64\t11585\tWix4UtilCA_X64\tDropSmb\t",
                "CustomAction:Wix4DropSmb_X64\t11265\tWix4UtilCA_X64\tDropSmb\t",
                "CustomAction:Wix4DropSmbRollback_X64\t11585\tWix4UtilCA_X64\tCreateSmb\t",
                "Wix4FileShare:ExampleFileShare\texample\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tAn example file share\tINSTALLFOLDER",
                "Wix4FileSharePermissions:ExampleFileShare\tEveryone\t1",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanRoundtripFileShare()
        {
            var folder = TestData.Get(@"TestData", "UsingFileShare");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, "decompile.xml");

            build.BuildAndDecompileAndBuild(Build, Decompile, output);

            var doc = XDocument.Load(output);
            var utilElementNames = doc.Descendants()
                .Where(e => e.Name.Namespace == "http://wixtoolset.org/schemas/v4/wxs/util")
                .Select(e => e.Name.LocalName)
                .OrderBy(s => s)
                .ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "FileShare",
                "FileSharePermission",
                "User",
            }, utilElementNames);
        }

        [Fact]
        public void CanBuildCloseApplication()
        {
            var folder = TestData.Get(@"TestData\CloseApplication");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Binary", "CustomAction", "Wix4CloseApplication");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_A64\t[Binary data]",
                "CustomAction:Wix4CheckRebootRequired_A64\t65\tWix4UtilCA_A64\tWixCheckRebootRequired\t",
                "CustomAction:Wix4CloseApplications_A64\t1\tWix4UtilCA_A64\tWixCloseApplications\t",
                "CustomAction:Wix4CloseApplicationsDeferred_A64\t3073\tWix4UtilCA_A64\tWixCloseApplicationsDeferred\t",
                "Wix4CloseApplication:CloseMyApp\texplorer.exe\t\t\t3\t\tMYAPPISRUNNING\t\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildInternetShortcutInProduct()
        {
            var folder = TestData.Get(@"TestData\InternetShortcut");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "RemoveFile", "Wix4InternetShortcut");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64\t[Binary data]",
                "CustomAction:Wix4CreateInternetShortcuts_X64\t3073\tWix4UtilCA_X64\tWixCreateInternetShortcuts\t",
                "CustomAction:Wix4RollbackInternetShortcuts_X64\t3329\tWix4UtilCA_X64\tWixRollbackInternetShortcuts\t",
                "CustomAction:Wix4SchedInternetShortcuts_X64\t1\tWix4UtilCA_X64\tWixSchedInternetShortcuts\t",
                "RemoveFile:uisdCsU32.1i4Hebrg1N7E194zJQ8Y\tPackage.ico\thoiptxrr.url|WiX Toolset (url).url\tINSTALLFOLDER\t2",
                "RemoveFile:uisjV.q0ROZZYR3h_lkpbkZtLtPH0A\tPackage.ico\tjcxd1dwf.lnk|WiX Toolset (link).lnk\tINSTALLFOLDER\t2",
                "Wix4InternetShortcut:uisdCsU32.1i4Hebrg1N7E194zJQ8Y\tPackage.ico\tINSTALLFOLDER\tWiX Toolset (url).url\thttps://wixtoolset.org\t1\t[#Package.ico]\t0",
                "Wix4InternetShortcut:uisjV.q0ROZZYR3h_lkpbkZtLtPH0A\tPackage.ico\tINSTALLFOLDER\tWiX Toolset (link).lnk\thttps://wixtoolset.org\t0\t[#Package.ico]\t0",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildInternetShortcutInMergeModule()
        {
            var folder = TestData.Get(@"TestData\InternetShortcutModule");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder }, "test.msm");

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "RemoveFile", "Wix4InternetShortcut");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t[Binary data]",
                "CustomAction:Wix4CreateInternetShortcuts_X64\t3073\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixCreateInternetShortcuts\t",
                "CustomAction:Wix4RollbackInternetShortcuts_X64\t3329\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixRollbackInternetShortcuts\t",
                "CustomAction:Wix4SchedInternetShortcuts_X64\t1\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixSchedInternetShortcuts\t",
                "RemoveFile:uisdCsU32.1i4Hebrg1N7E194zJQ8Y.047730A5_30FE_4A62_A520_DA9381B8226A\tPackage.ico.047730A5_30FE_4A62_A520_DA9381B8226A\thoiptxrr.url|WiX Toolset (url).url\tINSTALLFOLDER.047730A5_30FE_4A62_A520_DA9381B8226A\t2",
                "RemoveFile:uisjV.q0ROZZYR3h_lkpbkZtLtPH0A.047730A5_30FE_4A62_A520_DA9381B8226A\tPackage.ico.047730A5_30FE_4A62_A520_DA9381B8226A\tjcxd1dwf.lnk|WiX Toolset (link).lnk\tINSTALLFOLDER.047730A5_30FE_4A62_A520_DA9381B8226A\t2",
                "Wix4InternetShortcut:uisdCsU32.1i4Hebrg1N7E194zJQ8Y.047730A5_30FE_4A62_A520_DA9381B8226A\tPackage.ico.047730A5_30FE_4A62_A520_DA9381B8226A\tINSTALLFOLDER.047730A5_30FE_4A62_A520_DA9381B8226A\tWiX Toolset (url).url\thttps://wixtoolset.org\t1\t[#Package.ico.047730A5_30FE_4A62_A520_DA9381B8226A]\t0",
                "Wix4InternetShortcut:uisjV.q0ROZZYR3h_lkpbkZtLtPH0A.047730A5_30FE_4A62_A520_DA9381B8226A\tPackage.ico.047730A5_30FE_4A62_A520_DA9381B8226A\tINSTALLFOLDER.047730A5_30FE_4A62_A520_DA9381B8226A\tWiX Toolset (link).lnk\thttps://wixtoolset.org\t0\t[#Package.ico.047730A5_30FE_4A62_A520_DA9381B8226A]\t0",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildWithPermissionEx()
        {
            var folder = TestData.Get(@"TestData\PermissionEx");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Wix4SecureObject");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4SecureObject:ExampleRegistryKey\tRegistry\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix4SecureObject:filF5_pLhBuF5b4N9XEo52g_hUM5Lo\tFile\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix4SecureObject:INSTALLFOLDER\tCreateFolder\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix4SecureObject:regL6DnQ9yJpDJH5OdcVji4YXsdX2c\tRegistry\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
                "Wix4SecureObject:testsvc\tServiceInstall\t\tEveryone\t1\t268435456\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildRemoveRegistryKeyExInMergeModule()
        {
            var folder = TestData.Get(@"TestData", "RemoveRegistryKeyEx");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder }, "test.msm");

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "RemoveRegistry", "Wix4RemoveRegistryKeyEx");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t[Binary data]",
                "CustomAction:Wix4RemoveRegistryKeysEx_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t65\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixRemoveRegistryKeysEx\t",
                "Wix4RemoveRegistryKeyEx:rrxfcDhR4HhE3v3rYiQcNtQjyahQNg.047730A5_30FE_4A62_A520_DA9381B8226A\tfilh4juyUVjoUcWWtcQmd5L07FoON4.047730A5_30FE_4A62_A520_DA9381B8226A\t2\tSOFTWARE\\Example\t1\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildRemoveFolderExInMergeModule()
        {
            var folder = TestData.Get(@"TestData\RemoveFolderEx");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder }, "test.msm");

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "RemoveFile", "Wix4RemoveFolderEx");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t[Binary data]",
                "CustomAction:Wix4RemoveFoldersEx_X64.047730A5_30FE_4A62_A520_DA9381B8226A\t65\tWix4UtilCA_X64.047730A5_30FE_4A62_A520_DA9381B8226A\tWixRemoveFoldersEx\t",
                "Wix4RemoveFolderEx:wrf5qCm1SE.zp8djrlk78l1IYFXsEw.047730A5_30FE_4A62_A520_DA9381B8226A\tfilh4juyUVjoUcWWtcQmd5L07FoON4.047730A5_30FE_4A62_A520_DA9381B8226A\tRemoveProp.047730A5_30FE_4A62_A520_DA9381B8226A\t3\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildRemoveFolderExInPackage()
        {
            var folder = TestData.Get(@"TestData\RemoveFolderExPackage");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "RemoveFile", "Wix4RemoveFolderEx");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64\t[Binary data]",
                "CustomAction:Wix4RemoveFoldersEx_X64\t65\tWix4UtilCA_X64\tWixRemoveFoldersEx\t",
                "Wix4RemoveFolderEx:wrfRwBJnGq1p9zdOKI6qUQ.p.wHFtE\tfilF5_pLhBuF5b4N9XEo52g_hUM5Lo\tREMOVEPROP\t3\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildServiceConfig()
        {
            var folder = TestData.Get(@"TestData", "ServiceConfig");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder }, "test.msi");

            var results = build.BuildAndQuery(BuildX64, "Binary", "CustomAction", "ServiceConfig", "Wix4ServiceConfig");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_X64\t[Binary data]",
                "CustomAction:Wix4ExecServiceConfig_X64\t3073\tWix4UtilCA_X64\tExecServiceConfig\t",
                "CustomAction:Wix4RollbackServiceConfig_X64\t3329\tWix4UtilCA_X64\tRollbackServiceConfig\t",
                "CustomAction:Wix4SchedServiceConfig_X64\t1\tWix4UtilCA_X64\tSchedServiceConfig\t",
                "Wix4ServiceConfig:svc\tfilPeUUVRrj2.Q_YcmN55mro4H1aQY\t1\trestart\trestart\trestart\t\t\t\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildWithEventManifest()
        {
            var folder = TestData.Get(@"TestData\EventManifest");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Binary", "CustomAction", "Wix4EventManifest", "Wix4XmlFile");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_A64\t[Binary data]",
                "CustomAction:Wix4ConfigureEventManifestRegister_A64\t1\tWix4UtilCA_A64\tConfigureEventManifestRegister\t",
                "CustomAction:Wix4ConfigureEventManifestUnregister_A64\t1\tWix4UtilCA_A64\tConfigureEventManifestUnregister\t",
                "CustomAction:Wix4ExecXmlFile_A64\t11265\tWix4UtilCA_A64\tExecXmlFile\t",
                "CustomAction:Wix4ExecXmlFileRollback_A64\t11521\tWix4UtilCA_A64\tExecXmlFileRollback\t",
                "CustomAction:Wix4RegisterEventManifest_A64\t3073\tWix4UtilCA_A64\tWixQuietExec\t",
                "CustomAction:Wix4RollbackRegisterEventManifest_A64\t3393\tWix4UtilCA_A64\tWixQuietExec\t",
                "CustomAction:Wix4RollbackUnregisterEventManifest_A64\t3329\tWix4UtilCA_A64\tWixQuietExec\t",
                "CustomAction:Wix4SchedXmlFile_A64\t1\tWix4UtilCA_A64\tSchedXmlFile\t",
                "CustomAction:Wix4UnregisterEventManifest_A64\t3137\tWix4UtilCA_A64\tWixQuietExec\t",
                "Wix4EventManifest:Manifest.dll\t[#Manifest.dll]",
                "Wix4XmlFile:Config_Manifest.dllMessageFile\t[#Manifest.dll]\t/*/*/*/*[\\[]@messageFileName[\\]]\tmessageFileName\t[Manifest.dll]\t4100\tManifest.dll\t",
                "Wix4XmlFile:Config_Manifest.dllResourceFile\t[#Manifest.dll]\t/*/*/*/*[\\[]@resourceFileName[\\]]\tresourceFileName\t[Manifest.dll]\t4100\tManifest.dll\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildWithQueries()
        {
            var folder = TestData.Get(@"TestData\Queries");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "Binary", "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "Binary:Wix4UtilCA_A64\t[Binary data]",
                "CustomAction:Wix4BroadcastEnvironmentChange_A64\t65\tWix4UtilCA_A64\tWixBroadcastEnvironmentChange\t",
                "CustomAction:Wix4BroadcastSettingChange_A64\t65\tWix4UtilCA_A64\tWixBroadcastSettingChange\t",
                "CustomAction:Wix4CheckRebootRequired_A64\t65\tWix4UtilCA_A64\tWixCheckRebootRequired\t",
                "CustomAction:Wix4QueryNativeMachine_A64\t257\tWix4UtilCA_A64\tWixQueryNativeMachine\t",
                "CustomAction:Wix4QueryOsDriverInfo_A64\t257\tWix4UtilCA_A64\tWixQueryOsDriverInfo\t",
                "CustomAction:Wix4QueryOsInfo_A64\t257\tWix4UtilCA_A64\tWixQueryOsInfo\t",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildAndDecompiileQueries()
        {
            var folder = TestData.Get(@"TestData\Queries");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, "decompile.xml");

            build.BuildAndDecompileAndBuild(Build, Decompile, output);

            var doc = XDocument.Load(output);
            var utilElementNames = doc.Descendants()
                .Where(e => e.Name.Namespace == "http://wixtoolset.org/schemas/v4/wxs/util")
                .Select(e => e.Name.LocalName)
                .OrderBy(s => s)
                .ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "BroadcastEnvironmentChange",
                "BroadcastSettingChange",
                "CheckRebootRequired",
                "QueryNativeMachine",
                "QueryWindowsDriverInfo",
                "QueryWindowsSuiteInfo",
            }, utilElementNames);
        }

        [Fact]
        public void CanBuildWithXmlConfig()
        {
            var folder = TestData.Get(@"TestData", "XmlConfig");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildX64, "Wix4XmlConfig");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4XmlConfig:AddAttribute2\t[INSTALLFOLDER]my.xml\tAddElement\t\t\tTheAttribute2\tAttributeValue2\t0\tAdd\t4",
                "Wix4XmlConfig:AddElement\t[#MyXmlFile]\t\t//root/child2\t\tgrandchild3\t\t273\tAdd\t2",
                "Wix4XmlConfig:DelElement\t[#MyXmlFile]\t\t//root/child1\tgrandchild1\t\t\t289\tDel\t",
                "Wix4XmlConfig:uxcPPF6g4HJEQpBLT9w9GT6SKyHWww\t[#MyXmlFile]\tAddElement\t\t\tTheAttribute1\tAttributeValue1\t0\tAdd\t3",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanRoundtripXmlConfig()
        {
            var folder = TestData.Get(@"TestData", "XmlConfig");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });
            var output = Path.Combine(folder, "XmlConfigdecompile.xml");

            build.BuildAndDecompileAndBuild(Build, Decompile, output);

            var doc = XDocument.Load(output);
            var utilElementNames = doc.Descendants().Where(e => e.Name.Namespace == "http://wixtoolset.org/schemas/v4/wxs/util")
                                      .Select(e => e.Name.LocalName)
                                      .ToArray();

            WixAssert.CompareLineByLine(new[]
            {
                "XmlConfig",
                "XmlConfig",
                "XmlConfig",
                "XmlConfig"
            }, utilElementNames);
        }

        [Fact]
        public void CanBuildModuleWithXmlConfig()
        {
            var folder = TestData.Get(@"TestData", "XmlConfigModule");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder }, "test.msm");

            var results = build.BuildAndQuery(BuildX64, "Wix4XmlConfig");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4XmlConfig:AddElement.047730A5_30FE_4A62_A520_DA9381B8226A\t[my.xml.047730A5_30FE_4A62_A520_DA9381B8226A]\t\t//root/sub\txxx\t\t\t273\tParent.047730A5_30FE_4A62_A520_DA9381B8226A\t1",
                "Wix4XmlConfig:ChildElement.047730A5_30FE_4A62_A520_DA9381B8226A\t[my.xml.047730A5_30FE_4A62_A520_DA9381B8226A]\tAddElement.047730A5_30FE_4A62_A520_DA9381B8226A\t\txxx\t\t\t0\tChild.047730A5_30FE_4A62_A520_DA9381B8226A\t1",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CanBuildBundleWithSearches()
        {
            var folder = TestData.Get(@"TestData\BundleWithSearches");
            var rootFolder = TestData.Get();
            var wixext = Path.Combine(rootFolder, "WixToolset.Util.wixext.dll");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var bundlePath = Path.Combine(baseFolder, @"bin\test.exe");
                var baFolderPath = Path.Combine(baseFolder, "ba");
                var extractFolderPath = Path.Combine(baseFolder, "extract");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Bundle.wxs"),
                    "-ext", wixext,
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", bundlePath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(bundlePath));
                Assert.True(File.Exists(Path.Combine(baseFolder, @"bin\test.wixpdb")));

                var extractResult = BundleExtractor.ExtractBAContainer(null, bundlePath, baFolderPath, extractFolderPath);
                extractResult.AssertSuccess();

                var bundleExtensionDatas = extractResult.SelectBundleExtensionDataNodes("/be:BundleExtensionData/be:BundleExtension[@Id='Wix4UtilBundleExtension_X86']");
                Assert.Equal(1, bundleExtensionDatas.Count);
                Assert.Equal("<BundleExtension Id='Wix4UtilBundleExtension_X86'>" +
                    "<WixWindowsFeatureSearch Id='DetectSHA2SupportId' Type='sha2CodeSigning' />" +
                    "</BundleExtension>", bundleExtensionDatas[0].GetTestXml());

                var utilSearches = extractResult.SelectManifestNodes("/burn:BurnManifest/*[self::burn:ExtensionSearch or self::burn:DirectorySearch or self::burn:FileSearch or self::burn:MsiProductSearch or self::burn:RegistrySearch]")
                                                .Cast<XmlElement>()
                                                .Select(e => e.GetTestXml())
                                                .ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    @"<ExtensionSearch Id='DetectSHA2SupportId' Variable='IsSHA2Supported' ExtensionId='Wix4UtilBundleExtension_X86' />",
                    @"<DirectorySearch Id='DirectorySearchId' Variable='DirectorySearchVariable' Path='%windir%\System32' Type='exists' DisableFileRedirection='yes' />",
                    @"<FileSearch Id='FileSearchId' Variable='FileSearchVariable' Path='%windir%\System32\mscoree.dll' Type='exists' />",
                    @"<MsiProductSearch Id='ProductSearchId' Variable='ProductSearchVariable' Condition='1 &amp; 2 &lt; 3' UpgradeCode='{738D02BF-E231-4370-8209-E9FD4E1BE2A1}' Type='version' />",
                    @"<RegistrySearch Id='RegistrySearchId' Variable='RegistrySearchVariable' Root='HKLM' Key='SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' Value='Release' Type='value' VariableType='string' />",
                    @"<RegistrySearch Id='RegistrySearchId64' Variable='RegistrySearchVariable64' Root='HKLM' Key='SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' Value='Release' Win64='yes' Type='value' VariableType='string' />"
                }, utilSearches);
            }
        }

        [Fact]
        public void CanCreateUserAccountWithComment()
        {
            var folder = TestData.Get(@"TestData\CreateUser");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "Wix4User");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4User:TEST_USER00\tComponent1\ttestName00\t\ttest123!@#\tTest Comment 1\t1792",
                "Wix4User:TEST_USER01\tComponent1\ttestName01\t\ttest123!@#\tTest Comment 1\t1796",
                "Wix4User:TEST_USER02\tComponent1\ttestName02\t\ttest123!@#\t\t3840",
                "Wix4User:TEST_USER03\tComponent1\ttestName03\t\ttest123!@#\t\t3844",
                "Wix4User:TEST_USER04\tComponent1\ttestName04\t\ttest123!@#\tTest Comment 1\t1280",
                "Wix4User:TEST_USER05\tComponent1\ttestName05\t\ttest123!@#\tTest Comment 1\t1284",
                "Wix4User:TEST_USER06\tComponent1\ttestName06\t\ttest123!@#\t\t3328",
                "Wix4User:TEST_USER07\tComponent1\ttestName07\t\ttest123!@#\t\t3332",
                "Wix4User:TEST_USER10\tComponent1\ttestName10\t\ttest123!@#\tTest Comment 1\t1808",
                "Wix4User:TEST_USER11\tComponent1\ttestName11\t\ttest123!@#\tTest Comment 1\t1812",
                "Wix4User:TEST_USER12\tComponent1\ttestName12\t\ttest123!@#\t\t3856",
                "Wix4User:TEST_USER13\tComponent1\ttestName13\t\ttest123!@#\t\t3860",
                "Wix4User:TEST_USER14\tComponent1\ttestName14\t\ttest123!@#\tTest Comment 1\t1296",
                "Wix4User:TEST_USER15\tComponent1\ttestName15\t\ttest123!@#\tTest Comment 1\t1300",
                "Wix4User:TEST_USER16\tComponent1\ttestName16\t\ttest123!@#\t\t3344",
                "Wix4User:TEST_USER17\tComponent1\ttestName17\t\ttest123!@#\t\t3348",
                "Wix4User:TEST_USER20\tComponent1\ttestName20\t\ttest123!@#\tTest Comment 1\t768",
                "Wix4User:TEST_USER21\tComponent1\ttestName21\t\ttest123!@#\tTest Comment 1\t772",
                "Wix4User:TEST_USER22\tComponent1\ttestName22\t\ttest123!@#\t\t2816",
                "Wix4User:TEST_USER23\tComponent1\ttestName23\t\ttest123!@#\t\t2820",
                "Wix4User:TEST_USER24\tComponent1\ttestName24\t\ttest123!@#\tTest Comment 1\t256",
                "Wix4User:TEST_USER25\tComponent1\ttestName25\t\ttest123!@#\tTest Comment 1\t260",
                "Wix4User:TEST_USER26\tComponent1\ttestName26\t\ttest123!@#\t\t2304",
                "Wix4User:TEST_USER27\tComponent1\ttestName27\t\ttest123!@#\t\t2308",
                "Wix4User:TEST_USER30\tComponent1\ttestName30\t\ttest123!@#\tTest Comment 1\t784",
                "Wix4User:TEST_USER31\tComponent1\ttestName31\t\ttest123!@#\tTest Comment 1\t788",
                "Wix4User:TEST_USER32\tComponent1\ttestName32\t\ttest123!@#\t\t2832",
                "Wix4User:TEST_USER33\tComponent1\ttestName33\t\ttest123!@#\t\t2836",
                "Wix4User:TEST_USER34\tComponent1\ttestName34\t\ttest123!@#\tTest Comment 1\t272",
                "Wix4User:TEST_USER35\tComponent1\ttestName35\t\ttest123!@#\tTest Comment 1\t276",
                "Wix4User:TEST_USER36\tComponent1\ttestName36\t\ttest123!@#\t\t2320",
                "Wix4User:TEST_USER37\tComponent1\ttestName37\t\ttest123!@#\t\t2324",
                "Wix4User:TEST_USER40\tComponent1\ttestName40\t\ttest123!@#\tTest Comment 1\t1536",
                "Wix4User:TEST_USER41\tComponent1\ttestName41\t\ttest123!@#\tTest Comment 1\t1540",
                "Wix4User:TEST_USER42\tComponent1\ttestName42\t\ttest123!@#\t\t3584",
                "Wix4User:TEST_USER43\tComponent1\ttestName43\t\ttest123!@#\t\t3588",
                "Wix4User:TEST_USER44\tComponent1\ttestName44\t\ttest123!@#\tTest Comment 1\t1024",
                "Wix4User:TEST_USER45\tComponent1\ttestName45\t\ttest123!@#\tTest Comment 1\t1028",
                "Wix4User:TEST_USER46\tComponent1\ttestName46\t\ttest123!@#\t\t3072",
                "Wix4User:TEST_USER47\tComponent1\ttestName47\t\ttest123!@#\t\t3076",
                "Wix4User:TEST_USER50\tComponent1\ttestName50\t\ttest123!@#\tTest Comment 1\t1552",
                "Wix4User:TEST_USER51\tComponent1\ttestName51\t\ttest123!@#\tTest Comment 1\t1556",
                "Wix4User:TEST_USER52\tComponent1\ttestName52\t\ttest123!@#\t\t3600",
                "Wix4User:TEST_USER53\tComponent1\ttestName53\t\ttest123!@#\t\t3604",
                "Wix4User:TEST_USER54\tComponent1\ttestName54\t\ttest123!@#\tTest Comment 1\t1040",
                "Wix4User:TEST_USER55\tComponent1\ttestName55\t\ttest123!@#\tTest Comment 1\t1044",
                "Wix4User:TEST_USER56\tComponent1\ttestName56\t\ttest123!@#\t\t3088",
                "Wix4User:TEST_USER57\tComponent1\ttestName57\t\ttest123!@#\t\t3092",
                "Wix4User:TEST_USER60\tComponent1\ttestName60\t\ttest123!@#\tTest Comment 1\t512",
                "Wix4User:TEST_USER61\tComponent1\ttestName61\t\ttest123!@#\tTest Comment 1\t516",
                "Wix4User:TEST_USER62\tComponent1\ttestName62\t\ttest123!@#\t\t2560",
                "Wix4User:TEST_USER63\tComponent1\ttestName63\t\ttest123!@#\t\t2564",
                "Wix4User:TEST_USER64\tComponent1\ttestName64\t\ttest123!@#\tTest Comment 1\t0",
                "Wix4User:TEST_USER65\tComponent1\ttestName65\t\ttest123!@#\tTest Comment 1\t4",
                "Wix4User:TEST_USER66\tComponent1\ttestName66\t\ttest123!@#\t\t2048",
                "Wix4User:TEST_USER67\tComponent1\ttestName67\t\ttest123!@#\t\t2052",
                "Wix4User:TEST_USER70\tComponent1\ttestName70\t\ttest123!@#\tTest Comment 1\t528",
                "Wix4User:TEST_USER71\tComponent1\ttestName71\t\ttest123!@#\tTest Comment 1\t532",
                "Wix4User:TEST_USER72\tComponent1\ttestName72\t\ttest123!@#\t\t2576",
                "Wix4User:TEST_USER73\tComponent1\ttestName73\t\ttest123!@#\t\t2580",
                "Wix4User:TEST_USER74\tComponent1\ttestName74\t\ttest123!@#\tTest Comment 1\t16",
                "Wix4User:TEST_USER75\tComponent1\ttestName75\t\ttest123!@#\tTest Comment 1\t20",
                "Wix4User:TEST_USER76\tComponent1\ttestName76\t\ttest123!@#\t\t2064",
                "Wix4User:TEST_USER77\tComponent1\ttestName77\t\ttest123!@#\t\t2068",
            }, results.OrderBy(s => s).ToArray());
        }

        [Fact]
        public void CannotBuildBundleWithSearchesUsingBuiltinVariableNames()
        {
            var folder = TestData.Get("TestData", "BundleWithSearches");
            var rootFolder = TestData.Get();
            var wixext = Path.Combine(rootFolder, "WixToolset.Util.wixext.dll");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "BundleUsingBuiltinVariableNames.wxs"),
                    "-ext", wixext,
                    "-loc", Path.Combine(folder, "Bundle.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", "bundle.wixlib",
                });

                var messages = result.Messages.Select(m => m.ToString()).ToList();
                messages.Sort();

                WixAssert.CompareLineByLine(new[]
                {
                    "The DirectorySearch/@Variable attribute's value, 'InstallerName', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFiles64Folder', 'CommonFiles6432Folder', 'CommonFilesFolder', 'CompatibilityMode', 'ComputerName', 'Date', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'InstallerName', 'InstallerVersion', 'LocalAppDataFolder', 'LogonUser', 'MyPicturesFolder', 'NativeMachine', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProcessorArchitecture', 'ProgramFiles64Folder', 'ProgramFiles6432Folder', 'ProgramFilesFolder', 'ProgramMenuFolder', 'RebootPending', 'SendToFolder', 'ServicePackLevel', 'StartMenuFolder', 'StartupFolder', 'System64Folder', 'SystemFolder', 'SystemLanguageID', 'TempFolder', 'TemplateFolder', 'TerminalServer', 'UserLanguageID', 'UserUILanguageID', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsBuildNumber', 'WindowsFolder', 'WindowsVolume', 'WixBundleAction', 'WixBundleActiveParent', 'WixBundleCommandLineAction', 'WixBundleElevated', 'WixBundleExecutePackageAction', 'WixBundleExecutePackageCacheFolder', 'WixBundleForcedRestartPackage', 'WixBundleInstalled', 'WixBundleProviderKey', 'WixBundleSourceProcessFolder', 'WixBundleSourceProcessPath', 'WixBundleTag', 'WixBundleUILevel', or 'WixBundleVersion'.",
                    "The FileSearch/@Variable attribute's value, 'NativeMachine', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFiles64Folder', 'CommonFiles6432Folder', 'CommonFilesFolder', 'CompatibilityMode', 'ComputerName', 'Date', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'InstallerName', 'InstallerVersion', 'LocalAppDataFolder', 'LogonUser', 'MyPicturesFolder', 'NativeMachine', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProcessorArchitecture', 'ProgramFiles64Folder', 'ProgramFiles6432Folder', 'ProgramFilesFolder', 'ProgramMenuFolder', 'RebootPending', 'SendToFolder', 'ServicePackLevel', 'StartMenuFolder', 'StartupFolder', 'System64Folder', 'SystemFolder', 'SystemLanguageID', 'TempFolder', 'TemplateFolder', 'TerminalServer', 'UserLanguageID', 'UserUILanguageID', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsBuildNumber', 'WindowsFolder', 'WindowsVolume', 'WixBundleAction', 'WixBundleActiveParent', 'WixBundleCommandLineAction', 'WixBundleElevated', 'WixBundleExecutePackageAction', 'WixBundleExecutePackageCacheFolder', 'WixBundleForcedRestartPackage', 'WixBundleInstalled', 'WixBundleProviderKey', 'WixBundleSourceProcessFolder', 'WixBundleSourceProcessPath', 'WixBundleTag', 'WixBundleUILevel', or 'WixBundleVersion'.",
                    "The ProductSearch/@Variable attribute's value, 'Date', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFiles64Folder', 'CommonFiles6432Folder', 'CommonFilesFolder', 'CompatibilityMode', 'ComputerName', 'Date', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'InstallerName', 'InstallerVersion', 'LocalAppDataFolder', 'LogonUser', 'MyPicturesFolder', 'NativeMachine', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProcessorArchitecture', 'ProgramFiles64Folder', 'ProgramFiles6432Folder', 'ProgramFilesFolder', 'ProgramMenuFolder', 'RebootPending', 'SendToFolder', 'ServicePackLevel', 'StartMenuFolder', 'StartupFolder', 'System64Folder', 'SystemFolder', 'SystemLanguageID', 'TempFolder', 'TemplateFolder', 'TerminalServer', 'UserLanguageID', 'UserUILanguageID', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsBuildNumber', 'WindowsFolder', 'WindowsVolume', 'WixBundleAction', 'WixBundleActiveParent', 'WixBundleCommandLineAction', 'WixBundleElevated', 'WixBundleExecutePackageAction', 'WixBundleExecutePackageCacheFolder', 'WixBundleForcedRestartPackage', 'WixBundleInstalled', 'WixBundleProviderKey', 'WixBundleSourceProcessFolder', 'WixBundleSourceProcessPath', 'WixBundleTag', 'WixBundleUILevel', or 'WixBundleVersion'.",
                    "The RegistrySearch/@Variable attribute's value, 'VersionNT64', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFiles64Folder', 'CommonFiles6432Folder', 'CommonFilesFolder', 'CompatibilityMode', 'ComputerName', 'Date', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'InstallerName', 'InstallerVersion', 'LocalAppDataFolder', 'LogonUser', 'MyPicturesFolder', 'NativeMachine', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProcessorArchitecture', 'ProgramFiles64Folder', 'ProgramFiles6432Folder', 'ProgramFilesFolder', 'ProgramMenuFolder', 'RebootPending', 'SendToFolder', 'ServicePackLevel', 'StartMenuFolder', 'StartupFolder', 'System64Folder', 'SystemFolder', 'SystemLanguageID', 'TempFolder', 'TemplateFolder', 'TerminalServer', 'UserLanguageID', 'UserUILanguageID', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsBuildNumber', 'WindowsFolder', 'WindowsVolume', 'WixBundleAction', 'WixBundleActiveParent', 'WixBundleCommandLineAction', 'WixBundleElevated', 'WixBundleExecutePackageAction', 'WixBundleExecutePackageCacheFolder', 'WixBundleForcedRestartPackage', 'WixBundleInstalled', 'WixBundleProviderKey', 'WixBundleSourceProcessFolder', 'WixBundleSourceProcessPath', 'WixBundleTag', 'WixBundleUILevel', or 'WixBundleVersion'.",
                    "The RegistrySearch/@Variable attribute's value, 'WixBundleAction', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFiles64Folder', 'CommonFiles6432Folder', 'CommonFilesFolder', 'CompatibilityMode', 'ComputerName', 'Date', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'InstallerName', 'InstallerVersion', 'LocalAppDataFolder', 'LogonUser', 'MyPicturesFolder', 'NativeMachine', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProcessorArchitecture', 'ProgramFiles64Folder', 'ProgramFiles6432Folder', 'ProgramFilesFolder', 'ProgramMenuFolder', 'RebootPending', 'SendToFolder', 'ServicePackLevel', 'StartMenuFolder', 'StartupFolder', 'System64Folder', 'SystemFolder', 'SystemLanguageID', 'TempFolder', 'TemplateFolder', 'TerminalServer', 'UserLanguageID', 'UserUILanguageID', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsBuildNumber', 'WindowsFolder', 'WindowsVolume', 'WixBundleAction', 'WixBundleActiveParent', 'WixBundleCommandLineAction', 'WixBundleElevated', 'WixBundleExecutePackageAction', 'WixBundleExecutePackageCacheFolder', 'WixBundleForcedRestartPackage', 'WixBundleInstalled', 'WixBundleProviderKey', 'WixBundleSourceProcessFolder', 'WixBundleSourceProcessPath', 'WixBundleTag', 'WixBundleUILevel', or 'WixBundleVersion'.",
                    "The WindowsFeatureSearch/@Variable attribute's value, 'NTProductType', is one of the illegal options: 'AdminToolsFolder', 'AppDataFolder', 'CommonAppDataFolder', 'CommonFiles64Folder', 'CommonFiles6432Folder', 'CommonFilesFolder', 'CompatibilityMode', 'ComputerName', 'Date', 'DesktopFolder', 'FavoritesFolder', 'FontsFolder', 'InstallerName', 'InstallerVersion', 'LocalAppDataFolder', 'LogonUser', 'MyPicturesFolder', 'NativeMachine', 'NTProductType', 'NTSuiteBackOffice', 'NTSuiteDataCenter', 'NTSuiteEnterprise', 'NTSuitePersonal', 'NTSuiteSmallBusiness', 'NTSuiteSmallBusinessRestricted', 'NTSuiteWebServer', 'PersonalFolder', 'Privileged', 'ProcessorArchitecture', 'ProgramFiles64Folder', 'ProgramFiles6432Folder', 'ProgramFilesFolder', 'ProgramMenuFolder', 'RebootPending', 'SendToFolder', 'ServicePackLevel', 'StartMenuFolder', 'StartupFolder', 'System64Folder', 'SystemFolder', 'SystemLanguageID', 'TempFolder', 'TemplateFolder', 'TerminalServer', 'UserLanguageID', 'UserUILanguageID', 'VersionMsi', 'VersionNT', 'VersionNT64', 'WindowsBuildNumber', 'WindowsFolder', 'WindowsVolume', 'WixBundleAction', 'WixBundleActiveParent', 'WixBundleCommandLineAction', 'WixBundleElevated', 'WixBundleExecutePackageAction', 'WixBundleExecutePackageCacheFolder', 'WixBundleForcedRestartPackage', 'WixBundleInstalled', 'WixBundleProviderKey', 'WixBundleSourceProcessFolder', 'WixBundleSourceProcessPath', 'WixBundleTag', 'WixBundleUILevel', or 'WixBundleVersion'.",
                }, messages.ToArray());
            }
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }

        private static void BuildX64(string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Add("-platform");
            newArgs.Add("x64");
            newArgs.Add("-sw1072");

            var result = WixRunner.Execute(newArgs.ToArray());
            result.AssertSuccess();
        }

        private static void BuildARM64(string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Add("-platform");
            newArgs.Add("arm64");

            var result = WixRunner.Execute(newArgs.ToArray());
            result.AssertSuccess();
        }

        private static void Decompile(string[] args)
        {
            var result = WixRunner.Execute(args);
            result.AssertSuccess();
        }
    }
}
