// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.IO;
    using System.Xml.Linq;
    using WixToolset.Converters;
    using WixToolsetTest.Converters.Mocks;
    using Xunit;

    public class CustomActionFixture : BaseConverterFixture
    {
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
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomAction Id=\"Foo\" DllEntry=\"WixQuietExec\" BinaryRef=\"Wix4UtilCA_X86\" />",
                "  <CustomAction Id=\"Foo\" DllEntry=\"WixQuietExec64\" BinaryRef=\"Wix4UtilCA_X64\" />",
                "  <CustomAction Id=\"Foo\" DllEntry=\"WixQuietExec\" BinaryRef=\"Wix4UtilCA_X86\" />",
                "  <CustomAction Id=\"Foo\" DllEntry=\"WixQuietExec64\" BinaryRef=\"Wix4UtilCA_X64\" />",
                "</Wix>");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(11, errors);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void CanConvertCustomActionScript()
        {
            var parse = String.Join(Environment.NewLine,
                "<?xml version='1.0' encoding='utf-8'?>",
                "<Wix xmlns='http://wixtoolset.org/schemas/v4/wxs'>",
                "  <CustomAction Id='Foo' Script='jscript'>",
                "  function() {",
                "    var x = 0;",
                "    return x;",
                "  }",
                "  </CustomAction>",
                "</Wix>");

            var expected = String.Join(Environment.NewLine,
                "<Wix xmlns=\"http://wixtoolset.org/schemas/v4/wxs\">",
                "  <CustomAction Id=\"Foo\" Script=\"jscript\" ScriptSourceFile=\"Foo.js\" />",
                "</Wix>");

            var expectedScript = String.Join("\n",
                "function() {",
                "    var x = 0;",
                "    return x;",
                "  }");

            var document = XDocument.Parse(parse, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);

            var messaging = new MockMessaging();
            var converter = new WixConverter(messaging, 2, null, null);

            var errors = converter.ConvertDocument(document);

            var actual = UnformattedDocumentString(document);

            Assert.Equal(2, errors);
            Assert.Equal(expected, actual);

            var script = File.ReadAllText("Foo.js");
            Assert.Equal(expectedScript, script);
        }
    }
}
