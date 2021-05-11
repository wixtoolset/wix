// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class BootstrapperApplicationFixture : BaseConverterFixture
    {
        [Fact]
        public void CantCreateBootstrapperApplicationDllFromV3PayloadGroupRef()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <BootstrapperApplication Id='ba'>",
                "      <PayloadGroupRef Id='baPayloads' />",
                "    </BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <BootstrapperApplication Id=\"ba\">",
                "      <PayloadGroupRef Id=\"baPayloads\" />",
                "    </BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(2, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void ConvertDotNetCoreBootstrapperApplicationRefWithExistingElement()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs' xmlns:bal='http://wixtoolset.org/schemas/v4/wxs/bal'>",
                "  <Fragment>",
                "    <BootstrapperApplicationRef Id='DotNetCoreBootstrapperApplicationHost.Minimal'>",
                "      <bal:WixDotNetCoreBootstrapperApplication SelfContainedDeployment='yes' />",
                "    </BootstrapperApplicationRef>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:bal=\"http://wixtoolset.org/schemas/v4/wxs/bal\">",
                "  <Fragment>",
                "    <BootstrapperApplication>",
                "      <bal:WixDotNetCoreBootstrapperApplicationHost SelfContainedDeployment=\"yes\" Theme=\"none\" />",
                "    </BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(1, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void ConvertDotNetCoreBootstrapperApplicationRefWithoutExistingElement()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs' xmlns:bal='http://wixtoolset.org/schemas/v4/wxs/bal'>",
                "  <Fragment>",
                "    <BootstrapperApplicationRef Id='DotNetCoreBootstrapperApplicationHost' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:bal=\"http://wixtoolset.org/schemas/v4/wxs/bal\">",
                "  <Fragment>",
                "    <BootstrapperApplication>",
                "<bal:WixDotNetCoreBootstrapperApplicationHost />",
                "</BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(1, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void ConvertFrameworkBootstrapperApplicationRefWithExistingElement()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:bal='http://schemas.microsoft.com/wix/BalExtension'>",
                "  <Fragment>",
                "    <BootstrapperApplicationRef Id='ManagedBootstrapperApplicationHost'>",
                "      <bal:WixManagedBootstrapperApplicationHost LogoFile='logo.png' />",
                "    </BootstrapperApplicationRef>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:bal=\"http://wixtoolset.org/schemas/v4/wxs/bal\">",
                "  <Fragment>",
                "    <BootstrapperApplication>",
                "      <bal:WixManagedBootstrapperApplicationHost LogoFile=\"logo.png\" />",
                "    </BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void ConvertFrameworkBootstrapperApplicationRefWithoutExistingElement()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:bal='http://schemas.microsoft.com/wix/BalExtension'>",
                "  <Fragment>",
                "    <BootstrapperApplicationRef Id='ManagedBootstrapperApplicationHost.RtfLicense.Minimal' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:bal=\"http://wixtoolset.org/schemas/v4/wxs/bal\">",
                "  <Fragment>",
                "    <BootstrapperApplication>",
                "<bal:WixManagedBootstrapperApplicationHost Theme=\"none\" />",
                "</BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void ConvertStandardBootstrapperApplicationRefWithExistingElement()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:bal='http://schemas.microsoft.com/wix/BalExtension'>",
                "  <Fragment>",
                "    <BootstrapperApplicationRef Id='WixStandardBootstrapperApplication.Foundation'>",
                "      <bal:WixStandardBootstrapperApplication LaunchTarget='[InstallFolder]the.exe' />",
                "    </BootstrapperApplicationRef>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:bal=\"http://wixtoolset.org/schemas/v4/wxs/bal\">",
                "  <Fragment>",
                "    <BootstrapperApplication>",
                "      <bal:WixStandardBootstrapperApplication LaunchTarget=\"[InstallFolder]the.exe\" Theme=\"none\" />",
                "    </BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void ConvertStandardBootstrapperApplicationRefWithoutExistingElement()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:bal='http://schemas.microsoft.com/wix/BalExtension'>",
                "  <Fragment>",
                "    <BootstrapperApplicationRef Id='WixStandardBootstrapperApplication.RtfLicense' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:bal=\"http://wixtoolset.org/schemas/v4/wxs/bal\">",
                "  <Fragment>",
                "    <BootstrapperApplication>",
                "<bal:WixStandardBootstrapperApplication Theme=\"rtfLicense\" />",
                "</BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void CreateBootstrapperApplicationDllFromV3()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <BootstrapperApplication Id='ba' SourceFile='ba.dll' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <BootstrapperApplication Id=\"ba\">",
                "<BootstrapperApplicationDll SourceFile=\"ba.dll\" DpiAwareness=\"unaware\" />",
                "</BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void CreateBootstrapperApplicationDllFromV3Payload()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <BootstrapperApplication Id='ba'>",
                "      <Payload SourceFile='ba.dll' />",
                "    </BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <BootstrapperApplication Id=\"ba\">",
                "      ",
                "    ",
                "<BootstrapperApplicationDll SourceFile=\"ba.dll\" DpiAwareness=\"unaware\" />",
                "</BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void DoesntSetDpiUnawareFromV4()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <BootstrapperApplication Id='ba' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <BootstrapperApplication Id=\"ba\" />",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(0, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void KeepsDpiAwarenessFromV4()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <BootstrapperApplication Id='ba' SourceFile='ba.dll' DpiAwareness='system' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <BootstrapperApplication Id=\"ba\">",
                "<BootstrapperApplicationDll SourceFile=\"ba.dll\" DpiAwareness=\"system\" />",
                "</BootstrapperApplication>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(1, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void RemovesBalUseUILanguages()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:bal='http://schemas.microsoft.com/wix/BalExtension'>",
                "  <Fragment>",
                "    <BootstrapperApplication Id='ba' bal:UseUILanguages='true' />",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <BootstrapperApplication Id=\"ba\" />",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(5, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }
    }
}
