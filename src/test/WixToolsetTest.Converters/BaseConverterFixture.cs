// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolsetTest.Converters
{
    using System;
    using System.IO;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    using Xunit;

    public abstract class BaseConverterFixture
    {
        protected static string UnformattedDocumentString(XDocument document)
        {
            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            using (var xml = XmlWriter.Create(writer, new XmlWriterSettings { OmitXmlDeclaration = true }))
            {
                document.Save(xml);
            }

            return sb.ToString().TrimStart();
        }

        protected static string[] UnformattedDocumentLines(XDocument document)
        {
            var unformatted = UnformattedDocumentString(document);
            return unformatted.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        protected static void CompareLineByLine(string[] expectedLines, string[] actualLines)
        {
            for (var i = 0; i < expectedLines.Length; ++i)
            {
                Assert.True(actualLines.Length > i, $"{i}: Expected file longer than actual file");
                Assert.Equal($"{i}: {expectedLines[i]}", $"{i}: {actualLines[i]}");
            }
            Assert.True(expectedLines.Length == actualLines.Length, "Actual file longer than expected file");
        }
    }
}
