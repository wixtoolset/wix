// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.PowerShell
{
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.PowerShell;
    using Xunit;

    public class PowerShellExtensionFixture
    {
        [Fact]
        public void CantBuildUsingTypesFileWithoutSnapIn()
        {
            var folder = TestData.Get(@"TestData\TypesFile");
            var build = new Builder(folder, typeof(PowerShellExtensionFactory), new[] { folder });

            WixRunnerResult wixRunnerResult = null;
            var results = build.BuildAndQuery(args => {
                wixRunnerResult = WixRunner.Execute(args);
            });
            Assert.NotNull(wixRunnerResult);
            Assert.Equal((int)PSErrors.Ids.NeitherIdSpecified, wixRunnerResult.ExitCode);
        }
    }
}
