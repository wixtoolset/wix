// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class DependencyFixture : BaseConverterFixture
    {
        [Fact]
        public void FixPackageDependencyProvides()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:dep='http://schemas.microsoft.com/wix/DependencyExtension'>",
                "  <Fragment>",
                "    <ComponentGroup Id='Group1' Directory='INSTALLFOLDER'>",
                "      <Component>",
                "        <dep:Provides Key='abc' />",
                "      </Component>",
                "    </ComponentGroup>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:dep=\"http://wixtoolset.org/schemas/v4/wxs/dependency\">",
                "  <Fragment>",
                "    <ComponentGroup Id=\"Group1\" Directory=\"INSTALLFOLDER\">",
                "      <Component>",
                "        <Provides Key=\"abc\" dep:Check=\"yes\" />",
                "      </Component>",
                "    </ComponentGroup>",
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

        [Fact]
        public void FixPackageDependencyRequires()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:dep='http://schemas.microsoft.com/wix/DependencyExtension'>",
                "  <Fragment>",
                "    <ComponentGroup Id='Group1' Directory='INSTALLFOLDER'>",
                "      <Component>",
                "        <dep:Provides Key='abc'>",
                "          <dep:Requires Key='xyz' />",
                "        </dep:Provides>",
                "      </Component>",
                "    </ComponentGroup>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:dep=\"http://wixtoolset.org/schemas/v4/wxs/dependency\">",
                "  <Fragment>",
                "    <ComponentGroup Id=\"Group1\" Directory=\"INSTALLFOLDER\">",
                "      <Component>",
                "        <Provides Key=\"abc\" dep:Check=\"yes\">",
                "          <Requires Key=\"xyz\" dep:Enforce=\"yes\" />",
                "        </Provides>",
                "      </Component>",
                "    </ComponentGroup>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(7, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixPackageDependencyRequiresRef()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:dep='http://schemas.microsoft.com/wix/DependencyExtension'>",
                "  <Fragment>",
                "    <ComponentGroup Id='Group1' Directory='INSTALLFOLDER'>",
                "      <Component>",
                "        <dep:Provides Key='abc'>",
                "          <dep:RequiresRef Id='OtherRequires' />",
                "        </dep:Provides>",
                "      </Component>",
                "    </ComponentGroup>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\" xmlns:dep=\"http://wixtoolset.org/schemas/v4/wxs/dependency\">",
                "  <Fragment>",
                "    <ComponentGroup Id=\"Group1\" Directory=\"INSTALLFOLDER\">",
                "      <Component>",
                "        <Provides Key=\"abc\" dep:Check=\"yes\">",
                "          <RequiresRef Id=\"OtherRequires\" dep:Enforce=\"yes\" />",
                "        </Provides>",
                "      </Component>",
                "    </ComponentGroup>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(7, errors);

            var actualLines = UnformattedDocumentLines(document);
            WixAssert.CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixBundleDependencyProvides()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi' xmlns:dep='http://schemas.microsoft.com/wix/DependencyExtension'>",
                "  <Fragment>",
                "    <MsiPackage Id='Package1'>",
                "      <dep:Provides Key='abc' />",
                "    </MsiPackage>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <MsiPackage Id=\"Package1\">",
                "      <Provides Key=\"abc\" />",
                "    </MsiPackage>",
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
