using System;
using System.Xml;

namespace TablesAndSymbols
{
    class WixColumnDefinition
    {
        public WixColumnDefinition(string name, ColumnType type, int length, bool primaryKey, bool nullable, ColumnCategory category, long? minValue = null, long? maxValue = null, string keyTable = null, int? keyColumn = null, string possibilities = null, string description = null, ColumnModularizeType? modularizeType = null, bool forceLocalizable = false, bool useCData = false, bool unreal = false)
        {
            this.Name = name;
            this.Type = type;
            this.Length = length;
            this.PrimaryKey = primaryKey;
            this.Nullable = nullable;
            this.ModularizeType = modularizeType;
            this.ForceLocalizable = forceLocalizable;
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

        public string Name { get; }
        public ColumnType Type { get; }
        public int Length { get; }
        public bool PrimaryKey { get; }
        public bool Nullable { get; }
        public ColumnModularizeType? ModularizeType { get; }
        public bool ForceLocalizable { get; }
        public long? MinValue { get; }
        public long? MaxValue { get; }
        public string KeyTable { get; }
        public int? KeyColumn { get; }
        public ColumnCategory Category { get; }
        public string Possibilities { get; }
        public string Description { get; }
        public bool UseCData { get; }
        public bool Unreal { get; }

        internal static WixColumnDefinition Read(XmlReader reader)
        {
            if (!reader.LocalName.Equals("columnDefinition"))
            {
                throw new XmlException();
            }

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

            WixColumnDefinition columnDefinition = new WixColumnDefinition(name, type, length, primaryKey, nullable, category, minValue, maxValue, keyTable, keyColumn, possibilities, description, modularize, localizable, useCData, unreal);

            return columnDefinition;
        }
    }
}
