// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data.WindowsInstaller
{
    using System;
    using System.Globalization;
    using System.Xml;

    /// <summary>
    /// Definition of a table's column.
    /// </summary>
    public sealed class ColumnDefinition : IComparable<ColumnDefinition>
    {
        /// <summary>
        /// Creates a new column definition.
        /// </summary>
        /// <param name="name">Name of column.</param>
        /// <param name="type">Type of column</param>
        /// <param name="length">Length of column.</param>
        /// <param name="primaryKey">If column is primary key.</param>
        /// <param name="nullable">If column is nullable.</param>
        /// <param name="category">Validation category for column.</param>
        /// <param name="minValue">Optional minimum value for the column.</param>
        /// <param name="maxValue">Optional maximum value for the colum.</param>
        /// <param name="keyTable">Optional name of table for foreign key.</param>
        /// <param name="keyColumn">Optional name of column for foreign key.</param>
        /// <param name="possibilities">Set of possible values for column.</param>
        /// <param name="description">Description of column in vaidation table.</param>
        /// <param name="modularizeType">Type of modularization for column</param>
        /// <param name="forceLocalizable">If the column is localizable.</param>
        /// <param name="useCData">If whitespace should be preserved in a CDATA node.</param>
        public ColumnDefinition(string name, ColumnType type, int length, bool primaryKey, bool nullable, ColumnCategory category, long? minValue = null, long? maxValue = null, string keyTable = null, int? keyColumn = null, string possibilities = null, string description = null, ColumnModularizeType? modularizeType = null, bool forceLocalizable = false, bool useCData = false, bool unreal = false)
        {
            this.Name = name;
            this.Type = type;
            this.Length = length;
            this.PrimaryKey = primaryKey;
            this.Nullable = nullable;
            this.ModularizeType = CalculateModularizationType(modularizeType, category);
            this.IsLocalizable = forceLocalizable || ColumnType.Localized == type;
            this.MinValue = minValue;
            this.MaxValue = maxValue;
            this.KeyTable = keyTable;
            this.KeyColumn = keyColumn;
            this.Category = category;
            this.Possibilities = possibilities;
            this.Description = description;
            this.UseCData = useCData;
            this.Unreal = unreal;
        }

        /// <summary>
        /// Gets whether this column was added via a transform.
        /// </summary>
        /// <value>Whether this column was added via a transform.</value>
        public bool Added { get; set; }

        /// <summary>
        /// Gets the name of the column.
        /// </summary>
        /// <value>Name of column.</value>
        public string Name { get; }

        /// <summary>
        /// Gets the type of the column.
        /// </summary>
        /// <value>Type of column.</value>
        public ColumnType Type { get; }

        /// <summary>
        /// Gets the length of the column.
        /// </summary>
        /// <value>Length of column.</value>
        public int Length { get; }

        /// <summary>
        /// Gets if the column is a primary key.
        /// </summary>
        /// <value>true if column is primary key.</value>
        public bool PrimaryKey { get; }

        /// <summary>
        /// Gets if the column is nullable.
        /// </summary>
        /// <value>true if column is nullable.</value>
        public bool Nullable { get; }

        /// <summary>
        /// Gets the type of modularization for this column.
        /// </summary>
        /// <value>Column's modularization type.</value>
        public ColumnModularizeType ModularizeType { get; }

        /// <summary>
        /// Gets if the column is localizable. Can be because the type is localizable, or because the column 
        /// was explicitly set to be so.
        /// </summary>
        /// <value>true if column is localizable.</value>
        public bool IsLocalizable { get; }

        /// <summary>
        /// Gets the minimum value for the column.
        /// </summary>
        /// <value>Minimum value for the column.</value>
        public long? MinValue { get; }

        /// <summary>
        /// Gets the maximum value for the column.
        /// </summary>
        /// <value>Maximum value for the column.</value>
        public long? MaxValue { get; }

        /// <summary>
        /// Gets the table that has the foreign key for this column
        /// </summary>
        /// <value>Foreign key table name.</value>
        public string KeyTable { get; }

        /// <summary>
        /// Gets the foreign key column that this column refers to.
        /// </summary>
        /// <value>Foreign key column.</value>
        public int? KeyColumn { get; }

        /// <summary>
        /// Gets the validation category for this column.
        /// </summary>
        /// <value>Validation category.</value>
        public ColumnCategory Category { get; }

        /// <summary>
        /// Gets the set of possibilities for this column.
        /// </summary>
        /// <value>Set of possibilities for this column.</value>
        public string Possibilities { get; }

        /// <summary>
        /// Gets the description for this column.
        /// </summary>
        /// <value>Description of column.</value>
        public string Description { get; }

        /// <summary>
        /// Gets if whitespace should be preserved in a CDATA node.
        /// </summary>
        /// <value>true if whitespace should be preserved in a CDATA node.</value>
        public bool UseCData { get; }

        /// <summary>
        /// Gets if column is Unreal.
        /// </summary>
        /// <value>true if column should not be included in idts.</value>
        public bool Unreal { get; }

        /// <summary>
        /// Parses a column definition in a table definition.
        /// </summary>
        /// <param name="reader">Reader to get data from.</param>
        /// <returns>The ColumnDefintion represented by the Xml.</returns>
        internal static ColumnDefinition Read(XmlReader reader)
        {
            if (!reader.LocalName.Equals("columnDefinition"))
            {
                throw new XmlException();
            }

            bool added = false;
            ColumnCategory category = ColumnCategory.Unknown;
            string description = null;
            bool empty = reader.IsEmptyElement;
            int? keyColumn = null;
            string keyTable = null;
            int length = -1;
            bool localizable = false;
            long? maxValue = null;
            long? minValue = null;
            var modularize = ColumnModularizeType.None;
            string name = null;
            bool nullable = false;
            string possibilities = null;
            bool primaryKey = false;
            var type = ColumnType.Unknown;
            bool useCData = false;
            bool unreal = false;

            // parse the attributes
            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "added":
                        added = reader.Value.Equals("yes");
                        break;
                    case "category":
                        switch (reader.Value)
                        {
                            case "anyPath":
                                category = ColumnCategory.AnyPath;
                                break;
                            case "binary":
                                category = ColumnCategory.Binary;
                                break;
                            case "cabinet":
                                category = ColumnCategory.Cabinet;
                                break;
                            case "condition":
                                category = ColumnCategory.Condition;
                                break;
                            case "customSource":
                                category = ColumnCategory.CustomSource;
                                break;
                            case "defaultDir":
                                category = ColumnCategory.DefaultDir;
                                break;
                            case "doubleInteger":
                                category = ColumnCategory.DoubleInteger;
                                break;
                            case "filename":
                                category = ColumnCategory.Filename;
                                break;
                            case "formatted":
                                category = ColumnCategory.Formatted;
                                break;
                            case "formattedSddl":
                                category = ColumnCategory.FormattedSDDLText;
                                break;
                            case "guid":
                                category = ColumnCategory.Guid;
                                break;
                            case "identifier":
                                category = ColumnCategory.Identifier;
                                break;
                            case "integer":
                                category = ColumnCategory.Integer;
                                break;
                            case "language":
                                category = ColumnCategory.Language;
                                break;
                            case "lowerCase":
                                category = ColumnCategory.LowerCase;
                                break;
                            case "path":
                                category = ColumnCategory.Path;
                                break;
                            case "paths":
                                category = ColumnCategory.Paths;
                                break;
                            case "property":
                                category = ColumnCategory.Property;
                                break;
                            case "regPath":
                                category = ColumnCategory.RegPath;
                                break;
                            case "shortcut":
                                category = ColumnCategory.Shortcut;
                                break;
                            case "template":
                                category = ColumnCategory.Template;
                                break;
                            case "text":
                                category = ColumnCategory.Text;
                                break;
                            case "timeDate":
                                category = ColumnCategory.TimeDate;
                                break;
                            case "upperCase":
                                category = ColumnCategory.UpperCase;
                                break;
                            case "version":
                                category = ColumnCategory.Version;
                                break;
                            case "wildCardFilename":
                                category = ColumnCategory.WildCardFilename;
                                break;
                            default:
                                throw new InvalidOperationException();
                        }
                        break;
                    case "description":
                        description = reader.Value;
                        break;
                    case "keyColumn":
                        keyColumn = Convert.ToInt32(reader.Value, 10);
                        break;
                    case "keyTable":
                        keyTable = reader.Value;
                        break;
                    case "length":
                        length = Convert.ToInt32(reader.Value, 10);
                        break;
                    case "localizable":
                        localizable = reader.Value.Equals("yes");
                        break;
                    case "maxValue":
                        maxValue = Convert.ToInt32(reader.Value, 10);
                        break;
                    case "minValue":
                        minValue = Convert.ToInt32(reader.Value, 10);
                        break;
                    case "modularize":
                        switch (reader.Value)
                        {
                            case "column":
                                modularize = ColumnModularizeType.Column;
                                break;
                            case "companionFile":
                                modularize = ColumnModularizeType.CompanionFile;
                                break;
                            case "condition":
                                modularize = ColumnModularizeType.Condition;
                                break;
                            case "controlEventArgument":
                                modularize = ColumnModularizeType.ControlEventArgument;
                                break;
                            case "controlText":
                                modularize = ColumnModularizeType.ControlText;
                                break;
                            case "icon":
                                modularize = ColumnModularizeType.Icon;
                                break;
                            case "none":
                                modularize = ColumnModularizeType.None;
                                break;
                            case "property":
                                modularize = ColumnModularizeType.Property;
                                break;
                            case "semicolonDelimited":
                                modularize = ColumnModularizeType.SemicolonDelimited;
                                break;
                            default:
                                throw new XmlException();
                        }
                        break;
                    case "name":
                        switch (reader.Value)
                        {
                            case "CREATE":
                            case "DELETE":
                            case "DROP":
                            case "INSERT":
                                throw new XmlException();
                            default:
                                name = reader.Value;
                                break;
                        }
                        break;
                    case "nullable":
                        nullable = reader.Value.Equals("yes");
                        break;
                    case "primaryKey":
                        primaryKey = reader.Value.Equals("yes");
                        break;
                    case "set":
                        possibilities = reader.Value;
                        break;
                    case "type":
                        switch (reader.Value)
                        {
                            case "localized":
                                type = ColumnType.Localized;
                                break;
                            case "number":
                                type = ColumnType.Number;
                                break;
                            case "object":
                                type = ColumnType.Object;
                                break;
                            case "string":
                                type = ColumnType.String;
                                break;
                            case "preserved":
                                type = ColumnType.Preserved;
                                break;
                            default:
                                throw new XmlException();
                        }
                        break;
                    case "useCData":
                        useCData = reader.Value.Equals("yes");
                        break;
                    case "unreal":
                        unreal = reader.Value.Equals("yes");
                        break;
                }
            }

            // parse the child elements (there should be none)
            if (!empty)
            {
                bool done = false;

                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            throw new XmlException();
                        case XmlNodeType.EndElement:
                            done = true;
                            break;
                    }
                }

                if (!done)
                {
                    throw new XmlException();
                }
            }

            ColumnDefinition columnDefinition = new ColumnDefinition(name, type, length, primaryKey, nullable, category, minValue, maxValue, keyTable, keyColumn, possibilities, description, modularize, localizable, useCData, unreal);
            columnDefinition.Added = added;

            return columnDefinition;
        }

        /// <summary>
        /// Persists a ColumnDefinition in an XML format.
        /// </summary>
        /// <param name="writer">XmlWriter where the Output should persist itself as XML.</param>
        internal void Write(XmlWriter writer)
        {
            writer.WriteStartElement("columnDefinition", TableDefinitionCollection.XmlNamespaceUri);

            writer.WriteAttributeString("name", this.Name);

            switch (this.Type)
            {
                case ColumnType.Localized:
                    writer.WriteAttributeString("type", "localized");
                    break;
                case ColumnType.Number:
                    writer.WriteAttributeString("type", "number");
                    break;
                case ColumnType.Object:
                    writer.WriteAttributeString("type", "object");
                    break;
                case ColumnType.String:
                    writer.WriteAttributeString("type", "string");
                    break;
                case ColumnType.Preserved:
                    writer.WriteAttributeString("type", "preserved");
                    break;
            }

            writer.WriteAttributeString("length", this.Length.ToString(CultureInfo.InvariantCulture.NumberFormat));

            if (this.PrimaryKey)
            {
                writer.WriteAttributeString("primaryKey", "yes");
            }

            if (this.Nullable)
            {
                writer.WriteAttributeString("nullable", "yes");
            }

            if (this.IsLocalizable)
            {
                writer.WriteAttributeString("localizable", "yes");
            }

            if (this.Added)
            {
                writer.WriteAttributeString("added", "yes");
            }

            switch (this.ModularizeType)
            {
                case ColumnModularizeType.Column:
                    writer.WriteAttributeString("modularize", "column");
                    break;
                case ColumnModularizeType.CompanionFile:
                    writer.WriteAttributeString("modularize", "companionFile");
                    break;
                case ColumnModularizeType.Condition:
                    writer.WriteAttributeString("modularize", "condition");
                    break;
                case ColumnModularizeType.ControlEventArgument:
                    writer.WriteAttributeString("modularize", "controlEventArgument");
                    break;
                case ColumnModularizeType.ControlText:
                    writer.WriteAttributeString("modularize", "controlText");
                    break;
                case ColumnModularizeType.Icon:
                    writer.WriteAttributeString("modularize", "icon");
                    break;
                case ColumnModularizeType.None:
                    // this is the default value
                    break;
                case ColumnModularizeType.Property:
                    writer.WriteAttributeString("modularize", "property");
                    break;
                case ColumnModularizeType.SemicolonDelimited:
                    writer.WriteAttributeString("modularize", "semicolonDelimited");
                    break;
            }

            if (this.MinValue.HasValue)
            {
                writer.WriteAttributeString("minValue", this.MinValue.Value.ToString(CultureInfo.InvariantCulture.NumberFormat));
            }

            if (this.MaxValue.HasValue)
            {
                writer.WriteAttributeString("maxValue", this.MaxValue.Value.ToString(CultureInfo.InvariantCulture.NumberFormat));
            }

            if (!String.IsNullOrEmpty(this.KeyTable))
            {
                writer.WriteAttributeString("keyTable", this.KeyTable);
            }

            if (this.KeyColumn.HasValue)
            {
                writer.WriteAttributeString("keyColumn", this.KeyColumn.Value.ToString(CultureInfo.InvariantCulture.NumberFormat));
            }

            switch (this.Category)
            {
                case ColumnCategory.AnyPath:
                    writer.WriteAttributeString("category", "anyPath");
                    break;
                case ColumnCategory.Binary:
                    writer.WriteAttributeString("category", "binary");
                    break;
                case ColumnCategory.Cabinet:
                    writer.WriteAttributeString("category", "cabinet");
                    break;
                case ColumnCategory.Condition:
                    writer.WriteAttributeString("category", "condition");
                    break;
                case ColumnCategory.CustomSource:
                    writer.WriteAttributeString("category", "customSource");
                    break;
                case ColumnCategory.DefaultDir:
                    writer.WriteAttributeString("category", "defaultDir");
                    break;
                case ColumnCategory.DoubleInteger:
                    writer.WriteAttributeString("category", "doubleInteger");
                    break;
                case ColumnCategory.Filename:
                    writer.WriteAttributeString("category", "filename");
                    break;
                case ColumnCategory.Formatted:
                    writer.WriteAttributeString("category", "formatted");
                    break;
                case ColumnCategory.FormattedSDDLText:
                    writer.WriteAttributeString("category", "formattedSddl");
                    break;
                case ColumnCategory.Guid:
                    writer.WriteAttributeString("category", "guid");
                    break;
                case ColumnCategory.Identifier:
                    writer.WriteAttributeString("category", "identifier");
                    break;
                case ColumnCategory.Integer:
                    writer.WriteAttributeString("category", "integer");
                    break;
                case ColumnCategory.Language:
                    writer.WriteAttributeString("category", "language");
                    break;
                case ColumnCategory.LowerCase:
                    writer.WriteAttributeString("category", "lowerCase");
                    break;
                case ColumnCategory.Path:
                    writer.WriteAttributeString("category", "path");
                    break;
                case ColumnCategory.Paths:
                    writer.WriteAttributeString("category", "paths");
                    break;
                case ColumnCategory.Property:
                    writer.WriteAttributeString("category", "property");
                    break;
                case ColumnCategory.RegPath:
                    writer.WriteAttributeString("category", "regPath");
                    break;
                case ColumnCategory.Shortcut:
                    writer.WriteAttributeString("category", "shortcut");
                    break;
                case ColumnCategory.Template:
                    writer.WriteAttributeString("category", "template");
                    break;
                case ColumnCategory.Text:
                    writer.WriteAttributeString("category", "text");
                    break;
                case ColumnCategory.TimeDate:
                    writer.WriteAttributeString("category", "timeDate");
                    break;
                case ColumnCategory.UpperCase:
                    writer.WriteAttributeString("category", "upperCase");
                    break;
                case ColumnCategory.Version:
                    writer.WriteAttributeString("category", "version");
                    break;
                case ColumnCategory.WildCardFilename:
                    writer.WriteAttributeString("category", "wildCardFilename");
                    break;
            }

            if (!String.IsNullOrEmpty(this.Possibilities))
            {
                writer.WriteAttributeString("set", this.Possibilities);
            }

            if (!String.IsNullOrEmpty(this.Description))
            {
                writer.WriteAttributeString("description", this.Description);
            }

            if (this.UseCData)
            {
                writer.WriteAttributeString("useCData", "yes");
            }

            if (this.Unreal)
            {
                writer.WriteAttributeString("unreal", "yes");
            }

            writer.WriteEndElement();
        }

        /// <summary>
        /// Compare this column definition to another column definition.
        /// </summary>
        /// <remarks>
        /// Only Windows Installer traits are compared, allowing for updates to WiX-specific table definitions.
        /// </remarks>
        /// <param name="other">The <see cref="ColumnDefinition"/> to compare with this one.</param>
        /// <returns>0 if the columns' core propeties are the same; otherwise, non-0.</returns>
        public int CompareTo(ColumnDefinition other)
        {
            // by definition, this object is greater than null
            if (null == other)
            {
                return 1;
            }

            // compare column names
            int ret = String.Compare(this.Name, other.Name, StringComparison.Ordinal);

            // compare column types
            if (0 == ret)
            {
                ret = this.Type == other.Type ? 0 : -1;

                // compare column lengths
                if (0 == ret)
                {
                    ret = this.Length == other.Length ? 0 : -1;

                    // compare whether both are primary keys
                    if (0 == ret)
                    {
                        ret = this.PrimaryKey == other.PrimaryKey ? 0 : -1;

                        // compare nullability
                        if (0 == ret)
                        {
                            ret = this.Nullable == other.Nullable ? 0 : -1;
                        }
                    }
                }
            }

            return ret;
        }

        private static ColumnModularizeType CalculateModularizationType(ColumnModularizeType? modularizeType, ColumnCategory category)
        {
            if (modularizeType.HasValue)
            {
                return modularizeType.Value;
            }

            switch (category)
            {
                case ColumnCategory.Identifier:
                case ColumnCategory.CustomSource:
                    return ColumnModularizeType.Column;

                case ColumnCategory.Condition:
                    return ColumnModularizeType.Condition;

                case ColumnCategory.AnyPath:
                case ColumnCategory.Formatted:
                case ColumnCategory.FormattedSDDLText:
                case ColumnCategory.Path:
                case ColumnCategory.Paths:
                case ColumnCategory.RegPath:
                case ColumnCategory.Template:
                    return ColumnModularizeType.Property;

                case ColumnCategory.Shortcut:
                    return ColumnModularizeType.Property;
            }

            return ColumnModularizeType.None;
        }
    }
}
