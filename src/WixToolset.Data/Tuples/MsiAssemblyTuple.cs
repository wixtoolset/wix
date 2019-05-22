// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition MsiAssembly = new IntermediateTupleDefinition(
            TupleDefinitionType.MsiAssembly,
            new[]
            {
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.Component_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.Feature_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.File_Manifest), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.File_Application), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(MsiAssemblyTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiAssemblyTupleFields
    {
        Component_,
        Feature_,
        File_Manifest,
        File_Application,
        Attributes,
    }

    public class MsiAssemblyTuple : IntermediateTuple
    {
        public MsiAssemblyTuple() : base(TupleDefinitions.MsiAssembly, null, null)
        {
        }

        public MsiAssemblyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.MsiAssembly, sourceLineNumber, id)
        {
        }

        public IntermediateField this[MsiAssemblyTupleFields index] => this.Fields[(int)index];

        public string Component_
        {
            get => (string)this.Fields[(int)MsiAssemblyTupleFields.Component_];
            set => this.Set((int)MsiAssemblyTupleFields.Component_, value);
        }

        public string Feature_
        {
            get => (string)this.Fields[(int)MsiAssemblyTupleFields.Feature_];
            set => this.Set((int)MsiAssemblyTupleFields.Feature_, value);
        }

        public string File_Manifest
        {
            get => (string)this.Fields[(int)MsiAssemblyTupleFields.File_Manifest];
            set => this.Set((int)MsiAssemblyTupleFields.File_Manifest, value);
        }

        public string File_Application
        {
            get => (string)this.Fields[(int)MsiAssemblyTupleFields.File_Application];
            set => this.Set((int)MsiAssemblyTupleFields.File_Application, value);
        }

        public FileAssemblyType Type
        {
            get => (FileAssemblyType)this.Fields[(int)MsiAssemblyTupleFields.Attributes].AsNumber();
            set => this.Set((int)MsiAssemblyTupleFields.Attributes, (int)value);
        }
    }
}