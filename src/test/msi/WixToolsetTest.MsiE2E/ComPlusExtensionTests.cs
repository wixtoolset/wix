// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class ComPlusExtensionTests : MsiE2ETests
    {
        public ComPlusExtensionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanInstallUninstallNativeWithoutPartitions()
        {
            var product = this.CreatePackageInstaller("InstallUninstallNativeWithoutPartitions");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [RuntimeFact]
        public void CanInstallUninstallNET3WithoutPartitions()
        {
            var product = this.CreatePackageInstaller("InstallUninstallNET3WithoutPartitions");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [RuntimeFact]
        public void CanInstallUninstallNET4WithoutPartitions()
        {
            var product = this.CreatePackageInstaller("InstallUninstallNET4WithoutPartitions");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
        }

        [RuntimeFact(RequireWindowsServer = true)]
        public void CanInstallAndUninstallWithPartitions()
        {
            var product = this.CreatePackageInstaller("InstallUninstallWithPartitions");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
