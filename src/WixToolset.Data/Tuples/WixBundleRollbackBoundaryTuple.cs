// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleRollbackBoundary = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleRollbackBoundary,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleRollbackBoundaryTupleFields.WixChainItem_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleRollbackBoundaryTupleFields.Vital), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixBundleRollbackBoundaryTupleFields.Transaction), IntermediateFieldType.Number),
            },
            typeof(WixBundleRollbackBoundaryTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleRollbackBoundaryTupleFields
    {
        WixChainItem_,
        Vital,
        Transaction,
    }

    public class WixBundleRollbackBoundaryTuple : IntermediateTuple
    {
        public WixBundleRollbackBoundaryTuple() : base(TupleDefinitions.WixBundleRollbackBoundary, null, null)
        {
        }

        public WixBundleRollbackBoundaryTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleRollbackBoundary, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleRollbackBoundaryTupleFields index] => this.Fields[(int)index];

        public string WixChainItem_
        {
            get => (string)this.Fields[(int)WixBundleRollbackBoundaryTupleFields.WixChainItem_];
            set => this.Set((int)WixBundleRollbackBoundaryTupleFields.WixChainItem_, value);
        }

        public bool? Vital
        {
            get => (bool?)this.Fields[(int)WixBundleRollbackBoundaryTupleFields.Vital];
            set => this.Set((int)WixBundleRollbackBoundaryTupleFields.Vital, value);
        }

        public bool? Transaction
        {
            get => (bool?)this.Fields[(int)WixBundleRollbackBoundaryTupleFields.Transaction];
            set => this.Set((int)WixBundleRollbackBoundaryTupleFields.Transaction, value);
        }
    }
}