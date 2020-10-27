// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Dependency
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Dependency.Symbols;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;

    /// <summary>
    /// The compiler for the WiX Toolset Dependency Extension.
    /// </summary>
    public sealed class DependencyCompiler : BaseCompilerExtension
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

        public override XNamespace Namespace => "http://wixtoolset.org/schemas/v4/wxs/dependency";

        /// <summary>
        /// Processes an attribute for the Compiler.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">Attribute to process.</param>
        public override void ParseAttribute(Intermediate intermediate, IntermediateSection section, XElement parentElement, XAttribute attribute, IDictionary<string, string> context)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(parentElement);
            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                    switch (attribute.Name.LocalName)
                    {
                        case "ProviderKey":
                            this.ParseProviderKeyAttribute(section, sourceLineNumbers, parentElement, attribute);
                            break;
                        default:
                            this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
                            break;
                    }
                    break;
                default:
                    this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
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
        public override void ParseElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            var packageType = PackageType.None;

            switch (parentElement.Name.LocalName)
            {
                case "Bundle":
                case "Fragment":
                case "Module":
                case "Package":
                    switch (element.Name.LocalName)
                    {
                        case "Requires":
                            this.ParseRequiresElement(intermediate, section, element, null, false);
                            break;
                        default:
                            this.ParseHelper.UnexpectedElement(parentElement, element);
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
                    this.ParseHelper.UnexpectedElement(parentElement, element);
                    break;
            }

            if (PackageType.None != packageType)
            {
                var packageId = context["PackageId"];

                switch (element.Name.LocalName)
                {
                    case "Provides":
                        this.ParseProvidesElement(intermediate, section, element, packageType, packageId);
                        break;
                    default:
                        this.ParseHelper.UnexpectedElement(parentElement, element);
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
        public override IComponentKeyPath ParsePossibleKeyPathElement(Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(parentElement);
            IComponentKeyPath keyPath = null;

            switch (parentElement.Name.LocalName)
            {
                case "Component":
                    var componentId = context["ComponentId"];

                    // 64-bit components may cause issues downlevel.
                    Boolean.TryParse(context["Win64"], out var win64);

                    switch (element.Name.LocalName)
                    {
                        case "Provides":
                            if (win64)
                            {
                                this.Messaging.Write(DependencyWarnings.Win64Component(sourceLineNumbers, componentId));
                            }

                            keyPath = this.ParseProvidesElement(intermediate, section, element, PackageType.None, componentId);
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

            return keyPath;
        }

        /// <summary>
        /// Processes the ProviderKey bundle attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">The XML attribute for the ProviderKey attribute.</param>
        private void ParseProviderKeyAttribute(IntermediateSection section, SourceLineNumber sourceLineNumbers, XElement parentElement, XAttribute attribute)
        {
            Identifier id = null;
            string providerKey = null;
            int illegalChar = -1;

            switch (attribute.Name.LocalName)
            {
                case "ProviderKey":
                    providerKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attribute);
                    break;
                default:
                    this.ParseHelper.UnexpectedAttribute(parentElement, attribute);
                    break;
            }

            // Make sure the key does not contain any illegal characters or values.
            if (String.IsNullOrEmpty(providerKey))
            {
                this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, parentElement.Name.LocalName, attribute.Name.LocalName));
            }
            else if (0 <= (illegalChar = providerKey.IndexOfAny(DependencyCommon.InvalidCharacters)))
            {
                var sb = new StringBuilder(DependencyCommon.InvalidCharacters.Length * 2);
                Array.ForEach<char>(DependencyCommon.InvalidCharacters, c => sb.Append(c).Append(" "));

                this.Messaging.Write(DependencyErrors.IllegalCharactersInProvider(sourceLineNumbers, "ProviderKey", providerKey[illegalChar], sb.ToString()));
            }
            else if ("ALL" == providerKey)
            {
                this.Messaging.Write(DependencyErrors.ReservedValue(sourceLineNumbers, parentElement.Name.LocalName, "ProviderKey", providerKey));
            }

            // Generate the primary key for the row.
            id = this.ParseHelper.CreateIdentifier("dep", attribute.Name.LocalName, providerKey);

            if (!this.Messaging.EncounteredError)
            {
                // Create the provider symbol for the bundle. The Component_ field is required
                // in the table definition but unused for bundles, so just set it to the valid ID.
                section.AddSymbol(new WixDependencyProviderSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = id.Id,
                    ProviderKey = providerKey,
                    Attributes = WixDependencyProviderAttributes.ProvidesAttributesBundle,
                });
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
        private IComponentKeyPath ParseProvidesElement(Intermediate intermediate, IntermediateSection section, XElement node, PackageType packageType, string parentId)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            IComponentKeyPath keyPath = null;
            Identifier id = null;
            string key = null;
            string version = null;
            string displayName = null;
            int illegalChar = -1;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "Key":
                            key = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Version":
                            version = this.ParseHelper.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "DisplayName":
                            displayName = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
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

            // Make sure the key is valid. The key will default to the ProductCode for MSI packages
            // and the package code for MSP packages in the binder if not specified.
            if (!String.IsNullOrEmpty(key))
            {
                // Make sure the key does not contain any illegal characters or values.
                if (0 <= (illegalChar = key.IndexOfAny(DependencyCommon.InvalidCharacters)))
                {
                    var sb = new StringBuilder(DependencyCommon.InvalidCharacters.Length * 2);
                    Array.ForEach<char>(DependencyCommon.InvalidCharacters, c => sb.Append(c).Append(" "));

                    this.Messaging.Write(DependencyErrors.IllegalCharactersInProvider(sourceLineNumbers, "Key", key[illegalChar], sb.ToString()));
                }
                else if ("ALL" == key)
                {
                    this.Messaging.Write(DependencyErrors.ReservedValue(sourceLineNumbers, node.Name.LocalName, "Key", key));
                }
            }
            else if (PackageType.ExePackage == packageType || PackageType.MsuPackage == packageType)
            {
                // Must specify the provider key when authored for a package.
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }
            else if (PackageType.None == packageType)
            {
                // Make sure the ProductCode is authored and set the key.
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Property, "ProductCode");
                key = "!(bind.property.ProductCode)";
            }

            // The Version attribute should not be authored in or for an MSI package.
            if (!String.IsNullOrEmpty(version))
            {
                switch (packageType)
                {
                    case PackageType.None:
                        this.Messaging.Write(DependencyWarnings.DiscouragedVersionAttribute(sourceLineNumbers));
                        break;
                    case PackageType.MsiPackage:
                        this.Messaging.Write(DependencyWarnings.DiscouragedVersionAttribute(sourceLineNumbers, parentId));
                        break;
                }
            }
            else if (PackageType.MspPackage == packageType || PackageType.MsuPackage == packageType)
            {
                // Must specify the Version when authored for packages that do not contain a version.
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            // Need the element ID for child element processing, so generate now if not authored.
            if (null == id)
            {
                id = this.ParseHelper.CreateIdentifier("dep", node.Name.LocalName, parentId, key);
            }

            foreach (var child in node.Elements())
            {
                if (this.Namespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Requires":
                            this.ParseRequiresElement(intermediate, section, child, id.Id, PackageType.None == packageType);
                            break;
                        case "RequiresRef":
                            this.ParseRequiresRefElement(intermediate, section, child, id.Id, PackageType.None == packageType);
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

            if (!this.Messaging.EncounteredError)
            {
                var symbol = section.AddSymbol(new WixDependencyProviderSymbol(sourceLineNumbers, id)
                {
                    ComponentRef = parentId,
                    ProviderKey = key,
                });

                if (!String.IsNullOrEmpty(version))
                {
                    symbol.Version = version;
                }

                if (!String.IsNullOrEmpty(displayName))
                {
                    symbol.DisplayName = displayName;
                }

                if (PackageType.None == packageType)
                {
                    this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "DependencyCheck", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);

                    // Generate registry rows for the provider using binder properties.
                    var keyProvides = String.Concat(DependencyCommon.RegistryRoot, key);
                    var root = RegistryRootType.MachineUser;

                    var value = "[ProductCode]";
                    this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, root, keyProvides, null, value, parentId, false);

                    value = !String.IsNullOrEmpty(version) ? version : "[ProductVersion]";
                    var versionRegistrySymbol =
                        this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, root, keyProvides, "Version", value, parentId, false);

                    value = !String.IsNullOrEmpty(displayName) ? displayName : "[ProductName]";
                    this.ParseHelper.CreateRegistrySymbol(section, sourceLineNumbers, root, keyProvides, "DisplayName", value, parentId, false);

                    // Use the Version registry value and use that as a potential key path.
                    keyPath = this.CreateComponentKeyPath();
                    keyPath.Id = versionRegistrySymbol.Id;
                    keyPath.Explicit = false;
                    keyPath.Type = PossibleKeyPathType.Registry;
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
        private void ParseRequiresElement(Intermediate intermediate, IntermediateSection section, XElement node, string providerId, bool requiresAction)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            Identifier id = null;
            string providerKey = null;
            string minVersion = null;
            string maxVersion = null;
            int attributes = 0;
            int illegalChar = -1;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifier(sourceLineNumbers, attrib);
                            break;
                        case "ProviderKey":
                            providerKey = this.ParseHelper.GetAttributeValue(sourceLineNumbers, attrib);
                            break;
                        case "Minimum":
                            minVersion = this.ParseHelper.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "Maximum":
                            maxVersion = this.ParseHelper.GetAttributeVersionValue(sourceLineNumbers, attrib);
                            break;
                        case "IncludeMinimum":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DependencyCommon.RequiresAttributesMinVersionInclusive;
                            }
                            break;
                        case "IncludeMaximum":
                            if (YesNoType.Yes == this.ParseHelper.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= DependencyCommon.RequiresAttributesMaxVersionInclusive;
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

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            if (null == id)
            {
                // Generate an ID only if this element is authored under a Provides element; otherwise, a RequiresRef
                // element will be necessary and the Id attribute will be required.
                if (!String.IsNullOrEmpty(providerId))
                {
                    id = this.ParseHelper.CreateIdentifier("dep", node.Name.LocalName, providerKey);
                }
                else
                {
                    this.Messaging.Write(ErrorMessages.ExpectedAttributeWhenElementNotUnderElement(sourceLineNumbers, node.Name.LocalName, "Id", "Provides"));
                    id = Identifier.Invalid;
                }
            }

            if (String.IsNullOrEmpty(providerKey))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProviderKey"));
            }
            // Make sure the key does not contain any illegal characters.
            else if (0 <= (illegalChar = providerKey.IndexOfAny(DependencyCommon.InvalidCharacters)))
            {
                var sb = new StringBuilder(DependencyCommon.InvalidCharacters.Length * 2);
                Array.ForEach<char>(DependencyCommon.InvalidCharacters, c => sb.Append(c).Append(" "));

                this.Messaging.Write(DependencyErrors.IllegalCharactersInProvider(sourceLineNumbers, "ProviderKey", providerKey[illegalChar], sb.ToString()));
            }

            if (!this.Messaging.EncounteredError)
            {
                // Reference the Require custom action if required.
                if (requiresAction)
                {
                    this.AddReferenceToWixDependencyRequire(section, sourceLineNumbers);
                }

                var symbol = section.AddSymbol(new WixDependencySymbol(sourceLineNumbers, id)
                {
                    ProviderKey = providerKey,
                    MinVersion = minVersion,
                    MaxVersion = maxVersion,
                });

                if (0 != attributes)
                {
                    symbol.Attributes = attributes;
                }

                // Create the relationship between this WixDependency symbol and the WixDependencyProvider symbol.
                if (!String.IsNullOrEmpty(providerId))
                {
                    section.AddSymbol(new WixDependencyRefSymbol(sourceLineNumbers)
                    {
                        WixDependencyProviderRef = providerId,
                        WixDependencyRef = id.Id,
                    });
                }
            }
        }

        /// <summary>
        /// Processes the RequiresRef element.
        /// </summary>
        /// <param name="node">The XML node for the RequiresRef element.</param>
        /// <param name="providerId">The parent provider identifier.</param>
        /// <param name="requiresAction">Whether the Requires custom action should be referenced.</param>
        private void ParseRequiresRefElement(Intermediate intermediate, IntermediateSection section, XElement node, string providerId, bool requiresAction)
        {
            var sourceLineNumbers = this.ParseHelper.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || this.Namespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            id = this.ParseHelper.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
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

            this.ParseHelper.ParseForExtensionElements(this.Context.Extensions, intermediate, section, node);

            if (String.IsNullOrEmpty(id))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (!this.Messaging.EncounteredError)
            {
                // Reference the Require custom action if required.
                if (requiresAction)
                {
                    this.AddReferenceToWixDependencyRequire(section, sourceLineNumbers);
                }

                // Create a link dependency on the row that contains information we'll need during bind.
                this.ParseHelper.CreateSimpleReference(section, sourceLineNumbers, DependencySymbolDefinitions.WixDependency, id);

                // Create the relationship between the WixDependency row and the parent WixDependencyProvider row.
                section.AddSymbol(new WixDependencyRefSymbol(sourceLineNumbers)
                {
                    WixDependencyProviderRef = providerId,
                    WixDependencyRef = id,
                });
            }
        }

        private void AddReferenceToWixDependencyRequire(IntermediateSection section, SourceLineNumber sourceLineNumbers)
        {
            this.ParseHelper.CreateCustomActionReference(sourceLineNumbers, section, "DependencyRequire", this.Context.Platform, CustomActionPlatforms.X86 | CustomActionPlatforms.X64 | CustomActionPlatforms.ARM64);
        }
    }
}
