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
            WixAssert.CompareLineByLine(new[]
            {
                "[Converted] This file contains an XML declaration on the first line. (DeclarationPresent)",
                "[Converted] The namespace 'http://schemas.microsoft.com/wix/2006/wi' is out of date. It must be 'http://wixtoolset.org/schemas/v4/wxs'. (XmlnsValueWrong)",
                "[Converted] The TARGETDIR directory should no longer be explicitly referenced. Remove the DirectoryRef element with Id attribute 'TARGETDIR'. (StandardDirectoryRefDeprecated)",
                "A reference to the TARGETDIR Directory was removed. This can cause unintended side effects. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages (TargetDirRefRemoved)",
            }, messaging.Messages.Select(m => m.ToString()).ToArray());
            Assert.Equal(4, errors);
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
            Assert.Equal(5, errors);
        }

        [Fact]
        public void RemoveTargetDirWithComponents()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <DirectoryRef Id='TARGETDIR'>",
                "      <Component Id='C1'>",
                "        <File Source='c1.txt' />",
                "      </Component>",
                "      <Component Id='C2'>",
                "        <File Source='c2.txt' />",
                "      </Component>",
                "    </DirectoryRef>",
                "  </Fragment>",
                "  <Fragment>",
                "    <Directory Id='TARGETDIR'>",
                "      <Component Id='C3'>",
                "        <File Source='c3.txt' />",
                "      </Component>",
                "      <Component Id='C4' Directory='PreExisting'>",
                "        <File Source='c4.txt' />",
                "      </Component>",
                "    </Directory>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "      <Component Id=\"C1\" Directory=\"TARGETDIR\">",
                "        <File Id=\"c1.txt\" Source=\"c1.txt\" />",
                "      </Component>",
                "      <Component Id=\"C2\" Directory=\"TARGETDIR\">",
                "        <File Id=\"c2.txt\" Source=\"c2.txt\" />",
                "      </Component>",
                "    </Fragment>",
                "  <Fragment>",
                "      <Component Id=\"C3\" Directory=\"TARGETDIR\">",
                "        <File Id=\"c3.txt\" Source=\"c3.txt\" />",
                "      </Component>",
                "      <Component Id=\"C4\" Directory=\"PreExisting\">",
                "        <File Id=\"c4.txt\" Source=\"c4.txt\" />",
                "      </Component>",
                "    </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);

            WixAssert.CompareLineByLine(new[]
            {
                "[Converted] This file contains an XML declaration on the first line. (DeclarationPresent)",
                "[Converted] The namespace 'http://schemas.microsoft.com/wix/2006/wi' is out of date. It must be 'http://wixtoolset.org/schemas/v4/wxs'. (XmlnsValueWrong)",
                "[Converted] The TARGETDIR directory should no longer be explicitly referenced. Remove the DirectoryRef element with Id attribute 'TARGETDIR'. (StandardDirectoryRefDeprecated)",
                "A reference to the TARGETDIR Directory was removed. This can cause unintended side effects. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages (TargetDirRefRemoved)",
                "[Converted] The file id is being updated to 'c1.txt' to ensure it remains the same as the v3 default (AssignAnonymousFileId)",
                "[Converted] The file id is being updated to 'c2.txt' to ensure it remains the same as the v3 default (AssignAnonymousFileId)",
                "[Converted] The TARGETDIR directory should no longer be explicitly defined. Remove the Directory element with Id attribute 'TARGETDIR'. (TargetDirDeprecated)",
                "[Converted] The file id is being updated to 'c3.txt' to ensure it remains the same as the v3 default (AssignAnonymousFileId)",
                "[Converted] The file id is being updated to 'c4.txt' to ensure it remains the same as the v3 default (AssignAnonymousFileId)"
            }, messaging.Messages.Select(m => m.ToString()).ToArray());

            Assert.Equal(9, errors);
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
                "Referencing 'TARGETDIR' directory directly is no longer supported. The DirectoryRef will not be removed but you will probably need to reference a more specific directory. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages (EmptyStandardDirectoryRefNotConvertable)",
                "Referencing 'ProgramFilesFolder' directory directly is no longer supported. The DirectoryRef will not be removed but you will probably need to reference a more specific directory. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages (EmptyStandardDirectoryRefNotConvertable)",
                "Referencing 'DesktopFolder' directory directly is no longer supported. The DirectoryRef will not be removed but you will probably need to reference a more specific directory. See the conversion FAQ for more information: https://wixtoolset.org/docs/fourthree/faqs/#converting-packages (EmptyStandardDirectoryRefNotConvertable)"
            }, messaging.Messages.Select(m => m.ToString()).ToArray());

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
            Assert.Equal(4, errors);
        }
    }
}
