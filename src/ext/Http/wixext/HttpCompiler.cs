// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Http
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Http.Symbols;
    using static System.Net.Mime.MediaTypeNames;

    /// <summary>
    /// The compiler for the WiX Toolset Http Extension.
    /// </summary>
    public sealed class HttpCompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/http";

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
                case "ServiceInstall":
                    var serviceInstallName = context["ServiceInstallName"];
                    var serviceUser = String.IsNullOrEmpty(serviceInstallName) ? null : String.Concat("NT SERVICE\\", serviceInstallName);
                    var serviceComponentId = context["ServiceInstallComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "UrlReservation":
                            this.ParseUrlReservationElement(intermediate, section, element, serviceComponentId, serviceUser);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Component":
                    string componentId = context["ComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "SniSslCertificate":
                            this.ParseSniSslCertificateElement(intermediate, section, element, componentId);
                            break;

                        case "SslBinding":
                            this.ParseSslBindingElement(intermediate, section, element, componentId);
                            break;

                        case "Certificate":
                            this.ParseCertificateElement(intermediate, section, element, componentId);
                            break;

                        case "UrlReservation":
                            this.ParseUrlReservationElement(intermediate, section, element, componentId, null);
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
        /// Parses a SniSsl element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="componentId">Identifier of the component that owns this SNI SSL Certificate.</param>
        private void ParseSniSslCertificateElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            Identifier id = null;
            string host = null;
            string port = null;
            string appId = null;
            string store = null;
            string thumbprint = null;
            var handleExisting = HandleExisting.Replace;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "AppId":
                            appId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HandleExisting":
                            var handleExistingValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (handleExistingValue)
                            {
                                case "replace":
                                    handleExisting = HandleExisting.Replace;
                                    break;
                                case "ignore":
                                    handleExisting = HandleExisting.Ignore;
                                    break;
                                case "fail":
                                    handleExisting = HandleExisting.Fail;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "HandleExisting", handleExistingValue, "replace", "ignore", "fail"));
                                    break;
                            }
                            break;
                        case "Host":
                            host = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Port":
                            port = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Store":
                            store = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Thumbprint":
                            thumbprint = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, node, attrib);
                }
            }

            // Need the element ID for child element processing, so generate now if not authored.
            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("ssl", componentId, host, port);
            }

            // Required attributes.
            if (null == host)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Host"));
            }

            if (null == port)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Port"));
            }

            if (null == thumbprint)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Thumbprint"));
            }

            // Parse unknown children.
            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixHttpSniSslCertSymbol(sourceLineNumbers, id)
                {
                    Host = host,
                    Port = port,
                    Thumbprint = thumbprint,
                    AppId = appId,
                    Store = store,
                    HandleExisting = handleExisting,
                    ComponentRef = componentId,
                });

                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedHttpSniSslCertsInstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedHttpSniSslCertsUninstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            }
        }

        /// <summary>
        /// Parses a Ssl element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="componentId">Identifier of the component that owns this SSL Certificate.</param>
        private void ParseSslBindingElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            Identifier id = null;
            string host = null;
            string port = null;
            string appId = null;
            string store = null;
            string certificateRef = null;
            string thumbprint = null;
            var handleExisting = HandleExisting.Replace;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "AppId":
                            appId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "HandleExisting":
                            var handleExistingValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (handleExistingValue)
                            {
                                case "replace":
                                    handleExisting = HandleExisting.Replace;
                                    break;
                                case "ignore":
                                    handleExisting = HandleExisting.Ignore;
                                    break;
                                case "fail":
                                    handleExisting = HandleExisting.Fail;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "HandleExisting", handleExistingValue, "replace", "ignore", "fail"));
                                    break;
                            }
                            break;
                        case "Host":
                            host = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Port":
                            port = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Thumbprint":
                            thumbprint = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Store":
                            store = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, node, attrib);
                }
            }

            // Need the element ID for child element processing, so generate now if not authored.
            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("ssl", componentId, host, port);
            }

            // Required attributes.
            if (null == host)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Host"));
            }

            if (null == port)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Port"));
            }

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "CertificateRef":
                            if (null != thumbprint)
                            {
                                this.Messaging.Write(ErrorMessages.UnexpectedElementWithAttribute(childSourceLineNumbers, node.Name.LocalName, child.Name.LocalName, "Thumbprint"));
                            }

                            if (null == componentId)
                            {
                                this.Messaging.Write(HttpErrors.IllegalElementWithoutComponent(childSourceLineNumbers, child.Name.LocalName));
                            }
                            certificateRef = this.ParseCertificateRefElement(intermediate, section, child, id?.Id);

                            if (null == certificateRef)
                            {
                                this.Messaging.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "CertificateRef"));
                            }

                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, node, child);
                }
            }

            if (null == thumbprint && certificateRef == null)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Thumbprint"));
            }

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixHttpSslBindingSymbol(sourceLineNumbers, id)
                {
                    Host = host,
                    Port = port,
                    Thumbprint = thumbprint,
                    AppId = appId,
                    Store = store,
                    HandleExisting = handleExisting,
                    ComponentRef = componentId,
                });

                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedHttpSslBindingsInstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedHttpSslBindingsUninstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
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
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4InstallHttpCertificates", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4UninstallHttpCertificates", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            this.ParseHelper.EnsureTable(section, sourceLineNumbers, "Wix4HttpSslCertificateHash"); // Certificate CustomActions require the CertificateHash table

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixHttpCertificateSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = componentId,
                    Name = name,
                    StoreLocation = storeLocation,
                    StoreName = storeName,
                    Attributes = attributes,
                    BinaryRef = binaryRef,
                    CertificatePath = certificatePath,
                    PfxPassword = pfxPassword,
                });
            }
        }

        /// <summary>
        /// Parses a UrlReservation element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="componentId">Identifier of the component that owns this URL reservation.</param>
        /// <param name="securityPrincipal">The security principal of the parent element (null if nested under Component).</param>
        private void ParseUrlReservationElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentId, string securityPrincipal)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            Identifier id = null;
            var handleExisting = HandleExisting.Replace;
            string sddl = null;
            string url = null;
            var foundACE = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "HandleExisting":
                            var handleExistingValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (handleExistingValue)
                            {
                                case "replace":
                                    handleExisting = HandleExisting.Replace;
                                    break;
                                case "ignore":
                                    handleExisting = HandleExisting.Ignore;
                                    break;
                                case "fail":
                                    handleExisting = HandleExisting.Fail;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "HandleExisting", handleExistingValue, "replace", "ignore", "fail"));
                                    break;
                            }
                            break;
                        case "Sddl":
                            sddl = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Url":
                            url = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, node, attrib);
                }
            }

            // Need the element ID for child element processing, so generate now if not authored.
            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("url", componentId, securityPrincipal, url);
            }

            // Parse UrlAce children.
            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "UrlAce":
                            if (null != sddl)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalParentAttributeWhenNested(sourceLineNumbers, "UrlReservation", "Sddl", "UrlAce"));
                            }
                            else
                            {
                                foundACE = true;
                                this.ParseUrlAceElement(intermediate, section, child, id.Id, securityPrincipal);
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionElement(this.Context.Extensions, intermediate, section, node, child);
                }
            }

            // Url is required.
            if (null == url)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Url"));
            }

            // Security is required.
            if (null == sddl && !foundACE)
            {
                this.Messaging.Write(HttpErrors.NoSecuritySpecified(sourceLineNumbers));
            }

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixHttpUrlReservationSymbol(sourceLineNumbers, id)
                {
                    HandleExisting = handleExisting,
                    Sddl = sddl,
                    Url = url,
                    ComponentRef = componentId,
                });

                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedHttpUrlReservationsInstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix4SchedHttpUrlReservationsUninstall", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
            }
        }

        /// <summary>
        /// Parses a UrlAce element.
        /// </summary>
        /// <param name="node">The element to parse.</param>
        /// <param name="urlReservationId">The URL reservation ID.</param>
        /// <param name="defaultSecurityPrincipal">The default security principal.</param>
        private void ParseUrlAceElement(Intermediate intermediate, IntermediateSection section, XElement node, string urlReservationId, string defaultSecurityPrincipal)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            Identifier id = null;
            var securityPrincipal = defaultSecurityPrincipal;
            var rights = HttpConstants.GENERIC_ALL;
            string rightsValue = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "SecurityPrincipal":
                            securityPrincipal = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Rights":
                            rightsValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (rightsValue)
                            {
                                case "all":
                                    rights = HttpConstants.GENERIC_ALL;
                                    break;
                                case "delegate":
                                    rights = HttpConstants.GENERIC_WRITE;
                                    break;
                                case "register":
                                    rights = HttpConstants.GENERIC_EXECUTE;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Rights", rightsValue, "all", "delegate", "register"));
                                    break;
                            }
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, node, attrib);
                }
            }

            // Generate Id now if not authored.
            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("ace", urlReservationId, securityPrincipal, rightsValue);
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            // SecurityPrincipal is required.
            if (null == securityPrincipal)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "SecurityPrincipal"));
            }

            if (!this.Messaging.EncounteredError)
            {
                section.AddSymbol(new WixHttpUrlAceSymbol(sourceLineNumbers, id)
                {
                    WixHttpUrlReservationRef = urlReservationId,
                    SecurityPrincipal = securityPrincipal,
                    Rights = rights,
                });
            }
        }

        /// <summary>
        /// Parses a CertificateRef extension element.
        /// </summary>
        /// <param name="element">Element to parse.</param>
        /// <param name="bindingId">Identifier for parent Ssl binding.</param>
        private string ParseCertificateRefElement(Intermediate intermediate, IntermediateSection section, XElement element, string bindingId)
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
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, HttpSymbolDefinitions.WixHttpCertificate, id.Id);
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
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, HttpSymbolDefinitions.WixHttpCertificate, id.Id);

                section.AddSymbol(new WixHttpSslBindingCertificateSymbol(sourceLineNumbers)
                {
                    BindingRef = bindingId,
                    CertificateRef = id.Id,
                });
            }

            return id?.Id;
        }
    }
}
