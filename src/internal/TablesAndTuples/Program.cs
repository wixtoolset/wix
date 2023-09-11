using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SimpleJson;

namespace TablesAndSymbols
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
                if (tableDefinition.Symbolless)
                {
                    continue;
                }
                var symbolType = tableDefinition.SymbolDefinitionName;

                var fields = new JsonArray();
                var firstField = true;

                foreach (var columnDefinition in tableDefinition.Columns)
                {
                    if (firstField)
                    {
                        firstField = false;
                        if (tableDefinition.SymbolIdIsPrimaryKey)
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
                    else if (columnDefinition.Type == ColumnType.Number && columnDefinition.Length == 2 &&
                             columnDefinition.MinValue == 0 && columnDefinition.MaxValue == 1)
                    {
                        type = "bool";
                    }

                    if (columnDefinition.Type == ColumnType.Number && columnDefinition.Nullable)
                    {
                        type += "?";
                    }

                    var field = new JsonObject
                    {
                        { fieldName, type }
                    };

                    fields.Add(field);
                }

                var obj = new JsonObject
                {
                    { symbolType, fields }
                };
                array.Add(obj);
            }

            array.Sort(CompareSymbolDefinitions);

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
            var symbols = SimpleJson.SimpleJson.DeserializeObject(json) as JsonArray;

            var symbolNames = new List<string>();

            foreach (var symbolDefinition in symbols.Cast<JsonObject>())
            {
                var symbolName = symbolDefinition.Keys.Single();
                var fields = symbolDefinition.Values.Single() as JsonArray;

                var list = GetFields(fields).ToList();

                symbolNames.Add(symbolName);

                var text = GenerateSymbolFileText(prefix, symbolName, list);

                var pathSymbol = Path.Combine(outputFolder, symbolName + "Symbol.cs");
                Console.WriteLine("Writing: {0}", pathSymbol);
                File.WriteAllText(pathSymbol, text);
            }

            var content = SymbolNamesFileContent(prefix, symbolNames);
            var pathNames = Path.Combine(outputFolder, String.Concat(prefix, "SymbolDefinitions.cs"));
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

                var asFunction = $"As{(clrType.Contains("?") ? "Nullable" : "")}{fieldType}()";

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
                "            \"{2}\",",
                "            {3},",
                "            new[]",
                "            {");
            var columnDef =
                "                new ColumnDefinition(\"{1}\", ColumnType.{2}, {3}, primaryKey: {4}, nullable: {5}, ColumnCategory.{6}";
            var endColumnsDef = String.Join(Environment.NewLine,
                "            },");
            var unrealDef =
                "            unreal: true,";
            var endTableDef = String.Join(Environment.NewLine,
                "            symbolIdIsPrimaryKey: {1}",
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
                var symbolDefinition = tableDefinition.Symbolless ? "null" : $"{prefix}SymbolDefinitions.{tableDefinition.SymbolDefinitionName}";
                sb.AppendLine(startTableDef.Replace("{1}", tableDefinition.VariableName).Replace("{2}", tableDefinition.Name).Replace("{3}", symbolDefinition));
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
                sb.AppendLine(endTableDef.Replace("{1}", tableDefinition.SymbolIdIsPrimaryKey.ToString().ToLower()));
            }
            sb.AppendLine(startAllTablesDef);
            foreach (var tableDefinition in tableDefinitions)
            {
                sb.AppendLine(allTableDef.Replace("{1}", tableDefinition.VariableName));
            }
            sb.AppendLine(endAllTablesDef);
            sb.AppendLine(endClassDef);

            return sb.ToString();
        }

        private static string GenerateSymbolFileText(string prefix, string symbolName, List<(string Name, string Type, string ClrType, string AsFunction)> symbolFields)
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
            var startSymbolDef = String.Join(Environment.NewLine,
                "    using WixToolset.{2}.Symbols;",
                "",
                "    public static partial class {3}SymbolDefinitions",
                "    {",
                "        public static readonly IntermediateSymbolDefinition {1} = new IntermediateSymbolDefinition(",
                "            {3}SymbolDefinitionType.{1}{4},",
                "            new{5}[]",
                "            {");
            var fieldDef =
                "                new IntermediateFieldDefinition(nameof({1}SymbolFields.{2}), IntermediateFieldType.{3}),";
            var endSymbolDef = String.Join(Environment.NewLine,
                "            },",
                "            typeof({1}Symbol));",
                "    }",
                "}",
                "",
                "namespace WixToolset.{2}.Symbols",
                "{");
            var startEnumDef = String.Join(Environment.NewLine,
                "    public enum {1}SymbolFields",
                "    {");
            var fieldEnum =
                "        {2},";
            var startSymbol = String.Join(Environment.NewLine,
                "    }",
                "",
                "    public class {1}Symbol : IntermediateSymbol",
                "    {",
                "        public {1}Symbol() : base({3}SymbolDefinitions.{1}, null, null)",
                "        {",
                "        }",
                "",
                "        public {1}Symbol(SourceLineNumber sourceLineNumber, Identifier id = null) : base({3}SymbolDefinitions.{1}, sourceLineNumber, id)",
                "        {",
                "        }",
                "",
                "        public IntermediateField this[{1}SymbolFields index] => this.Fields[(int)index];");
            var fieldProp = String.Join(Environment.NewLine,
                "",
                "        public {4} {2}",
                "        {",
                "            get => {6}this.Fields[(int){1}SymbolFields.{2}]{5};",
                "            set => this.Set((int){1}SymbolFields.{2}, value);",
                "        }");
            var endSymbol = String.Join(Environment.NewLine,
                "    }",
                "}");

            var sb = new StringBuilder();

            sb.AppendLine(startFileDef.Replace("{2}", ns));
            if (ns != "Data")
            {
                sb.AppendLine(usingDataDef);
            }
            sb.AppendLine(startSymbolDef.Replace("{1}", symbolName).Replace("{2}", ns).Replace("{3}", prefix).Replace("{4}", toString).Replace("{5}", symbolFields.Any() ? null : " IntermediateFieldDefinition"));
            foreach (var field in symbolFields)
            {
                sb.AppendLine(fieldDef.Replace("{1}", symbolName).Replace("{2}", field.Name).Replace("{3}", field.Type));
            }
            sb.AppendLine(endSymbolDef.Replace("{1}", symbolName).Replace("{2}", ns).Replace("{3}", prefix));
            if (ns != "Data")
            {
                sb.AppendLine(usingDataDef);
                sb.AppendLine();
            }
            sb.AppendLine(startEnumDef.Replace("{1}", symbolName));
            foreach (var field in symbolFields)
            {
                sb.AppendLine(fieldEnum.Replace("{1}", symbolName).Replace("{2}", field.Name));
            }
            sb.AppendLine(startSymbol.Replace("{1}", symbolName).Replace("{2}", ns).Replace("{3}", prefix));
            foreach (var field in symbolFields)
            {
                var useCast = ns == "Data" && field.AsFunction != "AsPath()";
                var cast = useCast ? $"({field.ClrType})" : null;
                var asFunction = useCast ? null : $".{field.AsFunction}";
                sb.AppendLine(fieldProp.Replace("{1}", symbolName).Replace("{2}", field.Name).Replace("{3}", field.Type).Replace("{4}", field.ClrType).Replace("{5}", asFunction).Replace("{6}", cast));
            }
            sb.Append(endSymbol);

            return sb.ToString();
        }

        private static string SymbolNamesFileContent(string prefix, List<string> symbolNames)
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
                "    public enum {3}SymbolDefinitionType",
                "    {");
            var namesFormat =
                "        {1},";
            var midpoint = String.Join(Environment.NewLine,
                "    }",
                "",
                "    public static partial class {3}SymbolDefinitions",
                "    {",
                "        public static IntermediateSymbolDefinition ByName(string name)",
                "        {",
                "            if (!Enum.TryParse(name, out {3}SymbolDefinitionType type))",
                "            {",
                "                return null;",
                "            }",
                "",
                "            return ByType(type);",
                "        }",
                "",
                "        public static IntermediateSymbolDefinition ByType({3}SymbolDefinitionType type)",
                "        {",
                "            switch (type)",
                "            {");

            var caseFormat = String.Join(Environment.NewLine,
                "                case {3}SymbolDefinitionType.{1}:",
                "                    return {3}SymbolDefinitions.{1};",
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
            foreach (var symbolName in symbolNames)
            {
                sb.AppendLine(namesFormat.Replace("{1}", symbolName).Replace("{2}", ns).Replace("{3}", prefix));
            }
            sb.AppendLine(midpoint.Replace("{2}", ns).Replace("{3}", prefix));
            foreach (var symbolName in symbolNames)
            {
                sb.AppendLine(caseFormat.Replace("{1}", symbolName).Replace("{2}", ns).Replace("{3}", prefix));
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
                case "bool?":
                    return "Number";

                case "string":
                case "preserved":
                    return "String";

                case "number":
                case "number?":
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
                case "bool?":
                    return "bool?";

                case "string":
                case "preserved":
                    return "string";

                case "number":
                    return "int";
                case "number?":
                    return "int?";

                case "path":
                    return "IntermediateFieldPathValue";
            }

            throw new ArgumentException(fieldType);
        }

        private static int CompareSymbolDefinitions(object x, object y)
        {
            var first = (JsonObject)x;
            var second = (JsonObject)y;

            var firstType = first.Keys.Single();
            var secondType = second.Keys.Single();

            return firstType.CompareTo(secondType);
        }
    }
}
