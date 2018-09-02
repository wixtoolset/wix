// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.WixUnitTest
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using WixCop;
    using WixToolset;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Services;
    using Xunit;

    public class ConverterFixture
    {
        private static readonly XNamespace Wix4Namespace = "http://wixtoolset.org/schemas/v4/wxs";

        [Fact]
        public void EnsuresDeclaration()
        {
            string parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment />",
                "</Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EnsuresUtf8Declaration()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "    <Fragment />",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 4, null, null);

            int errors = converter.ConvertDocument(document);

            Assert.Equal(1, errors);
            Assert.Equal("1.0", document.Declaration.Version);
            Assert.Equal("utf-8", document.Declaration.Encoding);
        }

        [Fact]
        public void CanFixWhitespace()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'",
                "              Value='Val'>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "    <Fragment>",
                "        <Property Id=\"Prop\" Value=\"Val\" />",
                "    </Fragment>",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 4, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(4, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanFixCdataWhitespace()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'>",
                "       <![CDATA[1<2]]>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\"><![CDATA[1<2]]></Property>",
                "  </Fragment>",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertMainNamespace()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment />",
                "</Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            //Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertNamedMainNamespace()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<w:Wix xmlns:w='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <w:Fragment />",
                "</w:Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <w:Fragment />",
                "</w:Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetNamespaceOfPrefix("w"));
        }

        [Fact]
        public void CanConvertNonWixDefaultNamespace()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<w:Wix xmlns:w='http://schemas.microsoft.com/wix/2006/wi' xmlns='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <w:Fragment>",
                "    <Test />",
                "  </w:Fragment>",
                "</w:Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <w:Fragment>",
                "    <Test />",
                "  </w:Fragment>",
                "</w:Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetNamespaceOfPrefix("w"));
            Assert.Equal("http://wixtoolset.org/schemas/v4/wxs/util", document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertExtensionNamespace()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment />",
                "</Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:util=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <Fragment />",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertMissingNamespace()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix>",
                "  <Fragment />",
                "</Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertAnonymousFile()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <File Source='path\\to\\foo.txt' />",
                "</Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <File Id=\"foo.txt\" Source=\"path\\to\\foo.txt\" />",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationNo()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <MsiPackage SuppressSignatureValidation='no' />",
                "</Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <MsiPackage EnableSignatureValidation=\"yes\" />",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationYes()
        {
            string parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Payload SuppressSignatureValidation='yes' />",
                "</Wix>");

            string expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Payload />",
                "</Wix>");

            XDocument document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            Converter converter = new Converter(messaging, 2, null, null);

            int errors = converter.ConvertDocument(document);

            string actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        private static string UnformattedDocumentString(XDocument document)
        {
            StringBuilder sb = new StringBuilder();

            using (StringWriter writer = new StringWriter(sb))
            {
                document.Save(writer, SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces);
            }

            return sb.ToString();
        }

        private class DummyMessaging : IMessaging
        {
            public bool EncounteredError { get; set; }

            public int LastErrorNumber { get; set; }

            public bool ShowVerboseMessages { get; set; }
            public bool SuppressAllWarnings { get; set; }
            public bool WarningsAsError { get; set; }

            public void ElevateWarningMessage(int warningNumber)
            {
            }

            public string FormatMessage(Message message)
            {
                return "";
            }

            public void SetListener(IMessageListener listener)
            {
            }

            public void SuppressWarningMessage(int warningNumber)
            {
            }

            public void Write(Message message)
            {
            }

            public void Write(string message, bool verbose = false)
            {
            }
        }
    }
}
