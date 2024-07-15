// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.MsiE2E
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using WixTestTools;
    using Xunit.Abstractions;

    public class MsmqExtensionTests : MsiE2ETests
    {
        public MsmqExtensionTests(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
        }

        [RuntimePrereqFeatureFact("MSMQ-Container", "MSMQ-Server")]
        public void CanInstallAndUninstallMsmq()
        {
            var product = this.CreatePackageInstaller("MsmqInstall");
            product.InstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
            product.UninstallProduct(MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
