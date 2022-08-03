// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class WixIuiBaTests : BurnE2ETests
    {
        public WixIuiBaTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanInstallArchitectureSpecificPrimaryPackage()
        {
            var defaultPackage = this.CreatePackageInstaller("InternalUIPackage");
            var x86Package = this.CreatePackageInstaller("InternalUIx86Package");
            var x64Package = this.CreatePackageInstaller("InternalUIx64Package");
            var arm64Package = this.CreatePackageInstaller("InternalUIarm64Package");
            var bundle = this.CreateBundleInstaller("ArchSpecificBundle");

            defaultPackage.VerifyInstalled(false);
            x86Package.VerifyInstalled(false);
            x64Package.VerifyInstalled(false);
            arm64Package.VerifyInstalled(false);
            bundle.VerifyUnregisteredAndRemovedFromPackageCache();

            bundle.Install();
            bundle.VerifyRegisteredAndInPackageCache();
            defaultPackage.VerifyInstalled(false);

            var archSpecificInstalls = 0;
            if (x86Package.IsInstalled())
            {
                ++archSpecificInstalls;
            }

            if (x64Package.IsInstalled())
            {
                ++archSpecificInstalls;
            }

            if (arm64Package.IsInstalled())
            {
                ++archSpecificInstalls;
            }

            Assert.Equal(1, archSpecificInstalls);

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache();
            defaultPackage.VerifyInstalled(false);
            x86Package.VerifyInstalled(false);
            x64Package.VerifyInstalled(false);
            arm64Package.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void CanSilentlyInstallAndUninstallEmbeddedUIBundle()
        {
            var prereqPackage = this.CreatePackageInstaller("InternalUIPackage");
            var package = this.CreatePackageInstaller("EmbeddedUIPackage");
            var bundle = this.CreateBundleInstaller("EmbeddedUIBundle");

            prereqPackage.VerifyInstalled(false);
            package.VerifyInstalled(false);
            bundle.VerifyUnregisteredAndRemovedFromPackageCache();

            bundle.Install();
            bundle.VerifyRegisteredAndInPackageCache();
            prereqPackage.VerifyInstalled(true);
            package.VerifyInstalled(true);

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache();
            prereqPackage.VerifyInstalled(true);
            package.VerifyInstalled(false);
        }

        [RuntimeFact]
        public void CanSilentlyInstallAndUninstallInternalUIBundle()
        {
            var package = this.CreatePackageInstaller("InternalUIPackage");
            var bundle = this.CreateBundleInstaller("InternalUIBundle");

            package.VerifyInstalled(false);
            bundle.VerifyUnregisteredAndRemovedFromPackageCache();

            bundle.Install();
            bundle.VerifyRegisteredAndInPackageCache();
            package.VerifyInstalled(true);

            bundle.Uninstall();
            bundle.VerifyUnregisteredAndRemovedFromPackageCache();
            package.VerifyInstalled(false);
        }

        // Manual test for EmbeddedUIBundle:
        //  1. Double click EmbeddedUIBundle.exe.
        //  2. Verify that the prereq BA came up and click the install button (allow elevation).
        //  3. Verify that the prereq BA automatically closed after installing the prereq.
        //  4. Verify that the MSI UI came up and click the install button.
        //  5. After it's finished, click the exit button.
        //  6. Verify that no other UI is shown and that everything was installed.
        //  7. Double click EmbeddedUIBundle.exe (allow elevation).
        //  8. Verify that the prereq BA did not come up.
        //  9. Verify that the MSI UI came up and click the uninstall button.
        // 10. After it's finished, click the exit button.
        // 11. Verify that no other UI is shown and that everything was uninstalled except for the prereq which was permanent.
        // 12. Uninstall InternalUIPackage to make sure the machine is clean for other tests.

        // Alternate EmbeddedUIBundle test - manually install InternalUIPackage first and verify that the prereq BA doesn't come up during install either.

        // Manual test for InternalUIBundle:
        //  1. Double click InternalUIBundle.exe on a machine that will prompt for elevation.
        //  2. Verify that the splash screen appeared but the prereq BA did not come up.
        //  3. Verify that the elevation prompt came up immediately instead of flashing on the taskbar.
        //  4. Allow elevation.
        //  5. Verify that the MSI UI came up and the splash screen disappeared.
        //  6. Accept the two CA messages and click the install button.
        //  7. After it's finished, click the exit button.
        //  8. Verify that no other UI is shown and that everything was installed.
        //  9. Double click InternalUIBundle.exe (allow elevation).
        // 10. Verify that the prereq BA did not come up.
        // 11. Verify that the MSI UI came up and click the uninstall button.
        // 12. After it's finished, click the exit button.
        // 13. Verify that no other UI is shown and that everything was uninstalled to make sure the machine is clean for other tests.

        // Manual test for Help:
        // 1. Run EmbeddedUIBundle.exe /help from the command line.
        // 2. Verify that the prereq BA shows the help information without trying to install the prereqs.

        // Manual test for Layout:
        // 1. Run EmbeddedUIBundle.exe /layout from an unelevated command line on a machine that will prompt for elevation.
        // 2. Verify that the prereq BA performs the layout without requiring any input from the user.
        // 3. Verify that it never prompted for elevation.
        // 4. Click the exit button.

        // Manual test for Caching error:
        // 1. Copy InternalUIBundle.exe to a separate folder so that it can't find InternalUIPackage.msi.
        // 2. Attempt to install InternalUIBundle.exe (allow elevation).
        // 3. Verify that the prereq BA comes up with the Failure page saying that a file couldn't be found.
    }
}
