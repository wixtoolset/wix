// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class IisExtensionTests : MsiE2ETests
    {
        public IisExtensionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimePrereqFeatureFact("IIS-WebServerRole", "IIS-WebServer")]
        public void CanInstallAndUninstallIis()
        {
            var product = this.CreatePackageInstaller("InstallIis");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [RuntimePrereqFeatureFact("IIS-WebServerRole", "IIS-WebServer")]
        public void CanInstallAndUninstallWebDirProperties()
        {
            var product = this.CreatePackageInstaller("WebDirProperties");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
