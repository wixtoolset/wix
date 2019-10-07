// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleCatalog = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleCatalog,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleCatalogTupleFields.PayloadRef), IntermediateFieldType.String),
            },
            typeof(WixBundleCatalogTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleCatalogTupleFields
    {
        PayloadRef,
    }

    public class WixBundleCatalogTuple : IntermediateTuple
    {
        public WixBundleCatalogTuple() : base(TupleDefinitions.WixBundleCatalog, null, null)
        {
        }

        public WixBundleCatalogTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleCatalog, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleCatalogTupleFields index] => this.Fields[(int)index];

        public string PayloadRef
        {
            get => (string)this.Fields[(int)WixBundleCatalogTupleFields.PayloadRef];
            set => this.Set((int)WixBundleCatalogTupleFields.PayloadRef, value);
        }
    }
}
