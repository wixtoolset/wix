// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class IIsCompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/iis";

        /// <summary>
        /// Types of objects that custom HTTP Headers can be applied to.
        /// </summary>
        /// <remarks>Note that this must be kept in sync with the eHttpHeaderParentType in scahttpheader.h.</remarks>
        private enum HttpHeaderParentType
        {
            /// <summary>Custom HTTP Header is to be applied to a Web Virtual Directory.</summary>
            WebVirtualDir = 1,
            /// <summary>Custom HTTP Header is to be applied to a Web Site.</summary>
            WebSite = 2,
        }

        /// <summary>
        /// Types of objects that MimeMaps can be applied to.
        /// </summary>
        /// <remarks>Note that this must be kept in sync with the eMimeMapParentType in scamimemap.h.</remarks>
        private enum MimeMapParentType
        {
            /// <summary>MimeMap is to be applied to a Web Virtual Directory.</summary>
            WebVirtualDir = 1,
            WebSite = 2,
        }

        /// <summary>
        /// Types of objects that custom WebErrors can be applied to.
        /// </summary>
        /// <remarks>Note that this must be kept in sync with the eWebErrorParentType in scaweberror.h.</remarks>
        private enum WebErrorParentType
        {
            /// <summary>Custom WebError is to be applied to a Web Virtual Directory.</summary>
            WebVirtualDir = 1,

            /// <summary>Custom WebError is to be applied to a Web Site.</summary>
            WebSite = 2,
        }

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    string componentId = context["ComponentId"];
                    string directoryId = context["DirectoryId"];

                    switch (element.Name.LocalName)
                    {
                        case "Certificate":
                            this.ParseCertificateElement(intermediate, section, element, componentId);
                            break;
                        case "WebAppPool":
                            this.ParseWebAppPoolElement(intermediate, section, element, componentId);
                            break;
                        case "WebDir":
                            this.ParseWebDirElement(intermediate, section, element, componentId, null);
                            break;
                        case "WebFilter":
                            this.ParseWebFilterElement(intermediate, section, element, componentId, null);
                            break;
                        case "WebProperty":
                            this.ParseWebPropertyElement(intermediate, section, element, componentId);
                            break;
                        case "WebServiceExtension":
                            this.ParseWebServiceExtensionElement(intermediate, section, element, componentId);
                            break;
                        case "WebSite":
                            this.ParseWebSiteElement(intermediate, section, element, componentId);
                            break;
                        case "WebVirtualDir":
                            this.ParseWebVirtualDirElement(intermediate, section, element, componentId, null, null);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.Name.LocalName)
                    {
                        case "WebApplication":
                            this.ParseWebApplicationElement(intermediate, section, element);
                            break;
                        case "WebAppPool":
                            this.ParseWebAppPoolElement(intermediate, section, element, null);
                            break;
                        case "WebDirProperties":
                            this.ParseWebDirPropertiesElement(intermediate, section, element);
                            break;
                        case "WebLog":
                            this.ParseWebLogElement(intermediate, section, element);
                            break;
                        case "WebSite":
                            this.ParseWebSiteElement(intermediate, section, element, null);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.ParseHelper.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        /// <summary>
        /// Parses a certificate element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseCertificateElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int attributes = 0;
            string binaryKey = null;
            string certificatePath = null;
            string name = null;
            string pfxPassword = null;
            int storeLocation = 0;
            string storeName = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "BinaryKey":
                            attributes |= 2; // SCA_CERT_ATTRIBUTE_BINARYDATA
                            binaryKey = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Binary", binaryKey);
                            break;
                        case "CertificatePath":
                            certificatePath = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Overwrite":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 4; // SCA_CERT_ATTRIBUTE_OVERWRITE
                            }
                            else
                            {
                                attributes &= ~4; // SCA_CERT_ATTRIBUTE_OVERWRITE
                            }
                            break;
                        case "PFXPassword":
                            pfxPassword = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Request":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 1; // SCA_CERT_ATTRIBUTE_REQUEST
                            }
                            else
                            {
                                attributes &= ~1; // SCA_CERT_ATTRIBUTE_REQUEST
                            }
                            break;
                        case "StoreLocation":
                            string storeLocationValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < storeLocationValue.Length)
                            {
                                switch (storeLocationValue)
                                {
                                    case "currentUser":
                                        storeLocation = 1; // SCA_CERTSYSTEMSTORE_CURRENTUSER
                                        break;
                                    case "localMachine":
                                        storeLocation = 2; // SCA_CERTSYSTEMSTORE_LOCALMACHINE
                                        break;
                                    default:
                                        storeLocation = -1;
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "StoreLocation", storeLocationValue, "currentUser", "localMachine"));
                                        break;
                                }
                            }
                            break;
                        case "StoreName":
                            string storeNameValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < storeNameValue.Length)
                            {
                                switch (storeNameValue)
                                {
                                    case "ca":
                                        storeName = "CA";
                                        break;
                                    case "my":
                                    case "personal":
                                        storeName = "MY";
                                        break;
                                    case "request":
                                        storeName = "REQUEST";
                                        break;
                                    case "root":
                                        storeName = "Root";
                                        break;
                                    case "otherPeople":
                                        storeName = "AddressBook";
                                        break;
                                    case "trustedPeople":
                                        storeName = "TrustedPeople";
                                        break;
                                    case "trustedPublisher":
                                        storeName = "TrustedPublisher";
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "StoreName", storeNameValue, "ca", "my", "request", "root", "otherPeople", "trustedPeople", "trustedPublisher"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }


            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            if (0 == storeLocation)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "StoreLocation"));
            }

            if (null == storeName)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "StoreName"));
            }

            if (null != binaryKey && null != certificatePath)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "BinaryKey", "CertificatePath", certificatePath));
            }
            else if (null == binaryKey && null == certificatePath)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, element.Name.LocalName, "BinaryKey", "CertificatePath"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference InstallCertificates and UninstallCertificates since nothing will happen without them
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "InstallCertificates");
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "UninstallCertificates");
            this.ParseHelper.EnsureTable(section, sourceLineNumbers, "CertificateHash"); // Certificate CustomActions require the CertificateHash table

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "Certificate", id);
                row.Set(1, componentId);
                row.Set(2, name);
                row.Set(3, storeLocation);
                row.Set(4, storeName);
                row.Set(5, attributes);
                row.Set(6, binaryKey);
                row.Set(7, certificatePath);
                row.Set(8, pfxPassword);
            }
        }

        /// <summary>
        /// Parses a CertificateRef extension element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="webId">Identifier for parent web site.</param>
        private void ParseCertificateRefElement(Intermediate intermediate, IntermediateSection section, XElement element, string webId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Certificate", id.Id);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Certificate", id.Id);

                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebSiteCertificates");
                row.Set(0, webId);
                row.Set(1, id.Id);
            }
        }

        /// <summary>
        /// Parses a mime map element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="parentId">Identifier for parent symbol.</param>
        /// <param name="parentType">Type that parentId refers to.</param>
        private void ParseMimeMapElement(Intermediate intermediate, IntermediateSection section, XElement element, string parentId, MimeMapParentType parentType)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string extension = null;
            string type = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Extension":
                            extension = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            type = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == extension)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Extension"));
            }
            else if (0 < extension.Length)
            {
                if (!extension.StartsWith(".", StringComparison.Ordinal))
                {
                    this.Messaging.Write(IIsErrors.MimeMapExtensionMissingPeriod(sourceLineNumbers, element.Name.LocalName, "Extension", extension));
                }
            }

            if (null == type)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Type"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsMimeMap", id);
                row.Set(1, (int)parentType);
                row.Set(2, parentId);
                row.Set(3, type);
                row.Set(4, extension);
            }
        }

        /// <summary>
        /// Parses a recycle time element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <returns>Recycle time value.</returns>
        private string ParseRecycleTimeElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string value = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Value":
                            value = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == value)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Value"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            return value;
        }

        /// <summary>
        /// Parses a web address element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="parentWeb">Identifier of parent web site.</param>
        /// <returns>Identifier for web address.</returns>
        private string ParseWebAddressElement(Intermediate intermediate, IntermediateSection section, XElement element, string parentWeb)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string header = null;
            string ip = null;
            string port = null;
            bool secure = false;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Header":
                            header = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IP":
                            ip = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Port":
                            port = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Secure":
                            secure = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == port)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Port"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebAddress", id);
                row.Set(1, parentWeb);
                row.Set(2, ip);
                row.Set(3, port);
                row.Set(4, header);
                row.Set(5, secure ? 1 : 0);
            }

            return id?.Id;
        }

        /// <summary>
        /// Parses a web application element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <returns>Identifier for web application.</returns>
        private string ParseWebApplicationElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            YesNoDefaultType allowSessions = YesNoDefaultType.Default;
            string appPool = null;
            YesNoDefaultType buffer = YesNoDefaultType.Default;
            YesNoDefaultType clientDebugging = YesNoDefaultType.Default;
            string defaultScript = null;
            int isolation = 0;
            string name = null;
            YesNoDefaultType parentPaths = YesNoDefaultType.Default;
            int scriptTimeout = CompilerConstants.IntegerNotSet;
            int sessionTimeout = CompilerConstants.IntegerNotSet;
            YesNoDefaultType serverDebugging = YesNoDefaultType.Default;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "AllowSessions":
                            allowSessions = this.ParseHelper.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "Buffer":
                            buffer = this.ParseHelper.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "ClientDebugging":
                            clientDebugging = this.ParseHelper.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultScript":
                            defaultScript = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < defaultScript.Length)
                            {
                                switch (defaultScript)
                                {
                                    case "JScript":
                                    case "VBScript":
                                        // these are valid values
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, defaultScript, "JScript", "VBScript"));
                                        break;
                                }
                            }
                            break;
                        case "Isolation":
                            string isolationValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < isolationValue.Length)
                            {
                                switch (isolationValue)
                                {
                                    case "low":
                                        isolation = 0;
                                        break;
                                    case "medium":
                                        isolation = 2;
                                        break;
                                    case "high":
                                        isolation = 1;
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, isolationValue, "low", "medium", "high"));
                                        break;
                                }
                            }
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ParentPaths":
                            parentPaths = this.ParseHelper.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "ScriptTimeout":
                            scriptTimeout = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ServerDebugging":
                            serverDebugging = this.ParseHelper.GetAttributeYesNoDefaultValue(sourceLineNumbers, attrib);
                            break;
                        case "SessionTimeout":
                            sessionTimeout = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "WebAppPool":
                            appPool = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsAppPool", appPool);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }
            else if (-1 != name.IndexOf("\\", StringComparison.Ordinal))
            {
                this.Messaging.Write(IIsErrors.IllegalCharacterInAttributeValue(sourceLineNumbers, element.Name.LocalName, "Name", name, '\\'));
            }

            foreach (XElement child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "WebApplicationExtension":
                            this.ParseWebApplicationExtensionElement(intermediate, section, child, id?.Id);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebApplication", id);
                row.Set(1, name);
                row.Set(2, isolation);
                if (YesNoDefaultType.Default != allowSessions)
                {
                    row.Set(3, YesNoDefaultType.Yes == allowSessions ? 1 : 0);
                }

                if (CompilerConstants.IntegerNotSet != sessionTimeout)
                {
                    row.Set(4, sessionTimeout);
                }

                if (YesNoDefaultType.Default != buffer)
                {
                    row.Set(5, YesNoDefaultType.Yes == buffer ? 1 : 0);
                }

                if (YesNoDefaultType.Default != parentPaths)
                {
                    row.Set(6, YesNoDefaultType.Yes == parentPaths ? 1 : 0);
                }
                row.Set(7, defaultScript);
                if (CompilerConstants.IntegerNotSet != scriptTimeout)
                {
                    row.Set(8, scriptTimeout);
                }

                if (YesNoDefaultType.Default != serverDebugging)
                {
                    row.Set(9, YesNoDefaultType.Yes == serverDebugging ? 1 : 0);
                }

                if (YesNoDefaultType.Default != clientDebugging)
                {
                    row.Set(10, YesNoDefaultType.Yes == clientDebugging ? 1 : 0);
                }
                row.Set(11, appPool);
            }

            return id?.Id;
        }

        /// <summary>
        /// Parses a web application extension element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="application">Identifier for parent web application.</param>
        private void ParseWebApplicationExtensionElement(Intermediate intermediate, IntermediateSection section, XElement element, string application)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            int attributes = 0;
            string executable = null;
            string extension = null;
            string verbs = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "CheckPath":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 4;
                            }
                            else
                            {
                                attributes &= ~4;
                            }
                            break;
                        case "Executable":
                            executable = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Extension":
                            extension = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Script":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 1;
                            }
                            else
                            {
                                attributes &= ~1;
                            }
                            break;
                        case "Verbs":
                            verbs = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebApplicationExtension");
                row.Set(0, application);
                row.Set(1, extension);
                row.Set(2, verbs);
                row.Set(3, executable);
                if (0 < attributes)
                {
                    row.Set(4, attributes);
                }
            }
        }

        /// <summary>
        /// Parses web application pool element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Optional identifier of parent component.</param>
        private void ParseWebAppPoolElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int attributes = 0;
            int cpuAction = CompilerConstants.IntegerNotSet;
            string cpuMon = null;
            int idleTimeout = CompilerConstants.IntegerNotSet;
            int maxCpuUsage = 0;
            int maxWorkerProcs = CompilerConstants.IntegerNotSet;
            string managedRuntimeVersion = null;
            string managedPipelineMode = null;
            string name = null;
            int privateMemory = CompilerConstants.IntegerNotSet;
            int queueLimit = CompilerConstants.IntegerNotSet;
            int recycleMinutes = CompilerConstants.IntegerNotSet;
            int recycleRequests = CompilerConstants.IntegerNotSet;
            string recycleTimes = null;
            int refreshCpu = CompilerConstants.IntegerNotSet;
            string user = null;
            int virtualMemory = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "CpuAction":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            string cpuActionValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < cpuActionValue.Length)
                            {
                                switch (cpuActionValue)
                                {
                                    case "shutdown":
                                        cpuAction = 1;
                                        break;
                                    case "none":
                                        cpuAction = 0;
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, cpuActionValue, "shutdown", "none"));
                                        break;
                                }
                            }
                            break;
                        case "Identity":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            string identityValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < identityValue.Length)
                            {
                                switch (identityValue)
                                {
                                    case "networkService":
                                        attributes |= 1;
                                        break;
                                    case "localService":
                                        attributes |= 2;
                                        break;
                                    case "localSystem":
                                        attributes |= 4;
                                        break;
                                    case "other":
                                        attributes |= 8;
                                        break;
                                    case "applicationPoolIdentity":
                                        attributes |= 0x10;
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, identityValue, "networkService", "localService", "localSystem", "other", "applicationPoolIdentity"));
                                        break;
                                }
                            }
                            break;
                        case "IdleTimeout":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            idleTimeout = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "ManagedPipelineMode":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            managedPipelineMode = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);


                            if (!String.IsNullOrEmpty(managedPipelineMode))
                            {
                                switch (managedPipelineMode)
                                {
                                    // In 3.5 we allowed lower case values (per camel case enum style), we now use formatted fields, 
                                    // so the value needs to match exactly what we pass in to IIS which uses pascal case.
                                    case "classic":
                                        managedPipelineMode = "Classic";
                                        break;
                                    case "integrated":
                                        managedPipelineMode = "Integrated";
                                        break;
                                    case "Classic":
                                        break;
                                    case "Integrated":
                                        break;
                                    default:
                                        if (!this.ParseHelper.ContainsProperty(managedPipelineMode))
                                        {
                                            this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName, managedPipelineMode, "Classic", "Integrated"));
                                        }
                                        break;
                                }
                            }

                            break;
                        case "ManagedRuntimeVersion":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            managedRuntimeVersion = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MaxCpuUsage":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            maxCpuUsage = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 100);
                            break;
                        case "MaxWorkerProcesses":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            maxWorkerProcs = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PrivateMemory":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            privateMemory = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 4294967);
                            break;
                        case "QueueLimit":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            queueLimit = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "RecycleMinutes":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            recycleMinutes = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "RecycleRequests":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            recycleRequests = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "RefreshCpu":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            refreshCpu = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "User":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            user = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "User", user);
                            break;
                        case "VirtualMemory":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            virtualMemory = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 4294967);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            if (null == user && 8 == (attributes & 0x1F))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "User", "Identity", "other"));
            }

            if (null != user && 8 != (attributes & 0x1F))
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithoutOtherAttribute(sourceLineNumbers, element.Name.LocalName, "User", user, "Identity", "other"));
            }

            cpuMon = maxCpuUsage.ToString(CultureInfo.InvariantCulture.NumberFormat);
            if (CompilerConstants.IntegerNotSet != refreshCpu)
            {
                cpuMon = String.Concat(cpuMon, ",", refreshCpu.ToString(CultureInfo.InvariantCulture.NumberFormat));
                if (CompilerConstants.IntegerNotSet != cpuAction)
                {
                    cpuMon = String.Concat(cpuMon, ",", cpuAction.ToString(CultureInfo.InvariantCulture.NumberFormat));
                }
            }

            foreach (XElement child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "RecycleTime":
                            if (null == componentId)
                            {
                                SourceLineNumber childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, element.Name.LocalName));
                            }

                            if (null == recycleTimes)
                            {
                                recycleTimes = this.ParseRecycleTimeElement(intermediate, section, child);
                            }
                            else
                            {
                                recycleTimes = String.Concat(recycleTimes, ",", this.ParseRecycleTimeElement(intermediate, section, child));
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }

            if (null != componentId)
            {
                // Reference ConfigureIIs since nothing will happen without it
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureIIs");
            }

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsAppPool", id);
                row.Set(1, name);
                row.Set(2, componentId);
                row.Set(3, attributes);
                row.Set(4, user);
                if (CompilerConstants.IntegerNotSet != recycleMinutes)
                {
                    row.Set(5, recycleMinutes);
                }

                if (CompilerConstants.IntegerNotSet != recycleRequests)
                {
                    row.Set(6, recycleRequests);
                }
                row.Set(7, recycleTimes);
                if (CompilerConstants.IntegerNotSet != idleTimeout)
                {
                    row.Set(8, idleTimeout);
                }

                if (CompilerConstants.IntegerNotSet != queueLimit)
                {
                    row.Set(9, queueLimit);
                }
                row.Set(10, cpuMon);
                if (CompilerConstants.IntegerNotSet != maxWorkerProcs)
                {
                    row.Set(11, maxWorkerProcs);
                }

                if (CompilerConstants.IntegerNotSet != virtualMemory)
                {
                    row.Set(12, virtualMemory);
                }

                if (CompilerConstants.IntegerNotSet != privateMemory)
                {
                    row.Set(13, privateMemory);
                }
                row.Set(14, managedRuntimeVersion);
                row.Set(15, managedPipelineMode);
            }
        }

        /// <summary>
        /// Parses a web directory element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        /// <param name="parentWeb">Optional identifier for parent web site.</param>
        private void ParseWebDirElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string parentWeb)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string dirProperties = null;
            string path = null;
            string application = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "DirProperties":
                            dirProperties = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Path":
                            path = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebApplication":
                            application = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebSite":
                            if (null != parentWeb)
                            {
                                this.Messaging.Write(IIsErrors.WebSiteAttributeUnderWebSite(sourceLineNumbers, element.Name.LocalName));
                            }

                            parentWeb = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebSite", parentWeb);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == path)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Path"));
            }

            if (null == parentWeb)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "WebSite"));
            }

            foreach (XElement child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "WebApplication":
                            if (null != application)
                            {
                                this.Messaging.Write(IIsErrors.WebApplicationAlreadySpecified(childSourceLineNumbers, element.Name.LocalName));
                            }

                            application = this.ParseWebApplicationElement(intermediate, section, child);
                            break;
                        case "WebDirProperties":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            string childWebDirProperties = this.ParseWebDirPropertiesElement(intermediate, section, child);
                            if (null == dirProperties)
                            {
                                dirProperties = childWebDirProperties;
                            }
                            else
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, child.Name.LocalName, "DirProperties", child.Name.LocalName));
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }

            if (null == dirProperties)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "DirProperties"));
            }

            if (null != application)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebApplication", application);
            }

            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebDirProperties", dirProperties);

            // Reference ConfigureIIs since nothing will happen without it
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebDir", id);
                row.Set(1, componentId);
                row.Set(2, parentWeb);
                row.Set(3, path);
                row.Set(4, dirProperties);
                row.Set(5, application);
            }
        }

        /// <summary>
        /// Parses a web directory properties element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <returns>The identifier for this WebDirProperties.</returns>
        private string ParseWebDirPropertiesElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int access = 0;
            bool accessSet = false;
            int accessSSLFlags = 0;
            bool accessSSLFlagsSet = false;
            string anonymousUser = null;
            YesNoType aspDetailedError = YesNoType.NotSet;
            string authenticationProviders = null;
            int authorization = 0;
            bool authorizationSet = false;
            string cacheControlCustom = null;
            long cacheControlMaxAge = CompilerConstants.LongNotSet;
            string defaultDocuments = null;
            string httpExpires = null;
            bool iisControlledPassword = false;
            YesNoType index = YesNoType.NotSet;
            YesNoType logVisits = YesNoType.NotSet;
            YesNoType notCustomError = YesNoType.NotSet;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "AnonymousUser":
                            anonymousUser = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "User", anonymousUser);
                            break;
                        case "AspDetailedError":
                            aspDetailedError = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "AuthenticationProviders":
                            authenticationProviders = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CacheControlCustom":
                            cacheControlCustom = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CacheControlMaxAge":
                            cacheControlMaxAge = this.ParseHelper.GetAttributeLongValue(sourceLineNumbers, attrib, 0, uint.MaxValue); // 4294967295 (uint.MaxValue) represents unlimited
                            break;
                        case "ClearCustomError":
                            notCustomError = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "DefaultDocuments":
                            defaultDocuments = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HttpExpires":
                            httpExpires = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IIsControlledPassword":
                            iisControlledPassword = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "Index":
                            index = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;
                        case "LogVisits":
                            logVisits = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                            break;

                        // Access attributes
                        case "Execute":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                access |= 4;
                            }
                            else
                            {
                                access &= ~4;
                            }
                            accessSet = true;
                            break;
                        case "Read":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                access |= 1;
                            }
                            else
                            {
                                access &= ~1;
                            }
                            accessSet = true;
                            break;
                        case "Script":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                access |= 512;
                            }
                            else
                            {
                                access &= ~512;
                            }
                            accessSet = true;
                            break;
                        case "Write":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                access |= 2;
                            }
                            else
                            {
                                access &= ~2;
                            }
                            accessSet = true;
                            break;

                        // AccessSSL Attributes
                        case "AccessSSL":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 8;
                            }
                            else
                            {
                                accessSSLFlags &= ~8;
                            }
                            accessSSLFlagsSet = true;
                            break;
                        case "AccessSSL128":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 256;
                            }
                            else
                            {
                                accessSSLFlags &= ~256;
                            }
                            accessSSLFlagsSet = true;
                            break;
                        case "AccessSSLMapCert":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 128;
                            }
                            else
                            {
                                accessSSLFlags &= ~128;
                            }
                            accessSSLFlagsSet = true;
                            break;
                        case "AccessSSLNegotiateCert":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 32;
                            }
                            else
                            {
                                accessSSLFlags &= ~32;
                            }
                            accessSSLFlagsSet = true;
                            break;
                        case "AccessSSLRequireCert":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                accessSSLFlags |= 64;
                            }
                            else
                            {
                                accessSSLFlags &= ~64;
                            }
                            accessSSLFlagsSet = true;
                            break;

                        // Authorization attributes
                        case "AnonymousAccess":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 1;
                            }
                            else
                            {
                                authorization &= ~1;
                            }
                            authorizationSet = true;
                            break;
                        case "BasicAuthentication":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 2;
                            }
                            else
                            {
                                authorization &= ~2;
                            }
                            authorizationSet = true;
                            break;
                        case "DigestAuthentication":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 16;
                            }
                            else
                            {
                                authorization &= ~16;
                            }
                            authorizationSet = true;
                            break;
                        case "PassportAuthentication":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 64;
                            }
                            else
                            {
                                authorization &= ~64;
                            }
                            authorizationSet = true;
                            break;
                        case "WindowsAuthentication":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                authorization |= 4;
                            }
                            else
                            {
                                authorization &= ~4;
                            }
                            authorizationSet = true;
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebDirProperties", id);
                if (accessSet)
                {
                    row.Set(1, access);
                }

                if (authorizationSet)
                {
                    row.Set(2, authorization);
                }
                row.Set(3, anonymousUser);
                row.Set(4, iisControlledPassword ? 1 : 0);
                if (YesNoType.NotSet != logVisits)
                {
                    row.Set(5, YesNoType.Yes == logVisits ? 1 : 0);
                }

                if (YesNoType.NotSet != index)
                {
                    row.Set(6, YesNoType.Yes == index ? 1 : 0);
                }
                row.Set(7, defaultDocuments);
                if (YesNoType.NotSet != aspDetailedError)
                {
                    row.Set(8, YesNoType.Yes == aspDetailedError ? 1 : 0);
                }
                row.Set(9, httpExpires);
                if (CompilerConstants.LongNotSet != cacheControlMaxAge)
                {
                    row.Set(10, unchecked((int)cacheControlMaxAge));
                }
                row.Set(11, cacheControlCustom);
                if (YesNoType.NotSet != notCustomError)
                {
                    row.Set(12, YesNoType.Yes == notCustomError ? 1 : 0);
                }

                if (accessSSLFlagsSet)
                {
                    row.Set(13, accessSSLFlags);
                }

                if (null != authenticationProviders)
                {
                    row.Set(14, authenticationProviders);
                }
            }

            return id?.Id;
        }

        /// <summary>
        /// Parses a web error element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="parentType">Type of the parent.</param>
        /// <param name="parent">Id of the parent.</param>
        private void ParseWebErrorElement(Intermediate intermediate, IntermediateSection section, XElement element, WebErrorParentType parentType, string parent)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            int errorCode = CompilerConstants.IntegerNotSet;
            string file = null;
            string url = null;
            int subCode = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "ErrorCode":
                            errorCode = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 400, 599);
                            break;
                        case "File":
                            file = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SubCode":
                            subCode = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "URL":
                            url = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (CompilerConstants.IntegerNotSet == errorCode)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "ErrorCode"));
                errorCode = CompilerConstants.IllegalInteger;
            }

            if (CompilerConstants.IntegerNotSet == subCode)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "SubCode"));
                subCode = CompilerConstants.IllegalInteger;
            }

            if (String.IsNullOrEmpty(file) && String.IsNullOrEmpty(url))
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "File", "URL"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference ConfigureIIs since nothing will happen without it
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebError");
                row.Set(0, errorCode);
                row.Set(1, subCode);
                row.Set(2, (int)parentType);
                row.Set(3, parent);
                row.Set(4, file);
                row.Set(5, url);
            }
        }

        /// <summary>
        /// Parses a web filter element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="parentWeb">Optional identifier of parent web site.</param>
        private void ParseWebFilterElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string parentWeb)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string description = null;
            int flags = 0;
            int loadOrder = CompilerConstants.IntegerNotSet;
            string name = null;
            string path = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            description = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Flags":
                            flags = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "LoadOrder":
                            string loadOrderValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < loadOrderValue.Length)
                            {
                                switch (loadOrderValue)
                                {
                                    case "first":
                                        loadOrder = 0;
                                        break;
                                    case "last":
                                        loadOrder = -1;
                                        break;
                                    default:
                                        loadOrder = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                                        break;
                                }
                            }
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Path":
                            path = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebSite":
                            if (null != parentWeb)
                            {
                                this.Messaging.Write(IIsErrors.WebSiteAttributeUnderWebSite(sourceLineNumbers, element.Name.LocalName));
                            }

                            parentWeb = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebSite", parentWeb);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            if (null == path)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Path"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference ConfigureIIs since nothing will happen without it
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsFilter", id);
                row.Set(1, name);
                row.Set(2, componentId);
                row.Set(3, path);
                row.Set(4, parentWeb);
                row.Set(5, description);
                row.Set(6, flags);
                if (CompilerConstants.IntegerNotSet != loadOrder)
                {
                    row.Set(7, loadOrder);
                }
            }
        }

        /// <summary>
        /// Parses web log element.
        /// </summary>
        /// <param name="element">Node to be parsed.</param>
        private void ParseWebLogElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string type = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            string typeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (0 < typeValue.Length)
                            {
                                switch (typeValue)
                                {
                                    case "IIS":
                                        type = "Microsoft IIS Log File Format";
                                        break;
                                    case "NCSA":
                                        type = "NCSA Common Log File Format";
                                        break;
                                    case "none":
                                        type = "none";
                                        break;
                                    case "ODBC":
                                        type = "ODBC Logging";
                                        break;
                                    case "W3C":
                                        type = "W3C Extended Log File Format";
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Type", typeValue, "IIS", "NCSA", "none", "ODBC", "W3C"));
                                        break;
                                }
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == type)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Type"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebLog", id);
                row.Set(1, type);
            }
        }

        /// <summary>
        /// Parses a web property element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseWebPropertyElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string value = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            value = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            switch (id?.Id)
            {
                case "ETagChangeNumber":
                case "MaxGlobalBandwidth":
                    // Must specify a value for these
                    if (null == value)
                    {
                        this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Value", "Id", id.Id));
                    }
                    break;
                case "IIs5IsolationMode":
                case "LogInUTF8":
                    // Can't specify a value for these
                    if (null != value)
                    {
                        this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "Value", "Id", id.Id));
                    }
                    break;
                default:
                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Id", id?.Id, "ETagChangeNumber", "IIs5IsolationMode", "LogInUTF8", "MaxGlobalBandwidth"));
                    break;
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference ConfigureIIs since nothing will happen without it
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsProperty", id);
                row.Set(1, componentId);
                row.Set(2, 0);
                row.Set(3, value);
            }
        }

        /// <summary>
        /// Parses a web service extension element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseWebServiceExtensionElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int attributes = 0;
            string description = null;
            string file = null;
            string group = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Allow":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 1;
                            }
                            else
                            {
                                attributes &= ~1;
                            }
                            break;
                        case "Description":
                            description = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            file = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Group":
                            group = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UIDeletable":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 2;
                            }
                            else
                            {
                                attributes &= ~2;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == file)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "File"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference ConfigureIIs since nothing will happen without it
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebServiceExtension", id);
                row.Set(1, componentId);
                row.Set(2, file);
                row.Set(3, description);
                row.Set(4, group);
                row.Set(5, attributes);
            }
        }

        /// <summary>
        /// Parses a web site element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Optional identifier of parent component.</param>
        private void ParseWebSiteElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string application = null;
            int attributes = 0;
            int connectionTimeout = CompilerConstants.IntegerNotSet;
            string description = null;
            string directory = null;
            string dirProperties = null;
            string keyAddress = null;
            string log = null;
            string siteId = null;
            int sequence = CompilerConstants.IntegerNotSet;
            int state = CompilerConstants.IntegerNotSet;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "AutoStart":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                state = 2;
                            }
                            else if (state != 1)
                            {
                                state = 0;
                            }
                            break;
                        case "ConfigureIfExists":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes &= ~2;
                            }
                            else
                            {
                                attributes |= 2;
                            }
                            break;
                        case "ConnectionTimeout":
                            connectionTimeout = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, short.MaxValue);
                            break;
                        case "Description":
                            description = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directory = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Directory", directory);
                            break;
                        case "DirProperties":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            dirProperties = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SiteId":
                            siteId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if ("*" == siteId)
                            {
                                siteId = "-1";
                            }
                            break;
                        case "Sequence":
                            sequence = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 1, short.MaxValue);
                            break;
                        case "StartOnInstall":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            // when state is set to 2 it implies 1, so don't set it to 1
                            if (2 != state && YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                state = 1;
                            }
                            else if (2 != state)
                            {
                                state = 0;
                            }
                            break;
                        case "WebApplication":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            application = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebLog":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, element.Name.LocalName, attrib.Name.LocalName));
                            }

                            log = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebLog", log);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == description)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Description"));
            }

            if (null == directory && null != componentId)
            {
                this.Messaging.Write(IIsErrors.RequiredAttributeUnderComponent(sourceLineNumbers, element.Name.LocalName, "Directory"));
            }

            foreach (XElement child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "CertificateRef":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseCertificateRefElement(intermediate, section, child, id?.Id);
                            break;
                        case "HttpHeader":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseHttpHeaderElement(intermediate, section, child, HttpHeaderParentType.WebSite, id?.Id);
                            break;
                        case "WebAddress":
                            string address = this.ParseWebAddressElement(intermediate, section, child, id?.Id);
                            if (null == keyAddress)
                            {
                                keyAddress = address;
                            }
                            break;
                        case "WebApplication":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            if (null != application)
                            {
                                this.Messaging.Write(IIsErrors.WebApplicationAlreadySpecified(childSourceLineNumbers, element.Name.LocalName));
                            }

                            application = this.ParseWebApplicationElement(intermediate, section, child);
                            break;
                        case "WebDir":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseWebDirElement(intermediate, section, child, componentId, id?.Id);
                            break;
                        case "WebDirProperties":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            string childWebDirProperties = this.ParseWebDirPropertiesElement(intermediate, section, child);
                            if (null == dirProperties)
                            {
                                dirProperties = childWebDirProperties;
                            }
                            else
                            {
                                this.Messaging.Write(ErrorMessages.IllegalParentAttributeWhenNested(sourceLineNumbers, "WebSite", "DirProperties", child.Name.LocalName));
                            }
                            break;
                        case "WebError":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseWebErrorElement(intermediate, section, child, WebErrorParentType.WebSite, id?.Id);
                            break;
                        case "WebFilter":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseWebFilterElement(intermediate, section, child, componentId, id?.Id);
                            break;
                        case "WebVirtualDir":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            this.ParseWebVirtualDirElement(intermediate, section, child, componentId, id?.Id, null);
                            break;
                        case "MimeMap":
                            this.ParseMimeMapElement(intermediate, section, child, id?.Id, MimeMapParentType.WebSite);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }


            if (null == keyAddress)
            {
                this.Messaging.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, element.Name.LocalName, "WebAddress"));
            }

            if (null != application)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebApplication", application);
            }

            if (null != dirProperties)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebDirProperties", dirProperties);
            }

            if (null != componentId)
            {
                // Reference ConfigureIIs since nothing will happen without it
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureIIs");
            }

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebSite", id);
                row.Set(1, componentId);
                row.Set(2, description);
                if (CompilerConstants.IntegerNotSet != connectionTimeout)
                {
                    row.Set(3, connectionTimeout);
                }
                row.Set(4, directory);
                if (CompilerConstants.IntegerNotSet != state)
                {
                    row.Set(5, state);
                }

                if (0 != attributes)
                {
                    row.Set(6, attributes);
                }
                row.Set(7, keyAddress);
                row.Set(8, dirProperties);
                row.Set(9, application);
                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    row.Set(10, sequence);
                }
                row.Set(11, log);
                row.Set(12, siteId);
            }
        }

        /// <summary>
        /// Parses a HTTP Header element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="parentType">Type of the parent.</param>
        /// <param name="parent">Id of the parent.</param>
        private void ParseHttpHeaderElement(Intermediate intermediate, IntermediateSection section, XElement element, HttpHeaderParentType parentType, string parent)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string headerName = null;
            string headerValue = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Name":
                            headerName = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Value":
                            headerValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == headerName)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }
            else if (null == id)
            {
                id = this.ParseHelper.CreateIdentifierFromFilename(headerName);
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference ConfigureIIs since nothing will happen without it
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureIIs");

            var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsHttpHeader", id);
            row.Set(1, (int)parentType);
            row.Set(2, parent);
            row.Set(3, headerName);
            row.Set(4, headerValue);
            row.Set(5, 0);
            //row.Set(6, null);
        }

        /// <summary>
        /// Parses a virtual directory element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier of parent component.</param>
        /// <param name="parentWeb">Identifier of parent web site.</param>
        /// <param name="parentAlias">Alias of the parent web site.</param>
        private void ParseWebVirtualDirElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string parentWeb, string parentAlias)
        {
            SourceLineNumber sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string alias = null;
            string application = null;
            string directory = null;
            string dirProperties = null;

            foreach (XAttribute attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Alias":
                            alias = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Directory":
                            directory = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Directory", directory);
                            break;
                        case "DirProperties":
                            dirProperties = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebApplication":
                            application = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "WebSite":
                            if (null != parentWeb)
                            {
                                this.Messaging.Write(IIsErrors.WebSiteAttributeUnderWebSite(sourceLineNumbers, element.Name.LocalName));
                            }

                            parentWeb = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebSite", parentWeb);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(element, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            if (null == id)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
            }

            if (null == alias)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Alias"));
            }
            else if (-1 != alias.IndexOf("\\", StringComparison.Ordinal))
            {
                this.Messaging.Write(IIsErrors.IllegalCharacterInAttributeValue(sourceLineNumbers, element.Name.LocalName, "Alias", alias, '\\'));
            }

            if (null == directory)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Directory"));
            }

            if (null == parentWeb)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "WebSite"));
            }

            if (null == componentId)
            {
                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(sourceLineNumbers, element.Name.LocalName));
            }

            if (null != parentAlias)
            {
                alias = String.Concat(parentAlias, "/", alias);
            }

            foreach (XElement child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    SourceLineNumber childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "WebApplication":
                            if (null != application)
                            {
                                this.Messaging.Write(IIsErrors.WebApplicationAlreadySpecified(childSourceLineNumbers, element.Name.LocalName));
                            }

                            application = this.ParseWebApplicationElement(intermediate, section, child);
                            break;
                        case "WebDirProperties":
                            if (null == componentId)
                            {
                                this.Messaging.Write(IIsErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }

                            string childWebDirProperties = this.ParseWebDirPropertiesElement(intermediate, section, child);
                            if (null == dirProperties)
                            {
                                dirProperties = childWebDirProperties;
                            }
                            else
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, child.Name.LocalName, "DirProperties", child.Name.LocalName));
                            }
                            break;

                        case "WebError":
                            this.ParseWebErrorElement(intermediate, section, child, WebErrorParentType.WebVirtualDir, id?.Id);
                            break;
                        case "WebVirtualDir":
                            this.ParseWebVirtualDirElement(intermediate, section, child, componentId, parentWeb, alias);
                            break;
                        case "HttpHeader":
                            this.ParseHttpHeaderElement(intermediate, section, child, HttpHeaderParentType.WebVirtualDir, id?.Id);
                            break;
                        case "MimeMap":
                            this.ParseMimeMapElement(intermediate, section, child, id?.Id, MimeMapParentType.WebVirtualDir);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(element, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, element, child);
                }
            }

            if (null != dirProperties)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebDirProperties", dirProperties);
            }

            if (null != application)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "IIsWebApplication", application);
            }

            // Reference ConfigureIIs since nothing will happen without it
            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureIIs");

            if (!this.Messaging.EncounteredError)
            {
                var row = this.ParseHelper.CreateRow(section, sourceLineNumbers, "IIsWebVirtualDir", id);
                row.Set(1, componentId);
                row.Set(2, parentWeb);
                row.Set(3, alias);
                row.Set(4, directory);
                row.Set(5, dirProperties);
                row.Set(6, application);
            }
        }
    }
}
