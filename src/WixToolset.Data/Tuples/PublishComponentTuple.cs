// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition PublishComponent = new IntermediateTupleDefinition(
            TupleDefinitionType.PublishComponent,
            new[]
            {
                new IntermediateFieldDefinition(nameof(PublishComponentTupleFields.ComponentId), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PublishComponentTupleFields.Qualifier), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PublishComponentTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PublishComponentTupleFields.AppData), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(PublishComponentTupleFields.Feature_), IntermediateFieldType.String),
            },
            typeof(PublishComponentTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum PublishComponentTupleFields
    {
        ComponentId,
        Qualifier,
        Component_,
        AppData,
        Feature_,
    }

    public class PublishComponentTuple : IntermediateTuple
    {
        public PublishComponentTuple() : base(TupleDefinitions.PublishComponent, null, null)
        {
        }

        public PublishComponentTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.PublishComponent, sourceLineNumber, id)
        {
        }

        public IntermediateField this[PublishComponentTupleFields index] => this.Fields[(int)index];

        public string ComponentId
        {
            get => (string)this.Fields[(int)PublishComponentTupleFields.ComponentId]?.Value;
            set => this.Set((int)PublishComponentTupleFields.ComponentId, value);
        }

        public string Qualifier
        {
            get => (string)this.Fields[(int)PublishComponentTupleFields.Qualifier]?.Value;
            set => this.Set((int)PublishComponentTupleFields.Qualifier, value);
        }

        public string Component_
        {
            get => (string)this.Fields[(int)PublishComponentTupleFields.Component_]?.Value;
            set => this.Set((int)PublishComponentTupleFields.Component_, value);
        }

        public string AppData
        {
            get => (string)this.Fields[(int)PublishComponentTupleFields.AppData]?.Value;
            set => this.Set((int)PublishComponentTupleFields.AppData, value);
        }

        public string Feature_
        {
            get => (string)this.Fields[(int)PublishComponentTupleFields.Feature_]?.Value;
            set => this.Set((int)PublishComponentTupleFields.Feature_, value);
        }
    }
}