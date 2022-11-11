// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.BurnE2E
{
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class BAFunctionsTests : BurnE2ETests
    {
        public BAFunctionsTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void LogsPersistedRelatedBundleVariables()
        {
            this.CreatePackageInstaller("PackageAv1");
            this.CreatePackageInstaller("PackageAv2");
            var bundleAv1 = this.CreateBundleInstaller("BundleAv1");
            var bundleAv2 = this.CreateBundleInstaller("BundleAv2");

            bundleAv1.Install();
            bundleAv1.VerifyRegisteredAndInPackageCache();

            var bundleAv2InstallLogFilePath = bundleAv2.Install();
            bundleAv2.VerifyRegisteredAndInPackageCache();

            Assert.True(LogVerifier.MessageInLogFileRegex(bundleAv2InstallLogFilePath, @"Retrieved related bundle variable with BAFunctions: ANumber = 42"));
            Assert.True(LogVerifier.MessageInLogFileRegex(bundleAv2InstallLogFilePath, @"Retrieved related bundle variable with BAFunctions: AString = This is a test"));
        }
    }
}
