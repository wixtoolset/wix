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
                new IntermediateFieldDefinition(nameof(FileSFPCatalogTupleFields.FileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileSFPCatalogTupleFields.SFPCatalogRef), IntermediateFieldType.String),
            },
            typeof(FileSFPCatalogTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FileSFPCatalogTupleFields
    {
        FileRef,
        SFPCatalogRef,
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

        public string FileRef
        {
            get => (string)this.Fields[(int)FileSFPCatalogTupleFields.FileRef];
            set => this.Set((int)FileSFPCatalogTupleFields.FileRef, value);
        }

        public string SFPCatalogRef
        {
            get => (string)this.Fields[(int)FileSFPCatalogTupleFields.SFPCatalogRef];
            set => this.Set((int)FileSFPCatalogTupleFields.SFPCatalogRef, value);
        }
    }
}