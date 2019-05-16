// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixUpdateRegistration = new IntermediateTupleDefinition(
            TupleDefinitionType.WixUpdateRegistration,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationTupleFields.Manufacturer), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationTupleFields.Department), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationTupleFields.ProductFamily), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixUpdateRegistrationTupleFields.Classification), IntermediateFieldType.String),
            },
            typeof(WixUpdateRegistrationTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum WixUpdateRegistrationTupleFields
    {
        Manufacturer,
        Department,
        ProductFamily,
        Name,
        Classification,
    }

    public class WixUpdateRegistrationTuple : IntermediateTuple
    {
        public WixUpdateRegistrationTuple() : base(TupleDefinitions.WixUpdateRegistration, null, null)
        {
        }

        public WixUpdateRegistrationTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixUpdateRegistration, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixUpdateRegistrationTupleFields index] => this.Fields[(int)index];

        public string Manufacturer
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationTupleFields.Manufacturer];
            set => this.Set((int)WixUpdateRegistrationTupleFields.Manufacturer, value);
        }

        public string Department
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationTupleFields.Department];
            set => this.Set((int)WixUpdateRegistrationTupleFields.Department, value);
        }

        public string ProductFamily
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationTupleFields.ProductFamily];
            set => this.Set((int)WixUpdateRegistrationTupleFields.ProductFamily, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationTupleFields.Name];
            set => this.Set((int)WixUpdateRegistrationTupleFields.Name, value);
        }

        public string Classification
        {
            get => (string)this.Fields[(int)WixUpdateRegistrationTupleFields.Classification];
            set => this.Set((int)WixUpdateRegistrationTupleFields.Classification, value);
        }
    }
}