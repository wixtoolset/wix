using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace MessagesToMessages
{
    class Program
    {
        static readonly XNamespace ns = "http://schemas.microsoft.com/genmsgs/2004/07/messages";
        static readonly XName ClassDefinition = ns + "Class";
        static readonly XName MessageDefinition = ns + "Message";
        static readonly XName InstanceDefinition = ns + "Instance";
        static readonly XName ParameterDefinition = ns + "Parameter";
        static readonly XName Id = "Id";
        static readonly XName Level = "Level";
        static readonly XName Number = "Number";
        static readonly XName SourceLineNumbers = "SourceLineNumbers";
        static readonly XName Type = "Type";
        static readonly XName Name = "Name";

        static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                return;
            }
            else if (args.Length < 2)
            {
                Console.WriteLine("Need to specify output folder as well.");
            }
            else if (!Directory.Exists(args[1]))
            {
                Console.WriteLine("Output folder does not exist: {0}", args[1]);
            }

            var messages = ReadXml(Path.GetFullPath(args[0]));

            foreach (var m in messages.GroupBy(m => m.Level))
            {
                var className = m.First().ClassName;
                var result = GenerateCs(className, m.Key, m);

                var path = Path.Combine(args[1], className + ".cs");
                File.WriteAllText(path, result);
            }
        }

        private static IEnumerable<Message> ReadXml(string inputPath)
        {
            var doc = XDocument.Load(inputPath);

            foreach (var xClass in doc.Root.Descendants(ClassDefinition))
            {
                var name = xClass.Attribute(Name)?.Value;
                var level = xClass.Attribute(Level)?.Value;

                if (String.IsNullOrEmpty(name))
                {
                    name = level + "Messages";
                }
                if (String.IsNullOrEmpty(level))
                {
                    if (name.EndsWith("Errors", StringComparison.InvariantCultureIgnoreCase))
                    {
                        level = "Error";
                    }
                    else if (name.EndsWith("Verboses", StringComparison.InvariantCultureIgnoreCase))
                    {
                        level = "Verbose";
                    }
                    else if (name.EndsWith("Warnings", StringComparison.InvariantCultureIgnoreCase))
                    {
                        level = "Warning";
                    }
                }

                var unique = new HashSet<string>();
                var lastNumber = 0;
                foreach (var xMessage in xClass.Elements(MessageDefinition))
                {
                    var id = xMessage.Attribute(Id).Value;

                    if (!unique.Add(id))
                    {
                        Console.WriteLine("Duplicated message: {0}", id);
                    }

                    if (!Int32.TryParse(xMessage.Attribute(Number)?.Value, out var number))
                    {
                        number = lastNumber;
                    }
                    lastNumber = number + 1;

                    var sln = xMessage.Attribute(SourceLineNumbers)?.Value != "no";

                    var suffix = 0;
                    foreach (var xInstance in xMessage.Elements(InstanceDefinition))
                    {
                        var value = xInstance.Value.Trim();

                        var parameters = xInstance.Elements(ParameterDefinition).Select(ReadParameter).ToList();

                        yield return new Message { Id = id, ClassName = name, Level = level, Number = number, Parameters = parameters, SourceLineNumbers = sln, Value = value, Suffix = suffix == 0 ? String.Empty : suffix.ToString() };

                        ++suffix;
                    }
                }
            }
        }

        private static Parameter ReadParameter(XElement element)
        {
            var name = element.Attribute(Name)?.Value;
            var type = element.Attribute(Type)?.Value ?? "string";

            if (type.StartsWith("System."))
            {
                type = type.Substring(7);
            }

            switch (type)
            {
                case "String":
                    type = "string";
                    break;

                case "Int32":
                    type = "int";
                    break;

                case "Int64":
                    type = "long";
                    break;
            }

            return new Parameter { Name = name, Type = type };
        }

        private static string GenerateCs(string className, string level, IEnumerable<Message> messages)
        {
            var header = String.Join(Environment.NewLine,
                "// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.",
                "",
                "namespace WixToolset.Data",
                "{",
                "    using System;",
                "    using System.Resources;",
                "",
                "    public static class {0}",
                "    {");

            var messageFormat = String.Join(Environment.NewLine,
                "        public static Message {0}({1})",
                "        {{",
                "            return Message({4}, Ids.{0}, \"{3}\"{2});",
                "        }}",
                "");


            var endMessagesFormat = String.Join(Environment.NewLine,
                "        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, string format, params object[] args)",
                "        {{",
                "            return new Message(sourceLineNumber, MessageLevel.{0}, (int)id, format, args);",
                "        }}",
                "",
                "        private static Message Message(SourceLineNumber sourceLineNumber, Ids id, ResourceManager resourceManager, string resourceName, params object[] args)",
                "        {{",
                "            return new Message(sourceLineNumber, MessageLevel.{0}, (int)id, resourceManager, resourceName, args);",
                "        }}",
                "",
                "        public enum Ids",
                "        {{");

            var idEnumFormat =
                "            {0} = {1},";
            var footer = String.Join(Environment.NewLine,
                "        }",
                "    }",
                "}",
                "");

            var sb = new StringBuilder();

            sb.AppendLine(header.Replace("{0}", className));

            foreach (var message in messages.OrderBy(m => m.Id))
            {
                var paramsWithTypes = String.Join(", ", message.Parameters.Select(p => $"{p.Type} {p.Name}"));
                var paramsWithoutTypes = String.Join(", ", message.Parameters.Select(p => p.Name));

                if (message.SourceLineNumbers)
                {
                    paramsWithTypes = String.IsNullOrEmpty(paramsWithTypes)
                        ? "SourceLineNumber sourceLineNumbers"
                        : "SourceLineNumber sourceLineNumbers, " + paramsWithTypes;
                }

                if (!String.IsNullOrEmpty(paramsWithoutTypes))
                {
                    paramsWithoutTypes = ", " + paramsWithoutTypes;
                }

                sb.AppendFormat(messageFormat, message.Id, paramsWithTypes, paramsWithoutTypes, ToCSharpString(message.Value), message.SourceLineNumbers ? "sourceLineNumbers" : "null");

                sb.AppendLine();
            }

            sb.AppendFormat(endMessagesFormat, level);
            sb.AppendLine();

            var unique = new HashSet<int>();
            foreach (var message in messages.OrderBy(m => m.Number))
            {
                if (unique.Add(message.Number))
                {
                    sb.AppendFormat(idEnumFormat, message.Id, message.Number);
                    sb.AppendLine();
                }
            }

            sb.Append(footer);

            return sb.ToString();
        }

        private static string ToCSharpString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        private class Message
        {
            public string Id { get; set; }

            public string Suffix { get; set; }

            public string ClassName { get; set; }

            public string Level { get; set; }

            public int Number { get; set; }

            public bool SourceLineNumbers { get; set; }

            public string Value { get; set; }

            public IEnumerable<Parameter> Parameters { get; set; }
        }


        private class Parameter
        {
            public string Name { get; set; }

            public string Type { get; set; }
        }
    }
}
