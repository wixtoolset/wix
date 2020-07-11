// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.ExtensibilityServices
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility;
    using WixToolset.Extensibility.Data;
    using WixToolset.Extensibility.Services;

    internal class ParseHelper : IParseHelper
    {
        public ParseHelper(IWixToolsetServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;

            this.Messaging = serviceProvider.GetService<IMessaging>();
        }

        private IWixToolsetServiceProvider ServiceProvider { get; }

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

        [Obsolete]
        public Identifier CreateDirectoryRow(IntermediateSection section, SourceLineNumber sourceLineNumbers, Identifier id, string parentId, string name, ISet<string> sectionInlinedDirectoryIds, string shortName = null, string sourceName = null, string shortSourceName = null)
        {
            return this.CreateDirectorySymbol(section, sourceLineNumbers, id, parentId, name, sectionInlinedDirectoryIds, shortName, sourceName, shortSourceName);
        }

        public Identifier CreateDirectorySymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, Identifier id, string parentId, string name, ISet<string> sectionInlinedDirectoryIds, string shortName = null, string sourceName = null, string shortSourceName = null)
        {
            // For anonymous directories, create the identifier. If this identifier already exists in the
            // active section, bail so we don't add duplicate anonymous directory symbols (which are legal
            // but bloat the intermediate and ultimately make the linker do "busy work").
            if (null == id)
            {
                id = this.CreateIdentifier("dir", parentId, name, shortName, sourceName, shortSourceName);

                if (!sectionInlinedDirectoryIds.Add(id.Id))
                {
                    return id;
                }
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

        public string CreateDirectoryReferenceFromInlineSyntax(IntermediateSection section, SourceLineNumber sourceLineNumbers, string parentId, XAttribute attribute, ISet<string> sectionInlinedDirectoryIds)
        {
            string id = null;
            var inlineSyntax = this.GetAttributeInlineDirectorySyntax(sourceLineNumbers, attribute, true);

            if (null != inlineSyntax)
            {
                // Special case the single entry in the inline syntax since it is the most common case
                // and needs no extra processing. It's just a reference to an existing directory.
                if (1 == inlineSyntax.Length)
                {
                    id = inlineSyntax[0];
                    this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Directory, id);
                }
                else // start creating symbols for the entries in the inline syntax
                {
                    id = parentId;

                    var pathStartsAt = 0;
                    if (inlineSyntax[0].EndsWith(":", StringComparison.Ordinal))
                    {
                        // TODO: should overriding the parent identifier with a specific id be an error or a warning or just let it slide?
                        //if (null != parentId)
                        //{
                        //    this.core.Write(WixErrors.Xxx(sourceLineNumbers));
                        //}

                        id = inlineSyntax[0].TrimEnd(':');
                        this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.Directory, id);

                        pathStartsAt = 1;
                    }

                    for (var i = pathStartsAt; i < inlineSyntax.Length; ++i)
                    {
                        var inlineId = this.CreateDirectorySymbol(section, sourceLineNumbers, null, id, inlineSyntax[i], sectionInlinedDirectoryIds);
                        id = inlineId.Id;
                    }
                }
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
            return new Identifier(AccessModifier.Private, id);
        }

        public Identifier CreateIdentifierFromFilename(string filename)
        {
            var id = Common.GetIdentifierFromName(filename);
            return new Identifier(AccessModifier.Private, id);
        }

        public string CreateIdentifierValueFromPlatform(string name, Platform currentPlatform, BurnPlatforms supportedPlatforms)
        {
            string suffix = null;

            switch (currentPlatform)
            {
                case Platform.X86:
                    if ((supportedPlatforms & BurnPlatforms.X64) == BurnPlatforms.X64)
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
                case Platform.ARM:
                    if ((supportedPlatforms & BurnPlatforms.ARM) == BurnPlatforms.ARM)
                    {
                        suffix = "_A32";
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

        [Obsolete]
        public Identifier CreateRegistryRow(IntermediateSection section, SourceLineNumber sourceLineNumbers, RegistryRootType root, string key, string name, string value, string componentId, bool escapeLeadingHash)
        {
            return this.CreateRegistrySymbol(section, sourceLineNumbers, root, key, name, value, componentId, escapeLeadingHash);
        }

        public Identifier CreateRegistrySymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, RegistryRootType root, string key, string name, string value, string componentId, bool escapeLeadingHash)
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

            // Escape the leading '#' character for string registry values.
            if (escapeLeadingHash && null != value && value.StartsWith("#", StringComparison.Ordinal))
            {
                value = String.Concat("#", value);
            }

            var id = this.CreateIdentifier("reg", componentId, ((int)root).ToString(CultureInfo.InvariantCulture.NumberFormat), key.ToLowerInvariant(), (null != name ? name.ToLowerInvariant() : name));

            var symbol = section.AddSymbol(new RegistrySymbol(sourceLineNumbers, id)
            {
                Root = root,
                Key = key,
                Name = name,
                Value = value,
                ComponentRef = componentId,
            });

            return symbol.Id;
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

        [Obsolete]
        public void CreateWixGroupRow(IntermediateSection section, SourceLineNumber sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType childType, string childId)
        {
            this.CreateWixGroupSymbol(section, sourceLineNumbers, parentType, parentId, childType, childId);
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
            // TODO: verify variable is not a standard bundle variable
            if (variable == null)
            {
                this.Messaging.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, elementName, "Variable"));
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

        [Obsolete]
        public IntermediateSymbol CreateRow(IntermediateSection section, SourceLineNumber sourceLineNumbers, string tableName, Identifier identifier = null)
        {
            return this.CreateSymbol(section, sourceLineNumbers, tableName, identifier);
        }

        [Obsolete]
        public IntermediateSymbol CreateRow(IntermediateSection section, SourceLineNumber sourceLineNumbers, SymbolDefinitionType symbolType, Identifier identifier = null)
        {
            return this.CreateSymbol(section, sourceLineNumbers, symbolType, identifier);
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

        [Obsolete]
        public IntermediateSymbol CreateSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, SymbolDefinitionType symbolType, Identifier identifier = null)
        {
            var symbolDefinition = SymbolDefinitions.ByType(symbolType);

            return this.CreateSymbol(section, sourceLineNumbers, symbolDefinition, identifier);
        }

        public IntermediateSymbol CreateSymbol(IntermediateSection section, SourceLineNumber sourceLineNumbers, IntermediateSymbolDefinition symbolDefinition, Identifier identifier = null)
        {
            return section.AddSymbol(symbolDefinition.CreateSymbol(sourceLineNumbers, identifier));
        }

        public string CreateShortName(string longName, bool keepExtension, bool allowWildcards, params string[] args)
        {
            // canonicalize the long name if its not a localization identifier (they are case-sensitive)
            if (!this.IsValidLocIdentifier(longName))
            {
                longName = longName.ToLowerInvariant();
            }

            // collect all the data
            var strings = new List<string>(1 + args.Length);
            strings.Add(longName);
            strings.AddRange(args);

            // prepare for hashing
            var stringData = String.Join("|", strings);
            var data = Encoding.UTF8.GetBytes(stringData);

            // hash the data
            byte[] hash;
            using (var sha1 = new SHA1CryptoServiceProvider())
            {
                hash = sha1.ComputeHash(data);
            }

            // generate the short file/directory name without an extension
            var shortName = new StringBuilder(Convert.ToBase64String(hash));
            shortName.Remove(8, shortName.Length - 8).Replace('+', '-').Replace('/', '_');

            if (keepExtension)
            {
                var extension = Path.GetExtension(longName);

                if (4 < extension.Length)
                {
                    extension = extension.Substring(0, 4);
                }

                shortName.Append(extension);

                // check the generated short name to ensure its still legal (the extension may not be legal)
                if (!this.IsValidShortFilename(shortName.ToString(), allowWildcards))
                {
                    // remove the extension (by truncating the generated file name back to the generated characters)
                    shortName.Length -= extension.Length;
                }
            }

            return shortName.ToString().ToLowerInvariant();
        }

        public void EnsureTable(IntermediateSection section, SourceLineNumber sourceLineNumbers, TableDefinition tableDefinition)
        {
            section.AddSymbol(new WixEnsureTableSymbol(sourceLineNumbers)
            {
                Table = tableDefinition.Name,
            });

            // TODO: Check if the given table definition is a custom table. For now we have to assume that it isn't.
            //this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixCustomTable, tableDefinition.Name);
        }

        public void EnsureTable(IntermediateSection section, SourceLineNumber sourceLineNumbers, string tableName)
        {
            section.AddSymbol(new WixEnsureTableSymbol(sourceLineNumbers)
            {
                Table = tableName,
            });

            if (this.Creator == null)
            {
                this.CreateSymbolDefinitionCreator();
            }

            // TODO: The tableName may not be the same as the symbolName. For now, we have to assume that it is.
            // We don't add custom table definitions to the tableDefinitions collection,
            // so if it's not in there, it better be a custom table. If the Id is just wrong,
            // instead of a custom table, we get an unresolved reference at link time.
            if (!this.Creator.TryGetSymbolDefinitionByName(tableName, out var _))
            {
                this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixCustomTable, tableName);
            }
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
            var access = AccessModifier.Public;
            var value = Common.GetAttributeValue(this.Messaging, sourceLineNumbers, attribute, EmptyRule.CanBeEmpty);

            var separator = value.IndexOf(' ');
            if (separator > 0)
            {
                var prefix = value.Substring(0, separator);
                switch (prefix)
                {
                    case "public":
                    case "package":
                        access = AccessModifier.Public;
                        break;

                    case "internal":
                    case "library":
                        access = AccessModifier.Internal;
                        break;

                    case "protected":
                    case "file":
                        access = AccessModifier.Protected;
                        break;

                    case "private":
                    case "fragment":
                        access = AccessModifier.Private;
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

        public string[] GetAttributeInlineDirectorySyntax(SourceLineNumber sourceLineNumbers, XAttribute attribute, bool resultUsedToCreateReference = false)
        {
            string[] result = null;
            var value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (!String.IsNullOrEmpty(value))
            {
                var pathStartsAt = 0;
                result = value.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                if (result[0].EndsWith(":", StringComparison.Ordinal))
                {
                    var id = result[0].TrimEnd(':');
                    if (1 == result.Length)
                    {
                        this.Messaging.Write(ErrorMessages.InlineDirectorySyntaxRequiresPath(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, id));
                        return null;
                    }
                    else if (!this.IsValidIdentifier(id))
                    {
                        this.Messaging.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, id));
                        return null;
                    }

                    pathStartsAt = 1;
                }
                else if (resultUsedToCreateReference && 1 == result.Length)
                {
                    if (value.EndsWith("\\", StringComparison.Ordinal))
                    {
                        if (!this.IsValidLongFilename(result[0], false, false))
                        {
                            this.Messaging.Write(ErrorMessages.IllegalLongFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, result[0]));
                            return null;
                        }
                    }
                    else if (!this.IsValidIdentifier(result[0]))
                    {
                        this.Messaging.Write(ErrorMessages.IllegalIdentifier(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, result[0]));
                        return null;
                    }

                    return result; // return early to avoid additional checks below.
                }

                // Check each part of the relative path to ensure that it is a valid directory name.
                for (var i = pathStartsAt; i < result.Length; ++i)
                {
                    if (!this.IsValidLongFilename(result[i], false, false))
                    {
                        this.Messaging.Write(ErrorMessages.IllegalLongFilename(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value, result[i]));
                        return null;
                    }
                }

                if (1 < result.Length && !value.EndsWith("\\", StringComparison.Ordinal))
                {
                    this.Messaging.Write(WarningMessages.BackslashTerminateInlineDirectorySyntax(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                }
            }

            return result;
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

            if (0 < value.Length)
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
                    var normalizedPath = value.Replace('\\', '/');
                    if (normalizedPath.StartsWith("../", StringComparison.Ordinal) || normalizedPath.Contains("/../"))
                    {
                        this.Messaging.Write(ErrorMessages.PayloadMustBeRelativeToCache(sourceLineNumbers, attribute.Parent.Name.LocalName, attribute.Name.LocalName, value));
                    }
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
                if (Version.TryParse(value, out var version))
                {
                    return version.ToString();
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
            if (String.IsNullOrEmpty(filename))
            {
                return false;
            }
            else if (filename.Length > 259)
            {
                return false;
            }

            // Check for a non-period character (all periods is not legal)
            var allPeriods = true;
            foreach (var character in filename)
            {
                if ('.' != character)
                {
                    allPeriods = false;
                    break;
                }
            }

            if (allPeriods)
            {
                return false;
            }

            if (allowWildcards)
            {
                return filename.IndexOfAny(Common.IllegalWildcardLongFilenameCharacters) == -1;
            }
            else if (allowRelative)
            {
                return filename.IndexOfAny(Common.IllegalRelativeLongFilenameCharacters) == -1;
            }
            else
            {
                return filename.IndexOfAny(Common.IllegalLongFilenameCharacters) == -1;
            }
        }

        public bool IsValidShortFilename(string filename, bool allowWildcards = false)
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

        public void ParseForExtensionElements(IEnumerable<ICompilerExtension> extensions, Intermediate intermediate, IntermediateSection section, XElement element)
        {
            foreach (var child in element.Elements())
            {
                if (element.Name.Namespace == child.Name.Namespace)
                {
                    this.UnexpectedElement(element, child);
                }
                else
                {
                    this.ParseExtensionElement(extensions, intermediate, section, element, child);
                }
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

            if (null != beforeAction)
            {
                if (WindowsInstallerStandard.IsStandardAction(beforeAction))
                {
                    this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixAction, sequence.ToString(), beforeAction);
                }
                else
                {
                    this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, beforeAction);
                }
            }

            if (null != afterAction)
            {
                if (WindowsInstallerStandard.IsStandardAction(afterAction))
                {
                    this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.WixAction, sequence.ToString(), afterAction);
                }
                else
                {
                    this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, afterAction);
                }
            }

            return actionSymbol;
        }

        public void CreateCustomActionReference(SourceLineNumber sourceLineNumbers, IntermediateSection section, string customAction, Platform currentPlatform, CustomActionPlatforms supportedPlatforms)
        {
            if (!this.Messaging.EncounteredError)
            {
                var name = String.Concat("Wix4", customAction);
                var suffix = "_X86";

                switch (currentPlatform)
                {
                    case Platform.X64:
                        if ((supportedPlatforms & CustomActionPlatforms.X64) == CustomActionPlatforms.X64)
                        {
                            suffix = "_X64";
                        }
                        break;
                    case Platform.ARM:
                        if ((supportedPlatforms & CustomActionPlatforms.ARM) == CustomActionPlatforms.ARM)
                        {
                            suffix = "_A32";
                        }
                        break;
                    case Platform.ARM64:
                        if ((supportedPlatforms & CustomActionPlatforms.ARM64) == CustomActionPlatforms.ARM64)
                        {
                            suffix = "_A64";
                        }
                        break;
                }

                this.CreateSimpleReference(section, sourceLineNumbers, SymbolDefinitions.CustomAction, name + suffix);
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
