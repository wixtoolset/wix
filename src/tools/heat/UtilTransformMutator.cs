// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Harvesters
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Xml.Xsl;
    using WixToolset.Harvesters.Data;
    using WixToolset.Harvesters.Extensibility;

    public sealed class UtilTransformMutator : BaseMutatorExtension
    {
        private string transform;
        private int transformSequence;

        /// <summary>
        /// Instantiate a new UtilTransformMutator.
        /// </summary>
        /// <param name="transform">Path to the XSL transform file.</param>
        /// <param name="transformSequence">Order in which the transform should be applied,
        /// relative to other transforms.</param>
        public UtilTransformMutator(string transform, int transformSequence)
        {
            this.transform = transform;
            this.transformSequence = transformSequence;
        }

        /// <summary>
        /// Gets the sequence of the extension.
        /// </summary>
        /// <value>The sequence of the extension.</value>
        public override int Sequence
        {
            get { return 3000 + this.transformSequence; }
        }

        /// <summary>
        /// Mutate a WiX document as a string.
        /// </summary>
        /// <param name="wixString">The Wix document element as a string.</param>
        /// <returns>The mutated Wix document as a string.</returns>
        public override string Mutate(string wixString)
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(this.transform, XsltSettings.TrustedXslt, new XmlUrlResolver());

                using (XmlTextReader xmlReader = new XmlTextReader(new StringReader(wixString)))
                {
                    using (StringWriter stringWriter = new StringWriter())
                    {
                        XmlWriterSettings xmlSettings = new XmlWriterSettings();
                        xmlSettings.Indent = true;
                        xmlSettings.IndentChars = "    ";
                        xmlSettings.OmitXmlDeclaration = true;

                        using (XmlWriter xmlWriter = XmlWriter.Create(stringWriter, xmlSettings))
                        {
                            xslt.Transform(xmlReader, xmlWriter);
                        }

                        wixString = stringWriter.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                this.Core.Messaging.Write(HarvesterErrors.ErrorTransformingHarvestedWiX(this.transform, ex.Message));
                return null;
            }

            return wixString;
        }
    }
}
