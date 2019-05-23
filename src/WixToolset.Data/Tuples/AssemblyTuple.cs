// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Assembly = new IntermediateTupleDefinition(
            TupleDefinitionType.Assembly,
            new[]
            {
                new IntermediateFieldDefinition(nameof(AssemblyTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AssemblyTupleFields.FeatureRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AssemblyTupleFields.ManifestFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AssemblyTupleFields.ApplicationFileRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(AssemblyTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(AssemblyTupleFields.ProcessorArchitecture), IntermediateFieldType.String),
            },
            typeof(AssemblyTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum AssemblyTupleFields
    {
        ComponentRef,
        FeatureRef,
        ManifestFileRef,
        ApplicationFileRef,
        Attributes,
        ProcessorArchitecture,
    }

    public enum AssemblyType
    {
        /// <summary>File is not an assembly.</summary>
        NotAnAssembly,

        /// <summary>File is a Common Language Runtime Assembly.</summary>
        DotNetAssembly,

        /// <summary>File is Win32 SxS assembly.</summary>
        Win32Assembly,
    }

    public class AssemblyTuple : IntermediateTuple
    {
        public AssemblyTuple() : base(TupleDefinitions.Assembly, null, null)
        {
        }

        public AssemblyTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Assembly, sourceLineNumber, id)
        {
        }

        public IntermediateField this[AssemblyTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => (string)this.Fields[(int)AssemblyTupleFields.ComponentRef];
            set => this.Set((int)AssemblyTupleFields.ComponentRef, value);
        }

        public string FeatureRef
        {
            get => (string)this.Fields[(int)AssemblyTupleFields.FeatureRef];
            set => this.Set((int)AssemblyTupleFields.FeatureRef, value);
        }

        public string ManifestFileRef
        {
            get => (string)this.Fields[(int)AssemblyTupleFields.ManifestFileRef];
            set => this.Set((int)AssemblyTupleFields.ManifestFileRef, value);
        }

        public string ApplicationFileRef
        {
            get => (string)this.Fields[(int)AssemblyTupleFields.ApplicationFileRef];
            set => this.Set((int)AssemblyTupleFields.ApplicationFileRef, value);
        }

        public AssemblyType Type
        {
            get => (AssemblyType)this.Fields[(int)AssemblyTupleFields.Attributes].AsNumber();
            set => this.Set((int)AssemblyTupleFields.Attributes, (int)value);
        }

        public string ProcessorArchitecture
        {
            get => (string)this.Fields[(int)AssemblyTupleFields.ProcessorArchitecture];
            set => this.Set((int)AssemblyTupleFields.ProcessorArchitecture, value);
        }
    }
}
