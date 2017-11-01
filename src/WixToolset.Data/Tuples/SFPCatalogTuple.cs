// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition SFPCatalog = new IntermediateTupleDefinition(
            TupleDefinitionType.SFPCatalog,
            new[]
            {
                new IntermediateFieldDefinition(nameof(SFPCatalogTupleFields.SFPCatalog), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(SFPCatalogTupleFields.Catalog), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(SFPCatalogTupleFields.Dependency), IntermediateFieldType.String),
            },
            typeof(SFPCatalogTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum SFPCatalogTupleFields
    {
        SFPCatalog,
        Catalog,
        Dependency,
    }

    public class SFPCatalogTuple : IntermediateTuple
    {
        public SFPCatalogTuple() : base(TupleDefinitions.SFPCatalog, null, null)
        {
        }

        public SFPCatalogTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.SFPCatalog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[SFPCatalogTupleFields index] => this.Fields[(int)index];

        public string SFPCatalog
        {
            get => (string)this.Fields[(int)SFPCatalogTupleFields.SFPCatalog]?.Value;
            set => this.Set((int)SFPCatalogTupleFields.SFPCatalog, value);
        }

        public string Catalog
        {
            get => (string)this.Fields[(int)SFPCatalogTupleFields.Catalog]?.Value;
            set => this.Set((int)SFPCatalogTupleFields.Catalog, value);
        }

        public string Dependency
        {
            get => (string)this.Fields[(int)SFPCatalogTupleFields.Dependency]?.Value;
            set => this.Set((int)SFPCatalogTupleFields.Dependency, value);
        }
    }
}