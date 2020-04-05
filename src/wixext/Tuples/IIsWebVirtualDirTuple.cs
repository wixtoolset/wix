// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixToolset.Iis
{
    using WixToolset.Data;
    using WixToolset.Iis.Tuples;

    public static partial class IisTupleDefinitions
    {
        public static readonly IntermediateTupleDefinition IIsWebVirtualDir = new IntermediateTupleDefinition(
            IisTupleDefinitionType.IIsWebVirtualDir.ToString(),
            new[]
            {
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.ComponentRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.WebRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.Alias), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.DirectoryRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.DirPropertiesRef), IntermediateFieldType.String),
                new IntermediateFieldDefinition(nameof(IIsWebVirtualDirTupleFields.ApplicationRef), IntermediateFieldType.String),
            },
            typeof(IIsWebVirtualDirTuple));
    }
}

namespace WixToolset.Iis.Tuples
{
    using WixToolset.Data;

    public enum IIsWebVirtualDirTupleFields
    {
        ComponentRef,
        WebRef,
        Alias,
        DirectoryRef,
        DirPropertiesRef,
        ApplicationRef,
    }

    public class IIsWebVirtualDirTuple : IntermediateTuple
    {
        public IIsWebVirtualDirTuple() : base(IisTupleDefinitions.IIsWebVirtualDir, null, null)
        {
        }

        public IIsWebVirtualDirTuple(SourceLineNumber sourceLineNumber, Identifier id = null) : base(IisTupleDefinitions.IIsWebVirtualDir, sourceLineNumber, id)
        {
        }

        public IntermediateField this[IIsWebVirtualDirTupleFields index] => this.Fields[(int)index];

        public string ComponentRef
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.ComponentRef].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.ComponentRef, value);
        }

        public string WebRef
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.WebRef].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.WebRef, value);
        }

        public string Alias
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.Alias].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.Alias, value);
        }

        public string DirectoryRef
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.DirectoryRef].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.DirectoryRef, value);
        }

        public string DirPropertiesRef
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.DirPropertiesRef].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.DirPropertiesRef, value);
        }

        public string ApplicationRef
        {
            get => this.Fields[(int)IIsWebVirtualDirTupleFields.ApplicationRef].AsString();
            set => this.Set((int)IIsWebVirtualDirTupleFields.ApplicationRef, value);
        }
    }
}