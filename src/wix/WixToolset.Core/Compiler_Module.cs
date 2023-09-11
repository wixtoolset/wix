// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Globalization;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;
    using WixToolset.Extensibility;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        /// <summary>
        /// Parses a module element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseModuleElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            var codepage = 0;
            string moduleId = null;
            string version = null;
            var setCodepage = false;
            var setComments = false;
            var setPackageName = false;
            var setKeywords = false;
            var ignoredForMergeModules = false;

            this.GetDefaultPlatformAndInstallerVersion(out var platform, out var msiVersion);

            this.activeName = null;
            this.activeLanguage = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        this.activeName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        if ("PUT-MODULE-NAME-HERE" == this.activeName)
                        {
                            this.Core.Write(WarningMessages.PlaceholderValue(sourceLineNumbers, node.Name.LocalName, attrib.Name.LocalName, this.activeName));
                        }
                        else
                        {
                            this.activeName = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        }
                        break;
                    case "Codepage":
                        codepage = this.Core.GetAttributeCodePageValue(sourceLineNumbers, attrib);
                        break;
                    case "Guid":
                        moduleId = this.Core.GetAttributeGuidValue(sourceLineNumbers, attrib, false);
                        break;
                    case "InstallerVersion":
                        msiVersion = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int32.MaxValue);
                        break;
                    case "Language":
                        this.activeLanguage = this.Core.GetAttributeLocalizableIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "Version":
                        version = this.Core.GetAttributeVersionValue(sourceLineNumbers, attrib);
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

            if (null == this.activeName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            if (null == moduleId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Guid"));
            }

            if (null == this.activeLanguage)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Language"));
            }

            if (null == version)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Version"));
            }

            try
            {
                this.compilingModule = true; // notice that we are actually building a Merge Module here
                this.Core.CreateActiveSection(this.activeName, SectionType.Module, this.Context.CompilationId);

                foreach (var child in node.Elements())
                {
                    if (CompilerCore.WixNamespace == child.Name.Namespace)
                    {
                        switch (child.Name.LocalName)
                        {
                        case "AdminExecuteSequence":
                            this.ParseSequenceElement(child, SequenceTable.AdminExecuteSequence);
                            break;
                        case "AdminUISequence":
                            this.ParseSequenceElement(child, SequenceTable.AdminUISequence);
                            break;
                        case "AdvertiseExecuteSequence":
                            this.ParseSequenceElement(child, SequenceTable.AdvertiseExecuteSequence);
                            break;
                        case "InstallExecuteSequence":
                            this.ParseSequenceElement(child, SequenceTable.InstallExecuteSequence);
                            break;
                        case "InstallUISequence":
                            this.ParseSequenceElement(child, SequenceTable.InstallUISequence);
                            break;
                        case "AppId":
                            this.ParseAppIdElement(child, null, YesNoType.Yes, null, null, null);
                            break;
                        case "Binary":
                            this.ParseBinaryElement(child);
                            break;
                        case "Component":
                            this.ParseComponentElement(child, ComplexReferenceParentType.Module, this.activeName, this.activeLanguage, CompilerConstants.IntegerNotSet, null, null);
                            break;
                        case "ComponentGroupRef":
                            this.ParseComponentGroupRefElement(child, ComplexReferenceParentType.Module, this.activeName, this.activeLanguage);
                            break;
                        case "ComponentRef":
                            this.ParseComponentRefElement(child, ComplexReferenceParentType.Module, this.activeName, this.activeLanguage);
                            break;
                        case "Configuration":
                            this.ParseConfigurationElement(child);
                            break;
                        case "CustomAction":
                            this.ParseCustomActionElement(child);
                            break;
                        case "CustomActionRef":
                            this.ParseSimpleRefElement(child, SymbolDefinitions.CustomAction);
                            break;
                        case "CustomTable":
                            this.ParseCustomTableElement(child);
                            break;
                        case "CustomTableRef":
                            this.ParseCustomTableRefElement(child);
                            break;
                        case "Dependency":
                            this.ParseDependencyElement(child);
                            break;
                        case "Directory":
                            this.ParseDirectoryElement(child, null, CompilerConstants.IntegerNotSet, String.Empty);
                            break;
                        case "DirectoryRef":
                            this.ParseDirectoryRefElement(child);
                            break;
                        case "EmbeddedChainer":
                            this.ParseEmbeddedChainerElement(child);
                            break;
                        case "EmbeddedChainerRef":
                            this.ParseSimpleRefElement(child, SymbolDefinitions.MsiEmbeddedChainer);
                            break;
                        case "EnsureTable":
                            this.ParseEnsureTableElement(child);
                            break;
                        case "Exclusion":
                            this.ParseExclusionElement(child);
                            break;
                        case "File":
                            this.ParseNakedFileElement(child, ComplexReferenceParentType.Module, this.activeName, null, null);
                            break;
                        case "Icon":
                            this.ParseIconElement(child);
                            break;
                        case "IgnoreTable":
                            this.ParseIgnoreTableElement(child);
                            break;
                        case "Property":
                            this.ParsePropertyElement(child);
                            break;
                        case "PropertyRef":
                            this.ParseSimpleRefElement(child, SymbolDefinitions.Property);
                            break;
                        case "Requires":
                            this.ParseRequiresElement(child, null);
                            break;
                        case "SetDirectory":
                            this.ParseSetDirectoryElement(child);
                            break;
                        case "SetProperty":
                            this.ParseSetPropertyElement(child);
                            break;
                        case "SFPCatalog":
                            string parentName = null;
                            this.ParseSFPCatalogElement(child, ref parentName);
                            break;
                        case "StandardDirectory":
                            this.ParseStandardDirectoryElement(child);
                            break;
                        case "Substitution":
                            this.ParseSubstitutionElement(child);
                            break;
                        case "SummaryInformation":
                            this.ParseSummaryInformationElement(child, ref setCodepage, ref setComments, ref setPackageName, ref setKeywords, ref ignoredForMergeModules);
                            break;
                        case "UI":
                            this.ParseUIElement(child);
                            break;
                        case "UIRef":
                            this.ParseSimpleRefElement(child, SymbolDefinitions.WixUI);
                            break;
                        case "WixVariable":
                            this.ParseWixVariableElement(child);
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
                    if (!setPackageName)
                    {
                        this.Core.AddSymbol(new SummaryInformationSymbol(sourceLineNumbers)
                        {
                            PropertyId = SummaryInformationType.Subject,
                            Value = this.activeName
                        });
                    }

                    if (!setKeywords)
                    {
                        this.Core.AddSymbol(new SummaryInformationSymbol(sourceLineNumbers)
                        {
                            PropertyId = SummaryInformationType.Keywords,
                            Value = "MergeModule, MSI, database"
                        });
                    }

                    var symbol = this.Core.AddSymbol(new WixModuleSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, this.activeName, this.activeLanguage))
                    {
                        ModuleId = this.activeName,
                        Language = this.activeLanguage,
                        Version = version
                    });

                    this.Core.AddSymbol(new SummaryInformationSymbol(sourceLineNumbers)
                    {
                        PropertyId = SummaryInformationType.PackageCode,
                        Value = moduleId
                    });

                    this.Core.AddSymbol(new SummaryInformationSymbol(sourceLineNumbers)
                    {
                        PropertyId = SummaryInformationType.Title,
                        Value = "Merge Module"
                    });

                    this.Core.AddSymbol(new SummaryInformationSymbol(sourceLineNumbers)
                    {
                        PropertyId = SummaryInformationType.WordCount,
                        Value = "0"
                    });

                    if (!setComments)
                    {
                        this.Core.AddSymbol(new SummaryInformationSymbol(sourceLineNumbers)
                        {
                            PropertyId = SummaryInformationType.Comments,
                            Value = String.Format(CultureInfo.InvariantCulture, "This merge module contains the logic and data required to install {0}.", this.activeName)
                        });
                    }

                    this.ValidateAndAddCommonSummaryInformationSymbols(sourceLineNumbers, msiVersion, platform, this.activeLanguage);

                    this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixFragment, WixStandardLibraryIdentifiers.WixStandardModuleReferences);
                }
            }
            finally
            {
                this.compilingModule = false; // notice that we are no longer building a Merge Module here
            }
        }

        /// <summary>
        /// Parses a dependency element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseDependencyElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string requiredId = null;
            var requiredLanguage = CompilerConstants.IntegerNotSet;
            string requiredVersion = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "RequiredId":
                        requiredId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "RequiredLanguage":
                        requiredLanguage = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "RequiredVersion":
                        requiredVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == requiredId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RequiredId"));
                requiredId = String.Empty;
            }

            if (CompilerConstants.IntegerNotSet == requiredLanguage)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "RequiredLanguage"));
                requiredLanguage = CompilerConstants.IllegalInteger;
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var symbol = this.Core.AddSymbol(new ModuleDependencySymbol(sourceLineNumbers)
                {
                    ModuleID = this.activeName,
                    RequiredID = requiredId,
                    RequiredLanguage  = requiredLanguage,
                    RequiredVersion = requiredVersion
                });

                symbol.Set((int)ModuleDependencySymbolFields.ModuleLanguage, this.activeLanguage);
            }
        }

        /// <summary>
        /// Parses an exclusion element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseExclusionElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string excludedId = null;
            var excludeExceptLanguage = CompilerConstants.IntegerNotSet;
            var excludeLanguage = CompilerConstants.IntegerNotSet;
            var excludedLanguageField = "0";
            string excludedMaxVersion = null;
            string excludedMinVersion = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "ExcludedId":
                        excludedId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "ExcludeExceptLanguage":
                        excludeExceptLanguage = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "ExcludeLanguage":
                        excludeLanguage = this.Core.GetAttributeIntegerValue(sourceLineNumbers, attrib, 0, Int16.MaxValue);
                        break;
                    case "ExcludedMaxVersion":
                        excludedMaxVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "ExcludedMinVersion":
                        excludedMinVersion = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == excludedId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "ExcludedId"));
                excludedId = String.Empty;
            }

            if (CompilerConstants.IntegerNotSet != excludeExceptLanguage && CompilerConstants.IntegerNotSet != excludeLanguage)
            {
                this.Core.Write(ErrorMessages.IllegalModuleExclusionLanguageAttributes(sourceLineNumbers));
            }
            else if (CompilerConstants.IntegerNotSet != excludeExceptLanguage)
            {
                excludedLanguageField = Convert.ToString(-excludeExceptLanguage, CultureInfo.InvariantCulture);
            }
            else if (CompilerConstants.IntegerNotSet != excludeLanguage)
            {
                excludedLanguageField = Convert.ToString(excludeLanguage, CultureInfo.InvariantCulture);
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                var symbol = this.Core.AddSymbol(new ModuleExclusionSymbol(sourceLineNumbers)
                {
                    ModuleID = this.activeName,
                    ExcludedID = excludedId,
                    ExcludedMinVersion = excludedMinVersion,
                    ExcludedMaxVersion = excludedMaxVersion
                });

                symbol.Set((int)ModuleExclusionSymbolFields.ModuleLanguage, this.activeLanguage);
                symbol.Set((int)ModuleExclusionSymbolFields.ExcludedLanguage, excludedLanguageField);
            }
        }

        /// <summary>
        /// Parses a configuration element for a configurable merge module.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseConfigurationElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string contextData = null;
            string defaultValue = null;
            string description = null;
            string displayName = null;
            var format = CompilerConstants.IntegerNotSet;
            string helpKeyword = null;
            string helpLocation = null;
            bool keyNoOrphan = false;
            bool nonNullable = false;
            Identifier name = null;
            string type = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Name":
                        name = this.Core.GetAttributeIdentifier(sourceLineNumbers, attrib);
                        break;
                    case "ContextData":
                        contextData = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Description":
                        description = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "DefaultValue":
                        defaultValue = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "DisplayName":
                        displayName = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Format":
                        var formatStr = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        switch (formatStr)
                        {
                        case "Text":
                        case "text":
                            format = 0;
                            break;
                        case "Key":
                        case "key":
                            format = 1;
                            break;
                        case "Integer":
                        case "integer":
                            format = 2;
                            break;
                        case "Bitfield":
                        case "bitfield":
                            format = 3;
                            break;
                        case "":
                            break;
                        default:
                            this.Core.Write(ErrorMessages.IllegalAttributeValue(sourceLineNumbers, node.Name.LocalName, "Format", formatStr, "Text", "Key", "Integer", "Bitfield"));
                            break;
                        }
                        break;
                    case "HelpKeyword":
                        helpKeyword = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "HelpLocation":
                        helpLocation = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "KeyNoOrphan":
                        keyNoOrphan = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "NonNullable":
                        nonNullable = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
                        break;
                    case "Type":
                        type = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == name)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Name"));
            }

            if (CompilerConstants.IntegerNotSet == format)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Format"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new ModuleConfigurationSymbol(sourceLineNumbers, name)
                {
                    Format = format,
                    Type = type,
                    ContextData = contextData,
                    DefaultValue = defaultValue,
                    KeyNoOrphan = keyNoOrphan,
                    NonNullable = nonNullable,
                    DisplayName = displayName,
                    Description = description,
                    HelpLocation = helpLocation,
                    HelpKeyword = helpKeyword
                });
            }
        }

        /// <summary>
        /// Parses a substitution element for a configurable merge module.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseSubstitutionElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string column = null;
            string rowKeys = null;
            string table = null;
            string value = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Column":
                        column = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Row":
                        rowKeys = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
                        break;
                    case "Table":
                        table = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Value":
                        value = this.Core.GetAttributeValue(sourceLineNumbers, attrib);
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

            if (null == column)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Column"));
                column = String.Empty;
            }

            if (null == table)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Table"));
                table = String.Empty;
            }

            if (null == rowKeys)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Row"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new ModuleSubstitutionSymbol(sourceLineNumbers)
                {
                    Table = table,
                    Row = rowKeys,
                    Column = column,
                    Value = value
                });
            }
        }

        /// <summary>
        /// Parses an IgnoreTable element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseIgnoreTableElement(XElement node)
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

            if (null == id)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            this.Core.ParseForExtensionElements(node);

            if (!this.Core.EncounteredError)
            {
                this.Core.AddSymbol(new ModuleIgnoreTableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Section, id)));
            }
        }
    }
}
