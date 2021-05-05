// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.VisualStudio
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.VisualStudio;
    using Xunit;

    public class VisualStudioExtensionFixture
    {
        [Fact]
        public void CanBuildUsingVsixPackage()
        {
            var folder = TestData.Get(@"TestData\UsingVsixPackage");
            var build = new Builder(folder, typeof(VSExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "CustomAction");
            Assert.Equal(new[]
            {
                "CustomAction:SetVS2010Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2010_VSIX_INSTALLER_PATH]\t",
                "CustomAction:SetVS2012Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2012_VSIX_INSTALLER_PATH]\t",
                "CustomAction:SetVS2013Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2013_VSIX_INSTALLER_PATH]\t",
                "CustomAction:SetVS2015Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2015_VSIX_INSTALLER_PATH]\t",
                "CustomAction:vimLa9TyFoAVwf8JmA0_ZJHA69J2fo\t3122\tVS_VSIX_INSTALLER_PATH\t/q  \"[#filzi8nwT8Ta133xcfp7qSIdGdRiC0]\" /admin\t",
                "CustomAction:viuMpl8IvFSDAzTulrmpAzBwAmCRTQ\t1074\tVS_VSIX_INSTALLER_PATH\t/q  \"[#filzi8nwT8Ta133xcfp7qSIdGdRiC0]\"\t",
                "CustomAction:vrmLa9TyFoAVwf8JmA0_ZJHA69J2fo\t3442\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\" /admin\t",
                "CustomAction:vruMpl8IvFSDAzTulrmpAzBwAmCRTQ\t1394\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\"\t",
                "CustomAction:VSFindInstances\t257\tVSCA\tFindInstances\t",
                "CustomAction:vumLa9TyFoAVwf8JmA0_ZJHA69J2fo\t3186\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\" /admin\t",
                "CustomAction:vuuMpl8IvFSDAzTulrmpAzBwAmCRTQ\t1138\tVS_VSIX_INSTALLER_PATH\t/q  /u:\"ExampleVsix\"\t",
                "CustomAction:Vwd2012VsixWhenVSAbsent\t51\tVS_VSIX_INSTALLER_PATH\t[VWD2012_VSIX_INSTALL_ROOT]\\Common7\\IDE\\VSIXInstaller.exe\t",
                "CustomAction:Vwd2013VsixWhenVSAbsent\t51\tVS_VSIX_INSTALLER_PATH\t[VWD2013_VSIX_INSTALL_ROOT]\\Common7\\IDE\\VSIXInstaller.exe\t",
                "CustomAction:Vwd2015VsixWhenVSAbsent\t51\tVS_VSIX_INSTALLER_PATH\t[VWD2015_VSIX_INSTALL_ROOT]\\Common7\\IDE\\VSIXInstaller.exe\t",
            }, results);
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
