// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Core
{
    using System;
    using System.Xml.Linq;
    using WixBuildTools.TestSupport;
    using WixToolset.Core;
    using WixToolset.Data;
    using WixToolset.Extensibility.Services;
    using Xunit;

    public class ParserHelperFixture
    {
        [Fact]
        public void CanParseFourPartAttributeVersion()
        {
            var helper = GetParserHelper();

            var attribute = CreateAttribute("1.2.3.4");
            var result = helper.GetAttributeVersionValue(null, attribute);

            WixAssert.StringEqual("1.2.3.4", result);
        }

        [Fact]
        public void CannotParseFivePartAttributeVersion()
        {
            var helper = GetParserHelper();

            var attribute = CreateAttribute("1.2.3.4.5");
            var exception = Assert.Throws<WixException>(() => { helper.GetAttributeVersionValue(null, attribute); });
            WixAssert.StringEqual("The Test/@Value attribute's value, '1.2.3.4.5', is not a valid version. Specify a four-part version or semantic version, such as '#.#.#.#' or '#.#.#-label.#'.", exception.Message);
        }

        [Fact]
        public void CannotParseVersionTooLargeAttributeVersion()
        {
            var version = "4294967296.2.3.4";
            AssertVersion(version);
        }

        private static void AssertVersion(string version)
        {
            var helper = GetParserHelper();
            var attribute = CreateAttribute(version);
            var exception = Assert.Throws<WixException>(() => { helper.GetAttributeVersionValue(null, attribute); });
            WixAssert.StringEqual($"The Test/@Value attribute's value, '{version}', is not a valid version. Specify a four-part version or semantic version, such as '#.#.#.#' or '#.#.#-label.#'.", exception.Message);
        }

        [Fact]
        public void CanParseSemverAttributeVersion()
        {
            var helper = GetParserHelper();

            var attribute = CreateAttribute("10.99.444-preview.0");
            var result = helper.GetAttributeVersionValue(null, attribute);

            WixAssert.StringEqual("10.99.444-preview.0", result);
        }

        [Fact]
        public void CanParseFourPartSemverAttributeVersion()
        {
            var helper = GetParserHelper();

            var attribute = CreateAttribute("1.2.3.4-meta.123-other.456");
            var result = helper.GetAttributeVersionValue(null, attribute);

            WixAssert.StringEqual("1.2.3.4-meta.123-other.456", result);
        }

        [Fact]
        public void CanParseVersionWithLeadingV()
        {
            var helper = GetParserHelper();

            var attribute = CreateAttribute("v1.2.3.4");
            var result = helper.GetAttributeVersionValue(null, attribute);

            WixAssert.StringEqual("v1.2.3.4", result);
        }


        private static IParseHelper GetParserHelper()
        {
            var sp = WixToolsetServiceProviderFactory.CreateServiceProvider();
            var helper = sp.GetService<IParseHelper>();
            return helper;
        }

        private static XAttribute CreateAttribute(string value)
        {
            var attribute = new XAttribute("Value", value);
            _ = new XElement("Test", attribute);

            return attribute;
        }
    }
}
