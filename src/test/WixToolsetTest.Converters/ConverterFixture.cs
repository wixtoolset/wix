// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Converters;
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
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void EnsuresUtf8Declaration()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "    <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 4, null, null);

            var errors = converter.ConvertDocument(document);

            Assert.Equal(1, errors);
            Assert.Equal("1.0", document.Declaration.Version);
            Assert.Equal("utf-8", document.Declaration.Encoding);
        }

        [Fact]
        public void CanFixWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'",
                "              Value='Val'>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "    <Fragment>",
                "        <Property Id=\"Prop\" Value=\"Val\" />",
                "    </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 4, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(4, errors);
        }

        [Fact]
        public void CanPreserveNewLines()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "",
                "    <Property Id='Prop' Value='Val' />",
                "",
                "  </Fragment>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "    <Fragment>",
                "",
                "        <Property Id=\"Prop\" Value=\"Val\" />",
                "",
                "    </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 4, null, null);

            var conversions = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(3, conversions);
        }

        [Fact]
        public void CanConvertWithNewLineAtEndOfFile()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "",
                "    <Property Id='Prop' Value='Val' />",
                "",
                "  </Fragment>",
                "</Wix>",
                "");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "    <Fragment>",
                "",
                "        <Property Id=\"Prop\" Value=\"Val\" />",
                "",
                "    </Fragment>",
                "</Wix>",
                "");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 4, null, null);

            var conversions = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(3, conversions);
        }

        [Fact]
        public void CanFixCdataWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'>",
                "       <![CDATA[1<2]]>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\"><![CDATA[1<2]]></Property>",
                "  </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(2, errors);
        }

        [Fact]
        public void CanFixCdataWithWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'>",
                "       <![CDATA[",
                "           1<2",
                "       ]]>",
                "    </Property>",
                "  </Fragment>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Property Id=\"Prop\"><![CDATA[1<2]]></Property>",
                "  </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(2, errors);
        }

        [Fact]
        public void CanKeepCdataWithOnlyWhitespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <Property Id='Prop'><![CDATA[ ]]></Property>",
                "  </Fragment>",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);
            var errors = converter.ConvertDocument(document);
            Assert.Equal(0, errors);
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
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
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
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <w:Fragment />",
                "</w:Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
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
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<w:Wix xmlns:w=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <w:Fragment>",
                "    <Test />",
                "  </w:Fragment>",
                "</w:Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(expected, actual);
            Assert.Equal(2, errors);
            Assert.Equal(Wix4Namespace, document.Root.GetNamespaceOfPrefix("w"));
            Assert.Equal("http://wixtoolset.org/schemas/v4/wxs/util", document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertExtensionNamespace()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:util='http://schemas.microsoft.com/wix/UtilExtension'>",
                "  <Fragment />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:util=\"http://wixtoolset.org/schemas/v4/wxs/util\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
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
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
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
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Include xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <?define Version = 1.2.3 ?>",
                "  <Fragment>",
                "    <DirectoryRef Id=\"TARGETDIR\">",
                "      <Directory Id=\"ANOTHERDIR\" Name=\"Another\" />",
                "    </DirectoryRef>",
                "  </Fragment>",
                "</Include>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
            Assert.Equal(Wix4Namespace, document.Root.GetDefaultNamespace());
        }

        [Fact]
        public void CanConvertAnonymousFile()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <File Source='path\\to\\foo.txt' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <File Id=\"foo.txt\" Source=\"path\\to\\foo.txt\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertCustomTableBootstrapperApplicationData()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomTable Id='FgAppx' BootstrapperApplicationData='yes' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomTable Id=\"FgAppx\" Unreal=\"yes\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
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
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Directory Name=\"iamshort\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationNo()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <MsiPackage SuppressSignatureValidation='no' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <MsiPackage EnableSignatureValidation=\"yes\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertSuppressSignatureValidationYes()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Payload SuppressSignatureValidation='yes' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Payload />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(1, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertCustomAction()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomAction Id='Foo' BinaryKey='WixCA' DllEntry='CAQuietExec' />",
                "  <CustomAction Id='Foo' BinaryKey='WixCA_x64' DllEntry='CAQuietExec64' />",
                "  <CustomAction Id='Foo' BinaryKey='UtilCA' DllEntry='WixQuietExec' />",
                "  <CustomAction Id='Foo' BinaryKey='UtilCA_x64' DllEntry='WixQuietExec64' />",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomAction Id=\"Foo\" BinaryKey=\"Wix4UtilCA_X86\" DllEntry=\"WixQuietExec\" />",
                "  <CustomAction Id=\"Foo\" BinaryKey=\"Wix4UtilCA_X64\" DllEntry=\"WixQuietExec64\" />",
                "  <CustomAction Id=\"Foo\" BinaryKey=\"Wix4UtilCA_X86\" DllEntry=\"WixQuietExec\" />",
                "  <CustomAction Id=\"Foo\" BinaryKey=\"Wix4UtilCA_X64\" DllEntry=\"WixQuietExec64\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new DummyMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(6, errors);
            Assert.Equal(expected, actual);
        }

        private static string UnformattedDocumentString(XDocument document)
        {
            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            {
                document.Save(writer, SaveOptions.DisableFormatting);
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

            public string FormatMessage(Message message) => String.Empty;

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
