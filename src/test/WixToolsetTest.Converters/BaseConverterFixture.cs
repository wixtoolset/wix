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
        protected static string UnformattedDocumentString(XDocument document, bool omitXmlDeclaration = true)
        {
            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            using (var xml = XmlWriter.Create(writer, new XmlWriterSettings { OmitXmlDeclaration = omitXmlDeclaration }))
            {
                document.Save(xml);
            }

            return sb.ToString().TrimStart();
        }

        protected static string[] UnformattedDocumentLines(XDocument document, bool omitXmlDeclaration = true)
        {
            var unformatted = UnformattedDocumentString(document, omitXmlDeclaration);
            return unformatted.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
