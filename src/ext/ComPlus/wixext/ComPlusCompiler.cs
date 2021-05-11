// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.ComPlus
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;
    using WixToolset.ComPlus.Symbols;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset COM+ Extension.
    /// </summary>
    public sealed class ComPlusCompiler : BaseCompilerExtension
    {
        /// <summary>
        /// </summary>
        /// <remarks></remarks>
        public enum CpiAssemblyAttributes
        {
            EventClass = (1 << 0),
            DotNetAssembly = (1 << 1),
            DllPathFromGAC = (1 << 2),
            RegisterInCommit = (1 << 3)
        }

        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/complus";

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
                    var win64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "ComPlusPartition":
                            this.ParseComPlusPartitionElement(intermediate, section, element, componentId, win64);
                            break;
                        case "ComPlusPartitionRole":
                            this.ParseComPlusPartitionRoleElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusUserInPartitionRole":
                            this.ParseComPlusUserInPartitionRoleElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusGroupInPartitionRole":
                            this.ParseComPlusGroupInPartitionRoleElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusPartitionUser":
                            this.ParseComPlusPartitionUserElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusApplication":
                            this.ParseComPlusApplicationElement(intermediate, section, element, componentId, win64, null);
                            break;
                        case "ComPlusApplicationRole":
                            this.ParseComPlusApplicationRoleElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusUserInApplicationRole":
                            this.ParseComPlusUserInApplicationRoleElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusGroupInApplicationRole":
                            this.ParseComPlusGroupInApplicationRoleElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusAssembly":
                            this.ParseComPlusAssemblyElement(intermediate, section, element, componentId, win64, null);
                            break;
                        case "ComPlusRoleForComponent":
                            this.ParseComPlusRoleForComponentElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusRoleForInterface":
                            this.ParseComPlusRoleForInterfaceElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusRoleForMethod":
                            this.ParseComPlusRoleForMethodElement(intermediate, section, element, componentId, null);
                            break;
                        case "ComPlusSubscription":
                            this.ParseComPlusSubscriptionElement(intermediate, section, element, componentId, null);
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
                        case "ComPlusPartition":
                            this.ParseComPlusPartitionElement(intermediate, section, element, null, false);
                            break;
                        case "ComPlusPartitionRole":
                            this.ParseComPlusPartitionRoleElement(intermediate, section, element, null, null);
                            break;
                        case "ComPlusApplication":
                            this.ParseComPlusApplicationElement(intermediate, section, element, null, false, null);
                            break;
                        case "ComPlusApplicationRole":
                            this.ParseComPlusApplicationRoleElement(intermediate, section, element, null, null);
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
        /// Parses a COM+ partition element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        private void ParseComPlusPartitionElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, bool win64)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string id = null;
            string name = null;

            var properties = new Dictionary<string, string>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "PartitionId":
                            id = this.TryFormatGuidValue(this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Changeable":
                            this.Messaging.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Deleteable":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Deleteable"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Description":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Description"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null != componentKey && null == name)
            {
                this.Messaging.Write(ComPlusErrors.RequiredAttributeUnderComponent(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            if (null == componentKey && null == id && null == name)
            {
                this.Messaging.Write(ComPlusErrors.RequiredAttributeNotUnderComponent(sourceLineNumbers, node.Name.LocalName, "Id", "Name"));
            }

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusPartitionRole":
                            this.ParseComPlusPartitionRoleElement(intermediate, section, child, componentKey, key?.Id);
                            break;
                        case "ComPlusPartitionUser":
                            this.ParseComPlusPartitionUserElement(intermediate, section, child, componentKey, key?.Id);
                            break;
                        case "ComPlusApplication":
                            this.ParseComPlusApplicationElement(intermediate, section, child, componentKey, win64, key?.Id);
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

            section.AddSymbol(new ComPlusPartitionSymbol(sourceLineNumbers, key)
            {
                ComponentRef = componentKey,
                PartitionId = id,
                Name = name,
            });

            foreach (var kvp in properties)
            {
                section.AddSymbol(new ComPlusPartitionPropertySymbol(sourceLineNumbers)
                {
                    PartitionRef = key?.Id,
                    Name = kvp.Key,
                    Value = kvp.Value,
                });
            }

            if (componentKey != null)
            {
                this.AddReferenceToConfigureComPlus(section, sourceLineNumbers, node.Name.LocalName, win64);
            }
        }

        /// <summary>
        /// Parses a COM+ partition role element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application.</param>
        private void ParseComPlusPartitionRoleElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string partitionKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string name = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Partition":
                            if (null != partitionKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusPartition, partitionKey);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == partitionKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Partition"));
            }

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusUserInPartitionRole":
                            this.ParseComPlusUserInPartitionRoleElement(intermediate, section, child, componentKey, key?.Id);
                            break;
                        case "ComPlusGroupInPartitionRole":
                            this.ParseComPlusGroupInPartitionRoleElement(intermediate, section, child, componentKey, key?.Id);
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

            section.AddSymbol(new ComPlusPartitionRoleSymbol(sourceLineNumbers, key)
            {
                PartitionRef = partitionKey,
                Name = name,
            });
        }

        /// <summary>
        /// Parses a COM+ partition role user element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusUserInPartitionRoleElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string partitionRoleKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string user = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "PartitionRole":
                            if (null != partitionRoleKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionRoleKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusPartitionRole, partitionRoleKey);
                            break;
                        case "User":
                            user = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "User", user);
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

            if (null == partitionRoleKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PartitionRole"));
            }

            section.AddSymbol(new ComPlusUserInPartitionRoleSymbol(sourceLineNumbers, key)
            {
                PartitionRoleRef = partitionRoleKey,
                ComponentRef = componentKey,
                UserRef = user,
            });
        }

        /// <summary>
        /// Parses a COM+ partition role user element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusGroupInPartitionRoleElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string partitionRoleKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string group = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "PartitionRole":
                            if (null != partitionRoleKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionRoleKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusPartitionRole, partitionRoleKey);
                            break;
                        case "Group":
                            group = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Group", group);
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

            if (null == partitionRoleKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PartitionRole"));
            }

            section.AddSymbol(new ComPlusGroupInPartitionRoleSymbol(sourceLineNumbers, key)
            {
                PartitionRoleRef = partitionRoleKey,
                ComponentRef = componentKey,
                GroupRef = group,
            });
        }

        /// <summary>
        /// Parses a COM+ partition element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        private void ParseComPlusPartitionUserElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string partitionKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string user = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Partition":
                            if (null != partitionKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusPartition, partitionKey);
                            break;
                        case "User":
                            user = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "User", user);
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

            if (null == partitionKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Partition"));
            }

            section.AddSymbol(new ComPlusPartitionUserSymbol(sourceLineNumbers, key)
            {
                PartitionRef = partitionKey,
                ComponentRef = componentKey,
                UserRef = user,
            });
        }

        /// <summary>
        /// Parses a COM+ application element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="partitionKey">Optional identifier of parent partition.</param>
        private void ParseComPlusApplicationElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, bool win64, string partitionKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string id = null;
            string name = null;

            var properties = new Dictionary<string, string>();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Partition":
                            if (null != partitionKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusPartition, partitionKey);
                            break;
                        case "ApplicationId":
                            id = this.TryFormatGuidValue(this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThreeGigSupportEnabled":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["3GigSupportEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "AccessChecksLevel":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            var accessChecksLevelValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (accessChecksLevelValue)
                            {
                                case "applicationLevel":
                                    properties["AccessChecksLevel"] = "0";
                                    break;
                                case "applicationComponentLevel":
                                    properties["AccessChecksLevel"] = "1";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "AccessChecksLevel", accessChecksLevelValue, "applicationLevel", "applicationComponentLevel"));
                                    break;
                            }
                            break;
                        case "Activation":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            var activationValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (activationValue)
                            {
                                case "inproc":
                                    properties["Activation"] = "Inproc";
                                    break;
                                case "local":
                                    properties["Activation"] = "Local";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "Activation", activationValue, "inproc", "local"));
                                    break;
                            }
                            break;
                        case "ApplicationAccessChecksEnabled":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["ApplicationAccessChecksEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ApplicationDirectory":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["ApplicationDirectory"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Authentication":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string authenticationValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (authenticationValue)
                            {
                                case "default":
                                    properties["Authentication"] = "0";
                                    break;
                                case "none":
                                    properties["Authentication"] = "1";
                                    break;
                                case "connect":
                                    properties["Authentication"] = "2";
                                    break;
                                case "call":
                                    properties["Authentication"] = "3";
                                    break;
                                case "packet":
                                    properties["Authentication"] = "4";
                                    break;
                                case "integrity":
                                    properties["Authentication"] = "5";
                                    break;
                                case "privacy":
                                    properties["Authentication"] = "6";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "Authentication", authenticationValue, "default", "none", "connect", "call", "packet", "integrity", "privacy"));
                                    break;
                            }
                            break;
                        case "AuthenticationCapability":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            var authenticationCapabilityValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (authenticationCapabilityValue)
                            {
                                case "none":
                                    properties["AuthenticationCapability"] = "0";
                                    break;
                                case "secureReference":
                                    properties["AuthenticationCapability"] = "2";
                                    break;
                                case "staticCloaking":
                                    properties["AuthenticationCapability"] = "32";
                                    break;
                                case "dynamicCloaking":
                                    properties["AuthenticationCapability"] = "64";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "AuthenticationCapability", authenticationCapabilityValue, "none", "secureReference", "staticCloaking", "dynamicCloaking"));
                                    break;
                            }
                            break;
                        case "Changeable":
                            this.Messaging.Write(WarningMessages.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "CommandLine":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["CommandLine"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ConcurrentApps":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["ConcurrentApps"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CreatedBy":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["CreatedBy"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CRMEnabled":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["CRMEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "CRMLogFile":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["CRMLogFile"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Deleteable":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Deleteable"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Description":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Description"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DumpEnabled":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["DumpEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "DumpOnException":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["DumpOnException"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "DumpOnFailfast":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["DumpOnFailfast"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "DumpPath":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["DumpPath"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EventsEnabled":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["EventsEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Identity":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Identity"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ImpersonationLevel":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string impersonationLevelValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (impersonationLevelValue)
                            {
                                case "anonymous":
                                    properties["ImpersonationLevel"] = "1";
                                    break;
                                case "identify":
                                    properties["ImpersonationLevel"] = "2";
                                    break;
                                case "impersonate":
                                    properties["ImpersonationLevel"] = "3";
                                    break;
                                case "delegate":
                                    properties["ImpersonationLevel"] = "4";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "ImpersonationLevel", impersonationLevelValue, "anonymous", "identify", "impersonate", "delegate"));
                                    break;
                            }
                            break;
                        case "IsEnabled":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["IsEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "MaxDumpCount":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["MaxDumpCount"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Password":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Password"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "QCAuthenticateMsgs":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string qcAuthenticateMsgsValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (qcAuthenticateMsgsValue)
                            {
                                case "secureApps":
                                    properties["QCAuthenticateMsgs"] = "0";
                                    break;
                                case "off":
                                    properties["QCAuthenticateMsgs"] = "1";
                                    break;
                                case "on":
                                    properties["QCAuthenticateMsgs"] = "2";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "QCAuthenticateMsgs", qcAuthenticateMsgsValue, "secureApps", "off", "on"));
                                    break;
                            }
                            break;
                        case "QCListenerMaxThreads":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["QCListenerMaxThreads"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "QueueListenerEnabled":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["QueueListenerEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "QueuingEnabled":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["QueuingEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "RecycleActivationLimit":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleActivationLimit"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RecycleCallLimit":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleCallLimit"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RecycleExpirationTimeout":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleExpirationTimeout"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RecycleLifetimeLimit":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleLifetimeLimit"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RecycleMemoryLimit":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleMemoryLimit"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Replicable":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Replicable"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "RunForever":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RunForever"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ShutdownAfter":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["ShutdownAfter"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SoapActivated":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SoapActivated"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "SoapBaseUrl":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SoapBaseUrl"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SoapMailTo":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SoapMailTo"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SoapVRoot":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SoapVRoot"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SRPEnabled":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SRPEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "SRPTrustLevel":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            var srpTrustLevelValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (srpTrustLevelValue)
                            {
                                case "disallowed":
                                    properties["SRPTrustLevel"] = "0";
                                    break;
                                case "fullyTrusted":
                                    properties["SRPTrustLevel"] = "262144";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "SRPTrustLevel", srpTrustLevelValue, "disallowed", "fullyTrusted"));
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

            if (null != componentKey && null == name)
            {
                this.Messaging.Write(ComPlusErrors.RequiredAttributeUnderComponent(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            if (null == componentKey && null == id && null == name)
            {
                this.Messaging.Write(ComPlusErrors.RequiredAttributeNotUnderComponent(sourceLineNumbers, node.Name.LocalName, "Id", "Name"));
            }

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusApplicationRole":
                            this.ParseComPlusApplicationRoleElement(intermediate, section, child, componentKey, key?.Id);
                            break;
                        case "ComPlusAssembly":
                            this.ParseComPlusAssemblyElement(intermediate, section, child, componentKey, win64, key?.Id);
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

            section.AddSymbol(new ComPlusApplicationSymbol(sourceLineNumbers, key)
            {
                PartitionRef = partitionKey,
                ComponentRef = componentKey,
                ApplicationId = id,
                Name = name,
            });

            foreach (var kvp in properties)
            {
                section.AddSymbol(new ComPlusApplicationPropertySymbol(sourceLineNumbers)
                {
                    ApplicationRef = key?.Id,
                    Name = kvp.Key,
                    Value = kvp.Value,
                });
            }

            if (componentKey != null)
            {
                this.AddReferenceToConfigureComPlus(section, sourceLineNumbers, node.Name.LocalName, win64);
            }
        }

        /// <summary>
        /// Parses a COM+ application role element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application.</param>
        private void ParseComPlusApplicationRoleElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string applicationKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string name = null;

            var properties = new Dictionary<string, string>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Application":
                            if (null != applicationKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            applicationKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusApplication, applicationKey);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            if (null == componentKey)
                            {
                                this.Messaging.Write(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Description"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == applicationKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Application"));
            }

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusUserInApplicationRole":
                            this.ParseComPlusUserInApplicationRoleElement(intermediate, section, child, componentKey, key?.Id);
                            break;
                        case "ComPlusGroupInApplicationRole":
                            this.ParseComPlusGroupInApplicationRoleElement(intermediate, section, child, componentKey, key?.Id);
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

            section.AddSymbol(new ComPlusApplicationRoleSymbol(sourceLineNumbers, key)
            {
                ApplicationRef = applicationKey,
                ComponentRef = componentKey,
                Name = name,
            });

            foreach (var kvp in properties)
            {
                section.AddSymbol(new ComPlusApplicationRolePropertySymbol(sourceLineNumbers)
                {
                    ApplicationRoleRef = key?.Id,
                    Name = kvp.Key,
                    Value = kvp.Value,
                });
            }
        }

        /// <summary>
        /// Parses a COM+ application role user element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusUserInApplicationRoleElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string applicationRoleKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string user = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ApplicationRole":
                            if (null != applicationRoleKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            applicationRoleKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusApplicationRole, applicationRoleKey);
                            break;
                        case "User":
                            user = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "User", user);
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

            if (null == applicationRoleKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ApplicationRole"));
            }

            section.AddSymbol(new ComPlusUserInApplicationRoleSymbol(sourceLineNumbers, key)
            {
                ApplicationRoleRef = applicationRoleKey,
                ComponentRef = componentKey,
                UserRef = user,
            });
        }

        /// <summary>
        /// Parses a COM+ application role group element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusGroupInApplicationRoleElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string applicationRoleKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string group = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ApplicationRole":
                            if (null != applicationRoleKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            applicationRoleKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusApplicationRole, applicationRoleKey);
                            break;
                        case "Group":
                            group = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "Group", group);
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

            if (null == applicationRoleKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ApplicationRole"));
            }

            section.AddSymbol(new ComPlusGroupInApplicationRoleSymbol(sourceLineNumbers, key)
            {
                ApplicationRoleRef = applicationRoleKey,
                ComponentRef = componentKey,
                GroupRef = group,
            });
        }

        /// <summary>
        /// Parses a COM+ assembly element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application.</param>
        private void ParseComPlusAssemblyElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, bool win64, string applicationKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string assemblyName = null;
            string dllPath = null;
            string tlbPath = null;
            string psDllPath = null;
            int attributes = 0;

            var hasComponents = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Application":
                            if (null != applicationKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            applicationKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusApplication, applicationKey);
                            break;
                        case "AssemblyName":
                            assemblyName = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DllPath":
                            dllPath = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "TlbPath":
                            tlbPath = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PSDllPath":
                            psDllPath = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            string typeValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (typeValue)
                            {
                                case ".net":
                                    attributes |= (int)CpiAssemblyAttributes.DotNetAssembly;
                                    break;
                                case "native":
                                    attributes &= ~(int)CpiAssemblyAttributes.DotNetAssembly;
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusAssembly", "Type", typeValue, ".net", "native"));
                                    break;
                            }
                            break;
                        case "EventClass":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)CpiAssemblyAttributes.EventClass;
                            }
                            else
                            {
                                attributes &= ~(int)CpiAssemblyAttributes.EventClass;
                            }
                            break;
                        case "DllPathFromGAC":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)CpiAssemblyAttributes.DllPathFromGAC;
                            }
                            else
                            {
                                attributes &= ~(int)CpiAssemblyAttributes.DllPathFromGAC;
                            }
                            break;
                        case "RegisterInCommit":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)CpiAssemblyAttributes.RegisterInCommit;
                            }
                            else
                            {
                                attributes &= ~(int)CpiAssemblyAttributes.RegisterInCommit;
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

            if (null == applicationKey && 0 == (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Application", "Type", "native"));
            }
            if (null != assemblyName && 0 == (attributes & (int)CpiAssemblyAttributes.DllPathFromGAC))
            {
                this.Messaging.Write(ComPlusErrors.UnexpectedAttributeWithoutOtherValue(sourceLineNumbers, node.Name.LocalName, "AssemblyName", "DllPathFromGAC", "no"));
            }
            if (null == tlbPath && 0 != (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "TlbPath", "Type", ".net"));
            }
            if (null != psDllPath && 0 != (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Messaging.Write(ComPlusErrors.UnexpectedAttributeWithOtherValue(sourceLineNumbers, node.Name.LocalName, "PSDllPath", "Type", ".net"));
            }
            if (0 != (attributes & (int)CpiAssemblyAttributes.EventClass) && 0 != (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Messaging.Write(ComPlusErrors.UnexpectedAttributeWithOtherValue(sourceLineNumbers, node.Name.LocalName, "EventClass", "yes", "Type", ".net"));
            }

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusAssemblyDependency":
                            this.ParseComPlusAssemblyDependencyElement(intermediate, section, child, key?.Id);
                            break;
                        case "ComPlusComponent":
                            this.ParseComPlusComponentElement(intermediate, section, child, componentKey, key?.Id);
                            hasComponents = true;
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

            if (0 == (attributes & (int)CpiAssemblyAttributes.DotNetAssembly) && !hasComponents)
            {
                this.Messaging.Write(ComPlusWarnings.MissingComponents(sourceLineNumbers));
            }

            section.AddSymbol(new ComPlusAssemblySymbol(sourceLineNumbers, key)
            {
                ApplicationRef = applicationKey,
                ComponentRef = componentKey,
                AssemblyName = assemblyName,
                DllPath = dllPath,
                TlbPath = tlbPath,
                PSDllPath = psDllPath,
                Attributes = attributes,
            });

            this.AddReferenceToConfigureComPlus(section, sourceLineNumbers, node.Name.LocalName, win64);
        }

        /// <summary>
        /// Parses a COM+ assembly dependency element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="assemblyKey">Identifier of parent assembly.</param>
        private void ParseComPlusAssemblyDependencyElement(Intermediate intermediate, IntermediateSection section, XElement node, string assemblyKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            string requiredAssemblyKey = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "RequiredAssembly":
                            requiredAssemblyKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            section.AddSymbol(new ComPlusAssemblyDependencySymbol(sourceLineNumbers)
            {
                AssemblyRef = assemblyKey,
                RequiredAssemblyRef = requiredAssemblyKey,
            });
        }

        /// <summary>
        /// Parses a COM+ component element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="assemblyKey">Identifier of parent assembly.</param>
        private void ParseComPlusComponentElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string assemblyKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string clsid = null;

            var properties = new Dictionary<string, string>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "CLSID":
                            clsid = "{" + this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib) + "}";
                            break;
                        case "AllowInprocSubscribers":
                            properties["AllowInprocSubscribers"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ComponentAccessChecksEnabled":
                            properties["ComponentAccessChecksEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ComponentTransactionTimeout":
                            properties["ComponentTransactionTimeout"] = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 3600).ToString();
                            break;
                        case "ComponentTransactionTimeoutEnabled":
                            properties["ComponentTransactionTimeoutEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "COMTIIntrinsics":
                            properties["COMTIIntrinsics"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ConstructionEnabled":
                            properties["ConstructionEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ConstructorString":
                            properties["ConstructorString"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CreationTimeout":
                            properties["CreationTimeout"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            properties["Description"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EventTrackingEnabled":
                            properties["EventTrackingEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ExceptionClass":
                            properties["ExceptionClass"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "FireInParallel":
                            properties["FireInParallel"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "IISIntrinsics":
                            properties["IISIntrinsics"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "InitializesServerApplication":
                            properties["InitializesServerApplication"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "IsEnabled":
                            properties["IsEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "IsPrivateComponent":
                            properties["IsPrivateComponent"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "JustInTimeActivation":
                            properties["JustInTimeActivation"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "LoadBalancingSupported":
                            properties["LoadBalancingSupported"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "MaxPoolSize":
                            properties["MaxPoolSize"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MinPoolSize":
                            properties["MinPoolSize"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MultiInterfacePublisherFilterCLSID":
                            properties["MultiInterfacePublisherFilterCLSID"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MustRunInClientContext":
                            properties["MustRunInClientContext"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "MustRunInDefaultContext":
                            properties["MustRunInDefaultContext"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ObjectPoolingEnabled":
                            properties["ObjectPoolingEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "PublisherID":
                            properties["PublisherID"] = this.TryFormatGuidValue(this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "SoapAssemblyName":
                            properties["SoapAssemblyName"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SoapTypeName":
                            properties["SoapTypeName"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Synchronization":
                            var synchronizationValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (synchronizationValue)
                            {
                                case "ignored":
                                    properties["Synchronization"] = "0";
                                    break;
                                case "none":
                                    properties["Synchronization"] = "1";
                                    break;
                                case "supported":
                                    properties["Synchronization"] = "2";
                                    break;
                                case "required":
                                    properties["Synchronization"] = "3";
                                    break;
                                case "requiresNew":
                                    properties["Synchronization"] = "4";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusComponent", "Synchronization", synchronizationValue, "ignored", "none", "supported", "required", "requiresNew"));
                                    break;
                            }
                            break;
                        case "Transaction":
                            var transactionValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (transactionValue)
                            {
                                case "ignored":
                                    properties["Transaction"] = "0";
                                    break;
                                case "none":
                                    properties["Transaction"] = "1";
                                    break;
                                case "supported":
                                    properties["Transaction"] = "2";
                                    break;
                                case "required":
                                    properties["Transaction"] = "3";
                                    break;
                                case "requiresNew":
                                    properties["Transaction"] = "4";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusComponent", "Transaction", transactionValue, "ignored", "none", "supported", "required", "requiresNew"));
                                    break;
                            }
                            break;
                        case "TxIsolationLevel":
                            var txIsolationLevelValue = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (txIsolationLevelValue)
                            {
                                case "any":
                                    properties["TxIsolationLevel"] = "0";
                                    break;
                                case "readUnCommitted":
                                    properties["TxIsolationLevel"] = "1";
                                    break;
                                case "readCommitted":
                                    properties["TxIsolationLevel"] = "2";
                                    break;
                                case "repeatableRead":
                                    properties["TxIsolationLevel"] = "3";
                                    break;
                                case "serializable":
                                    properties["TxIsolationLevel"] = "4";
                                    break;
                                default:
                                    this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, "ComPlusComponent", "TxIsolationLevel", txIsolationLevelValue, "any", "readUnCommitted", "readCommitted", "repeatableRead", "serializable"));
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

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusRoleForComponent":
                            this.ParseComPlusRoleForComponentElement(intermediate, section, child, componentKey, key?.Id);
                            break;
                        case "ComPlusInterface":
                            this.ParseComPlusInterfaceElement(intermediate, section, child, componentKey, key?.Id);
                            break;
                        case "ComPlusSubscription":
                            this.ParseComPlusSubscriptionElement(intermediate, section, child, componentKey, key?.Id);
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

            section.AddSymbol(new ComPlusComponentSymbol(sourceLineNumbers, key)
            {
                AssemblyRef = assemblyKey,
                CLSID = clsid,
            });

            foreach (var kvp in properties)
            {
                section.AddSymbol(new ComPlusComponentPropertySymbol(sourceLineNumbers)
                {
                    ComPlusComponentRef = key?.Id,
                    Name = kvp.Key,
                    Value = kvp.Value,
                });
            }
        }

        /// <summary>
        /// Parses a COM+ application role for component element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="cpcomponentKey">Identifier of parent COM+ component.</param>
        private void ParseComPlusRoleForComponentElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string cpcomponentKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string applicationRoleKey = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Component":
                            if (null != cpcomponentKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            cpcomponentKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusComponent, cpcomponentKey);
                            break;
                        case "ApplicationRole":
                            applicationRoleKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == cpcomponentKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Component"));
            }

            section.AddSymbol(new ComPlusRoleForComponentSymbol(sourceLineNumbers, key)
            {
                ComPlusComponentRef = cpcomponentKey,
                ApplicationRoleRef = applicationRoleKey,
                ComponentRef = componentKey,
            });
        }

        /// <summary>
        /// Parses a COM+ interface element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="cpcomponentKey">Identifier of parent COM+ component.</param>
        private void ParseComPlusInterfaceElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string cpcomponentKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            // parse attributes
            Identifier key = null;
            string iid = null;

            var properties = new Dictionary<string, string>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "IID":
                            iid = "{" + this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib) + "}";
                            break;
                        case "Description":
                            properties["Description"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "QueuingEnabled":
                            properties["QueuingEnabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
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

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusRoleForInterface":
                            this.ParseComPlusRoleForInterfaceElement(intermediate, section, child, componentKey, key?.Id);
                            break;
                        case "ComPlusMethod":
                            this.ParseComPlusMethodElement(intermediate, section, child, componentKey, key?.Id);
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

            section.AddSymbol(new ComPlusInterfaceSymbol(sourceLineNumbers, key)
            {
                ComPlusComponentRef = cpcomponentKey,
                IID = iid,
            });

            foreach (var kvp in properties)
            {
                section.AddSymbol(new ComPlusInterfacePropertySymbol(sourceLineNumbers)
                {
                    InterfaceRef = key?.Id,
                    Name = kvp.Key,
                    Value = kvp.Value,
                });
            }
        }

        /// <summary>
        /// Parses a COM+ application role for interface element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="interfaceKey">Identifier of parent interface.</param>
        private void ParseComPlusRoleForInterfaceElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string interfaceKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string applicationRoleKey = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Interface":
                            if (null != interfaceKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            interfaceKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusInterface, interfaceKey);
                            break;
                        case "ApplicationRole":
                            applicationRoleKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == interfaceKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Interface"));
            }

            section.AddSymbol(new ComPlusRoleForInterfaceSymbol(sourceLineNumbers, key)
            {
                InterfaceRef = interfaceKey,
                ApplicationRoleRef = applicationRoleKey,
                ComponentRef = componentKey,
            });
        }

        /// <summary>
        /// Parses a COM+ method element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="interfaceKey">Identifier of parent interface.</param>
        private void ParseComPlusMethodElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string interfaceKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            var index = CompilerConstants.IntegerNotSet;
            string name = null;

            var properties = new Dictionary<string, string>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Index":
                            index = this.ParseHelper.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "AutoComplete":
                            properties["AutoComplete"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Description":
                            properties["Description"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusRoleForMethod":
                            this.ParseComPlusRoleForMethodElement(intermediate, section, child, componentKey, key?.Id);
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

            if (CompilerConstants.IntegerNotSet == index && null == name)
            {
                this.Messaging.Write(ComPlusErrors.RequiredAttribute(sourceLineNumbers, node.Name.LocalName, "Index", "Name"));
            }

            var symbol = section.AddSymbol(new ComPlusMethodSymbol(sourceLineNumbers, key)
            {
                InterfaceRef = interfaceKey,
                Name = name,
            });

            if (CompilerConstants.IntegerNotSet != index)
            {
                symbol.Index = index;
            }

            foreach (var kvp in properties)
            {
                section.AddSymbol(new ComPlusMethodPropertySymbol(sourceLineNumbers)
                {
                    MethodRef = key?.Id,
                    Name = kvp.Key,
                    Value = kvp.Value,
                });
            }
        }

        /// <summary>
        /// Parses a COM+ application role for method element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="methodKey">Identifier of parent method.</param>
        private void ParseComPlusRoleForMethodElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string methodKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string applicationRoleKey = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Method":
                            if (null != methodKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            methodKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusMethod, methodKey);
                            break;
                        case "ApplicationRole":
                            applicationRoleKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == methodKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Method"));
            }

            section.AddSymbol(new ComPlusRoleForMethodSymbol(sourceLineNumbers, key)
            {
                MethodRef = methodKey,
                ApplicationRoleRef = applicationRoleKey,
                ComponentRef = componentKey,
            });
        }

        /// <summary>
        /// Parses a COM+ event subscription element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="cpcomponentKey">Identifier of parent COM+ component.</param>
        private void ParseComPlusSubscriptionElement(Intermediate intermediate, IntermediateSection section, XElement node, string componentKey, string cpcomponentKey)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);

            Identifier key = null;
            string id = null;
            string name = null;
            string eventCLSID = null;
            string publisherID = null;

            var properties = new Dictionary<string, string>();

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Component":
                            if (null != cpcomponentKey)
                            {
                                this.Messaging.Write(ErrorMessages.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            cpcomponentKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, ComPlusSymbolDefinitions.ComPlusComponent, cpcomponentKey);
                            break;
                        case "SubscriptionId":
                            id = this.TryFormatGuidValue(this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Name":
                            name = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EventCLSID":
                            eventCLSID = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PublisherID":
                            publisherID = this.TryFormatGuidValue(this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Description":
                            properties["Description"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Enabled":
                            properties["Enabled"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "EventClassPartitionID":
                            properties["EventClassPartitionID"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "FilterCriteria":
                            properties["FilterCriteria"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InterfaceID":
                            properties["InterfaceID"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MachineName":
                            properties["MachineName"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MethodName":
                            properties["MethodName"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PerUser":
                            properties["PerUser"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Queued":
                            properties["Queued"] = YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "SubscriberMoniker":
                            properties["SubscriberMoniker"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UserName":
                            properties["UserName"] = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == cpcomponentKey)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Component"));
            }

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            section.AddSymbol(new ComPlusSubscriptionSymbol(sourceLineNumbers, key)
            {
                Subscription = key?.Id,
                ComPlusComponentRef = cpcomponentKey,
                ComponentRef = componentKey,
                SubscriptionId = id,
                Name = name,
                EventCLSID = eventCLSID,
                PublisherID = publisherID,
            });

            foreach (var kvp in properties)
            {
                section.AddSymbol(new ComPlusSubscriptionPropertySymbol(sourceLineNumbers)
                {
                    SubscriptionRef = key?.Id,
                    Name = kvp.Key,
                    Value = kvp.Value,
                });
            }
        }

        /// <summary>
        /// Attempts to parse the input value as a GUID, and in case the value is a valid
        /// GUID returnes it in the format "{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}".
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        private string TryFormatGuidValue(string val)
        {
            if (!Guid.TryParse(val, out var guid))
            {
                return val;
            }
            return guid.ToString("B").ToUpper();
        }

        private void AddReferenceToConfigureComPlus(IntermediateSection section, SourceLineNumber sourceLineNumbers, string elementName, bool win64)
        {
            if (win64)
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall_x64");
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall_x64");
            }
            else
            {
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall");
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall");
            }

        }
    }
}
