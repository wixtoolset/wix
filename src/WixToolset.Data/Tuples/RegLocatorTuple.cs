// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition RegLocator = new IntermediateTupleDefinition(
            TupleDefinitionType.RegLocator,
            new[]
            {
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Signature_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Type), IntermediateFieldType.Number),
            },
            typeof(RegLocatorTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RegLocatorTupleFields
    {
        Signature_,
        Root,
        Key,
        Name,
        Type,
    }

    public class RegLocatorTuple : IntermediateTuple
    {
        public RegLocatorTuple() : base(TupleDefinitions.RegLocator, null, null)
        {
        }

        public RegLocatorTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.RegLocator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RegLocatorTupleFields index] => this.Fields[(int)index];

        public string Signature_
        {
            get => (string)this.Fields[(int)RegLocatorTupleFields.Signature_];
            set => this.Set((int)RegLocatorTupleFields.Signature_, value);
        }

        public int Root
        {
            get => (int)this.Fields[(int)RegLocatorTupleFields.Root];
            set => this.Set((int)RegLocatorTupleFields.Root, value);
        }

        public string Key
        {
            get => (string)this.Fields[(int)RegLocatorTupleFields.Key];
            set => this.Set((int)RegLocatorTupleFields.Key, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)RegLocatorTupleFields.Name];
            set => this.Set((int)RegLocatorTupleFields.Name, value);
        }

        public int Type
        {
            get => (int)this.Fields[(int)RegLocatorTupleFields.Type];
            set => this.Set((int)RegLocatorTupleFields.Type, value);
        }
    }
}