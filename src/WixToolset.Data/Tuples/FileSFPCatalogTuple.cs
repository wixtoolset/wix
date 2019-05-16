// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition FileSFPCatalog = new IntermediateTupleDefinition(
            TupleDefinitionType.FileSFPCatalog,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileSFPCatalogTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSFPCatalogTupleFields.SFPCatalog_), IntermediateFieldType.String),
            },
            typeof(FileSFPCatalogTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FileSFPCatalogTupleFields
    {
        File_,
        SFPCatalog_,
    }

    public class FileSFPCatalogTuple : IntermediateTuple
    {
        public FileSFPCatalogTuple() : base(TupleDefinitions.FileSFPCatalog, null, null)
        {
        }

        public FileSFPCatalogTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.FileSFPCatalog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileSFPCatalogTupleFields index] => this.Fields[(int)index];

        public string File_
        {
            get => (string)this.Fields[(int)FileSFPCatalogTupleFields.File_];
            set => this.Set((int)FileSFPCatalogTupleFields.File_, value);
        }

        public string SFPCatalog_
        {
            get => (string)this.Fields[(int)FileSFPCatalogTupleFields.SFPCatalog_];
            set => this.Set((int)FileSFPCatalogTupleFields.SFPCatalog_, value);
        }
    }
}