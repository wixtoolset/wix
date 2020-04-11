using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace TablesAndTuples
{
    class WixTableDefinition
    {
        public WixTableDefinition(string name, IEnumerable<WixColumnDefinition> columns, bool unreal, bool tupleless, string tupleDefinitionName, bool? tupleIdIsPrimaryKey)
        {
            this.Name = name;
            this.Unreal = unreal;
            this.Columns = columns?.ToArray();
            this.Tupleless = tupleless;
            this.TupleDefinitionName = tupleless ? null : tupleDefinitionName ?? name.Replace("_", "");
            this.TupleIdIsPrimaryKey = tupleIdIsPrimaryKey ?? DeriveTupleIdIsPrimaryKey(this.Columns);
        }

        public string Name { get; }

        public string TupleDefinitionName { get; }

        public bool Unreal { get; }

        public WixColumnDefinition[] Columns { get; }

        public bool TupleIdIsPrimaryKey { get; }

        public bool Tupleless { get; }

        static WixTableDefinition Read(XmlReader reader)
        {
            var empty = reader.IsEmptyElement;
            string name = null;
            string tupleDefinitionName = null;
            var unreal = false;
            bool? tupleIdIsPrimaryKey = null;
            var tupleless = false;

            while (reader.MoveToNextAttribute())
            {
                switch (reader.LocalName)
                {
                    case "name":
                        name = reader.Value;
                        break;
                    case "tupleDefinitionName":
                        tupleDefinitionName = reader.Value;
                        break;
                    case "tupleIdIsPrimaryKey":
                        tupleIdIsPrimaryKey = reader.Value.Equals("yes");
                        break;
                    case "tupleless":
                        tupleless = reader.Value.Equals("yes");
                        break;
                    case "unreal":
                        unreal = reader.Value.Equals("yes");
                        break;
                }
            }

            if (null == name)
            {
                throw new XmlException();
            }

            var columns = new List<WixColumnDefinition>();

            // parse the child elements
            if (!empty)
            {
                var done = false;

                while (!done && reader.Read())
                {
                    switch (reader.NodeType)
                    {
                        case XmlNodeType.Element:
                            switch (reader.LocalName)
                            {
                                case "columnDefinition":
                                    var columnDefinition = WixColumnDefinition.Read(reader);
                                    columns.Add(columnDefinition);
                                    break;
                                default:
                                    throw new XmlException();
                            }
                            break;
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

            return new WixTableDefinition(name, columns.ToArray(), unreal, tupleless, tupleDefinitionName, tupleIdIsPrimaryKey);
        }

        static bool DeriveTupleIdIsPrimaryKey(WixColumnDefinition[] columns)
        {
            return columns[0].PrimaryKey &&
                   columns[0].Type == ColumnType.String &&
                   columns[0].Category == ColumnCategory.Identifier &&
                   !columns[0].Name.EndsWith("_") &&
                   (columns.Length == 1 || !columns.Skip(1).Any(t => t.PrimaryKey));
        }

        public static List<WixTableDefinition> LoadCollection(string inputPath)
        {
            using (var reader = XmlReader.Create(inputPath))
            {
                reader.MoveToContent();

                if ("tableDefinitions" != reader.LocalName)
                {
                    throw new XmlException();
                }

                var empty = reader.IsEmptyElement;
                var tableDefinitions = new List<WixTableDefinition>();

                while (reader.MoveToNextAttribute())
                {
                }

                // parse the child elements
                if (!empty)
                {
                    var done = false;

                    while (!done && reader.Read())
                    {
                        switch (reader.NodeType)
                        {
                            case XmlNodeType.Element:
                                switch (reader.LocalName)
                                {
                                    case "tableDefinition":
                                        tableDefinitions.Add(WixTableDefinition.Read(reader));
                                        break;
                                    default:
                                        throw new XmlException();
                                }
                                break;
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

                return tableDefinitions;
            }
        }
    }
}
