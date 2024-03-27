// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using WixTestTools;
    using Xunit;
    using Xunit.Abstractions;

    public class CustomActionTests : MsiE2ETests
    {
        public CustomActionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper) { }

        [RuntimeFact]
        public void CanInstallAndUninstallWithManagedCustomAction()
        {
            var product = this.CreatePackageInstaller("ManagedCustomActions");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);

            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
