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
                new IntermediateFieldDefinition(nameof(WixPatchBaselineTupleFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineTupleFields.ValidationFlags), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineTupleFields.BaselineFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineTupleFields.UpdateFile), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixPatchBaselineTupleFields.TransformFile), IntermediateFieldType.Path),
            },
            typeof(WixPatchBaselineTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPatchBaselineTupleFields
    {
        DiskId,
        ValidationFlags,
        BaselineFile,
        UpdateFile,
        TransformFile,
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

        public int DiskId
        {
            get => (int)this.Fields[(int)WixPatchBaselineTupleFields.DiskId];
            set => this.Set((int)WixPatchBaselineTupleFields.DiskId, value);
        }

        public TransformFlags ValidationFlags
        {
            get => (TransformFlags)this.Fields[(int)WixPatchBaselineTupleFields.ValidationFlags].AsNumber();
            set => this.Set((int)WixPatchBaselineTupleFields.ValidationFlags, (int)value);
        }

        public IntermediateFieldPathValue BaselineFile
        {
            get => this.Fields[(int)WixPatchBaselineTupleFields.BaselineFile].AsPath();
            set => this.Set((int)WixPatchBaselineTupleFields.BaselineFile, value);
        }

        public IntermediateFieldPathValue UpdateFile
        {
            get => this.Fields[(int)WixPatchBaselineTupleFields.UpdateFile].AsPath();
            set => this.Set((int)WixPatchBaselineTupleFields.UpdateFile, value);
        }

        public IntermediateFieldPathValue TransformFile
        {
            get => this.Fields[(int)WixPatchBaselineTupleFields.TransformFile].AsPath();
            set => this.Set((int)WixPatchBaselineTupleFields.TransformFile, value);
        }
    }
}
