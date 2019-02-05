// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Linq;
    using System.Xml.Schema;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX Toolset Internet Information Services Extension.
    /// </summary>
    public sealed class ComPlusCompiler : CompilerExtension
    {
        /// <summary>
        /// Instantiate a new ComPlusCompiler.
        /// </summary>
        public ComPlusCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/complus";
        }

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

        /// <summary>
        /// Processes an element for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        public override void ParseElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    string componentId = context["ComponentId"];
                    string directoryId = context["DirectoryId"];
                    bool win64 = Boolean.Parse(context["Win64"]);

                    switch (element.Name.LocalName)
                    {
                        case "ComPlusPartition":
                            this.ParseComPlusPartitionElement(element, componentId, win64);
                            break;
                        case "ComPlusPartitionRole":
                            this.ParseComPlusPartitionRoleElement(element, componentId, null);
                            break;
                        case "ComPlusUserInPartitionRole":
                            this.ParseComPlusUserInPartitionRoleElement(element, componentId, null);
                            break;
                        case "ComPlusGroupInPartitionRole":
                            this.ParseComPlusGroupInPartitionRoleElement(element, componentId, null);
                            break;
                        case "ComPlusPartitionUser":
                            this.ParseComPlusPartitionUserElement(element, componentId, null);
                            break;
                        case "ComPlusApplication":
                            this.ParseComPlusApplicationElement(element, componentId, win64, null);
                            break;
                        case "ComPlusApplicationRole":
                            this.ParseComPlusApplicationRoleElement(element, componentId, null);
                            break;
                        case "ComPlusUserInApplicationRole":
                            this.ParseComPlusUserInApplicationRoleElement(element, componentId, null);
                            break;
                        case "ComPlusGroupInApplicationRole":
                            this.ParseComPlusGroupInApplicationRoleElement(element, componentId, null);
                            break;
                        case "ComPlusAssembly":
                            this.ParseComPlusAssemblyElement(element, componentId, win64, null);
                            break;
                        case "ComPlusRoleForComponent":
                            this.ParseComPlusRoleForComponentElement(element, componentId, null);
                            break;
                        case "ComPlusRoleForInterface":
                            this.ParseComPlusRoleForInterfaceElement(element, componentId, null);
                            break;
                        case "ComPlusRoleForMethod":
                            this.ParseComPlusRoleForMethodElement(element, componentId, null);
                            break;
                        case "ComPlusSubscription":
                            this.ParseComPlusSubscriptionElement(element, componentId, null);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.Name.LocalName)
                    {
                        case "ComPlusPartition":
                            this.ParseComPlusPartitionElement(element, null, false);
                            break;
                        case "ComPlusPartitionRole":
                            this.ParseComPlusPartitionRoleElement(element, null, null);
                            break;
                        case "ComPlusApplication":
                            this.ParseComPlusApplicationElement(element, null, false, null);
                            break;
                        case "ComPlusApplicationRole":
                            this.ParseComPlusApplicationRoleElement(element, null, null);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                default:
                    this.Core.UnexpectedElement(parentElement, element);
                    break;
            }
        }

        /// <summary>
        /// Parses a COM+ partition element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        private void ParseComPlusPartitionElement(XElement node, string componentKey, bool win64)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string id = null;
            string name = null;

            Hashtable properties = new Hashtable();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "PartitionId":
                            id = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Changeable":
                            this.Core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "Deleteable":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Deleteable"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Description":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != componentKey && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttributeUnderComponent(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            if (null == componentKey && null == id && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttributeNotUnderComponent(sourceLineNumbers, node.Name.LocalName, "Id", "Name"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusPartitionRole":
                            this.ParseComPlusPartitionRoleElement(child, componentKey, key);
                            break;
                        case "ComPlusPartitionUser":
                            this.ParseComPlusPartitionUserElement(child, componentKey, key);
                            break;
                        case "ComPlusApplication":
                            this.ParseComPlusApplicationElement(child, componentKey, win64, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusPartition");
            row[0] = key;
            row[1] = componentKey;
            row[2] = id;
            row[3] = name;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusPartitionProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }

            if (componentKey != null)
            {
                if (win64)
                {
                    if (this.Core.CurrentPlatform == Platform.IA64)
                    {
                        this.Core.OnMessage(WixErrors.UnsupportedPlatformForElement(sourceLineNumbers, "ia64", node.Name.LocalName));
                    }
                    else
                    {
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall_x64");
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall_x64");
                    }
                }
                else
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall");
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall");
                }
            }
        }

        /// <summary>
        /// Parses a COM+ partition role element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application.</param>
        private void ParseComPlusPartitionRoleElement(XElement node, string componentKey, string partitionKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string name = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Partition":
                            if (null != partitionKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusPartition", partitionKey);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == partitionKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Partition"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusUserInPartitionRole":
                            this.ParseComPlusUserInPartitionRoleElement(child, componentKey, key);
                            break;
                        case "ComPlusGroupInPartitionRole":
                            this.ParseComPlusGroupInPartitionRoleElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            // add table row
            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusPartitionRole");
            row[0] = key;
            row[1] = partitionKey;
            row[3] = name;
        }

        /// <summary>
        /// Parses a COM+ partition role user element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusUserInPartitionRoleElement(XElement node, string componentKey, string partitionRoleKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string user = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "PartitionRole":
                            if (null != partitionRoleKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusPartitionRole", partitionRoleKey);
                            break;
                        case "User":
                            user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "User", user);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == partitionRoleKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PartitionRole"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusUserInPartitionRole");
            row[0] = key;
            row[1] = partitionRoleKey;
            row[2] = componentKey;
            row[3] = user;
        }

        /// <summary>
        /// Parses a COM+ partition role user element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusGroupInPartitionRoleElement(XElement node, string componentKey, string partitionRoleKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string group = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "PartitionRole":
                            if (null != partitionRoleKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusPartitionRole", partitionRoleKey);
                            break;
                        case "Group":
                            group = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Group", group);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == partitionRoleKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "PartitionRole"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusGroupInPartitionRole");
            row[0] = key;
            row[1] = partitionRoleKey;
            row[2] = componentKey;
            row[3] = group;
        }

        /// <summary>
        /// Parses a COM+ partition element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        private void ParseComPlusPartitionUserElement(XElement node, string componentKey, string partitionKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string user = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Partition":
                            if (null != partitionKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusPartition", partitionKey);
                            break;
                        case "User":
                            user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "User", user);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == partitionKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Partition"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusPartitionUser");
            row[0] = key;
            row[1] = partitionKey;
            row[2] = componentKey;
            row[3] = user;
        }

        /// <summary>
        /// Parses a COM+ application element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="partitionKey">Optional identifier of parent partition.</param>
        private void ParseComPlusApplicationElement(XElement node, string componentKey, bool win64, string partitionKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string id = null;
            string name = null;

            Hashtable properties = new Hashtable();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Partition":
                            if (null != partitionKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            partitionKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusPartition", partitionKey);
                            break;
                        case "ApplicationId":
                            id = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ThreeGigSupportEnabled":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["3GigSupportEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "AccessChecksLevel":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string accessChecksLevelValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (accessChecksLevelValue)
                            {
                                case "applicationLevel":
                                    properties["AccessChecksLevel"] = "0";
                                    break;
                                case "applicationComponentLevel":
                                    properties["AccessChecksLevel"] = "1";
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "AccessChecksLevel", accessChecksLevelValue, "applicationLevel", "applicationComponentLevel"));
                                    break;
                            }
                            break;
                        case "Activation":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string activationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (activationValue)
                            {
                                case "inproc":
                                    properties["Activation"] = "Inproc";
                                    break;
                                case "local":
                                    properties["Activation"] = "Local";
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "Activation", activationValue, "inproc", "local"));
                                    break;
                            }
                            break;
                        case "ApplicationAccessChecksEnabled":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["ApplicationAccessChecksEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ApplicationDirectory":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["ApplicationDirectory"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Authentication":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string authenticationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "Authentication", authenticationValue, "default", "none", "connect", "call", "packet", "integrity", "privacy"));
                                    break;
                            }
                            break;
                        case "AuthenticationCapability":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string authenticationCapabilityValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "AuthenticationCapability", authenticationCapabilityValue, "none", "secureReference", "staticCloaking", "dynamicCloaking"));
                                    break;
                            }
                            break;
                        case "Changeable":
                            this.Core.OnMessage(WixWarnings.DeprecatedAttribute(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            break;
                        case "CommandLine":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["CommandLine"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ConcurrentApps":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["ConcurrentApps"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CreatedBy":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["CreatedBy"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CRMEnabled":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["CRMEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "CRMLogFile":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["CRMLogFile"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Deleteable":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Deleteable"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Description":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DumpEnabled":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["DumpEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "DumpOnException":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["DumpOnException"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "DumpOnFailfast":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["DumpOnFailfast"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "DumpPath":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["DumpPath"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EventsEnabled":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["EventsEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Identity":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Identity"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "ImpersonationLevel":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string impersonationLevelValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "ImpersonationLevel", impersonationLevelValue, "anonymous", "identify", "impersonate", "delegate"));
                                    break;
                            }
                            break;
                        case "IsEnabled":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["IsEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "MaxDumpCount":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["MaxDumpCount"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Password":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Password"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "QCAuthenticateMsgs":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string qcAuthenticateMsgsValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "QCAuthenticateMsgs", qcAuthenticateMsgsValue, "secureApps", "off", "on"));
                                    break;
                            }
                            break;
                        case "QCListenerMaxThreads":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["QCListenerMaxThreads"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "QueueListenerEnabled":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["QueueListenerEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "QueuingEnabled":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["QueuingEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "RecycleActivationLimit":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleActivationLimit"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RecycleCallLimit":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleCallLimit"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RecycleExpirationTimeout":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleExpirationTimeout"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RecycleLifetimeLimit":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleLifetimeLimit"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "RecycleMemoryLimit":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RecycleMemoryLimit"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Replicable":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Replicable"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "RunForever":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["RunForever"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ShutdownAfter":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["ShutdownAfter"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SoapActivated":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SoapActivated"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "SoapBaseUrl":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SoapBaseUrl"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SoapMailTo":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SoapMailTo"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SoapVRoot":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SoapVRoot"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SRPEnabled":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["SRPEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "SRPTrustLevel":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            string srpTrustLevelValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (srpTrustLevelValue)
                            {
                                case "disallowed":
                                    properties["SRPTrustLevel"] = "0";
                                    break;
                                case "fullyTrusted":
                                    properties["SRPTrustLevel"] = "262144";
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusApplication", "SRPTrustLevel", srpTrustLevelValue, "disallowed", "fullyTrusted"));
                                    break;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null != componentKey && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttributeUnderComponent(sourceLineNumbers, node.Name.LocalName, "Name"));
            }
            if (null == componentKey && null == id && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttributeNotUnderComponent(sourceLineNumbers, node.Name.LocalName, "Id", "Name"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusApplicationRole":
                            this.ParseComPlusApplicationRoleElement(child, componentKey, key);
                            break;
                        case "ComPlusAssembly":
                            this.ParseComPlusAssemblyElement(child, componentKey, win64, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusApplication");
            row[0] = key;
            row[1] = partitionKey;
            row[2] = componentKey;
            row[3] = id;
            row[4] = name;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusApplicationProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }

            if (componentKey != null)
            {
                if (win64)
                {
                    if (this.Core.CurrentPlatform == Platform.IA64)
                    {
                        this.Core.OnMessage(WixErrors.UnsupportedPlatformForElement(sourceLineNumbers, "ia64", node.Name.LocalName));
                    }
                    else
                    {
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall_x64");
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall_x64");
                    }
                }
                else
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall");
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall");
                }
            }
        }

        /// <summary>
        /// Parses a COM+ application role element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application.</param>
        private void ParseComPlusApplicationRoleElement(XElement node, string componentKey, string applicationKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string name = null;

            Hashtable properties = new Hashtable();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Application":
                            if (null != applicationKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            applicationKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusApplication", applicationKey);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            if (null == componentKey)
                            {
                                this.Core.OnMessage(ComPlusErrors.IllegalAttributeWithoutComponent(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName));
                            }
                            properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == applicationKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Application"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusUserInApplicationRole":
                            this.ParseComPlusUserInApplicationRoleElement(child, componentKey, key);
                            break;
                        case "ComPlusGroupInApplicationRole":
                            this.ParseComPlusGroupInApplicationRoleElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusApplicationRole");
            row[0] = key;
            row[1] = applicationKey;
            row[2] = componentKey;
            row[3] = name;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusApplicationRoleProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        /// <summary>
        /// Parses a COM+ application role user element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusUserInApplicationRoleElement(XElement node, string componentKey, string applicationRoleKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string user = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ApplicationRole":
                            if (null != applicationRoleKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusApplicationRole", applicationRoleKey);
                            break;
                        case "User":
                            user = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "User", user);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == applicationRoleKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ApplicationRole"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusUserInApplicationRole");
            row[0] = key;
            row[1] = applicationRoleKey;
            row[2] = componentKey;
            row[3] = user;
        }

        /// <summary>
        /// Parses a COM+ application role group element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application role.</param>
        private void ParseComPlusGroupInApplicationRoleElement(XElement node, string componentKey, string applicationRoleKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string group = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "ApplicationRole":
                            if (null != applicationRoleKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusApplicationRole", applicationRoleKey);
                            break;
                        case "Group":
                            group = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "Group", group);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == applicationRoleKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ApplicationRole"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusGroupInApplicationRole");
            row[0] = key;
            row[1] = applicationRoleKey;
            row[2] = componentKey;
            row[3] = group;
        }

        /// <summary>
        /// Parses a COM+ assembly element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="applicationKey">Optional identifier of parent application.</param>
        private void ParseComPlusAssemblyElement(XElement node, string componentKey, bool win64, string applicationKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string assemblyName = null;
            string dllPath = null;
            string tlbPath = null;
            string psDllPath = null;
            int attributes = 0;

            bool hasComponents = false;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Application":
                            if (null != applicationKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            applicationKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusApplication", applicationKey);
                            break;
                        case "AssemblyName":
                            assemblyName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "DllPath":
                            dllPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "TlbPath":
                            tlbPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PSDllPath":
                            psDllPath = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Type":
                            string typeValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            switch (typeValue)
                            {
                                case ".net":
                                    attributes |= (int)CpiAssemblyAttributes.DotNetAssembly;
                                    break;
                                case "native":
                                    attributes &= ~(int)CpiAssemblyAttributes.DotNetAssembly;
                                    break;
                                default:
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusAssembly", "Type", typeValue, ".net", "native"));
                                    break;
                            }
                            break;
                        case "EventClass":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)CpiAssemblyAttributes.EventClass;
                            }
                            else
                            {
                                attributes &= ~(int)CpiAssemblyAttributes.EventClass;
                            }
                            break;
                        case "DllPathFromGAC":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)CpiAssemblyAttributes.DllPathFromGAC;
                            }
                            else
                            {
                                attributes &= ~(int)CpiAssemblyAttributes.DllPathFromGAC;
                            }
                            break;
                        case "RegisterInCommit":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= (int)CpiAssemblyAttributes.RegisterInCommit;
                            }
                            else
                            {
                                attributes &= ~(int)CpiAssemblyAttributes.RegisterInCommit;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == applicationKey && 0 == (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Application", "Type", "native"));
            }
            if (null != assemblyName && 0 == (attributes & (int)CpiAssemblyAttributes.DllPathFromGAC))
            {
                this.Core.OnMessage(ComPlusErrors.UnexpectedAttributeWithoutOtherValue(sourceLineNumbers, node.Name.LocalName, "AssemblyName", "DllPathFromGAC", "no"));
            }
            if (null == tlbPath && 0 != (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "TlbPath", "Type", ".net"));
            }
            if (null != psDllPath && 0 != (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Core.OnMessage(ComPlusErrors.UnexpectedAttributeWithOtherValue(sourceLineNumbers, node.Name.LocalName, "PSDllPath", "Type", ".net"));
            }
            if (0 != (attributes & (int)CpiAssemblyAttributes.EventClass) && 0 != (attributes & (int)CpiAssemblyAttributes.DotNetAssembly))
            {
                this.Core.OnMessage(ComPlusErrors.UnexpectedAttributeWithOtherValue(sourceLineNumbers, node.Name.LocalName, "EventClass", "yes", "Type", ".net"));
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusAssemblyDependency":
                            this.ParseComPlusAssemblyDependencyElement(child, key);
                            break;
                        case "ComPlusComponent":
                            this.ParseComPlusComponentElement(child, componentKey, key);
                            hasComponents = true;
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (0 == (attributes & (int)CpiAssemblyAttributes.DotNetAssembly) && !hasComponents)
            {
                this.Core.OnMessage(ComPlusWarnings.MissingComponents(sourceLineNumbers));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusAssembly");
            row[0] = key;
            row[1] = applicationKey;
            row[2] = componentKey;
            row[3] = assemblyName;
            row[4] = dllPath;
            row[5] = tlbPath;
            row[6] = psDllPath;
            row[7] = attributes;

            if (win64)
            {
                if (this.Core.CurrentPlatform == Platform.IA64)
                {
                    this.Core.OnMessage(WixErrors.UnsupportedPlatformForElement(sourceLineNumbers, "ia64", node.Name.LocalName));
                }
                else
                {
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall_x64");
                    this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall_x64");
                }
            }
            else
            {
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusInstall");
                this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "ConfigureComPlusUninstall");
            }
        }

        /// <summary>
        /// Parses a COM+ assembly dependency element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="assemblyKey">Identifier of parent assembly.</param>
        private void ParseComPlusAssemblyDependencyElement(XElement node, string assemblyKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string requiredAssemblyKey = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "RequiredAssembly":
                            requiredAssemblyKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusAssemblyDependency");
            row[0] = assemblyKey;
            row[1] = requiredAssemblyKey;
        }

        /// <summary>
        /// Parses a COM+ component element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="assemblyKey">Identifier of parent assembly.</param>
        private void ParseComPlusComponentElement(XElement node, string componentKey, string assemblyKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string clsid = null;

            Hashtable properties = new Hashtable();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "CLSID":
                            clsid = "{" + this.Core.GetAttributeValue(sourceLineNumbers, attrib) + "}";
                            break;
                        case "AllowInprocSubscribers":
                            properties["AllowInprocSubscribers"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ComponentAccessChecksEnabled":
                            properties["ComponentAccessChecksEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ComponentTransactionTimeout":
                            properties["ComponentTransactionTimeout"] = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, 3600).ToString();
                            break;
                        case "ComponentTransactionTimeoutEnabled":
                            properties["ComponentTransactionTimeoutEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "COMTIIntrinsics":
                            properties["COMTIIntrinsics"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ConstructionEnabled":
                            properties["ConstructionEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ConstructorString":
                            properties["ConstructorString"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "CreationTimeout":
                            properties["CreationTimeout"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Description":
                            properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EventTrackingEnabled":
                            properties["EventTrackingEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ExceptionClass":
                            properties["ExceptionClass"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "FireInParallel":
                            properties["FireInParallel"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "IISIntrinsics":
                            properties["IISIntrinsics"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "InitializesServerApplication":
                            properties["InitializesServerApplication"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "IsEnabled":
                            properties["IsEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "IsPrivateComponent":
                            properties["IsPrivateComponent"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "JustInTimeActivation":
                            properties["JustInTimeActivation"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "LoadBalancingSupported":
                            properties["LoadBalancingSupported"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "MaxPoolSize":
                            properties["MaxPoolSize"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MinPoolSize":
                            properties["MinPoolSize"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MultiInterfacePublisherFilterCLSID":
                            properties["MultiInterfacePublisherFilterCLSID"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MustRunInClientContext":
                            properties["MustRunInClientContext"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "MustRunInDefaultContext":
                            properties["MustRunInDefaultContext"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "ObjectPoolingEnabled":
                            properties["ObjectPoolingEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "PublisherID":
                            properties["PublisherID"] = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "SoapAssemblyName":
                            properties["SoapAssemblyName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "SoapTypeName":
                            properties["SoapTypeName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Synchronization":
                            string synchronizationValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusComponent", "Synchronization", synchronizationValue, "ignored", "none", "supported", "required", "requiresNew"));
                                    break;
                            }
                            break;
                        case "Transaction":
                            string transactionValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusComponent", "Transaction", transactionValue, "ignored", "none", "supported", "required", "requiresNew"));
                                    break;
                            }
                            break;
                        case "TxIsolationLevel":
                            string txIsolationLevelValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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
                                    this.Core.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, "ComPlusComponent", "TxIsolationLevel", txIsolationLevelValue, "any", "readUnCommitted", "readCommitted", "repeatableRead", "serializable"));
                                    break;
                            }
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusRoleForComponent":
                            this.ParseComPlusRoleForComponentElement(child, componentKey, key);
                            break;
                        case "ComPlusInterface":
                            this.ParseComPlusInterfaceElement(child, componentKey, key);
                            break;
                        case "ComPlusSubscription":
                            this.ParseComPlusSubscriptionElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusComponent");
            row[0] = key;
            row[1] = assemblyKey;
            row[2] = clsid;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusComponentProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        /// <summary>
        /// Parses a COM+ application role for component element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="cpcomponentKey">Identifier of parent COM+ component.</param>
        private void ParseComPlusRoleForComponentElement(XElement node, string componentKey, string cpcomponentKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string applicationRoleKey = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Component":
                            if (null != cpcomponentKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            cpcomponentKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusComponent", cpcomponentKey);
                            break;
                        case "ApplicationRole":
                            applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == cpcomponentKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Component"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusRoleForComponent");
            row[0] = key;
            row[1] = cpcomponentKey;
            row[2] = applicationRoleKey;
            row[3] = componentKey;
        }

        /// <summary>
        /// Parses a COM+ interface element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="cpcomponentKey">Identifier of parent COM+ component.</param>
        private void ParseComPlusInterfaceElement(XElement node, string componentKey, string cpcomponentKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            // parse attributes
            string key = null;
            string iid = null;

            Hashtable properties = new Hashtable();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "IID":
                            iid = "{" + this.Core.GetAttributeValue(sourceLineNumbers, attrib) + "}";
                            break;
                        case "Description":
                            properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "QueuingEnabled":
                            properties["QueuingEnabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusRoleForInterface":
                            this.ParseComPlusRoleForInterfaceElement(child, componentKey, key);
                            break;
                        case "ComPlusMethod":
                            this.ParseComPlusMethodElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusInterface");
            row[0] = key;
            row[1] = cpcomponentKey;
            row[2] = iid;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusInterfaceProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        /// <summary>
        /// Parses a COM+ application role for interface element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="interfaceKey">Identifier of parent interface.</param>
        private void ParseComPlusRoleForInterfaceElement(XElement node, string componentKey, string interfaceKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string applicationRoleKey = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Interface":
                            if (null != interfaceKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            interfaceKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusInterface", interfaceKey);
                            break;
                        case "ApplicationRole":
                            applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == interfaceKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Interface"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusRoleForInterface");
            row[0] = key;
            row[1] = interfaceKey;
            row[2] = applicationRoleKey;
            row[3] = componentKey;
        }

        /// <summary>
        /// Parses a COM+ method element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="interfaceKey">Identifier of parent interface.</param>
        private void ParseComPlusMethodElement(XElement node, string componentKey, string interfaceKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            int index = CompilerConstants.IntegerNotSet;
            string name = null;

            Hashtable properties = new Hashtable();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Index":
                            index = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, int.MaxValue);
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "AutoComplete":
                            properties["AutoComplete"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Description":
                            properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "ComPlusRoleForMethod":
                            this.ParseComPlusRoleForMethodElement(child, componentKey, key);
                            break;
                        default:
                            this.Core.UnexpectedElement(node, child);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionElement(node, child);
                }
            }

            if (CompilerConstants.IntegerNotSet == index && null == name)
            {
                this.Core.OnMessage(ComPlusErrors.RequiredAttribute(sourceLineNumbers, node.Name.LocalName, "Index", "Name"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusMethod");
            row[0] = key;
            row[1] = interfaceKey;
            if (CompilerConstants.IntegerNotSet != index)
            {
                row[2] = index;
            }
            row[3] = name;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusMethodProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        /// <summary>
        /// Parses a COM+ application role for method element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="methodKey">Identifier of parent method.</param>
        private void ParseComPlusRoleForMethodElement(XElement node, string componentKey, string methodKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string applicationRoleKey = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Method":
                            if (null != methodKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            methodKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusMethod", methodKey);
                            break;
                        case "ApplicationRole":
                            applicationRoleKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == methodKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Method"));
            }

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusRoleForMethod");
            row[0] = key;
            row[1] = methodKey;
            row[2] = applicationRoleKey;
            row[3] = componentKey;
        }

        /// <summary>
        /// Parses a COM+ event subscription element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="componentKey">Identifier of parent component.</param>
        /// <param name="cpcomponentKey">Identifier of parent COM+ component.</param>
        private void ParseComPlusSubscriptionElement(XElement node, string componentKey, string cpcomponentKey)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);

            string key = null;
            string id = null;
            string name = null;
            string eventCLSID = null;
            string publisherID = null;

            Hashtable properties = new Hashtable();

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            key = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            break;
                        case "Component":
                            if (null != cpcomponentKey)
                            {
                                this.Core.OnMessage(WixErrors.IllegalAttributeWhenNested(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, node.Parent.Name.LocalName));
                            }
                            cpcomponentKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, "ComPlusComponent", cpcomponentKey);
                            break;
                        case "SubscriptionId":
                            id = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Name":
                            name = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "EventCLSID":
                            eventCLSID = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PublisherID":
                            publisherID = TryFormatGuidValue(this.Core.GetAttributeValue(sourceLineNumbers, attrib));
                            break;
                        case "Description":
                            properties["Description"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Enabled":
                            properties["Enabled"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "EventClassPartitionID":
                            properties["EventClassPartitionID"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "FilterCriteria":
                            properties["FilterCriteria"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "InterfaceID":
                            properties["InterfaceID"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MachineName":
                            properties["MachineName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "MethodName":
                            properties["MethodName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "PerUser":
                            properties["PerUser"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "Queued":
                            properties["Queued"] = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib) ? "1" : "0";
                            break;
                        case "SubscriberMoniker":
                            properties["SubscriberMoniker"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "UserName":
                            properties["UserName"] = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(node, attrib);
                            break;
                    }
                }
                else
                {
                    this.Core.ParseExtensionAttribute(node, attrib);
                }
            }

            if (null == cpcomponentKey)
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Component"));
            }

            this.Core.ParseForExtensionElements(node);

            Row row = this.Core.CreateRow(sourceLineNumbers, "ComPlusSubscription");
            row[0] = key;
            row[1] = cpcomponentKey;
            row[2] = componentKey;
            row[3] = id;
            row[4] = name;
            row[5] = eventCLSID;
            row[6] = publisherID;

            IDictionaryEnumerator propertiesEnumerator = properties.GetEnumerator();
            while (propertiesEnumerator.MoveNext())
            {
                Row propertyRow = this.Core.CreateRow(sourceLineNumbers, "ComPlusSubscriptionProperty");
                propertyRow[0] = key;
                propertyRow[1] = (string)propertiesEnumerator.Key;
                propertyRow[2] = (string)propertiesEnumerator.Value;
            }
        }

        /// <summary>
        /// Attempts to parse the input value as a GUID, and in case the value is a valid
        /// GUID returnes it in the format "{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}".
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        string TryFormatGuidValue(string val)
        {
            try
            {
                Guid guid = new Guid(val);
                return guid.ToString("B").ToUpper();
            }
            catch (FormatException)
            {
                return val;
            }
            catch (OverflowException)
            {
                return val;
            }
        }
    }
}
