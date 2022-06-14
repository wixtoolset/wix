// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using WixTestTools;
    using WixToolset.Mba.Core;
    using Xunit;
    using Xunit.Abstractions;

    public class ExePackageTests : BurnE2ETests
    {
        public ExePackageTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanInstallAndUninstallPerMachineArpEntryExePackage()
        {
            var perMachineArpEntryExePackageBundle = this.CreateBundleInstaller(@"PerMachineArpEntryExePackage");
            var arpEntryExePackage = this.CreateArpEntryInstaller(perMachineArpEntryExePackageBundle, "TestExe");
            var arpId = arpEntryExePackage.ArpId;

            arpEntryExePackage.VerifyRegistered(false);

            var installLogPath = perMachineArpEntryExePackageBundle.Install();
            perMachineArpEntryExePackageBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);

            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\""));

            var uninstallLogPath = perMachineArpEntryExePackageBundle.Uninstall();
            perMachineArpEntryExePackageBundle.VerifyUnregisteredAndRemovedFromPackageCache();
            arpEntryExePackage.VerifyRegistered(false);

            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"testexe.exe\" /regd HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId}"));
        }

        [RuntimeFact]
        public void CanRecacheAndReinstallPerMachineArpEntryExePackageOnUninstallRollback()
        {
            var packageTestExe = this.CreatePackageInstaller("PackageTestExe");
            var perMachineArpEntryExePackageUninstallFailureBundle = this.CreateBundleInstaller(@"PerMachineArpEntryExePackageUninstallFailure");
            var arpEntryExePackage = this.CreateArpEntryInstaller(perMachineArpEntryExePackageUninstallFailureBundle, "TestExe");
            var arpId = arpEntryExePackage.ArpId;
            var testBAController = this.CreateTestBAController();

            arpEntryExePackage.VerifyRegistered(false);
            packageTestExe.VerifyInstalled(false);

            testBAController.SetPackageRequestedCacheType("TestExe", BOOTSTRAPPER_CACHE_TYPE.Remove);

            var installLogPath = perMachineArpEntryExePackageUninstallFailureBundle.Install();
            perMachineArpEntryExePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);
            packageTestExe.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\""));

            testBAController.ResetPackageStates("TestExe");
            testBAController.SetAllowAcquireAfterValidationFailure();

            var uninstallLogPath = perMachineArpEntryExePackageUninstallFailureBundle.Uninstall((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, "FAILWHENDEFERRED=1");
            perMachineArpEntryExePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);
            packageTestExe.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, "TESTBA: OnCachePackageNonVitalValidationFailure() - id: TestExe, default: None, requested: Acquire"));
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"testexe.exe\" /regd HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId}"));
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"TestExe.exe\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\""));
        }

        [RuntimeFact]
        public void CanReinstallPerMachineArpEntryExePackageOnUninstallRollback()
        {
            var packageTestExe = this.CreatePackageInstaller("PackageTestExe");
            var perMachineArpEntryExePackageUninstallFailureBundle = this.CreateBundleInstaller(@"PerMachineArpEntryExePackageUninstallFailure");
            var arpEntryExePackage = this.CreateArpEntryInstaller(perMachineArpEntryExePackageUninstallFailureBundle, "TestExe");
            var arpId = arpEntryExePackage.ArpId;

            arpEntryExePackage.VerifyRegistered(false);
            packageTestExe.VerifyInstalled(false);

            var installLogPath = perMachineArpEntryExePackageUninstallFailureBundle.Install();
            perMachineArpEntryExePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);
            packageTestExe.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\""));

            var uninstallLogPath = perMachineArpEntryExePackageUninstallFailureBundle.Uninstall((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, "FAILWHENDEFERRED=1");
            perMachineArpEntryExePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);
            packageTestExe.VerifyInstalled(true);

            Assert.False(LogVerifier.MessageInLogFile(uninstallLogPath, "TESTBA: OnCachePackageNonVitalValidationFailure() - id: TestExe"));
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"testexe.exe\" /regd HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId}"));
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"TestExe.exe\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\""));
        }

        [RuntimeFact]
        public void CanSkipReinstallPerMachineArpEntryExePackageOnUninstallRollback()
        {
            var packageTestExe = this.CreatePackageInstaller("PackageTestExe");
            var perMachineArpEntryExePackageUninstallFailureBundle = this.CreateBundleInstaller(@"PerMachineArpEntryExePackageUninstallFailure");
            var arpEntryExePackage = this.CreateArpEntryInstaller(perMachineArpEntryExePackageUninstallFailureBundle, "TestExe");
            var arpId = arpEntryExePackage.ArpId;
            var testBAController = this.CreateTestBAController();

            arpEntryExePackage.VerifyRegistered(false);
            packageTestExe.VerifyInstalled(false);

            testBAController.SetPackageRequestedCacheType("TestExe", BOOTSTRAPPER_CACHE_TYPE.Remove);

            var installLogPath = perMachineArpEntryExePackageUninstallFailureBundle.Install();
            perMachineArpEntryExePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);
            packageTestExe.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\""));

            var uninstallLogPath = perMachineArpEntryExePackageUninstallFailureBundle.Uninstall((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE, "FAILWHENDEFERRED=1");
            perMachineArpEntryExePackageUninstallFailureBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(false);
            packageTestExe.VerifyInstalled(true);

            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, "TESTBA: OnCachePackageNonVitalValidationFailure() - id: TestExe, default: None, requested: None"));
            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"testexe.exe\" /regd HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId}"));
        }

        [RuntimeFact]
        public void CanUninstallPerMachineArpEntryExePackageOnInstallRollback()
        {
            var perMachineArpEntryExePackageFailureBundle = this.CreateBundleInstaller(@"PerMachineArpEntryExePackageFailure");
            var arpEntryExePackage = this.CreateArpEntryInstaller(perMachineArpEntryExePackageFailureBundle, "TestExe");
            var arpId = arpEntryExePackage.ArpId;

            arpEntryExePackage.VerifyRegistered(false);

            var installLogPath = perMachineArpEntryExePackageFailureBundle.Install((int)MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);
            perMachineArpEntryExePackageFailureBundle.VerifyUnregisteredAndRemovedFromPackageCache();
            arpEntryExePackage.VerifyRegistered(false);

            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\""));
            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"testexe.exe\" /regd HKLM\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId}"));
        }

        [RuntimeFact]
        public void CanInstallAndUninstallPerUserArpEntryExePackage()
        {
            var perUserArpEntryExePackageBundle = this.CreateBundleInstaller(@"PerUserArpEntryExePackage");
            var arpEntryExePackage = this.CreateArpEntryInstaller(perUserArpEntryExePackageBundle, "TestExe");
            var arpId = arpEntryExePackage.ArpId;

            arpEntryExePackage.VerifyRegistered(false);

            var installLogPath = perUserArpEntryExePackageBundle.Install();
            perUserArpEntryExePackageBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);

            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\" /regw \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},QuietUninstallString,String,\\\""));

            var uninstallLogPath = perUserArpEntryExePackageBundle.Uninstall();
            perUserArpEntryExePackageBundle.VerifyUnregisteredAndRemovedFromPackageCache();
            arpEntryExePackage.VerifyRegistered(false);

            Assert.True(LogVerifier.MessageInLogFile(uninstallLogPath, $"testexe.exe\" /regd HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId}"));
        }

        [RuntimeFact]
        public void FailsUninstallWhenPerUserArpEntryExePackageMissingQuietUninstallString()
        {
            var brokenPerUserArpEntryExePackageBundle = this.CreateBundleInstaller(@"BrokenPerUserArpEntryExePackage");
            var arpEntryExePackage = this.CreateArpEntryInstaller(brokenPerUserArpEntryExePackageBundle, "TestExe");
            var arpId = arpEntryExePackage.ArpId;

            arpEntryExePackage.VerifyRegistered(false);
            brokenPerUserArpEntryExePackageBundle.VerifyUnregisteredAndRemovedFromPackageCache();

            var installLogPath = brokenPerUserArpEntryExePackageBundle.Install();
            brokenPerUserArpEntryExePackageBundle.VerifyRegisteredAndInPackageCache();
            arpEntryExePackage.VerifyRegistered(true);

            Assert.True(LogVerifier.MessageInLogFile(installLogPath, $"TestExe.exe\" /regw \"HKCU\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{arpId},DisplayVersion,String,1.0.0.0\""));

            brokenPerUserArpEntryExePackageBundle.Uninstall((int)MSIExec.MSIExecReturnCode.ERROR_INVALID_PARAMETER);
            brokenPerUserArpEntryExePackageBundle.VerifyRegisteredAndInPackageCache();

            arpEntryExePackage.Unregister();

            brokenPerUserArpEntryExePackageBundle.Uninstall();
            brokenPerUserArpEntryExePackageBundle.VerifyUnregisteredAndRemovedFromPackageCache();
        }
    }
}
