// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixPatchFamilyGroup = new IntermediateTupleDefinition(
            TupleDefinitionType.WixPatchFamilyGroup,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPatchFamilyGroupTupleFields.WixPatchFamilyGroup), IntermediateFieldType.String),
            },
            typeof(WixPatchFamilyGroupTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPatchFamilyGroupTupleFields
    {
        WixPatchFamilyGroup,
    }

    public class WixPatchFamilyGroupTuple : IntermediateTuple
    {
        public WixPatchFamilyGroupTuple() : base(TupleDefinitions.WixPatchFamilyGroup, null, null)
        {
        }

        public WixPatchFamilyGroupTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixPatchFamilyGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPatchFamilyGroupTupleFields index] => this.Fields[(int)index];

        public string WixPatchFamilyGroup
        {
            get => (string)this.Fields[(int)WixPatchFamilyGroupTupleFields.WixPatchFamilyGroup]?.Value;
            set => this.Set((int)WixPatchFamilyGroupTupleFields.WixPatchFamilyGroup, value);
        }
    }
}