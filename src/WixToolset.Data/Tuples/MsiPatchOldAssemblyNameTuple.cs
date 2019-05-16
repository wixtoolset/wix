// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiPatchOldAssemblyName = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiPatchOldAssemblyName,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyNameTupleFields.Assembly), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyNameTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiPatchOldAssemblyNameTupleFields.Value), IntermediateFieldType.String),
            },
            typeof(MsiPatchOldAssemblyNameTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiPatchOldAssemblyNameTupleFields
    {
        Assembly,
        Name,
        Value,
    }

    public class MsiPatchOldAssemblyNameTuple : IntermediateTuple
    {
        public MsiPatchOldAssemblyNameTuple() : base(TupleDefinitions.MsiPatchOldAssemblyName, null, null)
        {
        }

        public MsiPatchOldAssemblyNameTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiPatchOldAssemblyName, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiPatchOldAssemblyNameTupleFields index] => this.Fields[(int)index];

        public string Assembly
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyNameTupleFields.Assembly];
            set => this.Set((int)MsiPatchOldAssemblyNameTupleFields.Assembly, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyNameTupleFields.Name];
            set => this.Set((int)MsiPatchOldAssemblyNameTupleFields.Name, value);
        }

        public string Value
        {
            get => (string)this.Fields[(int)MsiPatchOldAssemblyNameTupleFields.Value];
            set => this.Set((int)MsiPatchOldAssemblyNameTupleFields.Value, value);
        }
    }
}