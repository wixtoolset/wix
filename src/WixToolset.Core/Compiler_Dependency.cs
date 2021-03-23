// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        // The root registry key for the dependency extension. We write to Software\Classes explicitly
        // based on the current security context instead of HKCR. See
        // http://msdn.microsoft.com/en-us/library/ms724475(VS.85).aspx for more information.
        private const string DependencyRegistryRoot = @"Software\Classes\Installer\Dependencies\";

        private static readonly char[] InvalidDependencyCharacters = new char[] { ' ', '\"', ';', '\\' };

        /// <summary>
        /// Processes the ProviderKey bundle attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line number for the parent element.</param>
        /// <param name="parentElement">Parent element of attribute.</param>
        /// <param name="attribute">The XML attribute for the ProviderKey attribute.</param>
        private void ParseBundleProviderKeyAttribute(SourceLineNumber sourceLineNumbers, XElement parentElement, XAttribute attribute)
        {
            var providerKey = this.Core.GetAttributeValue(sourceLineNumbers, attribute);
            int illegalChar;

            // Make sure the key does not contain any illegal characters or values.
            if (String.IsNullOrEmpty(providerKey))
            {
                this.Messaging.Write(ErrorMessages.IllegalEmptyAttributeValue(sourceLineNumbers, parentElement.Name.LocalName, attribute.Name.LocalName));
            }
            else if (0 <= (illegalChar = providerKey.IndexOfAny(InvalidDependencyCharacters)))
            {
                this.Messaging.Write(CompilerErrors.IllegalCharactersInProvider(sourceLineNumbers, "ProviderKey", providerKey[illegalChar], String.Join(" ", InvalidDependencyCharacters)));
            }
            else if ("ALL" == providerKey)
            {
                this.Messaging.Write(CompilerErrors.ReservedValue(sourceLineNumbers, parentElement.Name.LocalName, "ProviderKey", providerKey));
            }

            if (!this.Messaging.EncounteredError)
            {
                // Generate the primary key for the row.
                var id = this.Core.CreateIdentifier("dep", attribute.Name.LocalName, providerKey);

                // Create the provider symbol for the bundle. The Component_ field is required
                // in the table definition but unused for bundles, so just set it to the valid ID.
                this.Core.AddSymbol(new WixDependencyProviderSymbol(sourceLineNumbers, id)
                {
                    ParentRef = id.Id,
                    ProviderKey = providerKey,
                    Attributes = WixDependencyProviderAttributes.ProvidesAttributesBundle,
                });
            }
        }

        /// <summary>
        /// Processes the Provides element.
        /// </summary>
        /// <param name="node">The XML node for the Provides element.</param>
        /// <param name="packageType">The type of the package being chained into a bundle, or null if building an MSI package.</param>
        /// <param name="parentId">The identifier of the parent component or package.</param>
        /// <param name="possibleKeyPath">Possible KeyPath identifier.</param>
        /// <returns>Yes if this is the keypath.</returns>
        private YesNoType ParseProvidesElement(XElement node, WixBundlePackageType? packageType, string parentId, out string possibleKeyPath)
        {
            possibleKeyPath = null;

            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string key = null;
            string version = null;
            string displayName = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
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
                int illegalChar;

                // Make sure the key does not contain any illegal characters or values.
                if (0 <= (illegalChar = key.IndexOfAny(InvalidDependencyCharacters)))
                {
                    this.Messaging.Write(CompilerErrors.IllegalCharactersInProvider(sourceLineNumbers, "Key", key[illegalChar], String.Join(" ", InvalidDependencyCharacters)));
                }
                else if ("ALL" == key)
                {
                    this.Messaging.Write(CompilerErrors.ReservedValue(sourceLineNumbers, node.Name.LocalName, "Key", key));
                }
            }
            else if (!packageType.HasValue)
            {
                // Make sure the ProductCode is authored and set the key.
                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.Property, "ProductCode");
                key = "!(bind.property.ProductCode)";
            }
            else if (WixBundlePackageType.Exe == packageType || WixBundlePackageType.Msu == packageType)
            {
                // Must specify the provider key when authored for a package.
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Key"));
            }

            // The Version attribute should not be authored in or for an MSI package.
            if (!String.IsNullOrEmpty(version))
            {
                switch (packageType)
                {
                    case null:
                        this.Messaging.Write(CompilerWarnings.DiscouragedVersionAttribute(sourceLineNumbers));
                        break;
                    case WixBundlePackageType.Msi:
                        this.Messaging.Write(CompilerWarnings.DiscouragedVersionAttribute(sourceLineNumbers, parentId));
                        break;
                }
            }
            else if (WixBundlePackageType.Msp == packageType || WixBundlePackageType.Msu == packageType)
            {
                // Must specify the Version when authored for packages that do not contain a version.
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            // Need the element ID for child element processing, so generate now if not authored.
            if (null == id)
            {
                id = this.Core.CreateIdentifier("dep", node.Name.LocalName, parentId, key);
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    switch (child.Name.LocalName)
                    {
                        case "Requires":
                            this.ParseRequiresElement(child, id.Id, requiresAction: !packageType.HasValue);
                            break;
                        case "RequiresRef":
                            this.ParseRequiresRefElement(child, id.Id, requiresAction: !packageType.HasValue);
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

            if (!this.Messaging.EncounteredError)
            {
                var symbol = this.Core.AddSymbol(new WixDependencyProviderSymbol(sourceLineNumbers, id)
                {
                    ParentRef = parentId,
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

                if (!packageType.HasValue)
                {
                    // Generate registry rows for the provider using binder properties.
                    var keyProvides = String.Concat(DependencyRegistryRoot, key);
                    var root = RegistryRootType.MachineUser;

                    var value = "[ProductCode]";
                    this.Core.CreateRegistryRow(sourceLineNumbers, root, keyProvides, null, value, parentId);

                    value = !String.IsNullOrEmpty(version) ? version : "[ProductVersion]";
                    var versionRegistrySymbol = this.Core.CreateRegistryRow(sourceLineNumbers, root, keyProvides, "Version", value, parentId);

                    value = !String.IsNullOrEmpty(displayName) ? displayName : "[ProductName]";
                    this.Core.CreateRegistryRow(sourceLineNumbers, root, keyProvides, "DisplayName", value, parentId);

                    // Use the Version registry value and use that as a potential key path.
                    possibleKeyPath = versionRegistrySymbol.Id;
                }
            }

            return YesNoType.NotSet;
        }

        /// <summary>
        /// Processes the Requires element.
        /// </summary>
        /// <param name="node">The XML node for the Requires element.</param>
        /// <param name="providerId">The parent provider identifier.</param>
        /// <param name="requiresAction">Whether the Requires custom action should be referenced.</param>
        private void ParseRequiresElement(XElement node, string providerId, bool requiresAction)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            Identifier id = null;
            string providerKey = null;
            string minVersion = null;
            string maxVersion = null;
            var attributes = WixDependencySymbolAttributes.None;
            var illegalChar = -1;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
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
                                attributes |= WixDependencySymbolAttributes.RequiresAttributesMinVersionInclusive;
                            }
                            break;
                        case "IncludeMaximum":
                            if (YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib))
                            {
                                attributes |= WixDependencySymbolAttributes.RequiresAttributesMaxVersionInclusive;
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
                    this.Messaging.Write(ErrorMessages.ExpectedAttributeWhenElementNotUnderElement(sourceLineNumbers, node.Name.LocalName, "Id", "Provides"));
                    id = Identifier.Invalid;
                }
            }

            if (String.IsNullOrEmpty(providerKey))
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ProviderKey"));
            }
            // Make sure the key does not contain any illegal characters.
            else if (0 <= (illegalChar = providerKey.IndexOfAny(InvalidDependencyCharacters)))
            {
                this.Messaging.Write(CompilerErrors.IllegalCharactersInProvider(sourceLineNumbers, "ProviderKey", providerKey[illegalChar], String.Join(" ", InvalidDependencyCharacters)));
            }

            if (!this.Messaging.EncounteredError)
            {
                var symbol = this.Core.AddSymbol(new WixDependencySymbol(sourceLineNumbers, id)
                {
                    ProviderKey = providerKey,
                    MinVersion = minVersion,
                    MaxVersion = maxVersion,
                    Attributes = attributes
                });

                // Create the relationship between this WixDependency symbol and the WixDependencyProvider symbol.
                if (!String.IsNullOrEmpty(providerId))
                {
                    this.Core.AddSymbol(new WixDependencyRefSymbol(sourceLineNumbers)
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
        private void ParseRequiresRefElement(XElement node, string providerId, bool requiresAction)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string id = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
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
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (!this.Messaging.EncounteredError)
            {
                // Create a link dependency on the row that contains information we'll need during bind.
                this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixDependency, id);

                // Create the relationship between the WixDependency row and the parent WixDependencyProvider row.
                this.Core.AddSymbol(new WixDependencyRefSymbol(sourceLineNumbers)
                {
                    WixDependencyProviderRef = providerId,
                    WixDependencyRef = id,
                });
            }
        }
    }
}
