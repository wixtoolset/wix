// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Sql
{
    using WixToolset.Data;
    using WixToolset.Sql.Tuples;

    public static partial class SqlTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition SqlFileSpec = new IntermediateTupleDefinition(
            SqlTupleDefinitionType.SqlFileSpec.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(SqlFileSpecTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlFileSpecTupleFields.Filename), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlFileSpecTupleFields.Size), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlFileSpecTupleFields.MaxSize), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SqlFileSpecTupleFields.GrowthSize), IntermediateFieldType.String),
            },
            typeof(SqlFileSpecTuple));
    }
}

namespace WixToolset.Sql.Tuples
{
    using WixToolset.Data;

    public enum SqlFileSpecTupleFields
    {
        Name,
        Filename,
        Size,
        MaxSize,
        GrowthSize,
    }

    public class SqlFileSpecTuple : IntermediateTuple
    {
        public SqlFileSpecTuple() : base(SqlTupleDefinitions.SqlFileSpec, null, null)
        {
        }

        public SqlFileSpecTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(SqlTupleDefinitions.SqlFileSpec, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SqlFileSpecTupleFields index] => this.Fields[(int)index];

        public string Name
        {
            get => this.Fields[(int)SqlFileSpecTupleFields.Name].AsString();
            set => this.Set((int)SqlFileSpecTupleFields.Name, value);
        }

        public string Filename
        {
            get => this.Fields[(int)SqlFileSpecTupleFields.Filename].AsString();
            set => this.Set((int)SqlFileSpecTupleFields.Filename, value);
        }

        public string Size
        {
            get => this.Fields[(int)SqlFileSpecTupleFields.Size].AsString();
            set => this.Set((int)SqlFileSpecTupleFields.Size, value);
        }

        public string MaxSize
        {
            get => this.Fields[(int)SqlFileSpecTupleFields.MaxSize].AsString();
            set => this.Set((int)SqlFileSpecTupleFields.MaxSize, value);
        }

        public string GrowthSize
        {
            get => this.Fields[(int)SqlFileSpecTupleFields.GrowthSize].AsString();
            set => this.Set((int)SqlFileSpecTupleFields.GrowthSize, value);
        }
    }
}