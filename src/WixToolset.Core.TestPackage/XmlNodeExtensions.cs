// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.TestPackage
{
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Xml;

    public static class XmlNodeExtensions
    {
        public static string GetTestXml(this XmlNode node, Dictionary<string, List<string>> ignoredAttributesByElementName = null)
        {
            return node.OuterXml.GetTestXml(ignoredAttributesByElementName);
        }

        public static string GetTestXml(this string xml, Dictionary<string, List<string>> ignoredAttributesByElementName = null)
        {
            string formattedXml;
            using (var sw = new StringWriter())
            using (var writer = new TestXmlWriter(sw))
            {
                var doc = new XmlDocument();
                doc.LoadXml(xml);

                if (ignoredAttributesByElementName != null)
                {
                    HandleIgnoredAttributes(doc, ignoredAttributesByElementName);
                }

                doc.Save(writer);
                formattedXml = sw.ToString();
            }

            return Regex.Replace(formattedXml, " xmlns(:[^=]+)?='[^']*'", "");
        }

        private static void HandleIgnoredAttributes(XmlNode node, Dictionary<string, List<string>> ignoredAttributesByElementName)
        {
            if (node.Attributes != null && ignoredAttributesByElementName.TryGetValue(node.LocalName, out var ignoredAttributes))
            {
                foreach (var ignoredAttribute in ignoredAttributes)
                {
                    var attribute = node.Attributes[ignoredAttribute];
                    if (attribute != null)
                    {
                        attribute.Value = "*";
                    }
                }
            }

            if (node.ChildNodes != null)
            {
                foreach (XmlNode childNode in node.ChildNodes)
                {
                    HandleIgnoredAttributes(childNode, ignoredAttributesByElementName);
                }
            }
        }

        private class TestXmlWriter : XmlTextWriter
        {
            public TestXmlWriter(TextWriter w)
                : base(w)
            {
                this.QuoteChar = '\'';
            }

            public override void WriteStartDocument()
            {
                //OmitXmlDeclaration
            }
        }
    }
}
