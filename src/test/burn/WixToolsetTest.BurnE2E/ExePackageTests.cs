// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class ExePackageTests : BurnE2ETests
    {
        public ExePackageTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanInstallAndUninstallPerMachineArpEntryExePackage()
        {
            const string arpId = "{4D9EC36A-1E63-4244-875C-3ECB0A2CAE30}";
            var perMachineArpEntryExePackageBundle = this.CreateBundleInstaller(@"PerMachineArpEntryExePackage");
            var arpEntryExePackage = this.CreateArpEntryInstaller(perMachineArpEntryExePackageBundle, "TestExe");

            arpEntryExePackage.VerifyRegistered(false);

            var installLogPath = perMachineArpEntryExePackageBundle.Install();
            perMachineArpEntryExePackageBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);

            LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\"");

            var uninstallLogPath = perMachineArpEntryExePackageBundle.Uninstall();
            perMachineArpEntryExePackageBundle.VerifyUnregisteredAndRemovedFromPackageCache();
            arpEntryExePackage.VerifyRegistered(false);

            LogVerifier.MessageInLogFile(uninstallLogPath, $"testexe.exe\" /regd HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId}");
        }

        [RuntimeFact]
        public void CanUninstallPerMachineArpEntryExePackageOnRollback()
        {
            const string arpId = "{80E90929-EEA5-48A7-A680-A0237A1CAD84}";
            var perMachineArpEntryExePackageFailureBundle = this.CreateBundleInstaller(@"PerMachineArpEntryExePackageFailure");
            var arpEntryExePackage = this.CreateArpEntryInstaller(perMachineArpEntryExePackageFailureBundle, "TestExe");

            arpEntryExePackage.VerifyRegistered(false);

            var installLogPath = perMachineArpEntryExePackageFailureBundle.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);
            perMachineArpEntryExePackageFailureBundle.VerifyUnregisteredAndRemovedFromPackageCache();
            arpEntryExePackage.VerifyRegistered(false);

            LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\"");
            LogVerifier.MessageInLogFile(installLogPath, $"testexe.exe\" /regd HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId}");
        }

        [RuntimeFact]
        public void CanInstallAndUninstallPerUserArpEntryExePackage()
        {
            const string arpId = "{9B5300C7-9B34-4670-9614-185B02AB87EF}";
            var perUserArpEntryExePackageBundle = this.CreateBundleInstaller(@"PerUserArpEntryExePackage");
            var arpEntryExePackage = this.CreateArpEntryInstaller(perUserArpEntryExePackageBundle, "TestExe");

            arpEntryExePackage.VerifyRegistered(false);

            var installLogPath = perUserArpEntryExePackageBundle.Install();
            perUserArpEntryExePackageBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);

            LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\"");

            var uninstallLogPath = perUserArpEntryExePackageBundle.Uninstall();
            perUserArpEntryExePackageBundle.VerifyUnregisteredAndRemovedFromPackageCache();
            arpEntryExePackage.VerifyRegistered(false);

            LogVerifier.MessageInLogFile(uninstallLogPath, $"testexe.exe\" /regd HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId}");
        }

        [RuntimeFact]
        public void FailsUninstallWhenPerUserArpEntryExePackageMissingQuietUninstallString()
        {
            const string arpId = "{DE9F8594-5856-4454-AB10-3C01ED246D7D}";
            var brokenPerUserArpEntryExePackageBundle = this.CreateBundleInstaller(@"BrokenPerUserArpEntryExePackage");
            var arpEntryExePackage = this.CreateArpEntryInstaller(brokenPerUserArpEntryExePackageBundle, "TestExe");

            arpEntryExePackage.VerifyRegistered(false);
            brokenPerUserArpEntryExePackageBundle.VerifyUnregisteredAndRemovedFromPackageCache();

            var installLogPath = brokenPerUserArpEntryExePackageBundle.Install();
            brokenPerUserArpEntryExePackageBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);

            LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\"");

            brokenPerUserArpEntryExePackageBundle.Uninstall((int)MSIExec.MSIExecReturnCode.ERROR_INVALID_PARAMETER);
            brokenPerUserArpEntryExePackageBundle.VerifyRegisteredAndInPackageCache();

            arpEntryExePackage.Unregister();

            brokenPerUserArpEntryExePackageBundle.Uninstall();
            brokenPerUserArpEntryExePackageBundle.VerifyUnregisteredAndRemovedFromPackageCache();
        }
    }
}
