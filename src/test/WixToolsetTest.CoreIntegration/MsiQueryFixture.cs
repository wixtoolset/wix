// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using WixBuildTools.TestSupport;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class MsiQueryFixture
    {
        [Fact(Skip = "Test demonstrates failure")]
        public void PopulatesDirectoryTableWithValidDefaultDir()
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
                    Path.Combine(folder, "DefaultDir", "DefaultDir.wxs"),
                    Path.Combine(folder, "ProductWithComponentGroupRef", "Product.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                result.AssertSuccess();

                Assert.True(File.Exists(msiPath));
                var results = Query.QueryDatabase(msiPath, new[] { "Directory" });
                Assert.Equal(new[]
                {
                    "Directory:INSTALLFOLDER\tProgramFilesFolder\toekcr5lq|MsiPackage",
                    "Directory:NAMEANDSHORTNAME\tINSTALLFOLDER\tSHORTNAM|NameAndShortName",
                    "Directory:NAMEANDSHORTSOURCENAME\tINSTALLFOLDER\tNAMEASSN|NameAndShortSourceName",
                    "Directory:NAMEWITHSHORTVALUE\tINSTALLFOLDER\tSHORTVAL",
                    "Directory:ProgramFilesFolder\tTARGETDIR\t.",
                    "Directory:SHORTNAMEANDLONGSOURCENAME\tINSTALLFOLDER\tSHNALSNM:6ukthv5q|ShortNameAndLongSourceName",
                    "Directory:SHORTNAMEONLY\tINSTALLFOLDER\tSHORTONL",
                    "Directory:SOURCENAME\tINSTALLFOLDER\ts2s5bq-i|NameAndSourceName:dhnqygng|SourceNameWithName",
                    "Directory:SOURCENAMESONLY\tINSTALLFOLDER\t.:SRCNAMON|SourceNameOnly",
                    "Directory:SOURCENAMEWITHSHORTVALUE\tINSTALLFOLDER\t.:SRTSRCVL",
                    "Directory:TARGETDIR\t\tSourceDir",
                }, results);
            }
        }
    }
}
