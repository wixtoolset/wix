// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition PatchPackage = new IntermediateTupleDefinition(
            TupleDefinitionType.PatchPackage,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PatchPackageTupleFields.PatchId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PatchPackageTupleFields.Media_), IntermediateFieldType.Number),
            },
            typeof(PatchPackageTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum PatchPackageTupleFields
    {
        PatchId,
        Media_,
    }

    public class PatchPackageTuple : IntermediateTuple
    {
        public PatchPackageTuple() : base(TupleDefinitions.PatchPackage, null, null)
        {
        }

        public PatchPackageTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.PatchPackage, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PatchPackageTupleFields index] => this.Fields[(int)index];

        public string PatchId
        {
            get => (string)this.Fields[(int)PatchPackageTupleFields.PatchId];
            set => this.Set((int)PatchPackageTupleFields.PatchId, value);
        }

        public int Media_
        {
            get => (int)this.Fields[(int)PatchPackageTupleFields.Media_];
            set => this.Set((int)PatchPackageTupleFields.Media_, value);
        }
    }
}