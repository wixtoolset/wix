// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System;
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Dtf.WindowsInstaller;
    using Xunit;

    public class InstanceTransformFixture
    {
        [Fact]
        public void CanBuildInstanceTransform()
        {
            var folder = TestData.Get("TestData", "InstanceTransform");

            using (var fs = new DisposableFileSystem())
            {
                var intermediateFolder = fs.GetFolder();
                var msiPath = Path.Combine(intermediateFolder, "bin", "test.msi");
                var wixpdbPath = Path.Combine(intermediateFolder, "bin", "test.wixpdb");
                var mstPath = Path.Combine(intermediateFolder, "iii.mst");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Package.wxs"),
                    Path.Combine(folder, "PackageComponents.wxs"),
                    "-loc", Path.Combine(folder, "Package.en-us.wxl"),
                    "-bindpath", Path.Combine(folder, "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                var output = WindowsInstallerData.Load(wixpdbPath, false);
                var substorage = output.SubStorages.Single();
                Assert.Equal("I1", substorage.Name);

                var data = substorage.Data;
                WixAssert.CompareLineByLine(new[]
                {
                    "_SummaryInformation",
                    "Property",
                    "Upgrade"
                }, data.Tables.Select(t => t.Name).ToArray());

                WixAssert.CompareLineByLine(new[]
                {
                    "INSTANCEPROPERTY\tI1",
                    "ProductName\tMsiPackage (Instance 1)",
                }, JoinRows(data.Tables["Property"]));

                WixAssert.CompareLineByLine(new[]
                {
                    "{22222222-2222-2222-2222-222222222222}\t\t1.0.0.0\t1033\t1\t\tWIX_UPGRADE_DETECTED",
                    "{11111111-1111-1111-1111-111111111111}\t\t1.0.0.0\t1033\t1\t0\t0",
                    "{22222222-2222-2222-2222-222222222222}\t1.0.0.0\t\t1033\t2\t\tWIX_DOWNGRADE_DETECTED",
                    "{11111111-1111-1111-1111-111111111111}\t1.0.0.0\t\t1033\t2\t0\t0",
                }, JoinRows(data.Tables["Upgrade"]));

                var names = Query.GetSubStorageNames(msiPath);
                Query.ExtractSubStorage(msiPath, "I1", mstPath);

                using (var db = new Database(msiPath, DatabaseOpenMode.Transact))
                {
                    db.ApplyTransform(mstPath);

                    var results = Query.QueryDatabase(db, new[] { "Property", "Upgrade" });
                    var resultsWithoutProductCode = results.Where(s => !s.StartsWith("Property:ProductCode\t{")).ToArray();
                    WixAssert.CompareLineByLine(new[]
                    {
                        "Property:ALLUSERS\t1",
                        "Property:INSTANCEPROPERTY\tI1",
                        "Property:Manufacturer\tExample Corporation",
                        "Property:ProductLanguage\t1033",
                        "Property:ProductName\tMsiPackage (Instance 1)",
                        "Property:ProductVersion\t1.0.0.0",
                        "Property:SecureCustomProperties\tINSTANCEPROPERTY;WIX_DOWNGRADE_DETECTED;WIX_UPGRADE_DETECTED",
                        "Property:UpgradeCode\t{22222222-2222-2222-2222-222222222222}",
                        "Upgrade:{22222222-2222-2222-2222-222222222222}\t\t1.0.0.0\t1033\t1\t\tWIX_UPGRADE_DETECTED",
                        "Upgrade:{22222222-2222-2222-2222-222222222222}\t1.0.0.0\t\t1033\t2\t\tWIX_DOWNGRADE_DETECTED",
                    }, resultsWithoutProductCode);
                }
            }
        }

        private static string[] JoinRows(Table table)
        {
            return table.Rows.Select(r => JoinFields(r.Fields)).ToArray();

            static string JoinFields(Field[] fields)
            {
                return String.Join('\t', fields.Select(f => f.ToString()));
            }
        }
    }
}
