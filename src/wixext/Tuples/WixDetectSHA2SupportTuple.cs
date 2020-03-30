// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixDetectSHA2Support = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.WixDetectSHA2Support.ToString(),
            new IntermediateFieldDefinition[0],
            typeof(WixDetectSHA2SupportTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public class WixDetectSHA2SupportTuple : IntermediateTuple
    {
        public WixDetectSHA2SupportTuple() : base(UtilTupleDefinitions.WixDetectSHA2Support, null, null)
        {
        }

        public WixDetectSHA2SupportTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.WixDetectSHA2Support, sourceLineNumber, id)
        {
        }

        public IntermediateField this[GroupTupleFields index] => this.Fields[(int)index];
    }
}
