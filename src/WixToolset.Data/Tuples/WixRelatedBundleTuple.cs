// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixRelatedBundle = new IntermediateTupleDefinition(
            TupleDefinitionType.WixRelatedBundle,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixRelatedBundleTupleFields.BundleId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRelatedBundleTupleFields.Action), IntermediateFieldType.Number),
            },
            typeof(WixRelatedBundleTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixRelatedBundleTupleFields
    {
        BundleId,
        Action,
    }

    public enum RelatedBundleActionType
    {
        Detect,
        Upgrade,
        Addon,
        Patch
    }

    public class WixRelatedBundleTuple : IntermediateTuple
    {
        public WixRelatedBundleTuple() : base(TupleDefinitions.WixRelatedBundle, null, null)
        {
        }

        public WixRelatedBundleTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixRelatedBundle, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixRelatedBundleTupleFields index] => this.Fields[(int)index];

        public string BundleId
        {
            get => (string)this.Fields[(int)WixRelatedBundleTupleFields.BundleId]?.Value;
            set => this.Set((int)WixRelatedBundleTupleFields.BundleId, value);
        }

        public RelatedBundleActionType Action
        {
            get => (RelatedBundleActionType)this.Fields[(int)WixRelatedBundleTupleFields.Action].AsNumber();
            set => this.Set((int)WixRelatedBundleTupleFields.Action, (int)value);
        }
    }
}