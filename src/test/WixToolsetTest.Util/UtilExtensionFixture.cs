// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.VisualStudio
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Util;
    using Xunit;

    public class VisualStudioExtensionFixture
    {
        [Fact]
        public void CanBuildUsingFileShare()
        {
            var folder = TestData.Get(@"TestData\UsingFileShare");
            var build = new Builder(folder, typeof(UtilExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "FileShare", "FileSharePermissions");
            Assert.Equal(new[]
            {
                "FileShare:SetVS2010Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2010_VSIX_INSTALLER_PATH]\t0",
                "FileSharePermissions:SetVS2012Vsix\t51\tVS_VSIX_INSTALLER_PATH\t[VS2012_VSIX_INSTALLER_PATH]\t0",
            }, results.OrderBy(s => s).ToArray());
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args, out var messages);
            Assert.Equal(0, result);
            Assert.Empty(messages);
        }
    }
}
