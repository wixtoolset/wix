// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class RemotePayloadFixture : BaseConverterFixture
    {
        [Fact]
        public void CanConvertExePackageRemotePayload()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <PackageGroup Id='exe'>",
                "      <ExePackage Name='example.exe' DownloadUrl='example.com'>",
                "        <RemotePayload",
                "          Description='Microsoft ASP.NET Core 3.1.8 - Shared Framework'",
                "          Hash='61DC9EAA0C8968E48E13C5913ED202A2F8F94DBA'",
                "          CertificatePublicKey='3756E9BBF4461DCD0AA68E0D1FCFFA9CEA47AC18'",
                "          CertificateThumbprint='2485A7AFA98E178CB8F30C9838346B514AEA4769'",
                "          ProductName='Microsoft ASP.NET Core 3.1.8 - Shared Framework'",
                "          Size='7841880'",
                "          Version='3.1.8.20421' />",
                "      </ExePackage>",
                "    </PackageGroup>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <PackageGroup Id=\"exe\">",
                "      <ExePackage>",
                "        <ExePackagePayload Description=\"Microsoft ASP.NET Core 3.1.8 - Shared Framework\" Hash=\"61DC9EAA0C8968E48E13C5913ED202A2F8F94DBA\" ProductName=\"Microsoft ASP.NET Core 3.1.8 - Shared Framework\" Size=\"7841880\" Version=\"3.1.8.20421\" Name=\"example.exe\" DownloadUrl=\"example.com\" />",
                "      </ExePackage>",
                "    </PackageGroup>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(6, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void CanConvertMsuPackageRemotePayload()
        {
            var parse = String.Join(Environment.NewLine,
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <Fragment>",
                "    <PackageGroup Id='msu'>",
                "      <MsuPackage Name='example.msu' DownloadUrl='example.com' Compressed='no'>",
                "        <RemotePayload",
                "          Description='msu description'",
                "          Hash='71DC9EAA0C8968E48E13C5913ED202A2F8F94DBB'",
                "          ProductName='msu product name'",
                "          Size='500'",
                "          Version='0.0.0.0' />",
                "      </MsuPackage>",
                "    </PackageGroup>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <PackageGroup Id=\"msu\">",
                "      <MsuPackage>",
                "        <MsuPackagePayload Description=\"msu description\" Hash=\"71DC9EAA0C8968E48E13C5913ED202A2F8F94DBB\" ProductName=\"msu product name\" Size=\"500\" Version=\"0.0.0.0\" Name=\"example.msu\" DownloadUrl=\"example.com\" />",
                "      </MsuPackage>",
                "    </PackageGroup>",
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
