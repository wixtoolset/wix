// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixFeatureGroup = new IntermediateTupleDefinition(
            TupleDefinitionType.WixFeatureGroup,
            new IntermediateFieldDefinition[0],
            typeof(WixFeatureGroupTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixFeatureGroupTupleFields
    {
    }

    public class WixFeatureGroupTuple : IntermediateTuple
    {
        public WixFeatureGroupTuple() : base(TupleDefinitions.WixFeatureGroup, null, null)
        {
        }

        public WixFeatureGroupTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixFeatureGroup, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFeatureGroupTupleFields index] => this.Fields[(int)index];
   }
}