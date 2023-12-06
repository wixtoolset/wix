// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using Xunit;

    public class UIFixture
    {
        [Fact]
        public void PopulatesControlTables()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "UI", "DialogsInInstallUISequence.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));

                var results = Query.QueryDatabase(msiPath, new[] { "CheckBox", "Control", "ControlCondition", "InstallUISequence" });
                WixAssert.CompareLineByLine(new[]
                {
                    "CheckBox:WIXUI_EXITDIALOGOPTIONALCHECKBOX\t1",
                    "Control:FirstDialog\tHeader\tText\t0\t13\t90\t13\t3\t\tFirstDialogHeader\tTitle\t",
                    "Control:FirstDialog\tTitle\tText\t0\t0\t90\t13\t3\t\tFirstDialogTitle\tHeader\t",
                    "Control:SecondDialog\tOptionalCheckBox\tCheckBox\t0\t13\t100\t40\t2\tWIXUI_EXITDIALOGOPTIONALCHECKBOX\t[WIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT]\tTitle\tOptional checkbox|Check this box for fun",
                    "Control:SecondDialog\tTitle\tText\t0\t0\t90\t13\t3\t\tSecondDialogTitle\tOptionalCheckBox\t",
                    "ControlCondition:FirstDialog\tHeader\tDisable\tInstalled",
                    "ControlCondition:FirstDialog\tHeader\tHide\tInstalled",
                    "ControlCondition:SecondDialog\tOptionalCheckBox\tShow\tWIXUI_EXITDIALOGOPTIONALCHECKBOXTEXT AND NOT Installed",
                    "InstallUISequence:CostFinalize\t\t1000",
                    "InstallUISequence:CostInitialize\t\t800",
                    "InstallUISequence:ExecuteAction\t\t1300",
                    "InstallUISequence:FileCost\t\t900",
                    "InstallUISequence:FindRelatedProducts\t\t25",
                    "InstallUISequence:FirstDialog\tInstalled AND PATCH\t1298",
                    "InstallUISequence:LaunchConditions\t\t100",
                    "InstallUISequence:MigrateFeatureStates\t\t1200",
                    "InstallUISequence:SecondDialog\tNOT Installed\t1299",
                    "InstallUISequence:ValidateProductID\t\t700",
                }, results);
            }
        }

        [Fact]
        public void CanErrorWithInvalidControlType()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin\test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "UI", "DialogWithInvalidControlType.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "MinimalComponentGroup.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath,
                });

                var errors = result.Messages.Where(m => m.Level == MessageLevel.Error).ToList();
                Assert.Equal(new[]
                {
                    21,
                    52
                }, errors.Select(e => e.Id).ToArray());
            }
        }
    }
}
