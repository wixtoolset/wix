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
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.ManifestFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.ApplicationFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(MsiAssemblyTupleFields.Attributes), IntermediateFieldType.Number),
            },
            typeof(MsiAssemblyTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum MsiAssemblyTupleFields
    {
        ComponentRef,
        FeatureRef,
        ManifestFileRef,
        ApplicationFileRef,
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

        public string ComponentRef
        {
            get => (string)this.Fields[(int)MsiAssemblyTupleFields.ComponentRef];
            set => this.Set((int)MsiAssemblyTupleFields.ComponentRef, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)MsiAssemblyTupleFields.FeatureRef];
            set => this.Set((int)MsiAssemblyTupleFields.FeatureRef, value);
        }

        public string ManifestFileRef
        {
            get => (string)this.Fields[(int)MsiAssemblyTupleFields.ManifestFileRef];
            set => this.Set((int)MsiAssemblyTupleFields.ManifestFileRef, value);
        }

        public string ApplicationFileRef
        {
            get => (string)this.Fields[(int)MsiAssemblyTupleFields.ApplicationFileRef];
            set => this.Set((int)MsiAssemblyTupleFields.ApplicationFileRef, value);
        }

        public FileAssemblyType Type
        {
            get => (FileAssemblyType)this.Fields[(int)MsiAssemblyTupleFields.Attributes].AsNumber();
            set => this.Set((int)MsiAssemblyTupleFields.Attributes, (int)value);
        }
    }
}