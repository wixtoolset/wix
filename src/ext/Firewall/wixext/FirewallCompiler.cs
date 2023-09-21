// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Firewall.Symbols;

    /// <summary>
    /// The compiler for the WiX Toolset Firewall Extension.
    /// </summary>
    public sealed class FirewallCompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => FirewallConstants.Namespace;

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
                case "File":
                    var fileId = context["FileId"];
                    var fileComponentId = context["ComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "FirewallException":
                            this.ParseFirewallExceptionElement(intermediate, section, parentElement, element, fileComponentId, fileId, null);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Component":
                    var componentId = context["ComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "FirewallException":
                            this.ParseFirewallExceptionElement(intermediate, section, parentElement, element, componentId, null, null);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "ServiceConfig":
                    var serviceConfigName = context["ServiceConfigServiceName"];
                    var serviceConfigComponentId = context["ServiceConfigComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "FirewallException":
                            this.ParseFirewallExceptionElement(intermediate, section, parentElement, element, serviceConfigComponentId, null, serviceConfigName);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "ServiceInstall":
                    var serviceInstallName = context["ServiceInstallName"];
                    var serviceInstallComponentId = context["ServiceInstallComponentId"];

                    switch (element.Name.LocalName)
                    {
                        case "FirewallException":
                            this.ParseFirewallExceptionElement(intermediate, section, parentElement, element, serviceInstallComponentId, null, serviceInstallName);
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
        /// Parses a FirewallException element.
        /// </summary>
        /// <param name="parentElement">The parent element of the one being parsed.</param>
        /// <param name="element">The element to parse.</param>
        /// <param name="componentId">Identifier of the component that owns this firewall exception.</param>
        /// <param name="fileId">The file identifier of the parent element (null if nested under Component).</param>
        /// <param name="serviceName">The service name of the parent element (null if not nested under ServiceConfig or ServiceInstall).</param>
        private void ParseFirewallExceptionElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, string componentId, string fileId, string serviceName)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string name = null;
            int attributes = 0;
            string file = null;
            string program = null;
            string port = null;
            string protocol = null;
            string profile = null;
            string scope = null;
            string remoteAddresses = null;
            string description = null;
            int? direction = null;
            string protocolValue = null;
            string action = null;
            string edgeTraversal = null;
            string enabled = null;
            string grouping = null;
            string icmpTypesAndCodes = null;
            string interfaces = null;
            string interfaceValue = null;
            string interfaceTypes = null;
            string interfaceTypeValue = null;
            string localScope = null;
            string localAddresses = null;
            string remotePort = null;
            string service = null;
            string localAppPackageId = null;
            string localUserAuthorizedList = null;
            string localUserOwner = null;
            string remoteMachineAuthorizedList = null;
            string remoteUserAuthorizedList = null;
            string secureFlags = null;

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
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "File":
                            if (fileId != null)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, "File", parentElement.Name.LocalName));
                            }
                            else
                            {
                                file = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "IgnoreFailure":
                            if (this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) == YesNoType.Yes)
                            {
                                attributes |= 0x1; // feaIgnoreFailures
                            }
                            break;
                        case "OnUpdate":
                            var onupdate = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (onupdate)
                            {
                                case "DoNothing":
                                    attributes |= 0x2; // feaIgnoreUpdates
                                    break;
                                case "EnableOnly":
                                    attributes |= 0x4; // feaEnableOnUpdate
                                    break;

                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "OnUpdate", onupdate, "EnableOnly", "DoNothing"));
                                    break;
                            }
                            break;
                        case "Program":
                            if (fileId != null)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, "Program", parentElement.Name.LocalName));
                            }
                            else
                            {
                                program = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "Port":
                            port = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Protocol":
                            protocolValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (protocolValue)
                            {
                                case FirewallConstants.IntegerNotSetString:
                                    break;

                                case "tcp":
                                    protocol = FirewallConstants.NET_FW_IP_PROTOCOL_TCP.ToString();
                                    break;
                                case "udp":
                                    protocol = FirewallConstants.NET_FW_IP_PROTOCOL_UDP.ToString();
                                    break;

                                default:
                                    protocol = protocolValue;
                                    if (!this.ParseHelper.ContainsProperty(protocolValue))
                                    {
                                        if (!Int32.TryParse(protocolValue, out var parsedProtocol) || parsedProtocol > 255 || parsedProtocol < 0)
                                        {
                                            this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Protocol", protocolValue, "tcp", "udp", "0-255"));
                                        }
                                    }
                                    break;
                            }
                            break;
                        case "Scope":
                            scope = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (scope)
                            {
                                case "any":
                                    remoteAddresses = "*";
                                    break;
                                case "localSubnet":
                                    remoteAddresses = "LocalSubnet";
                                    break;
                                case "DNS":
                                    remoteAddresses = "dns";
                                    break;
                                case "DHCP":
                                    remoteAddresses = "dhcp";
                                    break;
                                case "WINS":
                                    remoteAddresses = "wins";
                                    break;
                                case "defaultGateway":
                                    remoteAddresses = "DefaultGateway";
                                    break;
                                default:
                                    remoteAddresses = scope;
                                    if (!this.ParseHelper.ContainsProperty(scope))
                                    {
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Scope", scope, "any", "localSubnet", "DNS", "DHCP", "WINS", "defaultGateway"));
                                    }
                                    break;
                            }
                            break;
                        case "Profile":
                            var profileValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (profileValue)
                            {
                                case "domain":
                                    profile = FirewallConstants.NET_FW_PROFILE2_DOMAIN.ToString();
                                    break;
                                case "private":
                                    profile = FirewallConstants.NET_FW_PROFILE2_PRIVATE.ToString();
                                    break;
                                case "public":
                                    profile = FirewallConstants.NET_FW_PROFILE2_PUBLIC.ToString();
                                    break;
                                case "all":
                                    profile = FirewallConstants.NET_FW_PROFILE2_ALL.ToString();
                                    break;
                                default:
                                    profile = profileValue;
                                    if (!this.ParseHelper.ContainsProperty(profileValue))
                                    {
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Profile", profileValue, "domain", "private", "public", "all"));
                                    }
                                    break;
                            }
                            break;
                        case "Description":
                            description = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Outbound":
                            direction = this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) == YesNoType.Yes
                                ? FirewallConstants.NET_FW_RULE_DIR_OUT
                                : FirewallConstants.NET_FW_RULE_DIR_IN;
                            break;
                        case "Action":
                            action = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (action)
                            {
                                case "Block":
                                    action = "0";
                                    break;
                                case "Allow":
                                    action = "1";
                                    break;

                                default:
                                    if (!this.ParseHelper.ContainsProperty(action))
                                    {
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Action", action, "Allow", "Block"));
                                    }
                                    break;
                            }
                            break;
                        case "EdgeTraversal":
                            edgeTraversal = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (edgeTraversal)
                            {
                                case "Deny":
                                    edgeTraversal = FirewallConstants.NET_FW_EDGE_TRAVERSAL_TYPE_DENY.ToString();
                                    break;
                                case "Allow":
                                    edgeTraversal = FirewallConstants.NET_FW_EDGE_TRAVERSAL_TYPE_ALLOW.ToString();
                                    break;
                                case "DeferToApp":
                                    attributes |= 0x8; // feaAddINetFwRule2
                                    edgeTraversal = FirewallConstants.NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_APP.ToString();
                                    break;
                                case "DeferToUser":
                                    attributes |= 0x8; // feaAddINetFwRule2
                                    edgeTraversal = FirewallConstants.NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER.ToString();
                                    break;

                                default:
                                    if (!this.ParseHelper.ContainsProperty(edgeTraversal))
                                    {
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "EdgeTraversal", edgeTraversal, "Allow", "DeferToApp", "DeferToUser", "Deny"));
                                    }
                                    break;
                            }
                            break;
                        case "Enabled":
                            enabled = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            if (!this.ParseHelper.ContainsProperty(enabled))
                            {
                                switch (this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                                {
                                    case YesNoType.Yes:
                                        enabled = "1";
                                        break;
                                    case YesNoType.No:
                                        enabled = "0";
                                        break;

                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalYesNoValue(sourceLineNumbers, element.Name.LocalName, "Enabled", enabled));
                                        break;
                                }
                            }
                            break;
                        case "Grouping":
                            grouping = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "IcmpTypesAndCodes":
                            icmpTypesAndCodes = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Interface":
                            interfaceValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            interfaces = interfaceValue;
                            break;
                        case "InterfaceType":
                            interfaceTypeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (interfaceTypeValue)
                            {
                                case "RemoteAccess":
                                case "Wireless":
                                case "Lan":
                                case "All":
                                    break;

                                default:
                                    if (!this.ParseHelper.ContainsProperty(interfaceTypeValue))
                                    {
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "InterfaceType", interfaceTypeValue, "RemoteAccess", "Wireless", "Lan", "All"));
                                    }
                                    break;
                            }
                            interfaceTypes = interfaceTypeValue;
                            break;
                        case "LocalScope":
                            localScope = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (localScope)
                            {
                                case "any":
                                    localAddresses = "*";
                                    break;
                                case "localSubnet":
                                    localAddresses = "LocalSubnet";
                                    break;
                                case "DNS":
                                    localAddresses = "dns";
                                    break;
                                case "DHCP":
                                    localAddresses = "dhcp";
                                    break;
                                case "WINS":
                                    localAddresses = "wins";
                                    break;
                                case "defaultGateway":
                                    localAddresses = "DefaultGateway";
                                    break;

                                default:
                                    if (!this.ParseHelper.ContainsProperty(localScope))
                                    {
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "LocalScope", localScope, "any", "localSubnet", "DNS", "DHCP", "WINS", "defaultGateway"));
                                    }
                                    else
                                    {
                                        localAddresses = localScope;
                                    }
                                    break;
                            }
                            break;
                        case "RemotePort":
                            remotePort = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Service":
                            if (serviceName != null)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, "Service", parentElement.Name.LocalName));
                            }
                            else
                            {
                                service = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "LocalAppPackageId":
                            localAppPackageId = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes |= 0x10; // feaAddINetFwRule3
                            break;
                        case "LocalUserAuthorizedList":
                            localUserAuthorizedList = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes |= 0x10; // feaAddINetFwRule3
                            break;
                        case "LocalUserOwner":
                            localUserOwner = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes |= 0x10; // feaAddINetFwRule3
                            break;
                        case "RemoteMachineAuthorizedList":
                            remoteMachineAuthorizedList = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes |= 0x10; // feaAddINetFwRule3
                            break;
                        case "RemoteUserAuthorizedList":
                            remoteUserAuthorizedList = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes |= 0x10; // feaAddINetFwRule3
                            break;
                        case "IPSecSecureFlags":
                            secureFlags = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            attributes |= 0x10; // feaAddINetFwRule3
                            if (!this.ParseHelper.ContainsProperty(secureFlags))
                            {
                                switch (secureFlags)
                                {
                                    case "None":
                                        secureFlags = "0";
                                        break;
                                    case "NoEncapsulation":
                                        secureFlags = "1";
                                        break;
                                    case "WithIntegrity":
                                        secureFlags = "2";
                                        break;
                                    case "NegotiateEncryption":
                                        secureFlags = "3";
                                        break;
                                    case "Encrypt":
                                        secureFlags = "4";
                                        break;
                                    default:
                                        this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "IPSecSecureFlags", secureFlags, "None", "NoEncapsulation", "WithIntegrity", "NegotiateEncryption", "Encrypt"));
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

            // parse children
            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "RemoteAddress":
                            if (scope != null)
                            {
                                this.Messaging.Write(FirewallErrors.IllegalRemoteAddressWithScopeAttribute(sourceLineNumbers));
                            }
                            else
                            {
                                this.ParseRemoteAddressElement(intermediate, section, child, ref remoteAddresses);
                            }
                            break;
                        case "Interface":
                            if (interfaceValue != null)
                            {
                                this.Messaging.Write(FirewallErrors.IllegalInterfaceWithInterfaceAttribute(sourceLineNumbers));
                            }
                            else
                            {
                                this.ParseInterfaceElement(intermediate, section, child, ref interfaces);
                            }
                            break;
                        case "InterfaceType":
                            if (interfaceTypeValue != null)
                            {
                                this.Messaging.Write(FirewallErrors.IllegalInterfaceTypeWithInterfaceTypeAttribute(sourceLineNumbers));
                            }
                            else
                            {
                                this.ParseInterfaceTypeElement(intermediate, section, child, ref interfaceTypes);
                            }
                            break;
                        case "LocalAddress":
                            if (localScope != null)
                            {
                                this.Messaging.Write(FirewallErrors.IllegalLocalAddressWithLocalScopeAttribute(sourceLineNumbers));
                            }
                            else
                            {
                                this.ParseLocalAddressElement(intermediate, section, child, ref localAddresses);
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

            if (id == null)
            {
                // firewall rule names are meant to be unique
                id = this.ParseHelper.CreateIdentifier("fex", name, componentId);
            }

            // Name is required
            if (name == null)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            if (service == null)
            {
                service = serviceName;
            }

            // can't have both Program and File
            if (program != null && file != null)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "File", "Program"));
            }

            // Defer to user edge traversal setting can only be used in a firewall rule where program path and TCP/UDP protocol are specified with no additional conditions.
            if (edgeTraversal == FirewallConstants.NET_FW_EDGE_TRAVERSAL_TYPE_DEFER_TO_USER.ToString())
            {
                if (protocol != null && !(protocol == FirewallConstants.NET_FW_IP_PROTOCOL_TCP.ToString() || protocol == FirewallConstants.NET_FW_IP_PROTOCOL_UDP.ToString()))
                {
                    this.Messaging.Write(ErrorMessages.IllegalAttributeValueWithLegalList(sourceLineNumbers, element.Name.LocalName, "Protocol", protocolValue, "tcp,udp"));
                }

                if (String.IsNullOrEmpty(fileId) && String.IsNullOrEmpty(file) && String.IsNullOrEmpty(program))
                {
                    this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Program", "EdgeTraversal", "DeferToUser"));
                }

                if (port != null)
                {
                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "Port", "EdgeTraversal", "DeferToUser"));
                }

                if (remotePort != null)
                {
                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "RemotePort", "EdgeTraversal", "DeferToUser"));
                }

                if (localScope != null)
                {
                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "LocalScope", "EdgeTraversal", "DeferToUser"));
                }

                if (scope != null)
                {
                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "Scope", "EdgeTraversal", "DeferToUser"));
                }

                if (profile != null)
                {
                    this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "Profile", "EdgeTraversal", "DeferToUser"));
                }

                if (service != null)
                {
                    if (serviceName != null)
                    {
                        this.Messaging.Write(ErrorMessages.IllegalAttributeValueWhenNested(sourceLineNumbers, element.Name.LocalName, "EdgeTraversal", "DeferToUser", parentElement.Name.LocalName));
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "Service", "EdgeTraversal", "DeferToUser"));
                    }
                }
            }

            if (!this.Messaging.EncounteredError)
            {
                // at this point, File attribute and File parent element are treated the same
                if (file != null)
                {
                    fileId = file;
                }

                var symbol = section.AddSymbol(new WixFirewallExceptionSymbol(sourceLineNumbers, id)
                {
                    Name = name,
                    RemoteAddresses = remoteAddresses,
                    ComponentRef = componentId,
                    Description = description,
                    Direction = direction ?? FirewallConstants.NET_FW_RULE_DIR_IN,
                    Action = action ?? FirewallConstants.IntegerNotSetString,
                    EdgeTraversal = edgeTraversal ?? FirewallConstants.IntegerNotSetString,
                    Enabled = enabled ?? FirewallConstants.IntegerNotSetString,
                    Grouping = grouping,
                    IcmpTypesAndCodes = icmpTypesAndCodes,
                    Interfaces = interfaces,
                    InterfaceTypes = interfaceTypes,
                    LocalAddresses = localAddresses,
                    Port = port,
                    Profile = profile ?? FirewallConstants.IntegerNotSetString,
                    Protocol = protocol ?? FirewallConstants.IntegerNotSetString,
                    RemotePort = remotePort,
                    ServiceName = service,
                    LocalAppPackageId = localAppPackageId,
                    LocalUserAuthorizedList = localUserAuthorizedList,
                    LocalUserOwner = localUserOwner,
                    RemoteMachineAuthorizedList = remoteMachineAuthorizedList,
                    RemoteUserAuthorizedList = remoteUserAuthorizedList,
                    SecureFlags = secureFlags ?? FirewallConstants.IntegerNotSetString,
                });

                if (String.IsNullOrEmpty(protocol))
                {
                    if (!String.IsNullOrEmpty(port) || !String.IsNullOrEmpty(remotePort))
                    {
                        symbol.Protocol = FirewallConstants.NET_FW_IP_PROTOCOL_TCP.ToString();
                    }
                }

                if (!String.IsNullOrEmpty(fileId))
                {
                    symbol.Program = $"[#{fileId}]";
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.File, fileId);
                }
                else if (!String.IsNullOrEmpty(program))
                {
                    symbol.Program = program;
                }

                if (attributes != CompilerConstants.IntegerNotSet)
                {
                    symbol.Attributes = attributes;
                }

                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix5SchedFirewallExceptionsInstall", this.Context.Platform, CustomActionPlatforms.ARM64 | CustomActionPlatforms.X64 | CustomActionPlatforms.X86);
                this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "Wix5SchedFirewallExceptionsUninstall", this.Context.Platform, CustomActionPlatforms.ARM64 | CustomActionPlatforms.X64 | CustomActionPlatforms.X86);
            }
        }

        /// <summary>
        /// Parses a RemoteAddress element
        /// </summary>
        /// <param name="element">The element to parse.</param>
        private void ParseRemoteAddressElement(Intermediate intermediate, IntermediateSection section, XElement element, ref string remoteAddresses)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string address = null;

            // no attributes
            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Value":
                            address = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (String.IsNullOrEmpty(address))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Value"));
            }
            else
            {
                if (String.IsNullOrEmpty(remoteAddresses))
                {
                    remoteAddresses = address;
                }
                else
                {
                    remoteAddresses = String.Concat(remoteAddresses, ",", address);
                }
            }
        }

        /// <summary>
        /// Parses an Interface element
        /// </summary>
        /// <param name="element">The element to parse.</param>
        private void ParseInterfaceElement(Intermediate intermediate, IntermediateSection section, XElement element, ref string interfaces)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string name = null;

            // no attributes
            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (String.IsNullOrEmpty(name))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }
            else
            {
                if (String.IsNullOrEmpty(interfaces))
                {
                    interfaces = name;
                }
                else
                {
                    interfaces = String.Concat(interfaces, FirewallConstants.FORBIDDEN_FIREWALL_CHAR, name);
                }
            }
        }

        /// <summary>
        /// Parses an InterfaceType element
        /// </summary>
        /// <param name="element">The element to parse.</param>
        private void ParseInterfaceTypeElement(Intermediate intermediate, IntermediateSection section, XElement element, ref string interfaceTypes)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string value = null;

            // no attributes
            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Value":
                            value = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (String.IsNullOrEmpty(value))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Value"));
            }
            else
            {
                switch (value)
                {
                    case "RemoteAccess":
                    case "Wireless":
                    case "Lan":
                    case "All":
                        break;

                    default:
                        if (!this.ParseHelper.ContainsProperty(value))
                        {
                            this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Value", value, "RemoteAccess", "Wireless", "Lan", "All"));
                            value = null;
                        }
                        break;
                }

                if (String.IsNullOrEmpty(interfaceTypes))
                {
                    interfaceTypes = value;
                }
                else if (interfaceTypes.Contains("All"))
                {
                    if (value != "All")
                    {
                        this.Messaging.Write(FirewallErrors.IllegalInterfaceTypeWithInterfaceTypeAll(sourceLineNumbers));
                    }
                }
                else if(!String.IsNullOrEmpty(value))
                {
                    interfaceTypes = String.Concat(interfaceTypes, ",", value);
                }
            }
        }

        /// <summary>
        /// Parses a RemoteAddress element
        /// </summary>
        /// <param name="element">The element to parse.</param>
        private void ParseLocalAddressElement(Intermediate intermediate, IntermediateSection section, XElement element, ref string localAddresses)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            string address = null;

            // no attributes
            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Value":
                            address = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                    }
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            if (String.IsNullOrEmpty(address))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Value"));
            }
            else
            {
                if (String.IsNullOrEmpty(localAddresses))
                {
                    localAddresses = address;
                }
                else
                {
                    localAddresses = String.Concat(localAddresses, ",", address);
                }
            }
        }
    }
}
