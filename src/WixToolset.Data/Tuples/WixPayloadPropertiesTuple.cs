// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixPayloadProperties = new IntermediateTupleDefinition(
            TupleDefinitionType.WixPayloadProperties,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixPayloadPropertiesTupleFields.Payload), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPayloadPropertiesTupleFields.Package), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPayloadPropertiesTupleFields.Container), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPayloadPropertiesTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPayloadPropertiesTupleFields.Size), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPayloadPropertiesTupleFields.DownloadUrl), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixPayloadPropertiesTupleFields.LayoutOnly), IntermediateFieldType.String),
            },
            typeof(WixPayloadPropertiesTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixPayloadPropertiesTupleFields
    {
        Payload,
        Package,
        Container,
        Name,
        Size,
        DownloadUrl,
        LayoutOnly,
    }

    public class WixPayloadPropertiesTuple : IntermediateTuple
    {
        public WixPayloadPropertiesTuple() : base(TupleDefinitions.WixPayloadProperties, null, null)
        {
        }

        public WixPayloadPropertiesTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixPayloadProperties, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixPayloadPropertiesTupleFields index] => this.Fields[(int)index];

        public string Payload
        {
            get => (string)this.Fields[(int)WixPayloadPropertiesTupleFields.Payload]?.Value;
            set => this.Set((int)WixPayloadPropertiesTupleFields.Payload, value);
        }

        public string Package
        {
            get => (string)this.Fields[(int)WixPayloadPropertiesTupleFields.Package]?.Value;
            set => this.Set((int)WixPayloadPropertiesTupleFields.Package, value);
        }

        public string Container
        {
            get => (string)this.Fields[(int)WixPayloadPropertiesTupleFields.Container]?.Value;
            set => this.Set((int)WixPayloadPropertiesTupleFields.Container, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixPayloadPropertiesTupleFields.Name]?.Value;
            set => this.Set((int)WixPayloadPropertiesTupleFields.Name, value);
        }

        public string Size
        {
            get => (string)this.Fields[(int)WixPayloadPropertiesTupleFields.Size]?.Value;
            set => this.Set((int)WixPayloadPropertiesTupleFields.Size, value);
        }

        public string DownloadUrl
        {
            get => (string)this.Fields[(int)WixPayloadPropertiesTupleFields.DownloadUrl]?.Value;
            set => this.Set((int)WixPayloadPropertiesTupleFields.DownloadUrl, value);
        }

        public string LayoutOnly
        {
            get => (string)this.Fields[(int)WixPayloadPropertiesTupleFields.LayoutOnly]?.Value;
            set => this.Set((int)WixPayloadPropertiesTupleFields.LayoutOnly, value);
        }
    }
}