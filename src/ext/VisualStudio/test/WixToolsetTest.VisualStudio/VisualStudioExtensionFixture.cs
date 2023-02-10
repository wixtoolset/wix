// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.VisualStudio
{
    using WixInternal.TestSupport;
    using WixInternal.Core.TestPackage;
    using WixToolset.VisualStudio;
    using Xunit;
    using System.Linq;

    public class VisualStudioExtensionFixture
    {
        [Fact]
        public void CanBuildUsingVsixPackage()
        {
            var folder = TestData.Get(@"TestData\UsingVsixPackage");
            var build = new Builder(folder, typeof(VSExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction");
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:SetVS2010Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2010_VSIX_INSTALLER_PATH]\t",
                "CustomAction:SetVS2012Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2012_VSIX_INSTALLER_PATH]\t",
                "CustomAction:SetVS2013Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2013_VSIX_INSTALLER_PATH]\t",
                "CustomAction:SetVS2015Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2015_VSIX_INSTALLER_PATH]\t",
                "CustomAction:vimLa9TyFoAVwf8JmA0_ZJHA69J2fo\t3122\tVS_VSIX_INSTALLER_PATH\t/q  \"[#filzi8nwT8Ta133xcfp7qSIdGdRiC0]\" /admin\t",
                "CustomAction:viuMpl8IvFSDAzTulrmpAzBwAmCRTQ\t1074\tVS_VSIX_INSTALLER_PATH\t/q  \"[#filzi8nwT8Ta133xcfp7qSIdGdRiC0]\"\t",
                "CustomAction:vrmLa9TyFoAVwf8JmA0_ZJHA69J2fo\t3442\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\" /admin\t",
                "CustomAction:vruMpl8IvFSDAzTulrmpAzBwAmCRTQ\t1394\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\"\t",
                "CustomAction:vumLa9TyFoAVwf8JmA0_ZJHA69J2fo\t3186\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\" /admin\t",
                "CustomAction:vuuMpl8IvFSDAzTulrmpAzBwAmCRTQ\t1138\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\"\t",
                "CustomAction:Vwd2012VsixWhenVSAbsent\t51\tVS_VSIX_INSTALLER_PATH\t[VWD2012_VSIX_INSTALL_ROOT]\\Common7\\IDE\\VSIXInstaller.exe\t",
                "CustomAction:Vwd2013VsixWhenVSAbsent\t51\tVS_VSIX_INSTALLER_PATH\t[VWD2013_VSIX_INSTALL_ROOT]\\Common7\\IDE\\VSIXInstaller.exe\t",
                "CustomAction:Vwd2015VsixWhenVSAbsent\t51\tVS_VSIX_INSTALLER_PATH\t[VWD2015_VSIX_INSTALL_ROOT]\\Common7\\IDE\\VSIXInstaller.exe\t",
                "CustomAction:Wix4VSFindInstances_X86\t257\tVSCA_X86\tFindInstances\t",
            }, results);
        }

        [Fact]
        public void CanBuildUsingVsixPackageOnArm64()
        {
            var folder = TestData.Get(@"TestData\UsingVsixPackage");
            var build = new Builder(folder, typeof(VSExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(BuildARM64, "CustomAction", "Property");

            var customActionResults = results.Where(r => r.StartsWith("CustomAction:")).ToArray();
            WixAssert.CompareLineByLine(new[]
            {
                "CustomAction:SetVS2010Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2010_VSIX_INSTALLER_PATH]\t",
                "CustomAction:SetVS2012Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2012_VSIX_INSTALLER_PATH]\t",
                "CustomAction:SetVS2013Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2013_VSIX_INSTALLER_PATH]\t",
                "CustomAction:SetVS2015Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2015_VSIX_INSTALLER_PATH]\t",
                "CustomAction:vimLa9TyFoAVwf8JmA0_ZJHA69J2fo\t3122\tVS_VSIX_INSTALLER_PATH\t/q  \"[#filzi8nwT8Ta133xcfp7qSIdGdRiC0]\" /admin\t",
                "CustomAction:viuMpl8IvFSDAzTulrmpAzBwAmCRTQ\t1074\tVS_VSIX_INSTALLER_PATH\t/q  \"[#filzi8nwT8Ta133xcfp7qSIdGdRiC0]\"\t",
                "CustomAction:vrmLa9TyFoAVwf8JmA0_ZJHA69J2fo\t3442\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\" /admin\t",
                "CustomAction:vruMpl8IvFSDAzTulrmpAzBwAmCRTQ\t1394\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\"\t",
                "CustomAction:vumLa9TyFoAVwf8JmA0_ZJHA69J2fo\t3186\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\" /admin\t",
                "CustomAction:vuuMpl8IvFSDAzTulrmpAzBwAmCRTQ\t1138\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\"\t",
                "CustomAction:Vwd2012VsixWhenVSAbsent\t51\tVS_VSIX_INSTALLER_PATH\t[VWD2012_VSIX_INSTALL_ROOT]\\Common7\\IDE\\VSIXInstaller.exe\t",
                "CustomAction:Vwd2013VsixWhenVSAbsent\t51\tVS_VSIX_INSTALLER_PATH\t[VWD2013_VSIX_INSTALL_ROOT]\\Common7\\IDE\\VSIXInstaller.exe\t",
                "CustomAction:Vwd2015VsixWhenVSAbsent\t51\tVS_VSIX_INSTALLER_PATH\t[VWD2015_VSIX_INSTALL_ROOT]\\Common7\\IDE\\VSIXInstaller.exe\t",
                "CustomAction:Wix4VSFindInstances_A64\t257\tVSCA_A64\tFindInstances\t",
            }, customActionResults);

            var propertyResults = results.Single(r => r.StartsWith("Property:SecureCustomProperties")).Split('\t')[1].Split(';');
            WixAssert.CompareLineByLine(new[]
            {
                "VS_VSIX_INSTALLER_PATH",
                "VS2010_VSIX_INSTALLER_PATH",
                "VS2012_VSIX_INSTALLER_PATH",
                "VS2013_VSIX_INSTALLER_PATH",
                "VS2015_VSIX_INSTALLER_PATH",
                "VS2017_IDE_DIR",
                "VS2017_ROOT_FOLDER",
                "VS2017DEVENV",
                "VS2019_IDE_VCSHARP_PROJECTSYSTEM_INSTALLED",
                "VS2022_ROOT_FOLDER",
                "WIX_DOWNGRADE_DETECTED",
                "WIX_UPGRADE_DETECTED",
            }, propertyResults);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }

        private static void BuildARM64(string[] args)
        {
            var newArgs = args.ToList();
            newArgs.Add("-platform");
            newArgs.Add("arm64");

            var result = WixRunner.Execute(newArgs.ToArray());
            result.AssertSuccess();
        }
    }
}
