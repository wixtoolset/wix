// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebDir = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebDir.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.WebRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.Path), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.DirPropertiesRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebDirTupleFields.ApplicationRef), IntermediateFieldType.String),
            },
            typeof(IIsWebDirTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebDirTupleFields
    {
        ComponentRef,
        WebRef,
        Path,
        DirPropertiesRef,
        ApplicationRef,
    }

    public class IIsWebDirTuple : IntermediateTuple
    {
        public IIsWebDirTuple() : base(IisTupleDefinitions.IIsWebDir, null, null)
        {
        }

        public IIsWebDirTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebDir, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebDirTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)IIsWebDirTupleFields.ComponentRef].AsString();
            set => this.Set((int)IIsWebDirTupleFields.ComponentRef, value);
        }

        public string WebRef
        {
            get => this.Fields[(int)IIsWebDirTupleFields.WebRef].AsString();
            set => this.Set((int)IIsWebDirTupleFields.WebRef, value);
        }

        public string Path
        {
            get => this.Fields[(int)IIsWebDirTupleFields.Path].AsString();
            set => this.Set((int)IIsWebDirTupleFields.Path, value);
        }

        public string DirPropertiesRef
        {
            get => this.Fields[(int)IIsWebDirTupleFields.DirPropertiesRef].AsString();
            set => this.Set((int)IIsWebDirTupleFields.DirPropertiesRef, value);
        }

        public string ApplicationRef
        {
            get => this.Fields[(int)IIsWebDirTupleFields.ApplicationRef].AsString();
            set => this.Set((int)IIsWebDirTupleFields.ApplicationRef, value);
        }
    }
}