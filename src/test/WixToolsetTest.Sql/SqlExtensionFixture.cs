// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Sql
{
    using System.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using WixToolset.Sql;
    using Xunit;

    public class SqlExtensionFixture
    {
        [Fact]
        public void CanBuildUsingSqlStuff()
        {
            var folder = TestData.Get(@"TestData\UsingSql");
            var build = new Builder(folder, typeof(SqlExtensionFactory), new[] { folder });

            var results = build.BuildAndQuery(Build, "Wix4SqlDatabase", "Wix4SqlFileSpec", "Wix4SqlScript", "Wix4SqlString");
            WixAssert.CompareLineByLine(new[]
            {
                "Wix4SqlDatabase:TestDB\tMySQLHostName\tMyInstanceName\tMyDB\tDatabaseComponent\t\tTestFileSpecId\tTestLogFileSpecId\t35",
                "Wix4SqlFileSpec:TestFileSpecId\tTestFileSpecLogicalName\tTestFileSpec\t10MB\t100MB\t10%",
                "Wix4SqlFileSpec:TestLogFileSpecId\tTestLogFileSpecLogicalName\tTestLogFileSpec\t1MB\t10MB\t1%",
                "Wix4SqlScript:TestScript\tTestDB\tDatabaseComponent\tScriptBinary\t\t1\t",
                "Wix4SqlString:TestString\tTestDB\tDatabaseComponent\tCREATE TABLE TestTable1(name varchar(20), value varchar(20))\t\t1\t",
            }, results.ToArray());
        }

        private static void Build(string[] args)
        {
            var result = WixRunner.Execute(args)
                                  .AssertSuccess();
        }
    }
}
