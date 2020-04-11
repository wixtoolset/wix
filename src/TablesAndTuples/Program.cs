using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SimpleJson;

namespace TablesAndTuples
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }
            else if (Path.GetExtension(args[0]) == ".xml")
            {
                if (args.Length < 2)
                {
                    Console.WriteLine("Need to specify output json file as well.");
                    return;
                }
                if (Path.GetExtension(args[1]) != ".json")
                {
                    Console.WriteLine("Output needs to be .json");
                    return;
                }

                string prefix = null;
                if (args.Length > 2)
                {
                    prefix = args[2];
                }

                var csFile = Path.Combine(Path.GetDirectoryName(args[1]), String.Concat(prefix ?? "WindowsInstaller", "TableDefinitions.cs"));

                ReadXmlWriteJson(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]), Path.GetFullPath(csFile), prefix);
            }
            else if (Path.GetExtension(args[0]) == ".json")
            {
                string prefix = null;
                if (args.Length < 2)
                {
                    Console.WriteLine("Need to specify output folder.");
                    return;
                }
                else if (args.Length > 2)
                {
                    prefix = args[2];
                }

                ReadJsonWriteCs(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]), prefix);
            }
        }

        private static void ReadXmlWriteJson(string inputPath, string outputPath, string csOutputPath, string prefix)
        {
            var tableDefinitions = ReadXmlWriteCs(inputPath, csOutputPath, prefix);

            var array = new JsonArray();

            foreach (var tableDefinition in tableDefinitions)
            {
                if (tableDefinition.Tupleless)
                {
                    continue;
                }
                var tupleType = tableDefinition.TupleDefinitionName;

                var fields = new JsonArray();
                var firstField = true;

                foreach (var columnDefinition in tableDefinition.Columns)
                {
                    if (firstField)
                    {
                        firstField = false;
                        if (tableDefinition.TupleIdIsPrimaryKey)
                        {
                            continue;
                        }
                    }

                    var fieldName = columnDefinition.Name;
                    fieldName = Regex.Replace(fieldName, "^([^_]+)_([^_]*)$", x =>
                    {
                        return $"{x.Groups[2].Value}{x.Groups[1].Value}Ref";
                    });
                    var type = columnDefinition.Type.ToString().ToLower();

                    if (type == "localized")
                    {
                        type = "string";
                    }
                    else if (type == "object")
                    {
                        type = "path";
                    }

                    var field = new JsonObject
                    {
                        { fieldName, type }
                    };

                    fields.Add(field);
                }

                var obj = new JsonObject
                {
                    { tupleType, fields }
                };
                array.Add(obj);
            }

            array.Sort(CompareTupleDefinitions);

            var strat = new PocoJsonSerializerStrategy();
            var json = SimpleJson.SimpleJson.SerializeObject(array, strat);

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
            File.WriteAllText(outputPath, json);
        }

        private static List<WixTableDefinition> ReadXmlWriteCs(string inputPath, string outputPath, string prefix)
        {
            var tableDefinitions = WixTableDefinition.LoadCollection(inputPath);
            var text = GenerateCsTableDefinitionsFileText(prefix, tableDefinitions);
            Console.WriteLine("Writing: {0}", outputPath);
            File.WriteAllText(outputPath, text);
            return tableDefinitions;
        }

        private static void ReadJsonWriteCs(string inputPath, string outputFolder, string prefix)
        {
            var json = File.ReadAllText(inputPath);
            var tuples = SimpleJson.SimpleJson.DeserializeObject(json) as JsonArray;

            var tupleNames = new List<string>();

            foreach (var tupleDefinition in tuples.Cast<JsonObject>())
            {
                var tupleName = tupleDefinition.Keys.Single();
                var fields = tupleDefinition.Values.Single() as JsonArray;

                var list = GetFields(fields).ToList();

                tupleNames.Add(tupleName);

                var text = GenerateTupleFileText(prefix, tupleName, list);

                var pathTuple = Path.Combine(outputFolder, tupleName + "Tuple.cs");
                Console.WriteLine("Writing: {0}", pathTuple);
                File.WriteAllText(pathTuple, text);
            }

            var content = TupleNamesFileContent(prefix, tupleNames);
            var pathNames = Path.Combine(outputFolder, String.Concat(prefix, "TupleDefinitions.cs"));
            Console.WriteLine("Writing: {0}", pathNames);
            File.WriteAllText(pathNames, content);
        }

        private static IEnumerable<(string Name, string Type, string ClrType, string AsFunction)> GetFields(JsonArray fields)
        {
            foreach (var field in fields.Cast<JsonObject>())
            {
                var fieldName = field.Keys.Single();
                var fieldType = field.Values.Single() as string;

                var clrType = ConvertToClrType(fieldType);
                fieldType = ConvertToFieldType(fieldType);

                var asFunction = $"As{fieldType}()";

                yield return (Name: fieldName, Type: fieldType, ClrType: clrType, AsFunction: asFunction);
            }
        }

        private static string GenerateCsTableDefinitionsFileText(string prefix, List<WixTableDefinition> tableDefinitions)
        {
            var ns = prefix ?? "Data";

            var startClassDef = String.Join(Environment.NewLine,
                "// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.",
                "",
                "namespace WixToolset.{1}",
                "{",
                "    using WixToolset.Data.WindowsInstaller;",
                "",
                "    public static class {2}TableDefinitions",
                "    {");
            var startTableDef = String.Join(Environment.NewLine,
                "        public static readonly TableDefinition {1} = new TableDefinition(",
                "            \"{1}\",",
                "            new[]",
                "            {");
            var columnDef =
                "                new ColumnDefinition(\"{1}\", ColumnType.{2}, {3}, primaryKey: {4}, nullable: {5}, ColumnCategory.{6}";
            var endColumnsDef = String.Join(Environment.NewLine,
                "            },");
            var unrealDef =
                "            unreal: true,";
            var tupleNameDef =
                "            tupleDefinitionName: {1}TupleDefinitions.{2}.Name,";
            var endTableDef = String.Join(Environment.NewLine,
                "            tupleIdIsPrimaryKey: {1}",
                "        );",
                "");
            var startAllTablesDef = String.Join(Environment.NewLine,
                "        public static readonly TableDefinition[] All = new[]",
                "        {");
            var allTableDef =
                "            {1},";
            var endAllTablesDef =
                "        };";
            var endClassDef = String.Join(Environment.NewLine,
                "    }",
                "}");

            var sb = new StringBuilder();

            sb.AppendLine(startClassDef.Replace("{1}", ns).Replace("{2}", prefix));
            foreach (var tableDefinition in tableDefinitions)
            {
                sb.AppendLine(startTableDef.Replace("{1}", tableDefinition.Name));
                foreach (var columnDefinition in tableDefinition.Columns)
                {
                    sb.Append(columnDef.Replace("{1}", columnDefinition.Name).Replace("{2}", columnDefinition.Type.ToString()).Replace("{3}", columnDefinition.Length.ToString())
                        .Replace("{4}", columnDefinition.PrimaryKey.ToString().ToLower()).Replace("{5}", columnDefinition.Nullable.ToString().ToLower()).Replace("{6}", columnDefinition.Category.ToString()));
                    if (columnDefinition.MinValue.HasValue)
                    {
                        sb.AppendFormat(", minValue: {0}", columnDefinition.MinValue.Value);
                    }
                    if (columnDefinition.MaxValue.HasValue)
                    {
                        sb.AppendFormat(", maxValue: {0}", columnDefinition.MaxValue.Value);
                    }
                    if (!String.IsNullOrEmpty(columnDefinition.KeyTable))
                    {
                        sb.AppendFormat(", keyTable: \"{0}\"", columnDefinition.KeyTable);
                    }
                    if (columnDefinition.KeyColumn.HasValue)
                    {
                        sb.AppendFormat(", keyColumn: {0}", columnDefinition.KeyColumn.Value);
                    }
                    if (!String.IsNullOrEmpty(columnDefinition.Possibilities))
                    {
                        sb.AppendFormat(", possibilities: \"{0}\"", columnDefinition.Possibilities);
                    }
                    if (!String.IsNullOrEmpty(columnDefinition.Description))
                    {
                        sb.AppendFormat(", description: \"{0}\"", columnDefinition.Description.Replace("\\", "\\\\").Replace("\"", "\\\""));
                    }
                    if (columnDefinition.ModularizeType.HasValue && columnDefinition.ModularizeType.Value != ColumnModularizeType.None)
                    {
                        sb.AppendFormat(", modularizeType: ColumnModularizeType.{0}", columnDefinition.ModularizeType.ToString());
                    }
                    if (columnDefinition.ForceLocalizable)
                    {
                        sb.Append(", forceLocalizable: true");
                    }
                    if (columnDefinition.UseCData)
                    {
                        sb.Append(", useCData: true");
                    }
                    if (columnDefinition.Unreal)
                    {
                        sb.Append(", unreal: true");
                    }
                    sb.AppendLine("),");
                }
                sb.AppendLine(endColumnsDef);
                if (tableDefinition.Unreal)
                {
                    sb.AppendLine(unrealDef);
                }
                if (!tableDefinition.Tupleless)
                {
                    sb.AppendLine(tupleNameDef.Replace("{1}", prefix).Replace("{2}", tableDefinition.TupleDefinitionName));
                }
                sb.AppendLine(endTableDef.Replace("{1}", tableDefinition.TupleIdIsPrimaryKey.ToString().ToLower()));
            }
            sb.AppendLine(startAllTablesDef);
            foreach (var tableDefinition in tableDefinitions)
            {
                sb.AppendLine(allTableDef.Replace("{1}", tableDefinition.Name));
            }
            sb.AppendLine(endAllTablesDef);
            sb.AppendLine(endClassDef);

            return sb.ToString();
        }

        private static string GenerateTupleFileText(string prefix, string tupleName, List<(string Name, string Type, string ClrType, string AsFunction)> tupleFields)
        {
            var ns = prefix ?? "Data";
            var toString = String.IsNullOrEmpty(prefix) ? null : ".ToString()";

            var startFileDef = String.Join(Environment.NewLine,
                "// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.",
                "",
                "namespace WixToolset.{2}",
                "{");
            var usingDataDef =
                "    using WixToolset.Data;";
            var startTupleDef = String.Join(Environment.NewLine,
                "    using WixToolset.{2}.Tuples;",
                "",
                "    public static partial class {3}TupleDefinitions",
                "    {",
                "        public static readonly IntermediateTupleDefinition {1} = new IntermediateTupleDefinition(",
                "            {3}TupleDefinitionType.{1}{4},",
                "            new{5}[]",
                "            {");
            var fieldDef =
                "                new IntermediateFieldDefinition(nameof({1}TupleFields.{2}), IntermediateFieldType.{3}),";
            var endTupleDef = String.Join(Environment.NewLine,
                "            },",
                "            typeof({1}Tuple));",
                "    }",
                "}",
                "",
                "namespace WixToolset.{2}.Tuples",
                "{");
            var startEnumDef = String.Join(Environment.NewLine,
                "    public enum {1}TupleFields",
                "    {");
            var fieldEnum =
                "        {2},";
            var startTuple = String.Join(Environment.NewLine,
                "    }",
                "",
                "    public class {1}Tuple : IntermediateTuple",
                "    {",
                "        public {1}Tuple() : base({3}TupleDefinitions.{1}, null, null)",
                "        {",
                "        }",
                "",
                "        public {1}Tuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base({3}TupleDefinitions.{1}, sourceLineNumber, id)",
                "        {",
                "        }",
                "",
                "        public IntermediateField this[{1}TupleFields index] => this.Fields[(int)index];");
            var fieldProp = String.Join(Environment.NewLine,
                "",
                "        public {4} {2}",
                "        {",
                "            get => {6}this.Fields[(int){1}TupleFields.{2}]{5};",
                "            set => this.Set((int){1}TupleFields.{2}, value);",
                "        }");
            var endTuple = String.Join(Environment.NewLine,
                "    }",
                "}");

            var sb = new StringBuilder();

            sb.AppendLine(startFileDef.Replace("{2}", ns));
            if (ns != "Data")
            {
                sb.AppendLine(usingDataDef);
            }
            sb.AppendLine(startTupleDef.Replace("{1}", tupleName).Replace("{2}", ns).Replace("{3}", prefix).Replace("{4}", toString).Replace("{5}", tupleFields.Any() ? null : " IntermediateFieldDefinition"));
            foreach (var field in tupleFields)
            {
                sb.AppendLine(fieldDef.Replace("{1}", tupleName).Replace("{2}", field.Name).Replace("{3}", field.Type));
            }
            sb.AppendLine(endTupleDef.Replace("{1}", tupleName).Replace("{2}", ns).Replace("{3}", prefix));
            if (ns != "Data")
            {
                sb.AppendLine(usingDataDef);
                sb.AppendLine();
            }
            sb.AppendLine(startEnumDef.Replace("{1}", tupleName));
            foreach (var field in tupleFields)
            {
                sb.AppendLine(fieldEnum.Replace("{1}", tupleName).Replace("{2}", field.Name));
            }
            sb.AppendLine(startTuple.Replace("{1}", tupleName).Replace("{2}", ns).Replace("{3}", prefix));
            foreach (var field in tupleFields)
            {
                var useCast = ns == "Data" && field.AsFunction != "AsPath()";
                var cast = useCast ? $"({field.ClrType})" : null;
                var asFunction = useCast ? null : $".{field.AsFunction}";
                sb.AppendLine(fieldProp.Replace("{1}", tupleName).Replace("{2}", field.Name).Replace("{3}", field.Type).Replace("{4}", field.ClrType).Replace("{5}", asFunction).Replace("{6}", cast));
            }
            sb.Append(endTuple);

            return sb.ToString();
        }

        private static string TupleNamesFileContent(string prefix, List<string> tupleNames)
        {
            var ns = prefix ?? "Data";

            var header = String.Join(Environment.NewLine,
                "// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.",
                "",
                "namespace WixToolset.{2}",
                "{",
                "    using System;",
                "    using WixToolset.Data;",
                "",
                "    public enum {3}TupleDefinitionType",
                "    {");
            var namesFormat =
                "        {1},";
            var midpoint = String.Join(Environment.NewLine,
                "    }",
                "",
                "    public static partial class {3}TupleDefinitions",
                "    {",
                "        public static readonly Version Version = new Version(\"4.0.0\");",
                "",
                "        public static IntermediateTupleDefinition ByName(string name)",
                "        {",
                "            if (!Enum.TryParse(name, out {3}TupleDefinitionType type))",
                "            {",
                "                return null;",
                "            }",
                "",
                "            return ByType(type);",
                "        }",
                "",
                "        public static IntermediateTupleDefinition ByType({3}TupleDefinitionType type)",
                "        {",
                "            switch (type)",
                "            {");

            var caseFormat = String.Join(Environment.NewLine,
                "                case {3}TupleDefinitionType.{1}:",
                "                    return {3}TupleDefinitions.{1};",
                "");

            var footer = String.Join(Environment.NewLine,
                "                default:",
                "                    throw new ArgumentOutOfRangeException(nameof(type));",
                "            }",
                "        }",
                "    }",
                "}");

            var sb = new StringBuilder();

            sb.AppendLine(header.Replace("{2}", ns).Replace("{3}", prefix));
            foreach (var tupleName in tupleNames)
            {
                sb.AppendLine(namesFormat.Replace("{1}", tupleName).Replace("{2}", ns).Replace("{3}", prefix));
            }
            sb.AppendLine(midpoint.Replace("{2}", ns).Replace("{3}", prefix));
            foreach (var tupleName in tupleNames)
            {
                sb.AppendLine(caseFormat.Replace("{1}", tupleName).Replace("{2}", ns).Replace("{3}", prefix));
            }
            sb.AppendLine(footer);

            return sb.ToString();
        }

        private static string ConvertToFieldType(string fieldType)
        {
            switch (fieldType.ToLowerInvariant())
            {
                case "bool":
                    return "Bool";

                case "string":
                case "preserved":
                    return "String";

                case "number":
                    return "Number";

                case "path":
                    return "Path";
            }

            throw new ArgumentException(fieldType);
        }

        private static string ConvertToClrType(string fieldType)
        {
            switch (fieldType.ToLowerInvariant())
            {
                case "bool":
                    return "bool";

                case "string":
                case "preserved":
                    return "string";

                case "number":
                    return "int";

                case "path":
                    return "string";
            }

            throw new ArgumentException(fieldType);
        }

        private static int CompareTupleDefinitions(object x, object y)
        {
            var first = (JsonObject)x;
            var second = (JsonObject)y;

            var firstType = first.Keys.Single();
            var secondType = second.Keys.Single();

            return firstType.CompareTo(secondType);
        }
    }
}
