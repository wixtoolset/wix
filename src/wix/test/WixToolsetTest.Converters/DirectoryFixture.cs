// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Linq;
    using System.Xml.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class DirectoryFixture : BaseConverterFixture
    {
        [Fact]
        public void RemoveTargetDir()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Directory Id='TARGETDIR' Name='SourceDir'>",
                "      <!-- Comment -->",
                "      <Directory Id='RootFolder' Name='Root'>",
                "        <Directory Id='ChildFolder' Name='Child' />",
                "      </Directory>",
                "    </Directory>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "      <!-- Comment -->",
                "      <Directory Id=\"RootFolder\" Name=\"Root\">",
                "        <Directory Id=\"ChildFolder\" Name=\"Child\" />",
                "      </Directory>",
                "    </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
            Assert.Equal(3, errors);
        }

        [Fact]
        public void RemoveTargetDirRef()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <DirectoryRef Id='TARGETDIR'>",
                "      <!-- Comment -->",
                "      <Directory Id='RootFolder' Name='Root'>",
                "        <Directory Id='ChildFolder' Name='Child' />",
                "      </Directory>",
                "    </DirectoryRef>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "      <!-- Comment -->",
                "      <Directory Id=\"RootFolder\" Name=\"Root\">",
                "        <Directory Id=\"ChildFolder\" Name=\"Child\" />",
                "      </Directory>",
                "    </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
            Assert.Equal(3, errors);
        }

        [Fact]
        public void FixStandardDirectory()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Directory Id='TARGETDIR' Name='SourceDir'>",
                "      <Directory Id='ProgramFilesFolder' Name='PFiles'>",
                "        <Directory Id='ChildFolder' Name='Child' />",
                "      </Directory>",
                "    </Directory>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "      <StandardDirectory Id=\"ProgramFilesFolder\">",
                "        <Directory Id=\"ChildFolder\" Name=\"Child\" />",
                "      </StandardDirectory>",
                "    </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
            Assert.Equal(4, errors);
        }

        [Fact]
        public void FixStandardDirectoryRef()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <DirectoryRef Id='ProgramFilesFolder'>",
                "      <Directory Id='ChildFolder' Name='Child' />",
                "    </DirectoryRef>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <StandardDirectory Id=\"ProgramFilesFolder\">",
                "      <Directory Id=\"ChildFolder\" Name=\"Child\" />",
                "    </StandardDirectory>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
            Assert.Equal(3, errors);
        }

        [Fact]
        public void RemoveTargetDirRefAndFixStandardDirectory()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <DirectoryRef Id='TARGETDIR'>",
                "      <Directory Id='ProgramFilesFolder' Name='PFiles'>",
                "        <Directory Id='ChildFolder' Name='Child' />",
                "      </Directory>",
                "    </DirectoryRef>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "      <StandardDirectory Id=\"ProgramFilesFolder\">",
                "        <Directory Id=\"ChildFolder\" Name=\"Child\" />",
                "      </StandardDirectory>",
                "    </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
            Assert.Equal(4, errors);
        }

        [Fact]
        public void ErrorOnEmptyStandardDirectoryRef()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <DirectoryRef Id='TARGETDIR' />",
                "    <DirectoryRef Id='ProgramFilesFolder' />",
                "    <DirectoryRef Id='DesktopFolder' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <DirectoryRef Id=\"TARGETDIR\" />",
                "    <DirectoryRef Id=\"ProgramFilesFolder\" />",
                "    <DirectoryRef Id=\"DesktopFolder\" />",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            WixAssert.CompareLineByLine(new[]
            {
                "[Converted] The namespace 'http://schemas.microsoft.com/wix/2006/wi' is out of date. It must be 'http://wixtoolset.org/schemas/v4/wxs'. (XmlnsValueWrong)",
                "Referencing 'TARGETDIR' directory directly is no longer supported. The DirectoryRef will not be removed but you will probably need to reference a more specific directory. (EmptyStandardDirectoryRefNotConvertable)",
                "Referencing 'ProgramFilesFolder' directory directly is no longer supported. The DirectoryRef will not be removed but you will probably need to reference a more specific directory. (EmptyStandardDirectoryRefNotConvertable)",
                "Referencing 'DesktopFolder' directory directly is no longer supported. The DirectoryRef will not be removed but you will probably need to reference a more specific directory. (EmptyStandardDirectoryRefNotConvertable)"
            }, messaging.Messages.Select(m => m.ToString()).ToArray());

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
            Assert.Equal(4, errors);
        }
    }
}
