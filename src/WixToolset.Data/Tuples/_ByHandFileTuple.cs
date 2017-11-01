// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Data
{
    using WixToolset.Data.Tuples;

    //public static partial class TupleDefinitionNames
    //{
    //    public const string File = nameof(TupleDefinitionNames.File);
    //}

    /*
    [
        {
        "File" : [
            { "Component" : "string" },
            { "Name" : "string" },
            { "Compressed" : "bool" },
            ]
        },
        {
        "Component": [
            { "Guid" : "string" },
            ]
        },
    ]
    */

    public static partial class TupleDefinitions
    {
        public static readonly IntermediateTupleDefinition FileOriginal = new IntermediateTupleDefinition(
            TupleDefinitionType.File,
            new[]
            {
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.Component), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.Name), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.ShortName), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.Size), IntermediateFieldType.Number),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.Version), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.Language), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.ReadOnly), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.Hidden), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.System), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.Vital), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.Checksum), IntermediateFieldType.Bool),
                new IntermediateFieldDefinition(nameof(FileTupleFieldsOriginal.Compressed), IntermediateFieldType.Bool),
            },
            typeof(FileTuple));
    }
}

namespace WixToolset.Data.Tuples
{
    public enum FileTupleFieldsOriginal
    {
        Component,
        Name,
        ShortName,
        Size,
        Version,
        Language,
        ReadOnly,
        Hidden,
        System,
        Vital,
        Checksum,
        Compressed,
    }

    public class FileTupleOriginal : IntermediateTuple
    {
        public FileTupleOriginal() : base(TupleDefinitions.File, null, null)
        {
        }

        public FileTupleOriginal(SourceLineNumber sourceLineNumber, Identifier id = null) : base(TupleDefinitions.File, sourceLineNumber, id)
        {
        }

        public IntermediateField this[FileTupleFields index] => this.Fields[(int)index];

        public string Component
        {
            get => (string)this.Fields[(int)FileTupleFieldsOriginal.Component]?.Value;
            set => this.Set((int)FileTupleFieldsOriginal.Component, value);
        }
    }
}
