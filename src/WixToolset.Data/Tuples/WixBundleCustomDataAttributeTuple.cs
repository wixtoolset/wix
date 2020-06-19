// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBundleCustomDataAttribute = new IntermediateTupleDefinition(
            TupleDefinitionType.WixBundleCustomDataAttribute,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataAttributeTupleFields.CustomDataRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixBundleCustomDataAttributeTupleFields.Name), IntermediateFieldType.String),
            },
            typeof(WixBundleCustomDataAttributeTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixBundleCustomDataAttributeTupleFields
    {
        CustomDataRef,
        Name,
    }

    public class WixBundleCustomDataAttributeTuple : IntermediateTuple
    {
        public WixBundleCustomDataAttributeTuple() : base(TupleDefinitions.WixBundleCustomDataAttribute, null, null)
        {
        }

        public WixBundleCustomDataAttributeTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixBundleCustomDataAttribute, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBundleCustomDataAttributeTupleFields index] => this.Fields[(int)index];

        public string CustomDataRef
        {
            get => (string)this.Fields[(int)WixBundleCustomDataAttributeTupleFields.CustomDataRef];
            set => this.Set((int)WixBundleCustomDataAttributeTupleFields.CustomDataRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixBundleCustomDataAttributeTupleFields.Name];
            set => this.Set((int)WixBundleCustomDataAttributeTupleFields.Name, value);
        }
    }
}
