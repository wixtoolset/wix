// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
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

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EnsuresDeclarationWhenIgnored()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-16'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, ignoreErrors: new[] { "DeclarationPresent" } );

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document, omitXmlDeclaration: false);

            Assert.Equal(0, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertMainNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            //Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertNamedMainNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<w:Wix xmlns:w='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <w:Fragment />",
                "</w:Wix>");

            var expected = String.Join(Environment.NewLine,
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <w:Fragment />",
                "</w:Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
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

            var expected = String.Join(Environment.NewLine,
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <w:Fragment>",
                "    <Test />",
                "  </w:Fragment>",
                "</w:Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
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

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(4, errors);
            Assert.Equal(expected, actual);
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

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
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

            var expected = String.Join(Environment.NewLine,
                "<Include xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <?define Version = 1.2.3 ?>",
                "  <Fragment>",
                "    <DirectoryRef Id=\"TARGETDIR\">",
                "      <Directory Id=\"ANOTHERDIR\" Name=\"Another\" />",
                "    </DirectoryRef>",
                "  </Fragment>",
                "</Include>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
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

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <File Id=\"foo.txt\" Source=\"path\\to\\foo.txt\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(3, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertShortNameDirectoryWithoutName()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Directory ShortName='iamshort' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Directory Name=\"iamshort\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertCatalogElement()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Catalog Id='idCatalog' SourceFile='path\\to\\catalog.cat' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  ",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationNo()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <MsiPackage SuppressSignatureValidation='no' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <MsiPackage />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationYes()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Payload SuppressSignatureValidation='yes' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Payload />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CantConvertVerbTarget()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Verb Target='anything' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Verb Target=\"anything\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
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

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "<Fragment>",
                "<ComponentGroup Id=\"!(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$!(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$$$(loc.Variable)\" />",
                "<ComponentGroup Id=\"$$$$!(loc.Variable)\" />",
                "</Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(3, errors);
            Assert.Equal(expected, actual);
        }
    }
}
