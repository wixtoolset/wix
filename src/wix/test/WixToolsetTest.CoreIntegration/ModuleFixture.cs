// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using Xunit;

    public class ModuleFixture
    {
        [Fact]
        public void CanSuppressModularization()
        {
            var folder = TestData.Get(@"TestData\SuppressModularization");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Module.wxs"),
                    "-loc", Path.Combine(folder, "Module.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-sw1079",
                    "-sw1086",
                    "-o", Path.Combine(intermediateFolder, @"bin\test.msm")
                });

                result.AssertSuccess();

                var msmPath = Path.Combine(intermediateFolder, @"bin\test.msm");

                var rows = Query.QueryDatabase(msmPath, new[] { "CustomAction", "Property" });
                WixAssert.CompareLineByLine(new[]
                {
                    "CustomAction:Test\t11265\tFakeCA.243FB739_4D05_472F_9CFB_EF6B1017B6DE\tTestEntry\t",
                    "Property:MsiHiddenProperties\tTest"
                }, rows);
            }
        }
    }
}
