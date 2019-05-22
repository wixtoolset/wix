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
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Root), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Key), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Type), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(RegLocatorTupleFields.Win64), IntermediateFieldType.Bool),
            },
            typeof(RegLocatorTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum RegLocatorTupleFields
    {
        Root,
        Key,
        Name,
        Type,
        Win64,
    }

    public enum RegLocatorType
    {
        Directory,
        FileName,
        Raw
    };

    public class RegLocatorTuple : IntermediateTuple
    {
        public RegLocatorTuple() : base(TupleDefinitions.RegLocator, null, null)
        {
        }

        public RegLocatorTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.RegLocator, sourceLineNumber, id)
        {
        }

        public IntermediateField this[RegLocatorTupleFields index] => this.Fields[(int)index];

        public RegistryRootType Root
        {
            get => (RegistryRootType)this.Fields[(int)RegLocatorTupleFields.Root].AsNumber();
            set => this.Set((int)RegLocatorTupleFields.Root, (int)value);
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

        public RegLocatorType Type
        {
            get => (RegLocatorType)this.Fields[(int)RegLocatorTupleFields.Type].AsNumber();
            set => this.Set((int)RegLocatorTupleFields.Type, (int)value);
        }

        public bool Win64
        {
            get => this.Fields[(int)RegLocatorTupleFields.Win64].AsBool();
            set => this.Set((int)RegLocatorTupleFields.Win64, value);
        }
    }
}
