// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Firewall
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;
    using WixToolset.Firewall.Tuples;

    /// <summary>
    /// The compiler for the WiX Toolset Firewall Extension.
    /// </summary>
    public sealed class FirewallCompiler : BaseCompilerExtension
    {
        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/firewall";

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
                            this.ParseFirewallExceptionElement(intermediate, section, element, fileComponentId, fileId);
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
                            this.ParseFirewallExceptionElement(intermediate, section, element, componentId, null);
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
        /// <param name="element">The element to parse.</param>
        /// <param name="componentId">Identifier of the component that owns this firewall exception.</param>
        /// <param name="fileId">The file identifier of the parent element (null if nested under Component).</param>
        private void ParseFirewallExceptionElement(Intermediate intermediate, IntermediateSection section, XElement element, string componentId, string fileId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);
            Identifier id = null;
            string name = null;
            int attributes = 0;
            string file = null;
            string program = null;
            string port = null;
            int? protocol = null;
            int? profile = null;
            string scope = null;
            string remoteAddresses = null;
            string description = null;

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
                            if (null != fileId)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, "File", "File"));
                            }
                            else
                            {
                                file = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            }
                            break;
                        case "IgnoreFailure":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= 0x1; // feaIgnoreFailures
                            }
                            break;
                        case "Program":
                            if (null != fileId)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, element.Name.LocalName, "Program", "File"));
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
                            var protocolValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (protocolValue)
                            {
                                case "tcp":
                                    protocol = FirewallConstants.NET_FW_IP_PROTOCOL_TCP;
                                    break;
                                case "udp":
                                    protocol = FirewallConstants.NET_FW_IP_PROTOCOL_UDP;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Protocol", protocolValue, "tcp", "udp"));
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
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Scope", scope, "any", "localSubnet"));
                                    break;
                            }
                            break;
                        case "Profile":
                            var profileValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (profileValue)
                            {
                                case "domain":
                                    profile = FirewallConstants.NET_FW_PROFILE2_DOMAIN;
                                    break;
                                case "private":
                                    profile = FirewallConstants.NET_FW_PROFILE2_PRIVATE;
                                    break;
                                case "public":
                                    profile = FirewallConstants.NET_FW_PROFILE2_PUBLIC;
                                    break;
                                case "all":
                                    profile = FirewallConstants.NET_FW_PROFILE2_ALL;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, element.Name.LocalName, "Profile", profileValue, "domain", "private", "public", "all"));
                                    break;
                            }
                            break;
                        case "Description":
                            description = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            // parse RemoteAddress children
            foreach (var child in element.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "RemoteAddress":
                            if (null != scope)
                            {
                                this.Messaging.Write(FirewallErrors.IllegalRemoteAddressWithScopeAttribute(sourceLineNumbers));
                            }
                            else
                            {
                                this.ParseRemoteAddressElement(intermediate, section, child, ref remoteAddresses);
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

            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("fex", name, remoteAddresses, componentId);
            }

            // Name is required
            if (null == name)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, element.Name.LocalName, "Name"));
            }

            // Scope or child RemoteAddress(es) are required
            if (null == remoteAddresses)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttributeOrElement(sourceLineNumbers, element.Name.LocalName, "Scope", "RemoteAddress"));
            }

            // can't have both Program and File
            if (null != program && null != file)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeWithOtherAttribute(sourceLineNumbers, element.Name.LocalName, "File", "Program"));
            }

            // must be nested under File, have File or Program attributes, or have Port attribute
            if (String.IsNullOrEmpty(fileId) && String.IsNullOrEmpty(file) && String.IsNullOrEmpty(program) && String.IsNullOrEmpty(port))
            {
                this.Messaging.Write(FirewallErrors.NoExceptionSpecified(sourceLineNumbers));
            }

            if (!this.Messaging.EncounteredError)
            {
                // at this point, File attribute and File parent element are treated the same
                if (null != file)
                {
                    fileId = file;
                }

                var tuple = section.AddTuple(new WixFirewallExceptionTuple(sourceLineNumbers, id)
                {
                    Name = name,
                    RemoteAddresses = remoteAddresses,
                    Profile = profile ?? FirewallConstants.NET_FW_PROFILE2_ALL,
                    ComponentRef = componentId,
                    Description = description,
                });

                if (!String.IsNullOrEmpty(port))
                {
                    tuple.Port = port;

                    if (!protocol.HasValue)
                    {
                        // default protocol is "TCP"
                        protocol = FirewallConstants.NET_FW_IP_PROTOCOL_TCP;
                    }
                }

                if (protocol.HasValue)
                {
                    tuple.Protocol = protocol.Value;
                }

                if (!String.IsNullOrEmpty(fileId))
                {
                    tuple.Program = $"[#{fileId}]";
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, TupleDefinitions.File, fileId);
                }
                else if (!String.IsNullOrEmpty(program))
                {
                    tuple.Program = program;
                }

                if (CompilerConstants.IntegerNotSet != attributes)
                {
                    tuple.Attributes = attributes;
                }

                if (this.Context.Platform == Platform.ARM)
                {
                    // Ensure ARM version of the CA is referenced
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, TupleDefinitions.CustomAction, "WixSchedFirewallExceptionsInstall_ARM");
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, TupleDefinitions.CustomAction, "WixSchedFirewallExceptionsUninstall_ARM");
                }
                else
                {
                    // All other supported platforms use x86
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, TupleDefinitions.CustomAction, "WixSchedFirewallExceptionsInstall");
                    this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, TupleDefinitions.CustomAction, "WixSchedFirewallExceptionsUninstall");
                }
            }
        }

        /// <summary>
        /// Parses a RemoteAddress element
        /// </summary>
        /// <param name="element">The element to parse.</param>
        private void ParseRemoteAddressElement(Intermediate intermediate, IntermediateSection section, XElement element, ref string remoteAddresses)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(element);

            // no attributes
            foreach (var attrib in element.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    this.ParseHelper.UnexpectedAttribute(element, attrib);
                }
                else
                {
                    this.ParseHelper.ParseExtensionAttribute(this.Context.Extensions, intermediate, section, element, attrib);
                }
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, element);

            var address = this.ParseHelper.GetTrimmedInnerText(element);
            if (String.IsNullOrEmpty(address))
            {
                this.Messaging.Write(FirewallErrors.IllegalEmptyRemoteAddress(sourceLineNumbers));
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
    }
}
