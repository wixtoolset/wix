// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using WixTestTools;
    using WixToolset.BootstrapperApplicationApi;
    using Xunit;
    using Xunit.Abstractions;

    public class ConfigurableScopeTests : BurnE2ETests
    {
        public ConfigurableScopeTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CommandLineScopeTestNoopBecauseNonDefaultPlan()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerMachine);

            var bundle = this.CreateBundleInstaller("AllPmouBundleTestBA");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install(arguments: "/peruser");

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Scope command-line switch ignored because the bootstrapper application already specified a scope."));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void CommandLineScopeTestNoopBecauseNoConfigurablePackages()
        {
            var bundle = this.CreateBundleInstaller("PerMachineBundle");
            var log = bundle.Install(arguments: "/peruser");

            Assert.True(LogVerifier.MessageInLogFile(log, "Scope command-line switch ignored because the bundle doesn't have any packages with configurable scope."));
        }

        [RuntimeFact]
        public void CommandLineScopeTestPerUser()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.Default);

            var bundle = this.CreateBundleInstaller("AllPmouBundleTestBA");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install(arguments: "/peruser");

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 3 packages, action: Install, planned scope: Default"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void CommandLineScopeTestPerMachine()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.Default);

            var bundle = this.CreateBundleInstaller("AllPuomBundleTestBA");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install(arguments: "/permachine");

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 3 packages, action: Install, planned scope: Default"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PMOU_Bundle_Default_Plan_Installs_PerMachine()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.Default);

            var bundle = this.CreateBundleInstaller("AllPmouBundleTestBA");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 3 packages, action: Install, planned scope: Default"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));

            log = bundle.Repair();
            Assert.True(LogVerifier.MessageInLogFile(log, "Bundle was already installed with scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Detected package: PmouPkg1.msi, state: Present, authored scope: PerMachineOrUser, detected scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Detected package: PmouPkg2.msi, state: Present, authored scope: PerMachineOrUser, detected scope: PerMachine,"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PUOM_Bundle_Default_Plan_Installs_PerUser()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.Default);

            var bundle = this.CreateBundleInstaller("AllPuomBundleTestBA");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 3 packages, action: Install, planned scope: Default"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PMOU_Bundle_PM_Plan_Installs_PerMachine()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerMachine);

            var bundle = this.CreateBundleInstaller("AllPmouBundleTestBA");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 3 packages, action: Install, planned scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PUOM_Bundle_PM_Plan_Installs_PerMachine()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerMachine);

            var bundle = this.CreateBundleInstaller("AllPuomBundleTestBA");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 3 packages, action: Install, planned scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PMOU_Bundle_PU_Plan_Installs_PerUser()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerUser);

            var bundle = this.CreateBundleInstaller("AllPmouBundleTestBA");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 3 packages, action: Install, planned scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser,"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PUOM_Bundle_PU_Plan_Installs_PerUser()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerUser);

            var bundle = this.CreateBundleInstaller("AllPuomBundleTestBA");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 3 packages, action: Install, planned scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PMOU_Bundle_Default_Plan_Installs_PerMachine()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.Default);

            var bundle = this.CreateBundleInstaller("PmPmouBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 4 packages, action: Install, planned scope: Default"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PMOU_Bundle_PM_Plan_Installs_PerMachine()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerMachine);

            var bundle = this.CreateBundleInstaller("PmPmouBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 4 packages, action: Install, planned scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PMOU_Bundle_PU_Plan_Installs_PerUserMostly()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerUser);

            var bundle = this.CreateBundleInstaller("PmPmouBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 4 packages, action: Install, planned scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser,"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PUOM_Bundle_Default_Plan_Installs_PerUser()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.Default);

            var bundle = this.CreateBundleInstaller("PmPuomBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 4 packages, action: Install, planned scope: Default"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PUOM_Bundle_PM_Plan_Installs_PerMachine()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerMachine);

            var bundle = this.CreateBundleInstaller("PmPuomBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 4 packages, action: Install, planned scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PUOM_Bundle_PU_Plan_Installs_PerUserMostly()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerUser);

            var bundle = this.CreateBundleInstaller("PmPuomBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 4 packages, action: Install, planned scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned configurable scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PU_PMOU_Bundle_Default_Plan_Installs_PerMachine()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.Default);

            var bundle = this.CreateBundleInstaller("PmPuPmouBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 5 packages, action: Install, planned scope: Default"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerUserPkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));

            log = bundle.Repair();
            Assert.True(LogVerifier.MessageInLogFile(log, "Detected package: PerMachinePkg.msi, state: Present, authored scope: PerMachine, detected scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Detected package: PmouPkg1.msi, state: Present, authored scope: PerMachineOrUser, detected scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Detected package: PmouPkg2.msi, state: Present, authored scope: PerMachineOrUser, detected scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Detected package: PerUserPkg.msi, state: Present, authored scope: PerUser, detected scope: PerUser,"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PU_PMOU_Bundle_PM_Plan_Installs_PerMachine()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerMachine);

            var bundle = this.CreateBundleInstaller("PmPuPmouBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var perUserPkg = this.CreatePackageInstaller("PerUserPkg");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled();
            perUserPkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 5 packages, action: Install, planned scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerUserPkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled(false);
            perUserPkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PU_PMOU_Bundle_PU_Plan_Installs_PerUserMostly()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerUser);

            var bundle = this.CreateBundleInstaller("PmPuPmouBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var perUserPkg = this.CreatePackageInstaller("PerUserPkg");
            var pkg1 = this.CreatePackageInstaller("PmouPkg1");
            var pkg2 = this.CreatePackageInstaller("PmouPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled();
            perUserPkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 5 packages, action: Install, planned scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerUserPkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser,"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PmouPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser,"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled(false);
            perUserPkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PU_PUOM_Bundle_Default_Plan_Installs_PerUser()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.Default);

            var bundle = this.CreateBundleInstaller("PmPuPuomBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var perUserPkg = this.CreatePackageInstaller("PerUserPkg");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled();
            perUserPkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 5 packages, action: Install, planned scope: Default"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerUserPkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled(false);
            perUserPkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PU_PUOM_Bundle_PM_Plan_Installs_PerMachineMostly()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerMachine);

            var bundle = this.CreateBundleInstaller("PmPuPuomBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var perUserPkg = this.CreatePackageInstaller("PerUserPkg");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled();
            perUserPkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 5 packages, action: Install, planned scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerUserPkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: true);
            perMachinePkg.VerifyInstalled(false);
            perUserPkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void PM_PU_PUOM_Bundle_PU_Plan_Installs_PerUserMostly()
        {
            var testBAController = this.CreateTestBAController();
            testBAController.SetBundleScope(BundleScope.PerUser);

            var bundle = this.CreateBundleInstaller("PmPuPuomBundleTestBA");
            var perMachinePkg = this.CreatePackageInstaller("PerMachinePkg");
            var perUserPkg = this.CreatePackageInstaller("PerUserPkg");
            var pkg1 = this.CreatePackageInstaller("PuomPkg1");
            var pkg2 = this.CreatePackageInstaller("PuomPkg2");
            var log = bundle.Install();

            bundle.VerifyRegisteredAndInPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled();
            perUserPkg.VerifyInstalled();
            pkg1.VerifyInstalled();
            pkg2.VerifyInstalled();

            Assert.True(LogVerifier.MessageInLogFile(log, "Plan begin, 5 packages, action: Install, planned scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerMachinePkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerMachine"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PerUserPkg.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg1.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));
            Assert.True(LogVerifier.MessageInLogFile(log, "Planned package: PuomPkg2.msi, state: Absent, default requested: Present, ba requested: Present, execute: Install, rollback: Uninstall, scope: PerUser"));

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache(plannedPerMachine: false);
            perMachinePkg.VerifyInstalled(false);
            perUserPkg.VerifyInstalled(false);
            pkg1.VerifyInstalled(false);
            pkg2.VerifyInstalled(false);
        }


    }
}
