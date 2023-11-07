// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.IO;
    using System.Linq;
    using WixInternal.Core.TestPackage;
    using WixInternal.TestSupport;
    using WixToolset.Data;
    using Xunit;

    public class EncodingFixture
    {
        [Fact]
        public void PopulatesAppIdTableWhenAdvertised()
        {
            var folder = TestData.Get(@"TestData");

            using (var fs = new DisposableFileSystem())
            {
                var baseFolder = fs.GetFolder();
                var intermediateFolder = Path.Combine(baseFolder, "obj");
                var msiPath = Path.Combine(baseFolder, @"bin", "test.msi");

                var result = WixRunner.Execute(new[]
                {
                    "build",
                    Path.Combine(folder, "Encoding", "Encoding.wxs"),
                    "-bindpath", Path.Combine(folder, "SingleFile", "data"),
                    "-intermediateFolder", intermediateFolder,
                    "-o", msiPath
                });

                var errors = result.Messages.Where(m => m.Level == MessageLevel.Error).Select(m => m.ToString().Replace(folder, "<testdata>")).ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "A string was provided with characters that are not available in the specified database code page '1252'. Either change these characters to ones that exist in the database's code page, or update the database's code page by modifying one of the following attributes: Package/@Codepage, Module/@Codepage, Patch/@Codepage, or WixLocalization/@Codepage.",
                    "A string was provided with characters that are not available in the specified database code page '1252'. Either change these characters to ones that exist in the database's code page, or update the database's code page by modifying one of the following attributes: Package/@Codepage, Module/@Codepage, Patch/@Codepage, or WixLocalization/@Codepage.",
                    "A string was provided with characters that are not available in the specified database code page '1252'. Either change these characters to ones that exist in the database's code page, or update the database's code page by modifying one of the following attributes: Package/@Codepage, Module/@Codepage, Patch/@Codepage, or WixLocalization/@Codepage.",
                }, errors);
            }
        }
    }
}
