// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using WixToolset.Data;
    using WixToolset.Data.Symbols;

    /// <summary>
    /// Compiler of the WiX toolset.
    /// </summary>
    internal partial class Compiler : ICompiler
    {
        /// <summary>
        /// Parses a custom table element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <remarks>not cleaned</remarks>
        private void ParseCustomTableElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string tableId = null;
            var unreal = false;
            var columns = new List<WixCustomTableColumnSymbol>();
            var foundColumns = false;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                    case "Id":
                        tableId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                        break;
                    case "Unreal":
                        unreal = YesNoType.Yes == this.Core.GetAttributeYesNoValue(sourceLineNumbers, attrib);
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

            if (null == tableId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }
            else if (31 < tableId.Length)
            {
                this.Core.Write(ErrorMessages.CustomTableNameTooLong(sourceLineNumbers, node.Name.LocalName, "Id", tableId));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "Column":
                            foundColumns = true;

                            var column = this.ParseColumnElement(child, childSourceLineNumbers, tableId);
                            if (column != null)
                            {
                                columns.Add(column);
                            }
                            break;
                        case "Row":
                            this.ParseRowElement(child, childSourceLineNumbers, tableId);
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

            if (columns.Count > 0)
            {
                if (!columns.Where(c => c.PrimaryKey).Any())
                {
                    this.Core.Write(ErrorMessages.CustomTableMissingPrimaryKey(sourceLineNumbers));
                }

                if (!this.Core.EncounteredError)
                {
                    var columnNames = String.Join(new string(WixCustomTableSymbol.ColumnNamesSeparator, 1), columns.Select(c => c.Name));

                    this.Core.AddSymbol(new WixCustomTableSymbol(sourceLineNumbers, new Identifier(AccessModifier.Global, tableId))
                    {
                        ColumnNames = columnNames,
                        Unreal = unreal,
                    });
                }
                else if (!foundColumns)
                {
                    this.Core.Write(ErrorMessages.ExpectedElement(sourceLineNumbers, node.Name.LocalName, "Column"));
                }
            }
        }

        /// <summary>
        /// Parses a CustomTableRef element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        private void ParseCustomTableRefElement(XElement node)
        {
            var sourceLineNumbers = Preprocessor.GetSourceLineNumbers(node);
            string tableId = null;

            foreach (var attrib in node.Attributes())
            {
                if (String.IsNullOrEmpty(attrib.Name.NamespaceName) || CompilerCore.WixNamespace == attrib.Name.Namespace)
                {
                    switch (attrib.Name.LocalName)
                    {
                        case "Id":
                            tableId = this.Core.GetAttributeIdentifierValue(sourceLineNumbers, attrib);
                            this.Core.CreateSimpleReference(sourceLineNumbers, SymbolDefinitions.WixCustomTable, tableId);
                            this.Core.EnsureTable(sourceLineNumbers, tableId);
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

            if (null == tableId)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(sourceLineNumbers, node.Name.LocalName, "Id"));
            }

            foreach (var child in node.Elements())
            {
                if (CompilerCore.WixNamespace == child.Name.Namespace)
                {
                    var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                    switch (child.Name.LocalName)
                    {
                        case "Row":
                            this.ParseRowElement(child, childSourceLineNumbers, tableId);
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
        }

        /// <summary>
        /// Parses a Column element.
        /// </summary>
        /// <param name="child">Element to parse.</param>
        /// <param name="childSourceLineNumbers">Element's SourceLineNumbers.</param>
        /// <param name="tableId">Table Id.</param>
        private WixCustomTableColumnSymbol ParseColumnElement(XElement child, SourceLineNumber childSourceLineNumbers, string tableId)
        {
            string columnName = null;
            IntermediateFieldType? columnType = null;
            var description = String.Empty;
            int? keyColumn = null;
            var keyTable = String.Empty;
            var localizable = false;
            long? maxValue = null;
            long? minValue = null;
            WixCustomTableColumnCategoryType? category = null;
            var modularization = WixCustomTableColumnModularizeType.None;
            var nullable = false;
            var primaryKey = false;
            var setValues = String.Empty;
            var columnUnreal = false;
            var width = 0;

            foreach (var childAttrib in child.Attributes())
            {
                switch (childAttrib.Name.LocalName)
                {
                    case "Id":
                        columnName = this.Core.GetAttributeIdentifierValue(childSourceLineNumbers, childAttrib);
                        break;
                    case "Category":
                        var categoryValue = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                        switch (categoryValue)
                        {
                            case "text":
                                category = WixCustomTableColumnCategoryType.Text;
                                break;
                            case "upperCase":
                                category = WixCustomTableColumnCategoryType.UpperCase;
                                break;
                            case "lowerCase":
                                category = WixCustomTableColumnCategoryType.LowerCase;
                                break;
                            case "integer":
                                category = WixCustomTableColumnCategoryType.Integer;
                                break;
                            case "doubleInteger":
                                category = WixCustomTableColumnCategoryType.DoubleInteger;
                                break;
                            case "timeDate":
                                category = WixCustomTableColumnCategoryType.TimeDate;
                                break;
                            case "identifier":
                                category = WixCustomTableColumnCategoryType.Identifier;
                                break;
                            case "property":
                                category = WixCustomTableColumnCategoryType.Property;
                                break;
                            case "filename":
                                category = WixCustomTableColumnCategoryType.Filename;
                                break;
                            case "wildCardFilename":
                                category = WixCustomTableColumnCategoryType.WildCardFilename;
                                break;
                            case "path":
                                category = WixCustomTableColumnCategoryType.Path;
                                break;
                            case "paths":
                                category = WixCustomTableColumnCategoryType.Paths;
                                break;
                            case "anyPath":
                                category = WixCustomTableColumnCategoryType.AnyPath;
                                break;
                            case "defaultDir":
                                category = WixCustomTableColumnCategoryType.DefaultDir;
                                break;
                            case "regPath":
                                category = WixCustomTableColumnCategoryType.RegPath;
                                break;
                            case "formatted":
                                category = WixCustomTableColumnCategoryType.Formatted;
                                break;
                            case "formattedSddl":
                                category = WixCustomTableColumnCategoryType.FormattedSddl;
                                break;
                            case "template":
                                category = WixCustomTableColumnCategoryType.Template;
                                break;
                            case "condition":
                                category = WixCustomTableColumnCategoryType.Condition;
                                break;
                            case "guid":
                                category = WixCustomTableColumnCategoryType.Guid;
                                break;
                            case "version":
                                category = WixCustomTableColumnCategoryType.Version;
                                break;
                            case "language":
                                category = WixCustomTableColumnCategoryType.Language;
                                break;
                            case "binary":
                                category = WixCustomTableColumnCategoryType.Binary;
                                break;
                            case "customSource":
                                category = WixCustomTableColumnCategoryType.CustomSource;
                                break;
                            case "cabinet":
                                category = WixCustomTableColumnCategoryType.Cabinet;
                                break;
                            case "shortcut":
                                category = WixCustomTableColumnCategoryType.Shortcut;
                                break;
                            case "":
                                break;
                            default:
                                this.Core.Write(ErrorMessages.IllegalAttributeValue(childSourceLineNumbers, child.Name.LocalName, "Category", categoryValue,
                                    "text", "upperCase", "lowerCase", "integer", "doubleInteger", "timeDate", "identifier", "property", "filename",
                                    "wildCardFilename", "path", "paths", "anyPath", "defaultDir", "regPath", "formatted", "formattedSddl", "template",
                                    "condition", "guid", "version", "language", "binary", "customSource", "cabinet", "shortcut"));
                                columnType = IntermediateFieldType.String; // set a value to prevent expected attribute error below.
                                break;
                        }
                        break;
                    case "Description":
                        description = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                        break;
                    case "KeyColumn":
                        keyColumn = this.Core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib, 1, 32);
                        break;
                    case "KeyTable":
                        keyTable = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                        break;
                    case "Localizable":
                        localizable = YesNoType.Yes == this.Core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                        break;
                    case "MaxValue":
                        maxValue = this.Core.GetAttributeLongValue(childSourceLineNumbers, childAttrib, Int32.MinValue + 1, Int32.MaxValue);
                        break;
                    case "MinValue":
                        minValue = this.Core.GetAttributeLongValue(childSourceLineNumbers, childAttrib, Int32.MinValue + 1, Int32.MaxValue);
                        break;
                    case "Modularize":
                        var modularizeValue = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                        switch (modularizeValue)
                        {
                            case "column":
                                modularization = WixCustomTableColumnModularizeType.Column;
                                break;
                            case "companionFile":
                                modularization = WixCustomTableColumnModularizeType.CompanionFile;
                                break;
                            case "condition":
                                modularization = WixCustomTableColumnModularizeType.Condition;
                                break;
                            case "controlEventArgument":
                                modularization = WixCustomTableColumnModularizeType.ControlEventArgument;
                                break;
                            case "controlText":
                                modularization = WixCustomTableColumnModularizeType.ControlText;
                                break;
                            case "icon":
                                modularization = WixCustomTableColumnModularizeType.Icon;
                                break;
                            case "none":
                                modularization = WixCustomTableColumnModularizeType.None;
                                break;
                            case "property":
                                modularization = WixCustomTableColumnModularizeType.Property;
                                break;
                            case "semicolonDelimited":
                                modularization = WixCustomTableColumnModularizeType.SemicolonDelimited;
                                break;
                            case "":
                                break;
                            default:
                                this.Core.Write(ErrorMessages.IllegalAttributeValue(childSourceLineNumbers, child.Name.LocalName, "Modularize", modularizeValue, "column", "companionFile", "condition", "controlEventArgument", "controlText", "icon", "property", "semicolonDelimited"));
                                columnType = IntermediateFieldType.String; // set a value to prevent expected attribute error below.
                                break;
                        }
                        break;
                    case "Nullable":
                        nullable = YesNoType.Yes == this.Core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                        break;
                    case "PrimaryKey":
                        primaryKey = YesNoType.Yes == this.Core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                        break;
                    case "Set":
                        setValues = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                        break;
                    case "Type":
                        var typeValue = this.Core.GetAttributeValue(childSourceLineNumbers, childAttrib);
                        switch (typeValue)
                        {
                            case "binary":
                                columnType = IntermediateFieldType.Path;
                                break;
                            case "int":
                                columnType = IntermediateFieldType.Number;
                                break;
                            case "string":
                                columnType = IntermediateFieldType.String;
                                break;
                            case "":
                                break;
                            default:
                                this.Core.Write(ErrorMessages.IllegalAttributeValue(childSourceLineNumbers, child.Name.LocalName, "Type", typeValue, "binary", "int", "string"));
                                columnType = IntermediateFieldType.String; // set a value to prevent expected attribute error below.
                                break;
                        }
                        break;
                    case "Width":
                        width = this.Core.GetAttributeIntegerValue(childSourceLineNumbers, childAttrib, 0, Int32.MaxValue);
                        break;
                    case "Unreal":
                        columnUnreal = YesNoType.Yes == this.Core.GetAttributeYesNoValue(childSourceLineNumbers, childAttrib);
                        break;
                    default:
                        this.Core.UnexpectedAttribute(child, childAttrib);
                        break;
                }
            }

            if (null == columnName)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Id"));
            }

            if (!columnType.HasValue)
            {
                this.Core.Write(ErrorMessages.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Type"));
            }
            else if (columnType == IntermediateFieldType.Number)
            {
                if (2 != width && 4 != width)
                {
                    this.Core.Write(ErrorMessages.CustomTableIllegalColumnWidth(childSourceLineNumbers, child.Name.LocalName, "Width", width));
                }
            }
            else if (columnType == IntermediateFieldType.Path)
            {
                if (!category.HasValue)
                {
                    category = WixCustomTableColumnCategoryType.Binary;
                }
                else if (category != WixCustomTableColumnCategoryType.Binary)
                {
                    this.Core.Write(ErrorMessages.ExpectedBinaryCategory(childSourceLineNumbers));
                }
            }

            this.Core.ParseForExtensionElements(child);

            if (this.Core.EncounteredError)
            {
                return null;
            }

            var attributes = primaryKey ? WixCustomTableColumnSymbolAttributes.PrimaryKey : WixCustomTableColumnSymbolAttributes.None;
            attributes |= localizable ? WixCustomTableColumnSymbolAttributes.Localizable : WixCustomTableColumnSymbolAttributes.None;
            attributes |= nullable ? WixCustomTableColumnSymbolAttributes.Nullable : WixCustomTableColumnSymbolAttributes.None;
            attributes |= columnUnreal ? WixCustomTableColumnSymbolAttributes.Unreal : WixCustomTableColumnSymbolAttributes.None;

            var column = this.Core.AddSymbol(new WixCustomTableColumnSymbol(childSourceLineNumbers, new Identifier(AccessModifier.Section, tableId, columnName))
            {
                TableRef = tableId,
                Name = columnName,
                Type = columnType.Value,
                Attributes = attributes,
                Width = width,
                Category = category,
                Description = description,
                KeyColumn = keyColumn,
                KeyTable = keyTable,
                MaxValue = maxValue,
                MinValue = minValue,
                Modularize = modularization,
                Set = setValues,
            });
            return column;
        }

        /// <summary>
        /// Parses a Row element.
        /// </summary>
        /// <param name="node">Element to parse.</param>
        /// <param name="sourceLineNumbers">Element's SourceLineNumbers.</param>
        /// <param name="tableId">Table Id.</param>
        private void ParseRowElement(XElement node, SourceLineNumber sourceLineNumbers, string tableId)
        {
            var rowId = Guid.NewGuid().ToString("N").ToUpperInvariant();

            foreach (var attrib in node.Attributes())
            {
                this.Core.ParseExtensionAttribute(node, attrib);
            }

            foreach (var child in node.Elements())
            {
                var childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(child);
                switch (child.Name.LocalName)
                {
                    case "Data":
                        string columnName = null;
                        string data = null;
                        foreach (var attrib in child.Attributes())
                        {
                            switch (attrib.Name.LocalName)
                            {
                                case "Column":
                                    columnName = this.Core.GetAttributeValue(childSourceLineNumbers, attrib);
                                    break;
                                case "Value":
                                    data = this.Core.GetAttributeValue(childSourceLineNumbers, attrib);
                                    break;
                                default:
                                    this.Core.ParseExtensionAttribute(child, attrib);
                                    break;
                            }
                        }

                        this.Core.InnerTextDisallowed(node);

                        if (null == columnName)
                        {
                            this.Core.Write(ErrorMessages.ExpectedAttribute(childSourceLineNumbers, child.Name.LocalName, "Column"));
                        }

                        if (!this.Core.EncounteredError)
                        {
                            this.Core.AddSymbol(new WixCustomTableCellSymbol(childSourceLineNumbers, new Identifier(AccessModifier.Section, tableId, rowId, columnName))
                            {
                                RowId = rowId,
                                ColumnRef = columnName,
                                TableRef = tableId,
                                Data = data
                            });
                        }
                        break;
                    default:
                        this.Core.UnexpectedElement(node, child);
                        break;
                }
            }
        }
    }
}
