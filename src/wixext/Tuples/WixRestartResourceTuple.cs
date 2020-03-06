// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Util
{
    using WixToolset.Data;
    using WixToolset.Util.Tuples;

    public static partial class UtilTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixRestartResource = new IntermediateTupleDefinition(
            UtilTupleDefinitionType.WixRestartResource.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixRestartResourceTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRestartResourceTupleFields.Resource), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixRestartResourceTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(WixRestartResourceTuple));
    }
}

namespace WixToolset.Util.Tuples
{
    using WixToolset.Data;

    public enum WixRestartResourceTupleFields
    {
        ComponentRef,
        Resource,
        Attributes,
    }

    public class WixRestartResourceTuple : IntermediateTuple
    {
        public WixRestartResourceTuple() : base(UtilTupleDefinitions.WixRestartResource, null, null)
        {
        }

        public WixRestartResourceTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(UtilTupleDefinitions.WixRestartResource, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixRestartResourceTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)WixRestartResourceTupleFields.ComponentRef].AsString();
            set => this.Set((int)WixRestartResourceTupleFields.ComponentRef, value);
        }

        public string Resource
        {
            get => this.Fields[(int)WixRestartResourceTupleFields.Resource].AsString();
            set => this.Set((int)WixRestartResourceTupleFields.Resource, value);
        }

        public int Attributes
        {
            get => this.Fields[(int)WixRestartResourceTupleFields.Attributes].AsNumber();
            set => this.Set((int)WixRestartResourceTupleFields.Attributes, value);
        }
    }
}