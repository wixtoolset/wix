// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.Xml.Linq;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class ConditionFixture : BaseConverterFixture
    {
        [Fact]
        public void FixControlCondition()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <UI>",
                "      <Dialog Id='Dlg1'>",
                "        <Control Id='Control1'>",
                "          <Condition Action='hide'>a&lt;>b</Condition>",
                "        </Control>",
                "      </Dialog>",
                "    </UI>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <UI>",
                "      <Dialog Id=\"Dlg1\">",
                "        <Control Id=\"Control1\" HideCondition=\"a&lt;&gt;b\">",
                "          ",
                "        </Control>",
                "      </Dialog>",
                "    </UI>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixComponentCondition()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Component Id='Comp1' Directory='ApplicationFolder'>",
                "      <Condition>1&lt;2</Condition>",
                "    </Component>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Component Id=\"Comp1\" Directory=\"ApplicationFolder\" Condition=\"1&lt;2\">",
                "      ",
                "    </Component>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixFeatureCondition()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Feature Id='Feature1'>",
                "      <Condition Level='0'>PROP = 1</Condition>",
                "    </Feature>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Feature Id=\"Feature1\">",
                "      <Level Value=\"0\" Condition=\"PROP = 1\" />",
                "    </Feature>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixLaunchCondition()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Condition Message='Stop the install'>",
                "      1&lt;2",
                "    </Condition>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Launch Condition=\"1&lt;2\" Message=\"Stop the install\" />",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(3, errors);

            var actualLines = UnformattedDocumentLines(document);
            CompareLineByLine(expected, actualLines);
        }

        [Fact]
        public void FixPermissionExCondition()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns='http://schemas.microsoft.com/wix/2006/wi'>",
                "  <Fragment>",
                "    <Component Id='Comp1' Guid='*' Directory='ApplicationFolder'>",
                "      <PermissionEx Sddl='sddl'>",
                "        <Condition>1&lt;2</Condition>",
                "      </PermissionEx>",
                "    </Component>",
                "  </Fragment>",
                "</Wix>");

            var expected = new[]
            {
                "<?xml version=\"1.0\" encoding=\"utf-16\"?>",
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <Fragment>",
                "    <Component Id=\"Comp1\" Directory=\"ApplicationFolder\">",
                "      <PermissionEx Sddl=\"sddl\" Condition=\"1&lt;2\">",
                "        ",
                "      </PermissionEx>",
                "    </Component>",
                "  </Fragment>",
                "</Wix>"
            };

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new Wix3Converter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);
            Assert.Equal(4, errors);

            var actualLines = UnformattedDocumentLines(document);
            CompareLineByLine(expected, actualLines);
        }
    }
}
