// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Example.Extension
{
    using WixToolset.Data;
    using WixToolset.Data.WindowsInstaller;

    public class ExampleRow : Row
    {
        public ExampleRow(SourceLineNumber sourceLineNumbers, Table table)
            : base(sourceLineNumbers, table)
        {
        }

        public ExampleRow(SourceLineNumber sourceLineNumbers, TableDefinition tableDefinition)
            : base(sourceLineNumbers, tableDefinition)
        {
        }

        public string Example
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        public string Value
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }
    }
}
