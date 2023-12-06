// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Xml;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;
    using WixToolset.Versioning;

    internal class ParseHelper : IParseHelper
    {
        public ParseHelper(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.BundleValidator = serviceProvider.GetService<IBundleValidator>();
            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IServiceProvider ServiceProvider { get; }

        private IBundleValidator BundleValidator { get; }

        private IMessaging Messaging { get; }

        private ISymbolDefinitionCreator Creator { get; set; }

        public bool ContainsProperty(string possibleProperty)
        {
            return Common.ContainsProperty(possibleProperty);
        }

        public void CreateComplexReference(IntermediateSection section, SourceLineNumber sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, string parentLanguage, ComplexReferenceChildType childType, string childId, bool isPrimary)
        {
            section.AddSymbol(new WixComplexReferenceSymbol(sourceLineNumbers)
            {
                Parent = parentId,
                ParentType = parentType,
                ParentLanguage = parentLanguage,
                Child = childId,
                ChildType = childType,
                IsPrimary = isPrimary
            });

            this.CreateWixGroupSymbol(section, sourceLineNumbers, parentType, parentId, childType, childId);
        }

        public Identifier CreateDirectorySymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, Identifier id, string parentId, string name, string shortName = null, string sourceName = null, string shortSourceName = null)
        {
            if (null == id)
            {
                id = this.CreateIdentifier("d", parentId, name, shortName, sourceName, shortSourceName);
            }

            var symbol = section.AddSymbol(new DirectorySymbol(sourceLineNumbers, id)
            {
                ParentDirectoryRef = parentId,
                Name = name,
                ShortName = shortName,
                SourceName = sourceName,
                SourceShortName = shortSourceName
            });

            return symbol.Id;
        }

        public string CreateDirectoryReferenceFromInlineSyntax(IntermediateSection section, SourceLineNumber sourceLineNumbers, XAttribute attribute, string parentId, string inlineSyntax, IDictionary<string, string> sectionCachedInlinedDirectoryIds)
        {
            if (String.IsNullOrEmpty(parentId))
            {
                throw new ArgumentNullException(nameof(parentId));
            }

            if (String.IsNullOrEmpty(inlineSyntax))
            {
                inlineSyntax = this.GetAttributeLongFilename(sourceLineNumbers, attribute, false, true);
            }

            if (String.IsNullOrEmpty(inlineSyntax))
            {
                return parentId;
            }

            inlineSyntax = inlineSyntax.Trim('\\', '/');

            var cacheKey = String.Concat(parentId, ":", inlineSyntax);

            if (!sectionCachedInlinedDirectoryIds.TryGetValue(cacheKey, out var id))
            {
                var identifier = this.CreateDirectorySymbol(section, sourceLineNumbers, id: null, parentId, inlineSyntax);

                id = identifier.Id;
            }
            else
            {
                this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Directory, id);
            }

            return id;
        }

        public string CreateGuid(Guid namespaceGuid, string value)
        {
            return Uuid.NewUuid(namespaceGuid, value).ToString("B").ToUpperInvariant();
        }

        public Identifier CreateIdentifier(string prefix, params string[] args)
        {
            var id = Common.GenerateIdentifier(prefix, args);
            return new Identifier(AccessModifier.Section, id);
        }

        public Identifier CreateIdentifierFromFilename(string filename)
        {
            var id = Common.GetIdentifierFromName(filename);
            return new Identifier(AccessModifier.Section, id);
        }

        public string CreateIdentifierValueFromPlatform(string name, Platform currentPlatform, BurnPlatforms supportedPlatforms)
        {
            string suffix = null;

            switch (currentPlatform)
            {
                case Platform.X86:
                    if ((supportedPlatforms & BurnPlatforms.X86) == BurnPlatforms.X86)
                    {
                        suffix = "_X86";
                    }
                    break;
                case Platform.X64:
                    if ((supportedPlatforms & BurnPlatforms.X64) == BurnPlatforms.X64)
                    {
                        suffix = "_X64";
                    }
                    break;
                case Platform.ARM64:
                    if ((supportedPlatforms & BurnPlatforms.ARM64) == BurnPlatforms.ARM64)
                    {
                        suffix = "_A64";
                    }
                    break;
            }

            return suffix == null ? null : name + suffix;
        }

        public Identifier CreateRegistrySymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, RegistryRootType root, string key, string name, string value, string componentId, RegistryValueType valueType = RegistryValueType.String, RegistryValueActionType valueAction = RegistryValueActionType.Write)
        {
            if (RegistryRootType.Unknown == root)
            {
                throw new ArgumentOutOfRangeException(nameof(root));
            }

            if (null == key)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (null == componentId)
            {
                throw new ArgumentNullException(nameof(componentId));
            }

            var id = this.CreateIdentifier("reg", componentId, ((int)root).ToString(CultureInfo.InvariantCulture.NumberFormat), key.ToLowerInvariant(), (null != name ? name.ToLowerInvariant() : name));

            var symbol = section.AddSymbol(new RegistrySymbol(sourceLineNumbers, id)
            {
                Root = root,
                Key = key,
                Name = name,
                Value = value,
                ValueType = valueType,
                ValueAction = valueAction,
                ComponentRef = componentId,
            });

            return symbol.Id;
        }

        public Identifier CreateRegistrySymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, RegistryRootType root, string key, string name, int value, string componentId)
        {
            return this.CreateRegistrySymbol(section, sourceLineNumbers, root, key, name, value.ToString(), componentId, RegistryValueType.Integer);
        }

        public void CreateSimpleReference(IntermediateSection section, SourceLineNumber sourceLineNumbers, string symbolName, string primaryKey)
        {
            section.AddSymbol(new WixSimpleReferenceSymbol(sourceLineNumbers)
            {
                Table = symbolName,
                PrimaryKeys = primaryKey
            });
        }

        public void CreateSimpleReference(IntermediateSection section, SourceLineNumber sourceLineNumbers, string symbolName, params string[] primaryKeys)
        {
            section.AddSymbol(new WixSimpleReferenceSymbol(sourceLineNumbers)
            {
                Table = symbolName,
                PrimaryKeys = String.Join("/", primaryKeys)
            });
        }

        public void CreateSimpleReference(IntermediateSection section, SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition, string primaryKey)
        {
            this.CreateSimpleReference(section, sourceLineNumbers, symbolDefinition.Name, primaryKey);
        }

        public void CreateSimpleReference(IntermediateSection section, SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition, params string[] primaryKeys)
        {
            this.CreateSimpleReference(section, sourceLineNumbers, symbolDefinition.Name, primaryKeys);
        }

        public void CreateWixGroupSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType childType, string childId)
        {
            if (null == parentId || ComplexReferenceParentType.Unknown == parentType)
            {
                return;
            }

            if (null == childId)
            {
                throw new ArgumentNullException(nameof(childId));
            }

            section.AddSymbol(new WixGroupSymbol(sourceLineNumbers)
            {
                ParentId = parentId,
                ParentType = parentType,
                ChildId = childId,
                ChildType = childType,
            });
        }

        public void CreateWixSearchSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, string elementName, Identifier id, string variable, string condition, string after, string bundleExtensionId)
        {
            if (variable == null)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, elementName, "Variable"));
            }
            else if (!this.IsValidLocIdentifier(variable) && !Common.IsValidBinderVariable(variable))
            {
                this.BundleValidator.ValidateBundleVariableNameValue(sourceLineNumbers, elementName, "Variable", variable, BundleVariableNameRule.CanBeWellKnown | BundleVariableNameRule.CanHaveReservedPrefix);
            }

            section.AddSymbol(new WixSearchSymbol(sourceLineNumbers, id)
            {
                Variable = variable,
                Condition = condition,
                BundleExtensionRef = bundleExtensionId,
            });

            if (after != null)
            {
                this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixSearch, after);
                // TODO: We're currently defaulting to "always run after", which we will need to change...
                this.CreateWixSearchRelationSymbol(section, sourceLineNumbers, id, after, 2);
            }

            if (!String.IsNullOrEmpty(bundleExtensionId))
            {
                this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixBundleExtension, bundleExtensionId);
            }
        }

        public void CreateWixSearchRelationSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, Identifier id, string parentId, int attributes)
        {
            section.AddSymbol(new WixSearchRelationSymbol(sourceLineNumbers, id)
            {
                ParentSearchRef = parentId,
                Attributes = attributes,
            });
        }

        public IntermediateSymbol CreateSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, string symbolName, Identifier identifier = null)
        {
            if (this.Creator == null)
            {
                this.CreateSymbolDefinitionCreator();
            }

            if (!this.Creator.TryGetSymbolDefinitionByName(symbolName, out var symbolDefinition))
            {
                throw new ArgumentException(nameof(symbolName));
            }

            return this.CreateSymbol(section, sourceLineNumbers, symbolDefinition, identifier);
        }

        public IntermediateSymbol CreateSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition, Identifier identifier = null)
        {
            return section.AddSymbol(symbolDefinition.CreateSymbol(sourceLineNumbers, identifier));
        }

        public void EnsureTable(IntermediateSection section, SourceLineNumber sourceLineNumbers, TableDefinition tableDefinition)
        {
            section.AddSymbol(new WixEnsureTableSymbol(sourceLineNumbers)
            {
                Table = tableDefinition.Name,
            });
        }

        public void EnsureTable(IntermediateSection section, SourceLineNumber sourceLineNumbers, string tableName)
        {
            section.AddSymbol(new WixEnsureTableSymbol(sourceLineNumbers)
            {
                Table = tableName,
            });
        }

        public Identifier GetAttributeBundleVariableNameIdentifier(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            var variableId = this.GetAttributeIdentifier(sourceLineNumbers, attribute);

            if (!String.IsNullOrEmpty(variableId?.Id))
            {
                this.BundleValidator.ValidateBundleVariableNameDeclaration(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, variableId.Id);
            }

            return variableId;
        }

        public string GetAttributeBundleVariableNameValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, BundleVariableNameRule nameRule = BundleVariableNameRule.CanBeWellKnown | BundleVariableNameRule.CanHaveReservedPrefix)
        {
            var variableName = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (!String.IsNullOrEmpty(variableName) && !this.IsValidLocIdentifier(variableName) && !Common.IsValidBinderVariable(variableName))
            {
                this.BundleValidator.ValidateBundleVariableNameValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, variableName, nameRule);
            }

            return variableName;
        }

        public string GetAttributeGuidValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool generatable = false, bool canBeEmpty = false)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException(nameof(attribute));
            }

            var emptyRule = canBeEmpty ? EmptyRule.CanBeEmpty : EmptyRule.CanBeWhitespaceOnly;
            var value = this.GetAttributeValue(sourceLineNumbers, attribute, emptyRule);

            if (String.IsNullOrEmpty(value))
            {
                if (canBeEmpty)
                {
                    return String.Empty;
                }
            }
            else
            {
                if (generatable && value == "*")
                {
                    return value;
                }

                if (Guid.TryParse(value, out var guid))
                {
                    return guid.ToString("B").ToUpperInvariant();
                }

                if (value.StartsWith("!(loc", StringComparison.Ordinal) || value.StartsWith("$(loc", StringComparison.Ordinal) || value.StartsWith("!(wix", StringComparison.Ordinal))
                {
                    return value;
                }

                if (value.StartsWith("PUT-GUID-", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("{PUT-GUID-", StringComparison.OrdinalIgnoreCase))
                {
                    this.Messaging.Write(ErrorMessages.ExampleGuid(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                else
                {
                    this.Messaging.Write(ErrorMessages.IllegalGuidValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return CompilerConstants.IllegalGuid;
        }

        public Identifier GetAttributeIdentifier(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            var access = AccessModifier.Global;
            var value = Common.GetAttributeValue(this.Messaging, sourceLineNumbers, attribute, EmptyRule.CanBeEmpty);

            var separator = value.IndexOf(' ');
            if (separator > 0)
            {
                var prefix = value.Substring(0, separator);
                switch (prefix)
                {
                    case "global":
                    case "public":
                    case "package":
                        access = AccessModifier.Global;
                        break;

                    case "internal":
                    case "library":
                        access = AccessModifier.Library;
                        break;

                    case "file":
                    case "protected":
                        access = AccessModifier.File;
                        break;

                    case "private":
                    case "fragment":
                    case "section":
                        access = AccessModifier.Section;
                        break;

                    case "virtual":
                        access = AccessModifier.Virtual;
                        break;

                    case "override":
                        access = AccessModifier.Override;
                        break;

                    default:
                        return null;
                }

                value = value.Substring(separator + 1).Trim();
            }

            if (!Common.IsIdentifier(value))
            {
                this.Messaging.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                return null;
            }
            else if (72 < value.Length)
            {
                this.Messaging.Write(WarningMessages.IdentifierTooLong(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
            }

            return new Identifier(access, value);
        }

        public string GetAttributeIdentifierValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            return Common.GetAttributeIdentifierValue(this.Messaging, sourceLineNumbers, attribute);
        }

        public int GetAttributeIntegerValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, int minimum, int maximum)
        {
            return Common.GetAttributeIntegerValue(this.Messaging, sourceLineNumbers, attribute, minimum, maximum);
        }

        public string GetAttributeLongFilename(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowWildcards, bool allowRelative)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (!String.IsNullOrEmpty(value))
            {
                if (!this.IsValidLongFilename(value, allowWildcards, allowRelative) && !this.IsValidLocIdentifier(value))
                {
                    if (allowRelative)
                    {
                        this.Messaging.Write(ErrorMessages.IllegalRelativeLongFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    }
                    else
                    {
                        this.Messaging.Write(ErrorMessages.IllegalLongFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    }
                }
                else if (allowRelative)
                {
                    value = this.BundleValidator.GetCanonicalRelativePath(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value);
                }
                else if (CompilerCore.IsAmbiguousFilename(value))
                {
                    this.Messaging.Write(WarningMessages.AmbiguousFileOrDirectoryName(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return value;
        }

        public long GetAttributeLongValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, long minimum, long maximum)
        {
            Debug.Assert(minimum > CompilerConstants.LongNotSet && minimum > CompilerConstants.IllegalLong, "The legal values for this attribute collide with at least one sentinel used during parsing.");

            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                try
                {
                    var longValue = Convert.ToInt64(value, CultureInfo.InvariantCulture.NumberFormat);

                    if (CompilerConstants.LongNotSet == longValue || CompilerConstants.IllegalLong == longValue)
                    {
                        this.Messaging.Write(ErrorMessages.IntegralValueSentinelCollision(sourceLineNumbers, longValue));
                    }
                    else if (minimum > longValue || maximum < longValue)
                    {
                        this.Messaging.Write(ErrorMessages.IntegralValueOutOfRange(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, longValue, minimum, maximum));
                        longValue = CompilerConstants.IllegalLong;
                    }

                    return longValue;
                }
                catch (FormatException)
                {
                    this.Messaging.Write(ErrorMessages.IllegalLongValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
                catch (OverflowException)
                {
                    this.Messaging.Write(ErrorMessages.IllegalLongValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return CompilerConstants.IllegalLong;
        }

        public string GetAttributeValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, EmptyRule emptyRule = EmptyRule.CanBeWhitespaceOnly)
        {
            return Common.GetAttributeValue(this.Messaging, sourceLineNumbers, attribute, emptyRule);
        }

        public RegistryRootType? GetAttributeRegistryRootValue(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool allowHkmu)
        {
            var value = this.GetAttributeValue(sourceLineNumbers, attribute);
            if (String.IsNullOrEmpty(value))
            {
                return null;
            }

            switch (value)
            {
                case "HKCR":
                    return RegistryRootType.ClassesRoot;

                case "HKCU":
                    return RegistryRootType.CurrentUser;

                case "HKLM":
                    return RegistryRootType.LocalMachine;

                case "HKU":
                    return RegistryRootType.Users;

                case "HKMU":
                    if (allowHkmu)
                    {
                        return RegistryRootType.MachineUser;
                    }
                    break;
            }

            if (allowHkmu)
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, "HKMU", "HKCR", "HKCU", "HKLM", "HKU"));
            }
            else
            {
                this.Messaging.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, "HKCR", "HKCU", "HKLM", "HKU"));
            }

            return RegistryRootType.Unknown;
        }

        public string GetAttributeVersionValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (!String.IsNullOrEmpty(value))
            {
                if (WixVersion.TryParse(value, out var _))
                {
                    return value;
                }

                // Allow versions to contain binder variables.
                if (Common.ContainsValidBinderVariable(value))
                {
                    return value;
                }

                this.Messaging.Write(ErrorMessages.IllegalVersionValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
            }

            return null;
        }

        public YesNoDefaultType GetAttributeYesNoDefaultValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            switch (value)
            {
                case "yes":
                case "true":
                    return YesNoDefaultType.Yes;

                case "no":
                case "false":
                    return YesNoDefaultType.No;

                case "default":
                    return YesNoDefaultType.Default;

                default:
                    this.Messaging.Write(ErrorMessages.IllegalYesNoDefaultValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    return YesNoDefaultType.IllegalValue;
            }
        }

        public YesNoType GetAttributeYesNoValue(SourceLineNumber sourceLineNumbers, XAttribute attribute)
        {
            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            switch (value)
            {
                case "yes":
                case "true":
                    return YesNoType.Yes;

                case "no":
                case "false":
                    return YesNoType.No;

                default:
                    this.Messaging.Write(ErrorMessages.IllegalYesNoValue(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    return YesNoType.IllegalValue;
            }
        }

        public SourceLineNumber GetSourceLineNumbers(XElement element)
        {
            return Preprocessor.GetSourceLineNumbers(element);
        }

        public string GetConditionInnerText(XElement element)
        {
            var value = Common.GetInnerText(element)?.Trim().Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');

            // Return null for a non-existant condition.
            return String.IsNullOrEmpty(value) ? null : value;
        }

        public string GetTrimmedInnerText(XElement element)
        {
            var value = Common.GetInnerText(element);
            return value?.Trim();
        }

        public void InnerTextDisallowed(XElement element)
        {
            Common.InnerTextDisallowed(this.Messaging, element, null);
        }

        public void InnerTextDisallowed(XElement element, string attributeName)
        {
            Common.InnerTextDisallowed(this.Messaging, element, attributeName);
        }

        public bool IsValidIdentifier(string value)
        {
            return Common.IsIdentifier(value);
        }

        public bool IsValidLocIdentifier(string identifier)
        {
            return Common.TryParseWixVariable(identifier, 0, out var parsed) && parsed.Index == 0 && parsed.Length == identifier.Length && parsed.Namespace == "loc";
        }

        public bool IsValidLongFilename(string filename, bool allowWildcards, bool allowRelative)
        {
            return Common.IsValidLongFilename(filename, allowWildcards, allowRelative);
        }

        public bool IsValidShortFilename(string filename, bool allowWildcards)
        {
            return Common.IsValidShortFilename(filename, allowWildcards);
        }

        public void ParseExtensionAttribute(IEnumerable<ICompilerExtension> extensions, Intermediate intermediate, IntermediateSection section, XElement element, XAttribute attribute, IDictionary<string, string> context = null)
        {
            // Ignore attributes defined by the W3C because we'll assume they are always right.
            if ((String.IsNullOrEmpty(attribute.Name.NamespaceName) && attribute.Name.LocalName.Equals("xmlns", StringComparison.Ordinal)) ||
                attribute.Name.NamespaceName.StartsWith(CompilerCore.W3SchemaPrefix.NamespaceName, StringComparison.Ordinal))
            {
                return;
            }

            if (ParseHelper.TryFindExtension(extensions, attribute.Name.NamespaceName, out var extension))
            {
                extension.ParseAttribute(intermediate, section, element, attribute, context);
            }
            else
            {
                var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);
                this.Messaging.Write(ErrorMessages.UnhandledExtensionAttribute(sourceLineNumbers, element.Name.LocalName, attribute.Name.LocalName, attribute.Name.NamespaceName));
            }
        }

        public void ParseExtensionElement(IEnumerable<ICompilerExtension> extensions, Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context = null)
        {
            if (ParseHelper.TryFindExtension(extensions, element.Name.Namespace, out var extension))
            {
                extension.ParseElement(intermediate, section, parentElement, element, context);
            }
            else
            {
                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);
                this.Messaging.Write(ErrorMessages.UnhandledExtensionElement(childSourceLineNumbers, parentElement.Name.LocalName, element.Name.LocalName, element.Name.NamespaceName));
            }
        }

        public IComponentKeyPath ParsePossibleKeyPathExtensionElement(IEnumerable<ICompilerExtension> extensions, Intermediate intermediate, IntermediateSection section, XElement parentElement, XElement element, IDictionary<string, string> context)
        {
            IComponentKeyPath keyPath = null;

            if (ParseHelper.TryFindExtension(extensions, element.Name.Namespace, out var extension))
            {
                keyPath = extension.ParsePossibleKeyPathElement(intermediate, section, parentElement, element, context);
            }
            else
            {
                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);
                this.Messaging.Write(ErrorMessages.UnhandledExtensionElement(childSourceLineNumbers, parentElement.Name.LocalName, element.Name.LocalName, element.Name.NamespaceName));
            }

            return keyPath;
        }

        public void ParseForExtensionElements(IEnumerable<ICompilerExtension> extensions, Intermediate intermediate, IntermediateSection section, XElement element, IDictionary<string, string> context = null)
        {
            var checkInnerText = false;

            foreach (var child in element.Nodes())
            {
                if (child is XElement childElement)
                {
                    if (element.Name.Namespace == childElement.Name.Namespace)
                    {
                        this.UnexpectedElement(element, childElement);
                    }
                    else
                    {
                        this.ParseExtensionElement(extensions, intermediate, section, element, childElement, context);
                    }
                }
                else
                {
                    checkInnerText = true;
                }
            }

            if (checkInnerText)
            {
                this.InnerTextDisallowed(element);
            }
        }

        public WixActionSymbol ScheduleActionSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, AccessModifier access, SequenceTable sequence, string actionName, string condition, string beforeAction, string afterAction, bool overridable = false)
        {
            var actionId = new Identifier(access, sequence, actionName);

            var actionSymbol = section.AddSymbol(new WixActionSymbol(sourceLineNumbers, actionId)
            {
                SequenceTable = sequence,
                Action = actionName,
                Condition = condition,
                Before = beforeAction,
                After = afterAction,
                Overridable = overridable,
            });

            if (beforeAction != null || afterAction != null)
            {
                this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixAction, sequence.ToString(), beforeAction ?? afterAction);
            }

            return actionSymbol;
        }

        public void CreateCustomActionReference(SourceLineNumber sourceLineNumbers, IntermediateSection section, string customAction, Platform currentPlatform, CustomActionPlatforms supportedPlatforms)
        {
            if (!this.Messaging.EncounteredError)
            {
                var suffix = "_X86";

                switch (currentPlatform)
                {
                    case Platform.X64:
                        if ((supportedPlatforms & CustomActionPlatforms.X64) == CustomActionPlatforms.X64)
                        {
                            suffix = "_X64";
                        }
                        break;
                    case Platform.ARM64:
                        if ((supportedPlatforms & CustomActionPlatforms.ARM64) == CustomActionPlatforms.ARM64)
                        {
                            suffix = "_A64";
                        }
                        break;
                }

                this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, customAction + suffix);
            }
        }

        public void UnexpectedAttribute(XElement element, XAttribute attribute)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);
            Common.UnexpectedAttribute(this.Messaging, sourceLineNumbers, attribute);
        }

        public void UnexpectedElement(XElement parentElement, XElement childElement)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(childElement);
            this.Messaging.Write(ErrorMessages.UnexpectedElement(sourceLineNumbers, parentElement.Name.LocalName, childElement.Name.LocalName));
        }

        private void CreateSymbolDefinitionCreator()
        {
            this.Creator = this.ServiceProvider.GetService<ISymbolDefinitionCreator>();
        }

        private static bool TryFindExtension(IEnumerable<ICompilerExtension> extensions, XNamespace ns, out ICompilerExtension extension)
        {
            extension = null;

            foreach (var ext in extensions)
            {
                if (ext.Namespace == ns)
                {
                    extension = ext;
                    break;
                }
            }

            return extension != null;
        }
    }
}
