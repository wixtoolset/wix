// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Extensibility;

    /// <summary>
    /// The compiler for the WiX toolset dependency extension.
    /// </summary>
    public sealed class DependencyCompiler : CompilerExtension
    {
        /// <summary>
        /// Package type when parsing the Provides element.
        /// </summary>
        private enum PackageType
        {
            None,
            ExePackage,
            MsiPackage,
            MspPackage,
            MsuPackage
        }

        public DependencyCompiler()
        {
            this.Namespace = "http://wixtoolset.org/schemas/v4/wxs/dependency";
        }

        /// <summary>
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">Attribute to process.</param>
        public override void ParseAttribute(XElement parentElement, XAttribute attribute, IDictionary<string, string> context)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(parentElement);
            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                    switch (attribute.Name.LocalName)
                    {
                        case "ProviderKey":
                            this.ParseProviderKeyAttribute(sourceLineNumbers, parentElement, attribute);
                            break;
                        default:
                            this.Core.UnexpectedAttribute(parentElement, attribute);
                            break;
                    }
                    break;
                default:
                    this.Core.UnexpectedAttribute(parentElement, attribute);
                    break;
            }
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
            PackageType packageType = PackageType.None;

            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                case "Fragment":
                case "Module":
                case "Product":
                    switch (element.Name.LocalName)
                    {
                        case "Requires":
                            this.ParseRequiresElement(element, null, false);
                            break;
                        default:
                            this.Core.UnexpectedElement(parentElement, element);
                            break;
                    }
                    break;
                case "ExePackage":
                    packageType = PackageType.ExePackage;
                    break;
                case "MsiPackage":
                    packageType = PackageType.MsiPackage;
                    break;
                case "MspPackage":
                    packageType = PackageType.MspPackage;
                    break;
                case "MsuPackage":
                    packageType = PackageType.MsuPackage;
                    break;
                default:
                    this.Core.UnexpectedElement(parentElement, element);
                    break;
            }

            if (PackageType.None != packageType)
            {
                string packageId = context["PackageId"];

                switch (element.Name.LocalName)
                {
                    case "Provides":
                        this.ParseProvidesElement(element, packageType, packageId);
                        break;
                    default:
                        this.Core.UnexpectedElement(parentElement, element);
                        break;
                }
            }
        }

        /// <summary>
        /// Processes a child element of a Component for the Compiler.
        /// </summary>
        /// <param name="parentElement">Parent element of element to process.</param>
        /// <param name="element">Element to process.</param>
        /// <param name="context">Extra information about the context in which this element is being parsed.</param>
        /// <returns>The component key path type if set.</returns>
        public override ComponentKeyPath ParsePossibleKeyPathElement(XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(parentElement);
            ComponentKeyPath keyPath = null;

            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    string componentId = context["ComponentId"];

                    // 64-bit components may cause issues downlevel.
                    bool win64 = false;
                    Boolean.TryParse(context["Win64"], out win64);

                    switch (element.Name.LocalName)
                    {
                        case "Provides":
                            if (win64)
                            {
                                this.Core.OnMessage(DependencyWarnings.Win64Component(sourceLineNumbers, componentId));
                            }

                            keyPath = this.ParseProvidesElement(element, PackageType.None, componentId);
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

            return keyPath;
        }

        /// <summary>
        /// Processes the ProviderKey bundle attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">The XML attribute for the ProviderKey attribute.</param>
        private void ParseProviderKeyAttribute(SourceLineNumber sourceLineNumbers, XElement parentElement, XAttribute attribute)
        {
            Identifier id = null;
            string providerKey = null;
            int illegalChar = -1;

            switch (attribute.Name.LocalName)
            {
                case "ProviderKey":
                    providerKey = this.Core.GetAttributeValue(sourceLineNumbers, attribute);
                    break;
                default:
                    this.Core.UnexpectedAttribute(parentElement, attribute);
                    break;
            }

            // Make sure the key does not contain any illegal characters or values.
            if (String.IsNullOrEmpty(providerKey))
            {
                this.Core.OnMessage(WixErrors.IllegalEmptyAttributeValue(sourceLineNumbers, parentElement.Name.LocalName, attribute.Name.LocalName));
            }
            else if (0 <= (illegalChar = providerKey.IndexOfAny(DependencyCommon.InvalidCharacters)))
            {
                StringBuilder sb = new StringBuilder(DependencyCommon.InvalidCharacters.Length * 2);
                Array.ForEach<char>(DependencyCommon.InvalidCharacters, c => sb.Append(c).Append(" "));

                this.Core.OnMessage(DependencyErrors.IllegalCharactersInProvider(sourceLineNumbers, "ProviderKey", providerKey[illegalChar], sb.ToString()));
            }
            else if ("ALL" == providerKey)
            {
                this.Core.OnMessage(DependencyErrors.ReservedValue(sourceLineNumbers, parentElement.Name.LocalName, "ProviderKey", providerKey));
            }

            // Generate the primary key for the row.
            id = this.Core.CreateIdentifier("dep", attribute.Name.LocalName, providerKey);

            if (!this.Core.EncounteredError)
            {
                // Create the provider row for the bundle. The Component_ field is required
                // in the table definition but unused for bundles, so just set it to the valid ID.
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixDependencyProvider", id);
                row[1] = id.Id;
                row[2] = providerKey;
                row[5] = DependencyCommon.ProvidesAttributesBundle;
            }
        }

        /// <summary>
        /// Processes the Provides element.
        /// </summary>
        /// <param name="node">The XML node for the Provides element.</param>
        /// <param name="packageType">The type of the package being chained into a bundle, or "None" if building an MSI package.</param>
        /// <param name="keyPath">Explicit key path.</param>
        /// <param name="parentId">The identifier of the parent component or package.</param>
        /// <returns>The type of key path if set.</returns>
        private ComponentKeyPath ParseProvidesElement(XElement node, PackageType packageType, string parentId)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            ComponentKeyPath keyPath = null;
            Identifier id = null;
            string key = null;
            string version = null;
            string displayName = null;
            int attributes = 0;
            int illegalChar = -1;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Version":
                            version = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            // Make sure the key is valid. The key will default to the ProductCode for MSI packages
            // and the package code for MSP packages in the binder if not specified.
            if (!String.IsNullOrEmpty(key))
            {
                // Make sure the key does not contain any illegal characters or values.
                if (0 <= (illegalChar = key.IndexOfAny(DependencyCommon.InvalidCharacters)))
                {
                    StringBuilder sb = new StringBuilder(DependencyCommon.InvalidCharacters.Length * 2);
                    Array.ForEach<char>(DependencyCommon.InvalidCharacters, c => sb.Append(c).Append(" "));

                    this.Core.OnMessage(DependencyErrors.IllegalCharactersInProvider(sourceLineNumbers, "Key", key[illegalChar], sb.ToString()));
                }
                else if ("ALL" == key)
                {
                    this.Core.OnMessage(DependencyErrors.ReservedValue(sourceLineNumbers, node.Name.LocalName, "Key", key));
                }
            }
            else if (PackageType.ExePackage == packageType || PackageType.MsuPackage == packageType)
            {
                // Must specify the provider key when authored for a package.
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }
            else if (PackageType.None == packageType)
            {
                // Make sure the ProductCode is authored and set the key.
                this.Core.CreateSimpleReference(sourceLineNumbers, "Property", "ProductCode");
                key = "!(bind.property.ProductCode)";
            }

            // The Version attribute should not be authored in or for an MSI package.
            if (!String.IsNullOrEmpty(version))
            {
                switch (packageType)
                {
                    case PackageType.None:
                        this.Core.OnMessage(DependencyWarnings.DiscouragedVersionAttribute(sourceLineNumbers));
                        break;
                    case PackageType.MsiPackage:
                        this.Core.OnMessage(DependencyWarnings.DiscouragedVersionAttribute(sourceLineNumbers, parentId));
                        break;
                }
            }
            else if (PackageType.MspPackage == packageType || PackageType.MsuPackage == packageType)
            {
                // Must specify the Version when authored for packages that do not contain a version.
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            // Need the element ID for child element processing, so generate now if not authored.
            if (null == id)
            {
                id = this.Core.CreateIdentifier("dep", node.Name.LocalName, parentId, key);
            }

            foreach (XElement child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Requires":
                            this.ParseRequiresElement(child, id.Id, PackageType.None == packageType);
                            break;
                        case "RequiresRef":
                            this.ParseRequiresRefElement(child, id.Id, PackageType.None == packageType);
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

            if (!this.Core.EncounteredError)
            {
                // Create the row in the provider table.
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixDependencyProvider", id);
                row[1] = parentId;
                row[2] = key;

                if (!String.IsNullOrEmpty(version))
                {
                    row[3] = version;
                }

                if (!String.IsNullOrEmpty(displayName))
                {
                    row[4] = displayName;
                }

                if (0 != attributes)
                {
                    row[5] = attributes;
                }

                if (PackageType.None == packageType)
                {
                    // Reference the Check custom action to check for dependencies on the current provider.
                    if (Platform.ARM == this.Core.CurrentPlatform)
                    {
                        // Ensure the ARM version of the CA is referenced.
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixDependencyCheck_ARM");
                    }
                    else
                    {
                        // All other supported platforms use x86.
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixDependencyCheck");
                    }

                    // Generate registry rows for the provider using binder properties.
                    string keyProvides = String.Concat(DependencyCommon.RegistryRoot, key);

                    row = this.Core.CreateRow(sourceLineNumbers, "Registry", this.Core.CreateIdentifier("reg", id.Id, "(Default)"));
                    row[1] = -1;
                    row[2] = keyProvides;
                    row[3] = null;
                    row[4] = "[ProductCode]";
                    row[5] = parentId;

                    // Use the Version registry value and use that as a potential key path.
                    Identifier idVersion = this.Core.CreateIdentifier("reg", id.Id, "Version");
                    keyPath = new ComponentKeyPath() { Id = idVersion.Id, Explicit = false, Type = ComponentKeyPathType.Registry };

                    row = this.Core.CreateRow(sourceLineNumbers, "Registry", idVersion);
                    row[1] = -1;
                    row[2] = keyProvides;
                    row[3] = "Version";
                    row[4] = !String.IsNullOrEmpty(version) ? version : "[ProductVersion]";
                    row[5] = parentId;

                    row = this.Core.CreateRow(sourceLineNumbers, "Registry", this.Core.CreateIdentifier("reg", id.Id, "DisplayName"));
                    row[1] = -1;
                    row[2] = keyProvides;
                    row[3] = "DisplayName";
                    row[4] = !String.IsNullOrEmpty(displayName) ? displayName : "[ProductName]";
                    row[5] = parentId;

                    if (0 != attributes)
                    {
                        row = this.Core.CreateRow(sourceLineNumbers, "Registry", this.Core.CreateIdentifier("reg", id.Id, "Attributes"));
                        row[1] = -1;
                        row[2] = keyProvides;
                        row[3] = "Attributes";
                        row[4] = String.Concat("#", attributes.ToString(CultureInfo.InvariantCulture.NumberFormat));
                        row[5] = parentId;
                    }
                }
            }

            return keyPath;
        }

        /// <summary>
        /// Processes the Requires element.
        /// </summary>
        /// <param name="node">The XML node for the Requires element.</param>
        /// <param name="providerId">The parent provider identifier.</param>
        /// <param name="requiresAction">Whether the Requires custom action should be referenced.</param>
        private void ParseRequiresElement(XElement node, string providerId, bool requiresAction)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string providerKey = null;
            string minVersion = null;
            string maxVersion = null;
            int attributes = 0;
            int illegalChar = -1;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ProviderKey":
                            providerKey = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Minimum":
                            minVersion = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Maximum":
                            maxVersion = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "IncludeMinimum":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DependencyCommon.RequiresAttributesMinVersionInclusive;
                            }
                            break;
                        case "IncludeMaximum":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DependencyCommon.RequiresAttributesMaxVersionInclusive;
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

            this.Core.ParseForExtensionElements(node);

            if (null == id)
            {
                // Generate an ID only if this element is authored under a Provides element; otherwise, a RequiresRef
                // element will be necessary and the Id attribute will be required.
                if (!String.IsNullOrEmpty(providerId))
                {
                    id = this.Core.CreateIdentifier("dep", node.Name.LocalName, providerKey);
                }
                else
                {
                    this.Core.OnMessage(WixErrors.ExpectedAttributeWhenElementNotUnderElement(sourceLineNumbers, node.Name.LocalName, "Id", "Provides"));
                    id = Identifier.Invalid;
                }
            }

            if (String.IsNullOrEmpty(providerKey))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProviderKey"));
            }
            // Make sure the key does not contain any illegal characters.
            else if (0 <= (illegalChar = providerKey.IndexOfAny(DependencyCommon.InvalidCharacters)))
            {
                StringBuilder sb = new StringBuilder(DependencyCommon.InvalidCharacters.Length * 2);
                Array.ForEach<char>(DependencyCommon.InvalidCharacters, c => sb.Append(c).Append(" "));

                this.Core.OnMessage(DependencyErrors.IllegalCharactersInProvider(sourceLineNumbers, "ProviderKey", providerKey[illegalChar], sb.ToString()));
            }


            if (!this.Core.EncounteredError)
            {
                // Reference the Require custom action if required.
                if (requiresAction)
                {
                    if (Platform.ARM == this.Core.CurrentPlatform)
                    {
                        // Ensure the ARM version of the CA is referenced.
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixDependencyRequire_ARM");
                    }
                    else
                    {
                        // All other supported platforms use x86.
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixDependencyRequire");
                    }
                }

                Row row = this.Core.CreateRow(sourceLineNumbers, "WixDependency", id);
                row[1] = providerKey;
                row[2] = minVersion;
                row[3] = maxVersion;

                if (0 != attributes)
                {
                    row[4] = attributes;
                }

                // Create the relationship between this WixDependency row and the WixDependencyProvider row.
                if (!String.IsNullOrEmpty(providerId))
                {
                    // Create the relationship between the WixDependency row and the parent WixDependencyProvider row.
                    row = this.Core.CreateRow(sourceLineNumbers, "WixDependencyRef");
                    row[0] = providerId;
                    row[1] = id.Id;
                }
            }
        }

        /// <summary>
        /// Processes the RequiresRef element.
        /// </summary>
        /// <param name="node">The XML node for the RequiresRef element.</param>
        /// <param name="providerId">The parent provider identifier.</param>
        /// <param name="requiresAction">Whether the Requires custom action should be referenced.</param>
        private void ParseRequiresRefElement(XElement node, string providerId, bool requiresAction)
        {
            SourceLineNumber sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (XAttribute attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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

            this.Core.ParseForExtensionElements(node);

            if (String.IsNullOrEmpty(id))
            {
                this.Core.OnMessage(WixErrors.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (!this.Core.EncounteredError)
            {
                // Reference the Require custom action if required.
                if (requiresAction)
                {
                    if (Platform.ARM == this.Core.CurrentPlatform)
                    {
                        // Ensure the ARM version of the CA is referenced.
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixDependencyRequire_ARM");
                    }
                    else
                    {
                        // All other supported platforms use x86.
                        this.Core.CreateSimpleReference(sourceLineNumbers, "CustomAction", "WixDependencyRequire");
                    }
                }

                // Create a link dependency on the row that contains information we'll need during bind.
                this.Core.CreateSimpleReference(sourceLineNumbers, "WixDependency", id);

                // Create the relationship between the WixDependency row and the parent WixDependencyProvider row.
                Row row = this.Core.CreateRow(sourceLineNumbers, "WixDependencyRef");
                row[0] = providerId;
                row[1] = id;
            }
        }
    }
}
