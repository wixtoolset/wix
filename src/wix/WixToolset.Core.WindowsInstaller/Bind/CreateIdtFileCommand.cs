// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Core.WindowsInstaller.Bind
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;
    using WixToolset.Extensibility.Services;

    internal class CreateIdtFileCommand
    {
        public CreateIdtFileCommand(IMessaging messaging, Table table, int codepage, string intermediateFolder, bool keepAddedColumns)
        {
            this.Messaging = messaging;
            this.Table = table;
            this.Codepage = codepage;
            this.IntermediateFolder = intermediateFolder;
            this.KeepAddedColumns = keepAddedColumns;
        }

        private IMessaging Messaging { get; }

        private Table Table { get; }

        private int Codepage { get; set; }

        private string IntermediateFolder { get; }

        private bool KeepAddedColumns { get; }

        public string IdtPath { get; private set; }

        public void Execute()
        {
            // write out the table to an IDT file
            var encoding = GetCodepageEncoding(this.Codepage);

            this.IdtPath = Path.Combine(this.IntermediateFolder, String.Concat(this.Table.Name, ".idt"));

            using (var idtWriter = new StreamWriter(this.IdtPath, false, encoding))
            {
                this.TableToIdtDefinition(this.Table, idtWriter, this.KeepAddedColumns);
            }
        }

        private void TableToIdtDefinition(Table table, StreamWriter writer, bool keepAddedColumns)
        {
            if (table.Definition.Unreal)
            {
                return;
            }

            if (TableDefinition.MaxColumnsInRealTable < table.Definition.Columns.Length)
            {
                throw new WixException(ErrorMessages.TooManyColumnsInRealTable(table.Definition.Name, table.Definition.Columns.Length, TableDefinition.MaxColumnsInRealTable));
            }

            // Tack on the table header, and flush before we start writing bytes directly to the stream.
            var header = this.TableDefinitionToIdtDefinition(table.Definition, keepAddedColumns);
            writer.Write(header);
            writer.Flush();

            using (var binary = new BinaryWriter(writer.BaseStream, writer.Encoding, true))
            {
                // Create an encoding that replaces characters with question marks, and doesn't throw. We'll 
                // use this in case of errors
                Encoding convertEncoding = Encoding.GetEncoding(writer.Encoding.CodePage);

                foreach (Row row in table.Rows)
                {
                    string rowString = this.RowToIdtDefinition(row, keepAddedColumns);
                    byte[] rowBytes;

                    try
                    {
                        // GetBytes will throw an exception if any character doesn't match our current encoding
                        rowBytes = writer.Encoding.GetBytes(rowString);
                    }
                    catch (EncoderFallbackException)
                    {
                        this.Messaging.Write(ErrorMessages.InvalidStringForCodepage(row.SourceLineNumbers, Convert.ToString(writer.Encoding.CodePage, CultureInfo.InvariantCulture)));

                        rowBytes = convertEncoding.GetBytes(rowString);
                    }

                    binary.Write(rowBytes, 0, rowBytes.Length);
                }
            }
        }

        private string TableDefinitionToIdtDefinition(TableDefinition definition, bool keepAddedColumns)
        {
            var first = true;
            var columnString = new StringBuilder();
            var dataString = new StringBuilder();
            var tableString = new StringBuilder();

            tableString.Append(definition.Name);
            foreach (var column in definition.Columns)
            {
                // Conditionally keep columns added in a transform; otherwise,
                // break because columns can only be added at the end.
                if (column.Added && !keepAddedColumns)
                {
                    break;
                }

                if (column.Unreal)
                {
                    continue;
                }

                if (!first)
                {
                    columnString.Append('\t');
                    dataString.Append('\t');
                }

                columnString.Append(column.Name);
                dataString.Append(ColumnIdtType(column));

                if (column.PrimaryKey)
                {
                    tableString.AppendFormat("\t{0}", column.Name);
                }

                first = false;
            }
            columnString.Append("\r\n");
            columnString.Append(dataString);
            columnString.Append("\r\n");
            columnString.Append(tableString);
            columnString.Append("\r\n");

            return columnString.ToString();
        }

        private string RowToIdtDefinition(Row row, bool keepAddedColumns)
        {
            var first = true;
            var sb = new StringBuilder();

            foreach (var field in row.Fields)
            {
                // Conditionally keep columns added in a transform; otherwise,
                // break because columns can only be added at the end.
                if (field.Column.Added && !keepAddedColumns)
                {
                    break;
                }

                if (field.Column.Unreal)
                {
                    continue;
                }

                if (first)
                {
                    first = false;
                }
                else
                {
                    sb.Append('\t');
                }

                sb.Append(this.FieldToIdtValue(field));
            }
            sb.Append("\r\n");

            return sb.ToString();
        }

        private string FieldToIdtValue(Field field)
        {
            var data = field.AsString();

            if (String.IsNullOrEmpty(data))
            {
                return data;
            }

            // Special field value idt-specific escaping.
            return data.Replace('\t', '\x10')
                       .Replace('\r', '\x11')
                       .Replace('\n', '\x19');
        }

        private static Encoding GetCodepageEncoding(int codepage)
        {
            Encoding encoding;

            // If UTF8 encoding, use the UTF8-specific constructor to avoid writing
            // the byte order mark at the beginning of the file
            if (codepage == Encoding.UTF8.CodePage)
            {
                encoding = new UTF8Encoding(false, true);
            }
            else
            {
                if (codepage == 0)
                {
                    codepage = Encoding.ASCII.CodePage;
                }

                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                encoding = Encoding.GetEncoding(codepage, new EncoderExceptionFallback(), new DecoderExceptionFallback());
            }

            return encoding;
        }

        /// <summary>
        /// Gets the type of the column in IDT format.
        /// </summary>
        /// <value>IDT format for column type.</value>
        private static string ColumnIdtType(ColumnDefinition column)
        {
            char typeCharacter;
            switch (column.Type)
            {
                case ColumnType.Number:
                    typeCharacter = column.Nullable ? 'I' : 'i';
                    break;
                case ColumnType.Preserved:
                case ColumnType.String:
                    typeCharacter = column.Nullable ? 'S' : 's';
                    break;
                case ColumnType.Localized:
                    typeCharacter = column.Nullable ? 'L' : 'l';
                    break;
                case ColumnType.Object:
                    typeCharacter = column.Nullable ? 'V' : 'v';
                    break;
                default:
                    throw new InvalidOperationException($"Unknown column type: {column.Type}");
            }

            return String.Concat(typeCharacter, column.Length);
        }
    }
}
