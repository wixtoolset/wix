// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;

[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]

namespace WixToolsetTest.PowerShell
{
    using WixInternal.MSTestSupport;
    using WixInternal.Core.MSTestPackage;
    using WixToolset.PowerShell;

    [TestClass]
    public class PowerShellExtensionFixture
    {
        [TestMethod]
        public void CantBuildUsingTypesFileWithoutSnapIn()
        {
            var folder = TestData.Get(@"TestData\TypesFile");
            var build = new Builder(folder, typeof(PowerShellExtensionFactory), new[] { folder });

            WixRunnerResult wixRunnerResult = null;
            var results = build.BuildAndQuery(args => {
                wixRunnerResult = WixRunner.Execute(args);
            });
            Assert.IsNotNull(wixRunnerResult);
            Assert.AreEqual((int)PSErrors.Ids.NeitherIdSpecified, wixRunnerResult.ExitCode);
        }
    }
}
