// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Tuples;

    public static partial class BalTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBalBAFunctions = new IntermediateTupleDefinition(
            BalTupleDefinitionType.WixBalBAFunctions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalBAFunctionsTupleFields.PayloadId), IntermediateFieldType.String),
            },
            typeof(WixBalBAFunctionsTuple));
    }
}

namespace WixToolset.Bal.Tuples
{
    using WixToolset.Data;

    public enum WixBalBAFunctionsTupleFields
    {
        PayloadId,
    }

    public class WixBalBAFunctionsTuple : IntermediateTuple
    {
        public WixBalBAFunctionsTuple() : base(BalTupleDefinitions.WixBalBAFunctions, null, null)
        {
        }

        public WixBalBAFunctionsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalTupleDefinitions.WixBalBAFunctions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalBAFunctionsTupleFields index] => this.Fields[(int)index];

        public string PayloadId
        {
            get => this.Fields[(int)WixBalBAFunctionsTupleFields.PayloadId].AsString();
            set => this.Set((int)WixBalBAFunctionsTupleFields.PayloadId, value);
        }
    }
}