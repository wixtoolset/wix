// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition Directory = new IntermediateTupleDefinition(
            TupleDefinitionType.Directory,
            new[]
            {
                new IntermediateFieldDefinition(nameof(DirectoryTupleFields.ParentDirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectoryTupleFields.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectoryTupleFields.ShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectoryTupleFields.SourceName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectoryTupleFields.SourceShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(DirectoryTupleFields.ComponentGuidGenerationSeed), IntermediateFieldType.String),
            },
            typeof(DirectoryTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum DirectoryTupleFields
    {
        ParentDirectoryRef,
        Name,
        ShortName,
        SourceName,
        SourceShortName,
        ComponentGuidGenerationSeed,
    }

    public class DirectoryTuple : IntermediateTuple
    {
        public DirectoryTuple() : base(TupleDefinitions.Directory, null, null)
        {
        }

        public DirectoryTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.Directory, sourceLineNumber, id)
        {
        }

        public IntermediateField this[DirectoryTupleFields index] => this.Fields[(int)index];

        public string ParentDirectoryRef
        {
            get => (string)this.Fields[(int)DirectoryTupleFields.ParentDirectoryRef];
            set => this.Set((int)DirectoryTupleFields.ParentDirectoryRef, value);
        }

        public string Name
        {
            get => (string)this.Fields[(int)DirectoryTupleFields.Name];
            set => this.Set((int)DirectoryTupleFields.Name, value);
        }

        public string ShortName
        {
            get => (string)this.Fields[(int)DirectoryTupleFields.ShortName];
            set => this.Set((int)DirectoryTupleFields.ShortName, value);
        }

        public string SourceName
        {
            get => (string)this.Fields[(int)DirectoryTupleFields.SourceName];
            set => this.Set((int)DirectoryTupleFields.SourceName, value);
        }

        public string SourceShortName
        {
            get => (string)this.Fields[(int)DirectoryTupleFields.SourceShortName];
            set => this.Set((int)DirectoryTupleFields.SourceShortName, value);
        }

        public string ComponentGuidGenerationSeed
        {
            get => (string)this.Fields[(int)DirectoryTupleFields.ComponentGuidGenerationSeed];
            set => this.Set((int)DirectoryTupleFields.ComponentGuidGenerationSeed, value);
        }
    }
}
