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
            string handleExistingValue = null;

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
                            handleExistingValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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
    }
}
