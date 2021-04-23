// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.CoreIntegration
{
    using System.Collections.Generic;
    using WixToolset.Core.TestPackage;
    using Xunit;

    public class TestXmlFixture
    {
        [Fact]
        public void ChangesIgnoredAttributesToStarToHelpMakeTestsLessFragile()
        {
            var original = @"<Top One='f'>
  <First Two='t'>
    <Target One='a' Two='b' Three='c' />
  </First>
  <Target One='z' Two='x' Three = 'y' />
</Top>";
            var expected = "<Top One='f'><First Two='t'><Target One='*' Two='*' Three='c' /></First><Target One='*' Two='*' Three='y' /></Top>";
            var ignored = new Dictionary<string, List<string>> { { "Target", new List<string> { "One", "Two", "Missing" } } };
            Assert.Equal(expected, original.GetTestXml(ignored));
        }

        [Fact]
        public void OutputsSingleQuotesSinceDoubleQuotesInCsharpLiteralStringsArePainful()
        {
            var original = "<Test Simple=\"\" EscapedDoubleQuote=\"&quot;\" SingleQuoteValue=\"'test'\" Alternating='\"' AlternatingEscaped='&quot;' />";
            var expected = "<Test Simple='' EscapedDoubleQuote='\"' SingleQuoteValue='&apos;test&apos;' Alternating='\"' AlternatingEscaped='\"' />";
            Assert.Equal(expected, original.GetTestXml());
        }

        [Fact]
        public void RemovesAllNamespacesToReduceTyping()
        {
            var original = "<Test xmlns='a'><Child xmlns:b='b'><Grandchild xmlns:c='c' /><Grandchild /></Child></Test>";
            var expected = "<Test><Child><Grandchild /><Grandchild /></Child></Test>";
            Assert.Equal(expected, original.GetTestXml());
        }

        [Fact]
        public void RemovesUnnecessaryWhitespaceToAvoidLineEndingIssues()
        {
            var original = @"<Test>
  <Child>
    <Grandchild />
    <Grandchild />
  </Child>
</Test>";
            var expected = "<Test><Child><Grandchild /><Grandchild /></Child></Test>";
            Assert.Equal(expected, original.GetTestXml());
        }

        [Fact]
        public void RemovesXmlDeclarationToReduceTyping()
        {
            var original = "<?xml version='1.0'?><Test />";
            var expected = "<Test />";
            Assert.Equal(expected, original.GetTestXml());
        }
    }
}
