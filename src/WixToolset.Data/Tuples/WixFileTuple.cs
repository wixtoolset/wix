// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition WixFile = new IntermediateTupleDefinition(
            TupleDefinitionType.WixFile,
            new[]
            {
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.File_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.AssemblyType), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.File_AssemblyManifest), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.File_AssemblyApplication), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.Directory_), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.DiskId), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.Source), IntermediateFieldType.Path),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.ProcessorArchitecture), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.PatchGroup), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.Attributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.PatchAttributes), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(WixFileTupleFields.DeltaPatchHeaderSource), IntermediateFieldType.String),
            },
            typeof(WixFileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    using System;

    public enum WixFileTupleFields
    {
        File_,
        AssemblyType,
        File_AssemblyManifest,
        File_AssemblyApplication,
        Directory_,
        DiskId,
        Source,
        ProcessorArchitecture,
        PatchGroup,
        Attributes,
        PatchAttributes,
        DeltaPatchHeaderSource,
    }

    /// <summary>
    /// Every file row has an assembly type.
    /// </summary>
    public enum FileAssemblyType
    {
        /// <summary>File is not an assembly.</summary>
        NotAnAssembly,

        /// <summary>File is a Common Language Runtime Assembly.</summary>
        DotNetAssembly,

        /// <summary>File is Win32 SxS assembly.</summary>
        Win32Assembly,
    }

    /// <summary>
    /// PatchAttribute values
    /// </summary>
    [Flags]
    public enum PatchAttributeType
    {
        None = 0,

        /// <summary>Prevents the updating of the file that is in fact changed in the upgraded image relative to the target images.</summary>
        Ignore = 1,

        /// <summary>Set if the entire file should be installed rather than creating a binary patch.</summary>
        IncludeWholeFile = 2,

        /// <summary>Set to indicate that the patch is non-vital.</summary>
        AllowIgnoreOnError = 4,

        /// <summary>Allowed bits.</summary>
        Defined = Ignore | IncludeWholeFile | AllowIgnoreOnError
    }

    public class WixFileTuple : IntermediateTuple
    {
        public WixFileTuple() : base(TupleDefinitions.WixFile, null, null)
        {
        }

        public WixFileTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.WixFile, sourceLineNumber, id)
        {
        }

        public IntermediateField this[WixFileTupleFields index] => this.Fields[(int)index];

        public string File_
        {
            get => (string)this.Fields[(int)WixFileTupleFields.File_];
            set => this.Set((int)WixFileTupleFields.File_, value);
        }

        public FileAssemblyType AssemblyType
        {
            get => (FileAssemblyType)(int)this.Fields[(int)WixFileTupleFields.AssemblyType];
            set => this.Set((int)WixFileTupleFields.AssemblyType, (int)value);
        }

        public string File_AssemblyManifest
        {
            get => (string)this.Fields[(int)WixFileTupleFields.File_AssemblyManifest];
            set => this.Set((int)WixFileTupleFields.File_AssemblyManifest, value);
        }

        public string File_AssemblyApplication
        {
            get => (string)this.Fields[(int)WixFileTupleFields.File_AssemblyApplication];
            set => this.Set((int)WixFileTupleFields.File_AssemblyApplication, value);
        }

        public string Directory_
        {
            get => (string)this.Fields[(int)WixFileTupleFields.Directory_];
            set => this.Set((int)WixFileTupleFields.Directory_, value);
        }

        public int DiskId
        {
            get => (int)this.Fields[(int)WixFileTupleFields.DiskId];
            set => this.Set((int)WixFileTupleFields.DiskId, value);
        }

        public IntermediateFieldPathValue Source
        {
            get => this.Fields[(int)WixFileTupleFields.Source].AsPath();
            set => this.Set((int)WixFileTupleFields.Source, value);
        }

        public string ProcessorArchitecture
        {
            get => (string)this.Fields[(int)WixFileTupleFields.ProcessorArchitecture];
            set => this.Set((int)WixFileTupleFields.ProcessorArchitecture, value);
        }

        public int PatchGroup
        {
            get => (int)this.Fields[(int)WixFileTupleFields.PatchGroup];
            set => this.Set((int)WixFileTupleFields.PatchGroup, value);
        }

        public int Attributes
        {
            get => (int)this.Fields[(int)WixFileTupleFields.Attributes];
            set => this.Set((int)WixFileTupleFields.Attributes, value);
        }

        public PatchAttributeType PatchAttributes
        {
            get => (PatchAttributeType)(int)this.Fields[(int)WixFileTupleFields.PatchAttributes];
            set => this.Set((int)WixFileTupleFields.PatchAttributes, (int)value);
        }

        public string DeltaPatchHeaderSource
        {
            get => (string)this.Fields[(int)WixFileTupleFields.DeltaPatchHeaderSource];
            set => this.Set((int)WixFileTupleFields.DeltaPatchHeaderSource, value);
        }
    }
}