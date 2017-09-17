// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using WixToolset.Data;

    /// <summary>
    /// WiX source code converter.
    /// </summary>
    public class Converter
    {
        private const string XDocumentNewLine = "\n"; // XDocument normlizes "\r\n" to just "\n".
        private static readonly XNamespace WixNamespace = "http://wixtoolset.org/schemas/v4/wxs";

        private static readonly XName FileElementName = WixNamespace + "File";
        private static readonly XName ExePackageElementName = WixNamespace + "ExePackage";
        private static readonly XName MsiPackageElementName = WixNamespace + "MsiPackage";
        private static readonly XName MspPackageElementName = WixNamespace + "MspPackage";
        private static readonly XName MsuPackageElementName = WixNamespace + "MsuPackage";
        private static readonly XName PayloadElementName = WixNamespace + "Payload";
        private static readonly XName WixElementWithoutNamespaceName = XNamespace.None + "Wix";

        private static readonly Dictionary<string, XNamespace> OldToNewNamespaceMapping = new Dictionary<string, XNamespace>()
        {
            { "http://schemas.microsoft.com/wix/BalExtension", "http://wixtoolset.org/schemas/v4/wxs/bal" },
            { "http://schemas.microsoft.com/wix/ComPlusExtension", "http://wixtoolset.org/schemas/v4/wxs/complus" },
            { "http://schemas.microsoft.com/wix/DependencyExtension", "http://wixtoolset.org/schemas/v4/wxs/dependency" },
            { "http://schemas.microsoft.com/wix/DifxAppExtension", "http://wixtoolset.org/schemas/v4/wxs/difxapp" },
            { "http://schemas.microsoft.com/wix/FirewallExtension", "http://wixtoolset.org/schemas/v4/wxs/firewall" },
            { "http://schemas.microsoft.com/wix/GamingExtension", "http://wixtoolset.org/schemas/v4/wxs/gaming" },
            { "http://schemas.microsoft.com/wix/IIsExtension", "http://wixtoolset.org/schemas/v4/wxs/iis" },
            { "http://schemas.microsoft.com/wix/MsmqExtension", "http://wixtoolset.org/schemas/v4/wxs/msmq" },
            { "http://schemas.microsoft.com/wix/NetFxExtension", "http://wixtoolset.org/schemas/v4/wxs/netfx" },
            { "http://schemas.microsoft.com/wix/PSExtension", "http://wixtoolset.org/schemas/v4/wxs/powershell" },
            { "http://schemas.microsoft.com/wix/SqlExtension", "http://wixtoolset.org/schemas/v4/wxs/sql" },
            { "http://schemas.microsoft.com/wix/TagExtension", "http://wixtoolset.org/schemas/v4/wxs/tag" },
            { "http://schemas.microsoft.com/wix/UtilExtension", "http://wixtoolset.org/schemas/v4/wxs/util" },
            { "http://schemas.microsoft.com/wix/VSExtension", "http://wixtoolset.org/schemas/v4/wxs/vs" },
            { "http://wixtoolset.org/schemas/thmutil/2010", "http://wixtoolset.org/schemas/v4/thmutil" },
            { "http://schemas.microsoft.com/wix/2009/Lux", "http://wixtoolset.org/schemas/v4/lux" },
            { "http://schemas.microsoft.com/wix/2006/wi", "http://wixtoolset.org/schemas/v4/wxs" },
            { "http://schemas.microsoft.com/wix/2006/localization", "http://wixtoolset.org/schemas/v4/wxl" },
            { "http://schemas.microsoft.com/wix/2006/libraries", "http://wixtoolset.org/schemas/v4/wixlib" },
            { "http://schemas.microsoft.com/wix/2006/objects", "http://wixtoolset.org/schemas/v4/wixobj" },
            { "http://schemas.microsoft.com/wix/2006/outputs", "http://wixtoolset.org/schemas/v4/wixout" },
            { "http://schemas.microsoft.com/wix/2007/pdbs", "http://wixtoolset.org/schemas/v4/wixpdb" },
            { "http://schemas.microsoft.com/wix/2003/04/actions", "http://wixtoolset.org/schemas/v4/wi/actions" },
            { "http://schemas.microsoft.com/wix/2006/tables", "http://wixtoolset.org/schemas/v4/wi/tables" },
            { "http://schemas.microsoft.com/wix/2006/WixUnit", "http://wixtoolset.org/schemas/v4/wixunit" },
        };

        private Dictionary<XName, Action<XElement>> ConvertElementMapping;

        /// <summary>
        /// Instantiate a new Converter class.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="errorsAsWarnings">Test errors to display as warnings.</param>
        /// <param name="ignoreErrors">Test errors to ignore.</param>
        public Converter(int indentationAmount, IEnumerable<string> errorsAsWarnings = null, IEnumerable<string> ignoreErrors = null)
        {
            this.ConvertElementMapping = new Dictionary<XName, Action<XElement>>()
            {
                { FileElementName, this.ConvertFileElement },
                { ExePackageElementName, this.ConvertSuppressSignatureValidation },
                { MsiPackageElementName, this.ConvertSuppressSignatureValidation },
                { MspPackageElementName, this.ConvertSuppressSignatureValidation },
                { MsuPackageElementName, this.ConvertSuppressSignatureValidation },
                { PayloadElementName, this.ConvertSuppressSignatureValidation },
                { WixElementWithoutNamespaceName, this.ConvertWixElementWithoutNamespace },
            };

            this.IndentationAmount = indentationAmount;

            this.ErrorsAsWarnings = new HashSet<ConverterTestType>(this.YieldConverterTypes(errorsAsWarnings));

            this.IgnoreErrors = new HashSet<ConverterTestType>(this.YieldConverterTypes(ignoreErrors));
        }

        private int Errors { get; set; }

        private HashSet<ConverterTestType> ErrorsAsWarnings { get; set; }

        private HashSet<ConverterTestType> IgnoreErrors { get; set; }

        private int IndentationAmount { get; set; }

        private string SourceFile { get; set; }

        /// <summary>
        /// Convert a file.
        /// </summary>
        /// <param name="sourceFile">The file to convert.</param>
        /// <param name="saveConvertedFile">Option to save the converted errors that are found.</param>
        /// <returns>The number of errors found.</returns>
        public int ConvertFile(string sourceFile, bool saveConvertedFile)
        {
            XDocument document;

            // Set the instance info.
            this.Errors = 0;
            this.SourceFile = sourceFile;

            try
            {
                document = XDocument.Load(this.SourceFile, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            }
            catch (XmlException e)
            {
                this.OnError(ConverterTestType.XmlException, (XObject)null, "The xml is invalid.  Detail: '{0}'", e.Message);

                return this.Errors;
            }

            this.ConvertDocument(document);

            // Fix errors if requested and necessary.
            if (saveConvertedFile && 0 < this.Errors)
            {
                try
                {
                    using (StreamWriter writer = File.CreateText(this.SourceFile))
                    {
                        document.Save(writer, SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    this.OnError(ConverterTestType.UnauthorizedAccessException, (XObject)null, "Could not write to file.");
                }
            }

            return this.Errors;
        }

        /// <summary>
        /// Convert a document.
        /// </summary>
        /// <param name="document">The document to convert.</param>
        /// <returns>The number of errors found.</returns>
        public int ConvertDocument(XDocument document)
        {
            XDeclaration declaration = document.Declaration;

            // Convert the declaration.
            if (null != declaration)
            {
                if (!String.Equals("utf-8", declaration.Encoding, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.OnError(ConverterTestType.DeclarationEncodingWrong, document.Root, "The XML declaration encoding is not properly set to 'utf-8'."))
                    {
                        declaration.Encoding = "utf-8";
                    }
                }
            }
            else // missing declaration
            {
                if (this.OnError(ConverterTestType.DeclarationMissing, (XNode)null, "This file is missing an XML declaration on the first line."))
                {
                    document.Declaration = new XDeclaration("1.0", "utf-8", null);
                    document.Root.AddBeforeSelf(new XText(XDocumentNewLine));
                }
            }

            // Start converting the nodes at the top.
            this.ConvertNode(document.Root, 0);

            return this.Errors;
        }

        /// <summary>
        /// Convert a single xml node.
        /// </summary>
        /// <param name="node">The node to convert.</param>
        /// <param name="level">The depth level of the node.</param>
        /// <returns>The converted node.</returns>
        private void ConvertNode(XNode node, int level)
        {
            // Convert this node's whitespace.
            if ((XmlNodeType.Comment == node.NodeType && 0 > ((XComment)node).Value.IndexOf(XDocumentNewLine, StringComparison.Ordinal)) ||
                XmlNodeType.CDATA == node.NodeType || XmlNodeType.Element == node.NodeType || XmlNodeType.ProcessingInstruction == node.NodeType)
            {
                this.ConvertWhitespace(node, level);
            }

            // Convert this node if it is an element.
            XElement element = node as XElement;

            if (null != element)
            {
                this.ConvertElement(element);

                // Convert all children of this element.
                IEnumerable<XNode> children = element.Nodes().ToList();

                foreach (XNode child in children)
                {
                    this.ConvertNode(child, level + 1);
                }
            }
        }

        private void ConvertElement(XElement element)
        {
            // Gather any deprecated namespaces, then update this element tree based on those deprecations.
            Dictionary<XNamespace, XNamespace> deprecatedToUpdatedNamespaces = new Dictionary<XNamespace, XNamespace>();

            foreach (XAttribute declaration in element.Attributes().Where(a => a.IsNamespaceDeclaration))
            {
                XNamespace ns;

                if (Converter.OldToNewNamespaceMapping.TryGetValue(declaration.Value, out ns))
                {
                    if (this.OnError(ConverterTestType.XmlnsValueWrong, declaration, "The namespace '{0}' is out of date.  It must be '{1}'.", declaration.Value, ns.NamespaceName))
                    {
                        deprecatedToUpdatedNamespaces.Add(declaration.Value, ns);
                    }
                }
            }

            if (deprecatedToUpdatedNamespaces.Any())
            {
                Converter.UpdateElementsWithDeprecatedNamespaces(element.DescendantsAndSelf(), deprecatedToUpdatedNamespaces);
            }

            // Convert the node in much greater detail.
            Action<XElement> convert;

            if (this.ConvertElementMapping.TryGetValue(element.Name, out convert))
            {
                convert(element);
            }
        }

        private void ConvertFileElement(XElement element)
        {
            if (null == element.Attribute("Id"))
            {
                XAttribute attribute = element.Attribute("Name");

                if (null == attribute)
                {
                    attribute = element.Attribute("Source");
                }

                if (null != attribute)
                {
                    string name = Path.GetFileName(attribute.Value);

                    if (this.OnError(ConverterTestType.AssignAnonymousFileId, element, "The file id is being updated to '{0}' to ensure it remains the same as the default", name))
                    {
                        IEnumerable<XAttribute> attributes = element.Attributes().ToList();
                        element.RemoveAttributes();
                        element.Add(new XAttribute("Id", Common.GetIdentifierFromName(name)));
                        element.Add(attributes);
                    }
                }
            }
        }

        private void ConvertSuppressSignatureValidation(XElement element)
        {
            XAttribute suppressSignatureValidation = element.Attribute("SuppressSignatureValidation");

            if (null != suppressSignatureValidation)
            {
                if (this.OnError(ConverterTestType.SuppressSignatureValidationDeprecated, element, "The chain package element contains deprecated '{0}' attribute. Use the 'EnableSignatureValidation' instead.", suppressSignatureValidation))
                {
                    if ("no" == suppressSignatureValidation.Value)
                    {
                        element.Add(new XAttribute("EnableSignatureValidation", "yes"));
                    }
                }

                suppressSignatureValidation.Remove();
            }
        }

        /// <summary>
        /// Converts a Wix element.
        /// </summary>
        /// <param name="element">The Wix element to convert.</param>
        /// <returns>The converted element.</returns>
        private void ConvertWixElementWithoutNamespace(XElement element)
        {
            if (this.OnError(ConverterTestType.XmlnsMissing, element, "The xmlns attribute is missing.  It must be present with a value of '{0}'.", WixNamespace.NamespaceName))
            {
                element.Name = WixNamespace.GetName(element.Name.LocalName);

                element.Add(new XAttribute("xmlns", WixNamespace.NamespaceName)); // set the default namespace.

                foreach (XElement elementWithoutNamespace in element.Elements().Where(e => XNamespace.None == e.Name.Namespace))
                {
                    elementWithoutNamespace.Name = WixNamespace.GetName(elementWithoutNamespace.Name.LocalName);
                }
            }
        }

        /// <summary>
        /// Convert the whitespace adjacent to a node.
        /// </summary>
        /// <param name="node">The node to convert.</param>
        /// <param name="level">The depth level of the node.</param>
        private void ConvertWhitespace(XNode node, int level)
        {
            // Fix the whitespace before this node.
            XText whitespace = node.PreviousNode as XText;

            if (null != whitespace)
            {
                if (XmlNodeType.CDATA == node.NodeType)
                {
                    if (this.OnError(ConverterTestType.WhitespacePrecedingCDATAWrong, node, "There should be no whitespace preceding a CDATA node."))
                    {
                        whitespace.Remove();
                    }
                }
                else
                {
                    if (!Converter.IsLegalWhitespace(this.IndentationAmount, level, whitespace.Value))
                    {
                        if (this.OnError(ConverterTestType.WhitespacePrecedingNodeWrong, node, "The whitespace preceding this node is incorrect."))
                        {
                            Converter.FixWhitespace(this.IndentationAmount, level, whitespace);
                        }
                    }
                }
            }

            // Fix the whitespace after CDATA nodes.
            XCData cdata = node as XCData;

            if (null != cdata)
            {
                whitespace = cdata.NextNode as XText;

                if (null != whitespace)
                {
                    if (this.OnError(ConverterTestType.WhitespaceFollowingCDATAWrong, node, "There should be no whitespace following a CDATA node."))
                    {
                        whitespace.Remove();
                    }
                }
            }
            else
            {
                // Fix the whitespace inside and after this node (except for Error which may contain just whitespace).
                XElement element = node as XElement;

                if (null != element && "Error" != element.Name.LocalName)
                {
                    if (!element.HasElements && !element.IsEmpty && String.IsNullOrEmpty(element.Value.Trim()))
                    {
                        if (this.OnError(ConverterTestType.NotEmptyElement, element, "This should be an empty element since it contains nothing but whitespace."))
                        {
                            element.RemoveNodes();
                        }
                    }

                    whitespace = node.NextNode as XText;

                    if (null != whitespace)
                    {
                        if (!Converter.IsLegalWhitespace(this.IndentationAmount, level - 1, whitespace.Value))
                        {
                            if (this.OnError(ConverterTestType.WhitespacePrecedingEndElementWrong, whitespace, "The whitespace preceding this end element is incorrect."))
                            {
                                Converter.FixWhitespace(this.IndentationAmount, level - 1, whitespace);
                            }
                        }
                    }
                }
            }
        }

        private IEnumerable<ConverterTestType> YieldConverterTypes(IEnumerable<string> types)
        {
            if (null != types)
            {
                foreach (string type in types)
                {
                    ConverterTestType itt;

                    if (Enum.TryParse<ConverterTestType>(type, true, out itt))
                    {
                        yield return itt;
                    }
                    else // not a known ConverterTestType
                    {
                        this.OnError(ConverterTestType.ConverterTestTypeUnknown, (XObject)null, "Unknown error type: '{0}'.", type);
                    }
                }
            }
        }

        private static void UpdateElementsWithDeprecatedNamespaces(IEnumerable<XElement> elements, Dictionary<XNamespace, XNamespace> deprecatedToUpdatedNamespaces)
        {
            foreach (XElement element in elements)
            {
                XNamespace ns;

                if (deprecatedToUpdatedNamespaces.TryGetValue(element.Name.Namespace, out ns))
                {
                    element.Name = ns.GetName(element.Name.LocalName);
                }

                // Remove all the attributes and add them back to with their namespace updated (as necessary).
                IEnumerable<XAttribute> attributes = element.Attributes().ToList();
                element.RemoveAttributes();

                foreach (XAttribute attribute in attributes)
                {
                    XAttribute convertedAttribute = attribute;

                    if (attribute.IsNamespaceDeclaration)
                    {
                        if (deprecatedToUpdatedNamespaces.TryGetValue(attribute.Value, out ns))
                        {
                            convertedAttribute = ("xmlns" == attribute.Name.LocalName) ? new XAttribute(attribute.Name.LocalName, ns.NamespaceName) : new XAttribute(XNamespace.Xmlns + attribute.Name.LocalName, ns.NamespaceName);
                        }
                    }
                    else if (deprecatedToUpdatedNamespaces.TryGetValue(attribute.Name.Namespace, out ns))
                    {
                        convertedAttribute = new XAttribute(ns.GetName(attribute.Name.LocalName), attribute.Value);
                    }

                    element.Add(convertedAttribute);
                }
            }
        }

        /// <summary>
        /// Determine if the whitespace preceding a node is appropriate for its depth level.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="level">The depth level that should match this whitespace.</param>
        /// <param name="whitespace">The whitespace to validate.</param>
        /// <returns>true if the whitespace is legal; false otherwise.</returns>
        private static bool IsLegalWhitespace(int indentationAmount, int level, string whitespace)
        {
            // strip off leading newlines; there can be an arbitrary number of these
            while (whitespace.StartsWith(XDocumentNewLine, StringComparison.Ordinal))
            {
                whitespace = whitespace.Substring(XDocumentNewLine.Length);
            }

            // check the length
            if (whitespace.Length != level * indentationAmount)
            {
                return false;
            }

            // check the spaces
            foreach (char character in whitespace)
            {
                if (' ' != character)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Fix the whitespace in a Whitespace node.
        /// </summary>
        /// <param name="indentationAmount">Indentation value to use when validating leading whitespace.</param>
        /// <param name="level">The depth level of the desired whitespace.</param>
        /// <param name="whitespace">The whitespace node to fix.</param>
        private static void FixWhitespace(int indentationAmount, int level, XText whitespace)
        {
            int newLineCount = 0;

            for (int i = 0; i + 1 < whitespace.Value.Length; ++i)
            {
                if (XDocumentNewLine == whitespace.Value.Substring(i, 2))
                {
                    ++i; // skip an extra character
                    ++newLineCount;
                }
            }

            if (0 == newLineCount)
            {
                newLineCount = 1;
            }

            // reset the whitespace value
            whitespace.Value = String.Empty;

            // add the correct number of newlines
            for (int i = 0; i < newLineCount; ++i)
            {
                whitespace.Value = String.Concat(whitespace.Value, XDocumentNewLine);
            }

            // add the correct number of spaces based on configured indentation amount
            whitespace.Value = String.Concat(whitespace.Value, new string(' ', level * indentationAmount));
        }

        /// <summary>
        /// Output an error message to the console.
        /// </summary>
        /// <param name="converterTestType">The type of converter test.</param>
        /// <param name="node">The node that caused the error.</param>
        /// <param name="message">Detailed error message.</param>
        /// <param name="args">Additional formatted string arguments.</param>
        /// <returns>Returns true indicating that action should be taken on this error, and false if it should be ignored.</returns>
        private bool OnError(ConverterTestType converterTestType, XObject node, string message, params object[] args)
        {
            if (this.IgnoreErrors.Contains(converterTestType)) // ignore the error
            {
                return false;
            }

            // Increase the error count.
            this.Errors++;

            SourceLineNumber sourceLine = (null == node) ? new SourceLineNumber(this.SourceFile ?? "wixcop.exe") : new SourceLineNumber(this.SourceFile, ((IXmlLineInfo)node).LineNumber);
            bool warning = this.ErrorsAsWarnings.Contains(converterTestType);
            string display = String.Format(CultureInfo.CurrentCulture, message, args);

            WixGenericMessageEventArgs ea = new WixGenericMessageEventArgs(sourceLine, (int)converterTestType, warning ? MessageLevel.Warning : MessageLevel.Error, "{0} ({1})", display, converterTestType.ToString());

            Messaging.Instance.OnMessage(ea);

            return true;
        }

        /// <summary>
        /// Converter test types.  These are used to condition error messages down to warnings.
        /// </summary>
        private enum ConverterTestType
        {
            /// <summary>
            /// Internal-only: displayed when a string cannot be converted to an ConverterTestType.
            /// </summary>
            ConverterTestTypeUnknown,

            /// <summary>
            /// Displayed when an XML loading exception has occurred.
            /// </summary>
            XmlException,

            /// <summary>
            /// Displayed when a file cannot be accessed; typically when trying to save back a fixed file.
            /// </summary>
            UnauthorizedAccessException,

            /// <summary>
            /// Displayed when the encoding attribute in the XML declaration is not 'UTF-8'.
            /// </summary>
            DeclarationEncodingWrong,

            /// <summary>
            /// Displayed when the XML declaration is missing from the source file.
            /// </summary>
            DeclarationMissing,

            /// <summary>
            /// Displayed when the whitespace preceding a CDATA node is wrong.
            /// </summary>
            WhitespacePrecedingCDATAWrong,

            /// <summary>
            /// Displayed when the whitespace preceding a node is wrong.
            /// </summary>
            WhitespacePrecedingNodeWrong,

            /// <summary>
            /// Displayed when an element is not empty as it should be.
            /// </summary>
            NotEmptyElement,

            /// <summary>
            /// Displayed when the whitespace following a CDATA node is wrong.
            /// </summary>
            WhitespaceFollowingCDATAWrong,

            /// <summary>
            /// Displayed when the whitespace preceding an end element is wrong.
            /// </summary>
            WhitespacePrecedingEndElementWrong,

            /// <summary>
            /// Displayed when the xmlns attribute is missing from the document element.
            /// </summary>
            XmlnsMissing,

            /// <summary>
            /// Displayed when the xmlns attribute on the document element is wrong.
            /// </summary>
            XmlnsValueWrong,

            /// <summary>
            /// Assign an identifier to a File element when on Id attribute is specified.
            /// </summary>
            AssignAnonymousFileId,

            /// <summary>
            /// SuppressSignatureValidation attribute is deprecated and replaced with EnableSignatureValidation.
            /// </summary>
            SuppressSignatureValidationDeprecated,
        }
    }
}
