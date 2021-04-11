// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Iis.Symbols;

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
                    var componentId = context["ComponentId"];
                    var directoryId = context["DirectoryId"];

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
                case "Package":
                    switch (element.Name.LocalName)
                    {
                        case "WebApplication":
                            this.ParseWebApplicationElement(intermediate, section, element);
                            break;
                        case "WebAppPool":
                            this.ParseWebAppPoolElement(intermediate, section, element, null);
                            break;
                        case "WebDirProperties":
                            this.ParseWebDirPropertiesElement(intermediate, section, element, null);
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int attributes = 8; // SCA_CERT_ATTRIBUTE_VITAL
            string binaryRef = null;
            string certificatePath = null;
            string name = null;
            string pfxPassword = null;
            int storeLocation = 0;
            string storeName = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "BinaryRef":
                            attributes |= 2; // SCA_CERT_ATTRIBUTE_BINARYDATA
                            binaryRef = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Binary, binaryRef);
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
                            var storeLocationValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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
                            var storeNameValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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
                        case "Vital":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 8; // SCA_CERT_ATTRIBUTE_VITAL
                            }
                            else
                            {
                                attributes &= ~8; // SCA_CERT_ATTRIBUTE_VITAL
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
                id = this.ParseHelper.CreateIdentifier("crt", componentId, binaryRef, certificatePath);
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

            if (null != binaryRef && null != certificatePath)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "BinaryRef", "CertificatePath", certificatePath));
            }
            else if (null == binaryRef && null == certificatePath)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttributes(sourceLineNumbers, element.Name.LocalName, "BinaryRef", "CertificatePath"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference InstallCertificates and UninstallCertificates since nothing will happen without them
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4InstallCertificates", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4UninstallCertificates", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.EnsureTable(section, sourceLineNumbers, IisTableDefinitions.CertificateHash); // Certificate CustomActions require the CertificateHash table

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new CertificateSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Name = name,
                    StoreLocation = storeLocation,
                    StoreName = storeName,
                    Attributes = attributes,
                    BinaryRef = binaryRef,
                    CertificatePath = certificatePath,
                    PFXPassword = pfxPassword,
                });
            }
        }

        /// <summary>
        /// Parses a CertificateRef extension element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="webId">Identifier for parent web site.</param>
        private void ParseCertificateRefElement(Intermediate intermediate, IntermediateSection section, XElement element, string webId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.Certificate, id.Id);
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
                id = this.ParseHelper.CreateIdentifier("wsc", webId);
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.Certificate, id.Id);

                section.AddSymbol(new IIsWebSiteCertificatesSymbol(sourceLineNumbers)
                {
                    WebRef = webId,
                    CertificateRef = id.Id,
                });
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string extension = null;
            string type = null;

            foreach (var attrib in element.Attributes())
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
                id = this.ParseHelper.CreateIdentifier("imm", parentId, type, extension);
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
                section.AddSymbol(new IIsMimeMapSymbol(sourceLineNumbers, id)
                {
                    ParentType = (int)parentType,
                    ParentValue = parentId,
                    MimeType = type,
                    Extension = extension,
                });
            }
        }

        /// <summary>
        /// Parses a recycle time element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <returns>Recycle time value.</returns>
        private string ParseRecycleTimeElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string value = null;

            foreach (var attrib in element.Attributes())
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string header = null;
            string ip = null;
            string port = null;
            var secure = false;

            foreach (var attrib in element.Attributes())
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
                id = this.ParseHelper.CreateIdentifier("iwa", parentWeb, ip, port);
            }

            if (null == port)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Port"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new IIsWebAddressSymbol(sourceLineNumbers, id)
                {
                    WebRef = parentWeb,
                    IP = ip,
                    Port = port,
                    Header = header,
                    Secure = secure ? 1 : 0,
                });
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            var allowSessions = YesNoDefaultType.Default;
            string appPool = null;
            var buffer = YesNoDefaultType.Default;
            var clientDebugging = YesNoDefaultType.Default;
            string defaultScript = null;
            int isolation = 0;
            string name = null;
            var parentPaths = YesNoDefaultType.Default;
            var scriptTimeout = CompilerConstants.IntegerNotSet;
            var sessionTimeout = CompilerConstants.IntegerNotSet;
            var serverDebugging = YesNoDefaultType.Default;

            foreach (var attrib in element.Attributes())
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
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsAppPool, appPool);
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
                id = this.ParseHelper.CreateIdentifier("wap", name, appPool);
            }

            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }
            else if (-1 != name.IndexOf("\\", StringComparison.Ordinal))
            {
                this.Messaging.Write(IIsErrors.IllegalCharacterInAttributeValue(sourceLineNumbers, element.Name.LocalName, "Name", name, '\\'));
            }

            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
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
                var symbol = section.AddSymbol(new IIsWebApplicationSymbol(sourceLineNumbers, id)
                {
                    Name = name,
                    Isolation = isolation,
                    DefaultScript = defaultScript,
                    AppPoolRef = appPool,
                });

                if (YesNoDefaultType.Default != allowSessions)
                {
                    symbol.AllowSessions = YesNoDefaultType.Yes == allowSessions ? 1 : 0;
                }

                if (CompilerConstants.IntegerNotSet != sessionTimeout)
                {
                    symbol.SessionTimeout = sessionTimeout;
                }

                if (YesNoDefaultType.Default != buffer)
                {
                    symbol.Buffer = YesNoDefaultType.Yes == buffer ? 1 : 0;
                }

                if (YesNoDefaultType.Default != parentPaths)
                {
                    symbol.ParentPaths = YesNoDefaultType.Yes == parentPaths ? 1 : 0;
                }

                if (CompilerConstants.IntegerNotSet != scriptTimeout)
                {
                    symbol.ScriptTimeout = scriptTimeout;
                }

                if (YesNoDefaultType.Default != serverDebugging)
                {
                    symbol.ServerDebugging = YesNoDefaultType.Yes == serverDebugging ? 1 : 0;
                }

                if (YesNoDefaultType.Default != clientDebugging)
                {
                    symbol.ClientDebugging = YesNoDefaultType.Yes == clientDebugging ? 1 : 0;
                }
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            int attributes = 0;
            string executable = null;
            string extension = null;
            string verbs = null;

            foreach (var attrib in element.Attributes())
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
                var symbol = section.AddSymbol(new IIsWebApplicationExtensionSymbol(sourceLineNumbers)
                {
                    ApplicationRef = application,
                    Extension = extension,
                    Verbs = verbs,
                    Executable = executable,
                });

                if (0 < attributes)
                {
                    symbol.Attributes = attributes;
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int attributes = 0;
            var cpuAction = CompilerConstants.IntegerNotSet;
            string cpuMon = null;
            var idleTimeout = CompilerConstants.IntegerNotSet;
            int maxCpuUsage = 0;
            var maxWorkerProcs = CompilerConstants.IntegerNotSet;
            string managedRuntimeVersion = null;
            string managedPipelineMode = null;
            string name = null;
            var privateMemory = CompilerConstants.IntegerNotSet;
            var queueLimit = CompilerConstants.IntegerNotSet;
            var recycleMinutes = CompilerConstants.IntegerNotSet;
            var recycleRequests = CompilerConstants.IntegerNotSet;
            string recycleTimes = null;
            var refreshCpu = CompilerConstants.IntegerNotSet;
            string user = null;
            var virtualMemory = CompilerConstants.IntegerNotSet;

            foreach (var attrib in element.Attributes())
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

                            var cpuActionValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

                            var identityValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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
                id = this.ParseHelper.CreateIdentifier("iap", name, componentId, user);
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

            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "RecycleTime":
                            if (null == componentId)
                            {
                                var childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
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
                this.AddReferenceToConfigureIIs(section, sourceLineNumbers);
            }

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new IIsAppPoolSymbol(sourceLineNumbers, id)
                {
                    Name = name,
                    ComponentRef = componentId,
                    Attributes = attributes,
                    UserRef = user,
                    RecycleTimes = recycleTimes,
                    CPUMon = cpuMon,
                    ManagedRuntimeVersion = managedRuntimeVersion,
                    ManagedPipelineMode = managedPipelineMode,
                });

                if (CompilerConstants.IntegerNotSet != recycleMinutes)
                {
                    symbol.RecycleMinutes = recycleMinutes;
                }

                if (CompilerConstants.IntegerNotSet != recycleRequests)
                {
                    symbol.RecycleRequests = recycleRequests;
                }

                if (CompilerConstants.IntegerNotSet != idleTimeout)
                {
                    symbol.IdleTimeout = idleTimeout;
                }

                if (CompilerConstants.IntegerNotSet != queueLimit)
                {
                    symbol.QueueLimit = queueLimit;
                }

                if (CompilerConstants.IntegerNotSet != maxWorkerProcs)
                {
                    symbol.MaxProc = maxWorkerProcs;
                }

                if (CompilerConstants.IntegerNotSet != virtualMemory)
                {
                    symbol.VirtualMemory = virtualMemory;
                }

                if (CompilerConstants.IntegerNotSet != privateMemory)
                {
                    symbol.PrivateMemory = privateMemory;
                }
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string dirProperties = null;
            string path = null;
            string application = null;

            foreach (var attrib in element.Attributes())
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
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebSite, parentWeb);
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
                id = this.ParseHelper.CreateIdentifier("iwd", componentId, parentWeb, dirProperties, application);
            }

            if (null == path)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Path"));
            }

            if (null == parentWeb)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "WebSite"));
            }

            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
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

                            string childWebDirProperties = this.ParseWebDirPropertiesElement(intermediate, section, child, componentId);
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
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebApplication, application);
            }

            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebDirProperties, dirProperties);

            // Reference ConfigureIIs since nothing will happen without it
            this.AddReferenceToConfigureIIs(section, sourceLineNumbers);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new IIsWebDirSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    WebRef = parentWeb,
                    Path = path,
                    DirPropertiesRef = dirProperties,
                    ApplicationRef = application,
                });
            }
        }

        /// <summary>
        /// Parses a web directory properties element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <returns>The identifier for this WebDirProperties.</returns>
        private string ParseWebDirPropertiesElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            int access = 0;
            var accessSet = false;
            int accessSSLFlags = 0;
            var accessSSLFlagsSet = false;
            string anonymousUser = null;
            var aspDetailedError = YesNoType.NotSet;
            string authenticationProviders = null;
            int authorization = 0;
            var authorizationSet = false;
            string cacheControlCustom = null;
            var cacheControlMaxAge = CompilerConstants.LongNotSet;
            string defaultDocuments = null;
            string httpExpires = null;
            var iisControlledPassword = false;
            var index = YesNoType.NotSet;
            var logVisits = YesNoType.NotSet;
            var notCustomError = YesNoType.NotSet;

            foreach (var attrib in element.Attributes())
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
                if (null == componentId)
                {
                    this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Id"));
                    id = Identifier.Invalid;
                }
                else
                {
                    id = this.ParseHelper.CreateIdentifier("wdp", componentId);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new IIsWebDirPropertiesSymbol(sourceLineNumbers, id)
                {
                    AnonymousUserRef = anonymousUser,
                    IIsControlledPassword = iisControlledPassword ? 1 : 0,
                    DefaultDoc = defaultDocuments,
                    HttpExpires = httpExpires,
                    CacheControlCustom = cacheControlCustom,
                });

                if (accessSet)
                {
                    symbol.Access = access;
                }

                if (authorizationSet)
                {
                    symbol.Authorization = authorization;
                }

                if (YesNoType.NotSet != logVisits)
                {
                    symbol.LogVisits = YesNoType.Yes == logVisits ? 1 : 0;
                }

                if (YesNoType.NotSet != index)
                {
                    symbol.Index = YesNoType.Yes == index ? 1 : 0;
                }

                if (YesNoType.NotSet != aspDetailedError)
                {
                    symbol.AspDetailedError = YesNoType.Yes == aspDetailedError ? 1 : 0;
                }

                if (CompilerConstants.LongNotSet != cacheControlMaxAge)
                {
                    symbol.CacheControlMaxAge = unchecked((int)cacheControlMaxAge);
                }

                if (YesNoType.NotSet != notCustomError)
                {
                    symbol.NoCustomError = YesNoType.Yes == notCustomError ? 1 : 0;
                }

                if (accessSSLFlagsSet)
                {
                    symbol.AccessSSLFlags = accessSSLFlags;
                }

                if (null != authenticationProviders)
                {
                    symbol.AuthenticationProviders = authenticationProviders;
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            var errorCode = CompilerConstants.IntegerNotSet;
            string file = null;
            string url = null;
            var subCode = CompilerConstants.IntegerNotSet;

            foreach (var attrib in element.Attributes())
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
            this.AddReferenceToConfigureIIs(section, sourceLineNumbers);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new IIsWebErrorSymbol(sourceLineNumbers)
                {
                    ErrorCode = errorCode,
                    SubCode = subCode,
                    ParentType = (int)parentType,
                    ParentValue = parent,
                    File = file,
                    URL = url,
                });
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string description = null;
            int flags = 0;
            var loadOrder = CompilerConstants.IntegerNotSet;
            string name = null;
            string path = null;

            foreach (var attrib in element.Attributes())
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
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebSite, parentWeb);
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
                id = this.ParseHelper.CreateIdentifier("ifl", name, componentId, path, parentWeb);
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
            this.AddReferenceToConfigureIIs(section, sourceLineNumbers);

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new IIsFilterSymbol(sourceLineNumbers, id)
                {
                    Name = name,
                    ComponentRef = componentId,
                    Path = path,
                    WebRef = parentWeb,
                    Description = description,
                    Flags = flags,
                });

                if (CompilerConstants.IntegerNotSet != loadOrder)
                {
                    symbol.LoadOrder = loadOrder;
                }
            }
        }

        /// <summary>
        /// Parses web log element.
        /// </summary>
        /// <param name="element">Node to be parsed.</param>
        private void ParseWebLogElement(Intermediate intermediate, IntermediateSection section, XElement element)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string type = null;

            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            var typeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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
                section.AddSymbol(new IIsWebLogSymbol(sourceLineNumbers, id)
                {
                    Format = type,
                });
            }
        }

        /// <summary>
        /// Parses a web property element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseWebPropertyElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string value = null;

            foreach (var attrib in element.Attributes())
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
            this.AddReferenceToConfigureIIs(section, sourceLineNumbers);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new IIsPropertySymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Attributes = 0,
                    Value = value,
                });
            }
        }

        /// <summary>
        /// Parses a web service extension element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Identifier for parent component.</param>
        private void ParseWebServiceExtensionElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
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
                id = this.ParseHelper.CreateIdentifier("iwe", componentId, file);
            }

            if (null == file)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "File"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            // Reference ConfigureIIs since nothing will happen without it
            this.AddReferenceToConfigureIIs(section, sourceLineNumbers);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new IIsWebServiceExtensionSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    File = file,
                    Description = description,
                    Group = group,
                    Attributes = attributes,
                });
            }
        }

        /// <summary>
        /// Parses a web site element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="componentId">Optional identifier of parent component.</param>
        private void ParseWebSiteElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string application = null;
            int attributes = 0;
            var connectionTimeout = CompilerConstants.IntegerNotSet;
            string description = null;
            string directory = null;
            string dirProperties = null;
            string keyAddress = null;
            string log = null;
            string siteId = null;
            var sequence = CompilerConstants.IntegerNotSet;
            var state = CompilerConstants.IntegerNotSet;

            foreach (var attrib in element.Attributes())
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
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Directory, directory);
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
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebLog, log);
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
                id = this.ParseHelper.CreateIdentifier("iws", description, componentId, siteId, application);
            }

            if (null == description)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Description"));
            }

            if (null == directory && null != componentId)
            {
                this.Messaging.Write(IIsErrors.RequiredAttributeUnderComponent(sourceLineNumbers, element.Name.LocalName, "Directory"));
            }

            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
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

                            string childWebDirProperties = this.ParseWebDirPropertiesElement(intermediate, section, child, componentId);
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
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebApplication, application);
            }

            if (null != dirProperties)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebDirProperties, dirProperties);
            }

            if (null != componentId)
            {
                // Reference ConfigureIIs since nothing will happen without it
                this.AddReferenceToConfigureIIs(section, sourceLineNumbers);
            }

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new IIsWebSiteSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Description = description,
                    DirectoryRef = directory,
                    KeyAddressRef = keyAddress,
                    DirPropertiesRef = dirProperties,
                    ApplicationRef = application,
                    LogRef = log,
                    WebsiteId = siteId,
                });

                if (CompilerConstants.IntegerNotSet != connectionTimeout)
                {
                    symbol.ConnectionTimeout = connectionTimeout;
                }

                if (CompilerConstants.IntegerNotSet != state)
                {
                    symbol.State = state;
                }

                if (0 != attributes)
                {
                    symbol.Attributes = attributes;
                }

                if (CompilerConstants.IntegerNotSet != sequence)
                {
                    symbol.Sequence = sequence;
                }
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string headerName = null;
            string headerValue = null;

            foreach (var attrib in element.Attributes())
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
            this.AddReferenceToConfigureIIs(section, sourceLineNumbers);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new IIsHttpHeaderSymbol(sourceLineNumbers, id)
                {
                    HttpHeader = id.Id,
                    ParentType = (int)parentType,
                    ParentValue = parent,
                    Name = headerName,
                    Value = headerValue,
                    Attributes = 0,
                });
            }
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
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string alias = null;
            string application = null;
            string directory = null;
            string dirProperties = null;

            foreach (var attrib in element.Attributes())
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
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Directory, directory);
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
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebSite, parentWeb);
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
                id = this.ParseHelper.CreateIdentifier("wvd", alias, directory, dirProperties, application, parentWeb);
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

            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
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

                            string childWebDirProperties = this.ParseWebDirPropertiesElement(intermediate, section, child, componentId);
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
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebDirProperties, dirProperties);
            }

            if (null != application)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, IisSymbolDefinitions.IIsWebApplication, application);
            }

            // Reference ConfigureIIs since nothing will happen without it
            this.AddReferenceToConfigureIIs(section, sourceLineNumbers);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new IIsWebVirtualDirSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    WebRef = parentWeb,
                    Alias = alias,
                    DirectoryRef = directory,
                    DirPropertiesRef = dirProperties,
                    ApplicationRef = application,
                });
            }
        }

        private void AddReferenceToConfigureIIs(IntermediateSection section, SourceLineNumber sourceLineNumbers)
        {
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4ConfigureIIs", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
        }
    }
}
