// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Tuples;

    public static partial class BalTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixBalBAFactoryAssembly = new IntermediateTupleDefinition(
            BalTupleDefinitionType.WixBalBAFactoryAssembly.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixBalBAFactoryTupleFields.PayloadId), IntermediateFieldType.String),
            },
            typeof(WixBalBAFactoryAssemblyTuple));
    }
}

namespace WixToolset.Bal.Tuples
{
    using WixToolset.Data;

    public enum WixBalBAFactoryTupleFields
    {
        PayloadId,
    }

    public class WixBalBAFactoryAssemblyTuple : IntermediateTuple
    {
        public WixBalBAFactoryAssemblyTuple() : base(BalTupleDefinitions.WixBalBAFactoryAssembly, null, null)
        {
        }

        public WixBalBAFactoryAssemblyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalTupleDefinitions.WixBalBAFactoryAssembly, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixBalBAFactoryTupleFields index] => this.Fields[(int)index];

        public string PayloadId
        {
            get => this.Fields[(int)WixBalBAFactoryTupleFields.PayloadId].AsString();
            set => this.Set((int)WixBalBAFactoryTupleFields.PayloadId, value);
        }
    }
}