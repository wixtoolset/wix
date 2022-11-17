// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Xml.Linq;
    using WixInternal.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class ConverterFixture : BaseConverterFixture
    {
        private static readonly XNamespace Wix4Namespace = "http://wixtoolset.org/schemas/v4/wxs";

        [Fact]
        public void EnsuresNoDeclaration()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(1, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void EnsuresDeclarationWhenIgnored()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-16'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment />",
                "</Wix>");

            var expected = new[]
            {
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, ignoreErrors: new[] { "DeclarationPresent" });

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document, omitXmlDeclaration: false);

            Assert.Equal(0, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanConvertMainNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(2, errors);
            //Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanConvertMainNamespaceFromDisk()
        {
            var dataFolder = TestData.Get("TestData", "FixDeclarationAndNamespace");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>",
            };

            using (var fs = new TestDataFolderFileSystem())
            {
                fs.Initialize(dataFolder);
                var path = Path.Combine(fs.BaseFolder, "FixDeclarationAndNamespace.wxs");

                var messaging = new MockMessaging();
                var converter = new WixConverter(messaging, 2, null, null);

                var errors = converter.ConvertFile(path, true);

                var messages = messaging.Messages.Select(m => $"{Path.GetFileName(m.SourceLineNumbers.FileName)}({m.SourceLineNumbers.LineNumber}) {m.ToString()}").ToArray();
                WixAssert.CompareLineByLine(new[]
                {
                    "FixDeclarationAndNamespace.wxs() [Converted] This file contains an XML declaration on the first line. (DeclarationPresent)",
                    "FixDeclarationAndNamespace.wxs(1) [Converted] The namespace 'http://schemas.microsoft.com/wix/2006/wi' is out of date.  It must be 'http://wixtoolset.org/schemas/v4/wxs'. (XmlnsValueWrong)"
                }, messages);

                var actual = File.ReadAllLines(path);
                WixAssert.CompareLineByLine(expected, actual);
            }
        }

        [Fact]
        public void CanConvertNamedMainNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<w:Wix xmlns:w='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <w:Fragment />",
                "</w:Wix>");

            var expected = new[]
            {
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <w:Fragment />",
                "</w:Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(2, errors);
            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetNamespaceOfPrefix("w"));
        }

        [Fact]
        public void CanConvertNonWixDefaultNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<w:Wix xmlns:w='http://schemas.microsoft.com/wix/2006/wi' xmlns='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <w:Fragment>",
                "    <Test />",
                "  </w:Fragment>",
                "</w:Wix>");

            var expected = new[]
            {
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <w:Fragment>",
                "    <Test />",
                "  </w:Fragment>",
                "</w:Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(3, errors);
            Assert.Equal(Wix4Namespace, document.Root.GetNamespaceOfPrefix("w"));
            Assert.Equal("http://wixtoolset.org/schemas/v4/wxs/util", document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanRemoveUnusedNamespaces()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(4, errors);
            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertMissingWixNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix>",
                "  <Fragment />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(2, errors);
            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertMissingIncludeNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Include>",
                "  <?define Version = 1.2.3 ?>",
                "  <Fragment>",
                "    <DirectoryRef Id='TARGETDIR'>",
                "      <Directory Id='ANOTHERDIR' Name='Another' />",
                "    </DirectoryRef>",
                "  </Fragment>",
                "</Include>");

            var expected = new[]
            {
                "<Include xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <?define Version = 1.2.3 ?>",
                "  <Fragment>",
                "    <DirectoryRef Id=\"TARGETDIR\">",
                "      <Directory Id=\"ANOTHERDIR\" Name=\"Another\" />",
                "    </DirectoryRef>",
                "  </Fragment>",
                "</Include>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(2, errors);
            WixAssert.CompareLineByLine(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertAnonymousFile()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <File Source='path\\to\\foo.txt' />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <File Id=\"foo.txt\" Source=\"path\\to\\foo.txt\" />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(3, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanConvertShortNameDirectoryWithoutName()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Directory ShortName='iamshort' />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Directory Name=\"iamshort\" />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(2, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanConvertCatalogElement()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Catalog Id='idCatalog' SourceFile='path\\to\\catalog.cat' />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  ",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(1, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationNo()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <MsiPackage SuppressSignatureValidation='no' />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <MsiPackage />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(1, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationYes()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Payload SuppressSignatureValidation='yes' />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Payload />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(1, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CantConvertVerbTarget()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Verb Target='anything' />",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Verb Target=\"anything\" />",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(2, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CanConvertDeprecatedPrefix()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "<Fragment>",
                "<ComponentGroup Id=\"$(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$$(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$$$(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$$$$(loc.Variable)\" />",
                "</Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "<Fragment>",
                "<ComponentGroup Id=\"!(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$!(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$$$(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$$$!(loc.Variable)\" />",
                "</Fragment>",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(3, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }

        [Fact]
        public void CantConvertStandardCustomActionRescheduling()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <InstallExecuteSequence>",
                "     <Custom Action='WixCloseApplications' Before='StopServices' />",
                "  </InstallExecuteSequence>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <InstallExecuteSequence>",
                "     <Custom Action=\"WixCloseApplications\" Before=\"StopServices\" />",
                "  </InstallExecuteSequence>",
                "</Wix>",
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentLines(document);

            Assert.Equal(2, errors);
            WixAssert.CompareLineByLine(expected, actual);
        }
    }
}
