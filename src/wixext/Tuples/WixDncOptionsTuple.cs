// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Bal
{
    using WixToolset.Data;
    using WixToolset.Bal.Tuples;

    public static partial class BalTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixDncOptions = new IntermediateTupleDefinition(
            BalTupleDefinitionType.WixDncOptions.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixDncOptionsTupleFields.SelfContainedDeployment), IntermediateFieldType.Number),
            },
            typeof(WixDncOptionsTuple));
    }
}

namespace WixToolset.Bal.Tuples
{
    using WixToolset.Data;

    public enum WixDncOptionsTupleFields
    {
        SelfContainedDeployment,
    }

    public class WixDncOptionsTuple : IntermediateTuple
    {
        public WixDncOptionsTuple() : base(BalTupleDefinitions.WixDncOptions, null, null)
        {
        }

        public WixDncOptionsTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(BalTupleDefinitions.WixDncOptions, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixDncOptionsTupleFields index] => this.Fields[(int)index];

        public int SelfContainedDeployment
        {
            get => this.Fields[(int)WixDncOptionsTupleFields.SelfContainedDeployment].AsNumber();
            set => this.Set((int)WixDncOptionsTupleFields.SelfContainedDeployment, value);
        }
    }
}