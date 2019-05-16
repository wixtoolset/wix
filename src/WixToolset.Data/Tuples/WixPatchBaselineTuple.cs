// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixPatchBaseline = new IntermediateTupleDefinition(
            TupleDefinitionType.WixPatchBaseline,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchBaselineTupleFields.WixPatchBaseline), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineTupleFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineTupleFields.ValidationFlags), IntermediateFieldType.Number),
            },
            typeof(WixPatchBaselineTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPatchBaselineTupleFields
    {
        WixPatchBaseline,
        DiskId,
        ValidationFlags,
    }

    public class WixPatchBaselineTuple : IntermediateTuple
    {
        public WixPatchBaselineTuple() : base(TupleDefinitions.WixPatchBaseline, null, null)
        {
        }

        public WixPatchBaselineTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixPatchBaseline, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchBaselineTupleFields index] => this.Fields[(int)index];

        public string WixPatchBaseline
        {
            get => (string)this.Fields[(int)WixPatchBaselineTupleFields.WixPatchBaseline];
            set => this.Set((int)WixPatchBaselineTupleFields.WixPatchBaseline, value);
        }

        public int DiskId
        {
            get => (int)this.Fields[(int)WixPatchBaselineTupleFields.DiskId];
            set => this.Set((int)WixPatchBaselineTupleFields.DiskId, value);
        }

        public int ValidationFlags
        {
            get => (int)this.Fields[(int)WixPatchBaselineTupleFields.ValidationFlags];
            set => this.Set((int)WixPatchBaselineTupleFields.ValidationFlags, value);
        }
    }
}