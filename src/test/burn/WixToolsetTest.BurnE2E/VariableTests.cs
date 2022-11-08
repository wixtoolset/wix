// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using System.Collections.Generic;
    using System.IO;
    using WixInternal.TestSupport;
    using WixTestTools;
    using WixToolset.Mba.Core;
    using Xunit;
    using Xunit.Abstractions;

    public class VariableTests : BurnE2ETests
    {
        public VariableTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanHideHiddenVariables()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var bundleA = this.CreateBundleInstaller("BundleA");

            packageA.VerifyInstalled(false);

            var logFilePath = bundleA.Install(0, "InstallLocation=nothingtoseehere", "licensekey=supersecretkey");
            bundleA.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);

            // Burn logging its command line.
            Assert.True(LogVerifier.MessageInLogFile(logFilePath, "InstallLocation=nothingtoseehere licensekey=*****"));
            // Burn logging the MSI install command line.
            Assert.True(LogVerifier.MessageInLogFile(logFilePath, "INSTALLLOCATION=\"nothingtoseehere\" LICENSEKEY=\"*****\""));
            Assert.False(LogVerifier.MessageInLogFile(logFilePath, "supersecretkey"));
        }

        [RuntimeFact]
        public void CanSupportCaseSensitiveVariables()
        {
            var packageA = this.CreatePackageInstaller("PackageA");
            var bundleB = this.CreateBundleInstaller("BundleB");

            packageA.VerifyInstalled(false);

            var logFilePath = bundleB.Install(0, "InstallLocation=nothingtoseehere", "licensekey=supersecretkey");
            bundleB.VerifyRegisteredAndInPackageCache();

            packageA.VerifyInstalled(true);

            // Burn logging its command line.
            Assert.True(LogVerifier.MessageInLogFile(logFilePath, "InstallLocation=nothingtoseehere licensekey=*****"));
            // Burn logging the MSI install command line.
            Assert.True(LogVerifier.MessageInLogFile(logFilePath, "INSTALLLOCATION=\"\" LICENSEKEY=\"*****\""));
        }
    }
}
