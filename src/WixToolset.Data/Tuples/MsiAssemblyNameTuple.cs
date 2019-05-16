// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiAssemblyName = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiAssemblyName,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiAssemblyNameTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyNameTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyNameTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(MsiAssemblyNameTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiAssemblyNameTupleFields
    {
        Component_,
        Name,
        Value,
    }

    public class MsiAssemblyNameTuple : IntermediateTuple
    {
        public MsiAssemblyNameTuple() : base(TupleDefinitions.MsiAssemblyName, null, null)
        {
        }

        public MsiAssemblyNameTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiAssemblyName, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiAssemblyNameTupleFields index] => this.Fields[(int)index];

        public string Component_
        {
            get => (string)this.Fields[(int)MsiAssemblyNameTupleFields.Component_];
            set => this.Set((int)MsiAssemblyNameTupleFields.Component_, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)MsiAssemblyNameTupleFields.Name];
            set => this.Set((int)MsiAssemblyNameTupleFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)MsiAssemblyNameTupleFields.Value];
            set => this.Set((int)MsiAssemblyNameTupleFields.Value, value);
        }
    }
}