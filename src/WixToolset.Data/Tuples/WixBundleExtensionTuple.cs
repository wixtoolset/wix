// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleExtension = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleExtension,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleExtensionTupleFields.PayloadRef), IntermediateFieldType.String),
            },
            typeof(WixBundleExtensionTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleExtensionTupleFields
    {
        PayloadRef,
    }

    public class WixBundleExtensionTuple : IntermediateTuple
    {
        public WixBundleExtensionTuple() : base(TupleDefinitions.WixBundleExtension, null, null)
        {
        }

        public WixBundleExtensionTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleExtension, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleExtensionTupleFields index] => this.Fields[(int)index];

        public string PayloadRef
        {
            get => (string)this.Fields[(int)WixBundleExtensionTupleFields.PayloadRef];
            set => this.Set((int)WixBundleExtensionTupleFields.PayloadRef, value);
        }
    }
}
